/*
 * SGen is licensed under the terms of the MIT X11 license
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#ifndef __MONO_SGENARCHDEP_H__
#define __MONO_SGENARCHDEP_H__

#include <mono/utils/mono-sigcontext.h>

#ifdef __i386__

#define REDZONE_SIZE	0

#define ARCH_NUM_REGS 7		/* we're never storing ESP */
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"mov %%ecx, 0x00(%0)\n"	\
		"mov %%edx, 0x04(%0)\n"	\
		"mov %%ebx, 0x08(%0)\n"	\
		"mov %%edi, 0x0c(%0)\n"	\
		"mov %%esi, 0x10(%0)\n"	\
		"mov %%ebp, 0x14(%0)\n"	\
		: "=&a" (ptr)	\
		: "0" (cur_thread_regs)	\
		: "memory"	\
	)
#define ARCH_SIGCTX_SP(ctx)	(UCONTEXT_REG_ESP ((ctx)))
#define ARCH_SIGCTX_IP(ctx)	(UCONTEXT_REG_EIP ((ctx)))
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {			\
	(a)[0] = (gpointer) UCONTEXT_REG_EAX ((ctx));		\
	(a)[1] = (gpointer) UCONTEXT_REG_EBX ((ctx));		\
	(a)[2] = (gpointer) UCONTEXT_REG_ECX ((ctx));		\
	(a)[3] = (gpointer) UCONTEXT_REG_EDX ((ctx));		\
	(a)[4] = (gpointer) UCONTEXT_REG_ESI ((ctx));		\
	(a)[5] = (gpointer) UCONTEXT_REG_EDI ((ctx));		\
	(a)[6] = (gpointer) UCONTEXT_REG_EBP ((ctx));		\
	} while (0)

#elif defined(__x86_64__)

#define REDZONE_SIZE	128

#define ARCH_NUM_REGS 15	/* we're never storing RSP */
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"movq %%rcx, 0x00(%0)\n"	\
		"movq %%rdx, 0x08(%0)\n"	\
		"movq %%rbx, 0x10(%0)\n"	\
		"movq %%rdi, 0x18(%0)\n"	\
		"movq %%rsi, 0x20(%0)\n"	\
		"movq %%rbp, 0x28(%0)\n"	\
		"movq %%r8, 0x30(%0)\n"	\
		"movq %%r9, 0x38(%0)\n"	\
		"movq %%r10, 0x40(%0)\n"	\
		"movq %%r11, 0x48(%0)\n"	\
		"movq %%r12, 0x50(%0)\n"	\
		"movq %%r13, 0x58(%0)\n"	\
		"movq %%r14, 0x60(%0)\n"	\
		"movq %%r15, 0x68(%0)\n"	\
		: "=&a" (ptr)	\
		: "0" (cur_thread_regs)	\
		: "memory"	\
	)
#define ARCH_SIGCTX_SP(ctx)    (UCONTEXT_REG_RSP (ctx))
#define ARCH_SIGCTX_IP(ctx)    (UCONTEXT_REG_RIP (ctx))
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {	\
	((a)[0] = (UCONTEXT_REG_RAX (ctx)));	\
	((a)[1] = (UCONTEXT_REG_RBX (ctx)));	\
	((a)[2] = (UCONTEXT_REG_RCX (ctx)));	\
	((a)[3] = (UCONTEXT_REG_RDX (ctx)));	\
	((a)[4] = (UCONTEXT_REG_RSI (ctx)));	\
	((a)[5] = (UCONTEXT_REG_RDI (ctx)));	\
	((a)[6] = (UCONTEXT_REG_RBP (ctx)));	\
	((a)[7] = (UCONTEXT_REG_R8 (ctx)));	\
	((a)[8] = (UCONTEXT_REG_R9 (ctx)));	\
	((a)[9] = (UCONTEXT_REG_R10 (ctx)));	\
	((a)[10] = (UCONTEXT_REG_R11 (ctx)));	\
	((a)[11] = (UCONTEXT_REG_R12 (ctx)));	\
	((a)[12] = (UCONTEXT_REG_R13 (ctx)));	\
	((a)[13] = (UCONTEXT_REG_R14 (ctx)));	\
	((a)[14] = (UCONTEXT_REG_R15 (ctx)));	\
	} while (0)

#elif defined(__ppc__)

#define REDZONE_SIZE	224

#define ARCH_NUM_REGS 32
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"stmw r0, 0(%0)\n"	\
		:			\
		: "b" (ptr)		\
	)
#define ARCH_SIGCTX_SP(ctx)	(UCONTEXT_REG_Rn((ctx), 1))
#define ARCH_SIGCTX_IP(ctx)	(UCONTEXT_REG_NIP((ctx)))
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {	\
	int __i;	\
	for (__i = 0; __i < 32; ++__i)	\
		((a)[__i]) = UCONTEXT_REG_Rn((ctx), __i);	\
	} while (0)

#elif defined(__arm__)

#define REDZONE_SIZE	0

