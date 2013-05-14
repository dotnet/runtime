/*
 * exception.c: Exception handling
 *
 * Authors:
 *	Paolo Molaro    (lupus@ximian.com)
 *	Dietmar Maurer  (dietmar@ximian.com)
 *	Dick Porter     (dick@ximian.com)
 *      Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include <mono/metadata/exception.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-debug.h>
#include <string.h>

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

/**
 * mono_exception_from_name:
 * @image: the Mono image where to look for the class
 * @name_space: the namespace for the class
 * @name: class name
 *
 * Creates an exception of the given namespace/name class in the
 * current domain.
 *
 * Returns: the initialized exception instance.
 */
MonoException *
mono_exception_from_name (MonoImage *image, const char *name_space,
			  const char *name)
{
	return mono_exception_from_name_domain (mono_domain_get (), image, name_space, name);
}

/**
 * mono_exception_from_name_domain:
 * @domain: Domain where the return object will be created.
 * @image: the Mono image where to look for the class
 * @name_space: the namespace for the class
 * @name: class name
 *
 * Creates an exception object of the given namespace/name class on
 * the given domain.
 *
 * Returns: the initialized exception instance.
 */
MonoException *
mono_exception_from_name_domain (MonoDomain *domain, MonoImage *image, 
				 const char* name_space, const char *name)
{
	MonoClass *klass;
	MonoObject *o;
	MonoDomain *caller_domain = mono_domain_get ();

	klass = mono_class_from_name (image, name_space, name);

	o = mono_object_new (domain, klass);
	g_assert (o != NULL);

	if (domain != caller_domain)
		mono_domain_set_internal (domain);
	mono_runtime_object_init (o);
	if (domain != caller_domain)
		mono_domain_set_internal (caller_domain);

	return (MonoException *)o;
}


/**
 * mono_exception_from_token:
 * @image: the Mono image where to look for the class
 * @token: The type token of the class
 *
 * Creates an exception of the type given by @token.
 *
 * Returns: the initialized exception instance.
 */
MonoException *
mono_exception_from_token (MonoImage *image, guint32 token)
{
	MonoClass *klass;
	MonoObject *o;

	klass = mono_class_get (image, token);

	o = mono_object_new (mono_domain_get (), klass);
	g_assert (o != NULL);
	
	mono_runtime_object_init (o);

	return (MonoException *)o;
}

static MonoException *
create_exception_two_strings (MonoClass *klass, MonoString *a1, MonoString *a2)
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
	
	o = mono_object_new (domain, klass);

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
	mono_runtime_invoke (method, o, args, NULL);
	return (MonoException *) o;
}

/**
 * mono_exception_from_name_two_strings:
 * @image: the Mono image where to look for the class
 * @name_space: the namespace for the class
 * @name: class name
 * @a1: first string argument to pass
 * @a2: second string argument to pass
 *
 * Creates an exception from a constructor that takes two string
 * arguments.
 *
 * Returns: the initialized exception instance.
 */
MonoException *
mono_exception_from_name_two_strings (MonoImage *image, const char *name_space,
				      const char *name, MonoString *a1, MonoString *a2)
{
	MonoClass *klass = mono_class_from_name (image, name_space, name);

	return create_exception_two_strings (klass, a1, a2);
}

/**
 * mono_exception_from_name_msg:
 * @image: the Mono image where to look for the class
 * @name_space: the namespace for the class
 * @name: class name
 * @msg: the message to embed inside the exception
 *
 * Creates an exception and initializes its message field.
 *
 * Returns: the initialized exception instance.
 */
MonoException *
mono_exception_from_name_msg (MonoImage *image, const char *name_space,
			      const char *name, const char *msg)
{
	MonoException *ex;

	ex = mono_exception_from_name (image, name_space, name);

	if (msg)
		MONO_OBJECT_SETREF (ex, message, mono_string_new (mono_object_get_domain ((MonoObject*)ex), msg));

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
	MonoClass *klass = mono_class_get (image, token);

	return create_exception_two_strings (klass, a1, a2);
}

