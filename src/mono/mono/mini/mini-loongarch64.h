/**
 * \file
 *
 * Authors:
 *   Qiao Pengcheng (qiaopengcheng@loongson.cn), Liu An (liuan@loongson.cn)
 *
 * Copyright (c) 2021 Loongson Technology, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_MINI_LOONGARCH64_H__
#define __MONO_MINI_LOONGARCH64_H__

#include <glib.h>
#include <mono/arch/loongarch64/loongarch64-codegen.h>
#include <mono/utils/mono-context.h>

#define MONO_ARCH_CPU_SPEC mono_loongarch64_desc

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#if SIZEOF_REGISTER != 8
#error Unknown REGISTER_SIZE
#endif

#define IREG_SIZE 8
#define FREG_SIZE 8

#define MONO_ARCH_HAVE_PATCH_CODE_NEW 1
#define MONO_ARCH_GSHAREDVT_SUPPORTED 1
#define MONO_ARCH_INTERPRETER_SUPPORTED 1
#define MONO_ARCH_HAVE_EXCEPTIONS_INIT 1
#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS 1
#define MONO_ARCH_HAVE_OPCODE_NEEDS_EMULATION 1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES 1

#define MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE 1
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GSHARED_SUPPORTED 1

/* set the next to 0 once inssel-loongarch.brg is updated */
#define LOONGARCH_PASS_STRUCTS_BY_VALUE 1

#define MONO_ARCH_USE_SIGACTION 1
#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_NO_IOV_CHECK 1
#define MONO_ARCH_HAVE_DECOMPOSE_OPTS 1

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 1
#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->regs [4] = (gsize)exc; } while (0)
#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,func) do {	\
	MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0));			\
	MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0));			\
	MONO_CONTEXT_SET_IP ((ctx), (func));								\
	} while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf)

/*
 * r21 and t0/t8 used internally,
 * a0-a7 are for arguments,
 */
#define loongarch_temp  loongarch_t8

#define LOONGARCH_T_REGS ((1 << loongarch_t1) | \
			 (1 << loongarch_t2) | \
			 (1 << loongarch_t3) | \
			 (1 << loongarch_t4) | \
			 (1 << loongarch_t5) | \
			 (1 << loongarch_t6) | \
			 (1 << loongarch_t7))

#define LOONGARCH_S_REGS ((1 << loongarch_s0) | \
			 (1 << loongarch_s1) | \
			 (1 << loongarch_s2) | \
			 (1 << loongarch_s3) | \
			 (1 << loongarch_s4) | \
			 (1 << loongarch_s5) | \
			 (1 << loongarch_s6) | \
			 (1 << loongarch_s7) | \
			 (1 << loongarch_s8))

#define LOONGARCH_A_REGS ((1 << loongarch_a0) | \
			 (1 << loongarch_a1) | \
			 (1 << loongarch_a2) | \
			 (1 << loongarch_a3) | \
			 (1 << loongarch_a4) | \
			 (1 << loongarch_a5) | \
			 (1 << loongarch_a6) | \
			 (1 << loongarch_a7))

#define PARAM_REGS (loongarch_a7+1)
#define FP_PARAM_REGS 8
#define RETURN_REGS 2
#define FLOAT_RETURN_REGS 2

#define MONO_ARCH_CALLEE_REGS (LOONGARCH_T_REGS | LOONGARCH_A_REGS)
#define MONO_ARCH_CALLEE_SAVED_REGS LOONGARCH_S_REGS

#define MONO_ARCH_CALLEE_FREGS ((1 << loongarch_f0) |  \
			 (1 << loongarch_f1) | \
			 (1 << loongarch_f2) | \
			 (1 << loongarch_f3) | \
			 (1 << loongarch_f4) | \
			 (1 << loongarch_f5) | \
			 (1 << loongarch_f6) | \
			 (1 << loongarch_f7) | \
			 (1 << loongarch_ft0) | \
			 (1 << loongarch_ft1) | \
			 (1 << loongarch_ft2) | \
			 (1 << loongarch_ft3) | \
			 (1 << loongarch_ft4) | \
			 (1 << loongarch_ft5) | \
			 (1 << loongarch_ft6) | \
			 (1 << loongarch_ft7) | \
			 (1 << loongarch_ft8) | \
			 (1 << loongarch_ft9) | \
			 (1 << loongarch_ft10) | \
			 (1 << loongarch_ft11) | \
			 (1 << loongarch_ft12) | \
			 (1 << loongarch_ft13))

#define MONO_ARCH_CALLEE_SAVED_FREGS ((1 << loongarch_fs0) |  \
			 (1 << loongarch_fs1) | \
			 (1 << loongarch_fs2) | \
			 (1 << loongarch_fs3) | \
			 (1 << loongarch_fs4) | \
			 (1 << loongarch_fs5) | \
			 (1 << loongarch_fs6) | \
			 (1 << loongarch_fs7))

#define loongarch_ftemp loongarch_ft14
#define loongarch_ftemp2 loongarch_ft15

#define MONO_ARCH_USE_FPSTACK FALSE

/* Parameters used by the register allocator */
#define MONO_ARCH_INST_SREG2_MASK(ins) (0)
#define MONO_ARCH_INST_IS_REGPAIR(desc) (0)
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)
#define MONO_ARCH_INST_IS_FLOAT(desc) (desc == 'f')

// This define is called to get specific dest register as defined
// by md file (letter after "dest"). Overwise return -1
#define MONO_ARCH_INST_FIXED_REG(desc) ((desc) == 'a' ? loongarch_a0 : -1)

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

