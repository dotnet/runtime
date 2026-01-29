# Test: System.Tests.UInt128Tests.CompareTo_Other_ReturnsExpected

## Test Suite
System.Runtime.Tests

## Failure Type
exception

## Exception Type
Xunit.Sdk.EqualException

## Stack Trace
```
at System.Tests.UInt128Tests.CompareTo_Other_ReturnsExpected(UInt128 i, Object value, Int32 expected)
at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

## Failure Details
Test case: `(i: 234, value: 234, expected: 0)`
- Expected: 0
- Actual: 1

When comparing UInt128(234) to another UInt128(234), CompareTo should return 0 but returns 1.

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter
- Same issue as Int128Tests.CompareTo_Other_ReturnsExpected - likely related to boxed comparison
