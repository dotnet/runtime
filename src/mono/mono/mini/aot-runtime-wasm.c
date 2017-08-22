/**
 * \file
 * WASM AOT runtime
 */

#include "config.h"

#include <sys/types.h>

#include "mini.h"
#include "interp/interp.h"

#ifdef TARGET_WASM

static void
wasm_restore_context (void)
{
	g_error ("wasm_restore_context");
}

static void
wasm_call_filter (void)
{
	g_error ("wasm_call_filter");
}

static void
wasm_throw_exception (void)
{
	g_error ("wasm_throw_exception");
}

static void
wasm_rethrow_exception (void)
{
	g_error ("wasm_rethrow_exception");
}

static void
wasm_throw_corlib_exception (void)
{
	g_error ("wasm_throw_corlib_exception");
}

static void
wasm_enter_icall_trampoline (void *target_func, InterpMethodArguments *margs)
{
	g_error ("wasm_enter_icall_trampoline");
	
}

gpointer
mono_aot_get_trampoline_full (const char *name, MonoTrampInfo **out_tinfo)
{
	gpointer code = NULL;

	if (!strcmp (name, "restore_context"))
		code = wasm_restore_context;
	else if (!strcmp (name, "call_filter"))
		code = wasm_call_filter;
	else if (!strcmp (name, "throw_exception"))
		code = wasm_throw_exception;
	else if (!strcmp (name, "rethrow_exception"))
		code = wasm_rethrow_exception;
	else if (!strcmp (name, "throw_corlib_exception"))
		code = wasm_throw_corlib_exception;
	else if (!strcmp (name, "enter_icall_trampoline"))
		code = wasm_enter_icall_trampoline;

	g_assert (code);

	if (out_tinfo) {
		MonoTrampInfo *tinfo = g_new0 (MonoTrampInfo, 1);
		tinfo->code = code;
		tinfo->code_size = 1;
		tinfo->name = g_strdup (name);
		tinfo->ji = NULL;
		tinfo->unwind_ops = NULL;
		tinfo->uw_info = NULL;
		tinfo->uw_info_len = 0;
		tinfo->owns_uw_info = FALSE;

		*out_tinfo = tinfo;
	}

	return code;
}
#endif