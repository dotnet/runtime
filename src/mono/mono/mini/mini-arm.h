/**
 * \file
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_MINI_ARM_H__
#define __MONO_MINI_ARM_H__

#include <mono/arch/arm/arm-codegen.h>
#include <mono/utils/mono-context.h>
#include <glib.h>

#if defined(ARM_FPU_NONE)
#define MONO_ARCH_SOFT_FLOAT_FALLBACK 1
#endif

#if defined(__ARM_EABI__)
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define ARM_ARCHITECTURE "armel"
#else
#define ARM_ARCHITECTURE "armeb"
#endif
#else
#define ARM_ARCHITECTURE "arm"
#endif

#if defined(ARM_FPU_VFP)
#define ARM_FP_MODEL "vfp"
#elif defined(ARM_FPU_NONE)
#define ARM_FP_MODEL "vfp+fallback"
#elif defined(ARM_FPU_VFP_HARD)
#define ARM_FP_MODEL "vfp+hard"
#else
#error "At least one of ARM_FPU_NONE, ARM_FPU_VFP or ARM_FPU_VFP_HARD must be defined."
#endif

#define MONO_ARCH_ARCHITECTURE ARM_ARCHITECTURE "," ARM_FP_MODEL

#define MONO_ARCH_CPU_SPEC mono_arm_cpu_desc

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define ARM_LSW_REG ARMREG_R0
#define ARM_MSW_REG ARMREG_R1
#else
#define ARM_LSW_REG ARMREG_R1
#define ARM_MSW_REG ARMREG_R0
#endif

#define MONO_MAX_IREGS 16

#define MONO_SAVED_GREGS 10 /* r4-r11, ip, lr */

/* r4-r11, ip, lr: registers saved in the LMF  */
#define MONO_ARM_REGSAVE_MASK 0x5ff0
#define MONO_ARM_FIRST_SAVED_REG ARMREG_R4
#define MONO_ARM_NUM_SAVED_REGS 10

/* Parameters used by the register allocator */

#define MONO_ARCH_CALLEE_REGS ((1<<ARMREG_R0) | (1<<ARMREG_R1) | (1<<ARMREG_R2) | (1<<ARMREG_R3) | (1<<ARMREG_IP))
#define MONO_ARCH_CALLEE_SAVED_REGS ((1<<ARMREG_V1) | (1<<ARMREG_V2) | (1<<ARMREG_V3) | (1<<ARMREG_V4) | (1<<ARMREG_V5) | (1<<ARMREG_V6) | (1<<ARMREG_V7))

/*
 * TODO: Make use of VFP v3 registers d16-d31.
 */

/*
 * TODO: We can't use registers d8-d15 in hard float mode because the
 * register allocator doesn't allocate floating point registers globally.
 */

#if defined(ARM_FPU_VFP_HARD)
#define MONO_SAVED_FREGS 16
#define MONO_MAX_FREGS 32

/*
 * d8-d15 must be preserved across function calls. We use d14-d15 as
 * scratch registers in the JIT. The rest have no meaning tied to them.
 */
#define MONO_ARCH_CALLEE_FREGS 0x00005555
#define MONO_ARCH_CALLEE_SAVED_FREGS 0x55550000
#else
#define MONO_SAVED_FREGS 8
#define MONO_MAX_FREGS 16

/*
 * No registers need to be preserved across function calls. We use d0-d1
 * as scratch registers in the JIT. The rest have no meaning tied to them.
 */
#define MONO_ARCH_CALLEE_FREGS 0x55555550
#define MONO_ARCH_CALLEE_SAVED_FREGS 0x00000000
#endif

#define MONO_ARCH_USE_FPSTACK FALSE

#define MONO_ARCH_INST_SREG2_MASK(ins) (0)

