/*
 * exceptionsparc.c: exception support for 64 bit sparc
 *
 * Authors:
 *   Mark Crichton (crichton@gimp.org)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>

#include <mono/arch/sparc/sparc-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-sparc.h"

#warning NotReady

gboolean  mono_arch_handle_exception (struct sigcontext *ctx, gpointer obj, gboolean test_only);

typedef struct sigcontext MonoContext;

#define MONO_CONTEXT_SET_IP(ctx,ip) do { (ctx)->SC_EIP = (long)ip; } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,bp) do { (ctx)->SC_EBP = (long)bp; } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->SC_EIP))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->SC_EBP))

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

/* mono_arch_has_unwind_info:
 *
 * Tests if a function has an DWARF exception table able to restore
 * all caller saved registers. 
 */
gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

struct stack_frame
{
  void *next;
  void *return_address;
};
#endif

gpointer 
mono_arch_get_throw_exception (void)
{
	g_assert (TRUE);
	return NULL;
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
	g_assert (TRUE);
	return NULL;
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
	return NULL;
}

void
mono_jit_walk_stack (MonoStackWalk func, gpointer user_data) {
}

MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	return FALSE;
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
	g_assert_not_reached ();
}
