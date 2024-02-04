/**
 * \file
 * ARM64 backend for the Mono code generator
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 *
 * Based on mini-arm.c:
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
#include "cpu-arm64.h"
#include "ir-emit.h"
#include "aot-runtime.h"
#include "mini-runtime.h"

#include <mono/arch/arm64/arm64-codegen.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/mono-bitutils.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/marshal-shared.h>
#include "llvm-intrinsics-types.h"

#include "interp/interp.h"


// The following defines are here to support the inclusion of simd-arm64.h
#define EXPAND(x) x
#define PARENTHESIZE(...) (__VA_ARGS__)
#define EXPAND_FUN(m, ...) EXPAND(m PARENTHESIZE(__VA_ARGS__))
#define OPFMT_DS dreg, sreg1
#define OPFMT_TDS _t, dreg, sreg1
#define OPFMT_WDSS _w, dreg, sreg1, sreg2
#define OPFMT_WTDS _w, _t, dreg, sreg1
#define OPFMT_WTDSS _w, _t, dreg, sreg1, sreg2
#define OPFMT_WTDS _w, _t, dreg, sreg1
#define OPFMT_WTDSS_REV _w, _t, dreg, sreg2, sreg1
#define _UNDEF(...) g_assert_not_reached ()
#define _SKIP(...) goto manual_instruction_emit
#define SIMD_OP_CODE(reg_w, op, c) ((reg_w << 31) | (op) << 16 | (c))
#define VREG_64 VREG_LOW
#define VREG_128 VREG_FULL
#define OPCODE_BASIC 0
#define OPCODE_SIMD 1

#define SIMD_OP_INTERNAL(code, reg_w, fmt, i8fun, i16fun, i32fun, i64fun, f32fun, f64fun) \
	if (_f) { \
		if (_t == TYPE_F32) { \
			EXPAND_FUN (f32fun, code, OPFMT_##fmt); \
		} else { \
			EXPAND_FUN (f64fun, code, OPFMT_##fmt); \
		} \
	} else { \
		if (_t == TYPE_I8) { \
			EXPAND_FUN (i8fun, code, OPFMT_##fmt); \
		} else if (_t == TYPE_I16) { \
			EXPAND_FUN (i16fun, code, OPFMT_##fmt); \
		} else if (_t == TYPE_I32) { \
			EXPAND_FUN (i32fun, code, OPFMT_##fmt); \
		} else { \
			EXPAND_FUN (i64fun, code, OPFMT_##fmt); \
		} \
	}


// Several of the 32-bit bit shifts must remain 32-bit, since they assume possible wrap around.
// result of 32-bit shift implicitly converted to 64 bits (was 64-bit shift intended?)
MONO_DISABLE_WARNING(4334)

/*
 * Documentation:
 *
 * - ARM(R) Architecture Reference Manual, ARMv8, for ARMv8-A architecture profile (DDI0487A_a_armv8_arm.pdf)
 * - Procedure Call Standard for the ARM 64-bit Architecture (AArch64) (IHI0055B_aapcs64.pdf)
 * - ELF for the ARM 64-bit Architecture (IHI0056B_aaelf64.pdf)
 *
 * Register usage:
 * - ip0/ip1/lr are used as temporary registers
 * - r27 is used as the rgctx/imt register
 * - r28 is used to access arguments passed on the stack
 * - d15/d16 are used as fp temporary registers
 */

#define FP_TEMP_REG ARMREG_D16
#define FP_TEMP_REG2 ARMREG_D17
#define NEON_TMP_REG FP_TEMP_REG

#define STPX_MIN_OFFSET (-512)
#define STPX_MAX_OFFSET (504)
#define STPW_MIN_OFFSET (-256)
#define STPW_MAX_OFFSET (252)

#define IS_VALID_STPX_OFFSET(o) ((o) >= STPX_MIN_OFFSET && (o) <= (STPX_MAX_OFFSET))

#define THUNK_SIZE (4 * 4)

/* The single step trampoline */
static gpointer ss_trampoline;

/* The breakpoint trampoline */
static gpointer bp_trampoline;

static gboolean ios_abi;
static gboolean enable_ptrauth;

static char opcode_simd_status[OP_LAST - OP_START];

#if defined(HOST_WIN32)
#define WARN_UNUSED_RESULT _Check_return_
#else
#define WARN_UNUSED_RESULT __attribute__ ((__warn_unused_result__))
#endif
static WARN_UNUSED_RESULT guint8* emit_load_regset (guint8 *code, guint64 regs, int basereg, int offset);
static guint8* emit_brx (guint8 *code, int reg);
static guint8* emit_blrx (guint8 *code, int reg);

const char*
mono_arch_regname (int reg)
{
	static const char * rnames[] = {
		"r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9",
		"r10", "r11", "r12", "r13", "r14", "r15", "r16", "r17", "r18", "r19",
		"r20", "r21", "r22", "r23", "r24", "r25", "r26", "r27", "r28", "fp",
		"lr", "sp"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg)
{
	static const char * rnames[] = {
		"d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8", "d9",
		"d10", "d11", "d12", "d13", "d14", "d15", "d16", "d17", "d18", "d19",
		"d20", "d21", "d22", "d23", "d24", "d25", "d26", "d27", "d28", "d29",
		"d30", "d31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown fp";
}

const char *
mono_arch_xregname (int reg)
{
	static const char * rnames[] = {
		"v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9",
		"v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19",
		"v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29",
		"v30", "v31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	NOT_IMPLEMENTED;
	return 0;
}

#define MAX_ARCH_DELEGATE_PARAMS 7

static gpointer
get_delegate_invoke_impl (gboolean has_target, gboolean param_count, guint32 *code_size)
{
	guint8 *code, *start;

	MINI_BEGIN_CODEGEN ();

	if (has_target) {
		int size = 16;
		start = code = mono_global_codeman_reserve (size);

		/* Replace the this argument with the target */
		arm_dmb (code, ARM_DMB_ISHLD);
		arm_ldrx (code, ARMREG_IP0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		arm_ldrx (code, ARMREG_R0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, target));
		code = mono_arm_emit_brx (code, ARMREG_IP0);

		g_assert ((code - start) <= size);
	} else {
		int size, i;

		size = 8 + param_count * 4;
		start = code = mono_global_codeman_reserve (size);

		arm_ldrx (code, ARMREG_IP0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i)
			arm_movx (code, i, i + 1);
		code = mono_arm_emit_brx (code, ARMREG_IP0);

		g_assert ((code - start) <= size);
	}
	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL);

	if (code_size)
		*code_size = GPTRDIFF_TO_UINT32 (code - start);

	return MINI_ADDR_TO_FTNPTR (start);
}

#define MAX_VIRTUAL_DELEGATE_OFFSET 32

static gpointer
get_delegate_virtual_invoke_impl (MonoTrampInfo **info, gboolean load_imt_reg, int offset)
{
	guint8 *code, *start;
	const int size = 20;
	char *tramp_name;
	GSList *unwind_ops;

	if (offset / (int)sizeof (target_mgreg_t) > MAX_VIRTUAL_DELEGATE_OFFSET)
		return NULL;

	MINI_BEGIN_CODEGEN ();

	start = code = mono_global_codeman_reserve (size);

	unwind_ops = mono_arch_get_cie_program ();

	/* Replace the this argument with the target */
	arm_ldrx (code, ARMREG_R0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoDelegate, target));

	if (load_imt_reg)
		// FIXME:
		g_assert_not_reached ();

	/* Load this->vtable [offset] */
	arm_ldrx (code, ARMREG_IP0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoObject, vtable));
	arm_ldrx (code, ARMREG_IP0, ARMREG_IP0, offset);

	code = mono_arm_emit_brx (code, ARMREG_IP0);

	g_assert ((code - start) <= size);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE, NULL);

	tramp_name = mono_get_delegate_virtual_invoke_impl_name (load_imt_reg, offset);
	*info = mono_tramp_info_create (tramp_name, start, GPTRDIFF_TO_UINT32 (code - start), NULL, unwind_ops);
	g_free (tramp_name);

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
	guint8 *code;
	guint32 code_len;
	char *tramp_name;
	MonoTrampInfo *info = NULL;

	code = (guint8*)get_delegate_invoke_impl (TRUE, 0, &code_len);
	res = g_slist_prepend (res, mono_tramp_info_create ("delegate_invoke_impl_has_target", code, code_len, NULL, NULL));

	for (int i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		code = (guint8*)get_delegate_invoke_impl (FALSE, i, &code_len);
		tramp_name = g_strdup_printf ("delegate_invoke_impl_target_%d", i);
		res = g_slist_prepend (res, mono_tramp_info_create (tramp_name, code, code_len, NULL, NULL));
		g_free (tramp_name);
	}

	for (int i = 0; i <= MAX_VIRTUAL_DELEGATE_OFFSET; ++i) {
		get_delegate_virtual_invoke_impl (&info, FALSE, i * TARGET_SIZEOF_VOID_P);
		res = g_slist_prepend (res, info);
		//get_delegate_virtual_invoke_impl (&info, TRUE, i * TARGET_SIZEOF_VOID_P);
		//res = g_slist_prepend (res, info);
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

	/*
	 * vtypes are returned in registers, or using the dedicated r8 register, so
	 * they can be supported by delegate invokes.
	 */

	if (has_target) {
		static guint8* cached = NULL;

		if (cached)
			return cached;

		if (mono_ee_features.use_aot_trampolines)
			start = (guint8*)mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		else
			start = (guint8*)get_delegate_invoke_impl (TRUE, 0, NULL);
		mono_memory_barrier ();
		cached = start;
		return cached;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i;

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		code = cache [sig->param_count];
		if (code)
			return code;

		if (mono_ee_features.use_aot_trampolines) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = (guint8*)mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			start = (guint8*)get_delegate_invoke_impl (FALSE, sig->param_count, NULL);
		}
		mono_memory_barrier ();
		cache [sig->param_count] = start;
		return start;
	}

	return NULL;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg)
{
	MonoTrampInfo *info;
	gpointer code;

	if (load_imt_reg)
		// FIXME:
		return FALSE;

	code = get_delegate_virtual_invoke_impl (&info, load_imt_reg, offset);
	if (code)
		mono_tramp_info_register (info, NULL);

	return code;
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	return (gpointer)regs [ARMREG_R0];
}

void
mono_arch_cpu_init (void)
{
}

void
mono_arch_init (void)
{
#if defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_WATCHOS) || defined(TARGET_OSX)
	ios_abi = TRUE;
#endif
#ifdef MONO_ARCH_ENABLE_PTRAUTH
	enable_ptrauth = TRUE;
#endif

	if (!mono_aot_only)
		bp_trampoline = mini_get_breakpoint_trampoline ();

	mono_arm_gsharedvt_init ();

#ifndef DISABLE_JIT
	memset(opcode_simd_status, OPCODE_BASIC, OP_LAST - OP_START);
	#undef SIMD_OP 
	#define SIMD_OP(reg_w, op, c, fmt, i8fun, i16fun, i32fun, i64fun, f32fun, f64fun) opcode_simd_status[(op) - OP_START] = OPCODE_SIMD;
	#include "simd-arm64.h"
#endif
}

void
mono_arch_cleanup (void)
{
}

guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	*exclude_mask = 0;
	return 0;
}

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_finish_init (void)
{
}

static gboolean
is_type_float_macro (MonoTypeEnum type)
{
	return (type == MONO_TYPE_R4 || type == MONO_TYPE_R8); 
}

static gboolean
is_type_unsigned_macro (MonoTypeEnum type)
{
	return (type == MONO_TYPE_U1 || type == MONO_TYPE_U2 || type == MONO_TYPE_U4 || type == MONO_TYPE_U8);
}

static int
get_vector_size_macro (MonoInst *ins)
{
	g_assert (ins->klass);
	int size = mono_class_value_size (ins->klass, NULL);
	switch (size) {
	case 16:
		return VREG_FULL;
	case 8:
		return VREG_LOW;
	default:
		g_assert_not_reached ();
	}
}

static int
get_type_size_macro (MonoTypeEnum type)
{
	switch (type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		return TYPE_I8;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		return TYPE_I16;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return TYPE_I32;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return TYPE_I64;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
		return TYPE_I64;
#else
		return TYPE_I32;
#endif
	case MONO_TYPE_R4:
		return TYPE_F32;
	case MONO_TYPE_R8:
		return TYPE_F64;
	default:
		g_assert_not_reached ();
	}
}

/* The maximum length is 2 instructions */
static guint8*
emit_imm (guint8 *code, int dreg, int imm)
{
	if (imm < 0) {
		int limm = imm;
		int himm = (limm >> 16) & 0xffff;
		arm_movnx (code, dreg, (~limm) & 0xffff, 0);
		if (himm != 0xffff)
			arm_movkx (code, dreg, himm, 16);

	} else {
		int low = imm & 0xffff;
		int hi = (imm >> 16) & 0xffff;

		if (low == 0) {
			arm_movzx (code, dreg, hi, 16);
		} else if (hi == 0) {
			arm_movzx (code, dreg, low, 0);
		} else {
			arm_movzx (code, dreg, low, 0);
			arm_movkx (code, dreg, hi, 16);
		}
	}
	return code;
}

/* The maximum length is 4 instructions */
static guint8*
emit_imm64 (guint8 *code, int dreg, guint64 imm)
{
	// Determine if there is more advantage to using movn or movz for initialization.
	int num0 = 0, num1 = 0;
	for (int idx = 0; idx < 64; idx+=16) {
		int w = (imm >> idx) & 0xffff;
		if (w == 0)
			num0++;
		else if (w == 0xffff)
			num1++;
	}

	gboolean is_negated = num1 > num0;
	gboolean is_inited = FALSE;

	for (int idx = 0; idx < 64; idx+=16) {
		int w = (imm >> idx) & 0xffff;
		if (!is_inited && is_negated && w != 0xffff) {
			arm_movnx (code, dreg, (~w) & 0xffff, idx);
			is_inited = TRUE;
		} else if (!is_inited && !is_negated && w != 0x0) {
			arm_movzx (code, dreg, w, idx);
			is_inited = TRUE;
		} else if (is_inited && ((is_negated && w != 0xffff) || (!is_negated && w != 0x0))) {
			arm_movkx (code, dreg, w, idx);
		}
	}

	if (!is_inited) {
		if (is_negated)
			arm_movnx (code, dreg, 0, 0);
		else	
			arm_movzx (code, dreg, 0, 0);
	}

	return code;
}

guint8*
mono_arm_emit_imm64 (guint8 *code, int dreg, gint64 imm)
{
	return emit_imm64 (code, dreg, imm);
}

/*
 * emit_imm_template:
 *
 *   Emit a patchable code sequence for constructing a 64 bit immediate.
 */
