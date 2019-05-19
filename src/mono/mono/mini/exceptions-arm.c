/**
 * \file
 * exception support for ARM
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

#ifndef MONO_CROSS_COMPILE
#ifdef HOST_ANDROID
#include <asm/sigcontext.h>
#endif  /* def HOST_ANDROID */
#endif

#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif  /* def HAVE_UCONTEXT_H */

#include <mono/arch/arm/arm-codegen.h>
#include <mono/arch/arm/arm-vfp-codegen.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-arm.h"
#include "mini-runtime.h"
#include "aot-runtime.h"
#include "mono/utils/mono-sigcontext.h"
#include "mono/utils/mono-compiler.h"

#ifndef DISABLE_JIT

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 * The first argument in r0 is the pointer to the context.
 */
gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code;
	guint8 *start;
	int ctx_reg;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	start = code = mono_global_codeman_reserve (128);

	/* 
	 * Move things to their proper place so we can restore all the registers with
	 * one instruction.
	 */

	ctx_reg = ARMREG_R0;

	if (!mono_arch_is_soft_float ()) {
		ARM_ADD_REG_IMM8 (code, ARMREG_IP, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, fregs));
		ARM_FLDMD (code, ARM_VFP_D0, 16, ARMREG_IP);
	}

	/* move pc to PC */
	ARM_LDR_IMM (code, ARMREG_IP, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, pc));
	ARM_STR_IMM (code, ARMREG_IP, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, regs) + (ARMREG_PC * sizeof (target_mgreg_t)));

	/* restore everything */
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ctx_reg, MONO_STRUCT_OFFSET(MonoContext, regs));
	ARM_LDM (code, ARMREG_IP, 0xffff);

	/* never reached */
	ARM_DBRK (code);

	g_assert ((code - start) < 128);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("restore_context", start, code - start, ji, unwind_ops);

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
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code;
	guint8* start;
	int ctx_reg;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	start = code = mono_global_codeman_reserve (320);

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 8);

	/* restore all the regs from ctx (in r0), but not sp, the stack pointer */
	ctx_reg = ARMREG_R0;
	ARM_LDR_IMM (code, ARMREG_IP, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, pc));
	ARM_ADD_REG_IMM8 (code, ARMREG_LR, ctx_reg, MONO_STRUCT_OFFSET(MonoContext, regs) + (MONO_ARM_FIRST_SAVED_REG * sizeof (target_mgreg_t)));
	ARM_LDM (code, ARMREG_LR, MONO_ARM_REGSAVE_MASK);
	/* call handler at eip (r1) and set the first arg with the exception (r2) */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_R2);
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);

	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 8);

	/* epilog */
	ARM_POP_NWB (code, 0xff0 | ((1 << ARMREG_SP) | (1 << ARMREG_PC)));

	g_assert ((code - start) < 320);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("call_filter", start, code - start, ji, unwind_ops);

	return start;
}

#endif /* DISABLE_JIT */

void
mono_arm_throw_exception (MonoObject *exc, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean preserve_ips)
{
	ERROR_DECL (error);
	MonoContext ctx;
	gboolean rethrow = sp & 1;

	sp &= ~1; /* clear the optional rethrow bit */
	pc &= ~1; /* clear the thumb bit */
	/* adjust eip so that it point into the call instruction */
	pc -= 4;

	/*printf ("stack in throw: %p\n", esp);*/
	MONO_CONTEXT_SET_BP (&ctx, int_regs [ARMREG_FP - 4]);
	MONO_CONTEXT_SET_SP (&ctx, sp);
	MONO_CONTEXT_SET_IP (&ctx, pc);
	memcpy (((guint8*)&ctx.regs) + (ARMREG_R4 * sizeof (host_mgreg_t)), int_regs, 8 * sizeof (host_mgreg_t));
	memcpy (&ctx.fregs, fp_regs, sizeof (double) * 16);

	if (mono_object_isinst_checked (exc, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow) {
			mono_ex->stack_trace = NULL;
			mono_ex->trace_ips = NULL;
		} else if (preserve_ips) {
			mono_ex->caught_in_unmanaged = TRUE;
		}
	}
	mono_error_assert_ok (error);
	mono_handle_exception (&ctx, exc);
	mono_restore_context (&ctx);
	g_assert_not_reached ();
}

void
mono_arm_throw_exception_by_token (guint32 ex_token_index, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs)
{
	guint32 ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
	/* Clear thumb bit */
	pc &= ~1;

	mono_arm_throw_exception ((MonoObject*)mono_exception_from_token (mono_defaults.corlib, ex_token), pc, sp, int_regs, fp_regs, FALSE);
}