/**
 * mono_get_exception_divide_by_zero:
 *
 * Returns: a new instance of the System.DivideByZeroException
 */
MonoException *
mono_get_exception_divide_by_zero ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "DivideByZeroException");
}

/**
 * mono_get_exception_security:
 *
 * Returns: a new instance of the System.Security.SecurityException
 */
MonoException *
mono_get_exception_security ()
{
	return mono_exception_from_name (mono_get_corlib (), "System.Security",
					 "SecurityException");
}

/**
 * mono_get_exception_thread_abort:
 *
 * Returns: a new instance of the System.Threading.ThreadAbortException.
 */
MonoException *
mono_get_exception_thread_abort ()
{
	return mono_exception_from_name (mono_get_corlib (), "System.Threading",
					 "ThreadAbortException");
}

/**
 * mono_get_exception_thread_interrupted:
 *
 * Returns: a new instance of the System.Threading.ThreadInterruptedException.
 */
MonoException *
mono_get_exception_thread_interrupted ()
{
	return mono_exception_from_name (mono_get_corlib (), "System.Threading",
					 "ThreadInterruptedException");
}

/**
 * mono_get_exception_arithmetic:
 *
 * Returns: a new instance of the System.ArithmeticException.
 */
MonoException *
mono_get_exception_arithmetic ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "ArithmeticException");
}

/**
 * mono_get_exception_overflow:
 *
 * Returns: a new instance of the System.OverflowException
 */
MonoException *
mono_get_exception_overflow ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "OverflowException");
}

/**
 * mono_get_exception_null_reference:
 *
 * Returns: a new instance of the System.NullReferenceException
 */
MonoException *
mono_get_exception_null_reference ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "NullReferenceException");
}

/**
 * mono_get_exception_execution_engine:
 * @msg: the message to pass to the user
 *
 * Returns: a new instance of the System.ExecutionEngineException
 */
MonoException *
mono_get_exception_execution_engine (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "ExecutionEngineException", msg);
}

/**
 * mono_get_exception_serialization:
 * @msg: the message to pass to the user
 *
 * Returns: a new instance of the System.Runtime.Serialization.SerializationException
 */
MonoException *
mono_get_exception_serialization (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System.Runtime.Serialization", "SerializationException", msg);
}

/**
 * mono_get_exception_invalid_cast:
 *
 * Returns: a new instance of the System.InvalidCastException
 */
MonoException *
mono_get_exception_invalid_cast ()
{
	return mono_exception_from_name (mono_get_corlib (), "System", "InvalidCastException");
}

/**
 * mono_get_exception_invalid_operation:
 * @msg: the message to pass to the user
 *
 * Returns: a new instance of the System.InvalidOperationException
 */
MonoException *
mono_get_exception_invalid_operation (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System",
					"InvalidOperationException", msg);
}

/**
 * mono_get_exception_index_out_of_range:
 *
 * Returns: a new instance of the System.IndexOutOfRangeException
 */
MonoException *
mono_get_exception_index_out_of_range ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "IndexOutOfRangeException");
}

/**
 * mono_get_exception_array_type_mismatch:
 *
 * Returns: a new instance of the System.ArrayTypeMismatchException
 */
MonoException *
mono_get_exception_array_type_mismatch ()
{
	return mono_exception_from_name (mono_get_corlib (), "System",
					 "ArrayTypeMismatchException");
}

/**
 * mono_get_exception_type_load:
 * @class_name: the name of the class that could not be loaded
 * @assembly_name: the assembly where the class was looked up.
 *
 * Returns: a new instance of the System.TypeLoadException.
 */
MonoException *
mono_get_exception_type_load (MonoString *class_name, char *assembly_name)
{
	MonoString *s = assembly_name ? mono_string_new (mono_domain_get (), assembly_name) : mono_string_new (mono_domain_get (), "");

	return mono_exception_from_name_two_strings (mono_get_corlib (), "System",
						     "TypeLoadException", class_name, s);
}

