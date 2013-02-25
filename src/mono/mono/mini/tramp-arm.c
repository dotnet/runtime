/*
 * tramp-arm.c: JIT trampoline code for ARM
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001-2003 Ximian, Inc.
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/arm/arm-codegen.h>

#include "mini.h"
#include "mini-arm.h"

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

static guint8* nullified_class_init_trampoline;


#ifdef USE_JUMP_TABLES

static guint16
decode_imm16 (guint32 insn)
{
	return (((insn >> 16) & 0xf) << 12) | (insn & 0xfff);
}

#define INSN_MASK 0xff00000
#define MOVW_MASK ((3 << 24) | (0 << 20))
#define MOVT_MASK ((3 << 24) | (4 << 20))

gpointer*
mono_arch_jumptable_entry_from_code (guint8 *code)
{
	guint32 insn1 = ((guint32*)code) [0];
	guint32 insn2 = ((guint32*)code) [1];

	if (((insn1 & INSN_MASK) == MOVW_MASK) &&
	    ((insn2 & INSN_MASK) == MOVT_MASK) ) {
		guint32 imm_lo = decode_imm16 (insn1);
		guint32 imm_hi = decode_imm16 (insn2);
		return (gpointer*) GUINT_TO_POINTER (imm_lo | (imm_hi << 16));
	} else {
		g_assert_not_reached ();
		return NULL;
	}
}

#undef INSN_MASK
#undef MOVW_MASK
#undef MOVT_MASK

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	gpointer *jte;
	/*
	 * code_ptr is 4 instructions after MOVW/MOVT used to address
	 * jumptable entry.
	 */
	jte = mono_jumptable_get_entry (code_ptr - 16);
	g_assert ( jte != NULL);
	*jte = addr;
}
#else
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
#endif

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	guint8 *jump_entry;

	/* Patch the jump table entry used by the plt entry */
	if (*(guint32*)code == 0xe59fc000) {
		/* ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0); */
		guint32 offset = ((guint32*)code)[2];
		
		jump_entry = code + offset + 12;
	} else if (*(guint16*)(code - 4) == 0xf8df) {
		/* 
		 * Thumb PLT entry, begins with ldr.w ip, [pc, #8], code points to entry + 4, see
		 * mono_arm_get_thumb_plt_entry ().
		 */
		guint32 offset;

		code -= 4;
		offset = *(guint32*)(code + 12);
		jump_entry = code + offset + 8;
	} else {
		g_assert_not_reached ();
	}

	*(guint8**)jump_entry = addr;
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, mgreg_t *regs)
{
	mono_arch_patch_callsite (NULL, code, nullified_class_init_trampoline);
}

void
mono_arch_nullify_plt_entry (guint8 *code, mgreg_t *regs)
{
	if (mono_aot_only && !nullified_class_init_trampoline)
		nullified_class_init_trampoline = mono_aot_get_trampoline ("nullified_class_init_trampoline");

	mono_arch_patch_plt_entry (code, NULL, regs, nullified_class_init_trampoline);
}

#ifndef DISABLE_JIT

#define arm_is_imm12(v) ((int)(v) > -4096 && (int)(v) < 4096)

#ifndef USE_JUMP_TABLES
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
#endif

static inline guint8*
emit_bx (guint8* code, int reg)
{
	if (mono_arm_thumb_supported ())
		ARM_BX (code, reg);
	else
		ARM_MOV_REG_REG (code, ARMREG_PC, reg);
	return code;
}

/* Stack size for trampoline function 
 */
#define STACK ALIGN_TO (sizeof (MonoLMF), 8)

/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	guint8 *buf, *code = NULL;
#ifdef USE_JUMP_TABLES
	gpointer *load_get_lmf_addr = NULL, *load_trampoline = NULL;
#else
        guint8 *load_get_lmf_addr  = NULL, *load_trampoline  = NULL;
	gpointer *constants;
#endif

	int cfa_offset, lmf_offset, regsave_size, lr_offset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	int buf_len;

#ifdef USE_JUMP_TABLES
	g_assert (!aot);
