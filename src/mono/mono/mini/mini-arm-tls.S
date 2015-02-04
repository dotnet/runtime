/*
 * mini-arm-tls.S: tls getters and setters for arm platforms
 *
 * Copyright 2015 Xamarin, Inc.
 */

#include <config.h>

#ifndef MONO_CROSS_COMPILE

	/*
	 * The following thunks fetch the value corresponding to the key/offset
	 * passed in R0. These thunks don't do jumps to external code so execution
	 * within can be tracked. The tls value is returned in R0.
	 */

	.text
/* no .arch on clang. it only supports armv6+ anyway */
#ifndef TARGET_MACH
	.arch armv5
#endif
	.arm
	.align 4
#ifdef TARGET_MACH
	.global _mono_fast_get_tls_key
_mono_fast_get_tls_key :
#else
	.global mono_fast_get_tls_key
mono_fast_get_tls_key :
#endif
#if defined(HAVE_KW_THREAD) && defined(__linux__)
	mrc	p15, 0, r1, c13, c0, 3
	ldr	r0, [r1, r0]
	bx	lr
#else
	nop
#endif

	/*
	 * The following thunks fetch the value corresponding to the key/offset
	 * passed in R0. These thunks are used in the unlikely cases where we determine
	 * at runtime that the current implementation is not accounted for.
	 */

	.align 4
#ifdef TARGET_MACH
	.global _mono_fallback_get_tls_key
_mono_fallback_get_tls_key :
#else
	.global mono_fallback_get_tls_key
mono_fallback_get_tls_key :
#endif
#if defined(HAVE_KW_THREAD) && defined(__linux__)
	mov	r1, r0
	mvn	r0, #0xf000
	sub	r0, r0, #31
	push	{lr}
	blx	r0
	ldr	r0, [r0, r1]
	pop	{pc}
#else
	nop
#endif

	/*
	 * The following thunks set the value corresponding to the key/offset
	 * passed in R0. These thunks don't do jumps to external code so execution
	 * within can be tracked. The tls value is passed in R1.
	 */

	.align 4
#ifdef TARGET_MACH
	.global _mono_fast_set_tls_key
_mono_fast_set_tls_key :
#else
	.global mono_fast_set_tls_key
mono_fast_set_tls_key :
#endif
#if defined(HAVE_KW_THREAD) && defined(__linux__)
	mrc	p15, 0, r2, c13, c0, 3
	str	r1, [r2, r0]
	bx	lr
#else
	nop
#endif

	/*
	 * The following thunks set the value corresponding to the key/offset
	 * passed in R0. These thunks are used in the unlikely cases where we determine
	 * at runtime that the current implementation is not accounted for.
	 */

	.align 4
#ifdef TARGET_MACH
	.global _mono_fallback_set_tls_key
_mono_fallback_set_tls_key :
#else
	.global mono_fallback_set_tls_key
mono_fallback_set_tls_key :
#endif
#if defined(HAVE_KW_THREAD) && defined(__linux__)
	mov	r2, r0
	mvn	r0, #0xf000
	sub	r0, r0, #31
	push	{lr}
	blx	r0
	str	r1, [r0, r2]
	pop	{pc}
#else
	nop
#endif

#endif

