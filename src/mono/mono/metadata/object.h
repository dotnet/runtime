#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/metadata/class.h>

G_BEGIN_DECLS

typedef guchar MonoBoolean;

typedef struct _MonoReflectionMethod MonoReflectionMethod;
typedef struct _MonoReflectionAssembly MonoReflectionAssembly;
typedef struct _MonoReflectionModule MonoReflectionModule;
typedef struct _MonoReflectionField MonoReflectionField;
typedef struct _MonoReflectionProperty MonoReflectionProperty;
typedef struct _MonoReflectionEvent MonoReflectionEvent;
typedef struct _MonoReflectionType MonoReflectionType;
typedef struct _MonoDelegate MonoDelegate;
typedef struct _MonoException MonoException;
typedef struct _MonoThreadsSync MonoThreadsSync;
typedef struct _MonoThread MonoThread;
typedef struct _MonoDynamicAssembly MonoDynamicAssembly;
typedef struct _MonoDynamicImage MonoDynamicImage;
typedef struct _MonoReflectionMethodBody MonoReflectionMethodBody;
typedef struct _MonoAppContext MonoAppContext;

typedef struct {
	MonoVTable *vtable;
	MonoThreadsSync *synchronisation;
} MonoObject;

#ifdef MONO_BIG_ARRAYS
typedef guint64 mono_array_size_t;
#define MONO_ARRAY_MAX_INDEX G_MAXINT64
#define MONO_ARRAY_MAX_SIZE  G_MAXUINT64
#else
typedef guint32 mono_array_size_t;
#define MONO_ARRAY_MAX_INDEX ((gint32) 0x7fffffff)
#define MONO_ARRAY_MAX_SIZE  ((guint32) 0xffffffff)
#endif

typedef struct {
	mono_array_size_t length;
	mono_array_size_t lower_bound;
} MonoArrayBounds;

typedef struct {
	MonoObject obj;
	/* bounds is NULL for szarrays */
	MonoArrayBounds *bounds;
	/* total number of elements of the array */
	mono_array_size_t max_length; 
	/* we use double to ensure proper alignment on platforms that need it */
	double vector [MONO_ZERO_LEN_ARRAY];
} MonoArray;

typedef struct {
	MonoObject object;
	gint32 length;
	gunichar2 chars [MONO_ZERO_LEN_ARRAY];
} MonoString;

typedef MonoObject* (*MonoInvokeFunc)	     (MonoMethod *method, void *obj, void **params, MonoObject **exc);
typedef gpointer    (*MonoCompileFunc)	     (MonoMethod *method);
typedef void	    (*MonoMainThreadFunc)    (gpointer user_data);

#define mono_object_class(obj) (((MonoObject*)(obj))->vtable->klass)
#define mono_object_domain(obj) (((MonoObject*)(obj))->vtable->domain)

#define MONO_OBJECT_SETREF(obj,fieldname,value) do {	\
		mono_gc_wbarrier_set_field ((MonoObject*)(obj), &((obj)->fieldname), (MonoObject*)value);	\
		/*(obj)->fieldname = (value);*/	\
	} while (0)

/* This should be used if 's' can reside on the heap */
#define MONO_STRUCT_SETREF(s,field,value) do { \
        mono_gc_wbarrier_generic_store (&((s)->field), (MonoObject*)(value)); \
    } while (0)

#define mono_array_length(array) ((array)->max_length)
#define mono_array_addr(array,type,index) ((type*)(gpointer) mono_array_addr_with_size (array, sizeof (type), index))
#define mono_array_addr_with_size(array,size,index) ( ((char*)(array)->vector) + (size) * (index) )
#define mono_array_get(array,type,index) ( *(type*)mono_array_addr ((array), type, (index)) ) 
#define mono_array_set(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr ((array), type, (index));	\
		*__p = (value);	\
	} while (0)
#define mono_array_setref(array,index,value)	\
	do {	\
		gpointer *__p = (gpointer *) mono_array_addr ((array), gpointer, (index));	\
		mono_gc_wbarrier_set_arrayref ((array), __p, (MonoObject*)(value));	\
		/* *__p = (value);*/	\
	} while (0)
