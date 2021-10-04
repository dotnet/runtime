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
  size: 103,347,170
  binary format version: 1
  sections: 17
    id: Type size: 4,836
    id: Import size: 3,810
    id: Function size: 169,878
    id: Table size: 9
    id: Memory size: 7
    id: Global size: 19
    id: Export size: 7,444
    id: Element size: 454,314
    id: Code size: 62,014,120
    id: Data size: 10,905,780
    id: Custom size: 22,098,422
    id: Custom size: 3,578,734
    id: Custom size: 1,241,745
    id: Custom size: 120,526
    id: Custom size: 224,404
    id: Custom size: 1,877,737
    id: Custom size: 645,314
```

Disassemble function
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
