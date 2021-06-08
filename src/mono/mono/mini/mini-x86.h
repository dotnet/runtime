/**
 * \file
 */

#ifndef __MONO_MINI_X86_H__
#define __MONO_MINI_X86_H__

#include <mono/arch/x86/x86-codegen.h>
#include <mono/utils/mono-sigcontext.h>
#include <mono/utils/mono-context.h>

#ifdef HOST_WIN32
#include <windows.h>
#include <signal.h>

typedef void MONO_SIG_HANDLER_SIGNATURE ((*MonoW32ExceptionHandler));

void win32_seh_init(void);
void win32_seh_cleanup(void);
void win32_seh_set_handler(int type, MonoW32ExceptionHandler handler);

#ifndef SIGFPE
#define SIGFPE 4
#endif

#ifndef SIGILL
#define SIGILL 8
#endif

#ifndef	SIGSEGV
#define	SIGSEGV 11
#endif

LONG CALLBACK seh_handler(EXCEPTION_POINTERS* ep);

#endif /* HOST_WIN32 */

#if defined( __linux__) || defined(__sun) || defined(__APPLE__) || defined(__NetBSD__) || \
       defined(__FreeBSD__) || defined(__FreeBSD_kernel__) || defined(__OpenBSD__)
#define MONO_ARCH_USE_SIGACTION
#endif

#if defined(HOST_WATCHOS)
#undef MONO_ARCH_USE_SIGACTION
#endif

#ifndef HOST_WIN32

#ifdef HAVE_WORKING_SIGALTSTACK
/* 
 * solaris doesn't have pthread_getattr_np () needed by the sigaltstack setup
 * code.
 */
#ifndef __sun
#define MONO_ARCH_SIGSEGV_ON_ALTSTACK
#endif
/* Haiku doesn't have SA_SIGINFO */
#ifndef __HAIKU__
#define MONO_ARCH_USE_SIGACTION
#endif /* __HAIKU__ */

#endif /* HAVE_WORKING_SIGALTSTACK */
#endif /* !HOST_WIN32 */

#define MONO_ARCH_SUPPORT_TASKLETS 1

/* we should lower this size and make sure we don't call heavy stack users in the segv handler */
#if defined(__APPLE__)
#define MONO_ARCH_SIGNAL_STACK_SIZE MINSIGSTKSZ
#else
#define MONO_ARCH_SIGNAL_STACK_SIZE (16 * 1024)
#endif

#define MONO_ARCH_CPU_SPEC mono_x86_desc

#define MONO_MAX_IREGS 8
#define MONO_MAX_FREGS 8
#define MONO_MAX_XREGS 8

/* Parameters used by the register allocator */
#define MONO_ARCH_CALLEE_REGS X86_CALLEE_REGS
#define MONO_ARCH_CALLEE_SAVED_REGS X86_CALLER_REGS

#define MONO_ARCH_CALLEE_FREGS (0xff & ~(regmask (MONO_ARCH_FPSTACK_SIZE)))
#define MONO_ARCH_CALLEE_SAVED_FREGS 0

/* All registers are clobered by a call */
#define MONO_ARCH_CALLEE_XREGS (0xff & ~(regmask (MONO_MAX_XREGS)))
#define MONO_ARCH_CALLEE_SAVED_XREGS 0

#define MONO_ARCH_USE_FPSTACK TRUE
#define MONO_ARCH_FPSTACK_SIZE 6

#define MONO_ARCH_INST_FIXED_REG(desc) (((desc == ' ') || (desc == 'i')) ? -1 : ((desc == 's') ? X86_ECX : ((desc == 'a') ? X86_EAX : ((desc == 'd') ? X86_EDX : ((desc == 'l') ? X86_EAX : -1)))))

#define MONO_ARCH_INST_FIXED_MASK(desc) ((desc == 'y') ? (X86_BYTE_REGS) : 0)

/* RDX is clobbered by the opcode implementation before accessing sreg2 */
/* 
 * Originally this contained X86_EDX for div/rem opcodes, but that led to unsolvable 
 * situations since there are only 3 usable registers for local register allocation.
 * Instead, we handle the sreg2==edx case in the opcodes.
 */
#define MONO_ARCH_INST_SREG2_MASK(ins) 0

