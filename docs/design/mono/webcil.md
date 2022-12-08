# WebCIL assembly format

## Version

This is version 0 of the Webcil format.

## Motivation

When deploying the .NET runtime to the browser using WebAssembly, we have received some reports from
customers that certain users are unable to use their apps because firewalls and anti-virus software
may prevent browsers from downloading or caching assemblies with a .DLL extension and PE contents.

This document defines a new container format for ECMA-335 assemblies
that uses the `.webcil` extension and uses a new WebCIL container
format.


## Specification

As our starting point we take section II.25.1 "Structure of the
runtime file format" from ECMA-335 6th Edition.

|--------|
| PE Headers |
| CLI Header |
| CLI Data |
| Native Image Sections |


A Webcil file follows a similar structure


|--------|
| Webcil Headers |
| CLI Header |
| CLI Data |

## Webcil Headers

The Webcil headers consist of a Webcil header followed by a sequence of section headers.
(All multi-byte integers are in little endian format).

### Webcil Header

``` c
struct WebcilHeader {
	uint8_t id[2]; // 'W' 'C'
	uint8_t version;
	uint8_t reserved0; // 0
	// 4 bytes
	uint16_t coff_sections;
	uint16_t reserved1; // 0
	// 8 bytes

	uint32_t metadata_rva;
	uint32_t metadata_size;
	// 16 bytes

	uint32_t cli_flags;
	int32_t cli_entry_point;
	// 24 bytes

	uint32_t pe_cli_header_rva;
	uint32_t pe_cli_header_size;
	// 32 bytes
};
```

The Webcil header starts with the magic characters 'W' 'C' followed by the version (must be 0).
Then a reserved byte that must be 0 followed by a count of the section headers and 2 more reserved bytes.

The next 4 integers are a subset of ECMA-335 II.25.3.3 (CLI header) containing the RVA and the size
of the ECMA-335 metadata root, the Flags and EntryPointToken values.  The runtime treats all other
CLI header values as their default values or zero.

**FIXME** why do we need the CLI header fields if we're copying the CLI header anyway?

The last 2 integers are a subset of the PE Header data directory specifying the RVA and size of the CLI header.


### Section header table

Immediately following the Webcil header is a sequence (whose length is given by `coff_sections`
above) of section headers giving their virtual address and virtual size, as well as the offset in the Webcil
file and the size in the file.

``` c
struct SectionTable {
    uint32_t st_virtual_size;
    uint32_t st_virtual_address;
    uint32_t st_raw_data_size;
    uint32_t st_raw_data_ptr;
};
```

### Sections

Immediately following the section table are the sections.  These are copied verbatim from the PE file.

## Rationale

The intention is to include only the information necessary for the runtime to locate the metadata
root, and to resolve the RVA references in the metadata (for locating data declarations and method IL).

A goal is for the files not to be executable by .NET Framework.

Unlike PE files, mixing native and managed code is not a goal.

Lossless conversion from Webcil back to PE is not intended to be supported.  The format is being
documented in order to support diagnostic tooling and utilities such as decompilers, disassemblers,
file identification utilities, dependency analyzers, etc.

