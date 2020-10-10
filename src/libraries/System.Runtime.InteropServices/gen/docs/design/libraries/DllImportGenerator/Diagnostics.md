# Generator Diagnostics

For all [Roslyn diagnostics](https://docs.microsoft.com/dotnet/api/microsoft.codeanalysis.diagnostic) reported by the P/Invoke source generator:

| Setting  | Value              |
| -------- | ------------------ |
| Category | DllImportGenerator |
| Severity | Error              |
| Enabled  | True               |

The P/Invoke source generator emits the following diagnostics.

## `DLLIMPORTGEN001`: Specified type is not supported by source-generated P/Invokes

A method marked `GeneratedDllImport` has a parameter or return type that is not supported by source-generated P/Invokes.

```C#
// 'object' without any specific marshalling configuration
[GeneratedDllImport("NativeLib")]
public static partial void Method(object o);
```

## `DLLIMPORTGEN002`: Specified configuration is not supported by source-generated P/Invokes

A method marked `GeneratedDllImport` has configuration that is not supported by source-generated P/Invokes. This may be configuration of the method itself or its parameter or return types.

```C#
// MarshalAs value that does not map to an UnmanagedType
[GeneratedDllImport("NativeLib")]
public static partial void Method([MarshalAs(1)] int i);

// Unsupported field on MarshalAs (SafeArraySubType, SafeArrayUserDefinedSubType, IidParameterIndex)
[GeneratedDllImport("NativeLib")]
public static partial void Method([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BOOL)] bool[] bArr);

// Unsupported combination of MarshalAs and type being marshalled
[GeneratedDllImport("NativeLib")]
public static partial void Method([MarshalAs(UnmanagedType.LPStr)] bool b);
```