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

typedef void (* MonoW32ExceptionHandler) (int _dummy, EXCEPTION_RECORD *info, void *context);
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

#define MONO_ARCH_CPU_SPEC amd64_desc

#define MONO_MAX_IREGS 16

#define MONO_MAX_FREGS AMD64_XMM_NREG

#define MONO_ARCH_FP_RETURN_REG AMD64_XMM0

/* xmm15 is reserved for use by some opcodes */
#define MONO_ARCH_CALLEE_FREGS 0x7fff
#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define MONO_MAX_XREGS MONO_MAX_FREGS

#define MONO_ARCH_CALLEE_XREGS 0x7fff
#define MONO_ARCH_CALLEE_SAVED_XREGS 0


#define MONO_ARCH_CALLEE_REGS AMD64_CALLEE_REGS
#define MONO_ARCH_CALLEE_SAVED_REGS AMD64_CALLEE_SAVED_REGS

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == '\0') ? -1 : ((desc == 'i' ? -1 : ((desc == 'a') ? AMD64_RAX : ((desc == 's') ? AMD64_RCX : ((desc == 'd') ? AMD64_RDX : -1))))))

/* RDX is clobbered by the opcode implementation before accessing sreg2 */
#define MONO_ARCH_INST_SREG2_MASK(ins) (((ins [MONO_INST_CLOB] == 'a') || (ins [MONO_INST_CLOB] == 'd')) ? (1 << AMD64_RDX) : 0)

#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)

#define MONO_ARCH_FRAME_ALIGNMENT 16

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

#define MONO_ARCH_RETREG1 X86_EAX
#define MONO_ARCH_RETREG2 X86_EDX

/*This is the max size of the locals area of a given frame. I think 1MB is a safe default for now*/
#define MONO_ARCH_MAX_FRAME_SIZE 0x100000

struct MonoLMF {
	/* 
	 * If the lowest bit is set to 1, then this LMF has the rip field set. Otherwise,
	 * the rip field is not set, and the rsp field points to the stack location where
	 * the caller ip is saved.
	 * If the second lowest bit is set to 1, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 */
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	/* This is only set in trampoline LMF frames */
	MonoMethod *method;
#if defined(__default_codegen__) || defined(HOST_WIN32)
	guint64     rip;
#elif defined(__native_client_codegen__)
	/* On 64-bit compilers, default alignment is 8 for this field, */
	/* this allows the structure to match for 32-bit compilers.    */
	guint64     rip __attribute__ ((aligned(8)));
#endif
	guint64     rbx;
	guint64     rbp;
	guint64     rsp;
	guint64     r12;
	guint64     r13;
	guint64     r14;
	guint64     r15;
#ifdef HOST_WIN32
	guint64     rdi;
	guint64     rsi;
#endif
};

typedef struct MonoCompileArch {
	gint32 localloc_offset;
	gint32 reg_save_area_offset;
	gint32 stack_alloc_size;
	gint32 sp_fp_offset;
	gboolean omit_fp, omit_fp_computed, no_pushes;
	gpointer cinfo;
	gint32 async_point_count;
	gpointer vret_addr_loc;
#ifdef HOST_WIN32
	gpointer	unwindinfo;
#endif
	gpointer seq_point_info_var;
	gpointer ss_trigger_page_var;
	gpointer lmf_var;
} MonoCompileArch;

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->rax = (gsize)exc; } while (0)

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

#if !defined( HOST_WIN32 ) && !defined(__native_client__) && !defined(__native_client_codegen__)

#define MONO_ARCH_USE_SIGACTION 1

#ifdef HAVE_WORKING_SIGALTSTACK

#define MONO_ARCH_SIGSEGV_ON_ALTSTACK

#endif

#endif /* !HOST_WIN32 && !__native_client__ */

#if defined (__APPLE__)

#define MONO_ARCH_NOMAP32BIT

#elif defined (__NetBSD__)

#define REG_RAX 14
#define REG_RCX 3
#define REG_RDX 2
#define REG_RBX 13
#define REG_RSP 24
#define REG_RBP 12
#define REG_RSI 1
#define REG_RDI 0
#define REG_R8 4
#define REG_R9 5
#define REG_R10 6
#define REG_R11 7
#define REG_R12 8
#define REG_R13 9
#define REG_R14 10
#define REG_R15 11
#define REG_RIP 21

#define MONO_ARCH_NOMAP32BIT

#elif defined (__OpenBSD__)

#define MONO_ARCH_NOMAP32BIT

#elif defined (__DragonFly__)

#define MONO_ARCH_NOMAP32BIT

#elif defined (__FreeBSD__)

#define REG_RAX 7
#define REG_RCX 4
#define REG_RDX 3
#define REG_RBX 8
#define REG_RSP 23
#define REG_RBP 9
#define REG_RSI 2
#define REG_RDI 1
#define REG_R8  5
#define REG_R9  6
#define REG_R10 10
#define REG_R11 11
#define REG_R12 12
#define REG_R13 13
#define REG_R14 14
#define REG_R15 15
#define REG_RIP 20

/* 
 * FreeBSD does not have MAP_32BIT, so code allocated by the code manager might not have a
 * 32 bit address.
 */
#define MONO_ARCH_NOMAP32BIT

