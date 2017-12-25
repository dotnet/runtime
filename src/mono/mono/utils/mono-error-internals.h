/**
 * \file
 */

#ifndef __MONO_ERROR_INTERNALS_H__
#define __MONO_ERROR_INTERNALS_H__

#include "mono/utils/mono-compiler.h"
#include "mono/metadata/class-internals.h"

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
	union {
		/* Valid if error_code != MONO_ERROR_EXCEPTION_INSTANCE.
		 * Used by type or field load errors and generic error specified by class.
		 */
		MonoClass *klass;
		/* Valid if error_code == MONO_ERROR_EXCEPTION_INSTANCE.
		 * Generic error specified by a managed instance.
		 */
		uint32_t instance_handle;
	} exn;
	const char *full_message;
	const char *full_message_with_fields;
	const char *first_argument;
	const char *member_signature;

	void *padding [2];
} MonoErrorInternal;

/* Invariant: the error strings are allocated in the mempool of the given image */
struct _MonoErrorBoxed {
	MonoError error;
	MonoImage *image;
};

#define error_init(error) do {	\
	((MonoErrorInternal*)(error))->error_code = MONO_ERROR_NONE;	\
	((MonoErrorInternal*)(error))->flags = 0;	\
} while (0);

#define is_ok(error) ((error)->error_code == MONO_ERROR_NONE)

#define return_if_nok(error) do { if (!is_ok ((error))) return; } while (0)
#define return_val_if_nok(error,val) do { if (!is_ok ((error))) return (val); } while (0)

#define goto_if_nok(error,label) do { if (!is_ok ((error))) goto label; } while (0)

/* Only use this in icalls */
#define return_val_and_set_pending_if_nok(error,value)	\
	if (mono_error_set_pending_exception ((error)))	\
		return (value);

void
mono_error_assert_ok_pos (MonoError *error, const char* filename, int lineno) MONO_LLVM_INTERNAL;

#define mono_error_assert_ok(e) mono_error_assert_ok_pos (e, __FILE__, __LINE__)

void
mono_error_dup_strings (MonoError *error, gboolean dup_strings);

/* This function is not very useful as you can't provide any details beyond the message.*/
void
mono_error_set_error (MonoError *error, int error_code, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_assembly_load (MonoError *error, const char *assembly_name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_assembly_load_simple (MonoError *error, const char *assembly_name, gboolean refection_only);

void
mono_error_set_type_load_class (MonoError *error, MonoClass *klass, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_vset_type_load_class (MonoError *error, MonoClass *klass, const char *msg_format, va_list args);

void
mono_error_set_type_load_name (MonoError *error, const char *type_name, const char *assembly_name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(4,5);

void
mono_error_set_method_load (MonoError *oerror, MonoClass *klass, const char *method_name, const char *signature, const char *msg_format, ...);

void
mono_error_set_field_load (MonoError *error, MonoClass *klass, const char *field_name, const char *msg_format, ...)  MONO_ATTR_FORMAT_PRINTF(4,5);

void
mono_error_set_bad_image (MonoError *error, MonoImage *image, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_bad_image_name (MonoError *error, const char *file_name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_out_of_memory (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_argument (MonoError *error, const char *argument, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_argument_null (MonoError *oerror, const char *argument, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_not_verifiable (MonoError *oerror, MonoMethod *method, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(3,4);

void
mono_error_set_generic_error (MonoError *error, const char * name_space, const char *name, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(4,5);

void
mono_error_set_execution_engine (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_not_implemented (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_not_supported (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_invalid_operation (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_file_not_found (MonoError *error, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_exception_instance (MonoError *error, MonoException *exc);

void
mono_error_set_invalid_program (MonoError *oerror, const char *msg_format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

void
mono_error_set_invalid_cast (MonoError *oerror);

MonoException*
mono_error_prepare_exception (MonoError *error, MonoError *error_out);

MonoException*
mono_error_convert_to_exception (MonoError *error);

void
mono_error_move (MonoError *dest, MonoError *src);

MonoErrorBoxed*
mono_error_box (const MonoError *error, MonoImage *image);

gboolean
mono_error_set_from_boxed (MonoError *error, const MonoErrorBoxed *from);

const char*
mono_error_get_exception_name (MonoError *oerror);

#endif
