#ifndef __MONO_MINI_PPC_H__
#define __MONO_MINI_PPC_H__

#include <mono/arch/x86/x86-codegen.h>
#include <mono/arch/ppc/ppc-codegen.h>

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_ARCH_FRAME_ALIGNMENT 4

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

#define MONO_ARCH_BASEREG X86_EBP
#define MONO_ARCH_RETREG1 ppc_r3
#define MONO_ARCH_RETREG2 ppc_r4

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gulong     ebp;
	gulong     eip;
	gulong     iregs [19]; /* 13..31 */
	gdouble    fregs [20]; /* 14..31 */
};

#define MONO_ARCH_EMULATE_FCONV_TO_I8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1

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

#define PPC_NUM_REG_ARGS (PPC_LAST_ARG_REG-PPC_FIRST_ARG_REG+1)
#define PPC_NUM_REG_FPARGS (PPC_LAST_FPARG_REG-PPC_FIRST_FPARG_REG+1)

#endif /* __MONO_MINI_PPC_H__ */  
