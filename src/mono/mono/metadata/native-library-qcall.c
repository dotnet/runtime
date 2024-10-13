#include <minipal/entrypoints.h>
#include "mono/metadata/native-library.h"

static Entry mono_qcalls[] =
{
	{"NULL", NULL}, // This NULL entry can be removed when a QCall is added to Mono (and added to this array)
};

gpointer
mono_lookup_pinvoke_qcall_internal (const char *name)
{
	return (gpointer)minipal_resolve_dllimport(mono_qcalls, G_N_ELEMENTS(mono_qcalls), name);
}
