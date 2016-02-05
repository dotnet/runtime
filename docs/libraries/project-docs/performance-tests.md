Performance Tests
======================

This document contains instructions for building, running, and adding Performance tests. 

Requirements
--------------------
### Windows
To run performance tests on Windows, .NET portable v5.0 is required. This library is included in [the Visual Studio Community 2015 download](https://www.visualstudio.com/products/visual-studio-community-vs). To get the correct packages during installation, follow these steps after opening the installer:
1. Select "Custom Installation" if no installation is present, or "Modify" otherwise
2. Check the "Universal Windows App Development Tools" box under the "Windows and Web Development" menu
3. Install
### Linux
Performance tests on Linux require all of the same steps as they do for regular xunit tests - see the linux instructions [here](https://github.com/dotnet/corefx/blob/master/Documentation/building/unix-instructions.md). Once you can have a directory on your Linux machine with a working corerun and xunit.console.netcore.exe (as well as the test dll containing your perf tests!), you only need to run the following command:

`dnu commands install Microsoft.DotNet.xunit.performance.runner.dnx 1.0.0-alpha-build0021 -f https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json`

Be careful that your mscorlib, libcoreclr, and test dlls were compiled using the "/p:Configuration=Release" property. Otherwise you may get skewed results.

Running the tests
-----------
### Windows
Performance test files (if present) are stored within a library's ```tests/Performance``` directory and contain test methods that are all marked with a perf-specific *Benchmark* attribute. The performance tests will only be run if the ```performance``` property is set to ```true```.

To build and run the tests using msbuild for a project, run ```msbuild /t:BuildAndTest /p:Performance=true /p:Configuration=Release``` from the tests directory. If the v5.0 assemblies aren't installed on your system, an error will be raised and no tests will be run.

Note: Because build.cmd runs tests concurrently, it's not recommended that you execute the perf tests using it.

results will be in: corefx/bin/tests/Windows_NT.AnyCPU.Release/TESTNAME/dnxcore50
### Linux
From your tests directory, run:
```
xunit.performance System.Collections.Tests.dll -trait Benchmark=true -verbose -runner ./xunit.console.netcore.exe -runnerhost ./corerun -runid System.Collections.Tests.dll-Linux -outdir results
```

This will run the perf tests for System.Collections.Tests.dll and output the results in results/System.Collections.Tests.dll-Linux.xml and results/System.Collections.Tests.dll-Linux.csv

Adding new Performance tests
-----------
Performance tests for CoreFX are built on top of xunit and [the Microsoft xunit-performance runner](https://github.com/Microsoft/xunit-performance/). 

For the time being, perf tests should reside within their own "Performance" folder within the tests directory of a library (e.g. [corefx/src/System.IO.FileSystem/tests/Performance](https://github.com/dotnet/corefx/tree/master/src/System.IO.FileSystem/tests/Performance) contains perf tests for FileSystem).

Start by adding the following lines to the tests csproj:
```
  <ItemGroup>
    <!-- Performance Tests -->
    <Compile Include="Performance\Perf.Dictionary.cs" />
    <Compile Include="Performance\Perf.List.cs" />
    <Compile Include="$(CommonTestPath)\System\PerfUtils.cs">
      <Link>Common\System\PerfUtils.cs</Link>
    </Compile>
  </ItemGroup>
  <!-- Optimizations to configure Xunit for performance -->
  <ItemGroup>
    <IncludePerformanceTests>true</IncludePerformanceTests>
  </ItemGroup>
```
(Replace Dictionary/List with whatever class you’re testing.)

Next, the project.json for the tests directory also needs to import the xunit libraries:

```
    "Microsoft.DotNet.xunit.performance": "1.0.0-*",
    "xunit": "2.1.0",  
    "xunit.netcore.extensions": "1.0.0-prerelease-*"  
```
Once that’s all done, you can actually add tests to the file. The basic structure of a perf test file with file name Perf.Dictionary.cs should be like so:
```
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Collections.Tests
{
    public class Perf_Dictionary
    {
        [Benchmark]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < 20000; i++)
                    {
                        new Dictionary<int, string>();
                    }
        }
    }
}
```
The perf-test runner handles a lot of the specifics of the testing for you like iteration control. You won't need to add any Asserts or anything generally used in functional testing to get perf measurements, just make sure the thing within the “using” is big enough to avoid timer resolution errors.

