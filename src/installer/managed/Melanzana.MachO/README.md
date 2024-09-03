# Mach-O manipulation library

The `Melanzana.MachO` library implements object model for manipulation of Mach-O files. It allows reading files from disk into the in-memory object model, modifying it and then writing it back to disk. Additionally, universal binaries targeting more architectures can be read and written.

## Status

- `MachObjectFile` implements the in-memory representation of Mach-O file; it allows access to header fields and load commands (including sections and segments)
- Universal binaries and Mach-O files can be read using `MachReader.Read` method into the in-memory object model
- Universal binaries and Mach-O files can be written to disk using `MachWriter.Write` method from the in-memory object model
- Segments (`MachSegment`) and sections (`MachSection`) can be manipulated and content can be rewritten, the model is unified for 32-/64-bit Mach-O files

Minimal set of load commands is mapped to the strongly typed object model (eg. dynamicly linked dependencies, required platform versions).

Validation is completely missing.

## Acknowledgments

Many design decisions in this library were inspired by [LibObjectFile](https://github.com/xoofx/LibObjectFile), [LIEF project](https://github.com/lief-project/LIEF) and [go-macho](https://github.com/blacktop/go-macho).
