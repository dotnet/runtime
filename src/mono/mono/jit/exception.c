/*
 * exception.c: exception support
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>

#include <mono/arch/x86/x86-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>

#include "jit.h"
#include "codegen.h"
#include "debug.h"

#ifdef __FreeBSD__
# define SC_EAX sc_eax
# define SC_EBX sc_ebx
# define SC_ECX sc_ecx
# define SC_EDX sc_edx
# define SC_EBP sc_ebp
# define SC_EIP sc_eip
# define SC_ESP sc_esp
# define SC_EDI sc_edi
# define SC_ESI sc_esi
#else
# define SC_EAX eax
# define SC_EBX ebx
# define SC_ECX ecx
# define SC_EDX edx
# define SC_EBP ebp
# define SC_EIP eip
# define SC_ESP esp
# define SC_EDI edi
# define SC_ESI esi
#endif

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
static gpointer
arch_get_restore_context (void)
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	/* restore_contect (struct sigcontext *ctx) */
	/* we do not restore X86_EAX, X86_EDX */

	start = code = g_malloc (1024);
	
	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);

	/* get return address, stored in EDX */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EIP), 4);
	/* restore EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBX), 4);
	/* restore EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EDI), 4);
	/* restore ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESI), 4);
	/* restore ESP */
	x86_mov_reg_membase (code, X86_ESP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESP), 4);
	/* restore EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBP), 4);
	/* restore ECX. the exception object is passed here to the catch handler */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ECX), 4);

	/* jump to the saved IP */
	x86_jump_reg (code, X86_EDX);

	return start;
}

/*
 * arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter.
 */
static gpointer
arch_get_call_filter (void)
{
	static guint8 start [64];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	/* call_finally (struct sigcontext *ctx, unsigned long eip, gpointer exc) */
	code = start;

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	/* load eip */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 12, 4);
	/* save EBP */
	x86_push_reg (code, X86_EBP);
	/* push exc */
	x86_push_membase (code, X86_EBP, 16);
	/* set new EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBP), 4);
	/* restore registers used by global register allocation (EBX & ESI) */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBX), 4);
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESI), 4);
	/* save the ESP - this is used by endfinally */
	x86_mov_membase_reg (code, X86_EBP, mono_exc_esp_offset, X86_ESP, 4);
	/* call the handler */
	x86_call_reg (code, X86_ECX);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
	/* restore EBP */
	x86_pop_reg (code, X86_EBP);
	/* restore saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);

	g_assert ((code - start) < 64);
	return start;
}

/*
 * arch_get_call_finally:
 *
 * Returns a pointer to a method which calls a finally handler.
 */
static gpointer
arch_get_call_finally (void)
{
	static guint8 start [64];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	/* call_finally (struct sigcontext *ctx, unsigned long eip) */
	code = start;

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	/* load eip */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 12, 4);
	/* save EBP */
	x86_push_reg (code, X86_EBP);
	/* set new EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBP), 4);
	/* restore registers used by global register allocation (EBX & ESI) */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EBX), 4);
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_ESI), 4);
	/* save the ESP - this is used by endfinally */
	x86_mov_membase_reg (code, X86_EBP, mono_exc_esp_offset, X86_ESP, 4);
	/* call the handler */
	x86_call_reg (code, X86_ECX);
	/* restore EBP */
	x86_pop_reg (code, X86_EBP);
	/* restore saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);

	g_assert ((code - start) < 64);
	return start;
}

static MonoArray *
glist_to_array (GList *list) 
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	int len, i;

	if (!list)
		return NULL;

	len = g_list_length (list);
	res = mono_array_new (domain, mono_defaults.int_class, len);

	for (i = 0; list; list = list->next, i++)
		mono_array_set (res, gpointer, i, list->data);

	return res;
}

MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	MonoArray *ta = exc->trace_ips;
	int i, len;
	
	len = mono_array_length (ta);

	res = mono_array_new (domain, mono_defaults.stack_frame_class, len > skip ? len - skip : 0);

	for (i = skip; i < len; i++) {
		MonoJitInfo *ji;
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new (domain, mono_defaults.stack_frame_class);
		gpointer ip = mono_array_get (ta, gpointer, i);

		ji = mono_jit_info_table_find (domain, ip);
		g_assert (ji != NULL);

		sf->method = mono_method_get_object (domain, ji->method);
		sf->native_offset = (char *)ip - (char *)ji->code_start;
		sf->il_offset = mono_debug_il_offset_from_address (ji->method, sf->native_offset);

		if (need_file_info) {
			gchar *filename;

			filename = mono_debug_source_location_from_address (ji->method, sf->native_offset, &sf->line);

			sf->filename = mono_string_new (domain, filename ? filename : "<unknown>");
			sf->column = 0;

			g_free (filename);
		}

		mono_array_set (res, gpointer, i, sf);
	}

	return res;
}

MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	gpointer *sf = (gpointer *)&skip;
	gpointer ip = sf [-1];
	int addr;
	gpointer *bp = sf [-2];
	MonoMethod *m = NULL;

	do {
		MonoJitInfo *ji;
		addr = -1; /* unknown */

		if ((ji = mono_jit_info_table_find (domain, ip))) {
			m = ji->method;
			addr = (char *)ip - (char *)ji->code_start;
			ip = (gpointer)((char *)bp [1] - 5);
			bp = bp [0];
		} else {
			if (!lmf)
				return FALSE;
			
			m = lmf->method;

			bp = (gpointer)lmf->ebp;
			ip = (gpointer)lmf->eip;

			lmf = lmf->previous_lmf;
		}

		if ((unsigned)bp >= (unsigned)jit_tls->end_of_stack)
			return FALSE;

	} while (skip-- > 0);

	g_assert (m);

	*method = mono_method_get_object (domain, m);
	*iloffset = mono_debug_il_offset_from_address (m, addr);
	*native_offset = addr;

	if (need_file_info) {
		gchar *filename;

		filename = mono_debug_source_location_from_address (m, addr, line);

		*file = mono_string_new (domain, filename ? filename : "<unknown>");
		*column = 0;

		g_free (filename);
	}

	return TRUE;
}

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj:
 */
