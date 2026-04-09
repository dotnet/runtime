# IL Rewriting Basics

## Intro
One of the common use cases of the `ICorProfiler*` interfaces is to perform IL rewriting. Some possible reasons a profiler would want to rewrite IL:
- Inspecting interesting process state
  - Capturing exception state
  - Inspecting managed objects
  - Inspecting function arguments/return values
- Injecting method hooks at the start/end of the method that call in to another managed library

There are two ways to rewrite IL

1. At Module load time with `ICorProfilerInfo::SetILFunctionBody`
    This approach has the benefit that it is 'set it and forget it'. You can replace the IL at module load, and the runtime will treat this new IL as if the module contained that IL - you don't have to worry about any of the quirks of ReJIT. The downside is that it is unrevertable - once it is set, you cannot change your mind.

2. At any point during the process lifetime with `ICorProfilerInfo4::RequestReJIT` or `ICorProfilerInfo10::RequestReJITWithInliners`.
   This approach means that you can modify functions in response to changing conditions, and you can revert the modified code if you decide you are done with it. See the other entries about ReJIT in this folder for more information.

## How to rewrite IL
Hopefully this section will be fleshed out in the future. Right now we have some documentation in the archives at [Creating an IL-rewriting profiler](<./davbr-blog-archive/Creating an IL-rewriting profiler.md>), but there is no start to finish tutorial on IL rewriting.

## What if multiple profilers want to rewrite IL in a given process?
The `ICorProfiler*` interfaces do not provide a way to multiplex different profilers, only one profiler can be loaded at a time. The [CLR Instrumentation Engine](https://github.com/microsoft/CLRInstrumentationEngine) project was created to address this limitation. If you are concerned about profiler multiplexing, head over and check out the project. A short summary is that it provides a higher level interface than the `ICorProfiler*` interfaces, and also provides way for multiple profilers to interact in a well defined manner.
