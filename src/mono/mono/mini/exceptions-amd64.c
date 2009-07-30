/*
 * exceptions-amd64.c: exception support for AMD64
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#include <mono/arch/amd64/amd64-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-mmap.h>

#include "mini.h"
#include "mini-amd64.h"
#include "tasklets.h"
#include "debug-mini.h"

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

#ifdef PLATFORM_WIN32
static MonoW32ExceptionHandler fpe_handler;
static MonoW32ExceptionHandler ill_handler;
static MonoW32ExceptionHandler segv_handler;

static LPTOP_LEVEL_EXCEPTION_FILTER old_handler;

#define W32_SEH_HANDLE_EX(_ex) \
	if (_ex##_handler) _ex##_handler((int)sctx)

/*
 * Unhandled Exception Filter
 * Top-level per-process exception handler.
 */
LONG CALLBACK seh_handler(EXCEPTION_POINTERS* ep)
{
	EXCEPTION_RECORD* er;
	CONTEXT* ctx;
	MonoContext* sctx;
	LONG res;

	res = EXCEPTION_CONTINUE_EXECUTION;

	er = ep->ExceptionRecord;
	ctx = ep->ContextRecord;
	sctx = g_malloc(sizeof(MonoContext));

	/* Copy Win32 context to UNIX style context */
	sctx->rax = ctx->Rax;
	sctx->rbx = ctx->Rbx;
	sctx->rcx = ctx->Rcx;
	sctx->rdx = ctx->Rdx;
	sctx->rbp = ctx->Rbp;
	sctx->rsp = ctx->Rsp;
	sctx->rsi = ctx->Rsi;
	sctx->rdi = ctx->Rdi;
	sctx->rip = ctx->Rip;
	sctx->r12 = ctx->R12;
	sctx->r13 = ctx->R13;
	sctx->r14 = ctx->R14;
	sctx->r15 = ctx->R15;

	switch (er->ExceptionCode) {
	case EXCEPTION_ACCESS_VIOLATION:
		W32_SEH_HANDLE_EX(segv);
		break;
	case EXCEPTION_ILLEGAL_INSTRUCTION:
		W32_SEH_HANDLE_EX(ill);
		break;
	case EXCEPTION_INT_DIVIDE_BY_ZERO:
	case EXCEPTION_INT_OVERFLOW:
	case EXCEPTION_FLT_DIVIDE_BY_ZERO:
	case EXCEPTION_FLT_OVERFLOW:
	case EXCEPTION_FLT_UNDERFLOW:
	case EXCEPTION_FLT_INEXACT_RESULT:
		W32_SEH_HANDLE_EX(fpe);
		break;
	default:
		break;
	}

	/* Copy context back */
	/* Nonvolatile */
	ctx->Rsp = sctx->rsp; 
	ctx->Rdi = sctx->rdi; 
	ctx->Rsi = sctx->rsi; 
	ctx->Rbx = sctx->rbx; 
	ctx->Rbp = sctx->rbp;
	ctx->R12 = sctx->r12; 
	ctx->R13 = sctx->r13; 
	ctx->R14 = sctx->r14;
	ctx->R15 = sctx->r15;
	ctx->Rip = sctx->rip; 

	/* Volatile But should not matter?*/
	ctx->Rax = sctx->rax; 
	ctx->Rcx = sctx->rcx; 
	ctx->Rdx = sctx->rdx;

	g_free (sctx);

	return res;
}

void win32_seh_init()
{
	old_handler = SetUnhandledExceptionFilter(seh_handler);
}

void win32_seh_cleanup()
{
	if (old_handler) SetUnhandledExceptionFilter(old_handler);
}

void win32_seh_set_handler(int type, MonoW32ExceptionHandler handler)
{
	switch (type) {
	case SIGFPE:
		fpe_handler = handler;
		break;
	case SIGILL:
		ill_handler = handler;
		break;
	case SIGSEGV:
		segv_handler = handler;
		break;
	default:
		break;
	}
}

#endif /* PLATFORM_WIN32 */

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *start = NULL;
	guint8 *code;

	/* restore_contect (MonoContext *ctx) */

	*ji = NULL;

	start = code = mono_global_codeman_reserve (256);

	amd64_mov_reg_reg (code, AMD64_R11, AMD64_ARG_REG1, 8);

	/* Restore all registers except %rip and %r11 */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rax), 8);
	amd64_mov_reg_membase (code, AMD64_RCX, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rcx), 8);
	amd64_mov_reg_membase (code, AMD64_RDX, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rdx), 8);
	amd64_mov_reg_membase (code, AMD64_RBX, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rbx), 8);
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rbp), 8);
	amd64_mov_reg_membase (code, AMD64_RSI, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rsi), 8);
	amd64_mov_reg_membase (code, AMD64_RDI, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rdi), 8);
	//amd64_mov_reg_membase (code, AMD64_R8, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r8), 8);
	//amd64_mov_reg_membase (code, AMD64_R9, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r9), 8);
	//amd64_mov_reg_membase (code, AMD64_R10, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r10), 8);
	amd64_mov_reg_membase (code, AMD64_R12, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r12), 8);
	amd64_mov_reg_membase (code, AMD64_R13, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r13), 8);
	amd64_mov_reg_membase (code, AMD64_R14, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r14), 8);
	amd64_mov_reg_membase (code, AMD64_R15, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, r15), 8);

	if (mono_running_on_valgrind ()) {
		/* Prevent 'Address 0x... is just below the stack ptr.' errors */
		amd64_mov_reg_membase (code, AMD64_R8, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rsp), 8);
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rip), 8);
		amd64_mov_reg_reg (code, AMD64_RSP, AMD64_R8, 8);
	} else {
		amd64_mov_reg_membase (code, AMD64_RSP, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rsp), 8);
		/* get return address */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11,  G_STRUCT_OFFSET (MonoContext, rip), 8);
	}

	/* jump to the saved IP */
	amd64_jump_reg (code, AMD64_R11);

	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

	return start;
}

