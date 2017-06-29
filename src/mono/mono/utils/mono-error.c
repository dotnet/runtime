/**
 * \file
 * Error handling code
 *
 * Authors:
 *	Rodrigo Kumpera    (rkumpera@novell.com)
 * Copyright 2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <glib.h>

#include <config.h>
#include "mono-error.h"
#include "mono-error-internals.h"

#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object-internals.h>

#define set_error_messagev() do { \
	if (!(error->full_message = g_strdup_vprintf (msg_format, args))) \
			error->flags |= MONO_ERROR_INCOMPLETE; \
} while (0)

#define set_error_message() do { \
	va_list args; \
	va_start (args, msg_format); \
	set_error_messagev();	     \
	va_end (args); \
} while (0)

static void
mono_error_set_generic_errorv (MonoError *oerror, const char *name_space, const char *name, const char *msg_format, va_list args);

static gboolean
is_managed_exception (MonoErrorInternal *error)
{
	return (error->error_code == MONO_ERROR_EXCEPTION_INSTANCE);
}

static gboolean
is_boxed (MonoErrorInternal *error)
{
	return ((error->flags & MONO_ERROR_MEMPOOL_BOXED) != 0);
}

static void
mono_error_prepare (MonoErrorInternal *error)
{
	/* mono_error_set_* after a mono_error_cleanup without an intervening init */
	g_assert (error->error_code != MONO_ERROR_CLEANUP_CALLED_SENTINEL);
	if (error->error_code != MONO_ERROR_NONE)
		return;

	error->type_name = error->assembly_name = error->member_name = error->full_message = error->exception_name_space = error->exception_name = error->full_message_with_fields = error->first_argument = NULL;
	error->exn.klass = NULL;
}

static MonoClass*
get_class (MonoErrorInternal *error)
{
	MonoClass *klass = NULL;
	if (is_managed_exception (error))
		klass = mono_object_class (mono_gchandle_get_target (error->exn.instance_handle));
	else
		klass = error->exn.klass;
	return klass;
}

static const char*
get_type_name (MonoErrorInternal *error)
{
	if (error->type_name)
		return error->type_name;
	MonoClass *klass = get_class (error);
	if (klass)
		return klass->name;
	return "<unknown type>";
}

static const char*
get_assembly_name (MonoErrorInternal *error)
{
	if (error->assembly_name)
		return error->assembly_name;
	MonoClass *klass = get_class (error);
	if (klass && klass->image)
		return klass->image->name;
	return "<unknown assembly>";
}

void
mono_error_init_flags (MonoError *oerror, unsigned short flags)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	g_assert (sizeof (MonoError) == sizeof (MonoErrorInternal));

	error->error_code = MONO_ERROR_NONE;
	error->flags = flags;
}

/**
 * mono_error_init:
 * \param error Pointer to \c MonoError struct to initialize
 * Any function which takes a \c MonoError for purposes of reporting an error
 * is required to call either this or \c mono_error_init_flags on entry.
 */
void
mono_error_init (MonoError *error)
{
	mono_error_init_flags (error, 0);
}

void
mono_error_cleanup (MonoError *oerror)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	short int orig_error_code = error->error_code;
	gboolean free_strings = error->flags & MONO_ERROR_FREE_STRINGS;
	gboolean has_instance_handle = is_managed_exception (error);

	/* Two cleanups in a row without an intervening init. */
	g_assert (orig_error_code != MONO_ERROR_CLEANUP_CALLED_SENTINEL);
	/* Mempool stored error shouldn't be cleaned up */
	g_assert (!is_boxed (error));

	/* Mark it as cleaned up. */
	error->error_code = MONO_ERROR_CLEANUP_CALLED_SENTINEL;
	error->flags = 0;

	if (orig_error_code == MONO_ERROR_NONE)
		return;


	if (has_instance_handle)
		mono_gchandle_free (error->exn.instance_handle);


	g_free ((char*)error->full_message);
	g_free ((char*)error->full_message_with_fields);
	error->full_message = NULL;
	error->full_message_with_fields = NULL;
	if (!free_strings) //no memory was allocated
		return;

	g_free ((char*)error->type_name);
	g_free ((char*)error->assembly_name);
	g_free ((char*)error->member_name);
	g_free ((char*)error->exception_name_space);
	g_free ((char*)error->exception_name);
	g_free ((char*)error->first_argument);
	error->type_name = error->assembly_name = error->member_name = error->exception_name_space = error->exception_name = error->first_argument = NULL;
	error->exn.klass = NULL;

}

