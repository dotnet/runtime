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
#include <mono/metadata/exception.h>

#include "mini.h"
#include "mini-ppc.h"
#include "debug-private.h"

typedef struct sigcontext MonoContext;

/* we have the stack pointer, not the base pointer in sigcontext */
#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->sc_ir = (int)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->sc_sp = (int)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->sc_ir))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sc_sp))

/* disbale this for now */
#undef MONO_USE_EXC_TABLES

#ifdef MONO_USE_EXC_TABLES

/*************************************/
/*    STACK UNWINDING STUFF          */
/*************************************/

/* These definitions are from unwind-dw2.c in glibc 2.2.5 */

/* For x86 */
#define DWARF_FRAME_REGISTERS 17

typedef struct frame_state
{
  void *cfa;
  void *eh_ptr;
  long cfa_offset;
  long args_size;
  long reg_or_offset[DWARF_FRAME_REGISTERS+1];
  unsigned short cfa_reg;
  unsigned short retaddr_column;
  char saved[DWARF_FRAME_REGISTERS+1];
} frame_state;

#if 0

static long
get_sigcontext_reg (struct sigcontext *ctx, int dwarf_regnum)
{
	switch (dwarf_regnum) {
	case X86_EAX:
		return ctx->eax;
	case X86_EBX:
		return ctx->ebx;
	case X86_ECX:
		return ctx->ecx;
	case X86_EDX:
		return ctx->edx;
	case X86_ESI:
		return ctx->esi;
	case X86_EDI:
		return ctx->edi;
	case X86_EBP:
		return ctx->ebp;
	case X86_ESP:
		return ctx->esp;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

static void
set_sigcontext_reg (struct sigcontext *ctx, int dwarf_regnum, long value)
{
	switch (dwarf_regnum) {
	case X86_EAX:
		ctx->eax = value;
		break;
	case X86_EBX:
		ctx->ebx = value;
		break;
	case X86_ECX:
		ctx->ecx = value;
		break;
	case X86_EDX:
		ctx->edx = value;
		break;
	case X86_ESI:
		ctx->esi = value;
		break;
	case X86_EDI:
		ctx->edi = value;
		break;
	case X86_EBP:
		ctx->ebp = value;
		break;
	case X86_ESP:
		ctx->esp = value;
		break;
	case 8:
		ctx->eip = value;
		break;
	default:
		g_assert_not_reached ();
	}
}

typedef struct frame_state * (*framesf) (void *, struct frame_state *);

static framesf frame_state_for = NULL;

static gboolean inited = FALSE;

typedef char ** (*get_backtrace_symbols_type) (void *__const *__array, int __size);

static get_backtrace_symbols_type get_backtrace_symbols = NULL;

static void
init_frame_state_for (void)
{
	GModule *module;

	/*
	 * There are two versions of __frame_state_for: one in libgcc.a and the
	 * other in glibc.so. We need the version from glibc.
	 * For more info, see this:
	 * http://gcc.gnu.org/ml/gcc/2002-08/msg00192.html
	 */
	if ((module = g_module_open ("libc.so.6", G_MODULE_BIND_LAZY))) {
	
		if (!g_module_symbol (module, "__frame_state_for", (gpointer*)&frame_state_for))
			frame_state_for = NULL;

		if (!g_module_symbol (module, "backtrace_symbols", (gpointer*)&get_backtrace_symbols)) {
			get_backtrace_symbols = NULL;
			frame_state_for = NULL;
		}

		g_module_close (module);
	}

	inited = TRUE;
}

#endif

/* mono_arch_has_unwind_info:
 *
 * Tests if a function has an DWARF exception table able to restore
 * all caller saved registers. 
 */
gboolean
mono_arch_has_unwind_info (MonoMethod *method)
{
#if 0
	struct frame_state state_in;
	struct frame_state *res;

	if (!inited) 
		init_frame_state_for ();
	
	if (!frame_state_for)
		return FALSE;

	g_assert (method->addr);

	memset (&state_in, 0, sizeof (state_in));

	/* offset 10 is just a guess, but it works for all methods tested */
	if ((res = frame_state_for ((char *)method->addr + 10, &state_in))) {

		if (res->saved [X86_EBX] != 1 ||
		    res->saved [X86_EDI] != 1 ||
		    res->saved [X86_EBP] != 1 ||
		    res->saved [X86_ESI] != 1) {
			return FALSE;
		}
		return TRUE;
	} else {
		return FALSE;
	}
#else
	return FALSE;
#endif
}

struct stack_frame
{
  void *next;
  void *return_address;
};

static MonoJitInfo *
ppc_unwind_native_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, struct sigcontext *ctx, 
			 struct sigcontext *new_ctx, MonoLMF *lmf, char **trace)
{
#if 0
	struct stack_frame *frame;
	gpointer max_stack;
	MonoJitInfo *ji;
	struct frame_state state_in;
	struct frame_state *res;

	if (trace)
		*trace = NULL;

	if (!inited) 
		init_frame_state_for ();

	if (!frame_state_for)
		return FALSE;

	frame = MONO_CONTEXT_GET_BP (ctx);

	max_stack = lmf && lmf->method ? lmf : jit_tls->end_of_stack;

	*new_ctx = *ctx;

	memset (&state_in, 0, sizeof (state_in));

	while ((gpointer)frame->next < (gpointer)max_stack) {
		gpointer ip, addr = frame->return_address;
		void *cfa;
		char *tmp, **symbols;

		if (trace) {
			ip = MONO_CONTEXT_GET_IP (new_ctx);
			symbols = get_backtrace_symbols (&ip, 1);
			if (*trace)
				tmp = g_strdup_printf ("%s\nin (unmanaged) %s", *trace, symbols [0]);
			else
				tmp = g_strdup_printf ("in (unmanaged) %s", symbols [0]);

			free (symbols);
			g_free (*trace);
			*trace = tmp;
		}

		if ((res = frame_state_for (addr, &state_in))) {	
			int i;

			cfa = (gint8*) (get_sigcontext_reg (new_ctx, res->cfa_reg) + res->cfa_offset);
			frame = (struct stack_frame *)((gint8*)cfa - 8);
			for (i = 0; i < DWARF_FRAME_REGISTERS + 1; i++) {
				int how = res->saved[i];
				long val;
				g_assert ((how == 0) || (how == 1));
			
				if (how == 1) {
					val = * (long*) ((gint8*)cfa + res->reg_or_offset[i]);
					set_sigcontext_reg (new_ctx, i, val);
				}
			}
			new_ctx->esp = (long)cfa;

			if (res->saved [X86_EBX] == 1 &&
			    res->saved [X86_EDI] == 1 &&
			    res->saved [X86_EBP] == 1 &&
			    res->saved [X86_ESI] == 1 &&
			    (ji = mono_jit_info_table_find (domain, frame->return_address))) {
				//printf ("FRAME CFA %s\n", mono_method_full_name (ji->method, TRUE));
				return ji;
			}

		} else {
			//printf ("FRAME %p %p %p\n", frame, MONO_CONTEXT_GET_IP (new_ctx), mono_jit_info_table_find (domain, MONO_CONTEXT_GET_IP (new_ctx)));

			MONO_CONTEXT_SET_IP (new_ctx, frame->return_address);
			frame = frame->next;
			MONO_CONTEXT_SET_BP (new_ctx, frame);

			/* stop if !frame or when we detect an unexpected managed frame */
			if (!frame || mono_jit_info_table_find (domain, frame->return_address)) {
				if (trace) {
					g_free (*trace);
					*trace = NULL;
				}
				return NULL;
			}
		}
	}

	//if (!lmf)
	//g_assert_not_reached ();

	if (trace) {
		g_free (*trace);
		*trace = NULL;
	}
#endif
	return NULL;
}

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

#if 0
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

	/* jump to the saved IP */
	x86_jump_reg (code, X86_EDX);

#endif
	return start;
}

