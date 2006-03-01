#ifndef __UTILS_MONO_COMPILER_H__
#define __UTILS_MONO_COMPILER_H__
/*
 * This file includes macros used in the runtime to encapsulate different
 * compiler behaviours.
 */
#include <config.h>

#ifdef HAVE_KW_THREAD
#if HAVE_TLS_MODEL_ATTR

#if defined (__powerpc__)
#define MONO_TLS_FAST
#elif defined(PIC)
#define MONO_TLS_FAST __attribute__((tls_model("initial-exec")))
#else
#define MONO_TLS_FAST __attribute__((tls_model("local-exec")))
#endif

#else
#define MONO_TLS_FAST 
#endif

#if defined(__GNUC__) && defined(__i386__)
#if defined(PIC)
#define MONO_THREAD_VAR_OFFSET(var,offset) do { int tmp; __asm ("call 1f; 1: popl %0; addl $_GLOBAL_OFFSET_TABLE_+[.-1b], %0; movl " #var "@gotntpoff(%0), %1" : "=r" (tmp), "=r" (offset)); } while (0)
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("movl $" #var "@ntpoff, %0" : "=r" (offset))
#endif
#elif defined(__x86_64__)
#if defined(PIC)
#define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  __asm ("movq " #var "@GOTTPOFF(%%rip), %0" : "=r" (foo)); offset = foo; } while (0)
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  __asm ("movq $" #var "@TPOFF, %0" : "=r" (foo)); offset = foo; } while (0)
#endif
#elif defined(__ia64__) && !defined(__INTEL_COMPILER)
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("addl %0 = @tprel(" #var "#), r0 ;;\n" : "=r" (offset))
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
#define isnormal(x)	_finite(x)

#define popen		_popen
#define pclose		_pclose

#include <direct.h>
#define mkdir(x)	_mkdir(x)

/* GCC specific functions aren't available */
#define __builtin_return_address(x)	NULL

#endif /* _MSC_VER */

#if HAVE_VISIBILITY_HIDDEN
#define MONO_INTERNAL __attribute__ ((visibility ("hidden")))
#else
#define MONO_INTERNAL 
#endif

#endif /* __UTILS_MONO_COMPILER_H__*/

