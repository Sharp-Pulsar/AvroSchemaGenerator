using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions("Build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master", "dev" },
    OnPullRequestBranches = new[] { "master", "dev" },

    InvokedTargets = new[] { nameof(Compile) })]

[GitHubActions("Tests",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master", "dev" },
    OnPullRequestBranches = new[] { "master", "dev" },
    InvokedTargets = new[] { nameof(Test) })]


[GitHubActions("PublishBeta",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "beta_branch" },
    InvokedTargets = new[] { nameof(PushBeta) })]

[GitHubActions("Publish",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Push) })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Test);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "net5.0")] readonly GitVersion GitVersion;

    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter] string GithubSource = "https://nuget.pkg.github.com/OWNER/index.json";

    //[Parameter] string NugetApiKey = Environment.GetEnvironmentVariable("SHARP_PULSAR_NUGET_API_KEY");
    [Parameter("NuGet API Key", Name = "NUGET_API_KEY")]
    readonly string NugetApiKey;

    [Parameter("GitHub Build Number", Name = "BUILD_NUMBER")]
    readonly string BuildNumber;

    [Parameter("GitHub Access Token for Packages", Name = "GH_API_KEY")]
    readonly string GitHubApiKey;
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
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion("1.9.0")
                .SetFileVersion("1.9.0")
                //.SetInformationalVersion("1.9.0")
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var projectName = "AvroSchemaGenerator.Tests";
            var project = Solution.GetProjects("*.Tests").First();
            Information($"Running tests from {projectName}");

            foreach (var fw in project.GetTargetFrameworks())
            {
                Information($"Running for {projectName} ({fw}) ...");
                try
                {
                    DotNetTest(c => c
                        .SetProjectFile(project)
                        .SetConfiguration(Configuration.ToString())
                        .SetFramework(fw)
                        //.SetDiagnosticsFile(TestsDirectory)
                        //.SetLogger("trx")
                        .SetVerbosity(verbosity: DotNetVerbosity.Normal)
                        .EnableNoBuild()); ;
                }
                catch (Exception ex)
                {
                    Information(ex.Message);
                }
            }
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
              .SetAssemblyVersion("2.3.1")
              .SetVersionPrefix("2.3.1")
              .SetPackageReleaseNotes($"Fix wrong version in nuget")
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
              .SetAssemblyVersion($"2.3.1-beta")
              .SetVersionPrefix("2.3.1")
              .SetPackageReleaseNotes($"Fix wrong version in nuget")
              .SetVersionSuffix($"beta")
              .SetDescription("Generate Avro Schema with support for RECURSIVE SCHEMA")
              .SetPackageTags("Avro", "Schema Generator")
              .AddAuthors("Ebere Abanonu (@mestical)")
              .SetPackageProjectUrl("https://github.com/eaba/AvroSchemaGenerator")
              .SetOutputDirectory(ArtifactsDirectory / "nuget")); ;

      });
    Target Push => _ => _
      .DependsOn(Test)
      .DependsOn(Pack)
      .Requires(() => NugetApiUrl)
      .Requires(() => !NugetApiKey.IsNullOrEmpty())
      .Requires(() => !GitHubApiKey.IsNullOrEmpty())
      .Requires(() => !BuildNumber.IsNullOrEmpty())
      .Requires(() => Configuration.Equals(Configuration.Release))
      .Executes(() =>
      {
          GlobFiles(ArtifactsDirectory / "nuget", "*.nupkg")
              .NotEmpty()
              .Where(x => !x.EndsWith("symbols.nupkg"))
              .ForEach(x =>
              {
                  DotNetNuGetPush(s => s
                      .SetTargetPath(x)
                      .SetSource(NugetApiUrl)
                      .SetApiKey(NugetApiKey)
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
      .DependsOn(Test)
      .DependsOn(PackBeta)
      .Requires(() => NugetApiUrl)
      .Requires(() => !NugetApiKey.IsNullOrEmpty())
      .Requires(() => !GitHubApiKey.IsNullOrEmpty())
      .Requires(() => !BuildNumber.IsNullOrEmpty())
      .Requires(() => Configuration.Equals(Configuration.Release))
      .Executes(() =>
      {
          GlobFiles(ArtifactsDirectory / "nuget", "*.nupkg")
              .NotEmpty()
              .Where(x => !x.EndsWith("symbols.nupkg"))
              .ForEach(x =>
              {
                  DotNetNuGetPush(s => s
                      .SetTargetPath(x)
                      .SetSource(NugetApiUrl)
                      .SetApiKey(NugetApiKey)
                  );

                  /*DotNetNuGetPush(s => s
                      .SetApiKey(GitHubApiKey)
                      .SetSymbolApiKey(GitHubApiKey)
                      .SetTargetPath(x)
                      .SetSource(GithubSource)
                      .SetSymbolSource(GithubSource));*/
              });
      });

    static void Information(string info)
    {
        Logger.Info(info);
    }
}
