#ifndef __MONO_MINI_SPARC_H__
#define __MONO_MINI_SPARC_H__

#include <mono/arch/sparc/sparc-codegen.h>

/* Check this for Sparc.  I think it is right. */
#define MONO_ARCH_FRAME_ALIGNMENT 4

/* Also check this. */
#define MONO_ARCH_CODE_ALIGNMENT 32

/* BASEREG = Frame pointer
 * RETREG? = Return register (but is it for caller or callee?)
 */
#define MONO_ARCH_BASEREG sparc_fp
#define MONO_ARCH_RETREG1 sparc_i0

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	guint32     ebp;
	guint32     eip;
};

#endif /* __MONO_MINI_SPARC_H__ */  
