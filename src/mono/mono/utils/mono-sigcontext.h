/**
 * \file
 */

#ifndef __MONO_MONO_SIGCONTEXT_H__
#define __MONO_MONO_SIGCONTEXT_H__

#include <config.h>
#if defined(HOST_ANDROID)
#include <asm/sigcontext.h>
#endif

#ifdef HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <signal.h>

#if defined(TARGET_X86)

#if defined(__APPLE__)
#include <AvailabilityMacros.h>
#endif

#if defined(__FreeBSD__) || defined(__FreeBSD_kernel__) || defined(__DragonFly__)
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
#  if defined (TARGET_IOS) || (MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_10_5)
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__eax)
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__ebx)
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__ecx)
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__edx)
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__ebp)
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__esp)
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__esi)
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__edi)
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__eip)
	#define UCONTEXT_HAS_XMM(_ctx) (TRUE)
	#define UCONTEXT_REG_XMM0(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm0)
	#define UCONTEXT_REG_XMM1(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm1)
	#define UCONTEXT_REG_XMM2(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm2)
	#define UCONTEXT_REG_XMM3(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm3)
	#define UCONTEXT_REG_XMM4(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm4)
	#define UCONTEXT_REG_XMM5(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm5)
	#define UCONTEXT_REG_XMM6(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm6)
	#define UCONTEXT_REG_XMM7(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm7)
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
    #define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->sc_eax)
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->sc_ebx)
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->sc_ecx)
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->sc_edx)
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->sc_ebp)
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->sc_esp)
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->sc_esi)
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->sc_edi)
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->sc_eip)
#elif defined(HOST_SOLARIS)
	#define UCONTEXT_REG_EAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [EAX])
	#define UCONTEXT_REG_EBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [EBX])
	#define UCONTEXT_REG_ECX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [ECX])
	#define UCONTEXT_REG_EDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [EDX])
	#define UCONTEXT_REG_EBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [EBP])
	#define UCONTEXT_REG_ESP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [ESP])
	#define UCONTEXT_REG_ESI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [ESI])
	#define UCONTEXT_REG_EDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [EDI])
	#define UCONTEXT_REG_EIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.gregs [EIP])
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

#elif defined(TARGET_AMD64)

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
	#define UCONTEXT_REG_R8(ctx)  (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r8)
	#define UCONTEXT_REG_R9(ctx)  (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r9)
	#define UCONTEXT_REG_R10(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r10)
	#define UCONTEXT_REG_R11(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r11)
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r12)
	#define UCONTEXT_REG_R13(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r13)
	#define UCONTEXT_REG_R14(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r14)
	#define UCONTEXT_REG_R15(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r15)
	#define UCONTEXT_HAS_XMM(_ctx) (TRUE)
	#define UCONTEXT_REG_XMM0(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm0)
	#define UCONTEXT_REG_XMM1(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm1)
	#define UCONTEXT_REG_XMM2(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm2)
	#define UCONTEXT_REG_XMM3(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm3)
	#define UCONTEXT_REG_XMM4(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm4)
	#define UCONTEXT_REG_XMM5(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm5)
	#define UCONTEXT_REG_XMM6(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm6)
	#define UCONTEXT_REG_XMM7(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm7)
	#define UCONTEXT_REG_XMM8(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm8)
	#define UCONTEXT_REG_XMM9(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm9)
	#define UCONTEXT_REG_XMM10(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm10)
	#define UCONTEXT_REG_XMM11(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm11)
	#define UCONTEXT_REG_XMM12(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm12)
	#define UCONTEXT_REG_XMM13(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm13)
	#define UCONTEXT_REG_XMM14(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm14)
	#define UCONTEXT_REG_XMM15(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__fs.__fpu_xmm15)
#elif defined(__FreeBSD__) || defined(__FreeBSD_kernel__)
	#define UCONTEXT_REG_RAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rax)
	#define UCONTEXT_REG_RBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rbx)
	#define UCONTEXT_REG_RCX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rcx)
	#define UCONTEXT_REG_RDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rdx)
	#define UCONTEXT_REG_RBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rbp)
	#define UCONTEXT_REG_RSP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rsp)
	#define UCONTEXT_REG_RSI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rsi)
	#define UCONTEXT_REG_RDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rdi)
	#define UCONTEXT_REG_RIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_rip)
	#define UCONTEXT_REG_R8(ctx)  (((ucontext_t*)(ctx))->uc_mcontext.mc_r8)
	#define UCONTEXT_REG_R9(ctx)  (((ucontext_t*)(ctx))->uc_mcontext.mc_r9)
	#define UCONTEXT_REG_R10(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_r10)
	#define UCONTEXT_REG_R11(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_r11)
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_r12)
	#define UCONTEXT_REG_R13(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_r13)
	#define UCONTEXT_REG_R14(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_r14)
	#define UCONTEXT_REG_R15(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_r15)
