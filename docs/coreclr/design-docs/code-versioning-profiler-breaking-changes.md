# Code Versioning Profiler Breaking Changes #

The runtime changes done as part of the code versioning feature will cause some (hopefully minor) breaking changes to be visible via the profiler API. My goal is to advertise these coming changes and solicit feedback about what will be easiest for profiler writers to absorb. Currently this feature is only under development in the .NET Core version of the runtime. If you solely support a profiler on full .NET Framework this change doesn't affect you.

## Underlying issue ##

Code versioning, and in particular its use for tiered compilation means that the runtime will be invoking the JIT more than it did in the past. Historically there was a 1:1 relationship between a FunctionID and a single JITCompilationFinished event. For those profilers using ReJIT APIs the 1:1 relationship was between a (FunctionID,ReJITID) pair and a single [Re]JITCompilationFinished event. [Re]JitCompilationStarted events were usually 1:1 as well, but not guaranteed in all cases. Tiered compilation will break these invariants by invoking the JIT potentially multiple times per method.

## Likely ways you will see behavior change ##

1. There will be more JITCompilation events, and potentially more ReJIT compilation events than there were before.

2. These JIT events may originate from a background worker thread that may be different from the thread which ultimately runs the jitted code.

3. Calls to ICorProfilerInfo4::GetCodeInfo3 will only return information about the first jitted code body for a given FunctionID,rejitID pair. We'll need to create a new API to handle code bodies after the first.

4. Calls to ICorProfilerInfo4::GetILToNativeMapping2 will only return information about the first jitted code body for a given FunctionID,rejitID pair. We'll need to create a new API to handle code bodies after the first.

5. IL supplied during the JITCompilationStarted callback is now verified the same as if you had provided it during ModuleLoadFinished.


## Obscure ways you might see behavior change ##

1. If tiered compilation fails to publish an updated code body on a method that has already been instrumented with RequestReJIT and jitted, the profiler could receive a rejit error callback reporting the problem. This should only occur on OOM or process memory corruption.

2. The timing of ReJITCompilationFinished has been adjusted to be slightly earlier (after the new code body is generated, but prior to updating the previous jitted code to modify control flow). This raises a slim possibility for a ReJIT error to be reported after ReJITCompilationFinished in the case of OOM or process memory corruption.


There are likely some other variations of the changed behavior I haven't thought of yet, but if further testing, code review, or discussion brings it to the surface I'll add it here. Feel free to get in touch on github (@noahfalk), or if you have anything you want to discuss in private you can email me at noahfalk AT microsoft.com