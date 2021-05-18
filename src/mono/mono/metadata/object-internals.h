/**
 * \file
 */

#ifndef __MONO_OBJECT_INTERNALS_H__
#define __MONO_OBJECT_INTERNALS_H__

#include <mono/utils/mono-forward-internal.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/handle-decl.h>

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/abi-details.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-machine.h"
#include "mono/utils/mono-stack-unwinding.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-coop-mutex.h"
#include <mono/metadata/icalls.h>

/* Use this as MONO_CHECK_ARG (arg,expr,) in functions returning void */
#define MONO_CHECK_ARG(arg, expr, retval) do {				\
	if (G_UNLIKELY (!(expr)))					\
	{								\
		if (0) { (void)(arg); } /* check if the name exists */	\
		ERROR_DECL (error);					\
		mono_error_set_argument_format (error, #arg, "assertion `%s' failed", #expr); \
		mono_error_set_pending_exception (error);		\
		return retval;						\
	} 								\
} while (0)

#define MONO_CHECK_ARG_NULL_NAMED(arg, argname, retval) do {	\
	if (G_UNLIKELY (!(arg)))				\
	{							\
		ERROR_DECL (error);				\
		mono_error_set_argument_null (error, (argname), "");	\
		mono_error_set_pending_exception (error);	\
		return retval;					\
	}							\
} while (0)
/* Use this as MONO_CHECK_ARG_NULL (arg,) in functions returning void */
#define MONO_CHECK_ARG_NULL(arg, retval) do { 			\
	if (G_UNLIKELY (!(arg)))				\
	{							\
		mono_error_set_argument_null (error, #arg, "");	\
		return retval;					\
	}							\
} while (0)

/* Use this as MONO_CHECK_ARG_NULL_HANDLE (arg,) in functions returning void */
#define MONO_CHECK_ARG_NULL_HANDLE(arg, retval) do { 		\
	if (G_UNLIKELY (MONO_HANDLE_IS_NULL (arg)))		\
	{							\
		mono_error_set_argument_null (error, #arg, "");	\
		return retval;					\
	}							\
} while (0)

#define MONO_CHECK_ARG_NULL_HANDLE_NAMED(arg, argname, retval) do { \
	if (G_UNLIKELY (MONO_HANDLE_IS_NULL (arg)))		\
	{							\
		mono_error_set_argument_null (error, (argname), "");	\
		return retval;					\
	}							\
} while (0)

/* Use this as MONO_CHECK_NULL (arg,) in functions returning void */
#define MONO_CHECK_NULL(arg, retval) do { 			\
	if (G_UNLIKELY (!(arg)))				\
	{							\
		ERROR_DECL (error);				\
		mono_error_set_null_reference (error);		\
		mono_error_set_pending_exception (error);	\
		return retval;					\
	} 							\
} while (0)

MonoClass *
mono_class_create_array (MonoClass *element_class, uint32_t rank);

MonoArrayHandle
mono_array_new_specific_handle (MonoVTable *vtable, uintptr_t n, MonoError *error);

MonoArray*
mono_array_new_specific_checked (MonoVTable *vtable, uintptr_t n, MonoError *error);

/* 
 * Macros which cache.
 * These should be used instead of the original versions.
 */

static inline MonoClass*
mono_array_class_get_cached_function (MonoClass *eclass, MonoClass **aclass)
{
	MonoClass *a = *aclass;
	if (a)
		return a;
	a = mono_class_create_array (eclass, 1);
	g_assert (a);
	if (a)
		*aclass = a;
	return *aclass;
}

// eclass should be a run-time constant
// If you get an error using this macro, you need to manually instantiate the MonoClass *foo ## _array cache.
// See for example object_class_array.
#define mono_array_class_get_cached(eclass) (mono_array_class_get_cached_function ((eclass), &(eclass ## _array)))

static inline MonoArray*
mono_array_new_cached_function (MonoClass *aclass, int size, MonoError *error)
{
	MonoVTable *vtable = mono_class_vtable_checked (aclass, error);
	MonoArray *arr = NULL;
	if (is_ok (error))
		arr = mono_array_new_specific_checked (vtable, size, error);
	return arr;
}

// eclass should be a run-time constant
// If you get an error using this macro, you need to manually instantiate the MonoClass *foo ## _array cache.
// See for example object_class_array.
#define mono_array_new_cached(eclass, size, error) \
	mono_array_new_cached_function (mono_array_class_get_cached (eclass), (size), (error))

static inline MonoArrayHandle
mono_array_new_cached_handle_function (MonoClass *aclass, int size, MonoError *error)
{
	MonoVTable *vtable = mono_class_vtable_checked (aclass, error);
	MonoArrayHandle arr = NULL_HANDLE_ARRAY;
	if (is_ok (error))
		arr = mono_array_new_specific_handle (vtable, size, error);
	return arr;
}

// eclass should be a run-time constant
// If you get an error using this macro, you need to manually instantiate the MonoClass *foo ## _array cache.
// See for example object_class_array.
#define mono_array_new_cached_handle(eclass, size, error) \
	mono_array_new_cached_handle_function (mono_array_class_get_cached (eclass), (size), (error))

#ifdef MONO_BIG_ARRAYS
typedef uint64_t mono_array_size_t;
typedef int64_t mono_array_lower_bound_t;
#define MONO_ARRAY_MAX_INDEX G_MAXINT64
#define MONO_ARRAY_MAX_SIZE  G_MAXUINT64
#else
typedef uint32_t mono_array_size_t;
typedef int32_t mono_array_lower_bound_t;
#define MONO_ARRAY_MAX_INDEX ((int32_t) 0x7fffffff)
#define MONO_ARRAY_MAX_SIZE  ((uint32_t) 0xffffffff)
#endif

typedef struct {
	mono_array_size_t length;
	mono_array_lower_bound_t lower_bound;
} MonoArrayBounds;

struct _MonoArray {
	MonoObject obj;
	/* bounds is NULL for szarrays */
	MonoArrayBounds *bounds;
	/* total number of elements of the array */
	mono_array_size_t max_length; 
	/* we use mono_64bitaligned_t to ensure proper alignment on platforms that need it */
	mono_64bitaligned_t vector [MONO_ZERO_LEN_ARRAY];
};

#define MONO_SIZEOF_MONO_ARRAY (MONO_STRUCT_OFFSET_CONSTANT (MonoArray, vector))

struct _MonoString {
	MonoObject object;
	int32_t length;
	mono_unichar2 chars [MONO_ZERO_LEN_ARRAY];
};

#define MONO_SIZEOF_MONO_STRING (MONO_STRUCT_OFFSET (MonoString, chars))

#define mono_object_class(obj) (((MonoObject*)(obj))->vtable->klass)
#define mono_object_domain(obj) (((MonoObject*)(obj))->vtable->domain)

#define mono_string_chars_fast(s) ((mono_unichar2*)(s)->chars)
#define mono_string_length_fast(s) ((s)->length)

/**
 * mono_array_length_internal:
 * \param array a \c MonoArray*
 * \returns the total number of elements in the array. This works for
 * both vectors and multidimensional arrays.
 */
#define mono_array_length_internal(array) ((array)->max_length)

static inline
uintptr_t
mono_array_handle_length (MonoArrayHandle arr)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_array_length_internal (MONO_HANDLE_RAW (arr));
}

// Equivalent to mono_array_addr_with_size, except:
// 1. A macro instead of a function -- the types of size and index are open.
// 2. mono_array_addr_with_size could, but does not, do GC mode transitions.
#define mono_array_addr_with_size_fast(array,size,index) ( ((char*)(array)->vector) + (size) * (index) )

#define mono_array_addr_fast(array,type,index) ((type*)(void*) mono_array_addr_with_size_fast (array, sizeof (type), index))
#define mono_array_get_fast(array,type,index) ( *(type*)mono_array_addr_fast ((array), type, (index)) )
#define mono_array_set_fast(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr_fast ((array), type, (index));	\
		*__p = (value);	\
	} while (0)
#define mono_array_setref_fast(array,index,value)	\
	do {	\
		void **__p = (void **) mono_array_addr_fast ((array), void*, (index));	\
		mono_gc_wbarrier_set_arrayref_internal ((array), __p, (MonoObject*)(value));	\
		/* *__p = (value);*/	\
	} while (0)
#define mono_array_memcpy_refs_fast(dest,destidx,src,srcidx,count)	\
	do {	\
		void **__p = (void **) mono_array_addr_fast ((dest), void*, (destidx));	\
		void **__s = mono_array_addr_fast ((src), void*, (srcidx));	\
		mono_gc_wbarrier_arrayref_copy_internal (__p, __s, (count));	\
	} while (0)

