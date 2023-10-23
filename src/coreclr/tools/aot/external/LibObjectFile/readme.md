# LibObjectFile [![Build Status](https://github.com/xoofx/LibObjectFile/workflows/ci/badge.svg?branch=master)](https://github.com/xoofx/LibObjectFile/actions) [![NuGet](https://img.shields.io/nuget/v/LibObjectFile.svg)](https://www.nuget.org/packages/LibObjectFile/)

<img align="right" width="200px" height="200px" src="https://raw.githubusercontent.com/xoofx/LibObjectFile/master/img/libobjectfile.png">

LibObjectFile is a .NET library to read, manipulate and write linker and executable object files (e.g ELF, ar, DWARF, COFF...)

> NOTE: Currently LibObjectFile supports only the following file format:
>
> - **ELF** object-file format
> - **DWARF** debugging format (version 4)
> - **Archive `ar`** file format (Common, GNU and BSD variants)
>
> There is a longer term plan to support other file formats (e.g COFF, MACH-O, .lib) but as I don't 
> have a need for them right now, it is left as an exercise for PR contributors! ;)

## Usage

```C#
// Reads an ELF file
using var inStream = File.OpenRead("helloworld");
var elf = ElfObjectFile.Read(inStream);
foreach(var section in elf.Sections)
{
    Console.WriteLine(section.Name);
}
// Print the content of the ELF as readelf output
elf.Print(Console.Out);
// Write the ElfObjectFile to another file on the disk
using var outStream = File.OpenWrite("helloworld2");
elf.Write(outStream);
```

## Features
- Full support of Archive `ar` file format including Common, GNU and BSD variants.
- Good support for the ELF file format:
  - Read and write from/to a `System.IO.Stream`
  - Handling of LSB/MSB
  - Support the following sections: 
    - String Table
    - Symbol Table
    - Relocation Table: supported I386, X86_64, ARM and AARCH64 relocations (others can be exposed by adding some mappings)
    - Note Table
    - Other sections fallback to `ElfCustomSection`
  - Program headers with or without sections
  - Print with `readelf` similar output
- Support for DWARF debugging format:
  - Partial support of Version 4 (currently still the default for GCC)
  - Support for the sections: `.debug_info`, `.debug_line`, `.debug_aranges`, `.debug_abbrev` and `.debug_str` 
  - Support for Dwarf expressions
  - High level interface, automatic layout/offsets between sections.
  - Integration with ELF to support easy reading/writing back
  - Support for relocatable sections
- Use of a Diagnostics API to validate file format (on read/before write)
- Library requiring `net7.0`
    - If you are looking for `netstandard2.1` support you will need to use `0.4.0` version

## Documentation

The [doc/readme.md](doc/readme.md) explains how the library is designed and can be used.

## Known Issues

PR Welcome if you are willing to contribute to one of these issues:

- [ ] Add more unit tests

### ELF
There are still a few missing implementation of `ElfSection` for completeness:

- [ ] Dynamic Linking Table (`SHT_DYNAMIC`)
- [ ] Version Symbol Table (`SHT_VERSYM`)
- [ ] Version Needs Table (`SHT_VERNEED`)

These sections are currently loaded as `ElfCustomSection` but should have their own ElfXXXTable (e.g `ElfDynamicLinkingTable`, `ElfVersionSymbolTable`...)

### DWARF

- [ ] Version 4: support for `.debug_types`, `.debug_frame`, `.debug_loc`, `.debug_ranges`, `.debug_pubnames`, `.debug_pubtypes`, `.debug_macinfo` section
- [ ] Version 5

### Other file formats
In a future version I would like to implement the following file format:

- [ ] COFF
- [ ] Mach-O
- [ ] Portable in Memory file format to easily convert between ELF/COFF/Mach-O file formats.

## Download

LibObjectFile is available as a NuGet package: [![NuGet](https://img.shields.io/nuget/v/LibObjectFile.svg)](https://www.nuget.org/packages/LibObjectFile/)

## Build

In order to build LibObjectFile, you need to have installed the [.NET Core 3.0 SDK](https://www.microsoft.com/net/core).

Running the tests require Ubuntu 18.04. `dotnet test` will work on Windows (via WSL) and on that version of Ubuntu.
If you're using macOS or another Linux, there's a Dockerfile and a helper script under `src` to run tests in the right OS version.

## License

This software is released under the [BSD-Clause 2 license](https://github.com/xoofx/LibObjectFile/blob/master/license.txt).

## Author

Alexandre MUTEL aka [xoofx](http://xoofx.com)

## Supporters

Supports this project with a monthly donation and help me continue improving it. \[[Become a supporter](https://github.com/sponsors/xoofx)\]

[<img src="https://github.com/bruno-garcia.png?size=200" width="64px;" style="border-radius: 50%" alt="bruno-garcia"/>](https://github.com/bruno-garcia)