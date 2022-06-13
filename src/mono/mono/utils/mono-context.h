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
#include <signal.h>

#define MONO_CONTEXT_OFFSET(field, index, field_type) \
    "i" (offsetof (MonoContext, field) + (index) * sizeof (field_type))

#if defined(TARGET_X86)
#if defined(__APPLE__)
#define MONO_HAVE_SIMD_REG
typedef struct __darwin_xmm_reg MonoContextSimdReg;
#endif
#elif defined(TARGET_AMD64)
#if defined(__APPLE__)
#define MONO_HAVE_SIMD_REG
typedef struct __darwin_xmm_reg MonoContextSimdReg;
#elif defined(__linux__) && defined(__GLIBC__)
#define MONO_HAVE_SIMD_REG
typedef struct _libc_xmmreg MonoContextSimdReg;
#elif defined(HOST_WIN32)
#define MONO_HAVE_SIMD_REG
//#define MONO_HAVE_SIMD_REG_AVX
#include <emmintrin.h>
typedef __m128d MonoContextSimdReg;
#elif defined(HOST_ANDROID)
#define MONO_HAVE_SIMD_REG
typedef struct _libc_xmmreg MonoContextSimdReg;
#elif defined(__linux__) || defined(__OpenBSD__)
#define MONO_HAVE_SIMD_REG
#include <emmintrin.h>
typedef __m128d MonoContextSimdReg;
#endif
#elif defined(TARGET_ARM64)
/* We need a definition for MonoContextSimdReg even when cross-compiling
   from Windows, but __uint128_t doesn't exist. Here __m128d is used as
   a stand-in. This is not expected to work for Windows ARM64 native builds. */
