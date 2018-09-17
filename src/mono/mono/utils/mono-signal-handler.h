/**
 * \file
 * Handle signal handler differences across platforms
 *
 * Copyright (C) 2013 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SIGNAL_HANDLER_H__
#define __MONO_SIGNAL_HANDLER_H__

#include "config.h"

/*
 * When a signal is delivered to a thread on a Krait Android device
 * that's in the middle of skipping over an "IT" block, such as this
 * one:
 *
 * 0x40184ef0 <dlfree+1308>:	ldr	r1, [r3, #0]
 * 0x40184ef2 <dlfree+1310>:	add.w	r5, r12, r2, lsl #3
 * 0x40184ef6 <dlfree+1314>:	lsls.w	r2, r0, r2
 * 0x40184efa <dlfree+1318>:	tst	r2, r1
 * ### this is the IT instruction
 * 0x40184efc <dlfree+1320>:	itt	eq
 * 0x40184efe <dlfree+1322>:	orreq	r2, r1
 * ### signal arrives here
 * 0x40184f00 <dlfree+1324>:	streq	r2, [r3, #0]
 * 0x40184f02 <dlfree+1326>:	beq.n	0x40184f1a <dlfree+1350>
 * 0x40184f04 <dlfree+1328>:	ldr	r2, [r5, #8]
 * 0x40184f06 <dlfree+1330>:	ldr	r3, [r3, #16]
 *
 * then the first few (at most four, one would assume) instructions of
 * the signal handler (!) might be skipped.  They happen to be the
 * push of the frame pointer and return address, so once the signal
 * handler has done its work, it returns into a SIGSEGV.
 */

#if defined (TARGET_ARM) && defined (HAVE_ARMV7) && defined (TARGET_ANDROID)
#define KRAIT_IT_BUG_WORKAROUND	1
#endif

#ifdef KRAIT_IT_BUG_WORKAROUND
#define MONO_SIGNAL_HANDLER_FUNC(access, name, arglist)		\
	static void __krait_ ## name arglist;	\
	__attribute__ ((__naked__)) access void				\
	name arglist							\
	{								\
		asm volatile (						\
			      "mov r0, r0\n\t"				\
			      "mov r0, r0\n\t"				\
			      "mov r0, r0\n\t"				\
			      "mov r0, r0\n\t"				\
				  "b __krait_" # name			\
				  "\n\t");						\
	}	\
	static __attribute__ ((__used__)) void __krait_ ## name arglist
#endif

/* Don't use this */
#ifndef MONO_SIGNAL_HANDLER_FUNC
#define MONO_SIGNAL_HANDLER_FUNC(access, name, arglist) access void name arglist
#endif

/*
 * Macros to work around signal handler differences on various platforms.
 *
 * To declare a signal handler function:
 * void MONO_SIG_HANDLER_SIGNATURE (handler_func)
 * To define a signal handler function:
 * MONO_SIG_HANDLER_FUNC(access, name)
 * To call another signal handler function:
 * handler_func (MONO_SIG_HANDLER_PARAMS);
 * To obtain the signal number:
 * int signo = MONO_SIG_HANDLER_GET_SIGNO ();
 * To obtain the signal context:
 * MONO_SIG_HANDLER_GET_CONTEXT ().
 * This will define a variable name 'ctx'.
 */

#ifdef HOST_WIN32
#define MONO_SIG_HANDLER_INFO_TYPE EXCEPTION_POINTERS
/* seh_vectored_exception_handler () passes in a CONTEXT* */
#else
/* sigaction */
#define MONO_SIG_HANDLER_INFO_TYPE siginfo_t
#endif

// FIXME This should be defined (MONO_ARCH_USE_SIGACTION) || defined (HOST_WIN32)
// But the correct value isn't coming through on Mac. A small C-only test does work on Mac.
#if /*defined (HOST_WIN32) ||*/ defined (HOST_LINUX)
#define MONO_SIG_HANDLER_DEBUG 1 // "with_fault_addr" but could be extended in future, so "debug"
#endif

#define MONO_SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, MONO_SIG_HANDLER_INFO_TYPE *_info, void *context)
#define MONO_SIG_HANDLER_FUNC(access, ftn) MONO_SIGNAL_HANDLER_FUNC (access, ftn, (int _dummy, MONO_SIG_HANDLER_INFO_TYPE *_info, void *context))
#define MONO_SIG_HANDLER_PARAMS _dummy, _info, context
#define MONO_SIG_HANDLER_GET_SIGNO() (_dummy)
#define MONO_SIG_HANDLER_GET_INFO() (_info)
#define MONO_SIG_HANDLER_GET_CONTEXT \
    void *ctx = context;

#ifdef MONO_SIG_HANDLER_DEBUG
// Same as MONO_SIG_HANDLER_FUNC but debug_fault_addr is added to params, and no_optimize.
// The Krait workaround is not needed here, due to this not actually being the signal handler,
// so MONO_SIGNAL_HANDLER_FUNC is combined into it.
#define MONO_SIG_HANDLER_FUNC_DEBUG(access, ftn) access MONO_ATTRIBUTE_NO_OPTIMIZE void ftn \
	(int _dummy, MONO_SIG_HANDLER_INFO_TYPE *_info, void *context, void * volatile debug_fault_addr G_GNUC_UNUSED)
#define MONO_SIG_HANDLER_PARAMS_DEBUG MONO_SIG_HANDLER_PARAMS, debug_fault_addr
#endif

#endif // __MONO_SIGNAL_HANDLER_H__
