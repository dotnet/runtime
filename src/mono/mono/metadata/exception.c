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

MonoObject*
mono_exception_from_name (MonoImage *image, const char *name_space,
			  const char *name)
{
	MonoClass *klass;
	MonoObject *o;

	klass = mono_class_from_name (image, name_space, name);

	o = mono_object_new (klass);
	g_assert (o != NULL);
	
	mono_runtime_object_init (o);

	return o;
}

MonoObject*
mono_get_exception_divide_by_zero ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "DivideByZeroException");
	return ex;
}

MonoObject*
mono_get_exception_security ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "SecurityException");
	return ex;
}

MonoObject*
mono_get_exception_arithmetic ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "ArithmeticException");
	return ex;
}

MonoObject*
mono_get_exception_overflow ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "OverflowException");
	return ex;
}

MonoObject*
mono_get_exception_null_reference ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "NullReferenceException");
	return ex;
}

MonoObject*
mono_get_exception_execution_engine ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "ExecutionEngineException");
	return ex;
}

MonoObject*
mono_get_exception_invalid_cast ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "InvalidCastException");
	return ex;
}

MonoObject*
mono_get_exception_index_out_of_range ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "IndexOutOfRangeException");
	return ex;
}

MonoObject*
mono_get_exception_array_type_mismatch ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "ArrayTypeMismatchException");
	return ex;
}

MonoObject*
mono_get_exception_missing_method ()
{
	static MonoObject *ex = NULL;
	if (ex)
		return ex;
	ex = mono_exception_from_name (mono_defaults.corlib, "System",
				       "MissingMethodException");
	return ex;
}

MonoException*
mono_get_exception_argument_null (const guchar *arg)
{
	MonoException *ex;

	ex = (MonoException *)mono_exception_from_name (
	        mono_defaults.corlib, "System", "ArgumentNullException");

	if (arg)
		((MonoArgumentException *)ex)->param_name =
			mono_string_new (arg);
	
	return ex;
}

MonoException *
mono_get_exception_argument (const guchar *arg, const guchar *msg)
{
	MonoException *ex;

	ex = (MonoException *)mono_exception_from_name (
	        mono_defaults.corlib, "System", "ArgumentException");

	if (msg)
		ex->message = mono_string_new (msg);
	
	if (arg)
		((MonoArgumentException *)ex)->param_name =
			mono_string_new (arg);
	
	return ex;
}

MonoException *
mono_get_exception_io (const guchar *msg)
{
	MonoException *ex;

	ex=(MonoException *)mono_exception_from_name(
	        mono_defaults.corlib, "System.IO", "IOException");

	ex->message=mono_string_new(msg);
	
	return(ex);
}