#elif defined(__NetBSD__)
	#define UCONTEXT_REG_RAX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RAX])
	#define UCONTEXT_REG_RBX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RBX])
	#define UCONTEXT_REG_RCX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RCX])
	#define UCONTEXT_REG_RDX(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RDX])
	#define UCONTEXT_REG_RBP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RBP])
	#define UCONTEXT_REG_RSP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RSP])
	#define UCONTEXT_REG_RSI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RSI])
	#define UCONTEXT_REG_RDI(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RDI])
	#define UCONTEXT_REG_RIP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_RIP])
	#define UCONTEXT_REG_R8(ctx)  (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R8])
	#define UCONTEXT_REG_R9(ctx)  (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R9])
	#define UCONTEXT_REG_R10(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R10])
	#define UCONTEXT_REG_R11(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R11])
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R12])
	#define UCONTEXT_REG_R13(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R13])
	#define UCONTEXT_REG_R14(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R14])
	#define UCONTEXT_REG_R15(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs [_REG_R15])
#elif defined(__OpenBSD__)
    /* OpenBSD/amd64 has no gregs array, ucontext_t == sigcontext */
	#define UCONTEXT_REG_RAX(ctx) (((ucontext_t*)(ctx))->sc_rax)
	#define UCONTEXT_REG_RBX(ctx) (((ucontext_t*)(ctx))->sc_rbx)
	#define UCONTEXT_REG_RCX(ctx) (((ucontext_t*)(ctx))->sc_rcx)
	#define UCONTEXT_REG_RDX(ctx) (((ucontext_t*)(ctx))->sc_rdx)
	#define UCONTEXT_REG_RBP(ctx) (((ucontext_t*)(ctx))->sc_rbp)
	#define UCONTEXT_REG_RSP(ctx) (((ucontext_t*)(ctx))->sc_rsp)
	#define UCONTEXT_REG_RSI(ctx) (((ucontext_t*)(ctx))->sc_rsi)
	#define UCONTEXT_REG_RDI(ctx) (((ucontext_t*)(ctx))->sc_rdi)
	#define UCONTEXT_REG_RIP(ctx) (((ucontext_t*)(ctx))->sc_rip)
	#define UCONTEXT_REG_R8(ctx) (((ucontext_t*)(ctx))->sc_r8)
	#define UCONTEXT_REG_R9(ctx) (((ucontext_t*)(ctx))->sc_r9)
	#define UCONTEXT_REG_R10(ctx) (((ucontext_t*)(ctx))->sc_r10)
	#define UCONTEXT_REG_R11(ctx) (((ucontext_t*)(ctx))->sc_r11)
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->sc_r12)
	#define UCONTEXT_REG_R13(ctx) (((ucontext_t*)(ctx))->sc_r13)
	#define UCONTEXT_REG_R14(ctx) (((ucontext_t*)(ctx))->sc_r14)
	#define UCONTEXT_REG_R15(ctx) (((ucontext_t*)(ctx))->sc_r15)
#elif !defined(HOST_WIN32)
	#define UCONTEXT_GREGS(ctx)	((guint64*)&(((ucontext_t*)(ctx))->uc_mcontext.gregs))
#if defined(__GLIBC__)
	/*
	 * Ordinarily, ctx->uc_mcontext.fpregs is a pointer to somewhere in
	 * ctx->__fpregs_mem and is the preferred way to access the fpstate.
	 * However, some versions of Windows Subsystem for Linux have a bug where
	 * the fpregs field is a NULL pointer instead. Since accessing __fpregs_mem
	 * directly is quite complicated because its exact layout depends on CPU
	 * features and/or kernel configuration, we sinply won't make the fpstate
	 * available if the fpregs pointer is NULL.
	 *
	 * This is of course not correct (as we won't scan XMM registers on those
	 * broken WSL versions), but it'll at least prevent a crash when accessing
	 * the fpregs pointer.
	 */
	#define UCONTEXT_HAS_FREGS(ctx) (!!((ucontext_t *) (ctx))->uc_mcontext.fpregs)
	#define UCONTEXT_FREGS(ctx)	(((ucontext_t *) (ctx))->uc_mcontext.fpregs->_xmm)