#if defined(HOST_WIN32)
#define MONO_HAVE_SIMD_REG
#include <emmintrin.h>
typedef __m128d MonoContextSimdReg;
#else
#define MONO_HAVE_SIMD_REG
typedef __uint128_t MonoContextSimdReg;
#endif
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
	host_mgreg_t wasm_sp;
	host_mgreg_t wasm_bp;
	host_mgreg_t llvm_exc_reg;
	host_mgreg_t wasm_ip;
	host_mgreg_t wasm_pc;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->wasm_ip = (host_mgreg_t)(gsize)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->wasm_bp = (host_mgreg_t)(gsize)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->wasm_sp = (host_mgreg_t)(gsize)(sp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(gsize)((ctx)->wasm_ip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(gsize)((ctx)->wasm_bp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(gsize)((ctx)->wasm_sp))

#elif ((defined(__i386__) || defined(_M_IX86)) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_X86))

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
	host_mgreg_t eax;
	host_mgreg_t ebx;
	host_mgreg_t ecx;
	host_mgreg_t edx;
	host_mgreg_t ebp;
	host_mgreg_t esp;
	host_mgreg_t esi;
	host_mgreg_t edi;
	host_mgreg_t eip;
#ifdef __APPLE__
    MonoContextSimdReg fregs [X86_XMM_NREG];
#endif
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->eip = (host_mgreg_t)(gsize)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->ebp = (host_mgreg_t)(gsize)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->esp = (host_mgreg_t)(gsize)(sp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(gsize)((ctx)->eip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(gsize)((ctx)->ebp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(gsize)((ctx)->esp))

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
		[eax] MONO_CONTEXT_OFFSET (eax, 0, host_mgreg_t), \
		[ebx] MONO_CONTEXT_OFFSET (ebx, 0, host_mgreg_t), \
		[ecx] MONO_CONTEXT_OFFSET (ecx, 0, host_mgreg_t), \
		[edx] MONO_CONTEXT_OFFSET (edx, 0, host_mgreg_t), \
		[ebp] MONO_CONTEXT_OFFSET (ebp, 0, host_mgreg_t), \
		[esp] MONO_CONTEXT_OFFSET (esp, 0, host_mgreg_t), \
		[esi] MONO_CONTEXT_OFFSET (esi, 0, host_mgreg_t), \
		[edi] MONO_CONTEXT_OFFSET (edi, 0, host_mgreg_t) \
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

#elif ((defined(__x86_64__) || defined(_M_X64)) && !defined(MONO_CROSS_COMPILE)) || (defined(TARGET_AMD64))

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

MONO_DISABLE_WARNING(4324) // 'struct_name' : structure was padded due to __declspec(align())

typedef struct {
	host_mgreg_t gregs [AMD64_NREG];
#if defined(MONO_HAVE_SIMD_REG_AVX)
	// Lower AMD64_XMM_NREG fregs holds lower 128 bit YMM. Upper AMD64_XMM_NREG fregs holds upper 128-bit YMM.
	MonoContextSimdReg fregs [AMD64_XMM_NREG * 2];
#elif defined(MONO_HAVE_SIMD_REG)
	MonoContextSimdReg fregs [AMD64_XMM_NREG];
#else
	double fregs [AMD64_XMM_NREG];
#endif
} MonoContext;

MONO_RESTORE_WARNING

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->gregs [AMD64_RIP] = (host_mgreg_t)(gsize)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->gregs [AMD64_RBP] = (host_mgreg_t)(gsize)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->gregs [AMD64_RSP] = (host_mgreg_t)(gsize)(esp); } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(gsize)((ctx)->gregs [AMD64_RIP]))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(gsize)((ctx)->gregs [AMD64_RBP]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(gsize)((ctx)->gregs [AMD64_RSP]))

#if defined(HOST_WIN32) && !defined(__GNUC__)
/* msvc doesn't support inline assembly, so have to use a separate .asm file */
// G_EXTERN_C due to being written in assembly.
#if defined(MONO_HAVE_SIMD_REG_AVX) && defined(__AVX__)
G_EXTERN_C void mono_context_get_current_avx (void *);
#define MONO_CONTEXT_GET_CURRENT(ctx) \
do { \
	mono_context_get_current_avx((void*)&(ctx)); \
} while (0)
#elif defined(MONO_HAVE_SIMD_REG_AVX)
G_EXTERN_C void mono_context_get_current (void *);
#define MONO_CONTEXT_GET_CURRENT(ctx) \
do { \
	mono_context_get_current((void*)&(ctx)); \
	memset (&(ctx.fregs [AMD64_XMM_NREG]), 0, sizeof (MonoContextSimdReg) * AMD64_XMM_NREG); \
} while (0)
#else
G_EXTERN_C void mono_context_get_current (void *);
#define MONO_CONTEXT_GET_CURRENT(ctx) \
do { \
	mono_context_get_current((void*)&(ctx)); \
} while (0)
#endif

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
				[rax] MONO_CONTEXT_OFFSET (gregs, AMD64_RAX, host_mgreg_t),	\
				[rcx] MONO_CONTEXT_OFFSET (gregs, AMD64_RCX, host_mgreg_t),	\
				[rdx] MONO_CONTEXT_OFFSET (gregs, AMD64_RDX, host_mgreg_t),	\
				[rbx] MONO_CONTEXT_OFFSET (gregs, AMD64_RBX, host_mgreg_t),	\
				[rsp] MONO_CONTEXT_OFFSET (gregs, AMD64_RSP, host_mgreg_t),	\
				[rbp] MONO_CONTEXT_OFFSET (gregs, AMD64_RBP, host_mgreg_t),	\
				[rsi] MONO_CONTEXT_OFFSET (gregs, AMD64_RSI, host_mgreg_t),	\
				[rdi] MONO_CONTEXT_OFFSET (gregs, AMD64_RDI, host_mgreg_t),	\
				[r8] MONO_CONTEXT_OFFSET (gregs, AMD64_R8, host_mgreg_t), \
				[r9] MONO_CONTEXT_OFFSET (gregs, AMD64_R9, host_mgreg_t), \
				[r10] MONO_CONTEXT_OFFSET (gregs, AMD64_R10, host_mgreg_t),	\
				[r11] MONO_CONTEXT_OFFSET (gregs, AMD64_R11, host_mgreg_t),	\
				[r12] MONO_CONTEXT_OFFSET (gregs, AMD64_R12, host_mgreg_t),	\
				[r13] MONO_CONTEXT_OFFSET (gregs, AMD64_R13, host_mgreg_t),	\
				[r14] MONO_CONTEXT_OFFSET (gregs, AMD64_R14, host_mgreg_t),	\
				[r15] MONO_CONTEXT_OFFSET (gregs, AMD64_R15, host_mgreg_t),	\
				[rip] MONO_CONTEXT_OFFSET (gregs, AMD64_RIP, host_mgreg_t)	\
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
	host_mgreg_t pc;
	host_mgreg_t regs [16];
	double fregs [16];
	host_mgreg_t cpsr;
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->pc = (host_mgreg_t)(gsize)ip; } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->regs [ARMREG_FP] = (host_mgreg_t)(gsize)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,bp) do { (ctx)->regs [ARMREG_SP] = (host_mgreg_t)(gsize)bp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(gsize)((ctx)->pc))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(gsize)((ctx)->regs [ARMREG_FP]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(gsize)((ctx)->regs [ARMREG_SP]))

