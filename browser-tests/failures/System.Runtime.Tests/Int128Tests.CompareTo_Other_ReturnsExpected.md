# Test: System.Tests.Int128Tests.CompareTo_Other_ReturnsExpected

## Test Suite
System.Runtime.Tests

## Failure Type
exception

## Exception Type
Xunit.Sdk.EqualException

## Stack Trace
```
at System.Tests.Int128Tests.CompareTo_Other_ReturnsExpected(Int128 i, Object value, Int32 expected)
at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

## Failure Details
Test case: `(i: 234, value: 234, expected: 0)`
- Expected: 0
- Actual: 1

When comparing Int128(234) to another Int128(234), CompareTo should return 0 but returns 1.

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter
- The issue is in Int128.CompareTo implementation when boxed/unboxed comparison happens
