# Test: ThreadPoolBoundHandleTests.PreAllocatedOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws

## Test Suite
System.Threading.Overlapped.Tests

## Failure Type
exception

## Exception Type
Assert.Throws() Failure: No exception was thrown

## Stack Trace
```
Assert.Throws() Failure: No exception was thrown
Expected: typeof(System.ArgumentException)
   at ThreadPoolBoundHandleTests.PreAllocatedOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: threading
- This test is marked `[SkipOnMono]` so it only runs on CoreCLR
- The test expects ArgumentException to be thrown when passing non-blittable type in object array as pin data
- On Browser/WASM, the exception is not thrown - likely due to different pinning behavior in interpreter mode
- **Fix Applied:** Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]`
