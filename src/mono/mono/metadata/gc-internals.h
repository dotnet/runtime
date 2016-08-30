/*
 * metadata/gc-internals.h: Internal GC interface
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_METADATA_GC_INTERNAL_H__
#define __MONO_METADATA_GC_INTERNAL_H__

#include <glib.h>
#include <mono/utils/gc_wrapper.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/sgen/gc-internal-agnostic.h>
#include <mono/utils/gc_wrapper.h>

#define mono_domain_finalizers_lock(domain) mono_os_mutex_lock (&(domain)->finalizable_objects_hash_lock);
#define mono_domain_finalizers_unlock(domain) mono_os_mutex_unlock (&(domain)->finalizable_objects_hash_lock);

/* Register a memory area as a conservatively scanned GC root */
#define MONO_GC_REGISTER_ROOT_PINNING(x,src,msg) mono_gc_register_root ((char*)&(x), sizeof(x), MONO_GC_DESCRIPTOR_NULL, (src), (msg))

#define MONO_GC_UNREGISTER_ROOT(x) mono_gc_deregister_root ((char*)&(x))

/*
 * Register a memory location as a root pointing to memory allocated using
 * mono_gc_alloc_fixed (). This includes MonoGHashTable.
 */
/* The result of alloc_fixed () is not GC tracked memory */
#define MONO_GC_REGISTER_ROOT_FIXED(x,src,msg) do { \
	if (!mono_gc_is_moving ())				\
		MONO_GC_REGISTER_ROOT_PINNING ((x),(src),(msg)); \
	} while (0)

/*
 * Return a GC descriptor for an array containing N pointers to memory allocated
 * by mono_gc_alloc_fixed ().
 */
/* For SGEN, the result of alloc_fixed () is not GC tracked memory */
#define MONO_GC_ROOT_DESCR_FOR_FIXED(n) (mono_gc_is_moving () ? mono_gc_make_root_descr_all_refs (0) : MONO_GC_DESCRIPTOR_NULL)

/* Register a memory location holding a single object reference as a GC root */
#define MONO_GC_REGISTER_ROOT_SINGLE(x,src,msg) do { \
	g_assert (sizeof (x) == sizeof (MonoObject*)); \
	mono_gc_register_root ((char*)&(x), sizeof(MonoObject*), mono_gc_make_root_descr_all_refs (1), (src), (msg)); \
	} while (0)

/*
 * This is used for fields which point to objects which are kept alive by other references
 * when using Boehm.
 */
#define MONO_GC_REGISTER_ROOT_IF_MOVING(x,src,msg) do { \
	if (mono_gc_is_moving ()) \
		MONO_GC_REGISTER_ROOT_SINGLE(x,src,msg);		\
} while (0)

#define MONO_GC_UNREGISTER_ROOT_IF_MOVING(x) do { \
	if (mono_gc_is_moving ()) \
		MONO_GC_UNREGISTER_ROOT (x);			\
} while (0)

/* useful until we keep track of gc-references in corlib etc. */
#define IS_GC_REFERENCE(class,t) (mono_gc_is_moving () ? FALSE : ((t)->type == MONO_TYPE_U && (class)->image == mono_defaults.corlib))

void   mono_object_register_finalizer               (MonoObject  *obj);
void   ves_icall_System_GC_InternalCollect          (int          generation);
gint64 ves_icall_System_GC_GetTotalMemory           (MonoBoolean  forceCollection);
void   ves_icall_System_GC_KeepAlive                (MonoObject  *obj);
void   ves_icall_System_GC_ReRegisterForFinalize    (MonoObject  *obj);
void   ves_icall_System_GC_SuppressFinalize         (MonoObject  *obj);
void   ves_icall_System_GC_WaitForPendingFinalizers (void);

MonoObject *ves_icall_System_GCHandle_GetTarget (guint32 handle);
guint32     ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type);
void        ves_icall_System_GCHandle_FreeHandle (guint32 handle);
gpointer    ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle);
void        ves_icall_System_GC_register_ephemeron_array (MonoObject *array);
MonoObject  *ves_icall_System_GC_get_ephemeron_tombstone (void);


extern void mono_gc_init (void);
extern void mono_gc_base_init (void);
extern void mono_gc_cleanup (void);
extern void mono_gc_base_cleanup (void);

/*
 * Return whenever the current thread is registered with the GC (i.e. started
 * by the GC pthread wrappers on unix.
 */
extern gboolean mono_gc_is_gc_thread (void);

extern gboolean mono_gc_is_finalizer_internal_thread (MonoInternalThread *thread);

extern void mono_gc_set_stack_end (void *stack_end);

/* only valid after the RECLAIM_START GC event and before RECLAIM_END
 * Not exported in public headers, but can be linked to (unsupported).
 */
