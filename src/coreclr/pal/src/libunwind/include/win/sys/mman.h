// This is an incomplete & imprecice implementation of the Posix
// standard file by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

#include <sys/types.h>

#define MAP_FAILED (void *) -1
#define MAP_ANONYMOUS        1
#define MAP_ANON             MAP_ANONYMOUS
#define MAP_PRIVATE          2
#define PROT_READ            4
#define PROT_WRITE           8
#define PROT_EXEC            16

void* mmap(void *, size_t, int, int, int, size_t);
int   munmap(void *, size_t);

#endif // _MSC_VER
