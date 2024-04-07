/**
 * \file
 */

#ifndef __MONO_MINI_AMD64_H__
#define __MONO_MINI_AMD64_H__

#include <mono/arch/amd64/amd64-codegen.h>
#include <mono/utils/mono-sigcontext.h>
#include <mono/utils/mono-context.h>
#include <glib.h>

#ifdef HOST_WIN32
#include <windows.h>
#include <signal.h>

#if !defined(_MSC_VER)
/* sigcontext surrogate */
struct sigcontext {
	guint64 eax;
	guint64 ebx;
	guint64 ecx;
	guint64 edx;
	guint64 ebp;
	guint64 esp;
	guint64 esi;
	guint64 edi;
	guint64 eip;
};
#endif

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

typedef struct {
	SRWLOCK lock;
	PVOID handle;
	gsize begin_range;
	gsize end_range;
	PRUNTIME_FUNCTION rt_funcs;
	DWORD rt_funcs_current_count;
	DWORD rt_funcs_max_count;
} DynamicFunctionTableEntry;

#define MONO_UNWIND_INFO_RT_FUNC_SIZE 128

typedef BOOLEAN (WINAPI* RtlInstallFunctionTableCallbackPtr)(
	DWORD64 TableIdentifier,
	DWORD64 BaseAddress,
	DWORD Length,
	PGET_RUNTIME_FUNCTION_CALLBACK Callback,
	PVOID Context,
	PCWSTR OutOfProcessCallbackDll);

typedef BOOLEAN (WINAPI* RtlDeleteFunctionTablePtr)(
	PRUNTIME_FUNCTION FunctionTable);

// On Win8/Win2012Server and later we can use dynamic growable function tables
// instead of RtlInstallFunctionTableCallback. This gives us the benefit to
// include all needed unwind upon registration.
typedef DWORD (NTAPI* RtlAddGrowableFunctionTablePtr)(
    PVOID * DynamicTable,
    PRUNTIME_FUNCTION FunctionTable,
    DWORD EntryCount,
    DWORD MaximumEntryCount,
    ULONG_PTR RangeBase,
    ULONG_PTR RangeEnd);

typedef VOID (NTAPI* RtlGrowFunctionTablePtr)(
    PVOID DynamicTable,
    DWORD NewEntryCount);

typedef VOID (NTAPI* RtlDeleteGrowableFunctionTablePtr)(
    PVOID DynamicTable);

#endif /* HOST_WIN32 */

#ifdef sun    // Solaris x86
#  undef SIGSEGV_ON_ALTSTACK
#  define MONO_ARCH_NOMAP32BIT

struct sigcontext {
        unsigned short gs, __gsh;
        unsigned short fs, __fsh;
        unsigned short es, __esh;
        unsigned short ds, __dsh;
        unsigned long edi;
        unsigned long esi;
        unsigned long ebp;
        unsigned long esp;
        unsigned long ebx;
        unsigned long edx;
        unsigned long ecx;
        unsigned long eax;
        unsigned long trapno;
        unsigned long err;
        unsigned long eip;
        unsigned short cs, __csh;
        unsigned long eflags;
        unsigned long esp_at_signal;
        unsigned short ss, __ssh;
        unsigned long fpstate[95];
      unsigned long filler[5];
};
#endif  // sun, Solaris x86

#ifndef DISABLE_SIMD
#define MONO_ARCH_SIMD_INTRINSICS 1
#define MONO_ARCH_NEED_SIMD_BANK 1
#define MONO_ARCH_USE_SHARED_FP_SIMD_BANK 1
#endif

#if defined(__APPLE__)
#define MONO_ARCH_SIGNAL_STACK_SIZE MINSIGSTKSZ
#else
#define MONO_ARCH_SIGNAL_STACK_SIZE (16 * 1024)
#endif

#define MONO_ARCH_CPU_SPEC mono_amd64_desc

#define MONO_MAX_IREGS 16

#define MONO_MAX_FREGS AMD64_XMM_NREG

#define MONO_ARCH_FP_RETURN_REG AMD64_XMM0

