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
 * arch_get_call_finally:
 *
 * Returns a pointer to a method which calls a finally handler.
 */
static gpointer
arch_get_call_finally (void)
{
	static guint8 start [28];
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

	g_assert ((code - start) < 28);
	return start;
}

/*
 * return TRUE if the exception is catched. It also sets the 
 * stack_trace String. 
 */
static gboolean
arch_exc_is_catched (MonoDomain *domain, MonoJitTlsData *jit_tls, gpointer ip, 
		     gpointer *bp, gpointer obj)
{
	MonoJitInfo *ji;
	gpointer *end_of_stack;
	MonoLMF *lmf = jit_tls->lmf;
	MonoMethod *m;
	int i;

	end_of_stack = jit_tls->end_of_stack;
	g_assert (end_of_stack);

	while (1) {

		ji = mono_jit_info_table_find (domain, ip);

		if (ji) { /* we are inside managed code */
			m = ji->method;
			
			if (mono_object_isinst (obj, mono_defaults.exception_class)) {
				char    *strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);
				char    *tmp, *tmpsig, *source_location, *tmpaddr;
				gint32   address, iloffset;

				if (!strcmp (strace, "TODO: implement stack traces")){
					g_free (strace);
					strace = g_strdup ("");
				}

				address = (char *)ip - (char *)ji->code_start;

				source_location = mono_debug_source_location_from_address (m, address);
				iloffset = mono_debug_il_offset_from_address (m, address);

				if (iloffset < 0)
					tmpaddr = g_strdup_printf ("<0x%05x>", address);
				else
					tmpaddr = g_strdup_printf ("[0x%05x]", iloffset);

				tmpsig = mono_signature_get_desc(m->signature, TRUE);
				if (source_location)
					tmp = g_strdup_printf ("%sin %s (at %s) %s.%s:%s (%s)\n", strace, tmpaddr,
							       source_location, m->klass->name_space, m->klass->name,
							       m->name, tmpsig);
				else
					tmp = g_strdup_printf ("%sin %s %s.%s:%s (%s)\n", strace, tmpaddr,
							       m->klass->name_space, m->klass->name, m->name, tmpsig);
				g_free (source_location);
				g_free (tmpsig);
				g_free (strace);

				((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);
				g_free (tmp);
			}

			if (ji->num_clauses) {

				g_assert (ji->clauses);

				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];
				
					if (ei->try_start <= ip && ip <= (ei->try_end)) { 
						/* catch block */
						if (ei->flags == 0 && mono_object_isinst (obj, 
						        mono_class_get (m->klass->image, ei->token_or_filter))) {
							return TRUE;
						}
					}
				}
			}

			/* continue unwinding */

			ip = (gpointer)(*((int *)bp + 1) - 5);
			bp = (gpointer)(*((int *)bp));

			if (bp >= end_of_stack)
				return FALSE;
	
		} else {
			if (!lmf) 
				return FALSE;

			bp = (gpointer)lmf->ebp;
			ip = (gpointer)lmf->eip;

			m = lmf->method;

			if (mono_object_isinst (obj, mono_defaults.exception_class)) {
				char  *strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);
				char  *tmp;

				if (!strcmp (strace, "TODO: implement stack traces"))
					strace = g_strdup ("");

				tmp = g_strdup_printf ("%sin (unmanaged) %s.%s:%s ()\n", strace, m->klass->name_space,  
						       m->klass->name, m->name);

				g_free (strace);

				((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);
				g_free (tmp);
			}

			lmf = lmf->previous_lmf;

			if (bp >= end_of_stack)
				return FALSE;
		}
	}
	
	return FALSE;
}

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj:
 */
void
arch_handle_exception (struct sigcontext *ctx, gpointer obj)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	gpointer ip = (gpointer)ctx->SC_EIP;
	static void (*restore_context) (struct sigcontext *);
	static void (*call_finally) (struct sigcontext *, unsigned long);
	void (*cleanup) (MonoObject *exc);
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	gpointer end_of_stack;

	g_assert (ctx != NULL);

	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	}

	if (!restore_context)
		restore_context = arch_get_restore_context ();
	
	if (!call_finally)
		call_finally = arch_get_call_finally ();

	end_of_stack = jit_tls->end_of_stack;
	g_assert (end_of_stack);

	cleanup = jit_tls->abort_func;

	if (!arch_exc_is_catched (domain, jit_tls, ip, (gpointer *)ctx->SC_EBP, obj)) {
		if (mono_debug_format != MONO_DEBUG_FORMAT_NONE) {
			mono_debug_make_symbols ();
			G_BREAKPOINT ();
		}
		mono_unhandled_exception (obj);
	}

	while (1) {

		ji = mono_jit_info_table_find (domain, ip);
	
		if (ji) { /* we are inside managed code */
			MonoMethod *m = ji->method;
			int offset;

			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];

					if (ei->try_start <= ip && ip <= (ei->try_end)) { 
						/* catch block */
						if (ei->flags == 0 && mono_object_isinst (obj, 
						        mono_class_get (m->klass->image, ei->token_or_filter))) {
					
							ctx->SC_EIP = (unsigned long)ei->handler_start;
							ctx->SC_ECX = (unsigned long)obj;
							restore_context (ctx);
							g_assert_not_reached ();
						}
					}
				}

				/* no handler found - we need to call all finally handlers */
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];

					if (ei->try_start <= ip && ip < (ei->try_end) &&
					    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
						call_finally (ctx, (unsigned long)ei->handler_start);
					}
				}
			}

			/* continue unwinding */

			offset = -1;
			/* restore caller saved registers */
			if (ji->used_regs & X86_ESI_MASK) {
				ctx->SC_EBX = *((int *)ctx->SC_EBP + offset);
				offset--;
			}
			if (ji->used_regs & X86_EDI_MASK) {
				ctx->SC_EDI = *((int *)ctx->SC_EBP + offset);
				offset--;
			}
			if (ji->used_regs & X86_EBX_MASK) {
				ctx->SC_ESI = *((int *)ctx->SC_EBP + offset);
			}

			ctx->SC_ESP = ctx->SC_EBP;
			ctx->SC_EIP = *((int *)ctx->SC_EBP + 1) - 5;
			ctx->SC_EBP = *((int *)ctx->SC_EBP);

			ip = (gpointer)ctx->SC_EIP;

			if (ctx->SC_EBP > (unsigned)end_of_stack) {
				g_assert (cleanup);
				cleanup (obj);
				g_assert_not_reached ();
			}
	
		} else {
			MonoLMF *lmf = jit_tls->lmf;

			if (!lmf) {
				g_assert (cleanup);
				cleanup (obj);
				g_assert_not_reached ();
			}

			jit_tls->lmf = lmf->previous_lmf;

			ctx->SC_ESI = lmf->esi;
			ctx->SC_EDI = lmf->edi;
			ctx->SC_EBX = lmf->ebx;
			ctx->SC_EBP = lmf->ebp;
			ctx->SC_EIP = lmf->eip;
			ctx->SC_ESP = (unsigned long)&lmf->eip;

			ip = (gpointer)ctx->SC_EIP;

			if (ctx->SC_EBP >= (unsigned)end_of_stack) {
				g_assert (cleanup);
				cleanup (obj);
				g_assert_not_reached ();
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
	
	arch_handle_exception (&ctx, exc);

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
