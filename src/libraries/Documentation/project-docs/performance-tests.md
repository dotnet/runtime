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

Before running the performance tests you must run ```build -release``` from the root folder.

To build and run the tests using msbuild for a project, run ```msbuild /t:BuildAndTest /p:Performance=true /p:ConfigurationGroup=Release /p:TargetOS=Windows_NT``` from the Performance directory with Admin privileges. If the v5.0 assemblies aren't installed on your system, an error will be raised and no tests will be run.

Note: Because build.cmd runs tests concurrently, it's not recommended that you execute the perf tests using it.

results will be in: corefx/bin/tests/Windows_NT.AnyCPU.Release/TESTNAME/netcoreapp1.0
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
Once that’s all done, you can actually add tests to the file.

Writing Test Cases
-----------
```C#
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Collections.Tests
{
    public class Perf_Dictionary
    {
        private volatile Dictionary<int, string> dict;

        [Benchmark(InnerIterationCount = 2000)]
        public void ctor()
        {
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        dict = new Dictionary<int, string>();
                    }
        }
    }
}
```

The above benchmark will test the performance of the Dictionary<int, string> constructor. Each iteration of the benchmark will call the constructor 2000 times (`InnerIterationCount`).

Test cases should adhere to the following guidelines, within reason:

* Individual test cases should be of the "microbenchmark" variety. They should test a single function, in as isolated an environment as possible.
* The "real work" must be done inside of the `using (iteration.StartMeasurement())` block. All extra work (setup, cleanup) should be done outside of this block, so as to not pollute the data being collected.
* Individual iterations of a test case should take from 100 milliseconds to 1 second. This is everything inside of the `using (iteration.StartMeasurement())` block.
* Test cases may need to use an "inner iteration" concept in order for individual invocations of the "outer iteration" to last from 100 ms to 1s. The example above shows this.
* Some functions are prone to being entirely optimized out from test cases. For example, if the results of `Vector3.Add()` are not stored anywhere, then there are no observable side-effects, and the entire operation can be optimized out by the JIT. For operations which are susceptible to this, care must be taken to ensure that the operations are not entirely skipped. Try the following:
  * Pass intermediate values to a volatile static field. This is done in the example code above.
  * If the value is a struct, compute a value dependent on the structure, and store that in a volatile static field.
* There are two main ways to detect when a test case is being "optimized out":
  * Look at the disassembly of the function (with the Visual Studio disassembler, for example).
  * Observe unusual changes in the duration metric. If your test suddenly takes 1% of its previous time, odds are something has gone wrong.
* Before using intrinsic data types (int, string, etc) to represent value and reference types in your test, consider if the code under test is optimized for those types versus normal classes and structs.
  * Also consider interfaces. For example, methods on ```List<T>``` using equality will be much faster on Ts that implement  ```IEquatable<T>```.

Avoid the following performance test test anti-patterns:
* Tests for multiple methods which all end up calling the same final overload. This just adds noise and extra duplicate data to sift through.
* Having too many test cases which only differ by "input data". For example, testing the same operation on a collection with size 1, 10, 100, 1000, 10000, etc. This is a common pitfall when using `[Theory]` and `[InlineData]`. Instead, focus on the key scenarios and minimize the numbers of test cases. This results in less noise, less data to sift through, and less test maintenance cost.
* Performing more than a single operation in the "core test loop". There are times when this is necessary, but they are few and far between. Take extra care if you notice that your test case is doing too many things, and try to focus on creating a small, isolated microbenchmark.
