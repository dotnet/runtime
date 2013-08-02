/*
 * metadata/gc-internal.h: Internal GC interface
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 */

#ifndef __MONO_METADATA_GC_INTERNAL_H__
#define __MONO_METADATA_GC_INTERNAL_H__

#include <glib.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/utils/gc_wrapper.h>

typedef struct {
	int minor_gc_count;
	int major_gc_count;
	long long minor_gc_time_usecs;
	long long major_gc_time_usecs;
} GCStats;

#define mono_domain_finalizers_lock(domain) EnterCriticalSection (&(domain)->finalizable_objects_hash_lock);
#define mono_domain_finalizers_unlock(domain) LeaveCriticalSection (&(domain)->finalizable_objects_hash_lock);

/* Register a memory area as a conservatively scanned GC root */
#define MONO_GC_REGISTER_ROOT_PINNING(x) mono_gc_register_root ((char*)&(x), sizeof(x), NULL)

#define MONO_GC_UNREGISTER_ROOT(x) mono_gc_deregister_root ((char*)&(x))

/*
 * Register a memory location as a root pointing to memory allocated using
 * mono_gc_alloc_fixed (). This includes MonoGHashTable.
 */
/* The result of alloc_fixed () is not GC tracked memory */
#define MONO_GC_REGISTER_ROOT_FIXED(x) do { \
	if (!mono_gc_is_moving ())				\
		MONO_GC_REGISTER_ROOT_PINNING ((x)); \
	} while (0)

/*
 * Return a GC descriptor for an array containing N pointers to memory allocated
 * by mono_gc_alloc_fixed ().
 */
/* For SGEN, the result of alloc_fixed () is not GC tracked memory */
#define MONO_GC_ROOT_DESCR_FOR_FIXED(n) (mono_gc_is_moving () ? mono_gc_make_root_descr_all_refs (0) : NULL)

/* Register a memory location holding a single object reference as a GC root */
#define MONO_GC_REGISTER_ROOT_SINGLE(x) do { \
	g_assert (sizeof (x) == sizeof (MonoObject*)); \
	mono_gc_register_root ((char*)&(x), sizeof(MonoObject*), mono_gc_make_root_descr_all_refs (1)); \
	} while (0)

/*
 * This is used for fields which point to objects which are kept alive by other references
 * when using Boehm.
 */
#define MONO_GC_REGISTER_ROOT_IF_MOVING(x) do { \
	if (mono_gc_is_moving ()) \
		MONO_GC_REGISTER_ROOT_SINGLE(x);		\
} while (0)

#define MONO_GC_UNREGISTER_ROOT_IF_MOVING(x) do { \
	if (mono_gc_is_moving ()) \
		MONO_GC_UNREGISTER_ROOT (x);			\
} while (0)

/* useful until we keep track of gc-references in corlib etc. */
#define IS_GC_REFERENCE(t) (mono_gc_is_moving () ? FALSE : ((t)->type == MONO_TYPE_U && class->image == mono_defaults.corlib))

extern GCStats gc_stats MONO_INTERNAL;

