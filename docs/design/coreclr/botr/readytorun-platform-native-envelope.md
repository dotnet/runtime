# ReadyToRun Platform Native Envelope

Up through .NET 10, ReadyToRun (R2R) uses the Windows PE format as the native envelope on every platform. Non‑Windows platforms therefore load a PE file with the .NET loader performing the required fixups and code activation.

In .NET 11, we plan to start adding support beyond the PE format. We will target support for:
- Composite R2R only
- Mach-O object files emitted by `crossgen2`
- Runtime using a composite R2R image that is a Mach-O shared library
   - Linking the object files into a shared library is expected to be handled by the SDK and is not covered in this document.

The tentative high-level design is outlined below. As we implement this support, this document should be updated with more details and the [ReadyToRun overview](./readytorun-overview.md) and [ReadyToRun format](./readytorun-format.md) should be updated to reflect the changes.

## crossgen2: producing Mach-O object files

Mach‑O support will only be supported for composite ReadyToRun when the target OS is macOS. It will be opt-in via a new `crossgen2` flag:
- `--obj-format macho`

`crossgen2` will:
- Produce a Mach-O object file as the composite R2R image with the `RTR_HEADER` export for the `READYTORUN_HEADER`.
- Mark each input IL assembly as a component R2R assembly: `READYTORUN_FLAG_COMPONENT`.
- Mark each input IL assembly with a new flag indicating that the associated composite image is in the platform-native format: `READYTORUN_FLAG_PLATFORM_NATIVE_IMAGE`

`crossgen2` does not produce the final shared library. A separate SDK / build linking step must preserve the `RTR_HEADER` export in the final `dylib`.

## Runtime: consuming a platform-native R2R image

The runtime will be updated to handle platform-native R2R images during assembly load.

1. Load IL assembly and determine if it is a R2R assembly.
2. If it is not a component R2R assembly, proceed with existing R2R load logic.
  - We will not have platform-native support for this scenario
3. If it is a component R2R assembly with the new `READYTORUN_FLAG_PLATFORM_NATIVE_IMAGE` flag set:
   a. Read `OwnerCompositeExecutable` value.
   b. Invoke host callback with component assembly path and owner composite name.
   c. On success, obtain pointer to composite `READYTORUN_HEADER` and use it for native method lookup / fixups.
   d. On failure, fall back to IL/JIT path.
4. If the platform-native flag is not set, proceed with existing R2R load logic (PE assembly lookup and load).

### Host callback

The [`host_runtime_contract`](/src/native/corehost/host_runtime_contract.h) will be updated with a new callback for getting native code information.

```c
enum data_type
{
   r2r_header_ptr,
}

// The requested data type determines what is included in the request_context and what is expected in the out data
bool get_native_code_data(
   data_type type,
   const void* request_context,
   /*out*/ void** data,
);
```

This leaves it to the host to do the actual load (for example, `dlopen` of a shared library, using something statically linked into the host itself) of the platform-native image. It is also responsible for any caching desired.