#ifdef TARGET_WIN32
/* xmm5 is used as a scratch register */
#define MONO_ARCH_CALLEE_FREGS 0x1f
/* xmm6:xmm15 */
#define MONO_ARCH_CALLEE_SAVED_FREGS (0xffff - 0x3f)
#define MONO_ARCH_FP_SCRATCH_REG AMD64_XMM5
#else
/* xmm15 is used as a scratch register */
#define MONO_ARCH_CALLEE_FREGS 0x7fff
#define MONO_ARCH_CALLEE_SAVED_FREGS 0
#define MONO_ARCH_FP_SCRATCH_REG AMD64_XMM15
#endif

#define MONO_MAX_XREGS MONO_MAX_FREGS

#define MONO_ARCH_CALLEE_XREGS MONO_ARCH_CALLEE_FREGS
#define MONO_ARCH_CALLEE_SAVED_XREGS MONO_ARCH_CALLEE_SAVED_FREGS


#define MONO_ARCH_CALLEE_REGS AMD64_CALLEE_REGS
#define MONO_ARCH_CALLEE_SAVED_REGS AMD64_CALLEE_SAVED_REGS

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == '\0') ? -1 : ((desc == 'i' ? -1 : ((desc == 'a') ? AMD64_RAX : ((desc == 's') ? AMD64_RCX : ((desc == 'd') ? AMD64_RDX : ((desc == 'A') ? MONO_AMD64_ARG_REG1 : -1)))))))

/* RDX is clobbered by the opcode implementation before accessing sreg2 */
#define MONO_ARCH_INST_SREG2_MASK(ins) (((ins [MONO_INST_CLOB] == 'a') || (ins [MONO_INST_CLOB] == 'd')) ? (1 << AMD64_RDX) : 0)

#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)

#define MONO_ARCH_FRAME_ALIGNMENT 16

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

struct MonoLMF {
	/*
	 * The rsp field points to the stack location where the caller ip is saved.
	 * If the second lowest bit is set, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 * If the third lowest bit is set, then this is a MonoLMFTramp structure, and
	 * the 'rbp' field is not valid.
	 */
	gpointer    previous_lmf;
	guint64     rbp;
	guint64     rsp;
};

/* LMF structure used by the JIT trampolines */
typedef struct {
	struct MonoLMF lmf;
	MonoContext *ctx;
	gpointer lmf_addr;
} MonoLMFTramp;

typedef struct MonoCompileArch {
	gint32 localloc_offset;
	gint32 reg_save_area_offset;
	gint32 stack_alloc_size;
	gint32 sp_fp_offset;
	guint32 saved_iregs;
	gboolean omit_fp;
	gboolean omit_fp_computed;
	CallInfo *cinfo;
	gint32 async_point_count;
	MonoInst *vret_addr_loc;
	MonoInst *seq_point_info_var;
	MonoInst *ss_tramp_var;
	MonoInst *bp_tramp_var;
	MonoInst *lmf_var;
	MonoInst *swift_error_var;
#ifdef HOST_WIN32
	struct _UNWIND_INFO* unwindinfo;
#endif
} MonoCompileArch;

#ifdef TARGET_WIN32

static const AMD64_Reg_No param_regs [] = { AMD64_RCX, AMD64_RDX, AMD64_R8, AMD64_R9 };

static const AMD64_XMM_Reg_No float_param_regs [] = { AMD64_XMM0, AMD64_XMM1, AMD64_XMM2, AMD64_XMM3 };

static const AMD64_Reg_No return_regs [] = { AMD64_RAX };

static const AMD64_XMM_Reg_No float_return_regs [] = { AMD64_XMM0 };

#define PARAM_REGS G_N_ELEMENTS(param_regs)
#define FLOAT_PARAM_REGS G_N_ELEMENTS(float_param_regs)
#define RETURN_REGS G_N_ELEMENTS(return_regs)
#define FLOAT_RETURN_REGS G_N_ELEMENTS(float_return_regs)

#else
#define PARAM_REGS 6
#define FLOAT_PARAM_REGS 8
#define RETURN_REGS 2
#define FLOAT_RETURN_REGS 2

