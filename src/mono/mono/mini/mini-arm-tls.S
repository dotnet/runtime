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

.macro DECLARE_GLOBAL_SYMBOL name
#ifdef TARGET_MACH
	.global _\name
_\name:
#else
	.global \name
\name:
#endif
.endm

	.text
/* no .arch on clang. it only supports armv6+ anyway */
#ifndef TARGET_MACH
	.arch armv5
#endif
	.arm
	.align 4

DECLARE_GLOBAL_SYMBOL mono_fast_get_tls_key
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
DECLARE_GLOBAL_SYMBOL mono_fast_get_tls_key_end

	/*
	 * The following thunks fetch the value corresponding to the key/offset
	 * passed in R0. These thunks are used in the unlikely cases where we determine
	 * at runtime that the current implementation is not accounted for.
	 */

	.align 4
DECLARE_GLOBAL_SYMBOL mono_fallback_get_tls_key
#if defined(__linux__) && defined(HAVE_KW_THREAD) && defined(__ARM_EABI__)
	push	{lr}
	mov	r1, r0
	bl	__aeabi_read_tp
	ldr	r0, [r0, r1]
	pop	{pc}
#elif defined(TARGET_IOS)
	push	{r7, lr}
	mov	r7, sp
	bl	_pthread_getspecific
	pop	{r7, pc}
#elif defined(TARGET_ANDROID)
	push	{lr}
	bl	pthread_getspecific
	pop	{pc}
#endif
	/*
	 * The following thunks set the value corresponding to the key/offset
	 * passed in R0. These thunks don't do jumps to external code so execution
	 * within can be tracked. The tls value is passed in R1.
	 */

	.align 4
DECLARE_GLOBAL_SYMBOL mono_fast_set_tls_key
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
DECLARE_GLOBAL_SYMBOL mono_fast_set_tls_key_end

	/*
	 * The following thunks set the value corresponding to the key/offset
	 * passed in R0. These thunks are used in the unlikely cases where we determine
	 * at runtime that the current implementation is not accounted for.
	 */

	.align 4
DECLARE_GLOBAL_SYMBOL mono_fallback_set_tls_key
#if defined(__linux__) && defined(HAVE_KW_THREAD) && defined(__ARM_EABI__)
	push	{lr}
	mov	r2, r0
	bl	__aeabi_read_tp
	str	r1, [r0, r2]
	pop	{pc}
#elif defined(TARGET_IOS)
	push	{r7, lr}
	mov	r7, sp
	bl	_pthread_setspecific
	pop	{r7, pc}
#elif defined(TARGET_ANDROID)
	push	{lr}
	bl	pthread_setspecific
	pop	{pc}
#endif


	/* Additional tls getters/setters */

#if defined(TARGET_ANDROID)
	.align 4
DECLARE_GLOBAL_SYMBOL mono_fast_get_tls_key2
	bic	r0, r0, #0x80000000
	mrc	15, 0, r1, cr13, cr0, #3
	lsls	r0, r0, #3
	ldr	r1, [r1, #4]
	add	r0, r1
	ldr	r0, [r0, #0x68]
	bx	lr
DECLARE_GLOBAL_SYMBOL mono_fast_get_tls_key2_end

	/*
	 * In order to use this fast setter we need to make sure that the key was
	 * already set at least once using the pthread api, otherwise it will not
	 * interwork with the api.
	 */
	.align 4
DECLARE_GLOBAL_SYMBOL mono_fast_set_tls_key2
	bic	r0, r0, #0x80000000
	mrc	15, 0, r2, cr13, cr0, #3
	lsls	r0, r0, #3
	ldr	r2, [r2, #4]
	add	r0, r2
	str	r1, [r0, #0x68]
	bx	lr
DECLARE_GLOBAL_SYMBOL mono_fast_set_tls_key2_end
#endif

#endif