/**
 * mono_get_exception_not_implemented:
 * @msg: the message to pass to the user
 *
 * Returns: a new instance of the System.NotImplementedException
 */
MonoException *
mono_get_exception_not_implemented (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "NotImplementedException", msg);
}

/**
 * mono_get_exception_not_supported:
 * @msg: the message to pass to the user
 *
 * Returns: a new instance of the System.NotSupportedException
 */
MonoException *
mono_get_exception_not_supported (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "NotSupportedException", msg);
}

/**
 * mono_get_exception_missing_method:
 * @class_name: the class where the lookup was performed.
 * @member_name: the name of the missing method.
 *
 * Returns: a new instance of the System.MissingMethodException
 */
MonoException *
mono_get_exception_missing_method (const char *class_name, const char *member_name)
{
	MonoString *s1 = mono_string_new (mono_domain_get (), class_name);
	MonoString *s2 = mono_string_new (mono_domain_get (), member_name);

	return mono_exception_from_name_two_strings (mono_get_corlib (), "System",
						     "MissingMethodException", s1, s2);
}

/**
 * mono_get_exception_missing_field:
 * @class_name: the class where the lookup was performed
 * @member_name: the name of the missing method.
 *
 * Returns: a new instance of the System.MissingFieldException
 */
MonoException *
mono_get_exception_missing_field (const char *class_name, const char *member_name)
{
	MonoString *s1 = mono_string_new (mono_domain_get (), class_name);
	MonoString *s2 = mono_string_new (mono_domain_get (), member_name);

	return mono_exception_from_name_two_strings (mono_get_corlib (), "System",
						     "MissingFieldException", s1, s2);
}

/**
 * mono_get_exception_argument_null:
 * @arg: the name of the argument that is null
 *
 * Returns: a new instance of the System.ArgumentNullException
 */
MonoException*
mono_get_exception_argument_null (const char *arg)
{
	MonoException *ex;

	ex = mono_exception_from_name ( 
		mono_get_corlib (), "System", "ArgumentNullException");

	if (arg) {
		MonoArgumentException *argex = (MonoArgumentException *)ex;
		MONO_OBJECT_SETREF (argex, param_name, mono_string_new (mono_object_get_domain ((MonoObject*)ex), arg));
	}
	
	return ex;
}

/**
 * mono_get_exception_argument:
 * @arg: the name of the invalid argument.
 *
 * Returns: a new instance of the System.ArgumentException
 */
MonoException *
mono_get_exception_argument (const char *arg, const char *msg)
{
	MonoException *ex;

	ex = mono_exception_from_name_msg (
		mono_get_corlib (), "System", "ArgumentException", msg);

	if (arg) {
		MonoArgumentException *argex = (MonoArgumentException *)ex;
		MONO_OBJECT_SETREF (argex, param_name, mono_string_new (mono_object_get_domain ((MonoObject*)ex), arg));
	}
	
	return ex;
}

/**
 * mono_get_exception_argument_out_of_range:
 * @arg: the name of the out of range argument.
 *
 * Returns: a new instance of the System.ArgumentOutOfRangeException
 */
MonoException *
mono_get_exception_argument_out_of_range (const char *arg)
{
	MonoException *ex;

	ex = mono_exception_from_name (
		mono_get_corlib (), "System", "ArgumentOutOfRangeException");

	if (arg) {
		MonoArgumentException *argex = (MonoArgumentException *)ex;
		MONO_OBJECT_SETREF (argex, param_name, mono_string_new (mono_object_get_domain ((MonoObject*)ex), arg));
	}
	
	return ex;
}

/**
 * mono_get_exception_thread_state:
 * @msg: the message to present to the user
 *
 * Returns: a new instance of the System.Threading.ThreadStateException
 */
MonoException *
mono_get_exception_thread_state (const char *msg)
{
	return mono_exception_from_name_msg (
		mono_get_corlib (), "System.Threading", "ThreadStateException", msg);
}