/*
 * arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
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
	/* call_filter (struct sigcontext *ctx, unsigned long eip, gpointer exc) */
	code = start;

#if 0
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
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, SC_EDI), 4);
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
#endif
	return start;
}

static void
throw_exception (unsigned long eax, unsigned long ecx, unsigned long edx, unsigned long ebx,
		 unsigned long esi, unsigned long edi, unsigned long ebp, MonoObject *exc,
		 unsigned long eip,  unsigned long esp)
{
	static void (*restore_context) (struct sigcontext *);
	struct sigcontext ctx;

	if (!restore_context)
		restore_context = arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip -= 1;

#if 0
	ctx.SC_ESP = esp;
	ctx.SC_EIP = eip;
	ctx.SC_EBP = ebp;
	ctx.SC_EDI = edi;
	ctx.SC_ESI = esi;
	ctx.SC_EBX = ebx;
	ctx.SC_EDX = edx;
	ctx.SC_ECX = ecx;
	ctx.SC_EAX = eax;
#endif	
	mono_arch_handle_exception (&ctx, exc, FALSE);
	restore_context (&ctx);

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
mono_arch_get_throw_exception (void)
{
	static guint8 start [24];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

#if 0
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
#endif
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
	static guint8 start [32];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;
#if 0
	x86_push_membase (code, X86_ESP, 4); /* exception name */
	x86_push_imm (code, "System");
	x86_push_imm (code, mono_defaults.exception_class->image);
	x86_call_code (code, mono_exception_from_name);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);
	/* save the newly create object (overwrite exception name)*/
	x86_mov_membase_reg (code, X86_ESP, 4, X86_EAX, 4);
	x86_jump_code (code, mono_arch_get_throw_exception ());

	g_assert ((code - start) < 32);
#endif
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

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
static MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	ji = mono_jit_info_table_find (domain, ip);

	if (trace)
		*trace = NULL;

	if (native_offset)
		*native_offset = -1;

	if (managed)
		*managed = FALSE;

	if (ji != NULL) {
		char *source_location, *tmpaddr, *fname;
		gint32 address, iloffset;
		int offset;

		*new_ctx = *ctx;

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		address = (char *)ip - (char *)ji->code_start;

		if (native_offset)
			*native_offset = address;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (trace) {
			if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
				mono_debug_make_symbols ();

			source_location = mono_debug_source_location_from_address (ji->method, address, NULL);
			iloffset = mono_debug_il_offset_from_address (ji->method, address);
			source_location = NULL;
			iloffset = -1;

			if (iloffset < 0)
				tmpaddr = g_strdup_printf ("<0x%05x>", address);
			else
				tmpaddr = g_strdup_printf ("[0x%05x]", iloffset);
		
			fname = mono_method_full_name (ji->method, TRUE);

			if (source_location)
				*trace = g_strdup_printf ("in %s (at %s) %s", tmpaddr, source_location, fname);
			else
				*trace = g_strdup_printf ("in %s %s", tmpaddr, fname);

			g_free (fname);
			g_free (source_location);
			g_free (tmpaddr);
		}
#if 0				
		offset = -1;
		/* restore caller saved registers */
		if (ji->used_regs & X86_EBX_MASK) {
			new_ctx->SC_EBX = *((int *)ctx->SC_EBP + offset);
			offset--;
		}
		if (ji->used_regs & X86_EDI_MASK) {
			new_ctx->SC_EDI = *((int *)ctx->SC_EBP + offset);
			offset--;
		}
		if (ji->used_regs & X86_ESI_MASK) {
			new_ctx->SC_ESI = *((int *)ctx->SC_EBP + offset);
		}

		new_ctx->SC_ESP = ctx->SC_EBP;
		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->SC_EIP = *((int *)ctx->SC_EBP + 1) - 1;
		new_ctx->SC_EBP = *((int *)ctx->SC_EBP);
#endif
		return ji;
#ifdef MONO_USE_EXC_TABLES
	} else if ((ji = ppc_unwind_native_frame (domain, jit_tls, ctx, new_ctx, *lmf, trace))) {
		return ji;
#endif
	} else if (*lmf) {
		
		*new_ctx = *ctx;

		if (!(*lmf)->method)
			return (gpointer)-1;

		if (trace)
			*trace = g_strdup_printf ("in (unmanaged) %s", mono_method_full_name ((*lmf)->method, TRUE));
		
		ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip);
		g_assert (ji != NULL);

#if 0
		new_ctx->SC_ESI = (*lmf)->esi;
		new_ctx->SC_EDI = (*lmf)->edi;
		new_ctx->SC_EBX = (*lmf)->ebx;
		new_ctx->SC_EBP = (*lmf)->ebp;
		new_ctx->SC_EIP = (*lmf)->eip;
		/* the lmf is always stored on the stack, so the following
		 * expression points to a stack location which can be used as ESP */
		new_ctx->SC_ESP = (unsigned long)&((*lmf)->eip);
#endif
		*lmf = (*lmf)->previous_lmf;

		return ji;
		
	}

	return NULL;
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

		sf->method = mono_method_get_object (domain, ji->method, NULL);
		sf->native_offset = (char *)ip - (char *)ji->code_start;

		sf->il_offset = mono_debug_il_offset_from_address (ji->method, sf->native_offset);
		sf->il_offset = -1;

		if (need_file_info) {
			gchar *filename;
			
			filename = mono_debug_source_location_from_address (ji->method, sf->native_offset, &sf->line);
			filename = NULL;

			sf->filename = filename? mono_string_new (domain, filename): NULL;
			sf->column = 0;

			g_free (filename);
		}

		mono_array_set (res, gpointer, i, sf);
	}

	return res;
}

