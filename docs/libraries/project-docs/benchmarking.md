# Benchmarking .NET Core 2.0 / 2.1 applications

We recommend using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) as it allows specifying custom SDK paths and measuring performance not just in-proc but also out-of-proc as a dedicated executable.

```
<ItemGroup>
   <PackageReference Include="BenchmarkDotNet" Version="0.10.11" />
</ItemGroup>
```

## Defining your benchmark

See [BenchmarkDotNet](http://benchmarkdotnet.org/Guides/GettingStarted.htm) documentation -- minimally you need to adorn a public method with the `[Benchmark]` attribute but there are many other ways to customize what is done such as using parameter sets or setup/cleanup methods. Of course, you'll want to bracket just the relevant code in your benchmark, ensure there are sufficient iterations that you minimise noise, as well as leaving the machine otherwise idle while you measure.

# Benchmarking .NET Core 2.0 applications
For benchmarking .NET Core 2.0 applications you only need the .NET Core 2.0 SDK installed: https://www.microsoft.com/net/download/windows. Make sure that your `TargetFramework` property in your csproj is set to `netcoreapp2.0` and follow the official BenchmarkDotNet instructions: http://benchmarkdotnet.org.

# Benchmarking .NET Core 2.1 applications
Make sure to download the .NET Core 2.1 SDK zip archive (https://github.com/dotnet/core-setup#daily-builds) and extract it somewhere locally, e.g.: `C:\dotnet-nightly\`.

For the sake of this tutorial we won't modify the `PATH` variable and instead always explicitly call the `dotnet.exe` from the downloaded SDK folder.

The shared framework is a set of assemblies that are packed into a `netcoreapp` Nuget package which is used when you set your `TargetFramework` to `netcoreappX.X`. You can either decide to use your local self-compiled shared framework package or use the one which is bundled with the .NET Core 2.1 SDK.

## Alternative 1 - Using the shared framework from the .NET Core 2.1 SDK
Follow the instructions described here https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#advanced-scenario---using-a-nightly-build-of-microsoftnetcoreapp and skip the last part which calls the `dotnet.exe` to run the application.

Add a benchmark class, configure it either with a manual configuration or by attributing it and pass the class type to the BenchmarkRunner:

```csharp
[MemoryDiagnoser]
// ...
public class Benchmark
{
     // Benchmark code ...
}

public class Program
{
    public static void Main()
    {
         BenchmarkRunner.Run<Benchmark>();
    }
}
```

## Alternative 2 - Using your self-compiled shared framework
Follow the instructions described here https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#more-advanced-scenario---using-your-local-corefx-build and skip the last part which calls the `dotnet.exe` to run the application.
Make sure to build your local corefx repository in RELEASE mode `.\build -release`! You currently need to have a self-contained application to inject your local shared framework package.

Currently there is no straightforward way to run your BenchmarkDotNet application in a dedicated process, therefore we are using the InProcess switch `[InProcess]`:

```csharp
[InProcess]
public class Benchmark
{
     // Benchmark code ...
}

public class Program
{
    public static void Main()
    {
         BenchmarkRunner.Run<Benchmark>();
    }
}
```

# Benchmark multiple or custom .NET Core 2.x SDKs
Follow the instructions described here https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#advanced-scenario---using-a-nightly-build-of-microsoftnetcoreapp and skip the last part which calls the `dotnet.exe` to run the application.

Whenever you want to benchmark an application simultaneously with one or multiple different .NET Core run time framework versions, you want to create a manual BenchmarkDotNet configuration file. Add the desired amount of Jobs and `NetCoreAppSettings` to specify the `targetFrameworkMoniker`, `runtimeFrameworkVersion` and `customDotNetCliPath`:

```csharp
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

public class MainConfig : ManualConfig
{
    public MainConfig()
    {
        // Job #1
        Add(Job.Default
            .With(Runtime.Core)
            .With(CsProjCoreToolchain.From(new NetCoreAppSettings(
                targetFrameworkMoniker: "netcoreapp2.1",
                runtimeFrameworkVersion: "2.1.0-preview1-25919-02", // <-- Adjust version here
                customDotNetCliPath: @"C:\dotnet-nightly\dotnet.exe", // <-- Adjust path here
                name: "Core 2.1.0-preview"))));
            
        // Job #2 which could be in-process (see Alternative #2)
        // ...
        
        // Job #3 which could be .NET Core 2.0
        // ...

        // Add whatever jobs you need
        Add(DefaultColumnProviders.Instance);
        Add(MarkdownExporter.GitHub);
        Add(new ConsoleLogger());
        Add(new HtmlExporter());
        Add(MemoryDiagnoser.Default);
    }
}
```

In your application entry point pass the configuration to the BenchmarkRunner:
```csharp
public class Benchmark
{
     // Benchmark code ...
}

public class Program
{
    public static void Main()
    {
         BenchmarkRunner.Run<Benchmark>(new MainConfig());
    }
}
```

# Running the benchmark

To get valid results make sure to run your project in RELEASE configuration:

```
cd "path/to/your/benchmark/project"
"C:\dotnet-nightly\dotnet.exe" run -c Release
```

# Reporting results

Often in a Github Pull Request or issue you will want to share performance results to justify a change. If you add the `MarkdownExporter` job in the configuration (as you can see in Alternative 3), BenchmarkDotNet will have created a Markdown (*.md) file in the `BenchmarkDotNet.Artifacts` folder which you can paste in, along with the code you benchmarked.

# References
- [BenchmarkDotNet](http://benchmarkdotnet.org/)
- [BenchmarkDotNet Github](https://github.com/dotnet/BenchmarkDotNet)
- [.NET Core SDK](https://github.com/dotnet/core-setup)