gboolean
mono_error_ok (MonoError *error)
{
	return error->error_code == MONO_ERROR_NONE;
}

void
mono_error_assert_ok_pos (MonoError *error, const char* filename, int lineno)
{
	if (mono_error_ok (error))
		return;

	g_error ("%s:%d: %s\n", filename, lineno, mono_error_get_message (error));
}

unsigned short
mono_error_get_error_code (MonoError *error)
{
	return error->error_code;
}

/*Return a pointer to the internal error message, might be NULL.
Caller should not release it.*/
const char*
mono_error_get_message (MonoError *oerror)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	if (error->error_code == MONO_ERROR_NONE)
		return NULL;
	if (error->full_message_with_fields)
		return error->full_message_with_fields;

	error->full_message_with_fields = g_strdup_printf ("%s assembly:%s type:%s member:%s",
		error->full_message,
		get_assembly_name (error),
		get_type_name (error),
		error->member_name ? error->member_name : "<none>");

	return error->full_message_with_fields ? error->full_message_with_fields : error->full_message;
}

/*
 * Inform that this error has heap allocated strings.
 * The strings will be duplicated if @dup_strings is TRUE
 * otherwise they will just be free'd in mono_error_cleanup.
 */
void
mono_error_dup_strings (MonoError *oerror, gboolean dup_strings)
{
#define DUP_STR(field) do { if (error->field) {\
	if (!(error->field = g_strdup (error->field))) \
		error->flags |= MONO_ERROR_INCOMPLETE; \
	}} while (0);

	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	error->flags |= MONO_ERROR_FREE_STRINGS;

	if (dup_strings) {
		DUP_STR (type_name);
		DUP_STR (assembly_name);
		DUP_STR (member_name);
		DUP_STR (exception_name_space);
		DUP_STR (exception_name);
		DUP_STR (first_argument);
	}
#undef DUP_STR
}

void
mono_error_set_error (MonoError *oerror, int error_code, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = error_code;
	set_error_message ();
}

static void
mono_error_set_assembly_name (MonoError *oerror, const char *assembly_name)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	g_assert (error->error_code != MONO_ERROR_NONE);

	error->assembly_name = assembly_name;
}

static void
mono_error_set_member_name (MonoError *oerror, const char *member_name)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	error->member_name = member_name;
}

static void
mono_error_set_type_name (MonoError *oerror, const char *type_name)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	error->type_name = type_name;
}

static void
mono_error_set_class (MonoError *oerror, MonoClass *klass)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	if (is_managed_exception (error))
		return;
	error->exn.klass = klass;	
}

static void
mono_error_set_corlib_exception (MonoError *oerror, const char *name_space, const char *name)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	error->exception_name_space = name_space;
	error->exception_name = name;
}


void
mono_error_set_assembly_load (MonoError *oerror, const char *assembly_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_FILE_NOT_FOUND;
	mono_error_set_assembly_name (oerror, assembly_name);

	set_error_message ();
}


void
mono_error_set_assembly_load_simple (MonoError *oerror, const char *assembly_name, gboolean refection_only)
{
	if (refection_only)
		mono_error_set_assembly_load (oerror, assembly_name, "Cannot resolve dependency to assembly because it has not been preloaded. When using the ReflectionOnly APIs, dependent assemblies must be pre-loaded or loaded on demand through the ReflectionOnlyAssemblyResolve event.");
	else
		mono_error_set_assembly_load (oerror, assembly_name, "Could not load file or assembly '%s' or one of its dependencies.", assembly_name);
}

void
mono_error_set_type_load_class (MonoError *oerror, MonoClass *klass, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_vset_type_load_class (oerror, klass, msg_format, args);
	va_end (args);
}

void
mono_error_vset_type_load_class (MonoError *oerror, MonoClass *klass, const char *msg_format, va_list args)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_TYPE_LOAD;
	mono_error_set_class (oerror, klass);
	set_error_messagev ();
}

