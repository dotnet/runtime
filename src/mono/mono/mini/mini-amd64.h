#ifndef __MONO_MINI_AMD64_H__
#define __MONO_MINI_AMD64_H__

#include <mono/arch/amd64/amd64-codegen.h>
#include <glib.h>

#ifdef PLATFORM_WIN32
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

typedef void (* MonoW32ExceptionHandler) (int);
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

#endif /* PLATFORM_WIN32 */

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

#define MONO_ARCH_SIGNAL_STACK_SIZE (16 * 1024)

#define MONO_ARCH_CPU_SPEC amd64_desc

#define MONO_MAX_IREGS 16

#define MONO_MAX_FREGS AMD64_XMM_NREG

/* xmm15 is reserved for use by some opcodes */
#define MONO_ARCH_CALLEE_FREGS 0xef
#define MONO_ARCH_CALLEE_SAVED_FREGS 0

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

#define MONO_ARCH_BASEREG X86_EBP
#define MONO_ARCH_RETREG1 X86_EAX
#define MONO_ARCH_RETREG2 X86_EDX

#define MONO_ARCH_AOT_PLT_OFFSET_REG AMD64_RAX

#define MONO_ARCH_ENCODE_LREG(r1,r2) (r1 | (r2<<3))

#define inst_dreg_low dreg&7 
#define inst_dreg_high dreg>>3
#define inst_sreg1_low sreg1&7 
#define inst_sreg1_high sreg1>>3
#define inst_sreg2_low sreg2&7 
#define inst_sreg2_high sreg2>>3

struct MonoLMF {
	/* 
	 * If the lowest bit is set to 1, then this LMF has the rip field set. Otherwise,
	 * the rip field is not set, and the rsp field points to the stack location where
	 * the caller ip is saved.
	 */
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	/* This is only set in trampoline LMF frames */
	MonoMethod *method;
	guint64     rip;
	guint64     rbx;
	guint64     rbp;
	guint64     rsp;
	guint64     r12;
	guint64     r13;
	guint64     r14;
	guint64     r15;
#ifdef PLATFORM_WIN32
	guint64     rdi;
	guint64     rsi;
#endif
};

typedef struct MonoCompileArch {
	gint32 lmf_offset;
	gint32 localloc_offset;
	gint32 reg_save_area_offset;
	gint32 stack_alloc_size;
	gboolean omit_fp, omit_fp_computed;
	gpointer cinfo;
	gint32 async_point_count;
} MonoCompileArch;

typedef struct {
	guint64 rax;
	guint64 rbx;
	guint64 rcx;
	guint64 rdx;
	guint64 rbp;
	guint64 rsp;
    guint64 rsi;
	guint64 rdi;
	guint64 rip;
	guint64 r12;
	guint64 r13;
	guint64 r14;
	guint64 r15;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->rip = (guint64)(ip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->rbp = (guint64)(bp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->rsp = (guint64)(esp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->rip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->rbp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->rsp))

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

#ifndef PLATFORM_WIN32

#define MONO_ARCH_USE_SIGACTION 1

#ifdef HAVE_WORKING_SIGALTSTACK

#define MONO_ARCH_SIGSEGV_ON_ALTSTACK

#endif

#endif /* PLATFORM_WIN32 */

#ifdef __FreeBSD__

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

#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS

#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1

#define MONO_ARCH_ENABLE_EMIT_STATE_OPT 1
#define MONO_ARCH_ENABLE_REGALLOC_IN_EH_BLOCKS 1
#define MONO_ARCH_ENABLE_MONO_LMF_VAR 1
#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_THROW_CORLIB_EXCEPTION 1
#define MONO_ARCH_HAVE_CREATE_TRAMPOLINE_FROM_TOKEN 1
#define MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE 1
#define MONO_ARCH_HAVE_ATOMIC_ADD 1
#define MONO_ARCH_HAVE_ATOMIC_EXCHANGE 1
#define MONO_ARCH_HAVE_ATOMIC_CAS_IMM 1
#define MONO_ARCH_HAVE_IMT 1
#define MONO_ARCH_HAVE_TLS_GET 1
#define MONO_ARCH_IMT_REG AMD64_R11
#define MONO_ARCH_VTABLE_REG AMD64_R11
/*
 * We use r10 for the rgctx register rather than r11 because r11 is
 * used by the trampoline as a scratch register and hence might be
 * clobbered across method call boundaries.
 */
#define MONO_ARCH_RGCTX_REG AMD64_R10
#define MONO_ARCH_COMMON_VTABLE_TRAMPOLINE 1
#define MONO_ARCH_HAVE_NOTIFY_PENDING_EXC 1
#define MONO_ARCH_ENABLE_NORMALIZE_OPCODES 1

#define MONO_ARCH_AOT_SUPPORTED 1

void 
mono_amd64_patch (unsigned char* code, gpointer target) MONO_INTERNAL;

typedef struct {
	guint8 *address;
	guint8 saved_byte;
} MonoBreakpointInfo;

extern MonoBreakpointInfo mono_breakpoint_info [MONO_BREAKPOINT_ARRAY_SIZE];

#endif /* __MONO_MINI_AMD64_H__ */  