// _internal is like _fast, but preserves the preexisting subtlety of the closed types of things:
//  	int size
//	uintptr_t idx
// in order to mimic non-_internal but without the GC mode transitions, or at least,
// to avoid the runtime using the embedding API, whether or not it has GC mode transitions.
static inline char*
mono_array_addr_with_size_internal (MonoArray *array, int size, uintptr_t idx)
{
	return mono_array_addr_with_size_fast (array, size, idx);
}

#define mono_array_addr_internal(array,type,index) ((type*)(void*) mono_array_addr_with_size_internal (array, sizeof (type), index))
#define mono_array_get_internal(array,type,index) ( *(type*)mono_array_addr_internal ((array), type, (index)) )
#define mono_array_set_internal(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr_internal ((array), type, (index));	\
		*__p = (value);	\
	} while (0)
#define mono_array_setref_internal(array,index,value)	\
	do {	\
		void **__p = (void **) mono_array_addr_internal ((array), void*, (index));	\
		mono_gc_wbarrier_set_arrayref_internal ((array), __p, (MonoObject*)(value));	\
		/* *__p = (value);*/	\
	} while (0)
#define mono_array_memcpy_refs_internal(dest,destidx,src,srcidx,count)	\
	do {	\
		void **__p = (void **) mono_array_addr_internal ((dest), void*, (destidx));	\
		void **__s = mono_array_addr_internal ((src), void*, (srcidx));	\
		mono_gc_wbarrier_arrayref_copy_internal (__p, __s, (count));	\
	} while (0)

static inline gboolean
mono_handle_array_has_bounds (MonoArrayHandle arr)
{
	return MONO_HANDLE_GETVAL (arr, bounds) != NULL;
}

static inline void
mono_handle_array_get_bounds_dim (MonoArrayHandle arr, gint32 dim, MonoArrayBounds *bounds)
{
	*bounds = MONO_HANDLE_GETVAL (arr, bounds [dim]);
}

typedef struct {
	MonoObject obj;
} MonoMarshalByRefObject;

TYPED_HANDLE_DECL (MonoMarshalByRefObject);

/* This is a copy of System.AppDomain */
struct _MonoAppDomain {
	MonoMarshalByRefObject mbr;
};

/* Safely access System.AppDomain from native code */
TYPED_HANDLE_DECL (MonoAppDomain);

typedef struct _MonoStringBuilder MonoStringBuilder;
TYPED_HANDLE_DECL (MonoStringBuilder);

struct _MonoStringBuilder {
	MonoObject object;
	MonoArray  *chunkChars;
	MonoStringBuilder* chunkPrevious;      // Link to the block logically before this block
	int chunkLength;                  // The index in ChunkChars that represent the end of the block
	int chunkOffset;                  // The logial offset (sum of all characters in previous blocks)
	int maxCapacity;
};

static inline int
mono_string_builder_capacity (MonoStringBuilderHandle sbh)
{
	MonoStringBuilder *sb = MONO_HANDLE_RAW (sbh);
	return sb->chunkOffset + sb->chunkChars->max_length;
}

static inline int
mono_string_builder_string_length (MonoStringBuilderHandle sbh)
{
	MonoStringBuilder *sb = MONO_HANDLE_RAW (sbh);
	return sb->chunkOffset + sb->chunkLength;
}

typedef struct {
	MonoType *type;
	gpointer  value;
	MonoClass *klass;
} MonoTypedRef;

typedef struct {
	gpointer args;
} MonoArgumentHandle;

typedef struct {
	MonoMethodSignature *sig;
	gpointer args;
	gint32 next_arg;
	gint32 num_args;
} MonoArgIterator;

struct _MonoException {
	MonoObject object;
	MonoString *class_name;
	MonoString *message;
	MonoObject *_data;
	MonoObject *inner_ex;
	MonoString *help_link;
	/* Stores the IPs and the generic sharing infos
	   (vtable/MRGCTX) of the frames. */
	MonoArray  *trace_ips;
	MonoString *stack_trace;
	MonoString *remote_stack_trace;
	gint32	    remote_stack_index;
	/* Dynamic methods referenced by the stack trace */
	MonoArray *dynamic_methods;
	gint32	    hresult;
	MonoString *source;
	MonoObject *serialization_manager;
	MonoObject *captured_traces;
	MonoArray  *native_trace_ips;
	gint32 caught_in_unmanaged;
};

typedef struct {
	MonoException base;
} MonoSystemException;

TYPED_HANDLE_DECL (MonoSystemException);

typedef struct {
	MonoObject   object;
	MonoObject  *async_state;
	MonoObject  *handle;
	MonoObject  *async_delegate;
	gpointer    *data;
	MonoObject  *object_data;
	MonoBoolean  sync_completed;
	MonoBoolean  completed;
	MonoBoolean  endinvoke_called;
	MonoObject  *async_callback;
	MonoObject  *execution_context;
	MonoObject  *original_context;
	gint64	     add_time;
} MonoAsyncResult;

TYPED_HANDLE_DECL (MonoAsyncResult);

typedef struct {
	MonoMarshalByRefObject object;
	gpointer     handle;
} MonoWaitHandle;

TYPED_HANDLE_DECL (MonoWaitHandle);

/* System.Threading.StackCrawlMark */
/*
 * This type is used to identify the method where execution has entered
 * the BCL during stack walks. The outermost public method should
 * define it like this:
 * StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
 * and pass the stackMark as a byref argument down the call chain
 * until it reaches an icall.
 */
typedef enum {
	STACK_CRAWL_ME = 0,
	STACK_CRAWL_CALLER = 1,
	STACK_CRAWL_CALLERS_CALLER = 2,
	STACK_CRAWL_THREAD = 3
} MonoStackCrawlMark;

/* MonoSafeHandle is in class-internals.h. */
/* Safely access System.Net.Sockets.SafeSocketHandle from native code */
TYPED_HANDLE_DECL (MonoSafeHandle);

/* This corresponds to System.Type */
struct _MonoReflectionType {
	MonoObject object;
	MonoType  *type;
};

/* Safely access System.Type from native code */
TYPED_HANDLE_DECL (MonoReflectionType);

typedef struct {
	MonoObject  object;
	MonoReflectionType *class_to_proxy;	
	MonoObject *context;
	MonoObject *unwrapped_server;
	gint32      target_domain_id;
	MonoString *target_uri;
	MonoObject *object_identity;
	MonoObject *obj_TP;
	MonoObject *stub_data;
} MonoRealProxy;

/* Safely access System.Runtime.Remoting.Proxies.RealProxy from native code */
TYPED_HANDLE_DECL (MonoRealProxy);

typedef struct _MonoIUnknown MonoIUnknown;
typedef struct _MonoIUnknownVTable MonoIUnknownVTable;

/* STDCALL on windows, CDECL everywhere else to work with XPCOM and MainWin COM */
#ifdef HOST_WIN32
#define STDCALL __stdcall
#else
#define STDCALL
#endif

struct _MonoIUnknownVTable
{
	int (STDCALL *QueryInterface)(MonoIUnknown *pUnk, gconstpointer riid, gpointer* ppv);
	int (STDCALL *AddRef)(MonoIUnknown *pUnk);
	int (STDCALL *Release)(MonoIUnknown *pUnk);
};

struct _MonoIUnknown
{
	const MonoIUnknownVTable *vtable;
};

typedef struct {
	MonoMarshalByRefObject object;
	MonoIUnknown *iunknown;
	GHashTable* itf_hash;
	MonoObject *synchronization_context;
} MonoComObject;

TYPED_HANDLE_DECL (MonoComObject);

typedef struct {
	MonoRealProxy real_proxy;
	MonoComObject *com_object;
	gint32 ref_count;
} MonoComInteropProxy;

TYPED_HANDLE_DECL (MonoComInteropProxy);

typedef struct {
	MonoObject	 object;
	MonoRealProxy	*rp;	
	MonoRemoteClass *remote_class;
	MonoBoolean	 custom_type_info;
} MonoTransparentProxy;

/* Safely access System.Runtime.Remoting.Proxies.TransparentProxy from native code */
TYPED_HANDLE_DECL (MonoTransparentProxy);

typedef struct {
	MonoObject obj;
	MonoReflectionMethod *method;
	MonoArray  *args;		
	MonoArray  *names;		
	MonoArray  *arg_types;	
	MonoObject *ctx;
	MonoObject *rval;
	MonoObject *exc;
	MonoAsyncResult *async_result;
	guint32	    call_type;
} MonoMethodMessage;

TYPED_HANDLE_DECL (MonoMethodMessage);

/* Keep in sync with the System.MonoAsyncCall */
typedef struct {
	MonoObject object;
	MonoMethodMessage *msg;
	MonoMethod *cb_method;
	MonoDelegate *cb_target;
	MonoObject *state;
	MonoObject *res;
	MonoArray *out_args;
} MonoAsyncCall;

