# Benchmarking .NET Core 2.0 / 2.1 applications

We recommend using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) as it allows specifying custom SDK paths and measuring performance not just in-proc but also out-of-proc as a dedicated executable.

```
<ItemGroup>
   <PackageReference Include="BenchmarkDotNet" Version="0.10.13" />
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

# Benchmarking local CoreFX builds

Since `0.10.13` BenchmarkDotNet knows [how to](./dogfooding.md#more-advanced-scenario---using-your-local-corefx-build) build a self-contained app against local CoreFX build. You just need to provide it the version you would like to benchmark and path to the folder with NuGet packages.

**Important:** BenchmarkDotNet will generate the right `.csproj` file for the self-contained app. It's going to reference the `.csproj` file of the project which defines benchmarks. It's going to work even if your project is not self-contained app targeting local CoreFX build. So you can just create a new solution with console app in Visual Studio, install BenchmarkDotNet and it's going to do the right thing for you. 

**Hint:** If you are curious to know what BDN does internally you just need to apply `[KeepBenchmarkFiles]` attribute to your class or set `KeepBenchmarkFiles = true` in your config file. After runing the benchmarks you can find the auto-generated files in `%pathToBenchmarkApp\bin\Release\$TFM\` folder.

```cs
class Program
{
    static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance.With(
                Job.ShortRun.With(
                    CustomCoreClrToolchain.CreateForLocalCoreFxBuild(
                        @"C:\Projects\forks\corefx\bin\packages\Release",
                        "4.5.0-preview2-26313-0"))));
}
```

**Warning:** BDN is going to restore the NuGet packages and install them in your `.nuget` folder. Please keep in mind that [you either have to remove them](./dogfooding.md#3---consuming-subsequent-code-changes-by-overwriting-the-binary-alternative-1) or [increase the version number](./dogfooding.md#3---consuming-subsequent-code-changes-by-overwriting-the-binary-alternative-2) after making some code changes and rebuilding the repo. **Otherwise, you are going to benchmark the same code over and over again**.

As an alternative to rebuilding entire CoreFX to regenerate the NuGet packages, you can provide the list of files that need to be copied to the published self-contained app. The files should be the dlls which you are trying to optimize. You can even define two jobs, one for the state before your local changes and one with the changes:

```cs
static void Main(string[] args)
    => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
        .Run(args, DefaultConfig.Instance
            .With(Job.ShortRun
                .With(CustomCoreClrToolchain.CreateForLocalCoreFxBuild(
                    pathToNuGetFolder: @"C:\Projects\forks\corefx\bin\packages\Release",
                    privateCoreFxNetCoreAppVersion: "4.5.0-preview2-26313-0",
                    displayName: "before"))
                .AsBaseline()
                .WithId("before"))
            .With(Job.ShortRun
                .With(CustomCoreClrToolchain.CreateForLocalCoreFxBuild(
                    pathToNuGetFolder: @"C:\Projects\forks\corefx\bin\packages\Release",
                    privateCoreFxNetCoreAppVersion: "4.5.0-preview2-26313-0",
                    displayName: "after",
                    filesToCopy: new [] {
					    @"c:\Projects\forks\corefx\bin\AnyOS.AnyCPU.Release\System.Text.RegularExpressions\netcoreapp\System.Text.RegularExpressions.dll"
				    }))
			    .WithId("after"))
            .KeepBenchmarkFiles());
```

Once you run the benchmarks with such a config it should be clear if you have improved the performance or not (like in the example below):

| Method |    Job | Toolchain | IsBaseline |      Mean |    Error |    StdDev | Scaled | ScaledSD |
|------- |------- |---------- |----------- |----------:|---------:|----------:|-------:|---------:|
| Sample |  after |     after |    Default | 35.077 us | 3.363 us | 0.1900 us |   8.64 |     0.15 |
| Sample | before |    before |       True |  4.060 us | 1.465 us | 0.0828 us |   1.00 |     0.00 |

# Benchmarking nightly CoreFX builds

Since `0.10.13` BenchmarkDotNet knows [how to](./dogfooding.md#advanced-scenario---using-a-nightly-build-of-microsoftnetcoreap) build a self-contained app against nightly CoreFX build. You just need to provide it the version you would like to benchmark. You don't need to provide url to MyGet feed, the default value is "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json".

```cs
static void Main(string[] args)
    => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
        .Run(args, DefaultConfig.Instance
            .With(Job.ShortRun
                .With(CustomCoreClrToolchain.CreateForNightlyCoreFxBuild("4.5.0-preview2-26215-01"))));
