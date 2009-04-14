/*
 * tramp-arm.c: JIT trampoline code for ARM
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/arm/arm-codegen.h>

#include "mini.h"
#include "mini-arm.h"

static guint8* nullified_class_init_trampoline;

/*
 * Return the instruction to jump from code to target, 0 if not
 * reachable with a single instruction
 */
static guint32
branch_for_target_reachable (guint8 *branch, guint8 *target)
{
	gint diff = target - branch - 8;
	g_assert ((diff & 3) == 0);
	if (diff >= 0) {
		if (diff <= 33554431)
			return (ARMCOND_AL << ARMCOND_SHIFT) | (ARM_BR_TAG) | (diff >> 2);
	} else {
		/* diff between 0 and -33554432 */
		if (diff >= -33554432)
			return (ARMCOND_AL << ARMCOND_SHIFT) | (ARM_BR_TAG) | ((diff >> 2) & ~0xff000000);
	}
	return 0;
}

/*
 * mono_arch_get_unbox_trampoline:
 * @gsctx: the generic sharing context
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoGenericSharingContext *gsctx, MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 0;
	MonoDomain *domain = mono_domain_get ();

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 1;

	start = code = mono_domain_code_reserve (domain, 16);

	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 4);
	ARM_ADD_REG_IMM8 (code, this_pos, this_pos, sizeof (MonoObject));
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	*(guint32*)code = (guint32)addr;
	code += 4;
	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 16);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;

	MonoDomain *domain = mono_domain_get ();

	buf_len = 16;

	start = code = mono_domain_code_reserve (domain, buf_len);

	ARM_LDR_IMM (code, MONO_ARCH_RGCTX_REG, ARMREG_PC, 0);
	ARM_LDR_IMM (code, ARMREG_PC, ARMREG_PC, 0);
	*(guint32*)code = mrgctx;
	code += 4;
	*(guint32*)code = (guint32)addr;
	code += 4;

	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	guint32 *code = (guint32*)code_ptr;

	/* This is the 'bl' or the 'mov pc' instruction */
	--code;
	
	/*
	 * Note that methods are called also with the bl opcode.
	 */
	if ((((*code) >> 25)  & 7) == 5) {
		/*g_print ("direct patching\n");*/
		arm_patch ((guint8*)code, addr);
		mono_arch_flush_icache ((guint8*)code, 4);
		return;
	}

	if ((((*code) >> 20) & 0xFF) == 0x12) {
		/*g_print ("patching bx\n");*/
		arm_patch ((guint8*)code, addr);
		mono_arch_flush_icache ((guint8*)(code - 2), 4);
		return;
	}

	g_assert_not_reached ();
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	/* Patch the jump table entry used by the plt entry */
	guint32 offset = ((guint32*)code)[3];
	guint8 *jump_entry = code + offset + 12;

	*(guint8**)jump_entry = addr;
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
	mono_arch_patch_callsite (NULL, code, nullified_class_init_trampoline);
}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	if (mono_aot_only && !nullified_class_init_trampoline)
		nullified_class_init_trampoline = mono_aot_get_named_code ("nullified_class_init_trampoline");

	mono_arch_patch_plt_entry (code, nullified_class_init_trampoline);
}

/* Stack size for trampoline function 
 */
#define STACK (sizeof (MonoLMF))

/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

#define GEN_TRAMP_SIZE 192

/*
 * Stack frame description when the generic trampoline is called.
 * caller frame
 * ------------------- old sp
 *  MonoLMF
 * ------------------- sp
 */
guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	MonoJumpInfo *ji;
	guint32 code_size;
	guchar *code;
	GSList *unwind_ops, *l;

	code = mono_arch_create_trampoline_code_full (tramp_type, &code_size, &ji, &unwind_ops, FALSE);

	mono_save_trampoline_xdebug_info ("<generic_trampoline>", code, code_size, unwind_ops);

	for (l = unwind_ops; l; l = l->next)
		g_free (l->data);
	g_slist_free (unwind_ops);

	return code;
}
	
