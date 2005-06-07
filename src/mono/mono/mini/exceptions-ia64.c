/*
 * exceptions-ia64.c: exception support for IA64
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 */

/*
 * We implement exception handling with the help of the libuwind library:
 * 
 * http://www.hpl.hp.com/research/linux/libunwind/
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <sys/ucontext.h>

#include <mono/arch/ia64/ia64-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-ia64.h"

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

#define NOT_IMPLEMENTED g_assert_not_reached ()

#define GP_SCRATCH_REG 31
#define GP_SCRATCH_REG2 30

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context (void)
{
	static guint8 *start = NULL;
	static gboolean inited = FALSE;
	Ia64CodegenState code;

	if (inited)
		return start;

	/* restore_contect (MonoContext *ctx) */

	start = mono_global_codeman_reserve (256);

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

/*
 * mono_arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 */
gpointer
mono_arch_get_call_filter (void)
{
	static guint8 *start;
	static gboolean inited = FALSE;
	int i;
	guint32 pos;
	Ia64CodegenState code;

	if (inited)
		return start;

	if (inited)
		return start;

	start = mono_global_codeman_reserve (256);

	/* call_filter (MonoContext *ctx, unsigned long eip) */

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

static void
throw_exception (MonoObject *exc, guint64 rip, guint64 rethrow)
{
	static void (*restore_context) (MonoContext *);
	unw_context_t unw_ctx;
	MonoContext ctx;
	int res;
	unw_word_t ip, sp;

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}

	res = unw_getcontext (&unw_ctx);
	g_assert (res == 0);
	res = unw_init_local (&ctx.cursor, &unw_ctx);
	g_assert (res == 0);

	res = unw_get_reg (&ctx.cursor, UNW_IA64_IP, &ip);
	g_assert (res == 0);

	res = unw_get_reg (&ctx.cursor, UNW_IA64_SP, &sp);
	g_assert (res == 0);

	/* FIXME: bp */

	MONO_CONTEXT_SET_IP (&ctx, ip);
	MONO_CONTEXT_SET_SP (&ctx, sp);

	mono_handle_exception (&ctx, exc, (gpointer)(rip + 1), FALSE);
	restore_context (&ctx);

	g_assert_not_reached ();
}

static gpointer
mono_create_ftnptr (gpointer ptr)
{
	gpointer *desc = g_malloc (2 * sizeof (gpointer));
	desc [0] = ptr;
	desc [1] = NULL;

	return desc;
}

static gpointer
get_throw_trampoline (gboolean rethrow)
{
	guint8* start;
	Ia64CodegenState code;
	gpointer ptr = throw_exception;
	int i, in0, local0, out0;
	unw_dyn_info_t *di;
	unw_dyn_region_info_t *r_pro;

	start = mono_global_codeman_reserve (256);

	in0 = 32;
	local0 = in0 + 1;
	out0 = local0 + 2;

	ia64_codegen_init (code, start);
	ia64_alloc (code, local0 + 0, local0 - in0, out0 - local0, 3, 0);
	ia64_mov_from_br (code, local0 + 1, IA64_B0);	

	/* FIXME: This depends on the current instruction emitter */

	r_pro = g_malloc0 (_U_dyn_region_info_size (2));
	r_pro->op_count = 2;
	r_pro->insn_count = 6;
	i = 0;
	_U_dyn_op_save_reg (&r_pro->op[i++], _U_QP_TRUE, /* when=*/ 2,
						/* reg=*/ UNW_IA64_AR_PFS, /* dst=*/ UNW_IA64_GR + local0 + 0);
	_U_dyn_op_save_reg (&r_pro->op[i++], _U_QP_TRUE, /* when=*/ 5,
						/* reg=*/ UNW_IA64_RP, /* dst=*/ UNW_IA64_GR + local0 + 1);
	g_assert ((unsigned) i <= r_pro->op_count);	

	/* Set args */
	ia64_mov (code, out0 + 0, in0 + 0);
	ia64_mov_from_br (code, out0 + 1, IA64_B0);
	ia64_adds_imm (code, out0 + 2, rethrow, IA64_R0);

	/* Call throw_exception */
	ia64_movl (code, GP_SCRATCH_REG, ptr);
	ia64_ld8_inc_imm (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, 8);
	ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2);
	ia64_ld8 (code, IA64_GP, GP_SCRATCH_REG);
	ia64_br_call_reg (code, IA64_B0, IA64_B6);

	/* Not reached */
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	di = g_malloc0 (sizeof (unw_dyn_info_t));
	di->start_ip = (unw_word_t) start;
	di->end_ip = (unw_word_t) code.buf;
	di->gp = 0;
	di->format = UNW_INFO_FORMAT_DYNAMIC;
	di->u.pi.name_ptr = (unw_word_t)"throw_trampoline";
	di->u.pi.regions = r_pro;

	_U_dyn_register (di);

	return mono_create_ftnptr (start);
}