/*
 * L is a generic register pair, while l means eax:rdx
 */
#define MONO_ARCH_INST_IS_REGPAIR(desc) (desc == 'l' || desc == 'L')
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (desc == 'l' ? X86_EDX : -1)

/* must be at a power of 2 and >= 8 */
#define MONO_ARCH_FRAME_ALIGNMENT 16

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

/*This is the max size of the locals area of a given frame. I think 1MB is a safe default for now*/
#define MONO_ARCH_MAX_FRAME_SIZE 0x100000

/*This is how much a try block must be extended when is is preceeded by a Monitor.Enter() call.
It's 4 bytes as this is how many bytes + 1 that 'add 0x10, %esp' takes. It is used to pop the arguments from
the monitor.enter call and must be already protected.*/
#define MONO_ARCH_MONITOR_ENTER_ADJUSTMENT 4

struct MonoLMF {
	/* 
	 * If the lowest bit is set to 1, then this is a trampoline LMF frame.
	 * If the second lowest bit is set to 1, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 */
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	/* Only set in trampoline LMF frames */
	MonoMethod *method;
	/* Only set in trampoline LMF frames */
	guint32     esp;
	guint32     ebx;
	guint32     edi;
	guint32     esi;
	guint32     ebp;
	guint32     eip;
};

typedef struct {
	gboolean need_stack_frame_inited;
	gboolean need_stack_frame;
	int sp_fp_offset, param_area_size;
	CallInfo *cinfo;
	MonoInst *ss_tramp_var;
	MonoInst *bp_tramp_var;
} MonoCompileArch;

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->eax = (gsize)exc; } while (0)

#if defined(HOST_WIN32)
#define __builtin_extract_return_addr(x) x
#define __builtin_return_address(x) _ReturnAddress()
#define __builtin_frame_address(x) _AddressOfReturnAddress()
#endif

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
		MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0));	\
		MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0));	\
	} while (0)


#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { (lmf)->ebp = -1; } while (0)

/* Enables OP_LSHL, OP_LSHL_IMM, OP_LSHR, OP_LSHR_IMM, OP_LSHR_UN, OP_LSHR_UN_IMM */
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS

#define MONO_ARCH_EMULATE_FCONV_TO_U8 1
#define MONO_ARCH_EMULATE_FCONV_TO_U4 1

#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1
#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_NEED_GOT_VAR 1
#define MONO_ARCH_IMT_REG X86_EDX
#define MONO_ARCH_VTABLE_REG X86_EDX
#define MONO_ARCH_RGCTX_REG MONO_ARCH_IMT_REG
#define MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_GOT_REG X86_EBX
#define MONO_ARCH_HAVE_GET_TRAMPOLINES 1
#define MONO_ARCH_HAVE_GENERAL_RGCTX_LAZY_FETCH_TRAMPOLINE 1

#define MONO_ARCH_INTERPRETER_SUPPORTED 1
#define MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED 1
#define MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP 1

#define MONO_ARCH_HAVE_CMOV_OPS 1

#ifdef MONO_ARCH_SIMD_INTRINSICS
#define MONO_ARCH_HAVE_DECOMPOSE_OPTS 1
#endif

#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS 1

#define MONO_ARCH_AOT_SUPPORTED 1

#define MONO_ARCH_GSHARED_SUPPORTED 1

#define MONO_ARCH_LLVM_SUPPORTED 1
#if defined(HOST_WIN32) && defined(TARGET_WIN32)
// Only supported for Windows cross compiler builds, host == Win32, target != Win32.
#undef MONO_ARCH_LLVM_SUPPORTED
#endif

#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1

#define MONO_ARCH_HAVE_EXCEPTIONS_INIT 1

#define MONO_ARCH_HAVE_CARD_TABLE_WBARRIER 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GC_MAPS_SUPPORTED 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK 1
#define MONO_ARCH_GSHAREDVT_SUPPORTED 1
#define MONO_ARCH_HAVE_OP_TAILCALL_MEMBASE 1
#define MONO_ARCH_HAVE_OP_TAILCALL_REG 1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES 1
#define MONO_ARCH_LLVM_TARGET_LAYOUT "e-p:32:32-n32-S128"

