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

#include <mono/utils/mono-context.h>

/*
 * Define either USE_MONO_CTX, or
 * ARCH_SIGCTX_SP/ARCH_SIGCTX_IP/ARCH_STORE_REGS/ARCH_COPY_SIGCTX_REGS.
 * Define ARCH_NUM_REGS to be the number of general registers in MonoContext, or the
 * number of registers stored by ARCH_STORE_REGS.
 */

#if defined(MONO_CROSS_COMPILE)

#define REDZONE_SIZE	0

#define ARCH_NUM_REGS 0
#define ARCH_STORE_REGS(ptr)
#define ARCH_SIGCTX_SP(ctx) NULL
#define ARCH_SIGCTX_IP(ctx) NULL
#define ARCH_COPY_SIGCTX_REGS(a,ctx)

#elif defined(TARGET_X86)

#define REDZONE_SIZE	0

#define ARCH_NUM_REGS 8

#ifndef MONO_ARCH_HAS_MONO_CONTEXT
#error 0
#endif

#define USE_MONO_CTX

#elif defined(TARGET_AMD64)

#define REDZONE_SIZE	128

#define ARCH_NUM_REGS 16
#define USE_MONO_CTX

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
		((a)[__i]) = (gpointer) UCONTEXT_REG_Rn((ctx), __i);	\
	} while (0)

/* MS_BLOCK_SIZE must be a multiple of the system pagesize, which for some
   archs is 64k.  */
#if defined(TARGET_POWERPC64) && _CALL_ELF == 2
#define ARCH_MIN_MS_BLOCK_SIZE	(64*1024)
#define ARCH_MIN_MS_BLOCK_SIZE_SHIFT	16
#endif

#elif defined(TARGET_ARM)

#define REDZONE_SIZE	0
#define USE_MONO_CTX

/* We dont store ip, sp */
#define ARCH_NUM_REGS 14

#elif defined(TARGET_ARM64)

#ifdef __linux__
#define REDZONE_SIZE    0
#elif defined(__APPLE__)
#define REDZONE_SIZE	128
#else
#error "Not implemented."
#endif
#define USE_MONO_CTX
#define ARCH_NUM_REGS 31

#elif defined(__mips__)

#define REDZONE_SIZE	0

#define USE_MONO_CTX
#define ARCH_NUM_REGS 32

#elif defined(__s390x__)

#define REDZONE_SIZE	0

#define USE_MONO_CTX
#define ARCH_NUM_REGS 16	

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

#ifndef REG_SP
#define REG_SP REG_O6
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