#endif
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
#define UCONTEXT_REG_R8(ctx)  (UCONTEXT_GREGS ((ctx)) [REG_R8])
#define UCONTEXT_REG_R9(ctx)  (UCONTEXT_GREGS ((ctx)) [REG_R9])
#define UCONTEXT_REG_R10(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R10])
#define UCONTEXT_REG_R11(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R11])
#define UCONTEXT_REG_R12(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R12])
#define UCONTEXT_REG_R13(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R13])
#define UCONTEXT_REG_R14(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R14])
#define UCONTEXT_REG_R15(ctx) (UCONTEXT_GREGS ((ctx)) [REG_R15])
#endif

#ifdef UCONTEXT_FREGS
#define UCONTEXT_HAS_XMM(ctx)   (UCONTEXT_HAS_FREGS (ctx))
#define UCONTEXT_REG_XMM0(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM0])
#define UCONTEXT_REG_XMM1(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM1])
#define UCONTEXT_REG_XMM2(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM2])
#define UCONTEXT_REG_XMM3(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM3])
#define UCONTEXT_REG_XMM4(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM4])
#define UCONTEXT_REG_XMM5(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM5])
#define UCONTEXT_REG_XMM6(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM6])
#define UCONTEXT_REG_XMM7(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM7])
#define UCONTEXT_REG_XMM8(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM8])
#define UCONTEXT_REG_XMM9(ctx)  (UCONTEXT_FREGS ((ctx)) [AMD64_XMM9])
#define UCONTEXT_REG_XMM10(ctx) (UCONTEXT_FREGS ((ctx)) [AMD64_XMM10])
#define UCONTEXT_REG_XMM11(ctx) (UCONTEXT_FREGS ((ctx)) [AMD64_XMM11])
#define UCONTEXT_REG_XMM12(ctx) (UCONTEXT_FREGS ((ctx)) [AMD64_XMM12])
#define UCONTEXT_REG_XMM13(ctx) (UCONTEXT_FREGS ((ctx)) [AMD64_XMM13])
#define UCONTEXT_REG_XMM14(ctx) (UCONTEXT_FREGS ((ctx)) [AMD64_XMM14])
#define UCONTEXT_REG_XMM15(ctx) (UCONTEXT_FREGS ((ctx)) [AMD64_XMM15])
#endif

#elif defined(__mono_ppc__)

#if HAVE_UCONTEXT_H
#include <ucontext.h>
#endif

#if defined(__linux__)

/* don't rely on glibc to include this for us, musl won't */
#include <asm/ptrace.h>
	typedef ucontext_t os_ucontext;

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

	#define UCONTEXT_REG_Rn(ctx, n)   (((os_ucontext*)(ctx))->uc_mcontext.mc_gpr [(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext.mc_fpreg [(n)])
	#define UCONTEXT_REG_NIP(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.mc_srr0)
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.mc_lr)
#elif defined(_AIX)
	typedef ucontext_t os_ucontext;

	#define UCONTEXT_REG_Rn(ctx, n)   (((os_ucontext*)(ctx))->uc_mcontext.jmp_context.gpr[(n)])
	#define UCONTEXT_REG_FPRn(ctx, n) (((os_ucontext*)(ctx))->uc_mcontext.jmp_context.fpr[(n)])
	#define UCONTEXT_REG_NIP(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.jmp_context.iar)
	#define UCONTEXT_REG_LNK(ctx)     (((os_ucontext*)(ctx))->uc_mcontext.jmp_context.lr)
#endif

#elif defined(TARGET_ARM)
#if defined(__APPLE__)
	typedef ucontext_t arm_ucontext;

	#define UCONTEXT_REG_PC(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__pc)
	#define UCONTEXT_REG_SP(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__sp)
	#define UCONTEXT_REG_LR(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__lr)
	#define UCONTEXT_REG_R0(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[0])
	#define UCONTEXT_REG_R1(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[1])
	#define UCONTEXT_REG_R2(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[2])
	#define UCONTEXT_REG_R3(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[3])
	#define UCONTEXT_REG_R4(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[4])
	#define UCONTEXT_REG_R5(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[5])
	#define UCONTEXT_REG_R6(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[6])
	#define UCONTEXT_REG_R7(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[7])
	#define UCONTEXT_REG_R8(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[8])
	#define UCONTEXT_REG_R9(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[9])
	#define UCONTEXT_REG_R10(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[10])
	#define UCONTEXT_REG_R11(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[11])
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__r[12])
	#define UCONTEXT_REG_CPSR(ctx) (((ucontext_t*)(ctx))->uc_mcontext->__ss.__cpsr)
	#define UCONTEXT_REG_VFPREGS(ctx) (double*)(((ucontext_t*)(ctx))->uc_mcontext->__fs.__r)
