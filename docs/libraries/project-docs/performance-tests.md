Performance Tests
======================

This document contains instructions for building, running, and adding Performance tests.

Building and Running Tests
-----------
Performance test files (if present) are stored within a library's ```tests/Performance``` directory and contain test methods that are all marked with a perf-specific *Benchmark* attribute.

**Step # 1:** Prior to running performance tests, a full build from the repo root must be completed: ```build -release```

**Step # 2:** Change directory to the performance tests directory: ```cd path/to/library/tests/Performance```

**Step # 3:** Build and run the tests:
 - Windows ```dotnet msbuild /t:BuildAndTest /p:ConfigurationGroup=Release```
 - Linux: ```dotnet msbuild /t:BuildAndTest /p:ConfigurationGroup=Release```

**Note: Because test build runs tests concurrently, do not use it for executing performance tests. If you still want to run them concurrently you need to pass the flag `/p:Performance=true` to it: `build -test -release /p:Performance=true`.**

The results files will be dropped in corefx/artifacts/bin/tests/TESTLIBRARY/FLAVOR/.  The console output will also specify the location of these files.

**Getting memory usage**

To see memory usage as well as time, add the following property to the command lines above: `/p:CollectFlags=stopwatch+gcapi`.

Adding New Performance Tests
-----------
Performance tests for CoreFX are built on top of xunit and [the Microsoft xunit-performance framework](https://github.com/Microsoft/xunit-performance/).

Performance tests should reside within their own "Performance" folder within the tests directory of a library (e.g. [corefx/src/System.IO.FileSystem/tests/Performance](https://github.com/dotnet/corefx/tree/master/src/System.IO.FileSystem/tests/Performance) contains perf tests for FileSystem).

It's easiest to copy and modify an existing example like the one above. Notice that you'll need these lines in the tests csproj:
```
  <ItemGroup>
    <ProjectReference Include="$(CommonTestPath)\Performance\PerfRunner\PerfRunner.csproj">
      <Project>{69e46a6f-9966-45a5-8945-2559fe337827}</Project>
      <Name>PerfRunner</Name>
    </ProjectReference>
  </ItemGroup>
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