#define mono_array_memcpy_refs(dest,destidx,src,srcidx,count)	\
	do {	\
		gpointer *__p = (gpointer *) mono_array_addr ((dest), gpointer, (destidx));	\
		mono_gc_wbarrier_arrayref_copy ((dest), __p, (count));	\
		memmove (__p, mono_array_addr ((src), gpointer, (srcidx)), (count) * sizeof (gpointer));	\
	} while (0)

#define mono_string_chars(s) ((gunichar2*)(s)->chars)
#define mono_string_length(s) ((s)->length)

MonoObject *
mono_object_new		    (MonoDomain *domain, MonoClass *klass);

MonoObject *
mono_object_new_specific    (MonoVTable *vtable);

/* can be used for classes without finalizer in non-profiling mode */
MonoObject *
mono_object_new_fast	    (MonoVTable *vtable);

MonoObject *
mono_object_new_alloc_specific (MonoVTable *vtable);

MonoObject *
mono_object_new_from_token  (MonoDomain *domain, MonoImage *image, guint32 token);

MonoArray*
mono_array_new		    (MonoDomain *domain, MonoClass *eclass, mono_array_size_t n);

MonoArray*
mono_array_new_full	    (MonoDomain *domain, MonoClass *array_class,
			     mono_array_size_t *lengths, mono_array_size_t *lower_bounds);

MonoArray *
mono_array_new_specific	    (MonoVTable *vtable, mono_array_size_t n);

MonoArray*
mono_array_clone	    (MonoArray *array);

MonoString*
mono_string_new_utf16	    (MonoDomain *domain, const guint16 *text, gint32 len);

MonoString*
mono_string_new_size	    (MonoDomain *domain, gint32 len);

MonoString*
mono_ldstr		    (MonoDomain *domain, MonoImage *image, guint32 str_index);

MonoString*
mono_string_is_interned	    (MonoString *str);

MonoString*
mono_string_intern	    (MonoString *str);

MonoString*
mono_string_new		    (MonoDomain *domain, const char *text);

MonoString*
mono_string_new_wrapper	    (const char *text);

MonoString*
mono_string_new_len	    (MonoDomain *domain, const char *text, guint length);

char *
mono_string_to_utf8	    (MonoString *string_obj);

gunichar2 *
mono_string_to_utf16	    (MonoString *string_obj);

MonoString *
mono_string_from_utf16	    (gunichar2 *data);

gboolean
mono_string_equal           (MonoString *s1, MonoString *s2);

guint
mono_string_hash            (MonoString *s);

int
mono_object_hash            (MonoObject* obj);

MonoObject *
mono_value_box		    (MonoDomain *domain, MonoClass *klass, gpointer val);

void
mono_value_copy             (gpointer dest, gpointer src, MonoClass *klass);

void
mono_value_copy_array       (MonoArray *dest, int dest_idx, gpointer src, int count);

MonoDomain*
mono_object_get_domain      (MonoObject *obj);

MonoClass*
mono_object_get_class       (MonoObject *obj);

gpointer
mono_object_unbox	    (MonoObject *obj);

MonoObject *
mono_object_clone	    (MonoObject *obj);

MonoObject *
mono_object_isinst	    (MonoObject *obj, MonoClass *klass);

MonoObject *
mono_object_isinst_mbyref   (MonoObject *obj, MonoClass *klass);

MonoObject *
mono_object_castclass_mbyref (MonoObject *obj, MonoClass *klass);

gboolean 
mono_monitor_try_enter       (MonoObject *obj, guint32 ms);

gboolean
mono_monitor_enter           (MonoObject *obj);

guint
mono_object_get_size         (MonoObject *o);

void 
mono_monitor_exit            (MonoObject *obj);

void
mono_raise_exception	    (MonoException *ex);

void
mono_runtime_object_init    (MonoObject *this_obj);

void
mono_runtime_class_init	    (MonoVTable *vtable);

