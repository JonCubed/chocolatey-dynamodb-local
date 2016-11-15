#addin "Cake.Json"

#reference "System.Net"
#reference "System.Net.Http"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

bool GracefullyAbort;
Uri LatestZip = null;
var Force = HasArgument("Force");
var SkipHash = HasArgument("SkipHash");

BuildState State = null;
string LatestVersion = string.Empty;
string LatestHash = string.Empty;

class BuildState {
    public string Version { get; set; }
    public string Hash { get; set; }
}

Task("Load State")
    .Does(() =>
    {
        if(!FileExists("./state.json")) {
            Information("No State file was found");
            State = new BuildState();
            return;
        }

        State = DeserializeJsonFromFile<BuildState>("./state.json");

        Information("State loaded");
        Information($"Version: {State.Version}");
        Information($"Hash: {State.Hash}");
    })
;

Task("Check Latest")
    .IsDependentOn("Load State")
    .Does( context =>
    {
        var httpClientHandler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        using ( var client = new HttpClient(httpClientHandler) )
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Cake", context.Environment.Runtime.CakeVersion.ToString()));

            var response = client.GetAsync("http://dynamodb-local.s3-website-us-west-2.amazonaws.com/dynamodb_local_latest.zip").GetAwaiter().GetResult();

            Information($"StatusCode: {response.StatusCode}");

            if (response.StatusCode != HttpStatusCode.Found)
            {
                throw new Exception("Unexpected response, latest version unknown.");
            }

            if ( response.Headers.Location == null )
            {
                throw new Exception("Unexpected result, latest version unknown.");
            }

            LatestZip = response.Headers.Location;

            Information($"location: {LatestZip.AbsolutePath}");

            var version = Regex.Match(LatestZip.AbsolutePath, @"(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})");

            if(!version.Success)
            {
                throw new Exception("Unexpected version number, latest version unknown.");
            }

            LatestVersion = $"{version.Groups["year"]}.{version.Groups["month"]}.{version.Groups["day"]}";
            Information($"Latest version: {LatestVersion}");

            if (State.Version == LatestVersion) {
                Warning("Current version is the latest version");

                if(Force) {
                    Warning("Build is being forced, continue packing anyways.");
                    return;
                }

                GracefullyAbort = true;
            }
            else if (SkipHash)
            {
                // new version so don't skip hash calculation next
                Warning("New version found, cannot skip hash calculation.");
                SkipHash = false;
            }
        }
    })
;

Task("Get Latest Hash")
    .WithCriteria( () => !GracefullyAbort)
    .IsDependentOn("Check Latest")
    .Does(() =>
    {
        if (SkipHash && !string.IsNullOrEmpty(State.Hash))
        {
            Warning($"Skipping hash calculation, re-using hash from state: {State.Hash}");
            LatestHash = State.Hash;
            return;
        }
        else if (SkipHash)
        {
            Information("State does not contain a hash, ignoring SkipHash flag");
        }

        var resource = DownloadFile(LatestZip.ToString());

        LatestHash = CalculateFileHash(resource.FullPath).ToHex();
        Information($"Zip file SHA256 hash: {LatestHash}");
    })
;

Task("Run Pack")
    .WithCriteria( () => !GracefullyAbort )
    .IsDependentOn("Get Latest Hash")
    .Does(() =>
    {
        CakeExecuteScript("./pack.cake", new CakeSettings {
            Arguments = new Dictionary<string, string>
            {
                { "latestUrl", LatestZip.ToString() },
                { "packageVersion", LatestVersion },
                { "latestHash", LatestHash }
            }
            ,ArgumentCustomization = builder => {
                builder.Append("-experimental");
                return builder;
            }
        });
    });

Task("Save State")
    .WithCriteria( () => !GracefullyAbort )
    .IsDependentOn("Run Pack")
    .Does(() =>
    {
        State.Version = LatestVersion;
        State.Hash = LatestHash;

        SerializeJsonToFile("./state.json", State);
    })
;

Task("Default")
    .IsDependentOn("Save State")
    .Does( () => {
        Information($"Current Version: {State.Version}");
        Information($"Latest Version: {LatestVersion}");
        Information($"Latest Hash: {LatestHash}");
        Information($"Latest Zip: {LatestZip}");
    })
    .OnError(exception =>
    {
        Warning("Error is being handled.");
    });
;

RunTarget("Default");