/*
 * mono_arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
gpointer
mono_arch_get_call_filter_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *start;
	int i;
	guint8 *code;
	guint32 pos;

	*ji = NULL;

	start = code = mono_global_codeman_reserve (128);

	/* call_filter (MonoContext *ctx, unsigned long eip) */
	code = start;

	/* Alloc new frame */
	amd64_push_reg (code, AMD64_RBP);
	amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, 8);

	/* Save callee saved regs */
	pos = 0;
	for (i = 0; i < AMD64_NREG; ++i)
		if (AMD64_IS_CALLEE_SAVED_REG (i)) {
			amd64_push_reg (code, i);
			pos += 8;
		}

	/* Save EBP */
	pos += 8;
	amd64_push_reg (code, AMD64_RBP);

	/* Make stack misaligned, the call will make it aligned again */
	if (! (pos & 8))
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);

	/* set new EBP */
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoContext, rbp), 8);
	/* load callee saved regs */
	amd64_mov_reg_membase (code, AMD64_RBX, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoContext, rbx), 8);
	amd64_mov_reg_membase (code, AMD64_R12, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoContext, r12), 8);
	amd64_mov_reg_membase (code, AMD64_R13, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoContext, r13), 8);
	amd64_mov_reg_membase (code, AMD64_R14, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoContext, r14), 8);
	amd64_mov_reg_membase (code, AMD64_R15, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoContext, r15), 8);
#ifdef PLATFORM_WIN32
	amd64_mov_reg_membase (code, AMD64_RDI, AMD64_ARG_REG1,  G_STRUCT_OFFSET (MonoContext, rdi), 8);
	amd64_mov_reg_membase (code, AMD64_RSI, AMD64_ARG_REG1,  G_STRUCT_OFFSET (MonoContext, rsi), 8);
#endif

	/* call the handler */
	amd64_call_reg (code, AMD64_ARG_REG2);

	if (! (pos & 8))
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);

	/* restore RBP */
	amd64_pop_reg (code, AMD64_RBP);

	/* Restore callee saved regs */
	for (i = AMD64_NREG; i >= 0; --i)
		if (AMD64_IS_CALLEE_SAVED_REG (i))
			amd64_pop_reg (code, i);

	amd64_leave (code);
	amd64_ret (code);

	g_assert ((code - start) < 128);

	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

	return start;
}

/* 
 * The first few arguments are dummy, to force the other arguments to be passed on
 * the stack, this avoids overwriting the argument registers in the throw trampoline.
 */
void
mono_amd64_throw_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
							guint64 dummy5, guint64 dummy6,
							MonoObject *exc, guint64 rip, guint64 rsp,
							guint64 rbx, guint64 rbp, guint64 r12, guint64 r13, 
							guint64 r14, guint64 r15, guint64 rdi, guint64 rsi, 
							guint64 rax, guint64 rcx, guint64 rdx,
							guint64 rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	ctx.rsp = rsp;
	ctx.rip = rip;
	ctx.rbx = rbx;
	ctx.rbp = rbp;
	ctx.r12 = r12;
	ctx.r13 = r13;
	ctx.r14 = r14;
	ctx.r15 = r15;
	ctx.rdi = rdi;
	ctx.rsi = rsi;
	ctx.rax = rax;
	ctx.rcx = rcx;
	ctx.rdx = rdx;

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}

	if (mono_debug_using_mono_debugger ()) {
		guint8 buf [16], *code;

		mono_breakpoint_clean_code (NULL, (gpointer)rip, 8, buf, sizeof (buf));
		code = buf + 8;

		if (buf [3] == 0xe8) {
			MonoContext ctx_cp = ctx;
			ctx_cp.rip = rip - 5;

			if (mono_debugger_handle_exception (&ctx_cp, exc)) {
				restore_context (&ctx_cp);
				g_assert_not_reached ();
			}
		}
	}

	/* adjust eip so that it point into the call instruction */
	ctx.rip -= 1;

	mono_handle_exception (&ctx, exc, (gpointer)rip, FALSE);
	restore_context (&ctx);

	g_assert_not_reached ();
}

static gpointer
get_throw_trampoline (gboolean rethrow, guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8* start;
	guint8 *code;

	start = code = mono_global_codeman_reserve (64);

	code = start;

	*ji = NULL;

	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RSP, 8);

	/* reverse order */
	amd64_push_imm (code, rethrow);
	amd64_push_reg (code, AMD64_RDX);
	amd64_push_reg (code, AMD64_RCX);
	amd64_push_reg (code, AMD64_RAX);
	amd64_push_reg (code, AMD64_RSI);
	amd64_push_reg (code, AMD64_RDI);
	amd64_push_reg (code, AMD64_R15);
	amd64_push_reg (code, AMD64_R14);
	amd64_push_reg (code, AMD64_R13);
	amd64_push_reg (code, AMD64_R12);
	amd64_push_reg (code, AMD64_RBP);
	amd64_push_reg (code, AMD64_RBX);

	/* SP */
	amd64_lea_membase (code, AMD64_RAX, AMD64_R11, 8);
	amd64_push_reg (code, AMD64_RAX);

	/* IP */
	amd64_push_membase (code, AMD64_R11, 0);

	/* Exception */
	amd64_push_reg (code, AMD64_ARG_REG1);

