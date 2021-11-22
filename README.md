# Mono on WASI

This is a severely hacked-up fork of the [.NET Runtime](https://github.com/dotnet/runtime) repository in which I've made changes to allow Mono to run in pure WASI runtimes such as [WAMR](https://github.com/bytecodealliance/wasm-micro-runtime/tree/main/wamr-sdk), [Wasmer](https://wasmer.io/), and [Wasmtime](https://wasmtime.dev/). Others may also work.

## Building

Use WSL or a real Linux environment.

Download and unpack the current release of [WASI SDK](https://github.com/WebAssembly/wasi-sdk). In the following instructions, I assume this is at `/home/steve/wasi-sdk-12.0`.

TODO: Is the `ICU_LIBDIR` path really needed? I think this path could be changed to the directory containing `libicui18n.a` etc inside a .NET 6/7 Mono WebAssembly runtime package directory.

```
cd src/mono
mkdir build-wasi
cd build-wasi

cmake -G Ninja -DWASI_SDK_PREFIX=/home/steve/wasi-sdk-12.0 -DICU_LIBDIR=/mnt/c/Users/stevesa/Desktop/MonoHelloWorld/browser-wasm-native/ -DCMAKE_C_FLAGS="--sysroot=/home/steve/wasi-sdk-12.0/share/wasi-sysroot -I/mnt/c/Users/stevesa/Desktop/MonoHelloWorld/browser-wasm-native/include -Wl,--allow-undefined" -DCMAKE_CXX_FLAGS="--sysroot=/home/steve/wasi-sdk-12.0/share/wasi-sysroot" -DCMAKE_TOOLCHAIN_FILE=/home/steve/wasi-sdk-12.0/share/cmake/wasi-sdk.cmake -DCMAKE_SYSROOT=/home/steve/wasi-sdk-12.0/share/wasi-sysroot/ -DENABLE_MINIMAL=jit,sgen_major_marksweep_conc,sgen_split_nursery,sgen_gc_bridge,sgen_toggleref,sgen_debug_helpers,sgen_binary_protocol,logging,shared_perfcounters,interpreter,threads,qcalls,debugger_agent,sockets,eventpipe -DDISABLE_SHARED_LIBS=1  -Wl,--allow-undefined ..

ninja
```

You can then use the resulting `src/mono/build-wasi/mono/mini/*.a` files as part of a build of [`dotnet-wasi-simpleapp`](https://github.com/SteveSandersonMS/dotnet-wasi-simpleapp).

If this is developed further, it could become a runtime pack that distributes the prebuilt runtime and libraries so it can easily be used with .NET console applications.
