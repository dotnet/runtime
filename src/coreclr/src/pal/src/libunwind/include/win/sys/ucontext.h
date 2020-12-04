// This is an incomplete & imprecice implementation of the *nix file
// by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows
#include <inttypes.h>

#if defined(__linux__) && defined(__x86_64__)
#  define SIZEOF_UCONTEXT 936
#elif defined(__linux__) && defined(__aarch64__)
#  define SIZEOF_UCONTEXT 4560
#elif defined(__linux__) && defined(__arm__)
#  define SIZEOF_UCONTEXT 744
#elif !defined(SIZEOF_UCONTEXT)
  // It is not clear whether the sizeof(ucontext_t) is important
  // While compiling on Windows the members are not referenced...
  // However the size maybe important during a case or a memcpy
  // Barring a full audit it could be important so require the size to be defined
#  error SIZEOF_UCONTEXT is unknown for this target
#endif

typedef struct ucontext
{
    uint8_t content[SIZEOF_UCONTEXT];
} ucontext_t;

#ifdef __aarch64__
// These types are used in the definition of the aarch64 unw_tdep_context_t
// They are not used in UNW_REMOTE_ONLY, so typedef them as something
typedef long sigset_t;
typedef long stack_t;

// Windows SDK defines reserved. It conflicts with arm64 ucontext
// Undefine it
#undef __reserved
#endif

#endif // _MSC_VER
