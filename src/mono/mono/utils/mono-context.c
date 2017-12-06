/**
 * \file
 * plat independent machine state definitions
 *
 *
 * Copyright (c) 2011 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <mono/utils/mono-sigcontext.h>

#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#if (defined(__i386__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_X86))

#include <mono/utils/mono-context.h>

#ifdef HOST_WIN32
#include <windows.h>
#endif

#ifdef __sun
#define REG_EAX EAX
#define REG_EBX EBX
#define REG_ECX ECX
#define REG_EDX EDX
#define REG_EBP EBP
#define REG_ESP ESP
#define REG_ESI ESI
#define REG_EDI EDI
#define REG_EIP EIP
#endif

void
mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#if defined (HOST_WATCHOS)
	printf("WARNING: mono_arch_sigctx_to_monoctx() called!\n");
	mctx->eax = 0xDEADBEEF;
	mctx->ebx = 0xDEADBEEF;
	mctx->ecx = 0xDEADBEEF;
	mctx->edx = 0xDEADBEEF;
	mctx->ebp = 0xDEADBEEF;
	mctx->esp = 0xDEADBEEF;
	mctx->esi = 0xDEADBEEF;
	mctx->edi = 0xDEADBEEF;
	mctx->eip = 0xDEADBEEF;
#elif MONO_CROSS_COMPILE
	g_assert_not_reached ();
#elif defined(MONO_SIGNAL_USE_UCONTEXT_T)
	ucontext_t *ctx = (ucontext_t*)sigctx;
	
	mctx->eax = UCONTEXT_REG_EAX (ctx);
	mctx->ebx = UCONTEXT_REG_EBX (ctx);
	mctx->ecx = UCONTEXT_REG_ECX (ctx);
	mctx->edx = UCONTEXT_REG_EDX (ctx);
	mctx->ebp = UCONTEXT_REG_EBP (ctx);
	mctx->esp = UCONTEXT_REG_ESP (ctx);
	mctx->esi = UCONTEXT_REG_ESI (ctx);
	mctx->edi = UCONTEXT_REG_EDI (ctx);
	mctx->eip = UCONTEXT_REG_EIP (ctx);
#ifdef UCONTEXT_HAS_XMM
	if (UCONTEXT_HAS_XMM (ctx)) {
		mctx->fregs [0] = UCONTEXT_REG_XMM0 (ctx);
		mctx->fregs [1] = UCONTEXT_REG_XMM1 (ctx);
		mctx->fregs [2] = UCONTEXT_REG_XMM2 (ctx);
		mctx->fregs [3] = UCONTEXT_REG_XMM3 (ctx);
		mctx->fregs [4] = UCONTEXT_REG_XMM4 (ctx);
		mctx->fregs [5] = UCONTEXT_REG_XMM5 (ctx);
		mctx->fregs [6] = UCONTEXT_REG_XMM6 (ctx);
		mctx->fregs [7] = UCONTEXT_REG_XMM7 (ctx);
	}
#endif
#elif defined(HOST_WIN32)
	CONTEXT *context = (CONTEXT*)sigctx;

	mctx->eip = context->Eip;
	mctx->edi = context->Edi;
	mctx->esi = context->Esi;
	mctx->ebx = context->Ebx;
	mctx->edx = context->Edx;
	mctx->ecx = context->Ecx;
	mctx->eax = context->Eax;
	mctx->ebp = context->Ebp;
	mctx->esp = context->Esp;
#else	
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	mctx->eax = ctx->SC_EAX;
	mctx->ebx = ctx->SC_EBX;
	mctx->ecx = ctx->SC_ECX;
	mctx->edx = ctx->SC_EDX;
	mctx->ebp = ctx->SC_EBP;
	mctx->esp = ctx->SC_ESP;
	mctx->esi = ctx->SC_ESI;
	mctx->edi = ctx->SC_EDI;
	mctx->eip = ctx->SC_EIP;
#endif
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
#if defined(HOST_WATCHOS)
	printf("WARNING: mono_arch_monoctx_to_sigctx() called!\n");
#elif MONO_CROSS_COMPILE
	g_assert_not_reached ();
#elif defined(MONO_SIGNAL_USE_UCONTEXT_T)
	ucontext_t *ctx = (ucontext_t*)sigctx;

	UCONTEXT_REG_EAX (ctx) = mctx->eax;
	UCONTEXT_REG_EBX (ctx) = mctx->ebx;
	UCONTEXT_REG_ECX (ctx) = mctx->ecx;
	UCONTEXT_REG_EDX (ctx) = mctx->edx;
	UCONTEXT_REG_EBP (ctx) = mctx->ebp;
	UCONTEXT_REG_ESP (ctx) = mctx->esp;
	UCONTEXT_REG_ESI (ctx) = mctx->esi;
	UCONTEXT_REG_EDI (ctx) = mctx->edi;
	UCONTEXT_REG_EIP (ctx) = mctx->eip;
#ifdef UCONTEXT_HAS_XMM
	if (UCONTEXT_HAS_XMM (ctx)) {
		UCONTEXT_REG_XMM0 (ctx) = mctx->fregs [0];
		UCONTEXT_REG_XMM1 (ctx) = mctx->fregs [1];
		UCONTEXT_REG_XMM2 (ctx) = mctx->fregs [2];
		UCONTEXT_REG_XMM3 (ctx) = mctx->fregs [3];
		UCONTEXT_REG_XMM4 (ctx) = mctx->fregs [4];
		UCONTEXT_REG_XMM5 (ctx) = mctx->fregs [5];
		UCONTEXT_REG_XMM6 (ctx) = mctx->fregs [6];
		UCONTEXT_REG_XMM7 (ctx) = mctx->fregs [7];
	}
#endif
#elif defined(HOST_WIN32)
	CONTEXT *context = (CONTEXT*)sigctx;

	context->Eip = mctx->eip;
	context->Edi = mctx->edi;
	context->Esi = mctx->esi;
	context->Ebx = mctx->ebx;
	context->Edx = mctx->edx;
	context->Ecx = mctx->ecx;
	context->Eax = mctx->eax;
	context->Ebp = mctx->ebp;
	context->Esp = mctx->esp;
#else
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	ctx->SC_EAX = mctx->eax;
	ctx->SC_EBX = mctx->ebx;
	ctx->SC_ECX = mctx->ecx;
	ctx->SC_EDX = mctx->edx;
	ctx->SC_EBP = mctx->ebp;
	ctx->SC_ESP = mctx->esp;
	ctx->SC_ESI = mctx->esi;
	ctx->SC_EDI = mctx->edi;
	ctx->SC_EIP = mctx->eip;
#endif
}

#elif (defined(__x86_64__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_AMD64)) /* defined(__i386__) */

