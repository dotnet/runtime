# Test: System.Tests.StringTests.Contains_Rune_StringComparison

## Test Suite
System.Runtime.Tests

## Failure Type
exception

## Exception Type
System.ArgumentOutOfRangeException

## Stack Trace
```
at System.Text.Rune..ctor(Char ch)
at System.Text.Rune.op_Explicit(Char ch)
at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

## Failure Details
- Message: "Specified argument was out of the range of valid values. (Parameter 'ch')"
- The Rune constructor rejects a character that should be valid

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter
- Issue with Rune explicit cast from Char
