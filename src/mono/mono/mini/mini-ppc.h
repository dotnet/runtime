#ifndef __MONO_MINI_PPC_H__
#define __MONO_MINI_PPC_H__

#include <mono/arch/ppc/ppc-codegen.h>
#include <mono/utils/mono-sigcontext.h>
#include <glib.h>

#ifdef __mono_ppc64__
#define MONO_ARCH_CPU_SPEC ppc64_cpu_desc
#else
#define MONO_ARCH_CPU_SPEC ppcg4
#endif

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_SAVED_GREGS 19
#define MONO_SAVED_FREGS 18

#define MONO_ARCH_FRAME_ALIGNMENT 16

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

void ppc_patch (guchar *code, const guchar *target);
void ppc_patch_full (guchar *code, const guchar *target, gboolean is_fd);

struct MonoLMF {
	/*
	 * If the second lowest bit is set to 1, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 */
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gulong     ebp;
	gulong     eip;
	/* Add a dummy field to force iregs to be aligned when cross compiling from x86 */
	gulong     dummy;
	mgreg_t    iregs [MONO_SAVED_GREGS]; /* 13..31 */
	gdouble    fregs [MONO_SAVED_FREGS]; /* 14..31 */
};

/* we define our own structure and we'll copy the data
 * from sigcontext/ucontext/mach when we need it.
 * This also makes us save stack space and time when copying
 * We might also want to add an additional field to propagate
 * the original context from the signal handler.
 */
typedef struct {
	gulong sc_ir;          // pc 
	gulong sc_sp;          // r1
	mgreg_t regs [MONO_SAVED_GREGS];
	double fregs [MONO_SAVED_FREGS];
} MonoContext;

typedef struct MonoCompileArch {
	int fp_conv_var_offset;
} MonoCompileArch;

/*
 * ILP32 uses a version of the ppc64 abi with sizeof(void*)==sizeof(long)==4.
 * To support this, code needs to follow the following conventions:
 * - for the size of a pointer use sizeof (gpointer)
 * - for the size of a register/stack slot use SIZEOF_REGISTER.
 * - for variables which contain values of registers, use mgreg_t.
 * - for loading/saving pointers/ints, use the normal ppc_load_reg/ppc_save_reg ()
 *   macros.
 * - for loading/saving register sized quantities, use the ppc_ldr/ppc_str 
 *   macros.
 * - make sure to not mix the two kinds of macros for the same memory location, 
 *   since ppc is big endian, so a 8 byte store followed by a 4 byte load will
 *   load the upper 32 bit of the value.
 * - use OP_LOADR_MEMBASE/OP_STORER_MEMBASE to load/store register sized
 *   quantities.
 */

#ifdef __mono_ppc64__
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS
#define MONO_ARCH_HAVE_ATOMIC_ADD 1
#define PPC_USES_FUNCTION_DESCRIPTOR

#ifndef __mono_ilp32__
#define MONO_ARCH_HAVE_TLS_GET 1
#define MONO_ARCH_ENABLE_MONITOR_IL_FASTPATH 1
#endif

#else /* must be __mono_ppc__ */

#if 0
/* enabling this for PPC32 causes hangs in the thread/delegate tests.
   So disable for now. */
#if defined(__linux__)
#define MONO_ARCH_ENABLE_MONITOR_IL_FASTPATH 1
#endif
#endif

#define MONO_ARCH_HAVE_TLS_GET 1
#define MONO_ARCH_EMULATE_FCONV_TO_I8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 1
#endif

#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_BIGMUL_INTRINS 1
#define MONO_ARCH_HAVE_ATOMIC_CAS 1

/* Parameters used by the register allocator */
#define MONO_ARCH_CALLEE_REGS ((0xff << ppc_r3) | (1 << ppc_r11) | (1 << ppc_r12))
#define MONO_ARCH_CALLEE_SAVED_REGS (0xfffff << ppc_r13) /* ppc_13 - ppc_31 */

#if defined(__APPLE__) || defined(__mono_ppc64__)
#define MONO_ARCH_CALLEE_FREGS (0x1fff << ppc_f1)
#else
#define MONO_ARCH_CALLEE_FREGS (0xff << ppc_f1)
#endif
#define MONO_ARCH_CALLEE_SAVED_FREGS (~(MONO_ARCH_CALLEE_FREGS | 1))

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#ifdef __mono_ppc64__
#define MONO_ARCH_INST_FIXED_REG(desc) (((desc) == 'a')? ppc_r3:\
					((desc) == 'g'? ppc_f1:-1))
