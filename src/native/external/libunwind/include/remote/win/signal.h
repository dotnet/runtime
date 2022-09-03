// This is an incomplete & imprecice implementation of the Posix
// standard file by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

// Posix is a superset of the ISO C signal.h
// include ISO C version first
#include <../ucrt/signal.h>
#include <sys/types.h>
#include <sys/ucontext.h>

#if defined(__linux__) && defined(__x86_64__)
#  define SIZEOF_SIGINFO 128
#elif defined(__linux__) && defined(__aarch64__)
#  define SIZEOF_SIGINFO 128
#elif defined(__linux__) && defined(__arm__)
#  define SIZEOF_SIGINFO 128
#elif !defined(SIZEOF_SIGINFO)
  // It is not clear whether the sizeof(siginfo_t) is important
  // While compiling on Windows the members are not referenced...
  // However the size maybe important during a case or a memcpy
  // Barring a full audit it could be important so require the size to be defined
#  error SIZEOF_SIGINFO is unknown for this target
#endif

typedef struct siginfo
{
    uint8_t content[SIZEOF_SIGINFO];
} siginfo_t;

typedef long sigset_t;

int          sigfillset(sigset_t *set);

#endif // _MSC_VER