#define MONO_ARCH_INST_FIXED_REG(desc) \
	(mono_arch_is_soft_float () ? \
	((desc) == 'l' || (desc) == 'f' || (desc) == 'g' ? ARM_LSW_REG : (desc) == 'a' ? ARMREG_R0 : -1) : \
	((desc) == 'l' ? ARM_LSW_REG : (desc) == 'a' ? ARMREG_R0 : -1))

#define MONO_ARCH_INST_IS_REGPAIR(desc) \
	(mono_arch_is_soft_float () ? \
	((desc) == 'l' || (desc) == 'L' || (desc) == 'f' || (desc) == 'g') : \
	((desc) == 'l' || (desc) == 'L'))

#define MONO_ARCH_INST_IS_FLOAT(desc) \
	(mono_arch_is_soft_float () ? \
	(FALSE) : \
	((desc) == 'f' || (desc) == 'g'))

#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) ((desc) == 'l' || (desc) == 'f' || (desc) == 'g' ? ARM_MSW_REG : -1)

#ifdef TARGET_WATCHOS
#define MONO_ARCH_FRAME_ALIGNMENT 16
#else
#define MONO_ARCH_FRAME_ALIGNMENT 8
#endif

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

/* This needs to hold both a 32 bit int and a 64 bit double */
#define mono_unwind_reg_t guint64

/* Argument marshallings for calls between gsharedvt and normal code */
typedef enum {
	GSHAREDVT_ARG_NONE = 0,
	GSHAREDVT_ARG_BYVAL_TO_BYREF = 1,
	GSHAREDVT_ARG_BYREF_TO_BYVAL = 2,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_I1 = 3,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_I2 = 4,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_U1 = 5,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_U2 = 6
} GSharedVtArgMarshal;

/* Return value marshalling for calls between gsharedvt and normal code */
typedef enum {
	GSHAREDVT_RET_NONE = 0,
	GSHAREDVT_RET_IREG = 1,
	GSHAREDVT_RET_IREGS = 2,
	GSHAREDVT_RET_I1 = 3,
	GSHAREDVT_RET_U1 = 4,
	GSHAREDVT_RET_I2 = 5,
	GSHAREDVT_RET_U2 = 6,
	GSHAREDVT_RET_VFP_R4 = 7,
	GSHAREDVT_RET_VFP_R8 = 8
} GSharedVtRetMarshal;

typedef struct {
	/* Method address to call */
	gpointer addr;
	/* The trampoline reads this, so keep the size explicit */
	int ret_marshal;
	/* If ret_marshal != NONE, this is the reg of the vret arg, else -1 */
	int vret_arg_reg;
	/* The stack slot where the return value will be stored */
	int vret_slot;
	int stack_usage, map_count;
	/* If not -1, then make a virtual call using this vtable offset */
	int vcall_offset;
	/* If 1, make an indirect call to the address in the rgctx reg */
	int calli;
	/* Whenever this is a in or an out call */
	int gsharedvt_in;
	/* Whenever this call uses fp registers */
	int have_fregs;
	CallInfo *caller_cinfo;
	CallInfo *callee_cinfo;
	/* Maps stack slots/registers in the caller to the stack slots/registers in the callee */
	/* A negative value means a register, i.e. -1=r0, -2=r1 etc. */
	int map [MONO_ZERO_LEN_ARRAY];
} GSharedVtCallInfo;


typedef enum {
	RegTypeNone,
	/* Passed/returned in an ireg */
	RegTypeGeneral,
	/* Passed/returned in a pair of iregs */
	RegTypeIRegPair,
	/* Passed on the stack */
	RegTypeBase,
	/* First word in r3, second word on the stack */
	RegTypeBaseGen,
	/* FP value passed in either an ireg or a vfp reg */
	RegTypeFP,
	/* Struct passed/returned in gregs */
	RegTypeStructByVal,
	RegTypeStructByAddr,
	RegTypeStructByAddrOnStack,
	/* gsharedvt argument passed by addr in greg */
	RegTypeGSharedVtInReg,
	/* gsharedvt argument passed by addr on stack */
	RegTypeGSharedVtOnStack,
	RegTypeHFA
} ArgStorage;