#elif defined(__linux__)
	typedef struct arm_ucontext {
		unsigned long       uc_flags;
		struct arm_ucontext *uc_link;
		struct {
			void *p;
			int flags;
			size_t size;
		} sstack_data;
		struct sigcontext sig_ctx;
		/* some 2.6.x kernel has fp data here after a few other fields
		* we don't use them for now...
		*/
	} arm_ucontext;
	#define UCONTEXT_REG_PC(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_pc)
	#define UCONTEXT_REG_SP(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_sp)
	#define UCONTEXT_REG_LR(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_lr)
	#define UCONTEXT_REG_R0(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r0)
	#define UCONTEXT_REG_R1(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r1)
	#define UCONTEXT_REG_R2(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r2)
	#define UCONTEXT_REG_R3(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r3)
	#define UCONTEXT_REG_R4(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r4)
	#define UCONTEXT_REG_R5(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r5)
	#define UCONTEXT_REG_R6(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r6)
	#define UCONTEXT_REG_R7(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r7)
	#define UCONTEXT_REG_R8(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r8)
	#define UCONTEXT_REG_R9(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r9)
	#define UCONTEXT_REG_R10(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_r10)
	#define UCONTEXT_REG_R11(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_fp)
	#define UCONTEXT_REG_R12(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_ip)
	#define UCONTEXT_REG_CPSR(ctx) (((arm_ucontext*)(ctx))->sig_ctx.arm_cpsr)
#elif defined(__NetBSD__)
	typedef ucontext_t arm_ucontext;

	#define UCONTEXT_REG_PC(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_PC])
	#define UCONTEXT_REG_SP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_SP])
	#define UCONTEXT_REG_LR(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_LR])
	#define UCONTEXT_REG_R0(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R0])
	#define UCONTEXT_REG_R1(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R1])
	#define UCONTEXT_REG_R2(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R2])
	#define UCONTEXT_REG_R3(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R3])
	#define UCONTEXT_REG_R4(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R4])
	#define UCONTEXT_REG_R5(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R5])
	#define UCONTEXT_REG_R6(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R6])
	#define UCONTEXT_REG_R7(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R7])
	#define UCONTEXT_REG_R8(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R8])
	#define UCONTEXT_REG_R9(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R9])
	#define UCONTEXT_REG_R10(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R10])
	#define UCONTEXT_REG_R11(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R11])
	#define UCONTEXT_REG_R12(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_R12])
	#define UCONTEXT_REG_CPSR(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_CPSR])
	#define UCONTEXT_REG_VFPREGS(ctx) (((ucontext_t*)(ctx))->uc_mcontext.__gregs[_REG_VFPREGS])
#endif

#elif defined(TARGET_ARM64)

#if defined(MONO_CROSS_COMPILE)
	#define UCONTEXT_REG_PC(ctx) NULL
	#define UCONTEXT_REG_SP(ctx) NULL
	#define UCONTEXT_REG_R0(ctx) NULL
	#define UCONTEXT_GREGS(ctx) NULL
#elif defined(__APPLE__)
#include <machine/_mcontext.h>
#include <sys/_types/_ucontext64.h>

	/* mach/arm/_structs.h */
#if __has_feature(ptrauth_calls)
	#define UCONTEXT_REG_PC(ctx) (host_mgreg_t)__darwin_arm_thread_state64_get_pc_fptr (((ucontext64_t*)(ctx))->uc_mcontext64->__ss)
	#define UCONTEXT_REG_SP(ctx) __darwin_arm_thread_state64_get_sp (((ucontext64_t*)(ctx))->uc_mcontext64->__ss)
	#define UCONTEXT_REG_LR(ctx) __darwin_arm_thread_state64_get_lr (((ucontext64_t*)(ctx))->uc_mcontext64->__ss)
	#define UCONTEXT_REG_SET_PC(ctx,val) __darwin_arm_thread_state64_set_pc_fptr (((ucontext64_t*)(ctx))->uc_mcontext64->__ss, (val))
	#define UCONTEXT_REG_SET_SP(ctx, val) __darwin_arm_thread_state64_set_sp (((ucontext64_t*)(ctx))->uc_mcontext64->__ss, (val))
