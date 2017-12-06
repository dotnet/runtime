/**
 * \file
 * plat independent machine state definitions
 *
 *
 * Copyright (c) 2011 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */


#ifndef __MONO_MONO_CONTEXT_H__
#define __MONO_MONO_CONTEXT_H__

#include "mono-compiler.h"
#include "mono-sigcontext.h"
#include "mono-machine.h"

#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

#define MONO_CONTEXT_OFFSET(field, index, field_type) \
    "i" (offsetof (MonoContext, field) + (index) * sizeof (field_type))

#if defined(TARGET_X86)
#if defined(__APPLE__)
typedef struct __darwin_xmm_reg MonoContextSimdReg;
#endif
#elif defined(TARGET_AMD64)
#if defined(__APPLE__)
typedef struct __darwin_xmm_reg MonoContextSimdReg;
#elif defined(__linux__) && defined(__GLIBC__)
typedef struct _libc_xmmreg MonoContextSimdReg;
#elif defined(HOST_WIN32)
#include <emmintrin.h>
typedef __m128d MonoContextSimdReg;
#elif defined(HOST_ANDROID)
typedef struct _libc_xmmreg MonoContextSimdReg;
#elif defined(__linux__)
#include <emmintrin.h>
typedef __m128d MonoContextSimdReg;
#endif
#elif defined(TARGET_ARM64)
typedef __uint128_t MonoContextSimdReg;
#endif

/*
 * General notes about mono-context.
 * Each arch defines a MonoContext struct with all GPR regs + IP/PC.
 * IP/PC should be the last element of the struct (this is a mild sgen constraint we could drop if needed)
 * Macros to get/set BP, SP and IP are defined too.
 * MONO_CONTEXT_GET_CURRENT captures the current context as close as possible. One reg might be clobbered
 *  to hold the address of the target MonoContext. It will be a caller save one, so should not be a problem.
 */
#if defined (TARGET_WASM)

typedef struct {
	mgreg_t wasm_sp;
	mgreg_t wasm_bp;
	mgreg_t llvm_exc_reg;
	mgreg_t wasm_ip;
	mgreg_t wasm_pc;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->wasm_ip = (mgreg_t)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->wasm_bp = (mgreg_t)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->wasm_sp = (mgreg_t)(sp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->wasm_ip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->wasm_bp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->wasm_sp))

#elif (defined(__i386__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_X86))

/*HACK, move this to an eventual mono-signal.c*/
#if defined( __linux__) || defined(__sun) || defined(__APPLE__) || defined(__NetBSD__) || \
       defined(__FreeBSD__) || defined(__OpenBSD__)
#if defined(HAVE_SIGACTION) || defined(__APPLE__)  // the __APPLE__ check is required for the tvos simulator, which has ucontext_t but not sigaction
#define MONO_SIGNAL_USE_UCONTEXT_T 1
#endif
#endif

#ifdef __HAIKU__
/* sigcontext surrogate */
struct sigcontext {
	vregs regs;
};
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
# define SC_EBX regs.ebx
# define SC_ECX regs.ecx
# define SC_EDX regs.edx
# define SC_EBP regs.ebp
# define SC_EIP regs.eip
# define SC_ESP regs.esp
# define SC_EDI regs.edi
# define SC_ESI regs.esi
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

#include <mono/arch/x86/x86-codegen.h>

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
#ifdef __APPLE__
    MonoContextSimdReg fregs [X86_XMM_NREG];
#endif
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->eip = (mgreg_t)(ip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->ebp = (mgreg_t)(bp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->esp = (mgreg_t)(sp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->eip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->ebp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->esp))

/*We set EAX to zero since we are clobering it anyway*/
#ifdef _MSC_VER
#define MONO_CONTEXT_GET_CURRENT(ctx) do { \
	void *_ptr = &(ctx);												\
	__asm {																\
	 __asm mov eax, _ptr												\
	 __asm mov [eax+0x00], eax											\
	 __asm mov [eax+0x04], ebx											\
	 __asm mov [eax+0x08], ecx											\
	 __asm mov [eax+0x0c], edx											\
	 __asm mov [eax+0x10], ebp											\
	 __asm mov [eax+0x14], esp											\
	 __asm mov [eax+0x18], esi											\
	 __asm mov [eax+0x1c], edi											\
	 __asm call __mono_context_get_ip									\
	 __asm __mono_context_get_ip:										\
	 __asm pop dword ptr [eax+0x20]										\
		 }																\
	} while (0)
#else

