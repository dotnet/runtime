#ifndef __MONO_MONO_SIGCONTEXT_H__
#define __MONO_MONO_SIGCONTEXT_H__

#include <config.h>

#if defined(__i386__)

#if defined(__FreeBSD__) || defined(__APPLE__)
#include <ucontext.h>
#endif
#if defined(__APPLE__)
#include <AvailabilityMacros.h>
#endif

#if defined(__FreeBSD__)
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_eax)
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_ebx)
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_ecx)
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_edx)
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_ebp)
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_esp)
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_esi)
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_edi)
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_eip)
#elif defined(__APPLE__)
#  if MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_10_5
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__eax)
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__ebx)
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__ecx)
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__edx)
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__ebp)
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__esp)
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__esi)
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__edi)
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__eip)
#  else
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.eax)
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.ebx)
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.ecx)
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.edx)
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.ebp)
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.esp)
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.esi)
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.edi)
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->ss.eip)
#  endif
#elif defined(__NetBSD__)
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_EAX])
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_EBX])
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_ECX])
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_EDX])
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_EBP])
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_ESP])
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_ESI])
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_EDI])
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_EIP])
#elif defined(__OpenBSD__)
	#define UCONTEXT_REG_EAX(ctx) ((ctx)->sc_eax)
	#define UCONTEXT_REG_EBX(ctx) ((ctx)->sc_ebx)
	#define UCONTEXT_REG_ECX(ctx) ((ctx)->sc_ecx)
	#define UCONTEXT_REG_EDX(ctx) ((ctx)->sc_edx)
	#define UCONTEXT_REG_EBP(ctx) ((ctx)->sc_ebp)
	#define UCONTEXT_REG_ESP(ctx) ((ctx)->sc_esp)
	#define UCONTEXT_REG_ESI(ctx) ((ctx)->sc_esi)
	#define UCONTEXT_REG_EDI(ctx) ((ctx)->sc_edi)
	#define UCONTEXT_REG_EIP(ctx) ((ctx)->sc_eip)
#else
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_EAX])
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_EBX])
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_ECX])
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_EDX])
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_EBP])
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_ESP])
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_ESI])
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_EDI])
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [REG_EIP])
#endif

#elif defined(__x86_64__)

#if defined(__APPLE__)
	#define UCONTEXT_REG_RAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rax)
	#define UCONTEXT_REG_RBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rbx)
	#define UCONTEXT_REG_RCX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rcx)
	#define UCONTEXT_REG_RDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rdx)
	#define UCONTEXT_REG_RBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rbp)
	#define UCONTEXT_REG_RSP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rsp)
	#define UCONTEXT_REG_RSI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rsi)
	#define UCONTEXT_REG_RDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rdi)
	#define UCONTEXT_REG_RIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__rip)
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r12)
	#define UCONTEXT_REG_R13(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r13)
	#define UCONTEXT_REG_R14(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r14)
	#define UCONTEXT_REG_R15(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r15)
#elif defined(__FreeBSD__)
#define UCONTEXT_GREGS(ctx)	((guint64*)&(((ucontext_t*)(ctx))->uc_mcontext))
#elif defined(__OpenBSD__)
    /* OpenBSD/amd64 has no gregs array, ucontext_t == sigcontext */
	#define UCONTEXT_REG_RAX(ctx) ((ctx)->sc_rax)
	#define UCONTEXT_REG_RBX(ctx) ((ctx)->sc_rbx)
	#define UCONTEXT_REG_RCX(ctx) ((ctx)->sc_rcx)
	#define UCONTEXT_REG_RDX(ctx) ((ctx)->sc_rdx)
	#define UCONTEXT_REG_RBP(ctx) ((ctx)->sc_rbp)
	#define UCONTEXT_REG_RSP(ctx) ((ctx)->sc_rsp)
	#define UCONTEXT_REG_RSI(ctx) ((ctx)->sc_rsi)
	#define UCONTEXT_REG_RDI(ctx) ((ctx)->sc_rdi)
	#define UCONTEXT_REG_RIP(ctx) ((ctx)->sc_rip)
	#define UCONTEXT_REG_R12(ctx) ((ctx)->sc_r12)
	#define UCONTEXT_REG_R13(ctx) ((ctx)->sc_r13)
	#define UCONTEXT_REG_R14(ctx) ((ctx)->sc_r14)
	#define UCONTEXT_REG_R15(ctx) ((ctx)->sc_r15)