#if defined(HOST_WATCHOS)

#define MONO_CONTEXT_GET_CURRENT(ctx) do { \
	gpointer _dummy; \
    ctx.regs [ARMREG_SP] = (host_mgreg_t)(gsize)&_dummy; \
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
	host_mgreg_t regs [32];
	/* FIXME not fully saved in trampolines */
	MonoContextSimdReg fregs [32];
	host_mgreg_t pc;
	/*
	 * fregs might not be initialized if this context was created from a
	 * ucontext.
	 */
	host_mgreg_t has_fregs;
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->pc = (host_mgreg_t)(gsize)ip; } while (0)
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->regs [ARMREG_FP] = (host_mgreg_t)(gsize)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,bp) do { (ctx)->regs [ARMREG_SP] = (host_mgreg_t)(gsize)bp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) (gpointer)(gsize)((ctx)->pc)
#define MONO_CONTEXT_GET_BP(ctx) (gpointer)(gsize)((ctx)->regs [ARMREG_FP])
#define MONO_CONTEXT_GET_SP(ctx) (gpointer)(gsize)((ctx)->regs [ARMREG_SP])

#if defined (HOST_TVOS)

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

#elif defined(HOST_WATCHOS)

#define MONO_CONTEXT_GET_CURRENT(ctx) do { \
	gpointer _dummy; \
	ctx.regs [ARMREG_SP] = (host_mgreg_t)(gsize)&_dummy; \
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

#elif (defined (HOST_POWERPC) && !defined (MONO_CROSS_COMPILE)) || defined (TARGET_POWERPC) /* defined(__arm__) */

/* we define our own structure and we'll copy the data
 * from sigcontext/ucontext/mach when we need it.
 * This also makes us save stack space and time when copying
 * We might also want to add an additional field to propagate
 * the original context from the signal handler.
 */
#if (defined (HOST_POWERPC64) && !defined (MONO_CROSS_COMPILE)) || defined (TARGET_POWERPC64)

typedef struct {
	gulong sc_ir;          // pc
	gulong sc_sp;          // r1
	host_mgreg_t regs [32];
	double fregs [32];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (gulong)(gsize)ip; } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (gulong)(gsize)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_sp = (gulong)(gsize)sp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(gsize)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(gsize)((ctx)->regs [ppc_r31]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(gsize)((ctx)->sc_sp))

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

#else /* !defined(HOST_POWERPC64) */

typedef struct {
	host_mgreg_t sc_ir;          // pc
	host_mgreg_t sc_sp;          // r1
	host_mgreg_t regs [32];
	double fregs [32];
} MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (host_mgreg_t)(gsize)ip; } while (0);
/* FIXME: should be called SET_SP */
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (host_mgreg_t)(gsize)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_sp = (host_mgreg_t)(gsize)sp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(gsize)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(gsize)((ctx)->regs [ppc_r31]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(gsize)((ctx)->sc_sp))

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

#elif defined(__s390x__)

#define MONO_ARCH_HAS_MONO_CONTEXT 1

#include <sys/ucontext.h>

#if __GLIBC_PREREQ(2, 26)
typedef ucontext_t MonoContext;
#else
typedef struct ucontext MonoContext;
#endif

#define MONO_CONTEXT_SET_IP(ctx,ip) 					\
	do {								\
		(ctx)->uc_mcontext.gregs[14] = (unsigned long)ip;	\
		(ctx)->uc_mcontext.psw.addr = (unsigned long)ip;	\
	} while (0);

#define MONO_CONTEXT_SET_SP(ctx,bp) MONO_CONTEXT_SET_BP((ctx),(bp))
#define MONO_CONTEXT_SET_BP(ctx,bp) 					\
	do {		 						\
		(ctx)->uc_mcontext.gregs[15] = (unsigned long)bp;	\
	} while (0)

