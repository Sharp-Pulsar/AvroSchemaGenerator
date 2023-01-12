// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;

[CustomGitHubActions("pr_validation",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    FetchDepth = 0,
    OnPushBranches = new[] { "main", "dev", "alpha", "beta" },
    OnPullRequestBranches = new[] { "main", "dev", "alpha", "beta" },
    InvokedTargets = new[] { nameof(All) },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    EnableGitHubToken = true,
    PublishArtifacts = true)
]


[CustomGitHubActions("Release",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    FetchDepth = 0,
    OnPushTags = new[] { "*" },
    InvokedTargets = new[] { nameof(Release) },
    PublishArtifacts = true,
    EnableGitHubToken = true,
    ImportSecrets = new[] { "NUGET_API_KEY" })]
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
            foreach (var version in new[] { "7.0.*" })
            {
                newSteps.Insert(1, new GitHubActionsSetupDotNetStep
                {
                    Version = version
                });
            }

            job.Steps = newSteps.ToArray();
        }
        catch (Exception ex)
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