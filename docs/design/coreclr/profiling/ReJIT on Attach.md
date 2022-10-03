# ReJIT on Attach

A longstanding feature request we've had from profiler authors is the ability to ReJIT a method after attach. There are non-trivial technical reasons this was never an option in desktop .Net, but CoreCLR has had some changes that made it more feasible to attain. As of .Net Core 3 preview5 profiler authors now have the ability to ReJIT methods after attach.

## The new API

To enable ReJIT on attach there is a new API `ICorProfilerInfo10::RequestReJITWithInliners`. Here is the signature:

```cpp
    HRESULT RequestReJITWithInliners(
                [in]                       DWORD       dwRejitFlags,
                [in]                       ULONG       cFunctions,
                [in, size_is(cFunctions)]  ModuleID    moduleIds[],
                [in, size_is(cFunctions)]  mdMethodDef methodIds[]);
```

Conceptually this works the same as `ICorProfilerInfo4::RequestReJIT` except it will automatically ReJIT any methods that have inlined the target method(s) in the past. The arguments are the same except for the addition of `dwRejitFlags` as the first parameter. The valid values for this argument come from this enum:

```cpp
typedef enum
{
    // ReJITted methods will be prevented from being inlined
    COR_PRF_REJIT_BLOCK_INLINING = 0x1,

    // This flag controls whether the runtime will call GetReJITParameters
    // on methods that are ReJITted because they inline a method that was requested
    // for ReJIT
    COR_PRF_REJIT_INLINING_CALLBACKS    = 0x2
} COR_PRF_REJIT_FLAGS;
```

Any callers of this API must set `COR_PRF_REJIT_BLOCK_INLINING`. Although it is possible that in the future this restriction will be lifted, the current implementation blocks ReJITted methods from being inlined (ever).

The other value `COR_PRF_REJIT_INLINING_CALLBACKS` controls whether you get a `ICorProfilerCallback4::GetReJITParameters` callback for any methods that are ReJITted as inliners of the requested method. The default is to not receive callbacks for these methods. You will always receive a `GetReJITParameters` callback for any methods that are explicitly requested.


## Inner workings/Limitations

With this API you are no longer required to monitor JIT callbacks to manually block inlining from occurring. To achieve that the runtime now globally blocks a ReJITted method from being inlined (even if it was ReJITted with `ICorProfilerInfo4::RequestReJIT` and not the new API). Once a method is reverted with `ICorProfilerInfo4::RequestRevert` inlining will occur again for any future jittings.

It is important to mention here how `RequestRevert` works. When you revert a ReJITted method, the original native code is activated. This means there are potential pitfalls for calling `RequestRevert`. Consider an app where method A inlines method B and the profiler wants to ReJIT both A and B. Once A and B are both ReJITted, the application will behave as expected. However, if later on the profiler decides to revert method A but intends to leave method B ReJITted, it might be surprising to find that once the original native code for A is activated this includes the inlined non-ReJIT IL for method B. Effectively any calls to B through A will be calling the original, unmodified IL.

To revert a method without having to reason about the inline sequence, we suggest calling RequestReJIT again on the method but providing the original IL in GetReJITParameters.

The limitation of collectible and dynamic methods has not been lifted. It is not currently possible to ReJIT these types of methods, although we would like to lift that restriction in the future. Even if you never intend to call RequestReJIT directly on a collectible or dynamic method, this may still affect you when doing ReJIT on attach if the method you would like to ReJIT has been inlined in a collectible or dynamic method. I.e. if you would like to ReJIT method A which has been inlined in collectible method B, there is currently no way to make method B call the updated method A.

## Metadata Changes on Attach

Usually profiler authors do not want to trivially change the IL for ReJITted methods, but rather inject new types/methods and call those new types/methods. Previously our guidance has been to do any metadata rewriting during the `ICorProfilerCallback::ModuleLoadFinished` callback. This advice presents a challenge if the profiler is not attached during module load and still wants to modify metadata.

To work around this a set of metadata changes is now legal to make at any point as long as you call `ICorProfilerInfo7::ApplyMetadata` afterwards:
* `DefineUserString`
* `DefineTypeRefByName`
* `DefineMemberRef`
* `DefineTypeDef`
* `DefineMethod`            - methods on new types, or non-virtual methods on existing types
* `DefineNestedType`        - only on new types
* `DefineCustomAttribute`
* `DefinePinvokeMap`
* `DefineModuleRef`
* `DefineField`             - only on new types
* `DefineEvent`
* `DefineMethodImpl`        - only on new types
* `DefineMethodSpec`


There are still some metadata changes that are definitely illegal and will almost certainly never become legal due to restrictions/assumptions existing in the runtime:
* Adding a virtual method to an existing type
* Adding a field to an existing type

Anything not listed as either legal or illegal is untested and should not be assumed to work.
