/**
 * \file
 * ARM backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"
#include <string.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/unlocked.h>

#include "interp/interp.h"

#include "mini-arm.h"
#include "cpu-arm.h"
#include "ir-emit.h"
#include "debugger-agent.h"
#include "mini-gc.h"
#include "mini-runtime.h"
#include "aot-runtime.h"
#include "mono/arch/arm/arm-vfp-codegen.h"

/* Sanity check: This makes no sense */
#if defined(ARM_FPU_NONE) && (defined(ARM_FPU_VFP) || defined(ARM_FPU_VFP_HARD))
#error "ARM_FPU_NONE is defined while one of ARM_FPU_VFP/ARM_FPU_VFP_HARD is defined"
#endif

/*
 * IS_SOFT_FLOAT: Is full software floating point used?
 * IS_HARD_FLOAT: Is full hardware floating point used?
 * IS_VFP: Is hardware floating point with software ABI used?
 *
 * These are not necessarily constants, e.g. IS_SOFT_FLOAT and
 * IS_VFP may delegate to mono_arch_is_soft_float ().
 */

#if defined(ARM_FPU_VFP_HARD)
#define IS_SOFT_FLOAT (FALSE)
#define IS_HARD_FLOAT (TRUE)
#define IS_VFP (TRUE)
#elif defined(ARM_FPU_NONE)
#define IS_SOFT_FLOAT (mono_arch_is_soft_float ())
#define IS_HARD_FLOAT (FALSE)
#define IS_VFP (!mono_arch_is_soft_float ())
#else
#define IS_SOFT_FLOAT (FALSE)
#define IS_HARD_FLOAT (FALSE)
#define IS_VFP (TRUE)
#endif

#define THUNK_SIZE (3 * 4)

#if __APPLE__
G_BEGIN_DECLS
void sys_icache_invalidate (void *start, size_t len);
G_END_DECLS
#endif

/* This mutex protects architecture specific caches */
#define mono_mini_arch_lock() mono_os_mutex_lock (&mini_arch_mutex)
#define mono_mini_arch_unlock() mono_os_mutex_unlock (&mini_arch_mutex)
static mono_mutex_t mini_arch_mutex;

static gboolean v5_supported = FALSE;
static gboolean v6_supported = FALSE;
static gboolean v7_supported = FALSE;
static gboolean v7s_supported = FALSE;
static gboolean v7k_supported = FALSE;
static gboolean thumb_supported = FALSE;
static gboolean thumb2_supported = FALSE;
/*
 * Whenever to use the ARM EABI
 */
static gboolean eabi_supported = FALSE;

/* 
 * Whenever to use the iphone ABI extensions:
 * http://developer.apple.com/library/ios/documentation/Xcode/Conceptual/iPhoneOSABIReference/index.html
 * Basically, r7 is used as a frame pointer and it should point to the saved r7 + lr.
 * This is required for debugging/profiling tools to work, but it has some overhead so it should
 * only be turned on in debug builds.
 */
static gboolean iphone_abi = FALSE;

/*
 * The FPU we are generating code for. This is NOT runtime configurable right now,
 * since some things like MONO_ARCH_CALLEE_FREGS still depend on defines.
 */
static MonoArmFPU arm_fpu;

#if defined(ARM_FPU_VFP_HARD)
/*
 * On armhf, d0-d7 are used for argument passing and d8-d15
 * must be preserved across calls, which leaves us no room
 * for scratch registers. So we use d14-d15 but back up their
 * previous contents to a stack slot before using them - see
 * mono_arm_emit_vfp_scratch_save/_restore ().
 */
static int vfp_scratch1 = ARM_VFP_D14;
static int vfp_scratch2 = ARM_VFP_D15;
#else
/*
 * On armel, d0-d7 do not need to be preserved, so we can
 * freely make use of them as scratch registers.
 */
static int vfp_scratch1 = ARM_VFP_D0;
static int vfp_scratch2 = ARM_VFP_D1;
#endif

static int i8_align;

static gpointer single_step_tramp, breakpoint_tramp;

/*
 * The code generated for sequence points reads from this location, which is
 * made read-only when single stepping is enabled.
 */
static gpointer ss_trigger_page;

/* Enabled breakpoints read from this trigger page */
static gpointer bp_trigger_page;

/*
 * TODO:
 * floating point support: on ARM it is a mess, there are at least 3
 * different setups, each of which binary incompat with the other.
 * 1) FPA: old and ugly, but unfortunately what current distros use
 *    the double binary format has the two words swapped. 8 double registers.
 *    Implemented usually by kernel emulation.
 * 2) softfloat: the compiler emulates all the fp ops. Usually uses the
 *    ugly swapped double format (I guess a softfloat-vfp exists, too, though).
 * 3) VFP: the new and actually sensible and useful FP support. Implemented
 *    in HW or kernel-emulated, requires new tools. I think this is what symbian uses.
 *
 * We do not care about FPA. We will support soft float and VFP.
 */
#define arm_is_imm12(v) ((v) > -4096 && (v) < 4096)
#define arm_is_imm8(v) ((v) > -256 && (v) < 256)
#define arm_is_fpimm8(v) ((v) >= -1020 && (v) <= 1020)

#define LDR_MASK ((0xf << ARMCOND_SHIFT) | (3 << 26) | (1 << 22) | (1 << 20) | (15 << 12))
#define LDR_PC_VAL ((ARMCOND_AL << ARMCOND_SHIFT) | (1 << 26) | (0 << 22) | (1 << 20) | (15 << 12))
#define IS_LDR_PC(val) (((val) & LDR_MASK) == LDR_PC_VAL)

//#define DEBUG_IMT 0

#ifndef DISABLE_JIT
static void mono_arch_compute_omit_fp (MonoCompile *cfg);
#endif

static guint8*
emit_aotconst (MonoCompile *cfg, guint8 *code, int dreg, int patch_type, gpointer data);

const char*
mono_arch_regname (int reg)
{
	static const char * rnames[] = {
		"arm_r0", "arm_r1", "arm_r2", "arm_r3", "arm_v1",
		"arm_v2", "arm_v3", "arm_v4", "arm_v5", "arm_v6",
		"arm_v7", "arm_fp", "arm_ip", "arm_sp", "arm_lr",
		"arm_pc"
	};
	if (reg >= 0 && reg < 16)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg)
{
	static const char * rnames[] = {
		"arm_f0", "arm_f1", "arm_f2", "arm_f3", "arm_f4",
		"arm_f5", "arm_f6", "arm_f7", "arm_f8", "arm_f9",
		"arm_f10", "arm_f11", "arm_f12", "arm_f13", "arm_f14",
		"arm_f15", "arm_f16", "arm_f17", "arm_f18", "arm_f19",
		"arm_f20", "arm_f21", "arm_f22", "arm_f23", "arm_f24",
		"arm_f25", "arm_f26", "arm_f27", "arm_f28", "arm_f29",
		"arm_f30", "arm_f31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}


#ifndef DISABLE_JIT
static guint8*
emit_big_add_temp (guint8 *code, int dreg, int sreg, int imm, int temp)
{
	int imm8, rot_amount;

	g_assert (temp == ARMREG_IP || temp == ARMREG_LR);

	if (imm == 0) {
		if (sreg != dreg)
			ARM_MOV_REG_REG (code, dreg, sreg);
	} else if ((imm8 = mono_arm_is_rotated_imm8 (imm, &rot_amount)) >= 0) {
		ARM_ADD_REG_IMM (code, dreg, sreg, imm8, rot_amount);
		return code;
	}
	if (dreg == sreg) {
		code = mono_arm_emit_load_imm (code, temp, imm);
		ARM_ADD_REG_REG (code, dreg, sreg, temp);
	} else {
		code = mono_arm_emit_load_imm (code, dreg, imm);
		ARM_ADD_REG_REG (code, dreg, dreg, sreg);
	}
	return code;
}

static guint8*
emit_big_add (guint8 *code, int dreg, int sreg, int imm)
{
	return emit_big_add_temp (code, dreg, sreg, imm, ARMREG_IP);
}

static guint8*
emit_ldr_imm (guint8 *code, int dreg, int sreg, int imm)
{
	if (!arm_is_imm12 (imm)) {
		g_assert (dreg != sreg);
		code = emit_big_add (code, dreg, sreg, imm);
		ARM_LDR_IMM (code, dreg, dreg, 0);
	} else {
		ARM_LDR_IMM (code, dreg, sreg, imm);
	}
	return code;
}

/* If dreg == sreg, this clobbers IP */
static guint8*
emit_sub_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int imm8, rot_amount;
	if ((imm8 = mono_arm_is_rotated_imm8 (imm, &rot_amount)) >= 0) {
		ARM_SUB_REG_IMM (code, dreg, sreg, imm8, rot_amount);
		return code;
	}
	if (dreg == sreg) {
		code = mono_arm_emit_load_imm (code, ARMREG_IP, imm);
		ARM_SUB_REG_REG (code, dreg, sreg, ARMREG_IP);
	} else {
		code = mono_arm_emit_load_imm (code, dreg, imm);
		ARM_SUB_REG_REG (code, dreg, dreg, sreg);
	}
	return code;
}

static guint8*
emit_memcpy (guint8 *code, int size, int dreg, int doffset, int sreg, int soffset)
{
	/* we can use r0-r3, since this is called only for incoming args on the stack */
	if (size > sizeof (target_mgreg_t) * 4) {
		guint8 *start_loop;
		code = emit_big_add (code, ARMREG_R0, sreg, soffset);
		code = emit_big_add (code, ARMREG_R1, dreg, doffset);
		start_loop = code = mono_arm_emit_load_imm (code, ARMREG_R2, size);
		ARM_LDR_IMM (code, ARMREG_R3, ARMREG_R0, 0);
		ARM_STR_IMM (code, ARMREG_R3, ARMREG_R1, 0);
		ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_R0, 4);
		ARM_ADD_REG_IMM8 (code, ARMREG_R1, ARMREG_R1, 4);
		ARM_SUBS_REG_IMM8 (code, ARMREG_R2, ARMREG_R2, 4);
		ARM_B_COND (code, ARMCOND_NE, 0);
		arm_patch (code - 4, start_loop);
		return code;
	}
	if (arm_is_imm12 (doffset) && arm_is_imm12 (doffset + size) &&
			arm_is_imm12 (soffset) && arm_is_imm12 (soffset + size)) {
		while (size >= 4) {
			ARM_LDR_IMM (code, ARMREG_LR, sreg, soffset);
			ARM_STR_IMM (code, ARMREG_LR, dreg, doffset);
			doffset += 4;
			soffset += 4;
			size -= 4;
		}
	} else if (size) {
		code = emit_big_add (code, ARMREG_R0, sreg, soffset);
		code = emit_big_add (code, ARMREG_R1, dreg, doffset);
		doffset = soffset = 0;
		while (size >= 4) {
			ARM_LDR_IMM (code, ARMREG_LR, ARMREG_R0, soffset);
			ARM_STR_IMM (code, ARMREG_LR, ARMREG_R1, doffset);
			doffset += 4;
			soffset += 4;
			size -= 4;
		}
	}
	g_assert (size == 0);
	return code;
}

static guint8*
emit_jmp_reg (guint8 *code, int reg)
{
	if (thumb_supported)
		ARM_BX (code, reg);
	else
		ARM_MOV_REG_REG (code, ARMREG_PC, reg);
	return code;
}

static guint8*
emit_call_reg (guint8 *code, int reg)
{
	if (v5_supported) {
		ARM_BLX_REG (code, reg);
	} else {
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		return emit_jmp_reg (code, reg);
	}
	return code;
}

static guint8*
emit_call_seq (MonoCompile *cfg, guint8 *code)
{
	if (cfg->method->dynamic) {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		code = emit_call_reg (code, ARMREG_IP);
	} else {
		ARM_BL (code, 0);
	}
	cfg->thunk_area += THUNK_SIZE;
	return code;
}

guint8*
mono_arm_patchable_b (guint8 *code, int cond)
{
	ARM_B_COND (code, cond, 0);
	return code;
}

guint8*
mono_arm_patchable_bl (guint8 *code, int cond)
{
	ARM_BL_COND (code, cond, 0);
	return code;
}

#if defined(__ARM_EABI__) && defined(__linux__) && !defined(HOST_ANDROID) && !defined(MONO_CROSS_COMPILE)
#define HAVE_AEABI_READ_TP 1
#endif

#ifdef HAVE_AEABI_READ_TP
G_BEGIN_DECLS
gpointer __aeabi_read_tp (void);
G_END_DECLS
#endif

gboolean
mono_arch_have_fast_tls (void)
{
#ifdef HAVE_AEABI_READ_TP
	static gboolean have_fast_tls = FALSE;
        static gboolean inited = FALSE;

	if (mini_get_debug_options ()->use_fallback_tls)
		return FALSE;

	if (inited)
		return have_fast_tls;

	if (v7_supported) {
		gpointer tp1, tp2;

		tp1 = __aeabi_read_tp ();
		asm volatile("mrc p15, 0, %0, c13, c0, 3" : "=r" (tp2));

		have_fast_tls = tp1 && tp1 == tp2;
	}
	inited = TRUE;
	return have_fast_tls;
#else
	return FALSE;
#endif
}

static guint8*
emit_tls_get (guint8 *code, int dreg, int tls_offset)
{
	g_assert (v7_supported);
	ARM_MRC (code, 15, 0, dreg, 13, 0, 3);
	ARM_LDR_IMM (code, dreg, dreg, tls_offset);
	return code;
}

static guint8*
emit_tls_set (guint8 *code, int sreg, int tls_offset)
{
	int tp_reg = (sreg != ARMREG_R0) ? ARMREG_R0 : ARMREG_R1;
	g_assert (v7_supported);
	ARM_MRC (code, 15, 0, tp_reg, 13, 0, 3);
	ARM_STR_IMM (code, sreg, tp_reg, tls_offset);
	return code;
}

/*
 * emit_save_lmf:
 *
 *   Emit code to push an LMF structure on the LMF stack.
 * On arm, this is intermixed with the initialization of other fields of the structure.
 */
static guint8*
emit_save_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset)
{
	int i;

	if (mono_arch_have_fast_tls () && mono_tls_get_tls_offset (TLS_KEY_LMF_ADDR) != -1) {
		code = emit_tls_get (code, ARMREG_R0, mono_tls_get_tls_offset (TLS_KEY_LMF_ADDR));
	} else {
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
							 GUINT_TO_POINTER (MONO_JIT_ICALL_mono_tls_get_lmf_addr));
		code = emit_call_seq (cfg, code);
	}
	/* we build the MonoLMF structure on the stack - see mini-arm.h */
	/* lmf_offset is the offset from the previous stack pointer,
	 * alloc_size is the total stack space allocated, so the offset
	 * of MonoLMF from the current stack ptr is alloc_size - lmf_offset.
	 * The pointer to the struct is put in r1 (new_lmf).
	 * ip is used as scratch
	 * The callee-saved registers are already in the MonoLMF structure
	 */
	code = emit_big_add (code, ARMREG_R1, ARMREG_SP, lmf_offset);
	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_R1, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R0, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_R1, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (code, ARMREG_R1, ARMREG_R0, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* Skip method (only needed for trampoline LMF frames) */
	ARM_STR_IMM (code, ARMREG_SP, ARMREG_R1, MONO_STRUCT_OFFSET (MonoLMF, sp));
	ARM_STR_IMM (code, ARMREG_FP, ARMREG_R1, MONO_STRUCT_OFFSET (MonoLMF, fp));
	/* save the current IP */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_PC);
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_R1, MONO_STRUCT_OFFSET (MonoLMF, ip));

	for (i = 0; i < MONO_ABI_SIZEOF (MonoLMF); i += sizeof (target_mgreg_t))
		mini_gc_set_slot_type_from_fp (cfg, lmf_offset + i, SLOT_NOREF);

	return code;
}

typedef struct {
	gint32 vreg;
	gint32 hreg;
} FloatArgData;

static guint8 *
emit_float_args (MonoCompile *cfg, MonoCallInst *inst, guint8 *code, int *max_len, guint *offset)
{
	GSList *list;

	set_code_cursor (cfg, code);

	for (list = inst->float_args; list; list = list->next) {
		FloatArgData *fad = (FloatArgData*)list->data;
		MonoInst *var = get_vreg_to_inst (cfg, fad->vreg);
		gboolean imm = arm_is_fpimm8 (var->inst_offset);

		/* 4+1 insns for emit_big_add () and 1 for FLDS. */
		if (!imm)
			*max_len += 20 + 4;

		*max_len += 4;

		code = realloc_code (cfg, *max_len);

		if (!imm) {
			code = emit_big_add (code, ARMREG_LR, var->inst_basereg, var->inst_offset);
			ARM_FLDS (code, fad->hreg, ARMREG_LR, 0);
		} else
			ARM_FLDS (code, fad->hreg, var->inst_basereg, var->inst_offset);

		set_code_cursor (cfg, code);
		*offset = code - cfg->native_code;
	}

	return code;
}

static guint8 *
mono_arm_emit_vfp_scratch_save (MonoCompile *cfg, guint8 *code, int reg)
{
	MonoInst *inst;

	g_assert (reg == vfp_scratch1 || reg == vfp_scratch2);

	inst = cfg->arch.vfp_scratch_slots [reg == vfp_scratch1 ? 0 : 1];

	if (IS_HARD_FLOAT) {
		if (!arm_is_fpimm8 (inst->inst_offset)) {
			code = emit_big_add (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
			ARM_FSTD (code, reg, ARMREG_LR, 0);
		} else
			ARM_FSTD (code, reg, inst->inst_basereg, inst->inst_offset);
	}

	return code;
}

static guint8 *
mono_arm_emit_vfp_scratch_restore (MonoCompile *cfg, guint8 *code, int reg)
{
	MonoInst *inst;

	g_assert (reg == vfp_scratch1 || reg == vfp_scratch2);

	inst = cfg->arch.vfp_scratch_slots [reg == vfp_scratch1 ? 0 : 1];

	if (IS_HARD_FLOAT) {
		if (!arm_is_fpimm8 (inst->inst_offset)) {
			code = emit_big_add (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
			ARM_FLDD (code, reg, ARMREG_LR, 0);
		} else
			ARM_FLDD (code, reg, inst->inst_basereg, inst->inst_offset);
	}

	return code;
}

/*
 * emit_restore_lmf:
 *
 *   Emit code to pop an LMF structure from the LMF stack.
 */
static guint8*
emit_restore_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset)
{
	int basereg, offset;

	if (lmf_offset < 32) {
		basereg = cfg->frame_reg;
		offset = lmf_offset;
	} else {
		basereg = ARMREG_R2;
		offset = 0;
		code = emit_big_add (code, ARMREG_R2, cfg->frame_reg, lmf_offset);
	}

	/* ip = previous_lmf */
	ARM_LDR_IMM (code, ARMREG_IP, basereg, offset + MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (code, ARMREG_LR, basereg, offset + MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_LR, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));

	return code;
}

#endif /* #ifndef DISABLE_JIT */

/*
 * mono_arch_get_argument_info:
 * @csig:  a method signature
 * @param_count: the number of parameters to consider
 * @arg_info: an array to store the result infos
 *
 * Gathers information on parameters such as size, alignment and
 * padding. arg_info should be large enought to hold param_count + 1 entries. 
 *
 * Returns the size of the activation frame.
 */
int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	int k, frame_size = 0;
	guint32 size, align, pad;
	int offset = 8;
	MonoType *t;

	t = mini_get_underlying_type (csig->ret);
	if (MONO_TYPE_ISSTRUCT (t)) {
		frame_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	arg_info [0].offset = offset;

	if (csig->hasthis) {
		frame_size += sizeof (target_mgreg_t);
		offset += 4;
	}

	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		size = mini_type_stack_size_full (csig->params [k], &align, csig->pinvoke);

		/* ignore alignment for now */
		align = 1;

		frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);	
		arg_info [k].pad = pad;
		frame_size += size;
		arg_info [k + 1].pad = 0;
		arg_info [k + 1].size = size;
		offset += pad;
		arg_info [k + 1].offset = offset;
		offset += size;
	}

	align = MONO_ARCH_FRAME_ALIGNMENT;
	frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);
	arg_info [k].pad = pad;

	return frame_size;
}

#define MAX_ARCH_DELEGATE_PARAMS 3

static guint8*
get_delegate_invoke_impl (MonoTrampInfo **info, gboolean has_target, gboolean param_count)
{
	guint8 *code, *start;
	GSList *unwind_ops = mono_arch_get_cie_program ();

	if (has_target) {
		start = code = mono_global_codeman_reserve (12);

		/* Replace the this argument with the target */
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, target));
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

		g_assert ((code - start) <= 12);

		mono_arch_flush_icache (start, 12);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	} else {
		int size, i;

		size = 8 + param_count * 4;
		start = code = mono_global_codeman_reserve (size);

		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i) {
			ARM_MOV_REG_REG (code, (ARMREG_R0 + i), (ARMREG_R0 + i + 1));
		}
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
		MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));
	}

	if (has_target) {
		 *info = mono_tramp_info_create ("delegate_invoke_impl_has_target", start, code - start, NULL, unwind_ops);
	} else {
		 char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", param_count);
		 *info = mono_tramp_info_create (name, start, code - start, NULL, unwind_ops);
		 g_free (name);
	}

	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL));

	return start;
}

/*
 * mono_arch_get_delegate_invoke_impls:
 *
 *   Return a list of MonoAotTrampInfo structures for the delegate invoke impl
 * trampolines.
 */
GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	MonoTrampInfo *info;
	int i;

	get_delegate_invoke_impl (&info, TRUE, 0);
	res = g_slist_prepend (res, info);

	for (i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		get_delegate_invoke_impl (&info, FALSE, i);
		res = g_slist_prepend (res, info);
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;
	MonoType *sig_ret;

	/* FIXME: Support more cases */
	sig_ret = mini_get_underlying_type (sig->ret);
	if (MONO_TYPE_ISSTRUCT (sig_ret))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;
		mono_mini_arch_lock ();
		if (cached) {
			mono_mini_arch_unlock ();
			return cached;
		}

		if (mono_ee_features.use_aot_trampolines) {
			start = (guint8*)mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, TRUE, 0);
			mono_tramp_info_register (info, NULL);
		}
		cached = start;
		mono_mini_arch_unlock ();
		return cached;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i;

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		mono_mini_arch_lock ();
		code = cache [sig->param_count];
		if (code) {
			mono_mini_arch_unlock ();
			return code;
		}

		if (mono_ee_features.use_aot_trampolines) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = (guint8*)mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			MonoTrampInfo *info;
			start = get_delegate_invoke_impl (&info, FALSE, sig->param_count);
			mono_tramp_info_register (info, NULL);
		}
		cache [sig->param_count] = start;
		mono_mini_arch_unlock ();
		return start;
	}

	return NULL;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg)
{
	return NULL;
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	return (gpointer)regs [ARMREG_R0];
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
	i8_align = MONO_ABI_ALIGNOF (gint64);
#ifdef MONO_CROSS_COMPILE
	/* Need to set the alignment of i8 since it can different on the target */
#ifdef TARGET_ANDROID
	/* linux gnueabi */
	mono_type_set_alignment (MONO_TYPE_I8, i8_align);
#endif
#endif
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
	char *cpu_arch;

#ifdef TARGET_WATCHOS
	mini_get_debug_options ()->soft_breakpoints = TRUE;
#endif

	mono_os_mutex_init_recursive (&mini_arch_mutex);
	if (mini_get_debug_options ()->soft_breakpoints) {
		if (!mono_aot_only)
			breakpoint_tramp = mini_get_breakpoint_trampoline ();
	} else {
		ss_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ|MONO_MMAP_32BIT, MONO_MEM_ACCOUNT_OTHER);
		bp_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ|MONO_MMAP_32BIT, MONO_MEM_ACCOUNT_OTHER);
		mono_mprotect (bp_trigger_page, mono_pagesize (), 0);
	}

	mono_aot_register_jit_icall ("mono_arm_throw_exception", mono_arm_throw_exception);
	mono_aot_register_jit_icall ("mono_arm_throw_exception_by_token", mono_arm_throw_exception_by_token);
	mono_aot_register_jit_icall ("mono_arm_resume_unwind", mono_arm_resume_unwind);
#if defined(MONO_ARCH_GSHAREDVT_SUPPORTED)
	mono_aot_register_jit_icall ("mono_arm_start_gsharedvt_call", mono_arm_start_gsharedvt_call);
#endif
	mono_aot_register_jit_icall ("mono_arm_unaligned_stack", mono_arm_unaligned_stack);
#if defined(__ARM_EABI__)
	eabi_supported = TRUE;
#endif

#if defined(ARM_FPU_VFP_HARD)
	arm_fpu = MONO_ARM_FPU_VFP_HARD;
#else
	arm_fpu = MONO_ARM_FPU_VFP;

#if defined(ARM_FPU_NONE) && !defined(TARGET_IOS)
	/*
	 * If we're compiling with a soft float fallback and it
	 * turns out that no VFP unit is available, we need to
	 * switch to soft float. We don't do this for iOS, since
	 * iOS devices always have a VFP unit.
	 */
	if (!mono_hwcap_arm_has_vfp)
		arm_fpu = MONO_ARM_FPU_NONE;

	/*
	 * This environment variable can be useful in testing
	 * environments to make sure the soft float fallback
	 * works. Most ARM devices have VFP units these days, so
	 * normally soft float code would not be exercised much.
	 */
	char *soft = g_getenv ("MONO_ARM_FORCE_SOFT_FLOAT");

	if (soft && !strncmp (soft, "1", 1))
		arm_fpu = MONO_ARM_FPU_NONE;
	g_free (soft);
#endif
#endif

	v5_supported = mono_hwcap_arm_is_v5;
	v6_supported = mono_hwcap_arm_is_v6;
	v7_supported = mono_hwcap_arm_is_v7;

	/*
	 * On weird devices, the hwcap code may fail to detect
	 * the ARM version. In that case, we can at least safely
	 * assume the version the runtime was compiled for.
	 */
#ifdef HAVE_ARMV5
	v5_supported = TRUE;
#endif
#ifdef HAVE_ARMV6
	v6_supported = TRUE;
#endif
#ifdef HAVE_ARMV7
	v7_supported = TRUE;
#endif

#if defined(TARGET_IOS)
	/* iOS is special-cased here because we don't yet
	   have a way to properly detect CPU features on it. */
	thumb_supported = TRUE;
	iphone_abi = TRUE;
#else
	thumb_supported = mono_hwcap_arm_has_thumb;
	thumb2_supported = mono_hwcap_arm_has_thumb2;