TYPED_HANDLE_DECL (MonoAsyncCall);

typedef struct {
	MonoObject obj;
	MonoArray *frames;
	MonoArray *captured_traces;
	MonoBoolean debug_info;
} MonoStackTrace;

TYPED_HANDLE_DECL (MonoStackTrace);

typedef struct {
	MonoObject obj;
	gint32 il_offset;
	gint32 native_offset;
	gint64 method_address;
	gint32 method_index;
	MonoReflectionMethod *method;
	MonoString *filename;
	gint32 line;
	gint32 column;
	MonoString *internal_method_name;
} MonoStackFrame;

TYPED_HANDLE_DECL (MonoStackFrame);

typedef enum {
	MONO_THREAD_FLAG_DONT_MANAGE			= 1, // Don't wait for or abort this thread
	MONO_THREAD_FLAG_NAME_SET				= 2, // Thread name set from managed code
	MONO_THREAD_FLAG_CLEANUP_FROM_NATIVE	= 4, // Thread initialized in native so clean up in native
} MonoThreadFlags;

struct _MonoThreadInfo;

typedef struct MonoThreadName {
	char* volatile chars;      // null check outside of lock
	gint32 free; // bool
	gint32 length;
} MonoThreadName;

void
mono_gstring_append_thread_name (GString*, MonoInternalThread*);

struct _MonoInternalThread {
	MonoObject  obj;
	volatile int lock_thread_id; /* to be used as the pre-shifted thread id in thin locks */
	MonoThreadHandle *handle;
	gpointer native_handle;
	MonoThreadName name;
	guint32	    state;      /* must be accessed while longlived->synch_cs is locked */
	MonoException *abort_exc;
	MonoGCHandle abort_state_handle;
	guint64 tid;	/* This is accessed as a gsize in the code (so it can hold a 64bit pointer on systems that need it), but needs to reserve 64 bits of space on all machines as it corresponds to a field in managed code */
	gsize debugger_thread; // FIXME switch to bool as soon as CI testing with corlib version bump works
	gpointer *static_data;
	struct _MonoThreadInfo *thread_info;
	/* This is modified using atomic ops, so keep it a gint32 */
	gint32 __interruption_requested;
	/* data that must live as long as this managed object is not finalized
	 * or as long as the underlying thread is attached, whichever is
	 * longer */
	MonoLongLivedThreadData *longlived;
	MonoBoolean threadpool_thread;
	guint8	apartment_state;
	gint32 managed_id;
	guint32 small_id;
	MonoThreadManageCallback manage_callback;
	gsize    flags;
	gpointer thread_pinning_ref;
	gint32 priority;
	GPtrArray *owned_mutexes;
	MonoOSEvent *suspended;
	gint32 self_suspended; // TRUE | FALSE
	gsize thread_state;

	/* Points to self, set when starting up/attaching */
	struct _MonoInternalThread *internal_thread;
	MonoException *pending_exception;

	/* This is used only to check that we are in sync between the representation
	 * of MonoInternalThread in native and InternalThread in managed
	 *
	 * DO NOT RENAME! DO NOT ADD FIELDS AFTER! */
	gpointer last;
};

typedef struct {
	guint32 state;
	MonoObject *additional;
} MonoStreamingContext;

typedef struct {
	MonoObject object;
	guint32 intType;
} MonoInterfaceTypeAttribute;

typedef struct {
	MonoObject object;
	guint32 intType;
} MonoClassInterfaceAttribute;

/* Safely access System.Delegate from native code */
TYPED_HANDLE_DECL (MonoDelegate);

/* 
 * Callbacks supplied by the runtime and called by the modules in metadata/
 * This interface is easier to extend than adding a new function type +
 * a new 'install' function for every callback.
 */
typedef struct {
	gpointer (*create_ftnptr) (gpointer addr);
	gpointer (*get_addr_from_ftnptr) (gpointer descr);
	char*    (*get_runtime_build_info) (void);
	const char*    (*get_runtime_build_version) (void);
	gpointer (*get_vtable_trampoline) (MonoVTable *vtable, int slot_index);
	gpointer (*get_imt_trampoline) (MonoVTable *vtable, int imt_slot_index);
	gboolean (*imt_entry_inited) (MonoVTable *vtable, int imt_slot_index);
	void     (*set_cast_details) (MonoClass *from, MonoClass *to);
	void     (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
	void     (*init_delegate) (MonoDelegateHandle delegate, MonoObjectHandle target, gpointer addr, MonoMethod *method, MonoError *error);
	MonoObject* (*runtime_invoke) (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);
	void*    (*compile_method) (MonoMethod *method, MonoError *error);
	gpointer (*create_jit_trampoline) (MonoMethod *method, MonoError *error);
	/* used to free a dynamic method */
	void     (*free_method) (MonoMethod *method);
	gpointer (*create_delegate_trampoline) (MonoClass *klass);
	GHashTable *(*get_weak_field_indexes) (MonoImage *image);
	void     (*install_state_summarizer) (void);
	gboolean (*is_interpreter_enabled) (void);
	void (*init_mem_manager)(MonoMemoryManager*);
	void (*free_mem_manager)(MonoMemoryManager*);
	void     (*metadata_update_published) (MonoAssemblyLoadContext *alc, uint32_t generation);
	void (*get_jit_stats)(gint64 *methods_compiled, gint64 *cil_code_size_bytes, gint64 *native_code_size_bytes);
	void (*get_exception_stats)(guint32 *exception_count);
} MonoRuntimeCallbacks;

typedef gboolean (*MonoInternalStackWalk) (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer data);
typedef gboolean (*MonoInternalExceptionFrameWalk) (MonoMethod *method, gpointer ip, size_t native_offset, gboolean managed, gpointer user_data);

typedef struct {
	void (*mono_walk_stack_with_ctx) (MonoInternalStackWalk func, MonoContext *ctx, MonoUnwindOptions options, void *user_data);
	void (*mono_walk_stack_with_state) (MonoInternalStackWalk func, MonoThreadUnwindState *state, MonoUnwindOptions options, void *user_data);
	void (*mono_raise_exception) (MonoException *ex);
	void (*mono_raise_exception_with_ctx) (MonoException *ex, MonoContext *ctx);
	gboolean (*mono_exception_walk_trace) (MonoException *ex, MonoInternalExceptionFrameWalk func, gpointer user_data);
	gboolean (*mono_install_handler_block_guard) (MonoThreadUnwindState *unwind_state);
	void (*mono_uninstall_current_handler_block_guard) (void);
	gboolean (*mono_current_thread_has_handle_block_guard) (void);
	gboolean (*mono_above_abort_threshold) (void);
	void (*mono_clear_abort_threshold) (void);
	void (*mono_reraise_exception) (MonoException *ex);
	void (*mono_summarize_managed_stack) (MonoThreadSummary *out);
	void (*mono_summarize_unmanaged_stack) (MonoThreadSummary *out);
	void (*mono_summarize_exception) (MonoException *exc, MonoThreadSummary *out);
	void (*mono_register_native_library) (const char *module_path, const char *module_name);
	void (*mono_allow_all_native_libraries) (void);
} MonoRuntimeExceptionHandlingCallbacks;

MONO_COLD void mono_set_pending_exception (MonoException *exc);

void
mono_delegate_ctor	    (MonoObjectHandle this_obj, MonoObjectHandle target, gpointer addr, MonoMethod *method, MonoError *error);

MonoMethod *
mono_get_delegate_invoke_checked (MonoClass *klass, MonoError *error);

MonoMethod *
mono_get_delegate_begin_invoke_checked (MonoClass *klass, MonoError *error);

MonoMethod *
mono_get_delegate_end_invoke_checked (MonoClass *klass, MonoError *error);

void
mono_runtime_free_method    (MonoMethod *method);

void
mono_install_callbacks      (MonoRuntimeCallbacks *cbs);

MONO_COMPONENT_API
MonoRuntimeCallbacks*
mono_get_runtime_callbacks (void);

void
mono_install_eh_callbacks (MonoRuntimeExceptionHandlingCallbacks *cbs);

MONO_COMPONENT_API
MonoRuntimeExceptionHandlingCallbacks *
mono_get_eh_callbacks (void);

void
mono_raise_exception_deprecated (MonoException *ex);

void
mono_reraise_exception_deprecated (MonoException *ex);

void
mono_raise_exception_with_context (MonoException *ex, MonoContext *ctx);

void
mono_type_initialization_init (void);

int
mono_thread_kill           (MonoInternalThread *thread, int signal);

MonoNativeTlsKey
mono_thread_get_tls_key    (void);

gint32
mono_thread_get_tls_offset (void);

MonoNativeTlsKey
mono_domain_get_tls_key    (void);

gint32
mono_domain_get_tls_offset (void);

/* Reflection and Reflection.Emit support */

/*
 * Handling System.Type objects:
 *
 *   Fields defined as System.Type in managed code should be defined as MonoObject* 
 * in unmanaged structures, and the monotype_cast () function should be used for 
 * casting them to MonoReflectionType* to avoid crashes/security issues when 
 * encountering instances of user defined subclasses of System.Type.
 */

#define IS_MONOTYPE(obj) (!(obj) || (m_class_get_image (mono_object_class ((obj))) == mono_defaults.corlib && ((MonoReflectionType*)(obj))->type != NULL))

#define IS_MONOTYPE_HANDLE(obj) IS_MONOTYPE (MONO_HANDLE_RAW (obj))

/* This should be used for accessing members of Type[] arrays */
#define mono_type_array_get(arr,index) monotype_cast (mono_array_get_internal ((arr), gpointer, (index)))

/*
 * Cast an object to MonoReflectionType, making sure it is a System.MonoType or
 * a subclass of it.
 */
static inline MonoReflectionType*
monotype_cast (MonoObject *obj)
{
	g_assert (IS_MONOTYPE (obj));

	return (MonoReflectionType*)obj;
}

/*
 * The following structure must match the C# implementation in our corlib.
 */

struct _MonoReflectionMethod {
	MonoObject object;
	MonoMethod *method;
	MonoString *name;
	MonoReflectionType *reftype;
};

/* Safely access System.Reflection.MonoMethod from native code */
TYPED_HANDLE_DECL (MonoReflectionMethod);

struct _MonoDelegate {
	MonoObject object;
	/* The compiled code of the target method */
	gpointer method_ptr;
	/* The invoke code */
	gpointer invoke_impl;
	MonoObject *target;
	MonoMethod *method;
	gpointer delegate_trampoline;
	/* Extra argument passed to the target method in llvmonly mode */
	gpointer extra_arg;
	/* 
	 * If non-NULL, this points to a memory location which stores the address of 
	 * the compiled code of the method, or NULL if it is not yet compiled.
	 */
	guint8 **method_code;
	gpointer interp_method;
	/* Interp method that is executed when invoking the delegate */
	gpointer interp_invoke_impl;
	MonoReflectionMethod *method_info;
	MonoReflectionMethod *original_method_info;
	MonoObject *data;
	MonoBoolean method_is_virtual;
};

typedef struct _MonoMulticastDelegate MonoMulticastDelegate;
struct _MonoMulticastDelegate {
	MonoDelegate delegate;
	MonoArray *delegates;
};

/* Safely access System.MulticastDelegate from native code */
TYPED_HANDLE_DECL (MonoMulticastDelegate);

struct _MonoReflectionField {
	MonoObject object;
	MonoClass *klass;
	MonoClassField *field;
	MonoString *name;
	MonoReflectionType *type;
	guint32 attrs;
};

/* Safely access System.Reflection.MonoField from native code */
TYPED_HANDLE_DECL (MonoReflectionField);

struct _MonoReflectionProperty {
	MonoObject object;
	MonoClass *klass;
	MonoProperty *property;
};

/* Safely access System.Reflection.MonoProperty from native code */
TYPED_HANDLE_DECL (MonoReflectionProperty);

/*This is System.EventInfo*/
struct _MonoReflectionEvent {
	MonoObject object;
};

/* Safely access System.Reflection.EventInfo from native code */
TYPED_HANDLE_DECL (MonoReflectionEvent);

typedef struct {
	MonoReflectionEvent object;
	MonoClass *klass;
	MonoEvent *event;
} MonoReflectionMonoEvent;

/* Safely access Systme.Reflection.MonoEvent from native code */
TYPED_HANDLE_DECL (MonoReflectionMonoEvent);

typedef struct {
	MonoObject object;
} MonoReflectionParameter;

/* Safely access System.Reflection.ParameterInfo from native code */
TYPED_HANDLE_DECL (MonoReflectionParameter);

struct _MonoReflectionMethodBody {
	MonoObject object;
};

/* Safely access System.Reflection.MethodBody from native code */
TYPED_HANDLE_DECL (MonoReflectionMethodBody);

/* System.RuntimeAssembly */
struct _MonoReflectionAssembly {
	MonoObject object;
	MonoAssembly *assembly;
};

typedef struct {
	MonoReflectionType *utype;
	MonoArray *values;
	MonoArray *names;
} MonoEnumInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoReflectionType *ret;
	guint32 attrs;
	guint32 implattrs;
	guint32 callconv;
} MonoMethodInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoReflectionType *declaring_type;
	MonoString *name;
	MonoReflectionMethod *get;
	MonoReflectionMethod *set;
	guint32 attrs;
} MonoPropertyInfo;