#define MONO_CONTEXT_GET_IP(ctx) (gpointer) (ctx)->uc_mcontext.psw.addr
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->uc_mcontext.gregs[15]))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->uc_mcontext.gregs[11]))

#define MONO_CONTEXT_GET_CURRENT(ctx)	\
	__asm__ __volatile__(	\
		"stmg	%%r0,%%r15,0(%0)\n"	\
		"std	%%f0,0(%1)\n"		\
		"std	%%f1,8(%1)\n"		\
		"std	%%f2,16(%1)\n"		\
		"std	%%f3,24(%1)\n"		\
		"std	%%f4,32(%1)\n"		\
		"std	%%f5,40(%1)\n"		\
		"std	%%f6,48(%1)\n"		\
		"std	%%f7,56(%1)\n"		\
		"std	%%f8,64(%1)\n"		\
		"std	%%f9,72(%1)\n"		\
		"std	%%f10,80(%1)\n"		\
		"std	%%f11,88(%1)\n"		\
		"std	%%f12,96(%1)\n"		\
		"std	%%f13,104(%1)\n"		\
		"std	%%f14,112(%1)\n"		\
		"std	%%f15,120(%1)\n"		\
		: : "a" (&(ctx).uc_mcontext.gregs[0]),		\
		    "a" (&(ctx).uc_mcontext.fpregs.fprs[0])	\
		: "memory"			\
	)

#elif (defined (HOST_RISCV) && !defined (MONO_CROSS_COMPILE)) || defined (TARGET_RISCV)

#include <mono/arch/riscv/riscv-codegen.h>

typedef struct {
	host_mgreg_t gregs [RISCV_N_GREGS]; // [0] contains pc since x0 is hard-wired to zero anyway.
	double fregs [RISCV_N_FREGS * 2 + 2]; // [32] contains fcsr (32 bits), the rest is for quad-precision values (currently unused).
} MonoContext;

#define MONO_CONTEXT_SET_IP(ctx, ip) do { (ctx)->gregs [RISCV_ZERO] = (host_mgreg_t) (ip); } while (0)
#define MONO_CONTEXT_SET_BP(ctx, bp) do { (ctx)->gregs [RISCV_FP] = (host_mgreg_t) (bp); } while (0)
#define MONO_CONTEXT_SET_SP(ctx, sp) do { (ctx)->gregs [RISCV_SP] = (host_mgreg_t) (sp); } while (0)

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer) ((ctx)->gregs [RISCV_ZERO]))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer) ((ctx)->gregs [RISCV_FP]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer) ((ctx)->gregs [RISCV_SP]))

#ifdef TARGET_RISCV64
#define _RISCV_STR "sd"
#define _RISCV_SZ "8"
#else
#define _RISCV_STR "sw"
#define _RISCV_SZ "4"
#endif

