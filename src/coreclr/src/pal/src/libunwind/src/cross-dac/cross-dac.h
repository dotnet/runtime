// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is minimal implementation of a header files required to cross compile
// libunwind on a Windows host

#ifndef CROSS_DAC_H_
#define CROSS_DAC_H_

#ifndef DACCESS_COMPILE
#error This file should only be include when compiling for DAC
// Contents of this file are here just to enable compliation of libunwind
// on Windows.  Contents may not represent actual Posix headers
// Many constants for instance are set to dummy values to make compilation possible...
// Use at your own risk
#endif

#include <inttypes.h>
#include <string.h>
#include <stddef.h>
#include <stdio.h>

#include "freebsd-elf_common.h"
#include "freebsd-elf32.h"
#include "freebsd-elf64.h"

// Endian.h
#define __LITTLE_ENDIAN        1234
#define __BIG_ENDIAN           4321
#define __BYTE_ORDER __LITTLE_ENDIAN

#ifdef TARGET_AMD64
typedef struct ucontext
{
    unsigned long DAC_IGNORE[16];
}
ucontext_t;
#endif // TARGET_AMD64

#ifdef TARGET_ARM64
typedef struct ucontext
{
    unsigned long DAC_IGNORE[16];
}
ucontext_t;
#endif // TARGET_ARM64

typedef long pid_t;
typedef long pthread_key_t;
typedef long pthread_mutex_t;
typedef long pthread_mutexattr_t;
typedef long pthread_once_t;
typedef ptrdiff_t ssize_t;
typedef long siginfo_t;
typedef long sigset_t;

// Posix constants (with somewhat arbitrary values)

#define MAP_FAILED (void *) -1
#define MAP_ANON             1
#define MAP_PRIVATE          2
#define PROT_READ            4
#define PROT_WRITE           8
#define PTHREAD_DESTRUCTOR_ITERATIONS 0
#define PTHREAD_MUTEX_INITIALIZER 0
#define PTHREAD_ONCE_INIT 0

// Posix function prototypes

int          close(int);
int          getpagesize(void);
void*        mmap(void *, size_t, int, int, int, size_t);
int          munmap(void *, size_t);
int          open(const char *, int, ...);
int          pthread_key_create(pthread_key_t *, void (*)(void*));
int          pthread_mutex_init(pthread_mutex_t *, const pthread_mutexattr_t *);
int          pthread_mutex_lock(pthread_mutex_t *);
int          pthread_mutex_unlock(pthread_mutex_t *);
int          pthread_once(pthread_once_t *, void (*)(void));
int          pthread_setspecific(pthread_key_t, const void *);
ssize_t      read(int fd, void *buf, size_t count);
int          sigfillset(sigset_t *set);
ssize_t      write(int, const void *, size_t);

#endif // CROSS_DAC_ENDIAN_H_