#define MONO_CONTEXT_GET_CURRENT_GREGS(ctx) \
	__asm__ __volatile__(   \
	"movl $0x0, %c[eax](%0)\n" \
	"mov %%ebx, %c[ebx](%0)\n" \
	"mov %%ecx, %c[ecx](%0)\n" \
	"mov %%edx, %c[edx](%0)\n" \
	"mov %%ebp, %c[ebp](%0)\n" \
	"mov %%esp, %c[esp](%0)\n" \
	"mov %%esi, %c[esi](%0)\n" \
	"mov %%edi, %c[edi](%0)\n" \
	"call 1f\n"     \
	"1: pop 0x20(%0)\n"     \
	:	\
	: "a" (&(ctx)),	\
		[eax] MONO_CONTEXT_OFFSET (eax, 0, mgreg_t), \
		[ebx] MONO_CONTEXT_OFFSET (ebx, 0, mgreg_t), \
		[ecx] MONO_CONTEXT_OFFSET (ecx, 0, mgreg_t), \
		[edx] MONO_CONTEXT_OFFSET (edx, 0, mgreg_t), \
		[ebp] MONO_CONTEXT_OFFSET (ebp, 0, mgreg_t), \
		[esp] MONO_CONTEXT_OFFSET (esp, 0, mgreg_t), \
		[esi] MONO_CONTEXT_OFFSET (esi, 0, mgreg_t), \
		[edi] MONO_CONTEXT_OFFSET (edi, 0, mgreg_t) \
	: "memory")

#ifdef UCONTEXT_REG_XMM
#define MONO_CONTEXT_GET_CURRENT_FREGS(ctx) \
	do { \
		__asm__ __volatile__ ( \
			"movups %%xmm0, %c[xmm0](%0)\n"	\
			"movups %%xmm1, %c[xmm1](%0)\n"	\
			"movups %%xmm2, %c[xmm2](%0)\n"	\
			"movups %%xmm3, %c[xmm3](%0)\n"	\
			"movups %%xmm4, %c[xmm4](%0)\n"	\
			"movups %%xmm5, %c[xmm5](%0)\n"	\
			"movups %%xmm6, %c[xmm6](%0)\n"	\
			"movups %%xmm7, %c[xmm7](%0)\n"	\
			: \
			: "a" (&(ctx)),	\
				[xmm0] MONO_CONTEXT_OFFSET (fregs, X86_XMM0, MonoContextSimdReg), \
				[xmm1] MONO_CONTEXT_OFFSET (fregs, X86_XMM1, MonoContextSimdReg), \
				[xmm2] MONO_CONTEXT_OFFSET (fregs, X86_XMM2, MonoContextSimdReg), \
				[xmm3] MONO_CONTEXT_OFFSET (fregs, X86_XMM3, MonoContextSimdReg), \
				[xmm4] MONO_CONTEXT_OFFSET (fregs, X86_XMM4, MonoContextSimdReg), \
				[xmm5] MONO_CONTEXT_OFFSET (fregs, X86_XMM5, MonoContextSimdReg), \
				[xmm6] MONO_CONTEXT_OFFSET (fregs, X86_XMM6, MonoContextSimdReg), \
				[xmm7] MONO_CONTEXT_OFFSET (fregs, X86_XMM7, MonoContextSimdReg)); \
	} while (0)
#else
#define MONO_CONTEXT_GET_CURRENT_FREGS(ctx)
#endif

#define MONO_CONTEXT_GET_CURRENT(ctx) \
    do {	\
		MONO_CONTEXT_GET_CURRENT_GREGS(ctx);	\
		MONO_CONTEXT_GET_CURRENT_FREGS(ctx);	\
	} while (0)

#endif

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#elif (defined(__x86_64__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_AMD64)) /* defined(__i386__) */

#include <mono/arch/amd64/amd64-codegen.h>

#if !defined( HOST_WIN32 )

// the __APPLE__ check is required for the tvos simulator, which has ucontext_t but not sigaction
#if defined(HAVE_SIGACTION) || defined(__APPLE__)
#define MONO_SIGNAL_USE_UCONTEXT_T 1
#endif

#endif

#ifdef __HAIKU__
/* sigcontext surrogate */
struct sigcontext {
	vregs regs;
};

// Haiku doesn't support this
#undef MONO_SIGNAL_USE_UCONTEXT_T
#endif

typedef struct {
	mgreg_t gregs [AMD64_NREG];
#if defined(__APPLE__) || (defined(__linux__) && defined(__GLIBC__)) || defined(HOST_WIN32)
	MonoContextSimdReg fregs [AMD64_XMM_NREG];
#else
	double fregs [AMD64_XMM_NREG];
#endif
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->gregs [AMD64_RIP] = (mgreg_t)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->gregs [AMD64_RBP] = (mgreg_t)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->gregs [AMD64_RSP] = (mgreg_t)(esp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->gregs [AMD64_RIP]))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->gregs [AMD64_RBP]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->gregs [AMD64_RSP]))

