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
#include <signal.h>
#include <string.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#if HAVE_BOEHM_GC
#include <gc/gc.h>
#endif

static void
default_runtime_object_init (MonoObject *o)
{
	return;
}

MonoRuntimeObjectInit mono_runtime_object_init = default_runtime_object_init;
MonoRuntimeExecMain   mono_runtime_exec_main = NULL;

void
mono_install_runtime_object_init (MonoRuntimeObjectInit func)
{
	mono_runtime_object_init = func? func: default_runtime_object_init;
}

void
mono_install_runtime_exec_main (MonoRuntimeExecMain func)
{
	mono_runtime_exec_main = func;
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
#if HAVE_BOEHM_GC
	void *o = GC_debug_malloc (size, "object", 1);
#else
	void *o = calloc (1, size);
#endif

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
#if HAVE_BOEHM_GC
	g_error ("mono_object_free called with boehm gc.");
#else
	MonoClass *c = o->vtable->klass;
	
	memset (o, 0, c->instance_size);
	free (o);
#endif
}

/**
 * mono_object_new:
 * @klass: the class of the object that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @klass
 */
MonoObject *
mono_object_new (MonoDomain *domain, MonoClass *klass)
{
	static guint32 uoid = 0;
	MonoObject *o;

	if (!klass->inited)
		mono_class_init (klass);


	if (klass->ghcimpl) {
		o = mono_object_allocate (klass->instance_size);
		o->vtable = mono_class_vtable (domain, klass);
	} else {
		guint32 *t;
		t = mono_object_allocate (klass->instance_size + 4);
		*t = ++uoid;
		o = (MonoObject *)(++t);
		o->vtable = mono_class_vtable (domain, klass);
	}

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
mono_object_new_from_token  (MonoDomain *domain, MonoImage *image, guint32 token)
{
	MonoClass *class;

	class = mono_class_get (image, token);

	return mono_object_new (domain, class);
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

	size = obj->vtable->klass->instance_size;
	o = mono_object_allocate (size);

	memcpy (o, obj, size);

	return o;
}

/**
 * mono_array_clone:
 * @array: the array to clone
 *
 * Returns: A newly created array who is a shallow copy of @array
 */
MonoArray*
mono_array_clone (MonoArray *array)
{
	MonoArray *o;
	int size, i;
	guint32 *sizes;
	MonoClass *klass = array->obj.vtable->klass;
	
	sizes = alloca (klass->rank * sizeof(guint32) * 2);
	size = mono_array_element_size (klass);
	for (i = 0; i < klass->rank; ++i) {
		sizes [i] = array->bounds [i].length;
		size *= array->bounds [i].length;
		sizes [i + klass->rank] = array->bounds [i].lower_bound;
	}
	o = mono_array_new_full (((MonoObject *)array)->vtable->domain, 
				 klass, sizes, sizes + klass->rank);
	memcpy (o, array, sizeof(MonoArray) + size);

	return o;
}

MonoArray*
mono_array_new_full (MonoDomain *domain, MonoClass *array_class, 
		     guint32 *lengths, guint32 *lower_bounds)
{
	guint32 byte_len;
	MonoObject *o;
	MonoArray *array;
	MonoArrayBounds *bounds;
	int i;

	if (!array_class->inited)
		mono_class_init (array_class);

	byte_len = mono_array_element_size (array_class);

#if HAVE_BOEHM_GC
	bounds = GC_debug_malloc (sizeof (MonoArrayBounds) * array_class->rank, "bounds", 0);
#else
	bounds = g_malloc0 (sizeof (MonoArrayBounds) * array_class->rank);
#endif
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
	o->vtable = mono_class_vtable (domain, array_class);

	array = (MonoArray*)o;

	array->bounds = bounds;
	array->max_length = bounds [0].length;

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
mono_array_new (MonoDomain *domain, MonoClass *eclass, guint32 n)
{
	MonoClass *ac;

	ac = mono_array_class_get (&eclass->byval_arg, 1);
	g_assert (ac != NULL);

	return mono_array_new_full (domain, ac, &n, NULL);
}

/**
 * mono_string_new_utf16:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString *
mono_string_new_utf16 (MonoDomain *domain, const guint16 *text, gint32 len)
{
	MonoString *s;
	MonoArray *ca;

	s = (MonoString*)mono_object_new (domain, mono_defaults.string_class);
	g_assert (s != NULL);

	ca = (MonoArray *)mono_array_new (domain, mono_defaults.char_class, len);
	g_assert (ca != NULL);
	
	s->c_str = ca;
	s->length = len;

	memcpy (ca->vector, text, len * 2);

	return s;
}

MonoString*
mono_string_new_len (MonoDomain *domain, const char *text, guint length)
{
	GError *error = NULL;
	MonoString *o = NULL;
	guint16 *ut;
	glong items_written;

	
	ut = g_utf8_to_utf16 (text, length, NULL, &items_written, &error);

	if (!error)
		o = mono_string_new_utf16 (domain, ut, items_written);
	else 
		g_error_free (error);

	g_free (ut);

	return o;
}

/**
 * mono_string_new:
 * @text: a pointer to an utf8 string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString*
mono_string_new (MonoDomain *domain, const char *text)
{
	GError *error = NULL;
	MonoString *o = NULL;
	guint16 *ut;
	glong items_written;
	int l;

	l = strlen (text);
	
	ut = g_utf8_to_utf16 (text, l, NULL, &items_written, &error);

	if (!error)
		o = mono_string_new_utf16 (domain, ut, items_written);
	else 
		g_error_free (error);

	g_free (ut);

	return o;
}

MonoString*
mono_string_new_wrapper (const char *text)
{
	MonoDomain *domain = mono_domain_get ();

	return mono_string_new (domain, text);
}

/**
 * mono_value_box:
 * @class: the class of the value
 * @value: a pointer to the unboxed data
 *
 * Returns: A newly created object which contains @value.
 */
MonoObject *
mono_value_box (MonoDomain *domain, MonoClass *class, gpointer value)
{
	MonoObject *res;
	int size;

	g_assert (class->valuetype);

	size = mono_class_instance_size (class);
	res = mono_object_allocate (size);
	res->vtable = mono_class_vtable (domain, class);

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
	MonoVTable *vt;
	MonoClass *oklass;

	if (!obj)
		return NULL;

	vt = obj->vtable;
	oklass = vt->klass;

	if (!klass->inited)
		mono_class_init (klass);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		if ((klass->interface_id <= oklass->max_interface_id) &&
		    vt->interface_offsets [klass->interface_id])
			return obj;
	} else {
		if (oklass == mono_defaults.transparent_proxy_class) {
			/* fixme: add check for IRemotingTypeInfo */
			MonoRealProxy *rp = ((MonoTransparentProxy *)obj)->rp;
			MonoType *type;
			type = ((MonoReflectionType *)rp->class_to_proxy)->type;
			oklass = mono_class_from_mono_type (type);
		}
		if (oklass->rank && oklass->rank == klass->rank) {
			if ((oklass->element_class->baseval - klass->element_class->baseval) <= 
			    klass->element_class->diffval)
				return obj;
		} else if ((oklass->baseval - klass->baseval) <= klass->diffval)
			return obj;
	}

	return NULL;
}

static MonoString*
mono_string_is_interned_lookup (MonoString *str, int insert)
{
	MonoGHashTable *ldstr_table;
	MonoString *res;
	char *ins = g_malloc (4 + str->length * 2);
	char *p;
	int bloblen;
	
	/* Encode the length */
	p = ins;
	mono_metadata_encode_value (2 * str->length, p, &p);
	bloblen = p - ins;
	p = ins;
	mono_metadata_encode_value (bloblen + 2 * str->length, p, &p);
	bloblen = (p - ins) + 2 * str->length;
	/*
	 * ins is stored in the hash table as a key and needs to have the same
	 * representation as in the metadata: we swap the character bytes on big
	 * endian boxes.
	 */
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		int i;
		char *p2 = mono_array_addr (str->c_str, char, 0);
		for (i = 0; i < str->length; ++i) {
			*p++ = p2 [1];
			*p++ = p2 [0];
			p2 += 2;
		}
	}
#else
	memcpy (p, str->c_str->vector, str->length * 2);
#endif
	ldstr_table = ((MonoObject *)str)->vtable->domain->ldstr_table;
	if ((res = mono_g_hash_table_lookup (ldstr_table, ins))) {
		g_free (ins);
		return res;
	}
	if (insert) {
		mono_g_hash_table_insert (ldstr_table, ins, str);
		return str;
	}
	g_free (ins);
	return NULL;
}