static guint8*
emit_imm64_template (guint8 *code, int dreg)
{
	arm_movzx (code, dreg, 0, 0);
	arm_movkx (code, dreg, 0, 16);
	arm_movkx (code, dreg, 0, 32);
	arm_movkx (code, dreg, 0, 48);

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_addw_imm (guint8 *code, int dreg, int sreg, int imm)
{
	if (!arm_is_arith_imm (imm)) {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_addw (code, dreg, sreg, ARMREG_LR);
	} else {
		arm_addw_imm (code, dreg, sreg, imm);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_addx_imm (guint8 *code, int dreg, int sreg, int imm)
{
	if (!arm_is_arith_imm (imm)) {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_addx (code, dreg, sreg, ARMREG_LR);
	} else {
		arm_addx_imm (code, dreg, sreg, imm);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_subw_imm (guint8 *code, int dreg, int sreg, int imm)
{
	if (!arm_is_arith_imm (imm)) {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_subw (code, dreg, sreg, ARMREG_LR);
	} else {
		arm_subw_imm (code, dreg, sreg, imm);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_subx_imm (guint8 *code, int dreg, int sreg, int imm)
{
	if (!arm_is_arith_imm (imm)) {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_subx (code, dreg, sreg, ARMREG_LR);
	} else {
		arm_subx_imm (code, dreg, sreg, imm);
	}
	return code;
}

/* Emit sp+=imm. Clobbers ip0/ip1 */
static WARN_UNUSED_RESULT guint8*
emit_addx_sp_imm (guint8 *code, int imm)
{
	code = emit_imm (code, ARMREG_IP0, imm);
	arm_movspx (code, ARMREG_IP1, ARMREG_SP);
	arm_addx (code, ARMREG_IP1, ARMREG_IP1, ARMREG_IP0);
	arm_movspx (code, ARMREG_SP, ARMREG_IP1);
	return code;
}

/* Emit sp-=imm. Clobbers ip0/ip1 */
static WARN_UNUSED_RESULT guint8*
emit_subx_sp_imm (guint8 *code, int imm)
{
	code = emit_imm (code, ARMREG_IP0, imm);
	arm_movspx (code, ARMREG_IP1, ARMREG_SP);
	arm_subx (code, ARMREG_IP1, ARMREG_IP1, ARMREG_IP0);
	arm_movspx (code, ARMREG_SP, ARMREG_IP1);
	return code;
}

static gboolean
encode_arm64_logical_imm (guint64 imm, gboolean islong, int* pn, int* pimms, int* pimmr)
{
	if (imm == 0)
		return FALSE;

	// We recongnize the pattern 0*1+0* for 32- and 64-bit immediates.
	// TODO: expand to full specification
	int lsb_zero, band_one, msb_zero;
	if (islong) {
		lsb_zero = mono_tzcnt64 (imm);
		msb_zero = mono_lzcnt64 (imm);
		band_one = 64 - lsb_zero - msb_zero;

		if (band_one == 0 || band_one == 64) 
			return FALSE;
	} else {
		lsb_zero = mono_tzcnt32 ((guint32)imm);
		msb_zero = mono_lzcnt32 ((guint32)imm);
		band_one = 32 - lsb_zero - msb_zero;

		if (band_one == 0 || band_one == 32) 
			return FALSE;
	}

	guint64 expected_imm = ((1 << band_one) - 1) << lsb_zero;
	if(imm != expected_imm)
		return FALSE;

	int imms = band_one - 1;
	int immr = lsb_zero == 0 ? 0 : (msb_zero + band_one);
	int n = islong ? 1 : 0;

	g_assert (imms >= 0 && imms < 128);
	g_assert (immr >= 0 && immr < 128);

	if (pn) *pn = n;
	if (pimms) *pimms = imms;
	if (pimmr) *pimmr = immr;

	return TRUE;
}


static WARN_UNUSED_RESULT guint8*
emit_andw_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int imms, immr;
	if (encode_arm64_logical_imm (imm, FALSE, NULL, &imms, &immr)) {
		arm_andw_imm (code, dreg, sreg, immr, imms);
	} else {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_andw (code, dreg, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_andx_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int n, imms, immr;
	if (encode_arm64_logical_imm (imm, TRUE, &n, &imms, &immr)) {
		arm_andx_imm (code, dreg, sreg, n, immr, imms);
	} else {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_andx (code, dreg, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_orrw_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int imms, immr;
	if (encode_arm64_logical_imm (imm, FALSE, NULL, &imms, &immr)) {
		arm_orrw_imm (code, dreg, sreg, immr, imms);
	} else {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_orrw (code, dreg, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_orrx_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int n, imms, immr;
	if (encode_arm64_logical_imm (imm, TRUE, &n, &imms, &immr)) {
		arm_orrx_imm (code, dreg, sreg, n, immr, imms);
	} else {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_orrx (code, dreg, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_eorw_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int imms, immr;
	if (encode_arm64_logical_imm (imm, FALSE, NULL, &imms, &immr)) {
		arm_eorw_imm (code, dreg, sreg, immr, imms);
	} else {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_eorw (code, dreg, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_eorx_imm (guint8 *code, int dreg, int sreg, int imm)
{
	int n, imms, immr;
	if (encode_arm64_logical_imm (imm, TRUE, &n, &imms, &immr)) {
		arm_eorx_imm (code, dreg, sreg, n, immr, imms);
	} else {
		code = emit_imm (code, ARMREG_LR, imm);
		arm_eorx (code, dreg, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_cmpw_imm (guint8 *code, int sreg, int imm)
{
	if (imm == 0) {
		arm_cmpw (code, sreg, ARMREG_RZR);
	} else {
		// FIXME:
		code = emit_imm (code, ARMREG_LR, imm);
		arm_cmpw (code, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_cmpx_imm (guint8 *code, int sreg, int imm)
{
	if (imm == 0) {
		arm_cmpx (code, sreg, ARMREG_RZR);
	} else {
		// FIXME:
		code = emit_imm (code, ARMREG_LR, imm);
		arm_cmpx (code, sreg, ARMREG_LR);
	}

	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strb (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_strb_imm (imm)) {
		arm_strb (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_strb_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strh (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_strh_imm (imm)) {
		arm_strh (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_strh_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strw (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_strw_imm (imm)) {
		arm_strw (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_strw_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strfpw (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_strw_imm (imm)) {
		arm_strfpw (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_addx (code, ARMREG_IP0, rn, ARMREG_IP0);
		arm_strfpw (code, rt, ARMREG_IP0, 0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strfpx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_strx_imm (imm)) {
		arm_strfpx (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_addx (code, ARMREG_IP0, rn, ARMREG_IP0);
		arm_strfpx (code, rt, ARMREG_IP0, 0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strfpq (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 16)) {
		arm_strfpq (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_addx (code, ARMREG_IP0, rn, ARMREG_IP0);
		arm_strfpq (code, rt, ARMREG_IP0, 0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_strx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_strx_imm (imm)) {
		arm_strx (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_strx_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrb (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 1)) {
		arm_ldrb (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrb_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrsbx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 1)) {
		arm_ldrsbx (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrsbx_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrh (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 2)) {
		arm_ldrh (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrh_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrshx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 2)) {
		arm_ldrshx (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrshx_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrswx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 4)) {
		arm_ldrswx (code, rt, rn, imm);
	} else {
		g_assert (rt != ARMREG_IP0);
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrswx_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrw (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 4)) {
		arm_ldrw (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrw_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 8)) {
		arm_ldrx (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_ldrx_reg (code, rt, rn, ARMREG_IP0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrfpw (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 4)) {
		arm_ldrfpw (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_addx (code, ARMREG_IP0, rn, ARMREG_IP0);
		arm_ldrfpw (code, rt, ARMREG_IP0, 0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrfpx (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 8)) {
		arm_ldrfpx (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_addx (code, ARMREG_IP0, rn, ARMREG_IP0);
		arm_ldrfpx (code, rt, ARMREG_IP0, 0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_ldrfpq (guint8 *code, int rt, int rn, int imm)
{
	if (arm_is_pimm12_scaled (imm, 16)) {
		arm_ldrfpq (code, rt, rn, imm);
	} else {
		g_assert (rn != ARMREG_IP0);
		code = emit_imm (code, ARMREG_IP0, imm);
		arm_addx (code, ARMREG_IP0, rn, ARMREG_IP0);
		arm_ldrfpq (code, rt, ARMREG_IP0, 0);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_smax_i8 (guint8 *code, int width, int type, int rd, int rn, int rm)
{
	g_assert (rd == rn);
	if (rn != rm) {
		arm_neon_cmgt (code, width, type, NEON_TMP_REG, rn, rm);
		arm_neon_bif (code, width, rd, rm, NEON_TMP_REG);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_umax_i8 (guint8 *code, int width, int type, int rd, int rn, int rm)
{
	g_assert (rd == rn);
	if (rn != rm) {
		arm_neon_cmhi (code, width, type, NEON_TMP_REG, rn, rm);
		arm_neon_bif (code, width, rd, rm, NEON_TMP_REG);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_smin_i8 (guint8 *code, int width, int type, int rd, int rn, int rm)
{
	g_assert (rd == rn);
	if (rn != rm) {
		arm_neon_cmgt (code, width, type, NEON_TMP_REG, rm, rn);
		arm_neon_bif (code, width, rd, rm, NEON_TMP_REG);
	}
	return code;
}

static WARN_UNUSED_RESULT guint8*
emit_umin_i8 (guint8 *code, int width, int type, int rd, int rn, int rm)
{
	g_assert (rd == rn);
	if (rn != rm) {
		arm_neon_cmhi (code, width, type, NEON_TMP_REG, rm, rn);
		arm_neon_bif (code, width, rd, rm, NEON_TMP_REG);
	}
	return code;
}

guint8*
mono_arm_emit_ldrx (guint8 *code, int rt, int rn, int imm)
{
	return emit_ldrx (code, rt, rn, imm);
}

static guint8*
emit_xextract_i8 (guint8* code, int dreg, int sreg1, int sreg2)
{
	guint8* ret = code;
	/* code: */
	arm_cbnzw (ret, sreg2, code + 12 /*upper*/);
	arm_neon_umov (ret, TYPE_I64, dreg, sreg1, 0);
	arm_b (ret, code + 16 /*done*/);
	/* upper: */
	arm_neon_umov (ret, TYPE_I64, dreg, sreg1, 1);
	/* done: */
	return ret;
}

static guint8*
emit_xextract_r8 (guint8* code, int dreg, int sreg1, int sreg2)
{
	guint8* ret = code;
	
	if (dreg == sreg1) {
		/* code: */
		arm_cbzw (ret, sreg2, code + 8 /*done*/);
		arm_neon_fdup_e (ret, VREG_FULL, TYPE_F64, dreg, sreg1, 1);
		/* done: */
	} else {
		/* code: */
		arm_cbnzw (ret, sreg2, code + 12 /*upper*/);
		arm_neon_fdup_e (ret, VREG_FULL, TYPE_F64, dreg, sreg1, 0);
		arm_b (ret, code + 16 /*done*/);
		/* upper: */
		arm_neon_fdup_e (ret, VREG_FULL, TYPE_F64, dreg, sreg1, 1);
		/* done: */
	}
	return ret;
}

static guint8*
emit_xinsert_i8_r8 (guint8* code, MonoTypeEnum type, int dreg, int src_reg, int repl_reg, int index_reg)
{
	guint8* ret = code;
	gboolean is_float = is_type_float_macro (type);
	int extra_code = 0;

	if (dreg != src_reg) {
		arm_neon_mov (ret, dreg, src_reg);
		extra_code = 4;
	}

	arm_cbnzw (ret, index_reg, code + 12 + extra_code);

	if (is_float) {
		arm_neon_ins_e (ret, TYPE_I64, dreg, repl_reg, 0, 0);
		arm_b (ret, code + 16 + extra_code);
		arm_neon_ins_e (ret, TYPE_I64, dreg, repl_reg, 1, 0);
	} else {
		arm_neon_ins_g (ret, TYPE_I64, dreg, repl_reg, 0);
		arm_b (ret, code + 16 + extra_code);
		arm_neon_ins_g (ret, TYPE_I64, dreg, repl_reg, 1);
	}

	return ret; // max. 5 instructions generated = 20 Bytes
}

static guint8*
emit_call (MonoCompile *cfg, guint8* code, MonoJumpInfoType patch_type, gconstpointer data)
{
	/*
	mono_add_patch_info_rel (cfg, GPTRDIFF_TO_INT (code - cfg->native_code), patch_type, data, MONO_R_ARM64_IMM);
	code = emit_imm64_template (code, ARMREG_LR);
	arm_blrx (code, ARMREG_LR);
	*/
	mono_add_patch_info_rel (cfg, GPTRDIFF_TO_INT (code - cfg->native_code), patch_type, data, MONO_R_ARM64_BL);
	arm_bl (code, code);
	cfg->thunk_area += THUNK_SIZE;
	return code;
}

static guint8*
emit_aotconst_full (MonoCompile *cfg, MonoJumpInfo **ji, guint8 *code, guint8 *start, int dreg, guint32 patch_type, gconstpointer data)
{
	if (cfg)
		mono_add_patch_info (cfg, GPTRDIFF_TO_INT (code - cfg->native_code), (MonoJumpInfoType)patch_type, data);
	else
		*ji = mono_patch_info_list_prepend (*ji, GPTRDIFF_TO_INT (code - start), (MonoJumpInfoType)patch_type, data);
	/* See arch_emit_got_access () in aot-compiler.c */
	arm_ldrx_lit (code, dreg, 0);
	arm_nop (code);
	arm_nop (code);
	return code;
}

static guint8*
emit_aotconst (MonoCompile *cfg, guint8 *code, int dreg, guint32 patch_type, gconstpointer data)
{
	return emit_aotconst_full (cfg, NULL, code, NULL, dreg, patch_type, data);
}

/*
 * mono_arm_emit_aotconst:
 *
 *   Emit code to load an AOT constant into DREG. Usable from trampolines.
 */
guint8*
mono_arm_emit_aotconst (gpointer ji, guint8 *code, guint8 *code_start, int dreg, guint32 patch_type, gconstpointer data)
{
	return emit_aotconst_full (NULL, (MonoJumpInfo**)ji, code, code_start, dreg, patch_type, data);
}

gboolean
mono_arch_have_fast_tls (void)
{
#if defined(TARGET_IOS) || defined(TARGET_TVOS)
	return FALSE;
#else
	return TRUE;
#endif
}

static guint8*
emit_tls_get (guint8 *code, int dreg, int tls_offset)
{
	arm_mrs (code, dreg, ARM_MRS_REG_TPIDR_EL0);
	if (tls_offset < 256) {
		arm_ldrx (code, dreg, dreg, tls_offset);
	} else {
		code = emit_addx_imm (code, dreg, dreg, tls_offset);
		arm_ldrx (code, dreg, dreg, 0);
	}
	return code;
}

static guint8*
emit_tls_set (guint8 *code, int sreg, int tls_offset)
{
	int tmpreg = ARMREG_IP0;

	g_assert (sreg != tmpreg);
	arm_mrs (code, tmpreg, ARM_MRS_REG_TPIDR_EL0);
	if (tls_offset < 256) {
		arm_strx (code, sreg, tmpreg, tls_offset);
	} else {
		code = emit_addx_imm (code, tmpreg, tmpreg, tls_offset);
		arm_strx (code, sreg, tmpreg, 0);
	}
	return code;
}

/*
 * Emits
 * - mov sp, fp
 * - ldrp [fp, lr], [sp], !stack_offfset
 * Clobbers TEMP_REGS.
 */
WARN_UNUSED_RESULT guint8*
mono_arm_emit_destroy_frame (guint8 *code, int stack_offset, guint64 temp_regs)
{
	// At least one of these registers must be available, or both.
	gboolean const temp0 = (temp_regs & (1 << ARMREG_IP0)) != 0;
	gboolean const temp1 = (temp_regs & (1 << ARMREG_IP1)) != 0;
	g_assert (temp0 || temp1);
	int const temp = temp0 ? ARMREG_IP0 : ARMREG_IP1;

	arm_movspx (code, ARMREG_SP, ARMREG_FP);

	if (arm_is_ldpx_imm (stack_offset)) {
		arm_ldpx_post (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, stack_offset);
	} else {
		arm_ldpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
		/* sp += stack_offset */
		if (temp0 && temp1) {
			code = emit_addx_sp_imm (code, stack_offset);
		} else {
			int imm = stack_offset;

			/* Can't use addx_sp_imm () since we can't clobber both ip0/ip1 */
			arm_addx_imm (code, temp, ARMREG_SP, 0);
			while (imm > 256) {
				arm_addx_imm (code, temp, temp, 256);
				imm -= 256;
			}
			arm_addx_imm (code, ARMREG_SP, temp, imm);
		}
	}
	return code;
}

#define is_call_imm(diff) ((gint)(diff) >= -33554432 && (gint)(diff) <= 33554431)

static guint8*
emit_thunk (guint8 *code, gconstpointer target)
{
	guint8 *p = code;

	arm_ldrx_lit (code, ARMREG_IP0, code + 8);
	arm_brx (code, ARMREG_IP0);
	*(guint64*)code = (guint64)target;
	code += sizeof (guint64);

	mono_arch_flush_icache (p, GPTRDIFF_TO_INT (code - p));
	return code;
}

static gpointer
create_thunk (MonoCompile *cfg, guchar *code, const guchar *target, int relocation)
{
	MonoJitInfo *ji;
	MonoThunkJitInfo *info;
	guint8 *thunks, *p;
	int thunks_size;
	guint8 *orig_target;
	guint8 *target_thunk;
	MonoJitMemoryManager* jit_mm;

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

		cfg->arch.thunks += THUNK_SIZE;
		cfg->arch.thunks_size -= THUNK_SIZE;

		return thunks;
	} else {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);
		info = mono_jit_info_get_thunk_info (ji);
		g_assert (info);

		thunks = (guint8*)ji->code_start + info->thunks_offset;
		thunks_size = info->thunks_size;

		orig_target = mono_arch_get_call_target (code + 4);

		/* Arbitrary lock */
		jit_mm = get_default_jit_mm ();

		jit_mm_lock (jit_mm);

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
				} else if (((guint64*)p) [1] == (guint64)target) {
					/* Thunk already points to target */
					target_thunk = p;
					break;
				}
			}
		}

		//printf ("THUNK: %p %p %p\n", code, target, target_thunk);

		if (!target_thunk) {
			jit_mm_unlock (jit_mm);
			g_print ("thunk failed %p->%p, thunk space=%d method %s, relocation %d", code, target, thunks_size, cfg ? mono_method_full_name (cfg->method, TRUE) : mono_method_full_name (jinfo_get_method (ji), TRUE), relocation);
			g_assert_not_reached ();
		}

		emit_thunk (target_thunk, target);

		jit_mm_unlock (jit_mm);

		return target_thunk;
	}
}

static void
arm_patch_full (MonoCompile *cfg, guint8 *code, guint8 *target, int relocation)
{
	switch (relocation) {
	case MONO_R_ARM64_B:
		target = MINI_FTNPTR_TO_ADDR (target);
		if (arm_is_bl_disp (code, target)) {
			arm_b (code, target);
		} else {
			gpointer thunk;

			thunk = create_thunk (cfg, code, target, relocation);
			g_assert (arm_is_bl_disp (code, thunk));
			arm_b (code, thunk);
		}
		break;
	case MONO_R_ARM64_BCC: {
		int cond;

		cond = arm_get_bcc_cond (code);
		arm_bcc (code, cond, target);
		break;
	}
	case MONO_R_ARM64_CBZ:
		arm_set_cbz_target (code, target);
		break;
	case MONO_R_ARM64_IMM: {
		guint64 imm = (guint64)target;
		int dreg;

		/* emit_imm64_template () */
		dreg = arm_get_movzx_rd (code);
		arm_movzx (code, dreg, imm & 0xffff, 0);
		arm_movkx (code, dreg, (imm >> 16) & 0xffff, 16);
		arm_movkx (code, dreg, (imm >> 32) & 0xffff, 32);
		arm_movkx (code, dreg, (imm >> 48) & 0xffff, 48);
		break;
	}
	case MONO_R_ARM64_BL:
		target = MINI_FTNPTR_TO_ADDR (target);
		if (arm_is_bl_disp (code, target)) {
			arm_bl (code, target);
		} else {
			gpointer thunk;

			thunk = create_thunk (cfg, code, target, relocation);
			g_assert (arm_is_bl_disp (code, thunk));
			arm_bl (code, thunk);
		}
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
arm_patch_rel (guint8 *code, guint8 *target, int relocation)
{
	arm_patch_full (NULL, code, target, relocation);
}

void
mono_arm_patch (guint8 *code, guint8 *target, int relocation)
{
	arm_patch_rel (code, target, relocation);
}

void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	guint8 *ip;

	ip = ji->ip.i + code;

	switch (ji->type) {
	case MONO_PATCH_INFO_METHOD_JUMP:
		/* ji->relocation is not set by the caller */
		arm_patch_full (cfg, ip, (guint8*)target, MONO_R_ARM64_B);
		mono_arch_flush_icache (ip, 8);
		break;
	default:
		arm_patch_full (cfg, ip, (guint8*)target, ji->relocation);
		break;
	case MONO_PATCH_INFO_NONE:
		break;
	}
}

void
mono_arch_flush_register_windows (void)
{
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	return (MonoMethod*)regs [MONO_ARCH_RGCTX_REG];
}

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*)regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, (guint8*)NULL, (guint8*)NULL, ARMREG_SP, 0);

	return l;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ctx->regs [reg];
}

host_mgreg_t*
mono_arch_context_get_int_reg_address (MonoContext *ctx, int reg)
{
	return &ctx->regs [reg];
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, host_mgreg_t val)
{
	ctx->regs [reg] = val;
}

/*
 * mono_arch_set_target:
 *
 *   Set the target architecture the JIT backend should generate code for, in the form
 * of a GNU target triplet. Only used in AOT mode.
 */
void
mono_arch_set_target (char *mtriple)
{
	if (strstr (mtriple, "darwin") || strstr (mtriple, "ios")) {
		ios_abi = TRUE;
	}
}

static void
add_general (CallInfo *cinfo, ArgInfo *ainfo, int size, gboolean sign)
{
	if (cinfo->gr >= PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		/*
		 * FIXME: The vararg argument handling code in ves_icall_System_ArgIterator_IntGetNextArg
		 * assumes every argument is allocated to a separate full size stack slot.
		 */
		if (ios_abi && !cinfo->vararg) {
			/* Assume size == align */
		} else {
			/* Put arguments into 8 byte aligned stack slots */
			size = 8;
			sign = FALSE;
		}
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, size);
		ainfo->offset = cinfo->stack_usage;
		ainfo->slot_size = size;
		ainfo->sign = sign;
		cinfo->stack_usage += size;
	} else {
		ainfo->storage = ArgInIReg;
		ainfo->reg = cinfo->gr;
		cinfo->gr ++;
	}
}

static void
add_fp (CallInfo *cinfo, ArgInfo *ainfo, gboolean single)
{
	int size = single ? 4 : 8;

	if (cinfo->fr >= FP_PARAM_REGS) {
		ainfo->storage = single ? ArgOnStackR4 : ArgOnStackR8;
		if (ios_abi) {
			cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, size);
			ainfo->offset = cinfo->stack_usage;
			ainfo->slot_size = size;
			cinfo->stack_usage += size;
		} else {
			ainfo->offset = cinfo->stack_usage;
			ainfo->slot_size = 8;
			/* Put arguments into 8 byte aligned stack slots */
			cinfo->stack_usage += 8;
		}
	} else {
		if (single)
			ainfo->storage = ArgInFRegR4;
		else
			ainfo->storage = ArgInFReg;
		ainfo->reg = cinfo->fr;
		cinfo->fr ++;
	}
}

static gboolean
is_hfa (MonoType *t, int *out_nfields, int *out_esize, int *field_offsets)
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
			int nested_field_offsets [16];

			MonoType *fixed_etype;
			int fixed_len;
			if (mono_marshal_shared_get_fixed_buffer_attr (field, &fixed_etype, &fixed_len)) {
				if (fixed_etype->type != MONO_TYPE_R4 && fixed_etype->type != MONO_TYPE_R8)
					return FALSE;
				if (fixed_len > 16)
					return FALSE;
				nested_nfields = fixed_len;
				nested_esize = fixed_etype->type == MONO_TYPE_R4 ? 4 : 8;
				for (int i = 0; i < nested_nfields; ++i)
					nested_field_offsets [i] = i * nested_esize;
			} else {
				if (!is_hfa (ftype, &nested_nfields, &nested_esize, nested_field_offsets))
					return FALSE;
			}

			if (nested_esize == 4)
				ftype = m_class_get_byval_arg (mono_defaults.single_class);
			else
				ftype = m_class_get_byval_arg (mono_defaults.double_class);
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			for (int i = 0; i < nested_nfields; ++i) {
				if (nfields + i < 4)
					field_offsets [nfields + i] = field->offset - MONO_ABI_SIZEOF (MonoObject) + nested_field_offsets [i];
			}
			nfields += nested_nfields;
		} else {
			if (!(!m_type_is_byref (ftype) && (ftype->type == MONO_TYPE_R4 || ftype->type == MONO_TYPE_R8)))
				return FALSE;
			if (prev_ftype && prev_ftype->type != ftype->type)
				return FALSE;
			prev_ftype = ftype;
			if (nfields < 4)
				field_offsets [nfields] = field->offset - MONO_ABI_SIZEOF (MonoObject);
			nfields ++;
		}
	}
	if (nfields == 0 || nfields > 4)
		return FALSE;
	*out_nfields = nfields;
	*out_esize = prev_ftype->type == MONO_TYPE_R4 ? 4 : 8;
	return TRUE;
}

static void
add_valuetype (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t, gboolean is_return)
{
	int i, size, align_size, nregs, nfields, esize;
	int field_offsets [16];
	guint32 align;

	size = mini_type_stack_size_full (t, &align, cinfo->pinvoke);
	align_size = ALIGN_TO (size, 8);

	/* FIXME: gshared, gsharedvt, dyncalls */
#if 0
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	if (m_class_is_simd_type (klass) && size <= 16 && !cinfo->pinvoke && !is_return && cinfo->fr < FP_PARAM_REGS) {
		ainfo->storage = ArgInSIMDReg;
		ainfo->reg = cinfo->fr;
		ainfo->nregs = 1;
		ainfo->size = size;
		cinfo->fr ++;
		return;
	}
#endif

	nregs = align_size / 8;
	if (is_hfa (t, &nfields, &esize, field_offsets)) {
		/*
		 * The struct might include nested float structs aligned at 8,
		 * so need to keep track of the offsets of the individual fields.
		 */
		if (cinfo->fr + nfields <= FP_PARAM_REGS) {
			ainfo->storage = ArgHFA;
			ainfo->reg = cinfo->fr;
			ainfo->nregs = nfields;
			ainfo->size = size;
			ainfo->esize = esize;
			for (i = 0; i < nfields; ++i)
				ainfo->foffsets [i] = GINT_TO_UINT8 (field_offsets [i]);
			cinfo->fr += ainfo->nregs;
		} else {
			ainfo->nfregs_to_skip = FP_PARAM_REGS > cinfo->fr ? FP_PARAM_REGS - cinfo->fr : 0;
			cinfo->fr = FP_PARAM_REGS;
			if (!(ios_abi && cinfo->pinvoke))
				size = ALIGN_TO (size, 8);
			ainfo->storage = ArgVtypeOnStack;
			cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, align);
			ainfo->offset = cinfo->stack_usage;
			ainfo->size = size;
			ainfo->hfa = TRUE;
			ainfo->nregs = nfields;
			ainfo->esize = esize;
			cinfo->stack_usage += size;
		}
		return;
	}

	if (align_size > 16) {
		ainfo->storage = ArgVtypeByRef;
		ainfo->size = size;
		return;
	}

	if (cinfo->gr + nregs > PARAM_REGS) {
		size = ALIGN_TO (size, 8);
		ainfo->storage = ArgVtypeOnStack;
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, align);
		ainfo->offset = cinfo->stack_usage;
		ainfo->size = size;
		cinfo->stack_usage += size;
		cinfo->gr = PARAM_REGS;
	} else {
		ainfo->storage = ArgVtypeInIRegs;
		ainfo->reg = cinfo->gr;
		ainfo->nregs = nregs;
		ainfo->size = size;
		cinfo->gr += nregs;
	}
}

static void
add_param (CallInfo *cinfo, ArgInfo *ainfo, MonoType *t, gboolean is_return)
{
	MonoType *ptype;

	ptype = mini_get_underlying_type (t);
	switch (ptype->type) {
	case MONO_TYPE_I1:
		add_general (cinfo, ainfo, 1, TRUE);
		break;
	case MONO_TYPE_U1:
		add_general (cinfo, ainfo, 1, FALSE);
		break;
	case MONO_TYPE_I2:
		add_general (cinfo, ainfo, 2, TRUE);
		break;
	case MONO_TYPE_U2:
		add_general (cinfo, ainfo, 2, FALSE);
		break;
#ifdef MONO_ARCH_ILP32
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_I4:
		add_general (cinfo, ainfo, 4, TRUE);
		break;
#ifdef MONO_ARCH_ILP32
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
	case MONO_TYPE_U4:
		add_general (cinfo, ainfo, 4, FALSE);
		break;
#ifndef MONO_ARCH_ILP32
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		add_general (cinfo, ainfo, 8, FALSE);
		break;
	case MONO_TYPE_R8:
		add_fp (cinfo, ainfo, FALSE);
		break;
	case MONO_TYPE_R4:
		add_fp (cinfo, ainfo, TRUE);
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF:
		add_valuetype (cinfo, ainfo, ptype, is_return);
		break;
	case MONO_TYPE_VOID:
		ainfo->storage = ArgNone;
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ptype)) {
			add_general (cinfo, ainfo, 8, FALSE);
		} else if (mini_is_gsharedvt_variable_type (ptype)) {
			/*
			 * Treat gsharedvt arguments as large vtypes
			 */
			ainfo->storage = ArgVtypeByRef;
			ainfo->gsharedvt = TRUE;
		} else {
			add_valuetype (cinfo, ainfo, ptype, is_return);
		}
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (mini_is_gsharedvt_type (ptype));
		ainfo->storage = ArgVtypeByRef;
		ainfo->gsharedvt = TRUE;
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

static int
call_info_size (MonoMethodSignature *sig)
{
	int n = sig->hasthis + sig->param_count;

	return sizeof (CallInfo) + (sizeof (ArgInfo) * n);
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 */
static CallInfo*
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int n, pstart, pindex;

	n = sig->hasthis + sig->param_count;

	int size = call_info_size (sig);
	if (mp)
		cinfo = mono_mempool_alloc0 (mp, size);
	else
		cinfo = g_malloc0 (size);

	cinfo->nargs = n;
	cinfo->pinvoke = sig->pinvoke;
	// Constrain this to OSX only for now
#ifdef TARGET_OSX
	cinfo->vararg = sig->call_convention == MONO_CALL_VARARG;
#endif

	/* Return value */
	add_param (cinfo, &cinfo->ret, sig->ret, TRUE);
	if (cinfo->ret.storage == ArgVtypeByRef)
		cinfo->ret.reg = ARMREG_R8;
	/* Reset state */
	cinfo->gr = 0;
	cinfo->fr = 0;
	cinfo->stack_usage = 0;

	/* Parameters */
	if (sig->hasthis)
		add_general (cinfo, cinfo->args + 0, 8, FALSE);
	pstart = 0;
	for (pindex = pstart; pindex < sig->param_count; ++pindex) {
		ainfo = cinfo->args + sig->hasthis + pindex;

		if ((sig->call_convention == MONO_CALL_VARARG) && (pindex == sig->sentinelpos)) {
			/* Prevent implicit arguments and sig_cookie from
			   being passed in registers */
			cinfo->gr = PARAM_REGS;
			cinfo->fr = FP_PARAM_REGS;
			/* Emit the signature cookie just before the implicit arguments */
			add_param (cinfo, &cinfo->sig_cookie, mono_get_int_type (), FALSE);
		}

#ifdef MONO_ARCH_HAVE_SWIFTCALL
		if (mono_method_signature_has_ext_callconv (sig, MONO_EXT_CALLCONV_SWIFTCALL)) {
			MonoClass *swift_self = mono_class_try_get_swift_self_class ();
			MonoClass *swift_error = mono_class_try_get_swift_error_class ();
			MonoClass *swift_error_ptr = mono_class_create_ptr (m_class_get_this_arg (swift_error));
			MonoClass *klass = mono_class_from_mono_type_internal (sig->params [pindex]);
			if (klass == swift_self && sig->pinvoke) {
				guint32 align;
				MonoType *ptype = mini_get_underlying_type (sig->params [pindex]);
				int size = mini_type_stack_size_full (ptype, &align, cinfo->pinvoke);
				g_assert (size == 8);

				ainfo->storage = ArgVtypeInIRegs;
				ainfo->reg = ARMREG_R20;
				ainfo->nregs = 1;
				ainfo->size = size;
				continue;
			} else if (klass == swift_error || klass == swift_error_ptr) {
				if (sig->pinvoke)
					ainfo->reg = ARMREG_R21;
				else
					add_param (cinfo, ainfo, sig->params [pindex], FALSE);
				ainfo->storage = ArgSwiftError;
				continue;
			}
		}
#endif

		add_param (cinfo, ainfo, sig->params [pindex], FALSE);
		if (ainfo->storage == ArgVtypeByRef) {
			/* Pass the argument address in the next register */
			if (cinfo->gr >= PARAM_REGS) {
				ainfo->storage = ArgVtypeByRefOnStack;
				cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, 8);
				ainfo->offset = cinfo->stack_usage;
				cinfo->stack_usage += 8;
			} else {
				ainfo->reg = cinfo->gr;
				cinfo->gr ++;
			}
		}
	}

	/* Handle the case where there are no implicit arguments */
	if ((sig->call_convention == MONO_CALL_VARARG) && (pindex == sig->sentinelpos)) {
		/* Prevent implicit arguments and sig_cookie from
		   being passed in registers */
		cinfo->gr = PARAM_REGS;
		cinfo->fr = FP_PARAM_REGS;
		/* Emit the signature cookie just before the implicit arguments */
		add_param (cinfo, &cinfo->sig_cookie, mono_get_int_type (), FALSE);
	}

	cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);

	return cinfo;
}

static int
arg_need_temp (ArgInfo *ainfo)
{
	if (ainfo->storage == ArgHFA && ainfo->esize == 4)
		return ainfo->size;
	return 0;
}

static gpointer
arg_get_storage (CallContext *ccontext, ArgInfo *ainfo)
{
        switch (ainfo->storage) {
		case ArgVtypeInIRegs:
		case ArgInIReg:
			if (ainfo->reg == ARMREG_R20)
				return &ccontext->gregs [PARAM_REGS + 1];
			else
				return &ccontext->gregs [ainfo->reg];
		case ArgInFReg:
		case ArgInFRegR4:
		case ArgHFA:
                        return &ccontext->fregs [ainfo->reg];
		case ArgOnStack:
		case ArgOnStackR4:
		case ArgOnStackR8:
		case ArgVtypeOnStack:
			return ccontext->stack + ainfo->offset;
		case ArgVtypeByRefOnStack:
			return *(gpointer*)(ccontext->stack + ainfo->offset);
		case ArgVtypeByRef:
			return (gpointer) ccontext->gregs [ainfo->reg];
		case ArgSwiftError:
			return &ccontext->gregs [PARAM_REGS + 2];
                default:
                        g_error ("Arg storage type not yet supported");
        }
}

static void
arg_get_val (CallContext *ccontext, ArgInfo *ainfo, gpointer dest)
{
	g_assert (arg_need_temp (ainfo));

	float *dest_float = (float*)dest;
	for (int k = 0; k < ainfo->nregs; k++) {
		*dest_float = *(float*)&ccontext->fregs [ainfo->reg + k];
		dest_float++;
	}
}

static void
arg_set_val (CallContext *ccontext, ArgInfo *ainfo, gpointer src)
{
	g_assert (arg_need_temp (ainfo));

	float *src_float = (float*)src;
	for (int k = 0; k < ainfo->nregs; k++) {
		*(float*)&ccontext->fregs [ainfo->reg + k] = *src_float;
		src_float++;
	}
}

gpointer
mono_arch_get_interp_native_call_info (MonoMemoryManager *mem_manager, MonoMethodSignature *sig)
{
	CallInfo *cinfo = get_call_info (NULL, sig);
	if (mem_manager) {
		int size = call_info_size (sig);
		gpointer res = mono_mem_manager_alloc0 (mem_manager, size);
		memcpy (res, cinfo, size);
		g_free (cinfo);
		return res;
	} else {
		return cinfo;
	}
}

void
mono_arch_free_interp_native_call_info (gpointer call_info)
{
	/* Allocated by get_call_info () */
	g_free (call_info);
}

/* Set arguments in the ccontext (for i2n entry) */
void
mono_arch_set_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = (CallInfo*)call_info;
	gpointer storage;
	ArgInfo *ainfo;

	memset (ccontext, 0, sizeof (CallContext));

	ccontext->stack_size = ALIGN_TO (cinfo->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	if (ccontext->stack_size)
		ccontext->stack = (guint8*)g_calloc (1, ccontext->stack_size);

	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == ArgVtypeByRef) {
			storage = interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, -1);
			ccontext->gregs [cinfo->ret.reg] = (gsize)storage;
		}
	}

	g_assert (!sig->hasthis);

	for (int i = 0; i < sig->param_count; i++) {
		ainfo = &cinfo->args [i];

		if (ainfo->storage == ArgVtypeByRef) {
			ccontext->gregs [ainfo->reg] = (host_mgreg_t)interp_cb->frame_arg_to_storage ((MonoInterpFrameHandle)frame, sig, i);
			continue;
		} else if (ainfo->storage == ArgVtypeByRefOnStack) {
			storage = ccontext->stack + ainfo->offset;
			*(gpointer*)storage = interp_cb->frame_arg_to_storage (frame, sig, i);
			continue;
		}

		int temp_size = arg_need_temp (ainfo);

		if (temp_size)
			storage = alloca (temp_size); // FIXME? alloca in a loop
		else
			storage = arg_get_storage (ccontext, ainfo);

		if (ainfo->storage == ArgSwiftError) {
			*(gpointer*)storage = 0;
			continue;
		}

		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, i, storage);
		if (temp_size)
			arg_set_val (ccontext, ainfo, storage);
	}
}

/* Set return value in the ccontext (for n2i return) */
void
mono_arch_set_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info, gpointer retp)
{
	const MonoEECallbacks *interp_cb;
	CallInfo *cinfo = (CallInfo*)call_info;
	gpointer storage;
	ArgInfo *ainfo;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	ainfo = &cinfo->ret;

	if (retp) {
		g_assert (ainfo->storage == ArgVtypeByRef);
		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, -1, retp);
	} else {
		g_assert (ainfo->storage != ArgVtypeByRef);
		int temp_size = arg_need_temp (ainfo);

		if (temp_size)
			storage = alloca (temp_size);
		else
			storage = arg_get_storage (ccontext, ainfo);
		memset (ccontext, 0, sizeof (CallContext)); // FIXME
		interp_cb->frame_arg_to_data ((MonoInterpFrameHandle)frame, sig, -1, storage);
		if (temp_size)
			arg_set_val (ccontext, ainfo, storage);
	}
}

/* Gets the arguments from ccontext (for n2i entry) */
gpointer
mono_arch_get_native_call_context_args (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb = mini_get_interp_callbacks ();
	CallInfo *cinfo = (CallInfo*)call_info;
	gpointer storage;
	ArgInfo *ainfo;

	for (guint i = 0; i < sig->param_count + sig->hasthis; i++) {
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

	storage = NULL;
	if (sig->ret->type != MONO_TYPE_VOID) {
		ainfo = &cinfo->ret;
		if (ainfo->storage == ArgVtypeByRef)
			storage = (gpointer) ccontext->gregs [cinfo->ret.reg];
	}
	return storage;
}

/* Gets the return value from ccontext (for i2n exit) */
void
mono_arch_get_native_call_context_ret (CallContext *ccontext, gpointer frame, MonoMethodSignature *sig, gpointer call_info)
{
	const MonoEECallbacks *interp_cb;
	CallInfo *cinfo = (CallInfo*)call_info;
	ArgInfo *ainfo;
	gpointer storage;

	if (sig->ret->type == MONO_TYPE_VOID)
		return;

	interp_cb = mini_get_interp_callbacks ();
	ainfo = &cinfo->ret;

	if (ainfo->storage != ArgVtypeByRef) {
		int temp_size = arg_need_temp (ainfo);

		if (temp_size) {
			storage = alloca (temp_size);
			arg_get_val (ccontext, ainfo, storage);
		} else {
			storage = arg_get_storage (ccontext, ainfo);
		}
		interp_cb->data_to_frame_arg ((MonoInterpFrameHandle)frame, sig, -1, storage);
	}
}

#ifdef MONO_ARCH_HAVE_SWIFTCALL
/**
 * Gets error context from `ccontext` registers by indirectly storing the value onto the stack.
 *
 * The function searches for an argument with SwiftError type
 * If found, it retrieves the value from `ccontext`.
 */
gpointer
mono_arch_get_swift_error (CallContext *ccontext, MonoMethodSignature *sig, int *arg_index)
{
	MonoClass *swift_error = mono_class_try_get_swift_error_class ();
	MonoClass *swift_error_ptr = mono_class_create_ptr (m_class_get_this_arg (swift_error));
	for (guint i = 0; i < sig->param_count + sig->hasthis; i++) {
		MonoClass *klass = mono_class_from_mono_type_internal (sig->params [i]);
		if (klass && (klass == swift_error || klass == swift_error_ptr)) {
			*arg_index = i;
			return &ccontext->gregs [PARAM_REGS + 2];
		}
	}

	return NULL;
}
#endif

typedef struct {
	MonoMethodSignature *sig;
	CallInfo *cinfo;
	MonoType *rtype;
	MonoType **param_types;
	int n_fpargs, n_fpret;
} ArchDynCallInfo;

static gboolean
dyn_call_supported (CallInfo *cinfo, MonoMethodSignature *sig)
{
	int i;

	// FIXME: Add more cases
	switch (cinfo->ret.storage) {
	case ArgNone:
	case ArgInIReg:
	case ArgInFReg:
	case ArgInFRegR4:
	case ArgVtypeByRef:
		break;
	case ArgVtypeInIRegs:
		if (cinfo->ret.nregs > 2)
			return FALSE;
		break;
	case ArgHFA:
		break;
	default:
		return FALSE;
	}

	for (i = 0; i < cinfo->nargs; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgVtypeInIRegs:
		case ArgInFReg:
		case ArgInFRegR4:
		case ArgHFA:
		case ArgVtypeByRef:
		case ArgVtypeByRefOnStack:
		case ArgOnStack:
		case ArgVtypeOnStack:
			break;
		default:
			return FALSE;
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

	switch (cinfo->ret.storage) {
	case ArgInFReg:
	case ArgInFRegR4:
		info->n_fpret = 1;
		break;
	case ArgHFA:
		info->n_fpret = cinfo->ret.nregs;
		break;
	default:
		break;
	}

	return (MonoDynCallInfo*)info;
}

void
mono_arch_dyn_call_free (MonoDynCallInfo *info)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;

	g_free (ainfo->cinfo);
	g_free (ainfo->param_types);
	g_free (ainfo);
}

int
mono_arch_dyn_call_get_buf_size (MonoDynCallInfo *info)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;

	g_assert (ainfo->cinfo->stack_usage % MONO_ARCH_FRAME_ALIGNMENT == 0);
	return sizeof (DynCallArgs) + ainfo->cinfo->stack_usage;
}

static double
bitcast_r4_to_r8 (float f)
{
	float *p = &f;

	return *(double*)p;
}

static float
bitcast_r8_to_r4 (double f)
{
	double *p = &f;

	return *(float*)p;
}

void
mono_arch_start_dyn_call (MonoDynCallInfo *info, gpointer **args, guint8 *ret, guint8 *buf)
{
	ArchDynCallInfo *dinfo = (ArchDynCallInfo*)info;
	DynCallArgs *p = (DynCallArgs*)buf;
	int aindex, arg_index, greg, i, pindex;
	MonoMethodSignature *sig = dinfo->sig;
	CallInfo *cinfo = dinfo->cinfo;

	p->res = 0;
	p->ret = ret;
	p->n_fpargs = dinfo->n_fpargs;
	p->n_fpret = dinfo->n_fpret;
	p->n_stackargs = cinfo->stack_usage / sizeof (host_mgreg_t);

	arg_index = 0;
	greg = 0;
	pindex = 0;

	if (sig->hasthis)
		p->regs [greg ++] = (host_mgreg_t)*(args [arg_index ++]);

	if (cinfo->ret.storage == ArgVtypeByRef)
		p->regs [ARMREG_R8] = (host_mgreg_t)ret;

	for (aindex = pindex; aindex < sig->param_count; aindex++) {
		MonoType *t = dinfo->param_types [aindex];
		gpointer *arg = args [arg_index ++];
		ArgInfo *ainfo = &cinfo->args [aindex + sig->hasthis];
		int slot = -1;

		if (ainfo->storage == ArgOnStack || ainfo->storage == ArgVtypeOnStack || ainfo->storage == ArgVtypeByRefOnStack) {
			slot = PARAM_REGS + 1 + (ainfo->offset / sizeof (host_mgreg_t));
		} else {
			slot = ainfo->reg;
		}

		if (m_type_is_byref (t)) {
			p->regs [slot] = (host_mgreg_t)*arg;
			continue;
		}

		if (ios_abi && ainfo->storage == ArgOnStack) {
			guint8 *stack_arg = (guint8*)&(p->regs [PARAM_REGS + 1]) + ainfo->offset;
			gboolean handled = TRUE;

			/* Special case arguments smaller than 1 machine word */
			switch (t->type) {
			case MONO_TYPE_U1:
				*(guint8*)stack_arg = *(guint8*)arg;
				break;
			case MONO_TYPE_I1:
				*(gint8*)stack_arg = *(gint8*)arg;
				break;
			case MONO_TYPE_U2:
				*(guint16*)stack_arg = *(guint16*)arg;
				break;
			case MONO_TYPE_I2:
				*(gint16*)stack_arg = *(gint16*)arg;
				break;
			case MONO_TYPE_I4:
				*(gint32*)stack_arg = *(gint32*)arg;
				break;
			case MONO_TYPE_U4:
				*(guint32*)stack_arg = *(guint32*)arg;
				break;
			default:
				handled = FALSE;
				break;
			}
			if (handled)
				continue;
		}

		switch (t->type) {
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			p->regs [slot] = (host_mgreg_t)*arg;
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
		case MONO_TYPE_R4:
			p->fpregs [ainfo->reg] = bitcast_r4_to_r8 (*(float*)arg);
			p->n_fpargs ++;
			break;
		case MONO_TYPE_R8:
			p->fpregs [ainfo->reg] = *(double*)arg;
			p->n_fpargs ++;
			break;
		case MONO_TYPE_GENERICINST:
			if (MONO_TYPE_IS_REFERENCE (t)) {
				p->regs [slot] = (host_mgreg_t)*arg;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
			switch (ainfo->storage) {
			case ArgVtypeInIRegs:
				for (i = 0; i < ainfo->nregs; ++i)
					p->regs [slot ++] = ((host_mgreg_t*)arg) [i];
				break;
			case ArgHFA:
				if (ainfo->esize == 4) {
					for (i = 0; i < ainfo->nregs; ++i)
						p->fpregs [ainfo->reg + i] = bitcast_r4_to_r8 (((float*)arg) [ainfo->foffsets [i] / 4]);
				} else {
					for (i = 0; i < ainfo->nregs; ++i)
						p->fpregs [ainfo->reg + i] = ((double*)arg) [ainfo->foffsets [i] / 8];
				}
				p->n_fpargs += ainfo->nregs;
				break;
			case ArgVtypeByRef:
			case ArgVtypeByRefOnStack:
				p->regs [slot] = (host_mgreg_t)arg;
				break;
			case ArgVtypeOnStack:
				for (i = 0; i < ainfo->size / 8; ++i)
					p->regs [slot ++] = ((host_mgreg_t*)arg) [i];
				break;
			default:
				g_assert_not_reached ();
				break;
			}
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
	CallInfo *cinfo = ainfo->cinfo;
	DynCallArgs *args = (DynCallArgs*)buf;
	MonoType *ptype = ainfo->rtype;
	guint8 *ret = args->ret;
	host_mgreg_t res = args->res;
	host_mgreg_t res2 = args->res2;
	int i;

	if (cinfo->ret.storage == ArgVtypeByRef)
		return;

	switch (ptype->type) {
	case MONO_TYPE_VOID:
		*(gpointer*)ret = NULL;
		break;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		*(gpointer*)ret = (gpointer)res;
		break;
	case MONO_TYPE_I1:
		*(gint8*)ret = GHMREG_TO_UINT8 (res);
		break;
	case MONO_TYPE_U1:
		*(guint8*)ret = GHMREG_TO_UINT8 (res);
		break;
	case MONO_TYPE_I2:
		*(gint16*)ret = GHMREG_TO_INT16 (res);
		break;
	case MONO_TYPE_U2:
		*(guint16*)ret = GHMREG_TO_UINT16 (res);
		break;
	case MONO_TYPE_I4:
		*(gint32*)ret = GHMREG_TO_INT32 (res);
		break;
	case MONO_TYPE_U4:
		*(guint32*)ret = GHMREG_TO_UINT32 (res);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*(guint64*)ret = res;
		break;
	case MONO_TYPE_R4:
		*(float*)ret = bitcast_r8_to_r4 (args->fpregs [0]);
		break;
	case MONO_TYPE_R8:
		*(double*)ret = args->fpregs [0];
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_IS_REFERENCE (ptype)) {
			*(gpointer*)ret = (gpointer)res;
			break;
		} else {
			/* Fall though */
		}
	case MONO_TYPE_VALUETYPE:
		switch (ainfo->cinfo->ret.storage) {
		case ArgVtypeInIRegs:
			*(host_mgreg_t*)ret = res;
			if (ainfo->cinfo->ret.nregs > 1)
				((host_mgreg_t*)ret) [1] = res2;
			break;
		case ArgHFA:
			/* Use the same area for returning fp values */
			if (cinfo->ret.esize == 4) {
				for (i = 0; i < cinfo->ret.nregs; ++i)
					((float*)ret) [cinfo->ret.foffsets [i] / 4] = bitcast_r8_to_r4 (args->fpregs [i]);
			} else {
				for (i = 0; i < cinfo->ret.nregs; ++i)
					((double*)ret) [cinfo->ret.foffsets [i] / 8] = args->fpregs [i];
			}
			break;
		default:
			g_assert_not_reached ();
			break;
		}
		break;
	default:
		g_assert_not_reached ();
	}
}

#if __APPLE__
G_BEGIN_DECLS
void sys_icache_invalidate (void *start, size_t len);
G_END_DECLS
#endif

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#ifndef MONO_CROSS_COMPILE
#if __APPLE__
	sys_icache_invalidate (code, size);
#else
	/* Don't rely on GCC's __clear_cache implementation, as it caches
	 * icache/dcache cache line sizes, that can vary between cores on
	 * big.LITTLE architectures. */
	guint64 end = (guint64) (code + size);
	guint64 addr;
	/* always go with cacheline size of 4 bytes as this code isn't perf critical
	 * anyway. Reading the cache line size from a machine register can be racy
	 * on a big.LITTLE architecture if the cores don't have the same cache line
	 * sizes. */
	const size_t icache_line_size = 4;
	const size_t dcache_line_size = 4;

	addr = (guint64) code & ~(guint64) (dcache_line_size - 1);
	for (; addr < end; addr += dcache_line_size)
		asm volatile("dc civac, %0" : : "r" (addr) : "memory");
	asm volatile("dsb ish" : : : "memory");

	addr = (guint64) code & ~(guint64) (icache_line_size - 1);
	for (; addr < end; addr += icache_line_size)
		asm volatile("ic ivau, %0" : : "r" (addr) : "memory");

	asm volatile ("dsb ish" : : : "memory");
	asm volatile ("isb" : : : "memory");
#endif
#endif
}

#ifndef DISABLE_JIT

gboolean
mono_arch_opcode_needs_emulation (MonoCompile *cfg, int opcode)
{
	NOT_IMPLEMENTED;
	return FALSE;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;

	for (guint i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) ||
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		if (mono_is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i;

	/* r28 is reserved for cfg->arch.args_reg */
	/* r27 is reserved for the imt argument */
	for (i = ARMREG_R19; i <= ARMREG_R26; ++i) {
		if (!(mono_method_signature_has_ext_callconv (cfg->method->signature, MONO_EXT_CALLCONV_SWIFTCALL) && i == ARMREG_R21))
			regs = g_list_prepend (regs, GUINT_TO_POINTER (i));
	}

	return regs;
}

guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	MonoInst *ins = cfg->varinfo [vmv->idx];

	if (ins->opcode == OP_ARG)
		return 1;
	else
		return 2;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);
	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	if (cinfo->ret.storage == ArgVtypeByRef) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		cfg->vret_addr->flags |= MONO_INST_VOLATILE;
	}

	if (cfg->gen_sdb_seq_points) {
		MonoInst *ins;

		if (cfg->compile_aot) {
			ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_info_var = ins;
		}

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.ss_tramp_var = ins;

		ins = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.bp_tramp_var = ins;
	}

	if (cfg->method->save_lmf) {
		cfg->create_lmf_var = TRUE;
		cfg->lmf_ir = TRUE;
	}
}

void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoInst *ins;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int offset, size, align;
	guint32 locals_stack_size, locals_stack_align;
	gint32 *offsets;

	/*
	 * Allocate arguments and locals to either register (OP_REGVAR) or to a stack slot (OP_REGOFFSET).
	 * Compute cfg->stack_offset and update cfg->used_int_regs.
	 */

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	/*
	 * The ARM64 ABI always uses a frame pointer.
	 * The instruction set prefers positive offsets, so fp points to the bottom of the
	 * frame, and stack slots are at positive offsets.
	 * If some arguments are received on the stack, their offsets relative to fp can
	 * not be computed right now because the stack frame might grow due to spilling
	 * done by the local register allocator. To solve this, we reserve a register
	 * which points to them.
	 * The stack frame looks like this:
	 * args_reg -> <bottom of parent frame>
	 *             <locals etc>
	 *       fp -> <saved fp+lr>
     *       sp -> <localloc/params area>
	 */
	cfg->frame_reg = ARMREG_FP;
	cfg->flags |= MONO_CFG_HAS_SPILLUP;
	offset = 0;

	/* Saved fp+lr */
	offset += 16;

	if (cinfo->stack_usage) {
		g_assert (!(cfg->used_int_regs & (1 << ARMREG_R28)));
		cfg->arch.args_reg = ARMREG_R28;
		cfg->used_int_regs |= 1 << ARMREG_R28;
	}

	if (cfg->method->save_lmf) {
		/* The LMF var is allocated normally */
	} else {
		/* Callee saved regs */
		cfg->arch.saved_gregs_offset = offset;
		for (guint i = 0; i < 32; ++i)
			if ((MONO_ARCH_CALLEE_SAVED_REGS & (1 << i)) && (cfg->used_int_regs & (1 << i)))
				offset += 8;
	}

	/* Return value */
	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
	case ArgInFReg:
	case ArgInFRegR4:
		cfg->ret->opcode = OP_REGVAR;
		cfg->ret->dreg = cinfo->ret.reg;
		break;
	case ArgVtypeInIRegs:
	case ArgHFA: {
		/* Allocate a local to hold the result, the epilog will copy it to the correct place */
		MonoType *ret_type = mini_get_underlying_type (sig->ret);
		MonoClass *klass = mono_class_from_mono_type_internal (ret_type);
		if (mini_class_is_simd (cfg, klass)) {
			int align_simd = mono_type_size (m_class_get_byval_arg (klass), NULL);
			offset = ALIGN_TO (offset, align_simd);
		}

		cfg->ret->opcode = OP_REGOFFSET;
		cfg->ret->inst_basereg = cfg->frame_reg;
		cfg->ret->inst_offset = offset;
		if (cinfo->ret.storage == ArgHFA)
			// FIXME:
			offset += 64;
		else
			offset += 16;
		break;
	}
	case ArgVtypeByRef:
		/* This variable will be initialized in the prolog from R8 */
		cfg->vret_addr->opcode = OP_REGOFFSET;
		cfg->vret_addr->inst_basereg = cfg->frame_reg;
		cfg->vret_addr->inst_offset = offset;
		offset += 8;
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr =");
			mono_print_ins (cfg->vret_addr);
		}
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	/* Arguments */
	for (guint i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ainfo = cinfo->args + i;

		ins = cfg->args [i];
		if (ins->opcode == OP_REGVAR)
			continue;

		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInFRegR4:
			// FIXME: Use nregs/size
			/* These will be copied to the stack in the prolog */
			ins->inst_offset = offset;
			offset += 8;
			break;
		case ArgOnStack:
		case ArgOnStackR4:
		case ArgOnStackR8:
		case ArgVtypeOnStack:
			/* These are in the parent frame */
			g_assert (cfg->arch.args_reg);
			ins->inst_basereg = cfg->arch.args_reg;
			ins->inst_offset = ainfo->offset;
			break;
		case ArgVtypeInIRegs:
		case ArgHFA:
		case ArgInSIMDReg:
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			/* These arguments are saved to the stack in the prolog */
			ins->inst_offset = offset;
			if (cfg->verbose_level >= 2)
				printf ("arg %d allocated to %s+0x%0x.\n", i, mono_arch_regname (ins->inst_basereg), (int)ins->inst_offset);
			if (ainfo->storage == ArgHFA)
				// FIXME:
				offset += 64;
			else
				offset += 16;
			break;
		case ArgVtypeByRefOnStack: {
			MonoInst *vtaddr;

			if (ainfo->gsharedvt) {
				ins->opcode = OP_REGOFFSET;
				ins->inst_basereg = cfg->arch.args_reg;
				ins->inst_offset = ainfo->offset;
				break;
			}

			/* The vtype address is in the parent frame */
			g_assert (cfg->arch.args_reg);
			MONO_INST_NEW (cfg, vtaddr, 0);
			vtaddr->opcode = OP_REGOFFSET;
			vtaddr->inst_basereg = cfg->arch.args_reg;
			vtaddr->inst_offset = ainfo->offset;

			/* Need an indirection */
			ins->opcode = OP_VTARG_ADDR;
			ins->inst_left = vtaddr;
			break;
		}
		case ArgVtypeByRef: {
			MonoInst *vtaddr;

			if (ainfo->gsharedvt) {
				ins->opcode = OP_REGOFFSET;
				ins->inst_basereg = cfg->frame_reg;
				ins->inst_offset = offset;
				offset += 8;
				break;
			}

			/* The vtype address is in a register, will be copied to the stack in the prolog */
			MONO_INST_NEW (cfg, vtaddr, 0);
			vtaddr->opcode = OP_REGOFFSET;
			vtaddr->inst_basereg = cfg->frame_reg;
			vtaddr->inst_offset = offset;
			offset += 8;

			/* Need an indirection */
			ins->opcode = OP_VTARG_ADDR;
			ins->inst_left = vtaddr;
			break;
		}
		case ArgSwiftError: {
			ins->flags |= MONO_INST_VOLATILE;
			size = 8;
			align = 8;
			offset += align - 1;
			offset &= ~(align - 1);
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			ins->inst_offset = offset;
			offset += size;

			cfg->arch.swift_error_var = ins;
			cfg->used_int_regs |= 1 << ARMREG_R21;
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	/* Allocate these first so they have a small offset, OP_SEQ_POINT depends on this */
	// FIXME: Allocate these to registers
	ins = cfg->arch.seq_point_info_var;
	if (ins) {
		size = 8;
		align = 8;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}
	ins = cfg->arch.ss_tramp_var;
	if (ins) {
		size = 8;
		align = 8;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}
	ins = cfg->arch.bp_tramp_var;
	if (ins) {
		size = 8;
		align = 8;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}

	/* Locals */
	offsets = mono_allocate_stack_slots (cfg, FALSE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align)
		offset = ALIGN_TO (offset, locals_stack_align);

	for (guint i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		if (offsets [i] != -1) {
			ins = cfg->varinfo [i];
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			ins->inst_offset = offset + offsets [i];
			//printf ("allocated local %d to ", i); mono_print_ins (ins);
		}
	}
	offset += locals_stack_size;

	offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	cfg->stack_offset = offset;
}

#ifdef ENABLE_LLVM
LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	guint n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	LLVMCallInfo *linfo;

	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->mempool, sig);

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	switch (cinfo->ret.storage) {
	case ArgInIReg:
	case ArgInFReg:
	case ArgInFRegR4:
		linfo->ret.storage = LLVMArgNormal;
		break;
	case ArgNone:
		linfo->ret.storage = LLVMArgNone;
		break;
	case ArgVtypeByRef:
		linfo->ret.storage = LLVMArgVtypeByRef;
		break;
		//
		// FIXME: This doesn't work yet since the llvm backend represents these types as an i8
		// array which is returned in int regs
		//
	case ArgHFA:
		linfo->ret.storage = LLVMArgFpStruct;
		linfo->ret.nslots = cinfo->ret.nregs;
		linfo->ret.esize = cinfo->ret.esize;
		break;
	case ArgVtypeInIRegs:
		/* LLVM models this by returning an int */
		linfo->ret.storage = LLVMArgVtypeAsScalar;
		linfo->ret.nslots = cinfo->ret.nregs;
		linfo->ret.esize = cinfo->ret.esize;
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	for (guint i = 0; i < n; ++i) {
		LLVMArgInfo *lainfo = &linfo->args [i];

		ainfo = cinfo->args + i;

		lainfo->storage = LLVMArgNone;

		MonoType *t;
		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = mono_get_int_type ();
		t = mini_type_get_underlying_type (t);

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInFRegR4:
		case ArgOnStack:
		case ArgOnStackR4:
		case ArgOnStackR8:
			lainfo->storage = LLVMArgNormal;
			break;
		case ArgVtypeByRef:
		case ArgVtypeByRefOnStack:
			lainfo->storage = LLVMArgVtypeByRef;
			break;
		case ArgHFA: {
			int j;

			lainfo->storage = LLVMArgAsFpArgs;
			lainfo->nslots = ainfo->nregs;
			lainfo->esize = ainfo->esize;
			for (j = 0; j < ainfo->nregs; ++j)
				lainfo->pair_storage [j] = LLVMArgInFPReg;
			break;
		}
		case ArgVtypeInIRegs:
			lainfo->storage = LLVMArgAsIArgs;
			lainfo->nslots = ainfo->nregs;
			break;
		case ArgVtypeOnStack:
			if (ainfo->hfa) {
				int j;
				/* Same as above */
				lainfo->storage = LLVMArgAsFpArgs;
				lainfo->nslots = ainfo->nregs;
				lainfo->esize = ainfo->esize;
				lainfo->ndummy_fpargs = ainfo->nfregs_to_skip;
				for (j = 0; j < ainfo->nregs; ++j)
					lainfo->pair_storage [j] = LLVMArgInFPReg;
			} else {
				lainfo->storage = LLVMArgAsIArgs;
				lainfo->nslots = ainfo->size / 8;
			}
			break;
		case ArgInSIMDReg:
			lainfo->storage = LLVMArgVtypeInSIMDReg;
			break;
		default:
			g_assert_not_reached ();
			break;
		}
	}

	return linfo;
}
#endif

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *arg)
{
	MonoInst *ins;

	switch (storage) {
	case ArgInIReg:
		MONO_INST_NEW (cfg, ins, OP_MOVE);
		ins->dreg = mono_alloc_ireg_copy (cfg, arg->dreg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
		break;
	case ArgInFReg:
		MONO_INST_NEW (cfg, ins, OP_FMOVE);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = arg->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	case ArgInFRegR4:
		if (COMPILE_LLVM (cfg))
			MONO_INST_NEW (cfg, ins, OP_FMOVE);
		else
			MONO_INST_NEW (cfg, ins, OP_RMOVE);
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

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;
	int sig_reg;

	if (MONO_IS_TAILCALL_OPCODE (call))
		NOT_IMPLEMENTED;

	g_assert (cinfo->sig_cookie.storage == ArgOnStack);

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

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoMethodSignature *sig;
	MonoInst *arg, *vtarg;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int i;

	sig = call->signature;

	cinfo = get_call_info (cfg->mempool, sig);

	switch (cinfo->ret.storage) {
	case ArgVtypeInIRegs:
	case ArgHFA:
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
	case ArgVtypeByRef:
		/* Pass the vtype return address in R8 */
		g_assert (!MONO_IS_TAILCALL_OPCODE (call) || call->vret_var == cfg->vret_addr);
		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->sreg1 = call->vret_var->dreg;
		vtarg->dreg = mono_alloc_preg (cfg);
		MONO_ADD_INS (cfg->cbb, vtarg);

		mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		break;
	default:
		break;
	}

	for (i = 0; i < cinfo->nargs; ++i) {
		ainfo = cinfo->args + i;
		arg = call->args [i];

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
		}

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInFRegR4:
			add_outarg_reg (cfg, call, ainfo->storage, ainfo->reg, arg);
			break;
		case ArgOnStack:
			switch (ainfo->slot_size) {
			case 8:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, arg->dreg);
				break;
			case 4:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, ARMREG_SP, ainfo->offset, arg->dreg);
				break;
			case 2:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, ARMREG_SP, ainfo->offset, arg->dreg);
				break;
			case 1:
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, ARMREG_SP, ainfo->offset, arg->dreg);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		case ArgOnStackR8:
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, ainfo->offset, arg->dreg);
			break;
		case ArgOnStackR4:
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, ARMREG_SP, ainfo->offset, arg->dreg);
			break;
		case ArgVtypeInIRegs:
		case ArgVtypeByRef:
		case ArgVtypeByRefOnStack:
		case ArgVtypeOnStack:
		case ArgInSIMDReg:
		case ArgHFA: {
			MonoInst *ins;
			guint32 align;
			guint32 size;

			size = mono_class_value_size (arg->klass, &align);

			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->sreg1 = arg->dreg;
			ins->klass = arg->klass;
			ins->backend.size = size;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			MONO_ADD_INS (cfg->cbb, ins);
			break;
		}
		case ArgSwiftError: {
			MONO_EMIT_NEW_I8CONST (cfg, ainfo->reg, 0);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (cinfo->nargs == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	call->call_info = cinfo;
	call->stack_usage = cinfo->stack_usage;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	MonoInst *load;
	int i;

	if (ins->backend.size == 0 && !ainfo->gsharedvt)
		return;

	switch (ainfo->storage) {
	case ArgVtypeInIRegs:
		for (i = 0; i < ainfo->nregs; ++i) {
			// FIXME: Smaller sizes
			MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = i * sizeof (target_mgreg_t);
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg + i, load);
		}
		break;
	case ArgHFA:
		for (i = 0; i < ainfo->nregs; ++i) {
			if (ainfo->esize == 4)
				MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
			else
				MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
			load->dreg = mono_alloc_freg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = ainfo->foffsets [i];
			MONO_ADD_INS (cfg->cbb, load);
			add_outarg_reg (cfg, call, ainfo->esize == 4 ? ArgInFRegR4 : ArgInFReg, ainfo->reg + i, load);
		}
		break;
	case ArgVtypeByRef:
	case ArgVtypeByRefOnStack: {
		MonoInst *vtaddr, *arg;

		/* Pass the vtype address in a reg/on the stack */
		if (ainfo->gsharedvt) {
			load = src;
		} else {
			/* Make a copy of the argument */
			vtaddr = mono_compile_create_var (cfg, m_class_get_byval_arg (ins->klass), OP_LOCAL);

			MONO_INST_NEW (cfg, load, OP_LDADDR);
			load->inst_p0 = vtaddr;
			vtaddr->flags |= MONO_INST_INDIRECT;
			load->type = STACK_MP;
			load->klass = vtaddr->klass;
			load->dreg = mono_alloc_ireg (cfg);
			MONO_ADD_INS (cfg->cbb, load);
			mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, ainfo->size, 8);
		}

		if (ainfo->storage == ArgVtypeByRef) {
			MONO_INST_NEW (cfg, arg, OP_MOVE);
			arg->dreg = mono_alloc_preg (cfg);
			arg->sreg1 = load->dreg;
			MONO_ADD_INS (cfg->cbb, arg);
			add_outarg_reg (cfg, call, ArgInIReg, ainfo->reg, arg);
		} else {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, load->dreg);
		}
		break;
	}
	case ArgVtypeOnStack: {
		int load_opcode = OP_LOADI8_MEMBASE;
		int store_opcode = OP_STOREI8_MEMBASE_REG;
		int size = 8;
		int offset = 0;
		while (offset < ainfo->size) {
			int left = ainfo->size - offset;
			if (left < 8) {
				switch (left) {
				case 7:
				case 6:
				case 5:
				case 4:
					load_opcode = OP_LOADI4_MEMBASE;
					store_opcode = OP_STOREI4_MEMBASE_REG;
					size = 4;
					break;
				case 3:
				case 2:
					load_opcode = OP_LOADI2_MEMBASE;
					store_opcode = OP_STOREI2_MEMBASE_REG;
					size = 2;
					break;
				case 1:
					load_opcode = OP_LOADI1_MEMBASE;
					store_opcode = OP_STOREI1_MEMBASE_REG;
					size = 1;
					break;
				default:
					g_assert_not_reached ();
					break;
				}
			}
			MONO_INST_NEW (cfg, load, load_opcode);
			load->dreg = mono_alloc_ireg (cfg);
			load->inst_basereg = src->dreg;
			load->inst_offset = offset;
			MONO_ADD_INS (cfg->cbb, load);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, store_opcode, ARMREG_SP, ainfo->offset + offset, load->dreg);
			offset += size;
		}
		break;
	}
	case ArgInSIMDReg:
		MONO_INST_NEW (cfg, load, OP_LOADX_MEMBASE);
		load->dreg = mono_alloc_ireg (cfg);
		load->inst_basereg = src->dreg;
		load->inst_offset = 0;
		load->klass = src->klass;
		MONO_ADD_INS (cfg->cbb, load);
		add_outarg_reg (cfg, call, ArgInFReg, ainfo->reg, load);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);
	CallInfo *cinfo;

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
		break;
	case ArgInFReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		break;
	case ArgInFRegR4:
		if (COMPILE_LLVM (cfg))
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		else
			MONO_EMIT_NEW_UNALU (cfg, OP_RMOVE, cfg->ret->dreg, val->dreg);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

#ifndef DISABLE_JIT

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	g_assert (caller_sig);
	g_assert (callee_sig);

	CallInfo *caller_info = get_call_info (NULL, caller_sig);
	CallInfo *callee_info = get_call_info (NULL, callee_sig);

	gboolean res = IS_SUPPORTED_TAILCALL (callee_info->stack_usage <= caller_info->stack_usage)
		  && IS_SUPPORTED_TAILCALL (caller_info->ret.storage == callee_info->ret.storage);

	// FIXME Limit stack_usage to 1G. emit_ldrx / strx has 32bit limits.
	res &= IS_SUPPORTED_TAILCALL (callee_info->stack_usage < (1 << 30));
	res &= IS_SUPPORTED_TAILCALL (caller_info->stack_usage < (1 << 30));

	// valuetype parameters are the address of a local
	const ArgInfo *ainfo;
	ainfo = callee_info->args + callee_sig->hasthis;
	for (int i = 0; res && i < callee_sig->param_count; ++i) {
		res = IS_SUPPORTED_TAILCALL (ainfo [i].storage != ArgVtypeByRef)
			&& IS_SUPPORTED_TAILCALL (ainfo [i].storage != ArgVtypeByRefOnStack);
	}

	g_free (caller_info);
	g_free (callee_info);

	return res;
}

#endif

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return (imm >= -((gint64)1<<31) && imm <= (((gint64)1<<31)-1));
}

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	//NOT_IMPLEMENTED;
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	//NOT_IMPLEMENTED;
}

#define ADD_NEW_INS(cfg,dest,op) do {       \
		MONO_INST_NEW ((cfg), (dest), (op)); \
        mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *temp, *last_ins = NULL;

	MONO_BB_FOR_EACH_INS (bb, ins) {
		switch (ins->opcode) {
		case OP_SBB:
		case OP_ISBB:
		case OP_SUBCC:
		case OP_ISUBCC:
			if (ins->next  && (ins->next->opcode == OP_COND_EXC_C || ins->next->opcode == OP_COND_EXC_IC))
				/* ARM sets the C flag to 1 if there was _no_ overflow */
				ins->next->opcode = OP_COND_EXC_NC;
			break;
		case OP_IDIV_IMM:
		case OP_IREM_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_UN_IMM:
		case OP_LREM_IMM:
			mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_LOCALLOC_IMM:
			if (ins->inst_imm > 32) {
				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = temp->dreg;
				ins->opcode = GINT_TO_OPCODE (mono_op_imm_to_op (ins->opcode));
			}
			break;
		case OP_ICOMPARE_IMM:
			if (ins->inst_imm == 0 && ins->next && ins->next->opcode == OP_IBEQ) {
				ins->next->opcode = OP_ARM64_CBZW;
				ins->next->sreg1 = ins->sreg1;
				NULLIFY_INS (ins);
			} else if (ins->inst_imm == 0 && ins->next && ins->next->opcode == OP_IBNE_UN) {
				ins->next->opcode = OP_ARM64_CBNZW;
				ins->next->sreg1 = ins->sreg1;
				NULLIFY_INS (ins);
			}
			break;
		case OP_LCOMPARE_IMM:
		case OP_COMPARE_IMM:
			if (ins->inst_imm == 0 && ins->next && ins->next->opcode == OP_LBEQ) {
				ins->next->opcode = OP_ARM64_CBZX;
				ins->next->sreg1 = ins->sreg1;
				NULLIFY_INS (ins);
			} else if (ins->inst_imm == 0 && ins->next && ins->next->opcode == OP_LBNE_UN) {
				ins->next->opcode = OP_ARM64_CBNZX;
				ins->next->sreg1 = ins->sreg1;
				NULLIFY_INS (ins);
			}
			break;
		case OP_FCOMPARE:
		case OP_RCOMPARE: {
			gboolean swap = FALSE;
			int reg;

			if (!ins->next) {
				/* Optimized away */
				NULLIFY_INS (ins);
				break;
			}

			/*
			 * FP compares with unordered operands set the flags
			 * to NZCV=0011, which matches some non-unordered compares
			 * as well, like LE, so have to swap the operands.
			 */
			switch (ins->next->opcode) {
			case OP_FBLT:
				ins->next->opcode = OP_FBGT;
				swap = TRUE;
				break;
			case OP_FBLE:
				ins->next->opcode = OP_FBGE;
				swap = TRUE;
				break;
			case OP_RBLT:
				ins->next->opcode = OP_RBGT;
				swap = TRUE;
				break;
			case OP_RBLE:
				ins->next->opcode = OP_RBGE;
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
		default:
			break;
		}

		last_ins = ins;
	}
	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;
}

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *long_ins)
{
}

static int
opcode_to_armcond (int opcode)
{
	switch (opcode) {
	case OP_IBEQ:
	case OP_LBEQ:
	case OP_FBEQ:
	case OP_CEQ:
	case OP_ICEQ:
	case OP_LCEQ:
	case OP_FCEQ:
	case OP_RCEQ:
	case OP_COND_EXC_IEQ:
	case OP_COND_EXC_EQ:
		return ARMCOND_EQ;
	case OP_IBGE:
	case OP_LBGE:
	case OP_FBGE:
	case OP_ICGE:
	case OP_FCGE:
	case OP_RCGE:
		return ARMCOND_GE;
	case OP_IBGT:
	case OP_LBGT:
	case OP_FBGT:
	case OP_CGT:
	case OP_ICGT:
	case OP_LCGT:
	case OP_FCGT:
	case OP_RCGT:
	case OP_COND_EXC_IGT:
	case OP_COND_EXC_GT:
		return ARMCOND_GT;
	case OP_IBLE:
	case OP_LBLE:
	case OP_FBLE:
	case OP_ICLE:
	case OP_FCLE:
	case OP_RCLE:
		return ARMCOND_LE;
	case OP_IBLT:
	case OP_LBLT:
	case OP_FBLT:
	case OP_CLT:
	case OP_ICLT:
	case OP_LCLT:
	case OP_COND_EXC_ILT:
	case OP_COND_EXC_LT:
		return ARMCOND_LT;
	case OP_IBNE_UN:
	case OP_LBNE_UN:
	case OP_FBNE_UN:
	case OP_ICNEQ:
	case OP_FCNEQ:
	case OP_RCNEQ:
	case OP_COND_EXC_INE_UN:
	case OP_COND_EXC_NE_UN:
		return ARMCOND_NE;
	case OP_IBGE_UN:
	case OP_LBGE_UN:
	case OP_FBGE_UN:
	case OP_ICGE_UN:
	case OP_COND_EXC_IGE_UN:
	case OP_COND_EXC_GE_UN:
		return ARMCOND_HS;
	case OP_IBGT_UN:
	case OP_LBGT_UN:
	case OP_FBGT_UN:
	case OP_CGT_UN:
	case OP_ICGT_UN:
	case OP_LCGT_UN:
	case OP_FCGT_UN:
	case OP_RCGT_UN:
	case OP_COND_EXC_IGT_UN:
	case OP_COND_EXC_GT_UN:
		return ARMCOND_HI;
	case OP_IBLE_UN:
	case OP_LBLE_UN:
	case OP_FBLE_UN:
	case OP_ICLE_UN:
	case OP_COND_EXC_ILE_UN:
	case OP_COND_EXC_LE_UN:
		return ARMCOND_LS;
	case OP_IBLT_UN:
	case OP_LBLT_UN:
	case OP_FBLT_UN:
	case OP_CLT_UN:
	case OP_ICLT_UN:
	case OP_LCLT_UN:
	case OP_COND_EXC_ILT_UN:
	case OP_COND_EXC_LT_UN:
		return ARMCOND_LO;
		/*
		 * FCMP sets the NZCV condition bits as follows:
		 * eq = 0110
		 * < = 1000
		 * > = 0010
		 * unordered = 0011
		 * ARMCOND_LT is N!=V, so it matches unordered too, so
		 * fclt and fclt_un need to be special cased.
		 */
	case OP_FCLT:
	case OP_RCLT:
		/* N==1 */
		return ARMCOND_MI;
	case OP_FCLT_UN:
	case OP_RCLT_UN:
		return ARMCOND_LT;
	case OP_COND_EXC_C:
	case OP_COND_EXC_IC:
		return ARMCOND_CS;
	case OP_COND_EXC_OV:
	case OP_COND_EXC_IOV:
		return ARMCOND_VS;
	case OP_COND_EXC_NC:
	case OP_COND_EXC_INC:
		return ARMCOND_CC;
	case OP_COND_EXC_NO:
	case OP_COND_EXC_INO:
		return ARMCOND_VC;
	default:
		printf ("%s\n", mono_inst_name (opcode));
		g_assert_not_reached ();
		return -1;
	}
}

/* This clobbers LR */
static WARN_UNUSED_RESULT guint8*
emit_cond_exc (MonoCompile *cfg, guint8 *code, int opcode, const char *exc_name)
{
	int cond;

	cond = opcode_to_armcond (opcode);
	/* Capture PC */
	arm_adrx (code, ARMREG_IP1, code);
	mono_add_patch_info_rel (cfg, GPTRDIFF_TO_INT (code - cfg->native_code), MONO_PATCH_INFO_EXC, exc_name, MONO_R_ARM64_BCC);
	arm_bcc (code, cond, 0);
	return code;
}

static guint8*
emit_move_return_value (MonoCompile *cfg, guint8 * code, MonoInst *ins)
{
	CallInfo *cinfo;
	MonoCallInst *call;

	if (cfg->arch.swift_error_var) {
		code = emit_ldrx (code, ARMREG_IP0, cfg->arch.swift_error_var->inst_basereg, GTMREG_TO_INT (cfg->arch.swift_error_var->inst_offset));
		code = emit_strx (code, ARMREG_R21, ARMREG_IP0, 0);
	}

	call = (MonoCallInst*)ins;
	cinfo = call->call_info;
	g_assert (cinfo);
	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		/* LLVM compiled code might only set the bottom bits */
		if (call->signature && mini_get_underlying_type (call->signature->ret)->type == MONO_TYPE_I4)
			arm_sxtwx (code, call->inst.dreg, cinfo->ret.reg);
		else if (call->inst.dreg != cinfo->ret.reg)
			arm_movx (code, call->inst.dreg, cinfo->ret.reg);
		break;
	case ArgInFReg:
		if (call->inst.dreg != cinfo->ret.reg)
			arm_fmovd (code, call->inst.dreg, cinfo->ret.reg);
		break;
	case ArgInFRegR4:
		arm_fmovs (code, call->inst.dreg, cinfo->ret.reg);
		break;
	case ArgVtypeInIRegs: {
		MonoInst *loc = cfg->arch.vret_addr_loc;
		int i;

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);
		code = emit_ldrx (code, ARMREG_LR, loc->inst_basereg, GTMREG_TO_INT (loc->inst_offset));
		for (i = 0; i < cinfo->ret.nregs; ++i) {
			int offs = i * 8;
			// emit a pair of str as one stp
			if (i+1 < cinfo->ret.nregs && IS_VALID_STPX_OFFSET (offs)) {
				arm_stpx (code, cinfo->ret.reg + i, cinfo->ret.reg + i + 1, ARMREG_LR, offs);
				i++;
			} else {
				arm_strx (code, cinfo->ret.reg + i, ARMREG_LR, offs);
			}
		}
		break;
	}
	case ArgHFA: {
		MonoInst *loc = cfg->arch.vret_addr_loc;
		int i;

		/* Load the destination address */
		g_assert (loc && loc->opcode == OP_REGOFFSET);
		code = emit_ldrx (code, ARMREG_LR, loc->inst_basereg, GTMREG_TO_INT (loc->inst_offset));
		for (i = 0; i < cinfo->ret.nregs; ++i) {
			if (cinfo->ret.esize == 4)
				arm_strfpw (code, cinfo->ret.reg + i, ARMREG_LR, cinfo->ret.foffsets [i]);
			else
				arm_strfpx (code, cinfo->ret.reg + i, ARMREG_LR, cinfo->ret.foffsets [i]);
		}
		break;
	}
	case ArgVtypeByRef:
		break;
	default:
		g_assert_not_reached ();
		break;
	}
	return code;
}

static guint8*
emit_xextract (guint8* code, int width, int mode, int dreg, int sreg1)
{
	switch (mode) {
	case SIMD_EXTR_IS_ANY_SET:
		arm_neon_umaxv (code, width, TYPE_I8, FP_TEMP_REG, sreg1);
		arm_neon_umov_b (code, dreg, FP_TEMP_REG, 0);
		arm_lsrw(code, dreg, dreg, 7); // dreg contains 0xff for TRUE or 0x0 for FALSE, normalize to 0x1/0x0
		break;
	case SIMD_EXTR_ARE_ALL_SET:
		arm_neon_uminv (code, width, TYPE_I8, FP_TEMP_REG, sreg1);
		arm_neon_umov_b (code, dreg, FP_TEMP_REG, 0);
		arm_lsrw(code, dreg, dreg, 7);
		break;
	default:
		g_assert_not_reached ();
	}

	return code;
}

/*
 * emit_branch_island:
 *
 *   Emit a branch island for the conditional branches from cfg->native_code + start_offset to code.
 */
static guint8*
emit_branch_island (MonoCompile *cfg, guint8 *code, int start_offset)
{
	MonoJumpInfo *ji;

	/* Iterate over the patch infos added so far by this bb */
	int island_size = 0;
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->ip.i < start_offset)
			/* The patch infos are in reverse order, so this means the end */
			break;
		if (ji->relocation == MONO_R_ARM64_BCC || ji->relocation == MONO_R_ARM64_CBZ)
			island_size += 4;
	}

	if (island_size) {
		code = realloc_code (cfg, island_size);

		/* Branch over the island */
		arm_b (code, code + 4 + island_size);

		for (ji = cfg->patch_info; ji; ji = ji->next) {
			if (ji->ip.i < start_offset)
				break;
			if (ji->relocation == MONO_R_ARM64_BCC || ji->relocation == MONO_R_ARM64_CBZ) {
				/* Rewrite the cond branch so it branches to an unconditional branch in the branch island */
				arm_patch_rel (cfg->native_code + ji->ip.i, code, ji->relocation);
				/* Rewrite the patch so it points to the unconditional branch */
				ji->ip.i = GPTRDIFF_TO_INT (code - cfg->native_code);
				ji->relocation = MONO_R_ARM64_B;
				arm_b (code, code);
			}
		}
		set_code_cursor (cfg, code);
	}
	return code;
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint8 *code = cfg->native_code + cfg->code_len;
	int start_offset, max_len, dreg, sreg1, sreg2, sreg3;
	target_mgreg_t imm;

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	start_offset = GPTRDIFF_TO_INT (code - cfg->native_code);
	g_assert (GINT_TO_UINT(start_offset) <= cfg->code_size);

	MONO_BB_FOR_EACH_INS (bb, ins) {
		guint offset = GPTRDIFF_TO_UINT (code - cfg->native_code);
		set_code_cursor (cfg, code);
		max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);

		if (G_UNLIKELY (cfg->arch.cond_branch_islands && offset - start_offset > 4 * 0x1ffff)) {
			/* Emit a branch island for large basic blocks */
			code = emit_branch_island (cfg, code, start_offset);
			offset = GPTRDIFF_TO_UINT (code - cfg->native_code);
			start_offset = offset;
		}

		mono_debug_record_line_number (cfg, ins, offset);

		dreg = ins->dreg;
		sreg1 = ins->sreg1;
		sreg2 = ins->sreg2;
		sreg3 = ins->sreg3;
		imm = ins->inst_imm;

		if (opcode_simd_status [ins->opcode - OP_START] == OPCODE_SIMD)
		{
			const int _t = get_type_size_macro (ins->inst_c1);
			const gboolean _f = is_type_float_macro (ins->inst_c1);
			const int _w = get_vector_size_macro (ins);

			#undef SIMD_OP
			#define SIMD_OP(reg_w, op, c, fmt, i8fun, i16fun, i32fun, i64fun, f32fun, f64fun) \
				case SIMD_OP_CODE (VREG_##reg_w, (op), (c)): { \
					SIMD_OP_INTERNAL (code, reg_w, fmt, i8fun, i16fun, i32fun, i64fun, f32fun, f64fun); \
					} break;

			switch (SIMD_OP_CODE (_w, ins->opcode, ins->inst_c0)) {
				#include "simd-arm64.h"
			default:
				g_assert_not_reached();
				break;
			}

			goto after_instruction_emit;
		}

	manual_instruction_emit:
		switch (ins->opcode) {
		case OP_ICONST:
			code = emit_imm (code, dreg, GTMREG_TO_INT (ins->inst_c0));
			break;
		case OP_I8CONST:
			code = emit_imm64 (code, dreg, ins->inst_c0);
			break;
		case OP_MOVE:
			if (dreg != sreg1)
				arm_movx (code, dreg, sreg1);
			break;
		case OP_NOP:
		case OP_RELAXED_NOP:
			break;
		case OP_JUMP_TABLE:
			mono_add_patch_info_rel (cfg, offset, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0, MONO_R_ARM64_IMM);
			code = emit_imm64_template (code, dreg);
			break;
		case OP_BREAK:
			/*
			 * gdb does not like encountering the hw breakpoint ins in the debugged code.
			 * So instead of emitting a trap, we emit a call a C function and place a
			 * breakpoint there.
			 */
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_break));
			break;
		case OP_LOCALLOC: {
			guint8 *buf [16];

			arm_addx_imm (code, ARMREG_IP0, sreg1, (MONO_ARCH_FRAME_ALIGNMENT - 1));
			// FIXME: andx_imm doesn't work yet
			code = emit_imm (code, ARMREG_IP1, -MONO_ARCH_FRAME_ALIGNMENT);
			arm_andx (code, ARMREG_IP0, ARMREG_IP0, ARMREG_IP1);
			//arm_andx_imm (code, ARMREG_IP0, sreg1, - MONO_ARCH_FRAME_ALIGNMENT);
			arm_movspx (code, ARMREG_IP1, ARMREG_SP);
			arm_subx (code, ARMREG_IP1, ARMREG_IP1, ARMREG_IP0);
			arm_movspx (code, ARMREG_SP, ARMREG_IP1);

			/* Init */
			/* ip1 = pointer, ip0 = end */
			arm_addx (code, ARMREG_IP0, ARMREG_IP1, ARMREG_IP0);
			buf [0] = code;
			arm_cmpx (code, ARMREG_IP1, ARMREG_IP0);
			buf [1] = code;
			arm_bcc (code, ARMCOND_EQ, 0);
			arm_stpx (code, ARMREG_RZR, ARMREG_RZR, ARMREG_IP1, 0);
			arm_addx_imm (code, ARMREG_IP1, ARMREG_IP1, 16);
			arm_b (code, buf [0]);
			arm_patch_rel (buf [1], code, MONO_R_ARM64_BCC);

			arm_movspx (code, dreg, ARMREG_SP);
			if (cfg->param_area)
				code = emit_subx_sp_imm (code, cfg->param_area);
			break;
		}
		case OP_LOCALLOC_IMM: {
			int aligned_imm, aligned_imm_offset;

			aligned_imm = ALIGN_TO (ins->inst_imm, MONO_ARCH_FRAME_ALIGNMENT);
			g_assert (arm_is_arith_imm (aligned_imm));
			arm_subx_imm (code, ARMREG_SP, ARMREG_SP, aligned_imm);

			/* Init */
			g_assert (MONO_ARCH_FRAME_ALIGNMENT == 16);
			aligned_imm_offset = 0;
			while (aligned_imm_offset < aligned_imm) {
				arm_stpx (code, ARMREG_RZR, ARMREG_RZR, ARMREG_SP, aligned_imm_offset);
				aligned_imm_offset += 16;
			}
			arm_movspx (code, dreg, ARMREG_SP);
			if (cfg->param_area)
				code = emit_subx_sp_imm (code, cfg->param_area);
			break;
		}
		case OP_AOTCONST:
			code = emit_aotconst (cfg, code, dreg, (MonoJumpInfoType)(gsize)ins->inst_i1, ins->inst_p0);
			break;
		case OP_OBJC_GET_SELECTOR:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_OBJC_SELECTOR_REF, ins->inst_p0);
			/* See arch_emit_objc_selector_ref () in aot-compiler.c */
			arm_ldrx_lit (code, ins->dreg, 0);
			arm_nop (code);
			arm_nop (code);
			break;
		case OP_SEQ_POINT: {
			MonoInst *info_var = cfg->arch.seq_point_info_var;

			/*
			 * For AOT, we use one got slot per method, which will point to a
			 * SeqPointInfo structure, containing all the information required
			 * by the code below.
			 */
			if (cfg->compile_aot) {
				g_assert (info_var);
				g_assert (info_var->opcode == OP_REGOFFSET);
			}

			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				MonoInst *var = cfg->arch.ss_tramp_var;

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load ss_tramp_var */
				/* This is equal to &ss_trampoline */
				arm_ldrx (code, ARMREG_IP1, var->inst_basereg, GTMREG_TO_INT (var->inst_offset));
				/* Load the trampoline address */
				arm_ldrx (code, ARMREG_IP1, ARMREG_IP1, 0);
				/* Call it if it is non-null */
				arm_cbzx (code, ARMREG_IP1, code + 8);
				code = mono_arm_emit_blrx (code, ARMREG_IP1);
			}

			mono_add_seq_point (cfg, bb, ins, GPTRDIFF_TO_INT (code - cfg->native_code));

			if (cfg->compile_aot) {
				const guint32 bp_tramp_offset = GPTRDIFF_TO_UINT32 (code - cfg->native_code);
				guint32 val;

				arm_ldrx (code, ARMREG_IP1, info_var->inst_basereg, GTMREG_TO_INT (info_var->inst_offset));
				/* Add the bp_tramp_offset */
				val = ((bp_tramp_offset / 4) * sizeof (target_mgreg_t)) + MONO_STRUCT_OFFSET (SeqPointInfo, bp_addrs);
				/* Load the info->bp_addrs [bp_tramp_offset], which is either 0 or the address of the bp trampoline */
				code = emit_ldrx (code, ARMREG_IP1, ARMREG_IP1, val);
				/* Skip the load if its 0 */
				arm_cbzx (code, ARMREG_IP1, code + 8);
				/* Call the breakpoint trampoline */
				code = mono_arm_emit_blrx (code, ARMREG_IP1);
			} else {
				MonoInst *var = cfg->arch.bp_tramp_var;

				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				/* Load the address of the bp trampoline into IP0 */
				arm_ldrx (code, ARMREG_IP0, var->inst_basereg, GTMREG_TO_INT (var->inst_offset));
				/*
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				arm_nop (code);
			}
			break;
		}
			/* SIMD that is not table-generated */
			/* TODO: once https://github.com/dotnet/runtime/issues/83252 is done,
			 * move the following two to the codegen table in simd-arm64.h
			 */
		case OP_ONES_COMPLEMENT:
			arm_neon_not (code, get_vector_size_macro (ins), dreg, sreg1);
			break;
		case OP_NEGATION:
			if (is_type_float_macro (ins->inst_c1)) {
				arm_neon_fneg (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1);
			} else {
				arm_neon_neg (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1);
			}
			break;
		case OP_ARM64_BIC:
			arm_neon_bic (code, get_vector_size_macro (ins), dreg, sreg1, sreg2);
			break;
		case OP_BSL:
			arm_neon_mov (code, NEON_TMP_REG, sreg1);
			arm_neon_bsl (code, get_vector_size_macro (ins), NEON_TMP_REG, sreg2, ins->sreg3);
			arm_neon_mov (code, dreg, NEON_TMP_REG);
			break;
		case OP_XBINOP:
			switch (ins->inst_c0) {
			case OP_IMAX:
				code = emit_smax_i8 (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1, sreg2);
				break;
			case OP_IMAX_UN:
				code = emit_umax_i8 (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1, sreg2);
				break;
			case OP_IMIN:
				code = emit_smin_i8 (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1, sreg2);
				break;
			case OP_IMIN_UN:
				code = emit_umin_i8 (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1, sreg2);
				break;
			default:
				g_assert_not_reached ();
			}
			break;
		case OP_XOP_OVR_X_X_X: {
			IntrinsicId iid = (IntrinsicId) ins->inst_c0;
			switch (iid) {
			case INTRINS_AARCH64_ADV_SIMD_TBL1:
				arm_neon_tbl1_16b (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_ADV_SIMD_USHL:
				arm_neon_ushl (code, get_vector_size_macro (ins), get_type_size_macro (ins->inst_c1), dreg, sreg1, sreg2);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		}
		case OP_XOP_OVR_X_X_X_X: {
			IntrinsicId iid = (IntrinsicId) ins->inst_c0;
			g_assert (dreg == sreg1);
			g_assert (mono_class_value_size (ins->klass, NULL) == 16);
			switch (iid) {
			case INTRINS_AARCH64_ADV_SIMD_SDOT:
				arm_neon_sdot_4s (code, dreg, sreg2, sreg3);
				break;
			case INTRINS_AARCH64_ADV_SIMD_UDOT:
				arm_neon_udot_4s (code, dreg, sreg2, sreg3);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		}
		case OP_ARM64_BROADCAST_ELEM:
			arm_neon_smov (code, TYPE_I32, ARMREG_IP0, sreg1, GTMREG_TO_UINT32 (ins->inst_c0));
			arm_neon_dup_g_4s (code, dreg, ARMREG_IP0);
			break;

		case OP_XZERO:
			arm_neon_movi_b (code, get_vector_size_macro (ins), dreg, 0);
			break;
		case OP_XONES:
			arm_neon_movi_b (code, get_vector_size_macro (ins), dreg, 0xff);
			break;
		case OP_XEXTRACT: 
			code = emit_xextract (code, (ins->inst_c1 == 8) ? VREG_LOW : VREG_FULL, GTMREG_TO_INT (ins->inst_c0), dreg, sreg1);
			break;
		case OP_STOREX_MEMBASE:
			if (ins->klass && mono_class_value_size (ins->klass, NULL) == 8)
				code = emit_strfpx (code, sreg1, dreg, GTMREG_TO_INT (ins->inst_offset));
			else
				code = emit_strfpq (code, sreg1, dreg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADX_MEMBASE:
			if (ins->klass && mono_class_value_size (ins->klass, NULL) == 8)
				code = emit_ldrfpx (code, dreg, sreg1, GTMREG_TO_INT (ins->inst_offset));
			else
				code = emit_ldrfpq (code, dreg, sreg1, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_XMOVE:
			if(dreg != sreg1)
				arm_neon_mov (code, dreg, sreg1);
			break;
		case OP_XCONST: {
			if (cfg->compile_aot && cfg->code_exec_only) {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_X128_GOT, ins->inst_p0);
				arm_ldrx_lit (code, ARMREG_IP0, 0);
				arm_ldrfpq (code, dreg, ARMREG_IP0, 0);
			} else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_X128, ins->inst_p0);
				arm_neon_ldrq_lit (code, dreg, 0);
			}
			break;
		}
		case OP_XCAST:
			break;
		case OP_EXPAND_I1:
		case OP_EXPAND_I2:
		case OP_EXPAND_I4:
		case OP_EXPAND_I8: {
			const int t = get_type_size_macro (ins->inst_c1);
			arm_neon_dup_g (code, VREG_FULL, t, dreg, sreg1);
			break;
		}
		case OP_EXPAND_R4:
		case OP_EXPAND_R8: {
			const int t = get_type_size_macro (ins->inst_c1);
			if (ins->opcode == OP_EXPAND_R8)
				arm_neon_fdup_e (code, VREG_FULL, t, dreg, sreg1, 0);
			else
				arm_neon_fdup_e (code, get_vector_size_macro (ins), t, dreg, sreg1, 0);
			break;
		}
		case OP_EXTRACT_I1:
		case OP_EXTRACT_I2:
		case OP_EXTRACT_I4:
		case OP_EXTRACT_I8: {
			const int t = get_type_size_macro (ins->inst_c1);
			// smov is not defined for i64
			if (is_type_unsigned_macro (ins->inst_c1) || t == TYPE_I64) {
				arm_neon_umov (code, t, dreg, sreg1, GTMREG_TO_UINT32 (ins->inst_c0));
			} else {
				arm_neon_smov (code, t, dreg, sreg1, GTMREG_TO_UINT32 (ins->inst_c0));
			}
			break;
		}
		case OP_EXTRACT_R4:
		case OP_EXTRACT_R8:
			if (ins->dreg != ins->sreg1 || ins->inst_c0 != 0) {
				const int t = get_type_size_macro (ins->inst_c1);
				// Technically, this broadcasts element #inst_c0 to all dest XREG elements; whereas it should
				// set the FREG to the said element. Since FREG and XREG pool is the same on arm64 and the rest
				// of the F/XREG is ignored in FREG mode, this operation remains valid.
				// FIXME: pass VREG_LOW for 64-bit vectors
				arm_neon_fdup_e (code, VREG_FULL, t, dreg, sreg1, GTMREG_TO_UINT32 (ins->inst_c0));
			}
			break;
		
		case OP_XEXTRACT_I8:
			code = emit_xextract_i8 (code, dreg, sreg1, sreg2);
			break;
		case OP_XEXTRACT_R8:
			code = emit_xextract_r8 (code, dreg, sreg1, sreg2);
			break;

		case OP_XEXTRACT_I1:
		case OP_XEXTRACT_I2:
		case OP_XEXTRACT_I4: 
		case OP_XEXTRACT_R4: 
			g_assert_not_reached ();
			break;

		case OP_XINSERT_I1:
		case OP_XINSERT_I2:
		case OP_XINSERT_I4:
		case OP_XINSERT_R4:
			g_assert_not_reached();
			break;
		case OP_XINSERT_I8:
			code = emit_xinsert_i8_r8 (code, MONO_TYPE_I8, dreg, sreg1, sreg2, sreg3);
			break;
		case OP_XINSERT_R8:
			code = emit_xinsert_i8_r8 (code, MONO_TYPE_R8, dreg, sreg1, sreg2, sreg3);
			break;

		case OP_INSERT_I1:
		case OP_INSERT_I2:
		case OP_INSERT_I4:
		case OP_INSERT_I8: {
			const int t = get_type_size_macro (ins->inst_c1);
			if (dreg != sreg1)
				arm_neon_mov (code, dreg, sreg1);

			arm_neon_ins_g(code, t, dreg, sreg2, GTMREG_TO_UINT32 (ins->inst_c0));
			break;
		}
		case OP_INSERT_R4:
		case OP_INSERT_R8: {
			int t = 0;
			switch (ins->inst_c1) {
			case MONO_TYPE_R4:
				t = SIZE_4;
				break;
			case MONO_TYPE_R8:
				t = SIZE_8;
				break;
			}

			int idx_to = GTMREG_TO_UINT32 (ins->inst_c0) & 0xff;
			int idx_from = GTMREG_TO_UINT32 (ins->inst_c0) >> 8;
			if (dreg != sreg1) {
				if (dreg != sreg2) {
					arm_neon_mov (code, dreg, sreg1);
					arm_neon_ins_e(code, t, dreg, sreg2, idx_to, idx_from);
				} else {
					arm_neon_mov (code, NEON_TMP_REG, sreg1);
					arm_neon_ins_e(code, t, NEON_TMP_REG, sreg2, idx_to, idx_from);
					arm_neon_mov (code, dreg, NEON_TMP_REG);
				}
			} else {
				g_assert (dreg != sreg2);
				arm_neon_ins_e(code, t, dreg, sreg2, idx_to, idx_from);
			}
			break;
		}
		case OP_ARM64_XTN:
			// The '-1' here and in XTN2 is to account for the arm_neon_xtn macro defining
			// its type as the type of the destination. Here inst_c1 is the type of the 
			// source data. Since XTN(2) steps down the type by one; e.g. I4 to I2, we 
			// subtract unity.
			arm_neon_xtn (code, get_type_size_macro (ins->inst_c1) - 1, dreg, sreg1);
			break;

		case OP_ARM64_XTN2: 
			g_assert (dreg == sreg1);
			arm_neon_xtn2 (code, get_type_size_macro (ins->inst_c1) - 1, dreg, sreg2);
			break;

		case OP_ARM64_FCVTN:
			// Only double->float is supported here, while arm64 can also do float->half.
			arm_neon_fcvtn (code, dreg, sreg1);
			break;

		case OP_ARM64_FCVTN2:
			g_assert (dreg == sreg1);
			arm_neon_fcvtn2 (code, dreg, sreg2); 
			break;

		case OP_ARM64_XADDV: {
			switch (ins->inst_c0) {
			case INTRINS_AARCH64_ADV_SIMD_FADDV: {
				const int width = get_vector_size_macro (ins);
				if (ins->inst_c1 == MONO_TYPE_R8) {
					arm_neon_faddp (code, VREG_FULL, TYPE_F64, dreg, sreg1, sreg1);
				} else if (ins->inst_c1 == MONO_TYPE_R4) {
					arm_neon_faddp (code, width, TYPE_F32, dreg, sreg1, sreg1);
					if (width == VREG_FULL)
						arm_neon_faddp (code, width, TYPE_F32, dreg, dreg, dreg);
				} else {
					g_assert_not_reached ();
				} 
				break;
			}
			case INTRINS_AARCH64_ADV_SIMD_UADDV:
			case INTRINS_AARCH64_ADV_SIMD_SADDV: 
				if (get_type_size_macro (ins->inst_c1) == TYPE_I64) 
					arm_neon_addp (code, VREG_FULL, TYPE_I64, dreg, sreg1, sreg1);
				else
					g_assert_not_reached (); // remaining int types are handled through the codegen table
				break;

			default:
				g_assert_not_reached ();
			}
			break;
		}
		case OP_CREATE_SCALAR_INT: {
			const int t = get_type_size_macro (ins->inst_c1);
			arm_neon_movi_b (code, VREG_FULL, dreg, 0);
			arm_neon_ins_g(code, t, dreg, sreg1, 0);
			break;
		}
		case OP_CREATE_SCALAR_FLOAT: {
			int t = 0;
			switch (ins->inst_c1) {
			case MONO_TYPE_R4:
				t = SIZE_4;
				break;
			case MONO_TYPE_R8:
				t = SIZE_8;
				break;
			}
			// Use a temp register for zero op, as sreg1 and dreg share the same register here
			arm_neon_movi_b (code, VREG_FULL, NEON_TMP_REG, 0);
			arm_neon_ins_e(code, t, NEON_TMP_REG, sreg1, 0, 0);
			arm_neon_mov (code, dreg, NEON_TMP_REG);
			break;
		}
		case OP_CREATE_SCALAR_UNSAFE_INT: {
			const int t = get_type_size_macro (ins->inst_c1);
			arm_neon_ins_g (code, t, dreg, sreg1, 0);
			break;
		}
		case OP_CREATE_SCALAR_UNSAFE_FLOAT: {
			if (dreg != sreg1) {
				int t = 0;
				switch (ins->inst_c1) {
				case MONO_TYPE_R4:
					t = SIZE_4;
					break;
				case MONO_TYPE_R8:
					t = SIZE_8;
					break;
				}
				arm_neon_ins_e (code, t, dreg, sreg1, 0, 0);
			}
			break;
		}
		case OP_XCONCAT: {
			if (dreg != sreg1)
				arm_neon_mov (code, dreg, sreg1);

			arm_neon_ins_e (code, SIZE_8, dreg, sreg2, 1, 0); 
		 	break;
		}
		case OP_XLOWER: {
			if (dreg == sreg1) {
				// clean the upper half
				arm_neon_movi_b (code, VREG_FULL, NEON_TMP_REG, 0);
				arm_neon_ins_e (code, SIZE_8, dreg, NEON_TMP_REG, 1, 0); 
			} else {
				arm_neon_movi_b (code, VREG_FULL, dreg, 0);
				arm_neon_mov_8b (code, dreg, sreg1);
			}
			break;
		}
		case OP_XUPPER:
			// shift in 64 zeros from the left
			arm_neon_movi_b (code, VREG_FULL, NEON_TMP_REG, 0);
			arm_neon_ext_16b (code, dreg, sreg1, NEON_TMP_REG, 8);
			break;
	
		case OP_XINSERT_LOWER:
		case OP_XINSERT_UPPER: {
			if (dreg != sreg1)
				arm_neon_mov (code, dreg, sreg1);

			int insert_at = (ins->opcode == OP_XINSERT_LOWER) ? 0 : 1;
			arm_neon_ins_e (code, SIZE_8, dreg, sreg2, insert_at, 0);
			break;
		}

		/* BRANCH */
		case OP_BR:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_ARM64_B);
			arm_b (code, code);
			break;
		case OP_BR_REG:
			arm_brx (code, sreg1);
			break;
		case OP_IBEQ:
		case OP_IBGE:
		case OP_IBGT:
		case OP_IBLE:
		case OP_IBLT:
		case OP_IBNE_UN:
		case OP_IBGE_UN:
		case OP_IBGT_UN:
		case OP_IBLE_UN:
		case OP_IBLT_UN:
		case OP_LBEQ:
		case OP_LBGE:
		case OP_LBGT:
		case OP_LBLE:
		case OP_LBLT:
		case OP_LBNE_UN:
		case OP_LBGE_UN:
		case OP_LBGT_UN:
		case OP_LBLE_UN:
		case OP_LBLT_UN:
		case OP_FBEQ:
		case OP_FBNE_UN:
		case OP_FBLT:
		case OP_FBGT:
		case OP_FBGT_UN:
		case OP_FBLE:
		case OP_FBGE:
		case OP_FBGE_UN: {
			int cond;

			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_BCC);
			cond = opcode_to_armcond (ins->opcode);
			arm_bcc (code, cond, 0);
			break;
		}
		case OP_FBLT_UN:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_BCC);
			/* For fp compares, ARMCOND_LT is lt or unordered */
			arm_bcc (code, ARMCOND_LT, 0);
			break;
		case OP_FBLE_UN:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_BCC);
			arm_bcc (code, ARMCOND_EQ, 0);
			mono_add_patch_info_rel (cfg, GPTRDIFF_TO_INT (code - cfg->native_code), MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_BCC);
			/* For fp compares, ARMCOND_LT is lt or unordered */
			arm_bcc (code, ARMCOND_LT, 0);
			break;
		case OP_ARM64_CBZW:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_CBZ);
			arm_cbzw (code, sreg1, 0);
			break;
		case OP_ARM64_CBZX:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_CBZ);
			arm_cbzx (code, sreg1, 0);
			break;
		case OP_ARM64_CBNZW:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_CBZ);
			arm_cbnzw (code, sreg1, 0);
			break;
		case OP_ARM64_CBNZX:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_true_bb, MONO_R_ARM64_CBZ);
			arm_cbnzx (code, sreg1, 0);
			break;

			/* ALU */
		case OP_IADD:
			arm_addw (code, dreg, sreg1, sreg2);
			break;
		case OP_LADD:
			arm_addx (code, dreg, sreg1, sreg2);
			break;
		case OP_ISUB:
			arm_subw (code, dreg, sreg1, sreg2);
			break;
		case OP_LSUB:
			arm_subx (code, dreg, sreg1, sreg2);
			break;
		case OP_IAND:
			arm_andw (code, dreg, sreg1, sreg2);
			break;
		case OP_LAND:
			arm_andx (code, dreg, sreg1, sreg2);
			break;
		case OP_IOR:
			arm_orrw (code, dreg, sreg1, sreg2);
			break;
		case OP_LOR:
			arm_orrx (code, dreg, sreg1, sreg2);
			break;
		case OP_IXOR:
			arm_eorw (code, dreg, sreg1, sreg2);
			break;
		case OP_LXOR:
			arm_eorx (code, dreg, sreg1, sreg2);
			break;
		case OP_INEG:
			arm_negw (code, dreg, sreg1);
			break;
		case OP_LNEG:
			arm_negx (code, dreg, sreg1);
			break;
		case OP_INOT:
			arm_mvnw (code, dreg, sreg1);
			break;
		case OP_LNOT:
			arm_mvnx (code, dreg, sreg1);
			break;
		case OP_IADDCC:
			arm_addsw (code, dreg, sreg1, sreg2);
			break;
		case OP_ADDCC:
		case OP_LADDCC:
			arm_addsx (code, dreg, sreg1, sreg2);
			break;
		case OP_ISUBCC:
			arm_subsw (code, dreg, sreg1, sreg2);
			break;
		case OP_LSUBCC:
		case OP_SUBCC:
			arm_subsx (code, dreg, sreg1, sreg2);
			break;
		case OP_ICOMPARE:
			arm_cmpw (code, sreg1, sreg2);
			break;
		case OP_COMPARE:
		case OP_LCOMPARE:
			arm_cmpx (code, sreg1, sreg2);
			break;
		case OP_IADD_IMM:
			code = emit_addw_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_LADD_IMM:
		case OP_ADD_IMM:
			code = emit_addx_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_ISUB_IMM:
			code = emit_subw_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_LSUB_IMM:
			code = emit_subx_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_IAND_IMM:
			code = emit_andw_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_LAND_IMM:
		case OP_AND_IMM:
			code = emit_andx_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_IOR_IMM:
			code = emit_orrw_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_LOR_IMM:
			code = emit_orrx_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_IXOR_IMM:
			code = emit_eorw_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_LXOR_IMM:
			code = emit_eorx_imm (code, dreg, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_ICOMPARE_IMM:
			code = emit_cmpw_imm (code, sreg1, GTMREG_TO_INT (imm));
			break;
		case OP_LCOMPARE_IMM:
		case OP_COMPARE_IMM:
			if (imm == 0) {
				arm_cmpx (code, sreg1, ARMREG_RZR);
			} else {
				// FIXME: 32 vs 64 bit issues for 0xffffffff
				code = emit_imm64 (code, ARMREG_LR, imm);
				arm_cmpx (code, sreg1, ARMREG_LR);
			}
			break;
		case OP_ISHL:
			arm_lslvw (code, dreg, sreg1, sreg2);
			break;
		case OP_LSHL:
			arm_lslvx (code, dreg, sreg1, sreg2);
			break;
		case OP_ISHR:
			arm_asrvw (code, dreg, sreg1, sreg2);
			break;
		case OP_LSHR:
			arm_asrvx (code, dreg, sreg1, sreg2);
			break;
		case OP_ISHR_UN:
			arm_lsrvw (code, dreg, sreg1, sreg2);
			break;
		case OP_LSHR_UN:
			arm_lsrvx (code, dreg, sreg1, sreg2);
			break;
		case OP_ISHL_IMM:
			if (imm == 0)
				arm_movx (code, dreg, sreg1);
			else
				arm_lslw (code, dreg, sreg1, imm);
			break;
		case OP_SHL_IMM:
		case OP_LSHL_IMM:
			if (imm == 0)
				arm_movx (code, dreg, sreg1);
			else
				arm_lslx (code, dreg, sreg1, imm);
			break;
		case OP_ISHR_IMM:
			if (imm == 0)
				arm_movx (code, dreg, sreg1);
			else
				arm_asrw (code, dreg, sreg1, GTMREG_TO_UINT32 (imm));
			break;
		case OP_LSHR_IMM:
		case OP_SHR_IMM:
			if (imm == 0)
				arm_movx (code, dreg, sreg1);
			else
				arm_asrx (code, dreg, sreg1, GTMREG_TO_UINT32 (imm));
			break;
		case OP_ISHR_UN_IMM:
			if (imm == 0)
				arm_movx (code, dreg, sreg1);
			else
				arm_lsrw (code, dreg, sreg1, GTMREG_TO_UINT32 (imm));
			break;
		case OP_SHR_UN_IMM:
		case OP_LSHR_UN_IMM:
			if (imm == 0)
				arm_movx (code, dreg, sreg1);
			else
				arm_lsrx (code, dreg, sreg1, GTMREG_TO_UINT32 (imm));
			break;

			/* 64BIT ALU */
		case OP_SEXT_I4:
			arm_sxtwx (code, dreg, sreg1);
			break;
		case OP_ZEXT_I4:
			/* Clean out the upper word */
			arm_movw (code, dreg, sreg1);
			break;

			/* MULTIPLY/DIVISION */
		case OP_IDIV:
		case OP_IREM:
			// FIXME: Optimize this
			/* Check for zero */
			arm_cmpx_imm (code, sreg2, 0);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "DivideByZeroException");
			/* Check for INT_MIN/-1 */
			code = emit_imm (code, ARMREG_IP0, 0x80000000);
			arm_cmpx (code, sreg1, ARMREG_IP0);
			arm_cset (code, ARMCOND_EQ, ARMREG_IP1);
			code = emit_imm (code, ARMREG_IP0, 0xffffffff);
			arm_cmpx (code, sreg2, ARMREG_IP0);
			arm_cset (code, ARMCOND_EQ, ARMREG_IP0);
			arm_andx (code, ARMREG_IP0, ARMREG_IP0, ARMREG_IP1);
			arm_cmpx_imm (code, ARMREG_IP0, 1);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "OverflowException");
			if (ins->opcode == OP_IREM) {
				arm_sdivw (code, ARMREG_LR, sreg1, sreg2);
				arm_msubw (code, dreg, ARMREG_LR, sreg2, sreg1);
			} else {
				arm_sdivw (code, dreg, sreg1, sreg2);
			}
			break;
		case OP_IDIV_UN:
			arm_cmpx_imm (code, sreg2, 0);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "DivideByZeroException");
			arm_udivw (code, dreg, sreg1, sreg2);
			break;
		case OP_IREM_UN:
			arm_cmpx_imm (code, sreg2, 0);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "DivideByZeroException");
			arm_udivw (code, ARMREG_LR, sreg1, sreg2);
			arm_msubw (code, dreg, ARMREG_LR, sreg2, sreg1);
			break;
		case OP_LDIV:
		case OP_LREM:
			// FIXME: Optimize this
			/* Check for zero */
			arm_cmpx_imm (code, sreg2, 0);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "DivideByZeroException");
			/* Check for INT64_MIN/-1 */
			code = emit_imm64 (code, ARMREG_IP0, 0x8000000000000000);
			arm_cmpx (code, sreg1, ARMREG_IP0);
			arm_cset (code, ARMCOND_EQ, ARMREG_IP1);
			code = emit_imm64 (code, ARMREG_IP0, 0xffffffffffffffff);
			arm_cmpx (code, sreg2, ARMREG_IP0);
			arm_cset (code, ARMCOND_EQ, ARMREG_IP0);
			arm_andx (code, ARMREG_IP0, ARMREG_IP0, ARMREG_IP1);
			arm_cmpx_imm (code, ARMREG_IP0, 1);
			/* 64 bit uses OverflowException */
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "OverflowException");
			if (ins->opcode == OP_LREM) {
				arm_sdivx (code, ARMREG_LR, sreg1, sreg2);
				arm_msubx (code, dreg, ARMREG_LR, sreg2, sreg1);
			} else {
				arm_sdivx (code, dreg, sreg1, sreg2);
			}
			break;
		case OP_LDIV_UN:
			arm_cmpx_imm (code, sreg2, 0);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "DivideByZeroException");
			arm_udivx (code, dreg, sreg1, sreg2);
			break;
		case OP_LREM_UN:
			arm_cmpx_imm (code, sreg2, 0);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_IEQ, "DivideByZeroException");
			arm_udivx (code, ARMREG_LR, sreg1, sreg2);
			arm_msubx (code, dreg, ARMREG_LR, sreg2, sreg1);
			break;
		case OP_IMUL:
			arm_mulw (code, dreg, sreg1, sreg2);
			break;
		case OP_LMUL:
			arm_mulx (code, dreg, sreg1, sreg2);
			break;
		case OP_IMUL_IMM:
			code = emit_imm (code, ARMREG_LR, GTMREG_TO_INT (imm));
			arm_mulw (code, dreg, sreg1, ARMREG_LR);
			break;
		case OP_MUL_IMM:
		case OP_LMUL_IMM:
			code = emit_imm (code, ARMREG_LR, GTMREG_TO_INT (imm));
			arm_mulx (code, dreg, sreg1, ARMREG_LR);
			break;

			/* CONVERSIONS */
		case OP_ICONV_TO_I1:
		case OP_LCONV_TO_I1:
			arm_sxtbx (code, dreg, sreg1);
			break;
		case OP_ICONV_TO_I2:
		case OP_LCONV_TO_I2:
			arm_sxthx (code, dreg, sreg1);
			break;
		case OP_ICONV_TO_U1:
		case OP_LCONV_TO_U1:
			arm_uxtbw (code, dreg, sreg1);
			break;
		case OP_ICONV_TO_U2:
		case OP_LCONV_TO_U2:
			arm_uxthw (code, dreg, sreg1);
			break;

			/* CSET */
		case OP_CEQ:
		case OP_ICEQ:
		case OP_LCEQ:
		case OP_CLT:
		case OP_ICLT:
		case OP_LCLT:
		case OP_CGT:
		case OP_ICGT:
		case OP_LCGT:
		case OP_CLT_UN:
		case OP_ICLT_UN:
		case OP_LCLT_UN:
		case OP_CGT_UN:
		case OP_ICGT_UN:
		case OP_LCGT_UN:
		case OP_ICNEQ:
		case OP_ICGE:
		case OP_ICLE:
		case OP_ICGE_UN:
		case OP_ICLE_UN: {
			int cond;

			cond = opcode_to_armcond (ins->opcode);
			arm_cset (code, cond, dreg);
			break;
		}
		case OP_FCEQ:
		case OP_FCLT:
		case OP_FCLT_UN:
		case OP_FCGT:
		case OP_FCGT_UN:
		case OP_FCNEQ:
		case OP_FCLE:
		case OP_FCGE: {
			int cond;

			cond = opcode_to_armcond (ins->opcode);
			arm_fcmpd (code, sreg1, sreg2);
			arm_cset (code, cond, dreg);
			break;
		}

			/* MEMORY */
		case OP_LOADI1_MEMBASE:
			code = emit_ldrsbx (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADU1_MEMBASE:
			code = emit_ldrb (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADI2_MEMBASE:
			code = emit_ldrshx (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADU2_MEMBASE:
			code = emit_ldrh (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADI4_MEMBASE:
			code = emit_ldrswx (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADU4_MEMBASE:
			code = emit_ldrw (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE:
			code = emit_ldrx (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM: {
			int immreg;

			if (imm != 0) {
				code = emit_imm (code, ARMREG_LR, GTMREG_TO_INT (imm));
				immreg = ARMREG_LR;
			} else {
				immreg = ARMREG_RZR;
			}

			switch (ins->opcode) {
			case OP_STOREI1_MEMBASE_IMM:
				code = emit_strb (code, immreg, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			case OP_STOREI2_MEMBASE_IMM:
				code = emit_strh (code, immreg, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			case OP_STOREI4_MEMBASE_IMM:
				code = emit_strw (code, immreg, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			case OP_STORE_MEMBASE_IMM:
			case OP_STOREI8_MEMBASE_IMM:
				code = emit_strx (code, immreg, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		}
		case OP_STOREI1_MEMBASE_REG:
			code = emit_strb (code, sreg1, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_STOREI2_MEMBASE_REG:
			code = emit_strh (code, sreg1, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_STOREI4_MEMBASE_REG:
			code = emit_strw (code, sreg1, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI8_MEMBASE_REG:
			code = emit_strx (code, sreg1, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_TLS_GET:
			code = emit_tls_get (code, dreg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_TLS_SET:
			code = emit_tls_set (code, sreg1, GTMREG_TO_INT (ins->inst_offset));
			break;
			/* Atomic */
		case OP_MEMORY_BARRIER:
			arm_dmb (code, ARM_DMB_ISH);
			break;
		case OP_ATOMIC_ADD_I4: {
			guint8 *buf [16];

			buf [0] = code;
			arm_ldxrw (code, ARMREG_IP0, sreg1);
			arm_addx (code, ARMREG_IP0, ARMREG_IP0, sreg2);
			arm_stlxrw (code, ARMREG_IP1, ARMREG_IP0, sreg1);
			arm_cbnzw (code, ARMREG_IP1, buf [0]);

			arm_dmb (code, ARM_DMB_ISH);
			arm_movx (code, dreg, ARMREG_IP0);
			break;
		}
		case OP_ATOMIC_ADD_I8: {
			guint8 *buf [16];

			buf [0] = code;
			arm_ldxrx (code, ARMREG_IP0, sreg1);
			arm_addx (code, ARMREG_IP0, ARMREG_IP0, sreg2);
			arm_stlxrx (code, ARMREG_IP1, ARMREG_IP0, sreg1);
			arm_cbnzx (code, ARMREG_IP1, buf [0]);

			arm_dmb (code, ARM_DMB_ISH);
			arm_movx (code, dreg, ARMREG_IP0);
			break;
		}
		case OP_ATOMIC_EXCHANGE_I4: {
			guint8 *buf [16];

			buf [0] = code;
			arm_ldxrw (code, ARMREG_IP0, sreg1);
			arm_stlxrw (code, ARMREG_IP1, sreg2, sreg1);
			arm_cbnzw (code, ARMREG_IP1, buf [0]);

			arm_dmb (code, ARM_DMB_ISH);
			arm_movx (code, dreg, ARMREG_IP0);
			break;
		}
		case OP_ATOMIC_EXCHANGE_I8: {
			guint8 *buf [16];

			buf [0] = code;
			arm_ldxrx (code, ARMREG_IP0, sreg1);
			arm_stlxrx (code, ARMREG_IP1, sreg2, sreg1);
			arm_cbnzw (code, ARMREG_IP1, buf [0]);

			arm_dmb (code, ARM_DMB_ISH);
			arm_movx (code, dreg, ARMREG_IP0);
			break;
		}
		case OP_ATOMIC_CAS_I4: {
			guint8 *buf [16];

			/* sreg2 is the value, sreg3 is the comparand */
			buf [0] = code;
			arm_ldxrw (code, ARMREG_IP0, sreg1);
			arm_cmpw (code, ARMREG_IP0, ins->sreg3);
			buf [1] = code;
			arm_bcc (code, ARMCOND_NE, 0);
			arm_stlxrw (code, ARMREG_IP1, sreg2, sreg1);
			arm_cbnzw (code, ARMREG_IP1, buf [0]);
			arm_patch_rel (buf [1], code, MONO_R_ARM64_BCC);

			arm_dmb (code, ARM_DMB_ISH);
			arm_movx (code, dreg, ARMREG_IP0);
			break;
		}
		case OP_ATOMIC_CAS_I8: {
			guint8 *buf [16];

			buf [0] = code;
			arm_ldxrx (code, ARMREG_IP0, sreg1);
			arm_cmpx (code, ARMREG_IP0, ins->sreg3);
			buf [1] = code;
			arm_bcc (code, ARMCOND_NE, 0);
			arm_stlxrx (code, ARMREG_IP1, sreg2, sreg1);
			arm_cbnzw (code, ARMREG_IP1, buf [0]);
			arm_patch_rel (buf [1], code, MONO_R_ARM64_BCC);

			arm_dmb (code, ARM_DMB_ISH);
			arm_movx (code, dreg, ARMREG_IP0);
			break;
		}
		case OP_ATOMIC_LOAD_I1: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarb (code, ins->dreg, ARMREG_LR);
			arm_sxtbx (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_ATOMIC_LOAD_U1: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarb (code, ins->dreg, ARMREG_LR);
			arm_uxtbx (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_ATOMIC_LOAD_I2: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarh (code, ins->dreg, ARMREG_LR);
			arm_sxthx (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_ATOMIC_LOAD_U2: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarh (code, ins->dreg, ARMREG_LR);
			arm_uxthx (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_ATOMIC_LOAD_I4: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarw (code, ins->dreg, ARMREG_LR);
			arm_sxtwx (code, ins->dreg, ins->dreg);
			break;
		}
		case OP_ATOMIC_LOAD_U4: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarw (code, ins->dreg, ARMREG_LR);
			arm_movw (code, ins->dreg, ins->dreg); /* Clear upper half of the register. */
			break;
		}
		case OP_ATOMIC_LOAD_I8:
		case OP_ATOMIC_LOAD_U8: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarx (code, ins->dreg, ARMREG_LR);
			break;
		}
		case OP_ATOMIC_LOAD_R4: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarw (code, ARMREG_LR, ARMREG_LR);
			arm_fmov_rx_to_double (code, ins->dreg, ARMREG_LR);
			break;
		}
		case OP_ATOMIC_LOAD_R8: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			arm_ldarx (code, ARMREG_LR, ARMREG_LR);
			arm_fmov_rx_to_double (code, ins->dreg, ARMREG_LR);
			break;
		}
		case OP_ATOMIC_STORE_I1:
		case OP_ATOMIC_STORE_U1: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			arm_stlrb (code, ARMREG_LR, ins->sreg1);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			break;
		}
		case OP_ATOMIC_STORE_I2:
		case OP_ATOMIC_STORE_U2: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			arm_stlrh (code, ARMREG_LR, ins->sreg1);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			break;
		}
		case OP_ATOMIC_STORE_I4:
		case OP_ATOMIC_STORE_U4: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			arm_stlrw (code, ARMREG_LR, ins->sreg1);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			break;
		}
		case OP_ATOMIC_STORE_I8:
		case OP_ATOMIC_STORE_U8: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			arm_stlrx (code, ARMREG_LR, ins->sreg1);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			break;
		}
		case OP_ATOMIC_STORE_R4: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			arm_fmov_double_to_rx (code, ARMREG_IP0, ins->sreg1);
			arm_stlrw (code, ARMREG_LR, ARMREG_IP0);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			break;
		}
		case OP_ATOMIC_STORE_R8: {
			code = emit_addx_imm (code, ARMREG_LR, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			arm_fmov_double_to_rx (code, ARMREG_IP0, ins->sreg1);
			arm_stlrx (code, ARMREG_LR, ARMREG_IP0);
			if (ins->backend.memory_barrier_kind == MONO_MEMORY_BARRIER_SEQ)
				arm_dmb (code, ARM_DMB_ISH);
			break;
		}

			/* FP */
		case OP_R4CONST:
		case OP_R8CONST: {
			gboolean is_double = (ins->opcode == OP_R8CONST);
			guint64 r_imm = is_double ? *(guint64*)ins->inst_p0 : *(guint32*)ins->inst_p0;

			if (r_imm == 0) {
				arm_fmov_rx_to_double (code, dreg, ARMREG_RZR);
			} else {
				guint64 mask_constant = is_double ? 0x0000FFFFFFFFFFFF : 0x0007FFFF;
				
				// Arm64 floating-point modified immediate constant check (2 * 128 combinations)
				// Float:  aBbbbbbc defgh000 00000000 00000000
				// Double: aBbbbbbb bbcdefgh 00000000 00000000 00000000 00000000 00000000 00000000
				
				// Trailing zeros check
				if ((r_imm & mask_constant) == 0) {
					// Mask for b
					guint8 mask_b;
					int idx_last_b;
					if (is_double) {
						mask_b = 0xFF;
						idx_last_b = 54;
					} else {
						mask_b = 0x1F;
						idx_last_b = 25;
					}
					guint8 masked_b = (r_imm & ((guint64)mask_b << idx_last_b)) >> idx_last_b;

					int size = is_double ? 64 : 32;
					// NOT(B) == b check
					if (((r_imm & ((guint64)1 << (size - 2))) && masked_b == 0)
					|| (!(r_imm & ((guint64)1 << (size - 2))) && masked_b == mask_b)) {						
						//imm8 = abcdefgh
						guint8 imm8 = ((r_imm & ((guint64)1 << (size - 1))) >> (size - 8))
									| ((r_imm & ((guint64)1 << idx_last_b)) >> (idx_last_b - 6))
									| ((r_imm & ((guint64)0x3F << (idx_last_b - 6))) >> (idx_last_b - 6));
						
						arm_fmov_imm(code, (is_double ? 0x01 : 0x00), imm8, dreg);
						break;
					} 
				} 
				// Regular floating-point constant
				code = emit_imm64 (code, ARMREG_LR, r_imm);
				arm_fmov_rx_to_double (code, dreg, ARMREG_LR);
			}
			break;
		}
		case OP_LOADR8_MEMBASE:
			code = emit_ldrfpx (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_LOADR4_MEMBASE:
			code = emit_ldrfpw (code, dreg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_STORER8_MEMBASE_REG:
			code = emit_strfpx (code, sreg1, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_STORER4_MEMBASE_REG:
			code = emit_strfpw (code, sreg1, ins->inst_destbasereg, GTMREG_TO_INT (ins->inst_offset));
			break;
		case OP_FMOVE:
			if (dreg != sreg1)
				arm_fmovd (code, dreg, sreg1);
			break;
		case OP_RMOVE:
			if (dreg != sreg1)
				arm_fmovs (code, dreg, sreg1);
			break;
		case OP_MOVE_F_TO_I4:
			arm_fmov_double_to_rx (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_I4_TO_F:
			arm_fmov_rx_to_double (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_F_TO_I8:
			arm_fmov_double_to_rx (code, ins->dreg, ins->sreg1);
			break;
		case OP_MOVE_I8_TO_F:
			arm_fmov_rx_to_double (code, ins->dreg, ins->sreg1);
			break;
		case OP_FCOMPARE:
			arm_fcmpd (code, sreg1, sreg2);
			break;
		case OP_RCOMPARE:
			arm_fcmps (code, sreg1, sreg2);
			break;
		case OP_FCONV_TO_I1:
			arm_fcvtzs_dx (code, dreg, sreg1);
			arm_sxtbx (code, dreg, dreg);
			break;
		case OP_FCONV_TO_U1:
			arm_fcvtzu_dx (code, dreg, sreg1);
			arm_uxtbw (code, dreg, dreg);
			break;
		case OP_FCONV_TO_I2:
			arm_fcvtzs_dx (code, dreg, sreg1);
			arm_sxthx (code, dreg, dreg);
			break;
		case OP_FCONV_TO_U2:
			arm_fcvtzu_dx (code, dreg, sreg1);
			arm_uxthw (code, dreg, dreg);
			break;
		case OP_FCONV_TO_I4:
			arm_fcvtzs_dw (code, dreg, sreg1);
			break;
		case OP_FCONV_TO_U4:
			arm_fcvtzu_dw (code, dreg, sreg1);
			break;
		case OP_FCONV_TO_I8:
			arm_fcvtzs_dx (code, dreg, sreg1);
			break;
		case OP_FCONV_TO_U8:
			arm_fcvtzu_dx (code, dreg, sreg1);
			break;
		case OP_FCONV_TO_R4:
			arm_fcvt_ds (code, dreg, sreg1);
			break;
		case OP_ICONV_TO_R4:
			arm_scvtf_rw_to_s (code, dreg, sreg1);
			break;
		case OP_LCONV_TO_R4:
			arm_scvtf_rx_to_s (code, dreg, sreg1);
			break;
		case OP_ICONV_TO_R8:
			arm_scvtf_rw_to_d (code, dreg, sreg1);
			break;
		case OP_LCONV_TO_R8:
			arm_scvtf_rx_to_d (code, dreg, sreg1);
			break;
		case OP_ICONV_TO_R_UN:
			arm_ucvtf_rw_to_d (code, dreg, sreg1);
			break;
		case OP_LCONV_TO_R_UN:
			arm_ucvtf_rx_to_d (code, dreg, sreg1);
			break;
		case OP_FADD:
			arm_fadd_d (code, dreg, sreg1, sreg2);
			break;
		case OP_FSUB:
			arm_fsub_d (code, dreg, sreg1, sreg2);
			break;
		case OP_FMUL:
			arm_fmul_d (code, dreg, sreg1, sreg2);
			break;
		case OP_FDIV:
			arm_fdiv_d (code, dreg, sreg1, sreg2);
			break;
		case OP_FREM:
			/* Emulated */
			g_assert_not_reached ();
			break;
		case OP_FNEG:
			arm_fneg_d (code, dreg, sreg1);
			break;
		case OP_ARM_SETFREG_R4:
			arm_fcvt_ds (code, dreg, sreg1);
			break;
		case OP_CKFINITE:
			/* Check for infinity */
			code = emit_imm64 (code, ARMREG_LR, 0x7fefffffffffffffLL);
			arm_fmov_rx_to_double (code, FP_TEMP_REG, ARMREG_LR);
			arm_fabs_d (code, FP_TEMP_REG2, sreg1);
			arm_fcmpd (code, FP_TEMP_REG2, FP_TEMP_REG);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_GT, "ArithmeticException");
			/* Check for nans */
			arm_fcmpd (code, FP_TEMP_REG2, FP_TEMP_REG2);
			code = emit_cond_exc (cfg, code, OP_COND_EXC_OV, "ArithmeticException");
			arm_fmovd (code, dreg, sreg1);
			break;

			/* R4 */
		case OP_RADD:
			arm_fadd_s (code, dreg, sreg1, sreg2);
			break;
		case OP_RSUB:
			arm_fsub_s (code, dreg, sreg1, sreg2);
			break;
		case OP_RMUL:
			arm_fmul_s (code, dreg, sreg1, sreg2);
			break;
		case OP_RDIV:
			arm_fdiv_s (code, dreg, sreg1, sreg2);
			break;
		case OP_RNEG:
			arm_fneg_s (code, dreg, sreg1);
			break;
		case OP_RCONV_TO_I1:
			arm_fcvtzs_sx (code, dreg, sreg1);
			arm_sxtbx (code, dreg, dreg);
			break;
		case OP_RCONV_TO_U1:
			arm_fcvtzu_sx (code, dreg, sreg1);
			arm_uxtbw (code, dreg, dreg);
			break;
		case OP_RCONV_TO_I2:
			arm_fcvtzs_sx (code, dreg, sreg1);
			arm_sxthx (code, dreg, dreg);
			break;
		case OP_RCONV_TO_U2:
			arm_fcvtzu_sx (code, dreg, sreg1);
			arm_uxthw (code, dreg, dreg);
			break;
		case OP_RCONV_TO_I4:
			arm_fcvtzs_sw (code, dreg, sreg1);
			break;
		case OP_RCONV_TO_U4:
			arm_fcvtzu_sw (code, dreg, sreg1);
			break;
		case OP_RCONV_TO_I8:
			arm_fcvtzs_sx (code, dreg, sreg1);
			break;
		case OP_RCONV_TO_U8:
			arm_fcvtzu_sx (code, dreg, sreg1);
			break;
		case OP_RCONV_TO_R8:
			arm_fcvt_sd (code, dreg, sreg1);
			break;
		case OP_RCONV_TO_R4:
			if (dreg != sreg1)
				arm_fmovs (code, dreg, sreg1);
			break;
		case OP_RCEQ:
		case OP_RCLT:
		case OP_RCLT_UN:
		case OP_RCGT:
		case OP_RCGT_UN:
		case OP_RCNEQ:
		case OP_RCLE:
		case OP_RCGE: {
			int cond;

			cond = opcode_to_armcond (ins->opcode);
			arm_fcmps (code, sreg1, sreg2);
			arm_cset (code, cond, dreg);
			break;
		}

			/* CALLS */
		case OP_VOIDCALL:
		case OP_CALL:
		case OP_LCALL:
		case OP_FCALL:
		case OP_RCALL:
		case OP_VCALL2: {

			call = (MonoCallInst*)ins;
			const MonoJumpInfoTarget patch = mono_call_to_patch (call);
			code = emit_call (cfg, code, patch.type, patch.target);
			code = emit_move_return_value (cfg, code, ins);
			break;
		}
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
		case OP_LCALL_REG:
		case OP_FCALL_REG:
		case OP_RCALL_REG:
		case OP_VCALL2_REG:
			code = mono_arm_emit_blrx (code, sreg1);
			code = emit_move_return_value (cfg, code, ins);
			break;
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_FCALL_MEMBASE:
		case OP_RCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
			code = emit_ldrx (code, ARMREG_IP0, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
			code = mono_arm_emit_blrx (code, ARMREG_IP0);
			code = emit_move_return_value (cfg, code, ins);
			break;

		case OP_TAILCALL_PARAMETER:
			// This opcode helps compute sizes, i.e.
			// of the subsequent OP_TAILCALL, but contributes no code.
			g_assert (ins->next);
			break;

		case OP_TAILCALL:
		case OP_TAILCALL_MEMBASE:
		case OP_TAILCALL_REG: {
			int branch_reg = ARMREG_IP0;
			guint64 free_reg = 1 << ARMREG_IP1;
			call = (MonoCallInst*)ins;

			g_assert (!cfg->method->save_lmf);

			max_len += call->stack_usage / sizeof (target_mgreg_t) * ins_get_size (OP_TAILCALL_PARAMETER);
			while (G_UNLIKELY (offset + max_len > cfg->code_size)) {
				cfg->code_size *= 2;
				cfg->native_code = (unsigned char *)mono_realloc_native_code (cfg);
				code = cfg->native_code + offset;
				cfg->stat_code_reallocs++;
			}

			switch (ins->opcode) {
			case OP_TAILCALL:
				free_reg = (1 << ARMREG_IP0) | (1 << ARMREG_IP1);
				break;

			case OP_TAILCALL_REG:
				g_assert (sreg1 != -1);
				g_assert (sreg1 != ARMREG_IP0);
				g_assert (sreg1 != ARMREG_IP1);
				g_assert (sreg1 != ARMREG_LR);
				g_assert (sreg1 != ARMREG_SP);
				g_assert (sreg1 != ARMREG_R28);
				if ((sreg1 << 1) & MONO_ARCH_CALLEE_SAVED_REGS) {
					arm_movx (code, branch_reg, sreg1);
				} else {
					free_reg = (1 << ARMREG_IP0) | (1 << ARMREG_IP1);
					branch_reg = sreg1;
				}
				break;

			case OP_TAILCALL_MEMBASE:
				g_assert (ins->inst_basereg != -1);
				g_assert (ins->inst_basereg != ARMREG_IP0);
				g_assert (ins->inst_basereg != ARMREG_IP1);
				g_assert (ins->inst_basereg != ARMREG_LR);
				g_assert (ins->inst_basereg != ARMREG_SP);
				g_assert (ins->inst_basereg != ARMREG_R28);
				code = emit_ldrx (code, branch_reg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
				break;

			default:
				g_assert_not_reached ();
			}

			// Copy stack arguments.
			// FIXME a fixed size memcpy is desirable here,
			// at least for larger values of stack_usage.
			for (guint i = 0; i < call->stack_usage; i += sizeof (target_mgreg_t)) {
				code = emit_ldrx (code, ARMREG_LR, ARMREG_SP, i);
				code = emit_strx (code, ARMREG_LR, ARMREG_R28, i);
			}

			/* Restore registers */
			code = emit_load_regset (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, ARMREG_FP, cfg->arch.saved_gregs_offset);

			/* Destroy frame */
			code = mono_arm_emit_destroy_frame (code, cfg->stack_offset, free_reg);

			if (enable_ptrauth)
				/* There is no retab to authenticate lr */
				arm_autibsp (code);

			switch (ins->opcode) {
			case OP_TAILCALL:
				if (cfg->compile_aot) {
					/* This is not a PLT patch */
					code = emit_aotconst (cfg, code, branch_reg, MONO_PATCH_INFO_METHOD_JUMP, call->method);
				} else {
					mono_add_patch_info_rel (cfg, GPTRDIFF_TO_INT (code - cfg->native_code), MONO_PATCH_INFO_METHOD_JUMP, call->method, MONO_R_ARM64_B);
					arm_b (code, code);
					cfg->thunk_area += THUNK_SIZE;
					break;
				}
				// fallthrough
			case OP_TAILCALL_MEMBASE:
			case OP_TAILCALL_REG:
				code = mono_arm_emit_brx (code, branch_reg);
				break;

			default:
				g_assert_not_reached ();
			}

			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = GPTRDIFF_TO_INT (code - cfg->native_code);
			break;
		}
		case OP_ARGLIST:
			g_assert (cfg->arch.cinfo);
			code = emit_addx_imm (code, ARMREG_IP0, cfg->arch.args_reg, cfg->arch.cinfo->sig_cookie.offset);
			arm_strx (code, ARMREG_IP0, sreg1, 0);
			break;
		case OP_DYN_CALL: {
			MonoInst *var = cfg->dyn_call_var;
			guint8 *labels [16];
			int i;

			/*
			 * sreg1 points to a DynCallArgs structure initialized by mono_arch_start_dyn_call ().
			 * sreg2 is the function to call.
			 */

			g_assert (var->opcode == OP_REGOFFSET);

			arm_movx (code, ARMREG_LR, sreg1);
			arm_movx (code, ARMREG_IP1, sreg2);

			/* Save args buffer */
			code = emit_strx (code, ARMREG_LR, var->inst_basereg, GTMREG_TO_INT (var->inst_offset));

			/* Set fp argument regs */
			code = emit_ldrw (code, ARMREG_R0, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, n_fpargs));
			arm_cmpw (code, ARMREG_R0, ARMREG_RZR);
			labels [0] = code;
			arm_bcc (code, ARMCOND_EQ, 0);
			for (i = 0; i < 8; ++i)
				code = emit_ldrfpx (code, ARMREG_D0 + i, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, fpregs) + (i * 8));
			arm_patch_rel (labels [0], code, MONO_R_ARM64_BCC);

			/* Allocate callee area */
			code = emit_ldrx (code, ARMREG_R0, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, n_stackargs));
			arm_lslw (code, ARMREG_R0, ARMREG_R0, 3);
			arm_movspx (code, ARMREG_R1, ARMREG_SP);
			arm_subx (code, ARMREG_R1, ARMREG_R1, ARMREG_R0);
			arm_movspx (code, ARMREG_SP, ARMREG_R1);

			/* Set stack args */
			/* R1 = limit */
			code = emit_ldrx (code, ARMREG_R1, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, n_stackargs));
			/* R2 = pointer into 'regs' */
			code = emit_imm (code, ARMREG_R2, MONO_STRUCT_OFFSET (DynCallArgs, regs) + ((PARAM_REGS + 1) * sizeof (target_mgreg_t)));
			arm_addx (code, ARMREG_R2, ARMREG_LR, ARMREG_R2);
			/* R3 = pointer to stack */
			arm_movspx (code, ARMREG_R3, ARMREG_SP);
			labels [0] = code;
			arm_b (code, code);
			labels [1] = code;
			code = emit_ldrx (code, ARMREG_R5, ARMREG_R2, 0);
			code = emit_strx (code, ARMREG_R5, ARMREG_R3, 0);
			code = emit_addx_imm (code, ARMREG_R2, ARMREG_R2, sizeof (target_mgreg_t));
			code = emit_addx_imm (code, ARMREG_R3, ARMREG_R3, sizeof (target_mgreg_t));
			code = emit_subx_imm (code, ARMREG_R1, ARMREG_R1, 1);
			arm_patch_rel (labels [0], code, MONO_R_ARM64_B);
			arm_cmpw (code, ARMREG_R1, ARMREG_RZR);
			arm_bcc (code, ARMCOND_GT, labels [1]);

			/* Set argument registers + r8 */
			code = mono_arm_emit_load_regarray (code, 0x1ff, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, regs));

			/* Make the call */
			code = mono_arm_emit_blrx (code, ARMREG_IP1);

			/* Save result */
			code = emit_ldrx (code, ARMREG_LR, var->inst_basereg, GTMREG_TO_INT (var->inst_offset));
			arm_strx (code, ARMREG_R0, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, res));
			arm_strx (code, ARMREG_R1, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, res2));
			/* Save fp result */
			code = emit_ldrw (code, ARMREG_R0, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, n_fpret));
			arm_cmpw (code, ARMREG_R0, ARMREG_RZR);
			labels [1] = code;
			arm_bcc (code, ARMCOND_EQ, 0);
			for (i = 0; i < 8; ++i)
				code = emit_strfpx (code, ARMREG_D0 + i, ARMREG_LR, MONO_STRUCT_OFFSET (DynCallArgs, fpregs) + (i * 8));
			arm_patch_rel (labels [1], code, MONO_R_ARM64_BCC);
			break;
		}

		case OP_GENERIC_CLASS_INIT: {
			int byte_offset;
			guint8 *jump;

			byte_offset = MONO_STRUCT_OFFSET (MonoVTable, initialized);

			/* Load vtable->initialized */
			arm_ldrsbx (code, ARMREG_IP0, sreg1, byte_offset);
			jump = code;
			arm_cbnzx (code, ARMREG_IP0, 0);

			/* Slowpath */
			g_assert (sreg1 == ARMREG_R0);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_generic_class_init));

			mono_arm_patch (jump, code, MONO_R_ARM64_CBZ);
			break;
		}
		case OP_INIT_MRGCTX: {
			int field_offset;
			guint8 *jump;

			field_offset = MONO_STRUCT_OFFSET (MonoMethodRuntimeGenericContext, entries);

			/* Load mrgxtx->entries */
			arm_ldrx (code, ARMREG_IP0, sreg1, field_offset);
			jump = code;
			arm_cbnzx (code, ARMREG_IP0, 0);

			/* Slowpath */
			g_assert (sreg1 == ARMREG_R0);
			if (sreg2 != ARMREG_R1)
				arm_movx (code, ARMREG_R1, sreg2);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mini_init_method_rgctx));

			mono_arm_patch (jump, code, MONO_R_ARM64_CBZ);
			break;
		}

		case OP_CHECK_THIS:
			arm_ldrb (code, ARMREG_LR, sreg1, 0);
			break;
		case OP_NOT_NULL:
		case OP_NOT_REACHED:
		case OP_DUMMY_USE:
		case OP_DUMMY_ICONST:
		case OP_DUMMY_I8CONST:
		case OP_DUMMY_R8CONST:
		case OP_DUMMY_R4CONST:
			break;
		case OP_IL_SEQ_POINT:
			mono_add_seq_point (cfg, bb, ins, GPTRDIFF_TO_INT (code - cfg->native_code));
			break;

			/* EH */
		case OP_COND_EXC_C:
		case OP_COND_EXC_IC:
		case OP_COND_EXC_OV:
		case OP_COND_EXC_IOV:
		case OP_COND_EXC_NC:
		case OP_COND_EXC_INC:
		case OP_COND_EXC_NO:
		case OP_COND_EXC_INO:
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_IEQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_INE_UN:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_ILT_UN:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_IGT_UN:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_IGE_UN:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_ILE_UN:
		case OP_COND_EXC_LE_UN:
			code = emit_cond_exc (cfg, code, ins->opcode, (const char*)ins->inst_p1);
			break;
		case OP_THROW:
			if (sreg1 != ARMREG_R0)
				arm_movx (code, ARMREG_R0, sreg1);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_throw_exception));
			break;
		case OP_RETHROW:
			if (sreg1 != ARMREG_R0)
				arm_movx (code, ARMREG_R0, sreg1);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID,
							  GUINT_TO_POINTER (MONO_JIT_ICALL_mono_arch_rethrow_exception));
			break;
		case OP_CALL_HANDLER:
			mono_add_patch_info_rel (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb, MONO_R_ARM64_BL);
			arm_bl (code, 0);
			cfg->thunk_area += THUNK_SIZE;
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
			break;
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			/* Save caller address */
			code = emit_strx (code, ARMREG_LR, spvar->inst_basereg, GTMREG_TO_INT (spvar->inst_offset));

			/*
			 * Reserve a param area, see test_0_finally_param_area ().
			 * This is needed because the param area is not set up when
			 * we are called from EH code.
			 */
			if (cfg->param_area)
				code = emit_subx_sp_imm (code, cfg->param_area);
			break;
		}
		case OP_ENDFINALLY:
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			if (cfg->param_area)
				code = emit_addx_sp_imm (code, cfg->param_area);

			if (ins->opcode == OP_ENDFILTER && sreg1 != ARMREG_R0)
				arm_movx (code, ARMREG_R0, sreg1);

			/* Return to either after the branch in OP_CALL_HANDLER, or to the EH code */
			code = emit_ldrx (code, ARMREG_LR, spvar->inst_basereg, GTMREG_TO_INT (spvar->inst_offset));
			arm_brx (code, ARMREG_LR);
			break;
		}
		case OP_GET_EX_OBJ:
			if (ins->dreg != ARMREG_R0)
				arm_movx (code, ins->dreg, ARMREG_R0);
			break;
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, GPTRDIFF_TO_INT (code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = GPTRDIFF_TO_INT32 (code - cfg->native_code);
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, GPTRDIFF_TO_INT (code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = GPTRDIFF_TO_INT32 (code - cfg->native_code);
			break;
		}
		case OP_GC_SAFE_POINT: {
			guint8 *buf [1];

			arm_ldrx (code, ARMREG_IP1, ins->sreg1, 0);
			/* Call it if it is non-null */
			buf [0] = code;
			arm_cbzx (code, ARMREG_IP1, 0);
			code = emit_call (cfg, code, MONO_PATCH_INFO_JIT_ICALL_ID, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_threads_state_poll));
			mono_arm_patch (buf [0], code, MONO_R_ARM64_CBZ);
			break;
		}
		case OP_FILL_PROF_CALL_CTX:
			for (int i = 0; i < MONO_MAX_IREGS; i++)
				if ((MONO_ARCH_CALLEE_SAVED_REGS & (1 << i)) || i == ARMREG_SP || i == ARMREG_FP)
					arm_strx (code, i, ins->sreg1, MONO_STRUCT_OFFSET (MonoContext, regs) + i * sizeof (target_mgreg_t));
			break;

		/**** Arm.ArmBase ****/
		case OP_LZCNT32:
			arm_clzw (code, dreg, sreg1);
			break;

		case OP_LSCNT32:
			arm_clsw (code, dreg, sreg1);
			break;

		case OP_LZCNT64:
			arm_clzx (code, dreg, sreg1);
			break;

		case OP_LSCNT64:
			arm_clsx (code, dreg, sreg1);
			break;

		case OP_ARM64_SMULH:
			arm_smulh (code, dreg, sreg1, sreg2);
			break;

		case OP_ARM64_UMULH:
			arm_umulh (code, dreg, sreg1, sreg2);
			break;

		case OP_XOP_I8_I8:
			g_assert (ins->inst_c0 == INTRINS_BITREVERSE_I64);
			arm_rbitx (code, dreg, sreg1);
			break;

		case OP_XOP_I4_I4:
			g_assert (ins->inst_c0 == INTRINS_BITREVERSE_I32);
			arm_rbitw (code, dreg, sreg1);
			break;
		case OP_XOP_I4_I4_I4: {
			switch (ins->inst_c0) {
			case INTRINS_AARCH64_CRC32B:
				arm_crc32b (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_CRC32H:
				arm_crc32h (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_CRC32W:
				arm_crc32w (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_CRC32CB:
				arm_crc32cb (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_CRC32CH:
				arm_crc32ch (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_CRC32CW:
				arm_crc32cw (code, dreg, sreg1, sreg2);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		}
		case OP_XOP_I4_I4_I8: {
			switch (ins->inst_c0) {
			case INTRINS_AARCH64_CRC32X:
				arm_crc32x (code, dreg, sreg1, sreg2);
				break;
			case INTRINS_AARCH64_CRC32CX:
				arm_crc32cx (code, dreg, sreg1, sreg2);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			break;
		}
		case OP_ARM64_HINT:
			g_assert (ins->inst_c0 <= ARMHINT_SEVL);
			arm_hint (code, GTMREG_TO_UINT32 (ins->inst_c0));
			break;

		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}
		
	after_instruction_emit:
		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		
		}
	}
	set_code_cursor (cfg, code);

	/*
	 * If the compiled code size is larger than the bcc displacement (19 bits signed),
	 * insert branch islands between/inside basic blocks.
	 */
	if (cfg->arch.cond_branch_islands)
		code = emit_branch_island (cfg, code, start_offset);
}

static guint8*
emit_move_args (MonoCompile *cfg, guint8 *code)
{
	MonoInst *ins;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int i, part;
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);

	cinfo = cfg->arch.cinfo;
	g_assert (cinfo);
	for (i = 0; i < cinfo->nargs; ++i) {
		ainfo = cinfo->args + i;
		ins = cfg->args [i];

		if (ins->opcode == OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg:
				arm_movx (code, ins->dreg, ainfo->reg);
				if (i == 0 && sig->hasthis) {
					mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, GPTRDIFF_TO_INT (code - cfg->native_code));
					mono_add_var_location (cfg, ins, TRUE, ins->dreg, 0, GPTRDIFF_TO_INT (code - cfg->native_code), 0);
				}
				break;
			case ArgOnStack:
				switch (ainfo->slot_size) {
				case 1:
					if (ainfo->sign)
						code = emit_ldrsbx (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					else
						code = emit_ldrb (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					break;
				case 2:
					if (ainfo->sign)
						code = emit_ldrshx (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					else
						code = emit_ldrh (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					break;
				case 4:
					if (ainfo->sign)
						code = emit_ldrswx (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					else
						code = emit_ldrw (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					break;
				default:
					code = emit_ldrx (code, ins->dreg, cfg->arch.args_reg, ainfo->offset);
					break;
				}
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		} else {
			if (ainfo->storage != ArgVtypeByRef && ainfo->storage != ArgVtypeByRefOnStack)
				g_assert (ins->opcode == OP_REGOFFSET);

			switch (ainfo->storage) {
			case ArgInIReg:
				/* Stack slots for arguments have size 8 */
				code = emit_strx (code, ainfo->reg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
				if (i == 0 && sig->hasthis) {
					mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, GPTRDIFF_TO_INT (code - cfg->native_code));
					mono_add_var_location (cfg, ins, FALSE, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset), GPTRDIFF_TO_INT (code - cfg->native_code), 0);
				}
				break;
			case ArgInFReg:
				code = emit_strfpx (code, ainfo->reg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			case ArgInFRegR4:
				code = emit_strfpw (code, ainfo->reg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			case ArgOnStack:
			case ArgOnStackR4:
			case ArgOnStackR8:
			case ArgVtypeByRefOnStack:
			case ArgVtypeOnStack:
				break;
			case ArgVtypeByRef: {
				MonoInst *addr_arg = ins->inst_left;

				if (ainfo->gsharedvt) {
					g_assert (ins->opcode == OP_GSHAREDVT_ARG_REGOFFSET);
					arm_strx (code, ainfo->reg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
				} else {
					g_assert (ins->opcode == OP_VTARG_ADDR);
					g_assert (addr_arg->opcode == OP_REGOFFSET);
					arm_strx (code, ainfo->reg, addr_arg->inst_basereg, GTMREG_TO_INT (addr_arg->inst_offset));
				}
				break;
			}
			case ArgVtypeInIRegs:
				for (part = 0; part < ainfo->nregs; part ++) {
					int offs = GTMREG_TO_INT (ins->inst_offset + (part * 8));
					if (part + 1 < ainfo->nregs && IS_VALID_STPX_OFFSET (offs)) {
						arm_stpx (code, ainfo->reg + part, ainfo->reg + part + 1, ins->inst_basereg, offs);
						part++;
						continue;
					}
					code = emit_strx (code, ainfo->reg + part, ins->inst_basereg, offs);
				}
				break;
			case ArgHFA:
				for (part = 0; part < ainfo->nregs; part ++) {
					if (ainfo->esize == 4)
						code = emit_strfpw (code, ainfo->reg + part, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset + ainfo->foffsets [part]));
					else
						code = emit_strfpx (code, ainfo->reg + part, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset + ainfo->foffsets [part]));
				}
				break;
			case ArgInSIMDReg:
				code = emit_strfpq (code, ainfo->reg, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
				break;
			case ArgSwiftError:
				if (ainfo->offset) {
					code = emit_ldrx (code, ARMREG_IP0, cfg->arch.args_reg, ainfo->offset);
					code = emit_strx (code, ARMREG_IP0, cfg->arch.swift_error_var->inst_basereg, GTMREG_TO_INT (cfg->arch.swift_error_var->inst_offset));
				} else {
					code = emit_strx (code, ainfo->reg, cfg->arch.swift_error_var->inst_basereg, GTMREG_TO_INT (cfg->arch.swift_error_var->inst_offset));
				}
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}
	}

	return code;
}

/*
 * emit_store_regarray:
 *
 *   Emit code to store the registers in REGS into the appropriate elements of
 * the register array at BASEREG+OFFSET.
 */
static WARN_UNUSED_RESULT guint8*
emit_store_regarray (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i;

	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			// FIXME: use IS_VALID_STPX_OFFSET before doing STPX
			if (i + 1 < 32 && (regs & (1 << (i + 1))) && (i + 1 != ARMREG_SP)) {
				arm_stpx (code, i, i + 1, basereg, offset + (i * 8));
				i++;
			} else if (i == ARMREG_SP) {
				arm_movspx (code, ARMREG_IP1, ARMREG_SP);
				arm_strx (code, ARMREG_IP1, basereg, offset + (i * 8));
			} else {
				arm_strx (code, i, basereg, offset + (i * 8));
			}
		}
	}
	return code;
}

/*
 * emit_load_regarray:
 *
 *   Emit code to load the registers in REGS from the appropriate elements of
 * the register array at BASEREG+OFFSET.
 */
static WARN_UNUSED_RESULT guint8*
emit_load_regarray (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i;

	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if ((regs & (1 << (i + 1))) && (i + 1 != ARMREG_SP)) {
				if (IS_VALID_STPX_OFFSET (offset + (i * 8))) {
					arm_ldpx (code, i, i + 1, basereg, offset + (i * 8));
				} else {
					code = emit_ldrx (code, i, basereg, offset + (i * 8));
					code = emit_ldrx (code, i + 1, basereg, offset + ((i + 1) * 8));
				}
				i++;
			} else if (i == ARMREG_SP) {
				g_assert_not_reached ();
			} else {
				code = emit_ldrx (code, i, basereg, offset + (i * 8));
			}
		}
	}
	return code;
}

/*
 * emit_store_regset:
 *
 *   Emit code to store the registers in REGS into consecutive memory locations starting
 * at BASEREG+OFFSET.
 */
static WARN_UNUSED_RESULT guint8*
emit_store_regset (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i, pos;

	pos = 0;
	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			// FIXME: check IS_VALID_STPX_OFFSET before doing STPX
			if ((regs & (1 << (i + 1))) && (i + 1 != ARMREG_SP)) {
				arm_stpx (code, i, i + 1, basereg, offset + (pos * 8));
				i++;
				pos++;
			} else if (i == ARMREG_SP) {
				arm_movspx (code, ARMREG_IP1, ARMREG_SP);
				arm_strx (code, ARMREG_IP1, basereg, offset + (pos * 8));
			} else {
				arm_strx (code, i, basereg, offset + (pos * 8));
			}
			pos++;
		}
	}
	return code;
}

/*
 * emit_load_regset:
 *
 *   Emit code to load the registers in REGS from consecutive memory locations starting
 * at BASEREG+OFFSET.
 */
static WARN_UNUSED_RESULT guint8*
emit_load_regset (guint8 *code, guint64 regs, int basereg, int offset)
{
	int i, pos;

	pos = 0;
	for (i = 0; i < 32; ++i) {
		if (regs & (1 << i)) {
			if ((regs & (1 << (i + 1))) && (i + 1 != ARMREG_SP)) {
				arm_ldpx (code, i, i + 1, basereg, offset + (pos * 8));
				i++;
				pos++;
			} else if (i == ARMREG_SP) {
				g_assert_not_reached ();
			} else {
				arm_ldrx (code, i, basereg, offset + (pos * 8));
			}
			pos++;
		}
	}
	return code;
}

WARN_UNUSED_RESULT guint8*
mono_arm_emit_load_regarray (guint8 *code, guint64 regs, int basereg, int offset)
{
	return emit_load_regarray (code, regs, basereg, offset);
}

WARN_UNUSED_RESULT guint8*
mono_arm_emit_store_regarray (guint8 *code, guint64 regs, int basereg, int offset)
{
	return emit_store_regarray (code, regs, basereg, offset);
}

WARN_UNUSED_RESULT guint8*
mono_arm_emit_store_regset (guint8 *code, guint64 regs, int basereg, int offset)
{
	return emit_store_regset (code, regs, basereg, offset);
}

/* Same as emit_store_regset, but emit unwind info too */
/* CFA_OFFSET is the offset between the CFA and basereg */
static WARN_UNUSED_RESULT guint8*
emit_store_regset_cfa (MonoCompile *cfg, guint8 *code, guint64 regs, int basereg, int offset, int cfa_offset, guint64 no_cfa_regset)
{
	int i, j, pos, nregs;
	guint32 cfa_regset = GUINT64_TO_UINT32 (regs & ~no_cfa_regset);

	pos = 0;
	for (i = 0; i < 32; ++i) {
		nregs = 1;
		if (regs & (1 << i)) {
			if ((regs & (1 << (i + 1))) && (i + 1 != ARMREG_SP)) {
				if (IS_VALID_STPX_OFFSET (offset + (pos * 8))) {
					arm_stpx (code, i, i + 1, basereg, offset + (pos * 8));
				} else {
					code = emit_strx (code, i, basereg, offset + (pos * 8));
					code = emit_strx (code, i + 1, basereg, offset + (pos * 8) + 8);
				}
				nregs = 2;
			} else if (i == ARMREG_SP) {
				arm_movspx (code, ARMREG_IP1, ARMREG_SP);
				code = emit_strx (code, ARMREG_IP1, basereg, offset + (pos * 8));
			} else {
				code = emit_strx (code, i, basereg, offset + (pos * 8));
			}

			for (j = 0; j < nregs; ++j) {
				if (cfa_regset & (1 << (i + j)))
					mono_emit_unwind_op_offset (cfg, code, (i + j), (- cfa_offset) + offset + ((pos + j) * 8));
			}

			i += nregs - 1;
			pos += nregs;
		}
	}
	return code;
}

/*
 * emit_setup_lmf:
 *
 *   Emit code to initialize an LMF structure at LMF_OFFSET.
 * Clobbers ip0/ip1.
 */
static guint8*
emit_setup_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset, int cfa_offset)
{
	/*
	 * The LMF should contain all the state required to be able to reconstruct the machine state
	 * at the current point of execution. Since the LMF is only read during EH, only callee
	 * saved etc. registers need to be saved.
	 * FIXME: Save callee saved fp regs, JITted code doesn't use them, but native code does, and they
	 * need to be restored during EH.
	 */

	/* pc */
	arm_adrx (code, ARMREG_LR, code);
	code = emit_strx (code, ARMREG_LR, ARMREG_FP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, pc));
	/* gregs + fp + sp */
	/* Don't emit unwind info for sp/fp, they are already handled in the prolog */
	code = emit_store_regset_cfa (cfg, code, MONO_ARCH_LMF_REGS, ARMREG_FP, lmf_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs), cfa_offset, (1 << ARMREG_FP) | (1 << ARMREG_SP));

	return code;
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoBasicBlock *bb;
	guint8 *code;
	int cfa_offset, max_offset;

	sig = mono_method_signature_internal (method);
	cfg->code_size = 256 + sig->param_count * 64;
	code = cfg->native_code = g_malloc (cfg->code_size);

	/* This can be unaligned */
	cfg->stack_offset = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * - Setup frame
	 */
	cfa_offset = 0;
	mono_emit_unwind_op_def_cfa (cfg, code, ARMREG_SP, 0);

	if (enable_ptrauth)
		arm_pacibsp (code);

	/* Setup frame */
	if (arm_is_ldpx_imm (-cfg->stack_offset)) {
		arm_stpx_pre (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, -cfg->stack_offset);
	} else {
		/* sp -= cfg->stack_offset */
		/* This clobbers ip0/ip1 */
		code = emit_subx_sp_imm (code, cfg->stack_offset);
		arm_stpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	}
	cfa_offset += cfg->stack_offset;
	mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
	mono_emit_unwind_op_offset (cfg, code, ARMREG_FP, (- cfa_offset) + 0);
	mono_emit_unwind_op_offset (cfg, code, ARMREG_LR, (- cfa_offset) + 8);
	arm_movspx (code, ARMREG_FP, ARMREG_SP);
	mono_emit_unwind_op_def_cfa_reg (cfg, code, ARMREG_FP);
	if (cfg->param_area) {
		/* The param area is below the frame pointer */
		code = emit_subx_sp_imm (code, cfg->param_area);
	}

	if (cfg->method->save_lmf) {
		code = emit_setup_lmf (cfg, code, GTMREG_TO_INT32 (cfg->lmf_var->inst_offset), cfa_offset);
	} else {
		/* Save gregs */
		code = emit_store_regset_cfa (cfg, code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, ARMREG_FP, cfg->arch.saved_gregs_offset, cfa_offset, 0);
	}

	/* Setup args reg */
	if (cfg->arch.args_reg) {
		/* The register was already saved above */
		code = emit_addx_imm (code, cfg->arch.args_reg, ARMREG_FP, cfg->stack_offset);
	}

	/* Save return area addr received in R8 */
	if (cfg->vret_addr) {
		MonoInst *ins = cfg->vret_addr;

		g_assert (ins->opcode == OP_REGOFFSET);
		code = emit_strx (code, ARMREG_R8, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
	}

	/* Save mrgctx received in MONO_ARCH_RGCTX_REG */
	if (cfg->rgctx_var && !cfg->init_method_rgctx_elim) {
		MonoInst *ins = cfg->rgctx_var;

		g_assert (ins->opcode == OP_REGOFFSET);

		code = emit_strx (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));

		mono_add_var_location (cfg, cfg->rgctx_var, TRUE, MONO_ARCH_RGCTX_REG, 0, 0, GPTRDIFF_TO_INT (code - cfg->native_code));
		mono_add_var_location (cfg, cfg->rgctx_var, FALSE, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset), GPTRDIFF_TO_INT (code - cfg->native_code), 0);
	}

	/*
	 * Move arguments to their registers/stack locations.
	 */
	code = emit_move_args (cfg, code);

	/* Initialize seq_point_info_var */
	if (cfg->arch.seq_point_info_var) {
		MonoInst *ins = cfg->arch.seq_point_info_var;

		/* Initialize the variable from a GOT slot */
		code = emit_aotconst (cfg, code, ARMREG_IP0, MONO_PATCH_INFO_SEQ_POINT_INFO, cfg->method);
		g_assert (ins->opcode == OP_REGOFFSET);
		code = emit_strx (code, ARMREG_IP0, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));

		/* Initialize ss_tramp_var */
		ins = cfg->arch.ss_tramp_var;
		g_assert (ins->opcode == OP_REGOFFSET);

		code = emit_ldrx (code, ARMREG_IP1, ARMREG_IP0, MONO_STRUCT_OFFSET (SeqPointInfo, ss_tramp_addr));
		code = emit_strx (code, ARMREG_IP1, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
	} else {
		MonoInst *ins;

		if (cfg->arch.ss_tramp_var) {
			/* Initialize ss_tramp_var */
			ins = cfg->arch.ss_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			code = emit_imm64 (code, ARMREG_IP0, (guint64)&ss_trampoline);
			code = emit_strx (code, ARMREG_IP0, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
		}

		if (cfg->arch.bp_tramp_var) {
			/* Initialize bp_tramp_var */
			ins = cfg->arch.bp_tramp_var;
			g_assert (ins->opcode == OP_REGOFFSET);

			code = emit_imm64 (code, ARMREG_IP0, (guint64)bp_trampoline);
			code = emit_strx (code, ARMREG_IP0, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset));
		}
	}

	max_offset = 0;
	if (cfg->opt & MONO_OPT_BRANCH) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *ins;
			bb->max_offset = max_offset;

			MONO_BB_FOR_EACH_INS (bb, ins) {
				max_offset += ins_get_size (ins->opcode);
			}
		}
	}
	if (max_offset > 0x3ffff * 4)
		cfg->arch.cond_branch_islands = TRUE;

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	CallInfo *cinfo;
	int max_epilog_size;
	guint8 *code;
	int i;

	max_epilog_size = 16 + 20*4;
	code = realloc_code (cfg, max_epilog_size);

	if (cfg->method->save_lmf) {
		code = mono_arm_emit_load_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, ARMREG_FP, GTMREG_TO_INT (cfg->lmf_var->inst_offset + MONO_STRUCT_OFFSET (MonoLMF, gregs) - (MONO_ARCH_FIRST_LMF_REG * 8)));
	} else {
		/* Restore gregs */
		code = emit_load_regset (code, MONO_ARCH_CALLEE_SAVED_REGS & cfg->used_int_regs, ARMREG_FP, cfg->arch.saved_gregs_offset);
	}

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	switch (cinfo->ret.storage) {
	case ArgVtypeInIRegs: {
		MonoInst *ins = cfg->ret;

		for (i = 0; i < cinfo->ret.nregs; ++i) {
			int offs = GTMREG_TO_INT (ins->inst_offset + (i * 8));
			if (i + 1 < cinfo->ret.nregs && IS_VALID_STPX_OFFSET (offs)) {
				arm_ldpx (code, cinfo->ret.reg + i, cinfo->ret.reg + i + 1, ins->inst_basereg, offs);
				i++;
			} else { 
				code = emit_ldrx (code, cinfo->ret.reg + i, ins->inst_basereg, offs);
			}
		}
		break;
	}
	case ArgHFA: {
		MonoInst *ins = cfg->ret;

		for (i = 0; i < cinfo->ret.nregs; ++i) {
			if (cinfo->ret.esize == 4)
				code = emit_ldrfpw (code, cinfo->ret.reg + i, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset + cinfo->ret.foffsets [i]));
			else
				code = emit_ldrfpx (code, cinfo->ret.reg + i, ins->inst_basereg, GTMREG_TO_INT (ins->inst_offset + cinfo->ret.foffsets [i]));
		}
		break;
	}
	default:
		break;
	}

	/* Destroy frame */
	code = mono_arm_emit_destroy_frame (code, cfg->stack_offset, (1 << ARMREG_IP0) | (1 << ARMREG_IP1));

	if (enable_ptrauth)
		arm_retab (code);
	else
		arm_retx (code, ARMREG_LR);

	g_assert (code - (cfg->native_code + cfg->code_len) < max_epilog_size);

	set_code_cursor (cfg, code);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *ji;
	MonoClass *exc_class;
	guint8 *code, *ip;
	guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM];
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM];
	int i, id, size = 0;

	for (i = 0; i < MONO_EXC_INTRINS_NUM; i++) {
		exc_throw_pos [i] = NULL;
		exc_throw_found [i] = 0;
	}

	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type == MONO_PATCH_INFO_EXC) {
			i = mini_exception_id_by_name ((const char*)ji->data.target);
			if (!exc_throw_found [i]) {
				size += 32;
				exc_throw_found [i] = TRUE;
			}
		} else if (ji->type == MONO_PATCH_INFO_X128) {
			size += 16 + 15; /* sizeof (Vector128<T>) + alignment */
		}
	}

	code = realloc_code (cfg, size);

	/* Emit code to raise corlib exceptions */
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		if (ji->type != MONO_PATCH_INFO_EXC)
			continue;

		ip = cfg->native_code + ji->ip.i;

		id = mini_exception_id_by_name ((const char*)ji->data.target);

		if (exc_throw_pos [id]) {
			/* ip points to the bcc () in OP_COND_EXC_... */
			arm_patch_rel (ip, exc_throw_pos [id], ji->relocation);
			ji->type = MONO_PATCH_INFO_NONE;
			continue;
		}

		exc_throw_pos [id] = code;
		arm_patch_rel (ip, code, ji->relocation);

		/* We are being branched to from the code generated by emit_cond_exc (), the pc is in ip1 */

		/* r0 = type token */
		exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", ji->data.name);
		code = emit_imm (code, ARMREG_R0, m_class_get_type_token (exc_class) - MONO_TOKEN_TYPE_DEF);
		/* r1 = throw ip */
		arm_movx (code, ARMREG_R1, ARMREG_IP1);
		/* Branch to the corlib exception throwing trampoline */
		ji->ip.i = GPTRDIFF_TO_INT (code - cfg->native_code);
		ji->type = MONO_PATCH_INFO_JIT_ICALL_ID;
		ji->data.jit_icall_id = MONO_JIT_ICALL_mono_arch_throw_corlib_exception;
		ji->relocation = MONO_R_ARM64_BL;
		arm_bl (code, 0);
		cfg->thunk_area += THUNK_SIZE;
		set_code_cursor (cfg, code);
	}

	/* Handle relocations with RIP relative addressing */
	for (ji = cfg->patch_info; ji; ji = ji->next) {
		gboolean remove = FALSE;

		if (ji->type == MONO_PATCH_INFO_X128) {
			guint8 *pos;

			code = (guint8*)ALIGN_TO (code, 16);
			pos = cfg->native_code + ji->ip.i;
			arm_neon_ldrq_lit_fixup (pos, code);
			memcpy (code, ji->data.target, 16);
			code += 16;

			remove = TRUE;
		}

		if (remove) {
			if (ji == cfg->patch_info)
				cfg->patch_info = ji->next;
			else {
				MonoJumpInfo *tmp;

				for (tmp = cfg->patch_info; tmp->next != ji; tmp = tmp->next)
					;
				tmp->next = ji->next;
			}
		}
		set_code_cursor (cfg, code);
	}

	set_code_cursor (cfg, code);
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 0;
}

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	int i, buf_len, imt_reg;
	guint8 *buf, *code;

