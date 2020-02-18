Performance Requirements
========================

The .NET runtime supports a wide variety of high performance applications.  As such, performance is a key design element for every change.  This guidance is designed to share how we collect data and analyze the performance of the runtime.

You may also want to read about [performance coding guidelines](../coding-guidelines/performance-guidelines.md).

# Design Phase #
Make sure to address performance during the design phase of any change.  It is much easier to tweak a design to fit performance goals and requirements before implementation has started.

Here are some guidelines about how to think about performance during design:

- **DO** consider the performance of your change across a **wide variety of scenarios**.  While one scenario may benefit, others may not or may even regress.  Performance changes that penalize many scenarios for the benefit of one scenario are likely to be rejected unless the scenario is sufficiently important.
- **DO** ensure that any additional complexity, such as caches or tricky logic have a compelling reason for inclusion.
- **DO** ensure that performance fixes are **pay for play**.  This means that in general, whoever pays the cost of the fix also gets the benefit.  If scenarios or APIs pay for something that they never use or don't get benefit from, then this is essentially a performance regression.
- **DO** share your justification for any performance fixes in your pull request so that reviewers understand the trade-off that is being made.

# Cache Considerations #
A few guidelines to consider if you're planning to add a cache.  In addition to their upsides, they also come with downsides:

- Caches are generally additional complexity.  Thus there needs to be a **compelling** scenario when adding one.
- Caches need to be **pay for play**.  If there are scenarios that pay the cost but don't benefit, then the cache likely belongs at a different level of abstraction.
- Prior to adding a cache, analysis of size and lifetime needs to be completed.  Things to consider are whether the cache is unbounded in one or more scenarios, whether the lifetime of the cache is much longer than the times when it is useful and whether or not the cache needs any hints in order to be efficient.  If any of these considerations are true, likely the cache should be at a different level of abstraction.

# Prototyping #
If you need to convince yourself that the performance characteristics of a design are acceptable, consider writing a prototype.  The prototype should be just enough to be able to run a scenario that meets the scale requirements.  You can then capture a performance trace and analyze the results.

# Creating a Microbenchmark #
A microbenchmark is an application that executes a specific codepath multiple times with the intention of monitoring that codepath's performance.  The application usually runs many iterations of the code in question using a fine granularity timer, and then divides the total execution time by the number of iterations to determine the average execution time.  You may find times where you'd like to understand the performance of a small piece of code, and in some cases a microbenchmark is the right way to do this.

- **DO** use a microbenchmark when you have an isolated piece of code whose performance you want to analyze.
- **DO NOT** use a microbenchmark for code that has non-deterministic dependences (e.g. network calls, file I/O etc.)
- **DO** run all performance testing against retail optimized builds.
- **DO** run many iterations of the code in question to filter out noise.
- **DO** minimize the effects of other applications on the performance of the microbenchmark by closing as many unnecessary applications as possible.

# Profiling and Performance Tracing #
Measuring performance is an important part of ensuring that changes do not regress the performance of a feature or scenario.

Using a profiler allows you to run an existing workload without adding tracing statements or otherwise modifying it, and at the same time, get rich information on how the workload performs.

On the .NET team, we use a tool called **PerfView**, which runs on Windows, and allows for collection of performance data across an entire machine.

Capturing a trace using PerfView will allow you to:

- Investigate CPU usage and blocked time.
- Understand the performance of various runtime services (GC, JIT, etc.)
- Compare the performance of a workload by diffing before and after traces.
- Much, much more.

PerfView is available at the [PerfView repo](https://github.com/microsoft/perfview/releases).  The help documentation is quite substantial and can help you get started.  Clicking the blue links throughout PerfView's UI will also take you to the appropriate help topic.  It is also recommended that you watch the [PerfView Tutorial Videos](http://channel9.msdn.com/Series/PerfView-Tutorial).

# Running the CoreCLR Performance Tests on Windows #
1. The first step to running the performance tests locally is to do a release build of CoreCLR and all of the performance tests.  You can do this with the command `build.cmd x64 Release`, this will of course build the x64 runtime, and you should use x86 if you want to test x86.

2. After building the runtime you will need to generate a core root that contains all of the binaries we just built along with the required dependencies.  This can be done with the command `tests\runtest.cmd Release x64 GenerateLayoutOnly`, with the same caveat that x86 should be used if that is the platform that you are testing.

3. Now we need to actually run the performance tests. The performance tests live in the [dotnet/performance](https://github.com/dotnet/performance) repo. Instructions for running them are [here](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-dotnet-runtime.md).

4. Navigate to the `sandbox` directory in the root of your repo.  Inside that directory you will find a bunch of files that follow the name Perf-*.md.  These will contain the results, formatted as Markdown files, for each test that was run.

# Additional Help #
If you have questions, run into any issues, or would like help with any performance related topics, please feel free to post a question.  Someone from the .NET performance team will be happy to help.
