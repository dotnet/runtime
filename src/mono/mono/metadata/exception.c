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
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <string.h>

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

static MonoUnhandledExceptionFunc unhandled_exception_hook = NULL;
static gpointer unhandled_exception_hook_data = NULL;

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
	MonoError error;
	MonoClass *klass;
	MonoObject *o;
	MonoDomain *caller_domain = mono_domain_get ();

	klass = mono_class_load_from_name (image, name_space, name);

	o = mono_object_new_checked (domain, klass, &error);
	mono_error_assert_ok (&error);

	if (domain != caller_domain)
		mono_domain_set_internal (domain);
	mono_runtime_object_init_checked (o, &error);
	mono_error_assert_ok (&error);

	if (domain != caller_domain)
		mono_domain_set_internal (caller_domain);

	return (MonoException *)o;
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
	MonoError error;
	MonoClass *klass;
	MonoObject *o;

	klass = mono_class_get_checked (image, token, &error);
	mono_error_assert_ok (&error);

	o = mono_object_new_checked (mono_domain_get (), klass, &error);
	mono_error_assert_ok (&error);
	
	mono_runtime_object_init_checked (o, &error);
	mono_error_assert_ok (&error);

	return (MonoException *)o;
}

static MonoException *
create_exception_two_strings (MonoClass *klass, MonoString *a1, MonoString *a2, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethod *method = NULL;
	MonoObject *o;
	int count = 1;
	gpointer args [2];
	gpointer iter;
	MonoMethod *m;

	if (a2 != NULL)
		count++;
	
	o = mono_object_new_checked (domain, klass, error);
	mono_error_assert_ok (error);

	iter = NULL;
	while ((m = mono_class_get_methods (klass, &iter))) {
		MonoMethodSignature *sig;
		
		if (strcmp (".ctor", mono_method_get_name (m)))
			continue;
		sig = mono_method_signature (m);
		if (sig->param_count != count)
			continue;

		if (sig->params [0]->type != MONO_TYPE_STRING)
			continue;
		if (count == 2 && sig->params [1]->type != MONO_TYPE_STRING)
			continue;
		method = m;
		break;
	}

	args [0] = a1;
	args [1] = a2;

	mono_runtime_invoke_checked (method, o, args, error);
	return_val_if_nok (error, NULL);

	return (MonoException *) o;
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
				      const char *name, MonoString *a1, MonoString *a2)
{
	MonoError error;
	MonoException *ret;

	ret = mono_exception_from_name_two_strings_checked (image, name_space, name, a1, a2, &error);
	mono_error_cleanup (&error);
	return ret;
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
MonoException *
mono_exception_from_name_two_strings_checked (MonoImage *image, const char *name_space,
					      const char *name, MonoString *a1, MonoString *a2,
					      MonoError *error)
{
	MonoClass *klass;

	error_init (error);
	klass = mono_class_load_from_name (image, name_space, name);

	return create_exception_two_strings (klass, a1, a2, error);
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
	MonoError error;
	MonoException *ex;

	ex = mono_exception_from_name (image, name_space, name);

	if (msg) {
		MonoString  *msg_str = mono_string_new_checked (mono_object_get_domain ((MonoObject*)ex), msg, &error);
		mono_error_assert_ok (&error);
		MONO_OBJECT_SETREF (ex, message, msg_str);
	}

	return ex;
}

/**
 * mono_exception_from_token_two_strings:
 *
 *   Same as mono_exception_from_name_two_strings, but lookup the exception class using
 * IMAGE and TOKEN.
 */
MonoException *
mono_exception_from_token_two_strings (MonoImage *image, guint32 token,
									   MonoString *a1, MonoString *a2)
{
	MonoError error;
	MonoException *ret;
	ret = mono_exception_from_token_two_strings_checked (image, token, a1, a2, &error);
	mono_error_cleanup (&error);
	return ret;
}

/**
 * mono_exception_from_token_two_strings_checked:
 *
 *   Same as mono_exception_from_name_two_strings, but lookup the exception class using
 * IMAGE and TOKEN.
 */
MonoException *
mono_exception_from_token_two_strings_checked (MonoImage *image, guint32 token,
					       MonoString *a1, MonoString *a2,
					       MonoError *error)
{
	MonoClass *klass;

	error_init (error);

	klass = mono_class_get_checked (image, token, error);
	mono_error_assert_ok (error); /* FIXME handle the error. */

	return create_exception_two_strings (klass, a1, a2, error);
}

/**
 * mono_get_exception_divide_by_zero:
 * \returns a new instance of the \c System.DivideByZeroException
 */
MonoException *
mono_get_exception_divide_by_zero ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "DivideByZeroException");
}

