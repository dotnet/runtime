/*
 * object.c: Object creation for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#include <sys/mman.h>
#include <limits.h>    /* for PAGESIZE */
#ifndef PAGESIZE
#define PAGESIZE 4096
#endif
#endif

static void
default_runtime_object_init (MonoObject *o)
{
	return;
}

MonoRuntimeObjectInit mono_runtime_object_init = default_runtime_object_init;

void
mono_install_runtime_object_init (MonoRuntimeObjectInit func)
{
	mono_runtime_object_init = func? func: default_runtime_object_init;
}

/**
 * mono_object_allocate:
 * @size: number of bytes to allocate
 *
 * This is a very simplistic routine until we have our GC-aware
 * memory allocator. 
 *
 * Returns: an allocated object of size @size, or NULL on failure.
 */
void *
mono_object_allocate (size_t size)
{
	void *o = calloc (1, size);

	return o;
}

/**
 * mono_object_free:
 *
 * Frees the memory used by the object.  Debugging purposes
 * only, as we will have our GC system.
 */
void
mono_object_free (MonoObject *o)
{
	MonoClass *c = o->klass;
	
	memset (o, 0, c->instance_size);
	free (o);
}

/**
 * mono_object_new:
 * @klass: the class of the object that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @klass
 */
MonoObject *
mono_object_new (MonoClass *klass)
{
	MonoObject *o;

	if (!klass->inited)
		mono_class_init (klass);

	o = mono_object_allocate (klass->instance_size);
	o->klass = klass;

	return o;
}

/**
 * mono_object_new_from_token:
 * @image: Context where the type_token is hosted
 * @token: a token of the type that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @token in the @image image
 */
MonoObject *
mono_object_new_from_token  (MonoImage *image, guint32 token)
{
	MonoClass *class;

	class = mono_class_get (image, token);

	return mono_object_new (class);
}


/**
 * mono_object_clone:
 * @obj: the object to clone
 *
 * Returns: A newly created object who is a shallow copy of @obj
 */
MonoObject *
mono_object_clone (MonoObject *obj)
{
	MonoObject *o;
	int size;
	
	size = obj->klass->instance_size;
	o = mono_object_allocate (size);
	/* FIXME: handle arrays... */
	
	memcpy (o, obj, size);

	return o;
}

MonoArray*
mono_array_new_full (MonoClass *array_class, guint32 *lengths, guint32 *lower_bounds)
{
	guint32 byte_len;
	MonoObject *o;
	MonoArray *array;
	MonoArrayBounds *bounds;
	int i;

	if (!array_class->inited)
		mono_class_init (array_class);
	byte_len = mono_array_element_size (array_class);

	bounds = g_malloc0 (sizeof (MonoArrayBounds) * array_class->rank);
	for (i = 0; i < array_class->rank; ++i) {
		bounds [i].length = lengths [i];
		byte_len *= lengths [i];
	}

	if (lower_bounds)
		for (i = 0; i < array_class->rank; ++i)
			bounds [i].lower_bound = lower_bounds [i];
	/* 
	 * Following three lines almost taken from mono_object_new ():
	 * they need to be kept in sync.
	 */
	o = mono_object_allocate (sizeof (MonoArray) + byte_len);
	if (!o)
		G_BREAKPOINT ();
	o->klass = array_class;

	array = (MonoArray*)o;

	array->bounds = bounds;
	return array;
}

/*
 * mono_array_new:
 * @image: image where the object is being referenced
 * @eclass: element class
 * @n: number of array elements
 *
 * This routine creates a new szarray with @n elements of type @token
 */
MonoArray *
mono_array_new (MonoClass *eclass, guint32 n)
{
	MonoClass *ac;

	ac = mono_array_class_get (eclass, 1);
	g_assert (ac != NULL);

	return mono_array_new_full (ac, &n, NULL);
}

/**
 * mono_string_new_utf16:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString *
mono_string_new_utf16 (const guint16 *text, gint32 len)
{
	MonoString *s;
	MonoArray *ca;

	s = (MonoString*)mono_object_new (mono_defaults.string_class);
	g_assert (s != NULL);

	ca = (MonoArray *)mono_array_new (mono_defaults.string_class, len);
	g_assert (ca != NULL);
	
	s->c_str = ca;
	s->length = len;

	memcpy (ca->vector, text, len * 2);

	return s;
}

/**
 * mono_string_new:
 * @text: a pointer to an utf8 string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString*
mono_string_new (const char *text)
{
	MonoString *o;
	guint16 *ut;
	int i, l;

	/* fixme: use some kind of unicode library here */

	l = strlen (text);
	ut = g_malloc (l*2);

	for (i = 0; i < l; i++)
		ut [i] = text[i];
	
	o = mono_string_new_utf16 (ut, l);

	g_free (ut);

	return o;
}

/**
 * mono_value_box:
 * @class: the class of the value
 * @value: a pointer to the unboxed data
 *
 * Returns: A newly created object which contains @value.
 */