MonoMethod*
mono_object_get_virtual_method (MonoObject *obj, MonoMethod *method);

MonoObject*
mono_runtime_invoke	    (MonoMethod *method, void *obj, void **params,
			     MonoObject **exc);

MonoMethod *
mono_get_delegate_invoke    (MonoClass *klass);

MonoObject*
mono_runtime_delegate_invoke (MonoObject *delegate, void **params, 
			      MonoObject **exc);

MonoObject*
mono_runtime_invoke_array   (MonoMethod *method, void *obj, MonoArray *params,
			     MonoObject **exc);

gpointer
mono_method_get_unmanaged_thunk (MonoMethod *method);

MonoArray*
mono_runtime_get_main_args  (void);

void
mono_runtime_exec_managed_code (MonoDomain *domain,
				MonoMainThreadFunc main_func,
				gpointer main_args);

int
mono_runtime_run_main	    (MonoMethod *method, int argc, char* argv[], 
			     MonoObject **exc);

int
mono_runtime_exec_main	    (MonoMethod *method, MonoArray *args,
			     MonoObject **exc);

gpointer
mono_load_remote_field (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, gpointer *res);

MonoObject *
mono_load_remote_field_new (MonoObject *this_obj, MonoClass *klass, MonoClassField *field);

void
mono_store_remote_field (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, gpointer val);

void
mono_store_remote_field_new (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, MonoObject *arg);

void
mono_unhandled_exception    (MonoObject *exc);

void
mono_print_unhandled_exception (MonoObject *exc);

gpointer 
mono_compile_method	   (MonoMethod *method);

MonoRemoteClass*
mono_remote_class (MonoDomain *domain, MonoString *class_name, MonoClass *proxy_class);

/* accessors for fields and properties */
void
mono_field_set_value (MonoObject *obj, MonoClassField *field, void *value);

void
mono_field_static_set_value (MonoVTable *vt, MonoClassField *field, void *value);

void
mono_field_get_value (MonoObject *obj, MonoClassField *field, void *value);

void
mono_field_static_get_value (MonoVTable *vt, MonoClassField *field, void *value);

MonoObject *
mono_field_get_value_object (MonoDomain *domain, MonoClassField *field, MonoObject *obj);

void
mono_property_set_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc);

MonoObject*
mono_property_get_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc);

/* GC handles support 
 *
 * A handle can be created to refer to a managed object and either prevent it
 * from being garbage collected or moved or to be able to know if it has been 
 * collected or not (weak references).
 * mono_gchandle_new () is used to prevent an object from being garbage collected
 * until mono_gchandle_free() is called. Use a TRUE value for the pinned argument to
 * prevent the object from being moved (this should be avoided as much as possible 
 * and this should be used only for shorts periods of time or performance will suffer).
 * To create a weakref use mono_gchandle_new_weakref (): track_resurrection should
 * usually be false (see the GC docs for more details).
 * mono_gchandle_get_target () can be used to get the object referenced by both kinds
 * of handle: for a weakref handle, if an object has been collected, it will return NULL.
 */
guint32      mono_gchandle_new         (MonoObject *obj, gboolean pinned);
guint32      mono_gchandle_new_weakref (MonoObject *obj, gboolean track_resurrection);
MonoObject*  mono_gchandle_get_target  (guint32 gchandle);
void         mono_gchandle_free        (guint32 gchandle);

/* GC write barriers support */
void mono_gc_wbarrier_set_field     (MonoObject *obj, gpointer field_ptr, MonoObject* value);
void mono_gc_wbarrier_set_arrayref  (MonoArray *arr, gpointer slot_ptr, MonoObject* value);
void mono_gc_wbarrier_arrayref_copy (MonoArray *arr, gpointer slot_ptr, int count);
void mono_gc_wbarrier_generic_store (gpointer ptr, MonoObject* value);
void mono_gc_wbarrier_value_copy    (gpointer dest, gpointer src, int count, MonoClass *klass);
void mono_gc_wbarrier_object        (MonoObject* obj);

G_END_DECLS

#endif