#if DEBUG_IMT
	printf ("building IMT trampoline for class %s %s entries %d code size %d code at %p end %p vtable %p\n", m_class_get_name_space (vtable->klass), m_class_get_name (vtable->klass), count, size, start, ((guint8*)start) + size, vtable);
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		printf ("method %d (%p) %s vtable slot %p is_equals %d chunk size %d\n", i, item->key, item->key->name, &vtable->vtable [item->value.vtable_slot], item->is_equals, item->chunk_size);
	}
#endif

	buf_len = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case) {
					buf_len += 4 * 4 + 4;
				}
				buf_len += 4;
				if (item->has_target_code) {
					buf_len += 5 * 4;
				} else {
					buf_len += 6 * 4;
				}
				if (fail_case) {
					buf_len += 5 * 4;
				}
			} else {
				buf_len += 6 * 4;
			}
		} else {
			buf_len += 6 * 4;
		}
	}

	if (fail_tramp) {
		buf = (guint8 *)mini_alloc_generic_virtual_trampoline (vtable, buf_len);
	} else {
		MonoMemoryManager *mem_manager = m_class_get_mem_manager (vtable->klass);
		buf = mono_mem_manager_code_reserve (mem_manager, buf_len);
	}
	code = buf;

	MINI_BEGIN_CODEGEN ();

	/*
	 * We are called by JITted code, which passes in the IMT argument in
	 * MONO_ARCH_RGCTX_REG (r27). We need to preserve all caller saved regs
	 * except ip0/ip1.
	 */
	imt_reg = MONO_ARCH_RGCTX_REG;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		item->code_target = code;

		if (item->is_equals) {
			/*
			 * Check the imt argument against item->key, if equals, jump to either
			 * item->value.target_code or to vtable [item->value.vtable_slot].
			 * If fail_tramp is set, jump to it if not-equals.
			 */
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				/* Compare imt_reg with item->key */
				if (!item->compare_done || fail_case) {
					// FIXME: Optimize this
					code = emit_imm64 (code, ARMREG_IP0, (guint64)item->key);
					arm_cmpx (code, imt_reg, ARMREG_IP0);
				}
				item->jmp_code = code;
				arm_bcc (code, ARMCOND_NE, 0);
				/* Jump to target if equals */
				if (item->has_target_code) {
					code = emit_imm64 (code, ARMREG_IP0, (guint64)item->value.target_code);
					code = mono_arm_emit_brx (code, ARMREG_IP0);
				} else {
					guint64 imm = (guint64)&(vtable->vtable [item->value.vtable_slot]);

					code = emit_imm64 (code, ARMREG_IP0, imm);
					arm_ldrx (code, ARMREG_IP0, ARMREG_IP0, 0);
					code = mono_arm_emit_brx (code, ARMREG_IP0);
				}

				if (fail_case) {
					arm_patch_rel (item->jmp_code, code, MONO_R_ARM64_BCC);
					item->jmp_code = NULL;
					code = emit_imm64 (code, ARMREG_IP0, (guint64)fail_tramp);
					code = mono_arm_emit_brx (code, ARMREG_IP0);
				}
			} else {
				guint64 imm = (guint64)&(vtable->vtable [item->value.vtable_slot]);

				code = emit_imm64 (code, ARMREG_IP0, imm);
				arm_ldrx (code, ARMREG_IP0, ARMREG_IP0, 0);
				code = mono_arm_emit_brx (code, ARMREG_IP0);
			}
		} else {
			code = emit_imm64 (code, ARMREG_IP0, (guint64)item->key);
			arm_cmpx (code, imt_reg, ARMREG_IP0);
			item->jmp_code = code;
			arm_bcc (code, ARMCOND_HS, 0);
		}
	}
	/* Patch the branches */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code && item->check_target_idx)
			arm_patch_rel (item->jmp_code, imt_entries [item->check_target_idx]->code_target, MONO_R_ARM64_BCC);
	}

	g_assert ((code - buf) <= buf_len);

	MINI_END_CODEGEN (buf, GPTRDIFF_TO_INT (code - buf), MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL);

	return MINI_ADDR_TO_FTNPTR (buf);
}