/*
 * Different than other functions, this one here assumes that type_name and assembly_name to have been allocated just for us.
 * Which means mono_error_cleanup will free them.
 */
void
mono_error_set_type_load_name (MonoError *oerror, const char *type_name, const char *assembly_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_TYPE_LOAD;
	mono_error_set_type_name (oerror, type_name);
	mono_error_set_assembly_name (oerror, assembly_name);
	mono_error_dup_strings (oerror, FALSE);
	set_error_message ();
}

void
mono_error_set_method_load (MonoError *oerror, MonoClass *klass, const char *method_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_MISSING_METHOD;
	mono_error_set_class (oerror, klass);
	mono_error_set_member_name (oerror, method_name);
	set_error_message ();
}

void
mono_error_set_field_load (MonoError *oerror, MonoClass *klass, const char *field_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_MISSING_FIELD;
	mono_error_set_class (oerror, klass);
	mono_error_set_member_name (oerror, field_name);
	set_error_message ();	
}

void
mono_error_set_bad_image_name (MonoError *oerror, const char *assembly_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_BAD_IMAGE;
	mono_error_set_assembly_name (oerror, assembly_name);
	set_error_message ();
}

void
mono_error_set_bad_image (MonoError *oerror, MonoImage *image, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_BAD_IMAGE;
	error->assembly_name = image ? mono_image_get_name (image) : "<no_image>";
	set_error_message ();
}

void
mono_error_set_generic_errorv (MonoError *oerror, const char *name_space, const char *name, const char *msg_format, va_list args)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_GENERIC;
	mono_error_set_corlib_exception (oerror, name_space, name);
	set_error_messagev ();
}

void
mono_error_set_generic_error (MonoError *oerror, const char * name_space, const char *name, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, name_space, name, msg_format, args);
	va_end (args);
}

/**
 * mono_error_set_not_implemented:
 *
 * System.NotImplementedException
 */
void
mono_error_set_not_implemented (MonoError *oerror, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, "System", "NotImplementedException", msg_format, args);
	va_end (args);
}

/**
 * mono_error_set_execution_engine:
 *
 * System.ExecutionEngineException
 */
void
mono_error_set_execution_engine (MonoError *oerror, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, "System", "ExecutionEngineException", msg_format, args);
	va_end (args);
}

/**
 * mono_error_set_not_supported:
 *
 * System.NotSupportedException
 */
void
mono_error_set_not_supported (MonoError *oerror, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, "System", "NotSupportedException", msg_format, args);
	va_end (args);
}

/**
 * mono_error_set_invalid_operation:
 *
 * System.InvalidOperationException
 */
void
mono_error_set_invalid_operation (MonoError *oerror, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, "System", "InvalidOperationException", msg_format, args);
	va_end (args);
}

/**
 * mono_error_set_file_not_found:
 *
 * System.IO.FileNotFoundException
 */
void
mono_error_set_file_not_found (MonoError *oerror, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, "System.IO", "FileNotFoundException", msg_format, args);
	va_end (args);
}

void
mono_error_set_invalid_program (MonoError *oerror, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	mono_error_prepare (error);
	error->error_code = MONO_ERROR_INVALID_PROGRAM;

	set_error_message ();
}

/**
 * mono_error_set_invalid_cast:
 *
 * System.InvalidCastException
 */
void
mono_error_set_invalid_cast (MonoError *oerror)
{
        mono_error_set_generic_error (oerror, "System", "InvalidCastException", "");
}

void
mono_error_set_exception_instance (MonoError *oerror, MonoException *exc)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	mono_error_prepare (error);
	error->error_code = MONO_ERROR_EXCEPTION_INSTANCE;
	error->exn.instance_handle = mono_gchandle_new (exc ? &exc->object : NULL, FALSE);
}

void
mono_error_set_exception_handle (MonoError *oerror, MonoExceptionHandle exc)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	mono_error_prepare (error);
	error->error_code = MONO_ERROR_EXCEPTION_INSTANCE;
	error->exn.instance_handle = mono_gchandle_from_handle (MONO_HANDLE_CAST(MonoObject, exc), FALSE);
}

