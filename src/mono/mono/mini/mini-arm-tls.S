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
#if defined(__linux__)
	mrc	p15, 0, r1, c13, c0, 3
#if defined(HAVE_KW_THREAD)
	ldr	r0, [r1, r0]
#elif defined(TARGET_ANDROID)
	ldr	r0, [r1, r0, lsl #2]
#endif
	bx	lr
#elif defined(TARGET_IOS)
	mrc	p15, 0, r1, c13, c0, 3
	bic	r1, r1, #3
	ldr	r0, [r1, r0, lsl #2]
	bx	lr
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
#if defined(__linux__)
	mov	r1, r0
	mvn	r0, #0xf000
	sub	r0, r0, #31
	push	{lr}
	blx	r0
#if defined(HAVE_KW_THREAD)
	ldr	r0, [r0, r1]
#elif defined(TARGET_ANDROID)
	ldr	r0, [r0, r1, lsl #2]
#endif
	pop	{pc}
#elif defined(TARGET_IOS)
	push	{lr}
	bl	_pthread_getspecific
	pop	{pc}
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
#if defined(__linux__)
	mrc	p15, 0, r2, c13, c0, 3
#if defined(HAVE_KW_THREAD)
	str	r1, [r2, r0]
#elif defined(TARGET_ANDROID)
	str	r1, [r2, r0, lsl #2]
#endif
	bx	lr
#elif defined(TARGET_IOS)
	mrc	p15, 0, r2, c13, c0, 3
	bic	r2, r2, #3
	str	r1, [r2, r0, lsl #2]
	bx	lr
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
#if defined(__linux__)
	mov	r2, r0
	mvn	r0, #0xf000
	sub	r0, r0, #31
	push	{lr}
	blx	r0
#if defined(HAVE_KW_THREAD)
	str	r1, [r0, r2]
#elif defined(TARGET_ANDROID)
	str	r1, [r0, r2, lsl #2]
#endif
	pop	{pc}
#elif defined(TARGET_IOS)
	push	{lr}
	bl	_pthread_setspecific
	pop	{pc}
#endif

#endif

