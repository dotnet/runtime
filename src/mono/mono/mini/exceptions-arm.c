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

/*
 * So, it turns out that the ucontext struct defined by libc is incorrect.
 * We define our own version here and use it instead.
 */

#if __APPLE__
#define my_ucontext ucontext_t
#else
typedef struct my_ucontext {
	unsigned long       uc_flags;
	struct my_ucontext *uc_link;
	struct {
		void *p;
		int flags;
		size_t size;
	} sstack_data;
	struct sigcontext sig_ctx;
	/* some 2.6.x kernel has fp data here after a few other fields
	 * we don't use them for now...
	 */
} my_ucontext;
#endif

#define restore_regs_from_context(ctx_reg,ip_reg,tmp_reg) do {	\
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
mono_arch_get_restore_context_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *code;
	guint8 *start;

	*ji = NULL;

	start = code = mono_global_codeman_reserve (128);

	restore_regs_from_context (ARMREG_R0, ARMREG_R1, ARMREG_R2);
	/* restore also the stack pointer */
	ARM_LDR_IMM (code, ARMREG_SP, ARMREG_R0, G_STRUCT_OFFSET (MonoContext, esp));
	ARM_LDR_IMM (code, ARMREG_FP, ARMREG_R0, G_STRUCT_OFFSET (MonoContext, ebp));

	/* jump to the saved IP */
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
	/* never reached */
	ARM_DBRK (code);

	g_assert ((code - start) < 128);

	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

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
mono_arch_get_call_filter_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *code;
	guint8* start;

	*ji = NULL;

	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	start = code = mono_global_codeman_reserve (320);

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

	g_assert ((code - start) < 320);

	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

	return start;
}

void
mono_arm_throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;
	gboolean rethrow = eip & 1;

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	eip &= ~1; /* clear the optional rethrow bit */
	/* adjust eip so that it point into the call instruction */
	eip -= 4;

	setup_context (&ctx);

	/*printf ("stack in throw: %p\n", esp);*/
	MONO_CONTEXT_SET_BP (&ctx, int_regs [ARMREG_FP - 4]);
	MONO_CONTEXT_SET_SP (&ctx, esp);
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

void
mono_arm_throw_exception_by_token (guint32 type_token, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs)
{
	mono_arm_throw_exception ((MonoObject*)mono_exception_from_token (mono_defaults.corlib, type_token), eip, esp, int_regs, fp_regs);
}

/**
 * arch_get_throw_exception_generic:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); or
 * void (*func) (guint32 ex_token, guint8* ip);
 *
 */
static gpointer 
mono_arch_get_throw_exception_generic (int size, int by_token, gboolean rethrow, guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *start;
	guint8 *code;

	*ji = NULL;

	code = start = mono_global_codeman_reserve (size);

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	/* call throw_exception (exc, ip, sp, int_regs, fp_regs) */
	/* caller sp */
	ARM_ADD_REG_IMM8 (code, ARMREG_R2, ARMREG_SP, 10 * 4); /* 10 saved regs */
	/* exc is already in place in r0 */
	if (by_token) {
		/* The caller ip is already in R1 */
	} else {
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR); /* caller ip */
	}
	/* FIXME: pointer to the saved fp regs */
	/*pos = alloc_size - sizeof (double) * MONO_SAVED_FREGS;
	ppc_addi (code, ppc_r7, ppc_sp, pos);*/
	/* pointer to the saved int regs */
	ARM_MOV_REG_REG (code, ARMREG_R3, ARMREG_SP); /* the pushed regs */
	/* we encode rethrow in the ip, so we avoid args on the stack */
	ARM_ORR_REG_IMM8 (code, ARMREG_R1, ARMREG_R1, rethrow);

	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, by_token ? "mono_arm_throw_exception_by_token" : "mono_arm_throw_exception");
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)(gpointer)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
		code = mono_arm_emit_load_imm (code, ARMREG_IP, GPOINTER_TO_UINT (by_token ? (gpointer)mono_arm_throw_exception_by_token : (gpointer)mono_arm_throw_exception));
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	/* we should never reach this breakpoint */
	ARM_DBRK (code);
	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);

	*code_size = code - start;

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
mono_arch_get_rethrow_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return mono_arch_get_throw_exception_generic (132, FALSE, TRUE, code_size, ji, aot);
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
mono_arch_get_throw_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return mono_arch_get_throw_exception_generic (132, FALSE, FALSE, code_size, ji, aot);
}

