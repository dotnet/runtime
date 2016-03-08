#ifndef __MONO_MINI_AMD64_H__
#define __MONO_MINI_AMD64_H__

#include <mono/arch/amd64/amd64-codegen.h>
#include <mono/utils/mono-sigcontext.h>
#include <mono/utils/mono-context.h>
#include <glib.h>

#ifdef __native_client_codegen__
#define kNaClAlignmentAMD64 32
#define kNaClAlignmentMaskAMD64 (kNaClAlignmentAMD64 - 1)

/* TODO: use kamd64NaClLengthOfCallImm    */
/* temporarily using kNaClAlignmentAMD64 so padding in */
/* image-writer.c doesn't happen                       */
#define kNaClLengthOfCallImm kNaClAlignmentAMD64

int is_nacl_call_reg_sequence (guint8* code);
void amd64_nacl_clear_legacy_prefix_tag ();
void amd64_nacl_tag_legacy_prefix (guint8* code);
void amd64_nacl_tag_rex (guint8* code);
guint8* amd64_nacl_get_legacy_prefix_tag ();
guint8* amd64_nacl_get_rex_tag ();
void amd64_nacl_instruction_pre ();
void amd64_nacl_instruction_post (guint8 **start, guint8 **end);
void amd64_nacl_membase_handler (guint8** code, gint8 basereg, gint32 offset, gint8 dreg);
#endif

#ifdef HOST_WIN32
#include <windows.h>
/* use SIG* defines if possible */
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

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

typedef void (* MonoW32ExceptionHandler) (int _dummy, EXCEPTION_POINTERS *info, void *context);
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

#define MONO_ARCH_HAVE_RESTORE_STACK_SUPPORT 1

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

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == '\0') ? -1 : ((desc == 'i' ? -1 : ((desc == 'a') ? AMD64_RAX : ((desc == 's') ? AMD64_RCX : ((desc == 'd') ? AMD64_RDX : ((desc == 'A') ? MONO_AMD64_ARG_REG1 : -1)))))))

/* RDX is clobbered by the opcode implementation before accessing sreg2 */
#define MONO_ARCH_INST_SREG2_MASK(ins) (((ins [MONO_INST_CLOB] == 'a') || (ins [MONO_INST_CLOB] == 'd')) ? (1 << AMD64_RDX) : 0)

#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)

#define MONO_ARCH_FRAME_ALIGNMENT 16

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

/*This is the max size of the locals area of a given frame. I think 1MB is a safe default for now*/
#define MONO_ARCH_MAX_FRAME_SIZE 0x100000

struct MonoLMF {
	/* 
	 * If the lowest bit is set, then this LMF has the rip field set. Otherwise,
	 * the rip field is not set, and the rsp field points to the stack location where
	 * the caller ip is saved.
	 * If the second lowest bit is set, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 * If the third lowest bit is set, then this is a MonoLMFTramp structure, and
	 * the 'rbp' field is not valid.
	 */
	gpointer    previous_lmf;
#if defined(__default_codegen__) || defined(HOST_WIN32)
	guint64     rip;
#elif defined(__native_client_codegen__)
	/* On 64-bit compilers, default alignment is 8 for this field, */
	/* this allows the structure to match for 32-bit compilers.    */
	guint64     rip __attribute__ ((aligned(8)));
#endif
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
	gboolean omit_fp, omit_fp_computed;
	gpointer cinfo;
	gint32 async_point_count;
	gpointer vret_addr_loc;
#ifdef HOST_WIN32
	gpointer	unwindinfo;
#endif
	gpointer seq_point_info_var;
	gpointer ss_trigger_page_var;
	gpointer ss_tramp_var;
	gpointer bp_tramp_var;
	gpointer lmf_var;
} MonoCompileArch;

#ifdef TARGET_WIN32
#define PARAM_REGS 4
#define FLOAT_PARAM_REGS 4

static AMD64_Reg_No param_regs [] = { AMD64_RCX, AMD64_RDX, AMD64_R8, AMD64_R9 };

static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };
#else
#define PARAM_REGS 6
#define FLOAT_PARAM_REGS 8

static AMD64_Reg_No param_regs [] = { AMD64_RDI, AMD64_RSI, AMD64_RDX, AMD64_RCX, AMD64_R8, AMD64_R9 };

static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };
#endif

typedef struct {
	/* Method address to call */
	gpointer addr;
	/* The trampoline reads this, so keep the size explicit */
	int ret_marshal;
	/* If ret_marshal != NONE, this is the reg of the vret arg, else -1 (used in out case) */
	/* Equivalent of vret_arg_slot in the x86 implementation. */
	int vret_arg_reg;
	/* The stack slot where the return value will be stored (used in in case) */
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
typedef struct {
	gpointer ss_tramp_addr;
	gpointer bp_addrs [MONO_ZERO_LEN_ARRAY];
} SeqPointInfo;

typedef struct {
	mgreg_t regs [PARAM_REGS];
	mgreg_t res;
	guint8 *ret;
	double fregs [8];
	mgreg_t has_fp;
	guint8 buffer [256];
} DynCallArgs;

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->gregs [AMD64_RAX] = (gsize)exc; } while (0)
#define MONO_CONTEXT_SET_LLVM_EH_SELECTOR_REG(ctx, sel) do { (ctx)->gregs [AMD64_RDX] = (gsize)(sel); } while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf)

