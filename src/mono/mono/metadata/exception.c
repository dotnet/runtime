/*
 * exception.c: Exception handling
 *
 * Authors:
 *	Paolo Molaro    (lupus@ximian.com)
 *	Dietmar Maurer  (dietmar@ximian.com)
 *	Dick Porter     (dick@ximian.com)
 *      Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001, 2002 Ximian, Inc.
 */

#include <mono/metadata/exception.h>
#include <mono/metadata/class.h>
#include <mono/metadata/appdomain.h>
#include <string.h>

/**
 * mono_exception_from_name:
 * @image: the Mono image where to look for the class
 * @name_space: the namespace for the class
 * @name: class name
 *
 * Creates an exception of the given namespace/name class.
 *
 * Returns: the initialized exception instance.
 */
MonoException *
mono_exception_from_name (MonoImage *image, const char *name_space,
			  const char *name)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *klass;
	MonoObject *o;

	klass = mono_class_from_name (image, name_space, name);

	o = mono_object_new (domain, klass);
	g_assert (o != NULL);
	
	mono_runtime_object_init (o);

	return (MonoException *)o;
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
static MonoException *
mono_exception_from_name_two_strings (MonoImage *image, const char *name_space,
									  const char *name, MonoString *a1, MonoString *a2)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *klass;
	MonoMethod *method = NULL;
	MonoObject *o;
	int i;
	gpointer args [2];
	
	klass = mono_class_from_name (image, name_space, name);
	o = mono_object_new (domain, klass);

	for (i = 0; i < klass->method.count; ++i) {
		MonoMethodSignature *sig;
		
		if (strcmp (".ctor", klass->methods [i]->name))
			continue;
		sig = klass->methods [i]->signature;
		if (sig->param_count != 2)
			continue;

		if (sig->params [0]->type != MONO_TYPE_STRING ||
		    sig->params [1]->type != MONO_TYPE_STRING)
			continue;
		method = klass->methods [i];
	}

	args [0] = a1;
	args [1] = a2;
	mono_runtime_invoke (method, o, args, NULL);
	return (MonoException *) o;
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
static MonoException *
mono_exception_from_name_msg (MonoImage *image, const char *name_space,
							  const char *name, const guchar *msg)
{
	MonoException *ex;
	MonoDomain *domain;

    ex = mono_exception_from_name (image, name_space, name);

	domain = ((MonoObject *)ex)->vtable->domain;

	if (msg)
		ex->message = mono_string_new (domain, msg);

	return ex;
}

MonoException *
mono_get_exception_divide_by_zero ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "DivideByZeroException");
}

MonoException *
mono_get_exception_security ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "SecurityException");
}

MonoException *
mono_get_exception_thread_abort ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System.Threading",
					 "ThreadAbortException");
}

MonoException *
mono_get_exception_arithmetic ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "ArithmeticException");
}

MonoException *
mono_get_exception_overflow ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "OverflowException");
}

MonoException *
mono_get_exception_null_reference ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "NullReferenceException");
}

MonoException *
mono_get_exception_execution_engine (const guchar *msg)
{
	return mono_exception_from_name_msg (mono_defaults.corlib, "System",
										 "ExecutionEngineException", msg);
}

MonoException *
mono_get_exception_serialization (const guchar *msg)
{
	return mono_exception_from_name_msg (mono_defaults.corlib, "System.Runtime.Serialization",
										 "SerializationException", msg);
}

MonoException *
mono_get_exception_invalid_cast ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "InvalidCastException");
}

MonoException *
mono_get_exception_index_out_of_range ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "IndexOutOfRangeException");
}

MonoException *
mono_get_exception_array_type_mismatch ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "ArrayTypeMismatchException");
}

MonoException *
mono_get_exception_type_load (MonoString *type_name)
{
	MonoTypeLoadException *exc;
	
	exc = (MonoTypeLoadException *) mono_exception_from_name (mono_defaults.corlib,
					"System",
					"TypeLoadException");

	exc->type_name = type_name;
	return (MonoException *) exc;
}