#define MONO_CONTEXT_GET_CURRENT(ctx) \
	do { \
		__asm__ __volatile__ ( \
			_RISCV_STR " x1, " _RISCV_SZ "*1(%0)\n" \
			_RISCV_STR " x2, " _RISCV_SZ "*2(%0)\n" \
			_RISCV_STR " x3, " _RISCV_SZ "*3(%0)\n" \
			_RISCV_STR " x4, " _RISCV_SZ "*4(%0)\n" \
			_RISCV_STR " x5, " _RISCV_SZ "*5(%0)\n" \
			_RISCV_STR " x6, " _RISCV_SZ "*6(%0)\n" \
			_RISCV_STR " x7, " _RISCV_SZ "*7(%0)\n" \
			_RISCV_STR " x8, " _RISCV_SZ "*8(%0)\n" \
			_RISCV_STR " x9, " _RISCV_SZ "*9(%0)\n" \
			_RISCV_STR " x10, " _RISCV_SZ "*10(%0)\n" \
			_RISCV_STR " x11, " _RISCV_SZ "*11(%0)\n" \
			_RISCV_STR " x12, " _RISCV_SZ "*12(%0)\n" \
			_RISCV_STR " x13, " _RISCV_SZ "*13(%0)\n" \
			_RISCV_STR " x14, " _RISCV_SZ "*14(%0)\n" \
			_RISCV_STR " x15, " _RISCV_SZ "*15(%0)\n" \
			_RISCV_STR " x16, " _RISCV_SZ "*16(%0)\n" \
			_RISCV_STR " x17, " _RISCV_SZ "*17(%0)\n" \
			_RISCV_STR " x18, " _RISCV_SZ "*18(%0)\n" \
			_RISCV_STR " x19, " _RISCV_SZ "*19(%0)\n" \
			_RISCV_STR " x20, " _RISCV_SZ "*20(%0)\n" \
			_RISCV_STR " x21, " _RISCV_SZ "*21(%0)\n" \
			_RISCV_STR " x22, " _RISCV_SZ "*22(%0)\n" \
			_RISCV_STR " x23, " _RISCV_SZ "*23(%0)\n" \
			_RISCV_STR " x24, " _RISCV_SZ "*24(%0)\n" \
			_RISCV_STR " x25, " _RISCV_SZ "*25(%0)\n" \
			_RISCV_STR " x26, " _RISCV_SZ "*26(%0)\n" \
			_RISCV_STR " x27, " _RISCV_SZ "*27(%0)\n" \
			_RISCV_STR " x28, " _RISCV_SZ "*28(%0)\n" \
			_RISCV_STR " x29, " _RISCV_SZ "*29(%0)\n" \
			_RISCV_STR " x30, " _RISCV_SZ "*30(%0)\n" \
			_RISCV_STR " x31, " _RISCV_SZ "*31(%0)\n" \
			: \
			: "r" (&(ctx).gregs) \
			: "memory" \
		); \
		__asm__ __volatile__ ( \
			"frcsr t0\n" \
			"fsd f0, 8*0(%0)\n" \
			"fsd f1, 8*1(%0)\n" \
			"fsd f2, 8*2(%0)\n" \
			"fsd f3, 8*3(%0)\n" \
			"fsd f4, 8*4(%0)\n" \
			"fsd f5, 8*5(%0)\n" \
			"fsd f6, 8*6(%0)\n" \
			"fsd f7, 8*7(%0)\n" \
			"fsd f8, 8*8(%0)\n" \
			"fsd f9, 8*9(%0)\n" \
			"fsd f10, 8*10(%0)\n" \
			"fsd f11, 8*11(%0)\n" \
			"fsd f12, 8*12(%0)\n" \
			"fsd f13, 8*13(%0)\n" \
			"fsd f14, 8*14(%0)\n" \
			"fsd f15, 8*15(%0)\n" \
			"fsd f16, 8*16(%0)\n" \
			"fsd f17, 8*17(%0)\n" \
			"fsd f18, 8*18(%0)\n" \
			"fsd f19, 8*19(%0)\n" \
			"fsd f20, 8*20(%0)\n" \
			"fsd f21, 8*21(%0)\n" \
			"fsd f22, 8*22(%0)\n" \
			"fsd f23, 8*23(%0)\n" \
			"fsd f24, 8*24(%0)\n" \
			"fsd f25, 8*25(%0)\n" \
			"fsd f26, 8*26(%0)\n" \
			"fsd f27, 8*27(%0)\n" \
			"fsd f28, 8*28(%0)\n" \
			"fsd f29, 8*29(%0)\n" \
			"fsd f30, 8*30(%0)\n" \
			"fsd f31, 8*31(%0)\n" \
			"sw t0, 8*32(%0)\n" \
			: \
			: "r" (&(ctx).fregs) \
			: "t0", "memory" \
		); \
		__asm__ __volatile__ ( \
			"auipc t0, 0\n" \
			_RISCV_STR " t0, (%0)\n" \
			: \
			: "r" (&(ctx).gregs [0]) \
			: "t0", "memory" \
		); \
	} while (0)

#define MONO_ARCH_HAS_MONO_CONTEXT (1)

#else

#error "Implement mono-context for the current arch"

#endif

/*
 * The naming is misleading, the SIGCTX argument should be the platform's context
 * structure (ucontext_c on posix, CONTEXT on windows).
 */
MONO_COMPONENT_API void mono_sigctx_to_monoctx (void *sigctx, MonoContext *mctx);

/*
 * This will not completely initialize SIGCTX since MonoContext contains less
 * information that the system context. The caller should obtain a SIGCTX from
 * the system, and use this function to override the parts of it which are
 * also in MonoContext.
 */
MONO_COMPONENT_API void mono_monoctx_to_sigctx (MonoContext *mctx, void *sigctx);

#endif /* __MONO_MONO_CONTEXT_H__ */