gpointer 
mono_arch_get_throw_exception_by_name_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{	
	guint8* start;
	guint8 *code;

	*ji = NULL;

	start = code = mono_global_codeman_reserve (64);

	/* Not used on ARM */
	ARM_DBRK (code);

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
 * On ARM, the ip is passed instead of an offset.
 */
gpointer 
mono_arch_get_throw_corlib_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	return mono_arch_get_throw_exception_generic (168, TRUE, FALSE, code_size, ji, aot);
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
			 MonoContext *ctx, MonoContext *new_ctx, MonoLMF **lmf, gboolean *managed)
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
		int i;
		gssize regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		*new_ctx = *ctx;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (ji->from_aot)
			unwind_info = mono_aot_get_unwind_info (ji, &unwind_info_len);
		else
			unwind_info = mono_get_cached_unwind_info (ji->used_regs, &unwind_info_len);

		/* Beautiful numbers */
		for (i = 4; i < 12; ++i)
			regs [i] = new_ctx->regs [i - 4];
		regs [ARMREG_IP] = new_ctx->regs [8];
		regs [ARMREG_LR] = new_ctx->regs [9];
		regs [ARMREG_SP] = new_ctx->esp;

		mono_unwind_frame (unwind_info, unwind_info_len, ji->code_start, 
						   (guint8*)ji->code_start + ji->code_size,
						   ip, regs, MONO_MAX_IREGS, &cfa);

		for (i = 4; i < 12; ++i)
			new_ctx->regs [i - 4] = regs [i];
		new_ctx->regs [8] = regs [ARMREG_IP];
		new_ctx->regs [9] = regs [ARMREG_LR];
		new_ctx->eip = regs [ARMREG_LR];
		new_ctx->esp = (gsize)cfa;
		new_ctx->ebp = new_ctx->esp;

		/* Needed by get_exception_catch_class () */
		// FIXME: Is this neded ?
		new_ctx->regs [ARMREG_R11] = new_ctx->esp;

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->eip--;

		return ji;
	} else if (*lmf) {
		
		*new_ctx = *ctx;

		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip))) {
		} else {
			if (!(*lmf)->method)
				return (gpointer)-1;
			memset (res, 0, MONO_SIZEOF_JIT_INFO);
			res->method = (*lmf)->method;
		}

		memcpy (&new_ctx->regs [0], &(*lmf)->iregs [4], sizeof (gulong) * MONO_SAVED_GREGS);
		/* This is the sp for the current frame */
		new_ctx->esp = (*lmf)->iregs [ARMREG_FP];
		new_ctx->eip = (*lmf)->iregs [13];
		new_ctx->ebp = new_ctx->esp;

		*lmf = (*lmf)->previous_lmf;

		return ji ? ji : res;
	}

	return NULL;
}

void
mono_arch_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
#if BROKEN_LINUX
	struct ucontext *uc = sigctx;

	mctx->eip = uc->uc_mcontext.gregs [ARMREG_PC];
	mctx->esp = uc->uc_mcontext.gregs [ARMREG_SP];
	memcpy (&mctx->regs, &uc->uc_mcontext.gregs [ARMREG_R4], sizeof (gulong) * 8);
	/* memcpy (&mctx->fregs, &uc->uc_mcontext.uc_regs->fpregs.fpregs [14], sizeof (double) * MONO_SAVED_FREGS);*/
#else
	my_ucontext *my_uc = sigctx;

	mctx->eip = UCONTEXT_REG_PC (my_uc);
	mctx->esp = UCONTEXT_REG_SP (my_uc);
	memcpy (&mctx->regs, &UCONTEXT_REG_R4 (my_uc), sizeof (gulong) * 8);
#endif
	mctx->ebp = mctx->regs [ARMREG_FP - 4];
}

void
mono_arch_monoctx_to_sigctx (MonoContext *mctx, void *ctx)
{
#if BROKEN_LINUX
	struct ucontext *uc = ctx;

	uc->uc_mcontext.gregs [ARMREG_PC] = mctx->eip;
	uc->uc_mcontext.gregs [ARMREG_SP] = mctx->ebp;
	memcpy (&uc->uc_mcontext.gregs [ARMREG_R4], &mctx->regs, sizeof (gulong) * 8);
	/* memcpy (&uc->uc_mcontext.uc_regs->fpregs.fpregs [14], &mctx->fregs, sizeof (double) * MONO_SAVED_FREGS);*/
#else
	my_ucontext *my_uc = ctx;

	UCONTEXT_REG_PC (my_uc) = mctx->eip;
	UCONTEXT_REG_SP (my_uc) = mctx->ebp;
	memcpy (&UCONTEXT_REG_R4 (my_uc), &mctx->regs, sizeof (gulong) * 8);
#endif
}

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;
	gboolean result;

	mono_arch_sigctx_to_monoctx (ctx, &mctx);

	result = mono_handle_exception (&mctx, obj, (gpointer)mctx.eip, test_only);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	mono_arch_monoctx_to_sigctx (&mctx, ctx);
	return result;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#if BROKEN_LINUX
	struct ucontext *uc = sigctx;
	return (gpointer)uc->uc_mcontext.gregs [ARMREG_PC];
#else
	my_ucontext *my_uc = sigctx;
	return (void*) UCONTEXT_REG_PC (my_uc);
#endif
}

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

