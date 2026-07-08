// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// void* GetCurrentSP()
//
// Returns the current native (linear-memory) stack pointer by reading the wasm
// __stack_pointer global. This mirrors src/mono/mono/utils/mono-threads-wasm.S:
// wasi-sdk exposes no emscripten_stack_get_current(), and __stack_pointer is the
// portable equivalent that LLVM maintains for the C stack. threads.cpp uses this
// for stack-base/limit bookkeeping and stack-probe high-water marks.

.globaltype __stack_pointer, i32

.functype       GetCurrentSP () -> (i32)
.section        .text.GetCurrentSP,"",@
.hidden         GetCurrentSP
.globl          GetCurrentSP
.type           GetCurrentSP,@function
GetCurrentSP:
        .functype       GetCurrentSP () -> (i32)
        global.get      __stack_pointer
        return
        end_function
