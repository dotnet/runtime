#ifndef __MONO_MINI_SPARC_H__
#define __MONO_MINI_SPARC_H__

#include <mono/arch/sparc/sparc-codegen.h>

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_ARCH_FRAME_ALIGNMENT 8

#define MONO_ARCH_CODE_ALIGNMENT 32

#define MONO_ARCH_BASEREG sparc_fp
#define MONO_ARCH_RETREG1 sparc_i0

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	guint32     ip;
	guint32     sp;
	guint32     ebp;
};

typedef struct MonoCompileArch {
	guint32 lmf_offset;
	guint32 localloc_offset;
} MonoCompileArch;

#define MONO_ARCH_USE_SIGACTION 1

#define MONO_ARCH_EMULATE_FCONV_TO_I8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R4   1
#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1

#ifndef __GNUC__
/* assume Sun compiler if not GCC */
static void * __builtin_return_address(int depth)
{
	asm("ta      3");
	asm("tst     %i0");
	asm("be      retAddr_End");
	asm("mov     %fp, %l0");
	asm("retAddr_Start:");
	asm("sub     %i0, 1, %i0");
	asm("tst     %i0");
	asm("bne     retAddr_Start");
	asm("ld      [%l0+56], %l0");
	asm("retAddr_End:");
	asm("ld      [%l0+60], %i0");
}

static void * __builtin_frame_address(int depth)
{
	asm("ta      3");
	asm("tst     %i0");
	asm("be      frameAddr_End");
	asm("mov     %fp, %l0");
	asm("frameAddr_Start:");
	asm("sub     %i0, 1, %i0");
	asm("tst     %i0");
	asm("bne     frameAddr_Start");
	asm("ld      [%l0+56], %l0");
	asm("frameAddr_End:");
	asm("ld      [%l0+56], %i0");
}
#endif

gboolean mono_sparc_is_virtual_call (guint32 *code);

gpointer* mono_sparc_get_vcall_slot_addr (guint32 *code, guint32 *fp);

void mono_sparc_flushw (void);

gboolean mono_sparc_is_v9 (void);

#endif /* __MONO_MINI_SPARC_H__ */  
