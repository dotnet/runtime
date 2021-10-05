# wa-info
`wa-info` is a tool to inspect [WebAssembly](https://webassembly.org/) [binary format](https://webassembly.github.io/spec/core/binary/index.html) wasm files.

It can show sections overview in the summary output, disassemble function(s) code, show information about dotnet AOT. It is available as [dotnet tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).

### Installation

To install `wa-info` as dotnet tool run:
```
dotnet tool install -g wa-info
```

### Command line options

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
  -t, --type-filter=REGEX    Filter types and process only those matching REGEX
  -v, --verbose              Output information about progress during the run
                               of the tool
```

### Example usage
Summary of the module and sections
```
> wa-info.exe dotnet.wasm
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
> wa-info.exe -d -f ves_icall_RuntimeFieldInfo_GetValueInternal$ dotnet.wasm
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
> wa-info.exe --aot-stats dotnet.wasm
AOT stats: 5005 function(s) call(s) interpreter, 2.96% of 169215 functions
```