typedef struct {
	MonoReflectionType *declaring_type;
	MonoReflectionType *reflected_type;
	MonoString *name;
	MonoReflectionMethod *add_method;
	MonoReflectionMethod *remove_method;
	MonoReflectionMethod *raise_method;
	guint32 attrs;
	MonoArray *other_methods;
} MonoEventInfo;

typedef struct {
	MonoObject *member;
	gint32 code_pos;
} MonoReflectionILTokenInfo;

typedef struct {
	MonoObject object;
	MonoArray *code;
	gint32 code_len;
	gint32 max_stack;
	gint32 cur_stack;
	MonoArray *locals;
	MonoArray *ex_handlers;
	gint32 num_token_fixups;
	MonoArray *token_fixups;
} MonoReflectionILGen;

typedef struct {
	MonoArray *handlers;
	gint32 start;
	gint32 len;
	gint32 label;
} MonoILExceptionInfo;

typedef struct {
	MonoObject *extype;
	gint32 type;
	gint32 start;
	gint32 len;
	gint32 filter_offset;
} MonoILExceptionBlock;

typedef struct {
	MonoObject object;
	MonoObject *catch_type;
	gint32 filter_offset;
	gint32 flags;
	gint32 try_offset;
	gint32 try_length;
	gint32 handler_offset;
	gint32 handler_length;
} MonoReflectionExceptionHandlingClause;


/* Safely access System.Reflection.ExceptionHandlingClause from native code */
TYPED_HANDLE_DECL (MonoReflectionExceptionHandlingClause);

typedef struct {
	MonoObject object;
	MonoReflectionType *local_type;
	MonoBoolean is_pinned;
	guint16 local_index;
} MonoReflectionLocalVariableInfo;

/* Safely access System.Reflection.LocalVariableInfo from native code */
TYPED_HANDLE_DECL (MonoReflectionLocalVariableInfo);

typedef struct {
	/*
	 * Must have the same layout as MonoReflectionLocalVariableInfo, since
	 * LocalBuilder inherits from it under net 2.0.
	 */
	MonoObject object;
	MonoObject *type;
	MonoBoolean is_pinned;
	guint16 local_index;
	MonoString *name;
} MonoReflectionLocalBuilder;

typedef struct {
	MonoObject object;
	gint32 count;
	gint32 type;
	gint32 eltype;
	MonoString *guid;
	MonoString *mcookie;
	MonoString *marshaltype;
	MonoObject *marshaltyperef;
	gint32 param_num;
	MonoBoolean has_size;
} MonoReflectionMarshal;

typedef struct {
	MonoObject object;
	MonoObject* methodb;
	MonoString *name;
	MonoArray *cattrs;
	MonoReflectionMarshal *marshal_info;
	guint32 attrs;
	int position;
	guint32 table_idx;
	MonoObject *def_value;
} MonoReflectionParamBuilder;

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoReflectionILGen *ilgen;
	MonoArray *parameters;
	guint32 attrs;
	guint32 iattrs;
	guint32 table_idx;
	guint32 call_conv;
	MonoObject *type;
	MonoArray *pinfo;
	MonoArray *cattrs;
	MonoBoolean init_locals;
	MonoArray *param_modreq;
	MonoArray *param_modopt;
} MonoReflectionCtorBuilder;

/* Safely access System.Reflection.Emit.ConstructorBuilder from native code */
TYPED_HANDLE_DECL (MonoReflectionCtorBuilder);

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoObject *rtype;
	MonoArray *parameters;
	guint32 attrs;
	guint32 iattrs;
	MonoString *name;
	guint32 table_idx;
	MonoArray *code;
	MonoReflectionILGen *ilgen;
	MonoObject *type;
	MonoArray *pinfo;
	MonoArray *cattrs;
	MonoArray *override_methods;
	MonoString *dll;
	MonoString *dllentry;
	guint32 charset;
	guint32 extra_flags;
	guint32 native_cc;
	guint32 call_conv;
	MonoBoolean init_locals;
	MonoGenericContainer *generic_container;
	MonoArray *generic_params;
	MonoArray *return_modreq;
	MonoArray *return_modopt;
	MonoArray *param_modreq;
	MonoArray *param_modopt;
} MonoReflectionMethodBuilder;