#endif

	/* Format: armv(5|6|7[s])[-thumb[2]] */
	cpu_arch = g_getenv ("MONO_CPU_ARCH");

	/* Do this here so it overrides any detection. */
	if (cpu_arch) {
		if (strncmp (cpu_arch, "armv", 4) == 0) {
			v5_supported = cpu_arch [4] >= '5';
			v6_supported = cpu_arch [4] >= '6';
			v7_supported = cpu_arch [4] >= '7';
			v7s_supported = strncmp (cpu_arch, "armv7s", 6) == 0;
			v7k_supported = strncmp (cpu_arch, "armv7k", 6) == 0;
		}

		thumb_supported = strstr (cpu_arch, "thumb") != NULL;
		thumb2_supported = strstr (cpu_arch, "thumb2") != NULL;
		g_free (cpu_arch);
	}
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	/* no arm-specific optimizations yet */
	*exclude_mask = 0;
	return 0;
}

/*
 * This function test for all SIMD functions supported.
 *
 * Returns a bitmask corresponding to all supported versions.
 *
 */
guint32
mono_arch_cpu_enumerate_simd_versions (void)
{
	/* SIMD is currently unimplemented */
	return 0;
}

gboolean
mono_arm_is_hard_float (void)
{
	return arm_fpu == MONO_ARM_FPU_VFP_HARD;
}

#ifndef DISABLE_JIT

gboolean
mono_arch_opcode_needs_emulation (MonoCompile *cfg, int opcode)
{
	if (v7s_supported || v7k_supported) {
		switch (opcode) {
		case OP_IDIV:
		case OP_IREM:
		case OP_IDIV_UN:
		case OP_IREM_UN:
			return FALSE;
		default:
			break;
		}
	}
	return TRUE;
}

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
gboolean
mono_arch_is_soft_float (void)
{
	return arm_fpu == MONO_ARM_FPU_NONE;
}
#endif

static gboolean
is_regsize_var (MonoType *t)
{
	if (t->byref)
		return TRUE;
	t = mini_get_underlying_type (t);
	switch (t->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return TRUE;
	case MONO_TYPE_OBJECT:
		return TRUE;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			return TRUE;
		return FALSE;
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	}
	return FALSE;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		/* we can only allocate 32 bit values */
		if (is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = mono_varlist_insert_sorted (cfg, vars, vmv, FALSE);
		}
	}

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	mono_arch_compute_omit_fp (cfg);

	/* 
	 * FIXME: Interface calls might go through a static rgctx trampoline which
	 * sets V5, but it doesn't save it, so we need to save it ourselves, and
	 * avoid using it.
	 */
	if (cfg->flags & MONO_CFG_HAS_CALLS)
		cfg->uses_rgctx_reg = TRUE;

	if (cfg->arch.omit_fp)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_FP));
	regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V1));
	regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V2));
	regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V3));
	if (iphone_abi)
		/* V4=R7 is used as a frame pointer, but V7=R10 is preserved */
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V7));
	else
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V4));
	if (!(cfg->compile_aot || cfg->uses_rgctx_reg || COMPILE_LLVM (cfg)))
		/* V5 is reserved for passing the vtable/rgctx/IMT method */
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V5));
	/*regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V6));*/
	/*regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V7));*/

	return regs;
}

/*
 * mono_arch_regalloc_cost:
 *
 *  Return the cost, in number of memory references, of the action of 
 * allocating the variable VMV into a register during global register
 * allocation.
 */
guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	/* FIXME: */
	return 2;
}

#endif /* #ifndef DISABLE_JIT */

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#if defined(MONO_CROSS_COMPILE)
#elif __APPLE__
	sys_icache_invalidate (code, size);
#else
    __builtin___clear_cache ((char*)code, (char*)code + size);
#endif
}

#define DEBUG(a)

static void inline
add_general (guint *gr, guint *stack_size, ArgInfo *ainfo, gboolean simple)
{
	if (simple) {
		if (*gr > ARMREG_R3) {
			ainfo->size = 4;
			ainfo->offset = *stack_size;
			ainfo->reg = ARMREG_SP; /* in the caller */
			ainfo->storage = RegTypeBase;
			*stack_size += 4;
		} else {
			ainfo->storage = RegTypeGeneral;
			ainfo->reg = *gr;
		}
	} else {
		gboolean split;

		if (eabi_supported)
			split = i8_align == 4;
		else
			split = TRUE;

		ainfo->size = 8;
		if (*gr == ARMREG_R3 && split) {
			/* first word in r3 and the second on the stack */
			ainfo->offset = *stack_size;
			ainfo->reg = ARMREG_SP; /* in the caller */
			ainfo->storage = RegTypeBaseGen;
			*stack_size += 4;
		} else if (*gr >= ARMREG_R3) {
			if (eabi_supported) {
				/* darwin aligns longs to 4 byte only */
				if (i8_align == 8) {
					*stack_size += 7;
					*stack_size &= ~7;
				}
			}
			ainfo->offset = *stack_size;
			ainfo->reg = ARMREG_SP; /* in the caller */
			ainfo->storage = RegTypeBase;
			*stack_size += 8;
		} else {
			if (eabi_supported) {
				if (i8_align == 8 && ((*gr) & 1))
					(*gr) ++;
			}
			ainfo->storage = RegTypeIRegPair;
			ainfo->reg = *gr;
		}
		(*gr) ++;
	}
	(*gr) ++;
}

static void inline
add_float (guint *fpr, guint *stack_size, ArgInfo *ainfo, gboolean is_double, gint *float_spare)
{
	/*
	 * If we're calling a function like this:
	 *
	 * void foo(float a, double b, float c)
	 *
	 * We pass a in s0 and b in d1. That leaves us
	 * with s1 being unused. The armhf ABI recognizes
	 * this and requires register assignment to then
	 * use that for the next single-precision arg,
	 * i.e. c in this example. So float_spare either
	 * tells us which reg to use for the next single-
	 * precision arg, or it's -1, meaning use *fpr.
	 *
	 * Note that even though most of the JIT speaks
	 * double-precision, fpr represents single-
	 * precision registers.
	 *
	 * See parts 5.5 and 6.1.2 of the AAPCS for how
	 * this all works.
	 */

	if (*fpr < ARM_VFP_F16 || (!is_double && *float_spare >= 0)) {
		ainfo->storage = RegTypeFP;

		if (is_double) {
			/*
			 * If we're passing a double-precision value
			 * and *fpr is odd (e.g. it's s1, s3, ...)
			 * we need to use the next even register. So
			 * we mark the current *fpr as a spare that
			 * can be used for the next single-precision
			 * value.
			 */
			if (*fpr % 2) {
				*float_spare = *fpr;
				(*fpr)++;
			}

			/*
			 * At this point, we have an even register
			 * so we assign that and move along.
			 */
			ainfo->reg = *fpr;
			*fpr += 2;
		} else if (*float_spare >= 0) {
			/*
			 * We're passing a single-precision value
			 * and it looks like a spare single-
			 * precision register is available. Let's
			 * use it.
			 */

			ainfo->reg = *float_spare;
			*float_spare = -1;
		} else {
			/*
			 * If we hit this branch, we're passing a
			 * single-precision value and we can simply
			 * use the next available register.
			 */

			ainfo->reg = *fpr;
			(*fpr)++;
		}
	} else {
		/*
		 * We've exhausted available floating point
		 * regs, so pass the rest on the stack.
		 */

		if (is_double) {
			*stack_size += 7;
			*stack_size &= ~7;
		}

		ainfo->offset = *stack_size;
		ainfo->reg = ARMREG_SP;
		ainfo->storage = RegTypeBase;

		*stack_size += 8;
	}
}

static gboolean
is_hfa (MonoType *t, int *out_nfields, int *out_esize)
{
	MonoClass *klass;
	gpointer iter;
	MonoClassField *field;
	MonoType *ftype, *prev_ftype = NULL;
	int nfields = 0;

	klass = mono_class_from_mono_type_internal (t);
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		ftype = mono_field_get_type_internal (field);
		ftype = mini_get_underlying_type (ftype);

		if (MONO_TYPE_ISSTRUCT (ftype)) {
			int nested_nfields, nested_esize;

			if (!is_hfa (ftype, &nested_nfields, &nested_esize))
				return FALSE;
			if (nested_esize == 4)
				ftype = m_class_get_byval_arg (mono_defaults.single_class);
			else
				ftype = m_class_get_byval_arg (mono_defaults.double_class);
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			nfields += nested_nfields;
		} else {
			if (!(!ftype->byref && (ftype->type == MONO_TYPE_R4 || ftype->type == MONO_TYPE_R8)))
				return FALSE;
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			nfields ++;
		}
	}
	if (nfields == 0 || nfields > 4)
		return FALSE;
	*out_nfields = nfields;
	*out_esize = prev_ftype->type == MONO_TYPE_R4 ? 4 : 8;
	return TRUE;
}

static CallInfo*
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	guint i, gr, fpr, pstart;
	gint float_spare;
	int n = sig->hasthis + sig->param_count;
	int nfields, esize;
	guint32 align;
	MonoType *t;
	guint32 stack_size = 0;
	CallInfo *cinfo;
	gboolean is_pinvoke = sig->pinvoke;
	gboolean vtype_retaddr = FALSE;

	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	cinfo->nargs = n;
	gr = ARMREG_R0;
	fpr = ARM_VFP_F0;
	float_spare = -1;

	t = mini_get_underlying_type (sig->ret);
	switch (t->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
		cinfo->ret.storage = RegTypeGeneral;
		cinfo->ret.reg = ARMREG_R0;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		cinfo->ret.storage = RegTypeIRegPair;
		cinfo->ret.reg = ARMREG_R0;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		cinfo->ret.storage = RegTypeFP;

		if (t->type == MONO_TYPE_R4)
			cinfo->ret.size = 4;
		else
			cinfo->ret.size = 8;

		if (IS_HARD_FLOAT) {
			cinfo->ret.reg = ARM_VFP_F0;
		} else {
			cinfo->ret.reg = ARMREG_R0;
		}
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t)) {
			cinfo->ret.storage = RegTypeGeneral;
			cinfo->ret.reg = ARMREG_R0;
			break;
		}
		if (mini_is_gsharedvt_variable_type (t)) {
			cinfo->ret.storage = RegTypeStructByAddr;
			break;
		}
		/* Fall through */
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF:
		if (IS_HARD_FLOAT && sig->pinvoke && is_hfa (t, &nfields, &esize)) {
			cinfo->ret.storage = RegTypeHFA;
			cinfo->ret.reg = 0;
			cinfo->ret.nregs = nfields;
			cinfo->ret.esize = esize;
		} else {
			if (is_pinvoke) {
				int native_size = mono_class_native_size (mono_class_from_mono_type_internal (t), &align);
				int max_size;

#ifdef TARGET_WATCHOS
				max_size = 16;
#else
				max_size = 4;
#endif
				if (native_size <= max_size) {
					cinfo->ret.storage = RegTypeStructByVal;
					cinfo->ret.struct_size = native_size;
					cinfo->ret.nregs = ALIGN_TO (native_size, 4) / 4;
				} else {
					cinfo->ret.storage = RegTypeStructByAddr;
				}
			} else {
				cinfo->ret.storage = RegTypeStructByAddr;
			}
		}
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (mini_is_gsharedvt_type (t));
		cinfo->ret.storage = RegTypeStructByAddr;
		break;
	case MONO_TYPE_VOID:
		break;
	default:
		g_error ("Can't handle as return value 0x%x", sig->ret->type);
	}

	vtype_retaddr = cinfo->ret.storage == RegTypeStructByAddr;

	pstart = 0;
	n = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (vtype_retaddr && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_get_underlying_type (sig->params [0]))))) {
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0, TRUE);
		} else {
			add_general (&gr, &stack_size, &cinfo->args [sig->hasthis + 0], TRUE);
			pstart = 1;
		}
		n ++;
		cinfo->ret.reg = gr;
		gr ++;
		cinfo->vret_arg_index = 1;
	} else {
		/* this */
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0, TRUE);
			n ++;
		}
		if (vtype_retaddr) {
			cinfo->ret.reg = gr;
			gr ++;
		}
	}

	DEBUG(g_print("params: %d\n", sig->param_count));
	for (i = pstart; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [n];

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Prevent implicit arguments and sig_cookie from
			   being passed in registers */
			gr = ARMREG_R3 + 1;
			fpr = ARM_VFP_F16;
			/* Emit the signature cookie just before the implicit arguments */
			add_general (&gr, &stack_size, &cinfo->sig_cookie, TRUE);
		}
		DEBUG(g_print("param %d: ", i));
		if (sig->params [i]->byref) {
                        DEBUG(g_print("byref\n"));
			add_general (&gr, &stack_size, ainfo, TRUE);
			n++;
			continue;
		}
		t = mini_get_underlying_type (sig->params [i]);
		switch (t->type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			cinfo->args [n].size = 1;
			add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			cinfo->args [n].size = 2;
			add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			cinfo->args [n].size = 4;
			add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_OBJECT:
			cinfo->args [n].size = sizeof (target_mgreg_t);
			add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (t)) {
				cinfo->args [n].size = sizeof (target_mgreg_t);
				add_general (&gr, &stack_size, ainfo, TRUE);
				break;
			}
			if (mini_is_gsharedvt_variable_type (t)) {
				/* gsharedvt arguments are passed by ref */
				g_assert (mini_is_gsharedvt_type (t));
				add_general (&gr, &stack_size, ainfo, TRUE);
				switch (ainfo->storage) {
				case RegTypeGeneral:
					ainfo->storage = RegTypeGSharedVtInReg;
					break;
				case RegTypeBase:
					ainfo->storage = RegTypeGSharedVtOnStack;
					break;
				default:
					g_assert_not_reached ();
				}
				break;
			}
			/* Fall through */
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_VALUETYPE: {
			gint size;
			int align_size;
			int nwords, nfields, esize;
			guint32 align;

			if (IS_HARD_FLOAT && sig->pinvoke && is_hfa (t, &nfields, &esize)) {
				if (fpr + nfields < ARM_VFP_F16) {
					ainfo->storage = RegTypeHFA;
					ainfo->reg = fpr;
					ainfo->nregs = nfields;
					ainfo->esize = esize;
					if (esize == 4)
						fpr += nfields;
					else
						fpr += nfields * 2;
					break;
				} else {
					fpr = ARM_VFP_F16;
				}
			}

			if (t->type == MONO_TYPE_TYPEDBYREF) {
				size = MONO_ABI_SIZEOF (MonoTypedRef);
				align = sizeof (target_mgreg_t);
			} else {
				MonoClass *klass = mono_class_from_mono_type_internal (sig->params [i]);
				if (is_pinvoke)
					size = mono_class_native_size (klass, &align);
				else
					size = mini_type_stack_size_full (t, &align, FALSE);
			}
			DEBUG(g_print ("load %d bytes struct\n", size));

#ifdef TARGET_WATCHOS
			/* Watchos pass large structures by ref */
			/* We only do this for pinvoke to make gsharedvt/dyncall simpler */
			if (sig->pinvoke && size > 16) {
				add_general (&gr, &stack_size, ainfo, TRUE);
				switch (ainfo->storage) {
				case RegTypeGeneral:
					ainfo->storage = RegTypeStructByAddr;
					break;
				case RegTypeBase:
					ainfo->storage = RegTypeStructByAddrOnStack;
					break;
				default:
					g_assert_not_reached ();
					break;
				}
				break;
			}
#endif

			align_size = size;
			nwords = 0;
			align_size += (sizeof (target_mgreg_t) - 1);
			align_size &= ~(sizeof (target_mgreg_t) - 1);
			nwords = (align_size + sizeof (target_mgreg_t) -1 ) / sizeof (target_mgreg_t);
			ainfo->storage = RegTypeStructByVal;
			ainfo->struct_size = size;
			ainfo->align = align;

			if (eabi_supported) {
				if (align >= 8 && (gr & 1))
					gr ++;
			}
			if (gr > ARMREG_R3) {
				ainfo->size = 0;
				ainfo->vtsize = nwords;
			} else {
				int rest = ARMREG_R3 - gr + 1;
				int n_in_regs = rest >= nwords? nwords: rest;

				ainfo->size = n_in_regs;
				ainfo->vtsize = nwords - n_in_regs;
				ainfo->reg = gr;
				gr += n_in_regs;
				nwords -= n_in_regs;
			}
			stack_size = ALIGN_TO (stack_size, align);

			ainfo->offset = stack_size;
			/*g_print ("offset for arg %d at %d\n", n, stack_size);*/
			stack_size += nwords * sizeof (target_mgreg_t);
			break;
		}
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			ainfo->size = 8;
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_R4:
			ainfo->size = 4;

			if (IS_HARD_FLOAT)
				add_float (&fpr, &stack_size, ainfo, FALSE, &float_spare);
			else
				add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_R8:
			ainfo->size = 8;

			if (IS_HARD_FLOAT)
				add_float (&fpr, &stack_size, ainfo, TRUE, &float_spare);
			else
				add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_VAR:
		case MONO_TYPE_MVAR:
			/* gsharedvt arguments are passed by ref */
			g_assert (mini_is_gsharedvt_type (t));
			add_general (&gr, &stack_size, ainfo, TRUE);
			switch (ainfo->storage) {
			case RegTypeGeneral:
				ainfo->storage = RegTypeGSharedVtInReg;
				break;
			case RegTypeBase:
				ainfo->storage = RegTypeGSharedVtOnStack;
				break;
			default:
				g_assert_not_reached ();
			}
			break;
		default:
			g_error ("Can't handle 0x%x", sig->params [i]->type);
		}
		n ++;
	}

	/* Handle the case where there are no implicit arguments */
	if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
		/* Prevent implicit arguments and sig_cookie from
		   being passed in registers */
		gr = ARMREG_R3 + 1;
		fpr = ARM_VFP_F16;
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie, TRUE);
	}

	DEBUG (g_print ("      stack size: %d (%d)\n", (stack_size + 15) & ~15, stack_size));
	stack_size = ALIGN_TO (stack_size, MONO_ARCH_FRAME_ALIGNMENT);

	cinfo->stack_usage = stack_size;
	return cinfo;
}

/*
 * We need to create a temporary value if the argument is not stored in
 * a linear memory range in the ccontext (this normally happens for
 * value types if they are passed both by stack and regs).
 */
static int
arg_need_temp (ArgInfo *ainfo)
{
	if (ainfo->storage == RegTypeStructByVal && ainfo->vtsize)
		return ainfo->struct_size;
	return 0;
}

static gpointer
arg_get_storage (CallContext *ccontext, ArgInfo *ainfo)
{
	switch (ainfo->storage) {
		case RegTypeIRegPair:
		case RegTypeGeneral:
		case RegTypeStructByVal:
			return &ccontext->gregs [ainfo->reg];
		case RegTypeHFA:
		case RegTypeFP:
			return &ccontext->fregs [ainfo->reg];
		case RegTypeBase:
			return ccontext->stack + ainfo->offset;
		default:
			g_error ("Arg storage type not yet supported");
	}
}

static void
arg_get_val (CallContext *ccontext, ArgInfo *ainfo, gpointer dest)
{
	int reg_size = ainfo->size * sizeof (host_mgreg_t);
	g_assert (arg_need_temp (ainfo));
	memcpy (dest, &ccontext->gregs [ainfo->reg], reg_size);
	memcpy ((host_mgreg_t*)dest + ainfo->size, ccontext->stack + ainfo->offset, ainfo->struct_size - reg_size);
}

static void
arg_set_val (CallContext *ccontext, ArgInfo *ainfo, gpointer src)
{
	int reg_size = ainfo->size * sizeof (host_mgreg_t);
	g_assert (arg_need_temp (ainfo));
	memcpy (&ccontext->gregs [ainfo->reg], src, reg_size);
	memcpy (ccontext->stack + ainfo->offset, (host_mgreg_t*)src + ainfo->size, ainfo->struct_size - reg_size);
}

/* Set arguments in the ccontext (for i2n entry) */
void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = get_call_info (NULL, sig);
	gpointer storage;
	ArgInfo *ainfo;

	memset (ccontext, 0, sizeof (CallContext));

	ccontext->stack_size = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	if (ccontext->stack_size)
		ccontext->stack = (guint8*)g_calloc (1, ccontext->stack_size);

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == RegTypeStructByAddr) {
			storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, -1);
			ccontext->gregs [cinfo->ret.reg] = (host_mgreg_t)(gsize)storage;
		}
	}

	g_assert (!sig->hasthis);

	for (int i = 0; i < sig->param_count; i++) {
		ainfo = &cinfo->args [i];
		int temp_size = arg_need_temp (ainfo);

		if (temp_size)
			storage = alloca (temp_size); // FIXME? alloca in a loop
		else
			storage = arg_get_storage (ccontext, ainfo);

		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, i, storage);
		if (temp_size)
			arg_set_val (ccontext, ainfo, storage);
	}

	g_free (cinfo);
}

/* Set return value in the ccontext (for n2i return) */
void
mono_arch_set_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	MonoEECallbacks *interp_cb;
	CallInfo *cinfo;
	gpointer storage;
	ArgInfo *ainfo;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	cinfo = get_call_info (NULL, sig);
	ainfo = &cinfo->ret;

	if (ainfo->storage != RegTypeStructByAddr) {
		g_assert (!arg_need_temp (ainfo));
		storage = arg_get_storage (ccontext, ainfo);
		memset (ccontext, 0, sizeof (CallContext)); // FIXME
		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, -1, storage);
	}

	g_free (cinfo);
}

/* Gets the arguments from ccontext (for n2i entry) */
void
mono_arch_get_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = get_call_info (NULL, sig);
	gpointer storage;
	ArgInfo *ainfo;

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == RegTypeStructByAddr) {
			storage = (gpointer)(gsize)ccontext->gregs [cinfo->ret.reg];
			interp_cb->frame_arg_set_storage ((MonoInterpFrameHandle)frame, sig, -1, storage);
		}
	}

	for (int i = 0; i < sig->param_count + sig->hasthis; i++) {
		ainfo = &cinfo->args [i];
		int temp_size = arg_need_temp (ainfo);

		if (temp_size) {
			storage = alloca (temp_size); // FIXME? alloca in a loop
			arg_get_val (ccontext, ainfo, storage);
		} else {
			storage = arg_get_storage (ccontext, ainfo);
		}
		interp_cb->data_to_frame_arg ((MonoInterpFrameHandle)frame, sig, i, storage);
	}

	g_free (cinfo);
}

/* Gets the return value from ccontext (for i2n exit) */
void
mono_arch_get_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig)
{
	MonoEECallbacks *interp_cb;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	gpointer storage;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	cinfo = get_call_info (NULL, sig);
	ainfo = &cinfo->ret;

	if (ainfo->storage != RegTypeStructByAddr) {
		g_assert (!arg_need_temp (ainfo));
		storage = arg_get_storage (ccontext, ainfo);
		interp_cb->data_to_frame_arg ((MonoInterpFrameHandle)frame, sig, -1, storage);
	}

	g_free (cinfo);
}

#ifndef DISABLE_JIT

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	g_assert (caller_sig);
	g_assert (callee_sig);

	CallInfo *caller_info = get_call_info (NULL, caller_sig);
	CallInfo *callee_info = get_call_info (NULL, callee_sig);

	/*
	 * Tailcalls with more callee stack usage than the caller cannot be supported, since
	 * the extra stack space would be left on the stack after the tailcall.
	 */
	gboolean res = IS_SUPPORTED_TAILCALL (callee_info->stack_usage <= caller_info->stack_usage)
				&& IS_SUPPORTED_TAILCALL (caller_info->ret.storage == callee_info->ret.storage);

	// FIXME The limit here is that moving the parameters requires addressing the parameters
	// with 12bit (4K) immediate offsets. - 4 for TAILCALL_REG/MEMBASE
	res &= IS_SUPPORTED_TAILCALL (callee_info->stack_usage < (4096 - 4));
	res &= IS_SUPPORTED_TAILCALL (caller_info->stack_usage < (4096 - 4));

	g_free (caller_info);
	g_free (callee_info);

	return res;
}

static gboolean
debug_omit_fp (void)
{
#if 0
	return mono_debug_count ();
#else
	return TRUE;
#endif
}

/**
 * mono_arch_compute_omit_fp:
 * Determine whether the frame pointer can be eliminated.
 */
static void
mono_arch_compute_omit_fp (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i, locals_size;
	CallInfo *cinfo;

	if (cfg->arch.omit_fp_computed)
		return;

	header = cfg->header;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	/*
	 * FIXME: Remove some of the restrictions.
	 */
	cfg->arch.omit_fp = TRUE;
	cfg->arch.omit_fp_computed = TRUE;

	if (cfg->disable_omit_fp)
		cfg->arch.omit_fp = FALSE;
	if (!debug_omit_fp ())
		cfg->arch.omit_fp = FALSE;
	/*
	if (cfg->method->save_lmf)
		cfg->arch.omit_fp = FALSE;
	*/
	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		cfg->arch.omit_fp = FALSE;
	if (header->num_clauses)
		cfg->arch.omit_fp = FALSE;
	if (cfg->param_area)
		cfg->arch.omit_fp = FALSE;
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
		cfg->arch.omit_fp = FALSE;
	if ((mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)))
		cfg->arch.omit_fp = FALSE;
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];

		if (ainfo->storage == RegTypeBase || ainfo->storage == RegTypeBaseGen || ainfo->storage == RegTypeStructByVal) {
			/* 
			 * The stack offset can only be determined when the frame
			 * size is known.
			 */
			cfg->arch.omit_fp = FALSE;
		}
	}

	locals_size = 0;
	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		int ialign;

		locals_size += mono_type_size (ins->inst_vtype, &ialign);
	}
}