#ifdef PLATFORM_WIN32
	/* align stack */
	amd64_push_imm (code, 0);
	amd64_push_imm (code, 0);
	amd64_push_imm (code, 0);
	amd64_push_imm (code, 0);
	amd64_push_imm (code, 0);
	amd64_push_imm (code, 0);
#endif

	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_amd64_throw_exception");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_amd64_throw_exception);
	}
	amd64_call_reg (code, AMD64_R11);
	amd64_breakpoint (code);

	mono_arch_flush_icache (start, code - start);

	g_assert ((code - start) < 64);

	*code_size = code - start;

	return start;
}

/**
 * mono_arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
 */
gpointer 
mono_arch_get_throw_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return get_throw_trampoline (FALSE, code_size, ji, aot);
}

gpointer 
mono_arch_get_rethrow_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return get_throw_trampoline (TRUE, code_size, ji, aot);
}

gpointer 
mono_arch_get_throw_exception_by_name_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{	
	guint8* start;
	guint8 *code;

	start = code = mono_global_codeman_reserve (64);

	*ji = NULL;

	/* Not used on amd64 */
	amd64_breakpoint (code);

	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

	return start;
}

/**
 * mono_arch_get_throw_corlib_exception:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * Here, offset is the offset which needs to be substracted from the caller IP 
 * to get the IP of the throw. Passing the offset has the advantage that it 
 * needs no relocations in the caller.
 */
gpointer 
mono_arch_get_throw_corlib_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	static guint8* start;
	guint8 *code;
	guint64 throw_ex;

	start = code = mono_global_codeman_reserve (64);

	*ji = NULL;

	/* Push throw_ip */
	amd64_push_reg (code, AMD64_ARG_REG2);

	/* Call exception_from_token */
	amd64_mov_reg_reg (code, AMD64_ARG_REG2, AMD64_ARG_REG1, 8);
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_IMAGE, mono_defaults.exception_class->image);
		amd64_mov_reg_membase (code, AMD64_ARG_REG1, AMD64_RIP, 0, 8);
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_exception_from_token");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_ARG_REG1, mono_defaults.exception_class->image);
		amd64_mov_reg_imm (code, AMD64_R11, mono_exception_from_token);
	}
#ifdef PLATFORM_WIN32
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 32);
#endif
	amd64_call_reg (code, AMD64_R11);
#ifdef PLATFORM_WIN32
	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 32);
#endif

	/* Compute throw_ip */
	amd64_pop_reg (code, AMD64_ARG_REG2);
	/* return addr */
	amd64_pop_reg (code, AMD64_ARG_REG3);
	amd64_alu_reg_reg (code, X86_SUB, AMD64_ARG_REG3, AMD64_ARG_REG2);

	/* Put the throw_ip at the top of the misaligned stack */
	amd64_push_reg (code, AMD64_ARG_REG3);

	throw_ex = (guint64)mono_get_throw_exception ();

	/* Call throw_exception */
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_RAX, 8);
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_throw_exception");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, throw_ex);
	}
	/* The original IP is on the stack */
	amd64_jump_reg (code, AMD64_R11);

	g_assert ((code - start) < 64);

	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

	return start;
}

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, MonoLMF **lmf, gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mono_jit_info_table_find (domain, ip);

	if (managed)
		*managed = FALSE;

	*new_ctx = *ctx;

	if (ji != NULL) {
		gssize regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (ji->from_aot)
			unwind_info = mono_aot_get_unwind_info (ji, &unwind_info_len);
		else
			unwind_info = mono_get_cached_unwind_info (ji->used_regs, &unwind_info_len);
 
		regs [AMD64_RAX] = new_ctx->rax;
		regs [AMD64_RBX] = new_ctx->rbx;
		regs [AMD64_RCX] = new_ctx->rcx;
		regs [AMD64_RDX] = new_ctx->rdx;
		regs [AMD64_RBP] = new_ctx->rbp;
		regs [AMD64_RSP] = new_ctx->rsp;
		regs [AMD64_RSI] = new_ctx->rsi;
		regs [AMD64_RDI] = new_ctx->rdi;
		regs [AMD64_RIP] = new_ctx->rip;
		regs [AMD64_R12] = new_ctx->r12;
		regs [AMD64_R13] = new_ctx->r13;
		regs [AMD64_R14] = new_ctx->r14;
		regs [AMD64_R15] = new_ctx->r15;

		mono_unwind_frame (unwind_info, unwind_info_len, ji->code_start, 
						   (guint8*)ji->code_start + ji->code_size,
						   ip, regs, MONO_MAX_IREGS + 1, &cfa);

		new_ctx->rax = regs [AMD64_RAX];
		new_ctx->rbx = regs [AMD64_RBX];
		new_ctx->rcx = regs [AMD64_RCX];
		new_ctx->rdx = regs [AMD64_RDX];
		new_ctx->rbp = regs [AMD64_RBP];
		new_ctx->rsp = regs [AMD64_RSP];
		new_ctx->rsi = regs [AMD64_RSI];
		new_ctx->rdi = regs [AMD64_RDI];
		new_ctx->rip = regs [AMD64_RIP];
		new_ctx->r12 = regs [AMD64_R12];
		new_ctx->r13 = regs [AMD64_R13];
		new_ctx->r14 = regs [AMD64_R14];
		new_ctx->r15 = regs [AMD64_R15];
 
		/* The CFA becomes the new SP value */
		new_ctx->rsp = (gssize)cfa;

		/* Adjust IP */
		new_ctx->rip --;

		if (*lmf && ((*lmf) != jit_tls->first_lmf) && (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->rsp)) {
			/* remove any unused lmf */
			*lmf = (gpointer)(((guint64)(*lmf)->previous_lmf) & ~1);
		}

#ifndef MONO_AMD64_NO_PUSHES
		/* Pop arguments off the stack */
		{
			MonoJitArgumentInfo *arg_info = g_newa (MonoJitArgumentInfo, mono_method_signature (ji->method)->param_count + 1);

			guint32 stack_to_pop = mono_arch_get_argument_info (mono_method_signature (ji->method), mono_method_signature (ji->method)->param_count, arg_info);
			new_ctx->rsp += stack_to_pop;
		}
#endif

		return ji;
	} else if (*lmf) {
		guint64 rip;

		if (((guint64)(*lmf)->previous_lmf) & 1) {
			/* This LMF has the rip field set */
			rip = (*lmf)->rip;
		} else if ((*lmf)->rsp == 0) {
			/* Top LMF entry */
			return (gpointer)-1;
		} else {
			/* 
			 * The rsp field is set just before the call which transitioned to native 
			 * code. Obtain the rip from the stack.
			 */
			rip = *(guint64*)((*lmf)->rsp - sizeof (gpointer));
		}

		ji = mono_jit_info_table_find (domain, (gpointer)rip);
		if (!ji) {
			// FIXME: This can happen with multiple appdomains (bug #444383)
			return (gpointer)-1;
		}

		new_ctx->rip = rip;
		new_ctx->rbp = (*lmf)->rbp;
		new_ctx->rsp = (*lmf)->rsp;

		new_ctx->rbx = (*lmf)->rbx;
		new_ctx->r12 = (*lmf)->r12;
		new_ctx->r13 = (*lmf)->r13;
		new_ctx->r14 = (*lmf)->r14;
		new_ctx->r15 = (*lmf)->r15;
#ifdef PLATFORM_WIN32
		new_ctx->rdi = (*lmf)->rdi;
		new_ctx->rsi = (*lmf)->rsi;
#endif

		*lmf = (gpointer)(((guint64)(*lmf)->previous_lmf) & ~1);

		return ji ? ji : res;
	}

	return NULL;
}