#include <mono/utils/mono-context.h>

#ifdef HOST_WIN32
#include <windows.h>
#endif

void
mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#elif defined(MONO_SIGNAL_USE_UCONTEXT_T)
	ucontext_t *ctx = (ucontext_t*)sigctx;

	mctx->gregs [AMD64_RAX] = UCONTEXT_REG_RAX (ctx);
	mctx->gregs [AMD64_RBX] = UCONTEXT_REG_RBX (ctx);
	mctx->gregs [AMD64_RCX] = UCONTEXT_REG_RCX (ctx);
	mctx->gregs [AMD64_RDX] = UCONTEXT_REG_RDX (ctx);
	mctx->gregs [AMD64_RBP] = UCONTEXT_REG_RBP (ctx);
	mctx->gregs [AMD64_RSP] = UCONTEXT_REG_RSP (ctx);
	mctx->gregs [AMD64_RSI] = UCONTEXT_REG_RSI (ctx);
	mctx->gregs [AMD64_RDI] = UCONTEXT_REG_RDI (ctx);
	mctx->gregs [AMD64_R8] = UCONTEXT_REG_R8 (ctx);
	mctx->gregs [AMD64_R9] = UCONTEXT_REG_R9 (ctx);
	mctx->gregs [AMD64_R10] = UCONTEXT_REG_R10 (ctx);
	mctx->gregs [AMD64_R11] = UCONTEXT_REG_R11 (ctx);
	mctx->gregs [AMD64_R12] = UCONTEXT_REG_R12 (ctx);
	mctx->gregs [AMD64_R13] = UCONTEXT_REG_R13 (ctx);
	mctx->gregs [AMD64_R14] = UCONTEXT_REG_R14 (ctx);
	mctx->gregs [AMD64_R15] = UCONTEXT_REG_R15 (ctx);
	mctx->gregs [AMD64_RIP] = UCONTEXT_REG_RIP (ctx);

