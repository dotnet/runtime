#ifndef __MONO_MINI_X86_H__
#define __MONO_MINI_X86_H__

#include <mono/arch/x86/x86-codegen.h>
#ifdef PLATFORM_WIN32
#include <windows.h>
/* use SIG* defines if possible */
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

/* sigcontext surrogate */
struct sigcontext {
	unsigned int eax;
	unsigned int ebx;
	unsigned int ecx;
	unsigned int edx;
	unsigned int ebp;
	unsigned int esp;
	unsigned int esi;
	unsigned int edi;
	unsigned int eip;
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

#define MONO_MAX_IREGS 8
#define MONO_MAX_FREGS 6

#define MONO_ARCH_FRAME_ALIGNMENT 4

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
	guint32     ebx;
	guint32     edi;
	guint32     esi;
	guint32     ebp;
	guint32     eip;
};

typedef struct MonoCompileArch {
} MonoCompileArch;

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
# define SC_EAX eax
# define SC_EBX ebx
# define SC_ECX ecx
# define SC_EDX edx
# define SC_EBP ebp
# define SC_EIP eip
# define SC_ESP esp
# define SC_EDI edi
# define SC_ESI esi
#endif

typedef struct sigcontext MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->SC_EIP = (long)(ip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->SC_EBP = (long)(bp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->SC_ESP = (long)(esp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->SC_EIP))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->SC_EBP))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->SC_ESP))

#ifndef PLATFORM_WIN32
#ifdef HAVE_WORKING_SIGALTSTACK
#define MONO_ARCH_SIGSEGV_ON_ALTSTACK
/* NetBSD doesn't define SA_STACK */
#ifndef SA_STACK
#define SA_STACK SA_ONSTACK
#endif
#endif

#endif

#define MONO_ARCH_BIGMUL_INTRINS 1
#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1
#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_PIC_AOT 1
#define MONO_ARCH_NEED_GOT_VAR 1
#define MONO_ARCH_HAVE_THROW_CORLIB_EXCEPTION 1

#endif /* __MONO_MINI_X86_H__ */  