void
mono_error_set_out_of_memory (MonoError *oerror, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_OUT_OF_MEMORY;

	set_error_message ();
}

void
mono_error_set_argument (MonoError *oerror, const char *argument, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_ARGUMENT;
	error->first_argument = argument;

	set_error_message ();
}

void
mono_error_set_argument_null (MonoError *oerror, const char *argument, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_ARGUMENT_NULL;
	error->first_argument = argument;

	set_error_message ();
}

void
mono_error_set_not_verifiable (MonoError *oerror, MonoMethod *method, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_NOT_VERIFIABLE;
	if (method) {
		mono_error_set_class (oerror, method->klass);
		mono_error_set_member_name (oerror, mono_method_full_name (method, 1));
	}

	set_error_message ();
}


/* Used by mono_error_prepare_exception - it sets its own error on mono_string_new_checked failure. */
static MonoString*
string_new_cleanup (MonoDomain *domain, const char *text)
{
	MonoError ignored_err;
	MonoString *result = mono_string_new_checked (domain, text, &ignored_err);
	mono_error_cleanup (&ignored_err);
	return result;
}

static MonoString*
get_type_name_as_mono_string (MonoErrorInternal *error, MonoDomain *domain, MonoError *error_out)
{
	MonoString* res = NULL;

	if (error->type_name) {
		res = string_new_cleanup (domain, error->type_name);
		
	} else {
		MonoClass *klass = get_class (error);
		if (klass) {
			char *name = mono_type_full_name (&klass->byval_arg);
			if (name) {
				res = string_new_cleanup (domain, name);
				g_free (name);
			}
		}
	}
	if (!res)
		mono_error_set_out_of_memory (error_out, "Could not allocate type name");
	return res;
}

static void
set_message_on_exception (MonoException *exception, MonoErrorInternal *error, MonoError *error_out)
{
	MonoString *msg = string_new_cleanup (mono_domain_get (), error->full_message);
	if (msg)
		MONO_OBJECT_SETREF (exception, message, msg);
	else
		mono_error_set_out_of_memory (error_out, "Could not allocate exception object");
}