#else
#define UCONTEXT_GREGS(ctx)	((guint64*)&(((ucontext_t*)(ctx))->uc_mcontext.gregs))
#endif

#ifdef UCONTEXT_GREGS
#define UCONTEXT_REG_RAX(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RAX])
#define UCONTEXT_REG_RBX(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RBX])
#define UCONTEXT_REG_RCX(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RCX])
#define UCONTEXT_REG_RDX(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RDX])
#define UCONTEXT_REG_RBP(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RBP])
#define UCONTEXT_REG_RSP(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RSP])
#define UCONTEXT_REG_RSI(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RSI])
#define UCONTEXT_REG_RDI(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RDI])
#define UCONTEXT_REG_RIP(ctx) (UCONTEXT_GREGS ((ctx)) [REG_RIP])
#define UCONTEXT_REG_R12(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R12])
#define UCONTEXT_REG_R13(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R13])
#define UCONTEXT_REG_R14(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R14])
#define UCONTEXT_REG_R15(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R15])
#endif

#elif defined(__mono_ppc__)

#if HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#if defined(__linux__)
	typedef struct ucontext os_ucontext;

#ifdef __mono_ppc64__
	#define UCONTEXT_REG_Rn(ctx, n)   (((os_ucontext*)(ctx))->uc_mcontext.gp_regs [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext.fp_regs [(n)])
	#define UCONTEXT_REG_NIP(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.gp_regs [PT_NIP])
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.gp_regs [PT_LNK])
#else
	#define UCONTEXT_REG_Rn(ctx, n)   (((os_ucontext*)(ctx))->uc_mcontext.uc_regs->gregs [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext.uc_regs->fpregs.fpregs [(n)])
	#define UCONTEXT_REG_NIP(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.uc_regs->gregs [PT_NIP])
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.uc_regs->gregs [PT_LNK])
#endif
#elif defined (__APPLE__) && defined (_STRUCT_MCONTEXT)
	typedef struct __darwin_ucontext os_ucontext;

	#define UCONTEXT_REG_Rn(ctx, n)   ((&((os_ucontext*)(ctx))->uc_mcontext->__ss.__r0) [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext->__fs.__fpregs [(n)])
	#define UCONTEXT_REG_NIP(ctx)     (((os_ucontext*)(ctx))->uc_mcontext->__ss.__srr0)
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext->__ss.__lr)
#elif defined (__APPLE__) && !defined (_STRUCT_MCONTEXT)
	typedef struct ucontext os_ucontext;

	#define UCONTEXT_REG_Rn(ctx, n)   ((&((os_ucontext*)(ctx))->uc_mcontext->ss.r0) [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext->fs.fpregs [(n)])
	#define UCONTEXT_REG_NIP(ctx)     (((os_ucontext*)(ctx))->uc_mcontext->ss.srr0)
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext->ss.lr)
#elif defined(__NetBSD__)
	typedef ucontext_t os_ucontext;

	#define UCONTEXT_REG_Rn(ctx, n)   (((os_ucontext*)(ctx))->uc_mcontext.__gregs [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext.__fpregs.__fpu_regs [(n)])
	#define UCONTEXT_REG_NIP(ctx)     _UC_MACHINE_PC(ctx)
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.__gregs [_REG_LR])
#elif defined(__FreeBSD__)
	typedef ucontext_t os_ucontext;

	#define UCONTEXT_REG_Rn(ctx, n)   ((ctx)->uc_mcontext.mc_gpr [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) ((ctx)->uc_mcontext.mc_fpreg [(n)])
	#define UCONTEXT_REG_NIP(ctx)     ((ctx)->uc_mcontext.mc_srr0)
	#define UCONTEXT_REG_LNK(ctx)     ((ctx)->uc_mcontext.mc_lr)
#endif

#endif

#endif