#endif

	/* Now we'll create in 'buf' the ARM trampoline code. This
	 is the trampoline code common to all methods  */

	buf_len = 212;
	code = buf = mono_global_codeman_reserve (buf_len);

	/*
	 * At this point lr points to the specific arg and sp points to the saved
	 * regs on the stack (all but PC and SP). The original LR value has been
	 * saved as sp + LR_OFFSET by the push in the specific trampoline
	 */

	/* The offset of lmf inside the stack frame */
	lmf_offset = STACK - sizeof (MonoLMF);
	/* The size of the area already allocated by the push in the specific trampoline */
	regsave_size = 14 * sizeof (mgreg_t);
	/* The offset where lr was saved inside the regsave area */
	lr_offset = 13 * sizeof (mgreg_t);

	// FIXME: Finish the unwind info, the current info allows us to unwind
	// when the trampoline is not in the epilog

	// CFA = SP + (num registers pushed) * 4
	cfa_offset = 14 * sizeof (mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, cfa_offset);
	// PC saved at sp+LR_OFFSET
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_LR, -4);

	if (aot && tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
		/* 
		 * For page trampolines the data is in r1, so just move it, otherwise use the got slot as below.
		 * The trampoline contains a pc-relative offset to the got slot 
		 * preceeding the got slot where the value is stored. The offset can be
		 * found at [lr + 0].
		 */
		if (aot == 2) {
			ARM_MOV_REG_REG (code, ARMREG_V2, ARMREG_R1);
		} else {
			ARM_LDR_IMM (code, ARMREG_V2, ARMREG_LR, 0);
			ARM_ADD_REG_IMM (code, ARMREG_V2, ARMREG_V2, 4, 0);
			ARM_LDR_REG_REG (code, ARMREG_V2, ARMREG_V2, ARMREG_LR);
		}
	} else {
		if (tramp_type != MONO_TRAMPOLINE_GENERIC_CLASS_INIT) {
			ARM_LDR_IMM (code, ARMREG_V2, ARMREG_LR, 0);
		}
		else
			ARM_MOV_REG_REG (code, ARMREG_V2, MONO_ARCH_VTABLE_REG);
	}
	ARM_LDR_IMM (code, ARMREG_V3, ARMREG_SP, lr_offset);

	/* ok, now we can continue with the MonoLMF setup, mostly untouched 
	 * from emit_prolog in mini-arm.c
	 * This is a synthetized call to mono_get_lmf_addr ()
	 */
	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_get_lmf_addr");
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_R0, ARMREG_PC, ARMREG_R0);
	} else {
#ifdef USE_JUMP_TABLES
                load_get_lmf_addr = mono_jumptable_add_entry ();
                code = mono_arm_load_jumptable_entry (code, load_get_lmf_addr, ARMREG_R0);
#else
		load_get_lmf_addr = code;
		code += 4;
#endif
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_R0);

	/* we build the MonoLMF structure on the stack - see mini-arm.h
	 * The pointer to the struct is put in r1.
	 * the iregs array is already allocated on the stack by push.
	 */
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, STACK - regsave_size);
	cfa_offset += STACK - regsave_size;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	/* V1 == lmf */
	ARM_ADD_REG_IMM8 (code, ARMREG_V1, ARMREG_SP, STACK - sizeof (MonoLMF));

	/*
	 * The stack now looks like:
	 *       <saved regs>
	 * v1 -> <rest of LMF>
	 * sp -> <alignment>
	 */

	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (code, ARMREG_R2, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (code, ARMREG_V1, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* save method info (it's in v2) */
	if ((tramp_type == MONO_TRAMPOLINE_JIT) || (tramp_type == MONO_TRAMPOLINE_JUMP))
		ARM_STR_IMM (code, ARMREG_V2, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, method));
	else {
		ARM_MOV_REG_IMM8 (code, ARMREG_R2, 0);
		ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, method));
	}
	/* save caller SP */
	ARM_ADD_REG_IMM8 (code, ARMREG_R2, ARMREG_SP, cfa_offset);
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, sp));
	/* save caller FP */
	ARM_LDR_IMM (code, ARMREG_R2, ARMREG_V1, (G_STRUCT_OFFSET (MonoLMF, iregs) + ARMREG_FP*4));
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, fp));
	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (code, ARMREG_R2, 0);
	} else {
		ARM_LDR_IMM (code, ARMREG_R2, ARMREG_V1, (G_STRUCT_OFFSET (MonoLMF, iregs) + 13*4));
	}
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, ip));

	/*
	 * Now we're ready to call xxx_trampoline ().
	 */
	/* Arg 1: the saved registers */
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, iregs));

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
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
#ifdef USE_JUMP_TABLES
		load_trampoline = mono_jumptable_add_entry ();
		code = mono_arm_load_jumptable_entry (code, load_trampoline, ARMREG_IP);
#else
		load_trampoline = code;
		code += 4;
