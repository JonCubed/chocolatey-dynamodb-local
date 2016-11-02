#addin "Cake.FileHelpers"

#reference "System.Net"
#reference "System.Net.Http"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

string CurrentVersion = string.Empty;
string LatestVersion = string.Empty;
string Hash = null;
Uri LatestZip = null;

Task("Check Latest")
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
        }
    })
;

Task("Get Latest Hash")
    .IsDependentOn("Check Latest")
    .WithCriteria(() => !string.IsNullOrEmpty(LatestVersion) && CurrentVersion != LatestVersion)
    .Does(() =>
    {
        var resource = DownloadFile(LatestZip);

        Hash = CalculateFileHash(resource.FullPath).ToHex();
        Information($"Zip file SHA256 hash: {Hash}");
    })
;

Task("Setup Package")
    .IsDependentOn("Get Latest Hash")
    .Does(() =>
    {
        Information("Cleaning temp directory");
        CleanDirectory("./temp/package");

        Information("Setup package files");
        CopyDirectory("./package","./temp/package");

        DeleteFiles("./temp/package/*.ignore");
        DeleteFiles("./temp/package/tools/*.ignore");
    })
;

Task("Replace Tokens")
    .IsDependentOn("Setup Package")
    .Does(() =>
    {
        Information($"Replacing [URL] with '{LatestZip.ToString()}'");
        ReplaceTextInFiles("./temp/package/tools/*.ps1", "[URL]", LatestZip.ToString());

        Information($"Replacing [CHECKSUM] with '{Hash}'");
        ReplaceTextInFiles("./temp/package/tools/*.ps1", "[CHECKSUM]", Hash);
    })
;

Task("Default")
    .IsDependentOn("Pack")
    .Does( () => {
        Information($"Current Version: {CurrentVersion}");
        Information($"Latest Version: {LatestVersion}");
        Information($"Latest Zip: {LatestZip}");
    })
    .OnError(exception =>
    {
        Warning("Error is being handled.");
    });
;

Task("Pack")
    .IsDependentOn("Replace Tokens")
    .Does(() =>
    {
        var chocolateyPackSettings   = new ChocolateyPackSettings
        {
            // Id                      = "TestChocolatey",
            // Title                   = "The tile of the package",
            Version                 = LatestVersion,
            // Authors                 = new[] {"Amazon"},
            // Owners                  = new[] {"Jonathan Kuleff"},
            // Summary                 = "Excellent summary of what the package does",
            // Description             = "The description of the package",
            // ProjectUrl              = new Uri("https://github.com/SomeUser/TestChocolatey/"),
            // PackageSourceUrl        = new Uri("https://github.com/SomeUser/TestChocolatey/"),
            // ProjectSourceUrl        = new Uri("https://github.com/SomeUser/TestChocolatey/"),
            // DocsUrl                 = new Uri("https://github.com/SomeUser/TestChocolatey/"),
            // MailingListUrl          = new Uri("https://github.com/SomeUser/TestChocolatey/"),
            // BugTrackerUrl           = new Uri("https://github.com/SomeUser/TestChocolatey/"),
            // Tags                    = new [] {"Cake", "Script", "Build"},
            // Copyright               = "Some company 2015",
            // LicenseUrl              = new Uri("https://github.com/SomeUser/TestChocolatey/blob/master/LICENSE.md"),
            // RequireLicenseAcceptance= false,
            // IconUrl                 = new Uri("http://cdn.rawgit.com/SomeUser/TestChocolatey/master/icons/testchocolatey.png"),
            //ReleaseNotes            = new [] {"Bug fixes", "Issue fixes", "Typos"},
            // Files                   = new [] {
                                                //  new ChocolateyNuSpecContent {Source = "bin/TestChocolatey.dll", Target = "bin"},
                                            //   },
            // Debug                   = false,
            // Verbose                 = false,
            // Force                   = false,
            // Noop                    = false,
            // LimitOutput             = false,
            // ExecutionTimeout        = 13,
            // CacheLocation           = @"C:\temp",
            //AllowUnofficial          = false
        };

        ChocolateyPack("./temp/package/dynamodb-local.nuspec", chocolateyPackSettings);
    })
;

RunTarget("Default");
