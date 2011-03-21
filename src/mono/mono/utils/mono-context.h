/*
 * mono-context.h: plat independent machine state definitions
 *
 *
 * Copyright (c) 2011 Novell, Inc (http://www.novell.com)
 */


#ifndef __MONO_MONO_CONTEXT_H__
#define __MONO_MONO_CONTEXT_H__

#include "mono-compiler.h"
#include "mono-sigcontext.h"
#include "mono-machine.h"

#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

/*
 * General notes about mono-context.
 * Each arch defines a MonoContext struct with all GPR regs + IP/PC.
 * IP/PC should be the last element of the struct (this is a mild sgen constraint we could drop if needed)
 * Macros to get/set BP, SP and IP are defined too.
 * MONO_CONTEXT_GET_CURRENT captures the current context as close as possible. One reg might be clobbered
 *  to hold the address of the target MonoContext. It will be a caller save one, so should not be a problem.
 */
#if defined(__i386__)

/*HACK, move this to an eventual mono-signal.c*/
#if defined( __linux__) || defined(__sun) || defined(__APPLE__) || defined(__NetBSD__) || \
       defined(__FreeBSD__) || defined(__OpenBSD__)
#define MONO_SIGNAL_USE_SIGACTION
#endif

#if defined(__native_client__)
#undef MONO_SIGNAL_USE_SIGACTION
#endif

#ifdef HOST_WIN32
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
#endif

#if defined(__FreeBSD__) || defined(__NetBSD__) || defined(__OpenBSD__) || defined(__APPLE__)
# define SC_EAX sc_eax
# define SC_EBX sc_ebx
# define SC_ECX sc_ecx
# define SC_EDX sc_edx
# define SC_EBP sc_ebp
# define SC_EIP sc_eip
# define SC_ESP sc_esp
# define SC_EDI sc_edi
# define SC_ESI sc_esi
#elif defined(__HAIKU__)
# define SC_EAX regs.eax
# define SC_EBX regs._reserved_2[2]
# define SC_ECX regs.ecx
# define SC_EDX regs.edx
# define SC_EBP regs.ebp
# define SC_EIP regs.eip
# define SC_ESP regs.esp
# define SC_EDI regs._reserved_2[0]
# define SC_ESI regs._reserved_2[1]
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

typedef struct {
	mgreg_t eax;
	mgreg_t ebx;
	mgreg_t ecx;
	mgreg_t edx;
	mgreg_t ebp;
	mgreg_t esp;
	mgreg_t esi;
	mgreg_t edi;
	mgreg_t eip;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->eip = (mgreg_t)(ip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->ebp = (mgreg_t)(bp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->esp = (mgreg_t)(sp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->eip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->ebp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->esp))

/*We set EAX to zero since we are clobering it anyway*/
#define MONO_CONTEXT_GET_CURRENT(ctx) \
	__asm__ __volatile__(   \
	"movl $0x0, 0x00(%0)\n" \
	"mov %%ebx, 0x04(%0)\n" \
	"mov %%ecx, 0x08(%0)\n" \
	"mov %%edx, 0x0c(%0)\n" \
	"mov %%ebp, 0x10(%0)\n" \
	"mov %%esp, 0x14(%0)\n" \
	"mov %%esi, 0x18(%0)\n" \
	"mov %%edi, 0x1c(%0)\n" \
	"call 1f\n"     \
	"1: pop 0x20(%0)\n"     \
	:	\
	: "a" (&(ctx))  \
	: "memory")


#elif defined(__x86_64__) /* defined(__i386__) */


#if !defined( HOST_WIN32 ) && !defined(__native_client__) && !defined(__native_client_codegen__)

#define MONO_SIGNAL_USE_SIGACTION 1

#endif

typedef struct {
	mgreg_t rax;
	mgreg_t rbx;
	mgreg_t rcx;
	mgreg_t rdx;
	mgreg_t rbp;
	mgreg_t rsp;
    mgreg_t rsi;
	mgreg_t rdi;
	mgreg_t r8;
	mgreg_t r9;
	mgreg_t r10;
	mgreg_t r11;
	mgreg_t r12;
	mgreg_t r13;
	mgreg_t r14;
	mgreg_t r15;
	mgreg_t rip;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->rip = (mgreg_t)(ip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->rbp = (mgreg_t)(bp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->rsp = (mgreg_t)(esp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->rip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->rbp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->rsp))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"movq $0x0,  0x00(%0)\n"	\
		"movq %%rbx, 0x08(%0)\n"	\
		"movq %%rcx, 0x10(%0)\n"	\
		"movq %%rdx, 0x18(%0)\n"	\
		"movq %%rbp, 0x20(%0)\n"	\
		"movq %%rsp, 0x28(%0)\n"	\
		"movq %%rsi, 0x30(%0)\n"	\
		"movq %%rdi, 0x38(%0)\n"	\
		"movq %%r8,  0x40(%0)\n"	\
		"movq %%r9,  0x48(%0)\n"	\
		"movq %%r10, 0x50(%0)\n"	\
		"movq %%r11, 0x58(%0)\n"	\
		"movq %%r12, 0x60(%0)\n"	\
		"movq %%r13, 0x68(%0)\n"	\
		"movq %%r14, 0x70(%0)\n"	\
		"movq %%r15, 0x78(%0)\n"	\
		"leaq (%%rip), %%rdx\n"	\
		"movq %%rdx, 0x80(%0)\n"	\
		: 	\
		: "a" (&(ctx))	\
		: "rdx", "memory")

#elif defined(__arm__) /* defined(__x86_64__) */

typedef struct {
	gulong eip;          // pc 
	gulong esp;          // sp
	gulong regs [16];
	double fregs [8];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->eip = (int)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->regs [ARMREG_FP] = (int)bp; } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,bp) do { (ctx)->esp = (int)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->eip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs [ARMREG_FP]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->esp))

#elif defined(__mono_ppc__) /* defined(__arm__) */

/* we define our own structure and we'll copy the data
 * from sigcontext/ucontext/mach when we need it.
 * This also makes us save stack space and time when copying
 * We might also want to add an additional field to propagate
 * the original context from the signal handler.
 */
typedef struct {
	gulong sc_ir;          // pc 
	gulong sc_sp;          // r1
	mgreg_t regs [19]; /*FIXME, this must be changed to 32 for sgen*/
	double fregs [18];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (gulong)ip; } while (0);
/* FIXME: should be called SET_SP */
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (gulong)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_sp = (gulong)sp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs [ppc_r31-13]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_sp))

