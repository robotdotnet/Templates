// Target - The task you want to start. Runs the Default task if not specified.
var target = Argument("Target", "Default");
// Configuration - The build configuration (Debug/Release) to use.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if an Environment variable exists, use that.
var configuration = 
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") != null ? EnvironmentVariable("BuildNumber") : "Release";
// The build number to use in the version number of the built NuGet packages.
// There are multiple ways this value can be passed, this is a common pattern.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if running on AppVeyor, get it's build number.
// 3. Otherwise if running on Travis CI, get it's build number.
// 4. Otherwise if an Environment variable exists, use that.
// 5. Otherwise default the build number to 0.
var buildNumberInt =
    HasArgument("BuildNumber") ? Argument<int>("BuildNumber") :
    AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number :
    TravisCI.IsRunningOnTravisCI ? TravisCI.Environment.Build.BuildNumber :
    EnvironmentVariable("BuildNumber") != null ? int.Parse(EnvironmentVariable("BuildNumber")) : 0;

var buildNumber = buildNumberInt.ToString("D4");

var buildVersion = "1.0.0";

var buildType = (AppVeyor.IsRunningOnAppVeyor || TravisCI.IsRunningOnTravisCI) ? "ci-"  : "local-";

buildType = buildType + buildNumber;

var tagName = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
if (tagName != null) {
    // On AppVeyor
    buildVersion =  tagName.Substring(1);
    if (!tagName.Contains("-")) {
        // Building a full release
        buildType = "";
    } else {
        buildVersion = buildVersion.Substring(0, (tagName.IndexOf("-") - 1));
        buildType = tagName.Substring(tagName.IndexOf("-") + 1);
    }
}

string msBuildVersionArgs = "/p:VersionPrefix=\"" + buildVersion + "\" /p:VersionSuffix=\"" + buildType + "\"";

Console.WriteLine("BuildNumber: " + msBuildVersionArgs);

var artifactsDirectory = Directory("./artifacts");
var packagesDirectory = Directory("./packages");
var nuspecFile = GetFiles("./**/*.nuspec").First().ToString();
var nuspecContent = string.Empty;

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
        DeleteDirectories(GetDirectories("**/bin"), true);
        DeleteDirectories(GetDirectories("**/obj"), true);
    });


Task("Version")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        nuspecContent = System.IO.File.ReadAllText(nuspecFile);
        string version = buildVersion + "-" + buildType;
        System.IO.File.WriteAllText(nuspecFile, nuspecContent.Replace("****", version));
        Information("Version set to " + version);
    });


Task("Pack")
    .IsDependentOn("Version")
    .Does(() =>
    {
        NuGetPack(
            nuspecFile,
            new NuGetPackSettings()
            {
                OutputDirectory = artifactsDirectory
            });
        System.IO.File.WriteAllText(nuspecFile, nuspecContent);
    });

Task("Default")
    .IsDependentOn("Pack");

RunTarget(target);