guchar*
mono_arch_create_trampoline_code_full (MonoTrampolineType tramp_type, guint32 *code_size, MonoJumpInfo **ji, GSList **out_unwind_ops, gboolean aot)
{
	guint8 *buf, *code = NULL;
	guint8 *load_get_lmf_addr, *load_trampoline;
	gpointer *constants;
	GSList *unwind_ops = NULL;
	int cfa_offset;

	*ji = NULL;

	/* Now we'll create in 'buf' the ARM trampoline code. This
	 is the trampoline code common to all methods  */
	
	code = buf = mono_global_codeman_reserve (GEN_TRAMP_SIZE);

	/*
	 * At this point lr points to the specific arg and sp points to the saved
	 * regs on the stack (all but PC and SP). The original LR value has been
	 * saved as sp + LR_OFFSET by the push in the specific trampoline
	 */
#define LR_OFFSET (sizeof (gpointer) * 13)

	// FIXME: Finish the unwind info, the current info allows us to unwind
	// when the trampoline is not in the epilog

	// CFA = SP + (num registers pushed) * 4
	cfa_offset = 14 * sizeof (gpointer);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, cfa_offset);
	// PC saved at sp+LR_OFFSET
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_LR, -4);

	ARM_MOV_REG_REG (code, ARMREG_V1, ARMREG_SP);
	if (aot && tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
		/* 
		 * The trampoline contains a pc-relative offset to the got slot where the
		 * value is stored. The offset can be found at [lr + 4].
		 */
		ARM_LDR_IMM (code, ARMREG_V2, ARMREG_LR, 4);
		ARM_LDR_REG_REG (code, ARMREG_V2, ARMREG_V2, ARMREG_LR);
	} else {
		if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT)
			ARM_LDR_IMM (code, ARMREG_V2, ARMREG_LR, 0);
		else
			ARM_MOV_REG_REG (code, ARMREG_V2, MONO_ARCH_VTABLE_REG);
	}
	ARM_LDR_IMM (code, ARMREG_V3, ARMREG_SP, LR_OFFSET);

	/* ok, now we can continue with the MonoLMF setup, mostly untouched 
	 * from emit_prolog in mini-arm.c
	 * This is a synthetized call to mono_get_lmf_addr ()
	 */
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_R0, ARMREG_PC, ARMREG_R0);
	} else {
		load_get_lmf_addr = code;
		code += 4;
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R0);

	/* we build the MonoLMF structure on the stack - see mini-arm.h
	 * The pointer to the struct is put in r1.
	 * the iregs array is already allocated on the stack by push.
	 */
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, sizeof (MonoLMF) - sizeof (guint) * 14);
	cfa_offset += sizeof (MonoLMF) - sizeof (guint) * 14;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	ARM_ADD_REG_IMM8 (code, ARMREG_R1, ARMREG_SP, STACK - sizeof (MonoLMF));
	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (code, ARMREG_R2, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (code, ARMREG_R1, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* save method info (it's in v2) */
	if ((tramp_type == MONO_TRAMPOLINE_JIT) || (tramp_type == MONO_TRAMPOLINE_JUMP))
		ARM_STR_IMM (code, ARMREG_V2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, method));
	ARM_STR_IMM (code, ARMREG_SP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, ebp));
	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (code, ARMREG_R2, 0);
	} else {
		/* assumes STACK == sizeof (MonoLMF) */
		ARM_LDR_IMM (code, ARMREG_R2, ARMREG_SP, (G_STRUCT_OFFSET (MonoLMF, iregs) + 13*4));
	}
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, eip));

	/*
	 * Now we're ready to call xxx_trampoline ().
	 */
	/* Arg 1: the saved registers. It was put in v1 */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_V1);

	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (code, ARMREG_R1, 0);
	} else {
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_V3);
	}
	
	/* Arg 3: the specific argument, stored in v2
	 */
	ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_V2);

	if (aot) {
		char *icall_name = g_strdup_printf ("trampoline_func_%d", tramp_type);
		*ji = mono_patch_info_list_prepend (*ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
		load_trampoline = code;
		code += 4;
	}

	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	
	/* OK, code address is now on r0. Move it to the place on the stack
	 * where IP was saved (it is now no more useful to us and it can be
	 * clobbered). This way we can just restore all the regs in one inst
	 * and branch to IP.
	 */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_V1, (ARMREG_R12 * 4));

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* 
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_force_interruption_checkpoint");
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = mono_thread_force_interruption_checkpoint;
		code += 4;
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

	/*
	 * Now we restore the MonoLMF (see emit_epilogue in mini-arm.c)
	 * and the rest of the registers, so the method called will see
	 * the same state as before we executed.
	 * The pointer to MonoLMF is in r2.
	 */
	ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_SP);
	/* ip = previous_lmf */
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R2, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (code, ARMREG_LR, ARMREG_R2, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_LR, G_STRUCT_OFFSET (MonoLMF, previous_lmf));

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore the registers and jump to the code:
	 * Note that IP has been conveniently set to the method addr.
	 */
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, sizeof (MonoLMF) - sizeof (guint) * 14);
	ARM_POP_NWB (code, 0x5fff);
	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_IP);
	/* do we need to set sp? */
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, (14 * 4));
	if ((tramp_type == MONO_TRAMPOLINE_CLASS_INIT) || (tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT) || (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH))
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_LR);
	else
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

	constants = (gpointer*)code;
	constants [0] = mono_get_lmf_addr;
	constants [1] = (gpointer)mono_get_trampoline_func (tramp_type);

	if (!aot) {
		/* backpatch by emitting the missing instructions skipped above */
		ARM_LDR_IMM (load_get_lmf_addr, ARMREG_R0, ARMREG_PC, (code - load_get_lmf_addr - 8));
		ARM_LDR_IMM (load_trampoline, ARMREG_IP, ARMREG_PC, (code + 4 - load_trampoline - 8));
	}

	code += 8;

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);

	/* Sanity check */
	g_assert ((code - buf) <= GEN_TRAMP_SIZE);

	*code_size = code - buf;

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		guint32 code_len;

		/* Initialize the nullified class init trampoline used in the AOT case */
		nullified_class_init_trampoline = mono_arch_get_nullified_class_init_trampoline (&code_len);
	}

	*out_unwind_ops = unwind_ops;

	return buf;
}

