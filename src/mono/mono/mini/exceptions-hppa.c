/*
 * exceptions-hppa.c: exception support for HPPA
 *
 * Copyright (c) 2007 Randolph Chung
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <ucontext.h>

#include <mono/arch/hppa/hppa-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-hppa.h"

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

#define restore_regs_from_context(ctx_reg,ip_reg) do {			\
		int reg, ofs;						\
		hppa_ldw (code, G_STRUCT_OFFSET (MonoContext, pc), \
			ctx_reg, ip_reg); 				\
		ofs = G_STRUCT_OFFSET (MonoContext, regs);		\
		for (reg = 0; reg < 32; ++reg) {			\
			if (HPPA_IS_SAVED_GREG (reg)) {			\
				hppa_ldw (code, ofs, ctx_reg, reg); 	\
				ofs += 4; 				\
			} 						\
		} 							\
		hppa_set (code, G_STRUCT_OFFSET (MonoContext, fregs), hppa_r1); \
		for (reg = 4; reg < 32; ++reg) {			\
			if (HPPA_IS_SAVED_FREG (reg)) {			\
				hppa_flddx (code, hppa_r1, ctx_reg, reg); \
				hppa_ldo (code, sizeof(double), hppa_r1, hppa_r1); \
			} 						\
		}							\
	} while (0)


/* HPPA bit ops */
static inline int
hppa_low_sign_extend (unsigned val, unsigned bits)
{
	return (int) ((val & 0x1 ? (-1 << (bits - 1)) : 0) | val >> 1);
}

static inline int
hppa_sign_extend (unsigned val, unsigned bits)
{
	return (int) (val >> (bits - 1) ? (-1 << bits) | val : val);
}

static inline int
hppa_get_field (unsigned word, int from, int to)
{
	return ((word) >> (31 - (to)) & ((1 << ((to) - (from) + 1)) - 1));
}

static inline int
hppa_extract_14 (unsigned word)
{
	return hppa_low_sign_extend (word & 0x3fff, 14);
}

static inline int
hppa_extract_21 (unsigned word)
{
	int val;

	word &= 0x1fffff;
	word <<= 11;
	val = hppa_get_field (word, 20, 20);
	val <<= 11;
	val |= hppa_get_field (word, 9, 19);
	val <<= 2;
	val |= hppa_get_field (word, 5, 6);
	val <<= 5;
	val |= hppa_get_field (word, 0, 4);
	val <<= 2;
	val |= hppa_get_field (word, 7, 8);
	return hppa_sign_extend (val, 21) << 11;
}

static inline int
hppa_is_branch(unsigned int insn)
{
	switch (insn >> 26)
	{
		case 0x20:
		case 0x21:
		case 0x22:
		case 0x23:
		case 0x27:
		case 0x28:
		case 0x29:
		case 0x2a:
		case 0x2b:
		case 0x2f:
		case 0x30:
		case 0x31:
		case 0x32:
		case 0x33:
		case 0x38:
		case 0x39:
		case 0x3a:
		case 0x3b:
			return 1;

		default:
			return 0;
	}
}

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 * called as restore_context(MonoContext *ctx)
 */
gpointer
mono_arch_get_restore_context (void)
{
	guint8 *code;
	static guint8 start [384];
	static int inited = 0;

	if (inited)
		return start;
	inited = 1;

	code = start;
	restore_regs_from_context (hppa_r26, hppa_r20);
	/* restore also the stack pointer */
	hppa_ldw (code, G_STRUCT_OFFSET (MonoContext, sp), hppa_r26, hppa_r30);
	/* jump to the saved IP */
	hppa_bv (code, hppa_r0, hppa_r20);
	hppa_nop (code);

	/* not reached */
	*(guint32 *)code = 0xdeadbeef;

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	return start;
}

