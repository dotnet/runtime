/**
 * \file
 */

#ifndef __MONO_MINI_ARCH_H__
#define __MONO_MINI_ARCH_H__

#ifdef TARGET_X86
#include "mini-x86.h"
#elif defined(TARGET_AMD64)
#include "mini-amd64.h"
#elif defined(TARGET_POWERPC)
#include "mini-ppc.h"
#elif defined(TARGET_S390X)
# if defined(__s390x__)
#  include "mini-s390x.h"
# else
#error "s390 is no longer supported."
# endif
#elif defined(TARGET_ARM)
#include "mini-arm.h"
#elif defined(TARGET_ARM64)
#include "mini-arm64.h"
#elif defined (TARGET_RISCV)
#include "mini-riscv.h"
#elif TARGET_WASM
#include "mini-wasm.h"
#else
#error add arch specific include file in mini-arch.h
#endif

#if (MONO_ARCH_FRAME_ALIGNMENT == 4)
#define MONO_ARCH_LOCALLOC_ALIGNMENT 8
#else
#define MONO_ARCH_LOCALLOC_ALIGNMENT MONO_ARCH_FRAME_ALIGNMENT
#endif

#endif /* __MONO_MINI_ARCH_H__ */
