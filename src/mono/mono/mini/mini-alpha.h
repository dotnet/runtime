#ifndef __MONO_MINI_ALPHA_H__
#define __MONO_MINI_ALPHA_H__

#include <glib.h>

#include <mono/arch/alpha/alpha-codegen.h>

#define MONO_ARCH_CPU_SPEC alpha_desc

/* Parameters used by the register allocator */

/* Max number of integer registers (all int regs). Required definition */
#define MONO_MAX_IREGS 31

/* Max number of float registers (all float regs). Required definition */
#define MONO_MAX_FREGS 32

typedef enum {GT_NONE, GT_INT, GT_LONG,
	      GT_PTR, GT_FLOAT, GT_DOUBLE, GT_LD_GTADDR} AlphaGotType;

typedef struct
{
  union
  {
    int i;
    long l;
    void *p;
    float f;
    double d;
  } data;
} AlphaGotData;

typedef struct AlphaGotEntry
{
  struct AlphaGotEntry *next;

  AlphaGotType type;
  AlphaGotData value;

  gpointer  patch_info;
  gpointer  got_patch_info;
} AlphaGotEntry;

typedef struct MonoCompileArch
{
  gint32 lmf_offset;
  gint32 localloc_offset;
  gint32 reg_save_area_offset;
  gint32 args_save_area_offset;
  gint32 stack_size;        // Allocated stack size in bytes
  gint32 params_stack_size;  // Stack size reserved for call params by this compile method

  gpointer    got_data;
  glong       bwx;
} MonoCompileArch;

typedef struct ucontext MonoContext;

struct MonoLMF
{
  gpointer    previous_lmf;
  gpointer    lmf_addr;
  MonoMethod *method;
  guint64     ebp;          // FP ? caller FP
  guint64     eip;          // RA ? or caller PC
  guint64     rsp;          // SP ? caller SP
  guint64     rgp;          // GP
  guint64     r14;
  guint64     r13;
  guint64     r12;
};

#define MONO_ARCH_FRAME_ALIGNMENT 8
#define MONO_ARCH_CODE_ALIGNMENT 8

// Regs available for allocation in compile unit
// For Alpha: r1-r14, r22-r25
// 1111 1111 1111 1111 1111 1111 1111 1111
//  098 7654 3210 9876 5432 1098 7654 3210
// RRRR RRLL LLAA AAAA RLLL LLLL LLLL LLLL - No global regs
// RRRR RRLL LLAA AAAA RGGG LLLL LLLL LLLL - 3 global regs
//#define MONO_ARCH_CALLEE_REGS	((regmask_t)0x03C07FFF)
#define MONO_ARCH_CALLEE_REGS ((regmask_t)0x03C00FFF)
#define MONO_ARCH_CALLEE_FREGS	((regmask_t)0x03C07FFF)

// These are the regs that are considered global
// and should not be used by JIT reg allocator 
// (should be saved in compile unit). The JIT could use them
// in regalloc if they assigned as REGVARS (REGOFFSET to keep
// vars on stack
// the stack space will be reserved for them if they got used
// For Alpha - r9-r14. Actually later we could use some of the
// upper "t" regs, since local reg allocator doesn't like registers
// very much, so we could safely keep vars in them
//#define MONO_ARCH_CALLEE_SAVED_REGS ((regmask_t)0x3C00FE00)
#define MONO_ARCH_CALLEE_SAVED_REGS ((regmask_t)0x3C00F000)
#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define ALPHA_IS_CALLEE_SAVED_REG(reg) (MONO_ARCH_CALLEE_SAVED_REGS & (1 << (reg)))

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->uc_mcontext.sc_pc))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->uc_mcontext.sc_regs[15]))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->uc_mcontext.sc_regs[30]))

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->uc_mcontext.sc_pc = (long)(ip); } while (0);
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->uc_mcontext.sc_regs[15] = (long)(bp); } while (0);
#define MONO_CONTEXT_SET_SP(ctx,esp) do { (ctx)->uc_mcontext.sc_regs[30] = (long)(esp); } while (0);

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {        \
   mono_arch_flush_register_windows ();    \
   MONO_CONTEXT_SET_IP ((ctx), (start_func));      \
   MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0)); \
   MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0)); \
} while (0)

#define MONO_ARCH_USE_SIGACTION 1

//#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'r') ? IA64_R8 : ((desc == 'g') ? 8 : -1))
//#define MONO_ARCH_INST_IS_FLOAT(desc) ((desc == 'f') || (desc == 'g'))
#define MONO_ARCH_INST_SREG2_MASK(ins) (0)
#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)

// This define is called to get specific dest register as defined
// by md file (letter after "dest"). Overwise return -1
//#define MONO_ARCH_INST_FIXED_REG(desc)	(-1)
#define MONO_ARCH_INST_FIXED_REG(desc)  ((desc == 'o') ? alpha_at : ( (desc == 'a') ? alpha_r0 : -1) )

#if 0

/* r8..r11, r14..r29 */
#define MONO_ARCH_CALLEE_REGS ((regmask_t)(0x700UL) | (regmask_t)(0x3fffc000UL))

/* f6..f15, f33..f127 */
/* FIXME: Use the upper 64 bits as well */
#define MONO_ARCH_CALLEE_FREGS ((regmask_t)(0xfffffffe00000000UL) | ((regmask_t)(0x3ffUL) << 6))

#define MONO_ARCH_CALLEE_SAVED_REGS ~(MONO_ARCH_CALLEE_REGS)