/*
 * Set var information according to the calling convention. arm version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *ins;
	MonoType *sig_ret;
	int i, offset, size, align, curinst;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	guint32 ualign;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;
	sig_ret = mini_get_underlying_type (sig->ret);

	mono_arch_compute_omit_fp (cfg);

	if (cfg->arch.omit_fp)
		cfg->frame_reg = ARMREG_SP;
	else
		cfg->frame_reg = ARMREG_FP;

	cfg->flags |= MONO_CFG_HAS_SPILLUP;

	/* allow room for the vararg method args: void* and long/double */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method))
		cfg->param_area = MAX (cfg->param_area, sizeof (target_mgreg_t)*8);

	header = cfg->header;

	/* See mono_arch_get_global_int_regs () */
	if (cfg->flags & MONO_CFG_HAS_CALLS)
		cfg->uses_rgctx_reg = TRUE;

	if (cfg->frame_reg != ARMREG_SP)
		cfg->used_int_regs |= 1 << cfg->frame_reg;

	if (cfg->compile_aot || cfg->uses_rgctx_reg || COMPILE_LLVM (cfg))
		/* V5 is reserved for passing the vtable/rgctx/IMT method */
		cfg->used_int_regs |= (1 << MONO_ARCH_IMT_REG);

	offset = 0;
	curinst = 0;
	if (!MONO_TYPE_ISSTRUCT (sig_ret) && cinfo->ret.storage != RegTypeStructByAddr) {
		if (sig_ret->type != MONO_TYPE_VOID) {
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = ARMREG_R0;
		}
	}
	/* local vars are at a positive offset from the stack pointer */
	/* 
	 * also note that if the function uses alloca, we use FP
	 * to point at the local variables.
	 */
	offset = 0; /* linkage area */
	/* align the offset to 16 bytes: not sure this is needed here  */
	//offset += 8 - 1;
	//offset &= ~(8 - 1);

	/* add parameter area size for called functions */
	offset += cfg->param_area;
	offset += 8 - 1;
	offset &= ~(8 - 1);
	if (cfg->flags & MONO_CFG_HAS_FPOUT)
		offset += 8;

	/* allow room to save the return value */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method))
		offset += 8;

	switch (cinfo->ret.storage) {
	case RegTypeStructByVal:
	case RegTypeHFA:
		/* Allocate a local to hold the result, the epilog will copy it to the correct place */
		offset = ALIGN_TO (offset, 8);
		cfg->ret->opcode = OP_REGOFFSET;
		cfg->ret->inst_basereg = cfg->frame_reg;
		cfg->ret->inst_offset = offset;
		if (cinfo->ret.storage == RegTypeStructByVal)
			offset += cinfo->ret.nregs * sizeof (target_mgreg_t);
		else
			offset += 32;
		break;
	case RegTypeStructByAddr:
		ins = cfg->vret_addr;
		offset += sizeof (target_mgreg_t) - 1;
		offset &= ~(sizeof (target_mgreg_t) - 1);
		ins->inst_offset = offset;
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			g_print ("vret_addr =");
			mono_print_ins (cfg->vret_addr);
		}
		offset += sizeof (target_mgreg_t);
		break;
	default:
		break;
	}

	/* Allocate these first so they have a small offset, OP_SEQ_POINT depends on this */
	if (cfg->arch.seq_point_info_var) {
		MonoInst *ins;

		ins = cfg->arch.seq_point_info_var;

		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}
	if (cfg->arch.ss_trigger_page_var) {
		MonoInst *ins;

		ins = cfg->arch.ss_trigger_page_var;
		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}

	if (cfg->arch.seq_point_ss_method_var) {
		MonoInst *ins;

		ins = cfg->arch.seq_point_ss_method_var;
		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}
	if (cfg->arch.seq_point_bp_method_var) {
		MonoInst *ins;

		ins = cfg->arch.seq_point_bp_method_var;
		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}

	if (cfg->has_atomic_exchange_i4 || cfg->has_atomic_cas_i4 || cfg->has_atomic_add_i4) {
		/* Allocate a temporary used by the atomic ops */
		size = 4;
		align = 4;

		/* Allocate a local slot to hold the sig cookie address */
		offset += align - 1;
		offset &= ~(align - 1);
		cfg->arch.atomic_tmp_offset = offset;
		offset += size;
	} else {
		cfg->arch.atomic_tmp_offset = -1;
	}

	cfg->locals_min_stack_offset = offset;

	curinst = cfg->locals_start;
	for (i = curinst; i < cfg->num_varinfo; ++i) {
		MonoType *t;

		ins = cfg->varinfo [i];
		if ((ins->flags & MONO_INST_IS_DEAD) || ins->opcode == OP_REGVAR || ins->opcode == OP_REGOFFSET)
			continue;

		t = ins->inst_vtype;
		if (cfg->gsharedvt && mini_is_gsharedvt_variable_type (t))
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (ins->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (t) && t->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type_internal (t), &ualign);
			align = ualign;
		}
		else
			size = mono_type_size (t, &align);

		/* FIXME: if a structure is misaligned, our memcpy doesn't work,
		 * since it loads/stores misaligned words, which don't do the right thing.
		 */
		if (align < 4 && size >= 4)
			align = 4;
		if (ALIGN_TO (offset, align) > ALIGN_TO (offset, 4))
			mini_gc_set_slot_type_from_fp (cfg, ALIGN_TO (offset, 4), SLOT_NOREF);
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_offset = offset;
		ins->inst_basereg = cfg->frame_reg;
		offset += size;
		//g_print ("allocating local %d to %d\n", i, inst->inst_offset);
	}

	cfg->locals_max_stack_offset = offset;

	curinst = 0;
	if (sig->hasthis) {
		ins = cfg->args [curinst];
		if (ins->opcode != OP_REGVAR) {
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			offset += sizeof (target_mgreg_t) - 1;
			offset &= ~(sizeof (target_mgreg_t) - 1);
			ins->inst_offset = offset;
			offset += sizeof (target_mgreg_t);
		}
		curinst++;
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		size = 4;
		align = 4;

		/* Allocate a local slot to hold the sig cookie address */
		offset += align - 1;
		offset &= ~(align - 1);
		cfg->sig_cookie = offset;
		offset += size;
	}			

	for (i = 0; i < sig->param_count; ++i) {
		ainfo = cinfo->args + i;

		ins = cfg->args [curinst];

		switch (ainfo->storage) {
		case RegTypeHFA:
			offset = ALIGN_TO (offset, 8);
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			/* These arguments are saved to the stack in the prolog */
			ins->inst_offset = offset;
			if (cfg->verbose_level >= 2)
				g_print ("arg %d allocated to %s+0x%0x.\n", i, mono_arch_regname (ins->inst_basereg), (int)ins->inst_offset);
			// FIXME:
			offset += 32;
			break;
		default:
			break;
		}

		if (ins->opcode != OP_REGVAR) {
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			size = mini_type_stack_size_full (sig->params [i], &ualign, sig->pinvoke);
			align = ualign;
			/* FIXME: if a structure is misaligned, our memcpy doesn't work,
			 * since it loads/stores misaligned words, which don't do the right thing.
			 */
			if (align < 4 && size >= 4)
				align = 4;
			/* The code in the prolog () stores words when storing vtypes received in a register */
			if (MONO_TYPE_ISSTRUCT (sig->params [i]))
				align = 4;
			if (ALIGN_TO (offset, align) > ALIGN_TO (offset, 4))
				mini_gc_set_slot_type_from_fp (cfg, ALIGN_TO (offset, 4), SLOT_NOREF);
			offset += align - 1;
			offset &= ~(align - 1);
			ins->inst_offset = offset;
			offset += size;
		}
		curinst++;
	}

	/* align the offset to 8 bytes */
	if (ALIGN_TO (offset, 8) > ALIGN_TO (offset, 4))
		mini_gc_set_slot_type_from_fp (cfg, ALIGN_TO (offset, 4), SLOT_NOREF);
	offset += 8 - 1;
	offset &= ~(8 - 1);

	/* change sign? */
	cfg->stack_offset = offset;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;
	int i;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	if (IS_HARD_FLOAT) {
		for (i = 0; i < 2; i++) {
			MonoInst *inst = mono_compile_create_var (cfg, m_class_get_byval_arg (mono_defaults.double_class), OP_LOCAL);
			inst->flags |= MONO_INST_VOLATILE;

			cfg->arch.vfp_scratch_slots [i] = inst;
		}
	}

	if (cinfo->ret.storage == RegTypeStructByVal)
		cfg->ret_var_is_local = TRUE;

	if (cinfo->ret.storage == RegTypeStructByAddr) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			g_print ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_sdb_seq_points) {
		if (cfg->compile_aot) {
			MonoInst *ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_info_var = ins;

			if (!cfg->soft_breakpoints) {
				/* Allocate a separate variable for this to save 1 load per seq point */
				ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
				ins->flags |= MONO_INST_VOLATILE;
				cfg->arch.ss_trigger_page_var = ins;
			}
		}
		if (cfg->soft_breakpoints) {
			MonoInst *ins;

			ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_ss_method_var = ins;

			ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_bp_method_var = ins;
		}
	}
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;
	int sig_reg;

	if (MONO_IS_TAILCALL_OPCODE (call))
		NOT_IMPLEMENTED;

	g_assert (cinfo->sig_cookie.storage == RegTypeBase);
			
	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is 
	 * passed first and all the arguments which were before it are
	 * passed on the stack after the signature. So compensate by 
	 * passing a different signature.
	 */
	tmp_sig = mono_metadata_signature_dup (call->signature);
	tmp_sig->param_count -= call->signature->sentinelpos;
	tmp_sig->sentinelpos = 0;
	memcpy (tmp_sig->params, call->signature->params + call->signature->sentinelpos, tmp_sig->param_count * sizeof (MonoType*));

	sig_reg = mono_alloc_ireg (cfg);
	MONO_EMIT_NEW_SIGNATURECONST (cfg, sig_reg, tmp_sig);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, cinfo->sig_cookie.offset, sig_reg);
}

#ifdef ENABLE_LLVM
LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	LLVMCallInfo *linfo;

	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->mempool, sig);

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	/*
	 * LLVM always uses the native ABI while we use our own ABI, the
	 * only difference is the handling of vtypes:
	 * - we only pass/receive them in registers in some cases, and only 
	 *   in 1 or 2 integer registers.
	 */
	switch (cinfo->ret.storage) {
	case RegTypeGeneral:
	case RegTypeNone:
	case RegTypeFP:
	case RegTypeIRegPair:
		break;
	case RegTypeStructByAddr:
		if (sig->pinvoke) {
			linfo->ret.storage = LLVMArgVtypeByRef;
		} else {
			/* Vtype returned using a hidden argument */
			linfo->ret.storage = LLVMArgVtypeRetAddr;
			linfo->vret_arg_index = cinfo->vret_arg_index;
		}
		break;
#if TARGET_WATCHOS
	case RegTypeStructByVal:
		/* LLVM models this by returning an int array */
		linfo->ret.storage = LLVMArgAsIArgs;
		linfo->ret.nslots = cinfo->ret.nregs;
		break;
#endif
	case RegTypeHFA:
		linfo->ret.storage = LLVMArgFpStruct;
		linfo->ret.nslots = cinfo->ret.nregs;
		linfo->ret.esize = cinfo->ret.esize;
		break;
	default:
		cfg->exception_message = g_strdup_printf ("unknown ret conv (%d)", cinfo->ret.storage);
		cfg->disable_llvm = TRUE;
		return linfo;
	}

	for (i = 0; i < n; ++i) {
		LLVMArgInfo *lainfo = &linfo->args [i];
		ainfo = cinfo->args + i;

		lainfo->storage = LLVMArgNone;

		switch (ainfo->storage) {
		case RegTypeGeneral:
		case RegTypeIRegPair:
		case RegTypeBase:
		case RegTypeBaseGen:
		case RegTypeFP:
			lainfo->storage = LLVMArgNormal;
			break;
		case RegTypeStructByVal: {
			lainfo->storage = LLVMArgAsIArgs;
			int slotsize = eabi_supported && ainfo->align == 8 ? 8 : 4;
			lainfo->nslots = ALIGN_TO (ainfo->struct_size, slotsize) / slotsize;
			lainfo->esize = slotsize;
			break;
		}
		case RegTypeStructByAddr:
		case RegTypeStructByAddrOnStack:
			lainfo->storage = LLVMArgVtypeByRef;
			break;
		case RegTypeHFA: {
			int j;

			lainfo->storage = LLVMArgAsFpArgs;
			lainfo->nslots = ainfo->nregs;
			lainfo->esize = ainfo->esize;
			for (j = 0; j < ainfo->nregs; ++j)
				lainfo->pair_storage [j] = LLVMArgInFPReg;
			break;
		}
		default:
			cfg->exception_message = g_strdup_printf ("ainfo->storage (%d)", ainfo->storage);
			cfg->disable_llvm = TRUE;
			break;
		}
	}

	return linfo;
}
#endif

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *in, *ins;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = get_call_info (cfg->mempool, sig);

	switch (cinfo->ret.storage) {
	case RegTypeStructByVal:
	case RegTypeHFA:
		if (cinfo->ret.storage == RegTypeStructByVal && cinfo->ret.nregs == 1) {
			/* The JIT will transform this into a normal call */
			call->vret_in_reg = TRUE;
			break;
		}
		if (MONO_IS_TAILCALL_OPCODE (call))
			break;
		/*
		 * The vtype is returned in registers, save the return area address in a local, and save the vtype into
		 * the location pointed to by it after call in emit_move_return_value ().
		 */
		if (!cfg->arch.vret_addr_loc) {
			cfg->arch.vret_addr_loc = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			/* Prevent it from being register allocated or optimized away */
			cfg->arch.vret_addr_loc->flags |= MONO_INST_VOLATILE;
		}

		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->arch.vret_addr_loc->dreg, call->vret_var->dreg);
		break;
	case RegTypeStructByAddr: {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->sreg1 = call->vret_var->dreg;
		vtarg->dreg = mono_alloc_preg (cfg);
		MONO_ADD_INS (cfg->cbb, vtarg);

		mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		break;
	}
	default:
		break;
	}

	for (i = 0; i < n; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		MonoType *t;

		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = mono_get_int_type ();
		t = mini_get_underlying_type (t);

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
		}

		in = call->args [i];

		switch (ainfo->storage) {
		case RegTypeGeneral:
		case RegTypeIRegPair:
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = MONO_LVREG_LS (in->dreg);
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);

				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = MONO_LVREG_MS (in->dreg);
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg + 1, FALSE);
			} else if (!t->byref && ((t->type == MONO_TYPE_R8) || (t->type == MONO_TYPE_R4))) {
				if (ainfo->size == 4) {
					if (IS_SOFT_FLOAT) {
						/* mono_emit_call_args () have already done the r8->r4 conversion */
						/* The converted value is in an int vreg */
						MONO_INST_NEW (cfg, ins, OP_MOVE);
						ins->dreg = mono_alloc_ireg (cfg);
						ins->sreg1 = in->dreg;
						MONO_ADD_INS (cfg->cbb, ins);
						mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);
					} else {
						int creg;

						cfg->param_area = MAX (cfg->param_area, 8);
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, ARMREG_SP, (cfg->param_area - 8), in->dreg);
						creg = mono_alloc_ireg (cfg);
						MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8));
						mono_call_inst_add_outarg_reg (cfg, call, creg, ainfo->reg, FALSE);
					}
				} else {
					if (IS_SOFT_FLOAT) {
						MONO_INST_NEW (cfg, ins, OP_FGETLOW32);
						ins->dreg = mono_alloc_ireg (cfg);
						ins->sreg1 = in->dreg;
						MONO_ADD_INS (cfg->cbb, ins);
						mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);

						MONO_INST_NEW (cfg, ins, OP_FGETHIGH32);
						ins->dreg = mono_alloc_ireg (cfg);
						ins->sreg1 = in->dreg;
						MONO_ADD_INS (cfg->cbb, ins);
						mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg + 1, FALSE);
					} else {
						int creg;

						cfg->param_area = MAX (cfg->param_area, 8);
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, (cfg->param_area - 8), in->dreg);
						creg = mono_alloc_ireg (cfg);
						MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8));
						mono_call_inst_add_outarg_reg (cfg, call, creg, ainfo->reg, FALSE);
						creg = mono_alloc_ireg (cfg);
						MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8 + 4));
						mono_call_inst_add_outarg_reg (cfg, call, creg, ainfo->reg + 1, FALSE);
					}
				}
				cfg->flags |= MONO_CFG_HAS_FPOUT;
			} else {
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = in->dreg;
				MONO_ADD_INS (cfg->cbb, ins);

				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);
			}
			break;
		case RegTypeStructByVal:
		case RegTypeGSharedVtInReg:
		case RegTypeGSharedVtOnStack:
		case RegTypeHFA:
		case RegTypeStructByAddr:
		case RegTypeStructByAddrOnStack:
			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->opcode = OP_OUTARG_VT;
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			mono_call_inst_add_outarg_vt (cfg, call, ins);
			MONO_ADD_INS (cfg->cbb, ins);
			break;
		case RegTypeBase:
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
			} else if (!t->byref && ((t->type == MONO_TYPE_R4) || (t->type == MONO_TYPE_R8))) {
				if (t->type == MONO_TYPE_R8) {
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
				} else {
					if (IS_SOFT_FLOAT)
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
					else
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
				}
			} else {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
			}
			break;
		case RegTypeBaseGen:
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, (G_BYTE_ORDER == G_BIG_ENDIAN) ? MONO_LVREG_LS (in->dreg) : MONO_LVREG_MS (in->dreg));
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = G_BYTE_ORDER == G_BIG_ENDIAN ? MONO_LVREG_MS (in->dreg) : MONO_LVREG_LS (in->dreg);
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ARMREG_R3, FALSE);
			} else if (!t->byref && (t->type == MONO_TYPE_R8)) {
				int creg;

				/* This should work for soft-float as well */

				cfg->param_area = MAX (cfg->param_area, 8);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, (cfg->param_area - 8), in->dreg);
				creg = mono_alloc_ireg (cfg);
				mono_call_inst_add_outarg_reg (cfg, call, creg, ARMREG_R3, FALSE);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8));
				creg = mono_alloc_ireg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 4));
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, creg);
				cfg->flags |= MONO_CFG_HAS_FPOUT;
			} else {
				g_assert_not_reached ();
			}
			break;
		case RegTypeFP: {
			int fdreg = mono_alloc_freg (cfg);

			if (ainfo->size == 8) {
				MONO_INST_NEW (cfg, ins, OP_FMOVE);
				ins->sreg1 = in->dreg;
				ins->dreg = fdreg;
				MONO_ADD_INS (cfg->cbb, ins);

				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, TRUE);
			} else {
				FloatArgData *fad;

				/*
				 * Mono's register allocator doesn't speak single-precision registers that
				 * overlap double-precision registers (i.e. armhf). So we have to work around
				 * the register allocator and load the value from memory manually.
				 *
				 * So we create a variable for the float argument and an instruction to store
				 * the argument into the variable. We then store the list of these arguments
				 * in call->float_args. This list is then used by emit_float_args later to
				 * pass the arguments in the various call opcodes.
				 *
				 * This is not very nice, and we should really try to fix the allocator.
				 */

				MonoInst *float_arg = mono_compile_create_var (cfg, m_class_get_byval_arg (mono_defaults.single_class), OP_LOCAL);

				/* Make sure the instruction isn't seen as pointless and removed.
				 */
				float_arg->flags |= MONO_INST_VOLATILE;

				MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, float_arg->dreg, in->dreg);

				/* We use the dreg to look up the instruction later. The hreg is used to
				 * emit the instruction that loads the value into the FP reg.
				 */
				fad = mono_mempool_alloc0 (cfg->mempool, sizeof (FloatArgData));
				fad->vreg = float_arg->dreg;
				fad->hreg = ainfo->reg;

				call->float_args = g_slist_append_mempool (cfg->mempool, call->float_args, fad);
			}

			call->used_iregs |= 1 << ainfo->reg;
			cfg->flags |= MONO_CFG_HAS_FPOUT;
			break;
		}
		default:
			g_assert_not_reached ();
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	call->call_info = cinfo;
	call->stack_usage = cinfo->stack_usage;
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *arg)
{
	MonoInst *ins;

	switch (storage) {
	case RegTypeFP:
		MONO_INST_NEW (cfg, ins, OP_FMOVE);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	MonoInst *load;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int ovf_size = ainfo->vtsize;
	int doffset = ainfo->offset;
	int struct_size = ainfo->struct_size;
	int i, soffset, dreg, tmpreg;

	switch (ainfo->storage) {
	case RegTypeGSharedVtInReg:
	case RegTypeStructByAddr:
		/* Pass by addr */
		mono_call_inst_add_outarg_reg (cfg, call, src->dreg, ainfo->reg, FALSE);
		break;
	case RegTypeGSharedVtOnStack:
	case RegTypeStructByAddrOnStack:
		/* Pass by addr on stack */
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, src->dreg);
		break;
	case RegTypeHFA:
		for (i = 0; i < ainfo->nregs; ++i) {
			if (ainfo->esize == 4)
				MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
			else
				MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
			load->dreg = mono_alloc_freg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = i * ainfo->esize;
			MONO_ADD_INS (cfg->cbb, load);

			if (ainfo->esize == 4) {
				FloatArgData *fad;

				/* See RegTypeFP in mono_arch_emit_call () */
				MonoInst *float_arg = mono_compile_create_var (cfg, m_class_get_byval_arg (mono_defaults.single_class), OP_LOCAL);
				float_arg->flags |= MONO_INST_VOLATILE;
				MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, float_arg->dreg, load->dreg);

				fad = mono_mempool_alloc0 (cfg->mempool, sizeof (FloatArgData));
				fad->vreg = float_arg->dreg;
				fad->hreg = ainfo->reg + i;

				call->float_args = g_slist_append_mempool (cfg->mempool, call->float_args, fad);
			} else {
				add_outarg_reg (cfg, call, RegTypeFP, ainfo->reg + (i * 2), load);
			}
		}
		break;
	default:
		soffset = 0;
		for (i = 0; i < ainfo->size; ++i) {
			dreg = mono_alloc_ireg (cfg);
			switch (struct_size) {
			case 1:
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, dreg, src->dreg, soffset);
				break;
			case 2:
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, dreg, src->dreg, soffset);
				break;
			case 3:
				tmpreg = mono_alloc_ireg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, dreg, src->dreg, soffset);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, tmpreg, src->dreg, soffset + 1);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, tmpreg, tmpreg, 8);
				MONO_EMIT_NEW_BIALU (cfg, OP_IOR, dreg, dreg, tmpreg);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, tmpreg, src->dreg, soffset + 2);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, tmpreg, tmpreg, 16);
				MONO_EMIT_NEW_BIALU (cfg, OP_IOR, dreg, dreg, tmpreg);
				break;
			default:
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, soffset);
				break;
			}
			mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg + i, FALSE);
			soffset += sizeof (target_mgreg_t);
			struct_size -= sizeof (target_mgreg_t);
		}
		//g_print ("vt size: %d at R%d + %d\n", doffset, vt->inst_basereg, vt->inst_offset);
		if (ovf_size != 0)
			mini_emit_memcpy (cfg, ARMREG_SP, doffset, src->dreg, soffset, MIN (ovf_size * sizeof (target_mgreg_t), struct_size), struct_size < 4 ? 1 : 4);
		break;
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_get_underlying_type (mono_method_signature_internal (method)->ret);

	if (!ret->byref) {
		if (ret->type == MONO_TYPE_I8 || ret->type == MONO_TYPE_U8) {
			MonoInst *ins;

			if (COMPILE_LLVM (cfg)) {
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
			} else {
				MONO_INST_NEW (cfg, ins, OP_SETLRET);
				ins->sreg1 = MONO_LVREG_LS (val->dreg);
				ins->sreg2 = MONO_LVREG_MS (val->dreg);
				MONO_ADD_INS (cfg->cbb, ins);
			}
			return;
		}
		switch (arm_fpu) {
		case MONO_ARM_FPU_NONE:
			if (ret->type == MONO_TYPE_R8) {
				MonoInst *ins;

				MONO_INST_NEW (cfg, ins, OP_SETFRET);
				ins->dreg = cfg->ret->dreg;
				ins->sreg1 = val->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
				return;
			}
			if (ret->type == MONO_TYPE_R4) {
				/* Already converted to an int in method_to_ir () */
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
				return;
			}
			break;
		case MONO_ARM_FPU_VFP:
		case MONO_ARM_FPU_VFP_HARD:
			if (ret->type == MONO_TYPE_R8 || ret->type == MONO_TYPE_R4) {
				MonoInst *ins;

				MONO_INST_NEW (cfg, ins, OP_SETFRET);
				ins->dreg = cfg->ret->dreg;
				ins->sreg1 = val->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
				return;
			}
			break;
		default:
			g_assert_not_reached ();
		}
	}

	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

#endif /* #ifndef DISABLE_JIT */

gboolean 
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return TRUE;
}

typedef struct {
	MonoMethodSignature *sig;
	CallInfo *cinfo;
	MonoType *rtype;
	MonoType **param_types;
} ArchDynCallInfo;

static gboolean
dyn_call_supported (CallInfo *cinfo, MonoMethodSignature *sig)
{
	int i;

	switch (cinfo->ret.storage) {
	case RegTypeNone:
	case RegTypeGeneral:
	case RegTypeIRegPair:
	case RegTypeStructByAddr:
		break;
	case RegTypeFP:
		if (IS_VFP)
			break;
		else
			return FALSE;
	default:
		return FALSE;
	}

	for (i = 0; i < cinfo->nargs; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];
		int last_slot;

		switch (ainfo->storage) {
		case RegTypeGeneral:
		case RegTypeIRegPair:
		case RegTypeBaseGen:
		case RegTypeFP:
			break;
		case RegTypeBase:
			break;
		case RegTypeStructByVal:
			if (ainfo->size == 0)
				last_slot = PARAM_REGS + (ainfo->offset / 4) + ainfo->vtsize;
			else
				last_slot = ainfo->reg + ainfo->size + ainfo->vtsize;
			break;
		default:
			return FALSE;
		}
	}

	// FIXME: Can't use cinfo only as it doesn't contain info about I8/float */
	for (i = 0; i < sig->param_count; ++i) {
		MonoType *t = sig->params [i];

		if (t->byref)
			continue;

		t = mini_get_underlying_type (t);

		switch (t->type) {
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (IS_SOFT_FLOAT)
				return FALSE;
			else
				break;
			/*
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			return FALSE;
			*/
		default:
			break;
		}
	}

	return TRUE;
}

MonoDynCallInfo*
mono_arch_dyn_call_prepare (MonoMethodSignature *sig)
{
	ArchDynCallInfo *info;
	CallInfo *cinfo;
	int i;

	cinfo = get_call_info (NULL, sig);

	if (!dyn_call_supported (cinfo, sig)) {
		g_free (cinfo);
		return NULL;
	}

	info = g_new0 (ArchDynCallInfo, 1);
	// FIXME: Preprocess the info to speed up start_dyn_call ()
	info->sig = sig;
	info->cinfo = cinfo;
	info->rtype = mini_get_underlying_type (sig->ret);
	info->param_types = g_new0 (MonoType*, sig->param_count);
	for (i = 0; i < sig->param_count; ++i)
		info->param_types [i] = mini_get_underlying_type (sig->params [i]);
	
	return (MonoDynCallInfo*)info;
}

void
mono_arch_dyn_call_free (MonoDynCallInfo *info)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;

	g_free (ainfo->cinfo);
	g_free (ainfo);
}

int
mono_arch_dyn_call_get_buf_size (MonoDynCallInfo *info)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;

	g_assert (ainfo->cinfo->stack_usage % MONO_ARCH_FRAME_ALIGNMENT == 0);
	return sizeof (DynCallArgs) + ainfo->cinfo->stack_usage;
}

