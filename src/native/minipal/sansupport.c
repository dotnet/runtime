// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "utils.h"

// Use a typedef here as __declspec + pointer return type causes a parse error in MSVC
typedef const char* charptr_t;

charptr_t SANITIZER_CALLBACK_CALLCONV __asan_default_options(void);

charptr_t SANITIZER_CALLBACK_CALLCONV __asan_default_options(void) {
    // symbolize=1 to get symbolized stack traces
    // use_sigaltstack=0 as coreclr uses own alternate stack for signal handlers
    // detect_leaks=0 as coreclr intentionally doesn't clean up all memory on exit
    // handle_segv=0 as coreclr has it causes AddressSanitizer to crash the process even when in
    //               the middle of a block that is allowed to AV and can recover from AVs.
    //               (see the AVInRuntimeImplOkayHolder mechanism in CoreCLR for an example)
    // allocator_may_return_null=1 as .NET handles this gracefully by throwing an OutOfMemoryException.
  return "symbolize=1 use_sigaltstack=0 detect_leaks=0 handle_segv=0 allocator_may_return_null=1";
}

void SANITIZER_CALLBACK_CALLCONV __asan_on_error(void);
void SANITIZER_CALLBACK_CALLCONV __asan_on_error(void) {
}
