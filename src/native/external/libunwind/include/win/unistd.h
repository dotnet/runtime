// This is an incomplete & imprecice implementation of the Posix
// standard file by the same name


// Since this is only intended for VC++ compilers
// use #pragma once instead of guard macros
#pragma once

#ifdef _MSC_VER // Only for cross compilation to windows

#ifndef UNW_REMOTE_ONLY
// This is solely intended to enable compilation of libunwind
// for UNW_REMOTE_ONLY on windows
#error Cross compilation of libunwind on Windows can only support UNW_REMOTE_ONLY
#endif

#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <sys/types.h>

int          close(int);
int          getpagesize(void);
int          open(const char *, int, ...);
ssize_t      read(int fd, void *buf, size_t count);
ssize_t      write(int, const void *, size_t);

#endif // _MSC_VER
