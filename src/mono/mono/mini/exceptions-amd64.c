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
#include <sys/ucontext.h>

#include <mono/arch/amd64/amd64-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-amd64.h"

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
	sctx->eax = ctx->Eax;
	sctx->ebx = ctx->Ebx;
	sctx->ecx = ctx->Ecx;
	sctx->edx = ctx->Edx;
	sctx->ebp = ctx->Ebp;
	sctx->esp = ctx->Esp;
	sctx->esi = ctx->Esi;
	sctx->edi = ctx->Edi;
	sctx->eip = ctx->Eip;

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
	ctx->Eax = sctx->eax;
	ctx->Ebx = sctx->ebx;
	ctx->Ecx = sctx->ecx;
	ctx->Edx = sctx->edx;
	ctx->Ebp = sctx->ebp;
	ctx->Esp = sctx->esp;
	ctx->Esi = sctx->esi;
	ctx->Edi = sctx->edi;
	ctx->Eip = sctx->eip;

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
 * Can't allocate the helper methods in static arrays as on other platforms.
 */
static MonoCodeManager *code_manager = NULL;
static CRITICAL_SECTION code_manager_mutex;

void
mono_amd64_exceptions_init ()
{
	InitializeCriticalSection (&code_manager_mutex);
	code_manager = mono_code_manager_new ();
}

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context (void)
{
	static guint8 *start = NULL;
	static gboolean inited = FALSE;
	guint8 *code;

	if (inited)
		return start;

	/* restore_contect (MonoContext *ctx) */

	EnterCriticalSection (&code_manager_mutex);
	start = code = mono_code_manager_reserve (code_manager, 1024);
	LeaveCriticalSection (&code_manager_mutex);

	/* get return address */
	amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, rip), 8);

	/* Restore registers */
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, rbp), 8);
	amd64_mov_reg_membase (code, AMD64_RBX, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, rbx), 8);
	amd64_mov_reg_membase (code, AMD64_R12, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, r12), 8);
	amd64_mov_reg_membase (code, AMD64_R13, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, r13), 8);
	amd64_mov_reg_membase (code, AMD64_R14, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, r14), 8);
	amd64_mov_reg_membase (code, AMD64_R15, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, r15), 8);

	amd64_mov_reg_membase (code, AMD64_RSP, AMD64_RDI,  G_STRUCT_OFFSET (MonoContext, rsp), 8);

	/* jump to the saved IP */
	amd64_jump_reg (code, AMD64_RAX);

	inited = TRUE;

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
mono_arch_get_call_filter (void)
{
	static guint8 *start;
	static gboolean inited = FALSE;
	int i;
	guint8 *code;
	guint32 pos;

	if (inited)
		return start;

	EnterCriticalSection (&code_manager_mutex);
	start = code = mono_code_manager_reserve (code_manager, 64);
	LeaveCriticalSection (&code_manager_mutex);

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
	amd64_mov_reg_membase (code, AMD64_RBP, AMD64_RDI, G_STRUCT_OFFSET (MonoContext, rbp), 8);
	/* load callee saved regs */
	amd64_mov_reg_membase (code, AMD64_RBX, AMD64_RDI, G_STRUCT_OFFSET (MonoContext, rbx), 8);
	amd64_mov_reg_membase (code, AMD64_R12, AMD64_RDI, G_STRUCT_OFFSET (MonoContext, r12), 8);
	amd64_mov_reg_membase (code, AMD64_R13, AMD64_RDI, G_STRUCT_OFFSET (MonoContext, r13), 8);
	amd64_mov_reg_membase (code, AMD64_R14, AMD64_RDI, G_STRUCT_OFFSET (MonoContext, r14), 8);
	amd64_mov_reg_membase (code, AMD64_R15, AMD64_RDI, G_STRUCT_OFFSET (MonoContext, r15), 8);

	/* call the handler */
	amd64_call_reg (code, AMD64_RSI);

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

	g_assert ((code - start) < 64);

	inited = TRUE;

	return start;
}

static void
throw_exception (MonoObject *exc, guint64 rip, guint64 rsp,
				 guint64 rbx, guint64 rbp, guint64 r12, guint64 r13, 
				 guint64 r14, guint64 r15, guint64 rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	rip -= 1;

	ctx.rsp = rsp;
	ctx.rip = rip;
	ctx.rbx = rbx;
	ctx.rbp = rbp;
	ctx.r12 = r12;
	ctx.r13 = r13;
	ctx.r14 = r14;
	ctx.r15 = r15;

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	mono_handle_exception (&ctx, exc, (gpointer)(rip + 1), FALSE);
	restore_context (&ctx);

	g_assert_not_reached ();
}

