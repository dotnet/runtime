#ifndef __MONO_ERROR_INTERNALS_H__
#define __MONO_ERROR_INTERNALS_H__

#include "mono/utils/mono-compiler.h"
#include "mono/metadata/object-internals.h"

/*Keep in sync with MonoError*/
typedef struct {
	unsigned short error_code;
    unsigned short flags;

	/*These name are suggestions of their content. MonoError internals might use them for something else.*/
	const char *type_name;
	const char *assembly_name;
	const char *member_name;
	const char *exception_name_space;
	const char *exception_name;
	MonoClass *klass;
	const char *full_message;
	const char *full_message_with_fields;

	void *padding [4];
    char message [128];
} MonoErrorInternal;

void
mono_error_dup_strings (MonoError *error, gboolean dup_strings);

/* This function is not very useful as you can't provide any details beyond the message.*/
void
mono_error_set_error (MonoError *error, int error_code, const char *msg_format, ...);

void
mono_error_set_assembly_load (MonoError *error, const char *assembly_name, const char *msg_format, ...);

void
mono_error_set_assembly_load_simple (MonoError *error, const char *assembly_name, gboolean refection_only);

void
mono_error_set_type_load_class (MonoError *error, MonoClass *klass, const char *msg_format, ...);

void
mono_error_set_type_load_name (MonoError *error, const char *type_name, const char *assembly_name, const char *msg_format, ...);

void
mono_error_set_method_load (MonoError *error, MonoClass *klass, const char *method_name, const char *msg_format, ...);

void
mono_error_set_field_load (MonoError *error, MonoClass *klass, const char *field_name, const char *msg_format, ...);

void
mono_error_set_bad_image (MonoError *error, MonoImage *image, const char *msg_format, ...);

void
mono_error_set_bad_image_name (MonoError *error, const char *file_name, const char *msg_format, ...);

void
mono_error_set_out_of_memory (MonoError *error, const char *msg_format, ...);

void
mono_error_set_argument (MonoError *error, const char *argument, const char *msg_format, ...);

void
mono_error_set_not_verifiable (MonoError *oerror, MonoMethod *method, const char *msg_format, ...);

void
mono_error_set_generic_error (MonoError *error, const char * name_space, const char *name, const char *msg_format, ...);

void
mono_error_set_from_loader_error (MonoError *error);

MonoException*
mono_error_prepare_exception (MonoError *error, MonoError *error_out);

MonoException*
mono_error_convert_to_exception (MonoError *error);

void
mono_error_raise_exception (MonoError *error);

void
mono_loader_set_error_from_mono_error (MonoError *oerror);

#endif
