/*
 * sgen-archdep.h: Architecture dependent parts of SGen.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#ifndef __MONO_SGENARCHDEP_H__
#define __MONO_SGENARCHDEP_H__

#include <mono/utils/mono-sigcontext.h>

#if defined(MONO_CROSS_COMPILE)

#define REDZONE_SIZE	0

#define ARCH_NUM_REGS 0
#define ARCH_STORE_REGS(ptr)
#define ARCH_SIGCTX_SP(ctx) NULL
#define ARCH_SIGCTX_IP(ctx) NULL
#define ARCH_COPY_SIGCTX_REGS(a,ctx)

#elif defined(TARGET_X86)

#include <mono/utils/mono-context.h>

#define REDZONE_SIZE	0

#define ARCH_NUM_REGS 8

#ifdef MONO_ARCH_HAS_MONO_CONTEXT
#define USE_MONO_CTX
#else
#ifdef _MSC_VER
#define ARCH_STORE_REGS(ptr) __asm {	\
		__asm mov [ptr], edi \
		__asm mov [ptr+4], esi \
		__asm mov [ptr+8], ebx \
		__asm mov [ptr+12], edx \
		__asm mov [ptr+16], ecx \
		__asm mov [ptr+20], eax \
		__asm mov [ptr+24], ebp \
		__asm mov [ptr+28], esp \
	}
#else
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"mov %%edi,0(%0)\n"	\
		"mov %%esi,4(%0)\n"	\
		"mov %%ebx,8(%0)\n"	\
		"mov %%edx,12(%0)\n"	\
		"mov %%ecx,16(%0)\n"	\
		"mov %%eax,20(%0)\n"	\
		"mov %%ebp,24(%0)\n"	\
		"mov %%esp,28(%0)\n"	\
		:			\
		: "r" (ptr)	\
	)
#endif
#endif

/*FIXME, move this to mono-sigcontext as this is generaly useful.*/
#define ARCH_SIGCTX_SP(ctx)    (UCONTEXT_REG_ESP ((ctx)))
#define ARCH_SIGCTX_IP(ctx)    (UCONTEXT_REG_EIP ((ctx)))

#elif defined(TARGET_AMD64)

#include <mono/utils/mono-context.h>

#define REDZONE_SIZE	128

#define ARCH_NUM_REGS 16
#define USE_MONO_CTX

/*FIXME, move this to mono-sigcontext as this is generaly useful.*/
#define ARCH_SIGCTX_SP(ctx)    (UCONTEXT_REG_RSP (ctx))
#define ARCH_SIGCTX_IP(ctx)    (UCONTEXT_REG_RIP (ctx))

#elif defined(TARGET_POWERPC)

#define REDZONE_SIZE	224

#define ARCH_NUM_REGS 32
#ifdef __APPLE__
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"stmw r0, 0(%0)\n"	\
		:			\
		: "b" (ptr)		\
	)
#else
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"stmw 0, 0(%0)\n"	\
		:			\
		: "b" (ptr)		\
	)
#endif
#define ARCH_SIGCTX_SP(ctx)	(UCONTEXT_REG_Rn((ctx), 1))
#define ARCH_SIGCTX_IP(ctx)	(UCONTEXT_REG_NIP((ctx)))
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {	\
	int __i;	\
	for (__i = 0; __i < 32; ++__i)	\
		((a)[__i]) = UCONTEXT_REG_Rn((ctx), __i);	\
	} while (0)

#elif defined(TARGET_ARM)

#define REDZONE_SIZE	0
#define USE_MONO_CTX