static const AMD64_Reg_No param_regs [] = {AMD64_RDI, AMD64_RSI, AMD64_RDX,
					   AMD64_RCX, AMD64_R8,  AMD64_R9};

static const AMD64_XMM_Reg_No float_param_regs[] = {AMD64_XMM0, AMD64_XMM1, AMD64_XMM2,
						     AMD64_XMM3, AMD64_XMM4, AMD64_XMM5,
						     AMD64_XMM6, AMD64_XMM7};

static const AMD64_Reg_No return_regs [] = {AMD64_RAX, AMD64_RDX};
#endif

#define CTX_REGS 2
#define CTX_REGS_OFFSET AMD64_R12

typedef struct {
	/* Method address to call */
	gpointer addr;
	/* The trampoline reads this, so keep the size explicit */
	int ret_marshal;
	/* If ret_marshal != NONE, this is the reg of the vret arg, else -1 (used bu "out" case) */
	/* Equivalent of vret_arg_slot in the x86 implementation. */
	int vret_arg_reg;
	/* The stack slot where the return value will be stored (used by "in" case) */
	int vret_slot;
	int stack_usage, map_count;
	/* If not -1, then make a virtual call using this vtable offset */
	int vcall_offset;
	/* If 1, make an indirect call to the address in the rgctx reg */
	int calli;
	/* Whenever this is a in or an out call */
	int gsharedvt_in;
	/* Maps stack slots/registers in the caller to the stack slots/registers in the callee */
	int map [MONO_ZERO_LEN_ARRAY];
} GSharedVtCallInfo;

/* Structure used by the sequence points in AOTed code */
struct SeqPointInfo {
	gpointer ss_tramp_addr;
	gpointer bp_addrs [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	host_mgreg_t res;
	guint8 *ret;
	double fregs [8];
	host_mgreg_t has_fp;
	host_mgreg_t nstack_args;
	/* This should come last as the structure is dynamically extended */
	host_mgreg_t regs [PARAM_REGS];
} DynCallArgs;

typedef enum {
	ArgInIReg,
	ArgInFloatSSEReg,
	ArgInDoubleSSEReg,
	ArgOnStack,
	ArgValuetypeInReg,
	ArgValuetypeAddrInIReg,
	ArgValuetypeAddrOnStack,
	/* gsharedvt argument passed by addr */
	ArgGSharedVtInReg,
	ArgGSharedVtOnStack,
	/* Variable sized gsharedvt argument passed/returned by addr */
	ArgGsharedvtVariableInReg,
	ArgSwiftError,
	ArgNone /* only in pair_storage */
} ArgStorage;

typedef struct {
	gint16 offset;
	guint8  reg;
	ArgStorage storage : 8;

	/* Only if storage == ArgValuetypeInReg */
	ArgStorage pair_storage [2];
	guint8 pair_regs [2];
	/* The size of each pair (bytes) */
	int pair_size [2];
	int nregs;
	/* Only if storage == ArgOnStack */
	int arg_size; // Bytes, will always be rounded up/aligned to 8 byte boundary
	// Size in bytes for small arguments
	int byte_arg_size;
	guint8 pass_empty_struct : 1; // Set in scenarios when empty structs needs to be represented as argument.
	guint8 is_signed : 1;
} ArgInfo;

struct CallInfo {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	guint32 freg_usage;
	gint32 swift_error_index;
	gboolean need_stack_align;
	gboolean gsharedvt;
	/* The index of the vret arg in the argument list */
	int vret_arg_index;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
};

typedef struct {
	/* General registers */
	host_mgreg_t gregs [AMD64_NREG];
	/* Floating registers */
	double fregs [AMD64_XMM_NREG];
	/* Stack usage, used for passing params on stack */
	guint32 stack_size;
	guint8 *stack;
} CallContext;

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->gregs [AMD64_RAX] = (gsize)exc; } while (0)
#define MONO_CONTEXT_SET_LLVM_EH_SELECTOR_REG(ctx, sel) do { (ctx)->gregs [AMD64_RDX] = (gsize)(sel); } while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf)