/* callee saved regs + fp + sp */
#define MONO_ARCH_LMF_REGS ((0x1ff << 23) | (1 << loongarch_fp) | (1 << loongarch_sp))
#define MONO_ARCH_NUM_LMF_REGS (9 + 2)
#define MONO_ARCH_NUM_LMF_FREGS (8)
#define MONO_ARCH_FIRST_LMF_REG loongarch_s0
#define MONO_ARCH_LMF_REG_FP 1
#define MONO_ARCH_LMF_REG_SP 0

struct MonoLMF {
	gpointer	previous_lmf;
	gpointer	lmf_addr;
	MonoMethod	*method;
	gpointer	pc;
	host_mgreg_t    gregs [MONO_ARCH_NUM_LMF_REGS];
	gdouble	fregs [MONO_ARCH_NUM_LMF_FREGS];//should confirm ?! why not fpr ?!
	gulong		magic;
};

/* Structure used by the sequence points in AOTed code */
struct SeqPointInfo {// metadata/object-offsets.h
	gpointer ss_tramp_addr;
	guint8* bp_addrs [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	CallInfo *cinfo;
	int saved_gregs_offset;
	/* Points to arguments received on the stack */
	int args_reg;
	gint8 cond_branch_islands;
	MonoInst *vret_addr_loc;
	MonoInst *seq_point_info_var;
	MonoInst *ss_tramp_var;
	MonoInst *bp_tramp_var;
	guint8 *thunks;
	int thunks_size;
} MonoCompileArch;

#define MONO_ARCH_EMULATE_FCONV_TO_U4 1
#define MONO_ARCH_EMULATE_FCONV_TO_U8 1
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS 1
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_EMULATE_LONG_MUL_OVF_OPTS 1
#define MONO_ARCH_EMULATE_MUL_OVF 1
#define MONO_ARCH_FLOAT32_SUPPORTED 1
#define MONO_ARCH_HAVE_OP_TAILCALL_MEMBASE 1
#define MONO_ARCH_HAVE_OP_TAILCALL_REG 1

#define LOONGARCH_RET_ADDR_OFFSET (-sizeof (target_mgreg_t))

#define LOONGARCH_STACK_ALIGNMENT 16
#define MONO_ARCH_FRAME_ALIGNMENT 16

#define LOONGARCH_FIRST_ARG_REG loongarch_a0
#define LOONGARCH_LAST_ARG_REG loongarch_a7
#define LOONGARCH_FIRST_FPARG_REG loongarch_f0
#define LOONGARCH_LAST_FPARG_REG loongarch_f7

#define MONO_ARCH_IMT_REG loongarch_t1
#define MONO_ARCH_RGCTX_REG MONO_ARCH_IMT_REG

#define MONO_ARCH_VTABLE_REG loongarch_a0

/* Relocations */
#define MONO_R_LOONGARCH64_BL 1
#define MONO_R_LOONGARCH64_B 2
#define MONO_R_LOONGARCH64_BC 3
#define MONO_R_LOONGARCH64_BZ 4
#define MONO_R_LOONGARCH64_J 5
#define MONO_R_LOONGARCH64_JR 6


/// some features's macros for LoongArch64
#define MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED 1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK 1
#define MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP 1
#define MONO_ARCH_AOT_SUPPORTED 1
#define MONO_ARCH_LLVM_SUPPORTED 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1

typedef enum {
	ArgInIReg,
	ArgOnStack,
	ArgInFReg,
	ArgStructByVal,
	ArgStructByRef,
	ArgNone
} ArgStorage;

typedef struct {
	gint32  offset; //ArgOnStack.
	guint8  reg;
	guint8  freg;
	guint8  field_info; //bit0: whether the first field is float;
						//bit1: whether the size of first field is 8-bytes;
						//bit2: whether the second field is exist;
						//bit3: whether the second field is float;
						//bit4: whether the size of second field is 8-bytes;
						//field_info will be used by ArgStructByVal.
	guint8  slot_size;
	ArgStorage storage;
	gboolean sign;
	gboolean fin_ireg;  //float in ireg when freg is not available.
	guint32 size;   //for ArgStructByVal size is struction size, for ArgInFReg size is isR.
} ArgInfo;

struct CallInfo {
	int nargs;
	int gr;
	int fr;
	guint32 stack_usage;
	gboolean pinvoke, vararg;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
};

typedef struct {
	/* General registers for indirect returns */
	host_mgreg_t gregs [PARAM_REGS];
	/* Floating registers */
	double fregs [FP_PARAM_REGS];
	/* Stack usage, used for passing params on stack */
	guint32 stack_size;
	guint8* stack;
} CallContext;

guint8* mono_loongarch64_emit_destroy_frame (guint8 *code, int stack_offset);
void mono_loongarch64_patch (guint8 *code, guint8 *target, int relocation);
void mono_loongarch_gsharedvt_init (void);
GSList* mono_loongarch_get_exception_trampolines (gboolean aot);
guint8* mono_loongarch_emit_imm64 (guint8 *code, int dreg, gint64 imm);
guint8* mono_loongarch_emit_jirl (guint8 *code, int reg);
guint8* mono_loongarch_emit_store_regarray (guint8 *code, guint64 regs, int basereg, int offset);
guint8* mono_loongarch_emit_store_regset (guint8 *code, guint64 regs, int basereg, int offset);
guint8* mono_loongarch_emit_load_regset (guint8 *code, guint64 regs, int basereg, int offset);
guint8* mono_loongarch_emit_load_regarray (guint8 *code, guint64 regs, int basereg, int offset);
#endif /* __MONO_MINI_LOONGARCH64_H__ */