void
mono_arm_resume_unwind (guint32 dummy1, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs)
{
	MonoContext ctx;

	pc &= ~1; /* clear the optional rethrow bit */
	/* adjust eip so that it point into the call instruction */
	pc -= 4;

	MONO_CONTEXT_SET_BP (&ctx, int_regs [ARMREG_FP - 4]);
	MONO_CONTEXT_SET_SP (&ctx, sp);
	MONO_CONTEXT_SET_IP (&ctx, pc);
	memcpy (((guint8*)&ctx.regs) + (ARMREG_R4 * sizeof (host_mgreg_t)), int_regs, 8 * sizeof (host_mgreg_t));

	mono_resume_unwind (&ctx);
}

#ifndef DISABLE_JIT

/**
 * get_throw_trampoline:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); or
 * void (*func) (guint32 ex_token, guint8* ip);
 *
 */
static gpointer 
get_throw_trampoline (int size, gboolean corlib, gboolean rethrow, gboolean llvm, gboolean resume_unwind, const char *tramp_name, MonoTrampInfo **info, gboolean aot, gboolean preserve_ips)
{
	guint8 *start;
	guint8 *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int cfa_offset;

	code = start = mono_global_codeman_reserve (size);

	mono_add_unwind_op_def_cfa (unwind_ops, code, start, ARMREG_SP, 0);

	/* save all the regs on the stack */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
	ARM_PUSH (code, MONO_ARM_REGSAVE_MASK);

	cfa_offset = MONO_ARM_NUM_SAVED_REGS * sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, start, ARMREG_SP, cfa_offset);
	mono_add_unwind_op_offset (unwind_ops, code, start, ARMREG_LR, -(ptrdiff_t)sizeof (target_mgreg_t));

	/* Save fp regs */
	if (!mono_arch_is_soft_float ()) {
		ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, sizeof (double) * 16);
		cfa_offset += sizeof (double) * 16;
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, cfa_offset);
		ARM_FSTMD (code, ARM_VFP_D0, 16, ARMREG_SP);
	}

	/* Param area */
	int param_size = 8;
	if (!resume_unwind && !corlib)
		param_size += 4; // Extra arg
	/* SP isn't 16byte aligned at this point which matters for some targets */
	param_size = ALIGN_TO (cfa_offset + param_size, MONO_ARCH_FRAME_ALIGNMENT) - cfa_offset;
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, param_size);
	cfa_offset += param_size;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, cfa_offset);

	/* call throw_exception (exc, ip, sp, int_regs, fp_regs) */
	/* caller sp */
	ARM_ADD_REG_IMM8 (code, ARMREG_R2, ARMREG_SP, cfa_offset);
	/* we encode rethrow in sp */
	if (rethrow) {
		g_assert (!resume_unwind);
		g_assert (!corlib);
		ARM_ORR_REG_IMM8 (code, ARMREG_R2, ARMREG_R2, rethrow);
	}
	/* exc is already in place in r0 */
	if (corlib) {
		/* The caller ip is already in R1 */
		if (llvm) {
			/*
			 * The address passed by llvm might point to before the call,
			 * thus outside the eh range recorded by llvm. Use the return
			 * address instead.
			 * FIXME: Do this on more platforms.
			 */
			ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR); /* caller ip */
		}
	} else {
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR); /* caller ip */
	}
	/* int regs */
	ARM_ADD_REG_IMM8 (code, ARMREG_R3, ARMREG_SP, (cfa_offset - (MONO_ARM_NUM_SAVED_REGS * sizeof (target_mgreg_t))));
	if (resume_unwind || corlib) {
		/* fp regs */
		ARM_ADD_REG_IMM8 (code, ARMREG_LR, ARMREG_SP, 8);
		ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, 0);
	} else {
		/* preserve_ips */
		ARM_MOV_REG_IMM8 (code, ARMREG_R5, preserve_ips);
		ARM_STR_IMM (code, ARMREG_R5, ARMREG_SP, 4);

		/* fp regs */
		ARM_ADD_REG_IMM8 (code, ARMREG_LR, ARMREG_SP, 8);
		ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, 0);
	}

	if (aot) {
		const char *icall_name;

		if (resume_unwind)
			icall_name = "mono_arm_resume_unwind";
		else if (corlib)
			icall_name = "mono_arm_throw_exception_by_token";
		else
			icall_name = "mono_arm_throw_exception";

		ji = mono_patch_info_list_prepend (ji, code - start, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)(gpointer)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
		code = mono_arm_emit_load_imm (code, ARMREG_IP, GPOINTER_TO_UINT (resume_unwind ? (gpointer)mono_arm_resume_unwind : (corlib ? (gpointer)mono_arm_throw_exception_by_token : (gpointer)mono_arm_throw_exception)));
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	/* we should never reach this breakpoint */
	ARM_DBRK (code);
	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create (tramp_name, start, code - start, ji, unwind_ops);

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
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (132, FALSE, FALSE, FALSE, FALSE, "throw_exception", info, aot, FALSE);
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
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (132, FALSE, TRUE, FALSE, FALSE, "rethrow_exception", info, aot, FALSE);
}