void
mono_jit_walk_stack (MonoStackWalk func, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji;
	gint native_offset, il_offset;
	gboolean managed;

	MonoContext ctx, new_ctx;

	MONO_CONTEXT_SET_IP (&ctx, __builtin_return_address (0));
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (1));

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		il_offset = mono_debug_il_offset_from_address (ji->method, native_offset);

		if (func (ji->method, native_offset, il_offset, managed, user_data))
			return;
		
		ctx = new_ctx;
	}
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
	MonoJitInfo *ji;
	MonoContext ctx, new_ctx;

	MONO_CONTEXT_SET_IP (&ctx, ves_icall_get_frame_info);
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));

	skip++;

	do {
		ji = mono_arch_find_jit_info (domain, jit_tls, &ctx, &new_ctx, NULL, &lmf, native_offset, NULL);

		ctx = new_ctx;
		
		if (!ji || ji == (gpointer)-1 || MONO_CONTEXT_GET_BP (&ctx) >= jit_tls->end_of_stack)
			return FALSE;

		if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE)
			continue;

		skip--;

	} while (skip >= 0);

	*method = mono_method_get_object (domain, ji->method, NULL);
	*iloffset = mono_debug_il_offset_from_address (ji->method, *native_offset);
	*iloffset = -1;

	if (need_file_info) {
		gchar *filename;

		filename = mono_debug_source_location_from_address (ji->method, *native_offset, line);
		filename = NULL;

		*file = filename? mono_string_new (domain, filename): NULL;
		*column = 0;

		g_free (filename);
	}

	return TRUE;
}

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 *
 *
 */
