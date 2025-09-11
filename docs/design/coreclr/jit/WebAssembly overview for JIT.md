# WebAssembly overview for JIT

- [WebAssembly overview for JIT](#webassembly-overview-for-jit)
  - [Introduction](#introduction)
  - [Origins of WebAssembly](#origins-of-webassembly)
  - [WebAssembly priorities in detail](#webassembly-priorities-in-detail)
    - [Consistent performance](#consistent-performance)
    - [Fast, efficient startup](#fast-efficient-startup)
    - [Efficient, robust sandboxing](#efficient-robust-sandboxing)
    - [Portability](#portability)
  - [Key WebAssembly concepts \& topics](#key-webassembly-concepts--topics)
    - [Modules](#modules)
    - [Memories](#memories)
    - [Global Variables](#global-variables)
    - [Types and Signatures](#types-and-signatures)
    - [Control Flow](#control-flow)
    - [Function Pointers](#function-pointers)
    - [Traps](#traps)
    - [Shadow Stack](#shadow-stack)
    - [Extensions and Verification](#extensions-and-verification)
    - [ABI](#abi)
    - [Exception Handling](#exception-handling)
    - [Fixed-width SIMD](#fixed-width-simd)
    - [Threading](#threading)
    - [Async](#async)

## Introduction

This document attempts to call out key things to know about WebAssembly (aka WASM) and explain the reasons for its existence. For more detail on WebAssembly, please consult the official specification:

https://webassembly.github.io/spec/core/intro/introduction.html

And these documents, among others:

https://developer.mozilla.org/en-US/docs/WebAssembly

https://www.jakobmeier.ch/wasm-road-0


## Origins of WebAssembly

The key motivations behind WebAssembly were to provide consistent runtime performance in the browser with acceptable startup time and code size. Preceding technologies like [asm.js](https://developer.mozilla.org/en-US/docs/Games/Tools/asm.js) and [NaCL](https://en.wikipedia.org/wiki/Google_Native_Client) were attempts to solve the same problems with different upsides and downsides. Lessons from both fed into the development of WebAssembly (hereafter referred to as WASM).

WASM needed to provide all the same security guarantees as JavaScript, while functioning as a compilation target for most applications written in C, C++, Go, Rust or other languages, including potentially languages like Java that traditionally rely on a JIT. In practice the assumption was that much like with asm.js, WASM would be implemented on top of existing JavaScript JIT code generators to leverage existing investments into those technologies and have a consistent story for things like interop and host APIs.

Development began with the SpiderMonkey and V8 teams at Mozilla and Google, responsible for the full JavaScript stack, along with people from the former NaCL team at Google. The working group quickly expanded to include people from Apple's Safari team and Microsoft's Edge team.

## WebAssembly priorities in detail

### Consistent performance

  Optimizing JavaScript performance was a constant battle against the JIT shipped in each of the 3 leading browsers. Each JIT had its own complex set of heuristics that would change on a monthly basis, causing particular code to get deoptimized or have nondeterministic performance. WASM was designed to provide consistent performance, where the code you compile will have very similar performance across all browsers and architectures and the performance won't regress from release to release.

### Fast, efficient startup

  JavaScript and asm.js both suffer from long compilation times and high memory usage during compilation, due to the need to parse dozens or hundreds of megabytes of text, turn it into an AST, then turn it into IR, then turn it into native code. This compilation typically had to happen on every page load before an application could start running, even if techniques like tiering allowed amortizing some of the work. WASM is designed to be relatively quick to convert into IR and compile to native code, by offloading a lot of the optimization passes onto the compiler (i.e. clang) so the browser's JIT has less work to do. It's also easier to cache the compiled native code for a given WASM binary, so that on the second or third load of a webpage, it's even faster.

### Efficient, robust sandboxing

  Every memory access needs to be robustly bounds checked and things like type confusion attacks need to be impossible (at least at an abstract machine level). This is achieved through things like grow-only linear memory, strongly typed function pointers, and a protected shadow stack. For applications written in high-level memory safe languages, this is mostly successful, but for C applications it is not successful in practice due to their reliance on a stack and heap in linear memory. Attacks on C applications are still mitigated by browsers' robust sandboxes, however.

### Portability

  Every instruction needs to be well-specified so that compiled applications will work the same on x86, ARM, PPC, RISC-V, etc regardless of which OS or architecture revision is being used to run them. This means making compromises in terms of what operations are exposed, or specifying operations in a way that can require a single instruction to be emulated by a dozen instructions, and is at odds with the 'consistent performance' priority - but WASM mostly achieves both at the same time through hard work on the VM side.

## Key WebAssembly concepts & topics
In no particular order (sorry!)

### Modules

  A WASM application is comprised of one or more 'modules'. A module contains one or more sections, with some required and some optional.
  Conceptually, a module takes a set of 'imports' - you can think of these as syscalls - and provides a set of 'exports' by containing a list of functions that consume imports to perform work, where some of the functions are exported by name or by address. It is possible to have a module with very few imports - or even 0 imports - that still does meaningful work.

  Examples of typical sections are the import and export sections, the code section (containing function bodies), a global variables section, a signature section (defining strongly-typed function signatures), a data section, or a debug section containing things like function names.

  For the purposes of a JIT, you would typically create one module per method or per class, and the module would import any syscalls/icalls it needs, and export one function for each method. It would import a linear memory and function pointer table from the host application's module.

### Memories

  WASM has a linear memory model where a module can define 1 or more linear memories with an initial size (in pages) and a maximum size (in pages), and then at runtime it can be grown on-demand up to that requested maximum size (usually). The linear heap has no holes and no access controls (all of it is RW), does not support mmap, and cannot shrink.

  While a module can have multiple memories (or no memories at all), the typical scenario is for all modules to share a single growable linear memory.

### Global Variables

  A module can have one or more strongly-typed global variables, typically used for things like the top of the linear memory stack. These are read/written using dedicated instructions and identified by index, and can be imported/exported just like functions. These variables do not live in linear memory, so you can't take their address.

### Types and Signatures

  Core WASM supports a small set of types: `i32`, `i64`, `f32`, and `f64`. The SIMD extension adds a `v128` type. There are some additional extensions that expand the type system, but generally when speaking about global variables or function local variables, they are one of these types.

  Functions all have a predefined signature from the module's signature table, where each defined signature lists a return type and one or more argument types.

### Control Flow

  WASM enforces somewhat unusual constraints on control flow. All branch targets are represented as 'blocks' which must contain any instructions that branch to that target, which simplifies the implementation of the VM considerably. This creates challenges when compiling arbitrary C or C# to this target. A given block can only be targeted for either forward or backward branches, not both - a `block` can be targeted by jumping to its end, and a `loop` can be targeted by jumping to its beginning.

  When mapping code with complex control flow i.e. `goto` it is often necessary to convert a function body into one or more loops with a dispatcher switch at the top, and transform those jumps into a 'jump to the top of the loop, then jump down to the destination' pattern. This process may also require you to identify patterns of branches which form loops so you can transform them into WASM `loop` blocks to enable backward branching. [Emscripten refers to this as the 'relooper'](https://mozakai.blogspot.com/2012/05/reloop-all-blocks.html), [see also this PDF](https://llvm.org/devmtg/2013-11/slides/Zakai-Emscripten.pdf).

### Function Pointers

  Much like how WASM memory is linear and contiguous, function pointers are indexes into a single table of functions. Each entry in the table has a signature - i.e. `int (int, int)` - and then a function it points to, typically but not necessarily a function defined by the module the table belongs to. These functions can be provided by the host (webpage/browser), or another module.

  Each module can have its own table, though typically all modules will want to share a single table so they can exchange function pointers.

  Invoking a function pointer via `call_indirect` in WASM requires you to specify its signature at compile time and provide all of its formal arguments as values on the WASM stack. The VM will perform a signature check and also verify that the given pointer has a matching entry in the table, trapping if those requirements aren't met. These checks are more expensive in the presence of multiple modules, though the exact details depend on the implementation.

### Traps

  Various misbehavior will cause the WASM VM to 'trap', for example overflowing the stack, calling a null function pointer, or reading/writing out of the bounds of a linear memory. These traps can be viewed as equivalent to something like SIGBUS or SIGKILL, though it is not intended for applications to rely on the ability to handle them explicitly, even if it is possible to catch them in some cases. This creates a model where WASM applications behave deterministically if given deterministic inputs & imports, and will 'fail fast' when given malicious input or encountering a bug.

### Shadow Stack

  The WASM VM implements a simple stack machine, where opcodes push and pop statically-typed values onto/off a shadow stack. The stack is strongly typed and has a known height at every location in a given function, and is contained by a given function. You cannot take the address of values on the shadow stack and the contents of a function's shadow stack cease to exist once it returns. A function's shadow stack is fixed size, comprised of its formal arguments and any 'locals' defined at compile time, numbered sequentially - i.e. a function might have a signature of `int (int, int)` and then define `8` `i32` locals along with 2 `f64` locals, in which case `0` and `1` would be the formal arguments and the rest would be the `i32` and `f64` locals.

  Things like stackallocs and locals/arguments of struct types are typically implemented using a stack and heap in linear memory, though there is a relatively new 'GC' extension that allows defining strong object types and passing them around by-reference.

### Extensions and Verification

  WASM has no formal extension or feature detection model. The feature set is expanded by the working group assigning opcode ranges to new proposals, and an application detects the presence of a feature by attempting to load a tiny 'test module' that uses the opcode(s). If the module fails to load or run, the feature is not present.

  This interacts with the WASM priorities in an unfortunate way. An entire module is compiled at once at startup, and the entire module must be valid. It is not possible for a module to contain conditionally-compiled functions which rely on missing features or unavailable instructions. Adjusting to the presence/non-presence of i.e. SIMD requires shipping multiple versions of each module and feature detecting to determine which module version to load.

  The current state of most feature proposals can be found here: https://webassembly.org/features/

### ABI

  The de-facto ABI for C/C++ in WASM is defined at https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md. It is not part of the spec, and there are corner cases and scenarios that are left unspecified.

### Exception Handling

  Exception handling in WASM is relatively bare-bones. You define a try block with one or more catch clauses, where a given clause either catches a specific 'exception tag' or catches all exceptions (referred to as `catch_all`). Exception tags can be thought of conceptually like `Exception` or `ArgumentException` but in practice they are typically not used this way, and instead an entire language or compiler may use a single tag for its purposes - i.e. a `c++exception` tag which has an attached pointer into the linear memory where the real exception data lives. A given catch clause might then contain a series of type checks based on the data in linear memory.

  Emscripten currently mostly aligns with the the libc++ ABI (functions like `__cxa_begin_catch`) for exception handling. The best documentation I've found is at https://github.com/WebAssembly/tool-conventions/blob/main/EHScheme.md, and it appears to be derived from the Itanium C++ ABI.

### Fixed-width SIMD

  [The WASM SIMD extension](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md) operates on 128-bit vectors with a lowest-common-denominator feature set that (generally) is efficient on x86, x86-64, arm32, arm64, and risc-v, and has well-defined behavior. There is [a 'relaxed SIMD' extension](https://github.com/WebAssembly/relaxed-simd/tree/main/proposals/relaxed-simd) that provides an expanded set of vector operations that have less consistent performance or may expose platform-specific undefined behavior.

### Threading

  Generally speaking, threads do not exist as a concept in the WASM spec. Instead, an [optional 'post-MVP' threading extension](https://github.com/WebAssembly/threads/blob/main/proposals/threads/Overview.md) added support for things like atomics, fences, and shareable linear memory. Creating and manipulating threads is entirely the domain of the host.

  Each host "thread" in practice has its own separate instance of the application module(s), and each instance has its own function table, global variables, and imports/exports. These threads then coordinate by sharing a single linear memory and using a mix of host imports (like a socket API) and atomics/fences.

  It is necessary for an application to ensure that any changes to the function table are synchronized between threads, and any global variable changes need to be manually synchronized between threads (either by storing them in shared linear memory, or via RPC). ⚠️ All WASM global variables are effectively TLS variables. As a result of each instance having its own function table, function pointers are effectively thread-local! ⚠️

### Async

  There is a proposal to integrate WASM with JavaScript promises (the foundation of JS async/await). I am not intimately familiar with this proposal, but you can find it here: https://github.com/WebAssembly/js-promise-integration/blob/main/proposals/js-promise-integration/Overview.md

  The proposal implies the VM has an ability to suspend arbitrary WASM code until a promise is fulfilled, and then resume the code and provide it with the result of the asynchronous operation. This proposal is not yet widely supported and how it interacts with threads is unclear.
