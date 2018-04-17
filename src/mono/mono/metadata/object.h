/**
 * \file
 */

#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/metadata/object-forward.h>
#include <mono/metadata/class.h>
#include <mono/utils/mono-error.h>

MONO_BEGIN_DECLS

typedef mono_byte MonoBoolean;

typedef struct _MonoString MONO_RT_MANAGED_ATTR MonoString;
typedef struct _MonoArray MONO_RT_MANAGED_ATTR MonoArray;
typedef struct _MonoReflectionMethod MONO_RT_MANAGED_ATTR MonoReflectionMethod;
typedef struct _MonoReflectionAssembly MONO_RT_MANAGED_ATTR MonoReflectionAssembly;
typedef struct _MonoReflectionModule MONO_RT_MANAGED_ATTR MonoReflectionModule;
typedef struct _MonoReflectionField MONO_RT_MANAGED_ATTR MonoReflectionField;
typedef struct _MonoReflectionProperty MONO_RT_MANAGED_ATTR MonoReflectionProperty;
typedef struct _MonoReflectionEvent MONO_RT_MANAGED_ATTR MonoReflectionEvent;
typedef struct _MonoReflectionType MONO_RT_MANAGED_ATTR MonoReflectionType;
typedef struct _MonoDelegate MONO_RT_MANAGED_ATTR MonoDelegate;
typedef struct _MonoThreadsSync MonoThreadsSync;
typedef struct _MonoThread MONO_RT_MANAGED_ATTR MonoThread;
typedef struct _MonoDynamicAssembly MonoDynamicAssembly;
typedef struct _MonoDynamicImage MonoDynamicImage;
typedef struct _MonoReflectionMethodBody MONO_RT_MANAGED_ATTR MonoReflectionMethodBody;
typedef struct _MonoAppContext MONO_RT_MANAGED_ATTR MonoAppContext;

typedef struct MONO_RT_MANAGED_ATTR _MonoObject {
	MonoVTable *vtable;
	MonoThreadsSync *synchronisation;
} MonoObject;

typedef MonoObject* (*MonoInvokeFunc)	     (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);
typedef void*    (*MonoCompileFunc)	     (MonoMethod *method);
typedef void	    (*MonoMainThreadFunc)    (void* user_data);

#define MONO_OBJECT_SETREF(obj,fieldname,value) do {	\
		mono_gc_wbarrier_set_field ((MonoObject*)(obj), &((obj)->fieldname), (MonoObject*)value);	\
		/*(obj)->fieldname = (value);*/	\
	} while (0)

/* This should be used if 's' can reside on the heap */
#define MONO_STRUCT_SETREF(s,field,value) do { \
        mono_gc_wbarrier_generic_store (&((s)->field), (MonoObject*)(value)); \
    } while (0)

#define mono_array_addr(array,type,index) ((type*)(void*) mono_array_addr_with_size (array, sizeof (type), index))
#define mono_array_get(array,type,index) ( *(type*)mono_array_addr ((array), type, (index)) ) 
#define mono_array_set(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr ((array), type, (index));	\
		*__p = (value);	\
	} while (0)
#define mono_array_setref(array,index,value)	\
	do {	\
		void **__p = (void **) mono_array_addr ((array), void*, (index));	\
		mono_gc_wbarrier_set_arrayref ((array), __p, (MonoObject*)(value));	\
		/* *__p = (value);*/	\
	} while (0)
#define mono_array_memcpy_refs(dest,destidx,src,srcidx,count)	\
	do {	\
		void **__p = (void **) mono_array_addr ((dest), void*, (destidx));	\
		void **__s = mono_array_addr ((src), void*, (srcidx));	\
		mono_gc_wbarrier_arrayref_copy (__p, __s, (count));	\
	} while (0)

MONO_API mono_unichar2 *mono_string_chars  (MonoString *s);
MONO_API int            mono_string_length (MonoString *s);

MONO_RT_EXTERNAL_ONLY MONO_API MonoObject *
mono_object_new		    (MonoDomain *domain, MonoClass *klass);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_new_specific    (MonoVTable *vtable);

/* can be used for classes without finalizer in non-profiling mode */
MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_new_fast	    (MonoVTable *vtable);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_new_alloc_specific (MonoVTable *vtable);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_new_from_token  (MonoDomain *domain, MonoImage *image, uint32_t token);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoArray*
mono_array_new		    (MonoDomain *domain, MonoClass *eclass, uintptr_t n);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoArray*
mono_array_new_full	    (MonoDomain *domain, MonoClass *array_class,
			     uintptr_t *lengths, intptr_t *lower_bounds);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoArray *
