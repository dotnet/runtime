/**
 * \file
 * Internal GC interface
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
#include <mono/metadata/gc_wrapper.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/sgen/gc-internal-agnostic.h>
#include <mono/metadata/icalls.h>
#include <mono/utils/mono-compiler.h>

/* Register a memory area as a conservatively scanned GC root */
#define MONO_GC_REGISTER_ROOT_PINNING(x,src,key,msg) mono_gc_register_root ((char*)&(x), sizeof(x), MONO_GC_DESCRIPTOR_NULL, (src), (key), (msg))

#define MONO_GC_UNREGISTER_ROOT(x) mono_gc_deregister_root ((char*)&(x))

/*
 * The lowest bit is used to mark pinned handles by netcore's GCHandle class. These macros
 * are used to convert between the old int32 representation to a netcore compatible pointer
 * representation.
 */
#define MONO_GC_HANDLE_TO_UINT(ptr) ((guint32)((size_t)(ptr) >> 1))
#define MONO_GC_HANDLE_FROM_UINT(i) ((MonoGCHandle)((size_t)(i) << 1))
/*
 * Return a GC descriptor for an array containing N pointers to memory allocated
 * by mono_gc_alloc_fixed ().
 */
/* For SGEN, the result of alloc_fixed () is not GC tracked memory */
#define MONO_GC_ROOT_DESCR_FOR_FIXED(n) (mono_gc_is_moving () ? mono_gc_make_root_descr_all_refs (0) : MONO_GC_DESCRIPTOR_NULL)

/* Register a memory location holding a single object reference as a GC root */
#define MONO_GC_REGISTER_ROOT_SINGLE(x,src,key,msg) do { \
	g_assert (sizeof (x) == sizeof (MonoObject*)); \
	mono_gc_register_root ((char*)&(x), sizeof(MonoObject*), mono_gc_make_root_descr_all_refs (1), (src), (key),(msg)); \
	} while (0)

/*
 * This is used for fields which point to objects which are kept alive by other references
 * when using Boehm.
 */
#define MONO_GC_REGISTER_ROOT_IF_MOVING(x,src,key,msg) do { \
	if (mono_gc_is_moving ()) \
		MONO_GC_REGISTER_ROOT_SINGLE(x,src,key,msg);		\
} while (0)

#define MONO_GC_UNREGISTER_ROOT_IF_MOVING(x) do { \
	if (mono_gc_is_moving ()) \
		MONO_GC_UNREGISTER_ROOT (x);			\
} while (0)

/* useful until we keep track of gc-references in corlib etc. */
#define IS_GC_REFERENCE(class,t) (mono_gc_is_moving () ? FALSE : ((t)->type == MONO_TYPE_U && (class)->image == mono_defaults.corlib))

void   mono_object_register_finalizer               (MonoObject  *obj);

void
mono_object_register_finalizer_handle (MonoObjectHandle obj);

extern void mono_gc_init (void);
MONO_COMPONENT_API extern void mono_gc_base_init (void);
extern void mono_gc_base_cleanup (void);
extern void mono_gc_init_icalls (void);

/*
 * Return whenever the current thread is registered with the GC (i.e. started
 * by the GC pthread wrappers on unix.
 */
extern gboolean mono_gc_is_gc_thread (void);

MONO_COMPONENT_API extern gboolean mono_gc_is_finalizer_internal_thread (MonoInternalThread *thread);

extern void mono_gc_set_stack_end (void *stack_end);

/* only valid after the RECLAIM_START GC event and before RECLAIM_END
 * Not exported in public headers, but can be linked to (unsupported).
 */
gboolean mono_object_is_alive (MonoObject* obj);
MONO_COMPONENT_API gboolean mono_gc_is_finalizer_thread (MonoThread *thread);

void mono_gchandle_set_target (MonoGCHandle gchandle, MonoObject *obj);

/*Ephemeron functionality. Sgen only*/
gboolean    mono_gc_ephemeron_array_add (MonoObject *obj);

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
 */
MonoObject* mono_gc_alloc_fixed      (size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg);

// C++ callers outside of metadata (mini/tasklets.c) must use mono_gc_alloc_fixed_no_descriptor
// instead of mono_gc_alloc_fixed, or else compile twice -- boehm and sgen.
MonoObject*
mono_gc_alloc_fixed_no_descriptor (size_t size, MonoGCRootSource source, void *key, const char *msg);

void  mono_gc_free_fixed             (void* addr);

typedef void (*FinalizerThreadCallback) (gpointer user_data);

MonoObject*
mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size);

MonoObjectHandle
mono_gc_alloc_handle_pinned_obj (MonoVTable *vtable, gsize size);

MonoObject*
mono_gc_alloc_obj (MonoVTable *vtable, size_t size);

MonoObjectHandle
mono_gc_alloc_handle_obj (MonoVTable *vtable, gsize size);

MonoArray*
mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length);

MonoArray*
mono_gc_alloc_pinned_vector (MonoVTable *vtable, size_t size, uintptr_t max_length);

MonoArrayHandle
mono_gc_alloc_handle_vector (MonoVTable *vtable, gsize size, gsize max_length);

MonoArray*
mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size);

MonoArrayHandle
mono_gc_alloc_handle_array (MonoVTable *vtable, gsize size, gsize max_length, gsize bounds_size);

