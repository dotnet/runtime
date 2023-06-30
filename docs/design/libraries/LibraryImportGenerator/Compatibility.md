# Semantic Compatibility

Documentation on compatibility guidance and the current state. The version headings act as a rolling delta between the previous version.

## Version 3 (.NET 8)

### Safe Handles

Due to trimming issues with NativeAOT's implementation of `Activator.CreateInstance`, we have decided to change our recommendation of providing a public parameterless constructor for `ref`, `out`, and return scenarios to a requirement. We already required a parameterless constructor of some visibility, so changing to a requirement matches our design principles of taking breaking changes to make interop more understandable and enforce more of our best practices instead of going out of our way to provide backward compatibility at increasing costs.

### `UnmanagedType.Interface`

Support for `MarshalAs(UnmanagedType.Interface)` is added to the interop source generators. `UnmanagedType.Interface` will marshal a parameter/return value of a type `T` to a COM interface pointer the `ComInterfaceMarshaller<T>` type. It will not support marshalling through the built-in COM interop subsystem.

The `ComInterfaceMarshaller<T>` type has the following general behavior: An unmanaged pointer is marshalled to a managed object through `GetOrCreateObjectForComInstance` on a shared `StrategyBasedComWrappers` instance. A managed object is marshalled to an unmanaged pointer through that same shared instance with the `GetOrCreateComInterfaceForObject` method and then calling `QueryInterface` on the returned `IUnknown*` to get the pointer for the unmanaged interface with the IID from the managed type as defined by our default interface details strategy (or the IID of `IUnknown` if the managed type has no IID).

### Strict Blittability

Strict blittability checks have been slightly relaxed. A few select types are now considered strictly blittable that previously were not:

- `System.Runtime.InteropServices.CLong`
- `System.Runtime.InteropServices.CULong`
- `System.Runtime.InteropServices.NFloat`
- `System.Guid`

The first three types are interop intrinsics that were specifically designed to be used at the unmanaged API layer, so they should be considered blittable by all interop systems. `System.Guid` is extremely commonly used in COM-based APIs, and with the move to supporting COM interop in source-generation, this type shows up in signatures quite a bit. As .NET has always maintained that `Guid` is a blittable representation of `GUID` and it is marked as `NonVersionable` (so we have already committed to maintain the shape between multiple versions of the runtime), we have decided to add it to the list of strictly blittable types.

We strive to keep this list of "exceptions" to our strict blittability rules small, but we will continue to evaluate the list of types in the future as we explore new interop scenarios. We will follow these rules to determine if we will consider encoding the type as strictly blittable in the future:

- The type is defined in the same assembly as `System.Object` (the core assembly) and is marked either as `NonVersionable` or `Intrinsic` in CoreCLR's System.Private.CoreLib.
- The type is an primitive ABI type defined by the interop team.

Types that meet one of these two criterion will have stable shapes and will not accidentally introduce non-blittable fields like `bool` and `char`, so we can consider adding them to the exception list. We do not guarantee that we will add any more types as exceptions in the future, but we will consider it if we find a compelling reason to do so.

## Version 2 (.NET 7 Release)

The focus of version 2 is to support all repos that make up the .NET Product, including ASP.NET Core and Windows Forms, as well as all packages in dotnet/runtime.

### User defined type marshalling

Support for user-defined type marshalling in the source-generated marshalling is described in [UserTypeMarshallingV2.md](UserTypeMarshallingV2.md). This support replaces the designs specified in [StructMarshalling.md](StructMarshalling.md) and [SpanMarshallers.md](SpanMarshallers.md).


## Version 1 (.NET 6 Prototype and .NET 7 Previews)

The focus of version 1 is to support `NetCoreApp`. This implies that anything not needed by `NetCoreApp` is subject to change.

### Fallback mechanism

In the event a marshaller would generate code that has a specific target framework or version requirement that is not satisfied, the generator will instead produce a normal `DllImportAttribute` declaration. This fallback mechanism enables the use of `LibraryImportAttribute` in most circumstances and permits the conversion from `DllImportAttribute` to `LibraryImportAttribute` to be across most code bases. There are instances where the generator will not be able to handle signatures or configuration. For example, uses of `StringBuilder` are not supported in any form and consumers should retain uses of `DllImportAttribute`. Additionally, `LibraryImportAttribute` cannot represent all settings available on `DllImportAttribute`&mdash;see below for details.

### Semantic changes compared to `DllImportAttribute`