#if defined (HOST_WIN32) && !defined(__GNUC__)
/* msvc doesn't support inline assembly, so have to use a separate .asm file */
extern void mono_context_get_current (void *);
#define MONO_CONTEXT_GET_CURRENT(ctx) do { mono_context_get_current((void*)&(ctx)); } while (0)

#else

#define MONO_CONTEXT_GET_CURRENT_GREGS(ctx) \
	do { \
		__asm__ __volatile__(	\
			"movq $0x0,  %c[rax](%0)\n"	\
			"movq %%rcx, %c[rcx](%0)\n"	\
			"movq %%rdx, %c[rdx](%0)\n"	\
			"movq %%rbx, %c[rbx](%0)\n"	\
			"movq %%rsp, %c[rsp](%0)\n"	\
			"movq %%rbp, %c[rbp](%0)\n"	\
			"movq %%rsi, %c[rsi](%0)\n"	\
			"movq %%rdi, %c[rdi](%0)\n"	\
			"movq %%r8,  %c[r8](%0)\n"	\
			"movq %%r9,  %c[r9](%0)\n"	\
			"movq %%r10, %c[r10](%0)\n"	\
			"movq %%r11, %c[r11](%0)\n"	\
			"movq %%r12, %c[r12](%0)\n"	\
			"movq %%r13, %c[r13](%0)\n"	\
			"movq %%r14, %c[r14](%0)\n"	\
			"movq %%r15, %c[r15](%0)\n"	\
			/* "leaq (%%rip), %%rdx\n" is not understood by icc */	\
			".byte 0x48, 0x8d, 0x15, 0x00, 0x00, 0x00, 0x00\n" \
			"movq %%rdx, %c[rip](%0)\n"	\
			: 	\
			: "a" (&(ctx)),	\
				[rax] MONO_CONTEXT_OFFSET (gregs, AMD64_RAX, mgreg_t),	\
				[rcx] MONO_CONTEXT_OFFSET (gregs, AMD64_RCX, mgreg_t),	\
				[rdx] MONO_CONTEXT_OFFSET (gregs, AMD64_RDX, mgreg_t),	\
				[rbx] MONO_CONTEXT_OFFSET (gregs, AMD64_RBX, mgreg_t),	\
				[rsp] MONO_CONTEXT_OFFSET (gregs, AMD64_RSP, mgreg_t),	\
				[rbp] MONO_CONTEXT_OFFSET (gregs, AMD64_RBP, mgreg_t),	\
				[rsi] MONO_CONTEXT_OFFSET (gregs, AMD64_RSI, mgreg_t),	\
				[rdi] MONO_CONTEXT_OFFSET (gregs, AMD64_RDI, mgreg_t),	\
				[r8] MONO_CONTEXT_OFFSET (gregs, AMD64_R8, mgreg_t), \
				[r9] MONO_CONTEXT_OFFSET (gregs, AMD64_R9, mgreg_t), \
				[r10] MONO_CONTEXT_OFFSET (gregs, AMD64_R10, mgreg_t),	\
				[r11] MONO_CONTEXT_OFFSET (gregs, AMD64_R11, mgreg_t),	\
				[r12] MONO_CONTEXT_OFFSET (gregs, AMD64_R12, mgreg_t),	\
				[r13] MONO_CONTEXT_OFFSET (gregs, AMD64_R13, mgreg_t),	\
				[r14] MONO_CONTEXT_OFFSET (gregs, AMD64_R14, mgreg_t),	\
				[r15] MONO_CONTEXT_OFFSET (gregs, AMD64_R15, mgreg_t),	\
				[rip] MONO_CONTEXT_OFFSET (gregs, AMD64_RIP, mgreg_t)	\
			: "rdx", "memory");	\
	} while (0)