#elif defined(__sparc__) || defined(sparc) /* defined(__mono_ppc__) */

typedef struct MonoContext {
	guint8 *ip;
	gpointer *sp;
	gpointer *fp;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,eip) do { (ctx)->ip = (gpointer)(eip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,ebp) do { (ctx)->fp = (gpointer*)(ebp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->sp = (gpointer*)(esp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->ip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->fp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sp))

#elif defined(__ia64__) /*defined(__sparc__) || defined(sparc) */

#ifndef UNW_LOCAL_ONLY

#define UNW_LOCAL_ONLY
#include <libunwind.h>

#endif

typedef struct MonoContext {
	unw_cursor_t cursor;
	/* Whenever the ip in 'cursor' points to the ip where the exception happened */
	/* This is true for the initial context for exceptions thrown from signal handlers */
	gboolean precise_ip;
} MonoContext;

/*XXX SET_BP is missing*/
#define MONO_CONTEXT_SET_IP(ctx,eip) do { int err = unw_set_reg (&(ctx)->cursor, UNW_IA64_IP, (unw_word_t)(eip)); g_assert (err == 0); } while (0)
#define MONO_CONTEXT_SET_SP(ctx,esp) do { int err = unw_set_reg (&(ctx)->cursor, UNW_IA64_SP, (unw_word_t)(esp)); g_assert (err == 0); } while (0)

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(mono_ia64_context_get_ip ((ctx))))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(mono_ia64_context_get_fp ((ctx))))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(mono_ia64_context_get_sp ((ctx))))

#elif defined(__mips__) && SIZEOF_REGISTER == 4 /* defined(__ia64__) */

/* we define our own structure and we'll copy the data
 * from sigcontext/ucontext/mach when we need it.
 * This also makes us save stack space and time when copying
 * We might also want to add an additional field to propagate
 * the original context from the signal handler.
 */
typedef struct {
	gpointer	sc_pc;
	guint32		sc_regs [32];
	gfloat		sc_fpregs [32];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_pc = (int)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_regs[mips_fp] = (int)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_regs[mips_sp] = (int)(sp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_pc))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sc_regs[mips_fp]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_regs[mips_sp]))

#elif defined(__s390x__)

typedef struct ucontext MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) 					\
	do {								\
		(ctx)->uc_mcontext.gregs[14] = (unsigned long)ip;	\
		(ctx)->uc_mcontext.psw.addr = (unsigned long)ip;	\
	} while (0); 

#define MONO_CONTEXT_SET_SP(ctx,bp) MONO_CONTEXT_SET_BP((ctx),(bp))
#define MONO_CONTEXT_SET_BP(ctx,bp) 					\
	do {		 						\
		(ctx)->uc_mcontext.gregs[15] = (unsigned long)bp;	\
		(ctx)->uc_stack.ss_sp	     = (void*)bp;		\
	} while (0) 

#define MONO_CONTEXT_GET_IP(ctx) (gpointer) (ctx)->uc_mcontext.psw.addr
#define MONO_CONTEXT_GET_BP(ctx) MONO_CONTEXT_GET_SP((ctx))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->uc_mcontext.gregs[15]))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"stg	%%r0,0x00(%0)\n\t"	\
		"stg	%%r1,0x08(%0)\n\t"	\
		"stg	%%r2,0x10(%0)\n\t"	\
		"stg	%%r3,0x18(%0)\n\t"	\
		"stg	%%r4,0x20(%0)\n\t"	\
		"stg	%%r5,0x28(%0)\n\t"	\
		"stg	%%r6,0x30(%0)\n\t"	\
		"stg	%%r7,0x38(%0)\n\t"	\
		"stg	%%r8,0x40(%0)\n\t"	\
		"stg	%%r9,0x48(%0)\n\t"	\
		"stg	%%r10,0x50(%0)\n\t"	\
		"stg	%%r11,0x58(%0)\n\t"	\
		"stg	%%r12,0x60(%0)\n\t"	\
		"stg	%%r13,0x68(%0)\n\t"	\
		"stg	%%r14,0x70(%0)\n\t"	\
		"stg	%%r15,0x78(%0)\n"	\
		: "=&a" (ptr)			\
		: "0" (cur_thread_regs)		\
		: "memory"			\
	)

#else

#error "Implement mono-context for the current arch"

#endif

void mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx) MONO_INTERNAL;
void mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx) MONO_INTERNAL;

#endif /* __MONO_MONO_CONTEXT_H__ */