#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)
#else
#define MONO_ARCH_INST_FIXED_REG(desc) (((desc) == 'a')? ppc_r3:\
					((desc) == 'l')? ppc_r4:\
					((desc) == 'g'? ppc_f1:-1))
#define MONO_ARCH_INST_IS_REGPAIR(desc) (desc == 'l')
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (desc == 'l' ? ppc_r3 : -1)
#endif

#define MONO_ARCH_INST_SREG2_MASK(ins) (0)

#define MONO_ARCH_INST_IS_FLOAT(desc) ((desc == 'f') || (desc == 'g'))

/* deal with some of the ABI differences here */
#ifdef __APPLE__
#define PPC_RET_ADDR_OFFSET 8
#define PPC_STACK_PARAM_OFFSET 24
#define PPC_MINIMAL_STACK_SIZE 24
#define PPC_MINIMAL_PARAM_AREA_SIZE 0
#define PPC_FIRST_ARG_REG ppc_r3
#define PPC_LAST_ARG_REG ppc_r10
#define PPC_FIRST_FPARG_REG ppc_f1
#define PPC_LAST_FPARG_REG ppc_f13
#define PPC_PASS_STRUCTS_BY_VALUE 1
#else
/* Linux */
#ifdef __mono_ppc64__
#define PPC_RET_ADDR_OFFSET 16
#define PPC_STACK_PARAM_OFFSET 48
#define PPC_MINIMAL_STACK_SIZE 48
#define PPC_MINIMAL_PARAM_AREA_SIZE 64
#define PPC_LAST_FPARG_REG ppc_f13
#define PPC_PASS_STRUCTS_BY_VALUE 1
#define PPC_SMALL_RET_STRUCT_IN_REG 0
#define PPC_THREAD_PTR_REG ppc_r13
#else
#define PPC_RET_ADDR_OFFSET 4
#define PPC_STACK_PARAM_OFFSET 8
#define PPC_MINIMAL_STACK_SIZE 8
#define PPC_MINIMAL_PARAM_AREA_SIZE 0
#define PPC_LAST_FPARG_REG ppc_f8
#define PPC_PASS_STRUCTS_BY_VALUE 0
#define PPC_SMALL_RET_STRUCT_IN_REG 1
#define PPC_THREAD_PTR_REG ppc_r2
#endif
#define PPC_FIRST_ARG_REG ppc_r3
#define PPC_LAST_ARG_REG ppc_r10
#define PPC_FIRST_FPARG_REG ppc_f1
#endif

#if defined(HAVE_WORKING_SIGALTSTACK) && !defined(__APPLE__)
#define MONO_ARCH_SIGSEGV_ON_ALTSTACK 1
#define MONO_ARCH_SIGNAL_STACK_SIZE (12 * 1024)
#endif /* HAVE_WORKING_SIGALTSTACK */

#define MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
#define MONO_ARCH_HAVE_IMT 1
#define MONO_ARCH_IMT_REG ppc_r12

#define MONO_ARCH_VTABLE_REG	ppc_r12
#define MONO_ARCH_RGCTX_REG	ppc_r12

#define MONO_ARCH_NO_IOV_CHECK 1
#define MONO_ARCH_HAVE_DECOMPOSE_OPTS 1
#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS 1

#define MONO_ARCH_HAVE_GENERALIZED_IMT_THUNK 1

#define MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_HAVE_XP_UNWIND 1

#define MONO_ARCH_GSHARED_SUPPORTED 1

#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_AOT_SUPPORTED 1
#define MONO_ARCH_NEED_GOT_VAR 1
#if !defined(MONO_CROSS_COMPILE) && !defined(TARGET_PS3)
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1
#endif
#define MONO_ARCH_THIS_AS_FIRST_ARG 1