#ifdef UCONTEXT_REG_XMM
#define MONO_CONTEXT_GET_CURRENT_FREGS(ctx) \
	do { \
		__asm__ __volatile__ ( \
			"movups %%xmm0, %c[xmm0](%0)\n"	\
			"movups %%xmm1, %c[xmm1](%0)\n"	\
			"movups %%xmm2, %c[xmm2](%0)\n"	\
			"movups %%xmm3, %c[xmm3](%0)\n"	\
			"movups %%xmm4, %c[xmm4](%0)\n"	\
			"movups %%xmm5, %c[xmm5](%0)\n"	\
			"movups %%xmm6, %c[xmm6](%0)\n"	\
			"movups %%xmm7, %c[xmm7](%0)\n"	\
			"movups %%xmm8, %c[xmm8](%0)\n"	\
			"movups %%xmm9, %c[xmm9](%0)\n"	\
			"movups %%xmm10, %c[xmm10](%0)\n"	\
			"movups %%xmm11, %c[xmm11](%0)\n"	\
			"movups %%xmm12, %c[xmm12](%0)\n"	\
			"movups %%xmm12, %c[xmm12](%0)\n"	\
			"movups %%xmm14, %c[xmm14](%0)\n"	\
			"movups %%xmm15, %c[xmm15](%0)\n"	\
			: \
			: "a" (&(ctx)),	\
				[xmm0] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM0, MonoContextSimdReg), \
				[xmm1] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM1, MonoContextSimdReg), \
				[xmm2] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM2, MonoContextSimdReg), \
				[xmm3] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM3, MonoContextSimdReg), \
				[xmm4] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM4, MonoContextSimdReg), \
				[xmm5] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM5, MonoContextSimdReg), \
				[xmm6] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM6, MonoContextSimdReg), \
				[xmm7] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM7, MonoContextSimdReg), \
				[xmm8] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM8, MonoContextSimdReg), \
				[xmm9] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM9, MonoContextSimdReg), \
				[xmm10] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM10, MonoContextSimdReg), \
				[xmm11] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM11, MonoContextSimdReg), \
				[xmm12] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM12, MonoContextSimdReg), \
				[xmm13] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM13, MonoContextSimdReg), \
				[xmm14] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM14, MonoContextSimdReg), \
				[xmm15] MONO_CONTEXT_OFFSET (fregs, AMD64_XMM15, MonoContextSimdReg));	\
	} while (0)
#else
#define MONO_CONTEXT_GET_CURRENT_FREGS(ctx)
#endif

#define MONO_CONTEXT_GET_CURRENT(ctx) \
    do {	\
		MONO_CONTEXT_GET_CURRENT_GREGS(ctx);	\
		MONO_CONTEXT_GET_CURRENT_FREGS(ctx);	\
	} while (0)
#endif

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#elif (defined(__arm__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_ARM)) /* defined(__x86_64__) */

#include <mono/arch/arm/arm-codegen.h>

typedef struct {
	mgreg_t pc;
	mgreg_t regs [16];
	double fregs [16];
	mgreg_t cpsr;
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->pc = (mgreg_t)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->regs [ARMREG_FP] = (mgreg_t)bp; } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,bp) do { (ctx)->regs [ARMREG_SP] = (mgreg_t)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->pc))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs [ARMREG_FP]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->regs [ARMREG_SP]))

#if defined(HOST_WATCHOS)

#define MONO_CONTEXT_GET_CURRENT(ctx) do { \
	gpointer _dummy; \
    ctx.regs [ARMREG_SP] = &_dummy; \
} while (0);

#else

#define MONO_CONTEXT_GET_CURRENT(ctx)	do { 	\
	__asm__ __volatile__(			\
		"push {r0}\n"				\
		"push {r1}\n"				\
		"mov r0, %0\n"				\
		"ldr r1, [sp, #4]\n"   		\
		"str r1, [r0], #4\n"   		\
		"ldr r1, [sp, #0]\n"	   	\
		"str r1, [r0], #4\n"	   	\
		"stmia r0!, {r2-r12}\n"		\
		"str sp, [r0], #4\n"		\
		"str lr, [r0], #4\n"		\
		"mov r1, pc\n"				\
		"str r1, [r0], #4\n"		\
		"pop {r1}\n"				\
		"pop {r0}\n"				\
		:							\
		: "r" (&ctx.regs)			\
		: "memory"					\
	);								\
	ctx.pc = ctx.regs [15];			\
} while (0)

#endif

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#elif (defined(__aarch64__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_ARM64))

#include <mono/arch/arm64/arm64-codegen.h>

typedef struct {
	mgreg_t regs [32];
	/* FIXME not fully saved in trampolines */
	MonoContextSimdReg fregs [32];
	mgreg_t pc;
	/*
	 * fregs might not be initialized if this context was created from a
	 * ucontext.
	 */
	mgreg_t has_fregs;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->pc = (mgreg_t)ip; } while (0)
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->regs [ARMREG_FP] = (mgreg_t)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,bp) do { (ctx)->regs [ARMREG_SP] = (mgreg_t)bp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) (gpointer)((ctx)->pc)
#define MONO_CONTEXT_GET_BP(ctx) (gpointer)((ctx)->regs [ARMREG_FP])
#define MONO_CONTEXT_GET_SP(ctx) (gpointer)((ctx)->regs [ARMREG_SP])