void   mono_object_register_finalizer               (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_InternalCollect          (int          generation) MONO_INTERNAL;
gint64 ves_icall_System_GC_GetTotalMemory           (MonoBoolean  forceCollection) MONO_INTERNAL;
void   ves_icall_System_GC_KeepAlive                (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_ReRegisterForFinalize    (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_SuppressFinalize         (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_WaitForPendingFinalizers (void) MONO_INTERNAL;

MonoObject *ves_icall_System_GCHandle_GetTarget (guint32 handle) MONO_INTERNAL;
guint32     ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type) MONO_INTERNAL;
void        ves_icall_System_GCHandle_FreeHandle (guint32 handle) MONO_INTERNAL;
gpointer    ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle) MONO_INTERNAL;
void        ves_icall_System_GC_register_ephemeron_array (MonoObject *array) MONO_INTERNAL;
MonoObject  *ves_icall_System_GC_get_ephemeron_tombstone (void) MONO_INTERNAL;

MonoBoolean ves_icall_Mono_Runtime_SetGCAllowSynchronousMajor (MonoBoolean flag) MONO_INTERNAL;

extern void mono_gc_init (void) MONO_INTERNAL;
extern void mono_gc_base_init (void) MONO_INTERNAL;
extern void mono_gc_cleanup (void) MONO_INTERNAL;

/*
 * Return whenever the current thread is registered with the GC (i.e. started
 * by the GC pthread wrappers on unix.
 */
extern gboolean mono_gc_is_gc_thread (void) MONO_INTERNAL;

/*
 * Try to register a foreign thread with the GC, if we fail or the backend
 * can't cope with this concept - we return FALSE.
 */
extern gboolean mono_gc_register_thread (void *baseptr) MONO_INTERNAL;

extern gboolean mono_gc_is_finalizer_internal_thread (MonoInternalThread *thread) MONO_INTERNAL;

extern void mono_gc_set_stack_end (void *stack_end) MONO_INTERNAL;

/* only valid after the RECLAIM_START GC event and before RECLAIM_END
 * Not exported in public headers, but can be linked to (unsupported).
 */
extern MONO_API gboolean mono_object_is_alive (MonoObject* obj);
extern MONO_API gboolean mono_gc_is_finalizer_thread (MonoThread *thread);
extern MONO_API gpointer mono_gc_out_of_memory (size_t size);
extern MONO_API void     mono_gc_enable_events (void);

/* disappearing link functionality */
void        mono_gc_weak_link_add    (void **link_addr, MonoObject *obj, gboolean track) MONO_INTERNAL;
void        mono_gc_weak_link_remove (void **link_addr, gboolean track) MONO_INTERNAL;
MonoObject *mono_gc_weak_link_get    (void **link_addr) MONO_INTERNAL;

/*Ephemeron functionality. Sgen only*/
gboolean    mono_gc_ephemeron_array_add (MonoObject *obj) MONO_INTERNAL;

/* To disable synchronous, evacuating collections - concurrent SGen only */
gboolean    mono_gc_set_allow_synchronous_major (gboolean flag) MONO_INTERNAL;

MonoBoolean
GCHandle_CheckCurrentDomain (guint32 gchandle) MONO_INTERNAL;

/* simple interface for data structures needed in the runtime */
void* mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits) MONO_INTERNAL;

/* Return a root descriptor for a root with all refs */
void* mono_gc_make_root_descr_all_refs (int numbits) MONO_INTERNAL;

/* User defined marking function */
/* It should work like this:
 * foreach (ref in GC references in the are structure pointed to by ADDR)
 *    mark_func (ref)
 */
typedef void (*MonoGCMarkFunc)     (void **addr);
typedef void (*MonoGCRootMarkFunc) (void *addr, MonoGCMarkFunc mark_func);

/* Create a descriptor with a user defined marking function */
MONO_API void *mono_gc_make_root_descr_user (MonoGCRootMarkFunc marker);

/* Return whenever user defined marking functions are supported */
gboolean mono_gc_user_markers_supported (void) MONO_INTERNAL;

/* desc is the result from mono_gc_make_descr*. A NULL value means
 * all the words might contain GC pointers.
 * The memory is non-moving and it will be explicitly deallocated.
 * size bytes will be available from the returned address (ie, descr
 * must not be stored in the returned memory)
 * NOTE: Under Boehm, this returns memory allocated using GC_malloc, so the result should
 * be stored into a location registered using MONO_GC_REGISTER_ROOT_FIXED ().
 */
void* mono_gc_alloc_fixed            (size_t size, void *descr) MONO_INTERNAL;
void  mono_gc_free_fixed             (void* addr) MONO_INTERNAL;

/* make sure the gchandle was allocated for an object in domain */
gboolean mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain) MONO_INTERNAL;
void     mono_gchandle_free_domain  (MonoDomain *domain) MONO_INTERNAL;

