/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#ifndef __MONO_MINI_RISCV_H__
#define __MONO_MINI_RISCV_H__

#include <mono/arch/riscv/riscv-codegen.h>

#ifdef TARGET_RISCV64
#define MONO_RISCV_ARCHITECTURE "riscv64"
#else
#define MONO_RISCV_ARCHITECTURE "riscv32"
#endif

#if defined (RISCV_FPABI_SOFT)
#define MONO_ARCH_SOFT_FLOAT_FALLBACK
#define RISCV_FP_MODEL "soft-fp"
#elif defined (RISCV_FPABI_DOUBLE)
#define RISCV_FP_MODEL "double-fp"
#elif defined (RISCV_FPABI_SINGLE)
#define RISCV_FP_MODEL "single-fp"
#error "The single-precision RISC-V hard float ABI is not currently supported."
#else
#error "Unknown RISC-V FPABI. This is probably a bug in configure.ac."
#endif

#define MONO_ARCH_ARCHITECTURE MONO_RISCV_ARCHITECTURE "," RISCV_FP_MODEL

#ifdef TARGET_RISCV64
#define MONO_ARCH_CPU_SPEC mono_riscv64_cpu_desc
#else
#define MONO_ARCH_CPU_SPEC mono_riscv32_cpu_desc
#endif

#define MONO_MAX_IREGS (RISCV_N_GREGS)
#define MONO_MAX_FREGS (RISCV_N_FREGS)

/*
 * Register usage conventions:
 *
 * - a0..a7 and fa0..fa7 are argument/return registers.
 * - s0..11 and fs0..fs11 are callee-saved registers.
 * - a0..a1 are used as fixed registers (for the 'a' spec, soft float, and
 *   longs on 32-bit, as appropriate).
 * - t0..t1, ra, and ft0..ft2 are used as scratch registers and can't be
 *   allocated by the register allocator.
 * - t2 is used as the RGCTX/IMT register and can't be allocated by the
 *   register allocator.
 * - a0 is used as the VTable register for lazy fetch trampolines.
 * - sp, fp, gp, and tp are all reserved by the ABI and can't be allocated by
 *   the register allocator.
 * - x0 is hard-wired to zero and can't be allocated by the register allocator.
 */

#define MONO_ARCH_CALLEE_REGS        (0b11110000000000111111110000000000)
#define MONO_ARCH_CALLEE_SAVED_REGS  (0b00001111111111000000001100000000)

#ifdef RISCV_FPABI_SOFT

#define MONO_ARCH_CALLEE_FREGS       (0b11111111111111111111111111111000)
#define MONO_ARCH_CALLEE_SAVED_FREGS (0b00000000000000000000000000000000)

#else

#define MONO_ARCH_CALLEE_FREGS       (0b11110000000000111111110011111000)
#define MONO_ARCH_CALLEE_SAVED_FREGS (0b00001111111111000000001100000000)

#endif

#define MONO_ARCH_INST_SREG2_MASK(ins) \
	(0)
#define MONO_ARCH_INST_IS_FLOAT(desc) \
	(!mono_arch_is_soft_float () && (desc) == 'f')

#ifdef TARGET_RISCV64

#define MONO_ARCH_INST_FIXED_REG(desc) \
	((desc) == 'a' || (mono_arch_is_soft_float () && (desc) == 'f') ? RISCV_A0 : -1)
#define MONO_ARCH_INST_IS_REGPAIR(desc) \
	(FALSE)
#define MONO_ARCH_INST_REGPAIR_REG2(desc, hreg1) \
	(-1)

#else

#define MONO_ARCH_INST_FIXED_REG(desc) \
	((desc) == 'a' || (desc) == 'l' || (mono_arch_is_soft_float () && (desc) == 'f') ? RISCV_A0 : -1)
#define MONO_ARCH_INST_IS_REGPAIR(desc) \
	((desc) == 'l' || (mono_arch_is_soft_float () && (desc) == 'f'))
#define MONO_ARCH_INST_REGPAIR_REG2(desc, hreg1) \
	(RISCV_A1)

#endif

#define MONO_ARCH_RGCTX_REG  (RISCV_T2)
#define MONO_ARCH_IMT_REG    (RISCV_T2)
#define MONO_ARCH_VTABLE_REG (RISCV_A0)

