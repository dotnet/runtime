/**
 * \file
 * Exception handling
 *
 * Authors:
 *	Paolo Molaro    (lupus@ximian.com)
 *	Dietmar Maurer  (dietmar@ximian.com)
 *	Dick Porter     (dick@ximian.com)
 *      Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <glib.h>
#include <config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/jit-info.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <string.h>
#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif
#include "class-init.h"
#include "icall-decl.h"

static MonoUnhandledExceptionFunc unhandled_exception_hook = NULL;
static gpointer unhandled_exception_hook_data = NULL;

static MonoExceptionHandle
mono_exception_new_argument_internal (const char *type, const char *arg, const char *msg, MonoError *error);

/**
 * mono_exception_new_by_name:
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 *
 * Creates an exception of the given namespace/name class in the
 * current domain.
 *
 * \returns the initialized exception instance.
 */
static MonoExceptionHandle
mono_exception_new_by_name (MonoImage *image, const char *name_space, const char *name, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass * const klass = mono_class_load_from_name (image, name_space, name);

	MonoObjectHandle o = mono_object_new_handle (klass, error);
	goto_if_nok (error, return_null);

	mono_runtime_object_init_handle (o, error);
	mono_error_assert_ok (error);

	goto_if_ok (error, exit);
return_null:
	MONO_HANDLE_ASSIGN (o, NULL_HANDLE);
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoException, MONO_HANDLE_CAST (MonoException, o));
}

/**
 * mono_exception_from_name:
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 *
 * Creates an exception of the given namespace/name class in the
 * current domain.
 *
 * \returns the initialized exception instance.
 */
MonoException *
mono_exception_from_name (MonoImage *image, const char *name_space,
			  const char *name)
{
	return mono_exception_from_name_domain (mono_domain_get (), image, name_space, name);
}

/**
 * mono_exception_from_name_domain:
 * \param domain Domain where the return object will be created.
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 *
 * Creates an exception object of the given namespace/name class on
 * the given domain.
 *
 * \returns the initialized exception instance.
 */
MonoException *
mono_exception_from_name_domain (MonoDomain *domain, MonoImage *image,
				 const char* name_space, const char *name)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoExceptionHandle ret = mono_exception_new_by_name (image, name_space, name, error);
	mono_error_assert_ok (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_exception_from_token:
 * \param image the Mono image where to look for the class
 * \param token The type token of the class
 *
 * Creates an exception of the type given by \p token.
 *
 * \returns the initialized exception instance.
 */
MonoException *
mono_exception_from_token (MonoImage *image, guint32 token)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoClass *klass;

	klass = mono_class_get_checked (image, token, error);
	mono_error_assert_ok (error);

	MonoObjectHandle o = mono_object_new_handle (klass, error);
	mono_error_assert_ok (error);

	mono_runtime_object_init_handle (o, error);
	mono_error_assert_ok (error);

	HANDLE_FUNCTION_RETURN_OBJ (MONO_HANDLE_CAST (MonoException, o));
}

static MonoExceptionHandle
create_exception_two_strings (MonoClass *klass, MonoStringHandle a1, MonoStringHandle a2, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoMethod *method = NULL;
	int const count = 1 + !MONO_HANDLE_IS_NULL (a2);
	gpointer iter;
	MonoMethod *m;

	MonoObjectHandle o = mono_object_new_handle (klass, error);
	mono_error_assert_ok (error);

	iter = NULL;
	while ((m = mono_class_get_methods (klass, &iter))) {
		MonoMethodSignature *sig;

		if (strcmp (".ctor", mono_method_get_name (m)))
			continue;
		sig = mono_method_signature_internal (m);
		if (sig->param_count != count)
			continue;

		if (sig->params [0]->type != MONO_TYPE_STRING)
			continue;
		if (count == 2 && sig->params [1]->type != MONO_TYPE_STRING)
			continue;
		method = m;
		break;
	}
	g_assert (method);

	gpointer args [ ] = { MONO_HANDLE_RAW (a1), MONO_HANDLE_RAW (a2) };

	mono_runtime_invoke_handle_void (method, o, args, error);
	if (!is_ok (error))
		o = mono_new_null ();

	HANDLE_FUNCTION_RETURN_REF (MonoException, MONO_HANDLE_CAST (MonoException, o));
}

/**
 * mono_exception_from_name_two_strings:
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 * \param a1 first string argument to pass
 * \param a2 second string argument to pass
 *
 * Creates an exception from a constructor that takes two string
 * arguments.
 *
 * \returns the initialized exception instance.
 */
MonoException *
mono_exception_from_name_two_strings (MonoImage *image, const char *name_space,
				      const char *name, MonoString *a1_raw, MonoString *a2_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoString, a1);
	MONO_HANDLE_DCL (MonoString, a2);
	MonoExceptionHandle ret = mono_exception_from_name_two_strings_checked (image, name_space, name, a1, a2, error);
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_exception_from_name_two_strings_checked:
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 * \param a1 first string argument to pass
 * \param a2 second string argument to pass
 * \param error set on error
 *
 * Creates an exception from a constructor that takes two string
 * arguments.
 *
 * \returns the initialized exception instance. On failure returns
 * NULL and sets \p error.
 */