/*Can fail with out-of-memory*/
MonoException*
mono_error_prepare_exception (MonoError *oerror, MonoError *error_out)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	MonoException* exception = NULL;
	MonoString *assembly_name = NULL, *type_name = NULL, *method_name = NULL, *field_name = NULL, *msg = NULL;
	MonoDomain *domain = mono_domain_get ();

	error_init (error_out);

	switch (error->error_code) {
	case MONO_ERROR_NONE:
		return NULL;

	case MONO_ERROR_MISSING_METHOD:
		if ((error->type_name || error->exn.klass) && error->member_name) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;

			method_name = string_new_cleanup (domain, error->member_name);
			if (!method_name) {
				mono_error_set_out_of_memory (error_out, "Could not allocate method name");
				break;
			}

			exception = mono_exception_from_name_two_strings_checked (mono_defaults.corlib, "System", "MissingMethodException", type_name, method_name, error_out);
			if (exception)
				set_message_on_exception (exception, error, error_out);
		} else {
			exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingMethodException", error->full_message);
		}
		break;

	case MONO_ERROR_MISSING_FIELD:
		if ((error->type_name || error->exn.klass) && error->member_name) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;
			
			field_name = string_new_cleanup (domain, error->member_name);
			if (!field_name) {
				mono_error_set_out_of_memory (error_out, "Could not allocate field name");
				break;
			}
			
			exception = mono_exception_from_name_two_strings_checked (mono_defaults.corlib, "System", "MissingFieldException", type_name, field_name, error_out);
			if (exception)
				set_message_on_exception (exception, error, error_out);
		} else {
			exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingFieldException", error->full_message);
		}
		break;

	case MONO_ERROR_TYPE_LOAD:
		if ((error->type_name && error->assembly_name) || error->exn.klass) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;

			if (error->assembly_name) {
				assembly_name = string_new_cleanup (domain, error->assembly_name);
				if (!assembly_name) {
					mono_error_set_out_of_memory (error_out, "Could not allocate assembly name");
					break;
				}
			} else {
				assembly_name = mono_string_empty (domain);
			}

			exception = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System", "TypeLoadException", type_name, assembly_name, error_out);
			if (exception && error->full_message != NULL && strcmp (error->full_message, ""))
				set_message_on_exception (exception, error, error_out);
		} else {
			exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "TypeLoadException", error->full_message);
		}
		break;

	case MONO_ERROR_FILE_NOT_FOUND:
	case MONO_ERROR_BAD_IMAGE:
		if (error->assembly_name) {
			msg = string_new_cleanup (domain, error->full_message);
			if (!msg) {
				mono_error_set_out_of_memory (error_out, "Could not allocate message");
				break;
			}

			if (error->assembly_name) {
				assembly_name = string_new_cleanup (domain, error->assembly_name);
				if (!assembly_name) {
					mono_error_set_out_of_memory (error_out, "Could not allocate assembly name");
					break;
				}
			}

			if (error->error_code == MONO_ERROR_FILE_NOT_FOUND)
				exception = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System.IO", "FileNotFoundException", msg, assembly_name, error_out);
			else
				exception = mono_exception_from_name_two_strings_checked (mono_defaults.corlib, "System", "BadImageFormatException", msg, assembly_name, error_out);
		} else {
			if (error->error_code == MONO_ERROR_FILE_NOT_FOUND)
				exception = mono_exception_from_name_msg (mono_get_corlib (), "System.IO", "FileNotFoundException", error->full_message);
			else
				exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "BadImageFormatException", error->full_message);
		}
		break;

	case MONO_ERROR_OUT_OF_MEMORY:
		exception = mono_get_exception_out_of_memory ();
		break;

	case MONO_ERROR_ARGUMENT:
		exception = mono_get_exception_argument (error->first_argument, error->full_message);
		break;

	case MONO_ERROR_ARGUMENT_NULL:
		exception = mono_get_exception_argument_null (error->first_argument);
		break;

	case MONO_ERROR_NOT_VERIFIABLE: {
		char *type_name = NULL, *message;
		if (error->exn.klass) {
			type_name = mono_type_get_full_name (error->exn.klass);
			if (!type_name) {
				mono_error_set_out_of_memory (error_out, "Could not allocate message");
				break;
			}
		}
		message = g_strdup_printf ("Error in %s:%s %s", type_name, error->member_name, error->full_message);
		if (!message) {
			g_free (type_name);
			mono_error_set_out_of_memory (error_out, "Could not allocate message");
			break;	
		}
		exception = mono_exception_from_name_msg (mono_defaults.corlib, "System.Security", "VerificationException", message);
		g_free (message);
		g_free (type_name);
		break;
	}
	case MONO_ERROR_GENERIC:
		if (!error->exception_name_space || !error->exception_name)
			mono_error_set_execution_engine (error_out, "MonoError with generic error but no exception name was supplied");
		else
			exception = mono_exception_from_name_msg (mono_defaults.corlib, error->exception_name_space, error->exception_name, error->full_message);
		break;

	case MONO_ERROR_EXCEPTION_INSTANCE:
		exception = (MonoException*) mono_gchandle_get_target (error->exn.instance_handle);
		break;

	case MONO_ERROR_CLEANUP_CALLED_SENTINEL:
		mono_error_set_execution_engine (error_out, "MonoError reused after mono_error_cleanup");
		break;

	case MONO_ERROR_INVALID_PROGRAM: {
		gboolean lacks_message = error->flags & MONO_ERROR_INCOMPLETE;
		if (lacks_message)
			return mono_exception_from_name_msg (mono_defaults.corlib, "System", "InvalidProgramException", "");
		else
			return mono_exception_from_name_msg (mono_defaults.corlib, "System", "InvalidProgramException", error->full_message);
	}
	default:
		mono_error_set_execution_engine (error_out, "Invalid error-code %d", error->error_code);
	}

	if (!mono_error_ok (error_out))
		return NULL;
	if (!exception)
		mono_error_set_out_of_memory (error_out, "Could not allocate exception object");
	return exception;
}

/*
Convert this MonoError to an exception if it's faulty or return NULL.
The error object is cleant after.
*/