#ifdef UCONTEXT_HAS_XMM
	if (UCONTEXT_HAS_XMM (ctx)) {
		mctx->fregs [0] = UCONTEXT_REG_XMM0 (ctx);
		mctx->fregs [1] = UCONTEXT_REG_XMM1 (ctx);
		mctx->fregs [2] = UCONTEXT_REG_XMM2 (ctx);
		mctx->fregs [3] = UCONTEXT_REG_XMM3 (ctx);
		mctx->fregs [4] = UCONTEXT_REG_XMM4 (ctx);
		mctx->fregs [5] = UCONTEXT_REG_XMM5 (ctx);
		mctx->fregs [6] = UCONTEXT_REG_XMM6 (ctx);
		mctx->fregs [7] = UCONTEXT_REG_XMM7 (ctx);
		mctx->fregs [8] = UCONTEXT_REG_XMM8 (ctx);
		mctx->fregs [9] = UCONTEXT_REG_XMM9 (ctx);
		mctx->fregs [10] = UCONTEXT_REG_XMM10 (ctx);
		mctx->fregs [11] = UCONTEXT_REG_XMM11 (ctx);
		mctx->fregs [12] = UCONTEXT_REG_XMM12 (ctx);
		mctx->fregs [13] = UCONTEXT_REG_XMM13 (ctx);
		mctx->fregs [14] = UCONTEXT_REG_XMM14 (ctx);
		mctx->fregs [15] = UCONTEXT_REG_XMM15 (ctx);
	}
#endif

#elif defined(HOST_WIN32)
	CONTEXT *context = (CONTEXT*)sigctx;

	mctx->gregs [AMD64_RIP] = context->Rip;
	mctx->gregs [AMD64_RAX] = context->Rax;
	mctx->gregs [AMD64_RCX] = context->Rcx;
	mctx->gregs [AMD64_RDX] = context->Rdx;
	mctx->gregs [AMD64_RBX] = context->Rbx;
	mctx->gregs [AMD64_RSP] = context->Rsp;
	mctx->gregs [AMD64_RBP] = context->Rbp;
	mctx->gregs [AMD64_RSI] = context->Rsi;
	mctx->gregs [AMD64_RDI] = context->Rdi;
	mctx->gregs [AMD64_R8] = context->R8;
	mctx->gregs [AMD64_R9] = context->R9;
	mctx->gregs [AMD64_R10] = context->R10;
	mctx->gregs [AMD64_R11] = context->R11;
	mctx->gregs [AMD64_R12] = context->R12;
	mctx->gregs [AMD64_R13] = context->R13;
	mctx->gregs [AMD64_R14] = context->R14;
	mctx->gregs [AMD64_R15] = context->R15;
#elif defined(__HAIKU__)
	// Haiku uses sigcontext because there's no ucontext
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	mctx->gregs [AMD64_RIP] = ctx->regs.rip;
	mctx->gregs [AMD64_RAX] = ctx->regs.rax;
	mctx->gregs [AMD64_RCX] = ctx->regs.rcx;
	mctx->gregs [AMD64_RDX] = ctx->regs.rdx;
	mctx->gregs [AMD64_RBX] = ctx->regs.rbx;
	mctx->gregs [AMD64_RSP] = ctx->regs.rsp;
	mctx->gregs [AMD64_RBP] = ctx->regs.rbp;
	mctx->gregs [AMD64_RSI] = ctx->regs.rsi;
	mctx->gregs [AMD64_RDI] = ctx->regs.rdi;
	mctx->gregs [AMD64_R8] = ctx->regs.r8;
	mctx->gregs [AMD64_R9] = ctx->regs.r9;
	mctx->gregs [AMD64_R10] = ctx->regs.r10;
	mctx->gregs [AMD64_R11] = ctx->regs.r11;
	mctx->gregs [AMD64_R12] = ctx->regs.r12;
	mctx->gregs [AMD64_R13] = ctx->regs.r13;
	mctx->gregs [AMD64_R14] = ctx->regs.r14;
	mctx->gregs [AMD64_R15] = ctx->regs.r15;