typedef struct {
	gint32  offset;
	guint16 vtsize; /* in param area */
	/* RegTypeHFA */
	int esize;
	/* RegTypeHFA/RegTypeStructByVal */
	int nregs;
	guint8  reg;
	ArgStorage  storage;
	/* RegTypeStructByVal */
	gint32  struct_size, align;
	guint8  size    : 4; /* 1, 2, 4, 8, or regs used by RegTypeStructByVal */
	guint8  is_signed : 1;
} ArgInfo;

struct CallInfo {
	int nargs;
	guint32 stack_usage;
	/* The index of the vret arg in the argument list for RegTypeStructByAddr */
	int vret_arg_index;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
};

#define PARAM_REGS 4
#define FP_PARAM_REGS 8

typedef struct {
	/* General registers */
	host_mgreg_t gregs [PARAM_REGS];
	/* Floating registers */
	float fregs [FP_PARAM_REGS * 2];
	/* Stack usage, used for passing params on stack */
	guint32 stack_size;
	guint8 *stack;
} CallContext;

/* Structure used by the sequence points in AOTed code */
struct SeqPointInfo {
	gpointer ss_trigger_page;
	gpointer bp_trigger_page;
	gpointer ss_tramp_addr;
	guint8* bp_addrs [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	double fpregs [FP_PARAM_REGS];
	host_mgreg_t res, res2;
	guint8 *ret;
	guint32 has_fpregs;
	guint32 n_stackargs;
	/* This should come last as the structure is dynamically extended */
	host_mgreg_t regs [PARAM_REGS];
} DynCallArgs;

void arm_patch (guchar *code, const guchar *target);
guint8* mono_arm_emit_load_imm (guint8 *code, int dreg, guint32 val);
int mono_arm_is_rotated_imm8 (guint32 val, gint *rot_amount);

void
mono_arm_throw_exception_by_token (guint32 type_token, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs);

gpointer
mono_arm_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg, double *caller_fregs, double *callee_fregs);

typedef enum {
	MONO_ARM_FPU_NONE = 0,
	MONO_ARM_FPU_VFP = 1,
	MONO_ARM_FPU_VFP_HARD = 2
} MonoArmFPU;

/* keep the size of the structure a multiple of 8 */
struct MonoLMF {
	/* 
	 * If the second lowest bit is set to 1, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 */
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	/* This is only set in trampoline LMF frames */
	MonoMethod *method;
	host_mgreg_t sp;
	host_mgreg_t ip;
	host_mgreg_t fp;
	/* Currently only used in trampolines on armhf to hold d0-d15. We don't really
	 * need to put d0-d7 in the LMF, but it simplifies the trampoline code.
	 */
	double     fregs [16];
	/* all but sp and pc: matches the PUSH instruction layout in the trampolines
	 * 0-4 should be considered undefined (execpt in the magic tramp)
	 * sp is saved at IP.
	 */
	host_mgreg_t iregs [14];
};

typedef struct MonoCompileArch {
	MonoInst *seq_point_info_var;
	MonoInst *ss_trigger_page_var;
	MonoInst *seq_point_ss_method_var;
	MonoInst *seq_point_bp_method_var;
	MonoInst *vret_addr_loc;
	gboolean omit_fp;
	gboolean omit_fp_computed;
	CallInfo *cinfo;
	MonoInst *vfp_scratch_slots [2];
	int atomic_tmp_offset;
	guint8 *thunks;
	int thunks_size;
} MonoCompileArch;

#define MONO_ARCH_EMULATE_FCONV_TO_U4 1
#define MONO_ARCH_EMULATE_FCONV_TO_I8 1
#define MONO_ARCH_EMULATE_FCONV_TO_U8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_EMULATE_DIV 1
#define MONO_ARCH_EMULATE_CONV_R8_UN 1
#define MONO_ARCH_EMULATE_MUL_OVF 1