static gpointer
get_throw_trampoline (gboolean rethrow)
{
	guint8* start;
	guint8 *code;

	EnterCriticalSection (&code_manager_mutex);
	start = code = mono_code_manager_reserve (code_manager, 64);
	LeaveCriticalSection (&code_manager_mutex);

	code = start;

	/* Exception */
	amd64_mov_reg_reg (code, AMD64_RDI, AMD64_RDI, 8);
	/* IP */
	amd64_mov_reg_membase (code, AMD64_RSI, AMD64_RSP, 0, 8);
	/* SP */
	amd64_lea_membase (code, AMD64_RDX, AMD64_RSP, 8);
	/* Callee saved regs */
	amd64_mov_reg_reg (code, AMD64_RCX, AMD64_RBX, 8);
	amd64_mov_reg_reg (code, AMD64_R8, AMD64_RBP, 8);
	amd64_mov_reg_reg (code, AMD64_R9, AMD64_R12, 8);
	/* reverse order */
	amd64_push_imm (code, rethrow);
	amd64_push_reg (code, AMD64_R15);
	amd64_push_reg (code, AMD64_R14);
	amd64_push_reg (code, AMD64_R13);

	amd64_mov_reg_imm (code, AMD64_R11, throw_exception);
	amd64_call_reg (code, AMD64_R11);
	amd64_breakpoint (code);

	g_assert ((code - start) < 64);

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
mono_arch_get_throw_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;

	if (inited)
		return start;

	start = get_throw_trampoline (FALSE);

	inited = TRUE;

	return start;
}

gpointer 
mono_arch_get_rethrow_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;

	if (inited)
		return start;

	start = get_throw_trampoline (TRUE);

	inited = TRUE;

	return start;
}

