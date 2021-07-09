# Cross DAC Notes

The `crossdac` is a cross-compiled DAC. It is compiled to execute on one platform, but debug a target of a different architecture.

Our current crossdacs are all:

- compiled to run on Windows
- Same bitness. (Target and host have the same number of bits.
- target a *nix variant

The crossdac allow us to use Windows debugging tools to debug dumps from *nix processes.

## Design

### Limitations

- To avoid solving remoting and synchronization issues, the crossdac will not support live processes. Only dump debugging is supported.
- Similar to the DAC, each cross DAC must match its runtime. The DACs are indexed on a symbol server to allow the debuggers to get these as needed.

### Conditional Code Selection

This is a simple cross compilation of the DAC, `C++` code. This mean the `HOST_*` and the `TARGET_*` are configured differently. In this context:

- `HOST` refers to the architecture of the platform that is running the debugger.
- `TARGET` refers to the platform that generated the code dump.

In general, most code should be conditioned on `TARGET_*` variables. This is because in general we want the `DAC` to behave identically when cross compiled.

Code must be conditioned on `HOST` when it refers to host needed services. These have typically been thing like file i/o and memory allocation.

Initial implementation allowed the compiler to find most of these. The strategy was to assume all code should be conditioned on `TARGET` and let the compiler gripe.

### Type Layout

The DAC is essentially a memory parsing tool with supporting functionality. The layout of types in the DAC must match the layout of types in the runtime.

The `C++` standard is not explicit about all layout rules of data structures. Due to its historical evolution from `C`, most structures are arranged in an intuitive easily understood fashion. Newer and more exotic structures are less consistent.

Experimentation has shown that layout varies in inheritance cases. The DAC does not support general multiple inheritance, so that simplifies things. It does support multiple inheritance with the empty base classes.

These cases have proven to be problematic:

- Classes with empty base classes. (I the only issue is with multiple base classes.)
  - By default `gcc` use an empty base class optimization to eliminate the 1 byte of space these empty base classes normally consume (alone).
  - By default `Windows` compilers do not do this optimization. This is to preserve backward binary compatibility.
  - The Windows compilers allow this optimization to be enabled. Our code uses `EMPTY_BASES_DECL` to enable this optimization. It has to be applied to every structure that has multiple base classes or derives from a such a structure. See `__declspec(empty_bases)`.
- Packing of the first member of the derived class. In the case where the base class ended with padding:
  - `gcc` compilers reuse the padding for the first member of the derived class. This effectively removes the padding of the base class in the derived class.
  - Windows compilers do not remove this padding.
  - Our code uses the `DAC_ALIGNAS(a)` macro before the first element of the derived class to force the `gcc` compiler to align that member and keep the base classes padding.
    - The `a` parameter is preferentially the base classes typename.
    - However, in some cases the compiler will not allow this due to some circular layout issues it causes. In these cases, `a` can refer to a well known type instead. I prefer `int64_t`, `int32_t`, `size_t` ...

#### DacCompareNativeTypes Usage

I wrote and used [DacCompareNativeTypes](https://github.com/dotnet/diagnostics/tree/main/src/tests/DacCompareNativeTypes), to locate and identify type layout issues.

The tool is a bit crude, but it helped get the job done.

The `libcoreclr.so` has a lot of symbols. This proved very slow. So to expedite things, I compared the `dac` and later the `dbi` libraries for structure layout. This had the advantage of eliminating irrelevant data structures.

The compilers generate different debug data and different hidden data structures. The tool tries to overlook these. Be aware that not all differences are real. Some data structures are host only so these are expected to be different.

I usually ran the tool in a debugger so that I could look at other available meta-data the tool keeps. i.e. source file and line number.

### Missing/Different types

There are some cases where types are defined by the Target. These types maybe missing or different on the Host. In these cases we define the cross compilation types in `src/coreclr/inc/crosscomp.h`.

See `T_CRITICAL_SECTION` for a key example. In this case both host and target supported critical sections, but we needed to correctly map the target data structures. So we needed a type defined which was the TARGET's `CRITICAL_SECTION`.

So the Target's definition was made available for the cross compile. Additionally the macro was created to make sure references which required the Target's definition could be separated from ones which might need the host's definition.

There is also some defensive programming to make sure these structures accurate. See `T_CRITICAL_SECTION_VALIDATION_MESSAGE` for one example.

### Out of Process Unwinding

To fully support native stack processing, we needed a Target unwinder. For this `libunwind` was also cross-compiled.

See [CMake cross libunwind](https://github.com/dotnet/runtime/blob/0049c629381c5a18e4dadd1038c2bd6b3ae6e3e6/src/coreclr/CMakeLists.txt#L113)

### DBI

I use the term `DAC` in this document to refer to both the `DAC` and the `DBI` debug interface. Both were actually cross compiled. Be aware.

### Build entry point

The main build systme change is adding the ability to set the Target OS on a Windows build.

- See [build-runtime.cmd changes](https://github.com/dotnet/runtime/blob/0049c629381c5a18e4dadd1038c2bd6b3ae6e3e6/src/coreclr/build-runtime.cmd#L133-L134)
- See [Subsets.props](https://github.com/dotnet/runtime/blob/0049c629381c5a18e4dadd1038c2bd6b3ae6e3e6/eng/Subsets.props#L191-L197)

There are also changes to the official build to set these flags package the results and upload to the symbol server.

### Client changes

Various changes were required in the DAC clients to consume the new crossdac. These are really out of the scope of this document.