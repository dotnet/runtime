#ifndef __MONO_MINI_SPARC_H__
#define __MONO_MINI_SPARC_H__

#include <mono/arch/sparc/sparc-codegen.h>

#include <glib.h>

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_ARCH_FRAME_ALIGNMENT (sizeof (gpointer) * 2)

#define MONO_ARCH_CODE_ALIGNMENT 32

#define MONO_ARCH_BASEREG sparc_fp
#define MONO_ARCH_RETREG1 sparc_i0

#ifdef SPARCV9
#define MONO_SPARC_STACK_BIAS 2047
#else
#define MONO_SPARC_STACK_BIAS 0
#endif

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gpointer    ip;
	gpointer    sp;
	gpointer    ebp;
};

typedef struct MonoContext {
	guint8 *ip;
	gpointer *sp;
	gpointer *fp;
} MonoContext;

typedef struct MonoCompileArch {
	gint32 lmf_offset;
	gint32 localloc_offset;
} MonoCompileArch;

#define MONO_CONTEXT_SET_IP(ctx,eip) do { (ctx)->ip = (gpointer)(eip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,ebp) do { (ctx)->fp = (gpointer*)(ebp); } while (0); 
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->sp = (gpointer*)(esp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->ip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->fp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sp))

#define MONO_ARCH_USE_SIGACTION 1

//#define MONO_ARCH_SIGSEGV_ON_ALTSTACK

#define MONO_ARCH_EMULATE_FCONV_TO_I8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R4   1
#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_NEED_DIV_CHECK 1

#ifdef SPARCV9
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
#endif

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
#if SPARCV9
	asm("ldx     [%l0+2159], %l0");
	asm("retAddr_End:");
	asm("ldx     [%l0+2167], %i0");
#else
	asm("ld      [%l0+56], %l0");
	asm("retAddr_End:");
	asm("ld      [%l0+60], %i0");
#endif
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
#if SPARCV9
	asm("ldx     [%l0+2159], %l0");
	asm("frameAddr_End:");
	asm("ldx     [%l0+2159], %i0");
#else
	asm("ld      [%l0+56], %l0");
	asm("frameAddr_End:");
	asm("ld      [%l0+56], %i0");
#endif
}
#endif

gboolean mono_sparc_is_virtual_call (guint32 *code);

gpointer* mono_sparc_get_vcall_slot_addr (guint32 *code, gpointer *fp);

void mono_sparc_flushw (void);

gboolean mono_sparc_is_v9 (void);

gboolean mono_sparc_is_sparc64 (void);

struct MonoCompile;

guint32* mono_sparc_emit_save_lmf (guint32* code, guint32 lmf_offset);

guint32* mono_sparc_emit_restore_lmf (guint32 *code, guint32 lmf_offset);

#endif /* __MONO_MINI_SPARC_H__ */  