/**
 * mono_get_exception_security:
 * \returns a new instance of the \c System.Security.SecurityException
 */
MonoException *
mono_get_exception_security ()
{
	return mono_exception_from_name (mono_get_corlib (), "System.Security",
					 "SecurityException");
}

/**
 * mono_get_exception_thread_abort:
 * \returns a new instance of the \c System.Threading.ThreadAbortException
 */
MonoException *
mono_get_exception_thread_abort ()
{
	return mono_exception_from_name (mono_get_corlib (), "System.Threading",
					 "ThreadAbortException");
}

/**
 * mono_get_exception_thread_interrupted:
 * \returns a new instance of the \c System.Threading.ThreadInterruptedException
 */
MonoException *
mono_get_exception_thread_interrupted ()
{
	return mono_exception_from_name (mono_get_corlib (), "System.Threading",
					 "ThreadInterruptedException");
}

/**
 * mono_get_exception_arithmetic:
 * \returns a new instance of the \c System.ArithmeticException
 */
MonoException *
mono_get_exception_arithmetic ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "ArithmeticException");
}

/**
 * mono_get_exception_overflow:
 * \returns a new instance of the \c System.OverflowException
 */
MonoException *
mono_get_exception_overflow ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "OverflowException");
}

/**
 * mono_get_exception_null_reference:
 * \returns a new instance of the \c System.NullReferenceException
 */
MonoException *
mono_get_exception_null_reference ()
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
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "ExecutionEngineException", msg);
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
mono_get_exception_array_type_mismatch ()
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
mono_get_exception_type_load (MonoString *class_name, char *assembly_name)
{
	MonoError error;
	MonoString *s = NULL;
	if (assembly_name) {
		s = mono_string_new_checked (mono_domain_get (), assembly_name, &error);
		mono_error_assert_ok (&error);
	} else
		s = mono_string_empty (mono_domain_get ());

	MonoException *ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System",
								   "TypeLoadException", class_name, s, &error);
	mono_error_assert_ok (&error);
	return ret;
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
 * mono_get_exception_missing_method:
 * \param class_name the class where the lookup was performed.
 * \param member_name the name of the missing method.
 * \returns a new instance of the \c System.MissingMethodException
 */
MonoException *
mono_get_exception_missing_method (const char *class_name, const char *member_name)
{
	MonoError error;
	MonoString *s1 = mono_string_new_checked (mono_domain_get (), class_name, &error);
	mono_error_assert_ok (&error);
	MonoString *s2 = mono_string_new_checked (mono_domain_get (), member_name, &error);
	mono_error_assert_ok (&error);

	MonoException *ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System",
									   "MissingMethodException", s1, s2, &error);
	mono_error_assert_ok (&error);
	return ret;
}

/**
 * mono_get_exception_missing_field:
 * \param class_name the class where the lookup was performed
 * \param member_name the name of the missing method.
 * \returns a new instance of the \c System.MissingFieldException
 */
MonoException *
mono_get_exception_missing_field (const char *class_name, const char *member_name)
{
	MonoError error;
	MonoString *s1 = mono_string_new_checked (mono_domain_get (), class_name, &error);
	mono_error_assert_ok (&error);
	MonoString *s2 = mono_string_new_checked (mono_domain_get (), member_name, &error);
	mono_error_assert_ok (&error);

	MonoException *ret = mono_exception_from_name_two_strings_checked (mono_get_corlib (), "System",
								   "MissingFieldException", s1, s2, &error);
	mono_error_assert_ok (&error);
	return ret;
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

	ex = mono_exception_from_name ( 
		mono_get_corlib (), "System", "ArgumentNullException");

	if (arg) {
		MonoError error;
		MonoArgumentException *argex = (MonoArgumentException *)ex;
		MonoString *arg_str = mono_string_new_checked (mono_object_get_domain ((MonoObject*)ex), arg, &error);
		mono_error_assert_ok (&error);
		MONO_OBJECT_SETREF (argex, param_name, arg_str);
	}
	
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
	MonoException *ex;

	ex = mono_exception_from_name_msg (
		mono_get_corlib (), "System", "ArgumentException", msg);

	if (arg) {
		MonoError error;
		MonoArgumentException *argex = (MonoArgumentException *)ex;
		MonoString *arg_str = mono_string_new_checked (mono_object_get_domain ((MonoObject*)ex), arg, &error);
		mono_error_assert_ok (&error);
		MONO_OBJECT_SETREF (argex, param_name, arg_str);
	}
	
	return ex;
}