MonoExceptionHandle
mono_exception_from_name_two_strings_checked (MonoImage *image, const char *name_space,
					      const char *name, MonoStringHandle a1, MonoStringHandle a2,
					      MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *klass;

	error_init (error);
	klass = mono_class_load_from_name (image, name_space, name);

	HANDLE_FUNCTION_RETURN_REF (MonoException, create_exception_two_strings (klass, a1, a2, error));
}

/**
 * mono_exception_new_by_name_msg:
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 * \param msg the message to embed inside the exception
 *
 * Creates an exception and initializes its message field.
 *
 * \returns the initialized exception instance.
 */
MonoExceptionHandle
mono_exception_new_by_name_msg (MonoImage *image, const char *name_space,
			      const char *name, const char *msg, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoExceptionHandle ex = mono_exception_new_by_name (image, name_space, name, error);
	goto_if_nok (error, return_null);

	if (msg) {
		MonoStringHandle msg_str = mono_string_new_handle (msg, error);
		// FIXME? Maybe just ignore this error, the exception is close to correct.
		goto_if_nok (error, return_null);
		// ex->message = msg_str;
		MONO_HANDLE_SET (ex, message, msg_str);
	}
	goto exit;
return_null:
	MONO_HANDLE_ASSIGN (ex, NULL_HANDLE);
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoException, ex)
}

/**
 * mono_exception_from_name_msg:
 * \param image the Mono image where to look for the class
 * \param name_space the namespace for the class
 * \param name class name
 * \param msg the message to embed inside the exception
 *
 * Creates an exception and initializes its message field.
 *
 * \returns the initialized exception instance.
 */
MonoException *
mono_exception_from_name_msg (MonoImage *image, const char *name_space,
			      const char *name, const char *msg)
{
	HANDLE_FUNCTION_ENTER ();
	MonoExceptionHandle ex;
	MONO_ENTER_GC_UNSAFE;
	ERROR_DECL (error);
	ex = mono_exception_new_by_name_msg (image, name_space, name, msg, error);
	mono_error_cleanup (error);
	MONO_EXIT_GC_UNSAFE;
	HANDLE_FUNCTION_RETURN_OBJ (ex);
}

/**
 * mono_exception_from_token_two_strings:
 *
 *   Same as mono_exception_from_name_two_strings, but lookup the exception class using
 * IMAGE and TOKEN.
 */
MonoException *
mono_exception_from_token_two_strings (MonoImage *image, guint32 token, MonoString *arg1_raw, MonoString *arg2_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoString, arg1);
	MONO_HANDLE_DCL (MonoString, arg2);
	MonoExceptionHandle ret = mono_exception_from_token_two_strings_checked (image, token, arg1, arg2, error);
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_exception_from_token_two_strings_checked:
 *
 *   Same as mono_exception_from_name_two_strings, but lookup the exception class using
 * IMAGE and TOKEN.
 */
MonoExceptionHandle
mono_exception_from_token_two_strings_checked (MonoImage *image, guint32 token,
					       MonoStringHandle a1, MonoStringHandle a2,
					       MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *klass;

	error_init (error);

	klass = mono_class_get_checked (image, token, error);
	mono_error_assert_ok (error); /* FIXME handle the error. */

	HANDLE_FUNCTION_RETURN_REF (MonoException, create_exception_two_strings (klass, a1, a2, error));
}

/**
 * mono_get_exception_divide_by_zero:
 * \returns a new instance of the \c System.DivideByZeroException
 */
MonoException *
mono_get_exception_divide_by_zero (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "DivideByZeroException");
}

/**
 * mono_get_exception_security:
 * \returns a new instance of the \c System.Security.SecurityException
 */
MonoException *
mono_get_exception_security (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System.Security",
					 "SecurityException");
}

/**
 * mono_exception_new_thread_abort:
 * \returns a new instance of the \c System.Threading.ThreadAbortException
 */
MonoExceptionHandle
mono_exception_new_thread_abort (MonoError *error)
{
	return mono_exception_new_by_name (mono_get_corlib (), "System.Threading", "ThreadAbortException", error);
}

/**
 * mono_get_exception_thread_abort:
 * \returns a new instance of the \c System.Threading.ThreadAbortException
 */
MonoException *
mono_get_exception_thread_abort (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System.Threading",
					 "ThreadAbortException");
}

/**
 * mono_exception_new_thread_interrupted:
 * \returns a new instance of the \c System.Threading.ThreadInterruptedException
 */
MonoExceptionHandle
mono_exception_new_thread_interrupted (MonoError *error)
{
	return mono_exception_new_by_name (mono_get_corlib (), "System.Threading", "ThreadInterruptedException", error);
}

/**
 * mono_get_exception_thread_interrupted:
 * \returns a new instance of the \c System.Threading.ThreadInterruptedException
 */
MonoException *
mono_get_exception_thread_interrupted (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System.Threading",
					 "ThreadInterruptedException");
}

/**
 * mono_get_exception_arithmetic:
 * \returns a new instance of the \c System.ArithmeticException
 */
MonoException *
mono_get_exception_arithmetic (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "ArithmeticException");
}