gpointer 
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (132, FALSE, TRUE, FALSE, FALSE, "rethrow_preserve_exception", info, aot, TRUE);
}

/**
 * mono_arch_get_throw_corlib_exception:
 * \returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * Here, \c offset is the offset which needs to be substracted from the caller IP 
 * to get the IP of the throw. Passing the offset has the advantage that it 
 * needs no relocations in the caller.
 * On ARM, the ip is passed instead of an offset.
 */
gpointer 
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (168, TRUE, FALSE, FALSE, FALSE, "throw_corlib_exception", info, aot, FALSE);
}	

GSList*
mono_arm_get_exception_trampolines (gboolean aot)
{
	MonoTrampInfo *info;
	GSList *tramps = NULL;

	// FIXME Macro to make one line per trampoline and less repitition of names.

	/* LLVM uses the normal trampolines, but with a different name */
	get_throw_trampoline (168, TRUE, FALSE, FALSE, FALSE, "llvm_throw_corlib_exception_trampoline", &info, aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline;
	tramps = g_slist_prepend (tramps, info);
	
	get_throw_trampoline (168, TRUE, FALSE, TRUE, FALSE, "llvm_throw_corlib_exception_abs_trampoline", &info, aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline;
	tramps = g_slist_prepend (tramps, info);

	get_throw_trampoline (168, FALSE, FALSE, FALSE, TRUE, "llvm_resume_unwind_trampoline", &info, aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline;
	tramps = g_slist_prepend (tramps, info);

	return tramps;
}

#else

GSList*
mono_arm_get_exception_trampolines (gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

void
mono_arch_exceptions_init (void)
{
	gpointer tramp;
	GSList *tramps, *l;
	
	if (mono_aot_only) {

		// FIXME Macroize.

		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline, tramp, "llvm_throw_corlib_exception_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_abs_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline, tramp, "llvm_throw_corlib_exception_abs_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_resume_unwind_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline, tramp, "llvm_resume_unwind_trampoline", NULL, TRUE, NULL);

	} else {
		tramps = mono_arm_get_exception_trampolines (FALSE);
		for (l = tramps; l; l = l->next) {
			MonoTrampInfo *info = (MonoTrampInfo*)l->data;
			mono_register_jit_icall_info (info->jit_icall_info, info->code, g_strdup (info->name), NULL, TRUE, NULL);
			mono_tramp_info_register (info, NULL);
		}
		g_slist_free (tramps);
	}
}

/* 
 * mono_arch_unwind_frame:
 *
 * See exceptions-amd64.c for docs;
 */
gboolean
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 host_mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		int i;
		mono_unwind_reg_t regs [MONO_MAX_IREGS + 1 + 8];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		/*
		printf ("%s %p %p\n", ji->d.method->name, ji->code_start, ip);
		mono_print_unwind_info (unwind_info, unwind_info_len);
		*/

		for (i = 0; i < 16; ++i)
			regs [i] = new_ctx->regs [i];
#ifdef TARGET_IOS
		/* On IOS, d8..d15 are callee saved. They are mapped to 8..15 in unwind.c */
		for (i = 0; i < 8; ++i)
			regs [MONO_MAX_IREGS + i] = *(guint64*)&(new_ctx->fregs [8 + i]);
#endif

		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, (guint8*)ji->code_start,
						   (guint8*)ji->code_start + ji->code_size,
						   (guint8*)ip, NULL, regs, MONO_MAX_IREGS + 8,
						   save_locations, MONO_MAX_IREGS, &cfa);

		if (!success)
			return FALSE;

		for (i = 0; i < 16; ++i)
			new_ctx->regs [i] = regs [i];
		new_ctx->pc = regs [ARMREG_LR];
		new_ctx->regs [ARMREG_SP] = (gsize)cfa;
#ifdef TARGET_IOS
		for (i = 0; i < 8; ++i)
			new_ctx->fregs [8 + i] = *(double*)&(regs [MONO_MAX_IREGS + i]);
#endif

		/* Clear thumb bit */
		new_ctx->pc &= ~1;

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->pc--;

		return TRUE;
	} else if (*lmf) {
		g_assert ((((guint64)(*lmf)->previous_lmf) & 2) == 0);

		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;
		
		if ((ji = mini_jit_info_table_find (domain, (gpointer)(gsize)(*lmf)->ip, NULL))) {
			frame->ji = ji;
		} else {
			if (!(*lmf)->method)
				return FALSE;
			frame->method = (*lmf)->method;
		}

		/*
		 * The LMF is saved at the start of the method using:
		 * ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP)
		 * ARM_PUSH (code, 0x5ff0);
		 * So it stores the register state as it existed at the caller. We need to
		 * produce the register state which existed at the time of the call which
		 * transitioned to native call, so we save the sp/fp/ip in the LMF.
		 */
		memcpy (&new_ctx->regs [0], &(*lmf)->iregs [0], sizeof (host_mgreg_t) * 13);
		new_ctx->pc = (*lmf)->ip;
		new_ctx->regs [ARMREG_SP] = (*lmf)->sp;
		new_ctx->regs [ARMREG_FP] = (*lmf)->fp;

		/* Clear thumb bit */
		new_ctx->pc &= ~1;

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->pc--;

		*lmf = (MonoLMF*)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
}

/*
 * handle_exception:
 *
 *   Called by resuming from a signal handler.
 */
static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext ctx;

	memcpy (&ctx, &jit_tls->ex_ctx, sizeof (MonoContext));

	mono_handle_exception (&ctx, (MonoObject*)obj);

	mono_restore_context (&ctx);
}

