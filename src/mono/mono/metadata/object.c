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

MonoStats mono_stats;

void
mono_runtime_object_init (MonoObject *this)
{
	int i;
	MonoMethod *method = NULL;
	MonoClass *klass = this->vtable->klass;

	for (i = 0; i < klass->method.count; ++i) {
		if (!strcmp (".ctor", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == 0) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method);

	mono_runtime_invoke (method, this, NULL);
}

/*
 * runtime_class_init:
 * @klass: klass that needs to be initialized
 *
 * This routine calls the class constructor for @class.
 */
void
mono_runtime_class_init (MonoClass *klass)
{
	int i;

	for (i = 0; i < klass->method.count; ++i) {
		MonoMethod *method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
		    (strcmp (".cctor", method->name) == 0)) {
			mono_runtime_invoke (method, NULL, NULL);
			return;
		}
	}
	/* No class constructor found */
}

static MonoInvokeFunc default_mono_runtime_invoke = NULL;

MonoObject*
mono_runtime_invoke (MonoMethod *method, void *obj, void **params)
{
	if (!default_mono_runtime_invoke) {
		g_error ("runtime invoke called on uninitialized runtime");
		return NULL;
	}
	return default_mono_runtime_invoke (method, obj, params);
}

int
mono_runtime_exec_main (MonoMethod *method, MonoArray *args)
{
	gpointer pa [1];

	pa [0] = args;

	if (method->signature->ret->type == MONO_TYPE_I4) {
		MonoObject *res;
		res = mono_runtime_invoke (method, NULL, pa);
		return *(guint32 *)((char *)res + sizeof (MonoObject));
	} else {
		mono_runtime_invoke (method, NULL, pa);
		return 0;
	}
}

void
mono_install_runtime_invoke (MonoInvokeFunc func)
{
	default_mono_runtime_invoke = func;
}

