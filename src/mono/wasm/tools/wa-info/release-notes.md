### Release version 0.3

Changes:

 * parse WitExportSection
 * parse WitImportSection
 * new options to print WIT exports/imports
 * removed forgotten debug print

### Release version 0.2

Changes:

 * initial support for wit components
 * improved import description parsing

### Release version 0.1

Changes:

 * let the tools run on newer .NET versions (Aleksey)
 * load function names from .js.symbol files
 * fix I32 write
 * updated README's usage and added more examples

wa-diff
 * new h|hide-const-loads option to improve readability
 * new s|function-size option
 * print function code sizes comparison table for -s option without -d
 * improved and more complete diff of functions
 * fix error in function indexes use

### Release version 0.0.13

Changes:

* add new wa-edit tool
* allow replacing the Data section
* added data segment auto-split feature

wa-info and wa-diff
 * parse Memory section

wa-diff
 * fix error in function comparison

### Release version 0.0.12

Changes:

wa-info and wa-diff
 * parse Data section
 * parse Global section
 * fix error in function offsets calculation

### Release version 0.0.11

Changes:

wa-info and wa-diff
 * removed debug print

### Release version 0.0.10

Changes:

wa-info and wa-diff
 * add support for threads instructions
 * add support for prefix instructions
 * fix few missing instructions reading
 * fix -d without function filter or offset set, to print all functions again

### Release version 0.0.9

Changes:

wa-info and wa-diff
 * fix SIMD instructions disassembly
 * improve disassembly readability, display parameters and local variable names
 * handle vector type in value types

### Release version 0.0.8

Changes:

wa-diff
 * fix method lookup and comparison

wa-info
 * fix call instruction disassembly

### Release version 0.0.7

Changes:

 * new wa-diff tool to compare .wasm files

### Release version 0.0.6

Changes:

 * improve SIMD support
 * add exception handling support - instructions

### Release version 0.0.5

Changes:

 * add SIMD support
 * read table and element sections

### Release version 0.0.4

Changes:

 * new -o option to display instructions offsets

### Release version 0.0.3

Changes:

 * improve nuget package

### Release version 0.0.2

Changes:

 * faster disassembly with -f
 * improved disassembly output for call.indirect
 * print custom section names in the summary

### Release version 0.0.1

Initial release