typedef void (*FinalizerThreadCallback) (gpointer user_data);

/* if there are finalizers to run, run them. Returns the number of finalizers run */
gboolean mono_gc_pending_finalizers (void) MONO_INTERNAL;
void     mono_gc_finalize_notify    (void) MONO_INTERNAL;

void* mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size) MONO_INTERNAL;
void* mono_gc_alloc_obj (MonoVTable *vtable, size_t size) MONO_INTERNAL;
void* mono_gc_alloc_vector (MonoVTable *vtable, size_t size, uintptr_t max_length) MONO_INTERNAL;
void* mono_gc_alloc_array (MonoVTable *vtable, size_t size, uintptr_t max_length, uintptr_t bounds_size) MONO_INTERNAL;
void* mono_gc_alloc_string (MonoVTable *vtable, size_t size, gint32 len) MONO_INTERNAL;
void* mono_gc_make_descr_for_string (gsize *bitmap, int numbits) MONO_INTERNAL;
void* mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size) MONO_INTERNAL;
void* mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size) MONO_INTERNAL;

void  mono_gc_register_for_finalization (MonoObject *obj, void *user_data) MONO_INTERNAL;
void  mono_gc_add_memory_pressure (gint64 value) MONO_INTERNAL;
int   mono_gc_register_root (char *start, size_t size, void *descr) MONO_INTERNAL;
void  mono_gc_deregister_root (char* addr) MONO_INTERNAL;
int   mono_gc_finalizers_for_domain (MonoDomain *domain, MonoObject **out_array, int out_size) MONO_INTERNAL;
void  mono_gc_run_finalize (void *obj, void *data) MONO_INTERNAL;
void  mono_gc_clear_domain (MonoDomain * domain) MONO_INTERNAL;
void* mono_gc_alloc_mature (MonoVTable *vtable) MONO_INTERNAL;

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
int mono_gc_register_root_wbarrier (char *start, size_t size, void *descr) MONO_INTERNAL;

void mono_gc_wbarrier_set_root (gpointer ptr, MonoObject *value) MONO_INTERNAL;

/* Set a field of a root registered using mono_gc_register_root_wbarrier () */
#define MONO_ROOT_SETREF(s,fieldname,value) do {	\
	mono_gc_wbarrier_set_root (&((s)->fieldname), (MonoObject*)value); \
} while (0)

void  mono_gc_finalize_threadpool_threads (void) MONO_INTERNAL;

/* fast allocation support */

/* Accessible using mono_marshal_wrapper_info_from_wrapper () */
typedef struct {
	const char *gc_name;
	int alloc_type;
} AllocatorWrapperInfo;

MonoMethod* mono_gc_get_managed_allocator (MonoClass *klass, gboolean for_box) MONO_INTERNAL;
MonoMethod* mono_gc_get_managed_array_allocator (MonoClass *klass) MONO_INTERNAL;
MonoMethod *mono_gc_get_managed_allocator_by_type (int atype) MONO_INTERNAL;

guint32 mono_gc_get_managed_allocator_types (void) MONO_INTERNAL;

/* Return a short string identifying the GC, indented to be saved in AOT images */
const char *mono_gc_get_gc_name (void) MONO_INTERNAL;

/* Fast write barriers */
MonoMethod* mono_gc_get_write_barrier (void) MONO_INTERNAL;

/* Fast valuetype copy */
void mono_gc_wbarrier_value_copy_bitmap (gpointer dest, gpointer src, int size, unsigned bitmap) MONO_INTERNAL;

/* helper for the managed alloc support */
MonoString *mono_string_alloc (int length) MONO_INTERNAL;

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
	void (*thread_mark_func) (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise);
} MonoGCCallbacks;