gboolean mono_object_is_alive (MonoObject* obj);
gboolean mono_gc_is_finalizer_thread (MonoThread *thread);
gpointer mono_gc_out_of_memory (size_t size);
void     mono_gc_enable_events (void);
void     mono_gc_enable_alloc_events (void);

void mono_gchandle_set_target (guint32 gchandle, MonoObject *obj);

/*Ephemeron functionality. Sgen only*/
gboolean    mono_gc_ephemeron_array_add (MonoObject *obj);

MonoBoolean
mono_gc_GCHandle_CheckCurrentDomain (guint32 gchandle);

/* User defined marking function */
/* It should work like this:
 * foreach (ref in GC references in the are structure pointed to by ADDR)
 *    mark_func (ref)
 */
typedef void (*MonoGCMarkFunc)     (MonoObject **addr, void *gc_data);
typedef void (*MonoGCRootMarkFunc) (void *addr, MonoGCMarkFunc mark_func, void *gc_data);

/* Create a descriptor with a user defined marking function */
MonoGCDescriptor mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker);

/* Return whenever user defined marking functions are supported */
gboolean mono_gc_user_markers_supported (void);

/* desc is the result from mono_gc_make_descr*. A NULL value means
 * all the words might contain GC pointers.
 * The memory is non-moving and it will be explicitly deallocated.
 * size bytes will be available from the returned address (ie, descr
 * must not be stored in the returned memory)
 * NOTE: Under Boehm, this returns memory allocated using GC_malloc, so the result should
 * be stored into a location registered using MONO_GC_REGISTER_ROOT_FIXED ().
 */
void* mono_gc_alloc_fixed            (size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg);
void  mono_gc_free_fixed             (void* addr);

/* make sure the gchandle was allocated for an object in domain */
gboolean mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain);
void     mono_gchandle_free_domain  (MonoDomain *domain);

typedef void (*FinalizerThreadCallback) (gpointer user_data);

void* mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size);
void* mono_gc_alloc_obj (MonoVTable *vtable, size_t size);
void* mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length);
void* mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size);
void* mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len);
void* mono_gc_alloc_mature (MonoVTable *vtable, size_t size);
MonoGCDescriptor mono_gc_make_descr_for_string (gsize *bitmap, int numbits);

void  mono_gc_register_for_finalization (MonoObject *obj, void *user_data);
void  mono_gc_add_memory_pressure (gint64 value);
MONO_API int   mono_gc_register_root (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg);
void  mono_gc_deregister_root (char* addr);
void  mono_gc_finalize_domain (MonoDomain *domain);
void  mono_gc_run_finalize (void *obj, void *data);
void  mono_gc_clear_domain (MonoDomain * domain);
/* Signal early termination of finalizer processing inside the gc */
void  mono_gc_suspend_finalizers (void);


/* 
 * Register a root which can only be written using a write barrier.
 * Writes to the root must be done using a write barrier (MONO_ROOT_SETREF).
 * If the root uses an user defined mark routine, the writes are not required to be
 * to the area between START and START+SIZE.
 * The write barrier allows the GC to avoid scanning this root at each collection, so it
 * is more efficient.
 * FIXME: Add an API for clearing remset entries if a root with a user defined
 * mark routine is deleted.
 */
int mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, const char *msg);

void mono_gc_wbarrier_set_root (gpointer ptr, MonoObject *value);

/* Set a field of a root registered using mono_gc_register_root_wbarrier () */
#define MONO_ROOT_SETREF(s,fieldname,value) do {	\
	mono_gc_wbarrier_set_root (&((s)->fieldname), (MonoObject*)value); \
} while (0)

void  mono_gc_finalize_threadpool_threads (void);

/* fast allocation support */

typedef enum {
	// Regular fast path allocator.
	MANAGED_ALLOCATOR_REGULAR,
	// Managed allocator that just calls into the runtime. Used when allocation profiling w/ AOT.
	MANAGED_ALLOCATOR_SLOW_PATH,
} ManagedAllocatorVariant;

int mono_gc_get_aligned_size_for_allocator (int size);
MonoMethod* mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box, gboolean known_instance_size);
MonoMethod* mono_gc_get_managed_array_allocator (MonoClass *klass);
MonoMethod *mono_gc_get_managed_allocator_by_type (int atype, ManagedAllocatorVariant variant);

guint32 mono_gc_get_managed_allocator_types (void);

/* Return a short string identifying the GC, indented to be saved in AOT images */
const char *mono_gc_get_gc_name (void);

/* Fast write barriers */
MonoMethod* mono_gc_get_specific_write_barrier (gboolean is_concurrent);
MonoMethod* mono_gc_get_write_barrier (void);

/* Fast valuetype copy */
void mono_gc_wbarrier_value_copy_bitmap (gpointer dest, gpointer src, int size, unsigned bitmap);

/* helper for the managed alloc support */
MonoString *
ves_icall_string_alloc (int length);

