# Introduction #
The .NET runtime supports a wide variety of high performance applications.  As such, performance is a key design element for every change.  This guidance is designed to share how we collect data and analyze the performance of the runtime.

# Design Phase #
Make sure to address performance during the design phase of any change.  It is much easier to tweak a design to fit performance goals and requirements before implementation has started.

Think about how/how often the feature is used, and how much additional resources are required after the change.  

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

PerfView is available at the [Microsoft Download Center](http://www.microsoft.com/en-us/download/details.aspx?id=28567 "Microsoft Download Center").  The help documentation is quite substantial and can help you get started.  Clicking the blue links throughout PerfView's UI will also take you to the appropriate help topic.  It is also recommended that you watch the [PerfView Tutorial Videos](http://channel9.msdn.com/Series/PerfView-Tutorial).

# Additional Help #
If you have questions, run into any issues, or would like help with any performance related topics, please feel free to post a question.  Someone from the .NET performance team will be happy to help.