/*
 * arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
gpointer
mono_arch_get_call_filter (void)
{
	static guint8 start [1024];
	static int inited = 0;
	guint8 *code;
	int pos, i;

	if (inited)
		return start;

	inited = 1;
	/* call_filter (MonoContext *ctx, unsigned long eip, gpointer exc) */
	code = start;

	/* Save the return pointer in its regular location */
	hppa_stw (code, hppa_r2, -20, hppa_r30);

	/* Save all the registers on the stack */
	pos = 0;
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i)) {
			hppa_stw (code, i, pos, hppa_sp);
			pos += sizeof(gulong);
		}
	}
	pos = ALIGN_TO (pos, sizeof(double));
	hppa_set (code, pos, hppa_r1);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_FREG (i)) {
			hppa_fstdx (code, i, hppa_r1, hppa_sp);
			hppa_ldo (code, sizeof(double), hppa_r1, hppa_r1);
			pos += sizeof(double);
		}
	}

	pos += 64; /* Leave space for the linkage area */
	pos = ALIGN_TO (pos, MONO_ARCH_FRAME_ALIGNMENT);

	hppa_ldo (code, pos, hppa_sp, hppa_sp);

	/* restore all the regs from ctx (in r26), but not the stack pointer */
	restore_regs_from_context (hppa_r26, hppa_r20);

	/* call handler at the saved IP (r25) */
	hppa_ble (code, 0, hppa_r25);
	hppa_copy (code, hppa_r31, hppa_r2);

	/* epilog */
	hppa_ldo (code, -pos, hppa_sp, hppa_sp);

	/* Restore registers */
	pos = 0;
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i)) {
			hppa_ldw (code, pos, hppa_sp, i);
			pos += sizeof(gulong);
		}
	}
	pos = ALIGN_TO (pos, sizeof(double));
	hppa_set (code, pos, hppa_r1);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_FREG (i)) {
			hppa_flddx (code, hppa_r1, hppa_sp, i);
			hppa_ldo (code, sizeof(double), hppa_r1, hppa_r1);
			pos += sizeof(double);
		}
	}

	hppa_ldw (code, -20, hppa_sp, hppa_r2);
	hppa_bv (code, hppa_r0, hppa_r2);
	hppa_nop (code);

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gulong *int_regs, gdouble *fp_regs, gboolean rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip &= ~3;
	eip -= 8;

	MONO_CONTEXT_SET_BP (&ctx, esp);
	MONO_CONTEXT_SET_IP (&ctx, eip);
	memcpy (&ctx.regs, int_regs, sizeof (gulong) * MONO_SAVED_GREGS);
	memcpy (&ctx.fregs, fp_regs, sizeof (double) * MONO_SAVED_FREGS);

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	mono_handle_exception (&ctx, exc, (gpointer)(eip + 8), FALSE);

	restore_context (&ctx);

	g_assert_not_reached ();
}

/**
 * arch_get_throw_exception_generic:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); or
 * void (*func) (char *exc_name);
 *
 */