#ifdef _MSC_VER

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx, start_func) do { \
    guint64 stackptr; \
	stackptr = ((guint64)_AddressOfReturnAddress () - sizeof (void*));\
	MONO_CONTEXT_SET_IP ((ctx), (start_func)); \
	MONO_CONTEXT_SET_BP ((ctx), stackptr); \
	MONO_CONTEXT_SET_SP ((ctx), stackptr); \
} while (0)

#else

/*
 * __builtin_frame_address () is broken on some older gcc versions in the presence of
 * frame pointer elimination, see bug #82095.
 */
#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
        int tmp; \
        guint64 stackptr = (guint64)&tmp; \
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
		MONO_CONTEXT_SET_BP ((ctx), stackptr);	\
		MONO_CONTEXT_SET_SP ((ctx), stackptr);	\
	} while (0)

#endif

#if !defined( HOST_WIN32 ) && !defined(__HAIKU__) && defined (HAVE_SIGACTION)

#define MONO_ARCH_USE_SIGACTION 1

#ifdef ENABLE_SIGALTSTACK

#define MONO_ARCH_SIGSEGV_ON_ALTSTACK

#endif

#endif /* !HOST_WIN32 */

#if !defined(__linux__) && !defined(__sun)
#define MONO_ARCH_NOMAP32BIT 1
#endif

#ifdef TARGET_WIN32
#define MONO_AMD64_ARG_REG1 AMD64_RCX
#define MONO_AMD64_ARG_REG2 AMD64_RDX
#define MONO_AMD64_ARG_REG3 AMD64_R8
#define MONO_AMD64_ARG_REG4 AMD64_R9
#else
#define MONO_AMD64_ARG_REG1 AMD64_RDI
#define MONO_AMD64_ARG_REG2 AMD64_RSI
#define MONO_AMD64_ARG_REG3 AMD64_RDX
#define MONO_AMD64_ARG_REG4 AMD64_RCX
#endif

#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS

#define MONO_ARCH_EMULATE_CONV_R8_UN 1
#define MONO_ARCH_EMULATE_FCONV_TO_U8 1
// x64 FullAOT+LLVM fails to pass the basic-float tests without this.
#define MONO_ARCH_EMULATE_FCONV_TO_U4 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1
#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_IMT_REG AMD64_R10
#define MONO_ARCH_IMT_SCRATCH_REG AMD64_R11
#define MONO_ARCH_VTABLE_REG MONO_AMD64_ARG_REG1
/*
 * We use r10 for the imt/rgctx register rather than r11 because r11 is
 * used by the trampoline as a scratch register and hence might be
 * clobbered across method call boundaries.
 */
#define MONO_ARCH_RGCTX_REG MONO_ARCH_IMT_REG
#define MONO_ARCH_HAVE_CMOV_OPS 1
#define MONO_ARCH_HAVE_EXCEPTIONS_INIT 1
#define MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE 1
#define MONO_ARCH_HAVE_GET_TRAMPOLINES 1

#define MONO_ARCH_INTERPRETER_SUPPORTED 1
#define MONO_ARCH_AOT_SUPPORTED 1
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1

#define MONO_ARCH_GSHARED_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_PARAM_AREA 0

#define MONO_ARCH_LLVM_SUPPORTED 1
#if defined(HOST_WIN32) && defined(TARGET_WIN32) && !defined(_MSC_VER)
// Only supported for Windows cross compiler builds, host == Win32, target != Win32
// and only using MSVC for none cross compiler builds.
#undef MONO_ARCH_LLVM_SUPPORTED
#endif

#define MONO_ARCH_HAVE_CARD_TABLE_WBARRIER 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GC_MAPS_SUPPORTED 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK 1
#define MONO_ARCH_HAVE_CREATE_LLVM_NATIVE_THUNK 1
#define MONO_ARCH_HAVE_OP_TAILCALL_MEMBASE 1
#define MONO_ARCH_HAVE_OP_TAILCALL_REG 1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES 1
#define MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT 1
#define MONO_ARCH_HAVE_GENERAL_RGCTX_LAZY_FETCH_TRAMPOLINE 1
#define MONO_ARCH_HAVE_PATCH_JUMP_TRAMPOLINE 1
#define MONO_ARCH_FLOAT32_SUPPORTED 1
#define MONO_ARCH_LLVM_TARGET_LAYOUT "e-i64:64-i128:128-n8:16:32:64-S128"

