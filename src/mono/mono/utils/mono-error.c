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
	if (msg_format && !(error->full_message = g_strdup_vprintf (msg_format, args))) \
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
is_managed_error_code (guint16 error_code)
{
	return error_code == MONO_ERROR_EXCEPTION_INSTANCE;
}

static gboolean
is_managed_exception (MonoErrorInternal *error)
{
	return is_managed_error_code (error->error_code);
}

static gboolean
is_boxed_error_flags (guint16 error_flags)
{
	return (error_flags & MONO_ERROR_MEMPOOL_BOXED) != 0;
}

static gboolean
is_boxed (MonoErrorInternal *error)
{
	return is_boxed_error_flags (error->flags);
}

static void
mono_error_free_string (const char **error_string)
{
	g_free ((char*)*error_string);
	*error_string = NULL;
}

static void
mono_error_init_deferred (MonoErrorInternal *error)
// mono_error_init and mono_error_init_flags are optimized and only initialize a minimum.
// Initialize the rest, prior to originating an error.
{
	memset (&error->type_name, 0, sizeof (*error) - offsetof (MonoErrorInternal, type_name));
}

static void
mono_error_prepare (MonoErrorInternal *error)
{
	/* mono_error_set_* after a mono_error_cleanup without an intervening init */
	g_assert (error->error_code != MONO_ERROR_CLEANUP_CALLED_SENTINEL);
	if (error->error_code != MONO_ERROR_NONE)
		return;

	mono_error_init_deferred (error);
}

static MonoClass*
get_class (MonoErrorInternal *error)
{
	MonoClass *klass = NULL;
	if (is_managed_exception (error))
		klass = mono_object_class (mono_gchandle_get_target_internal (error->exn.instance_handle));
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
		return m_class_get_name (klass);
	return "<unknown type>";
}

static const char*
get_assembly_name (MonoErrorInternal *error)
{
	if (error->assembly_name)
		return error->assembly_name;
	MonoClass *klass = get_class (error);
	if (klass && m_class_get_image (klass))
		return m_class_get_image (klass)->name;
	return "<unknown assembly>";
}

void
mono_error_init_flags (MonoError *oerror, guint16 flags)
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
	// This function is called a lot so it is optimized.

	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	const guint32 init = oerror->init;
	const guint16 error_code = (guint16)(init & 0xFFFF);
	const guint16 error_flags = (guint16)(init >> 16);
#else
	const guint16 error_code = error->error_code;
	const guint16 error_flags = error->flags;
#endif
	/* Two cleanups in a row without an intervening init. */
	g_assert (error_code != MONO_ERROR_CLEANUP_CALLED_SENTINEL);

	/* Mempool stored error shouldn't be cleaned up */
	g_assert (!is_boxed_error_flags (error_flags));

	/* Mark it as cleaned up. */
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	oerror->init = MONO_ERROR_CLEANUP_CALLED_SENTINEL;
#else
	error->error_code = MONO_ERROR_CLEANUP_CALLED_SENTINEL;
	error->flags = 0;
#endif
	if (error_code == MONO_ERROR_NONE)
		return;

	if (is_managed_error_code (error_code))
		mono_gchandle_free_internal (error->exn.instance_handle);

	mono_error_free_string (&error->full_message);
	mono_error_free_string (&error->full_message_with_fields);

	if (!(error_flags & MONO_ERROR_FREE_STRINGS)) //no memory was allocated
		return;

	mono_error_free_string (&error->type_name);
	mono_error_free_string (&error->assembly_name);
	mono_error_free_string (&error->member_name);
	mono_error_free_string (&error->exception_name_space);
	mono_error_free_string (&error->exception_name);
	mono_error_free_string (&error->first_argument);
	error->exn.klass = NULL;
}

gboolean
mono_error_ok (MonoError *error)
{
	return error->error_code == MONO_ERROR_NONE;
}

guint16
mono_error_get_error_code (MonoError *error)
{
	return error->error_code;
}