MonoObject*
mono_runtime_invoke_array (MonoMethod *method, void *obj, MonoArray *params)
{
	MonoMethodSignature *sig = method->signature;
	gpointer *pa;
	int i;

	pa = alloca (sizeof (gpointer) * params->bounds->length);

	for (i = 0; i < params->bounds->length; i++) {
		if (sig->params [i]->byref) {
			/* nothing to do */
		}

		switch (sig->params [i]->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U:
		case MONO_TYPE_I:
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
		case MONO_TYPE_VALUETYPE:
			pa [i] = (char *)(((gpointer *)params->vector)[i]) + sizeof (MonoObject);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
			pa [i] = (char *)(((gpointer *)params->vector)[i]);
			break;
		default:
			g_error ("type 0x%x not handled in ves_icall_InternalInvoke", sig->params [i]->type);
		}
	}

	if (!strcmp (method->name, ".ctor")) {
		obj = mono_object_new (mono_domain_get (), method->klass);
		mono_runtime_invoke (method, obj, pa);
		return obj;
	} else
		return mono_runtime_invoke (method, obj, pa);
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

	mono_stats.new_object_count++;

	if (!klass->inited)
		mono_class_init (klass);

	if (klass->ghcimpl) {
		o = mono_object_allocate (klass->instance_size);
	} else {
		guint32 *t;
		t = mono_object_allocate (klass->instance_size + 4);
		*t = ++uoid;
		o = (MonoObject *)(++t);
	}
	o->vtable = mono_class_vtable (domain, klass);

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

/*
 * mono_array_new_full:
 * @domain: domain where the object is created
 * @array_class: array class
 * @lengths: lengths for each dimension in the array
 * @lower_bounds: lower bounds for each dimension in the array (may be NULL)
 *
 * This routine creates a new array objects with the given dimensions,
 * lower bounds and type.
 */
MonoArray*
mono_array_new_full (MonoDomain *domain, MonoClass *array_class, 
		     guint32 *lengths, guint32 *lower_bounds)
{
	guint32 byte_len, len;
	MonoObject *o;
	MonoArray *array;
	MonoArrayBounds *bounds;
	int i;

	if (!array_class->inited)
		mono_class_init (array_class);

	byte_len = mono_array_element_size (array_class);
	len = 1;

#if HAVE_BOEHM_GC
	bounds = GC_debug_malloc (sizeof (MonoArrayBounds) * array_class->rank, "bounds", 0);
#else
	bounds = g_malloc0 (sizeof (MonoArrayBounds) * array_class->rank);
#endif
	for (i = 0; i < array_class->rank; ++i) {
		bounds [i].length = lengths [i];
		len *= lengths [i];
	}

	if (lower_bounds)
		for (i = 0; i < array_class->rank; ++i)
			bounds [i].lower_bound = lower_bounds [i];
	byte_len *= len;
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
	array->max_length = len;

	return array;
}

/*
 * mono_array_new:
 * @domain: domain where the object is created
 * @eclass: element class
 * @n: number of array elements
 *
 * This routine creates a new szarray with @n elements of type @eclass.
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

/*
 * mono_string_new_len:
 * @text: a pointer to an utf8 string
 * @length: number of bytes in @text to consider
 *
 * Returns: A newly created string object which contains @text.
 */
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

/*
 * mono_string_new_wrapper:
 * @text: pointer to utf8 characters.
 *
 * Helper function to create a string object from @text in the current domain.
 */
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
	MonoDomain *domain;
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
	domain = ((MonoObject *)str)->vtable->domain;
	ldstr_table = domain->ldstr_table;
	mono_domain_lock (domain);
	if ((res = mono_g_hash_table_lookup (ldstr_table, ins))) {
		mono_domain_unlock (domain);
		g_free (ins);
		return res;
	}
	if (insert) {
		mono_g_hash_table_insert (ldstr_table, ins, str);
		mono_domain_unlock (domain);
		return str;
	}
	mono_domain_unlock (domain);
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

/*
 * mono_ldstr:
 * @domain: the domain where the string will be used.
 * @image: a metadata context
 * @idx: index into the user string table.
 * 
 * Implementation for the ldstr opcode.
 */
MonoString*
mono_ldstr (MonoDomain *domain, MonoImage *image, guint32 idx)
{
	const char *str, *sig;
	MonoString *o;
	size_t len2;
		
	sig = str = mono_metadata_user_string (image, idx);

	mono_domain_lock (domain);
	if ((o = mono_g_hash_table_lookup (domain->ldstr_table, sig))) {
		mono_domain_unlock (domain);
		return o;
	}
	
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
	mono_domain_unlock (domain);

	return o;
}

/*
 * mono_string_to_utf8:
 * @s: a System.String
 *
 * Return the UTF8 representation for @s.
 * the resulting buffer nedds to be freed with g_free().
 */
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

/*
 * mono_string_to_utf16:
 * @s: a MonoString
 *
 * Return an null-terminated array of the utf-16 chars
 * contained in @s. The result must be freed with g_free().
 * This is a temporary helper until our string implementation
 * is reworked to always include the null terminating char.
 */
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

/*
 * mono_raise_exception:
 * @ex: exception object
 *
 * Signal the runtime that the exception @ex has been raised in unmanaged code.
 */
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

void
mono_message_init (MonoDomain *domain,
		   MonoMethodMessage *this, 
		   MonoReflectionMethod *method,
		   MonoArray *out_args)
{
	MonoMethodSignature *sig = method->method->signature;
	MonoString *name;
	int i, j;
	char **names;
	guint8 arg_type;

	this->method = method;

	this->args = mono_array_new (domain, mono_defaults.object_class, sig->param_count);
	this->arg_types = mono_array_new (domain, mono_defaults.byte_class, sig->param_count);

	names = g_new (char *, sig->param_count);
	mono_method_get_param_names (method->method, (const char **) names);
	this->names = mono_array_new (domain, mono_defaults.string_class, sig->param_count);
	
	for (i = 0; i < sig->param_count; i++) {
		 name = mono_string_new (domain, names [i]);
		 mono_array_set (this->names, gpointer, i, name);	
	}

	g_free (names);
	
	for (i = 0, j = 0; i < sig->param_count; i++) {

		if (sig->params [i]->byref) {
			if (out_args) {
				gpointer arg = mono_array_get (out_args, gpointer, j);
				mono_array_set (this->args, gpointer, i, arg);
				j++;
			}
			arg_type = 2;
			if (sig->params [i]->attrs & PARAM_ATTRIBUTE_IN)
				arg_type |= 1;
		} else {
			arg_type = 1;
		}

		mono_array_set (this->arg_types, guint8, i, arg_type);
	}
}

/**
 * mono_remoting_invoke:
 * @real_proxy: pointer to a RealProxy object
 * @msg: The MonoMethodMessage to execute
 * @exc: used to store exceptions
 * @out_args: used to store output arguments
 *
 * This is used to call RealProxy::Invoke(). RealProxy::Invoke() returns an
 * IMessage interface and it is not trivial to extract results from there. So
 * we call an helper method PrivateInvoke instead of calling
 * RealProxy::Invoke() directly.
 *
 * Returns: the result object.
 */
MonoObject *
mono_remoting_invoke (MonoObject *real_proxy, MonoMethodMessage *msg, 
		      MonoObject **exc, MonoArray **out_args)
{
	static MonoMethod *im = NULL;
	gpointer pa [4];

	//static MonoObject *(*invoke) (gpointer, gpointer, MonoObject **, MonoArray **) = NULL;

	/* fixme: make this domain dependent */
	if (!im) {
		MonoClass *klass;
		int i;

		klass = mono_defaults.real_proxy_class; 
		       
		for (i = 0; i < klass->method.count; ++i) {
			if (!strcmp ("PrivateInvoke", klass->methods [i]->name) &&
			    klass->methods [i]->signature->param_count == 4) {
				im = klass->methods [i];
				break;
			}
		}
	
		g_assert (im);
	}

	pa [0] = real_proxy;
	pa [1] = msg;
	pa [2] = exc;
	pa [3] = out_args;

	return mono_runtime_invoke (im, NULL, pa);
}

MonoObject *
mono_message_invoke (MonoObject *target, MonoMethodMessage *msg, 
		     MonoObject **exc, MonoArray **out_args) 
{
	if (target && target->vtable->klass == mono_defaults.transparent_proxy_class) {

		return mono_remoting_invoke ((MonoObject *)((MonoTransparentProxy *)target)->rp, 
					     msg, exc, out_args);

	} else {
		MonoDomain *domain = mono_domain_get (); 
		MonoMethod *method = msg->method->method;
		MonoMethodSignature *sig = method->signature;
		MonoObject *res;
		int i, j, outarg_count = 0;

		for (i = 0; i < sig->param_count; i++) {
			if (sig->params [i]->byref) 
				outarg_count++;
		}

		*out_args = mono_array_new (domain, mono_defaults.object_class, outarg_count);
		*exc = NULL;

		for (i = 0, j = 0; i < sig->param_count; i++) {
			if (sig->params [i]->byref) {
				gpointer arg;
				arg = mono_array_get (msg->args, gpointer, i);
				mono_array_set (*out_args, gpointer, j, arg);
				j++;
			}
		}

		return mono_runtime_invoke_array (method, target, msg->args);
	}
}