#define MONO_ARCH_HAVE_INTERP_PINVOKE_TRAMP
#define MONO_ARCH_HAVE_INTERP_ENTRY_TRAMPOLINE 1
#define MONO_ARCH_HAVE_INTERP_NATIVE_TO_MANAGED 1
// FIXME: Doesn't work on windows
//#define MONO_ARCH_HAVE_INIT_MRGCTX 1

#if defined(TARGET_OSX) || defined(__linux__)
#define MONO_ARCH_HAVE_UNWIND_BACKTRACE 1
#endif

#define MONO_ARCH_GSHAREDVT_SUPPORTED 1


#if defined(HOST_TVOS) || defined(HOST_WATCHOS)
/* Neither tvOS nor watchOS give signal handlers access to a ucontext_t, so we
 * can't use signals to translate SIGFPE into a .NET-level exception. */
#define MONO_ARCH_NEED_DIV_CHECK 1
#endif

#if defined(TARGET_TVOS) || defined(TARGET_WATCHOS)
#define MONO_ARCH_EXPLICIT_NULL_CHECKS 1
#endif

/* Used for optimization, not complete */
#define MONO_ARCH_IS_OP_MEMBASE(opcode) ((opcode) == OP_X86_PUSH_MEMBASE)

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 1

#if defined(TARGET_OSX) || defined(TARGET_APPLE_MOBILE)
#define MONO_ARCH_HAVE_SWIFTCALL 1
#endif

void
mono_amd64_patch (unsigned char* code, gpointer target);

void
mono_amd64_throw_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
							guint64 dummy5, guint64 dummy6,
							MonoContext *mctx, MonoObject *exc, gboolean rethrow, gboolean preserve_ips);

void
mono_amd64_throw_corlib_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
								   guint64 dummy5, guint64 dummy6,
								   MonoContext *mctx, guint32 ex_token_index, gint64 pc_offset);

void
mono_amd64_resume_unwind (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
						  guint64 dummy5, guint64 dummy6,
						  MonoContext *mctx, guint32 dummy7, gint64 dummy8);

gpointer
mono_amd64_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg);

GSList*
mono_amd64_get_exception_trampolines (gboolean aot);

int
mono_amd64_get_tls_gs_offset (void);

#if defined(TARGET_WIN32) && !defined(DISABLE_JIT)

#define MONO_ARCH_HAVE_UNWIND_TABLE 1
#define MONO_ARCH_HAVE_CODE_CHUNK_TRACKING 1

#ifdef ENABLE_CHECKED_BUILD
#define ENABLE_CHECKED_BUILD_UNWINDINFO
#endif

#define MONO_MAX_UNWIND_CODES 22

typedef enum _UNWIND_OP_CODES {
    UWOP_PUSH_NONVOL = 0, /* info == register number */
    UWOP_ALLOC_LARGE,     /* no info, alloc size in next 2 slots */
    UWOP_ALLOC_SMALL,     /* info == size of allocation / 8 - 1 */
    UWOP_SET_FPREG,       /* no info, FP = RSP + UNWIND_INFO.FPRegOffset*16 */
    UWOP_SAVE_NONVOL,     /* info == register number, offset in next slot */
    UWOP_SAVE_NONVOL_FAR, /* info == register number, offset in next 2 slots */
    UWOP_SAVE_XMM128,     /* info == XMM reg number, offset in next slot */
    UWOP_SAVE_XMM128_FAR, /* info == XMM reg number, offset in next 2 slots */
    UWOP_PUSH_MACHFRAME   /* info == 0: no error-code, 1: error-code */
} UNWIND_CODE_OPS;

typedef union _UNWIND_CODE {
    struct {
        guchar CodeOffset;
        guchar UnwindOp : 4;
        guchar OpInfo   : 4;
    } UnwindCode;
    gushort FrameOffset;
} UNWIND_CODE, *PUNWIND_CODE;