#else
	g_assert_not_reached ();
#endif
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#elif defined(MONO_SIGNAL_USE_UCONTEXT_T)
	ucontext_t *ctx = (ucontext_t*)sigctx;

	UCONTEXT_REG_RAX (ctx) = mctx->gregs [AMD64_RAX];
	UCONTEXT_REG_RBX (ctx) = mctx->gregs [AMD64_RBX];
	UCONTEXT_REG_RCX (ctx) = mctx->gregs [AMD64_RCX];
	UCONTEXT_REG_RDX (ctx) = mctx->gregs [AMD64_RDX];
	UCONTEXT_REG_RBP (ctx) = mctx->gregs [AMD64_RBP];
	UCONTEXT_REG_RSP (ctx) = mctx->gregs [AMD64_RSP];
	UCONTEXT_REG_RSI (ctx) = mctx->gregs [AMD64_RSI];
	UCONTEXT_REG_RDI (ctx) = mctx->gregs [AMD64_RDI];
	UCONTEXT_REG_R8 (ctx) = mctx->gregs [AMD64_R8];
	UCONTEXT_REG_R9 (ctx) = mctx->gregs [AMD64_R9];
	UCONTEXT_REG_R10 (ctx) = mctx->gregs [AMD64_R10];
	UCONTEXT_REG_R11 (ctx) = mctx->gregs [AMD64_R11];
	UCONTEXT_REG_R12 (ctx) = mctx->gregs [AMD64_R12];
	UCONTEXT_REG_R13 (ctx) = mctx->gregs [AMD64_R13];
	UCONTEXT_REG_R14 (ctx) = mctx->gregs [AMD64_R14];
	UCONTEXT_REG_R15 (ctx) = mctx->gregs [AMD64_R15];
	UCONTEXT_REG_RIP (ctx) = mctx->gregs [AMD64_RIP];

#ifdef UCONTEXT_HAS_XMM
	if (UCONTEXT_HAS_XMM (ctx)) {
		UCONTEXT_REG_XMM0 (ctx) = mctx->fregs [0];
		UCONTEXT_REG_XMM1 (ctx) = mctx->fregs [1];
		UCONTEXT_REG_XMM2 (ctx) = mctx->fregs [2];
		UCONTEXT_REG_XMM3 (ctx) = mctx->fregs [3];
		UCONTEXT_REG_XMM4 (ctx) = mctx->fregs [4];
		UCONTEXT_REG_XMM5 (ctx) = mctx->fregs [5];
		UCONTEXT_REG_XMM6 (ctx) = mctx->fregs [6];
		UCONTEXT_REG_XMM7 (ctx) = mctx->fregs [7];
		UCONTEXT_REG_XMM8 (ctx) = mctx->fregs [8];
		UCONTEXT_REG_XMM9 (ctx) = mctx->fregs [9];
		UCONTEXT_REG_XMM10 (ctx) = mctx->fregs [10];
		UCONTEXT_REG_XMM11 (ctx) = mctx->fregs [11];
		UCONTEXT_REG_XMM12 (ctx) = mctx->fregs [12];
		UCONTEXT_REG_XMM13 (ctx) = mctx->fregs [13];
		UCONTEXT_REG_XMM14 (ctx) = mctx->fregs [14];
		UCONTEXT_REG_XMM15 (ctx) = mctx->fregs [15];
	}
#endif