MonoObject *
mono_value_box (MonoClass *class, gpointer value)
{
	MonoObject *res;
	int size;

	g_assert (class->valuetype);

	size = mono_class_instance_size (class);
	res = mono_object_allocate (size);
	res->klass = class;

	size = size - sizeof (MonoObject);

	memcpy ((char *)res + sizeof (MonoObject), value, size);

	return res;
}

/**
 * mono_object_isinst:
 * @obj: an object
 * @klass: a pointer to a class 
 *
 * Returns: @obj if @obj is derived from @klass
 */
MonoObject *
mono_object_isinst (MonoObject *obj, MonoClass *klass)
{
	MonoClass *oklass;

	if (!obj)
		return NULL;

	oklass = obj->klass;

	if (!klass->inited)
		mono_class_init (klass);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		if ((klass->interface_id <= oklass->max_interface_id) &&
		    oklass->interface_offsets [klass->interface_id])
			return obj;
	} else {
		if ((oklass->baseval - klass->baseval) <= klass->diffval)
			return obj;
	}

	return NULL;
}

static GHashTable *ldstr_table = NULL;

static int
ldstr_hash (const char* str)
{
	guint len, h;
	const char *end;
	len = mono_metadata_decode_blob_size (str, &str);
	end = str + len;
	h = *str;
	/*
	 * FIXME: The distribution may not be so nice with lots of
	 * null chars in the string.
	 */
	for (str += 1; str < end; str++)
		h = (h << 5) - h + *str;
	return h;
}

static gboolean
ldstr_equal (const char *str1, const char *str2) {
	int len;
	if ((len=mono_metadata_decode_blob_size (str1, &str1)) !=
				mono_metadata_decode_blob_size (str2, &str2))
		return 0;
	return memcmp (str1, str2, len) == 0;
}

typedef struct {
	MonoString *obj;
	MonoString *found;
} InternCheck;

static void
check_interned (gpointer key, MonoString *value, InternCheck *check)
{
	if (value == check->obj)
		check->found = value;
}

MonoString*
mono_string_is_interned (MonoString *o)
{
	InternCheck check;
	check.obj = o;
	check.found = NULL;
	/*
	 * Yes, this is slow. Our System.String implementation needs to be redone.
	 * And GLib needs foreach() methods that can be stopped halfway.
	 */
	g_hash_table_foreach (ldstr_table, (GHFunc)check_interned, &check);
	return check.found;
}

MonoString*
mono_string_intern (MonoString *str)
{
	MonoString *res;
	char *ins = g_malloc (4 + str->length * 2);
	char *p;
	
	/* Encode the length */
	p = ins;
	mono_metadata_encode_value (str->length, p, &p);
	memcpy (p, str->c_str->vector, str->length * 2);
	
	if ((res = g_hash_table_lookup (ldstr_table, str))) {
		g_free (ins);
		return res;
	}
	g_hash_table_insert (ldstr_table, ins, str);
	return str;
}

MonoString*
mono_ldstr (MonoImage *image, guint32 index)
{
	const char *str, *sig;
	MonoString *o;
	size_t len2;
	
	if (!ldstr_table)
		ldstr_table = g_hash_table_new ((GHashFunc)ldstr_hash, (GCompareFunc)ldstr_equal);
	
	sig = str = mono_metadata_user_string (image, index);
	
	if ((o = g_hash_table_lookup (ldstr_table, str)))
		return o;
	
	len2 = mono_metadata_decode_blob_size (str, &str);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP16(x) (x) = GUINT16_FROM_LE ((x))
	{
		gint i;
		guint16 *s;

		/* FIXME: it will be better to just add WRITE and after get it to previous state */
		mprotect ((void *) ((int) str & ~(PAGESIZE - 1)), len2 + ((int) str & (PAGESIZE - 1)),
				    PROT_READ | PROT_WRITE | PROT_EXEC);
		len2 >>= 1;
		for (i = 0, s = (guint16 *) str; i < len2; i++, s++) {
			*s = ((*s & 0xff) << 8) | (*s >> 8);
		}
	}
#undef SWAP16
#else
		len2 >>= 1;
#endif

	o = mono_string_new_utf16 ((guint16*)str, len2);
	g_hash_table_insert (ldstr_table, (gpointer)sig, o);

	return o;
}

char *
mono_string_to_utf8 (MonoString *s)
{
	char *as, *vector;
	int i;

	g_assert (s != NULL);

	if (!s->length || !s->c_str)
		return g_strdup ("");

	vector = (char*)s->c_str->vector;

	g_assert (vector != NULL);

	as = g_malloc (s->length + 1);

	/* fixme: replace with a real unicode/ansi conversion */
	for (i = 0; i < s->length; i++) {
		as [i] = vector [i*2];
	}

	as [i] = '\0';

	return as;
}

static void
default_ex_handler (MonoException *ex)
{
	MonoObject *o = (MonoObject*)ex;
	g_error ("Exception %s.%s raised in C code", o->klass->name_space, o->klass->name);
}

static MonoExceptionFunc ex_handler = default_ex_handler;

void
mono_install_handler        (MonoExceptionFunc func)
{
	ex_handler = func? func: default_ex_handler;
}

void
mono_raise_exception (MonoException *ex) 
{
	ex_handler (ex);
}

