/*
 * exceptions-arm.c: exception support for ARM
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <ucontext.h>

#include <mono/arch/arm/arm-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-arm.h"

static gboolean arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only);

/*

struct sigcontext {
	unsigned long trap_no;
	unsigned long error_code;
	unsigned long oldmask;
	unsigned long arm_r0;
	unsigned long arm_r1;
	unsigned long arm_r2;
	unsigned long arm_r3;
	unsigned long arm_r4;
	unsigned long arm_r5;
	unsigned long arm_r6;
	unsigned long arm_r7;
	unsigned long arm_r8;
	unsigned long arm_r9;
	unsigned long arm_r10;
	unsigned long arm_fp;
	unsigned long arm_ip;
	unsigned long arm_sp;
	unsigned long arm_lr;
	unsigned long arm_pc;
	unsigned long arm_cpsr;
	unsigned long fault_address;
};

gregs below is this struct
struct user_regs {
	unsigned long int uregs[18];
};

the companion user_fpregs has just 8 double registers
(it's valid for FPA mode, will need changes for VFP)

typedef struct {
	gregset_t gregs;
	fpregset_t fpregs;
} mcontext_t;
	    
typedef struct ucontext {
	unsigned long int uc_flags;
	struct ucontext *uc_link;
	__sigset_t uc_sigmask;
	stack_t uc_stack;
	mcontext_t uc_mcontext;
	long int uc_filler[5];
} ucontext_t;

*/


#define restore_regs_from_context(ctx_reg,ip_reg,tmp_reg) do {	\
		int reg;	\
		ARM_LDR_IMM (code, ip_reg, ctx_reg, G_STRUCT_OFFSET (MonoContext, eip));	\
		ARM_ADD_REG_IMM8 (code, tmp_reg, ctx_reg, G_STRUCT_OFFSET(MonoContext, regs));	\
		ARM_LDMIA (code, tmp_reg, MONO_ARM_REGSAVE_MASK);	\
	} while (0)

/* nothing to do */
#define setup_context(ctx)

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 * The first argument in r0 is the pointer to the context.
 */
gpointer
mono_arch_get_restore_context (void)
{
	guint8 *code;
	static guint8 start [128];
	static int inited = 0;

	if (inited)
		return start;
	inited = 1;

	code = start;
	restore_regs_from_context (ARMREG_R0, ARMREG_R1, ARMREG_R2);
	/* restore also the stack pointer, FIXME: handle sp != fp */
	ARM_LDR_IMM (code, ARMREG_SP, ARMREG_R0, G_STRUCT_OFFSET (MonoContext, ebp));
	/* jump to the saved IP */
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
	/* never reached */
	ARM_DBRK (code);

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	return start;
}

/*
 * arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
gpointer
mono_arch_get_call_filter (void)
{
	static guint8 start [320];
	static int inited = 0;
	guint8 *code;
	int alloc_size, pos, i;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start;

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	/* restore all the regs from ctx (in r0), but not sp, the stack pointer */
	restore_regs_from_context (ARMREG_R0, ARMREG_IP, ARMREG_LR);
	/* call handler at eip (r1) and set the first arg with the exception (r2) */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_R2);
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);

	/* epilog */
	ARM_POP_NWB (code, 0xff0 | ((1 << ARMREG_SP) | (1 << ARMREG_PC)));

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;
	gboolean rethrow = eip & 1;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	eip &= ~1; /* clear the optional rethrow bit */
	/* adjust eip so that it point into the call instruction */
	eip -= 4;

	setup_context (&ctx);

	/*printf ("stack in throw: %p\n", esp);*/
	MONO_CONTEXT_SET_BP (&ctx, esp);
	MONO_CONTEXT_SET_IP (&ctx, eip);
	memcpy (&ctx.regs, int_regs, sizeof (gulong) * 8);
	/* memcpy (&ctx.fregs, fp_regs, sizeof (double) * MONO_SAVED_FREGS); */

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	mono_handle_exception (&ctx, exc, (gpointer)(eip + 4), FALSE);
	restore_context (&ctx);
	g_assert_not_reached ();
}

/**
 * arch_get_throw_exception_generic:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); or
 * void (*func) (char *exc_name);
 *
 */