/**
 * mono_arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
 */
gpointer 
mono_arch_get_throw_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;

	if (inited)
		return start;

	start = get_throw_trampoline (FALSE);

	inited = TRUE;

	return start;
}

gpointer 
mono_arch_get_rethrow_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;

	if (inited)
		return start;

	start = get_throw_trampoline (TRUE);

	inited = TRUE;

	return start;
}

gpointer 
mono_arch_get_throw_exception_by_name (void)
{	
	static gboolean inited = FALSE;	
	guint8* start;
	Ia64CodegenState code;

	start = mono_global_codeman_reserve (64);

	/* Not used on ia64 */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
}

/**
 * mono_arch_get_throw_corlib_exception:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * Here, offset is the offset which needs to be substracted from the caller IP 
 * to get the IP of the throw. Passing the offset has the advantage that it 
 * needs no relocations in the caller.
 */
gpointer 
mono_arch_get_throw_corlib_exception (void)
{
	static guint8* start;
	static gboolean inited = FALSE;
	guint64 throw_ex;
	Ia64CodegenState code;

	if (inited)
		return start;

	start = mono_global_codeman_reserve (64);

	/* FIXME: */
	ia64_codegen_init (code, start);
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - start) <= 256);

	mono_arch_flush_icache (start, code.buf - start);

	return start;
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
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	int i, err;
	unw_word_t ip, sp;

	*new_ctx = *ctx;

	while (TRUE) {
		ip = MONO_CONTEXT_GET_IP (new_ctx);

		/* Avoid costly table lookup during stack overflow */
		if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
			ji = prev_ji;
		else
			ji = mono_jit_info_table_find (domain, ip);

		if (managed)
			*managed = FALSE;

		if (ji != NULL) {
			int offset;

			if (managed)
				if (!ji->method->wrapper_type)
					*managed = TRUE;

			/*
			 * Some managed methods like pinvoke wrappers might have save_lmf set.
			 * In this case, register save/restore code is not generated by the 
			 * JIT, so we have to restore callee saved registers from the lmf.
			 */
			if (ji->method->save_lmf) {
				NOT_IMPLEMENTED;
			}
			else {
			}

			if (*lmf && (MONO_CONTEXT_GET_BP (new_ctx) >= (gpointer)(*lmf)->ebp)) {
				/* remove any unused lmf */
				*lmf = (*lmf)->previous_lmf;
			}

			err = unw_step (&new_ctx->cursor);
			g_assert (err >= 0);

			err = unw_get_reg (&new_ctx->cursor, UNW_IA64_IP, &ip);
			g_assert (err == 0);

			err = unw_get_reg (&new_ctx->cursor, UNW_IA64_SP, &sp);
			g_assert (err == 0);

			/* FIXME: bp */

			MONO_CONTEXT_SET_IP (new_ctx, ip);
			MONO_CONTEXT_SET_SP (new_ctx, sp);

			return ji;
		}

		/* This is an unmanaged frame, so just unwind through it */
		err = unw_step (&new_ctx->cursor);
		g_assert (err >= 0);

		{
			char name[256];
			unw_word_t off;

			unw_get_proc_name (&new_ctx->cursor, name, 256, &off);
			printf ("F: %s\n", name);
		}

		if (err == 0)
			return NULL;

		err = unw_get_reg (&new_ctx->cursor, UNW_IA64_IP, &ip);
		g_assert (err == 0);

		err = unw_get_reg (&new_ctx->cursor, UNW_IA64_SP, &sp);
		g_assert (err == 0);

		/* FIXME: bp */

		MONO_CONTEXT_SET_IP (new_ctx, ip);
		MONO_CONTEXT_SET_SP (new_ctx, sp);
	}

	return NULL;
}

/**
 * mono_arch_handle_exception:
 *
 * @ctx: saved processor state
 * @obj: the exception object
 */
gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	ucontext_t *ctx = (ucontext_t*)sigctx;
	MonoContext mctx;

	NOT_IMPLEMENTED;
	return FALSE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	NOT_IMPLEMENTED;
	return NULL;
}