/* Safely access System.Reflection.Emit.MethodBuilder from native code */
TYPED_HANDLE_DECL (MonoReflectionMethodBuilder);

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoReflectionType *parent;
	MonoReflectionType *ret;
	MonoArray *parameters;
	MonoString *name;
	guint32 table_idx;
	guint32 call_conv;
} MonoReflectionArrayMethod;

/* Safely access System.Reflection.Emit.MonoArrayMethod from native code */
TYPED_HANDLE_DECL (MonoReflectionArrayMethod);

typedef struct {
	MonoReflectionAssembly assembly;
	MonoDynamicAssembly *dynamic_assembly;
	MonoArray *modules;
	MonoString *name;
	MonoArray *cattrs;
	MonoString *version;
	MonoString *culture;
	MonoArray *loaded_modules;
	guint32 access;
} MonoReflectionAssemblyBuilder;

/* Safely access System.Reflection.Emit.AssemblyBuilder from native code */
TYPED_HANDLE_DECL (MonoReflectionAssemblyBuilder);

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoObject *type;
	MonoString *name;
	MonoObject *def_value;
	gint32 offset;
	MonoReflectionType *typeb;
	MonoArray *rva_data;
	MonoArray *cattrs;
	MonoReflectionMarshal *marshal_info;
	MonoClassField *handle;
	MonoArray *modreq;
	MonoArray *modopt;
} MonoReflectionFieldBuilder;

/* Safely access System.Reflection.Emit.FieldBuilder from native code */ 
TYPED_HANDLE_DECL (MonoReflectionFieldBuilder);

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoString *name;
	MonoObject *type;
	MonoArray *parameters;
	MonoArray *cattrs;
	MonoObject *def_value;
	MonoReflectionMethodBuilder *set_method;
	MonoReflectionMethodBuilder *get_method;
	gint32 table_idx;
	MonoObject *type_builder;
	MonoArray *returnModReq;
	MonoArray *returnModOpt;
	MonoArray *paramModReq;
	MonoArray *paramModOpt;
	guint32 call_conv;
} MonoReflectionPropertyBuilder;

/* System.RuntimeModule */
struct _MonoReflectionModule {
	MonoObject	obj;
	MonoImage  *image;
	MonoReflectionAssembly *assembly;
	MonoString *fqname;
	MonoString *name;
	MonoString *scopename;
	MonoBoolean is_resource;
	guint32 token;
};

/* Safely access System.Reflection.Module from native code */
TYPED_HANDLE_DECL (MonoReflectionModule);

typedef struct {
	MonoReflectionModule module;
	MonoDynamicImage *dynamic_image;
	gint32     num_types;
	MonoArray *types;
	MonoArray *cattrs;
	guint32    table_idx;
	MonoReflectionAssemblyBuilder *assemblyb;
	gboolean is_main;
	MonoArray *resources;
	GHashTable *unparented_classes;
	MonoArray *table_indexes;
} MonoReflectionModuleBuilder;

/* Safely acess System.Reflection.Emit.ModuleBuidler from native code */
TYPED_HANDLE_DECL (MonoReflectionModuleBuilder);

typedef enum {
	MonoTypeBuilderNew = 0,
	MonoTypeBuilderEntered = 1,
	MonoTypeBuilderFinished = 2
} MonoTypeBuilderState;

struct _MonoReflectionTypeBuilder {
	MonoReflectionType type;
	MonoString *name;
	MonoString *nspace;
	MonoObject *parent;
	MonoReflectionType *nesting_type;
	MonoArray *interfaces;
	gint32     num_methods;
	MonoArray *methods;
	MonoArray *ctors;
	MonoArray *properties;
	gint32     num_fields;
	MonoArray *fields;
	MonoArray *events;
	MonoArray *cattrs;
	MonoArray *subtypes;
	guint32 attrs;
	guint32 table_idx;
	MonoReflectionModuleBuilder *module;
	gint32 class_size;
	gint32 packing_size;
	MonoGenericContainer *generic_container;
	MonoArray *generic_params;
	MonoReflectionType *created;
	gint32 state;
};

typedef struct {
	MonoReflectionType type;
	MonoReflectionType *element_type;
	gint32 rank;
} MonoReflectionArrayType;

/* Safely access System.Reflection.Emit.ArrayType (in DerivedTypes.cs) from native code */
TYPED_HANDLE_DECL (MonoReflectionArrayType);

typedef struct {
	MonoReflectionType type;
	MonoReflectionType *element_type;
} MonoReflectionDerivedType;

/* Safely access System.Reflection.Emit.SymbolType and subclasses (in DerivedTypes.cs) from native code */
TYPED_HANDLE_DECL (MonoReflectionDerivedType);

typedef struct {
	MonoReflectionType type;
	MonoReflectionTypeBuilder *tbuilder;
	MonoReflectionMethodBuilder *mbuilder;
	MonoString *name;
	guint32 index;
	MonoReflectionType *base_type;
	MonoArray *iface_constraints;
	MonoArray *cattrs;
	guint32 attrs;
} MonoReflectionGenericParam;

/* Safely access System.Reflection.Emit.GenericTypeParameterBuilder from native code */
TYPED_HANDLE_DECL (MonoReflectionGenericParam);

typedef struct {
	MonoReflectionType type;
	MonoReflectionTypeBuilder *tb;
} MonoReflectionEnumBuilder;

/* Safely access System.Reflection.Emit.EnumBuilder from native code */
TYPED_HANDLE_DECL (MonoReflectionEnumBuilder);

typedef struct _MonoReflectionGenericClass MonoReflectionGenericClass;
struct _MonoReflectionGenericClass {
	MonoReflectionType type;
	MonoReflectionType *generic_type; /*Can be either a MonoType or a TypeBuilder*/
	MonoArray *type_arguments;
};

/* Safely access System.Reflection.Emit.TypeBuilderInstantiation from native code */
TYPED_HANDLE_DECL (MonoReflectionGenericClass);

typedef struct {
	MonoObject  obj;
	MonoString *name;
	MonoReflectionType *type;
	MonoReflectionTypeBuilder *typeb;
	MonoArray *cattrs;
	MonoReflectionMethodBuilder *add_method;
	MonoReflectionMethodBuilder *remove_method;
	MonoReflectionMethodBuilder *raise_method;
	MonoArray *other_methods;
	guint32 attrs;
	guint32 table_idx;
} MonoReflectionEventBuilder;

typedef struct {
	MonoObject  obj;
	MonoReflectionMethod *ctor;
	MonoArray *data;
} MonoReflectionCustomAttr;

TYPED_HANDLE_DECL (MonoReflectionCustomAttr);

typedef struct {
	MonoObject object;
	guint32 utype;
	gint32 safe_array_subtype;
	MonoReflectionType *marshal_safe_array_user_defined_subtype;
	gint32 IidParameterIndex;
	guint32 array_subtype;
	gint16 size_param_index;
	gint32 size_const;
	MonoString *marshal_type;
	MonoReflectionType *marshal_type_ref;
	MonoString *marshal_cookie;
} MonoReflectionMarshalAsAttribute;

/* Safely access System.Runtime.InteropServices.MarshalAsAttribute */
TYPED_HANDLE_DECL (MonoReflectionMarshalAsAttribute);

typedef struct {
	MonoObject object;
	gint32 call_conv;
	gint32 charset;
	MonoBoolean best_fit_mapping;
	MonoBoolean throw_on_unmappable;
	MonoBoolean set_last_error;
} MonoReflectionUnmanagedFunctionPointerAttribute;

typedef struct {
	MonoObject object;
	MonoString *guid;
} MonoReflectionGuidAttribute;

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoString *name;
	MonoReflectionType *rtype;
	MonoArray *parameters;
	guint32 attrs;
	guint32 call_conv;
	MonoReflectionModule *module;
	MonoBoolean skip_visibility;
	MonoBoolean init_locals;
	MonoReflectionILGen *ilgen;
	gint32 nrefs;
	MonoArray *refs;
	GSList *referenced_by;
	MonoReflectionType *owner;
} MonoReflectionDynamicMethod;	

/* Safely access System.Reflection.Emit.DynamicMethod from native code */
TYPED_HANDLE_DECL (MonoReflectionDynamicMethod);

typedef struct {
	MonoObject object;
	MonoReflectionModuleBuilder *module;
	MonoArray *arguments;
	guint32 type;
	MonoReflectionType *return_type;
	guint32 call_conv;
	guint32 unmanaged_call_conv;
	MonoArray *modreqs;
	MonoArray *modopts;
} MonoReflectionSigHelper;

