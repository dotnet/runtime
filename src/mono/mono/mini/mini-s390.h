#ifndef __MONO_MINI_S390_H__
#define __MONO_MINI_S390_H__

#include <mono/arch/s390/s390-codegen.h>
#include <signal.h>

#define MONO_MAX_IREGS 16
#define MONO_MAX_FREGS 16

#define MONO_ARCH_FRAME_ALIGNMENT 8

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gulong      ebp;
	gulong      eip;
};

typedef struct ucontext MonoContext;

typedef struct MonoCompileArch {
} MonoCompileArch;

#define MONO_ARCH_EMULATE_FCONV_TO_I8 	1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 	1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 	1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_LMUL 		1

#define MONO_ARCH_USE_SIGACTION 	1

#define S390_STACK_ALIGNMENT		 8
#define S390_FIRST_ARG_REG 		s390_r2
#define S390_LAST_ARG_REG 		s390_r6
#define S390_FIRST_FPARG_REG 		s390_f0
#define S390_LAST_FPARG_REG 		s390_f2
#define S390_PASS_STRUCTS_BY_VALUE 	 1
#define S390_SMALL_RET_STRUCT_IN_REG	 1

#define S390_NUM_REG_ARGS (S390_LAST_ARG_REG-S390_FIRST_ARG_REG+1)
#define S390_NUM_REG_FPARGS (S390_LAST_FPARG_REG-S390_FIRST_FPARG_REG)

#define S390_OFFSET(b, t)	(guchar *) ((gint32) (b) - (gint32) (t))
#define S390_RELATIVE(b, t)     (guchar *) ((((gint32) (b) - (gint32) (t))) / 2)

#define CODEPTR(c, o) (o) = (short *) ((guint32) c - 2)
#define PTRSLOT(c, o) *(o) = (short) ((guint32) c - (guint32) (o) + 2)/2

#define S390_CC_EQ			8
#define S390_ALIGN(v, a)	(((a) > 0 ? (((v) + ((a) - 1)) & ~((a) - 1)) : (v)))

static void inline
s390_patch (guchar *code, gint32 target)
{
	gint32 *offset = (gint32 *) code;
	
	if (target != 00) {
		*offset = target;
	}
}

#endif /* __MONO_MINI_S390_H__ */  
