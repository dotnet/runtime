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

### Mach-O Emitter Decisions

There's a few cases in the R2R format that are not natively represented in the Mach-O format that have to be emulated. This section will describe some of the design decisions for the Mach-O R2R format.

#### Sections


Data moved out of `__TEXT,__text`:

- Precompiled managed code has been moved into `__TEXT,__managedcode`. `__TEXT,__text` gets special treatment by the linker and `__TEXT,__managedcode` matches NativeAOT.
- Read-only data such as jump tables, CLR metadata, Win32 Resources, managed unwind info, gc info, and the R2R headers are moved to `__TEXT,__const`

Data that stays in the corresponding locations as the PE envelope:

- Read-write data, such as fixup tables: `__DATA,__data`
- Import thunks: `__TEXT,__text`

#### Relocations

Symbol ranges are represented differently in Mach-O than other platforms. Apple linkers have issues when multiple symbols are defined at the same location. Additionally, the Mach format natively supports a "subtractor" reloc to represent the distance between two symbols. As a result, we can represent the start of the symbol range as the start symbol of the range. We can represent the size of the range we can represent as "end symbol location - start symbol location + end symbol size".

#### Base Symbol and RVAs

The R2R format, like the PE format, is heavily based around having RVAs emitted into the image that can be added to the base symbol of the image. The COFF object file format natively supports such a concept, and the PE format uses such a concept in the PE header. However, other formats do not natively support such a concept.

The Apple linker does provide a base symbol for the Mach format, but the base symbol depends on the output type, generally in the form `__mh_<output>_header`. For dylibs, the symbol is `__mh_dylib_header`. This symbol is located at the address returned by `dlinfo` and `dladdr` for the base address. It also points to the Mach header, which can be used to find the size of the image to bound reads of the R2R data.

As a result, we can emulate this support in the Mach format with ease:

1. The base symbol that we use in the object writer will be `__mh_dylib_header`.
2. To emit the distance from the base symbol, we will use a subtractor relocation to represent "symbol location - `__mh_dylib_header` location".

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
struct native_code_context
{
    size_t size;                       // size of this struct
    const char* assembly_path;         // component assembly path
    const char* owner_composite_name;  // name from component R2R header
};

struct native_code_data
{
   size_t size;           // size of this struct
   void* r2r_header_ptr;  // ReadyToRun header
   size_t image_size;     // size of the image
   void* image_base;      // base address where the image was loaded
};

bool get_native_code_data(
   const struct native_code_context* context,
   /*out*/ struct native_code_data* data
);
```

This leaves it to the host to do the actual load (for example, `dlopen` of a shared library, using something statically linked into the host itself) of the platform-native image. It is also responsible for any caching desired.