#endif
	}

	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_IP);

	/* OK, code address is now on r0. Move it to the place on the stack
	 * where IP was saved (it is now no more useful to us and it can be
	 * clobbered). This way we can just restore all the regs in one inst
	 * and branch to IP.
	 */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, iregs) + (ARMREG_R12 * sizeof (mgreg_t)));

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* 
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, "mono_thread_force_interruption_checkpoint");
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
#ifdef USE_JUMP_TABLES
		gpointer *jte = mono_jumptable_add_entry ();
		code = mono_arm_load_jumptable_entry (code, jte, ARMREG_IP);
		jte [0] = mono_thread_force_interruption_checkpoint;
#else
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = mono_thread_force_interruption_checkpoint;
		code += 4;
#endif
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_IP);

	/*
	 * Now we restore the MonoLMF (see emit_epilogue in mini-arm.c)
	 * and the rest of the registers, so the method called will see
	 * the same state as before we executed.
	 */
	/* ip = previous_lmf */
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (code, ARMREG_LR, ARMREG_V1, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_LR, G_STRUCT_OFFSET (MonoLMF, previous_lmf));

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore the registers and jump to the code:
	 * Note that IP has been conveniently set to the method addr.
	 */
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, STACK - regsave_size);
	ARM_POP_NWB (code, 0x5fff);
	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_IP);
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, regsave_size);
	if ((tramp_type == MONO_TRAMPOLINE_CLASS_INIT) || (tramp_type == MONO_TRAMPOLINE_GENERIC_CLASS_INIT) || (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH))
		code = emit_bx (code, ARMREG_LR);
	else
		code = emit_bx (code, ARMREG_IP);

#ifdef USE_JUMP_TABLES
	load_get_lmf_addr [0] = mono_get_lmf_addr;
	load_trampoline [0] = (gpointer)mono_get_trampoline_func (tramp_type);
#else
	constants = (gpointer*)code;
	constants [0] = mono_get_lmf_addr;
	constants [1] = (gpointer)mono_get_trampoline_func (tramp_type);

	if (!aot) {
		/* backpatch by emitting the missing instructions skipped above */
		ARM_LDR_IMM (load_get_lmf_addr, ARMREG_R0, ARMREG_PC, (code - load_get_lmf_addr - 8));
		ARM_LDR_IMM (load_trampoline, ARMREG_IP, ARMREG_PC, (code + 4 - load_trampoline - 8));
	}

	code += 8;
#endif

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);

	/* Sanity check */
	g_assert ((code - buf) <= buf_len);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		/* Initialize the nullified class init trampoline used in the AOT case */
		nullified_class_init_trampoline = mono_arch_get_nullified_class_init_trampoline (NULL);

	if (info)
		*info = mono_tramp_info_create (mono_get_generic_trampoline_name (tramp_type), buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_get_nullified_class_init_trampoline (MonoTrampInfo **info)
{
	guint8 *buf, *code;

	code = buf = mono_global_codeman_reserve (16);

	code = emit_bx (code, ARMREG_LR);

	mono_arch_flush_icache (buf, code - buf);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("nullified_class_init_trampoline"), buf, code - buf, NULL, NULL);

	return buf;
}

#define SPEC_TRAMP_SIZE 24

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	gpointer *constants;
#ifndef USE_JUMP_TABLES
	guint32 short_branch;
#endif
	guint32 size = SPEC_TRAMP_SIZE;

	tramp = mono_get_trampoline_code (tramp_type);

	mono_domain_lock (domain);
#ifdef USE_JUMP_TABLES
	code = buf = mono_domain_code_reserve_align (domain, size, 4);
#else
	code = buf = mono_domain_code_reserve_align (domain, size, 4);
	if ((short_branch = branch_for_target_reachable (code + 4, tramp))) {
		size = 12;
		mono_domain_code_commit (domain, code, SPEC_TRAMP_SIZE, size);
	}
#endif
	mono_domain_unlock (domain);

#ifdef USE_JUMP_TABLES
	/* For jumptables case we always generate the same code for trampolines,
	 * namely
	 *   push {r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, lr}
	 *   movw lr, lo(jte)
	 *   movt lr, hi(jte)
	 *   ldr r1, [lr + 4]
	 *   bx r1
	 */
	ARM_PUSH (code, 0x5fff);
	constants = mono_jumptable_add_entries (2);
	code = mono_arm_load_jumptable_entry_addr (code, constants, ARMREG_LR);
	ARM_LDR_IMM (code, ARMREG_R1, ARMREG_LR, 4);
	code = emit_bx (code, ARMREG_R1);
	constants [0] = arg1;
	constants [1] = tramp;
#else
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
		code = emit_bx (code, ARMREG_R1);

		constants = (gpointer*)code;
		constants [0] = arg1;
		constants [1] = tramp;
		code += 8;
	}
#endif

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);

	g_assert ((code - buf) <= size);

	if (code_len)
		*code_len = code - buf;

	return buf;
}