#else
	#define UCONTEXT_REG_PC(ctx) (((ucontext64_t*)(ctx))->uc_mcontext64->__ss.__pc)
	#define UCONTEXT_REG_SP(ctx) (((ucontext64_t*)(ctx))->uc_mcontext64->__ss.__sp)
	#define UCONTEXT_REG_SET_PC(ctx,val) do { \
		UCONTEXT_REG_PC (ctx) = (__uint64_t)(val); \
	} while (0)
#endif

	#define UCONTEXT_REG_R0(ctx) (((ucontext64_t*)(ctx))->uc_mcontext64->__ss.__x [ARMREG_R0])
	#define UCONTEXT_GREGS(ctx) (&(((ucontext64_t*)(ctx))->uc_mcontext64->__ss.__x))
#elif defined(__FreeBSD__)
#include <ucontext.h>
	/* https://lists.freebsd.org/pipermail/freebsd-arm/2017-February/015611.html */
	#define UCONTEXT_REG_PC(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_gpregs.gp_elr)
	#define UCONTEXT_REG_SP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_gpregs.gp_sp)
	#define UCONTEXT_REG_R0(ctx) (((ucontext_t*)(ctx))->uc_mcontext.mc_gpregs.gp_x [ARMREG_R0])
	#define UCONTEXT_GREGS(ctx) (&(((ucontext_t*)(ctx))->uc_mcontext.mc_gpregs.gp_x))
#elif defined(__OpenBSD__)
	/* ucontext_t == sigcontext */
	#define UCONTEXT_REG_PC(ctx) (((ucontext_t*)(ctx))->sc_elr)
	#define UCONTEXT_REG_SP(ctx) (((ucontext_t*)(ctx))->sc_sp)
	#define UCONTEXT_REG_R0(ctx) (((ucontext_t*)(ctx))->sc_x [ARMREG_R0])
	#define UCONTEXT_GREGS(ctx) (&(((ucontext_t*)(ctx))->sc_x))
#elif !defined(HOST_WIN32)
#include <ucontext.h>
	#define UCONTEXT_REG_PC(ctx) (((ucontext_t*)(ctx))->uc_mcontext.pc)
	#define UCONTEXT_REG_SP(ctx) (((ucontext_t*)(ctx))->uc_mcontext.sp)
	#define UCONTEXT_REG_R0(ctx) (((ucontext_t*)(ctx))->uc_mcontext.regs [ARMREG_R0])
	#define UCONTEXT_GREGS(ctx) (&(((ucontext_t*)(ctx))->uc_mcontext.regs))
#endif

#ifndef UCONTEXT_REG_SET_PC
#define UCONTEXT_REG_SET_PC(ctx, val) do { \
	UCONTEXT_REG_PC (ctx) = (guint64)(val); \
	 } while (0)
#endif
#ifndef UCONTEXT_REG_SET_SP
#define UCONTEXT_REG_SET_SP(ctx, val) do { \
	UCONTEXT_REG_SP (ctx) = (val); \
	 } while (0)
#endif

#elif defined(__s390x__)

# if HAVE_UCONTEXT_H
#  include <ucontext.h>
# endif

# define UCONTEXT_GREGS(ctx)	 (((ucontext_t *)(ctx))->uc_mcontext.gregs)
# define UCONTEXT_FREGS(ctx)     (((ucontext_t *)(ctx))->uc_mcontext.fpregs->fprs)
# define UCONTEXT_REG_Rn(ctx, n) (((ucontext_t *)(ctx))->uc_mcontext.gregs[(n)])
# define UCONTEXT_IP(ctx)         (((ucontext_t *)(ctx))->uc_mcontext.psw.addr)

#endif

#elif defined (TARGET_RISCV)

#if defined(MONO_CROSS_COMPILE)

#define UCONTEXT_GREGS(ctx) (NULL)
#define UCONTEXT_FREGS(ctx) (NULL)
#define UCONTEXT_REG_PC(ctx) (NULL)
#define UCONTEXT_REG_BP(ctx) (NULL)
#define UCONTEXT_REG_SP(ctx) (NULL)

#else

#include <ucontext.h>

#define UCONTEXT_GREGS(ctx) (((ucontext_t *) (ctx))->uc_mcontext.__gregs)
#define UCONTEXT_FREGS(ctx) (((ucontext_t *) (ctx))->uc_mcontext.__fpregs)
#define UCONTEXT_REG_PC(ctx) (UCONTEXT_GREGS ((ctx)) [REG_PC])
#define UCONTEXT_REG_BP(ctx) (UCONTEXT_GREGS ((ctx)) [REG_S0])
#define UCONTEXT_REG_SP(ctx) (UCONTEXT_GREGS ((ctx)) [REG_SP])

#endif

#endif