/**
 * mono_get_exception_overflow:
 * \returns a new instance of the \c System.OverflowException
 */
MonoException *
mono_get_exception_overflow (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "OverflowException");
}

/**
 * mono_get_exception_null_reference:
 * \returns a new instance of the \c System.NullReferenceException
 */
MonoException *
mono_get_exception_null_reference (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "NullReferenceException");
}

/**
 * mono_get_exception_execution_engine:
 * \param msg the message to pass to the user
 * \returns a new instance of the \c System.ExecutionEngineException
 */
MonoException *
mono_get_exception_execution_engine (const char *msg)
{
	MonoException *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_exception_from_name_msg (mono_get_corlib (), "System", "ExecutionEngineException", msg);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_get_exception_serialization:
 * \param msg the message to pass to the user
 * \returns a new instance of the \c System.Runtime.Serialization.SerializationException
 */
MonoException *
mono_get_exception_serialization (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System.Runtime.Serialization", "SerializationException", msg);
}

/**
 * mono_get_exception_invalid_cast:
 * \returns a new instance of the \c System.InvalidCastException
 */
MonoException *
mono_get_exception_invalid_cast ()
{
	return mono_exception_from_name (mono_get_corlib (), "System", "InvalidCastException");
}

/**
 * mono_get_exception_invalid_operation:
 * \param msg the message to pass to the user
 * \returns a new instance of the \c System.InvalidOperationException
 */
MonoException *
mono_get_exception_invalid_operation (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System",
					"InvalidOperationException", msg);
}

MonoExceptionHandle
mono_exception_new_invalid_operation (const char *msg, MonoError *error)
{
	return mono_exception_new_by_name_msg (mono_get_corlib (), "System",
					"InvalidOperationException", msg, error);
}

/**
 * mono_get_exception_index_out_of_range:
 * \returns a new instance of the \c System.IndexOutOfRangeException
 */
MonoException *
mono_get_exception_index_out_of_range ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "IndexOutOfRangeException");
}

/**
 * mono_get_exception_array_type_mismatch:
 * \returns a new instance of the \c System.ArrayTypeMismatchException
 */
MonoException *
mono_get_exception_array_type_mismatch (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "ArrayTypeMismatchException");
}

/**
 * mono_get_exception_type_load:
 * \param class_name the name of the class that could not be loaded
 * \param assembly_name the assembly where the class was looked up.
 * \returns a new instance of the \c System.TypeLoadException
 */
MonoException *
mono_get_exception_type_load (MonoString *class_name_raw, char *assembly_name)
{
	ERROR_DECL (error);
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoString, class_name);
	MonoStringHandle s = NULL_HANDLE_STRING;
	if (assembly_name) {
		s = mono_string_new_handle (assembly_name, error);
		mono_error_assert_ok (error);
	} else
		s = mono_string_empty_handle ();

	MonoExceptionHandle ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System",
								   "TypeLoadException", class_name, s, error);
	mono_error_assert_ok (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_get_exception_not_implemented:
 * \param msg the message to pass to the user
 * \returns a new instance of the \c System.NotImplementedException
 */
MonoException *
mono_get_exception_not_implemented (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "NotImplementedException", msg);
}

/**
 * mono_get_exception_not_supported:
 * \param msg the message to pass to the user
 * \returns a new instance of the \c System.NotSupportedException
 */
MonoException *
mono_get_exception_not_supported (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "NotSupportedException", msg);
}

/**
 * mono_get_exception_missing_member:
 * \param exception_type the specific exception type for the specific member type, i.e. field or method
 * \param class_name the class where the lookup was performed.
 * \param member_name the name of the missing method.
 * \returns a new instance of the \c exception_type (MissingFieldException or MissingMethodException)
 */
static MonoException*
mono_get_exception_missing_member (const char *exception_type, const char *class_name, const char *member_name)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoStringHandle s1 = mono_string_new_handle (class_name, error);
	mono_error_assert_ok (error);
	MonoStringHandle s2 = mono_string_new_handle (member_name, error);
	mono_error_assert_ok (error);

	MonoExceptionHandle ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System",
									   exception_type, s1, s2, error);
	mono_error_assert_ok (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_get_exception_missing_method:
 * \param class_name the class where the lookup was performed.
 * \param member_name the name of the missing method.
 * \returns a new instance of the \c System.MissingMethodException
 */
MonoException *
mono_get_exception_missing_method (const char *class_name, const char *member_name)
{
	return mono_get_exception_missing_member ("MissingMethodException", class_name, member_name);
}

/**
 * mono_get_exception_missing_field:
 * \param class_name the class where the lookup was performed
 * \param member_name the name of the missing field.
 * \returns a new instance of the \c System.MissingFieldException
 */
MonoException *
mono_get_exception_missing_field (const char *class_name, const char *member_name)
{
	return mono_get_exception_missing_member ("MissingFieldException", class_name, member_name);
}

/**
 * mono_get_exception_argument_internal:
 * \param type the actual type
 * \param arg the name of the argument that is invalid or null, etc.
 * \param msg optional message
 * \returns a new instance of the \c System.ArgumentException or derived
 */
static MonoException*
mono_get_exception_argument_internal (const char *type, const char *arg, const char *msg)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoExceptionHandle ex = mono_exception_new_argument_internal (type, arg, msg, error);
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (ex);
}