#if defined (HOST_APPLETVOS)

#define MONO_CONTEXT_GET_CURRENT(ctx) do { \
	arm_unified_thread_state_t thread_state;	\
	arm_neon_state64_t thread_fpstate;		\
	int state_flavor = ARM_UNIFIED_THREAD_STATE;	\
	int fpstate_flavor = ARM_NEON_STATE64;	\
	unsigned state_count = ARM_UNIFIED_THREAD_STATE_COUNT;	\
	unsigned fpstate_count = ARM_NEON_STATE64_COUNT;	\
	thread_port_t self = mach_thread_self ();	\
	kern_return_t ret = thread_get_state (self, state_flavor, (thread_state_t) &thread_state, &state_count);	\
	g_assert (ret == 0);	\
	ret = thread_get_state (self, fpstate_flavor, (thread_state_t) &thread_fpstate, &fpstate_count);	\
	g_assert (ret == 0);	\
	mono_mach_arch_thread_states_to_mono_context ((thread_state_t) &thread_state, (thread_state_t) &thread_fpstate, &ctx); \
	mach_port_deallocate (current_task (), self);	\
} while (0);

#else

#define MONO_CONTEXT_GET_CURRENT(ctx)	do { 	\
	__asm__ __volatile__(			\
		"mov x16, %0\n" \
		"stp x0, x1, [x16], #16\n"	\
		"stp x2, x3, [x16], #16\n"	\
		"stp x4, x5, [x16], #16\n"	\
		"stp x6, x7, [x16], #16\n"	\
		"stp x8, x9, [x16], #16\n"	\
		"stp x10, x11, [x16], #16\n"	\
		"stp x12, x13, [x16], #16\n"	\
		"stp x14, x15, [x16], #16\n"	\
		"stp xzr, x17, [x16], #16\n"	\
		"stp x18, x19, [x16], #16\n"	\
		"stp x20, x21, [x16], #16\n"	\
		"stp x22, x23, [x16], #16\n"	\
		"stp x24, x25, [x16], #16\n"	\
		"stp x26, x27, [x16], #16\n"	\
		"stp x28, x29, [x16], #16\n"	\
		"stp x30, xzr, [x16], #8\n"	\
		"mov x30, sp\n"				\
		"str x30, [x16], #8\n"		\
		"stp q0, q1, [x16], #32\n"	\
		"stp q2, q3, [x16], #32\n"	\
		"stp q4, q5, [x16], #32\n"	\
		"stp q6, q7, [x16], #32\n"	\
		"stp q8, q9, [x16], #32\n"	\
		"stp q10, q11, [x16], #32\n"	\
		"stp q12, q13, [x16], #32\n"	\
		"stp q14, q15, [x16], #32\n"	\
		"stp q16, q17, [x16], #32\n"	\
		"stp q18, q19, [x16], #32\n"	\
		"stp q20, q21, [x16], #32\n"	\
		"stp q22, q23, [x16], #32\n"	\
		"stp q24, q25, [x16], #32\n"	\
		"stp q26, q27, [x16], #32\n"	\
		"stp q28, q29, [x16], #32\n"	\
		"stp q30, q31, [x16], #32\n"		\
		:							\
		: "r" (&ctx.regs)			\
		: "x16", "x30", "memory"		\
	);								\
	__asm__ __volatile__( \
		"adr %0, L0%=\n" \
		"L0%=:\n"	\
		: "=r" (ctx.pc)		\
		:					\
		: "memory"			 \
	); \
} while (0)

#endif

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#elif defined(__mono_ppc__) /* defined(__arm__) */

/* we define our own structure and we'll copy the data
 * from sigcontext/ucontext/mach when we need it.
 * This also makes us save stack space and time when copying
 * We might also want to add an additional field to propagate
 * the original context from the signal handler.
 */
#ifdef __mono_ppc64__