[`CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) has been replaced with a new `StringMarshalling` enumeration. `Ansi` and `Auto` are no longer supported as first-class options and `Utf8` has been added.

With `DllImportAttribute`, the default value of [`CharSet`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.charset) is runtime/language-defined. In the built-in system, the default value of the `CharSet` property is `CharSet.Ansi`. The P/Invoke source generator makes no assumptions about `StringMarshalling` if it is not explicitly set on `LibraryImportAttribute`. Marshalling of `char` or `string` requires explicitly specifying marshalling information.

[`BestFitMapping`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.bestfitmapping) and [`ThrowOnUnmappableChar`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.throwonunmappablechar) will not be supported for `LibraryImportAttribute`. These values only have meaning on Windows when marshalling string data (`char`, `string`, `StringBuilder`) as [ANSI](https://docs.microsoft.com/windows/win32/intl/code-pages). As the general recommendation - including from Windows - is to move away from ANSI, the P/Invoke source generator will not support these fields.

[`CallingConvention`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.callingconvention) will not be supported for `LibraryImportAttribute`. Users will be required to use the new `UnmanagedCallConvAttribute` attribute instead. This attribute provides support for extensible calling conventions and provides parity with the `UnmanagedCallersOnlyAttribute` attribute and C# function pointer syntax. We will enable our conversion code-fix to automatically convert explicit and known calling convention usage to use the `UnmanagedCallConvAttribute`.

[`ExactSpelling`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.dllimportattribute.exactspelling) will not be supported for `LibraryImportAttribute`. If `ExactSpelling` is used on an existing `DllImport`, the offered code-fix will provide users with additional options for using `A` or `W` suffixed variants depending on the provided `CharSet` so they can explicitly choose which spelling is correct for their scenario.

### Required references

The following framework references are required:
- `System.Memory`
- `System.Runtime`
- `System.Runtime.InteropServices`

These are all part of `NetCoreApp` and will be referenced by default unless [implicit framework references are disabled](https://docs.microsoft.com/dotnet/core/project-sdk/msbuild-props#disableimplicitframeworkreferences).

### `char` marshalling

Marshalling of `char` will only be supported with `StringMarshalling.Utf16` or as `UnmanagedType.U2` or `UnmanagedType.I2`. It will not be supported when configured with any of the following:
  - [`UnmanagedType.U1` or `UnmanagedType.I1`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - `StringMarshalling.Utf8` will not be supported.
  - No explicit marshalling information - either `LibraryImportAttribute.StringMarshalling` or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

In the built-in system, marshalling with `CharSet.Ansi` and `CharSet.None` used the [system default Windows ANSI code page](https://docs.microsoft.com/windows/win32/api/stringapiset/nf-stringapiset-widechartomultibyte) when on Windows and took the first byte of the UTF-8 encoding on non-Windows platforms. The above reasoning also applies to marshalling of a `char` as `UnmanagedType.U1` and `UnmanagedType.I1`. All approaches are fundamentally flawed and therefore not supported. If a single-byte character is expected to be marshalled it is left to the caller to convert a .NET `char` into a single `byte` prior to calling the native function.

For `CharSet.Auto`, the built-in system relied upon detection at runtime of the platform when determining the targeted encoding. Performing this check in generated code violates the "pay-for-play" principle. Given that there are no scenarios for this feature in `NetCoreApp` it will not be supported.

### `string` marshalling

Marshalling of `string` will not be supported when configured with any of the following:
  - [`UnmanagedType.VBByRefStr`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype)
  - No explicit marshalling information - either `LibraryImportAttribute.StringMarshalling` or [`MarshalAsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshalasattribute)

When converting from native to managed, the built-in system would throw a [`MarshalDirectiveException`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshaldirectiveexception) if the string's length is over 0x7ffffff0. The generated marshalling code will no longer perform this check.

In the built-in system, marshalling a `string` contains an optimization for parameters passed by value to allocate on the stack (instead of through [`AllocCoTaskMem`](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.marshal.alloccotaskmem)) if the string is below a certain length (MAX_PATH). For UTF-16, this optimization was also applied for parameters passed by read-only reference. The generated marshalling code will include this optimization for read-only reference parameters for non-UTF-16 as well.

When marshalling as [ANSI](https://docs.microsoft.com/windows/win32/intl/code-pages) on Windows (using `UnmanagedType.LPStr`):
  - Best-fit mapping will be disabled and no exception will be thrown for unmappable characters. In the built-in system, this behaviour was configured through [`DllImportAttribute.BestFitMapping`] and [`DllImportAttribute.ThrowOnUnmappableChar`]. The generated marshalling code will have the equivalent behaviour of `BestFitMapping=false` and `ThrowOnUnmappableChar=false`.

The p/invoke source generator does not provide an equivalent to using `CharSet.Auto` in the built-in system. If platform-dependent behaviour is desired, it is left to the user to define different p/invokes with different marshalling configurations.

### `bool` marshalling

We have decided to use `System.Runtime.CompilerServices.DisableRuntimeMarshalling` to enable our custom value type marshalling support. As a result, when a value type that has a `bool` field is passed to native code through source-generated marshalling, the `bool` field is treated as a 1-byte value and is not normalized. Since this default is a little odd and unlikely to be the majority use case, we're going to generally take a stance that all `bool` marshalling must be explicitly specified via `MarshalAs` or other mechanisms.

To aid in conversion from `DllImport` to source-generated marshalling, the code-fix will automatically apply a `[MarshalAs(UnmangedType.Bool)]` attribute to `bool` parameters and return values to ensure the marshalling rules are not changed by the code fix.

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

[`LCIDConversionAttribute`](`https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.lcidconversionattribute`) will not be supported for methods marked with `LibraryImportAttribute`.

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
