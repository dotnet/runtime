# Webcil assembly format

## Version

This described version 0.0, and 1.0 of the Webcil payload format.
This describes versions 0 and 2 of the WebAssembly module Webcil wrapper. Version 1 is retired.

## Motivation

When deploying the .NET runtime to the browser using WebAssembly, we have received some reports from
customers that certain users are unable to use their apps because firewalls and anti-virus software
may prevent browsers from downloading or caching assemblies with a .DLL extension and PE contents.

This document defines a new container format for ECMA-335 assemblies that uses the `.wasm` extension
and uses a new Webcil metadata payload format wrapped in a WebAssembly module.


## Specification

### Webcil WebAssembly module

Webcil consists of a standard [binary WebAssembly version 0 module](https://webassembly.github.io/spec/core/binary/index.html) containing the following WAT module:

``` wat
(module
  (data "\0f\00\00\00") ;; data segment 0: payload size as a 4 byte LE uint32
  (data "webcil Payload\cc")  ;; data segment 1: webcil payload
  (memory (import "webcil" "memory") 1)
  (global (export "webcilVersion") i32 (i32.const 0))
  (func (export "getWebcilSize") (param $destPtr i32) (result)
    local.get $destPtr
    i32.const 0
    i32.const 4
    memory.init 0)
  (func (export "getWebcilPayload") (param $d i32) (param $n i32) (result)
    local.get $d
    i32.const 0
    local.get $n
    memory.init 1))
```

That is, the module imports linear memory 0 and exports:
* a global `i32` `webcilVersion` encoding the version of the WebAssembly wrapper (0 for this form),
* a function `getWebcilSize : i32 -> ()` that writes the size of the Webcil payload to the specified
  address in linear memory as a `u32` (that is: 4 LE bytes).
* a function `getWebcilPayload : i32 i32 -> ()` that writes `$n` bytes of the content of the Webcil
  payload at the spcified address `$d` in linear memory.

The Webcil payload size and payload content are stored in the data section of the WebAssembly module
as passive data segments 0 and 1, respectively.  The module must not contain additional data
segments. The module must store the payload size in data segment 0, and the payload content in data
segment 1.

The payload content in data segment 1 must be aligned on a 4-byte boundary within the web assembly
module.  Additional trailing padding may be added to the data segment 0 content to correctly align
data segment 1's content.

(**Rationale**: With this wrapper it is possible to split the WebAssembly module into a *prefix*
consisting of everything before the data section, the data section, and a *suffix* that consists of
everything after the data section.  The prefix and suffix do not depend on the contents of the
Webcil payload and a tool that generates Webcil files could simply emit the prefix and suffix from
constant data.  The data section is the only variable content between different Webcil-encoded .NET
assemblies)

(**Rationale**: Encoding the payload in the data section in passive data segments with known indices
allows a runtime that does not include a WebAssembly host or a runtime that does not wish to
instantiate the WebAssembly module to extract the payload by traversing the WebAssembly module and
locating the Webcil payload in the data section at segment 1.)

(**Rationale**: The alignment requirement is due to ECMA-335 metadata requiring certain portions of
the physical layout to be 4-byte aligned, for example ECMA-335 Section II.25.4 and II.25.4.5.
Aligning the Webcil content within the wasm module allows tools that directly examine the wasm
module without instantiating it to properly parse the ECMA-335 metadata in the Webcil payload.)

(**Note**: the wrapper may be versioned independently of the payload.)

#### WebAssembly module Webcil wrapper format version 1

Version 1 of the wrapper kept the ReadyToRun payload and function table in passive segments that the
host installed by calling `getWebcilPayload` and `fillWebcilTable`. It is retired and replaced by
version 2; hosts do not load it.

#### WebAssembly module Webcil wrapper format version 2

Version 2 of the WebAssembly module Webcil wrapper is **self-installing**: the engine installs the
payload, and the function table if there is one, at instantiation. CoreCLR uses version 2 for every
Webcil image - IL-only, single-assembly ReadyToRun, composite ReadyToRun, and composite component
forwarding stubs - and a version 1 payload is always wrapped this way. The passive version 0 wrapper
is only produced for version 0 payloads (Mono).

Data segment 0 stays passive and holds the size metadata, so a host can read it before instantiating
the module. If data segment 0 is at least 8 bytes in size and its second 4 bytes are non-zero when
interpreted as a little-endian u32, it encodes two little-endian u32 values: `payloadSize` (first 4
bytes) and `tableSize` (second 4 bytes). Otherwise it encodes only `payloadSize`, and `tableSize` is 0.

The module shall:

* import `memory` and an immutable `i32` global `__memory_base` from the `webcil` module;
* emit the payload as data segment 1, an active segment at `(global.get __memory_base)`;
* export `webcilVersion` with the value 2, and `getWebcilSize`;
* not export `getWebcilPayload` or `fillWebcilTable`. Calling `memory.init` or `table.init` against an
  active segment traps, because an active segment is implicitly dropped once applied.

If `tableSize` is non-zero (a ReadyToRun image), the module shall also import the table as
`__indirect_function_table`, the `__stack_pointer` and `__table_base` globals, and the other runtime
globals and tags its code uses; emit its function table as an active element segment at
`(global.get __table_base)` containing `tableSize` entries; and export `patchWebcilHeader`, which fills
in the `TableBase` field of the installed `WebcilHeader`.

A host loads a version 2 module by reserving a 16-byte-aligned range of `payloadSize` bytes in linear
memory and `tableSize` table slots, instantiating the module with `__memory_base` and `__table_base`
set to the start of those ranges, and then, if `tableSize` is non-zero, calling
`patchWebcilHeader(__memory_base, payloadSize)`. The host must reject any other `webcilVersion`.

``` wat
;; IL-only image.
(module
  (import "webcil" "memory" (memory (;0;) 1))
  (import "webcil" "__memory_base" (global (;0;) i32))
  (global (export "webcilVersion") i32 (i32.const 2))
  (func (export "getWebcilSize") (param $destPtr i32) (result)
    local.get $destPtr
    i32.const 0
    i32.const 4
    memory.init 0)
  (data "\0f\00\00\00") ;; data segment 0: payloadSize - passive, read before instantiation
  (data (global.get 0) "webcil Payload\cc")) ;; data segment 1: Webcil payload, active at __memory_base
```

``` wat
;; ReadyToRun image. The engine applies both segments at instantiation, so only the header's
;; TableBase field is left for the host to trigger.
(module
  (data "\0f\00\00\00\01\00\00\00") ;; data segment 0: payloadSize, tableSize - passive, read before instantiation
  (data (global.get 1) "webcil Payload\cc")  ;; data segment 1: Webcil payload, active at __memory_base
  (import "webcil" "memory" (memory (;0;) 1))
  (import "webcil" "__stack_pointer" (global (;0;) (mut i32)))
  (import "webcil" "__memory_base" (global (;1;) i32))
  (import "webcil" "__table_base" (global (;2;) i32))
  (import "webcil" "__indirect_function_table" (table (;0;) 1 funcref))
  (global (export "webcilVersion") i32 (i32.const 2))
  (func (export "getWebcilSize") (param $destPtr i32) (result)
    local.get $destPtr
    i32.const 0
    i32.const 4
    memory.init 0)
  (func (export "patchWebcilHeader") (param $d i32) (param $n i32) (result)
    local.get 1
    i32.const 32
    i32.ge_s
    if
     local.get 0
     global.get 2
     i32.store offset=28
    end
    )
  (func (param $d i32) (result i32)
    local.get 0)
  (elem (;0;) (global.get 2) func 2)) ;; active at __table_base
```

The module leaves `__memory_base` and `__table_base` as imports, which a host may satisfy in either of
two ways, with different consequences:

- **Supply them at instantiation**, as immutable `WebAssembly.Global` values. The segment offsets stay
  `global.get` of an *imported* global, which is a valid constant expression, so the module needs no
  further processing. The browser host and corerun do this.
- **Define and export them, then link the module into the host** with a merge tool. Merging internalizes
  the globals, and `global.get` of a *defined* global is not a constant expression outside the GC
  proposal - engines disagree here, so the merged module must have its offsets folded to `i32.const`
  before it is portable. The offline WASI pipeline does this.

Neither approach requires rewriting the segments themselves; only the second requires a fold, and that
fold is not free, because the pass that performs it also propagates globals into function bodies.

A host that reserves the composite's table slice at link time can treat `__table_base` as a constant:
reserving the first N slots leaves the composite at base 1 regardless of its size. `__memory_base` is
the address of the host's payload region and has to be read out of the linked host.

The module shall not export its compiled functions. Exports count towards the engine's
effective-type-size limit, so a module carrying a framework-sized function count becomes unloadable
if each function is exported; the element segment, not the export table, is what makes a function
reachable. Function names shall instead be carried in the `name` custom section, which is ignored by
engines, counts towards no limit, and may be stripped when size matters.

A tool that examines a version 2 module without instantiating it can still locate the payload as data
segment 1, after skipping the active segment's offset expression.

(**Rationale**: The size metadata describes the memory and table ranges that the host must reserve
before instantiation. Active segments initialize those ranges; they do not allocate memory or grow
the table. The browser host uses the sizes from the matching boot configuration to reserve these
ranges before instantiation, allowing both IL-only and R2R images to use `instantiateStreaming`
without first buffering or parsing the module. Corerun reads the sizes from data segment 0 of the
local module.)

WebCIL modules are trusted build artifacts, not isolated code. Their segment layout and size
metadata must agree with the compiler output and, for browser loading, the boot configuration.
The loader does not validate that layout before instantiation; a module importing the runtime's
memory can also access that memory from its code. Applications must deploy matching modules and
boot configuration, retaining the resource integrity hashes used by the browser loader.

(**Rationale**: `patchWebcilHeader` writes the `TableBase` field because the runtime reads it from the
mapped image rather than from a Wasm global, and an unwritten field reads as 0, silently shifting
every function index by `tableBase`. Keeping this one step in the module lets the runtime implement
the relocation scheme in its own code, reducing the volume of code needed in each Webcil file.)

(**Rationale**: Requiring an alignment of 16 bytes allows for both efficient memory usage for loading
images into linear memory, as well as for allowing for efficient storage of 128 bit vector constants
within the binary.)

### Webcil payload

The webcil payload contains the ECMA-335 metadata, IL and resources comprising a .NET assembly.

As our starting point we take section II.25.1 "Structure of the
runtime file format" from ECMA-335 6th Edition.

| |
|--------|
| PE Headers |
| CLI Header |
| CLI Data |
| Native Image Sections |
| |



A Webcil file follows a similar structure


| |
|--------|
| Webcil Headers |
| CLI Header |
| CLI Data |
| |

### Webcil Headers

The Webcil headers consist of a Webcil header followed by a sequence of section headers.
(All multi-byte integers are in little endian format).

#### Webcil Header

``` c
struct WebcilHeader {
    uint8_t Id[4]; // 'W' 'b' 'I' 'L'
    // 4 bytes
    uint16_t VersionMajor; // 0 or 1
    uint16_t VersionMinor; // 0
    // 8 bytes
    uint16_t CoffSections;
    uint16_t Reserved0; // 0 OR WebCilSection of relocation table
    // 12 bytes

    uint32_t PeCliHeaderRva;
    uint32_t PeCliHeaderSize;
    // 20 bytes

    uint32_t PeDebugRva;
    uint32_t PeDebugSize;
    // 28 bytes
};
```

The Webcil header starts with the magic characters 'W' 'b' 'I' 'L' followed by the version in major
minor format (must be 0 and 0).  Then a count of the section headers and two reserved bytes.

The next pairs of integers are a subset of the PE Header data directory specifying the RVA and size
of the CLI header, as well as the directory entry for the PE debug directory.

#### Webcil Header (V1.0 Changes)
For Webcil V1, the Reserved0 field may be used to store a 1-based index which corresponds to a
base reloc section.
```
    uint16_t Reserved0; // 0, or 1-based index of .reloc webcil section
```

The header structure has an additional `uint32_t` field called TableBase. The payload is installed
by the version 2 wrapper's active data segment at `__memory_base`; for a ReadyToRun image, the host
must call `patchWebcilHeader` after instantiation to write the `__table_base` value into this field
before the runtime consumes the image.

#### Section header table

Immediately following the Webcil header is a sequence (whose length is given by `CoffSections`
above) of section headers giving their virtual address and virtual size, as well as the offset in
the Webcil payload and the size in the file.  This is a subset of the PE section header that includes
enough information to correctly interpret the RVAs from the webcil header and from the .NET
metadata. Other information (such as the section names) are not included.

``` c
struct SectionHeader {
    uint32_t VirtualSize;
    uint32_t VirtualAddress;
    uint32_t SizeOfRawData;
    uint32_t PointerToRawData;
};
```

(**Note**: the `PointerToRawData` member is an offset from the beginning of the Webcil payload, not from the beginning of the WebAssembly wrapper module.)

#### Sections

The section data starts at the first 16-byte-aligned offset after the end of the
section header table. Any gap between the last section header and the first section's
raw data is filled with zero-valued padding bytes. Each subsequent section likewise
begins at a 16-byte-aligned offset. This alignment guarantees that RVA static fields
(such as those backing `ReadOnlySpan<T>` over types up to `Vector128<T>`) retain
their natural alignment when the payload is loaded into memory at a 16-byte-aligned
base address.

Because PE `SizeOfRawData` is normally a multiple of the PE `FileAlignment` (≥ 512),
the inter-section padding is almost always zero bytes. In the worst case a single
assembly may gain up to ~30 bytes of padding total (header-to-first-section plus
one boundary per additional section).

### Rationale

The intention is to include only the information necessary for the runtime to locate the metadata
root, and to resolve the RVA references in the metadata (for locating data declarations and method IL).

A goal is for the files not to be executable by .NET Framework.

Unlike PE files, mixing native and managed code is not a goal.

Lossless conversion from Webcil back to PE is not intended to be supported.  The format is being
documented in order to support diagnostic tooling and utilities such as decompilers, disassemblers,
file identification utilities, dependency analyzers, etc.

### Special sections

#### Webcil V1 (Base Relocations)
It is possible to specify base relocations in the standard PE base relocation format in Webcil V1.
Valid relocation types
| Relocation type | Value | Supported Wasm bitness | Purpose |
| --- | --- | --- | --- |
| IMAGE_REL_BASED_DIR64 | 10 | 64 bit only | Representing a pointer value of the loaded image in a 64 bit WebAssembly Memory |
| IMAGE_REL_BASED_HIGHLOW | 3 | 32 bit only | Representing a pointer value of the loaded image in a 32 bit WebAssembly Memory |
| IMAGE_REL_BASED_WASM32_TABLE | 12 | All | Representing a "function pointer" for 32 bits, and the minimal size for a function pointer on 64 bit webassembly (table sizes are limited to 32bits of entries even on 64 bit WebAssembly scenarios) |
| IMAGE_REL_BASED_WASM64_TABLE | 13 | All | Representing a "function pointer" for 64 bits scenarios, but available on 32bit platform since its not impractical to implement. |
| IMAGE_REL_BASED_ABSOLUTE | 0 | All | Used to put padding into the relocation block. |

`IMAGE_REL_BASED_WASM{32, 64}_TABLE` relocations represent a "table base offset" fixup; They should be used to indicate places
where function pointer table indices need to be offset after the Webcil payload has been loaded by the runtime. The offset will
be dependent on the state of the table when an implementation's loader loads a Webcil module.

The phyical layout of the section will be series of blocks. Each block must be is 4 byte aligned

``` c
struct IMAGE_BASE_RELOCATION {
    uint32_t VirtualAddress;;
    uint32_t SizeOfBlock;
};
```

| Field name | Meaning |
| --- | --- |
| `VirtualAddress` | RVA into the loaded webcil image |
| `SizeOfBlock` | Size of the block. This includes the size of the `IMAGE_BASE_RELOCATION` structure. |

Each 2 byte word following `IMAGE_BASE_RELOCATION` is decoded as a `uint16_t` where then lower 12 bits
indicate an offset from the `VirtualAddress` of the block, and the high 4 bits represents the relocation type.

## ReadyToRun perfmap offsets

When crossgen2 compiles an assembly to a ReadyToRun Webcil `.wasm` module (`--obj-format:wasm`) with
`--perfmap --perfmap-format-version:1`, it emits a sidecar `<assembly>.ni.r2rmap` text file for native
symbolication — the same artifact produced for PE/ELF/Mach-O R2R images. Only a small debug-directory
entry pointing at the sidecar is embedded in the module; the method table itself is never part of the
`.wasm` file.

Unlike the ECMA-335 metadata, IL and RVA-addressed data — which live in the Webcil *payload* (data
segment 1) — the R2R-compiled method bodies are emitted as **real WebAssembly functions in the module's
Code section**. The perfmap therefore keys each method by its position in that Code section rather than by
a payload RVA.

### Address space

Every method entry in the perfmap is a line of the form `RVA Length Name`, where:

* `RVA` is the byte offset, **from the start of the `.wasm` module**, of the method's function entry in
  the Code section (that is, the entry's LEB128 body-size prefix). It is a file offset into the wasm
  module — not a virtual address and not a Webcil payload RVA.
* `Length` is the size in bytes of that function entry (size prefix plus body).

Code entries are laid out contiguously, so a method's `RVA + Length` is the `RVA` of the next entry in the
Code section. The header pseudo-entries carry the target identity: OS token `8` (`Browser`) or `9`
(`Wasi`), architecture token `7` (`Wasm`), and perfmap format version `1`.

### Computing the offsets

The Code section is rewritten during final module emission: the object writer LEB128-shrinks each function
body's relocations to their minimal encoding, which changes both the entry's size prefix and the position
of every function that follows it. The offset captured when a method is first written is therefore a
*pre-shrink* content offset and does not match the final module layout.

To produce correct addresses the writer:

1. records the file offset at which the Code section's function-body content begins in the final module
   (after the section id byte, the section size prefix, and the function-count prefix), and
2. remaps each method's pre-shrink entry offset — and the offset of its end — to the corresponding
   post-shrink offset, so the reported `RVA` and `Length` reflect the shrunk on-disk layout.

Adding the recorded Code-section file offset to a method's post-shrink entry offset yields the `RVA` written
to the perfmap.

(**Rationale**: keying on the wasm Code-section file offset lets native profilers and tools such as
`dotnet-trace` and PerfView correlate the offsets a WebAssembly engine reports for executing frames with
managed method names — the same role the perfmap plays for native R2R code on other platforms. A payload
RVA would instead point into the metadata image, which is not what executes.)
