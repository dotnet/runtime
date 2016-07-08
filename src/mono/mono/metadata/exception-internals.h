#ifndef _MONO_METADATA_EXCEPTION_INTERNALS_H_
#define _MONO_METADATA_EXCEPTION_INTERNALS_H_

#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/utils/mono-error.h>

MonoException *
mono_get_exception_type_initialization_checked (const gchar *type_name, MonoException *inner, MonoError *error);

MonoException *
mono_get_exception_reflection_type_load_checked (MonoArray *types, MonoArray *exceptions, MonoError *error);

MonoException *
mono_get_exception_runtime_wrapped_checked (MonoObject *wrapped_exception, MonoError *error);

MonoException *
mono_exception_from_name_two_strings_checked (MonoImage *image, const char *name_space,
					      const char *name, MonoString *a1, MonoString *a2,
					      MonoError *error);

MonoException *
mono_exception_from_token_two_strings_checked (MonoImage *image, uint32_t token,
					       MonoString *a1, MonoString *a2,
					       MonoError *error);

#endif
