// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_cabi_wasi.h"

/* Done in C so we can avoid initializing the dotnet runtime and hence WASI libc */
/* It would be preferable to do this in C# but the constraints of cabi_realloc and the demands */
/* of WASI libc prevent us doing so. */
/* See https://github.com/bytecodealliance/wit-bindgen/issues/777  */
/* and https://github.com/WebAssembly/wasi-libc/issues/452 */
/* The component model `start` function might be an alternative to this depending on whether it */
/* has the same constraints as `cabi_realloc` */
__attribute__((__weak__, __export_name__("cabi_realloc")))
void *cabi_realloc(void *ptr, size_t old_size, size_t align, size_t new_size) {
    (void) old_size;
    if (new_size == 0) return (void*) align;
    void *ret = realloc(ptr, new_size);
    if (!ret) abort();
    return ret;
}
