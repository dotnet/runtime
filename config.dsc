// Root BuildXL configuration for the runtime repo test build.
//
// External rule SDKs are fetched via GitRepository/Download so the workspace
// can build against the latest pinned BuildXL rule snapshots without relying
// on sibling checkouts.
config({
    resolvers: [
        {
            kind: "DScript",
            modules: [
                f`${Environment.getPathValue("BUILDXL_BIN")}/Sdk/Sdk.Prelude/package.config.dsc`,
                f`${Environment.getPathValue("BUILDXL_BIN")}/Sdk/Sdk.Transformers/package.config.dsc`,
                f`${Environment.getPathValue("BUILDXL_BIN")}/Sdk/Sdk.Deployment/module.config.dsc`,
                f`${Environment.getPathValue("BUILDXL_BIN")}/Sdk/Sdk.Managed.Shared/module.config.dsc`,
            ]
        },
        {
            kind: "GitRepository",
            repositories: [
                {
                    moduleName: "bxl_rules_repo",
                    owner: "agocke",
                    repository: "bxl_rules",
                    commit: "3a494442b296c7459a4efdcfdccda5d66b6fe41a",
                },
                {
                    moduleName: "bxl_rules_dotnet_repo",
                    owner: "agocke",
                    repository: "bxl_rules_dotnet",
                    commit: "5528cd0c8f9a661bda18869b1fec1090fc0d5011",
                },
            ],
        },
        {
            kind: "DScript",
            modules: [
                // Repo-specific definitions
                f`defs/module.config.dsc`,

                // Common test support libraries
                f`src/tests/Common/module.config.dsc`,

                // Repo-specific test macro (like src/tests/live_test.bzl)
                f`src/tests/coreclr_test/module.config.dsc`,

                // Repo test root: owns all BUILD.dsc files under src/tests/
                // (recursively, stopping at nested module boundaries like
                // src/tests/Common/ and src/tests/coreclr_test/).
                f`src/tests/module.config.dsc`
            ]
        },
        {
            kind: "Download",
            downloads: [{
                moduleName: "DotNetSdk",
                url: "https://ci.dot.net/public/Sdk/11.0.100-preview.5.26227.104/dotnet-sdk-11.0.100-preview.5.26227.104-linux-x64.tar.gz",
                archiveType: "tgz",
            }],
        },
        {
            kind: "Nuget",
            repositories: {
                "nuget.org": "https://api.nuget.org/v3/index.json",
                "dotnet-public": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json",
                "dotnet-tools": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json",
                "dotnet-eng": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json",
                "dotnet11": "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json"
            },
            packages: [
                { id: "Microsoft.NETCore.App.Ref", version: "11.0.0-preview.5.26264.105", tfm: ".NETCoreApp,Version=v11.0",
                  dependentPackageIdsToSkip: ["*"], dependentPackageIdsToIgnore: ["*"] },
                { id: "Microsoft.DotNet.XUnitAssert", version: "3.2.2-beta.26211.102", tfm: ".NETCoreApp,Version=v10.0",
                  dependentPackageIdsToSkip: ["*"], dependentPackageIdsToIgnore: ["*"] },
                { id: "xunit.extensibility.core", version: "2.9.3", tfm: ".NETStandard,Version=v1.1",
                  dependentPackageIdsToSkip: ["*"], dependentPackageIdsToIgnore: ["*"] },
                { id: "Microsoft.DotNet.XUnitExtensions", version: "11.0.0-beta.26211.102", tfm: ".NETCoreApp,Version=v10.0",
                  dependentPackageIdsToSkip: ["*"], dependentPackageIdsToIgnore: ["*"] },
                { id: "xunit.abstractions", version: "2.0.3", tfm: ".NETStandard,Version=v1.0",
                  dependentPackageIdsToSkip: ["*"], dependentPackageIdsToIgnore: ["*"] }
            ]
        }
    ],

    mounts: [
        {
            name: a`SourceRoot`,
            path: p`.`,
            trackSourceFileChanges: true,
            isReadable: true
        },
        {
            name: a`BuildXLSdk`,
            path: p`${Environment.getPathValue("BUILDXL_BIN")}/Sdk`,
            trackSourceFileChanges: true,
            isReadable: true
        }
    ]
});