```

**Hint:** If you would like to compare the performance of different CoreFX versions, you just need to define multiple jobs, each using it's own toolchain.

```cs
DefaultConfig.Instance
    .With(Job.Default.With(CustomCoreClrToolchain.CreateForNightlyCoreFxBuild("4.5.0-preview2-26214-01", displayName: "before my change")));
    .With(Job.Default.With(CustomCoreClrToolchain.CreateForNightlyCoreFxBuild("4.5.0-preview2-26215-01", displayName: "after my change")));
```

# Benchmarking ANY CoreCLR and CoreFX builds

BenchmarkDotNet allows you to benchmark **ANY** CoreCLR and CoreFX builds. It just generates the right `.csproj` file with appropriate dependencies and `NuGet.config` file with the right feeds.

Example:

```
public class LocalCoreClrConfig : ManualConfig
{
	public LocalCoreClrConfig()
	{
		Add(Job.ShortRun.With(
			new CustomCoreClrToolchain(
				"local builds",
				coreClrNuGetFeed: @"C:\Projects\forks\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg",
				coreClrVersion: "2.1.0-preview2-26313-0",
				coreFxNuGetFeed: @"C:\Projects\forks\corefx\bin\packages\Release",
				coreFxVersion: "4.5.0-preview2-26313-0")
		));

		Add(Job.ShortRun.With(
			new CustomCoreClrToolchain(
				"local coreclr myget corefx",
				coreClrNuGetFeed: @"C:\Projects\forks\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg",
				coreClrVersion: "2.1.0-preview2-26313-0",
				coreFxNuGetFeed: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
				coreFxVersion: "4.5.0-preview2-26215-01")
		));

		Add(Job.ShortRun.With(
			new CustomCoreClrToolchain(
				"myget coreclr local corefx",
				coreClrNuGetFeed: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
				coreClrVersion: "2.1.0-preview2-26214-07",
				coreFxNuGetFeed: @"C:\Projects\forks\corefx\bin\packages\Release",
				coreFxVersion: "4.5.0-preview2-26313-0")
		));

		Add(Job.ShortRun.With(
			new CustomCoreClrToolchain(
				"myget builds",
				coreClrNuGetFeed: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
				coreClrVersion: "2.1.0-preview2-26214-07",
				coreFxNuGetFeed: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
				coreFxVersion: "4.5.0-preview2-26215-01")
		));

		// the rest of the config..
	}
}
```

The output is going to contain exact CoreCLR and CoreFX versions used:

```
BenchmarkDotNet=v0.10.12.20180215-develop, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.192)
Intel Core i7-3687U CPU 2.10GHz (Ivy Bridge), 1 CPU, 4 logical cores and 2 physical cores
Frequency=2533308 Hz, Resolution=394.7408 ns, Timer=TSC
.NET Core SDK=2.1.300-preview2-008162
  [Host]     : .NET Core 2.0.5 (CoreCLR 4.6.26020.03, CoreFX 4.6.26018.01), 64bit RyuJIT
  Job-DHYYZE : .NET Core ? (CoreCLR 4.6.26313.0, CoreFX 4.6.26313.0), 64bit RyuJIT
  Job-VGTPFY : .NET Core ? (CoreCLR 4.6.26313.0, CoreFX 4.6.26215.01), 64bit RyuJIT
  Job-IYZFNW : .NET Core ? (CoreCLR 4.6.26214.07, CoreFX 4.6.26215.01), 64bit RyuJIT
  Job-CTQFFQ : .NET Core ? (CoreCLR 4.6.26214.07, CoreFX 4.6.26313.0), 64bit RyuJIT
```

**Warning:** To fully understand the results you need to know what optimizations (PGO, CrossGen) were applied to given build. Usually, CoreCLR installed with the .NET Core SDK will be fully optimized and the fastest. On Windows, you can use the [disassembly diagnoser](http://adamsitnik.com/Disassembly-Diagnoser/) to check the produced assembly code.

# Benchmark multiple or custom .NET Core 2.x SDKs
Follow the instructions described [here](./dogfooding.md#advanced-scenario---using-a-nightly-build-of-microsoftnetcoreapp) and skip the last part which calls the `dotnet.exe` to run the application.

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