MonoString*
mono_string_is_interned (MonoString *o)
{
	return mono_string_is_interned_lookup (o, FALSE);
}

MonoString*
mono_string_intern (MonoString *str)
{
	return mono_string_is_interned_lookup (str, TRUE);
}

MonoString*
mono_ldstr (MonoDomain *domain, MonoImage *image, guint32 idx)
{
	const char *str, *sig;
	MonoString *o;
	size_t len2;
		
	sig = str = mono_metadata_user_string (image, idx);
	
	if ((o = mono_g_hash_table_lookup (domain->ldstr_table, sig)))
		return o;
	
	len2 = mono_metadata_decode_blob_size (str, &str);
	len2 >>= 1;

	o = mono_string_new_utf16 (domain, (guint16*)str, len2);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		int i;
		guint16 *p2 = (guint16*)mono_array_addr (o->c_str, guint16, 0);
		for (i = 0; i < len2; ++i) {
			*p2 = GUINT16_FROM_LE (*p2);
			++p2;
		}
	}
#endif
	mono_g_hash_table_insert (domain->ldstr_table, (gpointer)sig, o);

	return o;
}

char *
mono_string_to_utf8 (MonoString *s)
{
	char *as, *vector;
	GError *error = NULL;

	g_assert (s != NULL);

	if (!s->length || !s->c_str)
		return g_strdup ("");

	vector = (char*)s->c_str->vector;

	g_assert (vector != NULL);

	as = g_utf16_to_utf8 ((gunichar2 *)vector, s->length, NULL, NULL, &error);
	if (error)
		g_warning (error->message);

	return as;
}