void
mono_arch_start_dyn_call (MonoDynCallInfo *info, gpointer **args, guint8 *ret, guint8 *buf)
{
	ArchDynCallInfo *dinfo = (ArchDynCallInfo*)info;
	CallInfo *cinfo = dinfo->cinfo;
	DynCallArgs *p = (DynCallArgs*)buf;
	int arg_index, greg, i, j, pindex;
	MonoMethodSignature *sig = dinfo->sig;

	p->res = 0;
	p->ret = ret;
	p->has_fpregs = 0;
	p->n_stackargs = cinfo->stack_usage / sizeof (host_mgreg_t);

	arg_index = 0;
	greg = 0;
	pindex = 0;

	if (sig->hasthis || dinfo->cinfo->vret_arg_index == 1) {
		p->regs [greg ++] = (host_mgreg_t)(gsize)*(args [arg_index ++]);
		if (!sig->hasthis)
			pindex = 1;
	}

	if (dinfo->cinfo->ret.storage == RegTypeStructByAddr)
		p->regs [greg ++] = (host_mgreg_t)(gsize)ret;

	for (i = pindex; i < sig->param_count; i++) {
		MonoType *t = dinfo->param_types [i];
		gpointer *arg = args [arg_index ++];
		ArgInfo *ainfo = &dinfo->cinfo->args [i + sig->hasthis];
		int slot = -1;

		if (ainfo->storage == RegTypeGeneral || ainfo->storage == RegTypeIRegPair || ainfo->storage == RegTypeStructByVal) {
			slot = ainfo->reg;
		} else if (ainfo->storage == RegTypeFP) {
		} else if (ainfo->storage == RegTypeBase) {
			slot = PARAM_REGS + (ainfo->offset / 4);
		} else if (ainfo->storage == RegTypeBaseGen) {
			/* slot + 1 is the first stack slot, so the code below will work */
			slot = 3;
		} else {
			g_assert_not_reached ();
		}

		if (t->byref) {
			p->regs [slot] = (host_mgreg_t)(gsize)*arg;
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			p->regs [slot] = (host_mgreg_t)(gsize)*arg;
			break;
		case MONO_TYPE_U1:
			p->regs [slot] = *(guint8*)arg;
			break;
		case MONO_TYPE_I1:
			p->regs [slot] = *(gint8*)arg;
			break;
		case MONO_TYPE_I2:
			p->regs [slot] = *(gint16*)arg;
			break;
		case MONO_TYPE_U2:
			p->regs [slot] = *(guint16*)arg;
			break;
		case MONO_TYPE_I4:
			p->regs [slot] = *(gint32*)arg;
			break;
		case MONO_TYPE_U4:
			p->regs [slot] = *(guint32*)arg;
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			p->regs [slot ++] = (host_mgreg_t)(gsize)arg [0];
			p->regs [slot] = (host_mgreg_t)(gsize)arg [1];
			break;
		case MONO_TYPE_R4:
			if (ainfo->storage == RegTypeFP) {
				float f = *(float*)arg;
				p->fpregs [ainfo->reg / 2] = *(double*)&f;
				p->has_fpregs = 1;
			} else {
				p->regs [slot] = *(host_mgreg_t*)arg;
			}
			break;
		case MONO_TYPE_R8:
			if (ainfo->storage == RegTypeFP) {
				p->fpregs [ainfo->reg / 2] = *(double*)arg;
				p->has_fpregs = 1;
			} else {
				p->regs [slot ++] = (host_mgreg_t)(gsize)arg [0];
				p->regs [slot] = (host_mgreg_t)(gsize)arg [1];
			}
			break;
		case MONO_TYPE_GENERICINST:
			if (MONO_TYPE_IS_REFERENCE (t)) {
				p->regs [slot] = (host_mgreg_t)(gsize)*arg;
				break;
			} else {
				if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type_internal (t))) {
					MonoClass *klass = mono_class_from_mono_type_internal (t);
					guint8 *nullable_buf;
					int size;

					size = mono_class_value_size (klass, NULL);
					nullable_buf = g_alloca (size);
					g_assert (nullable_buf);

					/* The argument pointed to by arg is either a boxed vtype or null */
					mono_nullable_init (nullable_buf, (MonoObject*)arg, klass);

					arg = (gpointer*)nullable_buf;
					/* Fall though */
				} else {
					/* Fall though */
				}
			}
		case MONO_TYPE_VALUETYPE:
			g_assert (ainfo->storage == RegTypeStructByVal);

			if (ainfo->size == 0)
				slot = PARAM_REGS + (ainfo->offset / 4);
			else
				slot = ainfo->reg;

			for (j = 0; j < ainfo->size + ainfo->vtsize; ++j)
				p->regs [slot ++] = ((host_mgreg_t*)arg) [j];
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void
mono_arch_finish_dyn_call (MonoDynCallInfo *info, guint8 *buf)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;
	DynCallArgs *p = (DynCallArgs*)buf;
	MonoType *ptype = ainfo->rtype;
	guint8 *ret = p->ret;
	host_mgreg_t res = p->res;
	host_mgreg_t res2 = p->res2;

	switch (ptype->type) {
	case MONO_TYPE_VOID:
		*(gpointer*)ret = NULL;
		break;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		*(gpointer*)ret = (gpointer)(gsize)res;
		break;
	case MONO_TYPE_I1:
		*(gint8*)ret = res;
		break;
	case MONO_TYPE_U1:
		*(guint8*)ret = res;
		break;
	case MONO_TYPE_I2:
		*(gint16*)ret = res;
		break;
	case MONO_TYPE_U2:
		*(guint16*)ret = res;
		break;
	case MONO_TYPE_I4:
		*(gint32*)ret = res;
		break;
	case MONO_TYPE_U4:
		*(guint32*)ret = res;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		/* This handles endianness as well */
		((gint32*)ret) [0] = res;
		((gint32*)ret) [1] = res2;
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_IS_REFERENCE (ptype)) {
			*(gpointer*)ret = (gpointer)res;
			break;
		} else {
			/* Fall though */
		}
	case MONO_TYPE_VALUETYPE:
		g_assert (ainfo->cinfo->ret.storage == RegTypeStructByAddr);
		/* Nothing to do */
		break;
	case MONO_TYPE_R4:
		g_assert (IS_VFP);
		if (IS_HARD_FLOAT)
			*(float*)ret = *(float*)&p->fpregs [0];
		else
			*(float*)ret = *(float*)&res;
		break;
	case MONO_TYPE_R8: {
		host_mgreg_t regs [2];

		g_assert (IS_VFP);
		if (IS_HARD_FLOAT) {
			*(double*)ret = p->fpregs [0];
		} else {
			regs [0] = res;
			regs [1] = res2;

			*(double*)ret = *(double*)&regs;
		}
		break;
	}
	default:
		g_assert_not_reached ();
	}
}

#ifndef DISABLE_JIT

/*
 * The immediate field for cond branches is big enough for all reasonable methods
 */
#define EMIT_COND_BRANCH_FLAGS(ins,condcode) \
if (0 && ins->inst_true_bb->native_offset) { \
	ARM_B_COND (code, (condcode), (code - cfg->native_code + ins->inst_true_bb->native_offset) & 0xffffff); \
} else { \
	mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	ARM_B_COND (code, (condcode), 0); \
}

#define EMIT_COND_BRANCH(ins,cond) EMIT_COND_BRANCH_FLAGS(ins, branch_cc_table [(cond)])

/* emit an exception if condition is fail
 *
 * We assign the extra code used to throw the implicit exceptions
 * to cfg->bb_exit as far as the big branch handling is concerned
 */
#define EMIT_COND_SYSTEM_EXCEPTION_FLAGS(condcode,exc_name)            \
        do {                                                        \
		mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name); \
		ARM_BL_COND (code, (condcode), 0); \
	} while (0); 

#define EMIT_COND_SYSTEM_EXCEPTION(cond,exc_name) EMIT_COND_SYSTEM_EXCEPTION_FLAGS(branch_cc_table [(cond)], (exc_name))

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		MonoInst *last_ins = mono_inst_prev (ins, FILTER_IL_SEQ_POINT);

		switch (ins->opcode) {
		case OP_MUL_IMM: 
		case OP_IMUL_IMM: 
			/* Already done by an arch-independent pass */
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
			/* 
			 * OP_STORE_MEMBASE_REG reg, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG 
					 || last_ins->opcode == OP_STORE_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					MONO_DELETE_INS (bb, ins);
					continue;
				} else {
					//static int c = 0; g_print ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}

			/* 
			 * Note: reg1 must be different from the basereg in the second load
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_MOVE reg1, reg2
			 */
			} if (last_ins && (last_ins->opcode == OP_LOADI4_MEMBASE
					   || last_ins->opcode == OP_LOAD_MEMBASE) &&
			      ins->inst_basereg != last_ins->dreg &&
			      ins->inst_basereg == last_ins->inst_basereg &&
			      ins->inst_offset == last_ins->inst_offset) {

				if (ins->dreg == last_ins->dreg) {
					MONO_DELETE_INS (bb, ins);
					continue;
				} else {
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->dreg;
				}

				//g_assert_not_reached ();

#if 0
			/* 
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 * -->
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_ICONST reg, imm
			 */
			} else if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_IMM
						|| last_ins->opcode == OP_STORE_MEMBASE_IMM) &&
				   ins->inst_basereg == last_ins->inst_destbasereg &&
				   ins->inst_offset == last_ins->inst_offset) {
				//static int c = 0; g_print ("MATCHX %s %d\n", cfg->method->name,c++);
				ins->opcode = OP_ICONST;
				ins->inst_c0 = last_ins->inst_imm;
				g_assert_not_reached (); // check this rule
#endif
			}
			break;
		case OP_LOADU1_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI1_MEMBASE) ? OP_ICONV_TO_I1 : OP_ICONV_TO_U1;
				ins->sreg1 = last_ins->sreg1;				
			}
			break;
		case OP_LOADU2_MEMBASE:
		case OP_LOADI2_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI2_MEMBASE) ? OP_ICONV_TO_I2 : OP_ICONV_TO_U2;
				ins->sreg1 = last_ins->sreg1;				
			}
			break;
		case OP_MOVE:
			ins->opcode = OP_MOVE;
			/* 
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			/* 
			 * OP_MOVE sreg, dreg 
			 * OP_MOVE dreg, sreg
			 */
			if (last_ins && last_ins->opcode == OP_MOVE &&
			    ins->sreg1 == last_ins->dreg &&
			    ins->dreg == last_ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			break;
		}
	}
}

/* 
 * the branch_cc_table should maintain the order of these
 * opcodes.
case CEE_BEQ:
case CEE_BGE:
case CEE_BGT:
case CEE_BLE:
case CEE_BLT:
case CEE_BNE_UN:
case CEE_BGE_UN:
case CEE_BGT_UN:
case CEE_BLE_UN:
case CEE_BLT_UN:
 */
static const guchar 
branch_cc_table [] = {
	ARMCOND_EQ, 
	ARMCOND_GE, 
	ARMCOND_GT, 
	ARMCOND_LE,
	ARMCOND_LT, 
	
	ARMCOND_NE, 
	ARMCOND_HS, 
	ARMCOND_HI, 
	ARMCOND_LS,
	ARMCOND_LO
};

#define ADD_NEW_INS(cfg,dest,op) do {       \
		MONO_INST_NEW ((cfg), (dest), (op)); \
        mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

static int
map_to_reg_reg_op (int op)
{
	switch (op) {
	case OP_ADD_IMM:
		return OP_IADD;
	case OP_SUB_IMM:
		return OP_ISUB;
	case OP_AND_IMM:
		return OP_IAND;
	case OP_COMPARE_IMM:
		return OP_COMPARE;
	case OP_ICOMPARE_IMM:
		return OP_ICOMPARE;
	case OP_ADDCC_IMM:
		return OP_ADDCC;
	case OP_ADC_IMM:
		return OP_ADC;
	case OP_SUBCC_IMM:
		return OP_SUBCC;
	case OP_SBB_IMM:
		return OP_SBB;
	case OP_OR_IMM:
		return OP_IOR;
	case OP_XOR_IMM:
		return OP_IXOR;
	case OP_LOAD_MEMBASE:
		return OP_LOAD_MEMINDEX;
	case OP_LOADI4_MEMBASE:
		return OP_LOADI4_MEMINDEX;
	case OP_LOADU4_MEMBASE:
		return OP_LOADU4_MEMINDEX;
	case OP_LOADU1_MEMBASE:
		return OP_LOADU1_MEMINDEX;
	case OP_LOADI2_MEMBASE:
		return OP_LOADI2_MEMINDEX;
	case OP_LOADU2_MEMBASE:
		return OP_LOADU2_MEMINDEX;
	case OP_LOADI1_MEMBASE:
		return OP_LOADI1_MEMINDEX;
	case OP_STOREI1_MEMBASE_REG:
		return OP_STOREI1_MEMINDEX;
	case OP_STOREI2_MEMBASE_REG:
		return OP_STOREI2_MEMINDEX;
	case OP_STOREI4_MEMBASE_REG:
		return OP_STOREI4_MEMINDEX;
	case OP_STORE_MEMBASE_REG:
		return OP_STORE_MEMINDEX;
	case OP_STORER4_MEMBASE_REG:
		return OP_STORER4_MEMINDEX;
	case OP_STORER8_MEMBASE_REG:
		return OP_STORER8_MEMINDEX;
	case OP_STORE_MEMBASE_IMM:
		return OP_STORE_MEMBASE_REG;
	case OP_STOREI1_MEMBASE_IMM:
		return OP_STOREI1_MEMBASE_REG;
	case OP_STOREI2_MEMBASE_IMM:
		return OP_STOREI2_MEMBASE_REG;
	case OP_STOREI4_MEMBASE_IMM:
		return OP_STOREI4_MEMBASE_REG;
	}
	g_assert_not_reached ();
}

/*
 * Remove from the instruction list the instructions that can't be
 * represented with very simple instructions with no register
 * requirements.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *temp, *last_ins = NULL;
	int rot_amount, imm8, low_imm;

	MONO_BB_FOR_EACH_INS (bb, ins) {
loop_start:
		switch (ins->opcode) {
		case OP_ADD_IMM:
		case OP_SUB_IMM:
		case OP_AND_IMM:
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		case OP_ADDCC_IMM:
		case OP_ADC_IMM:
		case OP_SUBCC_IMM:
		case OP_SBB_IMM:
		case OP_OR_IMM:
		case OP_XOR_IMM:
		case OP_IADD_IMM:
		case OP_ISUB_IMM:
		case OP_IAND_IMM:
		case OP_IADC_IMM:
		case OP_ISBB_IMM:
		case OP_IOR_IMM:
		case OP_IXOR_IMM:
			if ((imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount)) < 0) {
				int opcode2 = mono_op_imm_to_op (ins->opcode);
				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				if (opcode2 == -1)
					g_error ("mono_op_imm_to_op failed for %s\n", mono_inst_name (ins->opcode));
				ins->opcode = opcode2;
			}
			if (ins->opcode == OP_SBB || ins->opcode == OP_ISBB || ins->opcode == OP_SUBCC)
				goto loop_start;
			else
				break;
		case OP_MUL_IMM:
		case OP_IMUL_IMM:
			if (ins->inst_imm == 1) {
				ins->opcode = OP_MOVE;
				break;
			}
			if (ins->inst_imm == 0) {
				ins->opcode = OP_ICONST;
				ins->inst_c0 = 0;
				break;
			}
			imm8 = mono_is_power_of_two (ins->inst_imm);
			if (imm8 > 0) {
				ins->opcode = OP_SHL_IMM;
				ins->inst_imm = imm8;
				break;
			}
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = OP_IMUL;
			break;
		case OP_SBB:
		case OP_ISBB:
		case OP_SUBCC:
		case OP_ISUBCC: {
			int try_count = 2;
			MonoInst *current = ins;

			/* may require a look-ahead of a couple instructions due to spilling */
			while (try_count-- && current->next) {
				if (current->next->opcode == OP_COND_EXC_C || current->next->opcode == OP_COND_EXC_IC) {
					/* ARM sets the C flag to 1 if there was _no_ overflow */
					current->next->opcode = OP_COND_EXC_NC;
					break;
				}
				current = current->next;
			}
			break;
		}
		case OP_IDIV_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_IMM:
		case OP_IREM_UN_IMM: {
			int opcode2 = mono_op_imm_to_op (ins->opcode);
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			if (opcode2 == -1)
				g_error ("mono_op_imm_to_op failed for %s\n", mono_inst_name (ins->opcode));
			ins->opcode = opcode2;
			break;
		}
		case OP_LOCALLOC_IMM:
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = temp->dreg;
			ins->opcode = OP_LOCALLOC;
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
		case OP_LOADU1_MEMBASE:
			/* we can do two things: load the immed in a register
			 * and use an indexed load, or see if the immed can be
			 * represented as an ad_imm + a load with a smaller offset
			 * that fits. We just do the first for now, optimize later.
			 */
			if (arm_is_imm12 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_LOADI2_MEMBASE:
		case OP_LOADU2_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (arm_is_imm8 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_LOADR4_MEMBASE:
		case OP_LOADR8_MEMBASE:
			if (arm_is_fpimm8 (ins->inst_offset))
				break;
			low_imm = ins->inst_offset & 0x1ff;
			if ((imm8 = mono_arm_is_rotated_imm8 (ins->inst_offset & ~0x1ff, &rot_amount)) >= 0) {
				ADD_NEW_INS (cfg, temp, OP_ADD_IMM);
				temp->inst_imm = ins->inst_offset & ~0x1ff;
				temp->sreg1 = ins->inst_basereg;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = temp->dreg;
				ins->inst_offset = low_imm;
			} else {
				MonoInst *add_ins;

				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);

				ADD_NEW_INS (cfg, add_ins, OP_IADD);
				add_ins->sreg1 = ins->inst_basereg;
				add_ins->sreg2 = temp->dreg;
				add_ins->dreg = mono_alloc_ireg (cfg);

				ins->inst_basereg = add_ins->dreg;
				ins->inst_offset = 0;
			}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
		case OP_STOREI1_MEMBASE_REG:
			if (arm_is_imm12 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_STOREI2_MEMBASE_REG:
			if (arm_is_imm8 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_STORER4_MEMBASE_REG:
		case OP_STORER8_MEMBASE_REG:
			if (arm_is_fpimm8 (ins->inst_offset))
				break;
			low_imm = ins->inst_offset & 0x1ff;
			if ((imm8 = mono_arm_is_rotated_imm8 (ins->inst_offset & ~ 0x1ff, &rot_amount)) >= 0 && arm_is_fpimm8 (low_imm)) {
				ADD_NEW_INS (cfg, temp, OP_ADD_IMM);
				temp->inst_imm = ins->inst_offset & ~0x1ff;
				temp->sreg1 = ins->inst_destbasereg;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->inst_destbasereg = temp->dreg;
				ins->inst_offset = low_imm;
			} else {
				MonoInst *add_ins;

				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);

				ADD_NEW_INS (cfg, add_ins, OP_IADD);
				add_ins->sreg1 = ins->inst_destbasereg;
				add_ins->sreg2 = temp->dreg;
				add_ins->dreg = mono_alloc_ireg (cfg);

				ins->inst_destbasereg = add_ins->dreg;
				ins->inst_offset = 0;
			}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			last_ins = temp;
			goto loop_start; /* make it handle the possibly big ins->inst_offset */
		case OP_FCOMPARE:
		case OP_RCOMPARE: {
			gboolean swap = FALSE;
			int reg;

			if (!ins->next) {
				/* Optimized away */
				NULLIFY_INS (ins);
				break;
			}

			/* Some fp compares require swapped operands */
			switch (ins->next->opcode) {
			case OP_FBGT:
				ins->next->opcode = OP_FBLT;
				swap = TRUE;
				break;
			case OP_FBGT_UN:
				ins->next->opcode = OP_FBLT_UN;
				swap = TRUE;
				break;
			case OP_FBLE:
				ins->next->opcode = OP_FBGE;
				swap = TRUE;
				break;
			case OP_FBLE_UN:
				ins->next->opcode = OP_FBGE_UN;
				swap = TRUE;
				break;
			default:
				break;
			}
			if (swap) {
				reg = ins->sreg1;
				ins->sreg1 = ins->sreg2;
				ins->sreg2 = reg;
			}
			break;
		}
		}

		last_ins = ins;
	}
	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;
}

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *long_ins)
{
	MonoInst *ins;

	if (long_ins->opcode == OP_LNEG) {
		ins = long_ins;
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ARM_RSBS_IMM, MONO_LVREG_LS (ins->dreg), MONO_LVREG_LS (ins->sreg1), 0);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ARM_RSC_IMM, MONO_LVREG_MS (ins->dreg), MONO_LVREG_MS (ins->sreg1), 0);
		NULLIFY_INS (ins);
	}
}

static guchar*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg  */
	if (IS_VFP) {
		code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
		if (is_signed)
			ARM_TOSIZD (code, vfp_scratch1, sreg);
		else
			ARM_TOUIZD (code, vfp_scratch1, sreg);
		ARM_FMRS (code, dreg, vfp_scratch1);
		code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
	}
	if (!is_signed) {
		if (size == 1)
			ARM_AND_REG_IMM8 (code, dreg, dreg, 0xff);
		else if (size == 2) {
			ARM_SHL_IMM (code, dreg, dreg, 16);
			ARM_SHR_IMM (code, dreg, dreg, 16);
		}
	} else {
		if (size == 1) {
			ARM_SHL_IMM (code, dreg, dreg, 24);
			ARM_SAR_IMM (code, dreg, dreg, 24);
		} else if (size == 2) {
			ARM_SHL_IMM (code, dreg, dreg, 16);
			ARM_SAR_IMM (code, dreg, dreg, 16);
		}
	}
	return code;
}

static guchar*
emit_r4_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg  */
	g_assert (IS_VFP);
	code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
	if (is_signed)
		ARM_TOSIZS (code, vfp_scratch1, sreg);
	else
		ARM_TOUIZS (code, vfp_scratch1, sreg);
	ARM_FMRS (code, dreg, vfp_scratch1);
	code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);

	if (!is_signed) {
		if (size == 1)
			ARM_AND_REG_IMM8 (code, dreg, dreg, 0xff);
		else if (size == 2) {
			ARM_SHL_IMM (code, dreg, dreg, 16);
			ARM_SHR_IMM (code, dreg, dreg, 16);
		}
	} else {
		if (size == 1) {
			ARM_SHL_IMM (code, dreg, dreg, 24);
			ARM_SAR_IMM (code, dreg, dreg, 24);
		} else if (size == 2) {
			ARM_SHL_IMM (code, dreg, dreg, 16);
			ARM_SAR_IMM (code, dreg, dreg, 16);
		}
	}
	return code;
}

#endif /* #ifndef DISABLE_JIT */

#define is_call_imm(diff) ((gint)(diff) >= -33554432 && (gint)(diff) <= 33554431)

static void
emit_thunk (guint8 *code, gconstpointer target)
{
	guint8 *p = code;

	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
	if (thumb_supported)
		ARM_BX (code, ARMREG_IP);
	else
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	*(guint32*)code = (guint32)(gsize)target;
	code += 4;
	mono_arch_flush_icache (p, code - p);
}

static void
handle_thunk (MonoCompile *cfg, MonoDomain *domain, guchar *code, const guchar *target)
{
	MonoJitInfo *ji = NULL;
	MonoThunkJitInfo *info;
	guint8 *thunks, *p;
	int thunks_size;
	guint8 *orig_target;
	guint8 *target_thunk;

	if (!domain)
		domain = mono_domain_get ();

	if (cfg) {
		/*
		 * This can be called multiple times during JITting,
		 * save the current position in cfg->arch to avoid
		 * doing a O(n^2) search.
		 */
		if (!cfg->arch.thunks) {
			cfg->arch.thunks = cfg->thunks;
			cfg->arch.thunks_size = cfg->thunk_area;
		}
		thunks = cfg->arch.thunks;
		thunks_size = cfg->arch.thunks_size;
		if (!thunks_size) {
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size, mono_method_full_name (cfg->method, TRUE));
			g_assert_not_reached ();
		}

		g_assert (*(guint32*)thunks == 0);
		emit_thunk (thunks, target);
		arm_patch (code, thunks);

		cfg->arch.thunks += THUNK_SIZE;
		cfg->arch.thunks_size -= THUNK_SIZE;
	} else {
		ji = mini_jit_info_table_find (domain, (char*)code, NULL);
		g_assert (ji);
		info = mono_jit_info_get_thunk_info (ji);
		g_assert (info);

		thunks = (guint8*)ji->code_start + info->thunks_offset;
		thunks_size = info->thunks_size;

		orig_target = mono_arch_get_call_target (code + 4);

		mono_mini_arch_lock ();

		target_thunk = NULL;
		if (orig_target >= thunks && orig_target < thunks + thunks_size) {
			/* The call already points to a thunk, because of trampolines etc. */
			target_thunk = orig_target;
		} else {
			for (p = thunks; p < thunks + thunks_size; p += THUNK_SIZE) {
				if (((guint32*)p) [0] == 0) {
					/* Free entry */
					target_thunk = p;
					break;
				} else if (((guint32*)p) [2] == (guint32)(gsize)target) {
					/* Thunk already points to target */
					target_thunk = p;
					break;
				}
			}
		}

		//g_print ("THUNK: %p %p %p\n", code, target, target_thunk);

		if (!target_thunk) {
			mono_mini_arch_unlock ();
			g_print ("thunk failed %p->%p, thunk space=%d method %s", code, target, thunks_size, cfg ? mono_method_full_name (cfg->method, TRUE) : mono_method_full_name (jinfo_get_method (ji), TRUE));
			g_assert_not_reached ();
		}

		emit_thunk (target_thunk, target);
		arm_patch (code, target_thunk);
		mono_arch_flush_icache (code, 4);

		mono_mini_arch_unlock ();
	}
}

