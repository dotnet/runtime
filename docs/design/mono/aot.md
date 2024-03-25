# Ahead of Time Compilation

## Introduction

The mono Ahead of Time (AOT) compiler enables the compilation of the IL code in a .NET assembly to
a native object file. This file is called an AOT image. This AOT image can be used by the runtime to avoid
having to JIT the IL code.

## Usage

The AOT compiler is integrated into the mono runtime executable, and can be run using the `--aot` command
line argument, i.e.
`<mono-executable> --aot HelloWorld.dll`

## Source code structure

- `aot-compiler.c`: The AOT compiler
- `aot-runtime.c`: Code used at runtime to load AOT images
- `image-writer.c`: Support code for emitting textual assembly
- `dwarfwriter.c`: Support code for emitting DWARF debug info

## Configurations

### Desktop AOT

In this mode, the AOT compiler creates a platform shared object file (.so/.dylib), i.e. `HelloWorld.dll.so`. During execution, when
an assembly is loaded, the runtime loads the corresponding shared object and uses it to avoid having to AOT the methods in the
assembly.

Emission of the native code is done by first emitting an assembly (.s) file, then compiling and linking it with the system tools
(`as`/`ld`, or `clang`).

### Static AOT

In this mode, the AOT compiler creates a platform object file (.o). This file needs to be linked into the application and registered
with the runtime.

Static compilation is enabled by using the `static` aot option, i.e. `--aot=static,...`. The resulting object file contains a linking
symbol named `mono_aot_module_<assembly name>_info`. This symbol needs to be passed to the a runtime function before the
runtime is initialized, i.e.:
`mono_aot_register_module (mono_aot_module_HelloWorld_info);`

### Full AOT

In this mode, which can be combined with the other modes, the compiler generates additional code which enables the runtime to
function without any code being generated at runtime. This includes 2 types of code:
- code for 'extra' methods, i.e. generic instances, runtime generated wrappers methods, etc.
- trampolines

This is enabled by using `full` aot option, i.e. `--aot=full,...`. At runtime, all assemblies need to have a full-aot-ed AOT image
present in order for the app to work. This is used on platforms which don't allow runtime code generation like IOS.

### LLVM support

LLVM support can be enabled using the `llvm` aot option, i.e. `--aot=llvm`. In this mode, instead of generating native code,
the AOT compiler generates an LLVM bitcode (.bc), file, then compiles it to native code using the `opt`/`llc` LLVM tools. The
various AOT data structures are also emitted into the .bc file instead of as assembly.
Since the LLVM backend currently doesn't support all .net methods, a smaller assembly file is still emitted, and linked together
with the `opt`/`llc` compiled object file into the final shared object file.

## Versioning

The generated AOT images have a dependency on the exact version input assembly used to generate them and the versions of all the
referenced assemblies. This means the GUIDs of the assemblies have to match. If there is a mismatch, the AOT image will fail to load.

## File structure

The AOT images exports one symbol named `mono_aot_module_<assembly name>_info` which points to a `MonoAotFileInfo` structure,
which contains pointers to the tables/structures. The AOT image contains:
- the native code
- data structures required to load the code
- cached data intended to speed up runtime operation

The AOT image contains serialized versions of many .NET objects like methods/types etc. This uses ad-hoc binary encodings.

## Runtime support

The `aot-runtime.c` file contains the runtime support for loading AOT images.

### Loading AOT images

When an assembly is loaded, the corresponding AOT images is either loaded using the system dynamic linker (`dlopen`), or
found among the statically linked AOT images.

### Loading methods

Every method in the AOT image is assigned an index. The AOT methods corresponding to 'normal' .NET methods are assigned
an index corresponding to their metadata token index, while the 'extra' methods are assigned subsequent indexes. There is
a hash table inside the AOT image mapping extra methods to their AOT indexes. Loading a method consists of
- finding its method index
- finding the method code/data corresponding to the method index

The mapping from method index to the code is done in an architecture specific way, designed to minimize the amount of
runtime relocations in the AOT image. In some cases, this involves generating an extra table with assembly call instructions to
all the methods, then disassembling this table at runtime.



### Runtime constants

The generated code needs to access data which is only available at runtime. For example, for an `ldstr "Hello"` instruction, the
`"Hello"` string is a runtime constant.

These constants are stored in a global table called the GOT which is modelled after the Global Offset Table in ELF images. The GOT
table contains pointers to runtime objects. The AOT image contains descriptions of these runtime objects so the AOT runtime can
compute them. The entries in the GOT are initialized either when the AOT image is loaded (for frequently used entries), or before
the method which uses them is first executed.

### Initializing methods

Before an AOTed method can be executed, it might need some initialization. This involves:
- executing its class cctor
- initializing the GOT slots used by the method

For methods compiled by the mono JIT, initialization is done when the method is loaded. This means that its not possible to
have direct calls between methods. Instead, calls between methods go through small pieces of generated code called PLT
(Program Linkage Table) entries, which transfer control to the runtime which loads the called method before executing it.
For methods compiled by LLVM, the method entry contains a call to the runtime which initializes the method.

## Trampolines

In full-aot mode, the AOT compiler needs to emit all the trampolines which will be used at runtime. This is done in
the following way:
- For most trampolines, the AOT compiler calls the normal trampoline creation function with the `aot` argument set
to TRUE, then saves the returned native code into the AOT image, along with some relocation information like the
GOT slots used by the trampolines.
- For some small trampolines, the AOT compiler directly emits platform specific assembly.

The runtime might require an unbounded number of certain trampolines, but the AOT image can only contain a fixed
number of them. To solve this problem, on some platforms (IOS), its possible to have infinite trampolines. This is
implemented by emitting a different version of these trampolines which reference their corresponding data using
relative addressing. At runtime, a page of these trampolines is mapped using `mmap` next to a writable page
which contains their corresponding data. The same page of trampolines is mapped multiple times at multiple
addresses.

## Cross compilation

Its possible to use the AOT compiler to target a platform different than the host. This requires a separate cross compiler
build of the runtime.
The generated code depends on offsets inside runtime structures like `MonoClass`/`MonoVTable` etc. which could
differ between the host and the target. This is handled by having a tool called the offsets-tool, which is a python
script which uses the clang python interface to compute and emit a C header file containing these offsets. The header
file is passed as a cmake argument during the runtime build. Inside the runtime code, the `MONO_STRUCT_OFFSET`
C macro reads the data from the offsets file to produce the offset corresponding to the target platform.