/**
 * mono_get_exception_argument_out_of_range:
 * \param arg the name of the out of range argument.
 * \returns a new instance of the \c System.ArgumentOutOfRangeException
 */
MonoException *
mono_get_exception_argument_out_of_range (const char *arg)
{
	MonoException *ex;

	ex = mono_exception_from_name (
		mono_get_corlib (), "System", "ArgumentOutOfRangeException");

	if (arg) {
		MonoError error;
		MonoArgumentException *argex = (MonoArgumentException *)ex;
		MonoString *arg_str = mono_string_new_checked (mono_object_get_domain ((MonoObject*)ex), arg, &error);
		mono_error_assert_ok (&error);
		MONO_OBJECT_SETREF (argex, param_name, arg_str);
	}
	
	return ex;
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
mono_get_exception_file_not_found (MonoString *fname)
{
	MonoError error;
	MonoException *ret = mono_exception_from_name_two_strings_checked (
		mono_get_corlib (), "System.IO", "FileNotFoundException", fname, fname, &error);
	mono_error_assert_ok (&error);
	return ret;
}

/**
 * mono_get_exception_file_not_found2:
 * \param msg an informative message for the user.
 * \param fname the name of the file not found.
 * \returns a new instance of the \c System.IO.FileNotFoundException
 */
MonoException *
mono_get_exception_file_not_found2 (const char *msg, MonoString *fname)
{
	MonoError error;
	MonoString *s = NULL;
	if (msg) {
		s = mono_string_new_checked (mono_domain_get (), msg, &error);
		mono_error_assert_ok (&error);
	}

	MonoException *ret = mono_exception_from_name_two_strings_checked (
		mono_get_corlib (), "System.IO", "FileNotFoundException", s, fname, &error);
	mono_error_assert_ok (&error);
	return ret;
}

/**
 * mono_get_exception_type_initialization:
 * \param type_name the name of the type that failed to initialize.
 * \param inner the inner exception.
 * \returns a new instance of the \c System.TypeInitializationException
 */
MonoException *
mono_get_exception_type_initialization (const gchar *type_name, MonoException *inner)
{
	MonoError error;
	MonoException *ret = mono_get_exception_type_initialization_checked (type_name, inner, &error);
	if (!is_ok (&error)) {
		mono_error_cleanup (&error);
		return NULL;
	}

	return ret;
}

MonoException *
mono_get_exception_type_initialization_checked (const gchar *type_name, MonoException *inner, MonoError *error)
{
	MonoClass *klass;
	gpointer args [2];
	MonoObject *exc;
	MonoMethod *method;
	gpointer iter;

	error_init (error);

	klass = mono_class_load_from_name (mono_get_corlib (), "System", "TypeInitializationException");

	mono_class_init (klass);

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		if (!strcmp (".ctor", mono_method_get_name (method))) {
			MonoMethodSignature *sig = mono_method_signature (method);

			if (sig->param_count == 2 && sig->params [0]->type == MONO_TYPE_STRING && mono_class_from_mono_type (sig->params [1]) == mono_defaults.exception_class)
				break;
		}
		method = NULL;
	}
	g_assert (method);

	MonoString *type_name_str = mono_string_new_checked (mono_domain_get (), type_name, error);
	mono_error_assert_ok (error);
	args [0] = type_name_str;
	args [1] = inner;

	exc = mono_object_new_checked (mono_domain_get (), klass, error);
	mono_error_assert_ok (error);

	mono_runtime_invoke_checked (method, exc, args, error);
	return_val_if_nok (error, NULL);

	return (MonoException *) exc;
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
mono_get_exception_bad_image_format2 (const char *msg, MonoString *fname)
{
	MonoError error;
	MonoString *s = NULL;

	if (msg) {
		s = mono_string_new_checked (mono_domain_get (), msg, &error);
		mono_error_assert_ok (&error);
	}

	MonoException *ret = mono_exception_from_name_two_strings_checked (
		mono_get_corlib (), "System", "BadImageFormatException", s, fname, &error);
	mono_error_assert_ok (&error);
	return ret;
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
	MonoError error;
	MONO_HANDLE_DCL (MonoArray, types);
	MONO_HANDLE_DCL (MonoArray, exceptions);
	MonoExceptionHandle ret = mono_get_exception_reflection_type_load_checked (types, exceptions, &error);
	if (is_ok (&error)) {
		mono_error_cleanup (&error);
		ret = MONO_HANDLE_CAST (MonoException, NULL_HANDLE);
		goto leave;
	}

leave:
	HANDLE_FUNCTION_RETURN_OBJ (ret);

}

MonoExceptionHandle
mono_get_exception_reflection_type_load_checked (MonoArrayHandle types, MonoArrayHandle exceptions, MonoError *error)
{
	MonoClass *klass;
	MonoMethod *method;
	gpointer iter;

	error_init (error);

	klass = mono_class_load_from_name (mono_get_corlib (), "System.Reflection", "ReflectionTypeLoadException");

	mono_class_init (klass);

	/* Find the Type[], Exception[] ctor */
	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		if (!strcmp (".ctor", mono_method_get_name (method))) {
			MonoMethodSignature *sig = mono_method_signature (method);

			if (sig->param_count == 2 && sig->params [0]->type == MONO_TYPE_SZARRAY && sig->params [1]->type == MONO_TYPE_SZARRAY)
				break;
		}
		method = NULL;
	}
	g_assert (method);

	MonoExceptionHandle exc = MONO_HANDLE_NEW (MonoException, mono_object_new_checked (mono_domain_get (), klass, error));
	mono_error_assert_ok (error);

	gpointer args [2];
	args [0] = MONO_HANDLE_RAW (types);
	args [1] = MONO_HANDLE_RAW (exceptions);

	mono_runtime_invoke_checked (method, MONO_HANDLE_RAW (exc), args, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoException, NULL_HANDLE));

	return exc;
}

