# Test: DispatchProxyTests.DispatchProxyTests.Create_Using_PrivateProxyAndInternalServiceWithExternalGenericArgument

## Test Suite
System.Reflection.DispatchProxy.Tests

## Failure Type
exception

## Exception Type
System.TypeLoadException

## Stack Trace
```
System.TypeLoadException : Type 'generatedProxy_7' from assembly 'ProxyBuilder, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' is attempting to implement an inaccessible interface.
   at System.Reflection.Emit.RuntimeTypeBuilder.TermCreateClass(QCallModule module, Int32 tk, ObjectHandleOnStack type)
   at System.Reflection.Emit.RuntimeTypeBuilder.CreateTypeNoLock()
   at System.Reflection.Emit.RuntimeTypeBuilder.CreateTypeInfoImpl()
   at System.Reflection.Emit.RuntimeTypeBuilder.CreateTypeInfoCore()
   at System.Reflection.Emit.TypeBuilder.CreateTypeInfo()
   at System.Reflection.Emit.TypeBuilder.CreateType()
   at System.Reflection.DispatchProxyGenerator.ProxyBuilder.CreateType()
   at System.Reflection.DispatchProxyGenerator.ProxyAssembly.GenerateProxyType(Type baseType, Type interfaceType, String interfaceParameter, String proxyParameter)
   at System.Reflection.DispatchProxyGenerator.ProxyAssembly.GetProxyType(Type baseType, Type interfaceType, String interfaceParameter, String proxyParameter)
   at System.Reflection.DispatchProxyGenerator.CreateProxyInstance(Type baseType, Type interfaceType, String interfaceParameter, String proxyParameter)
   at System.Reflection.DispatchProxy.Create[T,TProxy]()
   at TestType_PrivateProxy.Proxy[T]()
   at DispatchProxyTests.DispatchProxyTests.Create_Using_PrivateProxyAndInternalServiceWithExternalGenericArgument()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: reflection-emit
- Description: When creating a DispatchProxy with a private proxy type and an internal service interface that uses an external generic argument, the dynamically generated proxy type fails to implement the interface due to accessibility constraints.
- The test uses `TestType_PrivateProxy.Proxy<T>()` which internally calls `DispatchProxy.Create<T,TProxy>()` where the interface has accessibility restrictions that differ between Mono and CoreCLR on the Browser platform.
- This appears to be a difference in how CoreCLR handles cross-assembly accessibility for dynamically emitted types on the Browser/WASM platform.
- **Fix Applied:** Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]` attribute to skip on Browser platform.
