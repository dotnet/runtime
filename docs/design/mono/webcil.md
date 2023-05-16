# WebCIL assembly format

## Version

This is version 0.0 of the Webcil payload format.
This is version 0 of the WebAssembly module Webcil wrapper.

## Motivation

When deploying the .NET runtime to the browser using WebAssembly, we have received some reports from
customers that certain users are unable to use their apps because firewalls and anti-virus software
may prevent browsers from downloading or caching assemblies with a .DLL extension and PE contents.

This document defines a new container format for ECMA-335 assemblies that uses the `.wasm` extension
and uses a new WebCIL metadata payload format wrapped in a WebAssembly module.


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
	uint8_t id[4]; // 'W' 'b' 'I' 'L'
	// 4 bytes
	uint16_t version_major; // 0
	uint16_t version_minor; // 0
	// 8 bytes
	uint16_t coff_sections;
	uint16_t reserved0; // 0
	// 12 bytes

	uint32_t pe_cli_header_rva;
	uint32_t pe_cli_header_size;
	// 20 bytes

    uint32_t pe_debug_rva;
    uint32_t pe_debug_size;
    // 28 bytes
};
```

The Webcil header starts with the magic characters 'W' 'b' 'I' 'L' followed by the version in major
minor format (must be 0 and 0).  Then a count of the section headers and two reserved bytes.

The next pairs of integers are a subset of the PE Header data directory specifying the RVA and size
of the CLI header, as well as the directory entry for the PE debug directory.


#### Section header table

Immediately following the Webcil header is a sequence (whose length is given by `coff_sections`
above) of section headers giving their virtual address and virtual size, as well as the offset in
the Webcil payload and the size in the file.  This is a subset of the PE section header that includes
enough information to correctly interpret the RVAs from the webcil header and from the .NET
metadata. Other information (such as the section names) are not included.

``` c
struct SectionHeader {
    uint32_t st_virtual_size;
    uint32_t st_virtual_address;
    uint32_t st_raw_data_size;
    uint32_t st_raw_data_ptr;
};
```

(**Note**: the `st_raw_data_ptr` member is an offset from the beginning of the Webcil payload, not from the beginning of the WebAssembly wrapper module.)

#### Sections

Immediately following the section table are the sections.  These are copied verbatim from the PE file.

### Rationale

The intention is to include only the information necessary for the runtime to locate the metadata
root, and to resolve the RVA references in the metadata (for locating data declarations and method IL).

A goal is for the files not to be executable by .NET Framework.

Unlike PE files, mixing native and managed code is not a goal.

Lossless conversion from Webcil back to PE is not intended to be supported.  The format is being
documented in order to support diagnostic tooling and utilities such as decompilers, disassemblers,
file identification utilities, dependency analyzers, etc.