/**
 * mono_get_exception_runtime_wrapped:
 */
MonoException *
mono_get_exception_runtime_wrapped (MonoObject *wrapped_exception)
{
	MonoError error;
	MonoException *ret = mono_get_exception_runtime_wrapped_checked (wrapped_exception, &error);
	if (!is_ok (&error)) {
		mono_error_cleanup (&error);
		return NULL;
	}

	return ret;
}

MonoException *
mono_get_exception_runtime_wrapped_checked (MonoObject *wrapped_exception, MonoError *error)
{
	MonoClass *klass;
	MonoObject *o;
	MonoMethod *method;
	MonoDomain *domain = mono_domain_get ();
	gpointer params [16];

	klass = mono_class_load_from_name (mono_get_corlib (), "System.Runtime.CompilerServices", "RuntimeWrappedException");

	o = mono_object_new_checked (domain, klass, error);
	mono_error_assert_ok (error);
	g_assert (o != NULL);

	method = mono_class_get_method_from_name (klass, ".ctor", 1);
	g_assert (method);

	params [0] = wrapped_exception;

	mono_runtime_invoke_checked (method, o, params, error);
	return_val_if_nok (error, NULL);

	return (MonoException *)o;
}	

static gboolean
append_frame_and_continue (MonoMethod *method, gpointer ip, size_t native_offset, gboolean managed, gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	GString *text = (GString*)user_data;

	if (method) {
		char *msg = mono_debug_print_stack_frame (method, native_offset, domain);
		g_string_append_printf (text, "%s\n", msg);
		g_free (msg);
	} else {
		g_string_append_printf (text, "<unknown native frame 0x%x>\n", ip);
	}

	return FALSE;
}

char *
mono_exception_get_managed_backtrace (MonoException *exc)
{
	GString *text;

	text = g_string_new_len (NULL, 20);

	if (!mono_get_eh_callbacks ()->mono_exception_walk_trace (exc, append_frame_and_continue, text))
		g_string_append (text, "managed backtrace not available\n");

	return g_string_free (text, FALSE);
}

