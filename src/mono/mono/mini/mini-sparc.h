#ifndef __MONO_MINI_SPARC_H__
#define __MONO_MINI_SPARC_H__

#include <mono/arch/sparc/sparc-codegen.h>

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_ARCH_FRAME_ALIGNMENT 8

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

#define MONO_ARCH_EMULATE_FCONV_TO_I8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1

gboolean mono_sparc_is_virtual_call (guint32 *code);

#endif /* __MONO_MINI_SPARC_H__ */  
