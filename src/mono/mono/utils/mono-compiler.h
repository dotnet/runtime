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

#endif /* __UTILS_MONO_COMPILER_H__*/

