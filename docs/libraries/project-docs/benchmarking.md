# Benchmarking .NET Core applications

We recommend using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) as it allows specifying custom SDK paths and measuring performance not just in-proc but also out-of-proc as a dedicated executable.

```
<ItemGroup>
   <PackageReference Include="BenchmarkDotNet" Version="0.11.3" />
</ItemGroup>
```

## Defining your benchmark

See [BenchmarkDotNet](https://benchmarkdotnet.org/articles/guides/getting-started.html) documentation -- minimally you need to adorn a public method with the `[Benchmark]` attribute but there are many other ways to customize what is done such as using parameter sets or setup/cleanup methods. Of course, you'll want to bracket just the relevant code in your benchmark, ensure there are sufficient iterations that you minimise noise, as well as leaving the machine otherwise idle while you measure.

# Benchmarking local CoreFX builds

Since `0.11.1` BenchmarkDotNet knows how to run benchmarks with CoreRun. So you just need to provide it the path to CoreRun! The simplest way to do that is via console line arguments:

    dotnet run -c Release -f netcoreapp3.0 -- -f *MyBenchmarkName* --coreRun "C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe"

**Hint:** If you are curious to know what BDN does internally you just need to apply `[KeepBenchmarkFiles]` attribute to your class or set `KeepBenchmarkFiles = true` in your config file. After running the benchmarks you can find the auto-generated files in `%pathToBenchmarkApp\bin\Release\$TFM\` folder.

The alternative is to use `CoreRunToolchain` from code level:

```cs
class Program
{
    static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance.With(
                Job.ShortRun.With(
                    new CoreRunToolchain(
                        new FileInfo(@"C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe")
                        ))));
}
```


**Warning:** To fully understand the results you need to know what optimizations (PGO, CrossGen) were applied to given build. Usually, CoreCLR installed with the .NET Core SDK will be fully optimized and the fastest. On Windows, you can use the [disassembly diagnoser](http://adamsitnik.com/Disassembly-Diagnoser/) to check the produced assembly code.

## New API

If you are testing some new APIs you need to tell BenchmarkDotNet where is `dotnet cli` that is capable of building the code. You can do that by using the `--cli` command line argument.

# Running in process

If you want to run your benchmarks without spawning a new process per benchmark you can do that by passing `-i` console line argument. Please be advised that using [InProcessToolchain](https://benchmarkdotnet.org/articles/configs/toolchains.html#sample-introinprocess) is not recommended when one of your benchmarks might have side effects which affect other benchmarks. A good example is heavy allocating benchmark which affects the size of GC generations.

    dotnet run -c Release -f netcoreapp2.1 -- -f *MyBenchmarkName* -i

# Recommended workflow

1. Before you start benchmarking the code you need to build entire CoreFX in Release which is going to generate the right CoreRun bits for you:

        C:\Projects\corefx>build.cmd -release /p:ArchGroup=x64

After that, you should be able to find `CoreRun.exe` in a location similar to:

        C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe

2. Create a new .NET Core console app using your favorite IDE
3. Install BenchmarkDotNet (0.11.1+)
4. Define the benchmarks and pass the arguments to BenchmarkSwitcher

```cs
class Program
{
   static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
```
5. Run the benchmarks using `--coreRun` from the first step. Save the results in a dedicated folder.

        dotnet run -c Release -f netcoreapp3.0 -- -f * --coreRun "C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe" --artifacts ".\before"

6. Go to the corresponding CoreFX source folder (for example `corefx\src\System.Collections.Immutable`)
7. Apply the optimization that you want to test
8. Rebuild given CoreFX part in Release:

        dotnet msbuild /p:ConfigurationGroup=Release

You should notice that given `.dll` file have been updated in the `CoreRun` folder.

9. Run the benchmarks using `--coreRun` from the first step. Save the results in a dedicated folder.

        dotnet run -c Release -f netcoreapp3.0 -- -f * --coreRun "C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe" --artifacts ".\after"

10. Compare the results and repeat steps `7 - 9` until you are happy about the results.

## Benchmarking APIs implemented within System.Private.Corelib

1. The steps for this scenario are very similar to the above recommended workflow with a couple of extra steps to copy bits from one repo to the other. Before you start benchmarking the code you need to build entire CoreCLR in Release which is going to generate the `System.Private.Corelib.dll` for you:

        C:\Projects\coreclr>build.cmd -release -skiptests

After that, you should be able to find `System.Private.Corelib.dll` in a location similar to:

        C:\Projects\coreclr\bin\Product\Windows_NT.x64.Release

2. Build entire CoreFX in Release using your local private build of coreclr (See [Testing With Private CoreCLR Bits](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits))

        C:\Projects\corefx>build.cmd -release /p:CoreCLROverridePath=C:\Projects\coreclr\bin\Product\Windows_NT.x64.Release

After that, you should be able to find `CoreRun.exe` in a location similar to:

        C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe

3. Create a new .NET Core console app using your favorite IDE
4. Install BenchmarkDotNet (0.11.1+)
5. Define the benchmarks and pass the arguments to BenchmarkSwitcher

```cs
class Program
{
   static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
```
6. Run the benchmarks using `--coreRun` from the second step. Save the results in a dedicated folder.

        dotnet run -c Release -f netcoreapp3.0 -- -f * --coreRun "C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe" --artifacts ".\before"

7. Go to the corresponding CoreCLR source folder where the API you want to change exists (for example `coreclr\src\System.Private.CoreLib\shared\System`)
8. Apply the optimization that you want to test
9. Rebuild System.Private.Corelib with your change (optionally adding `-skipnative` if the change is isolated to managed code):

        C:\Projects\coreclr>build.cmd -release -skiptests -skipnative

10. For the next step, you have one of two options:

  - Rebuild given CoreFX part in Release:

          C:\Projects\corefx>build.cmd -release /p:CoreCLROverridePath=C:\Projects\coreclr\bin\Product\Windows_NT.x64.Release

  - OR manually copy over the relevant files from within the root of the coreclr output folder to where `CoreRun.exe` lives within corefx (excluding the subdirectories). This ends up being much faster than the first option and if the only thing that changed is   `System.Private.Corelib.dll`, just copy that over:

          Copy from: C:\Projects\coreclr\bin\Product\Windows_NT.x64.Release
          To: C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\

11. Run the benchmarks using `--coreRun` from the first step. Save the results in a dedicated folder.

        dotnet run -c Release -f netcoreapp3.0 -- -f * --coreRun "C:\Projects\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe" --artifacts ".\after"

12. Compare the results and repeat steps `8 - 11` until you are happy about the results.

# Reporting results

Often in a Github Pull Request or issue you will want to share performance results to justify a change. If you add the `MarkdownExporter` job in the configuration (as you can see in Alternative 3), BenchmarkDotNet will have created a Markdown (*.md) file in the `BenchmarkDotNet.Artifacts` folder which you can paste in, along with the code you benchmarked.

# References
- [BenchmarkDotNet](http://benchmarkdotnet.org/)
- [BenchmarkDotNet Github](https://github.com/dotnet/BenchmarkDotNet)
- [.NET Core SDK](https://github.com/dotnet/core-setup)
