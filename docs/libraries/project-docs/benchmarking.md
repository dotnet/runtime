# Benchmarking .NET Core 2.0 / 2.1 applications

We recommend using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) as with the latest version it allows specifying custom SDK paths and measuring performance not just in-proc but also out-of-proc as a dedicated executable.

```
<ItemGroup>
   <PackageReference Include="BenchmarkDotNet" Version="0.10.10" />
</ItemGroup>
```

## Defining your benchmark

See [BenchmarkDotNet](http://benchmarkdotnet.org/Guides/GettingStarted.htm) documentation -- minimally you need to adorn a public method with the `[Benchmark]` attribute but there are many other ways to customize what is done such as using parameter sets or setup/cleanup methods. Of course, you'll want to bracket just the relevant code in your benchmark, ensure there are sufficient iterations that you minimise noise, as well as leaving the machine otherwise idle while you measure.

# Benchmarking .NET Core 2.0 applications
For benchmarking .NET Core 2.0 applications you only need the .NET Core 2.0 SDK installed: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/release/2.0.0/dotnet-runtime-latest-win-x64.exe. Make sure that your `TargetFramework` property in your csproj is set to `netcoreapp2.0` and follow the official BenchmarkDotNet instructions: https://github.com/dotnet/BenchmarkDotNet/blob/master/docs/guide/Configs/Toolchains.md

# Benchmarking .NET Core 2.1 applications
Make sure to download the .NET Core 2.1 SDK zip archive (https://github.com/dotnet/core-setup#daily-builds) and extract it somewhere locally, e.g.: `C:\Program Files\dotnet-nightly\`.

In this tutorial we won't modify the `PATH` variable and instead always explicitly call the `dotnet.exe` from the downloaded SDK folder.

Check which version of the shared framework is bundled with the downloaded SDK and remember it for later. At the time writing this documentation it was `2.1.0-preview1-25919-02`.
> C:\Program Files\dotnet-nightly\shared\Microsoft.NETCore.App\2.1.0-preview1-25919-02

## Shared framework
The shared framework is a set of assemblies that are packed into a `netcoreapp` Nuget package which is used when you set your `TargetFramework` to `netcoreappX.X`. You can either decide to use your local self-compiled shared framework package or use the one from the .NET Core 2.1 SDK.

### Alternative 1 - Using the shared framework from the .NET Core 2.1 SDK
Follow the instructions described here https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#advanced-scenario---using-a-nightly-build-of-microsoftnetcoreapp and skip the last part which calls the `dotnet.exe`.

Now we are going to create a BenchmarkDotNet configuration file (let's call it `MainConfig.cs`) and add it to the project. The manual configuration allows us to set the path to our SDK via the `customDotNetCliPath` property: 

```
public class MainConfig : ManualConfig
{
    public MainConfig()
    {
        Add(Job.Default
            .With(Runtime.Core)
            .With(CsProjCoreToolchain.From(new NetCoreAppSettings(
                targetFrameworkMoniker: "netcoreapp2.1",
                runtimeFrameworkVersion: "2.1.0-preview1-25919-02", // <-- Adjust version here
                customDotNetCliPath: @"C:\Program Files\dotnet-nightly\dotnet.exe", // <-- Adjust path here
                name: "Core 2.1.0-preview")))
            .WithLaunchCount(1)
            .WithWarmupCount(1)
            .WithInvocationCount(10)
            .WithUnrollFactor(1)
            .WithTargetCount(3));

        // Add whatever jobs you need
        Add(DefaultColumnProviders.Instance);
        Add(MarkdownExporter.GitHub);
        Add(new ConsoleLogger());
        Add(new HtmlExporter());
        Add(MemoryDiagnoser.Default);
    }
}
```

### Alternative 2 - Using your self-compiled shared framework
Follow the instructions described here https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#more-advanced-scenario---using-your-local-corefx-build and skip the last part which calls the `dotnet.exe`.
Make sure to build your local corefx repository in RELEASE mode `.\build -release`! You currently need to have a self-contained application to inject your local shared framework package.

Now we are going to create a BenchmarkDotNet configuration file (let's call it `MainConfig.cs`) and add it to the project. Currently there is no easy way to run your BenchmarkDotNet application in a dedicated process, therefore we are using the InProcess switch `InProcessTollchain.Instance`:

```
public class MainConfig : ManualConfig
{
    public MainConfig()
    {
        Add(Job.Default
            .With(InProcessToolchain.Instance) <-- To run against your local built shared framework (in corefx folder)
            .WithLaunchCount(1)
            .WithWarmupCount(1)
            .WithInvocationCount(1)
            .WithUnrollFactor(1)
            .WithTargetCount(3));

        // Add whatever jobs you need
        Add(DefaultColumnProviders.Instance);
        Add(MarkdownExporter.GitHub);
        Add(new ConsoleLogger());
        Add(new HtmlExporter());
        Add(MemoryDiagnoser.Default);
    }
}
```

## Running the benchmark

In your application entry point pass the configuration to the BenchmarkRunner:
`BenchmarkRunner.Run<T>(new MainConfig()); // <-- Configuration class`

As mentioned before, instead of calling the dotnet in the `PATH` we call the `dotnet.exe` explicitely from the downloaded SDK folder.
To get valid results make sure to compile and run your project with RELEASE configuration:

```
> cd "path/to/your/benchmark/project"
> "C:\Program Files\dotnet-nightly\dotnet.exe" restore
> "C:\Program Files\dotnet-nightly\dotnet.exe" build -c Release
> "C:\Program Files\dotnet-nightly\dotnet.exe" run -c Release
```

# Reporting results

Often in a Github Pull Request or issue you will want to share performance results to justify a change. If you add the `MarkdownExporter` job in the configuration (as you can see in the example), BenchmarkDotNet will have created a Markdown (*.md) file in the `BenchmarkDotNet.Artifacts` folder which you can paste in, along with the code you benchmarked.

# References
- [BenchmarkDotNet](http://benchmarkdotnet.org/)
- [BenchmarkDotNet Github](https://github.com/dotnet/BenchmarkDotNet)
- [.NET Core SDK](https://github.com/dotnet/core-setup)