/* Used for optimization, not complete */
#define MONO_ARCH_IS_OP_MEMBASE(opcode) ((opcode) == OP_X86_PUSH_MEMBASE)

#define MONO_ARCH_EMIT_BOUNDS_CHECK(cfg, array_reg, offset, index_reg, ex_name) do { \
            MonoInst *inst; \
            MONO_INST_NEW ((cfg), inst, OP_X86_COMPARE_MEMBASE_REG); \
            inst->inst_basereg = array_reg; \
            inst->inst_offset = offset; \
            inst->sreg2 = index_reg; \
            MONO_ADD_INS ((cfg)->cbb, inst); \
			MONO_EMIT_NEW_COND_EXC (cfg, LE_UN, ex_name); \
	} while (0)

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 1

/* Return value marshalling for calls between gsharedvt and normal code */
typedef enum {
	GSHAREDVT_RET_NONE = 0,
	GSHAREDVT_RET_IREGS = 1,
	GSHAREDVT_RET_DOUBLE_FPSTACK = 2,
	GSHAREDVT_RET_FLOAT_FPSTACK = 3,
	GSHAREDVT_RET_STACK_POP = 4,
	GSHAREDVT_RET_I1 = 5,
	GSHAREDVT_RET_U1 = 6,
	GSHAREDVT_RET_I2 = 7,
	GSHAREDVT_RET_U2 = 8,
	GSHAREDVT_RET_IREG = 9
} GSharedVtRetMarshal;

typedef struct {
	/* Method address to call */
	gpointer addr;
	/* The trampoline reads this, so keep the size explicit */
	int ret_marshal;
	/* If ret_marshal != NONE, this is the stack slot of the vret arg, else -1 */
	int vret_arg_slot;
	/* The stack slot where the return value will be stored */
	int vret_slot;
	int stack_usage, map_count;
	/* If not -1, then make a virtual call using this vtable offset */
	int vcall_offset;
	/* If 1, make an indirect call to the address in the rgctx reg */
	int calli;
	/* Whenever this is a in or an out call */
	int gsharedvt_in;
	int map [MONO_ZERO_LEN_ARRAY];
} GSharedVtCallInfo;

typedef enum {
	ArgInIReg,
	ArgInFloatSSEReg,
	ArgInDoubleSSEReg,
	ArgOnStack,
	ArgValuetypeInReg,
	ArgOnFloatFpStack,
	ArgOnDoubleFpStack,
	/* gsharedvt argument passed by addr */
	ArgGSharedVt,
	ArgNone
} ArgStorage;

typedef struct {
	gint16 offset;
	gint8  reg;
	ArgStorage storage;
	int nslots;
	gboolean is_pair;

	/* Only if storage == ArgValuetypeInReg */
	ArgStorage pair_storage [2];
	gint8 pair_regs [2];
	guint8 pass_empty_struct : 1; // Set in scenarios when empty structs needs to be represented as argument.
} ArgInfo;

struct CallInfo {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	guint32 freg_usage;
	gboolean need_stack_align;
	guint32 stack_align_amount;
	gboolean vtype_retaddr;
	/* The index of the vret arg in the argument list */
	int vret_arg_index;
	int vret_arg_offset;
	/* Argument space popped by the callee */
	int callee_stack_pop;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
};

typedef struct {
	/* EAX:EDX */
	host_mgreg_t eax;
	host_mgreg_t edx;
	/* Floating point return value read from the top of x86 fpstack */
	double fret;
	/* Stack usage, used for passing params on stack */
	guint32 stack_size;
	guint8 *stack;
} CallContext;

guint32
mono_x86_get_this_arg_offset (MonoMethodSignature *sig);

void
mono_x86_throw_exception (host_mgreg_t *regs, MonoObject *exc,
						  host_mgreg_t eip, gboolean rethrow, gboolean preserve_ips);

void
mono_x86_throw_corlib_exception (host_mgreg_t *regs, guint32 ex_token_index,
								 host_mgreg_t eip, gint32 pc_offset);

void
mono_x86_patch (unsigned char* code, gpointer target);

gpointer
mono_x86_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg);

CallInfo*
mono_arch_get_call_info (MonoMemPool *mp, MonoMethodSignature *sig);

#endif /* __MONO_MINI_X86_H__ */  