/* We dont store ip, sp */
#define ARCH_NUM_REGS 14
#define ARCH_STORE_REGS(ptr)				\
	__asm__ __volatile__(				\
		"ldr r12, %0\n"				\
		"push {r0}\n"				\
		"push {r12}\n"				\
		"stmia r12!, {r0-r11}\n"		\
		"pop {r0}\n"				\
		"stmia r12!, {r0, lr}\n"		\
		"mov r12, r0\n"				\
		"pop {r0}\n"				\
		: 					\
		: "m" (ptr)				\
	)

#define ARCH_SIGCTX_SP(ctx)	(UCONTEXT_REG_SP((ctx)))
#define ARCH_SIGCTX_IP(ctx)	(UCONTEXT_REG_PC((ctx)))
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {			\
	((a)[0]) = (gpointer) (UCONTEXT_REG_R0((ctx)));		\
	((a)[1]) = (gpointer) (UCONTEXT_REG_R1((ctx)));		\
	((a)[2]) = (gpointer) (UCONTEXT_REG_R2((ctx)));		\
	((a)[3]) = (gpointer) (UCONTEXT_REG_R3((ctx)));		\
	((a)[4]) = (gpointer) (UCONTEXT_REG_R4((ctx)));		\
	((a)[5]) = (gpointer) (UCONTEXT_REG_R5((ctx)));		\
	((a)[6]) = (gpointer) (UCONTEXT_REG_R6((ctx)));		\
	((a)[7]) = (gpointer) (UCONTEXT_REG_R7((ctx)));		\
	((a)[8]) = (gpointer) (UCONTEXT_REG_R8((ctx)));		\
	((a)[9]) = (gpointer) (UCONTEXT_REG_R9((ctx)));		\
	((a)[10]) = (gpointer) (UCONTEXT_REG_R10((ctx)));	\
	((a)[11]) = (gpointer) (UCONTEXT_REG_R11((ctx)));	\
	((a)[12]) = (gpointer) (UCONTEXT_REG_R12((ctx)));	\
	((a)[13]) = (gpointer) (UCONTEXT_REG_LR((ctx)));	\
	} while (0)

#elif defined(__s390x__)

#define REDZONE_SIZE	0

#define ARCH_NUM_REGS 16	
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"stg	%%r0,0x00(%0)\n\t"	\
		"stg	%%r1,0x08(%0)\n\t"	\
		"stg	%%r2,0x10(%0)\n\t"	\
		"stg	%%r3,0x18(%0)\n\t"	\
		"stg	%%r4,0x20(%0)\n\t"	\
		"stg	%%r5,0x28(%0)\n\t"	\
		"stg	%%r6,0x30(%0)\n\t"	\
		"stg	%%r7,0x38(%0)\n\t"	\
		"stg	%%r8,0x40(%0)\n\t"	\
		"stg	%%r9,0x48(%0)\n\t"	\
		"stg	%%r10,0x50(%0)\n\t"	\
		"stg	%%r11,0x58(%0)\n\t"	\
		"stg	%%r12,0x60(%0)\n\t"	\
		"stg	%%r13,0x68(%0)\n\t"	\
		"stg	%%r14,0x70(%0)\n\t"	\
		"stg	%%r15,0x78(%0)\n"	\
		: "=&a" (ptr)			\
		: "0" (cur_thread_regs)		\
		: "memory"			\
	)
#define ARCH_SIGCTX_SP(ctx)	((UCONTEXT_GREGS((ctx))) [15])
#define ARCH_SIGCTX_IP(ctx)	((ucontext_t *) (ctx))->uc_mcontext.psw.addr
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {		\
	((a)[0] = (gpointer) (UCONTEXT_GREGS((ctx))) [0]);		\
	((a)[1] = (gpointer) (UCONTEXT_GREGS((ctx))) [1]);		\
	((a)[2] = (gpointer) (UCONTEXT_GREGS((ctx))) [2]);		\
	((a)[3] = (gpointer) (UCONTEXT_GREGS((ctx))) [3]);		\
	((a)[4] = (gpointer) (UCONTEXT_GREGS((ctx))) [4]);		\
	((a)[5] = (gpointer) (UCONTEXT_GREGS((ctx))) [5]);		\
	((a)[6] = (gpointer) (UCONTEXT_GREGS((ctx))) [6]);		\
	((a)[7] = (gpointer) (UCONTEXT_GREGS((ctx))) [7]);		\
	((a)[8] = (gpointer) (UCONTEXT_GREGS((ctx))) [8]);		\
	((a)[9] = (gpointer) (UCONTEXT_GREGS((ctx))) [9]);		\
	((a)[10] = (gpointer) (UCONTEXT_GREGS((ctx))) [10]);		\
	((a)[11] = (gpointer) (UCONTEXT_GREGS((ctx))) [11]);		\
	((a)[12] = (gpointer) (UCONTEXT_GREGS((ctx))) [12]);		\
	((a)[13] = (gpointer) (UCONTEXT_GREGS((ctx))) [13]);		\
	((a)[14] = (gpointer) (UCONTEXT_GREGS((ctx))) [14]);		\
	((a)[15] = (gpointer) (UCONTEXT_GREGS((ctx))) [15]);		\
	} while (0)

#endif

#endif /* __MONO_SGENARCHDEP_H__ */