static void
arm_patch_general (MonoCompile *cfg, MonoDomain *domain, guchar *code, const guchar *target)
{
	guint32 *code32 = (guint32*)code;
	guint32 ins = *code32;
	guint32 prim = (ins >> 25) & 7;
	guint32 tval = GPOINTER_TO_UINT (target);

	//g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);
	if (prim == 5) { /* 101b */
		/* the diff starts 8 bytes from the branch opcode */
		gint diff = target - code - 8;
		gint tbits;
		gint tmask = 0xffffffff;
		if (tval & 1) { /* entering thumb mode */
			diff = target - 1 - code - 8;
			g_assert (thumb_supported);
			tbits = 0xf << 28; /* bl->blx bit pattern */
			g_assert ((ins & (1 << 24))); /* it must be a bl, not b instruction */
			/* this low bit of the displacement is moved to bit 24 in the instruction encoding */
			if (diff & 2) {
				tbits |= 1 << 24;
			}
			tmask = ~(1 << 24); /* clear the link bit */
			/*g_print ("blx to thumb: target: %p, code: %p, diff: %d, mask: %x\n", target, code, diff, tmask);*/
		} else {
			tbits = 0;
		}
		if (diff >= 0) {
			if (diff <= 33554431) {
				diff >>= 2;
				ins = (ins & 0xff000000) | diff;
				ins &= tmask;
				*code32 = ins | tbits;
				return;
			}
		} else {
			/* diff between 0 and -33554432 */
			if (diff >= -33554432) {
				diff >>= 2;
				ins = (ins & 0xff000000) | (diff & ~0xff000000);
				ins &= tmask;
				*code32 = ins | tbits;
				return;
			}
		}
		
		handle_thunk (cfg, domain, code, target);
		return;
	}

	/*
	 * The alternative call sequences looks like this:
	 *
	 * 	ldr ip, [pc] // loads the address constant
	 * 	b 1f         // jumps around the constant
	 * 	address constant embedded in the code
	 *   1f:
	 * 	mov lr, pc
	 * 	mov pc, ip
	 *
	 * There are two cases for patching:
	 * a) at the end of method emission: in this case code points to the start
	 *    of the call sequence
	 * b) during runtime patching of the call site: in this case code points
	 *    to the mov pc, ip instruction
	 *
	 * We have to handle also the thunk jump code sequence:
	 *
	 * 	ldr ip, [pc]
	 * 	mov pc, ip
	 * 	address constant // execution never reaches here
	 */
	if ((ins & 0x0ffffff0) == 0x12fff10) {
		/* Branch and exchange: the address is constructed in a reg 
		 * We can patch BX when the code sequence is the following:
		 *  ldr     ip, [pc, #0]    ; 0x8
		 *  b       0xc
   		 *  .word code_ptr
   	 	 *  mov     lr, pc
  		 *  bx      ips
		 * */
		guint32 ccode [4];
		guint8 *emit = (guint8*)ccode;
		ARM_LDR_IMM (emit, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (emit, 0);
		ARM_MOV_REG_REG (emit, ARMREG_LR, ARMREG_PC);
		ARM_BX (emit, ARMREG_IP);

		/*patching from magic trampoline*/
		if (ins == ccode [3]) {
			g_assert (code32 [-4] == ccode [0]);
			g_assert (code32 [-3] == ccode [1]);
			g_assert (code32 [-1] == ccode [2]);
			code32 [-2] = (guint32)(gsize)target;
			return;
		}
		/*patching from JIT*/
		if (ins == ccode [0]) {
			g_assert (code32 [1] == ccode [1]);
			g_assert (code32 [3] == ccode [2]);
			g_assert (code32 [4] == ccode [3]);
			code32 [2] = (guint32)(gsize)target;
			return;
		}
		g_assert_not_reached ();
	} else if ((ins & 0x0ffffff0) == 0x12fff30) {
		/*
		 * ldr ip, [pc, #0]
		 * b 0xc
		 * .word code_ptr
		 * blx ip
		 */
		guint32 ccode [4];
		guint8 *emit = (guint8*)ccode;
		ARM_LDR_IMM (emit, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (emit, 0);
		ARM_BLX_REG (emit, ARMREG_IP);

		g_assert (code32 [-3] == ccode [0]);
		g_assert (code32 [-2] == ccode [1]);
		g_assert (code32 [0] == ccode [2]);

		code32 [-1] = (guint32)(gsize)target;
	} else {
		guint32 ccode [4];
		guint32 *tmp = ccode;
		guint8 *emit = (guint8*)tmp;
		ARM_LDR_IMM (emit, ARMREG_IP, ARMREG_PC, 0);
		ARM_MOV_REG_REG (emit, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG (emit, ARMREG_PC, ARMREG_IP);
		ARM_BX (emit, ARMREG_IP);
		if (ins == ccode [2]) {
			g_assert_not_reached (); // should be -2 ...
			code32 [-1] = (guint32)(gsize)target;
			return;
		}
		if (ins == ccode [0]) {
			/* handles both thunk jump code and the far call sequence */
			code32 [2] = (guint32)(gsize)target;
			return;
		}
		g_assert_not_reached ();
	}
//	g_print ("patched with 0x%08x\n", ins);
}

void
arm_patch (guchar *code, const guchar *target)
{
	arm_patch_general (NULL, NULL, code, target);
}

/* 
 * Return the >= 0 uimm8 value if val can be represented with a byte + rotation
 * (with the rotation amount in *rot_amount. rot_amount is already adjusted
 * to be used with the emit macros.
 * Return -1 otherwise.
 */
int
mono_arm_is_rotated_imm8 (guint32 val, gint *rot_amount)
{
	guint32 res, i;
	for (i = 0; i < 31; i+= 2) {
		if (i == 0)
			res = val;
		else
			res = (val << (32 - i)) | (val >> i);
		if (res & ~0xff)
			continue;
		*rot_amount = i? 32 - i: 0;
		return res;
	}
	return -1;
}

/*
 * Emits in code a sequence of instructions that load the value 'val'
 * into the dreg register. Uses at most 4 instructions.
 */
guint8*
mono_arm_emit_load_imm (guint8 *code, int dreg, guint32 val)
{
	int imm8, rot_amount;
#if 0
	ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
	/* skip the constant pool */
	ARM_B (code, 0);
	*(int*)code = val;
	code += 4;
	return code;
#endif
	if (mini_get_debug_options()->single_imm_size && v7_supported) {
		ARM_MOVW_REG_IMM (code, dreg, val & 0xffff);
		ARM_MOVT_REG_IMM (code, dreg, (val >> 16) & 0xffff);
		return code;
	}

	if ((imm8 = mono_arm_is_rotated_imm8 (val, &rot_amount)) >= 0) {
		ARM_MOV_REG_IMM (code, dreg, imm8, rot_amount);
	} else if ((imm8 = mono_arm_is_rotated_imm8 (~val, &rot_amount)) >= 0) {
		ARM_MVN_REG_IMM (code, dreg, imm8, rot_amount);
	} else {
		if (v7_supported) {
			ARM_MOVW_REG_IMM (code, dreg, val & 0xffff);
			if (val >> 16)
				ARM_MOVT_REG_IMM (code, dreg, (val >> 16) & 0xffff);
			return code;
		}
		if (val & 0xFF) {
			ARM_MOV_REG_IMM8 (code, dreg, (val & 0xFF));
			if (val & 0xFF00) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF00) >> 8, 24);
			}
			if (val & 0xFF0000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF0000) >> 16, 16);
			}
			if (val & 0xFF000000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF000000) >> 24, 8);
			}
		} else if (val & 0xFF00) {
			ARM_MOV_REG_IMM (code, dreg, (val & 0xFF00) >> 8, 24);
			if (val & 0xFF0000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF0000) >> 16, 16);
			}
			if (val & 0xFF000000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF000000) >> 24, 8);
			}
		} else if (val & 0xFF0000) {
			ARM_MOV_REG_IMM (code, dreg, (val & 0xFF0000) >> 16, 16);
			if (val & 0xFF000000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF000000) >> 24, 8);
			}
		}
		//g_assert_not_reached ();
	}
	return code;
}

gboolean
mono_arm_thumb_supported (void)
{
	return thumb_supported;
}

gboolean
mono_arm_eabi_supported (void)
{
	return eabi_supported;
}

int
mono_arm_i8_align (void)
{
	return i8_align;
}

#ifndef DISABLE_JIT