#define ARM_FIRST_ARG_REG 0
#define ARM_LAST_ARG_REG 3

#define MONO_ARCH_USE_SIGACTION 1

#if defined(HOST_WATCHOS)
#undef MONO_ARCH_USE_SIGACTION
#endif

#define MONO_ARCH_NEED_DIV_CHECK 1

#define MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE 1

#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS 1

#define MONO_ARCH_INTERPRETER_SUPPORTED 1
#define MONO_ARCH_AOT_SUPPORTED 1
#define MONO_ARCH_LLVM_SUPPORTED 1

#define MONO_ARCH_GSHARED_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_PARAM_AREA 0

#define MONO_ARCH_HAVE_OP_TAILCALL_MEMBASE 1
#define MONO_ARCH_HAVE_OP_TAILCALL_REG 1

#if !(defined(TARGET_ANDROID) && defined(MONO_CROSS_COMPILE))
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1
#endif

#define MONO_ARCH_HAVE_EXCEPTIONS_INIT 1
#define MONO_ARCH_HAVE_GET_TRAMPOLINES 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1
#define MONO_ARCH_GC_MAPS_SUPPORTED 1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GSHAREDVT_SUPPORTED 1
#define MONO_ARCH_HAVE_GENERAL_RGCTX_LAZY_FETCH_TRAMPOLINE 1
#define MONO_ARCH_HAVE_OPCODE_NEEDS_EMULATION 1
#define MONO_ARCH_HAVE_OBJC_GET_SELECTOR 1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES 1
#define MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT 1
#define MONO_ARCH_FLOAT32_SUPPORTED 1
#define MONO_ARCH_LLVM_TARGET_LAYOUT "e-p:32:32-n32-S64"

#define MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE 1
#define MONO_ARCH_HAVE_FTNPTR_ARG_TRAMPOLINE 1
#define MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP 1
#define MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED 1

#if defined(TARGET_WATCHOS) || (defined(__linux__) && !defined(TARGET_ANDROID))
#define MONO_ARCH_DISABLE_HW_TRAPS 1
#define MONO_ARCH_HAVE_UNWIND_BACKTRACE 1
#endif

/* ARM doesn't have too many registers, so we have to use a callee saved one */
#define MONO_ARCH_RGCTX_REG ARMREG_V5
#define MONO_ARCH_IMT_REG MONO_ARCH_RGCTX_REG
/* First argument reg */
#define MONO_ARCH_VTABLE_REG ARMREG_R0

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 0

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->regs [0] = (gsize)exc; } while (0)

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,func) do {	\
		MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0));	\
		MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0));	\
		MONO_CONTEXT_SET_IP ((ctx), (func));	\
	} while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf)

void
mono_arm_throw_exception (MonoObject *exc, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean preserve_ips);

void
mono_arm_throw_exception_by_token (guint32 type_token, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs);

void
mono_arm_resume_unwind (guint32 dummy1, host_mgreg_t pc, host_mgreg_t sp, host_mgreg_t *int_regs, gdouble *fp_regs);

gboolean
mono_arm_thumb_supported (void);

gboolean
mono_arm_eabi_supported (void);

int
mono_arm_i8_align (void);

GSList*
mono_arm_get_exception_trampolines (gboolean aot);

guint8*
mono_arm_get_thumb_plt_entry (guint8 *code);

guint8*
mono_arm_patchable_b (guint8 *code, int cond);

guint8*
mono_arm_patchable_bl (guint8 *code, int cond);

gboolean
mono_arm_is_hard_float (void);

void
mono_arm_unaligned_stack (MonoMethod *method);

/* MonoJumpInfo **ji */
guint8*
mono_arm_emit_aotconst (gpointer ji, guint8 *code, guint8 *buf, int dreg, int patch_type, gconstpointer data);

CallInfo*
mono_arch_get_call_info (MonoMemPool *mp, MonoMethodSignature *sig);

#endif /* __MONO_MINI_ARM_H__ */
