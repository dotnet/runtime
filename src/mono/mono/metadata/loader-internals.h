/**
* \file
*/

#ifndef _MONO_METADATA_LOADER_INTERNALS_H_
#define _MONO_METADATA_LOADER_INTERNALS_H_

#include <glib.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-error.h>

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, MonoError *error);

#ifdef ENABLE_NETCORE
void
mono_set_pinvoke_search_directories (int dir_count, char **dirs);
#endif

#endif