/**
 * mono_arch_handle_exception:
 *
 * @ctx: saved processor state
 * @obj: the exception object
 */
gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;

	mono_arch_sigctx_to_monoctx (sigctx, &mctx);

	if (mono_debugger_handle_exception (&mctx, (MonoObject *)obj))
		return TRUE;

	mono_handle_exception (&mctx, obj, MONO_CONTEXT_GET_IP (&mctx), test_only);

	mono_arch_monoctx_to_sigctx (&mctx, sigctx);

	return TRUE;
}

#ifdef MONO_ARCH_USE_SIGACTION
static inline guint64*
gregs_from_ucontext (ucontext_t *ctx)
{
	return (guint64 *) UCONTEXT_GREGS (ctx);
}
#endif
void
mono_arch_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#ifdef MONO_ARCH_USE_SIGACTION
	ucontext_t *ctx = (ucontext_t*)sigctx;

    guint64 *gregs = gregs_from_ucontext (ctx);

	mctx->rax = gregs [REG_RAX];
	mctx->rbx = gregs [REG_RBX];
	mctx->rcx = gregs [REG_RCX];
	mctx->rdx = gregs [REG_RDX];
	mctx->rbp = gregs [REG_RBP];
	mctx->rsp = gregs [REG_RSP];
	mctx->rsi = gregs [REG_RSI];
	mctx->rdi = gregs [REG_RDI];
	mctx->rip = gregs [REG_RIP];
	mctx->r12 = gregs [REG_R12];
	mctx->r13 = gregs [REG_R13];
	mctx->r14 = gregs [REG_R14];
	mctx->r15 = gregs [REG_R15];
#else
	MonoContext *ctx = (MonoContext *)sigctx;

	mctx->rax = ctx->rax;
	mctx->rbx = ctx->rbx;
	mctx->rcx = ctx->rcx;
	mctx->rdx = ctx->rdx;
	mctx->rbp = ctx->rbp;
	mctx->rsp = ctx->rsp;
	mctx->rsi = ctx->rsi;
	mctx->rdi = ctx->rdi;
	mctx->rip = ctx->rip;
	mctx->r12 = ctx->r12;
	mctx->r13 = ctx->r13;
	mctx->r14 = ctx->r14;
	mctx->r15 = ctx->r15;
#endif
}

void
mono_arch_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
#ifdef MONO_ARCH_USE_SIGACTION
	ucontext_t *ctx = (ucontext_t*)sigctx;

    guint64 *gregs = gregs_from_ucontext (ctx);

	gregs [REG_RAX] = mctx->rax;
	gregs [REG_RBX] = mctx->rbx;
	gregs [REG_RCX] = mctx->rcx;
	gregs [REG_RDX] = mctx->rdx;
	gregs [REG_RBP] = mctx->rbp;
	gregs [REG_RSP] = mctx->rsp;
	gregs [REG_RSI] = mctx->rsi;
	gregs [REG_RDI] = mctx->rdi;
	gregs [REG_RIP] = mctx->rip;
	gregs [REG_R12] = mctx->r12;
	gregs [REG_R13] = mctx->r13;
	gregs [REG_R14] = mctx->r14;
	gregs [REG_R15] = mctx->r15;