/* Safely access System.Reflection.Emit.SignatureHelper from native code */
TYPED_HANDLE_DECL (MonoReflectionSigHelper);

typedef struct {
	MonoObject object;
	MonoBoolean visible;
} MonoReflectionComVisibleAttribute;

typedef struct {
	MonoObject object;
	MonoReflectionType *type;
} MonoReflectionComDefaultInterfaceAttribute;

enum {
	RESOURCE_LOCATION_EMBEDDED = 1,
	RESOURCE_LOCATION_ANOTHER_ASSEMBLY = 2,
	RESOURCE_LOCATION_IN_MANIFEST = 4
};

typedef struct {
	MonoObject object;
	MonoReflectionAssembly *assembly;
	MonoString *filename;
	guint32 location;
} MonoManifestResourceInfo;

/* Safely access System.Reflection.ManifestResourceInfo from native code */
TYPED_HANDLE_DECL (MonoManifestResourceInfo);

/* A boxed IntPtr */
typedef struct {
	MonoObject object;
	gpointer m_value;
} MonoIntPtr;

/* Keep in sync with System.GenericParameterAttributes */
typedef enum {
	GENERIC_PARAMETER_ATTRIBUTE_NON_VARIANT		= 0,
	GENERIC_PARAMETER_ATTRIBUTE_COVARIANT		= 1,
	GENERIC_PARAMETER_ATTRIBUTE_CONTRAVARIANT	= 2,
	GENERIC_PARAMETER_ATTRIBUTE_VARIANCE_MASK	= 3,

	GENERIC_PARAMETER_ATTRIBUTE_NO_SPECIAL_CONSTRAINT	= 0,
	GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT	= 4,
	GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT	= 8,
	GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT	= 16,
	GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK	= 28
} GenericParameterAttributes;

typedef struct {
	MonoType *type;
	MonoClassField *field;
	MonoProperty *prop;
} CattrNamedArg;

// Keep in sync with System.Runtime.Loader.AssemblyLoadContext.InternalState
typedef enum {
	ALIVE = 0,
	UNLOADING = 1
} MonoManagedAssemblyLoadContextInternalState;

// Keep in sync with System.Runtime.Loader.AssemblyLoadContext
typedef struct {
	MonoObject object;
	MonoAssemblyLoadContext *native_assembly_load_context;
} MonoManagedAssemblyLoadContext;

TYPED_HANDLE_DECL (MonoManagedAssemblyLoadContext);

/* All MonoInternalThread instances should be pinned, so it's safe to use the raw ptr.  However
 * for uniformity, icall wrapping will make handles anyway.  So this is the method for getting the payload.
 */
static inline MonoInternalThread*
mono_internal_thread_handle_ptr (MonoInternalThreadHandle h)
{
	/* The SUPPRESS here prevents a Centrinel warning due to merely seeing this
	 * function definition.  Callees will still get a warning unless we
	 * attach a suppress attribute to the declaration.
	 */
	return MONO_HANDLE_SUPPRESS (MONO_HANDLE_RAW (h));
}

guint32       mono_image_insert_string (MonoReflectionModuleBuilderHandle module, MonoStringHandle str, MonoError *error);
guint32       mono_image_create_token  (MonoDynamicImage *assembly, MonoObjectHandle obj, gboolean create_methodspec, gboolean register_token, MonoError *error);
void          mono_dynamic_image_free (MonoDynamicImage *image);
void          mono_dynamic_image_free_image (MonoDynamicImage *image);
void          mono_dynamic_image_release_gc_roots (MonoDynamicImage *image);

void        mono_reflection_setup_internal_class  (MonoReflectionTypeBuilder *tb);

void        mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides, MonoError *error);

void mono_reflection_destroy_dynamic_method (MonoReflectionDynamicMethod *mb);

ICALL_EXPORT
void
ves_icall_SymbolType_create_unmanaged_type (MonoReflectionType *type);

void        mono_reflection_register_with_runtime (MonoReflectionType *type);

MonoMethodSignature * mono_reflection_lookup_signature (MonoImage *image, MonoMethod *method, guint32 token, MonoError *error);

MonoArrayHandle mono_param_get_objects_internal  (MonoMethod *method, MonoClass *refclass, MonoError *error);

MonoClass*
mono_class_bind_generic_parameters (MonoClass *klass, int type_argc, MonoType **types, gboolean is_dynamic);
MonoType*
mono_reflection_bind_generic_parameters (MonoReflectionTypeHandle type, int type_argc, MonoType **types, MonoError *error);
void
mono_reflection_generic_class_initialize (MonoReflectionGenericClass *type, MonoArray *fields);

ICALL_EXPORT
MonoReflectionEvent *
ves_icall_TypeBuilder_get_event_info (MonoReflectionTypeBuilder *tb, MonoReflectionEventBuilder *eb);

MonoReflectionMarshalAsAttributeHandle
mono_reflection_marshal_as_attribute_from_marshal_spec (MonoClass *klass, MonoMarshalSpec *spec, MonoError *error);

gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

gboolean
mono_reflection_call_is_assignable_to (MonoClass *klass, MonoClass *oklass, MonoError *error);

gboolean
mono_get_constant_value_from_blob (MonoTypeEnum type, const char *blob, void *value, MonoStringHandleOut string_handle, MonoError *error);

gboolean
mono_metadata_read_constant_value (const char *blob, MonoTypeEnum type, void *value, MonoError *error);

char*
mono_string_from_blob (const char *str, MonoError *error);

void
mono_release_type_locks (MonoInternalThread *thread);

/**
 * mono_string_handle_length:
 * \param s \c MonoString
 * \returns the length in characters of the string
 */
#ifdef ENABLE_CHECKED_BUILD_GC

int
mono_string_handle_length (MonoStringHandle s);

#else

#define mono_string_handle_length(s) (MONO_HANDLE_GETVAL ((s), length))

#endif

char *
mono_string_handle_to_utf8 (MonoStringHandle s, MonoError *error);

char *
mono_string_to_utf8_image (MonoImage *image, MonoStringHandle s, MonoError *error);

MonoArrayHandle
mono_array_clone_in_domain (MonoArrayHandle array, MonoError *error);

MonoArray*
mono_array_clone_checked (MonoArray *array, MonoError *error);

void
mono_array_full_copy (MonoArray *src, MonoArray *dest);

void
mono_array_full_copy_unchecked_size (MonoArray *src, MonoArray *dest, MonoClass *klass, uintptr_t size);

gboolean
mono_array_calc_byte_len (MonoClass *klass, uintptr_t len, uintptr_t *res);

MonoArray*
mono_array_new_checked (MonoClass *eclass, uintptr_t n, MonoError *error);

MonoArray*
mono_array_new_full_checked (MonoClass *array_class, uintptr_t *lengths, intptr_t *lower_bounds, MonoError *error);

ICALL_EXPORT
MonoArray*
ves_icall_array_new_specific (MonoVTable *vtable, uintptr_t n);

gpointer
mono_create_ftnptr (gpointer addr);

gpointer
mono_get_addr_from_ftnptr (gpointer descr);

void
mono_nullable_init (guint8 *buf, MonoObject *value, MonoClass *klass);

void
mono_nullable_init_from_handle (guint8 *buf, MonoObjectHandle value, MonoClass *klass);

void
mono_nullable_init_unboxed (guint8 *buf, gpointer value, MonoClass *klass);

MonoObject *
mono_value_box_checked (MonoClass *klass, void* val, MonoError *error);

MonoObjectHandle
mono_value_box_handle (MonoClass *klass, gpointer val, MonoError *error);

MonoObject*
mono_nullable_box (gpointer buf, MonoClass *klass, MonoError *error);

MonoObjectHandle
mono_nullable_box_handle (gpointer buf, MonoClass *klass, MonoError *error);

// A code size optimization (source and object) equivalent to MONO_HANDLE_NEW (MonoObject, NULL);
MonoObjectHandle
mono_new_null (void);

#ifdef MONO_SMALL_CONFIG
#define MONO_IMT_SIZE 9
#else
#define MONO_IMT_SIZE 19
#endif

typedef union {
	int vtable_slot;
	gpointer target_code;
} MonoImtItemValue;

typedef struct _MonoImtBuilderEntry {
	gpointer key;
	struct _MonoImtBuilderEntry *next;
	MonoImtItemValue value;
	int children;
	guint8 has_target_code : 1;
} MonoImtBuilderEntry;

typedef struct _MonoIMTCheckItem MonoIMTCheckItem;

