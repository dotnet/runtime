#include <common/entrypoints.h>
#include "mono/metadata/native-library.h"

static Entry mono_qcalls[] = 
{
	DllImportEntry(NULL) // This NULL entry can be removed when a QCall is added to Mono (and added to this array)
};

gpointer
mono_lookup_pinvoke_qcall_internal (const char *name)
{
	return (gpointer)minipal_resolve_dllimport(mono_qcalls, lengthof(mono_qcalls), name);
}