#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'r') ? IA64_R8 : ((desc == 'g') ? 8 : -1))
#define MONO_ARCH_INST_IS_FLOAT(desc) ((desc == 'f') || (desc == 'g'))
#define MONO_ARCH_INST_SREG2_MASK(ins) (0)
#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)

#define MONO_ARCH_IS_GLOBAL_IREG(reg) (is_hard_ireg (reg) && ((reg) >= cfg->arch.reg_local0) && ((reg) < cfg->arch.reg_out0))

#define MONO_ARCH_FRAME_ALIGNMENT 16

#define MONO_ARCH_CODE_ALIGNMENT 16

#define MONO_ARCH_RETREG1 IA64_R8
#define MONO_ARCH_FRETREG1 8

#define MONO_ARCH_SIGNAL_STACK_SIZE SIGSTKSZ

struct MonoLMF
{
	guint64    ebp;
};

typedef struct MonoContext {
	unw_cursor_t cursor;
} MonoContext;

typedef struct MonoCompileArch {
	gint32 stack_alloc_size;
	gint32 lmf_offset;
	gint32 localloc_offset;
	gint32 n_out_regs;
	gint32 reg_in0;
	gint32 reg_local0;
	gint32 reg_out0;
	gint32 reg_saved_ar_pfs;
	gint32 reg_saved_b0;
	gint32 reg_saved_sp;
	gint32 reg_fp;
	gint32 reg_saved_return_val;
	guint32 prolog_end_offset, epilog_begin_offset, epilog_end_offset;
	void *ret_var_addr_local;
	unw_dyn_region_info_t *r_pro, *r_epilog;
	void *last_bb;
	Ia64CodegenState code;
	gboolean omit_fp;
	GHashTable *branch_targets;
} MonoCompileArch;

static inline unw_word_t
mono_ia64_context_get_ip (MonoContext *ctx)
{
	unw_word_t ip;
	int err;

	err = unw_get_reg (&ctx->cursor, UNW_IA64_IP, &ip);
	g_assert (err == 0);

	/* Subtrack 1 so ip points into the actual instruction */
	return ip - 1;
}

static inline unw_word_t
mono_ia64_context_get_sp (MonoContext *ctx)
{
	unw_word_t sp;
	int err;

	err = unw_get_reg (&ctx->cursor, UNW_IA64_SP, &sp);
	g_assert (err == 0);

	return sp;
}

static inline unw_word_t
mono_ia64_context_get_fp (MonoContext *ctx)
{
	unw_cursor_t new_cursor;
	unw_word_t fp;
	int err;

	{
		unw_word_t ip, sp;

		err = unw_get_reg (&ctx->cursor, UNW_IA64_SP, &sp);
		g_assert (err == 0);

		err = unw_get_reg (&ctx->cursor, UNW_IA64_IP, &ip);
		g_assert (err == 0);
	}

	/* fp is the SP of the parent frame */
	new_cursor = ctx->cursor;

	err = unw_step (&new_cursor);
	g_assert (err >= 0);

	err = unw_get_reg (&new_cursor, UNW_IA64_SP, &fp);
	g_assert (err == 0);

	return fp;
}

#define MONO_CONTEXT_SET_IP(ctx,eip) do { int err = unw_set_reg (&(ctx)->cursor, UNW_IA64_IP, (unw_word_t)(eip)); g_assert (err == 0); } while (0)
#define MONO_CONTEXT_SET_BP(ctx,ebp) do { } while (0)
#define MONO_CONTEXT_SET_SP(ctx,esp) do { int err = unw_set_reg (&(ctx)->cursor, UNW_IA64_SP, (unw_word_t)(esp)); g_assert (err == 0); } while (0)

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)(mono_ia64_context_get_ip ((ctx))))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)(mono_ia64_context_get_fp ((ctx))))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)(mono_ia64_context_get_sp ((ctx))))

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
    MONO_INIT_CONTEXT_FROM_CURRENT (ctx); \
} while (0)

#define MONO_INIT_CONTEXT_FROM_CURRENT(ctx) do { \
	int res; \
	res = unw_getcontext (&unw_ctx); \
	g_assert (res == 0); \
	res = unw_init_local (&(ctx)->cursor, &unw_ctx); \
	g_assert (res == 0); \
} while (0)

#define MONO_ARCH_CONTEXT_DEF unw_context_t unw_ctx;

#define MONO_ARCH_USE_SIGACTION 1

#ifdef HAVE_WORKING_SIGALTSTACK
/*#define MONO_ARCH_SIGSEGV_ON_ALTSTACK*/
#endif

unw_dyn_region_info_t* mono_ia64_create_unwind_region (Ia64CodegenState *code);

#define MONO_ARCH_NO_EMULATE_MUL_IMM 1

#define MONO_ARCH_HAVE_IS_INT_OVERFLOW 1

#define MONO_ARCH_HAVE_INVALIDATE_METHOD 1
#define MONO_ARCH_HAVE_SAVE_UNWIND_INFO 1

#endif

#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS 1

#define MONO_ARCH_EMULATE_CONV_R8_UN     1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
//#define MONO_ARCH_EMULATE_LCONV_TO_R8    1
#define MONO_ARCH_EMULATE_FREM           1
#define MONO_ARCH_EMULATE_MUL_DIV        1
#define MONO_ARCH_EMULATE_LONG_MUL_OPTS  1
#define MONO_ARCH_NEED_DIV_CHECK         1


#define MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE 1

typedef struct {
    guint8 *address;
    guint8 *saved_byte;
} MonoBreakpointInfo;

extern MonoBreakpointInfo  mono_breakpoint_info[MONO_BREAKPOINT_ARRAY_SIZE];
#endif /* __MONO_MINI_ALPHA_H__ */  