MonoException *
mono_get_exception_not_implemented ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "NotImplementedException");
}

MonoException *
mono_get_exception_missing_method ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "MissingMethodException");
}

MonoException*
mono_get_exception_argument_null (const guchar *arg)
{
	MonoException *ex;
	MonoDomain *domain;

	ex = mono_exception_from_name ( 
		mono_defaults.corlib, "System", "ArgumentNullException");

	domain = ((MonoObject *)ex)->vtable->domain;

	if (arg)
		((MonoArgumentException *)ex)->param_name =
			mono_string_new (domain, arg);
	
	return ex;
}

MonoException *
mono_get_exception_argument (const guchar *arg, const guchar *msg)
{
	MonoException *ex;
	MonoDomain *domain;

	ex = mono_exception_from_name_msg (
		mono_defaults.corlib, "System", "ArgumentException", msg);

	domain = ((MonoObject *)ex)->vtable->domain;

	if (arg)
		((MonoArgumentException *)ex)->param_name =
			mono_string_new (domain, arg);
	
	return ex;
}

MonoException *
mono_get_exception_argument_out_of_range (const guchar *arg)
{
	MonoException *ex;
	MonoDomain *domain;

	ex = mono_exception_from_name (
		mono_defaults.corlib, "System", "ArgumentOutOfRangeException");

	domain = ((MonoObject *)ex)->vtable->domain;

	if (arg)
		((MonoArgumentException *)ex)->param_name =
			mono_string_new (domain, arg);
	
	return ex;
}

MonoException *
mono_get_exception_thread_state (const guchar *msg)
{
	return mono_exception_from_name_msg (mono_defaults.corlib, 
										 "System.Threading", "ThreadStateException",
										 msg);
}

MonoException *
mono_get_exception_io (const guchar *msg)
{
	return mono_exception_from_name_msg ( 
		mono_defaults.corlib, "System.IO", "IOException", msg);
}

MonoException *
mono_get_exception_file_not_found (MonoString *fname)
{
	return mono_exception_from_name_two_strings (
		mono_defaults.corlib, "System.IO", "FileNotFoundException", fname, fname);
}

MonoException *
mono_get_exception_type_initialization (const gchar *type_name, MonoException *inner)
{
	MonoClass *klass;
	gpointer args [2];
	MonoObject *exc;
	MonoMethod *method;
	gint i;

	klass = mono_class_from_name (mono_defaults.corlib, "System", "TypeInitializationException");
	g_assert (klass);

	mono_class_init (klass);

	/* TypeInitializationException only has 1 ctor with 2 args */
	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if (!strcmp (".ctor", method->name) && method->signature->param_count == 2)
			break;
		method = NULL;
	}

	g_assert (method);

	args [0] = mono_string_new (mono_domain_get (), type_name);
	args [1] = inner;

	exc = mono_object_new (mono_domain_get (), klass);
	mono_runtime_invoke (method, exc, args, NULL);

	return (MonoException *) exc;
}

MonoException *
mono_get_exception_synchronization_lock (const guchar *msg)
{
	return mono_exception_from_name_msg (mono_defaults.corlib, "System.Threading", "SynchronizationLockException", msg);
}

MonoException *
mono_get_exception_cannot_unload_appdomain (const guchar *msg)
{
	return mono_exception_from_name_msg (mono_defaults.corlib, "System", "CannotUnloadAppDomainException", msg);
}

MonoException *
mono_get_exception_appdomain_unloaded (void)
{
	return mono_exception_from_name (mono_defaults.corlib, "System", "AppDomainUnloadedException");
}

MonoException *
mono_get_exception_bad_image_format (const guchar *msg)
{
	return mono_exception_from_name_msg (mono_defaults.corlib, "System", "BadImageFormatException", msg);
}	