/* Set the callback functions callable by the GC */
void mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks) MONO_INTERNAL;
MonoGCCallbacks *mono_gc_get_gc_callbacks (void) MONO_INTERNAL;

/* Functions callable from the thread mark func */

/* Scan the memory area between START and END conservatively */
void mono_gc_conservatively_scan_area (void *start, void *end) MONO_INTERNAL;

/* Scan OBJ, returning its new address */
void *mono_gc_scan_object (void *obj) MONO_INTERNAL;

/* Return the bitmap encoded by a descriptor */
gsize* mono_gc_get_bitmap_for_descr (void *descr, int *numbits) MONO_INTERNAL;

/* Return the suspend signal number used by the GC to suspend threads,
   or -1 if not applicable. */
int mono_gc_get_suspend_signal (void) MONO_INTERNAL;

/*
 * Return a human readable description of the GC in malloc-ed memory.
 */
char* mono_gc_get_description (void) MONO_INTERNAL;

/*
 * Configure the GC to desktop mode
 */
void mono_gc_set_desktop_mode (void) MONO_INTERNAL;

/*
 * Return whenever this GC can move objects
 */
gboolean mono_gc_is_moving (void) MONO_INTERNAL;

typedef void* (*MonoGCLockedCallbackFunc) (void *data);

void* mono_gc_invoke_with_gc_lock (MonoGCLockedCallbackFunc func, void *data) MONO_INTERNAL;

int mono_gc_get_los_limit (void) MONO_INTERNAL;

guint8* mono_gc_get_card_table (int *shift_bits, gpointer *card_mask) MONO_INTERNAL;
gboolean mono_gc_card_table_nursery_check (void) MONO_INTERNAL;

void* mono_gc_get_nursery (int *shift_bits, size_t *size) MONO_INTERNAL;

void mono_gc_set_current_thread_appdomain (MonoDomain *domain) MONO_INTERNAL;

void mono_gc_set_skip_thread (gboolean skip) MONO_INTERNAL;

/*
 * Return whenever GC is disabled
 */
gboolean mono_gc_is_disabled (void) MONO_INTERNAL;

#if defined(__MACH__)
void mono_gc_register_mach_exception_thread (pthread_t thread) MONO_INTERNAL;
pthread_t mono_gc_get_mach_exception_thread (void) MONO_INTERNAL;
#endif

gboolean mono_gc_parse_environment_string_extract_number (const char *str, glong *out) MONO_INTERNAL;

gboolean mono_gc_precise_stack_mark_enabled (void) MONO_INTERNAL;

FILE *mono_gc_get_logfile (void) MONO_INTERNAL;

typedef void (*mono_reference_queue_callback) (void *user_data);

typedef struct _MonoReferenceQueue MonoReferenceQueue;
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

MonoReferenceQueue* mono_gc_reference_queue_new (mono_reference_queue_callback callback) MONO_INTERNAL;
void mono_gc_reference_queue_free (MonoReferenceQueue *queue) MONO_INTERNAL;
gboolean mono_gc_reference_queue_add (MonoReferenceQueue *queue, MonoObject *obj, void *user_data) MONO_INTERNAL;

#ifdef HOST_WIN32
BOOL APIENTRY mono_gc_dllmain (HMODULE module_handle, DWORD reason, LPVOID reserved) MONO_INTERNAL;
#endif

/*
Those functions must be used when it's possible that either destination is not
word aligned or size is not a multiple of word size.
*/
void mono_gc_bzero (void *dest, size_t size) MONO_INTERNAL;
void mono_gc_memmove (void *dest, const void *src, size_t size) MONO_INTERNAL;

guint mono_gc_get_vtable_bits (MonoClass *class) MONO_INTERNAL;

void mono_gc_register_altstack (gpointer stack, gint32 stack_size, gpointer altstack, gint32 altstack_size) MONO_INTERNAL;

#endif /* __MONO_METADATA_GC_INTERNAL_H__ */