/**
 * mono_get_exception_argument_null:
 * \param arg the name of the argument that is null
 * \returns a new instance of the \c System.ArgumentNullException
 */
MonoException*
mono_get_exception_argument_null (const char *arg)
{
	MonoException *ex;
	MONO_ENTER_GC_UNSAFE;
	ex = mono_get_exception_argument_internal ("ArgumentNullException", arg, NULL);
	MONO_EXIT_GC_UNSAFE;
	return ex;
}

/**
 * mono_get_exception_argument:
 * \param arg the name of the invalid argument.
 * \returns a new instance of the \c System.ArgumentException
 */
MonoException *
mono_get_exception_argument (const char *arg, const char *msg)
{
	return mono_get_exception_argument_internal ("ArgumentException", arg, msg);
}

static MonoExceptionHandle
mono_exception_new_argument_internal (const char *type, const char *arg, const char *msg, MonoError *error)
{
	MonoStringHandle arg_str = arg ? mono_string_new_handle (arg, error) : NULL_HANDLE_STRING;
	MonoStringHandle msg_str = msg ? mono_string_new_handle (msg, error) : NULL_HANDLE_STRING;

	if (!strcmp (type, "ArgumentException"))
		return mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System", type, msg_str, arg_str, error);
	else
		return mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System", type, arg_str, msg_str, error);
}

MonoExceptionHandle
mono_exception_new_argument (const char *arg, const char *msg, MonoError *error)
{
	return mono_exception_new_argument_internal ("ArgumentException", arg, msg, error);
}

MonoExceptionHandle
mono_exception_new_argument_null (const char *arg, MonoError *error)
{
	return mono_exception_new_argument_internal ("ArgumentNullException", arg, NULL, error);
}

MonoExceptionHandle
mono_exception_new_argument_out_of_range(const char *arg, const char *msg, MonoError *error)
{
	return mono_exception_new_argument_internal ("ArgumentOutOfRangeException", arg, msg, error);
}

MonoExceptionHandle
mono_exception_new_serialization (const char *msg, MonoError *error)
{
	return mono_exception_new_by_name_msg (mono_get_corlib (),
		"System.Runtime.Serialization", "SerializationException",
		"Could not serialize unhandled exception.", error);
}

/**
 * mono_get_exception_argument_out_of_range:
 * \param arg the name of the out of range argument.
 * \returns a new instance of the \c System.ArgumentOutOfRangeException
 */
MonoException *
mono_get_exception_argument_out_of_range (const char *arg)
{
	return mono_get_exception_argument_internal ("ArgumentOutOfRangeException", arg, NULL);
}

/**
 * mono_get_exception_thread_state:
 * \param msg the message to present to the user
 * \returns a new instance of the \c System.Threading.ThreadStateException
 */
MonoException *
mono_get_exception_thread_state (const char *msg)
{
	return mono_exception_from_name_msg (
		mono_get_corlib (), "System.Threading", "ThreadStateException", msg);
}

/**
 * mono_get_exception_io:
 * \param msg the message to present to the user
 * \returns a new instance of the \c System.IO.IOException
 */
MonoException *
mono_get_exception_io (const char *msg)
{
	return mono_exception_from_name_msg (
		mono_get_corlib (), "System.IO", "IOException", msg);
}

/**
 * mono_get_exception_file_not_found:
 * \param fname the name of the file not found.
 * \returns a new instance of the \c System.IO.FileNotFoundException
 */
MonoException *
mono_get_exception_file_not_found (MonoString *fname_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoString, fname);
	MonoExceptionHandle ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System.IO", "FileNotFoundException", fname, fname, error);
	mono_error_assert_ok (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_get_exception_file_not_found2:
 * \param msg an informative message for the user.
 * \param fname the name of the file not found.
 * \returns a new instance of the \c System.IO.FileNotFoundException
 */
MonoException *
mono_get_exception_file_not_found2 (const char *msg, MonoString *fname_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoString, fname);
	MonoStringHandle s = NULL_HANDLE_STRING;
	if (msg) {
		s = mono_string_new_handle (msg, error);
		mono_error_assert_ok (error);
	}
	MonoExceptionHandle ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System.IO", "FileNotFoundException", s, fname, error);
	mono_error_assert_ok (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_get_exception_type_initialization:
 * \param type_name the name of the type that failed to initialize.
 * \param inner the inner exception.
 * \returns a new instance of the \c System.TypeInitializationException
 */
MonoException *
mono_get_exception_type_initialization (const gchar *type_name, MonoException* inner_raw)
{
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoException, inner);
	ERROR_DECL (error);
	MonoExceptionHandle ret = mono_get_exception_type_initialization_handle (type_name, inner, error);
	if (!is_ok (error)) {
		ret = MONO_HANDLE_CAST (MonoException, mono_new_null ());
		mono_error_cleanup (error);
	}
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

MonoExceptionHandle
mono_get_exception_type_initialization_handle (const gchar *type_name, MonoExceptionHandle inner, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *klass;
	MonoMethod *method;
	gpointer iter;

	error_init (error);

	klass = mono_class_load_from_name (mono_get_corlib (), "System", "TypeInitializationException");

	mono_class_init_internal (klass);

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		if (!strcmp (".ctor", mono_method_get_name (method))) {
			MonoMethodSignature *sig = mono_method_signature_internal (method);

			if (sig->param_count == 2 && sig->params [0]->type == MONO_TYPE_STRING && mono_class_from_mono_type_internal (sig->params [1]) == mono_defaults.exception_class)
				break;
		}
		method = NULL;
	}
	g_assert (method);

	MonoStringHandle type_name_str = mono_string_new_handle (type_name, error);
	mono_error_assert_ok (error);
	gpointer args [ ] = { MONO_HANDLE_RAW (type_name_str), MONO_HANDLE_RAW (inner) };

	MonoObjectHandle exc = mono_object_new_handle (klass, error);
	mono_error_assert_ok (error);

	mono_runtime_invoke_handle_void (method, exc, args, error);
	goto_if_nok (error, return_null);
	goto exit;
return_null:
	exc = mono_new_null ();
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoException, MONO_HANDLE_CAST (MonoException, exc));
}

