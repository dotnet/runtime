/*
 * mono-context.c: plat independent machine state definitions
 *
 *
 * Copyright (c) 2011 Novell, Inc (http://www.novell.com)
 */

#include <mono/utils/mono-sigcontext.h>

#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#if defined(__i386__)

#include <mono/utils/mono-context.h>

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
#if defined (__native_client__)
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
#elif defined(MONO_SIGNAL_USE_SIGACTION)
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
#endif /* if defined(__native_client__) */
}

void
mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
#if defined(__native_client__)
	printf("WARNING: mono_arch_monoctx_to_sigctx() called!\n");
#elif defined(MONO_SIGNAL_USE_SIGACTION)
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
#endif /* __native_client__ */
}

#endif /* #if defined(__i386__) */
