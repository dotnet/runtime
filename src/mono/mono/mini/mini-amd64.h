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
#  define MONO_ARCH_USE_SIGACTION 1
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

/* Enables OP_LSHL, OP_LSHL_IMM, OP_LSHR, OP_LSHR_IMM, OP_LSHR_UN, OP_LSHR_UN_IMM */
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS

#define MONO_MAX_IREGS 16

#define MONO_MAX_FREGS 6

#define MONO_ARCH_FRAME_ALIGNMENT 16

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

#define MONO_ARCH_BASEREG X86_EBP
#define MONO_ARCH_RETREG1 X86_EAX
#define MONO_ARCH_RETREG2 X86_EDX

#define MONO_ARCH_ENCODE_LREG(r1,r2) (r1 | (r2<<3))

#define inst_dreg_low dreg&7 
#define inst_dreg_high dreg>>3
#define inst_sreg1_low sreg1&7 
#define inst_sreg1_high sreg1>>3
#define inst_sreg2_low sreg2&7 
#define inst_sreg2_high sreg2>>3

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	guint64     rip;
	guint64     rbx;
	guint64     ebp;
	guint64     r12;
	guint64     r13;
	guint64     r14;
	guint64     r15;
};

typedef struct MonoCompileArch {
	gint32 lmf_offset;
	gint32 localloc_offset;    
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

#if defined(__FreeBSD__) || defined(__NetBSD__) || defined(__OpenBSD__)
# define SC_EAX sc_eax
# define SC_EBX sc_ebx
# define SC_ECX sc_ecx
# define SC_EDX sc_edx
# define SC_EBP sc_ebp
# define SC_EIP sc_eip
# define SC_ESP sc_esp
# define SC_EDI sc_edi
# define SC_ESI sc_esi
#else
# define SC_EAX rax
# define SC_EBX rbx
# define SC_ECX rcx
# define SC_EDX rdx
# define SC_EBP rbp
# define SC_EIP rip
# define SC_ESP rsp
# define SC_EDI rdi
# define SC_ESI rsi

# define SC_RIP rip
# define SC_RSP rsp
# define SC_RBP rbp
# define SC_RBX rbx
# define SC_R12 r12
# define SC_R13 r13
# define SC_R14 r14
# define SC_R15 r15

#endif

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->rip = (long)(ip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->rbp = (long)(bp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->rsp = (long)(esp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->rip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->rbp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->rsp))

#define MONO_ARCH_USE_SIGACTION 1

/*
 * some icalls like mono_array_new_va needs to be called using a different 
 * calling convention.
 */
#define MONO_ARCH_VARARG_ICALLS 1

#ifndef PLATFORM_WIN32

#ifdef HAVE_WORKING_SIGALTSTACK

/*
 * FIXME: For some reason, when sigaltstack is enabled, the uc_mcontext member
 * in ucontext_t is not at the offset indicated by the definition of ucontext_t.
 */

//#define MONO_ARCH_SIGSEGV_ON_ALTSTACK

/* NetBSD doesn't define SA_STACK */
#ifndef SA_STACK
#define SA_STACK SA_ONSTACK
#endif
#endif

#endif

/* Enables OP_LSHL, OP_LSHL_IMM, OP_LSHR, OP_LSHR_IMM, OP_LSHR_UN, OP_LSHR_UN_IMM */
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS

#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1


gpointer*
mono_amd64_get_vcall_slot_addr (guint8* code, guint64 *regs);

void
mono_amd64_exceptions_init (void);

/* FIXME: */
//#define MONO_ARCH_BIGMUL_INTRINS 1

#endif /* __MONO_MINI_AMD64_H__ */  

