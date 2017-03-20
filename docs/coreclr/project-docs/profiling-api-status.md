# Status of CoreCLR Profiler APIs

The notes below will help you determine what profiling APIs are safe to use. The .NET Core project started with the codebase from the desktop CoreCLR/Silverlight so all the profiler APIs present there are also present in the code here. However that doesn't automatically imply that they are all working or being actively tested right now. Our goal is to eventually have everything tested and working across all the supported OSes. As we make progress we'll document it here. If you want to use APIs that we haven't tested yet you are welcome to do so, but you need to do your own testing to determine whether they work. If you do test APIs we haven't gotten to yet, we hope you'll add a note below in the Community Tested API section so that everyone can benefit.

# Microsoft Tested APIs:

### Windows

* ICorProfilerCallback:
 * `Initialize`
 * `ModuleLoadFinished`
 * `ModuleUnloadStarted`
 * `ModuleUnloadFinished`
 * `ModuleAttachedToAssembly`
 * `JITCompilationStarted`

* ICorProfilerInfo:
 * `GetModuleInfo`
 * `GetModuleMetaData`*
 * `GetModuleInfo`
 * `GetModuleInfo2`
 * `GetFunctionInfo`
 * `SetILFunctionBody`*
 * `SetILInstrumentedCodeMap`*
 * `GetILFunctionBodyAllocator`*
 * `GetRuntimeInformation`
 * `SetEventMask`

* The flags tested for SetEventMask are:
 * `COR_PRF_MONITOR_MODULE_LOADS`
 * `COR_PRF_MONITOR_JIT_COMPILATION`
 * `COR_PRF_DISABLE_INLINING`

\* Instrumentation APIs have not been tested on assemblies compiled with Ready2Run technology. Ready2Run is currently used
   for all Framework assemblies. 
   
### Linux
### OS X

# Community Tested APIs (please include GitHub handle)

### Windows
  * IProfilerCallback
    * ModuleLoadStarted (noahfalk on behalf of one of our vendors)
    * JITCompilationStarted (noahfalk on behalf of one of our vendors)
  * IMetaDataEmit
    * various DefineXXX methods (noahfalk on behalf of one of our vendors)
  * IMetadataDataDispenserEx
    * OpenScopeOnMemory (noahfalk on behalf of one of our vendors)
  * IMetaDataTables (noahfalk on behalf of one of our vendors)
  * IMetaDataAssemblyEmit
    * DefineAssemblyRef (noahfalk on behalf of one of our vendors)
    
### Linux
### OS X

# APIs definitely known not to work yet
### Windows
### Linux

* ICorProfilerInfo:
 * `SetEnterLeaveFunctionHooks`

* ICorProfilerInfo2:
 * `SetEnterLeaveFunctionHooks2`

* SetEventMask Flags that currently result in undefined behavior :
 * `COR_PRF_USE_PROFILE_IMAGES`
 * `COR_PRF_REQUIRE_PROFILE_IMAGE`

### OS X
* ICorProfilerInfo:
 * `SetEnterLeaveFunctionHooks`

* ICorProfilerInfo2:
 * `SetEnterLeaveFunctionHooks2`

* SetEventMask Flags that currently result in undefined behavior :
 * `COR_PRF_USE_PROFILE_IMAGES`
 * `COR_PRF_REQUIRE_PROFILE_IMAGE`