gunichar2 *
mono_string_to_utf16 (MonoString *s)
{
	char *as;

	g_assert (s != NULL);

	as = g_malloc ((s->length * 2) + 2);
	as [(s->length * 2)] = '\0';
	as [(s->length * 2) + 1] = '\0';

	if (!s->length || !s->c_str) {
		return (gunichar2 *)(as);
	}
	
	memcpy (as, mono_string_chars(s), s->length * 2);
	
	return (gunichar2 *)(as);
}

static void
default_ex_handler (MonoException *ex)
{
	MonoObject *o = (MonoObject*)ex;
	g_error ("Exception %s.%s raised in C code", o->vtable->klass->name_space, o->vtable->klass->name);
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

MonoWaitHandle *
mono_wait_handle_new (MonoDomain *domain, HANDLE handle)
{
	MonoWaitHandle *res;

	res = (MonoWaitHandle *)mono_object_new (domain, mono_defaults.waithandle_class);

	res->handle = handle;

	return res;
}

MonoAsyncResult *
mono_async_result_new (MonoDomain *domain, HANDLE handle, MonoObject *state, gpointer data)
{
	MonoAsyncResult *res;

	res = (MonoAsyncResult *)mono_object_new (domain, mono_defaults.asyncresult_class);

	res->data = data;
	res->async_state = state;
	res->handle = (MonoObject *)mono_wait_handle_new (domain, handle);
	res->sync_completed = FALSE;
	res->completed = FALSE;

	return res;
}