GSList *
mono_arch_get_trampolines (gboolean aot)
{
	return mono_arm_get_exception_trampolines (aot);
}

#else /* DISABLE_JIT */

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* !DISABLE_JIT */

#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = MINI_FTNPTR_TO_ADDR (ip);
	guint32 native_offset = GPTRDIFF_TO_UINT32 (ip - (guint8*)ji->code_start);

	if (ji->from_aot) {
		SeqPointInfo *info = mono_arch_get_seq_point_info ((guint8*)ji->code_start);

		if (enable_ptrauth)
			NOT_IMPLEMENTED;
		g_assert (native_offset % 4 == 0);
		g_assert (info->bp_addrs [native_offset / 4] == 0);
		info->bp_addrs [native_offset / 4] = (guint8*)mini_get_breakpoint_trampoline ();
	} else {
		/* ip points to an ldrx */
		code += 4;
		mono_codeman_enable_write ();
		code = mono_arm_emit_blrx (code, ARMREG_IP0);
		mono_codeman_disable_write ();
		mono_arch_flush_icache (ip, GPTRDIFF_TO_INT (code - ip));
	}
}

void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = MINI_FTNPTR_TO_ADDR (ip);

	if (ji->from_aot) {
		guint32 native_offset = GPTRDIFF_TO_UINT32 (ip - (guint8*)ji->code_start);
		SeqPointInfo *info = mono_arch_get_seq_point_info ((guint8*)ji->code_start);

		if (enable_ptrauth)
			NOT_IMPLEMENTED;

		g_assert (native_offset % 4 == 0);
		info->bp_addrs [native_offset / 4] = NULL;
	} else {
		/* ip points to an ldrx */
		code += 4;
		mono_codeman_enable_write ();
		arm_nop (code);
		mono_codeman_disable_write ();
		mono_arch_flush_icache (ip,GPTRDIFF_TO_INT ( code - ip));
	}
}

