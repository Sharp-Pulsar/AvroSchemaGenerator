// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using Nuke.Common.CI.GitHubActions;

[GitHubActions("Build",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master", "dev" },
    OnPullRequestBranches = new[] { "master", "dev" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(Compile) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    PublishArtifacts = false,
    EnableGitHubContext = true)
]

[GitHubActions("Tests",
    GitHubActionsImage.WindowsLatest,
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "master", "dev" },
    OnPullRequestBranches = new[] { "master", "dev" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(Test) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    EnableGitHubContext = true)
]


[GitHubActions("PublishBeta",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "beta_branch" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(PushBeta) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    PublishArtifacts = true,
    EnableGitHubContext = true,
    ImportSecrets = new[] { "NUGET_API_KEY", "GITHUB_TOKEN" })]

[GitHubActions("Publish",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPushBranches = new[] { "main" },
    CacheKeyFiles = new[] { "global.json", "SchemaGenerator/**/*.csproj" },
    InvokedTargets = new[] { nameof(Push) },
    OnPushExcludePaths = new[] { "docs/**/*", "package.json", "README.md" },
    PublishArtifacts = true,
    EnableGitHubContext = true,    
    ImportSecrets = new[] { "NUGET_API_KEY", "GITHUB_TOKEN" })
]
partial class Build
{
}