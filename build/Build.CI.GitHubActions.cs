// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;

[CustomGitHubActions("Build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master", "dev", "alpha", "beta" },
    OnPullRequestBranches = new[] { "master", "dev", "alpha", "beta" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(Compile) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    PublishArtifacts = false,
    EnableGitHubContext = true)
]

[CustomGitHubActions("Tests",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master", "dev", "alpha", "beta" },
    OnPullRequestBranches = new[] { "master", "dev", "alpha", "beta" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(Test) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    EnableGitHubContext = true)
]

[CustomGitHubActions("Release",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    //OnPushBranches = new[] { "main", "dev", "alpha", "beta" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(Release) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    PublishArtifacts = true,
    EnableGitHubContext = true,    
    ImportSecrets = new[] { "NUGET_API_KEY", "GITHUB_TOKEN" })]
partial class Build
{
}
class CustomGitHubActionsAttribute : GitHubActionsAttribute
{
    public CustomGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var job = base.GetJobs(image, relevantTargets);

        try
        {
            var newSteps = new List<GitHubActionsStep>(job.Steps);
            foreach (var version in new[] { "6.0.*", "5.0.*" })
            {
                newSteps.Insert(1, new GitHubActionsSetupDotNetStep
                {
                    Version = version
                });
            }

            job.Steps = newSteps.ToArray();
        }
        catch(Exception ex)
        {

        }
        return job;
    }
}

class GitHubActionsSetupDotNetStep : GitHubActionsStep
{
    public string Version { get; init; }

    public override void Write(CustomFileWriter writer)
    {
        try
        {
            writer.WriteLine("- uses: actions/setup-dotnet@v1");

            using (writer.Indent())
            {
                writer.WriteLine("with:");
                using (writer.Indent())
                {
                    writer.WriteLine($"dotnet-version: {Version}");
                }
            }
        }
        catch (Exception ex)
        {

        }
    }
}