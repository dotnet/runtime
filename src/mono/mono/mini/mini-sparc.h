/**
 * \file
 */

#ifndef __MONO_MINI_SPARC_H__
#define __MONO_MINI_SPARC_H__

#include <mono/arch/sparc/sparc-codegen.h>
#include <mono/utils/mono-context.h>

#include <glib.h>

#define MONO_ARCH_CPU_SPEC sparc_desc

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

/* Parameters used by the register allocator */

/* 
 * Use %o0..%o5 as local registers, plus %l7 since we need an extra register for
 * holding the sreg1 in call instructions.
 */
#define MONO_ARCH_CALLEE_REGS ((1 << sparc_o0) | (1 << sparc_o1) | (1 << sparc_o2) | (1 << sparc_o3) | (1 << sparc_o4) | (1 << sparc_o5) | (1 << sparc_l7))

#define MONO_ARCH_CALLEE_SAVED_REGS ((~MONO_ARCH_CALLEE_REGS) & ~(1 << sparc_g1))

#ifdef SPARCV9
/* Use %d34..%d62 as the double precision floating point local registers */
/* %d32 has the same encoding as %f1, so %d36%d38 == 0b1010 == 0xa */
#define MONO_ARCH_CALLEE_FREGS (0xaaaaaaa8)
#else
/* Use %f2..%f30 as the double precision floating point local registers */
#define MONO_ARCH_CALLEE_FREGS (0x55555554)
#endif

#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define MONO_ARCH_USE_FPSTACK FALSE
#ifdef SPARCV9
#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'o') ? sparc_o0 : -1)
#else
#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'o') ? sparc_o0 : ((desc == 'l') ? sparc_o1 : -1))
#endif
#define MONO_ARCH_INST_SREG2_MASK(ins) (0)

#ifdef SPARCV9
#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)
#else
#define MONO_ARCH_INST_IS_REGPAIR(desc) ((desc == 'l') || (desc == 'L'))
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (((desc == 'l') ? sparc_o0 : (desc == 'L' ? (hreg1 + 1) : -1)))
#endif

#if TARGET_SIZEOF_VOID_P == 8
#define MONO_ARCH_FRAME_ALIGNMENT 16
#else
#define MONO_ARCH_FRAME_ALIGNMENT 8
#endif

#define MONO_ARCH_CODE_ALIGNMENT 32

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

typedef struct MonoCompileArch {
	gint32 lmf_offset;
	gint32 localloc_offset;
	void *float_spill_slot;
} MonoCompileArch;

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
		MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
		MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0));	\
		MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0));	\
	} while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { (lmf)->ebp = -1; } while (0)

#define MONO_ARCH_USE_SIGACTION 1

#ifdef HAVE_WORKING_SIGALTSTACK
/*#define MONO_ARCH_SIGSEGV_ON_ALTSTACK*/
#endif

#define MONO_ARCH_EMULATE_FCONV_TO_U8   1
#define MONO_ARCH_EMULATE_FCONV_TO_I8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R4   1
#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_IMT_REG sparc_g1
#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS 1
#define MONO_ARCH_HAVE_TLS_INIT 1

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 0 // FIXME?

void mono_arch_tls_init (void);

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

void mono_sparc_flushw (void);

gboolean mono_sparc_is_v9 (void);

gboolean mono_sparc_is_sparc64 (void);

struct MonoCompile;

guint32* mono_sparc_emit_save_lmf (guint32* code, guint32 lmf_offset);

guint32* mono_sparc_emit_restore_lmf (guint32 *code, guint32 lmf_offset);

#endif /* __MONO_MINI_SPARC_H__ */  