gpointer
mono_arch_get_nullified_class_init_trampoline (guint32 *code_len)
{
	guint8 *buf, *code;

	code = buf = mono_global_codeman_reserve (16);

	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_LR);

	mono_arch_flush_icache (buf, code - buf);

	*code_len = code - buf;

	return buf;
}

#define SPEC_TRAMP_SIZE 24

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	gpointer *constants;
	guint32 short_branch, size = SPEC_TRAMP_SIZE;

	tramp = mono_get_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	code = buf = mono_domain_code_reserve_align (domain, size, 4);
	if ((short_branch = branch_for_target_reachable (code + 8, tramp))) {
		size = 12;
		mono_domain_code_commit (domain, code, SPEC_TRAMP_SIZE, size);
	}
	mono_domain_unlock (domain);

	/* we could reduce this to 12 bytes if tramp is within reach:
	 * ARM_PUSH ()
	 * ARM_BL ()
	 * method-literal
	 * The called code can access method using the lr register
	 * A 20 byte sequence could be:
	 * ARM_PUSH ()
	 * ARM_MOV_REG_REG (lr, pc)
	 * ARM_LDR_IMM (pc, pc, 0)
	 * method-literal
	 * tramp-literal
	 */
	/* We save all the registers, except PC and SP */
	ARM_PUSH (code, 0x5fff);
	if (short_branch) {
		constants = (gpointer*)code;
		constants [0] = GUINT_TO_POINTER (short_branch | (1 << 24));
		constants [1] = arg1;
		code += 8;
	} else {
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 8); /* temp reg */
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);

		constants = (gpointer*)code;
		constants [0] = arg1;
		constants [1] = tramp;
		code += 8;
	}

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);

	g_assert ((code - buf) <= size);

	if (code_len)
		*code_len = code - buf;

	return buf;
}