/* 
 * Functions supplied by the runtime and called by the GC. Currently only used
 * by SGEN.
 */
typedef struct {
	/* 
	 * Function called during thread startup/attach to allocate thread-local data 
	 * needed by the other functions.
	 */
	gpointer (*thread_attach_func) (void);
	/* 
	 * Function called during thread deatch to free the data allocated by
	 * thread_attach_func.
	 */
	void (*thread_detach_func) (gpointer user_data);
	/* 
	 * Function called from every thread when suspending for GC. It can save
	 * data needed for marking from thread stacks. user_data is the data returned 
	 * by attach_func. This might called with GC locks held and the word stopped,
	 * so it shouldn't do any synchronization etc.
	 */
	void (*thread_suspend_func) (gpointer user_data, void *sigcontext, MonoContext *ctx);
	/* 
	 * Function called to mark from thread stacks. user_data is the data returned 
	 * by attach_func. This is called twice, with the word stopped:
	 * - in the first pass, it should mark areas of the stack using
	 *   conservative marking by calling mono_gc_conservatively_scan_area ().
	 * - in the second pass, it should mark the remaining areas of the stack
	 *   using precise marking by calling mono_gc_scan_object ().
	 */
	void (*thread_mark_func) (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise, void *gc_data);
	/*
	 * Function called for debugging to get the current managed method for
	 * tracking the provenances of objects.
	 */
	gpointer (*get_provenance_func) (void);
} MonoGCCallbacks;

/* Set the callback functions callable by the GC */
void mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks);
MonoGCCallbacks *mono_gc_get_gc_callbacks (void);

/* Functions callable from the thread mark func */

/* Scan the memory area between START and END conservatively */
void mono_gc_conservatively_scan_area (void *start, void *end);

/* Scan OBJ, returning its new address */
void *mono_gc_scan_object (void *obj, void *gc_data);

/* Return the suspend signal number used by the GC to suspend threads,
   or -1 if not applicable. */
int mono_gc_get_suspend_signal (void);

/* Return the suspend signal number used by the GC to suspend threads,
   or -1 if not applicable. */
int mono_gc_get_restart_signal (void);

/*
 * Return a human readable description of the GC in malloc-ed memory.
 */
char* mono_gc_get_description (void);

/*
 * Configure the GC to desktop mode
 */
void mono_gc_set_desktop_mode (void);

/*
 * Return whenever this GC can move objects
 */
gboolean mono_gc_is_moving (void);

typedef void* (*MonoGCLockedCallbackFunc) (void *data);

void* mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data);

int mono_gc_get_los_limit (void);

guint8* mono_gc_get_card_table (int *shift_bits, gpointer *card_mask);
gboolean mono_gc_card_table_nursery_check (void);

void* mono_gc_get_nursery (int *shift_bits, size_t *size);

void mono_gc_set_current_thread_appdomain (MonoDomain *domain);

void mono_gc_set_skip_thread (gboolean skip);

#ifndef HOST_WIN32
int mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg);
#endif

/*
 * Return whenever GC is disabled
 */
gboolean mono_gc_is_disabled (void);

/*
 * Return whenever this is the null GC
 */
gboolean mono_gc_is_null (void);

void mono_gc_set_string_length (MonoString *str, gint32 new_length);

#if defined(__MACH__)
void mono_gc_register_mach_exception_thread (pthread_t thread);
pthread_t mono_gc_get_mach_exception_thread (void);
#endif

gboolean mono_gc_precise_stack_mark_enabled (void);

typedef struct _RefQueueEntry RefQueueEntry;

struct _RefQueueEntry {
	void *dis_link;
	guint32 gchandle;
	MonoDomain *domain;
	void *user_data;
	RefQueueEntry *next;
};

struct _MonoReferenceQueue {
	RefQueueEntry *queue;
	mono_reference_queue_callback callback;
	MonoReferenceQueue *next;
	gboolean should_be_deleted;
};

enum {
	MONO_GC_FINALIZER_EXTENSION_VERSION = 1,
};

typedef struct {
	int version;
	gboolean (*is_class_finalization_aware) (MonoClass *klass);
	void (*object_queued_for_finalization) (MonoObject *object);
} MonoGCFinalizerCallbacks;

MONO_API void mono_gc_register_finalizer_callbacks (MonoGCFinalizerCallbacks *callbacks);


#ifdef HOST_WIN32
BOOL APIENTRY mono_gc_dllmain (HMODULE module_handle, DWORD reason, LPVOID reserved);
#endif

guint mono_gc_get_vtable_bits (MonoClass *klass);

void mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size);

/* If set, print debugging messages around finalizers. */
extern gboolean log_finalizers;

/* If set, do not run finalizers. */
extern gboolean mono_do_not_finalize;
/* List of names of classes not to finalize. */
extern gchar **mono_do_not_finalize_class_names;

#endif /* __MONO_METADATA_GC_INTERNAL_H__ */

