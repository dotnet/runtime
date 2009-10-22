/*
 * mono-error.c: Error handling code
 *
 * Authors:
 *	Rodrigo Kumpera    (rkumpera@novell.com)
 * Copyright 2009 Novell, Inc (http://www.novell.com)
 */
#include <glib.h>

#include "mono-error.h"
#include "mono-error-internals.h"

#include <mono/metadata/exception.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/debug-helpers.h>

#define mono_internal_error_get_message(E) ((E)->full_message ? (E)->full_message : (E)->message)

#define set_error_message() do { \
	va_list args; \
	va_start (args, msg_format); \
	if (g_vsnprintf (error->message, sizeof (error->message), msg_format, args) >= sizeof (error->message)) {\
		if (!(error->full_message = g_strdup_vprintf (msg_format, args))) \
			error->flags |= MONO_ERROR_INCOMPLETE; \
	} \
	va_end (args); \
} while (0)

static void
mono_error_prepare (MonoErrorInternal *error)
{
	if (error->error_code != MONO_ERROR_NONE)
		return;

	error->type_name = error->assembly_name = error->member_name = error->full_message = error->exception_name_space = error->exception_name = NULL;
	error->klass = NULL;
	error->message [0] = 0;
}

void
mono_error_init_flags (MonoError *oerror, unsigned short flags)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	g_assert (sizeof (MonoError) == sizeof (MonoErrorInternal));

	error->error_code = MONO_ERROR_NONE;
	error->flags = flags;
}

void
mono_error_init (MonoError *error)
{
	mono_error_init_flags (error, 0);
}

void
mono_error_cleanup (MonoError *oerror)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	if (error->error_code == MONO_ERROR_NONE)
		return;

	g_free ((char*)error->full_message);
	if (!(error->flags & MONO_ERROR_FREE_STRINGS)) //no memory was allocated
		return;

	g_free ((char*)error->type_name);
	g_free ((char*)error->assembly_name);
	g_free ((char*)error->member_name);
	g_free ((char*)error->exception_name_space);
	g_free ((char*)error->exception_name);
}