struct _MonoIMTCheckItem {
	gpointer          key;
	int               check_target_idx;
	MonoImtItemValue  value;
	guint8           *jmp_code;
	guint8           *code_target;
	guint8            is_equals;
	guint8            compare_done;
	guint8            chunk_size;
	guint8            short_branch;
	guint8            has_target_code;
};

typedef gpointer (*MonoImtTrampolineBuilder) (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count, gpointer fail_trunk);

void
mono_install_imt_trampoline_builder (MonoImtTrampolineBuilder func);

void
mono_set_always_build_imt_trampolines (gboolean value);

void
mono_vtable_build_imt_slot (MonoVTable* vtable, int imt_slot);

guint32
mono_method_get_imt_slot (MonoMethod *method);

void
mono_method_add_generic_virtual_invocation (MonoVTable *vtable,
											gpointer *vtable_slot,
											MonoMethod *method, gpointer code);

void
mono_unhandled_exception_checked (MonoObjectHandle exc, MonoError *error);

void
mono_first_chance_exception_checked (MonoObjectHandle exc, MonoError *error);

void
mono_first_chance_exception_internal (MonoObject *exc_raw);

MonoVTable *
mono_class_try_get_vtable (MonoClass *klass);

gboolean
mono_runtime_run_module_cctor (MonoImage *image, MonoError *error);

gboolean
mono_runtime_class_init_full (MonoVTable *vtable, MonoError *error);

void
mono_method_clear_object (MonoMethod *method);

gsize*
mono_class_compute_bitmap (MonoClass *klass, gsize *bitmap, int size, int offset, int *max_set, gboolean static_fields);

gboolean
mono_class_is_reflection_method_or_constructor (MonoClass *klass);

MonoObjectHandle
mono_get_object_from_blob (MonoType *type, const char *blob, MonoStringHandleOut string_handle, MonoError *error);

gboolean
mono_class_has_ref_info (MonoClass *klass);

MonoReflectionTypeBuilder*
mono_class_get_ref_info_raw (MonoClass *klass);

void
mono_class_set_ref_info (MonoClass *klass, MonoObjectHandle obj);

void
mono_class_free_ref_info (MonoClass *klass);

MonoObject *
mono_object_new_pinned (MonoClass *klass, MonoError *error);

MonoObjectHandle
mono_object_new_pinned_handle (MonoClass *klass, MonoError *error);

MonoObject *
mono_object_new_specific_checked (MonoVTable *vtable, MonoError *error);

ICALL_EXPORT
MonoObject *
ves_icall_object_new (MonoClass *klass);
	
ICALL_EXPORT
MonoObject *
ves_icall_object_new_specific (MonoVTable *vtable);

MonoObject *
mono_object_new_alloc_specific_checked (MonoVTable *vtable, MonoError *error);

void
mono_field_get_value_internal (MonoObject *obj, MonoClassField *field, void *value);

void
mono_field_static_get_value_checked (MonoVTable *vt, MonoClassField *field, void *value, MonoStringHandleOut string_handle, MonoError *error);

void
mono_field_static_get_value_for_thread (MonoInternalThread *thread, MonoVTable *vt, MonoClassField *field, void *value, MonoStringHandleOut string_handle, MonoError *error);

guint8*
mono_static_field_get_addr (MonoVTable *vt, MonoClassField *field);

MonoMethod*
mono_object_handle_get_virtual_method (MonoObjectHandle obj, MonoMethod *method, MonoError *error);

/* exported, used by the debugger */
MONO_API void *
mono_vtable_get_static_field_data (MonoVTable *vt);

MonoObject *
mono_field_get_value_object_checked (MonoClassField *field, MonoObject *obj, MonoError *error);

MonoObjectHandle
mono_static_field_get_value_handle (MonoClassField *field, MonoError *error);

gpointer
mono_special_static_field_get_offset (MonoClassField *field, MonoError *error);

gboolean
mono_property_set_value_handle (MonoProperty *prop, MonoObjectHandle obj, void **params, MonoError *error);

MonoObject*
mono_property_get_value_checked (MonoProperty *prop, void *obj, void **params, MonoError *error);

MonoString*
mono_object_try_to_string (MonoObject *obj, MonoObject **exc, MonoError *error);

char *
mono_string_to_utf8_ignore (MonoString *s);

gboolean
mono_monitor_is_il_fastpath_wrapper (MonoMethod *method);

MonoStringHandle
mono_string_is_interned_lookup (MonoStringHandle str, gboolean insert, MonoError *error);

/**
 * mono_string_intern_checked:
 * \param str String to intern
 * \param error set on error.
 * Interns the string passed.
 * \returns The interned string. On failure returns NULL and sets \p error
 */
#define mono_string_intern_checked(str, error) (mono_string_is_interned_lookup ((str), TRUE, (error)))

/**
 * mono_string_is_interned_internal:
 * \param o String to probe
 * \returns Whether the string has been interned.
 */
#define mono_string_is_interned_internal(str, error) (mono_string_is_interned_lookup ((str), FALSE, (error)))

char *
mono_exception_handle_get_native_backtrace (MonoExceptionHandle exc);

char *
mono_exception_get_managed_backtrace (MonoException *exc);

gboolean
mono_exception_try_get_managed_backtrace (MonoException *exc, const char *prefix, char **result);

void
mono_copy_value (MonoType *type, void *dest, void *value, int deref_pointer);

void
mono_error_raise_exception_deprecated (MonoError *target_error);

gboolean
mono_error_set_pending_exception_slow (MonoError *error);

static inline gboolean
mono_error_set_pending_exception (MonoError *error)
{
	return is_ok (error) ? FALSE : mono_error_set_pending_exception_slow (error);
}

MonoArray *
mono_glist_to_array (GList *list, MonoClass *eclass, MonoError *error);

MonoObject *
mono_object_new_checked (MonoClass *klass, MonoError *error);

MonoObjectHandle
mono_object_new_handle (MonoClass *klass, MonoError *error);

// This function skips handling of remoting and COM.
// "alloc" means "less".
MonoObjectHandle
mono_object_new_alloc_by_vtable (MonoVTable *vtable, MonoError *error);

MonoObject*
mono_object_new_mature (MonoVTable *vtable, MonoError *error);

MonoObjectHandle
mono_object_new_handle_mature (MonoVTable *vtable, MonoError *error);

MonoObject *
mono_object_clone_checked (MonoObject *obj, MonoError *error);

MonoObjectHandle
mono_object_clone_handle (MonoObjectHandle obj, MonoError *error);

MONO_COMPONENT_API MonoObject *
mono_object_isinst_checked (MonoObject *obj, MonoClass *klass, MonoError *error);

MonoObjectHandle
mono_object_handle_isinst (MonoObjectHandle obj, MonoClass *klass, MonoError *error);

MonoObjectHandle
mono_object_handle_isinst_mbyref (MonoObjectHandle obj, MonoClass *klass, MonoError *error);

gboolean
mono_object_handle_isinst_mbyref_raw (MonoObjectHandle obj, MonoClass *klass, MonoError *error);

MonoStringHandle
mono_string_new_size_handle (gint32 len, MonoError *error);

MonoString*
mono_string_new_len_checked (const char *text, guint length, MonoError *error);

MonoString *
mono_string_new_size_checked (gint32 len, MonoError *error);

MonoString*
mono_ldstr_checked (MonoImage *image, uint32_t str_index, MonoError *error);

MonoStringHandle
mono_ldstr_handle (MonoImage *image, uint32_t str_index, MonoError *error);

MONO_PROFILER_API MonoString*
mono_string_new_checked (const char *text, MonoError *merror);

MonoString*
mono_string_new_wtf8_len_checked (const char *text, guint length, MonoError *error);

MonoString *
mono_string_new_utf16_checked (const gunichar2 *text, gint32 len, MonoError *error);

MonoStringHandle
mono_string_new_utf16_handle (const gunichar2 *text, gint32 len, MonoError *error);

MonoStringHandle
mono_string_new_utf8_len (const char *text, guint length, MonoError *error);

MonoString *
mono_string_from_utf16_checked (const mono_unichar2 *data, MonoError *error);

MonoString *
mono_string_from_utf32_checked (const mono_unichar4 *data, MonoError *error);

char*
mono_ldstr_utf8 (MonoImage *image, guint32 idx, MonoError *error);

MONO_COMPONENT_API
char*
mono_utf16_to_utf8 (const mono_unichar2 *s, gsize slength, MonoError *error);

char*
mono_utf16_to_utf8len (const mono_unichar2 *s, gsize slength, gsize *utf8_length, MonoError *error);

gboolean
mono_runtime_object_init_checked (MonoObject *this_obj, MonoError *error);

MONO_PROFILER_API MonoObject*
mono_runtime_try_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);

