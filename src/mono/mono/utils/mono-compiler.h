#ifndef __UTILS_MONO_COMPILER_H__
#define __UTILS_MONO_COMPILER_H__
/*
 * This file includes macros used in the runtime to encapsulate different
 * compiler behaviours.
 */
#include <config.h>

#ifdef HAVE_KW_THREAD
#if HAVE_TLS_MODEL_ATTR

#if defined(PIC) && defined(__x86_64__)
#define MONO_TLS_FAST 
#elif defined (__powerpc__)
#define MONO_TLS_FAST
#else
#define MONO_TLS_FAST __attribute__((tls_model("local-exec")))
#endif

#else
#define MONO_TLS_FAST 
#endif

#if defined(__GNUC__) && defined(__i386__)
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("jmp 1f; .section writetext, \"awx\"; 1: movl $" #var "@ntpoff, %0; jmp 2f; .previous; 2:" : "=r" (offset));
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1
#endif

#else /* no HAVE_KW_THREAD */

#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1

#endif

/* Deal with Microsoft C compiler differences */
#ifdef _MSC_VER

#include <float.h>
#define isnan(x)	_isnan(x)
#define trunc(x)	floor((x))
#define isinf(x)	(_isnan(x) ? 0 : (_fpclass(x) == _FPCLASS_NINF) ? -1 : (_fpclass(x) == _FPCLASS_PINF) ? 1 : 0)

#define popen		_popen
#define pclose		_pclose

#include <direct.h>
#define mkdir(x)	_mkdir(x)

/* GCC specific functions aren't available */
#define __builtin_return_address(x)	NULL

#endif /* _MSC_VER */


#endif /* __UTILS_MONO_COMPILER_H__*/

