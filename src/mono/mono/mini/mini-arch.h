#ifndef __MONO_MINI_ARCH_H__
#define __MONO_MINI_ARCH_H__

#ifdef __i386__
#include "mini-x86.h"
#elif defined(__x86_64__)
#include "mini-amd64.h"
#elif defined(__ppc__) || defined(__powerpc__)
#include "mini-ppc.h"
#elif defined(__sparc__) || defined(sparc)
#include "mini-sparc.h"
#elif defined(__s390__) || defined(s390)
# if defined(__s390x__)
#  include "mini-s390x.h"
# else
#  include "mini-s390.h"
# endif
#else
#error add arch specific include file in mini-arch.h
#endif

#endif /* __MONO_MINI_ARCH_H__ */  
