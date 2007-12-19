#include <config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/class-internals.h>
#include "mono-dbg.h"

guint32
mono_dbg_get_version (void)
{
	return MONO_DEBUGGER_VERSION;
}

gboolean
mono_dbg_read_generic_class (MonoDbgMemoryAccess memory, gconstpointer address,
			     MonoDbgGenericClass *result)
{
	MonoGenericClass gclass;

	if (!memory (address, &gclass, sizeof (MonoGenericClass)))
		return FALSE;

	result->container_class = gclass.container_class;
	result->generic_inst = gclass.context.class_inst;
	result->klass = gclass.cached_class;

	return TRUE;
}

gboolean
mono_dbg_read_generic_inst (MonoDbgMemoryAccess memory, gconstpointer address,
			    MonoDbgGenericInst *result)
{
	MonoGenericInst ginst;

	if (!memory (address, &ginst, sizeof (MonoGenericInst)))
		return FALSE;

	result->id = ginst.id;
	result->type_argc = ginst.type_argc;
	result->type_argv = g_new0 (gconstpointer, ginst.type_argc);

	if (!memory (ginst.type_argv, result->type_argv, ginst.type_argc * sizeof (gpointer)))
		return FALSE;

	return TRUE;
}
