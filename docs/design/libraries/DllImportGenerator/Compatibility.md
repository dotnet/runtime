# Semantic Compatibility

Documentation on compatibility guidance and the current state. The version headings act as a rolling delta between the previous version.

## Version 1

The focus of version 1 is to support `NetCoreApp`. This implies that anything not needed by `NetCoreApp` is subject to change.

### Fallback mechanism

In the event a marshaller would generate code that has a specific target framework or version requirement that is not satisfied, the generator will instead produce a normal `DllImportAttribute` declaration. This fallback mechanism enables the use of `GeneratedDllImportAttribute` in most circumstances and permits the conversion from `DllImportAttribute` to `GeneratedDllImportAttribute` to be across most code bases. There are instances where the generator will not be able to handle signatures or configuration. For example, uses of `StringBuilder` are not supported in any form and consumers should retain uses of `DllImportAttribute`. Additionally, `GeneratedDllImportAttribute` cannot represent all settings available on `DllImportAttribute`&mdash;see below for details.

### Semantic changes compared to `DllImportAttribute`

The default value of [`CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) is runtime/language-defined. In the built-in system, the default value of the `CharSet` property is `CharSet.Ansi`. The P/Invoke source generator makes no assumptions about the `CharSet` if it is not explicitly set on `GeneratedDllImportAttribute`. Marshalling of `char` or `string` requires explicitly specifying marshalling information.

The built-in system treats `CharSet.None` as `CharSet.Ansi`. The P/Invoke source generator will treat `CharSet.None` as if the `CharSet` was not set.

[`BestFitMapping`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.bestfitmapping) and [`ThrowOnUnmappableChar`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.throwonunmappablechar) will not be supported for `GeneratedDllImportAttribute`. These values only have meaning on Windows when marshalling string data (`char`, `string`, `StringBuilder`) as [ANSI](https://docs.microsoft.com/windows/win32/intl/code-pages). As the general recommendation - including from Windows - is to move away from ANSI, the P/Invoke source generator will not support these fields.

[`CallingConvention`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.callingconvention) will not be supported for `GeneratedDllImportAttribute`. Users will be required to use the new `UnmanagedCallConvAttribute` attribute instead. This attribute provides support for extensible calling conventions and provides parity with the `UnmanagedCallersOnlyAttribute` attribute and C# function pointer syntax. We will enable our conversion code-fix to automatically convert explicit and known calling convention usage to use the `UnmanagedCallConvAttribute`.

### Required references

The following framework references are required:
- `System.Memory`
- `System.Runtime`
- `System.Runtime.InteropServices`

These are all part of `NetCoreApp` and will be referenced by default unless [implicit framework references are disabled](https://docs.microsoft.com/dotnet/core/project-sdk/msbuild-props#disableimplicitframeworkreferences).

### `char` marshalling

Marshalling of `char` will not be supported when configured with any of the following:
  - [`CharSet.Ansi`, `CharSet.Auto`, or `CharSet.None`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.charset) will not be supported.
  - [`UnmanagedType.U1` or `UnmanagedType.I1`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either [`DllImportAttribute.CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

For `CharSet.Ansi` and `CharSet.None`, the built-in system used the [system default Windows ANSI code page](https://docs.microsoft.com/windows/win32/api/stringapiset/nf-stringapiset-widechartomultibyte) when on Windows and took the first byte of the UTF-8 encoding on non-Windows platforms. The above reasoning also applies to marshalling of a `char` as `UnmanagedType.U1` and `UnmanagedType.I1`. All approaches are fundamentally flawed and therefore not supported. If a single-byte character is expected to be marshalled it is left to the caller to convert a .NET `char` into a single `byte` prior to calling the native function.

For `CharSet.Auto`, the built-in system relied upon detection at runtime of the platform when determining the targeted encoding. Performing this check in generated code violates the "pay-for-play" principle. Given that there are no scenarios for this feature in `NetCoreApp` it will not be supported.

### `string` marshalling

Marshalling of `string` will not be supported when configured with any of the following:
  - [`CharSet.None`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.charset)
  - [`UnmanagedType.VBByRefStr`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either [`DllImportAttribute.CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

When converting from native to managed, the built-in system would throw a [`MarshalDirectiveException`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshaldirectiveexception) if the string's length is over 0x7ffffff0. The generated marshalling code will no longer perform this check.

In the built-in system, marshalling a `string` contains an optimization for parameters passed by value to allocate on the stack (instead of through [`AllocCoTaskMem`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.alloccotaskmem)) if the string is below a certain length (MAX_PATH). For UTF-16, this optimization was also applied for parameters passed by read-only reference. The generated marshalling code will include this optimization for read-only reference parameters for non-UTF-16 as well.

When marshalling as [ANSI](https://docs.microsoft.com/windows/win32/intl/code-pages) on Windows (using `CharSet.Ansi` or `UnmanagedType.LPStr`):
  - Best-fit mapping will be disabled and no exception will be thrown for unmappable characters. In the built-in system, this behaviour was configured through [`DllImportAttribute.BestFitMapping`] and [`DllImportAttribute.ThrowOnUnmappableChar`]. The generated marshalling code will have the equivalent behaviour of `BestFitMapping=false` and `ThrowOnUnmappableChar=false`.
  - No optimization for stack allocation will be performed. Marshalling will always allocate through `AllocCoTaskMem`.

On Windows, marshalling using `CharSet.Auto` is treated as UTF-16. When marshalling a string as UTF-16 by value or by read-only reference, the string is pinned and the pointer passed to the P/Invoke. The generated marshalling code will always pin the input string - even on non-Windows.

### Custom marshaller support

Using a custom marshaller (i.e. [`ICustomMarshaler`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.icustommarshaler)) with the `UnmanagedType.CustomMarshaler` value on `MarshalAsAttribute` is not supported. This also implies `MarshalAsAttribute` fields: [`MarshalTypeRef`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshaltyperef), [`MarshalType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshaltype), and [`MarshalCookie`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.marshalcookie) are unsupported.

### Array marshalling

Marshalling of arrays will not be supported when using [`UnmanagedType.SafeArray`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype). This implies that the following `MarshalAsAttribute` fields are unsupported: [`SafeArraySubType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.safearraysubtype) and [`SafeArrayUserDefinedSubType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.safearrayuserdefinedsubtype)

Specifying array-specific marshalling members on the `MarshalAsAttribute` such as [`SizeConst`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.sizeconst), [`ArraySubType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.arraysubtype), and [`SizeParamIndex`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute.sizeparamindex) with non-array `UnmanagedType` types is unsupported.

Only single-dimensional arrays are supported for source generated marshalling.

In the source-generated marshalling, arrays will be allocated on the stack (instead of through [`AllocCoTaskMem`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.alloccotaskmem)) if they are passed by value or by read-only reference if they contain at most 256 bytes of data. The built-in system does not support this optimization for arrays.

In the built-in system, marshalling a `char` array by value with `CharSet.Unicode` would default to also marshalling data out. In the source-generated marshalling, the `char` array must be marked with the `[Out]` attribute for data to be marshalled out by value.

### `in` keyword

For some types - blittable or Unicode `char` - passed by read-only reference via the [`in` keyword](https://docs.microsoft.com/dotnet/csharp/language-reference/keywords/in-parameter-modifier), the built-in system simply pins the parameter. The generated marshalling code does the same, such that there is no behavioural difference. A consequence of this behaviour is that any modifications made by the invoked function will be reflected in the caller. It is left to the user to avoid the situation in which `in` is used for a parameter that will actually be modified by the invoked function.

### `LCIDConversion` support

[`LCIDConversionAttribute`](`https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.lcidconversionattribute`) will not be supported for methods marked with `GeneratedDllImportAttribute`.

### `[In, Out]` Attributes

In the source generated marshalling, the `[In]` and `[Out]` attributes will only be supported on parameters passed by value. For by-ref parameters, users should use the `in`, `ref`, or `out` keywords respectively. Additionally, they will only be supported in scenarios where applying them would result in behavior different from the default, such as applying `[Out]` or `[In, Out]` to a by-value non-blittable array parameter. This is in contrast to the built-in system which will allow them in all cases even when they have no effect.

### Struct marshalling

Support for struct marshalling in the source-generated marshalling is described in [StructMarshalling.md](StructMarshalling.md).

### Unsupported types

Unlike the built-in system, the source generator does not support marshalling for the following types:
- [`CriticalHandle`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.criticalhandle)
- [`HandleRef`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.handleref)
- [`StringBuilder`](https://docs.microsoft.com/dotnet/api/system.text.stringbuilder)

The source generator also does not support marshalling objects using the following [`UnmanagedType`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype) values:
- `UnmanagedType.Interface`
- `UnmanagedType.IDispatch`
- `UnmanagedType.IInspectable`
- `UnmanagedType.IUnknown`

## Version 0

This version is the built-in IL Stub generation system that is triggered whenever a method marked with `DllImportAttribute` is invoked.
