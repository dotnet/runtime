# Semantic Compatibility

Documentation on compatibility guidance and the current state. The version headings act as a rolling delta between the previous version.

## Version 1

The focus of version 1 is to support `NetCoreApp`. This implies that anything not needed by `NetCoreApp` is subject to change.

### Semantic changes compared to `DllImportAttribute`

The default value of [`CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) is runtime/language-defined. In the built-in system, the default value of the `CharSet` property is `CharSet.Ansi`. The P/Invoke source generator makes no assumptions about the `CharSet` if it is not explicitly set on `GeneratedDllImportAttribute`. Marshalling of `char` or `string` requires explicitly specifying marshalling information.

The built-in system treats `CharSet.None` as `CharSet.Ansi`. The P/Invoke source generator will treat `CharSet.None` as if the `CharSet` was not set.

[`BestFitMapping`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.bestfitmapping) and [`ThrowOnUnmappableChar`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.throwonunmappablechar) will not be supported for `GeneratedDllImportAttribute`. These values only have meaning on Windows when marshalling string data (`char`, `string`, `StringBuilder`) as [ANSI](https://docs.microsoft.com/windows/win32/intl/code-pages). As the general recommendation - including from Windows - is to move away from ANSI, the P/Invoke source generator will not support these fields.

### `char` marshalling

Marshalling of `char` will not be supported when configured with any of the following:
  - [`CharSet.Ansi`, `CharSet.Auto`, or `CharSet.None`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.charset) will not be supported.
  - [`UnmangedType.U1` or `UnmangedType.I1`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either [`DllImportAttribute.CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

For `CharSet.Ansi` and `CharSet.None`, the built-in system used the [system default Windows ANSI code page](https://docs.microsoft.com/windows/win32/api/stringapiset/nf-stringapiset-widechartomultibyte) when on Windows and took the first byte of the UTF-8 encoding on non-Windows platforms. The above reasoning also applies to marshalling of a `char` as `UnmanagedType.U1` and `UnmanagedType.I1`. All approaches are fundamentally flawed and therefore not supported. If a single-byte character is expected to be marshalled it is left to the caller to convert a .NET `char` into a single `byte` prior to calling the native function.

For `CharSet.Auto`, the built-in system relied upon detection at runtime of the platform when determining the targeted encoding. Performing this check in generated code violates the "pay-for-play" principle. Given that there are no scenarios for this feature in `NetCoreApp` it will not be supported.

### `string` marshalling

Marshalling of `string` will not be supported when configured with any of the following:
  - [`CharSet.None`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.charset)
  - [`UnmangedType.VBByRefStr`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either [`DllImportAttribute.CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

When converting from native to managed, the built-in system would throw a [`MarshalDirectiveException`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshaldirectiveexception) if the string's length is over 0x7ffffff0. The generated marshalling code will no longer perform this check.

In the built-in system, marshalling a `string` contains an optimization for parameters passed by value to allocate on the stack (instead of through [`AllocCoTaskMem`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.alloccotaskmem)) if the string is below a certain length (MAX_PATH). For UTF-16, this optimization was also applied for parameters passed by read-only reference. The generated marshalling code will include this optimization for read-only reference parameters for non-UTF-16 as well.

When marshalling as [ANSI](https://docs.microsoft.com/windows/win32/intl/code-pages) on Windows (using `CharSet.Ansi`, `CharSet.Auto`, or `UnmanagedType.LPStr`):
  - Best-fit mapping will be disabled and no exception will be thrown for unmappable characters. In the built-in system, this behaviour was configured through [`DllImportAttribute.BestFitMapping`] and [`DllImportAttribute.ThrowOnUnmappableChar`]. The generated marshalling code will have the equivalent behaviour of `BestFitMapping=false` and `ThrowOnUnmappableChar=false`.
  - No optimization for stack allocation will be performed. Marshalling will always allocate through `AllocCoTaskMem`.

On Windows, marshalling using `CharSet.Auto` is treated as UTF-16. When marshalling a string as UTF-16 by value or by read-only reference, the string is pinned and the pointer passed to the P/Invoke. The generated marshalling code will always pin the input string - even on non-Windows.

### Custom marshaller support

Using a custom marshaller (i.e. [`ICustomMarshaler`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.icustommarshaler)) with the `UnmanagedType.CustomMarshaler` value on `MarshalAsAttribute` is not supported. This also implies `MarshalAsAttribute` fields: [`MarshalTypeRef`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshaltyperef), [`MarshalType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshaltype), and [`MarshalCookie`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshalcookie) are unsupported.

### Array marshalling

Marshalling of arrays will not be supported when using [`UnmangedType.SafeArray`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype). This implies that the following `MarshalAsAttribute` fields are unsupported: [`SafeArraySubType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.safearraysubtype) and [`SafeArrayUserDefinedSubType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.safearrayuserdefinedsubtype)

Specifying array-specific marshalling members on the `MarshalAsAttribute` such as [`SizeConst`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.sizeconst), [`ArraySubType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.arraysubtype), and [`SizeParamIndex`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.sizeparamindex) with non-array `UnmanagedType` types is unsupported.

Only single-dimensional arrays are supported for source generated marshalling.

Jagged arrays (arrays of arrays) are technically unsupported as was the case in the built-in marshalling system, but currently are not explicitly blocked by the source generator since they are not blocked at an architectural level, which was the case in the built-in system.

In the source-generated marshalling, arrays will be allocated on the stack (instead of through [`AllocCoTaskMem`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.alloccotaskmem)) if they are passed by value or by read-only reference if they contain at most 256 bytes of data. The built-in system does not support this optimization for arrays.

### `LCIDConversion` support

[`LCIDConversionAttribute`](`https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.lcidconversionattribute`) will not be supported for methods marked with `GeneratedDllImportAttribute`.

### `[In, Out]` Attributes

In the source generated marshalling, the `[In]` and `[Out]` attributes will only be supported on parameters passed by value. For by-ref parameters, users should use the `in`, `ref`, or `out` keywords respectively. Additionally, they will only be supported in scenarios where applying them would result in behavior different from the default, such as applying `[Out]` or `[In, Out]` to a by-value non-blittable array parameter. This is in contrast to the built-in system which will allow them in all cases even when they have no effect.

### Struct marshalling

Support for struct marshalling in the source-generated marshalling is described in [StructMarshalling.md](StructMarshalling.md).

## Verison 0

This version is the built-in IL Stub generation system that is triggered whenever a method marked with `DllImport` is invoked.