#elif defined(HOST_WIN32)
	CONTEXT *context = (CONTEXT*)sigctx;

	context->Rip = mctx->gregs [AMD64_RIP];
	context->Rax = mctx->gregs [AMD64_RAX];
	context->Rcx = mctx->gregs [AMD64_RCX];
	context->Rdx = mctx->gregs [AMD64_RDX];
	context->Rbx = mctx->gregs [AMD64_RBX];
	context->Rsp = mctx->gregs [AMD64_RSP];
	context->Rbp = mctx->gregs [AMD64_RBP];
	context->Rsi = mctx->gregs [AMD64_RSI];
	context->Rdi = mctx->gregs [AMD64_RDI];
	context->R8 = mctx->gregs [AMD64_R8];
	context->R9 = mctx->gregs [AMD64_R9];
	context->R10 = mctx->gregs [AMD64_R10];
	context->R11 = mctx->gregs [AMD64_R11];
	context->R12 = mctx->gregs [AMD64_R12];
	context->R13 = mctx->gregs [AMD64_R13];
	context->R14 = mctx->gregs [AMD64_R14];
	context->R15 = mctx->gregs [AMD64_R15];
#elif defined(__HAIKU__)
	// Haiku uses sigcontext because there's no ucontext
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	ctx->regs.rip = mctx->gregs [AMD64_RIP];
	ctx->regs.rax = mctx->gregs [AMD64_RAX];
	ctx->regs.rcx = mctx->gregs [AMD64_RCX];
	ctx->regs.rdx = mctx->gregs [AMD64_RDX];
	ctx->regs.rbx = mctx->gregs [AMD64_RBX];
	ctx->regs.rsp = mctx->gregs [AMD64_RSP];
	ctx->regs.rbp = mctx->gregs [AMD64_RBP];
	ctx->regs.rsi = mctx->gregs [AMD64_RSI];
	ctx->regs.rdi = mctx->gregs [AMD64_RDI];
	ctx->regs.r8 = mctx->gregs [AMD64_R8];
	ctx->regs.r9 = mctx->gregs [AMD64_R9];
	ctx->regs.r10 = mctx->gregs [AMD64_R10];
	ctx->regs.r11 = mctx->gregs [AMD64_R11];
	ctx->regs.r12 = mctx->gregs [AMD64_R12];
	ctx->regs.r13 = mctx->gregs [AMD64_R13];
	ctx->regs.r14 = mctx->gregs [AMD64_R14];
	ctx->regs.r15 = mctx->gregs [AMD64_R15];
#else
	g_assert_not_reached ();
#endif
}

#elif defined(__s390x__)

#include <mono/utils/mono-context.h>

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_sigctx_to_monoctx.                      */
/*                                                                  */
/* Function	- Called from the signal handler to convert signal  */
/*                context to MonoContext.                           */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_sigctx_to_monoctx (void *ctx, MonoContext *mctx)
{
	memcpy (mctx, ctx, sizeof(MonoContext));
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_monoctx_to_sigctx.                      */
/*                                                                  */
/* Function	- Convert MonoContext structure to signal context.  */
/*                                                                  */
/*------------------------------------------------------------------*/

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *ctx)
{
	memcpy (ctx, mctx, sizeof(MonoContext));
}

/*========================= End of Function ========================*/

#elif (defined(__arm__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_ARM))

#include <mono/utils/mono-context.h>
#include <mono/arch/arm/arm-codegen.h>
#include <mono/arch/arm/arm-vfp-codegen.h>

void
mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#else
	arm_ucontext *my_uc = sigctx;

	mctx->pc = UCONTEXT_REG_PC (my_uc);
	mctx->regs [ARMREG_SP] = UCONTEXT_REG_SP (my_uc);
	mctx->cpsr = UCONTEXT_REG_CPSR (my_uc);
	memcpy (&mctx->regs, &UCONTEXT_REG_R0 (my_uc), sizeof (mgreg_t) * 16);
#ifdef UCONTEXT_REG_VFPREGS
	memcpy (&mctx->fregs, UCONTEXT_REG_VFPREGS (my_uc), sizeof (double) * 16);