mono_array_new_specific	    (MonoVTable *vtable, uintptr_t n);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoArray*
mono_array_clone	    (MonoArray *array);

MONO_API char*
mono_array_addr_with_size   (MonoArray *array, int size, uintptr_t idx);

MONO_API uintptr_t
mono_array_length           (MonoArray *array);

MONO_API MonoString*
mono_string_empty	      (MonoDomain *domain);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_empty_wrapper   (void);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_new_utf16	    (MonoDomain *domain, const mono_unichar2 *text, int32_t len);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_new_size	    (MonoDomain *domain, int32_t len);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_ldstr		    (MonoDomain *domain, MonoImage *image, uint32_t str_index);

MONO_API MonoString*
mono_string_is_interned	    (MonoString *str);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_intern	    (MonoString *str);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_new		    (MonoDomain *domain, const char *text);

MONO_API MonoString*
mono_string_new_wrapper	    (const char *text);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_new_len	    (MonoDomain *domain, const char *text, unsigned int length);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString*
mono_string_new_utf32	    (MonoDomain *domain, const mono_unichar4 *text, int32_t len);

MONO_RT_EXTERNAL_ONLY
MONO_API char *
mono_string_to_utf8	    (MonoString *string_obj);

MONO_API char *
mono_string_to_utf8_checked (MonoString *string_obj, MonoError *error);

MONO_API mono_unichar2 *
mono_string_to_utf16	    (MonoString *string_obj);

MONO_API mono_unichar4 *
mono_string_to_utf32	    (MonoString *string_obj);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString *
mono_string_from_utf16	    (mono_unichar2 *data);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString *
mono_string_from_utf32	    (mono_unichar4 *data);

MONO_API mono_bool
mono_string_equal           (MonoString *s1, MonoString *s2);

MONO_API unsigned int
mono_string_hash            (MonoString *s);

MONO_API int
mono_object_hash            (MonoObject* obj);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString *
mono_object_to_string (MonoObject *obj, MonoObject **exc);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_value_box		    (MonoDomain *domain, MonoClass *klass, void* val);

MONO_API void
mono_value_copy             (void* dest, void* src, MonoClass *klass);

MONO_API void
mono_value_copy_array       (MonoArray *dest, int dest_idx, void* src, int count);

MONO_API MonoVTable *
mono_object_get_vtable      (MonoObject *obj);

MONO_API MonoDomain*
mono_object_get_domain      (MonoObject *obj);

MONO_API MonoClass*
mono_object_get_class       (MonoObject *obj);

MONO_API void*
mono_object_unbox	    (MonoObject *obj);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_clone	    (MonoObject *obj);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_isinst	    (MonoObject *obj, MonoClass *klass);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_isinst_mbyref   (MonoObject *obj, MonoClass *klass);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_object_castclass_mbyref (MonoObject *obj, MonoClass *klass);

MONO_API mono_bool 
mono_monitor_try_enter       (MonoObject *obj, uint32_t ms);

MONO_API mono_bool
mono_monitor_enter           (MonoObject *obj);

MONO_API void
mono_monitor_enter_v4        (MonoObject *obj, char *lock_taken);

MONO_API unsigned int
mono_object_get_size         (MonoObject *o);

MONO_API void 
mono_monitor_exit            (MonoObject *obj);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_raise_exception	    (MonoException *ex);

MONO_RT_EXTERNAL_ONLY
MONO_API mono_bool
mono_runtime_set_pending_exception (MonoException *exc, mono_bool overwrite);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_reraise_exception	    (MonoException *ex);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_runtime_object_init    (MonoObject *this_obj);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_runtime_class_init	    (MonoVTable *vtable);

MONO_API MonoDomain *
mono_vtable_domain          (MonoVTable *vtable);

MONO_API MonoClass *
mono_vtable_class           (MonoVTable *vtable);

MONO_API MonoMethod*
mono_object_get_virtual_method (MonoObject *obj, MonoMethod *method);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject*
mono_runtime_invoke	    (MonoMethod *method, void *obj, void **params,
			     MonoObject **exc);

MONO_API MonoMethod *
mono_get_delegate_invoke    (MonoClass *klass);

MONO_API MonoMethod *
mono_get_delegate_begin_invoke (MonoClass *klass);

MONO_API MonoMethod *
mono_get_delegate_end_invoke (MonoClass *klass);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject*
mono_runtime_delegate_invoke (MonoObject *delegate, void **params, 
			      MonoObject **exc);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject*
