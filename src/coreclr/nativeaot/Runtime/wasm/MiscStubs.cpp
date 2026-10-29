// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runtime-async returns its continuation through a mutable WebAssembly global.
asm(".globl __async_continuation\n"
    ".globaltype __async_continuation, i32\n"
    "__async_continuation:\n");
