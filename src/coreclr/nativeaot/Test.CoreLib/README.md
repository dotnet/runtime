# Test.CoreLib

This is a minimum viable core library for test purposes.

## How to use this

Test.CoreLib gets built as part of the repo. After you build the repo:

1. Compile your test program against Test.CoreLib

```
csc /noconfig /nostdlib Program.cs /r:<repo_root>\bin\Product\Windows_NT.x64.Debug\Test.CoreLib\Test.CoreLib.dll /out:repro.exe
```

2. Compile the IL with ILC

Use ilc.dll that was built with the repo to compile the program.

```
ilc repro.exe -o:repro.obj -r:<repo_root>\bin\Product\Windows_NT.x64.Debug\Test.CoreLib\Test.CoreLib.dll --systemmodule Test.CoreLib
```

3. Use native linker to link
