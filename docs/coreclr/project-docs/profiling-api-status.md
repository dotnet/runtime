APIs Internally Microsoft Tested:
1. Windows

ICorProfilerCallback:
 Initialize
 ModuleLoadFinished
 ModuleUnloadStarted
 ModuleUnloadFinished
 ModuleAttachedToAssembly
 JITCompilationStarted

ICorProfilerInfo:
 GetModuleInfo
 GetModuleMetaData
 GetModuleInfo
 GetModuleInfo2
 GetModuleMetaData
 GetFunctionInfo
 SetILFunctionBody
 SetILInstrumentedCodeMap
 GetILFunctionBodyAllocator
 GetRuntimeInformation
 SetEventMask

The flags tested for SetEventMask are:
 COR_PRF_MONITOR_MODULE_LOADS 
 COR_PRF_MONITOR_JIT_COMPILATION
 COR_PRF_DISABLE_INLINING

2. Linux
3. OSX

APIs known to work; tested by other contributors (please mention your github handle in case someone wants to reach out for further clarification)
1. Windows
2. Linux
3. OSX

APIs definitely known not to work yet
1. Windows
2. Linux
3. OSX
