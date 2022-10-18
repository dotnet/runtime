# Changelog

## 0.3.5 (11 Feb 2020)
- Fix a bug with bss sections not being correctly handled when trying to write them to a stream
- Add shadow sections between sections even when no-program headers are in the file.

## 0.3.4 (4 Jan 2020)
- Fix a bug if a program header has an invalid shadow section that does not correspond to a region in the file

## 0.3.3 (24 Dec 2019)
- Fix an invalid error when a program header size is bigger than expected

## 0.3.2 (22 Dec 2019)
- Fix a bug when reading ElfObjectFile from an existing ELF where ObjectFile.FileClass/Encoding/Version/OSABI/AbiVersion was not actually deserialized.

## 0.3.1 (18 Dec 2019)
- Fix creation of DWARF sections from scratch

## 0.3.0 (18 Dec 2019)
- Add support for DWARF Version 4 (missing only .debug_frame)
- Add support for layouting sections independently of the order defined sections header

## 0.2.1 (17 Nov 2019)
- Add verify for PT_LOAD segment align requirements

## 0.2.0 (17 Nov 2019)
- Add support for ElfNoteTable
- Add XML documentation and user manual
- Removed some accessors that should not have been public

## 0.1.0 (16 Nov 2019)
- Initial version with support for ELF file format