/**
 * mono_get_exception_synchronization_lock:
 * \param inner the inner exception.
 * \returns a new instance of the \c System.SynchronizationLockException
 */
MonoException *
mono_get_exception_synchronization_lock (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System.Threading", "SynchronizationLockException", msg);
}

/**
 * mono_get_exception_cannot_unload_appdomain:
 * \param inner the inner exception.
 * \returns a new instance of the \c System.CannotUnloadAppDomainException
 */
MonoException *
mono_get_exception_cannot_unload_appdomain (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "CannotUnloadAppDomainException", msg);
}

/**
 * mono_get_exception_appdomain_unloaded
 * \returns a new instance of the \c System.AppDomainUnloadedException
 */
MonoException *
mono_get_exception_appdomain_unloaded (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "AppDomainUnloadedException");
}

/**
 * mono_get_exception_bad_image_format:
 * \param msg an informative message for the user.
 * \returns a new instance of the \c System.BadImageFormatException
 */
MonoException *
mono_get_exception_bad_image_format (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "BadImageFormatException", msg);
}

/**
 * mono_get_exception_bad_image_format2:
 * \param msg an informative message for the user.
 * \param fname The full name of the file with the invalid image.
 * \returns a new instance of the \c System.BadImageFormatException
 */
MonoException *
mono_get_exception_bad_image_format2 (const char *msg, MonoString *fname_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoStringHandle s = NULL_HANDLE_STRING;
	MONO_HANDLE_DCL (MonoString, fname);

	if (msg) {
		s = mono_string_new_handle (msg, error);
		mono_error_assert_ok (error);
	}

	MonoExceptionHandle ret = mono_exception_from_name_two_strings_checked (
		mono_get_corlib (), "System", "BadImageFormatException", s, fname, error);
	mono_error_assert_ok (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_get_exception_stack_overflow:
 * \returns a new instance of the \c System.StackOverflowException
 */
MonoException *
mono_get_exception_stack_overflow (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "StackOverflowException");
}

/**
 * mono_get_exception_out_of_memory:
 * \returns a new instance of the \c System.OutOfMemoryException
 */
MonoException *
mono_get_exception_out_of_memory (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "OutOfMemoryException");
}

MonoExceptionHandle
mono_get_exception_out_of_memory_handle (void)
{
	return MONO_HANDLE_NEW (MonoException, mono_exception_from_name (mono_get_corlib (), "System", "OutOfMemoryException"));
}

/**
 * mono_get_exception_field_access:
 * \returns a new instance of the \c System.FieldAccessException
 */
MonoException *
mono_get_exception_field_access (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "FieldAccessException");
}

/**
 * mono_get_exception_field_access2:
 * \param msg an informative message for the user.
 * \returns a new instance of the \c System.FieldAccessException
 */
MonoException *
mono_get_exception_field_access_msg (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "FieldAccessException", msg);
}

/**
 * mono_get_exception_method_access:
 * \returns a new instance of the \c System.MethodAccessException
 */
MonoException *
mono_get_exception_method_access (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "MethodAccessException");
}

/**
 * mono_get_exception_method_access2:
 * \param msg an informative message for the user.
 * \returns a new instance of the \c System.MethodAccessException
 */
MonoException *
mono_get_exception_method_access_msg (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "MethodAccessException", msg);
}

/**
 * mono_get_exception_reflection_type_load:
 * \param types an array of types that were defined in the moduled loaded.
 * \param exceptions an array of exceptions that were thrown during the type loading.
 * \returns a new instance of the \c System.Reflection.ReflectionTypeLoadException
 */
MonoException *
mono_get_exception_reflection_type_load (MonoArray *types_raw, MonoArray *exceptions_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoArray, types);
	MONO_HANDLE_DCL (MonoArray, exceptions);
	MonoExceptionHandle ret = mono_get_exception_reflection_type_load_checked (types, exceptions, error);
	if (!is_ok (error))
		ret = MONO_HANDLE_CAST (MonoException, mono_new_null ());
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