/*
 * This works around a gcc 4.5 bug:
 * https://bugs.launchpad.net/ubuntu/+source/gcc-4.5/+bug/721531
 */
static MONO_NEVER_INLINE gpointer
get_handle_signal_exception_addr (void)
{
	return (gpointer)handle_signal_exception;
}

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj)
{
#if defined(MONO_CROSS_COMPILE)
	g_assert_not_reached ();
#elif defined(MONO_ARCH_USE_SIGACTION)
	arm_ucontext *sigctx = (arm_ucontext*)ctx;
	/*
	 * Handling the exception in the signal handler is problematic, since the original
	 * signal is disabled, and we could run arbitrary code though the debugger. So
	 * resume into the normal stack and do most work there if possible.
	 */
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	guint64 sp = UCONTEXT_REG_SP (sigctx);

	/* Pass the ctx parameter in TLS */
	mono_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);
	/* The others in registers */
	UCONTEXT_REG_R0 (sigctx) = (gsize)obj;

	/* Allocate a stack frame */
	sp -= 16;
	UCONTEXT_REG_SP (sigctx) = sp;

	UCONTEXT_REG_PC (sigctx) = (gsize)get_handle_signal_exception_addr ();
#ifdef UCONTEXT_REG_CPSR
	if ((gsize)UCONTEXT_REG_PC (sigctx) & 1)
		/* Transition to thumb */
		UCONTEXT_REG_CPSR (sigctx) |= (1 << 5);
	else
		/* Transition to ARM */
		UCONTEXT_REG_CPSR (sigctx) &= ~(1 << 5);
#endif

	return TRUE;
#else
	MonoContext mctx;
	gboolean result;

	mono_sigctx_to_monoctx (ctx, &mctx);

	result = mono_handle_exception (&mctx, obj);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	mono_monoctx_to_sigctx (&mctx, ctx);
	return result;
#endif
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#else
	arm_ucontext *my_uc = (arm_ucontext*)sigctx;
	return (void*) UCONTEXT_REG_PC (my_uc);
#endif
}

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	host_mgreg_t sp = (host_mgreg_t)MONO_CONTEXT_GET_SP (ctx);

	// FIXME:
	g_assert (!user_data);

	/* Allocate a stack frame */
	sp -= 16;
	MONO_CONTEXT_SET_SP (ctx, sp);

	mono_arch_setup_resume_sighandler_ctx (ctx, (gpointer)async_cb);
}

/*
 * mono_arch_setup_resume_sighandler_ctx:
 *
 *   Setup CTX so execution continues at FUNC.
 */
void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	MONO_CONTEXT_SET_IP (ctx,func);
	if ((host_mgreg_t)MONO_CONTEXT_GET_IP (ctx) & 1)
		/* Transition to thumb */
		ctx->cpsr |= (1 << 5);
	else
		/* Transition to ARM */
		ctx->cpsr &= ~(1 << 5);
}

void
mono_arch_undo_ip_adjustment (MonoContext *ctx)
{
	ctx->pc++;

	if (mono_arm_thumb_supported ())
		ctx->pc |= 1;
}

void
mono_arch_do_ip_adjustment (MonoContext *ctx)
{
	/* Clear thumb bit */
	ctx->pc &= ~1;

	ctx->pc--;
}