static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	CallInfo *cinfo;
	MonoCallInst *call;

	call = (MonoCallInst*)ins;
	cinfo = call->call_info;

	switch (cinfo->ret.storage) {
	case RegTypeStructByVal:
	case RegTypeHFA: {
		MonoInst *loc = cfg->arch.vret_addr_loc;
		int i;

		if (cinfo->ret.storage == RegTypeStructByVal && cinfo->ret.nregs == 1) {
			/* The JIT treats this as a normal call */
			break;
		}

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);

		if (arm_is_imm12 (loc->inst_offset)) {
			ARM_LDR_IMM (code, ARMREG_LR, loc->inst_basereg, loc->inst_offset);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_LR, loc->inst_offset);
			ARM_LDR_REG_REG (code, ARMREG_LR, loc->inst_basereg, ARMREG_LR);
		}

		if (cinfo->ret.storage == RegTypeStructByVal) {
			int rsize = cinfo->ret.struct_size;

			for (i = 0; i < cinfo->ret.nregs; ++i) {
				g_assert (rsize >= 0);
				switch (rsize) {
				case 0:
					break;
				case 1:
					ARM_STRB_IMM (code, i, ARMREG_LR, i * 4);
					break;
				case 2:
					ARM_STRH_IMM (code, i, ARMREG_LR, i * 4);
					break;
				default:
					ARM_STR_IMM (code, i, ARMREG_LR, i * 4);
					break;
				}
				rsize -= 4;
			}
		} else {
			for (i = 0; i < cinfo->ret.nregs; ++i) {
				if (cinfo->ret.esize == 4)
					ARM_FSTS (code, cinfo->ret.reg + i, ARMREG_LR, i * 4);
				else
					ARM_FSTD (code, cinfo->ret.reg + (i * 2), ARMREG_LR, i * 8);
			}
		}
		return code;
	}
	default:
		break;
	}

	switch (ins->opcode) {
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		if (IS_VFP) {
			MonoType *sig_ret = mini_get_underlying_type (((MonoCallInst*)ins)->signature->ret);
			if (sig_ret->type == MONO_TYPE_R4) {
				if (IS_HARD_FLOAT) {
					ARM_CVTS (code, ins->dreg, ARM_VFP_F0);
				} else {
					ARM_FMSR (code, ins->dreg, ARMREG_R0);
					ARM_CVTS (code, ins->dreg, ins->dreg);
				}
			} else {
				if (IS_HARD_FLOAT) {
					ARM_CPYD (code, ins->dreg, ARM_VFP_D0);
				} else {
					ARM_FMDRR (code, ARMREG_R0, ARMREG_R1, ins->dreg);
				}
			}
		}
		break;
	case OP_RCALL:
	case OP_RCALL_REG:
	case OP_RCALL_MEMBASE: {
		MonoType *sig_ret;

		g_assert (IS_VFP);

		sig_ret = mini_get_underlying_type (((MonoCallInst*)ins)->signature->ret);
		g_assert (sig_ret->type == MONO_TYPE_R4);
		if (IS_HARD_FLOAT) {
			ARM_CPYS (code, ins->dreg, ARM_VFP_F0);
		} else {
			ARM_FMSR (code, ins->dreg, ARMREG_R0);
			ARM_CPYS (code, ins->dreg, ins->dreg);
		}
		break;
	}
	default:
		break;
	}

	return code;
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	int max_len, cpos;
	int imm8, rot_amount;

	/* we don't align basic blocks of loops on arm */

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

    if (mono_break_at_bb_method && mono_method_desc_full_match (mono_break_at_bb_method, cfg->method) && bb->block_num == mono_break_at_bb_bb_num) {
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
							 GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break));
		code = emit_call_seq (cfg, code);
	}

	MONO_BB_FOR_EACH_INS (bb, ins) {
		guint offset = code - cfg->native_code;
		set_code_cursor (cfg, code);
		max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);
	//	if (ins->cil_code)
	//		g_print ("cil code\n");
		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_MEMORY_BARRIER:
			if (v7_supported) {
				ARM_DMB (code, ARM_DMB_ISH);
			} else if (v6_supported) {
				ARM_MOV_REG_IMM8 (code, ARMREG_R0, 0);
				ARM_MCR (code, 15, 0, ARMREG_R0, 7, 10, 5);
			}
			break;
		case OP_TLS_GET:
			code = emit_tls_get (code, ins->dreg, ins->inst_offset);
			break;
		case OP_TLS_SET:
			code = emit_tls_set (code, ins->sreg1, ins->inst_offset);
			break;
		case OP_ATOMIC_EXCHANGE_I4:
		case OP_ATOMIC_CAS_I4:
		case OP_ATOMIC_ADD_I4: {
			int tmpreg;
			guint8 *buf [16];

			g_assert (v7_supported);

			/* Free up a reg */
			if (ins->sreg1 != ARMREG_IP && ins->sreg2 != ARMREG_IP && ins->sreg3 != ARMREG_IP)
				tmpreg = ARMREG_IP;
			else if (ins->sreg1 != ARMREG_R0 && ins->sreg2 != ARMREG_R0 && ins->sreg3 != ARMREG_R0)
				tmpreg = ARMREG_R0;
			else if (ins->sreg1 != ARMREG_R1 && ins->sreg2 != ARMREG_R1 && ins->sreg3 != ARMREG_R1)
				tmpreg = ARMREG_R1;
			else
				tmpreg = ARMREG_R2;
			g_assert (cfg->arch.atomic_tmp_offset != -1);
			ARM_STR_IMM (code, tmpreg, cfg->frame_reg, cfg->arch.atomic_tmp_offset);

			switch (ins->opcode) {
			case OP_ATOMIC_EXCHANGE_I4:
				buf [0] = code;
				ARM_DMB (code, ARM_DMB_ISH);
				ARM_LDREX_REG (code, ARMREG_LR, ins->sreg1);
				ARM_STREX_REG (code, tmpreg, ins->sreg2, ins->sreg1);
				ARM_CMP_REG_IMM (code, tmpreg, 0, 0);
				buf [1] = code;
				ARM_B_COND (code, ARMCOND_NE, 0);
				arm_patch (buf [1], buf [0]);
				break;
			case OP_ATOMIC_CAS_I4:
				ARM_DMB (code, ARM_DMB_ISH);
				buf [0] = code;
				ARM_LDREX_REG (code, ARMREG_LR, ins->sreg1);
				ARM_CMP_REG_REG (code, ARMREG_LR, ins->sreg3);
				buf [1] = code;
				ARM_B_COND (code, ARMCOND_NE, 0);
				ARM_STREX_REG (code, tmpreg, ins->sreg2, ins->sreg1);
				ARM_CMP_REG_IMM (code, tmpreg, 0, 0);
				buf [2] = code;
				ARM_B_COND (code, ARMCOND_NE, 0);
				arm_patch (buf [2], buf [0]);
				arm_patch (buf [1], code);
				break;
			case OP_ATOMIC_ADD_I4:
				buf [0] = code;
				ARM_DMB (code, ARM_DMB_ISH);
				ARM_LDREX_REG (code, ARMREG_LR, ins->sreg1);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->sreg2);
				ARM_STREX_REG (code, tmpreg, ARMREG_LR, ins->sreg1);
				ARM_CMP_REG_IMM (code, tmpreg, 0, 0);
				buf [1] = code;
				ARM_B_COND (code, ARMCOND_NE, 0);
				arm_patch (buf [1], buf [0]);
				break;
			default:
				g_assert_not_reached ();
			}

			ARM_DMB (code, ARM_DMB_ISH);
			if (tmpreg != ins->dreg)
				ARM_LDR_IMM (code, tmpreg, cfg->frame_reg, cfg->arch.atomic_tmp_offset);
			ARM_MOV_REG_REG (code, ins->dreg, ARMREG_LR);
			break;
		}
		case OP_ATOMIC_LOAD_I1:
		case OP_ATOMIC_LOAD_U1:
		case OP_ATOMIC_LOAD_I2:
		case OP_ATOMIC_LOAD_U2:
		case OP_ATOMIC_LOAD_I4:
		case OP_ATOMIC_LOAD_U4:
		case OP_ATOMIC_LOAD_R4:
		case OP_ATOMIC_LOAD_R8: {
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				ARM_DMB (code, ARM_DMB_ISH);

			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);

			switch (ins->opcode) {
			case OP_ATOMIC_LOAD_I1:
				ARM_LDRSB_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
				break;
			case OP_ATOMIC_LOAD_U1:
				ARM_LDRB_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
				break;
			case OP_ATOMIC_LOAD_I2:
				ARM_LDRSH_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
				break;
			case OP_ATOMIC_LOAD_U2:
				ARM_LDRH_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
				break;
			case OP_ATOMIC_LOAD_I4:
			case OP_ATOMIC_LOAD_U4:
				ARM_LDR_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
				break;
			case OP_ATOMIC_LOAD_R4:
				if (cfg->r4fp) {
					ARM_ADD_REG_REG (code, ARMREG_LR, ins->inst_basereg, ARMREG_LR);
					ARM_FLDS (code, ins->dreg, ARMREG_LR, 0);
				} else {
					code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
					ARM_ADD_REG_REG (code, ARMREG_LR, ins->inst_basereg, ARMREG_LR);
					ARM_FLDS (code, vfp_scratch1, ARMREG_LR, 0);
					ARM_CVTS (code, ins->dreg, vfp_scratch1);
					code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
				}
				break;
			case OP_ATOMIC_LOAD_R8:
				ARM_ADD_REG_REG (code, ARMREG_LR, ins->inst_basereg, ARMREG_LR);
				ARM_FLDD (code, ins->dreg, ARMREG_LR, 0);
				break;
			}

			if (ins->backend.memory_barrier_kind != MONO_MEMORY_BARRIER_NONE)
				ARM_DMB (code, ARM_DMB_ISH);
			break;
		}
		case OP_ATOMIC_STORE_I1:
		case OP_ATOMIC_STORE_U1:
		case OP_ATOMIC_STORE_I2:
		case OP_ATOMIC_STORE_U2:
		case OP_ATOMIC_STORE_I4:
		case OP_ATOMIC_STORE_U4:
		case OP_ATOMIC_STORE_R4:
		case OP_ATOMIC_STORE_R8: {
			if (ins->backend.memory_barrier_kind != MONO_MEMORY_BARRIER_NONE)
				ARM_DMB (code, ARM_DMB_ISH);

			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);

			switch (ins->opcode) {
			case OP_ATOMIC_STORE_I1:
			case OP_ATOMIC_STORE_U1:
				ARM_STRB_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ARMREG_LR);
				break;
			case OP_ATOMIC_STORE_I2:
			case OP_ATOMIC_STORE_U2:
				ARM_STRH_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ARMREG_LR);
				break;
			case OP_ATOMIC_STORE_I4:
			case OP_ATOMIC_STORE_U4:
				ARM_STR_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ARMREG_LR);
				break;
			case OP_ATOMIC_STORE_R4:
				if (cfg->r4fp) {
					ARM_ADD_REG_REG (code, ARMREG_LR, ins->inst_destbasereg, ARMREG_LR);
					ARM_FSTS (code, ins->sreg1, ARMREG_LR, 0);
				} else {
					code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
					ARM_ADD_REG_REG (code, ARMREG_LR, ins->inst_destbasereg, ARMREG_LR);
					ARM_CVTD (code, vfp_scratch1, ins->sreg1);
					ARM_FSTS (code, vfp_scratch1, ARMREG_LR, 0);
					code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
				}
				break;
			case OP_ATOMIC_STORE_R8:
				ARM_ADD_REG_REG (code, ARMREG_LR, ins->inst_destbasereg, ARMREG_LR);
				ARM_FSTD (code, ins->sreg1, ARMREG_LR, 0);
				break;
			}

			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				ARM_DMB (code, ARM_DMB_ISH);
			break;
		}
		case OP_BIGMUL:
			ARM_SMULL_REG_REG (code, ins->backend.reg3, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_BIGMUL_UN:
			ARM_UMULL_REG_REG (code, ins->backend.reg3, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_imm & 0xFF);
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_STRB_IMM (code, ARMREG_LR, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_imm & 0xFFFF);
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_STRH_IMM (code, ARMREG_LR, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_imm);
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_STR_IMM (code, ARMREG_LR, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STOREI1_MEMBASE_REG:
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_STRB_IMM (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STOREI2_MEMBASE_REG:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_STRH_IMM (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			/* this case is special, since it happens for spill code after lowering has been called */
			if (arm_is_imm12 (ins->inst_offset)) {
				ARM_STR_IMM (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_STR_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ARMREG_LR);
			}
			break;
		case OP_STOREI1_MEMINDEX:
			ARM_STRB_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STOREI2_MEMINDEX:
			ARM_STRH_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STORE_MEMINDEX:
		case OP_STOREI4_MEMINDEX:
			ARM_STR_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			break;
		case OP_LOAD_MEMINDEX:
		case OP_LOADI4_MEMINDEX:
		case OP_LOADU4_MEMINDEX:
			ARM_LDR_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI1_MEMINDEX:
			ARM_LDRSB_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADU1_MEMINDEX:
			ARM_LDRB_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI2_MEMINDEX:
			ARM_LDRSH_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADU2_MEMINDEX:
			ARM_LDRH_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			/* this case is special, since it happens for spill code after lowering has been called */
			if (arm_is_imm12 (ins->inst_offset)) {
				ARM_LDR_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_LDR_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
			}
			break;
		case OP_LOADI1_MEMBASE:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_LDRSB_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADU1_MEMBASE:
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_LDRB_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADU2_MEMBASE:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_LDRH_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADI2_MEMBASE:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_LDRSH_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_ICONV_TO_I1:
			ARM_SHL_IMM (code, ins->dreg, ins->sreg1, 24);
			ARM_SAR_IMM (code, ins->dreg, ins->dreg, 24);
			break;
		case OP_ICONV_TO_I2:
			ARM_SHL_IMM (code, ins->dreg, ins->sreg1, 16);
			ARM_SAR_IMM (code, ins->dreg, ins->dreg, 16);
			break;
		case OP_ICONV_TO_U1:
			ARM_AND_REG_IMM8 (code, ins->dreg, ins->sreg1, 0xff);
			break;
		case OP_ICONV_TO_U2:
			ARM_SHL_IMM (code, ins->dreg, ins->sreg1, 16);
			ARM_SHR_IMM (code, ins->dreg, ins->dreg, 16);
			break;
		case OP_COMPARE:
		case OP_ICOMPARE:
			ARM_CMP_REG_REG (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_CMP_REG_IMM (code, ins->sreg1, imm8, rot_amount);
			break;
		case OP_BREAK:
			/*
			 * gdb does not like encountering the hw breakpoint ins in the debugged code. 
			 * So instead of emitting a trap, we emit a call a C function and place a 
			 * breakpoint there.
			 */
			//*(int*)code = 0xef9f0001;
			//code += 4;
			//ARM_DBRK (code);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
								 GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break));
			code = emit_call_seq (cfg, code);
			break;
		case OP_RELAXED_NOP:
			ARM_NOP (code);
			break;
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_ICONST:
		case OP_DUMMY_R8CONST:
		case OP_DUMMY_R4CONST:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_IL_SEQ_POINT:
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);
			break;
		case OP_SEQ_POINT: {
			int i;
			MonoInst *info_var = cfg->arch.seq_point_info_var;
			MonoInst *ss_trigger_page_var = cfg->arch.ss_trigger_page_var;
			MonoInst *ss_method_var = cfg->arch.seq_point_ss_method_var;
			MonoInst *bp_method_var = cfg->arch.seq_point_bp_method_var;
			MonoInst *var;
			int dreg = ARMREG_LR;

#if 0
			if (cfg->soft_breakpoints) {
				g_assert (!cfg->compile_aot);
			}
#endif

			/*
			 * For AOT, we use one got slot per method, which will point to a
			 * SeqPointInfo structure, containing all the information required
			 * by the code below.
			 */
			if (cfg->compile_aot) {
				g_assert (info_var);
				g_assert (info_var->opcode == OP_REGOFFSET);
			}

			if (!cfg->soft_breakpoints && !cfg->compile_aot) {
				/*
				 * Read from the single stepping trigger page. This will cause a
				 * SIGSEGV when single stepping is enabled.
				 * We do this _before_ the breakpoint, so single stepping after
				 * a breakpoint is hit will step to the next IL offset.
				 */
				g_assert (((guint64)(gsize)ss_trigger_page >> 32) == 0);
			}

			/* Single step check */
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				if (cfg->soft_breakpoints) {
					/* Load the address of the sequence point method variable. */
					var = ss_method_var;
					g_assert (var);
					g_assert (var->opcode == OP_REGOFFSET);
					code = emit_ldr_imm (code, dreg, var->inst_basereg, var->inst_offset);
					/* Read the value and check whether it is non-zero. */
					ARM_LDR_IMM (code, dreg, dreg, 0);
					ARM_CMP_REG_IMM (code, dreg, 0, 0);
					/* Call it conditionally. */
					ARM_BLX_REG_COND (code, ARMCOND_NE, dreg);
				} else {
					if (cfg->compile_aot) {
						/* Load the trigger page addr from the variable initialized in the prolog */
						var = ss_trigger_page_var;
						g_assert (var);
						g_assert (var->opcode == OP_REGOFFSET);
						code = emit_ldr_imm (code, dreg, var->inst_basereg, var->inst_offset);
					} else {
						ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
						ARM_B (code, 0);
						*(int*)code = (int)(gsize)ss_trigger_page;
						code += 4;
					}
					ARM_LDR_IMM (code, dreg, dreg, 0);
				}
			}

			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			/* Breakpoint check */
			if (cfg->compile_aot) {
				const guint32 offset = code - cfg->native_code;
				guint32 val;

				var = info_var;
				code = emit_ldr_imm (code, dreg, var->inst_basereg, var->inst_offset);
				/* Add the offset */
				val = ((offset / 4) * sizeof (guint8*)) + MONO_STRUCT_OFFSET (SeqPointInfo, bp_addrs);
				/* Load the info->bp_addrs [offset], which is either 0 or the address of a trigger page */
				if (arm_is_imm12 ((int)val)) {
					ARM_LDR_IMM (code, dreg, dreg, val);
				} else {
					ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF), 0);
					if (val & 0xFF00)
						ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF00) >> 8, 24);
					if (val & 0xFF0000)
						ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF0000) >> 16, 16);
					g_assert (!(val & 0xFF000000));

					ARM_LDR_IMM (code, dreg, dreg, 0);
				}
				/* What is faster, a branch or a load ? */
				ARM_CMP_REG_IMM (code, dreg, 0, 0);
				/* The breakpoint instruction */
				if (cfg->soft_breakpoints)
					ARM_BLX_REG_COND (code, ARMCOND_NE, dreg);
				else
					ARM_LDR_IMM_COND (code, dreg, dreg, 0, ARMCOND_NE);
			} else if (cfg->soft_breakpoints) {
				/* Load the address of the breakpoint method into ip. */
				var = bp_method_var;
				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				g_assert (arm_is_imm12 (var->inst_offset));
				ARM_LDR_IMM (code, dreg, var->inst_basereg, var->inst_offset);

				/*
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				ARM_NOP (code);
			} else {
				/* 
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				for (i = 0; i < 4; ++i)
					ARM_NOP (code);
			}

			/*
			 * Add an additional nop so skipping the bp doesn't cause the ip to point
			 * to another IL offset.
			 */

			ARM_NOP (code);
			break;
		}
		case OP_ADDCC:
		case OP_IADDCC:
			ARM_ADDS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IADD:
			ARM_ADD_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
		case OP_IADC:
			ARM_ADCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADDCC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ADDS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ADD_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ADC_IMM:
		case OP_IADC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ADCS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IADD_OVF:
			ARM_ADD_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_IADD_OVF_UN:
			ARM_ADD_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ISUB_OVF:
			ARM_SUB_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ISUB_OVF_UN:
			ARM_SUB_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_CARRY:
			ARM_ADCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_UN_CARRY:
			ARM_ADCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_CARRY:
			ARM_SBCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_UN_CARRY:
			ARM_SBCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUBCC:
		case OP_ISUBCC:
			ARM_SUBS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBCC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_SUBS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ISUB:
			ARM_SUB_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SBB:
		case OP_ISBB:
			ARM_SBCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_SUB_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_SBB_IMM:
		case OP_ISBB_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_SBCS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ARM_RSBS_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_RSBS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ARM_RSC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_RSC_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IAND:
			ARM_AND_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_IAND_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_AND_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IDIV:
			g_assert (v7s_supported || v7k_supported);
			ARM_SDIV (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IDIV_UN:
			g_assert (v7s_supported || v7k_supported);
			ARM_UDIV (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IREM:
			g_assert (v7s_supported || v7k_supported);
			ARM_SDIV (code, ARMREG_LR, ins->sreg1, ins->sreg2);
			ARM_MLS (code, ins->dreg, ARMREG_LR, ins->sreg2, ins->sreg1);
			break;
		case OP_IREM_UN:
			g_assert (v7s_supported || v7k_supported);
			ARM_UDIV (code, ARMREG_LR, ins->sreg1, ins->sreg2);
			ARM_MLS (code, ins->dreg, ARMREG_LR, ins->sreg2, ins->sreg1);
			break;
		case OP_DIV_IMM:
		case OP_REM_IMM:
			g_assert_not_reached ();
		case OP_IOR:
			ARM_ORR_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_IOR_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ORR_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IXOR:
			ARM_EOR_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_EOR_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ISHL:
			ARM_SHL_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
			if (ins->inst_imm)
				ARM_SHL_IMM (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_ISHR:
			ARM_SAR_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHR_IMM:
		case OP_ISHR_IMM:
			if (ins->inst_imm)
				ARM_SAR_IMM (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_SHR_UN_IMM:
		case OP_ISHR_UN_IMM:
			if (ins->inst_imm)
				ARM_SHR_IMM (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_ISHR_UN:
			ARM_SHR_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_INOT:
			ARM_MVN_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_INEG:
			ARM_RSB_REG_IMM8 (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_IMUL:
			if (ins->dreg == ins->sreg2)
				ARM_MUL_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				ARM_MUL_REG_REG (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_MUL_IMM:
			g_assert_not_reached ();
			break;
		case OP_IMUL_OVF:
			/* FIXME: handle ovf/ sreg2 != dreg */
			ARM_MUL_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			/* FIXME: MUL doesn't set the C/O flags on ARM */
			break;
		case OP_IMUL_OVF_UN:
			/* FIXME: handle ovf/ sreg2 != dreg */
			ARM_MUL_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			/* FIXME: MUL doesn't set the C/O flags on ARM */
			break;
		case OP_ICONST:
			code = mono_arm_emit_load_imm (code, ins->dreg, ins->inst_c0);
			break;
		case OP_AOTCONST:
			/* Load the GOT offset */
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0);
			ARM_LDR_IMM (code, ins->dreg, ARMREG_PC, 0);
			ARM_B (code, 0);
			*(gpointer*)code = NULL;
			code += 4;
			/* Load the value from the GOT */
			ARM_LDR_REG_REG (code, ins->dreg, ARMREG_PC, ins->dreg);
			break;
		case OP_OBJC_GET_SELECTOR:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_OBJC_SELECTOR_REF, ins->inst_p0);
			ARM_LDR_IMM (code, ins->dreg, ARMREG_PC, 0);
			ARM_B (code, 0);
			*(gpointer*)code = NULL;
			code += 4;
			ARM_LDR_REG_REG (code, ins->dreg, ARMREG_PC, ins->dreg);
			break;
		case OP_ICONV_TO_I4:
		case OP_ICONV_TO_U4:
		case OP_MOVE:
			if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_SETLRET: {
			int saved = ins->sreg2;
			if (ins->sreg2 == ARM_LSW_REG) {
				ARM_MOV_REG_REG (code, ARMREG_LR, ins->sreg2);
				saved = ARMREG_LR;
			}
			if (ins->sreg1 != ARM_LSW_REG)
				ARM_MOV_REG_REG (code, ARM_LSW_REG, ins->sreg1);
			if (saved != ARM_MSW_REG)
				ARM_MOV_REG_REG (code, ARM_MSW_REG, saved);
			break;
		}
		case OP_FMOVE:
			if (IS_VFP && ins->dreg != ins->sreg1)
				ARM_CPYD (code, ins->dreg, ins->sreg1);
			break;
		case OP_RMOVE:
			if (IS_VFP && ins->dreg != ins->sreg1)
				ARM_CPYS (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_F_TO_I4:
			if (cfg->r4fp) {
				ARM_FMRS (code, ins->dreg, ins->sreg1);
			} else {
				code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
				ARM_CVTD (code, vfp_scratch1, ins->sreg1);
				ARM_FMRS (code, ins->dreg, vfp_scratch1);
				code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
			}
			break;
		case OP_MOVE_I4_TO_F:
			if (cfg->r4fp) {
				ARM_FMSR (code, ins->dreg, ins->sreg1);
			} else {
				ARM_FMSR (code, ins->dreg, ins->sreg1);
				ARM_CVTS (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_FCONV_TO_R4:
			if (IS_VFP) {
				if (cfg->r4fp) {
					ARM_CVTD (code, ins->dreg, ins->sreg1);
				} else {
					ARM_CVTD (code, ins->dreg, ins->sreg1);
					ARM_CVTS (code, ins->dreg, ins->dreg);
				}
			}
			break;

		case OP_TAILCALL_PARAMETER:
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;

		case OP_TAILCALL:
		case OP_TAILCALL_MEMBASE:
		case OP_TAILCALL_REG: {
			gboolean const tailcall_membase = ins->opcode == OP_TAILCALL_MEMBASE;
			gboolean const tailcall_reg = ins->opcode == OP_TAILCALL_REG;
			MonoCallInst *call = (MonoCallInst*)ins;

			max_len += call->stack_usage / sizeof (target_mgreg_t) * ins_get_size (OP_TAILCALL_PARAMETER);

			if (IS_HARD_FLOAT)
				code = emit_float_args (cfg, call, code, &max_len, &offset);

			code = realloc_code (cfg, max_len);

			// For reg and membase, get destination in IP.

			if (tailcall_reg) {
				g_assert (ins->sreg1 > -1);
				if (ins->sreg1 != ARMREG_IP)
					ARM_MOV_REG_REG (code, ARMREG_IP, ins->sreg1);
			} else if (tailcall_membase) {
				g_assert (ins->sreg1 > -1);
				if (!arm_is_imm12 (ins->inst_offset)) {
					g_assert (ins->sreg1 != ARMREG_IP); // temp in emit_big_add
					code = emit_big_add (code, ARMREG_IP, ins->sreg1, ins->inst_offset);
					ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
				} else {
					ARM_LDR_IMM (code, ARMREG_IP, ins->sreg1, ins->inst_offset);
				}
			}

			/*
			 * The stack looks like the following:
			 * <caller argument area>
			 * <saved regs etc>
			 * <rest of frame>
			 * <callee argument area>
			 * <optionally saved IP> (about to be)
			 * Need to copy the arguments from the callee argument area to
			 * the caller argument area, and pop the frame.
			 */
			if (call->stack_usage) {
				int i, prev_sp_offset = 0;

				// When we get here, the parameters to the tailcall are already formed,
				// in registers and at the bottom of the grow-down stack.
				//
				// Our goal is generally preserve parameters, and trim the stack,
				// and, before trimming stack, move parameters from the bottom of the
				// frame to the bottom of the trimmed frame.

				// For the case of large frames, and presently therefore always,
				// IP is used as an adjusted frame_reg.
				// Be conservative and save IP around the movement
				// of parameters from the bottom of frame to top of the frame.
				const gboolean save_ip = tailcall_membase || tailcall_reg;
				if (save_ip)
					ARM_PUSH (code, 1 << ARMREG_IP);

				// When moving stacked parameters from the bottom
				// of the frame (sp) to the top of the frame (ip),
				// account, 0 or 4, for the conditional save of IP.
				const int offset_sp = save_ip ? 4 : 0;
				const int offset_ip = (save_ip && (cfg->frame_reg == ARMREG_SP)) ? 4 : 0;

				/* Compute size of saved registers restored below */
				if (iphone_abi)
					prev_sp_offset = 2 * 4;
				else
					prev_sp_offset = 1 * 4;
				for (i = 0; i < 16; ++i) {
					if (cfg->used_int_regs & (1 << i))
						prev_sp_offset += 4;
				}

				// Point IP at the start of where the parameters will go after trimming stack.
				// After locals and saved registers.
				code = emit_big_add (code, ARMREG_IP, cfg->frame_reg, cfg->stack_usage + prev_sp_offset);

				/* Copy arguments on the stack to our argument area */
				// FIXME a fixed size memcpy is desirable here,
				// at least for larger values of stack_usage.
				//
				// FIXME For most functions, with frames < 4K, we can use frame_reg directly here instead of IP.
				// See https://github.com/mono/mono/pull/12079
				// See https://github.com/mono/mono/pull/12079/commits/93e7007a9567b78fa8152ce404b372b26e735516
				for (i = 0; i < call->stack_usage; i += sizeof (target_mgreg_t)) {
					ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, i + offset_sp);
					ARM_STR_IMM (code, ARMREG_LR, ARMREG_IP, i + offset_ip);
				}

				if (save_ip)
					ARM_POP (code, 1 << ARMREG_IP);
			}

			/*
			 * Keep in sync with mono_arch_emit_epilog
			 */
			g_assert (!cfg->method->save_lmf);
			code = emit_big_add_temp (code, ARMREG_SP, cfg->frame_reg, cfg->stack_usage, ARMREG_LR);
			if (iphone_abi) {
				if (cfg->used_int_regs)
					ARM_POP (code, cfg->used_int_regs);
				ARM_POP (code, (1 << ARMREG_R7) | (1 << ARMREG_LR));
			} else {
				ARM_POP (code, cfg->used_int_regs | (1 << ARMREG_LR));
			}

			if (tailcall_reg || tailcall_membase) {
				code = emit_jmp_reg (code, ARMREG_IP);
			} else {
				mono_add_patch_info (cfg, (guint8*) code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, call->method);

				if (cfg->compile_aot) {
					ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
					ARM_B (code, 0);
					*(gpointer*)code = NULL;
					code += 4;
					ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_IP);
				} else {
					code = mono_arm_patchable_b (code, ARMCOND_AL);
					cfg->thunk_area += THUNK_SIZE;
				}
			}
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			ARM_LDRB_IMM (code, ARMREG_LR, ins->sreg1, 0);
			break;
		case OP_ARGLIST: {
			g_assert (cfg->sig_cookie < 128);
			ARM_LDR_IMM (code, ARMREG_IP, cfg->frame_reg, cfg->sig_cookie);
			ARM_STR_IMM (code, ARMREG_IP, ins->sreg1, 0);
			break;
		}
		case OP_FCALL:
		case OP_RCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;

			if (IS_HARD_FLOAT)
				code = emit_float_args (cfg, call, code, &max_len, &offset);

			mono_call_add_patch_info (cfg, call, code - cfg->native_code);

			code = emit_call_seq (cfg, code);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_REG:
		case OP_RCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			if (IS_HARD_FLOAT)
				code = emit_float_args (cfg, (MonoCallInst *)ins, code, &max_len, &offset);

			code = emit_call_reg (code, ins->sreg1);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_RCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE: {
			g_assert (ins->sreg1 != ARMREG_LR);
			call = (MonoCallInst*)ins;

			if (IS_HARD_FLOAT)
				code = emit_float_args (cfg, call, code, &max_len, &offset);
			if (!arm_is_imm12 (ins->inst_offset)) {
				/* sreg1 might be IP */
				ARM_MOV_REG_REG (code, ARMREG_LR, ins->sreg1);
				code = mono_arm_emit_load_imm (code, ARMREG_IP, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, ARMREG_LR);
				ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
				ARM_LDR_IMM (code, ARMREG_PC, ARMREG_IP, 0);
			} else {
				ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
				ARM_LDR_IMM (code, ARMREG_PC, ins->sreg1, ins->inst_offset);
			}
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			code = emit_move_return_value (cfg, ins, code);
			break;
		}
		case OP_GENERIC_CLASS_INIT: {
			int byte_offset;
			guint8 *jump;

			byte_offset = MONO_STRUCT_OFFSET (MonoVTable, initialized);

			g_assert (arm_is_imm8 (byte_offset));
			ARM_LDRSB_IMM (code, ARMREG_IP, ins->sreg1, byte_offset);
			ARM_CMP_REG_IMM (code, ARMREG_IP, 0, 0);
			jump = code;
			ARM_B_COND (code, ARMCOND_NE, 0);

			/* Uninitialized case */
			g_assert (ins->sreg1 == ARMREG_R0);

			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
								 GUINT_TO_POINTER (MONO_JIT_ICALL_mono_generic_class_init));
			code = emit_call_seq (cfg, code);

			/* Initialized case */
			arm_patch (jump, code);
			break;
		}
		case OP_LOCALLOC: {
			/* round the size to 8 bytes */
			ARM_ADD_REG_IMM8 (code, ins->dreg, ins->sreg1, (MONO_ARCH_FRAME_ALIGNMENT - 1));
			ARM_BIC_REG_IMM8 (code, ins->dreg, ins->dreg, (MONO_ARCH_FRAME_ALIGNMENT - 1));
			ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ins->dreg);
			/* memzero the area: dreg holds the size, sp is the pointer */
			if (ins->flags & MONO_INST_INIT) {
				guint8 *start_loop, *branch_to_cond;
				ARM_MOV_REG_IMM8 (code, ARMREG_LR, 0);
				branch_to_cond = code;
				ARM_B (code, 0);
				start_loop = code;
				ARM_STR_REG_REG (code, ARMREG_LR, ARMREG_SP, ins->dreg);
				arm_patch (branch_to_cond, code);
				/* decrement by 4 and set flags */
				ARM_SUBS_REG_IMM8 (code, ins->dreg, ins->dreg, sizeof (target_mgreg_t));
				ARM_B_COND (code, ARMCOND_GE, 0);
				arm_patch (code - 4, start_loop);
			}
			ARM_MOV_REG_REG (code, ins->dreg, ARMREG_SP);
			if (cfg->param_area)
				code = emit_sub_imm (code, ARMREG_SP, ARMREG_SP, ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			break;
		}
		case OP_DYN_CALL: {
			int i;
			MonoInst *var = cfg->dyn_call_var;
			guint8 *labels [16];

			g_assert (var->opcode == OP_REGOFFSET);
			g_assert (arm_is_imm12 (var->inst_offset));

			/* lr = args buffer filled by mono_arch_get_dyn_call_args () */
			ARM_MOV_REG_REG (code, ARMREG_LR, ins->sreg1);
			/* ip = ftn */
			ARM_MOV_REG_REG (code, ARMREG_IP, ins->sreg2);

			/* Save args buffer */
			ARM_STR_IMM (code, ARMREG_LR, var->inst_basereg, var->inst_offset);

			/* Set fp argument registers */
			if (IS_HARD_FLOAT) {
				ARM_LDR_IMM (code, ARMREG_R0, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, has_fpregs));
				ARM_CMP_REG_IMM (code, ARMREG_R0, 0, 0);
				labels [0] = code;
				ARM_B_COND (code, ARMCOND_EQ, 0);
				for (i = 0; i < FP_PARAM_REGS; ++i) {
					const int offset = MONO_STRUCT_OFFSET (DynCallArgs, fpregs) + (i * sizeof (double));
					g_assert (arm_is_fpimm8 (offset));
					ARM_FLDD (code, i * 2, ARMREG_LR, offset);
				}
				arm_patch (labels [0], code);
			}

			/* Allocate callee area */
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, n_stackargs));
			ARM_SHL_IMM (code, ARMREG_R1, ARMREG_R1, 2);
			ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_R1);

			/* Set stack args */
			/* R1 = limit */
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, n_stackargs));
			/* R2 = pointer into regs */
			code = emit_big_add (code, ARMREG_R2, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, regs) + (PARAM_REGS * sizeof (target_mgreg_t)));
			/* R3 = pointer to stack */
			ARM_MOV_REG_REG (code, ARMREG_R3, ARMREG_SP);
			/* Loop */
			labels [0] = code;
			ARM_B_COND (code, ARMCOND_AL, 0);
			labels [1] = code;
			ARM_LDR_IMM (code, ARMREG_R0, ARMREG_R2, 0);
			ARM_STR_IMM (code, ARMREG_R0, ARMREG_R3, 0);
			ARM_ADD_REG_IMM (code, ARMREG_R2, ARMREG_R2, sizeof (target_mgreg_t), 0);
			ARM_ADD_REG_IMM (code, ARMREG_R3, ARMREG_R3, sizeof (target_mgreg_t), 0);
			ARM_SUB_REG_IMM (code, ARMREG_R1, ARMREG_R1, 1, 0);
			arm_patch (labels [0], code);
			ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
			labels [2] = code;
			ARM_B_COND (code, ARMCOND_GT, 0);
			arm_patch (labels [2], labels [1]);

			/* Set argument registers */
			for (i = 0; i < PARAM_REGS; ++i)
				ARM_LDR_IMM (code, i, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, regs) + (i * sizeof (target_mgreg_t)));

			/* Make the call */
			ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
			ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

			/* Save result */
			ARM_LDR_IMM (code, ARMREG_IP, var->inst_basereg, var->inst_offset);
			ARM_STR_IMM (code, ARMREG_R0, ARMREG_IP, MONO_STRUCT_OFFSET (DynCallArgs, res)); 
			ARM_STR_IMM (code, ARMREG_R1, ARMREG_IP, MONO_STRUCT_OFFSET (DynCallArgs, res2));
			if (IS_HARD_FLOAT)
				ARM_FSTD (code, ARM_VFP_D0, ARMREG_IP, MONO_STRUCT_OFFSET (DynCallArgs, fpregs));
			break;
		}
		case OP_THROW: {
			if (ins->sreg1 != ARMREG_R0)
				ARM_MOV_REG_REG (code, ARMREG_R0, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			code = emit_call_seq (cfg, code);
			break;
		}
		case OP_RETHROW: {
			if (ins->sreg1 != ARMREG_R0)
				ARM_MOV_REG_REG (code, ARMREG_R0, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID,
					     GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			code = emit_call_seq (cfg, code);
			break;
		}
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			int param_area = ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT);
			int i, rot_amount;

			/* Reserve a param area, see filter-stack.exe */
			if (param_area) {
				if ((i = mono_arm_is_rotated_imm8 (param_area, &rot_amount)) >= 0) {
					ARM_SUB_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, param_area);
					ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
				}
			}

			if (arm_is_imm12 (spvar->inst_offset)) {
				ARM_STR_IMM (code, ARMREG_LR, spvar->inst_basereg, spvar->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_IP, spvar->inst_offset);
				ARM_STR_REG_REG (code, ARMREG_LR, spvar->inst_basereg, ARMREG_IP);
			}
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			int param_area = ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT);
			int i, rot_amount;

			/* Free the param area */
			if (param_area) {
				if ((i = mono_arm_is_rotated_imm8 (param_area, &rot_amount)) >= 0) {
					ARM_ADD_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, param_area);
					ARM_ADD_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
				}
			}

			if (ins->sreg1 != ARMREG_R0)
				ARM_MOV_REG_REG (code, ARMREG_R0, ins->sreg1);
			if (arm_is_imm12 (spvar->inst_offset)) {
				ARM_LDR_IMM (code, ARMREG_IP, spvar->inst_basereg, spvar->inst_offset);
			} else {
				g_assert (ARMREG_IP != spvar->inst_basereg);
				code = mono_arm_emit_load_imm (code, ARMREG_IP, spvar->inst_offset);
				ARM_LDR_REG_REG (code, ARMREG_IP, spvar->inst_basereg, ARMREG_IP);
			}
			ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			int param_area = ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT);
			int i, rot_amount;

			/* Free the param area */
			if (param_area) {
				if ((i = mono_arm_is_rotated_imm8 (param_area, &rot_amount)) >= 0) {
					ARM_ADD_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, param_area);
					ARM_ADD_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
				}
			}

			if (arm_is_imm12 (spvar->inst_offset)) {
				ARM_LDR_IMM (code, ARMREG_IP, spvar->inst_basereg, spvar->inst_offset);
			} else {
				g_assert (ARMREG_IP != spvar->inst_basereg);
				code = mono_arm_emit_load_imm (code, ARMREG_IP, spvar->inst_offset);
				ARM_LDR_REG_REG (code, ARMREG_IP, spvar->inst_basereg, ARMREG_IP);
			}
			ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
			break;
		}
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			code = mono_arm_patchable_bl (code, ARMCOND_AL);
			cfg->thunk_area += THUNK_SIZE;
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
			break;
		case OP_GET_EX_OBJ:
			if (ins->dreg != ARMREG_R0)
				ARM_MOV_REG_REG (code, ins->dreg, ARMREG_R0);
			break;

		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case OP_BR:
			/*if (ins->inst_target_bb->native_offset) {
				ARM_B (code, 0);
				//x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
			} else*/ {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
				code = mono_arm_patchable_b (code, ARMCOND_AL);
			} 
			break;
		case OP_BR_REG:
			ARM_MOV_REG_REG (code, ARMREG_PC, ins->sreg1);
			break;
		case OP_SWITCH:
			/* 
			 * In the normal case we have:
			 * 	ldr pc, [pc, ins->sreg1 << 2]
			 * 	nop
			 * If aot, we have:
			 * 	ldr lr, [pc, ins->sreg1 << 2]
			 * 	add pc, pc, lr
			 * After follows the data.
			 * FIXME: add aot support.
			 */
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_SWITCH, ins->inst_p0);
			max_len += 4 * GPOINTER_TO_INT (ins->klass);
			code = realloc_code (cfg, max_len);
			ARM_LDR_REG_REG_SHIFT (code, ARMREG_PC, ARMREG_PC, ins->sreg1, ARMSHIFT_LSL, 2);
			ARM_NOP (code);
			code += 4 * GPOINTER_TO_INT (ins->klass);
			break;
		case OP_CEQ:
		case OP_ICEQ:
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_EQ);
			break;
		case OP_CLT:
		case OP_ICLT:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_LT);
			break;
		case OP_CLT_UN:
		case OP_ICLT_UN:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_LO);
			break;
		case OP_CGT:
		case OP_ICGT:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_GT);
			break;
		case OP_CGT_UN:
		case OP_ICGT_UN:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_HI);
			break;
		case OP_ICNEQ:
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_EQ);
			break;
		case OP_ICGE:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_LT);
			break;
		case OP_ICLE:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_GT);
			break;
		case OP_ICGE_UN:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_LO);
			break;
		case OP_ICLE_UN:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_HI);
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (ins->opcode - OP_COND_EXC_EQ, ins->inst_p1);
			break;
		case OP_COND_EXC_IEQ:
		case OP_COND_EXC_INE_UN:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_ILT_UN:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_IGT_UN:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_IGE_UN:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_ILE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (ins->opcode - OP_COND_EXC_IEQ, ins->inst_p1);
			break;
		case OP_COND_EXC_C:
		case OP_COND_EXC_IC:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_CS, ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
		case OP_COND_EXC_IOV:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_VS, ins->inst_p1);
			break;
		case OP_COND_EXC_NC:
		case OP_COND_EXC_INC:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_CC, ins->inst_p1);
			break;
		case OP_COND_EXC_NO:
		case OP_COND_EXC_INO:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_VC, ins->inst_p1);
			break;
		case OP_IBEQ:
		case OP_IBNE_UN:
		case OP_IBLT:
		case OP_IBLT_UN:
		case OP_IBGT:
		case OP_IBGT_UN:
		case OP_IBGE:
		case OP_IBGE_UN:
		case OP_IBLE:
		case OP_IBLE_UN:
			EMIT_COND_BRANCH (ins, ins->opcode - OP_IBEQ);
			break;

		/* floating point opcodes */
		case OP_R8CONST:
			if (cfg->compile_aot) {
				ARM_FLDD (code, ins->dreg, ARMREG_PC, 0);
				ARM_B (code, 1);
				*(guint32*)code = ((guint32*)(ins->inst_p0))[0];
				code += 4;
				*(guint32*)code = ((guint32*)(ins->inst_p0))[1];
				code += 4;
			} else {
				/* FIXME: we can optimize the imm load by dealing with part of 
				 * the displacement in LDFD (aligning to 512).
				 */
				code = mono_arm_emit_load_imm (code, ARMREG_LR, (guint32)(gsize)ins->inst_p0);
				ARM_FLDD (code, ins->dreg, ARMREG_LR, 0);
			}
			break;
		case OP_R4CONST:
			if (cfg->compile_aot) {
				ARM_FLDS (code, ins->dreg, ARMREG_PC, 0);
				ARM_B (code, 0);
				*(guint32*)code = ((guint32*)(ins->inst_p0))[0];
				code += 4;
				if (!cfg->r4fp)
					ARM_CVTS (code, ins->dreg, ins->dreg);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, (guint32)(gsize)ins->inst_p0);
				ARM_FLDS (code, ins->dreg, ARMREG_LR, 0);
				if (!cfg->r4fp)
					ARM_CVTS (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_STORER8_MEMBASE_REG:
			/* This is generated by the local regalloc pass which runs after the lowering pass */
			if (!arm_is_fpimm8 (ins->inst_offset)) {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->inst_destbasereg);
				ARM_FSTD (code, ins->sreg1, ARMREG_LR, 0);
			} else {
				ARM_FSTD (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			}
			break;
		case OP_LOADR8_MEMBASE:
			/* This is generated by the local regalloc pass which runs after the lowering pass */
			if (!arm_is_fpimm8 (ins->inst_offset)) {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->inst_basereg);
				ARM_FLDD (code, ins->dreg, ARMREG_LR, 0);
			} else {
				ARM_FLDD (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			g_assert (arm_is_fpimm8 (ins->inst_offset));
			if (cfg->r4fp) {
				ARM_FSTS (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			} else {
				code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
				ARM_CVTD (code, vfp_scratch1, ins->sreg1);
				ARM_FSTS (code, vfp_scratch1, ins->inst_destbasereg, ins->inst_offset);
				code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
			}
			break;
		case OP_LOADR4_MEMBASE:
			if (cfg->r4fp) {
				ARM_FLDS (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			} else {
				g_assert (arm_is_fpimm8 (ins->inst_offset));
				code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
				ARM_FLDS (code, vfp_scratch1, ins->inst_basereg, ins->inst_offset);
				ARM_CVTS (code, ins->dreg, vfp_scratch1);
				code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
			}
			break;
		case OP_ICONV_TO_R_UN: {
			g_assert_not_reached ();
			break;
		}
		case OP_ICONV_TO_R4:
			if (cfg->r4fp) {
				ARM_FMSR (code, ins->dreg, ins->sreg1);
				ARM_FSITOS (code, ins->dreg, ins->dreg);
			} else {
				code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
				ARM_FMSR (code, vfp_scratch1, ins->sreg1);
				ARM_FSITOS (code, vfp_scratch1, vfp_scratch1);
				ARM_CVTS (code, ins->dreg, vfp_scratch1);
				code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
			}
			break;
		case OP_ICONV_TO_R8:
			code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
			ARM_FMSR (code, vfp_scratch1, ins->sreg1);
			ARM_FSITOD (code, ins->dreg, vfp_scratch1);
			code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
			break;

		case OP_SETFRET: {
			MonoType *sig_ret = mini_get_underlying_type (mono_method_signature_internal (cfg->method)->ret);
			if (sig_ret->type == MONO_TYPE_R4) {
				if (cfg->r4fp) {
					if (IS_HARD_FLOAT) {
						if (ins->sreg1 != ARM_VFP_D0)
							ARM_CPYS (code, ARM_VFP_D0, ins->sreg1);
					} else {
						ARM_FMRS (code, ARMREG_R0, ins->sreg1);
					}
				} else {
					ARM_CVTD (code, ARM_VFP_F0, ins->sreg1);

					if (!IS_HARD_FLOAT)
						ARM_FMRS (code, ARMREG_R0, ARM_VFP_F0);
				}
			} else {
				if (IS_HARD_FLOAT)
					ARM_CPYD (code, ARM_VFP_D0, ins->sreg1);
				else
					ARM_FMRRD (code, ARMREG_R0, ARMREG_R1, ins->sreg1);
			}
			break;
		}
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			break;
		case OP_FCONV_TO_I8:
		case OP_FCONV_TO_U8:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_R_UN:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_OVF_I4_2: {
			guint8 *high_bit_not_set, *valid_negative, *invalid_negative, *valid_positive;
			/* 
			 * Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000
			 */

			ARM_CMP_REG_IMM8 (code, ins->sreg1, 0);
			high_bit_not_set = code;
			ARM_B_COND (code, ARMCOND_GE, 0); /*branch if bit 31 of the lower part is not set*/

			ARM_CMN_REG_IMM8 (code, ins->sreg2, 1); /*This have the same effect as CMP reg, 0xFFFFFFFF */
			valid_negative = code;
			ARM_B_COND (code, ARMCOND_EQ, 0); /*branch if upper part == 0xFFFFFFFF (lower part has bit 31 set) */
			invalid_negative = code;
			ARM_B_COND (code, ARMCOND_AL, 0);
			
			arm_patch (high_bit_not_set, code);

			ARM_CMP_REG_IMM8 (code, ins->sreg2, 0);
			valid_positive = code;
			ARM_B_COND (code, ARMCOND_EQ, 0); /*branch if upper part == 0 (lower part has bit 31 clear)*/

			arm_patch (invalid_negative, code);
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_AL, "OverflowException");

			arm_patch (valid_negative, code);
			arm_patch (valid_positive, code);

			if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_FADD:
			ARM_VFP_ADDD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			ARM_VFP_SUBD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FMUL:
			ARM_VFP_MULD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FDIV:
			ARM_VFP_DIVD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FNEG:
			ARM_NEGD (code, ins->dreg, ins->sreg1);
			break;
		case OP_FREM:
			/* emulated */
			g_assert_not_reached ();
			break;
		case OP_FCOMPARE:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			break;
		case OP_RCOMPARE:
			g_assert (IS_VFP);
			ARM_CMPS (code, ins->sreg1, ins->sreg2);
			ARM_FMSTAT (code);
			break;
		case OP_FCEQ:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_EQ);
			break;
		case OP_FCLT:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			break;
		case OP_FCLT_UN:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_VS);
			break;
		case OP_FCGT:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			break;
		case OP_FCGT_UN:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_VS);
			break;
		case OP_FCNEQ:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_EQ);
			break;
		case OP_FCGE:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_MI);
			break;
		case OP_FCLE:
			if (IS_VFP) {
				ARM_CMPD (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_MI);
			break;

		/* ARM FPA flags table:
		 * N        Less than               ARMCOND_MI
		 * Z        Equal                   ARMCOND_EQ
		 * C        Greater Than or Equal   ARMCOND_CS
		 * V        Unordered               ARMCOND_VS
		 */
		case OP_FBEQ:
			EMIT_COND_BRANCH (ins, OP_IBEQ - OP_IBEQ);
			break;
		case OP_FBNE_UN:
			EMIT_COND_BRANCH (ins, OP_IBNE_UN - OP_IBEQ);
			break;
		case OP_FBLT:
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_MI); /* N set */
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_VS); /* V set */
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_MI); /* N set */
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
		case OP_FBLE:
		case OP_FBLE_UN:
			g_assert_not_reached ();
			break;
		case OP_FBGE:
			if (IS_VFP) {
				EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_GE);
			} else {
				/* FPA requires EQ even thou the docs suggests that just CS is enough */
				EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_EQ);
				EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_CS);
			}
			break;
		case OP_FBGE_UN:
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_VS); /* V set */
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_GE);
			break;

		case OP_CKFINITE: {
			if (IS_VFP) {
				code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch1);
				code = mono_arm_emit_vfp_scratch_save (cfg, code, vfp_scratch2);

				ARM_ABSD (code, vfp_scratch2, ins->sreg1);
				ARM_FLDD (code, vfp_scratch1, ARMREG_PC, 0);
				ARM_B (code, 1);
				*(guint32*)code = 0xffffffff;
				code += 4;
				*(guint32*)code = 0x7fefffff;
				code += 4;
				ARM_CMPD (code, vfp_scratch2, vfp_scratch1);
				ARM_FMSTAT (code);
				EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_GT, "OverflowException");
				ARM_CMPD (code, ins->sreg1, ins->sreg1);
				ARM_FMSTAT (code);
				EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_VS, "OverflowException");
				ARM_CPYD (code, ins->dreg, ins->sreg1);

				code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch1);
				code = mono_arm_emit_vfp_scratch_restore (cfg, code, vfp_scratch2);
			}
			break;
		}

		case OP_RCONV_TO_I1:
			code = emit_r4_to_int (cfg, code, ins->dreg, ins->sreg1, 1, TRUE);
			break;
		case OP_RCONV_TO_U1:
			code = emit_r4_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			break;
		case OP_RCONV_TO_I2:
			code = emit_r4_to_int (cfg, code, ins->dreg, ins->sreg1, 2, TRUE);
			break;
		case OP_RCONV_TO_U2:
			code = emit_r4_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			break;
		case OP_RCONV_TO_I4:
			code = emit_r4_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_RCONV_TO_U4:
			code = emit_r4_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			break;
		case OP_RCONV_TO_R4:
			g_assert (IS_VFP);
			if (ins->dreg != ins->sreg1)
				ARM_CPYS (code, ins->dreg, ins->sreg1);
			break;
		case OP_RCONV_TO_R8:
			g_assert (IS_VFP);
			ARM_CVTS (code, ins->dreg, ins->sreg1);
			break;
		case OP_RADD:
			ARM_VFP_ADDS (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_RSUB:
			ARM_VFP_SUBS (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_RMUL:
			ARM_VFP_MULS (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_RDIV:
			ARM_VFP_DIVS (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_RNEG:
			ARM_NEGS (code, ins->dreg, ins->sreg1);
			break;
		case OP_RCEQ:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_EQ);
			break;
		case OP_RCLT:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			break;
		case OP_RCLT_UN:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_VS);
			break;
		case OP_RCGT:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			break;
		case OP_RCGT_UN:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_VS);
			break;
		case OP_RCNEQ:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_EQ);
			break;
		case OP_RCGE:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_MI);
			break;
		case OP_RCLE:
			if (IS_VFP) {
				ARM_CMPS (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 1);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_MI);
			break;

		case OP_GC_LIVENESS_DEF:
		case OP_GC_LIVENESS_USE:
		case OP_GC_PARAM_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		case OP_GC_SPILL_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			bb->spill_slot_defs = g_slist_prepend_mempool (cfg->mempool, bb->spill_slot_defs, ins);
			break;
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = code - cfg->native_code;
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = code - cfg->native_code;
			break;
		}
		case OP_GC_SAFE_POINT: {
			guint8 *buf [1];

			ARM_LDR_IMM (code, ARMREG_IP, ins->sreg1, 0);
			ARM_CMP_REG_IMM (code, ARMREG_IP, 0, 0);
			buf [0] = code;
			ARM_B_COND (code, ARMCOND_EQ, 0);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll));
			code = emit_call_seq (cfg, code);
			arm_patch (buf [0], code);
			break;
		}
		case OP_FILL_PROF_CALL_CTX:
			for (int i = 0; i < ARMREG_MAX; i++)
				if ((MONO_ARCH_CALLEE_SAVED_REGS & (1 << i)) || i == ARMREG_SP || i == ARMREG_FP)
					ARM_STR_IMM (code, i, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, regs) + i * sizeof (target_mgreg_t));
			break;
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
	}

	set_code_cursor (cfg, code);
}