MonoExceptionHandle
mono_get_exception_reflection_type_load_checked (MonoArrayHandle types, MonoArrayHandle exceptions, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *klass;
	MonoMethod *method;
	gpointer iter;

	error_init (error);

	klass = mono_class_load_from_name (mono_get_corlib (), "System.Reflection", "ReflectionTypeLoadException");

	mono_class_init_internal (klass);

	/* Find the Type[], Exception[] ctor */
	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		if (!strcmp (".ctor", mono_method_get_name (method))) {
			MonoMethodSignature *sig = mono_method_signature_internal (method);

			if (sig->param_count == 2 && sig->params [0]->type == MONO_TYPE_SZARRAY && sig->params [1]->type == MONO_TYPE_SZARRAY)
				break;
		}
		method = NULL;
	}
	g_assert (method);

	MonoExceptionHandle exc = MONO_HANDLE_CAST (MonoException, MONO_HANDLE_NEW (MonoObject, mono_object_new_checked (klass, error)));
	mono_error_assert_ok (error);

	gpointer args [ ] = { MONO_HANDLE_RAW (types), MONO_HANDLE_RAW (exceptions) };

	mono_runtime_invoke_checked (method, MONO_HANDLE_RAW (exc), args, error);
	goto_if_nok (error, return_null);
	goto exit;
return_null:
	exc = MONO_HANDLE_CAST (MonoException, mono_new_null ());
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoException, exc);
}

/**
 * mono_get_exception_runtime_wrapped:
 */
MonoException *
mono_get_exception_runtime_wrapped (MonoObject *wrapped_exception_raw)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoObject, wrapped_exception);
	MonoExceptionHandle ret = mono_get_exception_runtime_wrapped_handle (wrapped_exception, error);
	if (!is_ok (error)) {
		mono_error_cleanup (error);
		ret = MONO_HANDLE_CAST (MonoException, mono_new_null ());
	}
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

MonoExceptionHandle
mono_get_exception_runtime_wrapped_handle (MonoObjectHandle wrapped_exception, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *klass;
	MonoMethod *method;

	klass = mono_class_load_from_name (mono_get_corlib (), "System.Runtime.CompilerServices", "RuntimeWrappedException");

	MonoObjectHandle o = mono_object_new_handle (klass, error);
	mono_error_assert_ok (error);
	g_assert (!MONO_HANDLE_IS_NULL (o));

	method = mono_class_get_method_from_name_checked (klass, ".ctor", 1, 0, error);
	mono_error_assert_ok (error);
	g_assert (method);

	gpointer args [ ] = { MONO_HANDLE_RAW (wrapped_exception) };

	mono_runtime_invoke_handle_void (method, o, args, error);
	goto_if_nok (error, return_null);
	goto exit;
return_null:
	o = mono_new_null ();
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoException, MONO_HANDLE_CAST (MonoException, o));
}

typedef struct {
	GString *text;
	const char *prefix;
} AppendFrameData;

static gboolean
append_frame_and_continue (MonoMethod *method, gpointer ip, size_t native_offset, gboolean managed, gpointer user_data)
{
	MONO_ENTER_GC_UNSAFE;
	AppendFrameData *data = (AppendFrameData *)user_data;

	if (data->prefix)
		g_string_append (data->text, data->prefix);
	if (method) {
		char *msg = mono_debug_print_stack_frame (method, (uint32_t)native_offset, NULL);
		g_string_append_printf (data->text, "%s\n", msg);
		g_free (msg);
	} else {
		g_string_append_printf (data->text, "at <unknown native frame 0x%p>\n", ip);
	}
	MONO_EXIT_GC_UNSAFE;
	return FALSE;
}

gboolean
mono_exception_try_get_managed_backtrace (MonoException *exc, const char *prefix, char **result)
{
	AppendFrameData data;

	data.text = g_string_new_len (NULL, 20);
	data.prefix = prefix;

	if (!mono_get_eh_callbacks ()->mono_exception_walk_trace (exc, append_frame_and_continue, &data)) {
		g_string_free (data.text, TRUE);
		*result = NULL;
		return FALSE;
	}

	*result = g_string_free (data.text, FALSE);
	return TRUE;
}

char *
mono_exception_get_managed_backtrace (MonoException *exc)
{
	char *result;

	if (!mono_exception_try_get_managed_backtrace (exc, NULL, &result))
		return g_strdup ("managed backtrace not available\n");

	return result;
}

char *
mono_exception_handle_get_native_backtrace (MonoExceptionHandle exc)
{
#ifdef HAVE_BACKTRACE_SYMBOLS
	MonoArrayHandle arr = MONO_HANDLE_NEW(MonoArray, NULL);
	int i, len;
	GString *text;
	char **messages;

	MONO_HANDLE_GET (arr, exc, native_trace_ips);

	if (MONO_HANDLE_IS_NULL(arr))
		return g_strdup ("");
	len = mono_array_handle_length (arr);
	text = g_string_new_len (NULL, len * 20);
	MonoGCHandle gchandle;
	gpointer *addr = MONO_ARRAY_HANDLE_PIN (arr, gpointer, 0, &gchandle);
	MONO_ENTER_GC_SAFE;
	messages = backtrace_symbols (addr, len);
	MONO_EXIT_GC_SAFE;
	mono_gchandle_free_internal (gchandle);

	for (i = 0; i < len; ++i) {
		gpointer ip;
		MONO_HANDLE_ARRAY_GETVAL (ip, arr, gpointer, i);
		MonoJitInfo *ji = mono_jit_info_table_find_internal (ip, TRUE, FALSE);
		if (ji) {
			char *msg = mono_debug_print_stack_frame (mono_jit_info_get_method (ji), (char*)ip - (char*)ji->code_start, NULL);
			g_string_append_printf (text, "%s\n", msg);
			g_free (msg);
		} else {
			g_string_append_printf (text, "%s\n", messages [i]);
		}
	}

	g_free (messages);
	return g_string_free (text, FALSE);
#else
	return g_strdup ("");
#endif
}