typedef struct {
	gulong sc_ir;          // pc 
	gulong sc_sp;          // r1
	mgreg_t regs [32];
	double fregs [32];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (gulong)ip; } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (gulong)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_sp = (gulong)sp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs [ppc_r31]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_sp))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"std 0, 0(%0)\n"	\
		"std 1, 8(%0)\n"	\
		"std 0, 8*0+16(%0)\n"	\
		"std 1, 8*1+16(%0)\n"	\
		"std 2, 8*2+16(%0)\n"	\
		"std 3, 8*3+16(%0)\n"	\
		"std 4, 8*4+16(%0)\n"	\
		"std 5, 8*5+16(%0)\n"	\
		"std 6, 8*6+16(%0)\n"	\
		"std 7, 8*7+16(%0)\n"	\
		"std 8, 8*8+16(%0)\n"	\
		"std 9, 8*9+16(%0)\n"	\
		"std 10, 8*10+16(%0)\n"	\
		"std 11, 8*11+16(%0)\n"	\
		"std 12, 8*12+16(%0)\n"	\
		"std 13, 8*13+16(%0)\n"	\
		"std 14, 8*14+16(%0)\n"	\
		"std 15, 8*15+16(%0)\n"	\
		"std 16, 8*16+16(%0)\n"	\
		"std 17, 8*17+16(%0)\n"	\
		"std 18, 8*18+16(%0)\n"	\
		"std 19, 8*19+16(%0)\n"	\
		"std 20, 8*20+16(%0)\n"	\
		"std 21, 8*21+16(%0)\n"	\
		"std 22, 8*22+16(%0)\n"	\
		"std 23, 8*23+16(%0)\n"	\
		"std 24, 8*24+16(%0)\n"	\
		"std 25, 8*25+16(%0)\n"	\
		"std 26, 8*26+16(%0)\n"	\
		"std 27, 8*27+16(%0)\n"	\
		"std 28, 8*28+16(%0)\n"	\
		"std 29, 8*29+16(%0)\n"	\
		"std 30, 8*30+16(%0)\n"	\
		"std 31, 8*31+16(%0)\n"	\
		"stfd 0, 8*0+8*32+16(%0)\n"	\
		"stfd 1, 8*1+8*32+16(%0)\n"	\
		"stfd 2, 8*2+8*32+16(%0)\n"	\
		"stfd 3, 8*3+8*32+16(%0)\n"	\
		"stfd 4, 8*4+8*32+16(%0)\n"	\
		"stfd 5, 8*5+8*32+16(%0)\n"	\
		"stfd 6, 8*6+8*32+16(%0)\n"	\
		"stfd 7, 8*7+8*32+16(%0)\n"	\
		"stfd 8, 8*8+8*32+16(%0)\n"	\
		"stfd 9, 8*9+8*32+16(%0)\n"	\
		"stfd 10, 8*10+8*32+16(%0)\n"	\
		"stfd 11, 8*11+8*32+16(%0)\n"	\
		"stfd 12, 8*12+8*32+16(%0)\n"	\
		"stfd 13, 8*13+8*32+16(%0)\n"	\
		"stfd 14, 8*14+8*32+16(%0)\n"	\
		"stfd 15, 8*15+8*32+16(%0)\n"	\
		"stfd 16, 8*16+8*32+16(%0)\n"	\
		"stfd 17, 8*17+8*32+16(%0)\n"	\
		"stfd 18, 8*18+8*32+16(%0)\n"	\
		"stfd 19, 8*19+8*32+16(%0)\n"	\
		"stfd 20, 8*20+8*32+16(%0)\n"	\
		"stfd 21, 8*21+8*32+16(%0)\n"	\
		"stfd 22, 8*22+8*32+16(%0)\n"	\
		"stfd 23, 8*23+8*32+16(%0)\n"	\
		"stfd 24, 8*24+8*32+16(%0)\n"	\
		"stfd 25, 8*25+8*32+16(%0)\n"	\
		"stfd 26, 8*26+8*32+16(%0)\n"	\
		"stfd 27, 8*27+8*32+16(%0)\n"	\
		"stfd 28, 8*28+8*32+16(%0)\n"	\
		"stfd 29, 8*29+8*32+16(%0)\n"	\
		"stfd 30, 8*30+8*32+16(%0)\n"	\
		"stfd 31, 8*31+8*32+16(%0)\n"	\
		: : "r" (&(ctx))	\
		: "memory"			\
	)

#else /* !defined(__mono_ppc64__) */

typedef struct {
	mgreg_t sc_ir;          // pc
	mgreg_t sc_sp;          // r1
	mgreg_t regs [32];
	double fregs [32];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (mgreg_t)ip; } while (0);