/**
 * mono_get_exception_io:
 * @msg: the message to present to the user
 *
 * Returns: a new instance of the System.IO.IOException
 */
MonoException *
mono_get_exception_io (const char *msg)
{
	return mono_exception_from_name_msg ( 
		mono_get_corlib (), "System.IO", "IOException", msg);
}

/**
 * mono_get_exception_file_not_found:
 * @fname: the name of the file not found.
 *
 * Returns: a new instance of the System.IO.FileNotFoundException
 */
MonoException *
mono_get_exception_file_not_found (MonoString *fname)
{
	return mono_exception_from_name_two_strings (
		mono_get_corlib (), "System.IO", "FileNotFoundException", fname, fname);
}

/**
 * mono_get_exception_file_not_found2:
 * @msg: an informative message for the user.
 * @fname: the name of the file not found.
 *
 * Returns: a new instance of the System.IO.FileNotFoundException
 */
MonoException *
mono_get_exception_file_not_found2 (const char *msg, MonoString *fname)
{
	MonoString *s = msg ? mono_string_new (mono_domain_get (), msg) : NULL;

	return mono_exception_from_name_two_strings (
		mono_get_corlib (), "System.IO", "FileNotFoundException", s, fname);
}

/**
 * mono_get_exception_type_initialization:
 * @type_name: the name of the type that failed to initialize.
 * @inner: the inner exception.
 *
 * Returns: a new instance of the System.TypeInitializationException
 */
MonoException *
mono_get_exception_type_initialization (const gchar *type_name, MonoException *inner)
{
	MonoClass *klass;
	gpointer args [2];
	MonoObject *exc;
	MonoMethod *method;
	gpointer iter;

	klass = mono_class_from_name (mono_get_corlib (), "System", "TypeInitializationException");
	g_assert (klass);

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

	args [0] = mono_string_new (mono_domain_get (), type_name);
	args [1] = inner;

	exc = mono_object_new (mono_domain_get (), klass);
	mono_runtime_invoke (method, exc, args, NULL);

	return (MonoException *) exc;
}

/**
 * mono_get_exception_synchronization_lock:
 * @inner: the inner exception.
 *
 * Returns: a new instance of the System.SynchronizationLockException
 */
MonoException *
mono_get_exception_synchronization_lock (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System.Threading", "SynchronizationLockException", msg);
}

/**
 * mono_get_exception_cannot_unload_appdomain:
 * @inner: the inner exception.
 *
 * Returns: a new instance of the System.CannotUnloadAppDomainException
 */
MonoException *
mono_get_exception_cannot_unload_appdomain (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "CannotUnloadAppDomainException", msg);
}

/**
 * mono_get_exception_appdomain_unloaded
 *
 * Returns: a new instance of the System.AppDomainUnloadedException
 */
MonoException *
mono_get_exception_appdomain_unloaded (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "AppDomainUnloadedException");
}

/**
 * mono_get_exception_bad_image_format:
 * @msg: an informative message for the user.
 *
 * Returns: a new instance of the System.BadImageFormatException
 */
MonoException *
mono_get_exception_bad_image_format (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "BadImageFormatException", msg);
}	

/**
 * mono_get_exception_bad_image_format2:
 * @msg: an informative message for the user.
 * @fname: The full name of the file with the invalid image.
 *
 * Returns: a new instance of the System.BadImageFormatException
 */
MonoException *
mono_get_exception_bad_image_format2 (const char *msg, MonoString *fname)
{
	MonoString *s = msg ? mono_string_new (mono_domain_get (), msg) : NULL;

	return mono_exception_from_name_two_strings (
		mono_get_corlib (), "System", "BadImageFormatException", s, fname);
}

/**
 * mono_get_exception_stack_overflow:
 *
 * Returns: a new instance of the System.StackOverflowException
 */
MonoException *
mono_get_exception_stack_overflow (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "StackOverflowException");	
}

/**
 * mono_get_exception_out_of_memory:
 *
 * Returns: a new instance of the System.OutOfMemoryException
 */