/**
 * mono_error_raise_exception_deprecated:
 * \param target_error the exception to raise
 *
 * Raises the exception of \p target_error.
 * Does nothing if \p target_error has a success error code.
 * Aborts in case of a double fault. This happens when it can't recover from an error caused by trying
 * to construct the first exception object.
 * The error object \p target_error is cleaned up.
*/
void
mono_error_raise_exception_deprecated (MonoError *target_error)
{
	MonoException *ex = mono_error_convert_to_exception (target_error);
	if (ex)
		mono_raise_exception_deprecated (ex);
}

/**
 * mono_error_set_pending_exception_slow:
 * \param error The error
 * If \p error is set, convert it to an exception and set the pending exception for the current icall.
 * \returns TRUE if \p error was set, or FALSE otherwise, so that you can write:
 *    if (mono_error_set_pending_exception (error)) {
 *      { ... cleanup code ... }
 *      return;
 *    }
 */
// For efficiency, call mono_error_set_pending_exception instead of mono_error_set_pending_exception_slow.
gboolean
mono_error_set_pending_exception_slow (MonoError *error)
{
	if (is_ok (error))
		return FALSE;

	HANDLE_FUNCTION_ENTER ();

	MonoExceptionHandle ex = mono_error_convert_to_exception_handle (error);
	gboolean const result = !MONO_HANDLE_IS_NULL (ex);
	if (result)
		mono_set_pending_exception_handle (ex);

	HANDLE_FUNCTION_RETURN_VAL (result);
}

void
mono_install_unhandled_exception_hook (MonoUnhandledExceptionFunc func, void *user_data)
{
	unhandled_exception_hook = func;
	unhandled_exception_hook_data = user_data;
}

void
mono_invoke_unhandled_exception_hook (MonoObject *exc)
{
	if (unhandled_exception_hook) {
		unhandled_exception_hook (exc, unhandled_exception_hook_data);
	} else {
		ERROR_DECL (inner_error);
		MonoObject *other = NULL;
		MonoString *str = mono_object_try_to_string (exc, &other, inner_error);
		char *msg = NULL;

		if (str && is_ok (inner_error)) {
			msg = mono_string_to_utf8_checked_internal (str, inner_error);
			if (!is_ok (inner_error)) {
				msg = g_strdup_printf ("Nested exception while formatting original exception");
				mono_error_cleanup (inner_error);
			}
		} else if (other) {
			char *original_backtrace = mono_exception_get_managed_backtrace ((MonoException*)exc);
			char *nested_backtrace = mono_exception_get_managed_backtrace ((MonoException*)other);

			msg = g_strdup_printf ("Nested exception detected.\nOriginal Exception: %s\nNested exception:%s\n",
				original_backtrace, nested_backtrace);

			g_free (original_backtrace);
			g_free (nested_backtrace);
		} else {
			msg = g_strdup ("Nested exception trying to figure out what went wrong");
		}
		mono_runtime_printf_err ("[ERROR] FATAL UNHANDLED EXCEPTION: %s", msg);
		g_free (msg);
#if defined(HOST_IOS)
		g_assertion_message ("Terminating runtime due to unhandled exception");
#else
		exit (mono_environment_exitcode_get ());
#endif
	}

	g_assert_not_reached ();
}

MonoExceptionHandle
mono_corlib_exception_new_with_args (const char *name_space, const char *name, const char *arg_0, const char *arg_1, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoStringHandle str_0 = NULL_HANDLE_STRING;
	MonoStringHandle str_1 = NULL_HANDLE_STRING;
	MonoExceptionHandle ex = MONO_HANDLE_CAST (MonoException, NULL_HANDLE);

	str_0 = arg_0 ? mono_string_new_handle (arg_0, error) : NULL_HANDLE_STRING;
	goto_if_nok (error, return_null);

	str_1 = arg_1 ? mono_string_new_handle (arg_1, error) : NULL_HANDLE_STRING;
	goto_if_nok (error, return_null);

	ex = mono_exception_from_name_two_strings_checked (mono_defaults.corlib, name_space, name, str_0, str_1, error);
	goto exit;
return_null:
	ex = MONO_HANDLE_CAST (MonoException, mono_new_null ());
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoException, ex);
}