/* FIXME: should be called SET_SP */
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (mgreg_t)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_sp = (mgreg_t)sp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs [ppc_r31]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_sp))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"stw 0, 0(%0)\n"	\
		"stw 1, 4(%0)\n"	\
		"stw 0, 4*0+8(%0)\n"	\
		"stw 1, 4*1+8(%0)\n"	\
		"stw 2, 4*2+8(%0)\n"	\
		"stw 3, 4*3+8(%0)\n"	\
		"stw 4, 4*4+8(%0)\n"	\
		"stw 5, 4*5+8(%0)\n"	\
		"stw 6, 4*6+8(%0)\n"	\
		"stw 7, 4*7+8(%0)\n"	\
		"stw 8, 4*8+8(%0)\n"	\
		"stw 9, 4*9+8(%0)\n"	\
		"stw 10, 4*10+8(%0)\n"	\
		"stw 11, 4*11+8(%0)\n"	\
		"stw 12, 4*12+8(%0)\n"	\
		"stw 13, 4*13+8(%0)\n"	\
		"stw 14, 4*14+8(%0)\n"	\
		"stw 15, 4*15+8(%0)\n"	\
		"stw 16, 4*16+8(%0)\n"	\
		"stw 17, 4*17+8(%0)\n"	\
		"stw 18, 4*18+8(%0)\n"	\
		"stw 19, 4*19+8(%0)\n"	\
		"stw 20, 4*20+8(%0)\n"	\
		"stw 21, 4*21+8(%0)\n"	\
		"stw 22, 4*22+8(%0)\n"	\
		"stw 23, 4*23+8(%0)\n"	\
		"stw 24, 4*24+8(%0)\n"	\
		"stw 25, 4*25+8(%0)\n"	\
		"stw 26, 4*26+8(%0)\n"	\
		"stw 27, 4*27+8(%0)\n"	\
		"stw 28, 4*28+8(%0)\n"	\
		"stw 29, 4*29+8(%0)\n"	\
		"stw 30, 4*30+8(%0)\n"	\
		"stw 31, 4*31+8(%0)\n"	\
		"stfd 0, 8*0+4*32+8(%0)\n"	\
		"stfd 1, 8*1+4*32+8(%0)\n"	\
		"stfd 2, 8*2+4*32+8(%0)\n"	\
		"stfd 3, 8*3+4*32+8(%0)\n"	\
		"stfd 4, 8*4+4*32+8(%0)\n"	\
		"stfd 5, 8*5+4*32+8(%0)\n"	\
		"stfd 6, 8*6+4*32+8(%0)\n"	\
		"stfd 7, 8*7+4*32+8(%0)\n"	\
		"stfd 8, 8*8+4*32+8(%0)\n"	\
		"stfd 9, 8*9+4*32+8(%0)\n"	\
		"stfd 10, 8*10+4*32+8(%0)\n"	\
		"stfd 11, 8*11+4*32+8(%0)\n"	\
		"stfd 12, 8*12+4*32+8(%0)\n"	\
		"stfd 13, 8*13+4*32+8(%0)\n"	\
		"stfd 14, 8*14+4*32+8(%0)\n"	\
		"stfd 15, 8*15+4*32+8(%0)\n"	\
		"stfd 16, 8*16+4*32+8(%0)\n"	\
		"stfd 17, 8*17+4*32+8(%0)\n"	\
		"stfd 18, 8*18+4*32+8(%0)\n"	\
		"stfd 19, 8*19+4*32+8(%0)\n"	\
		"stfd 20, 8*20+4*32+8(%0)\n"	\
		"stfd 21, 8*21+4*32+8(%0)\n"	\
		"stfd 22, 8*22+4*32+8(%0)\n"	\
		"stfd 23, 8*23+4*32+8(%0)\n"	\
		"stfd 24, 8*24+4*32+8(%0)\n"	\
		"stfd 25, 8*25+4*32+8(%0)\n"	\
		"stfd 26, 8*26+4*32+8(%0)\n"	\
		"stfd 27, 8*27+4*32+8(%0)\n"	\
		"stfd 28, 8*28+4*32+8(%0)\n"	\
		"stfd 29, 8*29+4*32+8(%0)\n"	\
		"stfd 30, 8*30+4*32+8(%0)\n"	\
		"stfd 31, 8*31+4*32+8(%0)\n"	\
		: : "r" (&(ctx))	\
		: "memory", "r0"	\
	)

#endif

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#elif defined(__sparc__) || defined(sparc) /* defined(__mono_ppc__) */

