# Reference opcode generator

Tool to generate the numbering of the reference opcodes for human readability.

Usage:

```sh
cd runtime
cat src/coreclr/inc/opcode.def | ./dotnet.sh run --project src/coreclr/inc/ReferenceOpcodesGenerator
```