void
mono_error_set_field_missing (MonoError *error, MonoClass *klass, const char *field_name, MonoType *sig, const char *reason, ...)
{
	char *result;
	GString *res;

	res = g_string_new ("Field not found: ");


	if (sig) {
		mono_type_get_desc (res, sig, TRUE);
		g_string_append_c (res, ' ');
	}

	if (klass) {
		if (m_class_get_name_space (klass)) {
			g_string_append (res, m_class_get_name_space (klass));
			g_string_append_c (res, '.');
		}
		g_string_append (res, m_class_get_name (klass));
	}
	else {
		g_string_append (res, "<unknown type>");
	}

	g_string_append_c (res, '.');

	if (field_name)
		g_string_append (res, field_name);
	else
		g_string_append (res, "<unknown field>");

	if (reason && *reason) {
		va_list args;
		va_start (args, reason);

		g_string_append (res, " Due to: ");
		g_string_append_vprintf (res, reason, args);
		va_end (args);
	}
	result = res->str;
	g_string_free (res, FALSE);

	mono_error_set_specific (error, MONO_ERROR_MISSING_FIELD, result);
}

/*
 * Sets @error to a method missing error.
 */
void
mono_error_set_method_missing (MonoError *error, MonoClass *klass, const char *method_name, MonoMethodSignature *sig, const char *reason, ...)
{
	int i;
	char *result;
	GString *res;

	res = g_string_new ("Method not found: ");

	if (sig) {
		mono_type_get_desc (res, sig->ret, TRUE);

		g_string_append_c (res, ' ');
	}

	if (klass) {
		if (m_class_get_name_space (klass)) {
			g_string_append (res, m_class_get_name_space (klass));
			g_string_append_c (res, '.');
		}
		g_string_append (res, m_class_get_name (klass));
	}
	else {
		g_string_append (res, "<unknown type>");
	}

	g_string_append_c (res, '.');

	if (method_name)
		g_string_append (res, method_name);
	else
		g_string_append (res, "<unknown method>");

	if (sig) {
		if (sig->generic_param_count) {
			g_string_append_c (res, '<');
			for (i = 0; i < sig->generic_param_count; ++i) {
				if (i > 0)
					g_string_append (res, ",");
				g_string_append_printf (res, "!%d", i);
			}
			g_string_append_c (res, '>');
		}

		g_string_append_c (res, '(');
		for (i = 0; i < sig->param_count; ++i) {
			if (i > 0)
				g_string_append_c (res, ',');
			mono_type_get_desc (res, sig->params [i], TRUE);
		}
		g_string_append_c (res, ')');
	}

	if (reason && *reason) {
		va_list args;
		va_start (args, reason);

		g_string_append (res, " Due to: ");
		g_string_append_vprintf (res, reason, args);
		va_end (args);
	}
	result = res->str;
	g_string_free (res, FALSE);

	mono_error_set_specific (error, MONO_ERROR_MISSING_METHOD, result);
}

#define SET_ERROR_MSG(STR_VAR, FMT_STR) do {	\
	va_list __args;	\
	va_start (__args, FMT_STR);	\
	STR_VAR = g_strdup_vprintf (FMT_STR, __args);	\
	va_end(__args);	\
} while (0);

/**
 * \p image_name argument will be g_strdup'd. Called must free passed value
 */
void
mono_error_set_bad_image_by_name (MonoError *error, const char *image_name, const char *msg_format, ...)
{
	char *str;
	SET_ERROR_MSG (str, msg_format);

	mono_error_set_specific (error, MONO_ERROR_BAD_IMAGE, str);
	if (image_name)
		mono_error_set_first_argument (error, image_name);
}

void
mono_error_set_bad_image (MonoError *error, MonoImage *image, const char *msg_format, ...)
{
	char *str;
	SET_ERROR_MSG (str, msg_format);

	mono_error_set_specific (error, MONO_ERROR_BAD_IMAGE, str);
	if (image)
		mono_error_set_first_argument (error, mono_image_get_name (image));
}

void
mono_error_set_file_not_found (MonoError *error, const char *file_name, const char *msg_format, ...)
{
	char *str;
	SET_ERROR_MSG (str, msg_format);

	mono_error_set_specific (error, MONO_ERROR_FILE_NOT_FOUND, str);
	if (file_name)
		mono_error_set_first_argument (error, file_name);
}

void
mono_error_set_simple_file_not_found (MonoError *error, const char *file_name)
{
	mono_error_set_file_not_found (error, file_name, "Could not load file or assembly '%s' or one of its dependencies.", file_name);
}

void
mono_error_set_argument_out_of_range (MonoError *error, const char *param_name, const char *msg_format, ...)
{
	char *str;
	SET_ERROR_MSG (str, msg_format);
	mono_error_set_specific (error, MONO_ERROR_ARGUMENT_OUT_OF_RANGE, str);
	if (param_name)
		mono_error_set_first_argument (error, param_name);
}

MonoExceptionHandle
mono_error_convert_to_exception_handle (MonoError *error)
{
	//FIXMEcoop mono_error_convert_to_exception is raw pointer
	// The "optimization" here is important to significantly reduce handle usage.
	return is_ok (error) ? MONO_HANDLE_CAST (MonoException, NULL_HANDLE)
		: MONO_HANDLE_NEW (MonoException, mono_error_convert_to_exception (error));
}
