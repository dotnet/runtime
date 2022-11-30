# wa-info
`wa-info` is a tool to inspect [WebAssembly](https://webassembly.org/) [binary format](https://webassembly.github.io/spec/core/binary/index.html) wasm files.

It can show sections overview in the summary output, disassemble function(s) code, show information about dotnet AOT. It is available as [dotnet tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).

# wa-diff

`wa-diff` is a tool to compare WebAssembly binary files

# wa-edit

`wa-edit` is a tool to modify WebAssembly binary files

### Installation

To install `wa-info` and `wa-diff` as dotnet tool run:
```
dotnet tool install -g wa-info
dotnet tool install -g wa-diff
```
to update use:
```
dotnet tool update -g wa-info
dotnet tool update -g wa-diff
```

### Command line options

wa-info:
```
Usage: wa-info.exe OPTIONS* file.wasm [file2.wasm ...]

Provides information about WebAssembly file(s)

Copyright 2021 Microsoft Corporation

Options:
      --aot-stats            Show stats about methods
  -a, --assembly-filter=REGEX
                             Filter assemblies and process only those matching
                               REGEX
  -d, --disassemble          Show functions(s) disassembled code
  -f, --function-filter=REGEX
                             Filter wasm functions REGEX
  -h, --help, -?             Show this message and exit
  -o, --instruction-offsets  Show instruction offsets
  -t, --type-filter=REGEX    Filter types and process only those matching REGEX
  -v, --verbose              Output information about progress during the run
                               of the tool
```

wa-diff:
```
Usage: wa-diff OPTIONS* file1.wasm file2.wasm

Compares WebAssembly binary file(s)

Copyright 2021 Microsoft Corporation

Options:
  -d, --disassemble          Show functions(s) disassembled code
  -f, --function-filter=REGEX
                             Filter wasm functions REGEX
  -v, --verbose              Output information about progress during the run
                               of the tool
```

wa-edit:
```
Usage: wa-edit OPTIONS* source.wasm destination.wasm

Modifies WebAssembly file (source.wasm) and writes updated file (destination.
wasm)

Copyright 2022 Microsoft Corporation

Options:
  -a, --data-auto-split      Split the data segment to avoid long empty chunks
                               with zeroes
  -d, --data-section=FILE    Replace the data section with content of the FILE
  -m, --data-section-mode=MODE
                             Set the data section replacement MODE. Possible
                               values: Active, Passive
  -o, --data-offset=VALUE    Data section offset
  -h, --help, -?             Show this message and exit
  -v, --verbose              Output information about progress during the run
                               of the tool
```

### Example usage
Disassemble function
```
> wa-info -d -f test test.wasm
(func test_simd(param i32) (result i32))
 local 1 123
 local.get $0
 i32x4.splat    [SIMD]
 local.tee $1
 local.get $1
 i16x8.avgr.u    [SIMD]
 i32x4.extract.lane    [SIMD]
```

Summary of the module and sections
```
> wa-info dotnet.wasm
Module: path: dotnet.wasm
  size: 74,502,446
  binary format version: 1
  sections: 17
    id: Type size: 5,237
    id: Import size: 4,315
    id: Function size: 163,566
    id: Table size: 9
    id: Memory size: 7
    id: Global size: 19
    id: Export size: 7,213
    id: Element size: 428,929
    id: Code size: 34,177,740
    id: Data size: 9,520,741
    id: Custom name: name size: 21,544,785
    id: Custom name: .debug_info size: 4,035,679
    id: Custom name: .debug_loc size: 1,436,083
    id: Custom name: .debug_ranges size: 140,966
    id: Custom name: .debug_abbrev size: 240,230
    id: Custom name: .debug_line size: 2,060,872
    id: Custom name: .debug_str size: 735,984
```

Disassemble function

*note: build the app with `-p:WasmNativeDebugSymbols=true -p:WasmNativeStrip=false` to preserve name custom section*
```
> wa-info -d -f ves_icall_RuntimeFieldInfo_GetValueInternal$ dotnet.wasm
(func ves_icall_RuntimeFieldInfo_GetValueInternal(param i32 i32 i32) (result i32))
 local.get $0
 i32.load align:2
 i32.load offset:12 align:2
 local.tee $0
 local.get $1
 if
  local.get $1
  i32.load align:2
 else
  i32.const 0

 local.tee $1
 local.get $2
 call mono_field_get_value_object_checked
 i32.const 11201820
 i32.load align:2
 call mono_handle_new
```

Get AOT stats
```
> wa-info --aot-stats dotnet.wasm
AOT stats: 5005 function(s) call(s) interpreter, 2.96% of 169215 functions
```

Compare functions
```
> wa-diff -d -f corlib_System_RuntimeType_IsDelegate dotnet1.wasm dotnet2.wasm
(func corlib_System_RuntimeType_IsDelegate(param i32 i32) (result i32))
...
   local.tee $2
   global.set $__stack_pointer
-  i32.const 1573600
+  i32.const 1573760
   i32.load8.u
   i32.eqz
...
    i32.const 25177
    call aot_wrapper_pinvoke_corlib__Interop_sl_Sys___le_PosixFAdvise_gt_g____PInvoke___verbar_83_0_pinvoke_i4_iii8i8cl1a_Interop_2fSys_2fFileAdvice_i4_iii8i8cl1a_Interop_2fSys_2fFileAdvice_
-   i32.const 1573600
+   i32.const 1573760
    i32.const 1
    i32.store8
...
   i32.store offset:12 align:2
   local.get $2
-  i32.const 1516792
+  i32.const 1516952
   i32.load align:2
   local.tee $3
```

Replace Data section
```
wa-edit -v dotnet.wasm d4.wasm -a -o 0 -d memory.dat
Reading wasm file: dotnet.wasm
Writing wasm file: d4.wasm
WebAssembly binary format version: 1
Reading section:      Type size:         1632
Reading section:    Import size:         3104
Reading section:  Function size:        17745
Reading section:     Table size:            5
Reading section:    Memory size:            7
Reading section:    Global size:         2394
Reading section:    Export size:         4227
Reading section:   Element size:         8702
Reading section:      Code size:      6282132
Reading section:      Data size:       649676    segments detected: 29,539 zero bytes stripped: 48,137,494
Reading section:    Custom size:       880619
Reading section:    Custom size:      3388054
Reading section:    Custom size:       360611
Reading section:    Custom size:       134358
Reading section:    Custom size:       211464
Reading section:    Custom size:      3645378
Reading section:    Custom size:       706701
```