typedef struct _UNWIND_INFO {
	guchar Version       : 3;
	guchar Flags         : 5;
	guchar SizeOfProlog;
	guchar CountOfCodes;
	guchar FrameRegister : 4;
	guchar FrameOffset   : 4;
	UNWIND_CODE UnwindCode[MONO_MAX_UNWIND_CODES];
/*	UNWIND_CODE MoreUnwindCode[((CountOfCodes + 1) & ~1) - 1];
 *	union {
 *		OPTIONAL ULONG ExceptionHandler;
 *		OPTIONAL ULONG FunctionEntry;
 *	};
 *	OPTIONAL ULONG ExceptionData[]; */
} UNWIND_INFO, *PUNWIND_INFO;

static inline guint
mono_arch_unwindinfo_get_size (guchar code_count)
{
	// Returned size will be used as the allocated size for unwind data trailing the memory used by compiled method.
	// Windows x64 ABI have some requirements on the data written into this memory. Both the RUNTIME_FUNCTION
	// and UNWIND_INFO struct needs to be DWORD aligned and the number of elements in unwind codes array
	// should have an even number of entries, while the count stored in UNWIND_INFO struct should hold the real number
	// of unwind codes. Adding extra bytes to the total size will make sure we can properly align the RUNTIME_FUNCTION
	// struct. Since our UNWIND_INFO follows RUNTIME_FUNCTION struct in memory, it will automatically be DWORD aligned
	// as well. Also make sure to allocate room for a padding UNWIND_CODE, if needed.
	return (sizeof (target_mgreg_t) + sizeof (UNWIND_INFO)) -
		(sizeof (UNWIND_CODE) * ((MONO_MAX_UNWIND_CODES - ((code_count + 1) & ~1))));
/* FIXME Something simpler should work:
	return sizeof (UNWIND_INFO) + sizeof (UNWIND_CODE) * (code_count + (code_count & 1));
*/
}

guchar
mono_arch_unwindinfo_get_code_count (GSList *unwind_ops);

PUNWIND_INFO
mono_arch_unwindinfo_alloc_unwind_info (GSList *unwind_ops);

void
mono_arch_unwindinfo_free_unwind_info (PUNWIND_INFO unwind_info);

guint
mono_arch_unwindinfo_init_method_unwind_info (gpointer cfg);

void
mono_arch_unwindinfo_install_method_unwind_info (PUNWIND_INFO *monoui, gpointer code, guint code_size);

void
mono_arch_unwindinfo_install_tramp_unwind_info (GSList *unwind_ops, gpointer code, guint code_size);

void
mono_arch_code_chunk_new (void *chunk, int size);

void
mono_arch_code_chunk_destroy (void *chunk);

#endif /* defined(TARGET_WIN32) && !defined(DISABLE_JIT) */

#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
// Allocate additional size for max 3 unwind ops (push + fp or sp small|large) + unwind info struct trailing code buffer.
#define MONO_TRAMPOLINE_UNWINDINFO_SIZE(max_code_count) (mono_arch_unwindinfo_get_size (max_code_count))
#define MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE (MONO_TRAMPOLINE_UNWINDINFO_SIZE(3))

static inline gboolean
mono_arch_unwindinfo_validate_size (GSList *unwind_ops, guint max_size)
{
	guint current_size = mono_arch_unwindinfo_get_size (mono_arch_unwindinfo_get_code_count (unwind_ops));
	return current_size <= max_size;
}

#else

#define MONO_TRAMPOLINE_UNWINDINFO_SIZE(max_code_count) 0
#define MONO_MAX_TRAMPOLINE_UNWINDINFO_SIZE 0

static inline gboolean
mono_arch_unwindinfo_validate_size (GSList *unwind_ops, guint max_size)
{
	return TRUE;
}
#endif

CallInfo* mono_arch_get_call_info (MonoMemPool *mp, MonoMethodSignature *sig);

#endif /* __MONO_MINI_AMD64_H__ */

