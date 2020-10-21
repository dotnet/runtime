# Semantic Compatibility

Documentation on compatibility guidance and the current state. The version headings act as a rolling delta between the previous version.

## Version 1

The focus of version 1 is to support `NetCoreApp`. This implies that anything not needed by `NetCoreApp` is subject to change.

### Semantic changes compared to `DllImportAttribute`

The default value of `CharSet` is runtime/language-defined. In the built-in system, the default value of the `CharSet` property is `CharSet.Ansi`. The P/Invoke source generator makes no assumptions about the `CharSet` if it is not explicitly set on `GeneratedDllImportAttribute`. Marshalling of `char` or `string` requires explicitly specifying marshalling information.

The built-in system treats `CharSet.None` as `CharSet.Ansi`. The P/Invoke source generator will treat `CharSet.None` as if the `CharSet` was not set.

### `char` marshaller

Marshalling of `char` will not be supported when configured with any of the following:
  - [`CharSet.Ansi`, `CharSet.Auto`, or `CharSet.None`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.charset) will not be supported.
  - [`UnmangedType.U1` or `UnmangedType.I1`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either [`DllImportAttribute.CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

For `CharSet.Ansi` and `CharSet.None`, the built-in system used the [system default Windows ANSI code page](https://docs.microsoft.com/windows/win32/api/stringapiset/nf-stringapiset-widechartomultibyte) when on Windows and took the first byte of the UTF-8 encoding on non-Windows platforms. The above reasoning also applies to marshalling of a `char` as `UnmanagedType.U1` and `UnmanagedType.I1`. All approaches are fundamentally flawed and therefore not supported. If a single-byte character is expected to be marshalled it is left to the caller to convert a .NET `char` into a single `byte` prior to calling the native function.

For `CharSet.Auto`, the built-in system relied upon detection at runtime of the platform when determining the targeted encoding. Performing this check in generated code violates the "pay-for-play" principle. Given that there are no scenarios for this feature in `NetCoreApp` it will not be supported.

### `string` marshaller

Marshalling of `string` will not be supported when configured with any of the following:
  - [`CharSet.None`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.charset)
  - [`UnmangedType.VBByRefStr`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either [`DllImportAttribute.CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

When converting from native to managed, the built-in system would throw a [`MarshalDirectiveException`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshaldirectiveexception) if the string's length is over 0x7ffffff0. The generated marshalling code will no longer perform this check.

In the built-in system, marshalling a `string` contains an optimization for parameters passed by value to allocate on the stack (instead of through [`AllocCoTaskMem`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.alloccotaskmem)) if the string is below a certain length (MAX_PATH). For UTF-16, this optimization was also applied for parameters passed by read-only reference. The generated marshalling code will include this optimization for read-only reference parameters for non-UTF-16 as well.

### Custom marshaller support

Using a custom marshaller (i.e. [`ICustomMarshaler`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.icustommarshaler)) with the `UnmanagedType.CustomMarshaler` value on `MarshalAsAttribute` is not supported. This also implies `MarshalAsAttribute` fields: [`MarshalTypeRef`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshaltyperef), [`MarshalType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshaltype), and [`MarshalCookie`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshalcookie) are unsupported.

## Verison 0

This version is the built-in IL Stub generation system that is triggered whenever a method marked with `DllImport` is invoked.