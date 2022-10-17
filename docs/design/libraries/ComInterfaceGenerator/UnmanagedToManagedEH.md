# Unmanaged to Managed Exception Handling

As part of building out the "vtable method stub" generator as the first steps on our path to the COM source generator, we need to determine how to handle exceptions at the managed code boundary in the unmanaged-calling-managed direction. When implementing a Com Callable Wrapper (or equivalent concept), the source generator will need to generate an `[UnmanagedCallersOnly]` wrapper of a method. We do not support propogating exceptions across an `[UnmanagedCallersOnly]` method on all platforms, so we need to determine how to handle exceptions.

We determined that there were three options:

1. Do nothing
2. Match the existing behavior in the runtime for `[PreserveSig]` on a method on a `[ComImport]` interface.
3. Allow the user to provide their own "exception -> return value" translation.

## Option 1: Do Nothing

This option seems the simplest option. The source generator will not emit a `try-catch` block and will just let the exception propogate when in a wrapper for a `VirtualMethodTableIndex` stub. There's a big problem with this path though. One goal of our COM source generator design is to let developers drop down a particular method from the "HRESULT-swapped, all nice COM defaults" mechanism to a `VirtualMethodTableIndex`-attributed method for particular methods when they do not match the expected default behaviors. This feature becomes untenable if we do not wrap the stub in a `try-catch`, as suddenly there is no mechanism to emulate the exception handling of exceptions thrown by marshallers. For cases where all marshallers are known to not throw exceptions except in cases where the process is unrecoverable (i.e. `OutOfMemoryException`), this version may provide a slight performance improvement as there will be no exception handling in the generated stub.

## Option 2: Match the existing behavior in the runtime for `[PreserveSig]` on a method on a `[ComImport]` interface

The runtime has some existing built-in behavior for generating a return value from an `Exception` in an unmanaged-to-managed `COM` stub. The behavior depends on the return type, and matches the following table:

| Type            | Return Value in Exceptional Scenario |
|-----------------|--------------------------------------|
| `void`          | Swallow the exception                |
| `int`           | `exception.HResult`                  |
| `uint`          | `(uint)exception.HResult`            |
| `float`         | `float.NaN`                          |
| `double`        | `double.NaN`                         |
| All other types | `default`                            |

We could match this behavior for all `VirtualMethodTableIndex`-attributed method stubs, but that would be forcibly encoding the rules of HResults and COM for our return types, and since COM is a Windows-centric technology, we don't want to lock users into this model in case they have different error code models that they want to integrate with. However, it does provide an exception handling boundary

## Option 3: Allow the user to provide their own "exception -> return value" translation

Another option would be to give the user control of how to handle the exception marshalling, with an option for some nice defaults to replicate the other options with an easy opt-in. We would provide the following enumeration and new members on `VirtualMethodTableIndexAttribute`:

```diff
namespace System.Runtime.InteropServices.Marshalling;

public class VirtualMethodTableIndexAttribute
{
+     public ExceptionMarshalling ExceptionMarshalling { get; }
+     public Type ExceptionMarshallingType { get; }
}

+ public enum ExceptionMarshalling
+ {
+     None, // No exception handling (equivalent to Option 1)
+     Com, // Match the COM-focused model described in Option 2
+     Custom
+ }
```

When the user sets `ExceptionMarshalling = ExceptionMarshalling.Custom`, they must specify a marshaller type in `ExceptionMarshallingType` that unmarshals a `System.Exception` to the same unmanaged return type as the marshaller of the return type in the method's signature.

To implement the `ExceptionMarshalling.Com` option, we will provide some marshallers to implement the different rules above, and select the correct rule given the unmanaged return type from the return type's marshaller. By basing the decision on the unmanaged type instead of the managed type, this mechanism will be able to kick in correctly for projects that use their own custom `HResult` struct that wraps an `int` or `uint`, as the `HResult` will be marshalled to an `int` or `uint` if it is well-defined.
