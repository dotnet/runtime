#ifndef __MONO_MINI_ARCH_H__
#define __MONO_MINI_ARCH_H__

#if defined(__i386__) || defined(__MINGW32__)
#include "mini-x86.h"
#elif defined(__ppc__) || defined(__powerpc__)
#include "mini-ppc.h"
#elif defined(__sparc__) || defined(sparc)
#include "mini-sparc.h"
#elif defined(__s390__) || defined(s390)
#include "mini-s390.h"
#else
#error add arch specific include file in mini-arch.h
#endif

#endif /* __MONO_MINI_ARCH_H__ */  