/**
 * mono_arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint8* start;
	static gboolean inited = FALSE;
	guint8 *code;
	guint64 throw_ex;

	if (inited)
		return start;

	EnterCriticalSection (&code_manager_mutex);
	start = code = mono_code_manager_reserve (code_manager, 64);
	LeaveCriticalSection (&code_manager_mutex);

	code = start;

	/* Push return address */
	amd64_push_reg (code, AMD64_RSI);

	/* Call exception_from_name */
	amd64_mov_reg_reg (code, AMD64_RDX, AMD64_RDI, 8);
	amd64_mov_reg_imm (code, AMD64_RSI, "System");
	amd64_mov_reg_imm (code, AMD64_RDI, mono_defaults.exception_class->image);

	amd64_mov_reg_imm (code, AMD64_R11, mono_exception_from_name);
	amd64_call_reg (code, AMD64_R11);

	/* Put the original return address at the top of the misaligned stack */
	amd64_pop_reg (code, AMD64_RSI);
	amd64_push_reg (code, AMD64_R11);
	amd64_push_reg (code, AMD64_RSI);

	throw_ex = (guint64)mono_arch_get_throw_exception ();

	/* Call throw_exception */
	amd64_mov_reg_reg (code, AMD64_RDI, AMD64_RAX, 8);
	amd64_mov_reg_imm (code, AMD64_R11, throw_ex);
	/* The original IP is on the stack */
	amd64_jump_reg (code, AMD64_R11);

	g_assert ((code - start) < 64);

	inited = TRUE;

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
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	int i;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mono_jit_info_table_find (domain, ip);

	if (managed)
		*managed = FALSE;

	if (ji != NULL) {
		int offset;

		*new_ctx = *ctx;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		/*
		 * Some managed methods like pinvoke wrappers might have save_lmf set.
		 * In this case, register save/restore code is not generated by the 
		 * JIT, so we have to restore callee saved registers from the lmf.
		 */
		if (ji->method->save_lmf) {
			/* 
			 * We only need to do this if the exception was raised in managed
			 * code, since otherwise the lmf was already popped of the stack.
			 */
			if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
				new_ctx->rbx = (*lmf)->rbx;
				new_ctx->r12 = (*lmf)->r12;
				new_ctx->r13 = (*lmf)->r13;
				new_ctx->r14 = (*lmf)->r14;
				new_ctx->r15 = (*lmf)->r15;
			}
		}
		else {
			offset = -1;
			/* restore caller saved registers */
			for (i = 0; i < AMD64_NREG; i ++)
				if (AMD64_IS_CALLEE_SAVED_REG (i) && (ji->used_regs & (1 << i))) {
					guint64 reg = *((guint64 *)ctx->SC_EBP + offset);
					offset --;
					switch (i) {
					case AMD64_RBX:
						new_ctx->rbx = reg;
						break;
					case AMD64_R12:
						new_ctx->r12 = reg;
						break;
					case AMD64_R13:
						new_ctx->r13 = reg;
						break;
					case AMD64_R14:
						new_ctx->r14 = reg;
						break;
					case AMD64_R15:
						new_ctx->r15 = reg;
						break;
					default:
						g_assert_not_reached ();
					}
				}
		}

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		/* Pop EBP and the return address */
		new_ctx->SC_ESP = ctx->SC_EBP + (2 * sizeof (gpointer));
		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->SC_EIP = *((guint64 *)ctx->SC_EBP + 1) - 1;
		new_ctx->SC_EBP = *((guint64 *)ctx->SC_EBP);

		/* Pop arguments off the stack */
		{
			MonoJitArgumentInfo *arg_info = alloca (sizeof (MonoJitArgumentInfo) * (ji->method->signature->param_count + 1));

			guint32 stack_to_pop = mono_arch_get_argument_info (ji->method->signature, ji->method->signature->param_count, arg_info);
			new_ctx->SC_ESP += stack_to_pop;
		}

		*res = *ji;
		return res;
	} else if (*lmf) {
		
		*new_ctx = *ctx;

		if (!(*lmf)->method)
			return (gpointer)-1;

		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->rip))) {
			*res = *ji;
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

		new_ctx->SC_RIP = (*lmf)->rip;
		new_ctx->SC_RBP = (*lmf)->ebp;

		new_ctx->SC_RBX = (*lmf)->rbx;
		new_ctx->SC_R12 = (*lmf)->r12;
		new_ctx->SC_R13 = (*lmf)->r13;
		new_ctx->SC_R14 = (*lmf)->r14;
		new_ctx->SC_R15 = (*lmf)->r15;

		/* the lmf is always stored on the stack, so the following
		 * expression points to a stack location which can be used as ESP */
		new_ctx->SC_ESP = ALIGN_TO ((guint64)&((*lmf)->rip), 16);

		*lmf = (*lmf)->previous_lmf;

		return res;
		
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
	ucontext_t *ctx = (ucontext_t*)sigctx;
	MonoContext mctx;

	mctx.rax = ctx->uc_mcontext.gregs [REG_RAX];
	mctx.rbx = ctx->uc_mcontext.gregs [REG_RBX];
	mctx.rcx = ctx->uc_mcontext.gregs [REG_RCX];
	mctx.rdx = ctx->uc_mcontext.gregs [REG_RDX];
	mctx.rbp = ctx->uc_mcontext.gregs [REG_RBP];
	mctx.rsp = ctx->uc_mcontext.gregs [REG_RSP];
	mctx.rsi = ctx->uc_mcontext.gregs [REG_RSI];
	mctx.rdi = ctx->uc_mcontext.gregs [REG_RDI];
	mctx.rip = ctx->uc_mcontext.gregs [REG_RIP];
	mctx.r12 = ctx->uc_mcontext.gregs [REG_R12];
	mctx.r13 = ctx->uc_mcontext.gregs [REG_R13];
	mctx.r14 = ctx->uc_mcontext.gregs [REG_R14];
	mctx.r15 = ctx->uc_mcontext.gregs [REG_R15];

	mono_handle_exception (&mctx, obj, (gpointer)mctx.rip, test_only);

	ctx->uc_mcontext.gregs [REG_RAX] = mctx.rax;
	ctx->uc_mcontext.gregs [REG_RBX] = mctx.rbx;
	ctx->uc_mcontext.gregs [REG_RCX] = mctx.rcx;
	ctx->uc_mcontext.gregs [REG_RDX] = mctx.rdx;
	ctx->uc_mcontext.gregs [REG_RBP] = mctx.rbp;
	ctx->uc_mcontext.gregs [REG_RSP] = mctx.rsp;
	ctx->uc_mcontext.gregs [REG_RSI] = mctx.rsi;
	ctx->uc_mcontext.gregs [REG_RDI] = mctx.rdi;
	ctx->uc_mcontext.gregs [REG_RIP] = mctx.rip;
	ctx->uc_mcontext.gregs [REG_R12] = mctx.r12;
	ctx->uc_mcontext.gregs [REG_R13] = mctx.r13;
	ctx->uc_mcontext.gregs [REG_R14] = mctx.r14;
	ctx->uc_mcontext.gregs [REG_R15] = mctx.r15;

	return TRUE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	ucontext_t *ctx = (ucontext_t*)sigctx;
	return (gpointer)ctx->uc_mcontext.gregs [REG_RIP];
}