MonoException*
mono_error_convert_to_exception (MonoError *target_error)
{
	MonoError error;
	MonoException *ex;

	/* Mempool stored error shouldn't be cleaned up */
	g_assert (!is_boxed ((MonoErrorInternal*)target_error));

	if (mono_error_ok (target_error))
		return NULL;

	ex = mono_error_prepare_exception (target_error, &error);
	if (!mono_error_ok (&error)) {
		MonoError second_chance;
		/*Try to produce the exception for the second error. FIXME maybe we should log about the original one*/
		ex = mono_error_prepare_exception (&error, &second_chance);

		g_assert (mono_error_ok (&second_chance)); /*We can't reasonable handle double faults, maybe later.*/
		mono_error_cleanup (&error);
	}
	mono_error_cleanup (target_error);
	return ex;
}

void
mono_error_move (MonoError *dest, MonoError *src)
{
	memcpy (dest, src, sizeof (MonoErrorInternal));
	error_init (src);
}

/**
 * mono_error_box:
 * \param ierror The input error that will be boxed.
 * \param image The mempool of this image will hold the boxed error.
 * Creates a new boxed error in the given mempool from \c MonoError.
 * It does not alter \p ierror, so you still have to clean it up with
 * \c mono_error_cleanup or \c mono_error_convert_to_exception or another such function.
 * \returns the boxed error, or NULL if the mempool could not allocate.
 */
MonoErrorBoxed*
mono_error_box (const MonoError *ierror, MonoImage *image)
{
	MonoErrorInternal *from = (MonoErrorInternal*)ierror;
	/* Don't know how to box a gchandle */
	g_assert (!is_managed_exception (from));
	MonoErrorBoxed* box = mono_image_alloc (image, sizeof (MonoErrorBoxed));
	box->image = image;
	mono_error_init_flags (&box->error, MONO_ERROR_MEMPOOL_BOXED);
	MonoErrorInternal *to = (MonoErrorInternal*)&box->error;

#define DUP_STR(field) do {						\
		if (from->field) {					\
			if (!(to->field = mono_image_strdup (image, from->field))) \
				to->flags |= MONO_ERROR_INCOMPLETE;	\
		} else {						\
			to->field = NULL;				\
		}							\
	} while (0)

	to->error_code = from->error_code;
	DUP_STR (type_name);
	DUP_STR (assembly_name);
	DUP_STR (member_name);
	DUP_STR (exception_name_space);
	DUP_STR (exception_name);
	DUP_STR (full_message);
	DUP_STR (full_message_with_fields);
	DUP_STR (first_argument);
	to->exn.klass = from->exn.klass;

#undef DUP_STR
	
	return box;
}


/**
 * mono_error_set_from_boxed:
 * \param oerror The error that will be set to the contents of the box.
 * \param box A mempool-allocated error.
 * Sets the error condition in the oerror from the contents of the
 * given boxed error.  Does not alter the boxed error, so it can be
 * used in a future call to \c mono_error_set_from_boxed as needed.  The
 * \p oerror should've been previously initialized with \c mono_error_init,
 * as usual.
 * \returns TRUE on success or FALSE on failure.
 */
gboolean
mono_error_set_from_boxed (MonoError *oerror, const MonoErrorBoxed *box)
{
	MonoErrorInternal* to = (MonoErrorInternal*)oerror;
	MonoErrorInternal* from = (MonoErrorInternal*)&box->error;
	g_assert (!is_managed_exception (from));

	mono_error_prepare (to);
	to->flags |= MONO_ERROR_FREE_STRINGS;
#define DUP_STR(field)	do {						\
		if (from->field) {					\
			if (!(to->field = g_strdup (from->field)))	\
				to->flags |= MONO_ERROR_INCOMPLETE;	\
		} else {						\
			to->field = NULL;				\
		}							\
	} while (0)

	to->error_code = from->error_code;
	DUP_STR (type_name);
	DUP_STR (assembly_name);
	DUP_STR (member_name);
	DUP_STR (exception_name_space);
	DUP_STR (exception_name);
	DUP_STR (full_message);
	DUP_STR (full_message_with_fields);
	DUP_STR (first_argument);
	to->exn.klass = from->exn.klass;
		  
#undef DUP_STR
	return (to->flags & MONO_ERROR_INCOMPLETE) == 0 ;
}