MonoString*
mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len);

MonoStringHandle
mono_gc_alloc_handle_string (MonoVTable *vtable, gsize size, gint32 len);

MonoObject*
mono_gc_alloc_mature (MonoVTable *vtable, size_t size);

MonoGCDescriptor mono_gc_make_descr_for_string (gsize *bitmap, int numbits);

MonoObjectHandle
mono_gc_alloc_handle_mature (MonoVTable *vtable, gsize size);

void mono_gc_register_obj_with_weak_fields (void *obj);
void
mono_gc_register_object_with_weak_fields (MonoObjectHandle obj);

typedef void (*MonoFinalizationProc)(gpointer, gpointer); // same as SGenFinalizationProc, GC_finalization_proc

void  mono_gc_register_for_finalization (MonoObject *obj, MonoFinalizationProc user_data);
void  mono_gc_add_memory_pressure (gint64 value);
MONO_API int   mono_gc_register_root (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg);
MONO_COMPONENT_API void  mono_gc_deregister_root (char* addr);
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
int mono_gc_register_root_wbarrier (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg);

void mono_gc_wbarrier_set_root (gpointer ptr, MonoObject *value);

/* Set a field of a root registered using mono_gc_register_root_wbarrier () */
#define MONO_ROOT_SETREF(s,fieldname,value) do {	\
	mono_gc_wbarrier_set_root (&((s)->fieldname), (MonoObject*)value); \
} while (0)

/* fast allocation support */

typedef enum {
	// Regular fast path allocator.
	MANAGED_ALLOCATOR_REGULAR,
	// Managed allocator that just calls into the runtime.
	MANAGED_ALLOCATOR_SLOW_PATH,
	// Managed allocator that works like the regular one but also calls into the profiler.
	MANAGED_ALLOCATOR_PROFILER,
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
/* WARNING: [dest, dest + size] must be within the bounds of a single type, otherwise the GC will lose remset entries */
G_EXTERN_C void mono_gc_wbarrier_range_copy (gpointer dest, gconstpointer src, int size);

typedef void (*MonoRangeCopyFunction)(gpointer, gconstpointer, int size);

MonoRangeCopyFunction
mono_gc_get_range_copy_func (void);

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
	/*
	 * Same as thread_mark_func, mark the intepreter frames.
	 */
	void (*interp_mark_func) (gpointer thread_info, GcScanFunc func, gpointer gc_data, gboolean precise);
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
MONO_COMPONENT_API gboolean mono_gc_is_moving (void);

typedef void* (*MonoGCLockedCallbackFunc) (void *data);

void* mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data);

int mono_gc_get_los_limit (void);

guint64 mono_gc_get_allocated_bytes_for_current_thread (void);

guint64 mono_gc_get_total_allocated_bytes (MonoBoolean precise);

void mono_gc_get_gcmemoryinfo (
	gint64 *high_memory_load_threshold_bytes,
	gint64 *memory_load_bytes,
	gint64 *total_available_memory_bytes,
	gint64 *total_committed_bytes,
	gint64 *heap_size_bytes,
	gint64 *fragmented_bytes);

void mono_gc_get_gctimeinfo (
	guint64 *time_last_gc_100ns,
	guint64 *time_since_last_gc_100ns,
	guint64 *time_max_gc_100ns);

guint8* mono_gc_get_card_table (int *shift_bits, gpointer *card_mask);
guint8* mono_gc_get_target_card_table (int *shift_bits, target_mgreg_t *card_mask);
gboolean mono_gc_card_table_nursery_check (void);

void* mono_gc_get_nursery (int *shift_bits, size_t *size);

// Don't use directly; set/unset MONO_THREAD_INFO_FLAGS_NO_GC instead.
void mono_gc_skip_thread_changing (gboolean skip);
void mono_gc_skip_thread_changed (gboolean skip);

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
	MonoGCHandle gchandle;
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

MonoVTable *mono_gc_get_vtable (MonoObject *obj);

guint mono_gc_get_vtable_bits (MonoClass *klass);

void mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size);

gboolean mono_gc_is_critical_method (MonoMethod *method);

G_EXTERN_C // due to THREAD_INFO_TYPE varying
gpointer mono_gc_thread_attach (THREAD_INFO_TYPE *info);

G_EXTERN_C // due to THREAD_INFO_TYPE varying
void mono_gc_thread_detach (THREAD_INFO_TYPE *info);

G_EXTERN_C // due to THREAD_INFO_TYPE varying
void mono_gc_thread_detach_with_lock (THREAD_INFO_TYPE *info);

G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean mono_gc_thread_in_critical_region (THREAD_INFO_TYPE *info);

/* If set, print debugging messages around finalizers. */
extern gboolean mono_log_finalizers;

/* If set, do not run finalizers. */
extern gboolean mono_do_not_finalize;
/* List of names of classes not to finalize. */
extern gchar **mono_do_not_finalize_class_names;

/*
 * Unified runtime stop/restart world, SGEN Only.
 * Will take and release the LOCK_GC.
 */
MONO_COMPONENT_API void mono_stop_world (MonoThreadInfoFlags flags);
MONO_COMPONENT_API void mono_restart_world (MonoThreadInfoFlags flags);

#endif /* __MONO_METADATA_GC_INTERNAL_H__ */