/* We dont store ip, sp */
#define ARCH_NUM_REGS 14
#define ARCH_STORE_REGS(ptr)		\
	__asm__ __volatile__(			\
		"push {lr}\n"				\
		"mov lr, %0\n"				\
		"stmia lr!, {r0-r12}\n"		\
		"pop {lr}\n"				\
		:							\
		: "r" (ptr)					\
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

#elif defined(__mips__)

#define REDZONE_SIZE	0

#define USE_MONO_CTX
#define ARCH_NUM_REGS 32

#define ARCH_SIGCTX_SP(ctx)	(UCONTEXT_GREGS((ctx))[29])
#define ARCH_SIGCTX_IP(ctx)	(UCONTEXT_REG_PC((ctx)))

#elif defined(__s390x__)

#define REDZONE_SIZE	0

#include <mono/utils/mono-context.h>

#define USE_MONO_CTX
#define ARCH_NUM_REGS 16	
#define ARCH_SIGCTX_SP(ctx)	((UCONTEXT_GREGS((ctx))) [15])
#define ARCH_SIGCTX_IP(ctx)	((ucontext_t *) (ctx))->uc_mcontext.psw.addr

#elif defined(__sparc__)

#define REDZONE_SIZE	0

/* Don't bother with %g0 (%r0), it's always hard-coded to zero */
#define ARCH_NUM_REGS 15	
#ifdef __sparcv9
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"st %%g1,[%0]\n\t"	\
		"st %%g2,[%0+0x08]\n\t"	\
		"st %%g3,[%0+0x10]\n\t"	\
		"st %%g4,[%0+0x18]\n\t"	\
		"st %%g5,[%0+0x20]\n\t"	\
		"st %%g6,[%0+0x28]\n\t"	\
		"st %%g7,[%0+0x30]\n\t"	\
		"st %%o0,[%0+0x38]\n\t"	\
		"st %%o1,[%0+0x40]\n\t"	\
		"st %%o2,[%0+0x48]\n\t"	\
		"st %%o3,[%0+0x50]\n\t"	\
		"st %%o4,[%0+0x58]\n\t"	\
		"st %%o5,[%0+0x60]\n\t"	\
		"st %%o6,[%0+0x68]\n\t"	\
		"st %%o7,[%0+0x70]\n\t"	\
		: 			\
		: "r" (ptr)		\
		: "memory"			\
	)
#else
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"st %%g1,[%0]\n\t"	\
		"st %%g2,[%0+0x04]\n\t"	\
		"st %%g3,[%0+0x08]\n\t"	\
		"st %%g4,[%0+0x0c]\n\t"	\
		"st %%g5,[%0+0x10]\n\t"	\
		"st %%g6,[%0+0x14]\n\t"	\
		"st %%g7,[%0+0x18]\n\t"	\
		"st %%o0,[%0+0x1c]\n\t"	\
		"st %%o1,[%0+0x20]\n\t"	\
		"st %%o2,[%0+0x24]\n\t"	\
		"st %%o3,[%0+0x28]\n\t"	\
		"st %%o4,[%0+0x2c]\n\t"	\
		"st %%o5,[%0+0x30]\n\t"	\
		"st %%o6,[%0+0x34]\n\t"	\
		"st %%o7,[%0+0x38]\n\t"	\
		: 			\
		: "r" (ptr)		\
		: "memory"			\
	)
#endif

#define ARCH_SIGCTX_SP(ctx)	(((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_SP])
#define ARCH_SIGCTX_IP(ctx)	(((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_PC])
#define ARCH_COPY_SIGCTX_REGS(a,ctx) do {	\
	(a)[0] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G1]);	\
	(a)[1] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G2]);	\
	(a)[2] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G3]);	\
	(a)[3] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G4]);	\
	(a)[4] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G5]);	\
	(a)[5] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G6]);	\
	(a)[6] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_G7]);	\
	(a)[7] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O0]);	\
	(a)[8] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O1]);	\
	(a)[9] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O2]);	\
	(a)[10] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O3]);	\
	(a)[11] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O4]);	\
	(a)[12] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O5]);	\
	(a)[13] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O6]);	\
	(a)[14] = (gpointer) (((ucontext_t *)(ctx))->uc_mcontext.gregs [REG_O7]);	\
	} while (0)

#endif

#endif /* __MONO_SGENARCHDEP_H__ */