#else
	MonoContext *ctx = (MonoContext *)sigctx;

	ctx->rax = mctx->rax;
	ctx->rbx = mctx->rbx;
	ctx->rcx = mctx->rcx;
	ctx->rdx = mctx->rdx;
	ctx->rbp = mctx->rbp;
	ctx->rsp = mctx->rsp;
	ctx->rsi = mctx->rsi;
	ctx->rdi = mctx->rdi;
	ctx->rip = mctx->rip;
	ctx->r12 = mctx->r12;
	ctx->r13 = mctx->r13;
	ctx->r14 = mctx->r14;
	ctx->r15 = mctx->r15;
#endif
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	
#ifdef MONO_ARCH_USE_SIGACTION

	ucontext_t *ctx = (ucontext_t*)sigctx;

    guint64 *gregs = gregs_from_ucontext (ctx);

	return (gpointer)gregs [REG_RIP];
#else
	MonoContext *ctx = sigctx;
	return (gpointer)ctx->rip;
#endif	
}

static void
restore_soft_guard_pages (void)
{
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	if (jit_tls->stack_ovf_guard_base)
		mono_mprotect (jit_tls->stack_ovf_guard_base, jit_tls->stack_ovf_guard_size, MONO_MMAP_NONE);
}

/* 
 * this function modifies mctx so that when it is restored, it
 * won't execcute starting at mctx.eip, but in a function that
 * will restore the protection on the soft-guard pages and return back to
 * continue at mctx.eip.
 */
static void
prepare_for_guard_pages (MonoContext *mctx)
{
	gpointer *sp;
	sp = (gpointer)(mctx->rsp);
	sp -= 1;
	/* the return addr */
	sp [0] = (gpointer)(mctx->rip);
	mctx->rip = (guint64)restore_soft_guard_pages;
	mctx->rsp = (guint64)sp;
}

static void
altstack_handle_and_restore (void *sigctx, gpointer obj, gboolean stack_ovf)
{
	void (*restore_context) (MonoContext *);
	MonoContext mctx;

	restore_context = mono_get_restore_context ();
	mono_arch_sigctx_to_monoctx (sigctx, &mctx);

	if (mono_debugger_handle_exception (&mctx, (MonoObject *)obj)) {
		if (stack_ovf)
			prepare_for_guard_pages (&mctx);
		restore_context (&mctx);
	}

	mono_handle_exception (&mctx, obj, MONO_CONTEXT_GET_IP (&mctx), FALSE);
	if (stack_ovf)
		prepare_for_guard_pages (&mctx);
	restore_context (&mctx);
}

void
mono_arch_handle_altstack_exception (void *sigctx, gpointer fault_addr, gboolean stack_ovf)
{
#ifdef MONO_ARCH_USE_SIGACTION
	MonoException *exc = NULL;
	ucontext_t *ctx = (ucontext_t*)sigctx;
	guint64 *gregs = gregs_from_ucontext (ctx);
	MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), (gpointer)gregs [REG_RIP]);
	gpointer *sp;
	int frame_size;

	if (stack_ovf)
		exc = mono_domain_get ()->stack_overflow_ex;
	if (!ji)
		mono_handle_native_sigsegv (SIGSEGV, sigctx);

	/* setup a call frame on the real stack so that control is returned there
	 * and exception handling can continue.
	 * The frame looks like:
	 *   ucontext struct
	 *   ...
	 *   return ip
	 * 128 is the size of the red zone
	 */
	frame_size = sizeof (ucontext_t) + sizeof (gpointer) * 4 + 128;
	frame_size += 15;
	frame_size &= ~15;
	sp = (gpointer)(gregs [REG_RSP] & ~15);
	sp = (gpointer)((char*)sp - frame_size);
	/* the arguments must be aligned */
	sp [-1] = (gpointer)gregs [REG_RIP];
	/* may need to adjust pointers in the new struct copy, depending on the OS */
	memcpy (sp + 4, ctx, sizeof (ucontext_t));
	/* at the return form the signal handler execution starts in altstack_handle_and_restore() */
	gregs [REG_RIP] = (unsigned long)altstack_handle_and_restore;
	gregs [REG_RSP] = (unsigned long)(sp - 1);
	gregs [REG_RDI] = (unsigned long)(sp + 4);
	gregs [REG_RSI] = (guint64)exc;
	gregs [REG_RDX] = stack_ovf;
#endif
}

guint64
mono_amd64_get_original_ip (void)
{
	MonoLMF *lmf = mono_get_lmf ();

	g_assert (lmf);

	/* Reset the change to previous_lmf */
	lmf->previous_lmf = (gpointer)((guint64)lmf->previous_lmf & ~1);

	return lmf->rip;
}

