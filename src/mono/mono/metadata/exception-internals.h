/**
 * \file
 */

#ifndef _MONO_METADATA_EXCEPTION_INTERNALS_H_
#define _MONO_METADATA_EXCEPTION_INTERNALS_H_

#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/metadata/handle.h>
#include <mono/utils/mono-error.h>

MonoException *
mono_get_exception_type_initialization_checked (const gchar *type_name, MonoException *inner, MonoError *error);

MonoExceptionHandle
mono_get_exception_reflection_type_load_checked (MonoArrayHandle types, MonoArrayHandle exceptions, MonoError *error);

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

typedef int (*MonoGetSeqPointFunc) (MonoDomain *domain, MonoMethod *method, gint32 native_offset);

void
mono_install_get_seq_point (MonoGetSeqPointFunc func);

void
mono_error_set_method_missing (MonoError *error, MonoClass *klass, const char *method_name, MonoMethodSignature *sig, const char *reason, ...) MONO_ATTR_FORMAT_PRINTF(5,6);

void
mono_error_set_field_missing (MonoError *oerror, MonoClass *klass, const char *field_name, MonoType *sig, const char *reason, ...) MONO_ATTR_FORMAT_PRINTF(5,6);

void
mono_error_set_bad_image (MonoError *error, MonoImage *image, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_bad_image_by_name (MonoError *error, const char *image_name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_file_not_found (MonoError *oerror, const char *file_name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_simple_file_not_found (MonoError *oerror, const char *assembly_name, gboolean refection_only);

MonoException *
mono_corlib_exception_new_with_args (const char *name_space, const char *name, const char *arg_0, const char *arg_1, MonoError *error);

MonoExceptionHandle
mono_exception_new_argument (const char *arg, const char *msg, MonoError *error);

MonoExceptionHandle
mono_exception_new_thread_interrupted (MonoError *error);

MonoExceptionHandle
mono_exception_new_thread_interrupted (MonoError *error);

MonoExceptionHandle
mono_exception_new_thread_abort (MonoError *error);

MonoExceptionHandle
mono_exception_new_thread_state (const char *msg, MonoError *error);

#endif