static gpointer 
mono_arch_get_throw_exception_generic (guint8 *start, int size, int by_name, gboolean rethrow)
{
	guint8 *code;
	int pos, frpos, i;

	code = start;

	/* We are called with r26 = exception */
	/* Stash the call site IP in the "return pointer" slot */
	hppa_stw (code, hppa_r2, -20, hppa_sp);

	/* Non-standard prologue - we don't want to clobber r3 */
	hppa_copy (code, hppa_sp, hppa_r1);
	hppa_ldo (code, 512, hppa_sp, hppa_sp);

	/* Save all the registers on the stack */
	pos = 0;
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i)) {
			hppa_stw (code, i, pos, hppa_r1);
			pos += sizeof(gulong);
		}
	}
	pos = ALIGN_TO (pos, sizeof(double));
	frpos = pos;
	hppa_set (code, pos, hppa_r20);
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_FREG (i)) {
			hppa_fstdx (code, i, hppa_r20, hppa_r1);
			hppa_ldo (code, sizeof(double), hppa_r20, hppa_r20);
			pos += sizeof(double);
		}
	}

	/* Now that we have saved r4, we copy the stack pointer to it for
	 * use below - we want a callee save register in case we do the
	 * function call below
	 */
	hppa_copy (code, hppa_r1, hppa_r4);

	if (by_name) {
		/* mono_exception_from_name (MonoImage *image, 
		 * 			     const char *name_space,
		 * 			     const char *name)
		 */
		void *func = __canonicalize_funcptr_for_compare (mono_exception_from_name);
		hppa_copy (code, hppa_r26, hppa_r24);
		hppa_set (code, mono_defaults.corlib, hppa_r26);
		hppa_set (code, "System", hppa_r25);
		hppa_ldil (code, hppa_lsel (func), hppa_r1);
		hppa_ble (code, hppa_rsel (func), hppa_r1);
		hppa_copy (code, hppa_r31, hppa_r2);
		hppa_copy (code, hppa_r28, hppa_r26);
	}

	/* call throw_exception (exc, ip, sp, int_regs, fp_regs, rethrow) */
	/* exc is already in place in r26 */

	hppa_ldw (code, -20, hppa_r4, hppa_r25); /* ip */
	hppa_copy (code, hppa_r4, hppa_r24); /* sp */
	hppa_ldo (code, 0, hppa_r4, hppa_r23);
	hppa_ldo (code, frpos, hppa_r4, hppa_r22);
	hppa_stw (code, hppa_r22, -52, hppa_sp);
	hppa_ldo (code, rethrow, hppa_r0, hppa_r22);
	hppa_stw (code, hppa_r22, -56, hppa_sp);

	hppa_set (code, throw_exception, hppa_r1);
	hppa_depi (code, 0, 31, 2, hppa_r1);
	hppa_ldw (code, 0, hppa_r1, hppa_r1);
	hppa_bv (code, hppa_r0, hppa_r1);
	hppa_nop (code);

	/* not reached */
	*(guint32 *)code = 0x88c0ffee;

	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	return start;
}

/**
 * mono_arch_get_rethrow_exception:
 *
 * Returns a function pointer which can be used to rethrow 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
 */
gpointer
mono_arch_get_rethrow_exception (void)
{
	static guint8 start [450];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, TRUE);
	inited = 1;
	return start;
}
/**
 * arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, mono_get_exception_arithmetic ()); 
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception (void)
{
	static guint8 start [450];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, FALSE);
	inited = 1;
	return start;
}

/**
 * arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, "ArithmeticException"); 
 * x86_call_code (code, arch_get_throw_exception_by_name ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint8 start [450];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE, FALSE);
	inited = 1;
	return start;
}	

static int
hppa_get_size_of_frame (MonoJitInfo *ji, int *stored_fp)
{
	guint32 *insn = (guint32 *)((unsigned long)ji->code_start & ~3);
	guint32 *end = (guint32 *)((unsigned long)ji->code_start + ji->code_size);
	int saw_branch = 0;
	int stack_size = 0;

	/* Look for the instruction that adjusts the stack pointer */
	while (insn < end) {
		/* ldo X(sp), sp */
		if ((insn[0] & 0xffffc000) == 0x37de0000) {
			stack_size += hppa_extract_14 (insn [0]);
		}
		/* stwm X,D(sp) */
		else if ((insn[0] & 0xffe00000) == 0x6fc00000) {
			stack_size += hppa_extract_14 (insn [0]);
			if (stored_fp)
				*stored_fp = 1;
		}
		/* addil/ldo */
		else if (((insn[0] & 0xffe00000) == 0x28200000) &&
			 ((insn[1] & 0xffff0000) == 0x343e0000)) {
			 stack_size += hppa_extract_21 (insn [0]);
			 stack_size += hppa_extract_14 (insn [1]);
			 insn++;
		}

		insn++;
		if (saw_branch)
			break;
		saw_branch = hppa_is_branch (insn[0]);
	}

	if (stack_size == 0) {
		g_print ("No stack frame found for function at %p\n", ji->code_start);
	}

	return stack_size;
}