const char*
mono_error_get_exception_name (MonoError *oerror)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	if (error->error_code == MONO_ERROR_NONE)
		return NULL;

	return error->exception_name;
}

/*Return a pointer to the internal error message, might be NULL.
Caller should not release it.*/
const char*
mono_error_get_message (MonoError *oerror)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	const guint16 error_code = error->error_code;
	if (error_code == MONO_ERROR_NONE)
		return NULL;

	g_assert (error_code != MONO_ERROR_CLEANUP_CALLED_SENTINEL);

	//Those are the simplified errors
	switch (error_code) {
	case MONO_ERROR_MISSING_METHOD:
	case MONO_ERROR_BAD_IMAGE:
	case MONO_ERROR_FILE_NOT_FOUND:
	case MONO_ERROR_MISSING_FIELD:
		return error->full_message;
	}

	if (error->full_message_with_fields)
		return error->full_message_with_fields;

	error->full_message_with_fields = g_strdup_printf ("%s assembly:%s type:%s member:%s",
		error->full_message,
		get_assembly_name (error),
		get_type_name (error),
		error->member_name);

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

/*
 * Sets @error to be of type @error_code with message @message
 * XXX only works for MONO_ERROR_MISSING_METHOD, MONO_ERROR_BAD_IMAGE, MONO_ERROR_FILE_NOT_FOUND and MONO_ERROR_MISSING_FIELD for now
*/
void
mono_error_set_specific (MonoError *oerror, int error_code, const char *message)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = error_code;
	error->full_message = message;
	error->flags |= MONO_ERROR_FREE_STRINGS;
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
 * mono_error_set_ambiguous_implementation:
 *
 * System.Runtime.AmbiguousImplementationException
 */
