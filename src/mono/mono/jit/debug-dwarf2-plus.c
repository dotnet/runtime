#include <stdlib.h>
#include <string.h>
#include <mono/metadata/class.h>
#include <mono/metadata/debug-symfile.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"

void
mono_debug_open_assembly_dwarf2_plus (AssemblyDebugInfo *info)
{
}

void
mono_debug_close_assembly_dwarf2_plus (AssemblyDebugInfo *info)
{
}

static MonoDebugMethodInfo *
method_info_func (MonoDebugSymbolFile *symfile, guint32 token, gpointer user_data)
{
	AssemblyDebugInfo *info = user_data;
	MonoMethod *method;
	DebugMethodInfo *minfo;

	method = g_hash_table_lookup (info->image->method_cache, GINT_TO_POINTER (token));
	if (!method)
		return NULL;

	minfo = g_hash_table_lookup (info->methods, method);

	return (MonoDebugMethodInfo *) minfo;
}

void
mono_debug_write_assembly_dwarf2_plus (AssemblyDebugInfo *info)
{
	MonoDebugSymbolFile *symfile;

	symfile = mono_debug_open_symbol_file (info->image, info->filename, TRUE);
	if (!symfile)
		return;

	mono_debug_update_symbol_file (symfile, method_info_func, info);

	mono_debug_close_symbol_file (symfile);
}