#endif /* DISABLE_JIT */

void
mono_arch_register_lowlevel_calls (void)
{
	/* The signature doesn't matter */
	mono_register_jit_icall (mono_arm_throw_exception, mono_icall_sig_void, TRUE);
	mono_register_jit_icall (mono_arm_throw_exception_by_token, mono_icall_sig_void, TRUE);
	mono_register_jit_icall (mono_arm_unaligned_stack, mono_icall_sig_void, TRUE);
}

#define patch_lis_ori(ip,val) do {\
		guint16 *__lis_ori = (guint16*)(ip);	\
		__lis_ori [1] = (((guint32)(gsize)(val)) >> 16) & 0xffff;	\
		__lis_ori [3] = ((guint32)(gsize)(val)) & 0xffff;	\
	} while (0)

void
mono_arch_patch_code_new (MonoCompile *cfg, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	unsigned char *ip = ji->ip.i + code;

	if (ji->type == MONO_PATCH_INFO_SWITCH) {
	}

	switch (ji->type) {
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *jt = (gpointer*)(ip + 8);
		int i;
		/* jt is the inlined jump table, 2 instructions after ip
		 * In the normal case we store the absolute addresses,
		 * otherwise the displacements.
		 */
		for (i = 0; i < ji->data.table->table_size; i++)
			jt [i] = code + (int)(gsize)ji->data.table->table [i];
		break;
	}
	case MONO_PATCH_INFO_IP:
		g_assert_not_reached ();
		patch_lis_ori (ip, ip);
		break;
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_SFLDA:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
	case MONO_PATCH_INFO_LDTOKEN:
		g_assert_not_reached ();
		/* from OP_AOTCONST : lis + ori */
		patch_lis_ori (ip, target);
		break;
	case MONO_PATCH_INFO_R4:
	case MONO_PATCH_INFO_R8:
		g_assert_not_reached ();
		*((gconstpointer *)(ip + 2)) = target;
		break;
	case MONO_PATCH_INFO_EXC_NAME:
		g_assert_not_reached ();
		*((gconstpointer *)(ip + 1)) = target;
		break;
	case MONO_PATCH_INFO_NONE:
	case MONO_PATCH_INFO_BB_OVF:
	case MONO_PATCH_INFO_EXC_OVF:
		/* everything is dealt with at epilog output time */
		break;
	default:
		arm_patch_general (cfg, domain, ip, (const guchar*)target);
		break;
	}
}

void
mono_arm_unaligned_stack (MonoMethod *method)
{
	g_assert_not_reached ();
}

#ifndef DISABLE_JIT

/*
 * Stack frame layout:
 * 
 *   ------------------- fp
 *   	MonoLMF structure or saved registers
 *   -------------------
 *   	locals
 *   -------------------
 *   	spilled regs
 *   -------------------
 *   	param area             size is cfg->param_area
 *   ------------------- sp
 */
guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int alloc_size, orig_alloc_size, pos, max_offset, i, rot_amount, part;
	guint8 *code;
	CallInfo *cinfo;
	int lmf_offset = 0;
	int prev_sp_offset, reg_offset;

	sig = mono_method_signature_internal (method);
	cfg->code_size = 256 + sig->param_count * 64;
	code = cfg->native_code = g_malloc (cfg->code_size);

	mono_emit_unwind_op_def_cfa (cfg, code, ARMREG_SP, 0);

	alloc_size = cfg->stack_offset;
	pos = 0;
	prev_sp_offset = 0;

	if (iphone_abi) {
		/* 
		 * The iphone uses R7 as the frame pointer, and it points at the saved
		 * r7+lr:
		 *         <lr>
		 * r7 ->   <r7>
		 *         <rest of frame>
		 * We can't use r7 as a frame pointer since it points into the middle of
		 * the frame, so we keep using our own frame pointer.
		 * FIXME: Optimize this.
		 */
		ARM_PUSH (code, (1 << ARMREG_R7) | (1 << ARMREG_LR));
		prev_sp_offset += 8; /* r7 and lr */
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset);
		mono_emit_unwind_op_offset (cfg, code, ARMREG_R7, (- prev_sp_offset) + 0);
		ARM_MOV_REG_REG (code, ARMREG_R7, ARMREG_SP);
	}

	if (!method->save_lmf) {
		if (iphone_abi) {
			/* No need to push LR again */
			if (cfg->used_int_regs)
				ARM_PUSH (code, cfg->used_int_regs);
		} else {
			ARM_PUSH (code, cfg->used_int_regs | (1 << ARMREG_LR));
			prev_sp_offset += 4;
		}
		for (i = 0; i < 16; ++i) {
			if (cfg->used_int_regs & (1 << i))
				prev_sp_offset += 4;
		}
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset);
		reg_offset = 0;
		for (i = 0; i < 16; ++i) {
			if ((cfg->used_int_regs & (1 << i))) {
				mono_emit_unwind_op_offset (cfg, code, i, (- prev_sp_offset) + reg_offset);
				mini_gc_set_slot_type_from_cfa (cfg, (- prev_sp_offset) + reg_offset, SLOT_NOREF);
				reg_offset += 4;
			}
		}
		mono_emit_unwind_op_offset (cfg, code, ARMREG_LR, -4);
		mini_gc_set_slot_type_from_cfa (cfg, -4, SLOT_NOREF);
	} else {
		ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
		ARM_PUSH (code, 0x5ff0);
		prev_sp_offset += 4 * 10; /* all but r0-r3, sp and pc */
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset);
		reg_offset = 0;
		for (i = 0; i < 16; ++i) {
			if ((i > ARMREG_R3) && (i != ARMREG_SP) && (i != ARMREG_PC)) {
				/* The original r7 is saved at the start */
				if (!(iphone_abi && i == ARMREG_R7))
					mono_emit_unwind_op_offset (cfg, code, i, (- prev_sp_offset) + reg_offset);
				reg_offset += 4;
			}
		}
		g_assert (reg_offset == 4 * 10);
		pos += MONO_ABI_SIZEOF (MonoLMF) - (4 * 10);
		lmf_offset = pos;
	}
	alloc_size += pos;
	orig_alloc_size = alloc_size;
	// align to MONO_ARCH_FRAME_ALIGNMENT bytes
	if (alloc_size & (MONO_ARCH_FRAME_ALIGNMENT - 1)) {
		alloc_size += MONO_ARCH_FRAME_ALIGNMENT - 1;
		alloc_size &= ~(MONO_ARCH_FRAME_ALIGNMENT - 1);
	}

	/* the stack used in the pushed regs */
	alloc_size += ALIGN_TO (prev_sp_offset, MONO_ARCH_FRAME_ALIGNMENT) - prev_sp_offset;
	cfg->stack_usage = alloc_size;
	if (alloc_size) {
		if ((i = mono_arm_is_rotated_imm8 (alloc_size, &rot_amount)) >= 0) {
			ARM_SUB_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_IP, alloc_size);
			ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
		}
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset + alloc_size);
	}
	if (cfg->frame_reg != ARMREG_SP) {
		ARM_MOV_REG_REG (code, cfg->frame_reg, ARMREG_SP);
		mono_emit_unwind_op_def_cfa_reg (cfg, code, cfg->frame_reg);
	}
	//g_print ("prev_sp_offset: %d, alloc_size:%d\n", prev_sp_offset, alloc_size);
	prev_sp_offset += alloc_size;

	for (i = 0; i < alloc_size - orig_alloc_size; i += 4)
		mini_gc_set_slot_type_from_cfa (cfg, (- prev_sp_offset) + orig_alloc_size + i, SLOT_NOREF);

        /* compute max_offset in order to use short forward jumps
	 * we could skip do it on arm because the immediate displacement
	 * for jumps is large enough, it may be useful later for constant pools
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins = bb->code;
		bb->max_offset = max_offset;

		MONO_BB_FOR_EACH_INS (bb, ins)
			max_offset += ins_get_size (ins->opcode);
	}

	/* stack alignment check */
	/*
	{
		guint8 *buf [16];
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_SP);
		code = mono_arm_emit_load_imm (code, ARMREG_IP, MONO_ARCH_FRAME_ALIGNMENT -1);
		ARM_AND_REG_REG (code, ARMREG_LR, ARMREG_LR, ARMREG_IP);
		ARM_CMP_REG_IMM (code, ARMREG_LR, 0, 0);
		buf [0] = code;
		ARM_B_COND (code, ARMCOND_EQ, 0);
		if (cfg->compile_aot)
			ARM_MOV_REG_IMM8 (code, ARMREG_R0, 0);
		else
			code = mono_arm_emit_load_imm (code, ARMREG_R0, (guint32)cfg->method);
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arm_unaligned_stack));
		code = emit_call_seq (cfg, code);
		arm_patch (buf [0], code);
	}
	*/

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		MonoInst *ins = cfg->rgctx_var;

		g_assert (ins->opcode == OP_REGOFFSET);

		if (arm_is_imm12 (ins->inst_offset)) {
			ARM_STR_IMM (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ins->inst_offset);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
			ARM_STR_REG_REG (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ARMREG_LR);
		}
	}

	/* load arguments allocated to register from the stack */
	pos = 0;

	cinfo = get_call_info (NULL, sig);

	if (cinfo->ret.storage == RegTypeStructByAddr) {
		ArgInfo *ainfo = &cinfo->ret;
		inst = cfg->vret_addr;
		g_assert (arm_is_imm12 (inst->inst_offset));
		ARM_STR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		ArgInfo *cookie = &cinfo->sig_cookie;

		/* Save the sig cookie address */
		g_assert (cookie->storage == RegTypeBase);

		g_assert (arm_is_imm12 (prev_sp_offset + cookie->offset));
		g_assert (arm_is_imm12 (cfg->sig_cookie));
		ARM_ADD_REG_IMM8 (code, ARMREG_IP, cfg->frame_reg, prev_sp_offset + cookie->offset);
		ARM_STR_IMM (code, ARMREG_IP, cfg->frame_reg, cfg->sig_cookie);
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];
		
		if (cfg->verbose_level > 2)
			g_print ("Saving argument %d (type: %d)\n", i, ainfo->storage);

		if (inst->opcode == OP_REGVAR) {
			if (ainfo->storage == RegTypeGeneral)
				ARM_MOV_REG_REG (code, inst->dreg, ainfo->reg);
			else if (ainfo->storage == RegTypeFP) {
				g_assert_not_reached ();
			} else if (ainfo->storage == RegTypeBase) {
				if (arm_is_imm12 (prev_sp_offset + ainfo->offset)) {
					ARM_LDR_IMM (code, inst->dreg, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, prev_sp_offset + ainfo->offset);
					ARM_LDR_REG_REG (code, inst->dreg, ARMREG_SP, ARMREG_IP);
				}
			} else
				g_assert_not_reached ();

			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		} else {
			switch (ainfo->storage) {
			case RegTypeHFA:
				for (part = 0; part < ainfo->nregs; part ++) {
					if (ainfo->esize == 4)
						ARM_FSTS (code, ainfo->reg + part, inst->inst_basereg, inst->inst_offset + (part * ainfo->esize));
					else
						ARM_FSTD (code, ainfo->reg + (part * 2), inst->inst_basereg, inst->inst_offset + (part * ainfo->esize));
				}
				break;
			case RegTypeGeneral:
			case RegTypeIRegPair:
			case RegTypeGSharedVtInReg:
			case RegTypeStructByAddr:
				switch (ainfo->size) {
				case 1:
					if (arm_is_imm12 (inst->inst_offset))
						ARM_STRB_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRB_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 2:
					if (arm_is_imm8 (inst->inst_offset)) {
						ARM_STRH_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRH_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 8:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					if (arm_is_imm12 (inst->inst_offset + 4)) {
						ARM_STR_IMM (code, ainfo->reg + 1, inst->inst_basereg, inst->inst_offset + 4);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset + 4);
						ARM_STR_REG_REG (code, ainfo->reg + 1, inst->inst_basereg, ARMREG_IP);
					}
					break;
				default:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				}
				break;
			case RegTypeBaseGen:
				if (arm_is_imm12 (prev_sp_offset + ainfo->offset)) {
					ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, prev_sp_offset + ainfo->offset);
					ARM_LDR_REG_REG (code, ARMREG_LR, ARMREG_SP, ARMREG_IP);
				}
				if (arm_is_imm12 (inst->inst_offset + 4)) {
					ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset + 4);
					ARM_STR_IMM (code, ARMREG_R3, inst->inst_basereg, inst->inst_offset);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset + 4);
					ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
					ARM_STR_REG_REG (code, ARMREG_R3, inst->inst_basereg, ARMREG_IP);
				}
				break;
			case RegTypeBase:
			case RegTypeGSharedVtOnStack:
			case RegTypeStructByAddrOnStack:
				if (arm_is_imm12 (prev_sp_offset + ainfo->offset)) {
					ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, prev_sp_offset + ainfo->offset);
					ARM_LDR_REG_REG (code, ARMREG_LR, ARMREG_SP, ARMREG_IP);
				}

				switch (ainfo->size) {
				case 1:
					if (arm_is_imm8 (inst->inst_offset)) {
						ARM_STRB_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRB_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 2:
					if (arm_is_imm8 (inst->inst_offset)) {
						ARM_STRH_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRH_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 8:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					if (arm_is_imm12 (prev_sp_offset + ainfo->offset + 4)) {
						ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, (prev_sp_offset + ainfo->offset + 4));
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, prev_sp_offset + ainfo->offset + 4);
						ARM_LDR_REG_REG (code, ARMREG_LR, ARMREG_SP, ARMREG_IP);
					}
					if (arm_is_imm12 (inst->inst_offset + 4)) {
						ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset + 4);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset + 4);
						ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				default:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				}
				break;
			case RegTypeFP: {
				int imm8, rot_amount;

				if ((imm8 = mono_arm_is_rotated_imm8 (inst->inst_offset, &rot_amount)) == -1) {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
					ARM_ADD_REG_REG (code, ARMREG_IP, ARMREG_IP, inst->inst_basereg);
				} else
					ARM_ADD_REG_IMM (code, ARMREG_IP, inst->inst_basereg, imm8, rot_amount);

				if (ainfo->size == 8)
					ARM_FSTD (code, ainfo->reg, ARMREG_IP, 0);
				else
					ARM_FSTS (code, ainfo->reg, ARMREG_IP, 0);
				break;
			}
			case RegTypeStructByVal: {
				int doffset = inst->inst_offset;
				int soffset = 0;
				int cur_reg;
				int size = 0;
				size = mini_type_stack_size_full (inst->inst_vtype, NULL, sig->pinvoke);
				for (cur_reg = 0; cur_reg < ainfo->size; ++cur_reg) {
					if (arm_is_imm12 (doffset)) {
						ARM_STR_IMM (code, ainfo->reg + cur_reg, inst->inst_basereg, doffset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, doffset);
						ARM_STR_REG_REG (code, ainfo->reg + cur_reg, inst->inst_basereg, ARMREG_IP);
					}
					soffset += sizeof (target_mgreg_t);
					doffset += sizeof (target_mgreg_t);
				}
				if (ainfo->vtsize) {
					/* FIXME: handle overrun! with struct sizes not multiple of 4 */
					//g_print ("emit_memcpy (prev_sp_ofs: %d, ainfo->offset: %d, soffset: %d)\n", prev_sp_offset, ainfo->offset, soffset);
					code = emit_memcpy (code, ainfo->vtsize * sizeof (target_mgreg_t), inst->inst_basereg, doffset, ARMREG_SP, prev_sp_offset + ainfo->offset);
				}
				break;
			}
			default:
				g_assert_not_reached ();
				break;
			}
		}
		pos++;
	}

	if (method->save_lmf)
		code = emit_save_lmf (cfg, code, alloc_size - lmf_offset);

	if (cfg->arch.seq_point_info_var) {
		MonoInst *ins = cfg->arch.seq_point_info_var;

		/* Initialize the variable from a GOT slot */
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_SEQ_POINT_INFO, cfg->method);
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_R0, ARMREG_PC, ARMREG_R0);

		g_assert (ins->opcode == OP_REGOFFSET);

		if (arm_is_imm12 (ins->inst_offset)) {
			ARM_STR_IMM (code, ARMREG_R0, ins->inst_basereg, ins->inst_offset);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
			ARM_STR_REG_REG (code, ARMREG_R0, ins->inst_basereg, ARMREG_LR);
		}
	}

	/* Initialize ss_trigger_page_var */
	if (!cfg->soft_breakpoints) {
		MonoInst *info_var = cfg->arch.seq_point_info_var;
		MonoInst *ss_trigger_page_var = cfg->arch.ss_trigger_page_var;
		int dreg = ARMREG_LR;

		if (info_var) {
			g_assert (info_var->opcode == OP_REGOFFSET);

			code = emit_ldr_imm (code, dreg, info_var->inst_basereg, info_var->inst_offset);
			/* Load the trigger page addr */
			ARM_LDR_IMM (code, dreg, dreg, MONO_STRUCT_OFFSET (SeqPointInfo, ss_trigger_page));
			ARM_STR_IMM (code, dreg, ss_trigger_page_var->inst_basereg, ss_trigger_page_var->inst_offset);
		}
	}

	if (cfg->arch.seq_point_ss_method_var) {
		MonoInst *ss_method_ins = cfg->arch.seq_point_ss_method_var;
		MonoInst *bp_method_ins = cfg->arch.seq_point_bp_method_var;

		g_assert (ss_method_ins->opcode == OP_REGOFFSET);
		g_assert (arm_is_imm12 (ss_method_ins->inst_offset));

		if (cfg->compile_aot) {
			MonoInst *info_var = cfg->arch.seq_point_info_var;
			int dreg = ARMREG_LR;

			g_assert (info_var->opcode == OP_REGOFFSET);
			g_assert (arm_is_imm12 (info_var->inst_offset));

			ARM_LDR_IMM (code, dreg, info_var->inst_basereg, info_var->inst_offset);
			ARM_LDR_IMM (code, dreg, dreg, MONO_STRUCT_OFFSET (SeqPointInfo, ss_tramp_addr));
			ARM_STR_IMM (code, dreg, ss_method_ins->inst_basereg, ss_method_ins->inst_offset);
		} else {
			g_assert (bp_method_ins->opcode == OP_REGOFFSET);
			g_assert (arm_is_imm12 (bp_method_ins->inst_offset));

			ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
			ARM_B (code, 1);
			*(gpointer*)code = &single_step_tramp;
			code += 4;
			*(gpointer*)code = breakpoint_tramp;
			code += 4;

			ARM_LDR_IMM (code, ARMREG_IP, ARMREG_LR, 0);
			ARM_STR_IMM (code, ARMREG_IP, ss_method_ins->inst_basereg, ss_method_ins->inst_offset);
			ARM_LDR_IMM (code, ARMREG_IP, ARMREG_LR, 4);
			ARM_STR_IMM (code, ARMREG_IP, bp_method_ins->inst_basereg, bp_method_ins->inst_offset);
		}
	}

	set_code_cursor (cfg, code);
	g_free (cinfo);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int pos, i, rot_amount;
	int max_epilog_size = 16 + 20*4;
	guint8 *code;
	CallInfo *cinfo;

	if (cfg->method->save_lmf)
		max_epilog_size += 128;

	code = realloc_code (cfg, max_epilog_size);

	/* Save the uwind state which is needed by the out-of-line code */
	mono_emit_unwind_op_remember_state (cfg, code);

	pos = 0;

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	switch (cinfo->ret.storage) {
	case RegTypeStructByVal: {
		MonoInst *ins = cfg->ret;

		if (cinfo->ret.nregs == 1) {
			if (arm_is_imm12 (ins->inst_offset)) {
				ARM_LDR_IMM (code, ARMREG_R0, ins->inst_basereg, ins->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_LDR_REG_REG (code, ARMREG_R0, ins->inst_basereg, ARMREG_LR);
			}
		} else {
			for (i = 0; i < cinfo->ret.nregs; ++i) {
				int offset = ins->inst_offset + (i * 4);
				if (arm_is_imm12 (offset)) {
					ARM_LDR_IMM (code, i, ins->inst_basereg, offset);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_LR, offset);
					ARM_LDR_REG_REG (code, i, ins->inst_basereg, ARMREG_LR);
				}
			}
		}
		break;
	}
	case RegTypeHFA: {
		MonoInst *ins = cfg->ret;

		for (i = 0; i < cinfo->ret.nregs; ++i) {
			if (cinfo->ret.esize == 4)
				ARM_FLDS (code, cinfo->ret.reg + i, ins->inst_basereg, ins->inst_offset + (i * cinfo->ret.esize));
			else
				ARM_FLDD (code, cinfo->ret.reg + (i * 2), ins->inst_basereg, ins->inst_offset + (i * cinfo->ret.esize));
		}
		break;
	}
	default:
		break;
	}

	if (method->save_lmf) {
		int lmf_offset, reg, sp_adj, regmask, nused_int_regs = 0;
		/* all but r0-r3, sp and pc */
		pos += MONO_ABI_SIZEOF (MonoLMF) - (MONO_ARM_NUM_SAVED_REGS * sizeof (target_mgreg_t));
		lmf_offset = pos;

		code = emit_restore_lmf (cfg, code, cfg->stack_usage - lmf_offset);

		/* This points to r4 inside MonoLMF->iregs */
		sp_adj = (MONO_ABI_SIZEOF (MonoLMF) - MONO_ARM_NUM_SAVED_REGS * sizeof (target_mgreg_t));
		reg = ARMREG_R4;
		regmask = 0x9ff0; /* restore lr to pc */
		/* Skip caller saved registers not used by the method */
		while (!(cfg->used_int_regs & (1 << reg)) && reg < ARMREG_FP) {
			regmask &= ~(1 << reg);
			sp_adj += 4;
			reg ++;
		}
		if (iphone_abi)
			/* Restored later */
			regmask &= ~(1 << ARMREG_PC);
		/* point sp at the registers to restore: 10 is 14 -4, because we skip r0-r3 */
		code = emit_big_add (code, ARMREG_SP, cfg->frame_reg, cfg->stack_usage - lmf_offset + sp_adj);
		for (i = 0; i < 16; i++) {
			if (regmask & (1 << i))
				nused_int_regs ++;
		}
		mono_emit_unwind_op_def_cfa (cfg, code, ARMREG_SP, ((iphone_abi ? 3 : 0) + nused_int_regs) * 4);
		/* restore iregs */
		ARM_POP (code, regmask); 
		if (iphone_abi) {
			for (i = 0; i < 16; i++) {
				if (regmask & (1 << i))
					mono_emit_unwind_op_same_value (cfg, code, i);
			}
			/* Restore saved r7, restore LR to PC */
			/* Skip lr from the lmf */
			mono_emit_unwind_op_def_cfa_offset (cfg, code, 3 * 4);
			ARM_ADD_REG_IMM (code, ARMREG_SP, ARMREG_SP, sizeof (target_mgreg_t), 0);
			mono_emit_unwind_op_def_cfa_offset (cfg, code, 2 * 4);
			ARM_POP (code, (1 << ARMREG_R7) | (1 << ARMREG_PC));
		}
	} else {
		int i, nused_int_regs = 0;

		for (i = 0; i < 16; i++) {
			if (cfg->used_int_regs & (1 << i))
				nused_int_regs ++;
		}

		if ((i = mono_arm_is_rotated_imm8 (cfg->stack_usage, &rot_amount)) >= 0) {
			ARM_ADD_REG_IMM (code, ARMREG_SP, cfg->frame_reg, i, rot_amount);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_IP, cfg->stack_usage);
			ARM_ADD_REG_REG (code, ARMREG_SP, cfg->frame_reg, ARMREG_IP);
		}

		if (cfg->frame_reg != ARMREG_SP) {
			mono_emit_unwind_op_def_cfa_reg (cfg, code, ARMREG_SP);
		}

		if (iphone_abi) {
			/* Restore saved gregs */
			if (cfg->used_int_regs) {
				mono_emit_unwind_op_def_cfa_offset (cfg, code, (2 + nused_int_regs) * 4);
				ARM_POP (code, cfg->used_int_regs);
				for (i = 0; i < 16; i++) {
					if (cfg->used_int_regs & (1 << i))
						mono_emit_unwind_op_same_value (cfg, code, i);
				}
			}
			mono_emit_unwind_op_def_cfa_offset (cfg, code, 2 * 4);
			/* Restore saved r7, restore LR to PC */
			ARM_POP (code, (1 << ARMREG_R7) | (1 << ARMREG_PC));
		} else {
			mono_emit_unwind_op_def_cfa_offset (cfg, code, (nused_int_regs + 1) * 4);
			ARM_POP (code, cfg->used_int_regs | (1 << ARMREG_PC));
		}
	}

	/* Restore the unwind state to be the same as before the epilog */
	mono_emit_unwind_op_restore_state (cfg, code);

	set_code_cursor (cfg, code);

}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int i;
	guint8 *code;
	guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM];
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM];
	int max_epilog_size = 50;

	for (i = 0; i < MONO_EXC_INTRINS_NUM; i++) {
		exc_throw_pos [i] = NULL;
		exc_throw_found [i] = 0;
	}

	/* count the number of exception infos */
     
	/* 
	 * make sure we have enough space for exceptions
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC) {
			i = mini_exception_id_by_name ((const char*)patch_info->data.target);
			if (!exc_throw_found [i]) {
				max_epilog_size += 32;
				exc_throw_found [i] = TRUE;
			}
		}
	}

	code = realloc_code (cfg, max_epilog_size);

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;

			i = mini_exception_id_by_name ((const char*)patch_info->data.target);
			if (exc_throw_pos [i]) {
				arm_patch (ip, exc_throw_pos [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
				break;
			} else {
				exc_throw_pos [i] = code;
			}
			arm_patch (ip, code);

			exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", patch_info->data.name);

			ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR);
			ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
			patch_info->type = MONO_PATCH_INFO_JIT_ICALL_ID;
			patch_info->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
			patch_info->ip.i = code - cfg->native_code;
			ARM_BL (code, 0);
			cfg->thunk_area += THUNK_SIZE;
			*(guint32*)(gpointer)code = m_class_get_type_token (exc_class) - MONO_TOKEN_TYPE_DEF;
			code += 4;
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}

	set_code_cursor (cfg, code);
}

#endif /* #ifndef DISABLE_JIT */

