# Tiered Compilation Guide

# Introduction

.NET Core and ASP.NET ship mostly as [ReadyToRun](https://github.com/dotnet/coreclr/blob/master/Documentation/botr/readytorun-overview.md) (R2R) for better startup performance, but steady-state performance is lower compared to only using the JIT due to some R2R versioning constraints.

Tiered Compilation aims to get the best of both:
- Using pregenerated code or jitting methods more quickly with fewer optimizations first, it aims to produce at least as good startup performance
- After determining which methods are hot, and jitting those methods again with more optimizations, it aims to produce at least as good steady-state performance
- Long-term, tiered compilation opens up opportunities for more optimizations, such as aggressive optimizations justified by what the app is doing (not done currently). Where ahead-of-time (AOT) compilation would require profiling the app ahead-of-time on scenarios that are hopefully representative of how the app would typically run, tiered compilation with run-time profiling would optimize based on how the app is actually running.

Tiered Compilation allows the .NET runtime to substitute different assembly code method implementations for the same method during the lifetime of an application to achieve higher performance. It currently does this in two ways:
- Startup - Whenever code needs to be jitted, the runtime first generates a low quality code body, then replaces it with a higher code quality version later if the method appears hot. The lower quality initial codegen saves JIT time and this savings typically dominates the additional cost to run the lower quality code for a short time.
- Steady-State - If code loaded from R2R images appears hot, the runtime replaces it with jitted code which is typically higher quality. At runtime the JIT is able to observe the exact dependencies that are loaded as well as CPU instruction support which allows it to generate superior code. In the future it may also utilize profile guided feedback but it does not currently do so.

## References

- [@noahfalk](https://github.com/noahfalk)'s [2.1 blog post](https://blogs.msdn.microsoft.com/dotnet/2018/08/02/tiered-compilation-preview-in-net-core-2-1/)
- [Demo of trying it out in 2.1](https://github.com/aspnet/JitBench/blob/tiered_compilation_demo/README.md)

# Using Tiered Compilation

Tiered compilation is enabled by default from .NET Core 2.2 preview 2 and in 3.0 daily builds.

- Download the .NET Core SDK from the [download archives](https://www.microsoft.com/net/download/archives)
  - For the latest pre-release installers, download a [daily build](https://github.com/dotnet/core/blob/master/daily-builds.md)
- For .NET Core 2.1 and 2.2 preview 1, tiered compilation may be enabled in any of the following ways ([instructions](https://github.com/aspnet/JitBench/blob/tiered_compilation_demo/README.md#16-run-the-app-with-tiered-compilation-enabled)):
  - In `<app>.csproj` before the build
  - In `<app>.runtimeconfig.json` after the build alongside the app assembly, before running the app
  - With an environment variable before running the app (in a console, or at user or system level)
- Ensure that the `<app>.csproj` is targeting the correct `TargetFramework` and `RuntimeFrameworkVersion`
  - `TargetFramework` should be `netcoreapp3.0` or `netcoreapp2.2`, etc.
  - `RuntimeFrameworkVersion` should be a .NET Core runtime version corresponding to the above
    - `dotnet --info` lists the available runtimes versions, use the version for Microsoft.NETCore.App
    - For .NET Core 2.2 preview 2, it is `2.2.0-preview2-26905-02`
- Run the app, and see [the demo](https://github.com/aspnet/JitBench/blob/tiered_compilation_demo/README.md#part-2---exploring-the-application-behavior) about how to see tiered compilation in action

# Known issues

- There is a known issue with benchmarks written in a simple way (single method with loop), which may run slower with tiered compilation. See the [cold method with hot loops issue](https://github.com/dotnet/coreclr/issues/19751) for information on how to identify the issue and some options for working around it.
- See known [tiered compilation issues](https://github.com/dotnet/coreclr/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+label%3Aarea-TieredCompilation)

# Providing feedback

- Mention [@noahfalk](https://github.com/noahfalk) and [@kouvel](https://github.com/kouvel) in your feedback
- Share your experiences by posting on the [2.2 issue](https://github.com/dotnet/coreclr/issues/18973)
- For issues:
  - Check existing issues in the CoreCLR repo (see known issues above), some may have guidance on how to identify and work around the issue
  - File a new issue in the [CoreCLR repo](https://github.com/dotnet/coreclr) with the `area-TieredCompilation` label
  - Tiered compilation may be disabled using the instructions in the "Using Tiered Compilation" section above, just flip the value from 1 to 0 or true to false as appropriate

# Other links

- [Design doc](https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/tiered-compilation.md)
- [Some design discussion](https://github.com/dotnet/coreclr/issues/4331)