void
mono_arch_start_single_stepping (void)
{
	ss_trampoline = mini_get_single_step_trampoline ();
}

void
mono_arch_stop_single_stepping (void)
{
	ss_trampoline = NULL;
}

gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	/* We use soft breakpoints on arm64 */
	return FALSE;
}

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	/* We use soft breakpoints on arm64 */
	return FALSE;
}

void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	g_assert_not_reached ();
}

void
mono_arch_skip_single_step (MonoContext *ctx)
{
	g_assert_not_reached ();
}

SeqPointInfo*
mono_arch_get_seq_point_info (guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;
	MonoJitMemoryManager *jit_mm;

	jit_mm = get_default_jit_mm ();

	// FIXME: Add a free function

	jit_mm_lock (jit_mm);
	info = (SeqPointInfo *)g_hash_table_lookup (jit_mm->arch_seq_points, code);
	jit_mm_unlock (jit_mm);

	if (!info) {
		ji = mini_jit_info_table_find (code);
		g_assert (ji);

		info = g_malloc0 (sizeof (SeqPointInfo) + (ji->code_size / 4) * sizeof(guint8*));

		info->ss_tramp_addr = &ss_trampoline;

		jit_mm_lock (jit_mm);
		g_hash_table_insert (jit_mm->arch_seq_points, code, info);
		jit_mm_unlock (jit_mm);
	}

	return info;
}

