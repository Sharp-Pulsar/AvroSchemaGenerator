using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[DotNetVerbosityMapping]
[ShutdownDotNetAfterServerBuild]
partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Pack);

    [CI] readonly GitHubActions GitHubActions;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "net6.0")] readonly GitVersion GitVersion;

    readonly string _githubContext = EnvironmentInfo.GetVariable<string>("GITHUB_CONTEXT");
    //readonly string _githubContext = JsonSerializer.Deserialize<JsonElement>(EnvironmentInfo.GetVariable<string>("GITHUB_CONTEXT"));
    
    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter] string GithubSource = "https://nuget.pkg.github.com/OWNER/index.json";

    [Parameter] [Secret] string NuGetApiKey;
    [Parameter] [Secret] string GitHubApiKey;
    AbsolutePath TestsDirectory => RootDirectory;
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestSourceDirectory => RootDirectory / "AvroSchemaGenerator.Tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {

        });

    Target Restore => _ => _
        .Executes(() =>
        {
            try
            {
                DotNetRestore(s => s
               .SetProjectFile(Solution));
            }
            catch (Exception ex)
            {
                Information(ex.ToString());
            }
        });

    Target Compile => _ => _
        .After(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GetVersion())
                .SetFramework("net6.0")
                .SetFileVersion(GetVersion())
                .SetVerbosity(verbosity: DotNetVerbosity.Detailed)
                .EnableNoRestore());
        });
    Target Test => _ => _
        .After(Compile)
        .Executes(() =>
        {
            var projectName = "AvroSchemaGenerator.Tests";
            var project = Solution.GetProjects("*.Tests").First();
            Information($"Running tests from {projectName}");
            DotNetTest(c => c
                   .SetProjectFile(project)
                   .SetConfiguration(Configuration.ToString())
                   .SetFramework("net6.0")   
                   .SetVerbosity(verbosity: DotNetVerbosity.Detailed)
                   .EnableNoBuild());
        });

    Target Pack => _ => _
      .DependsOn(Test)
      .Executes(() =>
      {
          var project = Solution.GetProject("AvroSchemaGenerator");
          DotNetPack(s => s
              .SetProject(project)
              .SetConfiguration(Configuration)
              .EnableNoBuild()
              
              .EnableNoRestore()
              .SetAssemblyVersion(GetVersion())
              .SetVersion(GetVersion())
              .SetPackageReleaseNotes(GetReleasenote())
              .SetDescription("Generate Avro Schema with support for RECURSIVE SCHEMA")
              .SetPackageTags("Avro", "Schema Generator")
              .AddAuthors("Ebere Abanonu (@mestical)")
              .SetPackageProjectUrl("https://github.com/eaba/AvroSchemaGenerator")
              .SetOutputDirectory(ArtifactsDirectory / "nuget")); ;

      });
    Target PackBeta => _ => _
      .DependsOn(Test)
      .Executes(() =>
      {
          var project = Solution.GetProject("AvroSchemaGenerator");
          DotNetPack(s => s
              .SetProject(project)
              .SetConfiguration(Configuration)
              .EnableNoBuild()
              .EnableNoRestore()
              .SetAssemblyVersion($"{GetVersion()}-beta")
              .SetVersion($"{GetVersion()}-beta")
              .SetPackageReleaseNotes(GetReleasenote())
              .SetDescription("Generate Avro Schema with support for RECURSIVE SCHEMA")
              .SetPackageTags("Avro", "Schema Generator")
              .AddAuthors("Ebere Abanonu (@mestical)")
              .SetPackageProjectUrl("https://github.com/eaba/AvroSchemaGenerator")
              .SetOutputDirectory(ArtifactsDirectory / "nuget")); ;

      });
    Target Push => _ => _
      .DependsOn(Pack)
      .Requires(() => NugetApiUrl)
      .Requires(() => !NuGetApiKey.IsNullOrEmpty())
      .Requires(() => !GitHubApiKey.IsNullOrEmpty())
      //.Requires(() => !BuildNumber.IsNullOrEmpty())
      .Requires(() => Configuration.Equals(Configuration.Release))
      .Executes(() =>
      {
          
          GlobFiles(ArtifactsDirectory / "nuget", "*.nupkg")
              .Where(x => !x.EndsWith("symbols.nupkg"))
              .ForEach(x =>
              {
                  Assert.NotNullOrEmpty(x);
                  DotNetNuGetPush(s => s
                      .SetTargetPath(x)
                      .SetSource(NugetApiUrl)
                      .SetApiKey(NuGetApiKey)
                  );

                  /*DotNetNuGetPush(s => s
                      .SetApiKey(GitHubApiKey)
                      .SetSymbolApiKey(GitHubApiKey)
                      .SetTargetPath(x)
                      .SetSource(GithubSource)
                      .SetSymbolSource(GithubSource));*/
              });
      });
    Target PushBeta => _ => _
      .DependsOn(PackBeta)
      .Requires(() => NugetApiUrl)
      .Requires(() => !NuGetApiKey.IsNullOrEmpty())
      .Requires(() => !GitHubApiKey.IsNullOrEmpty())
      //.Requires(() => !BuildNumber.IsNullOrEmpty())
      .Requires(() => Configuration.Equals(Configuration.Release))
      .Executes(() =>
      {
          GlobFiles(ArtifactsDirectory / "nuget", "*.nupkg")
              .Where(x => !x.EndsWith("symbols.nupkg"))
              .ForEach(x =>
              {
                  Assert.NotNullOrEmpty(x);
                  DotNetNuGetPush(s => s
                      .SetTargetPath(x)
                      .SetSource(NugetApiUrl)
                      .SetApiKey(NuGetApiKey)
                  );
              });
      });

    static void Information(string info)
    {
        Serilog.Log.Information(info);  
    }
    static string GetVersion()
    {
        return "2.5.1";
    }
    static string GetReleasenote()
    {
        return "Added README.md with Nuget package";
    }
}
