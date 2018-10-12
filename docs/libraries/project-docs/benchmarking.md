# Benchmarking .NET Core applications

We recommend using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) as it allows specifying custom SDK paths and measuring performance not just in-proc but also out-of-proc as a dedicated executable.

```
<ItemGroup>
   <PackageReference Include="BenchmarkDotNet" Version="0.11.1" />
</ItemGroup>
```

## Defining your benchmark

See [BenchmarkDotNet](https://benchmarkdotnet.org/articles/guides/getting-started.html) documentation -- minimally you need to adorn a public method with the `[Benchmark]` attribute but there are many other ways to customize what is done such as using parameter sets or setup/cleanup methods. Of course, you'll want to bracket just the relevant code in your benchmark, ensure there are sufficient iterations that you minimise noise, as well as leaving the machine otherwise idle while you measure.

# Benchmarking local CoreFX builds

Since `0.11.1` BenchmarkDotNet knows how to run benchmarks with CoreRun. So you just need to provide it the path to CoreRun! The simplest way to do that is via console line arguments:

    dotnet run -c Release -f netcoreapp2.1 -- -f *MyBenchmarkName* --coreRun "C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe"

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

        C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe

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

        dotnet run -c Release -f netcoreapp2.1 -- -f * --coreRun "C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe" --artifacts ".\before"

6. Go to the corresponding CoreFX source folder (an example `corefx\src\System.Collections.Immutable`)
7. Apply the optimization that you want to test
8. Rebuild given CoreFX part in Release:

        dotnet msbuild /p:ConfigurationGroup=Release

You should notice that given `.dll` file have been updated in the `CoreRun` folder.

9. Run the benchmarks using `--coreRun` from the first step. Save the results in a dedicated folder.

        dotnet run -c Release -f netcoreapp2.1 -- -f * --coreRun "C:\Projects\corefx\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\9.9.9\CoreRun.exe" --artifacts ".\after"

10. Compare the results and repeat steps `7 - 9` until you are happy about the results.

# Reporting results

Often in a Github Pull Request or issue you will want to share performance results to justify a change. If you add the `MarkdownExporter` job in the configuration (as you can see in Alternative 3), BenchmarkDotNet will have created a Markdown (*.md) file in the `BenchmarkDotNet.Artifacts` folder which you can paste in, along with the code you benchmarked.

# References
- [BenchmarkDotNet](http://benchmarkdotnet.org/)
- [BenchmarkDotNet Github](https://github.com/dotnet/BenchmarkDotNet)
- [.NET Core SDK](https://github.com/dotnet/core-setup)