gboolean
arch_handle_exception (struct sigcontext *ctx, gpointer obj, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	static void (*restore_context) (struct sigcontext *);
	static void (*call_finally) (struct sigcontext *, unsigned long);
	static int (*call_filter) (struct sigcontext *, gpointer, gpointer);
	void (*cleanup) (MonoObject *exc);
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	gpointer end_of_stack;
	struct sigcontext ctx_cp;
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;
	MonoMethod *m;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	g_assert (mono_object_isinst (obj, mono_defaults.exception_class));

	((MonoException *)obj)->stack_trace = NULL;

	if (!restore_context)
		restore_context = arch_get_restore_context ();
	
	if (!call_finally)
		call_finally = arch_get_call_finally ();

	if (!call_filter)
		call_filter = arch_get_call_filter ();

	end_of_stack = jit_tls->end_of_stack;
	g_assert (end_of_stack);

	cleanup = jit_tls->abort_func;
	g_assert (cleanup);
	
	if (!test_only) {
		ctx_cp = *ctx;
		if (!arch_handle_exception (&ctx_cp, obj, TRUE)) {
			if (mono_break_on_exc) {
				if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
					mono_debug_make_symbols ();
				G_BREAKPOINT ();
			}
			mono_unhandled_exception (obj);
		}
	}

	while (1) {

		ji = mono_jit_info_table_find (domain, (gpointer)ctx->SC_EIP);
	
		/* we are inside managed code if ji != NULL */
		if (ji != NULL) {
			int offset;
			m = ji->method;

			if (m == mono_start_method) {
				if (!test_only) {
					jit_tls->lmf = lmf;
					cleanup (obj);
					g_assert_not_reached ();
				} else {
					((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
					g_list_free (trace_ips);
					return FALSE;
				}
			}
			
			if (test_only) {
				char    *strace;
				char    *tmp, *source_location, *tmpaddr, *fname;
				gint32   address, iloffset;

				trace_ips = g_list_append (trace_ips, (gpointer)ctx->SC_EIP);

				if (!((MonoException*)obj)->stack_trace)
					strace = g_strdup ("");
				else
					strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);

				address = (char *)ctx->SC_EIP - (char *)ji->code_start;

				source_location = mono_debug_source_location_from_address (m, address, NULL);
				iloffset = mono_debug_il_offset_from_address (m, address);

				if (iloffset < 0)
					tmpaddr = g_strdup_printf ("<0x%05x>", address);
				else
					tmpaddr = g_strdup_printf ("[0x%05x]", iloffset);

				fname = mono_method_full_name (m, TRUE);

				if (source_location)
					tmp = g_strdup_printf ("%sin %s (at %s) %s\n", strace, tmpaddr,
							       source_location, fname);
				else
					tmp = g_strdup_printf ("%sin %s %s\n", strace, tmpaddr,
							       fname);
				g_free (fname);
				g_free (source_location);
				g_free (strace);
				g_free (tmpaddr);

				((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);

				g_free (tmp);
			}
			
			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];

					if (ei->try_start <= (gpointer)ctx->SC_EIP && 
					    (gpointer)ctx->SC_EIP <= ei->try_end) { 
						/* catch block */
						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, mono_class_get (m->klass->image, ei->data.token))) ||
						    ((ei->flags == MONO_EXCEPTION_CLAUSE_FILTER &&
						      call_filter (ctx, ei->data.filter, obj)))) {
							if (test_only) {
								((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
								g_list_free (trace_ips);
								return TRUE;
							}
							ctx->SC_EIP = (unsigned long)ei->handler_start;
							ctx->SC_ECX = (unsigned long)obj;
							jit_tls->lmf = lmf;
							restore_context (ctx);
							g_assert_not_reached ();
						}
					}
				}

				/* no handler found - we need to call all finally handlers */
				if (!test_only) {
					for (i = 0; i < ji->num_clauses; i++) {
						MonoJitExceptionInfo *ei = &ji->clauses [i];

						if (ei->try_start <= (gpointer)ctx->SC_EIP && 
						    (gpointer)ctx->SC_EIP < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							call_finally (ctx, (unsigned long)ei->handler_start);
						}
					}
				}
			}

			/* continue unwinding */

			offset = -1;
			/* restore caller saved registers */
			if (ji->used_regs & X86_EBX_MASK) {
				ctx->SC_EBX = *((int *)ctx->SC_EBP + offset);
				offset--;
			}
			if (ji->used_regs & X86_EDI_MASK) {
				ctx->SC_EDI = *((int *)ctx->SC_EBP + offset);
				offset--;
			}
			if (ji->used_regs & X86_ESI_MASK) {
				ctx->SC_ESI = *((int *)ctx->SC_EBP + offset);
			}

			ctx->SC_ESP = ctx->SC_EBP;
			ctx->SC_EIP = *((int *)ctx->SC_EBP + 1) - 5;
			ctx->SC_EBP = *((int *)ctx->SC_EBP);

			if (ctx->SC_EBP > (unsigned)end_of_stack) {
				if (!test_only) {
					jit_tls->lmf = lmf;
					cleanup (obj);
					g_assert_not_reached ();
				} else {
					((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
					g_list_free (trace_ips);
					return FALSE;
				}
			}
	
		} else {
			if (!lmf) {
				if (!test_only) {
					jit_tls->lmf = lmf;
					cleanup (obj);
					g_assert_not_reached ();
				} else {
					((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
					g_list_free (trace_ips);
					return FALSE;
				}
			}
			
			m = lmf->method;

			if (test_only) {
				char  *strace; 
				char  *tmp;

				trace_ips = g_list_append (trace_ips, lmf->method->info);

				if (!((MonoException*)obj)->stack_trace)
					strace = g_strdup ("");
				else
					strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);

				tmp = g_strdup_printf ("%sin (unmanaged) %s\n", strace, mono_method_full_name (m, TRUE));

				g_free (strace);

				((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);
				g_free (tmp);
			}

			ctx->SC_ESI = lmf->esi;
			ctx->SC_EDI = lmf->edi;
			ctx->SC_EBX = lmf->ebx;
			ctx->SC_EBP = lmf->ebp;
			ctx->SC_EIP = lmf->eip;
			ctx->SC_ESP = (unsigned long)&lmf->eip;

			lmf = lmf->previous_lmf;

			if (ctx->SC_EBP >= (unsigned)end_of_stack) {
				if (!test_only) {
					jit_tls->lmf = lmf;
					cleanup (obj);
					g_assert_not_reached ();
				} else {
					((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
					g_list_free (trace_ips);
					return FALSE;
				}
			}
		}
	}

	g_assert_not_reached ();
}

static void
throw_exception (unsigned long eax, unsigned long ecx, unsigned long edx, unsigned long ebx,
		 unsigned long esi, unsigned long edi, unsigned long ebp, MonoObject *exc,
		 unsigned long eip,  unsigned long esp)
{
	struct sigcontext ctx;

	/* adjust eip so that it point to the call instruction */
	eip -= 5;

	ctx.SC_ESP = esp;
	ctx.SC_EIP = eip;
	ctx.SC_EBP = ebp;
	ctx.SC_EDI = edi;
	ctx.SC_ESI = esi;
	ctx.SC_EBX = ebx;
	ctx.SC_EDX = edx;
	ctx.SC_ECX = ecx;
	ctx.SC_EAX = eax;
	
	arch_handle_exception (&ctx, exc, FALSE);

	g_assert_not_reached ();
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
arch_get_throw_exception (void)
{
	static guint8 start [24];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	x86_push_reg (code, X86_ESP);
	x86_push_membase (code, X86_ESP, 4); /* IP */
	x86_push_membase (code, X86_ESP, 12); /* exception */
	x86_push_reg (code, X86_EBP);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDX);
	x86_push_reg (code, X86_ECX);
	x86_push_reg (code, X86_EAX);
	x86_call_code (code, throw_exception);
	/* we should never reach this breakpoint */
	x86_breakpoint (code);

	g_assert ((code - start) < 24);
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
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer 
arch_get_throw_exception_by_name ()
{
	static guint8 start [32];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	/* fixme: we do not save EAX, EDX, ECD - unsure if we need that */

	x86_push_membase (code, X86_ESP, 4); /* exception name */
	x86_push_imm (code, "System");
	x86_push_imm (code, mono_defaults.exception_class->image);
	x86_call_code (code, mono_exception_from_name);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);
	/* save the newly create object (overwrite exception name)*/
	x86_mov_membase_reg (code, X86_ESP, 4, X86_EAX, 4);
	x86_jump_code (code, arch_get_throw_exception ());

	g_assert ((code - start) < 32);

	return start;
}	
