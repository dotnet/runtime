# Grammar extractor for IL tools

Tool to extract IL grammar in `Backus-Naur Form (BNF)` from `Yet Another Compiler-Compiler (Yacc)`.

Usage:

```sh
cd runtime
./dotnet.sh run --project src/coreclr/ilasm/GrammarExtractor src/coreclr/ilasm/asmparse.y > src/coreclr/ilasm/prebuilt/asmparse.grammar
```
