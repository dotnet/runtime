/**
* \file
*/

#ifndef _MONO_METADATA_LOADER_INTERNALS_H_
#define _MONO_METADATA_LOADER_INTERNALS_H_

#include <glib.h>
#include <mono/metadata/object-forward.h>

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, const char **exc_class, const char **exc_arg);

#endif
