#ifndef __MONO_MINI_PPC_H__
#define __MONO_MINI_PPC_H__

#include <mono/arch/ppc/ppc-codegen.h>
#include <glib.h>

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_SAVED_GREGS 19
#define MONO_SAVED_FREGS 18

#define MONO_ARCH_FRAME_ALIGNMENT 4

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

void ppc_patch (guchar *code, guchar *target);

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gulong     ebp;
	gulong     eip;
	gulong     iregs [MONO_SAVED_GREGS]; /* 13..31 */
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
	gulong regs [MONO_SAVED_GREGS];
	double fregs [MONO_SAVED_FREGS];
} MonoContext;

typedef struct MonoCompileArch {
} MonoCompileArch;

#define MONO_ARCH_EMULATE_FCONV_TO_I8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_BIGMUL_INTRINS 1

/* deal with some of the ABI differences here */
#ifdef __APPLE__
#define PPC_RET_ADDR_OFFSET 8
#define PPC_STACK_ALIGNMENT 16
#define PPC_STACK_PARAM_OFFSET 24
#define PPC_MINIMAL_STACK_SIZE 24
#define PPC_FIRST_ARG_REG ppc_r3
#define PPC_LAST_ARG_REG ppc_r10
#define PPC_FIRST_FPARG_REG ppc_f1
#define PPC_LAST_FPARG_REG ppc_f13
#define PPC_PASS_STRUCTS_BY_VALUE 1
#else
/* Linux */
#define PPC_RET_ADDR_OFFSET 4
#define PPC_STACK_ALIGNMENT 16
#define PPC_STACK_PARAM_OFFSET 8
#define PPC_MINIMAL_STACK_SIZE 8
#define PPC_FIRST_ARG_REG ppc_r3
#define PPC_LAST_ARG_REG ppc_r10
#define PPC_FIRST_FPARG_REG ppc_f1
#define PPC_LAST_FPARG_REG ppc_f8
/* set the next to 0 once inssel-ppc.brg is updated */
#define PPC_PASS_STRUCTS_BY_VALUE 1
#define PPC_SMALL_RET_STRUCT_IN_REG 1

#endif

#define MONO_ARCH_USE_SIGACTION 1
#define MONO_ARCH_NEED_DIV_CHECK 1

#define PPC_NUM_REG_ARGS (PPC_LAST_ARG_REG-PPC_FIRST_ARG_REG+1)
#define PPC_NUM_REG_FPARGS (PPC_LAST_FPARG_REG-PPC_FIRST_FPARG_REG+1)

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (int)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (int)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sc_sp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sc_sp))

#ifdef __APPLE__

typedef struct {
	unsigned long sp;
	unsigned long unused1;
	unsigned long lr;
} MonoPPCStackFrame;

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
		MonoPPCStackFrame *sframe;	\
		__asm__ volatile("lwz   %0,0(r1)" : "=r" (sframe));	\
		MONO_CONTEXT_SET_BP ((ctx), sframe->sp);	\
		sframe = (MonoPPCStackFrame*)sframe->sp;	\
		MONO_CONTEXT_SET_IP ((ctx), sframe->lr);	\
	} while (0)

#else

typedef struct {
	unsigned long sp;
	unsigned long lr;
} MonoPPCStackFrame;

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,func) do {	\
		MonoPPCStackFrame *sframe;	\
		__asm__ volatile("lwz   %0,0(1)" : "=r" (sframe));	\
		MONO_CONTEXT_SET_BP ((ctx), sframe->sp);	\
		sframe = (MonoPPCStackFrame*)sframe->sp;	\
		MONO_CONTEXT_SET_IP ((ctx), sframe->lr);	\
	} while (0)

#endif

#define mono_find_jit_info mono_arch_find_jit_info
#define CUSTOM_STACK_WALK 1
#define CUSTOM_EXCEPTION_HANDLING 1

#endif /* __MONO_MINI_PPC_H__ */  