#define PPC_NUM_REG_ARGS (PPC_LAST_ARG_REG-PPC_FIRST_ARG_REG+1)
#define PPC_NUM_REG_FPARGS (PPC_LAST_FPARG_REG-PPC_FIRST_FPARG_REG+1)

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (gulong)ip; } while (0);
/* FIXME: should be called SET_SP */
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (gulong)bp; } while (0);
#define MONO_CONTEXT_SET_SP(ctx,sp) do { (ctx)->sc_sp = (gulong)sp; } while (0);

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->regs [ppc_r31-13]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_sp))

#ifdef MONO_CROSS_COMPILE

typedef struct {
	unsigned long sp;
	unsigned long unused1;
	unsigned long lr;
} MonoPPCStackFrame;

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) g_assert_not_reached ()

#elif defined(__APPLE__)

typedef struct {
	unsigned long sp;
	unsigned long unused1;
	unsigned long lr;
} MonoPPCStackFrame;

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
		gpointer r1;					\
		__asm__ volatile("mr   %0,r1" : "=r" (r1));	\
		MONO_CONTEXT_SET_BP ((ctx), r1);		\
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
	} while (0)

#else

typedef struct {
	mgreg_t sp;
#ifdef __mono_ppc64__
	mgreg_t cr;
#endif
	mgreg_t lr;
} MonoPPCStackFrame;

#ifdef G_COMPILER_CODEWARRIOR
#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
		register gpointer r1_var;					\
		asm { mr r1_var, r1 };	\
		MONO_CONTEXT_SET_BP ((ctx), r1);		\
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
	} while (0)

#else
#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
		gpointer r1;					\
		__asm__ volatile("mr   %0,1" : "=r" (r1));	\
		MONO_CONTEXT_SET_BP ((ctx), r1);		\
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
	} while (0)

#endif
#endif

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { (lmf)->ebp = -1; } while (0)

typedef struct {
	gint8 reg;
	gint8 size;
	int vtsize;
	int offset;
} MonoPPCArgInfo;

#ifdef PPC_USES_FUNCTION_DESCRIPTOR
typedef struct {
	gpointer code;
	gpointer toc;
	gpointer env;
} MonoPPCFunctionDescriptor;

#define PPC_FTNPTR_SIZE			sizeof (MonoPPCFunctionDescriptor)
extern guint8* mono_ppc_create_pre_code_ftnptr (guint8 *code) MONO_INTERNAL;
#else
#define PPC_FTNPTR_SIZE			0
#define mono_ppc_create_pre_code_ftnptr(c)	c
#endif

#if defined(__linux__)
#define MONO_ARCH_USE_SIGACTION 1
#elif defined (__APPLE__) && defined (_STRUCT_MCONTEXT)
#define MONO_ARCH_USE_SIGACTION 1
#elif defined (__APPLE__) && !defined (_STRUCT_MCONTEXT)
#define MONO_ARCH_USE_SIGACTION 1
#elif defined(__NetBSD__)
#define MONO_ARCH_USE_SIGACTION 1
#elif defined(__FreeBSD__)
#define MONO_ARCH_USE_SIGACTION 1
#elif defined(MONO_CROSS_COMPILE)
	typedef MonoContext ucontext_t;
/*	typedef struct {
		int dummy;
	} ucontext_t;*/
	#define UCONTEXT_REG_Rn(ctx, n)
	#define UCONTEXT_REG_FPRn(ctx, n)
	#define UCONTEXT_REG_NIP(ctx)
	#define UCONTEXT_REG_LNK(ctx)

#else
/* For other operating systems, we pull the definition from an external file */
#include "mini-ppc-os.h"
#endif

void
mono_ppc_patch (guchar *code, const guchar *target) MONO_INTERNAL;

void
mono_ppc_throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, mgreg_t *int_regs, gdouble *fp_regs, gboolean rethrow) MONO_INTERNAL;

#ifdef __mono_ppc64__
#define MONO_PPC_32_64_CASE(c32,c64)	c64
extern void mono_ppc_emitted (guint8 *code, gint64 length, const char *format, ...);
#else
#define MONO_PPC_32_64_CASE(c32,c64)	c32
#endif

gboolean mono_ppc_is_direct_call_sequence (guint32 *code) MONO_INTERNAL;

void mono_ppc_patch_plt_entry (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr) MONO_INTERNAL;

void mono_ppc_set_func_into_sigctx (void *sigctx, void *func) MONO_INTERNAL;

#endif /* __MONO_MINI_PPC_H__ */
