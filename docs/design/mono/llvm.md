# LLVM Support in Mono

## Introduction

The default mono code generator is a single tier JIT and thus it can't generate highly optimized machine code.
To solve this problem, mono has support for emitting code using [LLVM](www.llvm.org).

## Configurations

LLVM can be used in multiple configurations in mono.

### JIT

In this mode, LLVM is used as a traditional JIT. First the JIT front end is used to generate LLVM
bitcode, then the bitcode is compiled to native code using the LLVM JIT APIs. This is enabled by the
`--llvm` command line option. Note that startup in this mode is pretty slow, so this is mostly useful for
server side/perf sensitive applications.

### AOT

In this mode, the mono AOT compiler uses LLVM to compile IL code. For methods not supported by LLVM, it fails back
to the JIT compiler. This mode is enabled by the `llvm` AOT option, i.e. `--aot=llvm` or by the normal `--llvm`
command line option. The AOT compiler emits a LLVM bitcode (.bc) file and optionally compiles it to native code
by invoking the LLVM command line tools (`opt`/`llc`).

### LLVMOnly

This mode is designed to target environments without runtime code generation/inline assembly. It is enabled by the
`llvmonly` AOT option, i.e. `--aot=llvmonly`. The generated .bc file is compiled using stock `clang`.

## The Mono LLVM fork

Mono uses a fork of LLVM with a limited set of changes. The fork is available at `https://github.com/dotnet/llvm-project`.
The mono changes are kept rebased on top of the corresponding upstream release branch, i.e. the `release/11.x` branch
in the fork contains the mono changes on top of the upstream `release/11.x` branch.

Some of the mono changes include:
* Some calling convention extensions to allow passing arguments in non-ABI registers like in `x11` on `x86-64`. This is
used by the runtime to implement various features like generic sharing.
* Emission of exception handling tables. These tables are needed by the mono EH code to process LLVM frames during
exception handling.
* Integration into the dotnet build system.

The mono runtime interacts with the fork in 2 ways:
* Some of the LLVM libraries are linked into the runtime.
* The `opt`/`llc` tools are used to compile .bc files to native code.

## Source code structure

Since the mono runtime is written in C, the parts written in C++ are kept in separate files and accessed through a
C API.

LLVM support is enabled in the runtime by setting the `LLVM_PREFIX` cmake variable to the root of the compiled LLVM
tree, i.e. the directory which contains `bin`/`lib` etc.

* `mini-llvm.c`: Contains the majority of the llvm backend code. This file uses the LLVM C API to generate bitcode.
* `mini-llvm-cpp.cpp`: Contains helper functions missing from the LLVM C API.
* `llvm-runtime.cpp`: Contains cpp functions used at runtime in llvmonly mode.
* `llvm-jit.cpp`: Contains the JIT code which compiles the bitcode emitted by the LLVM backend into the final native code.

## Compilation process

* The .net IL is compiled to the same internal IR used by the mono JIT, with slight differences.
* A set of optimization passes is ran including conversion to SSA form.
* The LLVM backend converts the internal IR to LLVM bitcode.
* The bicode is either saved to a .bc file (for AOT) or compiled to native code (for JIT).

## Code generation issues

### Null checks

In .net, loads/stores from a null address are converted to a null reference exception. To achieve this with LLVM,
explicit null checks are emitted, and the `implicit-null-checks` LLVM pass is used to fold the checks into loads/stores.

### Passing arguments in non-ABI registers

A new `mono` calling convention is added. In this calling convention, one argument can be marked with the `inreg` attribute.
This argument will be passed in a platform specific non-abi register like `x11` on `x86-64` or `r15` on `arm64`.

### Exception handling

Mono implements its own unwinding/exception handling system. In LLVM code, exception handling clauses are implemented using the
standard LLVM EH facilities like landing pads, invokes, etc. `llc` is modified to emit an exception handling table.
This table contains the following:
* A lookup table mapping addresses to a mono specific id, which is used by runtime to lookup the actual IL method corresponding
to an LLVM function.
* Dwarf unwind info for every LLVM function.
* For methods which have EH clauses, the try-catch-finally offsets inside the generated code.
* For shared methods, information on where to find the saved `this` pointer on the stack. This is used to construct the
actual generic instance method i.e. `List<T>.Add` + `this=List<int>` -> `List<int>.Add` for stack traces etc.
