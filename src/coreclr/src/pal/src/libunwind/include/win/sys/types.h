// This is an incomplete & imprecice implementation of the Posix
// standard file by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

// Posix is a superset of the ISO C sys/types
// include ISO C version first
#include <../ucrt/sys/types.h>
#include <stddef.h>

typedef int pid_t;
typedef ptrdiff_t ssize_t;

#endif // _MSC_VER