#define arm_is_imm12(v) ((int)(v) > -4096 && (int)(v) < 4096)

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot)
{
	guint32 code_size;
	MonoJumpInfo *ji;

	return mono_arch_create_rgctx_lazy_fetch_trampoline_full (slot, &code_size, &ji, FALSE);
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline_full (guint32 slot, guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	int tramp_size;
	guint32 code_len;
	guint8 **rgctx_null_jumps;
	int depth, index;
	int i, njumps;
	gboolean mrgctx;

	*ji = NULL;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += sizeof (MonoMethodRuntimeGenericContext) / sizeof (gpointer);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 64 + 16 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));
	njumps = 0;

	/* The vtable/mrgctx is in R0 */
	g_assert (MONO_ARCH_VTABLE_REG == ARMREG_R0);

	if (mrgctx) {
		/* get mrgctx ptr */
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_R0);
 	} else {
		/* load rgctx ptr from vtable */
		g_assert (arm_is_imm12 (G_STRUCT_OFFSET (MonoVTable, runtime_generic_context)));
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R0, G_STRUCT_OFFSET (MonoVTable, runtime_generic_context));
		/* is the rgctx ptr null? */
		ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		ARM_B_COND (code, ARMCOND_EQ, 0);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0) {
			g_assert (arm_is_imm12 (sizeof (MonoMethodRuntimeGenericContext)));
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R1, sizeof (MonoMethodRuntimeGenericContext));
		} else {
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R1, 0);
		}
		/* is the ptr null? */
		ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		ARM_B_COND (code, ARMCOND_EQ, 0);
	}

	/* fetch slot */
	code = mono_arm_emit_load_imm (code, ARMREG_R2, sizeof (gpointer) * (index + 1));
	ARM_LDR_REG_REG (code, ARMREG_R1, ARMREG_R1, ARMREG_R2);
	/* is the slot null? */
	ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [njumps ++] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	/* otherwise return, result is in R1 */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_R1);
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_LR);

	g_assert (njumps <= depth + 2);
	for (i = 0; i < njumps; ++i)
		arm_patch (rgctx_null_jumps [i], code);

	g_free (rgctx_null_jumps);

	/* Slowpath */

	/* The vtable/mrgctx is still in R0 */

	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, g_strdup_printf ("specific_trampoline_lazy_fetch_%u", slot));
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_R1);
	} else {
		tramp = mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), &code_len);

		/* Jump to the actual trampoline */
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0); /* temp reg */
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
		*(gpointer*)code = tramp;
		code += 4;
	}

	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	*code_size = code - buf;

	return buf;
}

#define arm_is_imm8(v) ((v) > -256 && (v) < 256)

gpointer
mono_arch_create_generic_class_init_trampoline (void)
{
	guint32 code_size;
	MonoJumpInfo *ji;

	return mono_arch_create_generic_class_init_trampoline_full (&code_size, &ji, FALSE);
}

gpointer
mono_arch_create_generic_class_init_trampoline_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	static int byte_offset = -1;
	static guint8 bitmask;
	guint8 *jump;
	int tramp_size;
	guint32 code_len, imm8;
	gint rot_amount;

	*ji = NULL;

	tramp_size = 64;

	code = buf = mono_global_codeman_reserve (tramp_size);

	if (byte_offset < 0)
		mono_marshal_find_bitfield_offset (MonoVTable, initialized, &byte_offset, &bitmask);

	g_assert (arm_is_imm8 (byte_offset));
	ARM_LDRSB_IMM (code, ARMREG_IP, MONO_ARCH_VTABLE_REG, byte_offset);
	imm8 = mono_arm_is_rotated_imm8 (bitmask, &rot_amount);
	g_assert (imm8 >= 0);
	ARM_AND_REG_IMM (code, ARMREG_IP, ARMREG_IP, imm8, rot_amount);
	ARM_CMP_REG_IMM (code, ARMREG_IP, 0, 0);
	jump = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);

	/* Initialized case */
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_LR);	

	/* Uninitialized case */
	arm_patch (jump, code);

	if (aot) {
		*ji = mono_patch_info_list_prepend (*ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_generic_class_init");
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_R1);
	} else {
		tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_GENERIC_CLASS_INIT, mono_get_root_domain (), &code_len);

		/* Jump to the actual trampoline */
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0); /* temp reg */
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_R1);
		*(gpointer*)code = tramp;
		code += 4;
	}

	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	*code_size = code - buf;

	return buf;
}