void
mono_error_set_ambiguous_implementation (MonoError *oerror, const char *msg_format, ...)
{
	va_list args;
	va_start (args, msg_format);
	mono_error_set_generic_errorv (oerror, "System.Runtime", "AmbiguousImplementationException", msg_format, args);
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

void
mono_error_set_invalid_program (MonoError *oerror, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	mono_error_prepare (error);
	error->error_code = MONO_ERROR_INVALID_PROGRAM;

	set_error_message ();
}

void
mono_error_set_member_access (MonoError *oerror, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	mono_error_prepare (error);
	error->error_code = MONO_ERROR_MEMBER_ACCESS;

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
	error->exn.instance_handle = mono_gchandle_new_internal (exc ? &exc->object : NULL, FALSE);
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
mono_error_set_argument_format (MonoError *oerror, const char *argument, const char *msg_format, ...)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_ARGUMENT;
	error->first_argument = argument;

	set_error_message ();
}

void
mono_error_set_argument (MonoError *oerror, const char *argument, const char *msg)
{
	MonoErrorInternal *error = (MonoErrorInternal*)oerror;
	mono_error_prepare (error);

	error->error_code = MONO_ERROR_ARGUMENT;
	error->first_argument = argument;
	if (msg && msg [0] && !(error->full_message = g_strdup (msg)))
		error->flags |= MONO_ERROR_INCOMPLETE;
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
static MonoStringHandle
string_new_cleanup (MonoDomain *domain, const char *text)
{
	ERROR_DECL (ignored_err);
	MonoStringHandle result = mono_string_new_handle (domain, text, ignored_err);
	mono_error_cleanup (ignored_err);
	return result;
}

static MonoStringHandle
get_type_name_as_mono_string (MonoErrorInternal *error, MonoDomain *domain, MonoError *error_out)
{
	HANDLE_FUNCTION_ENTER ();

	MonoStringHandle res = NULL_HANDLE_STRING;

	if (error->type_name) {
		res = string_new_cleanup (domain, error->type_name);
	} else {
		MonoClass *klass = get_class (error);
		if (klass) {
			char *name = mono_type_full_name (m_class_get_byval_arg (klass));
			if (name) {
				res = string_new_cleanup (domain, name);
				g_free (name);
			}
		}
	}
	if (MONO_HANDLE_IS_NULL (res))
		mono_error_set_out_of_memory (error_out, "Could not allocate type name");
	HANDLE_FUNCTION_RETURN_REF (MonoString, res);
}

#if 0
static MonoExceptionHandle
mono_error_prepare_exception_handle (MonoError *oerror, MonoError *error_out)
// Can fail with out-of-memory
{
	HANDLE_FUNCTION_ENTER ();
	MonoExceptionHandle ex = MONO_HANDLE_NEW (MonoException, mono_error_prepare_exception (oerror, error_out));
	HANDLE_FUNCTION_RETURN_REF (MonoException, ex);
}
#endif

/*Can fail with out-of-memory*/
MonoException*
mono_error_prepare_exception (MonoError *oerror, MonoError *error_out)
{
	HANDLE_FUNCTION_ENTER ();

	MonoErrorInternal *error = (MonoErrorInternal*)oerror;

	MonoExceptionHandle exception = MONO_HANDLE_CAST (MonoException, mono_new_null ());
	MonoDomain *domain = mono_domain_get ();
	char *type_name = NULL;
	char *message = NULL;

	error_init (error_out);

	const guint16 error_code = error->error_code;

	g_assert (error_code != MONO_ERROR_CLEANUP_CALLED_SENTINEL);

	switch (error_code) {
	case MONO_ERROR_NONE:
		goto exit;

	case MONO_ERROR_MISSING_METHOD:
		exception = mono_corlib_exception_new_with_args ("System", "MissingMethodException", error->full_message, error->first_argument, error_out);
		break;
	case MONO_ERROR_BAD_IMAGE:
		exception = mono_corlib_exception_new_with_args ("System", "BadImageFormatException", error->full_message, error->first_argument, error_out);
		break;
	case MONO_ERROR_FILE_NOT_FOUND:
		exception = mono_corlib_exception_new_with_args ("System.IO", "FileNotFoundException", error->full_message, error->first_argument, error_out);
		break;
	case MONO_ERROR_MISSING_FIELD:
		exception = mono_corlib_exception_new_with_args ("System", "MissingFieldException", error->full_message, error->first_argument, error_out);
		break;
	case MONO_ERROR_MEMBER_ACCESS:
		exception = mono_exception_new_by_name_msg (mono_defaults.corlib, "System", "MemberAccessException", error->full_message, error_out);
		break;

	case MONO_ERROR_TYPE_LOAD: {
		MonoStringHandle assembly_name;
		MonoStringHandle type_name;

		if ((error->type_name && error->assembly_name) || error->exn.klass) {
			type_name = get_type_name_as_mono_string (error, domain, error_out);
			if (!mono_error_ok (error_out))
				break;

			if (error->assembly_name) {
				assembly_name = string_new_cleanup (domain, error->assembly_name);
				if (MONO_HANDLE_IS_NULL (assembly_name)) {
					mono_error_set_out_of_memory (error_out, "Could not allocate assembly name");
					break;
				}
			} else {
				assembly_name = mono_string_empty_handle (domain);
			}

			exception = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System", "TypeLoadException", type_name, assembly_name, error_out);
			if (!MONO_HANDLE_IS_NULL (exception)) {
				const char *full_message = error->full_message;
				if (full_message && full_message [0]) {
					MonoStringHandle msg = string_new_cleanup (mono_domain_get (), full_message);	
					if (!MONO_HANDLE_IS_NULL (msg))
						MONO_HANDLE_SET (exception, message, msg);
					else
						mono_error_set_out_of_memory (error_out, "Could not allocate exception object");
				}
			}
		} else {
			exception = mono_exception_new_by_name_msg (mono_defaults.corlib, "System", "TypeLoadException", error->full_message, error_out);
		}
	}
	break;

	case MONO_ERROR_OUT_OF_MEMORY:
		if (domain)
			exception = MONO_HANDLE_NEW (MonoException, domain->out_of_memory_ex);
		if (MONO_HANDLE_IS_NULL (exception))
			exception = mono_get_exception_out_of_memory_handle ();
		break;

	case MONO_ERROR_ARGUMENT:
		exception = mono_exception_new_argument (error->first_argument, error->full_message, error_out);
		break;

	case MONO_ERROR_ARGUMENT_NULL:
		exception = mono_exception_new_argument_null (error->first_argument, error_out);
		break;
	
	case MONO_ERROR_ARGUMENT_OUT_OF_RANGE: 
		exception = mono_exception_new_argument_out_of_range(error->first_argument, error->full_message, error_out); 
		break;

	case MONO_ERROR_NOT_VERIFIABLE:
		if (error->exn.klass) {
			type_name = mono_type_get_full_name (error->exn.klass);
			if (!type_name)
				goto out_of_memory;
		}
		message = g_strdup_printf ("Error in %s:%s %s", type_name, error->member_name, error->full_message);
		if (!message)
			goto out_of_memory;
		exception = mono_exception_new_by_name_msg (mono_defaults.corlib, "System.Security", "VerificationException", message, error_out);
		break;

	case MONO_ERROR_GENERIC:
		if (!error->exception_name_space || !error->exception_name)
			mono_error_set_execution_engine (error_out, "MonoError with generic error but no exception name was supplied");
		else
			exception = mono_exception_new_by_name_msg (mono_defaults.corlib, error->exception_name_space, error->exception_name, error->full_message, error_out);
		break;

	case MONO_ERROR_EXCEPTION_INSTANCE:
		exception = MONO_HANDLE_CAST (MonoException, mono_gchandle_get_target_handle (error->exn.instance_handle));
		break;

	case MONO_ERROR_CLEANUP_CALLED_SENTINEL:
		mono_error_set_execution_engine (error_out, "MonoError reused after mono_error_cleanup");
		break;

	case MONO_ERROR_INVALID_PROGRAM:
		exception = mono_exception_new_by_name_msg (mono_defaults.corlib, "System", "InvalidProgramException",
			(error->flags & MONO_ERROR_INCOMPLETE) ? "" : error->full_message, error_out);
		break;

	default:
		mono_error_set_execution_engine (error_out, "Invalid error-code %d", error->error_code);
	}

	if (!mono_error_ok (error_out))
		goto return_null;

	if (MONO_HANDLE_IS_NULL (exception))
		mono_error_set_out_of_memory (error_out, "Could not allocate exception object");
	goto exit;
out_of_memory:
	mono_error_set_out_of_memory (error_out, "Could not allocate message");
	goto exit;
return_null:
	exception = MONO_HANDLE_CAST (MonoException, mono_new_null ());
exit:
	g_free (message);
	g_free (type_name);
	HANDLE_FUNCTION_RETURN_OBJ (exception);
}

/*
Convert this MonoError to an exception if it's faulty or return NULL.
The error object is cleant after.
*/
MonoException*
mono_error_convert_to_exception (MonoError *target_error)
{
	ERROR_DECL (error);
	MonoException *ex;

	/* Mempool stored error shouldn't be cleaned up */
	g_assert (!is_boxed ((MonoErrorInternal*)target_error));

	if (mono_error_ok (target_error))
		return NULL;

	ex = mono_error_prepare_exception (target_error, error);
	if (!mono_error_ok (error)) {
		ERROR_DECL (second_chance);
		/*Try to produce the exception for the second error. FIXME maybe we should log about the original one*/
		ex = mono_error_prepare_exception (error, second_chance);

		// We cannot reasonably handle double faults, maybe later.
		g_assert (mono_error_ok (second_chance));
		mono_error_cleanup (error);
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
	MonoErrorBoxed* box = (MonoErrorBoxed*)mono_image_alloc (image, sizeof (MonoErrorBoxed));
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

void
mono_error_set_first_argument (MonoError *oerror, const char *first_argument)
{
	MonoErrorInternal* to = (MonoErrorInternal*)oerror;
	to->first_argument = g_strdup (first_argument);
	to->flags |= MONO_ERROR_FREE_STRINGS;
}
