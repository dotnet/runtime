The dataflow analysis logic originates from IL Linker: https://github.com/dotnet/runtime/tree/main/src/tools/illink.

The purpose of this logic is to analyze dynamic behavior of the compiled code to make things like reflection work. This is done by analyzing the IL and reading dataflow annotations.

Let's try to keep this in sync. The ReferenceSource contains sources at the time of porting.

It should be updated whenever we take fixes from IL linker.

Standard updates when taking files from IL Linker:
* Note: These rules apply only to the DataFlow directory, the files which we share verbatime should remain exactly as-is in IL Linker for now
* Use the runtime's version of the license header in the file (starts with `Licensed to the .NET Foundation`)
* Use the formatting from runtime repo - in VS this can be done by reformatting the whole document: Edit -> Advanced -> Format whole document.
* Add `#nullable enable` since nullable is not globally enabled in ILC yet