MonoException *
mono_get_exception_out_of_memory (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "OutOfMemoryException");
}

/**
 * mono_get_exception_field_access:
 *
 * Returns: a new instance of the System.FieldAccessException
 */
MonoException *
mono_get_exception_field_access (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "FieldAccessException");
}

/**
 * mono_get_exception_field_access2:
 * @msg: an informative message for the user.
 *
 * Returns: a new instance of the System.FieldAccessException
 */
MonoException *
mono_get_exception_field_access_msg (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "FieldAccessException", msg);
}

/**
 * mono_get_exception_method_access:
 *
 * Returns: a new instance of the System.MethodAccessException
 */
MonoException *
mono_get_exception_method_access (void)
{
	return mono_exception_from_name (mono_get_corlib (), "System", "MethodAccessException");
}

/**
 * mono_get_exception_method_access2:
 * @msg: an informative message for the user.
 *
 * Returns: a new instance of the System.MethodAccessException
 */
MonoException *
mono_get_exception_method_access_msg (const char *msg)
{
	return mono_exception_from_name_msg (mono_get_corlib (), "System", "MethodAccessException", msg);
}

/**
 * mono_get_exception_reflection_type_load:
 * @types: an array of types that were defined in the moduled loaded.
 * @exceptions: an array of exceptions that were thrown during the type loading.
 *
 * Returns: a new instance of the System.Reflection.ReflectionTypeLoadException
 */
MonoException *
mono_get_exception_reflection_type_load (MonoArray *types, MonoArray *exceptions)
{
	MonoClass *klass;
	gpointer args [2];
	MonoObject *exc;
	MonoMethod *method;
	gpointer iter;

	klass = mono_class_from_name (mono_get_corlib (), "System.Reflection", "ReflectionTypeLoadException");
	g_assert (klass);
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

	args [0] = types;
	args [1] = exceptions;

	exc = mono_object_new (mono_domain_get (), klass);
	mono_runtime_invoke (method, exc, args, NULL);

	return (MonoException *) exc;
}

MonoException *
mono_get_exception_runtime_wrapped (MonoObject *wrapped_exception)
{
	MonoRuntimeWrappedException *ex = (MonoRuntimeWrappedException*)
		mono_exception_from_name (mono_get_corlib (), "System.Runtime.CompilerServices",
								  "RuntimeWrappedException");

   MONO_OBJECT_SETREF (ex, wrapped_exception, wrapped_exception);
   return (MonoException*)ex;
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
mono_exception_get_native_backtrace (MonoException *exc)
{
#ifdef HAVE_BACKTRACE_SYMBOLS
	MonoDomain *domain;
	MonoArray *arr = exc->native_trace_ips;
	int i, len;
	GString *text;
	char **messages;

	if (!arr)
		return g_strdup ("");
	domain = mono_domain_get ();
	len = mono_array_length (arr);
	text = g_string_new_len (NULL, len * 20);
	messages = backtrace_symbols (mono_array_addr (arr, gpointer, 0), len);


	for (i = 0; i < len; ++i) {
		gpointer ip = mono_array_get (arr, gpointer, i);
		MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), ip);
		if (ji) {
			char *msg = mono_debug_print_stack_frame (ji->method, (char*)ip - (char*)ji->code_start, domain);
			g_string_append_printf (text, "%s\n", msg);
			g_free (msg);
		} else {
			g_string_append_printf (text, "%s\n", messages [i]);
		}
	}

	free (messages);
	return g_string_free (text, FALSE);
#else
	return g_strdup ("");
#endif
}

MonoString *
ves_icall_Mono_Runtime_GetNativeStackTrace (MonoException *exc)
{
	char *trace;
	MonoString *res;
	if (!exc)
		mono_raise_exception (mono_get_exception_argument_null ("exception"));

	trace = mono_exception_get_native_backtrace (exc);
	res = mono_string_new (mono_domain_get (), trace);
	g_free (trace);
	return res;
}
