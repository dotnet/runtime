/*
 * exception.c: Exception handling
 *
 * Authors:
 *	Paolo Molaro (lupus@ximian.com)
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <mono/metadata/exception.h>
#include <mono/metadata/class.h>
#include <mono/metadata/appdomain.h>

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
	MonoException *ex;
	MonoDomain *domain;

	ex = mono_exception_from_name (mono_defaults.corlib, "System",
					 "ExecutionEngineException");

	domain = ((MonoObject *)ex)->vtable->domain;

	if (msg)
		ex->message = mono_string_new (domain, msg);

	return ex;
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
mono_get_exception_type_load ()
{
	return mono_exception_from_name (mono_defaults.corlib, "System",
					 "TypeLoadException");
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

	ex = (MonoException *)mono_exception_from_name ( 
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

	ex = (MonoException *)mono_exception_from_name (
	        mono_defaults.corlib, "System", "ArgumentException");

	domain = ((MonoObject *)ex)->vtable->domain;

	if (msg)
		ex->message = mono_string_new (domain, msg);
	
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

	ex = (MonoException *)mono_exception_from_name (
	        mono_defaults.corlib, "System", "ArgumentOutOfRangeException");

	domain = ((MonoObject *)ex)->vtable->domain;

	if (arg)
		((MonoArgumentException *)ex)->param_name =
			mono_string_new (domain, arg);
	
	return ex;
}

MonoException *
mono_get_exception_io (const guchar *msg)
{
	MonoException *ex;
	MonoDomain *domain;

	ex=(MonoException *)mono_exception_from_name ( 
	        mono_defaults.corlib, "System.IO", "IOException");

	domain = ((MonoObject *)ex)->vtable->domain;

	ex->message=mono_string_new (domain, msg);
	
	return(ex);
}