gboolean
mono_error_ok (MonoError *error)
{
	return error->error_code == MONO_ERROR_NONE;
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
	return mono_internal_error_get_message (error);
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

	error->klass = klass;	
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
mono_error_set_type_load_class (MonoError *oerror, MonoClass *klass, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_TYPE_LOAD;
	mono_error_set_class (oerror, klass);
	set_error_message ();
}

void
mono_error_set_type_load_name (MonoError *oerror, const char *type_name, const char *assembly_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_TYPE_LOAD;
	mono_error_set_type_name (oerror, type_name);
	mono_error_set_assembly_name (oerror, assembly_name);
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
mono_error_set_bad_image (MonoError *oerror, const char *assembly_name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_BAD_IMAGE;
	mono_error_set_assembly_name (oerror, assembly_name);
	set_error_message ();
}

void
mono_error_set_generic_error (MonoError *oerror, const char * name_space, const char *name, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_GENERIC;
	mono_error_set_corlib_exception (oerror, name_space, name);
	set_error_message ();
}

void
mono_error_set_out_of_memory (MonoError *oerror, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	va_list args;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_OUT_OF_MEMORY;
	va_start (args, msg_format);
	g_vsnprintf (error->message, sizeof (error->message), msg_format, args);
	va_end (args);
}

static MonoString*
get_type_name_as_mono_string (MonoErrorInternal *error, MonoDomain *domain, MonoError *error_out)
{
	MonoString* res = NULL;

	if (error->type_name) {
		res = mono_string_new (domain, error->type_name);
		
	} else if (error->klass) {
		char *name = mono_type_full_name (&error->klass->byval_arg);
		if (name) {
			res = mono_string_new (domain, name);
			g_free (name);
		}
	}
	if (!res)
		mono_error_set_out_of_memory (error_out, "Could not allocate type name");
	return res;
}

static void
set_message_on_exception (MonoException *exception, MonoErrorInternal *error, MonoError *error_out)
{
	MonoString *msg = mono_string_new (mono_domain_get (), mono_internal_error_get_message (error));
	if (msg)
		MONO_OBJECT_SETREF (exception, message, msg);
	else
		mono_error_set_out_of_memory (error_out, "Could not allocate exception object");
}

MonoException*
mono_error_prepare_exception (MonoError *oerror, MonoError *error_out)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	MonoException* exception = NULL;
	MonoString *assembly_name = NULL, *type_name = NULL, *method_name = NULL, *field_name = NULL, *msg = NULL;
	MonoDomain *domain = mono_domain_get ();


	switch (error->error_code) {
	case MONO_ERROR_NONE:
		return NULL;

	case MONO_ERROR_MISSING_METHOD:
		if ((error->type_name || error->klass) && error->member_name) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;

			method_name = mono_string_new (domain, error->member_name);
			if (!method_name) {
				mono_error_set_out_of_memory (error_out, "Could not allocate method name");
				break;
			}

			exception = mono_exception_from_name_two_strings (mono_defaults.corlib, "System", "MissingMethodException", type_name, method_name);
			if (exception)
				set_message_on_exception (exception, error, error_out);
		} else {
		 	exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingMethodException", mono_internal_error_get_message (error));
		}
		break;

	case MONO_ERROR_MISSING_FIELD:
		if ((error->type_name || error->klass) && error->member_name) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;
			
			field_name = mono_string_new (domain, error->member_name);
			if (!field_name) {
				mono_error_set_out_of_memory (error_out, "Could not allocate field name");
				break;
			}
			
			exception = mono_exception_from_name_two_strings (mono_defaults.corlib, "System", "MissingFieldException", type_name, field_name);
			if (exception)
				set_message_on_exception (exception, error, error_out);
		} else {
		 	exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingFieldException", mono_internal_error_get_message (error));
		}
		break;

	case MONO_ERROR_TYPE_LOAD:
		if (error->type_name || error->assembly_name) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;

			if (error->assembly_name) {
				assembly_name = mono_string_new (domain, error->assembly_name);
				if (!assembly_name) {
					mono_error_set_out_of_memory (error_out, "Could not allocate assembly name");
					break;
				}
			}

			exception = mono_exception_from_name_two_strings (mono_get_corlib (), "System", "TypeLoadException", type_name, assembly_name);
			if (exception)
				set_message_on_exception (exception, error, error_out);
		} else {
		 	exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "TypeLoadException", mono_internal_error_get_message (error));
		}
		break;

	case MONO_ERROR_FILE_NOT_FOUND:
	case MONO_ERROR_BAD_IMAGE:
		if (error->assembly_name) {
			msg = mono_string_new (domain, mono_internal_error_get_message (error));
			if (!msg) {
				mono_error_set_out_of_memory (error_out, "Could not allocate message");
				break;
			}

			if (error->assembly_name) {
				assembly_name = mono_string_new (domain, error->assembly_name);
				if (!assembly_name) {
					mono_error_set_out_of_memory (error_out, "Could not allocate assembly name");
					break;
				}
			}

			if (error->error_code == MONO_ERROR_FILE_NOT_FOUND)
				exception = mono_exception_from_name_two_strings (mono_get_corlib (), "System.IO", "FileNotFoundException", msg, assembly_name);
			else
				exception = mono_exception_from_name_two_strings (mono_defaults.corlib, "System", "BadImageFormatException", msg, assembly_name);
		} else {
			if (error->error_code == MONO_ERROR_FILE_NOT_FOUND)
				exception = mono_exception_from_name_msg (mono_get_corlib (), "System.IO", "FileNotFoundException", mono_internal_error_get_message (error));
			else
				exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "BadImageFormatException", mono_internal_error_get_message (error));
		}
		break;

	case MONO_ERROR_OUT_OF_MEMORY:
		exception = mono_get_exception_out_of_memory ();
		break;

	case MONO_ERROR_GENERIC:
		if (!error->exception_name_space || !error->exception_name)
			mono_error_set_generic_error (error_out, "System", "ExecutionEngineException", "MonoError with generic error but no exception name was supplied");
		else
			exception = mono_exception_from_name_msg (mono_defaults.corlib, error->exception_name_space, error->exception_name, mono_internal_error_get_message (error));
		break;

	default:
		mono_error_set_generic_error (error_out, "System", "ExecutionEngineException", "Invalid error-code %d", error->error_code);
	}

	if (!mono_error_ok (error_out))
		return NULL;
	if (!exception)
		mono_error_set_out_of_memory (error_out, "Could not allocate exception object");
	return exception;
}