char *
mono_exception_handle_get_native_backtrace (MonoExceptionHandle exc)
{
#ifdef HAVE_BACKTRACE_SYMBOLS
	MonoDomain *domain;
	MonoArrayHandle arr = MONO_HANDLE_NEW(MonoArray, NULL);
	int i, len;
	GString *text;
	char **messages;

	MONO_HANDLE_GET (arr, exc, native_trace_ips);

	if (MONO_HANDLE_IS_NULL(arr))
		return g_strdup ("");
	domain = mono_domain_get ();
	len = mono_array_handle_length (arr);
	text = g_string_new_len (NULL, len * 20);
	uint32_t gchandle;
	void *addr = MONO_ARRAY_HANDLE_PIN (arr, gpointer, 0, &gchandle);
	MONO_ENTER_GC_SAFE;
	messages = backtrace_symbols (addr, len);
	MONO_EXIT_GC_SAFE;
	mono_gchandle_free (gchandle);

	for (i = 0; i < len; ++i) {
		gpointer ip;
		MONO_HANDLE_ARRAY_GETVAL (ip, arr, gpointer, i);
		MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), (char *)ip);
		if (ji) {
			char *msg = mono_debug_print_stack_frame (mono_jit_info_get_method (ji), (char*)ip - (char*)ji->code_start, domain);
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

MonoStringHandle
ves_icall_Mono_Runtime_GetNativeStackTrace (MonoExceptionHandle exc, MonoError *error)
{
	char *trace;
	MonoStringHandle res;
	error_init (error);

	if (!exc) {
		mono_error_set_argument_null (error, "exception", "");
		return NULL_HANDLE_STRING;
	}

	trace = mono_exception_handle_get_native_backtrace (exc);
	res = mono_string_new_handle (mono_domain_get (), trace, error);
	g_free (trace);
	return res;
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
 * mono_error_set_pending_exception:
 * \param error The error
 * If \p error is set, convert it to an exception and set the pending exception for the current icall.
 * \returns TRUE if \p error was set, or FALSE otherwise, so that you can write:
 *    if (mono_error_set_pending_exception (error)) {
 *      { ... cleanup code ... }
 *      return;
 *    }
 */
gboolean
mono_error_set_pending_exception (MonoError *error)
{
	MonoException *ex = mono_error_convert_to_exception (error);
	if (ex) {
		mono_set_pending_exception (ex);
		return TRUE;
	} else {
		return FALSE;
	}
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
		MonoError inner_error;
		MonoObject *other = NULL;
		MonoString *str = mono_object_try_to_string (exc, &other, &inner_error);
		char *msg = NULL;
		
		if (str && is_ok (&inner_error)) {
			msg = mono_string_to_utf8_checked (str, &inner_error);
			if (!is_ok (&inner_error)) {
				msg = g_strdup_printf ("Nested exception while formatting original exception");
				mono_error_cleanup (&inner_error);
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


static MonoException *
create_exception_four_strings (MonoClass *klass, MonoString *a1, MonoString *a2, MonoString *a3, MonoString *a4, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethod *method = NULL;
	MonoObject *o;
	int count = 4;
	gpointer args [4];
	gpointer iter;
	MonoMethod *m;

	o = mono_object_new_checked (domain, klass, error);
	mono_error_assert_ok (error);

	iter = NULL;
	while ((m = mono_class_get_methods (klass, &iter))) {
		MonoMethodSignature *sig;

		if (strcmp (".ctor", mono_method_get_name (m)))
			continue;
		sig = mono_method_signature (m);
		if (sig->param_count != count)
			continue;

		int i;
		gboolean good = TRUE;
		for (i = 0; i < count; ++i) {
			if (sig->params [i]->type != MONO_TYPE_STRING) {
				good = FALSE;
				break;
			}
		}
		if (good) {
			method = m;
			break;
		}
	}

	g_assert (method);

	args [0] = a1;
	args [1] = a2;
	args [2] = a3;
	args [3] = a4;

	mono_runtime_invoke_checked (method, o, args, error);
	return_val_if_nok (error, NULL);

	return (MonoException *) o;
}

MonoException *
mono_exception_from_name_four_strings_checked (MonoImage *image, const char *name_space,
					      const char *name, MonoString *a1, MonoString *a2, MonoString *a3, MonoString *a4,
					      MonoError *error)
{
	MonoClass *klass;

	error_init (error);
	klass = mono_class_load_from_name (image, name_space, name);

	return create_exception_four_strings (klass, a1, a2, a3, a4, error);
}
