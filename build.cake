#addin "Cake.Json"

#reference "System.Net"
#reference "System.Net.Http"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

class BuildState {
    public string Version { get; set; }
}

bool GracefullyAbort;
string LatestVersion = string.Empty;
Uri LatestZip = null;
BuildState State = null;


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
                GracefullyAbort = true;
            }
        }
    })
;

Task("Run Pack")
    .WithCriteria( () => !GracefullyAbort )
    .IsDependentOn("Check Latest")
    .Does(() =>
    {
        CakeExecuteScript("./pack.cake", new CakeSettings {
            Arguments = new Dictionary<string, string>
            {
                { "latestUrl", LatestZip.ToString() },
                { "packageVersion", LatestVersion }
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

        SerializeJsonToFile("./state.json", State);
    })
;

Task("Default")
    .IsDependentOn("Save State")
    .Does( () => {
        Information($"Current Version: {State.Version}");
        Information($"Latest Version: {LatestVersion}");
        Information($"Latest Zip: {LatestZip}");
    })
    .OnError(exception =>
    {
        Warning("Error is being handled.");
    });
;

RunTarget("Default");