typedef struct MonoContext {
	mgreg_t regs [15];
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

#ifdef __sparcv9
#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"st %%g1,[%0]\n"	\
		"st %%g2,[%0+0x08]\n"	\
		"st %%g3,[%0+0x10]\n"	\
		"st %%g4,[%0+0x18]\n"	\
		"st %%g5,[%0+0x20]\n"	\
		"st %%g6,[%0+0x28]\n"	\
		"st %%g7,[%0+0x30]\n"	\
		"st %%o0,[%0+0x38]\n"	\
		"st %%o1,[%0+0x40]\n"	\
		"st %%o2,[%0+0x48]\n"	\
		"st %%o3,[%0+0x50]\n"	\
		"st %%o4,[%0+0x58]\n"	\
		"st %%o5,[%0+0x60]\n"	\
		"st %%o6,[%0+0x68]\n"	\
		"st %%o7,[%0+0x70]\n"	\
		: 			\
		: "r" (&(ctx))		\
		: "memory"			\
	)
#else
#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"st %%g1,[%0]\n"	\
		"st %%g2,[%0+0x04]\n"	\
		"st %%g3,[%0+0x08]\n"	\
		"st %%g4,[%0+0x0c]\n"	\
		"st %%g5,[%0+0x10]\n"	\
		"st %%g6,[%0+0x14]\n"	\
		"st %%g7,[%0+0x18]\n"	\
		"st %%o0,[%0+0x1c]\n"	\
		"st %%o1,[%0+0x20]\n"	\
		"st %%o2,[%0+0x24]\n"	\
		"st %%o3,[%0+0x28]\n"	\
		"st %%o4,[%0+0x2c]\n"	\
		"st %%o5,[%0+0x30]\n"	\
		"st %%o6,[%0+0x34]\n"	\
		"st %%o7,[%0+0x38]\n"	\
		: 			\
		: "r" (&(ctx))		\
		: "memory"			\
	)
#endif

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#elif ((defined(__mips__) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_MIPS))) && SIZEOF_REGISTER == 4

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#include <mono/arch/mips/mips-codegen.h>

typedef struct {
	mgreg_t	    sc_pc;
	mgreg_t		sc_regs [32];
	gfloat		sc_fpregs [32];
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_pc = (mgreg_t)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_regs[mips_fp] = (mgreg_t)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_regs[mips_sp] = (mgreg_t)(sp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_pc))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sc_regs[mips_fp]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_regs[mips_sp]))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"sw $0,0(%0)\n\t"	\
		"sw $1,4(%0)\n\t"	\
		"sw $2,8(%0)\n\t"	\
		"sw $3,12(%0)\n\t"	\
		"sw $4,16(%0)\n\t"	\
		"sw $5,20(%0)\n\t"	\
		"sw $6,24(%0)\n\t"	\
		"sw $7,28(%0)\n\t"	\
		"sw $8,32(%0)\n\t"	\
		"sw $9,36(%0)\n\t"	\
		"sw $10,40(%0)\n\t"	\
		"sw $11,44(%0)\n\t"	\
		"sw $12,48(%0)\n\t"	\
		"sw $13,52(%0)\n\t"	\
		"sw $14,56(%0)\n\t"	\
		"sw $15,60(%0)\n\t"	\
		"sw $16,64(%0)\n\t"	\
		"sw $17,68(%0)\n\t"	\
		"sw $18,72(%0)\n\t"	\
		"sw $19,76(%0)\n\t"	\
		"sw $20,80(%0)\n\t"	\
		"sw $21,84(%0)\n\t"	\
		"sw $22,88(%0)\n\t"	\
		"sw $23,92(%0)\n\t"	\
		"sw $24,96(%0)\n\t"	\
		"sw $25,100(%0)\n\t"	\
		"sw $26,104(%0)\n\t"	\
		"sw $27,108(%0)\n\t"	\
		"sw $28,112(%0)\n\t"	\
		"sw $29,116(%0)\n\t"	\
		"sw $30,120(%0)\n\t"	\
		"sw $31,124(%0)\n\t"	\
		: : "r" (&(ctx).sc_regs [0])	\
		: "memory"			\
	)

#elif defined(__s390x__)

#define MONO_ARCH_HAS_MONO_CONTEXT 1

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
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->uc_mcontext.gregs[15]))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->uc_mcontext.gregs[11]))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"stmg	%%r0,%%r15,0(%0)\n"	\
		: : "r" (&(ctx).uc_mcontext.gregs[0])	\
		: "memory"			\
	)

#else

#error "Implement mono-context for the current arch"

#endif

/*
 * The naming is misleading, the SIGCTX argument should be the platform's context
 * structure (ucontext_c on posix, CONTEXT on windows).
 */
void mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx);

/*
 * This will not completely initialize SIGCTX since MonoContext contains less
 * information that the system context. The caller should obtain a SIGCTX from
 * the system, and use this function to override the parts of it which are
 * also in MonoContext.
 */
void mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx);

#endif /* __MONO_MONO_CONTEXT_H__ */