gpointer
mono_arch_get_throw_pending_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *code, *start;
	guint8 *br[1];
	gpointer throw_trampoline;

	*ji = NULL;

	start = code = mono_global_codeman_reserve (128);

	/* We are in the frame of a managed method after a call */
	/* 
	 * We would like to throw the pending exception in such a way that it looks to
	 * be thrown from the managed method.
	 */

	/* Save registers which might contain the return value of the call */
	amd64_push_reg (code, AMD64_RAX);
	amd64_push_reg (code, AMD64_RDX);

	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
	amd64_movsd_membase_reg (code, AMD64_RSP, 0, AMD64_XMM0);

	/* Align stack */
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);

	/* Obtain the pending exception */
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_get_and_clear_pending_exception");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_thread_get_and_clear_pending_exception);
	}
	amd64_call_reg (code, AMD64_R11);

	/* Check if it is NULL, and branch */
	amd64_alu_reg_imm (code, X86_CMP, AMD64_RAX, 0);
	br[0] = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);

	/* exc != NULL branch */

	/* Save the exc on the stack */
	amd64_push_reg (code, AMD64_RAX);
	/* Align stack */
	amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);

	/* Obtain the original ip and clear the flag in previous_lmf */
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_amd64_get_original_ip");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_amd64_get_original_ip);
	}
	amd64_call_reg (code, AMD64_R11);	

	/* Load exc */
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RSP, 8, 8);

	/* Pop saved stuff from the stack */
	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 6 * 8);

	/* Setup arguments for the throw trampoline */
	/* Exception */
	amd64_mov_reg_reg (code, AMD64_ARG_REG1, AMD64_R11, 8);
	/* The trampoline expects the caller ip to be pushed on the stack */
	amd64_push_reg (code, AMD64_RAX);

	/* Call the throw trampoline */
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_amd64_throw_exception");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		throw_trampoline = mono_get_throw_exception ();
		amd64_mov_reg_imm (code, AMD64_R11, throw_trampoline);
	}
	/* We use a jump instead of a call so we can push the original ip on the stack */
	amd64_jump_reg (code, AMD64_R11);

	/* ex == NULL branch */
	mono_amd64_patch (br [0], code);

	/* Obtain the original ip and clear the flag in previous_lmf */
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_amd64_get_original_ip");
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
	} else {
		amd64_mov_reg_imm (code, AMD64_R11, mono_amd64_get_original_ip);
	}
	amd64_call_reg (code, AMD64_R11);	
	amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, 8);

	/* Restore registers */
	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
	amd64_movsd_reg_membase (code, AMD64_XMM0, AMD64_RSP, 0);
	amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
	amd64_pop_reg (code, AMD64_RDX);
	amd64_pop_reg (code, AMD64_RAX);

	/* Return to original code */
	amd64_jump_reg (code, AMD64_R11);

	g_assert ((code - start) < 128);

	*code_size = code - start;

	return start;
}

static gpointer throw_pending_exception;

/*
 * Called when a thread receives an async exception while executing unmanaged code.
 * Instead of checking for this exception in the managed-to-native wrapper, we hijack 
 * the return address on the stack to point to a helper routine which throws the
 * exception.
 */
void
mono_arch_notify_pending_exc (void)
{
	MonoLMF *lmf = mono_get_lmf ();

	if (lmf->rsp == 0)
		/* Initial LMF */
		return;

	if ((guint64)lmf->previous_lmf & 1)
		/* Already hijacked or trampoline LMF entry */
		return;

	/* lmf->rsp is set just before making the call which transitions to unmanaged code */
	lmf->rip = *(guint64*)(lmf->rsp - 8);
	/* Signal that lmf->rip is set */
	lmf->previous_lmf = (gpointer)((guint64)lmf->previous_lmf | 1);

	*(gpointer*)(lmf->rsp - 8) = throw_pending_exception;
}

void
mono_arch_exceptions_init (void)
{
	guint32 code_size;
	MonoJumpInfo *ji;

	if (mono_aot_only) {
		throw_pending_exception = mono_aot_get_named_code ("throw_pending_exception");
	} else {
		/* Call this to avoid initialization races */
		throw_pending_exception = mono_arch_get_throw_pending_exception_full (&code_size, &ji, FALSE);
	}
}

#ifdef PLATFORM_WIN32

/*
 * The mono_arch_unwindinfo* methods are used to build and add
 * function table info for each emitted method from mono.  On Winx64
 * the seh handler will not be called if the mono methods are not
 * added to the function table.  
 *
 * We should not need to add non-volatile register info to the 
 * table since mono stores that info elsewhere. (Except for the register 
 * used for the fp.)
 */

#define MONO_MAX_UNWIND_CODES 22

typedef union _UNWIND_CODE {
    struct {
        guchar CodeOffset;
        guchar UnwindOp : 4;
        guchar OpInfo   : 4;
    };
    gushort FrameOffset;
} UNWIND_CODE, *PUNWIND_CODE;

typedef struct _UNWIND_INFO {
	guchar Version       : 3;
	guchar Flags         : 5;
	guchar SizeOfProlog;
	guchar CountOfCodes;
	guchar FrameRegister : 4;
	guchar FrameOffset   : 4;
	/* custom size for mono allowing for mono allowing for*/
	/*UWOP_PUSH_NONVOL ebp offset = 21*/
	/*UWOP_ALLOC_LARGE : requires 2 or 3 offset = 20*/
	/*UWOP_SET_FPREG : requires 2 offset = 17*/
	/*UWOP_PUSH_NONVOL offset = 15-0*/
	UNWIND_CODE UnwindCode[MONO_MAX_UNWIND_CODES]; 

/*  	UNWIND_CODE MoreUnwindCode[((CountOfCodes + 1) & ~1) - 1];
 *   	union {
 *   	    OPTIONAL ULONG ExceptionHandler;
 *   	    OPTIONAL ULONG FunctionEntry;
 *   	};
 *   	OPTIONAL ULONG ExceptionData[]; */
} UNWIND_INFO, *PUNWIND_INFO;

typedef struct
{
	RUNTIME_FUNCTION runtimeFunction;
	UNWIND_INFO unwindInfo;
} MonoUnwindInfo, *PMonoUnwindInfo;

static void
mono_arch_unwindinfo_create (gpointer* monoui)
{
	PMonoUnwindInfo newunwindinfo;
	*monoui = newunwindinfo = g_new0 (MonoUnwindInfo, 1);
	newunwindinfo->unwindInfo.Version = 1;
}

