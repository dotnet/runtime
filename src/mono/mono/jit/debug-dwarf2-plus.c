#include <stdlib.h>
#include <string.h>
#include <mono/metadata/class.h>
#include <mono/metadata/debug-symfile.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"

static MonoDebugMethodInfo *
method_info_func (MonoDebugSymbolFile *symfile, guint32 token, gpointer user_data)
{
	AssemblyDebugInfo *info = user_data;
	MonoMethod *method;
	DebugMethodInfo *minfo;

#if 0
	method = g_hash_table_lookup (info->image->method_cache, GINT_TO_POINTER (token));
#else
	method = mono_get_method (info->image, token, NULL);
#endif
	if (!method)
		return NULL;

	minfo = g_hash_table_lookup (info->handle->methods, method);

	return (MonoDebugMethodInfo *) minfo;
}

void
mono_debug_open_assembly_dwarf2_plus (AssemblyDebugInfo *info)
{
	char *buf;

	buf = g_strdup_printf ("as %s -o %s", info->filename, info->objfile);
	system (buf);
	g_free (buf);

	mono_jit_compile_image (info->image, FALSE);

	info->symfile = mono_debug_open_symbol_file (info->image, info->objfile, TRUE);
}

void
mono_debug_close_assembly_dwarf2_plus (AssemblyDebugInfo *info)
{
	if (info->symfile)
		mono_debug_close_symbol_file (info->symfile);
}

void
mono_debug_write_assembly_dwarf2_plus (AssemblyDebugInfo *info)
{
	if (info->symfile)
		mono_debug_update_symbol_file (info->symfile, method_info_func, info);
}