#endif
#endif
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *ctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#else
	arm_ucontext *my_uc = ctx;

	UCONTEXT_REG_PC (my_uc) = mctx->pc;
	UCONTEXT_REG_SP (my_uc) = mctx->regs [ARMREG_SP];
	UCONTEXT_REG_CPSR (my_uc) = mctx->cpsr;
	/* The upper registers are not guaranteed to be valid */
	memcpy (&UCONTEXT_REG_R0 (my_uc), &mctx->regs, sizeof (mgreg_t) * 12);
#ifdef UCONTEXT_REG_VFPREGS
	memcpy (UCONTEXT_REG_VFPREGS (my_uc), &mctx->fregs, sizeof (double) * 16);
#endif
#endif
}

#elif (defined(__aarch64__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_ARM64))

#include <mono/utils/mono-context.h>

void
mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#else
	memcpy (mctx->regs, UCONTEXT_GREGS (sigctx), sizeof (mgreg_t) * 31);
	mctx->pc = UCONTEXT_REG_PC (sigctx);
	mctx->regs [ARMREG_SP] = UCONTEXT_REG_SP (sigctx);
#ifdef __linux__
	struct fpsimd_context *fpctx = (struct fpsimd_context*)&((ucontext_t*)sigctx)->uc_mcontext.__reserved;
	int i;

	g_assert (fpctx->head.magic == FPSIMD_MAGIC);
	for (i = 0; i < 32; ++i)
		mctx->fregs [i] = fpctx->vregs [i];
#endif
	/* FIXME: apple */
#endif
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#else
	memcpy (UCONTEXT_GREGS (sigctx), mctx->regs, sizeof (mgreg_t) * 31);
	UCONTEXT_REG_PC (sigctx) = mctx->pc;
	UCONTEXT_REG_SP (sigctx) = mctx->regs [ARMREG_SP];
#endif
}

#elif (defined(__mips__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_MIPS))

#include <mono/utils/mono-context.h>
#include <mono/arch/mips/mips-codegen.h>

void
mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
	int i;

	mctx->sc_pc = UCONTEXT_REG_PC (sigctx);
	for (i = 0; i < 32; ++i) {
		mctx->sc_regs[i] = UCONTEXT_GREGS (sigctx) [i];
		mctx->sc_fpregs[i] = UCONTEXT_FPREGS (sigctx) [i];
	}
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
	int i;

	UCONTEXT_REG_PC (sigctx) = mctx->sc_pc;
	for (i = 0; i < 32; ++i) {
		UCONTEXT_GREGS (sigctx) [i] = mctx->sc_regs[i];
		UCONTEXT_FPREGS (sigctx) [i] = mctx->sc_fpregs[i];
	}
}

#elif (((defined(__ppc__) || defined(__powerpc__) || defined(__ppc64__)) && !defined(MONO_CROSS_COMPILE))) || (defined(TARGET_POWERPC))

#include <mono/utils/mono-context.h>
#include <mono/mini/mini-ppc.h>

void
mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
	os_ucontext *uc = sigctx;

	mctx->sc_ir = UCONTEXT_REG_NIP(uc);
	mctx->sc_sp = UCONTEXT_REG_Rn(uc, 1);

	memcpy (&mctx->regs, &UCONTEXT_REG_Rn(uc, 0), sizeof (mgreg_t) * MONO_MAX_IREGS);
	memcpy (&mctx->fregs, &UCONTEXT_REG_FPRn(uc, 0), sizeof (double) * MONO_MAX_FREGS);
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
	os_ucontext *uc = sigctx;

	memcpy (&UCONTEXT_REG_Rn(uc, 0), &mctx->regs, sizeof (mgreg_t) * MONO_MAX_IREGS);
	memcpy (&UCONTEXT_REG_FPRn(uc, 0), &mctx->fregs, sizeof (double) * MONO_MAX_FREGS);

	/* The valid values for pc and sp are stored here and not in regs array */
	UCONTEXT_REG_NIP(uc) = mctx->sc_ir;
	UCONTEXT_REG_Rn(uc, 1) = mctx->sc_sp;
}

#endif /* #if defined(__i386__) */
