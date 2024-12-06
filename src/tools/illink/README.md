# Trimming Tools

This project hosts various tools and msbuild tasks which are used when trimming managed applications with modern .NET.

## IL Trimmer

The [IL Trimmer](src/linker/README.md) is the developer's tool that can be used to produce apps that contain only code and assembly dependencies which are necessary to run the app. It's fully integrated into
.NET SDKs via [ILLink.Tasks](src/ILLink.Tasks/README.md) build task and exposed via `dotnet publish` trimming [settings](https://learn.microsoft.com/dotnet/core/deploying/trim-self-contained#trim-your-app---cli).

The trimmer is always enabled for all size sensitive .NET workloads like Blazor WebAssembly, Xamarin or .NET mobile and can be manually enabled for other project types. The default apps trimming setting can be further customized by using a number of [msbuild properties](https://learn.microsoft.com/dotnet/core/deploying/trimming-options).

## Dependencies Analyzer

The [analyzer](src/analyzer/README.md) is a tool to analyze dependencies which were recorded during trimmer processing. It tracks details about reasons and connection between elements to keep it in the resulting linked assembly. It can be used to better understand the dependencies between different types and members to help further reduce the linked output.

## Trimming Lens

The [tlens](src/tlens/README.md) is another tool for developers which can be used to explore ways to reduce the size of trimmed apps or exploring libraries readiness for trimming. The tool produces a recommendation where the compiled source could be improved to produce even smaller outputs when trimmed using the trimmer.

## Source Code Analyzer

Another tool available for developers is implemented as [Roslyn Analyzer](src/ILLink.RoslynAnalyzer) which runs on source code and warns developers about code patterns and APIs which are problematic when building code which could be used with trimmed apps.

# Contributing
We welcome contributions! Many developers have helped make this project better by reporting [issues](https://github.com/dotnet/runtime/issues) or contributing [pull requests](https://github.com/dotnet/runtime/pulls) and tagging with the label `area-Tools-ILLink`.
