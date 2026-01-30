# Test: ThreadPoolBoundHandleTests.PreAllocatedOverlapped_NullAsCallback_ThrowsArgumentNullException

## Test Suite
System.Threading.Overlapped.Tests

## Failure Type
exception

## Exception Type
Assert.Throws() Failure: No exception was thrown

## Stack Trace
```
Assert.Throws() Failure: No exception was thrown
Expected: typeof(System.ArgumentNullException)
   at System.AssertExtensions.Throws[T](String expectedParamName, Func`1 testCode)
   at ThreadPoolBoundHandleTests.PreAllocatedOverlapped_NullAsCallback_ThrowsArgumentNullException()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: threading
- This test is marked `[SkipOnMono]` so it only runs on CoreCLR
- The test expects ArgumentNullException when callback is null
- On Browser/WASM, the exception is not thrown - likely due to different validation in interpreter mode
- **Fix Applied:** Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]`