#ifdef _MSC_VER

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx, start_func) do { \
    guint64 stackptr; \
	mono_arch_flush_register_windows (); \
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
		mono_arch_flush_register_windows ();	\
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
		MONO_CONTEXT_SET_BP ((ctx), stackptr);	\
		MONO_CONTEXT_SET_SP ((ctx), stackptr);	\
	} while (0)

#endif

/*
 * some icalls like mono_array_new_va needs to be called using a different 
 * calling convention.
 */
#define MONO_ARCH_VARARG_ICALLS 1

#if (!defined( HOST_WIN32 ) && !defined(__native_client__) && !defined(__native_client_codegen__)) && defined (HAVE_SIGACTION)

#define MONO_ARCH_USE_SIGACTION 1

#ifdef HAVE_WORKING_SIGALTSTACK

#define MONO_ARCH_SIGSEGV_ON_ALTSTACK

#endif

#endif /* !HOST_WIN32 && !__native_client__ */

#if !defined(__linux__)
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

#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1

#define MONO_ARCH_ENABLE_MONO_LMF_VAR 1
#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_HAVE_TLS_GET (mono_amd64_have_tls_get ())
#define MONO_ARCH_IMT_REG AMD64_R10
#define MONO_ARCH_IMT_SCRATCH_REG AMD64_R11
#define MONO_ARCH_VTABLE_REG MONO_AMD64_ARG_REG1
/*
 * We use r10 for the imt/rgctx register rather than r11 because r11 is
 * used by the trampoline as a scratch register and hence might be
 * clobbered across method call boundaries.
 */
#define MONO_ARCH_RGCTX_REG MONO_ARCH_IMT_REG
#define MONO_ARCH_EXC_REG AMD64_RAX
#define MONO_ARCH_HAVE_CMOV_OPS 1
#define MONO_ARCH_HAVE_EXCEPTIONS_INIT 1
#define MONO_ARCH_HAVE_GENERALIZED_IMT_THUNK 1
#define MONO_ARCH_HAVE_LIVERANGE_OPS 1
#define MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX 1
#define MONO_ARCH_HAVE_GET_TRAMPOLINES 1

#define MONO_ARCH_AOT_SUPPORTED 1
#if !defined( __native_client__ )
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1
#endif

#define MONO_ARCH_SUPPORT_TASKLETS 1

#define MONO_ARCH_GSHARED_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_PARAM_AREA 0

#define MONO_ARCH_LLVM_SUPPORTED 1
#define MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD 1
#define MONO_ARCH_HAVE_CARD_TABLE_WBARRIER 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GC_MAPS_SUPPORTED 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK 1
#define MONO_ARCH_HAVE_CREATE_LLVM_NATIVE_THUNK 1
#define MONO_ARCH_GSHAREDVT_SUPPORTED 1
#define MONO_ARCH_HAVE_OP_TAIL_CALL 1
#define MONO_ARCH_HAVE_TRANSLATE_TLS_OFFSET 1
#define MONO_ARCH_HAVE_DUMMY_INIT 1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES 1
#define MONO_ARCH_HAVE_PATCH_CODE_NEW 1
#define MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT 1

#if defined(TARGET_OSX) || defined(__linux__)
#define MONO_ARCH_HAVE_UNWIND_BACKTRACE 1
#endif

#if defined(TARGET_OSX) || defined(__linux__)
#define MONO_ARCH_HAVE_TLS_GET_REG 1
#endif

#if defined(TARGET_APPLETVOS)
/* No signals */
#define MONO_ARCH_NEED_DIV_CHECK 1
#endif

/* Used for optimization, not complete */
#define MONO_ARCH_IS_OP_MEMBASE(opcode) ((opcode) == OP_X86_PUSH_MEMBASE)

#define MONO_ARCH_EMIT_BOUNDS_CHECK(cfg, array_reg, offset, index_reg) do { \
            MonoInst *inst; \
            MONO_INST_NEW ((cfg), inst, OP_AMD64_ICOMPARE_MEMBASE_REG); \
            inst->inst_basereg = array_reg; \
            inst->inst_offset = offset; \
            inst->sreg2 = index_reg; \
            MONO_ADD_INS ((cfg)->cbb, inst); \
            MONO_EMIT_NEW_COND_EXC (cfg, LE_UN, "IndexOutOfRangeException"); \
       } while (0)

void 
mono_amd64_patch (unsigned char* code, gpointer target);

void
mono_amd64_throw_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
							guint64 dummy5, guint64 dummy6,
							MonoContext *mctx, MonoObject *exc, gboolean rethrow);

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

guint64
mono_amd64_get_original_ip (void);

guint8*
mono_amd64_emit_tls_get (guint8* code, int dreg, int tls_offset);

gboolean
mono_amd64_have_tls_get (void);

GSList*
mono_amd64_get_exception_trampolines (gboolean aot);

int
mono_amd64_get_tls_gs_offset (void) MONO_LLVM_INTERNAL;

#ifdef TARGET_WIN32

void mono_arch_unwindinfo_add_push_nonvol (gpointer* monoui, gpointer codebegin, gpointer nextip, guchar reg );
void mono_arch_unwindinfo_add_set_fpreg (gpointer* monoui, gpointer codebegin, gpointer nextip, guchar reg );
void mono_arch_unwindinfo_add_alloc_stack (gpointer* monoui, gpointer codebegin, gpointer nextip, guint size );
guint mono_arch_unwindinfo_get_size (gpointer monoui);
void mono_arch_unwindinfo_install_unwind_info (gpointer* monoui, gpointer code, guint code_size);

#define MONO_ARCH_HAVE_UNWIND_TABLE 1
#endif

#endif /* __MONO_MINI_AMD64_H__ */  