// The exc parameter is deliberately missing and so far this has proven to reduce code duplication.
// In particular, if an exception is returned from underlying otherwise succeeded call,
// is set into the MonoError with mono_error_set_exception_instance.
// The result is that caller need only check MonoError.
MONO_COMPONENT_API MonoObjectHandle
mono_runtime_try_invoke_handle (MonoMethod *method, MonoObjectHandle obj, void **params, MonoError* error);

MonoObject*
mono_runtime_invoke_checked (MonoMethod *method, void *obj, void **params, MonoError *error);

MonoObjectHandle
mono_runtime_invoke_handle (MonoMethod *method, MonoObjectHandle obj, void **params, MonoError* error);

void
mono_runtime_invoke_handle_void (MonoMethod *method, MonoObjectHandle obj, void **params, MonoError* error);

MonoObject*
mono_runtime_try_invoke_array (MonoMethod *method, void *obj, MonoArray *params,
			       MonoObject **exc, MonoError *error);

MonoObject*
mono_runtime_invoke_array_checked (MonoMethod *method, void *obj, MonoArray *params,
				   MonoError *error);

void* 
mono_compile_method_checked (MonoMethod *method, MonoError *error);

MonoObject*
mono_runtime_delegate_try_invoke (MonoObject *delegate, void **params,
				  MonoObject **exc, MonoError *error);

MonoObject*
mono_runtime_delegate_invoke_checked (MonoObject *delegate, void **params,
				      MonoError *error);

MonoArrayHandle
mono_runtime_get_main_args_handle (MonoError *error);

int
mono_runtime_run_main_checked (MonoMethod *method, int argc, char* argv[],
			       MonoError *error);

int
mono_runtime_try_run_main (MonoMethod *method, int argc, char* argv[],
			   MonoObject **exc);

int
mono_runtime_exec_main_checked (MonoMethod *method, MonoArray *args, MonoError *error);

int
mono_runtime_try_exec_main (MonoMethod *method, MonoArray *args, MonoObject **exc);

MonoAssembly*
mono_try_assembly_resolve_handle (MonoAssemblyLoadContext *alc, MonoStringHandle fname, MonoAssembly *requesting, MonoError *error);

gboolean
mono_runtime_object_init_handle (MonoObjectHandle this_obj, MonoError *error);

/* GC write barriers support */
void
mono_gc_wbarrier_object_copy_handle (MonoObjectHandle obj, MonoObjectHandle src);

MonoMethod*
mono_class_get_virtual_method (MonoClass *klass, MonoMethod *method, MonoError *error);

MonoStringHandle
mono_string_empty_handle (void);

/*
 * mono_object_get_data:
 *
 *   Return a pointer to the beginning of data inside a MonoObject.
 */
static inline gpointer
mono_object_get_data (MonoObject *o)
{
	return (guint8*)o + MONO_ABI_SIZEOF (MonoObject);
}

#define mono_handle_get_data_unsafe(handle) ((gpointer)((guint8*)MONO_HANDLE_RAW (handle) + MONO_ABI_SIZEOF (MonoObject)))

gpointer
mono_vtype_get_field_addr (gpointer vtype, MonoClassField *field);

#define MONO_OBJECT_SETREF_INTERNAL(obj,fieldname,value) do {	\
		mono_gc_wbarrier_set_field_internal ((MonoObject*)(obj), &((obj)->fieldname), (MonoObject*)value);	\
		/*(obj)->fieldname = (value);*/	\
	} while (0)

/* This should be used if 's' can reside on the heap */
#define MONO_STRUCT_SETREF_INTERNAL(s,field,value) do { \
        mono_gc_wbarrier_generic_store_internal (&((s)->field), (MonoObject*)(value)); \
    } while (0)

static inline gunichar2*
mono_string_chars_internal (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return s->chars;
}

static inline int
mono_string_length_internal (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return s->length;
}

MonoString*
mono_string_empty_internal (MonoDomain *domain);

char *
mono_string_to_utf8len (MonoStringHandle s, gsize *utf8len, MonoError *error);

char*
mono_string_to_utf8_checked_internal (MonoString *string_obj, MonoError *error);

mono_bool
mono_string_equal_internal (MonoString *s1, MonoString *s2);

unsigned
mono_string_hash_internal (MonoString *s);

int
mono_object_hash_internal (MonoObject* obj);

ICALL_EXTERN_C
void
mono_value_copy_internal (void* dest, const void* src, MonoClass *klass);

void
mono_value_copy_array_internal (MonoArray *dest, int dest_idx, const void* src, int count);

MONO_PROFILER_API MonoVTable* mono_object_get_vtable_internal (MonoObject *obj);

MonoDomain*
mono_object_get_domain_internal (MonoObject *obj);

static inline gpointer
mono_object_unbox_internal (MonoObject *obj)
{
	/* add assert for valuetypes? */
	g_assert (m_class_is_valuetype (mono_object_class (obj)));
	return mono_object_get_data (obj);
}

ICALL_EXPORT
void
mono_monitor_exit_internal (MonoObject *obj);

MONO_PROFILER_API unsigned mono_object_get_size_internal (MonoObject *o);

MONO_PROFILER_API MonoDomain* mono_vtable_domain_internal (MonoVTable *vtable);

MONO_PROFILER_API MonoClass* mono_vtable_class_internal (MonoVTable *vtable);

MonoMethod*
mono_object_get_virtual_method_internal (MonoObject *obj, MonoMethod *method);

MonoMethod*
mono_get_delegate_invoke_internal (MonoClass *klass);

MonoMethod*
mono_get_delegate_begin_invoke_internal (MonoClass *klass);

MonoMethod*
mono_get_delegate_end_invoke_internal (MonoClass *klass);

void
mono_unhandled_exception_internal (MonoObject *exc);

void
mono_print_unhandled_exception_internal (MonoObject *exc);

void
mono_raise_exception_internal (MonoException *ex);

void
mono_field_set_value_internal (MonoObject *obj, MonoClassField *field, void *value);

void
mono_field_static_set_value_internal (MonoVTable *vt, MonoClassField *field, void *value);

void
mono_field_get_value_internal (MonoObject *obj, MonoClassField *field, void *value);

MonoMethod* mono_get_context_capture_method (void);

guint8*
mono_runtime_get_aotid_arr (void);

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
MonoGCHandle
mono_gchandle_new_internal (MonoObject *obj, mono_bool pinned);

MonoGCHandle
mono_gchandle_new_weakref_internal (MonoObject *obj, mono_bool track_resurrection);

ICALL_EXTERN_C
MonoObject*
mono_gchandle_get_target_internal (MonoGCHandle gchandle);

void mono_gchandle_free_internal (MonoGCHandle gchandle);

/* Reference queue support
 *
 * A reference queue is used to get notifications of when objects are collected.
 * Call mono_gc_reference_queue_new to create a new queue and pass the callback that
 * will be invoked when registered objects are collected.
 * Call mono_gc_reference_queue_add to register a pair of objects and data within a queue.
 * The callback will be triggered once an object is both unreachable and finalized.
 */
MonoReferenceQueue*
mono_gc_reference_queue_new_internal (mono_reference_queue_callback callback);

void
mono_gc_reference_queue_free_internal (MonoReferenceQueue *queue);

mono_bool
mono_gc_reference_queue_add_internal (MonoReferenceQueue *queue, MonoObject *obj, void *user_data);

#define mono_gc_reference_queue_add_handle(queue, obj, user_data) \
	(mono_gc_reference_queue_add_internal ((queue), MONO_HANDLE_RAW (MONO_HANDLE_CAST (MonoObject, obj)), (user_data)))

/* GC write barriers support */
void
mono_gc_wbarrier_set_field_internal (MonoObject *obj, void* field_ptr, MonoObject* value);

void
mono_gc_wbarrier_set_arrayref_internal  (MonoArray *arr, void* slot_ptr, MonoObject* value);

void
mono_gc_wbarrier_arrayref_copy_internal (void* dest_ptr, const void* src_ptr, int count);

void
mono_gc_wbarrier_generic_store_internal (void volatile* ptr, MonoObject* value);

void
mono_gc_wbarrier_generic_store_atomic_internal (void *ptr, MonoObject *value);

ICALL_EXTERN_C
void
mono_gc_wbarrier_generic_nostore_internal (void* ptr);

void
mono_gc_wbarrier_value_copy_internal (void* dest, const void* src, int count, MonoClass *klass);

void
mono_gc_wbarrier_object_copy_internal (MonoObject* obj, MonoObject *src);

MONO_COMPONENT_API char *
mono_runtime_get_managed_cmd_line (void);

char *
mono_runtime_get_cmd_line (int argc, char **argv);

#ifdef HOST_WASM
int
mono_string_instance_is_interned (MonoString *str);
#endif

#endif /* __MONO_OBJECT_INTERNALS_H__ */
