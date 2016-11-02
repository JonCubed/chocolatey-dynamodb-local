#addin "Cake.FileHelpers"

var UrlToLatestZip = Argument<string>("latestUrl");
var LatestVersion = Argument<string>("packageVersion");
string Hash;

Task("Get Latest Hash")
    .Does(() =>
    {
        var resource = DownloadFile(UrlToLatestZip);

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
        Information($"Replacing [URL] with '{UrlToLatestZip}'");
        ReplaceTextInFiles("./temp/package/tools/*.ps1", "[URL]", UrlToLatestZip);

        Information($"Replacing [CHECKSUM] with '{Hash}'");
        ReplaceTextInFiles("./temp/package/tools/*.ps1", "[CHECKSUM]", Hash);
    })
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

RunTarget("Pack");