#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 0

#define MONO_ARCH_USE_FPSTACK (FALSE)

#define MONO_ARCH_FRAME_ALIGNMENT (16)
#define MONO_ARCH_CODE_ALIGNMENT  (32)

#define MONO_ARCH_EMULATE_MUL_DIV           (1)
#define MONO_ARCH_EMULATE_FREM              (1)

#ifdef TARGET_RISCV64

#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS (1)

#endif

#define MONO_ARCH_EMULATE_CONV_R8_UN        (1)
#define MONO_ARCH_EMULATE_FCONV_TO_U8       (1)
#define MONO_ARCH_EMULATE_FCONV_TO_U4       (1)
#define MONO_ARCH_EMULATE_FCONV_TO_I8       (1)
#define MONO_ARCH_EMULATE_LCONV_TO_R8       (1)
#define MONO_ARCH_EMULATE_LCONV_TO_R4       (1)
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN    (1)

#define MONO_ARCH_NEED_DIV_CHECK (1)

#define MONO_ARCH_HAVE_OP_TAIL_CALL          (1)
#define MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT (1)
#define MONO_ARCH_HAVE_CARD_TABLE_WBARRIER   (1)

#define MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE          (1)
#define MONO_ARCH_HAVE_GENERAL_RGCTX_LAZY_FETCH_TRAMPOLINE (1)
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES                     (1)
#define MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP                (1)
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES                (1)

#define MONO_ARCH_USE_SIGACTION          (1)

#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG                  (1)
#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS                  (1)
#define MONO_ARCH_HAVE_DECOMPOSE_OPTS                       (1)
#define MONO_ARCH_HAVE_EXCEPTIONS_INIT                      (1)
#define MONO_ARCH_HAVE_GET_TRAMPOLINES                      (1)
#define MONO_ARCH_HAVE_OPCODE_NEEDS_EMULATION               (1)
#define MONO_ARCH_HAVE_PATCH_CODE_NEW                       (1)
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK                 (1)
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX (1)

#define MONO_ARCH_GSHARED_SUPPORTED     (1)
#define MONO_ARCH_INTERPRETER_SUPPORTED (1)
//#define MONO_ARCH_AOT_SUPPORTED         (1)
//#define MONO_ARCH_LLVM_SUPPORTED        (1)
//#define MONO_ARCH_SOFT_DEBUG_SUPPORTED  (1)


// #define MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE (1)
// #define MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP (1)
// #define MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED (1)

typedef struct {
} MonoCompileArch;

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) \
	do { \
		(ctx)->gregs [RISCV_A0] = (host_mgreg_t) exc; \
	} while (0)

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx, func) \
	do { \
		MONO_CONTEXT_SET_IP ((ctx), (func)); \
		MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0)); \
		MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0)); \
	} while (0)

struct MonoLMF {
	// If the second-lowest bit of this field is set, this is a MonoLMFExt.
	gpointer previous_lmf;
	gpointer lmf_addr;
	host_mgreg_t pc;
	host_mgreg_t sp;
	host_mgreg_t ra;
	host_mgreg_t gregs [RISCV_N_GSREGS]; // s0..s11
	double fregs [RISCV_N_FSREGS]; // fs0..fs11
};

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf)

typedef struct {
	guint8 *stack;
} CallContext;

enum {
	MONO_R_RISCV_IMM  = 1,
	MONO_R_RISCV_B    = 2,
	MONO_R_RISCV_BEQ  = 3,
	MONO_R_RISCV_BNE  = 4,
	MONO_R_RISCV_BLT  = 5,
	MONO_R_RISCV_BGE  = 6,
	MONO_R_RISCV_BLTU = 7,
	MONO_R_RISCV_BGEU = 8,
};

__attribute__ ((warn_unused_result)) guint8 *
mono_riscv_emit_imm (guint8 *code, int rd, gsize imm);

__attribute__ ((warn_unused_result)) guint8 *
mono_riscv_emit_load (guint8 *code, int rd, int rs1, gint32 imm);

__attribute__ ((warn_unused_result)) guint8 *
mono_riscv_emit_store (guint8 *code, int rs1, int rs2, gint32 imm);

#endif