mono_runtime_invoke_array   (MonoMethod *method, void *obj, MonoArray *params,
			     MonoObject **exc);

MONO_RT_EXTERNAL_ONLY
MONO_API void*
mono_method_get_unmanaged_thunk (MonoMethod *method);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoArray*
mono_runtime_get_main_args  (void);

MONO_API void
mono_runtime_exec_managed_code (MonoDomain *domain,
				MonoMainThreadFunc main_func,
				void* main_args);

MONO_RT_EXTERNAL_ONLY
MONO_API int
mono_runtime_run_main	    (MonoMethod *method, int argc, char* argv[], 
			     MonoObject **exc);

MONO_RT_EXTERNAL_ONLY
MONO_API int
mono_runtime_exec_main	    (MonoMethod *method, MonoArray *args,
			     MonoObject **exc);

MONO_API int
mono_runtime_set_main_args  (int argc, char* argv[]);

/* The following functions won't be available with mono was configured with remoting disabled. */
/*#ifndef DISABLE_REMOTING */
MONO_RT_EXTERNAL_ONLY
MONO_API void*
mono_load_remote_field (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, void **res);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_load_remote_field_new (MonoObject *this_obj, MonoClass *klass, MonoClassField *field);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_store_remote_field (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, void* val);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_store_remote_field_new (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, MonoObject *arg);

/* #endif */

MONO_API void
mono_unhandled_exception    (MonoObject *exc);

MONO_API void
mono_print_unhandled_exception (MonoObject *exc);

MONO_RT_EXTERNAL_ONLY
MONO_API void* 
mono_compile_method	   (MonoMethod *method);

/* accessors for fields and properties */
MONO_API void
mono_field_set_value (MonoObject *obj, MonoClassField *field, void *value);

MONO_API void
mono_field_static_set_value (MonoVTable *vt, MonoClassField *field, void *value);

MONO_API void
mono_field_get_value (MonoObject *obj, MonoClassField *field, void *value);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_field_static_get_value (MonoVTable *vt, MonoClassField *field, void *value);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject *
mono_field_get_value_object (MonoDomain *domain, MonoClassField *field, MonoObject *obj);

MONO_RT_EXTERNAL_ONLY
MONO_API void
mono_property_set_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoObject*
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
MONO_API uint32_t      mono_gchandle_new         (MonoObject *obj, mono_bool pinned);
MONO_API uint32_t      mono_gchandle_new_weakref (MonoObject *obj, mono_bool track_resurrection);
MONO_API MonoObject*  mono_gchandle_get_target  (uint32_t gchandle);
MONO_API void         mono_gchandle_free        (uint32_t gchandle);

/* Reference queue support
 *
 * A reference queue is used to get notifications of when objects are collected.
 * Call mono_gc_reference_queue_new to create a new queue and pass the callback that
 * will be invoked when registered objects are collected.
 * Call mono_gc_reference_queue_add to register a pair of objects and data within a queue.
 * The callback will be triggered once an object is both unreachable and finalized.
 */

typedef void (*mono_reference_queue_callback) (void *user_data);
typedef struct _MonoReferenceQueue MonoReferenceQueue;

MONO_API MonoReferenceQueue* mono_gc_reference_queue_new (mono_reference_queue_callback callback);
MONO_API void mono_gc_reference_queue_free (MonoReferenceQueue *queue);
MONO_API mono_bool mono_gc_reference_queue_add (MonoReferenceQueue *queue, MonoObject *obj, void *user_data);



/* GC write barriers support */
MONO_API void mono_gc_wbarrier_set_field     (MonoObject *obj, void* field_ptr, MonoObject* value);
MONO_API void mono_gc_wbarrier_set_arrayref  (MonoArray *arr, void* slot_ptr, MonoObject* value);
MONO_API void mono_gc_wbarrier_arrayref_copy (void* dest_ptr, void* src_ptr, int count);
MONO_API void mono_gc_wbarrier_generic_store (void* ptr, MonoObject* value);
MONO_API void mono_gc_wbarrier_generic_store_atomic (void *ptr, MonoObject *value);
MONO_API void mono_gc_wbarrier_generic_nostore (void* ptr);
MONO_API void mono_gc_wbarrier_value_copy    (void* dest, void* src, int count, MonoClass *klass);
MONO_API void mono_gc_wbarrier_object_copy   (MonoObject* obj, MonoObject *src);

MONO_END_DECLS

#endif