void
mono_arch_unwindinfo_add_push_nonvol (gpointer* monoui, gpointer codebegin, gpointer nextip, guchar reg )
{
	PMonoUnwindInfo unwindinfo;
	PUNWIND_CODE unwindcode;
	guchar codeindex;
	if (!*monoui)
		mono_arch_unwindinfo_create (monoui);
	
	unwindinfo = (MonoUnwindInfo*)*monoui;

	if (unwindinfo->unwindInfo.CountOfCodes >= MONO_MAX_UNWIND_CODES)
		g_error ("Larger allocation needed for the unwind information.");

	codeindex = MONO_MAX_UNWIND_CODES - (++unwindinfo->unwindInfo.CountOfCodes);
	unwindcode = &unwindinfo->unwindInfo.UnwindCode[codeindex];
	unwindcode->UnwindOp = 0; /*UWOP_PUSH_NONVOL*/
	unwindcode->CodeOffset = (((guchar*)nextip)-((guchar*)codebegin));
	unwindcode->OpInfo = reg;

	if (unwindinfo->unwindInfo.SizeOfProlog >= unwindcode->CodeOffset)
		g_error ("Adding unwind info in wrong order.");
	
	unwindinfo->unwindInfo.SizeOfProlog = unwindcode->CodeOffset;
}

void
mono_arch_unwindinfo_add_set_fpreg (gpointer* monoui, gpointer codebegin, gpointer nextip, guchar reg )
{
	PMonoUnwindInfo unwindinfo;
	PUNWIND_CODE unwindcode;
	guchar codeindex;
	if (!*monoui)
		mono_arch_unwindinfo_create (monoui);
	
	unwindinfo = (MonoUnwindInfo*)*monoui;

	if (unwindinfo->unwindInfo.CountOfCodes + 1 >= MONO_MAX_UNWIND_CODES)
		g_error ("Larger allocation needed for the unwind information.");

	codeindex = MONO_MAX_UNWIND_CODES - (unwindinfo->unwindInfo.CountOfCodes += 2);
	unwindcode = &unwindinfo->unwindInfo.UnwindCode[codeindex];
	unwindcode->FrameOffset = 0; /*Assuming no frame pointer offset for mono*/
	unwindcode++;
	unwindcode->UnwindOp = 3; /*UWOP_SET_FPREG*/
	unwindcode->CodeOffset = (((guchar*)nextip)-((guchar*)codebegin));
	unwindcode->OpInfo = reg;
	
	unwindinfo->unwindInfo.FrameRegister = reg;

	if (unwindinfo->unwindInfo.SizeOfProlog >= unwindcode->CodeOffset)
		g_error ("Adding unwind info in wrong order.");
	
	unwindinfo->unwindInfo.SizeOfProlog = unwindcode->CodeOffset;
}

void
mono_arch_unwindinfo_add_alloc_stack (gpointer* monoui, gpointer codebegin, gpointer nextip, guint size )
{
	PMonoUnwindInfo unwindinfo;
	PUNWIND_CODE unwindcode;
	guchar codeindex;
	guchar codesneeded;
	if (!*monoui)
		mono_arch_unwindinfo_create (monoui);
	
	unwindinfo = (MonoUnwindInfo*)*monoui;

	if (size < 0x8)
		g_error ("Stack allocation must be equal to or greater than 0x8.");
	
	if (size <= 0x80)
		codesneeded = 1;
	else if (size <= 0x7FFF8)
		codesneeded = 2;
	else
		codesneeded = 3;
	
	if (unwindinfo->unwindInfo.CountOfCodes + codesneeded > MONO_MAX_UNWIND_CODES)
		g_error ("Larger allocation needed for the unwind information.");

	codeindex = MONO_MAX_UNWIND_CODES - (unwindinfo->unwindInfo.CountOfCodes += codesneeded);
	unwindcode = &unwindinfo->unwindInfo.UnwindCode[codeindex];

	if (codesneeded == 1) {
		/*The size of the allocation is 
		  (the number in the OpInfo member) times 8 plus 8*/
		unwindcode->OpInfo = (size - 8)/8;
		unwindcode->UnwindOp = 2; /*UWOP_ALLOC_SMALL*/
	}
	else {
		if (codesneeded == 3) {
			/*the unscaled size of the allocation is recorded
			  in the next two slots in little-endian format*/
			*((unsigned int*)(&unwindcode->FrameOffset)) = size;
			unwindcode += 2;
			unwindcode->OpInfo = 1;
		}
		else {
			/*the size of the allocation divided by 8
			  is recorded in the next slot*/
			unwindcode->FrameOffset = size/8; 
			unwindcode++;	
			unwindcode->OpInfo = 0;
			
		}
		unwindcode->UnwindOp = 1; /*UWOP_ALLOC_LARGE*/
	}

	unwindcode->CodeOffset = (((guchar*)nextip)-((guchar*)codebegin));

	if (unwindinfo->unwindInfo.SizeOfProlog >= unwindcode->CodeOffset)
		g_error ("Adding unwind info in wrong order.");
	
	unwindinfo->unwindInfo.SizeOfProlog = unwindcode->CodeOffset;
}

guint
mono_arch_unwindinfo_get_size (gpointer monoui)
{
	PMonoUnwindInfo unwindinfo;
	if (!monoui)
		return 0;
	
	unwindinfo = (MonoUnwindInfo*)monoui;
	return (8 + sizeof (MonoUnwindInfo)) - 
		(sizeof (UNWIND_CODE) * (MONO_MAX_UNWIND_CODES - unwindinfo->unwindInfo.CountOfCodes));
}

