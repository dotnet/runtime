# Managed/Native Interop in AOT

## Direct PInvoke Calls

The PInvoke calls in AOT compiled binaries are bound lazily at runtime by default for better compatibility. The AOT compiler
can be configured to generate direct calls for selected PInvoke methods that are bound during startup. The unmanaged libraries 
and entrypoints referenced via direct calls have to be always available at runtime, otherwise the native binary fails to start.

The benefits of direct PInvoke calls are:
- Direct PInvoke calls have *better steady state performance*
- Direct PInvoke calls make it possible to *link the unmanaged dependencies statically*

The direct PInvoke generation can be configured using `<DirectPInvoke>` items in your .csproj file. The item name can be either `modulename`
that enables direct calls for all module entrypoints, or `modulename!entrypointname` that enables direct call for specific module and entrypoint
only.

`<DirectPInvokeList>filename</DirectPInvokeList>` items in your .csproj file allow specifying a list in external file. It is useful when
specification of PInvoke direct calls is long and it is not practical to specify it using individual `<DirectPInvoke>` items. The file can
contain empty lines and comments starting with `#`.

Examples:

```xml
<ItemGroup>
  <!-- Generate direct PInvoke calls for everything in __Internal -->
  <!-- This option is replicates Mono AOT behavior that generates direct PInvoke calls for __Internal -->
  <DirectPInvoke Include="__Internal" />
  <!-- Generate direct PInvoke calls for everything in libc (also matches libc.so on Linux or libc.dylib on macOS --> 
  <DirectPInvoke Include="libc" />
  <!-- Generate direct PInvoke calls for Sleep in kernel32 (also matches kernel32.dll on Windows) -->
  <DirectPInvoke Include="kernel32!Sleep" />
  <!-- Generate direct PInvoke for all APIs listed in DirectXAPIs.txt -->
  <DirectPInvokeList Include="DirectXAPIs.txt" />
</ItemGroup>
```

### Linking

To statically link against a unmanaged library, you'll need to specify `<NativeLibrary Include="filename" />` pointing to `.lib` file on Windows and `.a` file on Unix

Examples:

```xml
<ItemGroup>
  <!-- Generate direct PInvokes for Dependency -->
  <DirectPInvoke Include="Dependency" />
  <!-- Specify library to link against -->
  <NativeLibrary Include="Dependency.lib" Condition="$(RuntimeIdentifier.StartsWith('win'))" />
  <NativeLibrary Include="Dependency.a" Condition="!$(RuntimeIdentifier.StartsWith('win'))" />
</ItemGroup>
```

## Native Exports

The native AOT compiler will export methods annotated with `UnmanagedCallersOnlyAttribute` and explicitly specified name as
public C entrypoints. It makes it possible to either dynamically or statically link the AOT compiled modules into external
programs. More details are in [NativeLibrary Sample](../../samples/NativeLibrary).