static void
hppa_analyze_frame (MonoJitInfo *ji, MonoContext *ctx, MonoContext *new_ctx)
{
	unsigned char *sp;
	int stack_size;
	int stored_fp = 0;

	stack_size = hppa_get_size_of_frame (ji, &stored_fp);

	sp = (unsigned char *)MONO_CONTEXT_GET_BP (ctx) - stack_size;
	MONO_CONTEXT_SET_BP (new_ctx, sp);
	MONO_CONTEXT_SET_IP (new_ctx, *(unsigned int *)(sp - 20));

	if (ji->method->save_lmf) {
		memcpy (&new_ctx->regs, (char *)sp + HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, regs), sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->fregs, (char *)sp + HPPA_STACK_LMF_OFFSET + G_STRUCT_OFFSET (MonoLMF, fregs), sizeof (double) * MONO_SAVED_FREGS);
	}
	else if (ji->used_regs) {
		char *pos = (char *)sp + HPPA_STACK_LMF_OFFSET;
		gulong val;
		int i, j = 0;

		for (i = 0; i <= 32; i++) {
			if ((1 << i) & ji->used_regs) {
				val = *(gulong *)pos;
				pos += sizeof (gulong);
				new_ctx->regs [j] = val;
			}
			if (HPPA_IS_SAVED_GREG (i))
				j++;
		}
	}
	if (stored_fp) {
		gulong val;
		val = *(gulong *)sp;
		new_ctx->regs [0] = val;
	}
}

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji,
			 MonoContext *ctx, MonoContext *new_ctx, MonoLMF **lmf, gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mini_jit_info_table_find (domain, ip, NULL);

	if (managed)
		*managed = FALSE;

	if (ji != NULL) {
		gint32 address;

		*new_ctx = *ctx;

		address = (char *)ip - (char *)ji->code_start;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (*lmf && (*lmf)->ebp != 0xffffffff && (MONO_CONTEXT_GET_BP (ctx) <= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		hppa_analyze_frame (ji, ctx, new_ctx);
		//printf("Managed frame: rewound (ip,sp)=(%x [%s],%x) to (%x,%x)\n", ctx->pc, mono_method_full_name (ji->method, FALSE), ctx->sp, new_ctx->pc, new_ctx->sp);

		return ji;
	} else if (*lmf) {
		
		*new_ctx = *ctx;

		if (!(*lmf)->method)
			return (gpointer)-1;

		if ((ji = mini_jit_info_table_find (domain, (gpointer)(*lmf)->eip, NULL))) {
		} else {
			memset (res, 0, MONO_SIZEOF_JIT_INFO);
			res->method = (*lmf)->method;
		}

		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		memcpy (&new_ctx->regs, (*lmf)->regs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->fregs, (*lmf)->regs, sizeof (double) * MONO_SAVED_FREGS);
		*lmf = (*lmf)->previous_lmf;
		//printf("Unmanaged frame: rewound to (ip,sp)=(%x [%s],%x)\n", new_ctx->pc, mono_method_full_name (ji ? ji->method : res->method, FALSE), new_ctx->sp);

		return ji ? ji : res;
	}

	return NULL;
}

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	struct ucontext *uc = sigctx;
	MonoContext mctx;
	gboolean result;
	int i, grs, frs;

	mctx.pc = uc->uc_mcontext.sc_iaoq [0];
	mctx.sp = uc->uc_mcontext.sc_gr [30];

	grs = 0; frs = 0;
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i))
			mctx.regs[grs++] = uc->uc_mcontext.sc_gr [i];
		if (HPPA_IS_SAVED_FREG (i))
			mctx.fregs[frs++] = uc->uc_mcontext.sc_fr [i];
	}
	g_assert (grs == MONO_SAVED_GREGS && frs == MONO_SAVED_FREGS);

	result = mono_handle_exception (&mctx, obj, (gpointer)mctx.pc, test_only);

	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause 
	 */
	uc->uc_mcontext.sc_iaoq [0] = mctx.pc;
	uc->uc_mcontext.sc_iaoq [1] = mctx.pc + 4;
	uc->uc_mcontext.sc_gr [30] = mctx.sp;

	grs = 0; frs = 0;
	for (i = 0; i < 32; i++) {
		if (HPPA_IS_SAVED_GREG (i))
			uc->uc_mcontext.sc_gr [i] = mctx.regs [grs++];
		if (HPPA_IS_SAVED_FREG (i))
			uc->uc_mcontext.sc_fr [i] = mctx.fregs [frs++];
	}

	return result;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	struct ucontext *uc = sigctx;
	return (gpointer)uc->uc_mcontext.sc_iaoq [0];
}