#endif /* __FreeBSD__ */

#ifdef HOST_WIN32
#define MONO_AMD64_ARG_REG1 AMD64_RCX
#define MONO_AMD64_ARG_REG2 AMD64_RDX
#else
#define MONO_AMD64_ARG_REG1 AMD64_RDI
#define MONO_AMD64_ARG_REG2 AMD64_RSI
#endif

#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS

#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1

#define MONO_ARCH_ENABLE_REGALLOC_IN_EH_BLOCKS 1
#if !defined(__APPLE__)
#define MONO_ARCH_ENABLE_MONO_LMF_VAR 1
#endif
#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE 1
#define MONO_ARCH_HAVE_ATOMIC_ADD 1
#define MONO_ARCH_HAVE_ATOMIC_EXCHANGE 1
#define MONO_ARCH_HAVE_ATOMIC_CAS 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_HAVE_IMT 1
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
#define MONO_ARCH_HAVE_CMOV_OPS 1
#define MONO_ARCH_HAVE_NOTIFY_PENDING_EXC 1
#define MONO_ARCH_HAVE_EXCEPTIONS_INIT 1
#define MONO_ARCH_ENABLE_GLOBAL_RA 1
#define MONO_ARCH_HAVE_GENERALIZED_IMT_THUNK 1
#define MONO_ARCH_HAVE_LIVERANGE_OPS 1
#define MONO_ARCH_HAVE_XP_UNWIND 1
#define MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX 1
#if !defined(HOST_WIN32)
#define MONO_ARCH_MONITOR_OBJECT_REG MONO_AMD64_ARG_REG1
#endif
#define MONO_ARCH_HAVE_GET_TRAMPOLINES 1

#define MONO_ARCH_AOT_SUPPORTED 1
#if !defined( HOST_WIN32 ) && !defined( __native_client__ )
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1
#endif

#if !defined(HOST_WIN32) || defined(__sun)
#define MONO_ARCH_ENABLE_MONITOR_IL_FASTPATH 1
#endif

#define MONO_ARCH_SUPPORT_TASKLETS 1

#ifndef HOST_WIN32
#define MONO_AMD64_NO_PUSHES 1
#endif

#define MONO_ARCH_GSHARED_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_SUPPORTED 1
#define MONO_ARCH_DYN_CALL_PARAM_AREA 0

#define MONO_ARCH_HAVE_LLVM_IMT_TRAMPOLINE 1
#define MONO_ARCH_LLVM_SUPPORTED 1
#define MONO_ARCH_THIS_AS_FIRST_ARG 1
#define MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD 1
#define MONO_ARCH_HAVE_CARD_TABLE_WBARRIER 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GC_MAPS_SUPPORTED 1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG 1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK 1
#define MONO_ARCH_HAVE_CREATE_LLVM_NATIVE_THUNK 1

#ifdef TARGET_OSX
#define MONO_ARCH_HAVE_TLS_GET_REG 1
#endif

gboolean
mono_amd64_tail_call_supported (MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig) MONO_INTERNAL;

#define MONO_ARCH_USE_OP_TAIL_CALL(caller_sig, callee_sig) mono_amd64_tail_call_supported (caller_sig, callee_sig)

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
mono_amd64_patch (unsigned char* code, gpointer target) MONO_INTERNAL;

void
mono_amd64_throw_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
							guint64 dummy5, guint64 dummy6,
							mgreg_t *regs, mgreg_t rip,
							MonoObject *exc, gboolean rethrow) MONO_INTERNAL;

void
mono_amd64_throw_corlib_exception (guint64 dummy1, guint64 dummy2, guint64 dummy3, guint64 dummy4,
								   guint64 dummy5, guint64 dummy6,
								   mgreg_t *regs, mgreg_t rip,
								   guint32 ex_token_index, gint64 pc_offset) MONO_INTERNAL;

guint64
mono_amd64_get_original_ip (void) MONO_INTERNAL;

guint8*
mono_amd64_emit_tls_get (guint8* code, int dreg, int tls_offset) MONO_INTERNAL;

gboolean
mono_amd64_have_tls_get (void) MONO_INTERNAL;

GSList*
mono_amd64_get_exception_trampolines (gboolean aot) MONO_INTERNAL;

typedef struct {
	guint8 *address;
	guint8 saved_byte;
} MonoBreakpointInfo;

extern MonoBreakpointInfo mono_breakpoint_info [MONO_BREAKPOINT_ARRAY_SIZE];

#ifdef HOST_WIN32

void mono_arch_unwindinfo_add_push_nonvol (gpointer* monoui, gpointer codebegin, gpointer nextip, guchar reg );
void mono_arch_unwindinfo_add_set_fpreg (gpointer* monoui, gpointer codebegin, gpointer nextip, guchar reg );
void mono_arch_unwindinfo_add_alloc_stack (gpointer* monoui, gpointer codebegin, gpointer nextip, guint size );
guint mono_arch_unwindinfo_get_size (gpointer monoui);
void mono_arch_unwindinfo_install_unwind_info (gpointer* monoui, gpointer code, guint code_size);

#define MONO_ARCH_HAVE_UNWIND_TABLE 1
#endif

#endif /* __MONO_MINI_AMD64_H__ */  