/*
 * mono_arch_get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	MonoDomain *domain = mono_domain_get ();
#ifdef USE_JUMP_TABLES
	gpointer *jte;
	guint32 size = 20;
#else
        guint32 size = 16;
#endif

	start = code = mono_domain_code_reserve (domain, size);

#ifdef USE_JUMP_TABLES
	jte = mono_jumptable_add_entry ();
	code = mono_arm_load_jumptable_entry (code, jte, ARMREG_IP);
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_R0, sizeof (MonoObject));
	code = emit_bx (code, ARMREG_IP);
	jte [0] = addr;
#else
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 4);
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_R0, sizeof (MonoObject));
	code = emit_bx (code, ARMREG_IP);
	*(guint32*)code = (guint32)addr;
	code += 4;
#endif
	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= size);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr)
{
	guint8 *code, *start;
#ifdef USE_JUMP_TABLES
	int buf_len = 20;
	gpointer *jte;
#else
	int buf_len = 16;
#endif
	MonoDomain *domain = mono_domain_get ();

	start = code = mono_domain_code_reserve (domain, buf_len);

#ifdef USE_JUMP_TABLES
	jte = mono_jumptable_add_entries (2);
	code = mono_arm_load_jumptable_entry_addr (code, jte, ARMREG_IP);
	ARM_LDR_IMM (code, MONO_ARCH_RGCTX_REG, ARMREG_IP, 0);
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 4);
	ARM_BX (code, ARMREG_IP);
	jte [0] = mrgctx;
	jte [1] = addr;
#else
	ARM_LDR_IMM (code, MONO_ARCH_RGCTX_REG, ARMREG_PC, 0);
	ARM_LDR_IMM (code, ARMREG_PC, ARMREG_PC, 0);
	*(guint32*)code = (guint32)mrgctx;
	code += 4;
	*(guint32*)code = (guint32)addr;
	code += 4;
#endif

	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);

	return start;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	int tramp_size;
	guint32 code_len;
	guint8 **rgctx_null_jumps;
	int depth, index;
	int i, njumps;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
#ifdef USE_JUMP_TABLES
	gpointer *jte;
#endif

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (gpointer);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 64 + 16 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, 0);

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
			g_assert (arm_is_imm12 (MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT));
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R1, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT);
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
	code = emit_bx (code, ARMREG_LR);

	g_assert (njumps <= depth + 2);
	for (i = 0; i < njumps; ++i)
		arm_patch (rgctx_null_jumps [i], code);

	g_free (rgctx_null_jumps);

	/* Slowpath */

	/* The vtable/mrgctx is still in R0 */

	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, g_strdup_printf ("specific_trampoline_lazy_fetch_%u", slot));
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_R1);
	} else {
		tramp = mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), &code_len);

		/* Jump to the actual trampoline */
#ifdef USE_JUMP_TABLES
		jte = mono_jumptable_add_entry ();
		jte [0] = tramp;
		code = mono_arm_load_jumptable_entry (code, jte, ARMREG_R1);
		code = emit_bx (code, ARMREG_R1);
#else
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0); /* temp reg */
		code = emit_bx (code, ARMREG_R1);
		*(gpointer*)code = tramp;
		code += 4;
#endif
	}

	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create (mono_get_rgctx_fetch_trampoline_name (slot), buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int tramp_size;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (aot);

	tramp_size = 32;

	code = buf = mono_global_codeman_reserve (tramp_size);

	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, 0);

	// FIXME: Currently, we always go to the slow path.
	/* Load trampoline addr */
	ARM_LDR_IMM (code, ARMREG_R1, MONO_ARCH_RGCTX_REG, 4);
	/* The vtable/mrgctx is in R0 */
	g_assert (MONO_ARCH_VTABLE_REG == ARMREG_R0);
	code = emit_bx (code, ARMREG_R1);

	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, ji, unwind_ops);

	return buf;
}

#define arm_is_imm8(v) ((v) > -256 && (v) < 256)

gpointer
mono_arch_create_generic_class_init_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	static int byte_offset = -1;
	static guint8 bitmask;
	guint8 *jump;
	int tramp_size;
	guint32 code_len, imm8;
	gint rot_amount;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

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
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, "specific_trampoline_generic_class_init");
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_R1);
	} else {
#ifdef USE_JUMP_TABLES
		gpointer *jte = mono_jumptable_add_entry ();
#endif
		tramp = mono_arch_create_specific_trampoline (NULL, MONO_TRAMPOLINE_GENERIC_CLASS_INIT, mono_get_root_domain (), &code_len);

		/* Jump to the actual trampoline */
#ifdef USE_JUMP_TABLES
		code = mono_arm_load_jumptable_entry (code, jte, ARMREG_R1);
		jte [0] = tramp;
		code = emit_bx (code, ARMREG_R1);
#else
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0); /* temp reg */
		code = emit_bx (code, ARMREG_R1);
		*(gpointer*)code = tramp;
		code += 4;