#endif /* MONO_ARCH_SOFT_DEBUG_SUPPORTED */

gboolean
mono_arch_opcode_supported (int opcode)
{
	switch (opcode) {
	case OP_ATOMIC_ADD_I4:
	case OP_ATOMIC_ADD_I8:
	case OP_ATOMIC_EXCHANGE_I4:
	case OP_ATOMIC_EXCHANGE_I8:
	case OP_ATOMIC_CAS_I4:
	case OP_ATOMIC_CAS_I8:
	case OP_ATOMIC_LOAD_I1:
	case OP_ATOMIC_LOAD_I2:
	case OP_ATOMIC_LOAD_I4:
	case OP_ATOMIC_LOAD_I8:
	case OP_ATOMIC_LOAD_U1:
	case OP_ATOMIC_LOAD_U2:
	case OP_ATOMIC_LOAD_U4:
	case OP_ATOMIC_LOAD_U8:
	case OP_ATOMIC_LOAD_R4:
	case OP_ATOMIC_LOAD_R8:
	case OP_ATOMIC_STORE_I1:
	case OP_ATOMIC_STORE_I2:
	case OP_ATOMIC_STORE_I4:
	case OP_ATOMIC_STORE_I8:
	case OP_ATOMIC_STORE_U1:
	case OP_ATOMIC_STORE_U2:
	case OP_ATOMIC_STORE_U4:
	case OP_ATOMIC_STORE_U8:
	case OP_ATOMIC_STORE_R4:
	case OP_ATOMIC_STORE_R8:
		return TRUE;
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
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	gpointer target = NULL;
	switch (jit_icall_id) {
#undef MONO_AOT_ICALL
#define MONO_AOT_ICALL(x) case MONO_JIT_ICALL_ ## x: target = (gpointer)x; break;
	MONO_AOT_ICALL (mono_arm_resume_unwind)
	MONO_AOT_ICALL (mono_arm_start_gsharedvt_call)
	MONO_AOT_ICALL (mono_arm_throw_exception)
	}
	return target;
}

static guint8*
emit_blrx (guint8 *code, int reg)
{
	if (enable_ptrauth)
		arm_blraaz (code, reg);
	else
		arm_blrx (code, reg);
	return code;
}

static guint8*
emit_brx (guint8 *code, int reg)
{
	if (enable_ptrauth)
		arm_braaz (code, reg);
	else
		arm_brx (code, reg);
	return code;
}

guint8*
mono_arm_emit_blrx (guint8 *code, int reg)
{
	return emit_blrx (code, reg);
}

guint8*
mono_arm_emit_brx (guint8 *code, int reg)
{
	return emit_brx (code, reg);
}

MonoCPUFeatures
mono_arch_get_cpu_features (void)
{
	guint64 features = MONO_CPU_INITED;

	if (mono_hwcap_arm64_has_crc32)
		features |= MONO_CPU_ARM64_CRC;
	if (mono_hwcap_arm64_has_dot)
		features |= MONO_CPU_ARM64_DP;
	if (mono_hwcap_arm64_has_rdm)
		features |= MONO_CPU_ARM64_RDM;
	if (mono_hwcap_arm64_has_sha1 && mono_hwcap_arm64_has_sha256 && mono_hwcap_arm64_has_aes)
		features |= MONO_CPU_ARM64_CRYPTO;

	return features;
}
