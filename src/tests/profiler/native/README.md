# Profiler.dll

This directory builds Profilers\Profiler.dll, which contains various implementations of ICorProfilerCallback used in our tests. It is used by ProfilerTestRunner.cs in ../common.

### Goals

1) Easy to run/debug a profiler test manually simply by executing the managed test binary + setting minimal env vars:

    CORECLR_ENABLE_PROFILING=1
    CORECLR_PROFILER={CLSID_of_profiler}
    CORECLR_PROFILER_PATH=path_to_profiler_dll

We should be very careful about adding any additional dependencies such as env vars or assumptions that certain files will reside in certain places. Any such dependencies need to be clearly documented.

2) Easy to understand what the test is doing given only an understanding of the ICorProfiler interfaces and basic C++.
This means we make limited use of helper functions, macros, and new interfaces that wrap or abstract the underlying APIs. If we do add another layer, it should represent a non-trivial unit of complexity (eg IL-rewriting) and using it should be optional for only a subset of tests that need it. Tests should also avoid trying to test too much at the same time. Making a new test for new functionality is a relatively quick operation.


### Implementation of this profiler dll:

There is a small set of shared implementation for all profiler implementations:

1. profiler.def - the dll exported entrypoints
2. dllmain.cpp - implementation of the exported entrypoints
3. classfactory.h/.cpp - implementation of standard COM IClassFactory, used to instantiate a new profiler
4. profiler.h/.cpp - a base class for all profiler implementations. It provides IUnknown, do-nothing implementations of all ICorProfilerCallbackXXX interfaces, and the pCorProfilerInfo field that allows calling back into the runtime

All the rest of the implementation is in test-specific profiler implementations that derive from the Profiler class. Each of these is in a sub-directory. See gcbasicprofiler/gcbasicprofiler.h/.cpp for a simple example.

### Adding a new profiler

When you want to test new profiler APIs you will need a new test profiler implementation. I recommend using the GC Basic Events test in gcbasicprofiler as an example. The steps are:

1) Get your new profiler building:

 - Copy and rename gcbasicprofiler folder.
 - Rename the source files and the gcbasicprofiler type within the source.
 - Add the new source files to CMakeLists.txt

2) Make your new profiler creatable via COM:

 - Create a new GUID and replace the one in YourProfiler::GetClsid()
 - Update classfactory.cpp to include your new profiler's header and update the list of profiler instances in ClassFactory::CreateInstance

        Profiler* profilers[] = {
    		new GCBasicProfiler(),
    		// add new profilers here
    	};

3) Override the profiler callback functions that are relevant for your test and delete the rest. At minimum you will need to ensure that the test prints the phrase "PROFILER TEST PASSES" at some point to indicate this is a passing test. Typically that occurs in the Shutdown() method. It is also likely you want to override Initialize() in order to call SetEventMask so that the profiler receives events.
