#ifndef __MONO_MINI_ARCH_H__
#define __MONO_MINI_ARCH_H__

#ifdef __i386__
#include "mini-x86.h"
#elif defined(__ppc__)
#include "mini-ppc.h"
#else
#error add arch specific include file in mini-arch.h
#endif

#endif /* __MONO_MINI_ARCH_H__ */  
