# Webcil assembly format

## Version

This described version 0.0, and 1.0 of the Webcil payload format.
This describes version 0 and 1 of the WebAssembly module Webcil wrapper.

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
* a global `i32` `webcilVersion` encoding the version of the WebAssembly wrapper (currently 0),
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
Version 1 of the WebAssembly module Webcil wrapper adds an additional capability and requirements.
If data segment 0 is at least 8 bytes in size, and the second 4 bytes has a non-zero value when interpreted as a 4-byte
little-endian unsigned 32-bit integer, then data segment 0 encodes two little-endian
u32 values: `payloadSize` (first 4 bytes) and `tableSize` (second 4 bytes). In this case,
`tableSize` shall be the number of table entries required for the WebAssembly
module to be loaded, and the module shall import a table, as well as `stackPointer`, `tableBase`, and
`imageBase` globals. There shall also be a `fillWebcilTable` function which will initialize the table
with appropriate values. The `getWebcilPayload` API shall be enhanced to fill in the `TableBase` field
of the `WebcilHeader`.

The memory of the WebcilPayload must also be allocated with 16 byte alignment.

``` wat
(module
  (data "\0f\00\00\00\01\00\00\00") ;; data segment 0: two little-endian u32 values (payloadSize, tableSize). This specifies a Webcil payload of size 15 bytes with 1 required table entry
  (data "webcil Payload\cc")  ;; data segment 1: Webcil payload
  (import "webcil" "memory" (memory (;0;) 1))
  (import "webcil" "stackPointer" (global (;0;) (mut i32)))
  (import "webcil" "imageBase" (global (;1;) i32))
  (import "webcil" "tableBase" (global (;2;) i32))
  (import "webcil" "table" (table (;0;) 1 funcref))
  (global (export "webcilVersion") i32 (i32.const 1))
  (func (export "getWebcilSize") (param $destPtr i32) (result)
    local.get $destPtr
    i32.const 0
    i32.const 4
    memory.init 0)
  (func (export "getWebcilPayload") (param $d i32) (param $n i32) (result)
  ;; Copy from the passive data segment
    local.get $d
    i32.const 0
    local.get $n
    memory.init 1
  ;; Set the table base, if the amount of data to write is large enough
    local.get 1
    i32.const 32 ;; the amount of bytes required so that the write below does not overflow the size specified
    i32.ge_s
    if
     local.get 0
     global.get 2 ;; get the tableBase from the global assigned during instantiate
     i32.store offset=28
    end
    )
  (func (export "fillWebcilTable") (result)
    global.get 2 ;; function pointers to fill in start at tableBase
    i32.const 0
    i32.const 1 ;; There is 1 element in elem segment 0
    table.init 0 0)
  (func (param $d i32) (result i32) ;; Example of function to be injected into "table"
    local.get 0)
  (elem (;0;) func 3))
```

(**Rationale**: With this approach it is possible to identify without loading the webcil module
exactly the allocations/table growth/globals which are needed to load the webcil module via
instantiateStreaming without actually loading the module.)

(**Rationale**: Using a new function called fillWebcilTable to fill in the table enables future
multithreading logic which may require instantiating the table in multiple workers, without
recopying the memory from the webassembly segment into the memory space.)

(**Rationale**: The getWebcilPayload api filling in the TableBase field of the WebcilHeader allows
the runtime to put a more complex implementation of the relocations scheme into the code which is part
of the runtime's wasm code, reducing the volume of code needed in each webcil file.)

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

The header structure has an additional `uint32_t` field called TableBase which is filled in with the
value of the tableBase global value during execution of `getWebcilPayload`.

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