void
mono_arch_finish_init (void)
{
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	/* FIXME: */
	return NULL;
}

#ifndef DISABLE_JIT

#endif

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	/* OP_AOTCONST */
	return 8;
}

void
mono_arch_flush_register_windows (void)
{
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	return (MonoMethod*)regs [MONO_ARCH_IMT_REG];
}

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*)(gsize)regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, (guint8*)NULL, (guint8*)NULL, ARMREG_SP, 0);

	return l;
}

/* #define ENABLE_WRONG_METHOD_CHECK 1 */
#define BASE_SIZE (6 * 4)
#define BSEARCH_ENTRY_SIZE (4 * 4)
#define CMP_SIZE (3 * 4)
#define BRANCH_SIZE (1 * 4)
#define CALL_SIZE (2 * 4)
#define WMC_SIZE (8 * 4)
#define DISTANCE(A, B) (((gint32)(gssize)(B)) - ((gint32)(gssize)(A)))

static arminstr_t *
arm_emit_value_and_patch_ldr (arminstr_t *code, arminstr_t *target, guint32 value)
{
	guint32 delta = DISTANCE (target, code);
	delta -= 8;
	g_assert (delta >= 0 && delta <= 0xFFF);
	*target = *target | delta;
	*code = value;
	return code + 1;
}

#ifdef ENABLE_WRONG_METHOD_CHECK
static void
mini_dump_bad_imt (int input_imt, int compared_imt, int pc)
{
	g_print ("BAD IMT comparing %x with expected %x at ip %x", input_imt, compared_imt, pc);
	g_assert (0);
}
#endif

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count,
	gpointer fail_tramp)
{
	int size, i;
	arminstr_t *code, *start;
	gboolean large_offsets = FALSE;
	guint32 **constant_pool_starts;
	arminstr_t *vtable_target = NULL;
	int extra_space = 0;
#ifdef ENABLE_WRONG_METHOD_CHECK
	char * cond;
#endif
	GSList *unwind_ops;

	size = BASE_SIZE;
	constant_pool_starts = g_new0 (guint32*, count);

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->has_target_code || !arm_is_imm12 (DISTANCE (vtable, &vtable->vtable[item->value.vtable_slot]))) {
				item->chunk_size += 32;
				large_offsets = TRUE;
			}

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case)
					item->chunk_size += CMP_SIZE;
				item->chunk_size += BRANCH_SIZE;
			} else {
#ifdef ENABLE_WRONG_METHOD_CHECK
				item->chunk_size += WMC_SIZE;
#endif
			}
			if (fail_case) {
				item->chunk_size += 16;
				large_offsets = TRUE;
			}
			item->chunk_size += CALL_SIZE;
		} else {
			item->chunk_size += BSEARCH_ENTRY_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}

	if (large_offsets)
		size += 4 * count; /* The ARM_ADD_REG_IMM to pop the stack */

	if (fail_tramp)
		code = mono_method_alloc_generic_virtual_trampoline (domain, size);
	else
		code = mono_domain_code_reserve (domain, size);
	start = code;

	unwind_ops = mono_arch_get_cie_program ();

#ifdef DEBUG_IMT
	g_print ("Building IMT trampoline for class %s %s entries %d code size %d code at %p end %p vtable %p fail_tramp %p\n", m_class_get_name_space (vtable->klass), m_class_get_name (vtable->klass), count, size, start, ((guint8*)start) + size, vtable, fail_tramp);
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		g_print ("method %d (%p) %s vtable slot %p is_equals %d chunk size %d\n", i, item->key, ((MonoMethod*)item->key)->name, &vtable->vtable [item->value.vtable_slot], item->is_equals, item->chunk_size);
	}
#endif

	if (large_offsets) {
		ARM_PUSH4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, 4 * sizeof (host_mgreg_t));
	} else {
		ARM_PUSH2 (code, ARMREG_R0, ARMREG_R1);
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, 2 * sizeof (host_mgreg_t));
	}
	ARM_LDR_IMM (code, ARMREG_R0, ARMREG_LR, -4);
	vtable_target = code;
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_V5);

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		arminstr_t *imt_method = NULL, *vtable_offset_ins = NULL, *target_code_ins = NULL;
		gint32 vtable_offset;

		item->code_target = (guint8*)code;

		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case) {
					imt_method = code;
					ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
					ARM_CMP_REG_REG (code, ARMREG_R0, ARMREG_R1);
				}
				item->jmp_code = (guint8*)code;
				ARM_B_COND (code, ARMCOND_NE, 0);
			} else {
				/*Enable the commented code to assert on wrong method*/
#ifdef ENABLE_WRONG_METHOD_CHECK
				imt_method = code;
				ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
				ARM_CMP_REG_REG (code, ARMREG_R0, ARMREG_R1);
				cond = code;
				ARM_B_COND (code, ARMCOND_EQ, 0);

/* Define this if your system is so bad that gdb is failing. */
#ifdef BROKEN_DEV_ENV
				ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_PC);
				ARM_BL (code, 0);
				arm_patch (code - 1, mini_dump_bad_imt);
#else
				ARM_DBRK (code);
#endif
				arm_patch (cond, code);
#endif
			}

			if (item->has_target_code) {
				/* Load target address */
				target_code_ins = code;
				ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
				/* Save it to the fourth slot */
				ARM_STR_IMM (code, ARMREG_R1, ARMREG_SP, 3 * sizeof (target_mgreg_t));
				/* Restore registers and branch */
				ARM_POP4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
				
				code = arm_emit_value_and_patch_ldr (code, target_code_ins, (gsize)item->value.target_code);
			} else {
				vtable_offset = DISTANCE (vtable, &vtable->vtable[item->value.vtable_slot]);
				if (!arm_is_imm12 (vtable_offset)) {
					/* 
					 * We need to branch to a computed address but we don't have
					 * a free register to store it, since IP must contain the 
					 * vtable address. So we push the two values to the stack, and
					 * load them both using LDM.
					 */
					/* Compute target address */
					vtable_offset_ins = code;
					ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
					ARM_LDR_REG_REG (code, ARMREG_R1, ARMREG_IP, ARMREG_R1);
					/* Save it to the fourth slot */
					ARM_STR_IMM (code, ARMREG_R1, ARMREG_SP, 3 * sizeof (target_mgreg_t));
					/* Restore registers and branch */
					ARM_POP4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
				
					code = arm_emit_value_and_patch_ldr (code, vtable_offset_ins, vtable_offset);
				} else {
					ARM_POP2 (code, ARMREG_R0, ARMREG_R1);
					if (large_offsets) {
						mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, 2 * sizeof (host_mgreg_t));
						ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 2 * sizeof (host_mgreg_t));
					}
					mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, 0);
					ARM_LDR_IMM (code, ARMREG_PC, ARMREG_IP, vtable_offset);
				}
			}

			if (fail_case) {
				arm_patch (item->jmp_code, (guchar*)code);

				target_code_ins = code;
				/* Load target address */
				ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
				/* Save it to the fourth slot */
				ARM_STR_IMM (code, ARMREG_R1, ARMREG_SP, 3 * sizeof (host_mgreg_t));
				/* Restore registers and branch */
				ARM_POP4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
				
				code = arm_emit_value_and_patch_ldr (code, target_code_ins, (gsize)fail_tramp);
				item->jmp_code = NULL;
			}

			if (imt_method)
				code = arm_emit_value_and_patch_ldr (code, imt_method, (guint32)(gsize)item->key);

			/*must emit after unconditional branch*/
			if (vtable_target) {
				code = arm_emit_value_and_patch_ldr (code, vtable_target, (guint32)(gsize)vtable);
				item->chunk_size += 4;
				vtable_target = NULL;
			}

			/*We reserve the space for bsearch IMT values after the first entry with an absolute jump*/
			constant_pool_starts [i] = code;
			if (extra_space) {
				code += extra_space;
				extra_space = 0;
			}
		} else {
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
			ARM_CMP_REG_REG (code, ARMREG_R0, ARMREG_R1);

			item->jmp_code = (guint8*)code;
			ARM_B_COND (code, ARMCOND_HS, 0);
			++extra_space;
		}
	}

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx)
				arm_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
		}
		if (i > 0 && item->is_equals) {
			int j;
			arminstr_t *space_start = constant_pool_starts [i];
			for (j = i - 1; j >= 0 && !imt_entries [j]->is_equals; --j) {
				space_start = arm_emit_value_and_patch_ldr (space_start, (arminstr_t*)imt_entries [j]->code_target, (guint32)(gsize)imt_entries [j]->key);
			}
		}
	}

#ifdef DEBUG_IMT
	{
		char *buff = g_strdup_printf ("thunk_for_class_%s_%s_entries_%d", m_class_get_name_space (vtable->klass), m_class_get_name (vtable->klass), count);
		mono_disassemble_code (NULL, (guint8*)start, size, buff);
		g_free (buff);
	}
#endif

	g_free (constant_pool_starts);

	mono_arch_flush_icache ((guint8*)start, size);
	MONO_PROFILER_RAISE (jit_code_buffer, ((guint8*)start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL));
	UnlockedAdd (&mono_stats.imt_trampolines_size, code - start);

	g_assert (DISTANCE (start, code) <= size);

	mono_tramp_info_register (mono_tramp_info_create (NULL, (guint8*)start, DISTANCE (start, code), NULL, unwind_ops), domain);

	return start;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ctx->regs [reg];
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, host_mgreg_t val)
{
	ctx->regs [reg] = val;
}

/*
 * mono_arch_get_trampolines:
 *
 *   Return a list of MonoTrampInfo structures describing arch specific trampolines
 * for AOT.
 */
GSList *
mono_arch_get_trampolines (gboolean aot)
{
	return mono_arm_get_exception_trampolines (aot);
}

#if defined(MONO_ARCH_SOFT_DEBUG_SUPPORTED)
/*
 * mono_arch_set_breakpoint:
 *
 *   Set a breakpoint at the native code corresponding to JI at NATIVE_OFFSET.
 * The location should contain code emitted by OP_SEQ_POINT.
 */
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	guint32 native_offset = ip - (guint8*)ji->code_start;
	MonoDebugOptions *opt = mini_get_debug_options ();

	if (ji->from_aot) {
		SeqPointInfo *info = mono_arch_get_seq_point_info (mono_domain_get (), (guint8*)ji->code_start);

		if (!breakpoint_tramp)
			breakpoint_tramp = mini_get_breakpoint_trampoline ();

		g_assert (native_offset % 4 == 0);
		g_assert (info->bp_addrs [native_offset / 4] == 0);
		info->bp_addrs [native_offset / 4] = (guint8*)(opt->soft_breakpoints ? breakpoint_tramp : bp_trigger_page);
	} else if (opt->soft_breakpoints) {
		code += 4;
		ARM_BLX_REG (code, ARMREG_LR);
		mono_arch_flush_icache (code - 4, 4);
	} else {
		int dreg = ARMREG_LR;

		/* Read from another trigger page */
		ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(int*)code = (int)(gssize)bp_trigger_page;
		code += 4;
		ARM_LDR_IMM (code, dreg, dreg, 0);

		mono_arch_flush_icache (code - 16, 16);

#if 0
		/* This is currently implemented by emitting an SWI instruction, which 
		 * qemu/linux seems to convert to a SIGILL.
		 */
		*(int*)code = (0xef << 24) | 8;
		code += 4;
		mono_arch_flush_icache (code - 4, 4);
#endif
	}
}

/*
 * mono_arch_clear_breakpoint:
 *
 *   Clear the breakpoint at IP.
 */
void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	MonoDebugOptions *opt = mini_get_debug_options ();
	guint8 *code = ip;
	int i;

	if (ji->from_aot) {
		guint32 native_offset = ip - (guint8*)ji->code_start;
		SeqPointInfo *info = mono_arch_get_seq_point_info (mono_domain_get (), (guint8*)ji->code_start);

		if (!breakpoint_tramp)
			breakpoint_tramp = mini_get_breakpoint_trampoline ();

		g_assert (native_offset % 4 == 0);
		g_assert (info->bp_addrs [native_offset / 4] == (guint8*)(opt->soft_breakpoints ? breakpoint_tramp : bp_trigger_page));
		info->bp_addrs [native_offset / 4] = 0;
	} else if (opt->soft_breakpoints) {
		code += 4;
		ARM_NOP (code);
		mono_arch_flush_icache (code - 4, 4);
	} else {
		for (i = 0; i < 4; ++i)
			ARM_NOP (code);

		mono_arch_flush_icache (ip, code - ip);
	}
}
	
/*
 * mono_arch_start_single_stepping:
 *
 *   Start single stepping.
 */
void
mono_arch_start_single_stepping (void)
{
	if (ss_trigger_page)
		mono_mprotect (ss_trigger_page, mono_pagesize (), 0);
	else
		single_step_tramp = mini_get_single_step_trampoline ();
}
	
/*
 * mono_arch_stop_single_stepping:
 *
 *   Stop single stepping.
 */
void
mono_arch_stop_single_stepping (void)
{
	if (ss_trigger_page)
		mono_mprotect (ss_trigger_page, mono_pagesize (), MONO_MMAP_READ);
	else
		single_step_tramp = NULL;
}

#if __APPLE__
#define DBG_SIGNAL SIGBUS
#else
#define DBG_SIGNAL SIGSEGV
#endif

/*
 * mono_arch_is_single_step_event:
 *
 *   Return whenever the machine state in SIGCTX corresponds to a single
 * step event.
 */
gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	siginfo_t *sinfo = (siginfo_t*)info;

	if (!ss_trigger_page)
		return FALSE;

	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= ss_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)ss_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
}

/*
 * mono_arch_is_breakpoint_event:
 *
 *   Return whenever the machine state in SIGCTX corresponds to a breakpoint event.
 */
gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	siginfo_t *sinfo = (siginfo_t*)info;

	if (!ss_trigger_page)
		return FALSE;

	if (sinfo->si_signo == DBG_SIGNAL) {
		/* Sometimes the address is off by 4 */
		if (sinfo->si_addr >= bp_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)bp_trigger_page + 128)
			return TRUE;
		else
			return FALSE;
	} else {
		return FALSE;
	}
}

/*
 * mono_arch_skip_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 4);
}

/*
 * mono_arch_skip_single_step:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_single_step (MonoContext *ctx)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 4);
}

/*
 * mono_arch_get_seq_point_info:
 *
 *   See mini-amd64.c for docs.
 */
SeqPointInfo*
mono_arch_get_seq_point_info (MonoDomain *domain, guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;

	// FIXME: Add a free function

	mono_domain_lock (domain);
	info = (SeqPointInfo*)g_hash_table_lookup (domain_jit_info (domain)->arch_seq_points,
								code);
	mono_domain_unlock (domain);

	if (!info) {
		ji = mono_jit_info_table_find (domain, code);
		g_assert (ji);

		info = g_malloc0 (sizeof (SeqPointInfo) + ji->code_size);

		info->ss_trigger_page = ss_trigger_page;
		info->bp_trigger_page = bp_trigger_page;
		info->ss_tramp_addr = &single_step_tramp;

		mono_domain_lock (domain);
		g_hash_table_insert (domain_jit_info (domain)->arch_seq_points,
							 code, info);
		mono_domain_unlock (domain);
	}

	return info;
}

#endif /* MONO_ARCH_SOFT_DEBUG_SUPPORTED */

/*
 * mono_arch_set_target:
 *
 *   Set the target architecture the JIT backend should generate code for, in the form
 * of a GNU target triplet. Only used in AOT mode.
 */
void
mono_arch_set_target (char *mtriple)
{
	/* The GNU target triple format is not very well documented */
	if (strstr (mtriple, "armv7")) {
		v5_supported = TRUE;
		v6_supported = TRUE;
		v7_supported = TRUE;
	}
	if (strstr (mtriple, "armv6")) {
		v5_supported = TRUE;
		v6_supported = TRUE;
	}
	if (strstr (mtriple, "armv7s")) {
		v7s_supported = TRUE;
	}
	if (strstr (mtriple, "armv7k")) {
		v7k_supported = TRUE;
	}
	if (strstr (mtriple, "thumbv7s")) {
		v5_supported = TRUE;
		v6_supported = TRUE;
		v7_supported = TRUE;
		v7s_supported = TRUE;
		thumb_supported = TRUE;
		thumb2_supported = TRUE;
	}
	if (strstr (mtriple, "darwin") || strstr (mtriple, "ios")) {
		v5_supported = TRUE;
		v6_supported = TRUE;
		thumb_supported = TRUE;
		iphone_abi = TRUE;
	}
	if (strstr (mtriple, "gnueabi"))
		eabi_supported = TRUE;
}

gboolean
mono_arch_opcode_supported (int opcode)
{
	switch (opcode) {
	case OP_ATOMIC_ADD_I4:
	case OP_ATOMIC_EXCHANGE_I4:
	case OP_ATOMIC_CAS_I4:
	case OP_ATOMIC_LOAD_I1:
	case OP_ATOMIC_LOAD_I2:
	case OP_ATOMIC_LOAD_I4:
	case OP_ATOMIC_LOAD_U1:
	case OP_ATOMIC_LOAD_U2:
	case OP_ATOMIC_LOAD_U4:
	case OP_ATOMIC_STORE_I1:
	case OP_ATOMIC_STORE_I2:
	case OP_ATOMIC_STORE_I4:
	case OP_ATOMIC_STORE_U1:
	case OP_ATOMIC_STORE_U2:
	case OP_ATOMIC_STORE_U4:
		return v7_supported;
	case OP_ATOMIC_LOAD_R4:
	case OP_ATOMIC_LOAD_R8:
	case OP_ATOMIC_STORE_R4:
	case OP_ATOMIC_STORE_R8:
		return v7_supported && IS_VFP;
	default:
		return FALSE;
	}
}

CallInfo*
mono_arch_get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	return get_call_info (mp, sig);
}

gpointer
mono_arch_get_get_tls_tramp (void)
{
	return NULL;
}

static G_GNUC_UNUSED guint8*
emit_aotconst (MonoCompile *cfg, guint8 *code, int dreg, int patch_type, gpointer data)
{
	/* OP_AOTCONST */
	mono_add_patch_info (cfg, code - cfg->native_code, (MonoJumpInfoType)patch_type, data);
	ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
	ARM_B (code, 0);
	*(gpointer*)code = NULL;
	code += 4;
	/* Load the value from the GOT */
	ARM_LDR_REG_REG (code, dreg, ARMREG_PC, dreg);
	return code;
}

guint8*
mono_arm_emit_aotconst (gpointer ji_list, guint8 *code, guint8 *buf, int dreg, int patch_type, gconstpointer data)
{
	MonoJumpInfo **ji = (MonoJumpInfo**)ji_list;

	*ji = mono_patch_info_list_prepend (*ji, code - buf, (MonoJumpInfoType)patch_type, data);
	ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
	ARM_B (code, 0);
	*(gpointer*)code = NULL;
	code += 4;
	ARM_LDR_REG_REG (code, dreg, ARMREG_PC, dreg);
	return code;
}