gboolean
mono_arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	static int (*call_filter) (MonoContext *, gpointer, gpointer) = NULL;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	g_assert (mono_object_isinst (obj, mono_defaults.exception_class));

	if (!call_filter)
		call_filter = arch_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		if (mono_jit_trace_calls)
			g_print ("EXCEPTION handling: %s\n", mono_object_class (obj)->name);
		if (!mono_arch_handle_exception (&ctx_cp, obj, TRUE)) {
			if (mono_break_on_exc) {
				if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
					mono_debug_make_symbols ();
				G_BREAKPOINT ();
			}
			mono_unhandled_exception (obj);
		}
	}

	while (1) {
		MonoContext new_ctx;
		char *trace = NULL;
		
		ji = mono_arch_find_jit_info (domain, jit_tls, ctx, &new_ctx, 
					      test_only ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			
			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE) {
				char *tmp, *strace;

				trace_ips = g_list_append (trace_ips, MONO_CONTEXT_GET_IP (ctx));

				if (!((MonoException*)obj)->stack_trace)
					strace = g_strdup ("");
				else
					strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);
			
				tmp = g_strdup_printf ("%s%s\n", strace, trace);
				g_free (strace);

				((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);

				g_free (tmp);
			}

			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];

					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */
						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, mono_class_get (ji->method->klass->image, ei->data.token))) ||
						    ((ei->flags == MONO_EXCEPTION_CLAUSE_FILTER &&
						      call_filter (ctx, ei->data.filter, obj)))) {
							if (test_only) {
								((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
								g_list_free (trace_ips);
								return TRUE;
							}
							if (mono_jit_trace_calls)
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							*((gpointer *)(MONO_CONTEXT_GET_BP (ctx) + ji->exvar_offset)) = obj;
							jit_tls->lmf = lmf;
							return 0;
						}
					}
				}

				/* no handler found - we need to call all finally handlers */
				if (!test_only) {
					for (i = 0; i < ji->num_clauses; i++) {
						MonoJitExceptionInfo *ei = &ji->clauses [i];
						
						if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_jit_trace_calls)
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							call_filter (ctx, ei->handler_start, NULL);
						}
					}
				}
			}
		}

		g_free (trace);
			
		*ctx = new_ctx;

		if ((ji == (gpointer)-1) || MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack) {
			if (!test_only) {
				jit_tls->lmf = lmf;
				jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				((MonoException*)obj)->trace_ips = glist_to_array (trace_ips);
				g_list_free (trace_ips);
				return FALSE;
			}
		}
	}

	g_assert_not_reached ();
}