static gpointer 
mono_arch_get_throw_exception_generic (guint8 *start, int size, int by_name, gboolean rethrow)
{
	guint8 *code;
	int alloc_size, pos, i;

	code = start;

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	if (by_name) {
		/* r0 has the name of the exception: get the object */
		ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_R0);
		code = mono_arm_emit_load_imm (code, ARMREG_R0, GPOINTER_TO_UINT (mono_defaults.corlib));
		code = mono_arm_emit_load_imm (code, ARMREG_R1, GPOINTER_TO_UINT ("System"));
		code = mono_arm_emit_load_imm (code, ARMREG_IP, GPOINTER_TO_UINT (mono_exception_from_name));
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	}

	/* call throw_exception (exc, ip, sp, int_regs, fp_regs) */
	/* caller sp */
	ARM_ADD_REG_IMM8 (code, ARMREG_R2, ARMREG_SP, 10 * 4); /* 10 saved regs */
	/* exc is already in place in r0 */
	if (by_name)
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_SP, 9 * 4); /* pos on the stack were lr was saved */
	else
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR); /* caller ip */
	/* FIXME: pointer to the saved fp regs */
	/*pos = alloc_size - sizeof (double) * MONO_SAVED_FREGS;
	ppc_addi (code, ppc_r7, ppc_sp, pos);*/
	/* pointer to the saved int regs */
	ARM_MOV_REG_REG (code, ARMREG_R3, ARMREG_SP); /* the pushed regs */
	/* we encode rethrow in the ip, so we avoid args on the stack */
	ARM_ORR_REG_IMM8 (code, ARMREG_R1, ARMREG_R1, rethrow);

	code = mono_arm_emit_load_imm (code, ARMREG_IP, GPOINTER_TO_UINT (throw_exception));
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	/* we should never reach this breakpoint */
	ARM_DBRK (code);
	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	return start;
}

/**
 * mono_arch_get_rethrow_exception:
 *
 * Returns a function pointer which can be used to rethrow 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
 */
gpointer
mono_arch_get_rethrow_exception (void)
{
	static guint8 start [132];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, TRUE);
	inited = 1;
	return start;
}
/**
 * arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, mono_get_exception_arithmetic ()); 
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception (void)
{
	static guint8 start [132];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, FALSE);
	inited = 1;
	return start;
}

/**
 * arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, "ArithmeticException"); 
 * x86_call_code (code, arch_get_throw_exception_by_name ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint8 start [168];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE, FALSE);
	inited = 1;
	return start;
}	

static MonoArray *
glist_to_array (GList *list, MonoClass *eclass) 
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	int len, i;

	if (!list)
		return NULL;

	len = g_list_length (list);
	res = mono_array_new (domain, eclass, len);

	for (i = 0; list; list = list->next, i++)
		mono_array_set (res, gpointer, i, list->data);

	return res;
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
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji,
			 MonoContext *ctx, MonoContext *new_ctx, char **trace, MonoLMF **lmf,
			 int *native_offset, gboolean *managed)
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
				memcpy (&new_ctx->regs [0], &(*lmf)->iregs [4], sizeof (gulong) * MONO_SAVED_GREGS);
			}
			new_ctx->esp = (*lmf)->iregs [13];
			new_ctx->eip = (*lmf)->iregs [14];
			new_ctx->ebp = new_ctx->esp;
		} else {
			int i;
			char* sp;
			offset = ji->used_regs >> 16;
			offset <<= 2;
			/* the saved regs are at sp + offset */
			/* restore caller saved registers */
			sp = (char*)ctx->ebp;
			sp += offset;
			for (i = 4; i < 16; ++i) {
				if (ji->used_regs & (1 << i)) {
					new_ctx->regs [i - 4] = *(gulong*)sp;
					sp += sizeof (gulong);
				}
			}
			/* IP and LR */
			new_ctx->esp = *(gulong*)sp;
			sp += sizeof (gulong);
			new_ctx->eip = *(gulong*)sp;
			sp += sizeof (gulong);
			new_ctx->ebp = new_ctx->esp;
		}

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->eip--;

		return ji;
	} else if (*lmf) {
		
		*new_ctx = *ctx;

		if (!(*lmf)->method)
			return (gpointer)-1;

		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip))) {
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

		memcpy (&new_ctx->regs [0], &(*lmf)->iregs [4], sizeof (gulong) * MONO_SAVED_GREGS);
		new_ctx->esp = (*lmf)->iregs [13];
		new_ctx->eip = (*lmf)->iregs [14];
		new_ctx->ebp = new_ctx->esp;

		*lmf = (*lmf)->previous_lmf;

		return ji ? ji : res;
	}

	return NULL;
}

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	struct ucontext *uc = ctx;
	MonoContext mctx;
	gboolean result;

	mctx.eip = uc->uc_mcontext.gregs [ARMREG_PC];
	mctx.ebp = uc->uc_mcontext.gregs [ARMREG_SP];
	memcpy (&mctx.regs, &uc->uc_mcontext.gregs [ARMREG_R4], sizeof (gulong) * 8);
	/* memcpy (&mctx.fregs, &uc->uc_mcontext.uc_regs->fpregs.fpregs [14], sizeof (double) * MONO_SAVED_FREGS);*/

	result = mono_handle_exception (&mctx, obj, (gpointer)mctx.eip, test_only);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	uc->uc_mcontext.gregs [ARMREG_PC] = mctx.eip;
	uc->uc_mcontext.gregs [ARMREG_SP] = mctx.ebp;
	memcpy (&uc->uc_mcontext.gregs [ARMREG_R4], &mctx.regs, sizeof (gulong) * 8);
	/* memcpy (&uc->uc_mcontext.uc_regs->fpregs.fpregs [14], &mctx.fregs, sizeof (double) * MONO_SAVED_FREGS);*/
	return result;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	struct ucontext *uc = sigctx;
	return (gpointer)uc->uc_mcontext.gregs [ARMREG_PC];
}

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