#endif
	}

	mono_arch_flush_icache (buf, code - buf);

	g_assert (code - buf <= tramp_size);

	if (info)
		*info = mono_tramp_info_create (g_strdup_printf ("generic_class_init_trampoline"), buf, code - buf, ji, unwind_ops);

	return buf;
}

#else

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_generic_class_init_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}
	
#endif /* DISABLE_JIT */

guint8*
mono_arch_get_call_target (guint8 *code)
{
	guint32 ins = ((guint32*)(gpointer)code) [-1];

#if MONOTOUCH
	/* Should be a 'bl' or a 'b' */
	if (((ins >> 25) & 0x7) == 0x5) {
#else
	/* Should be a 'bl' */
	if ((((ins >> 25) & 0x7) == 0x5) && (((ins >> 24) & 0x1) == 0x1)) {
#endif
		gint32 disp = ((gint32)ins) & 0xffffff;
		guint8 *target = code - 4 + 8 + (disp * 4);

		return target;
	} else {
		return NULL;
	}
}

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, mgreg_t *regs, guint8 *code)
{
	/* The offset is stored as the 4th word of the plt entry */
	return ((guint32*)plt_entry) [3];
}

/*
 * Return the address of the PLT entry called by the thumb code CODE.
 */
guint8*
mono_arm_get_thumb_plt_entry (guint8 *code)
{
	int s, j1, j2, imm10, imm11, i1, i2, imm32;
	guint8 *bl, *base;
	guint16 t1, t2;
	guint8 *target;

	/* code should be right after a BL */
	code = (guint8*)((mgreg_t)code & ~1);
	base = (guint8*)((mgreg_t)code & ~3);
	bl = code - 4;
	t1 = ((guint16*)bl) [0];
	t2 = ((guint16*)bl) [1];

	g_assert ((t1 >> 11) == 0x1e);

	s = (t1 >> 10) & 0x1;
	imm10 = (t1 >> 0) & 0x3ff;
	j1 = (t2 >> 13) & 0x1;
	j2 = (t2 >> 11) & 0x1;
	imm11 = t2 & 0x7ff;

	i1 = (s ^ j1) ? 0 : 1;
	i2 = (s ^ j2) ? 0 : 1;

	imm32 = (imm11 << 1) | (imm10 << 12) | (i2 << 22) | (i1 << 23);
	// FIXME:
	g_assert (s == 0);

	target = code + imm32;

	/* target now points to the thumb plt entry */
	/* ldr.w r12, [pc, #8] */
	g_assert (((guint16*)target) [0] == 0xf8df);
	g_assert (((guint16*)target) [1] == 0xc008);

	/* 
	 * The PLT info offset is at offset 16, but mono_arch_get_plt_entry_offset () returns
	 * the 3rd word, so compensate by returning a different value.
	 */
	target += 4;

	return target;
}

#ifndef DISABLE_JIT

/*
 * mono_arch_get_gsharedvt_arg_trampoline:
 *
 *   See tramp-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;
	gpointer *constants;

	buf_len = 24;

	start = code = mono_domain_code_reserve (domain, buf_len);

	/* Similar to the specialized trampoline code */
	ARM_PUSH (code, (1 << ARMREG_R0) | (1 << ARMREG_R1) | (1 << ARMREG_R2) | (1 << ARMREG_R3) | (1 << ARMREG_LR));
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 8);
	/* arg is passed in LR */
	ARM_LDR_IMM (code, ARMREG_LR, ARMREG_PC, 0);
	code = emit_bx (code, ARMREG_IP);
	constants = (gpointer*)code;
	constants [0] = arg;
	constants [1] = addr;
	code += 8;

	g_assert ((code - start) <= buf_len);

	nacl_domain_code_validate (domain, &start, buf_len, &code);
	mono_arch_flush_icache (start, code - start);

	return start;
}

#else

gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

#if defined(MONOTOUCH) || defined(MONO_EXTENSIONS)

#include "../../../mono-extensions/mono/mini/tramp-arm-gsharedvt.c"

#else

gpointer
mono_arm_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer *caller_regs, gpointer *callee_regs, gpointer mrgctx_reg)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	if (info)
		*info = NULL;
	return NULL;
}

#endif /* !MONOTOUCH */