PRUNTIME_FUNCTION
MONO_GET_RUNTIME_FUNCTION_CALLBACK ( DWORD64 ControlPc, IN PVOID Context )
{
	MonoJitInfo *ji;
	guint64 pos;
	PMonoUnwindInfo targetinfo;
	MonoDomain *domain = mono_domain_get ();

	ji = mono_jit_info_table_find (domain, (char*)ControlPc);
	if (!ji)
		return 0;

	pos = (guint64)(((char*)ji->code_start) + ji->code_size);
	
	targetinfo = (PMonoUnwindInfo)ALIGN_TO (pos, 8);

	targetinfo->runtimeFunction.UnwindData = ((DWORD64)&targetinfo->unwindInfo) - ((DWORD64)Context);

	return &targetinfo->runtimeFunction;
}

void
mono_arch_unwindinfo_install_unwind_info (gpointer* monoui, gpointer code, guint code_size)
{
	PMonoUnwindInfo unwindinfo, targetinfo;
	guchar codecount;
	guint64 targetlocation;
	if (!*monoui)
		return;

	unwindinfo = (MonoUnwindInfo*)*monoui;
	targetlocation = (guint64)&(((guchar*)code)[code_size]);
	targetinfo = (PMonoUnwindInfo) ALIGN_TO(targetlocation, 8);

	unwindinfo->runtimeFunction.EndAddress = code_size;
	unwindinfo->runtimeFunction.UnwindData = ((guchar*)&targetinfo->unwindInfo) - ((guchar*)code);
	
	memcpy (targetinfo, unwindinfo, sizeof (MonoUnwindInfo) - (sizeof (UNWIND_CODE) * MONO_MAX_UNWIND_CODES));
	
	codecount = unwindinfo->unwindInfo.CountOfCodes;
	if (codecount) {
		memcpy (&targetinfo->unwindInfo.UnwindCode[0], &unwindinfo->unwindInfo.UnwindCode[MONO_MAX_UNWIND_CODES-codecount], 
			sizeof (UNWIND_CODE) * unwindinfo->unwindInfo.CountOfCodes);
	}

	g_free (unwindinfo);
	*monoui = 0;

	RtlInstallFunctionTableCallback (((DWORD64)code) | 0x3, (DWORD64)code, code_size, MONO_GET_RUNTIME_FUNCTION_CALLBACK, code, NULL);
}

#endif

#if MONO_SUPPORT_TASKLETS
MonoContinuationRestore
mono_tasklets_arch_restore (void)
{
	static guint8* saved = NULL;
	guint8 *code, *start;
	int cont_reg = AMD64_R9; /* register usable on both call conventions */

	if (saved)
		return (MonoContinuationRestore)saved;
	code = start = mono_global_codeman_reserve (64);
	/* the signature is: restore (MonoContinuation *cont, int state, MonoLMF **lmf_addr) */
	/* cont is in AMD64_ARG_REG1 ($rcx or $rdi)
	 * state is in AMD64_ARG_REG2 ($rdx or $rsi)
	 * lmf_addr is in AMD64_ARG_REG3 ($r8 or $rdx)
	 * We move cont to cont_reg since we need both rcx and rdi for the copy
	 * state is moved to $rax so it's setup as the return value and we can overwrite $rsi
 	 */
	amd64_mov_reg_reg (code, cont_reg, MONO_AMD64_ARG_REG1, 8);
	amd64_mov_reg_reg (code, AMD64_RAX, MONO_AMD64_ARG_REG2, 8);
	/* setup the copy of the stack */
	amd64_mov_reg_membase (code, AMD64_RCX, cont_reg, G_STRUCT_OFFSET (MonoContinuation, stack_used_size), sizeof (int));
	amd64_shift_reg_imm (code, X86_SHR, AMD64_RCX, 3);
	x86_cld (code);
	amd64_mov_reg_membase (code, AMD64_RSI, cont_reg, G_STRUCT_OFFSET (MonoContinuation, saved_stack), sizeof (gpointer));
	amd64_mov_reg_membase (code, AMD64_RDI, cont_reg, G_STRUCT_OFFSET (MonoContinuation, return_sp), sizeof (gpointer));
	amd64_prefix (code, X86_REP_PREFIX);
	amd64_movsl (code);

	/* now restore the registers from the LMF */
	amd64_mov_reg_membase (code, AMD64_RCX, cont_reg, G_STRUCT_OFFSET (MonoContinuation, lmf), 8);
	amd64_mov_reg_membase (code, AMD64_RBX, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, rbx), 8);
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, rbp), 8);
	amd64_mov_reg_membase (code, AMD64_R12, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, r12), 8);
	amd64_mov_reg_membase (code, AMD64_R13, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, r13), 8);
	amd64_mov_reg_membase (code, AMD64_R14, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, r14), 8);
	amd64_mov_reg_membase (code, AMD64_R15, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, r15), 8);
#ifdef PLATFORM_WIN32
	amd64_mov_reg_membase (code, AMD64_RDI, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, rdi), 8);
	amd64_mov_reg_membase (code, AMD64_RSI, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, rsi), 8);
#endif
	amd64_mov_reg_membase (code, AMD64_RSP, AMD64_RCX, G_STRUCT_OFFSET (MonoLMF, rsp), 8);

	/* restore the lmf chain */
	/*x86_mov_reg_membase (code, X86_ECX, X86_ESP, 12, 4);
	x86_mov_membase_reg (code, X86_ECX, 0, X86_EDX, 4);*/

	/* state is already in rax */
	amd64_jump_membase (code, cont_reg, G_STRUCT_OFFSET (MonoContinuation, return_ip));
	g_assert ((code - start) <= 64);
	saved = start;
	return (MonoContinuationRestore)saved;
}
#endif

