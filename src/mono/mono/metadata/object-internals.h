#ifndef __MONO_OBJECT_INTERNALS_H__
#define __MONO_OBJECT_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/handle.h>
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-stack-unwinding.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-coop-mutex.h"

/* Use this as MONO_CHECK_ARG_NULL (arg,expr,) in functions returning void */
#define MONO_CHECK_ARG(arg, expr, retval)		G_STMT_START{		  \
		if (G_UNLIKELY (!(expr)))							  \
       {								  \
		MonoException *ex;					  \
		char *msg = g_strdup_printf ("assertion `%s' failed",	  \
		#expr);							  \
		if (arg) {} /* check if the name exists */		  \
		ex = mono_get_exception_argument (#arg, msg);		  \
		g_free (msg);						  \
		mono_set_pending_exception (ex);					  \
		return retval;										  \
       };				}G_STMT_END

/* Use this as MONO_CHECK_ARG_NULL (arg,) in functions returning void */
#define MONO_CHECK_ARG_NULL(arg, retval)	    G_STMT_START{		  \
		if (G_UNLIKELY (arg == NULL))						  \
       {								  \
		MonoException *ex;					  \
		if (arg) {} /* check if the name exists */		  \
		ex = mono_get_exception_argument_null (#arg);		  \
		mono_set_pending_exception (ex);					  \
		return retval;										  \
       };				}G_STMT_END

/* Use this as MONO_ARG_NULL (arg,) in functions returning void */
#define MONO_CHECK_NULL(arg, retval)	    G_STMT_START{		  \
		if (G_UNLIKELY (arg == NULL))						  \
       {								  \
		MonoException *ex;					  \
		if (arg) {} /* check if the name exists */		  \
		ex = mono_get_exception_null_reference ();		  \
		mono_set_pending_exception (ex);					  \
		return retval;										  \
       };				}G_STMT_END

#define mono_string_builder_capacity(sb) sb->chunkOffset + sb->chunkChars->max_length
#define mono_string_builder_string_length(sb) sb->chunkOffset + sb->chunkLength

/* 
 * Macros which cache the results of lookups locally.
 * These should be used instead of the original versions, if the __GNUC__
 * restriction is acceptable.
 */

#ifdef __GNUC__

/* name should be a compile-time constant */
#define mono_class_get_field_from_name_cached(klass,name) ({ \
			static MonoClassField *tmp_field; \
			if (!tmp_field) { \
				tmp_field = mono_class_get_field_from_name ((klass), (name)); \
				g_assert (tmp_field); \
			}; \
			tmp_field; })
/* eclass should be a run-time constant */
#define mono_array_class_get_cached(eclass,rank) ({	\
			static MonoClass *tmp_klass; \
			if (!tmp_klass) { \
				tmp_klass = mono_array_class_get ((eclass), (rank));	\
				g_assert (tmp_klass); \
			}; \
			tmp_klass; })
/* eclass should be a run-time constant */
#define mono_array_new_cached(domain, eclass, size, error) ({	\
			MonoVTable *__vtable = mono_class_vtable ((domain), mono_array_class_get_cached ((eclass), 1));	\
			MonoArray *__arr = mono_array_new_specific_checked (__vtable, (size), (error)); \
			__arr; })

#else

#define mono_class_get_field_from_name_cached(klass,name) mono_class_get_field_from_name ((klass), (name))
#define mono_array_class_get_cached(eclass,rank) mono_array_class_get ((eclass), (rank))
#define mono_array_new_cached(domain, eclass, size, error) mono_array_new_specific_checked (mono_class_vtable ((domain), mono_array_class_get_cached ((eclass), 1)), (size), (error))

#endif

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
	/* we use double to ensure proper alignment on platforms that need it */
	double vector [MONO_ZERO_LEN_ARRAY];
};

#define MONO_SIZEOF_MONO_ARRAY (sizeof (MonoArray) - MONO_ZERO_LEN_ARRAY * sizeof (double))

struct _MonoString {
	MonoObject object;
	int32_t length;
	mono_unichar2 chars [MONO_ZERO_LEN_ARRAY];
};

#define mono_object_class(obj) (((MonoObject*)(obj))->vtable->klass)
#define mono_object_domain(obj) (((MonoObject*)(obj))->vtable->domain)

#define mono_string_chars_fast(s) ((mono_unichar2*)(s)->chars)
#define mono_string_length_fast(s) ((s)->length)

#define mono_array_length_fast(array) ((array)->max_length)
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
		mono_gc_wbarrier_set_arrayref ((array), __p, (MonoObject*)(value));	\
		/* *__p = (value);*/	\
	} while (0)
#define mono_array_memcpy_refs_fast(dest,destidx,src,srcidx,count)	\
	do {	\
		void **__p = (void **) mono_array_addr_fast ((dest), void*, (destidx));	\
		void **__s = mono_array_addr_fast ((src), void*, (srcidx));	\
		mono_gc_wbarrier_arrayref_copy (__p, __s, (count));	\
	} while (0)


typedef struct {
	MonoObject obj;
	MonoObject *identity;
} MonoMarshalByRefObject;

/* This is a copy of System.AppDomain */
struct _MonoAppDomain {
	MonoMarshalByRefObject mbr;
	MonoDomain *data;
};

typedef struct _MonoStringBuilder MonoStringBuilder;

struct _MonoStringBuilder {
	MonoObject object;
	MonoArray  *chunkChars;
	MonoStringBuilder* chunkPrevious;      // Link to the block logically before this block
	int chunkLength;                  // The index in ChunkChars that represent the end of the block
	int chunkOffset;                  // The logial offset (sum of all characters in previous blocks)
	int maxCapacity;
};

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
	MonoObject *dynamic_methods;
	gint32	    hresult;
	MonoString *source;
	MonoObject *serialization_manager;
	MonoObject *captured_traces;
	MonoArray  *native_trace_ips;
};

typedef struct {
	MonoException base;
} MonoSystemException;

typedef struct {
	MonoSystemException base;
	MonoString *param_name;
} MonoArgumentException;

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

typedef struct {
	MonoMarshalByRefObject object;
	gpointer     handle;
} MonoWaitHandle;

/* This is a copy of System.Runtime.Remoting.Messaging.CallType */
typedef enum {
	CallType_Sync = 0,
	CallType_BeginInvoke = 1,
	CallType_EndInvoke = 2,
	CallType_OneWay = 3
} MonoCallType;

/* This corresponds to System.Type */
struct _MonoReflectionType {
	MonoObject object;
	MonoType  *type;
};

/* Safely access System.Type from native code */
TYPED_HANDLE_DECL (MonoReflectionType);

/* This corresponds to System.RuntimeType */
typedef struct {
	MonoReflectionType type;
	MonoObject *type_info;
} MonoReflectionMonoType;

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

typedef struct {
	MonoMarshalByRefObject object;
	gpointer iunknown;
	GHashTable* itf_hash;
	MonoObject *synchronization_context;
} MonoComObject;

typedef struct {
	MonoRealProxy real_proxy;
	MonoComObject *com_object;
	gint32 ref_count;
} MonoComInteropProxy;

typedef struct {
	MonoObject	 object;
	MonoRealProxy	*rp;	
	MonoRemoteClass *remote_class;
	MonoBoolean	 custom_type_info;
} MonoTransparentProxy;

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

typedef enum {
	MONO_THREAD_FLAG_DONT_MANAGE = 1, // Don't wait for or abort this thread
	MONO_THREAD_FLAG_NAME_SET = 2, // Thread name set from managed code
} MonoThreadFlags;

struct _MonoInternalThread {
	MonoObject  obj;
	volatile int lock_thread_id; /* to be used as the pre-shifted thread id in thin locks. Used for appdomain_ref push/pop */
	HANDLE	    handle;
	MonoArray  *cached_culture_info;
	gunichar2  *name;
	guint32	    name_len;
	guint32	    state;
	MonoException *abort_exc;
	int abort_state_handle;
	guint64 tid;	/* This is accessed as a gsize in the code (so it can hold a 64bit pointer on systems that need it), but needs to reserve 64 bits of space on all machines as it corresponds to a field in managed code */
	gpointer stack_ptr;
	gpointer *static_data;
	void *thread_info; /*This is MonoThreadInfo*, but to simplify dependencies, let's make it a void* here. */
	MonoAppContext *current_appcontext;
	MonoThread *root_domain_thread;
	MonoObject *_serialized_principal;
	int _serialized_principal_version;
	gpointer appdomain_refs;
	/* This is modified using atomic ops, so keep it a gint32 */
	gint32 interruption_requested;
	MonoCoopMutex *synch_cs;
	MonoBoolean threadpool_thread;
	MonoBoolean thread_interrupt_requested;
	int stack_size;
	guint8	apartment_state;
	gint32 critical_region_level;
	gint32 managed_id;
	guint32 small_id;
	MonoThreadManageCallback manage_callback;
	gpointer interrupt_on_stop;
	gsize    flags;
	gpointer thread_pinning_ref;
	gsize abort_protected_block_count;
	gint32 priority;
	/* 
	 * These fields are used to avoid having to increment corlib versions
	 * when a new field is added to this structure.
	 * Please synchronize any changes with InternalThread in Thread.cs, i.e. add the
	 * same field there.
	 */
	gsize unused1;
	gsize unused2;

	/* This is used only to check that we are in sync between the representation
	 * of MonoInternalThread in native and InternalThread in managed
	 *
	 * DO NOT RENAME! DO NOT ADD FIELDS AFTER! */
	gpointer last;
};

struct _MonoThread {
	MonoObject obj;
	struct _MonoInternalThread *internal_thread;
	MonoObject *start_obj;
	MonoException *pending_exception;
};

typedef struct {
	guint32 state;
	MonoObject *additional;
} MonoStreamingContext;

typedef struct {
	MonoObject obj;
	MonoBoolean readOnly;
	MonoString *AMDesignator;
	MonoString *PMDesignator;
	MonoString *DateSeparator;
	MonoString *TimeSeparator;
	MonoString *ShortDatePattern;
	MonoString *LongDatePattern;
	MonoString *ShortTimePattern;
	MonoString *LongTimePattern;
	MonoString *MonthDayPattern;
	MonoString *YearMonthPattern;
	guint32 FirstDayOfWeek;
	guint32 CalendarWeekRule;
	MonoArray *AbbreviatedDayNames;
	MonoArray *DayNames;
	MonoArray *MonthNames;
	MonoArray *GenitiveMonthNames;
	MonoArray *AbbreviatedMonthNames;
	MonoArray *GenitiveAbbreviatedMonthNames;
	MonoArray *ShortDatePatterns;
	MonoArray *LongDatePatterns;
	MonoArray *ShortTimePatterns;
	MonoArray *LongTimePatterns;
	MonoArray *MonthDayPatterns;
	MonoArray *YearMonthPatterns;
	MonoArray *ShortestDayNames;
} MonoDateTimeFormatInfo;

typedef struct 
{
	MonoObject obj;
	MonoArray *numberGroupSizes;
	MonoArray *currencyGroupSizes;
	MonoArray *percentGroupSizes;
	MonoString *positiveSign;
	MonoString *negativeSign;
	MonoString *numberDecimalSeparator;
	MonoString *numberGroupSeparator;
	MonoString *currencyGroupSeparator;
	MonoString *currencyDecimalSeparator;
	MonoString *currencySymbol;
	MonoString *ansiCurrencySymbol;	/* unused */
	MonoString *naNSymbol;
	MonoString *positiveInfinitySymbol;
	MonoString *negativeInfinitySymbol;
	MonoString *percentDecimalSeparator;
	MonoString *percentGroupSeparator;
	MonoString *percentSymbol;
	MonoString *perMilleSymbol;
	MonoString *nativeDigits; /* unused */
	gint32 dataItem; /* unused */
	guint32 numberDecimalDigits;
	gint32 currencyDecimalDigits;
	gint32 currencyPositivePattern;
	gint32 currencyNegativePattern;
	gint32 numberNegativePattern;
	gint32 percentPositivePattern;
	gint32 percentNegativePattern;
	gint32 percentDecimalDigits;
} MonoNumberFormatInfo;

typedef struct {
	MonoObject obj;
	gint32 lcid;
	MonoString *icu_name;
	gpointer ICU_collator;
} MonoCompareInfo;

typedef struct {
	MonoObject obj;
	MonoString *NativeName;
	MonoArray *ShortDatePatterns;
	MonoArray *YearMonthPatterns;
	MonoArray *LongDatePatterns;
	MonoString *MonthDayPattern;

	MonoArray *EraNames;
	MonoArray *AbbreviatedEraNames;
	MonoArray *AbbreviatedEnglishEraNames;
	MonoArray *DayNames;
	MonoArray *AbbreviatedDayNames;
	MonoArray *SuperShortDayNames;
	MonoArray *MonthNames;
	MonoArray *AbbreviatedMonthNames;
	MonoArray *GenitiveMonthNames;
	MonoArray *GenitiveAbbreviatedMonthNames;
} MonoCalendarData;

typedef struct {
	MonoObject obj;
	MonoString *AMDesignator;
	MonoString *PMDesignator;
	MonoString *TimeSeparator;
	MonoArray *LongTimePatterns;
	MonoArray *ShortTimePatterns;
	guint32 FirstDayOfWeek;
	guint32 CalendarWeekRule;
} MonoCultureData;

typedef struct {
	MonoObject obj;
	MonoBoolean is_read_only;
	gint32 lcid;
	gint32 parent_lcid;
	gint32 datetime_index;
	gint32 number_index;
	gint32 calendar_type;
	MonoBoolean use_user_override;
	MonoNumberFormatInfo *number_format;
	MonoDateTimeFormatInfo *datetime_format;
	MonoObject *textinfo;
	MonoString *name;
	MonoString *englishname;
	MonoString *nativename;
	MonoString *iso3lang;
	MonoString *iso2lang;
	MonoString *win3lang;
	MonoString *territory;
	MonoArray *native_calendar_names;
	MonoCompareInfo *compareinfo;
	const void* text_info_data;
} MonoCultureInfo;

typedef struct {
	MonoObject obj;
	gint32 geo_id;
	MonoString *iso2name;
	MonoString *iso3name;
	MonoString *win3name;
	MonoString *english_name;
	MonoString *native_name;
	MonoString *currency_symbol;
	MonoString *iso_currency_symbol;
	MonoString *currency_english_name;
	MonoString *currency_native_name;
} MonoRegionInfo;

typedef struct {
	MonoObject obj;
	MonoString *str;
	gint32 options;
	MonoArray *key;
	gint32 lcid;
} MonoSortKey;

typedef struct {
	MonoObject object;
	guint32 intType;
} MonoInterfaceTypeAttribute;

/* 
 * Callbacks supplied by the runtime and called by the modules in metadata/
 * This interface is easier to extend than adding a new function type +
 * a new 'install' function for every callback.
 */
typedef struct {
	gpointer (*create_ftnptr) (MonoDomain *domain, gpointer addr);
	gpointer (*get_addr_from_ftnptr) (gpointer descr);
	char*    (*get_runtime_build_info) (void);
	gpointer (*get_vtable_trampoline) (MonoVTable *vtable, int slot_index);
	gpointer (*get_imt_trampoline) (MonoVTable *vtable, int imt_slot_index);
	gboolean (*imt_entry_inited) (MonoVTable *vtable, int imt_slot_index);
	void     (*set_cast_details) (MonoClass *from, MonoClass *to);
	void     (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
	gboolean (*tls_key_supported) (MonoTlsKey key);
	void     (*init_delegate) (MonoDelegate *del);
	MonoObject* (*runtime_invoke) (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);
	void*    (*compile_method) (MonoMethod *method, MonoError *error);
	gpointer (*create_jump_trampoline) (MonoDomain *domain, MonoMethod *method, gboolean add_sync_wrapper, MonoError *error);
	gpointer (*create_jit_trampoline) (MonoDomain *domain, MonoMethod *method, MonoError *error);
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
	gboolean (*mono_current_thread_has_handle_block_guard) (void);
} MonoRuntimeExceptionHandlingCallbacks;

/* used to free a dynamic method */
typedef void        (*MonoFreeMethodFunc)       (MonoDomain *domain, MonoMethod *method);

MONO_COLD void mono_set_pending_exception (MonoException *exc);

/* remoting and async support */

MonoAsyncResult *
mono_async_result_new	    (MonoDomain *domain, HANDLE handle, 
			     MonoObject *state, gpointer data, MonoObject *object_data, MonoError *error);

MonoObject *
ves_icall_System_Runtime_Remoting_Messaging_AsyncResult_Invoke (MonoAsyncResult *ares);

MonoWaitHandle *
mono_wait_handle_new	    (MonoDomain *domain, HANDLE handle, MonoError *error);

HANDLE
mono_wait_handle_get_handle (MonoWaitHandle *handle);

gboolean
mono_message_init	    (MonoDomain *domain, MonoMethodMessage *this_obj, 
			     MonoReflectionMethod *method, MonoArray *out_args, MonoError *error);

MonoObject *
mono_message_invoke	    (MonoObject *target, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args, MonoError *error);

MonoMethodMessage *
mono_method_call_message_new (MonoMethod *method, gpointer *params, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state, MonoError *error);

void
mono_method_return_message_restore (MonoMethod *method, gpointer *params, MonoArray *out_args, MonoError *error);

gboolean
mono_delegate_ctor_with_method (MonoObject *this_obj, MonoObject *target, gpointer addr, MonoMethod *method, MonoError *error);

gboolean
mono_delegate_ctor	    (MonoObject *this_obj, MonoObject *target, gpointer addr, MonoError *error);

void*
mono_class_get_allocation_ftn (MonoVTable *vtable, gboolean for_box, gboolean *pass_size_in_words);

void
mono_runtime_free_method    (MonoDomain *domain, MonoMethod *method);

void
mono_install_free_method    (MonoFreeMethodFunc func);

void
mono_install_callbacks      (MonoRuntimeCallbacks *cbs);

MonoRuntimeCallbacks*
mono_get_runtime_callbacks (void);

void
mono_install_eh_callbacks (MonoRuntimeExceptionHandlingCallbacks *cbs);

MonoRuntimeExceptionHandlingCallbacks *
mono_get_eh_callbacks (void);

void
mono_raise_exception_with_context (MonoException *ex, MonoContext *ctx);

void
mono_type_initialization_init (void);

void
mono_type_initialization_cleanup (void);

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

#define IS_MONOTYPE(obj) (!(obj) || (((MonoObject*)(obj))->vtable->klass->image == mono_defaults.corlib && ((MonoReflectionType*)(obj))->type != NULL))

/* This should be used for accessing members of Type[] arrays */
#define mono_type_array_get(arr,index) monotype_cast (mono_array_get ((arr), gpointer, (index)))

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

typedef struct _MonoReflectionGenericMethod MonoReflectionGenericMethod;
struct _MonoReflectionGenericMethod {
	MonoReflectionMethod method;
};

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

struct _MonoReflectionField {
	MonoObject object;
	MonoClass *klass;
	MonoClassField *field;
	MonoString *name;
	MonoReflectionType *type;
	guint32 attrs;
};

struct _MonoReflectionProperty {
	MonoObject object;
	MonoClass *klass;
	MonoProperty *property;
};

/*This is System.EventInfo*/
struct _MonoReflectionEvent {
	MonoObject object;
	MonoObject *cached_add_event;
};

typedef struct {
	MonoReflectionEvent object;
	MonoClass *klass;
	MonoEvent *event;
} MonoReflectionMonoEvent;

typedef struct {
	MonoObject object;
	MonoReflectionType *ClassImpl;
	MonoObject *DefaultValueImpl;
	MonoObject *MemberImpl;
	MonoString *NameImpl;
	gint32 PositionImpl;
	guint32 AttrsImpl;
	MonoObject *MarshalAsImpl;
} MonoReflectionParameter;

struct _MonoReflectionMethodBody {
	MonoObject object;
	MonoArray *clauses;
	MonoArray *locals;
	MonoArray *il;
	MonoBoolean init_locals;
	guint32 local_var_sig_token;
	guint32 max_stack;
};

struct _MonoReflectionAssembly {
	MonoObject object;
	MonoAssembly *assembly;
	MonoObject *resolve_event_holder;
	/* CAS related */
	MonoObject *evidence;	/* Evidence */
	MonoObject *minimum;	/* PermissionSet - for SecurityAction.RequestMinimum */
	MonoObject *optional;	/* PermissionSet - for SecurityAction.RequestOptional */
	MonoObject *refuse;	/* PermissionSet - for SecurityAction.RequestRefuse */
	MonoObject *granted;	/* PermissionSet - for the resolved assembly granted permissions */
	MonoObject *denied;	/* PermissionSet - for the resolved assembly denied permissions */
	/* */
	MonoBoolean from_byte_array;
	MonoString *name;
};

/* Safely access System.Reflection.Assembly from native code */
TYPED_HANDLE_DECL (MonoReflectionAssembly);

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
	MonoString *name;
	MonoString *name_space;
	MonoReflectionType *etype;
	MonoReflectionType *nested_in;
	MonoReflectionAssembly *assembly;
	guint32 rank;
	MonoBoolean isprimitive;
} MonoTypeInfo;

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

typedef struct {
	MonoObject object;
	MonoReflectionType *local_type;
	MonoBoolean is_pinned;
	guint16 local_index;
} MonoReflectionLocalVariableInfo;

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
	MonoArray *permissions;
} MonoReflectionCtorBuilder;

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
	MonoArray *permissions;
} MonoReflectionMethodBuilder;

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

typedef struct {
	MonoArray *data;
	MonoString *name;
	MonoString *filename;
	guint32 attrs;
	guint32 offset;
	MonoObject *stream;
} MonoReflectionResource;

typedef struct {
	guint32 res_type;
	guint32 res_id;
	guint32 lang_id;
	MonoArray *res_data;
} MonoReflectionWin32Resource;

typedef struct {
	guint32 action;
	MonoString *pset;
} MonoReflectionPermissionSet;

typedef struct {
	MonoReflectionAssembly assembly;
	MonoDynamicAssembly *dynamic_assembly;
	MonoReflectionMethod *entry_point;
	MonoArray *modules;
	MonoString *name;
	MonoString *dir;
	MonoArray *cattrs;
	MonoArray *resources;
	MonoArray *public_key;
	MonoString *version;
	MonoString *culture;
	guint32 algid;
	guint32 flags;
	guint32 pekind;
	MonoBoolean delay_sign;
	guint32 access;
	MonoArray *loaded_modules;
	MonoArray *win32_resources;
	/* CAS related */
	MonoArray *permissions_minimum;
	MonoArray *permissions_optional;
	MonoArray *permissions_refused;
	gint32 pe_kind;
	gint32 machine;
	MonoBoolean corlib_internal;
	MonoArray *type_forwarders;
	MonoArray *pktoken; /* as hexadecimal byte[] */
} MonoReflectionAssemblyBuilder;

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoObject *type;
	MonoString *name;
	MonoObject *def_value;
	gint32 offset;
	gint32 table_idx;
	MonoReflectionType *typeb;
	MonoArray *rva_data;
	MonoArray *cattrs;
	MonoReflectionMarshal *marshal_info;
	MonoClassField *handle;
	MonoArray *modreq;
	MonoArray *modopt;
} MonoReflectionFieldBuilder;

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
	MonoArray *guid;
	guint32    table_idx;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoArray *global_methods;
	MonoArray *global_fields;
	gboolean is_main;
	MonoArray *resources;
} MonoReflectionModuleBuilder;

typedef struct {
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
	MonoArray *permissions;
	MonoReflectionType *created;
} MonoReflectionTypeBuilder;

typedef struct {
	MonoReflectionType type;
	MonoReflectionType *element_type;
	int rank;
} MonoReflectionArrayType;

typedef struct {
	MonoReflectionType type;
	MonoReflectionType *element_type;
} MonoReflectionDerivedType;

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

typedef struct _MonoReflectionGenericClass MonoReflectionGenericClass;
struct _MonoReflectionGenericClass {
	MonoReflectionType type;
	MonoReflectionType *generic_type; /*Can be either a MonoType or a TypeBuilder*/
	MonoArray *type_arguments;
	guint32 initialized;
};

typedef struct {
	MonoObject  obj;
	MonoString *name;
	MonoString *codebase;
	gint32 major, minor, build, revision;
	MonoObject  *cultureInfo;
	guint32     flags;
	guint32     hashalg;
	MonoObject  *keypair;
	MonoArray   *publicKey;
	MonoArray   *keyToken;
	guint32     versioncompat;
	MonoObject *version;
	guint32     processor_architecture;
} MonoReflectionAssemblyName;

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

typedef struct {
	MonoObject object;
	MonoString *marshal_cookie;
	MonoString *marshal_type;
	MonoReflectionType *marshal_type_ref;
	MonoReflectionType *marshal_safe_array_user_defined_subtype;
	guint32 utype;
	guint32 array_subtype;
	gint32 safe_array_subtype;
	gint32 size_const;
	gint32 IidParameterIndex;
	gint16 size_param_index;
} MonoReflectionMarshalAsAttribute;

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

typedef struct {
	MonoObject object;
	MonoReflectionGenericClass *inst;
	MonoObject *fb; /*can be either a MonoField or a FieldBuilder*/
} MonoReflectionFieldOnTypeBuilderInst;

typedef struct {
	MonoObject object;
	MonoReflectionGenericClass *inst;
	MonoObject *cb; /*can be either a MonoCMethod or ConstructorBuilder*/
} MonoReflectionCtorOnTypeBuilderInst;

typedef struct {
	MonoObject object;
	MonoReflectionType *inst;
	MonoObject *mb; /*can be either a MonoMethod or MethodBuilder*/
	MonoArray *method_args;
	MonoReflectionMethodBuilder *generic_method_definition;
} MonoReflectionMethodOnTypeBuilderInst;

typedef struct {
	MonoObject object;
	MonoBoolean visible;
} MonoReflectionComVisibleAttribute;

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

gboolean          mono_image_create_pefile (MonoReflectionModuleBuilder *module, HANDLE file, MonoError *error);
guint32       mono_image_insert_string (MonoReflectionModuleBuilder *module, MonoString *str);
guint32       mono_image_create_token  (MonoDynamicImage *assembly, MonoObject *obj, gboolean create_methodspec, gboolean register_token, MonoError *error);
guint32       mono_image_create_method_token (MonoDynamicImage *assembly, MonoObject *obj, MonoArray *opt_param_types, MonoError *error);
void          mono_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObject *obj);
void          mono_dynamic_image_free (MonoDynamicImage *image);
void          mono_dynamic_image_free_image (MonoDynamicImage *image);
void          mono_dynamic_image_release_gc_roots (MonoDynamicImage *image);

void        mono_reflection_setup_internal_class  (MonoReflectionTypeBuilder *tb);

MonoReflectionType*
ves_icall_TypeBuilder_create_runtime_class (MonoReflectionTypeBuilder *tb);

void
ves_icall_TypeBuilder_setup_internal_class (MonoReflectionTypeBuilder *tb);

void        mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides, MonoError *error);

void mono_reflection_destroy_dynamic_method (MonoReflectionDynamicMethod *mb);

void
ves_icall_SymbolType_create_unmanaged_type (MonoReflectionType *type);

void        mono_reflection_register_with_runtime (MonoReflectionType *type);

void        mono_reflection_create_custom_attr_data_args (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoArray **typed_args, MonoArray **named_args, CattrNamedArg **named_arg_info, MonoError *error);
MonoMethodSignature * mono_reflection_lookup_signature (MonoImage *image, MonoMethod *method, guint32 token, MonoError *error);

MonoArray* mono_param_get_objects_internal  (MonoDomain *domain, MonoMethod *method, MonoClass *refclass, MonoError *error);

MonoClass*
mono_class_bind_generic_parameters (MonoClass *klass, int type_argc, MonoType **types, gboolean is_dynamic);
MonoType*
mono_reflection_bind_generic_parameters (MonoReflectionType *type, int type_argc, MonoType **types, MonoError *error);
void
mono_reflection_generic_class_initialize (MonoReflectionGenericClass *type, MonoArray *fields);

MonoReflectionEvent *
ves_icall_TypeBuilder_get_event_info (MonoReflectionTypeBuilder *tb, MonoReflectionEventBuilder *eb);

MonoArray *
ves_icall_SignatureHelper_get_signature_local (MonoReflectionSigHelper *sig);

MonoArray *
ves_icall_SignatureHelper_get_signature_field (MonoReflectionSigHelper *sig);

MonoReflectionMarshalAsAttribute* mono_reflection_marshal_as_attribute_from_marshal_spec (MonoDomain *domain, MonoClass *klass, MonoMarshalSpec *spec, MonoError *error);

gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

gboolean
mono_reflection_call_is_assignable_to (MonoClass *klass, MonoClass *oklass, MonoError *error);

void
ves_icall_System_Reflection_CustomAttributeData_ResolveArgumentsInternal (MonoReflectionMethod *method, MonoReflectionAssembly *assembly, gpointer data, guint32 data_length, MonoArray **ctor_args, MonoArray ** named_args);

MonoType*
mono_reflection_type_get_handle (MonoReflectionType *ref, MonoError *error);

gboolean
mono_image_build_metadata (MonoReflectionModuleBuilder *module, MonoError *error);

int
mono_get_constant_value_from_blob (MonoDomain* domain, MonoTypeEnum type, const char *blob, void *value, MonoError *error);

void
mono_release_type_locks (MonoInternalThread *thread);

char *
mono_string_to_utf8_mp	(MonoMemPool *mp, MonoString *s, MonoError *error);

char *
mono_string_to_utf8_image (MonoImage *image, MonoString *s, MonoError *error);


MonoArray*
mono_array_clone_in_domain (MonoDomain *domain, MonoArray *array, MonoError *error);

MonoArray*
mono_array_clone_checked (MonoArray *array, MonoError *error);

void
mono_array_full_copy (MonoArray *src, MonoArray *dest);

gboolean
mono_array_calc_byte_len (MonoClass *klass, uintptr_t len, uintptr_t *res);

MonoArray*
mono_array_new_checked (MonoDomain *domain, MonoClass *eclass, uintptr_t n, MonoError *error);

MonoArray*
mono_array_new_full_checked (MonoDomain *domain, MonoClass *array_class, uintptr_t *lengths, intptr_t *lower_bounds, MonoError *error);

MonoArray*
mono_array_new_specific_checked (MonoVTable *vtable, uintptr_t n, MonoError *error);

MonoArray*
ves_icall_array_new (MonoDomain *domain, MonoClass *eclass, uintptr_t n);

MonoArray*
ves_icall_array_new_specific (MonoVTable *vtable, uintptr_t n);

#ifndef DISABLE_REMOTING
MonoObject *
mono_remoting_invoke (MonoObject *real_proxy, MonoMethodMessage *msg, MonoObject **exc, MonoArray **out_args, MonoError *error);

gpointer
mono_remote_class_vtable (MonoDomain *domain, MonoRemoteClass *remote_class, MonoRealProxy *real_proxy, MonoError *error);

gboolean
mono_upgrade_remote_class (MonoDomain *domain, MonoObject *tproxy, MonoClass *klass, MonoError *error);

void*
mono_load_remote_field_checked (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, void **res, MonoError *error);

MonoObject *
mono_load_remote_field_new_checked (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, MonoError *error);

gboolean
mono_store_remote_field_checked (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, void* val, MonoError *error);

gboolean
mono_store_remote_field_new_checked (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, MonoObject *arg, MonoError *error);


#endif

gpointer
mono_create_ftnptr (MonoDomain *domain, gpointer addr);

gpointer
mono_get_addr_from_ftnptr (gpointer descr);

void
mono_nullable_init (guint8 *buf, MonoObject *value, MonoClass *klass);

MonoObject *
mono_value_box_checked (MonoDomain *domain, MonoClass *klass, void* val, MonoError *error);

MonoObject*
mono_nullable_box (guint8 *buf, MonoClass *klass, MonoError *error);

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

typedef gpointer (*MonoImtTrampolineBuilder) (MonoVTable *vtable, MonoDomain *domain,
		MonoIMTCheckItem **imt_entries, int count, gpointer fail_trunk);

void
mono_install_imt_trampoline_builder (MonoImtTrampolineBuilder func);

void
mono_set_always_build_imt_trampolines (gboolean value);

void
mono_vtable_build_imt_slot (MonoVTable* vtable, int imt_slot);

guint32
mono_method_get_imt_slot (MonoMethod *method);

void
mono_method_add_generic_virtual_invocation (MonoDomain *domain, MonoVTable *vtable,
											gpointer *vtable_slot,
											MonoMethod *method, gpointer code);

gpointer
mono_method_alloc_generic_virtual_trampoline (MonoDomain *domain, int size);

typedef enum {
	MONO_UNHANDLED_POLICY_LEGACY,
	MONO_UNHANDLED_POLICY_CURRENT
} MonoRuntimeUnhandledExceptionPolicy;

MonoRuntimeUnhandledExceptionPolicy
mono_runtime_unhandled_exception_policy_get (void);
void
mono_runtime_unhandled_exception_policy_set (MonoRuntimeUnhandledExceptionPolicy policy);

MonoVTable *
mono_class_try_get_vtable (MonoDomain *domain, MonoClass *klass);

gboolean
mono_runtime_class_init_full (MonoVTable *vtable, MonoError *error);

void
mono_method_clear_object (MonoDomain *domain, MonoMethod *method);

void
mono_class_compute_gc_descriptor (MonoClass *klass);

gsize*
mono_class_compute_bitmap (MonoClass *klass, gsize *bitmap, int size, int offset, int *max_set, gboolean static_fields);

MonoObject*
mono_object_xdomain_representation (MonoObject *obj, MonoDomain *target_domain, MonoError *error);

gboolean
mono_class_is_reflection_method_or_constructor (MonoClass *klass);

MonoObject *
mono_get_object_from_blob (MonoDomain *domain, MonoType *type, const char *blob, MonoError *error);

gpointer
mono_class_get_ref_info (MonoClass *klass);

void
mono_class_set_ref_info (MonoClass *klass, gpointer obj);

void
mono_class_free_ref_info (MonoClass *klass);

MonoObject *
mono_object_new_pinned (MonoDomain *domain, MonoClass *klass, MonoError *error);

MonoObject *
mono_object_new_specific_checked (MonoVTable *vtable, MonoError *error);

MonoObject *
ves_icall_object_new (MonoDomain *domain, MonoClass *klass);
	
MonoObject *
ves_icall_object_new_specific (MonoVTable *vtable);

MonoObject *
mono_object_new_alloc_specific_checked (MonoVTable *vtable, MonoError *error);

void
mono_field_static_get_value_checked (MonoVTable *vt, MonoClassField *field, void *value, MonoError *error);

void
mono_field_static_get_value_for_thread (MonoInternalThread *thread, MonoVTable *vt, MonoClassField *field, void *value, MonoError *error);

/* exported, used by the debugger */
MONO_API void *
mono_vtable_get_static_field_data (MonoVTable *vt);

MonoObject *
mono_field_get_value_object_checked (MonoDomain *domain, MonoClassField *field, MonoObject *obj, MonoError *error);

gboolean
mono_property_set_value_checked (MonoProperty *prop, void *obj, void **params, MonoError *error);

MonoObject*
mono_property_get_value_checked (MonoProperty *prop, void *obj, void **params, MonoError *error);

MonoString*
mono_object_to_string_checked (MonoObject *obj, MonoError *error);

MonoString*
mono_object_try_to_string (MonoObject *obj, MonoObject **exc, MonoError *error);

char *
mono_string_to_utf8_ignore (MonoString *s);

char *
mono_string_to_utf8_image_ignore (MonoImage *image, MonoString *s);

char *
mono_string_to_utf8_mp_ignore (MonoMemPool *mp, MonoString *s);

gboolean
mono_monitor_is_il_fastpath_wrapper (MonoMethod *method);

MonoString*
mono_string_intern_checked (MonoString *str, MonoError *error);

char *
mono_exception_get_native_backtrace (MonoException *exc);

MonoString *
ves_icall_Mono_Runtime_GetNativeStackTrace (MonoException *exc);

char *
mono_exception_get_managed_backtrace (MonoException *exc);

void
mono_copy_value (MonoType *type, void *dest, void *value, int deref_pointer);

void
mono_error_raise_exception (MonoError *target_error);

gboolean
mono_error_set_pending_exception (MonoError *error);

MonoArray *
mono_glist_to_array (GList *list, MonoClass *eclass, MonoError *error);

MonoObject *
mono_object_new_checked (MonoDomain *domain, MonoClass *klass, MonoError *error);

MonoObject*
mono_object_new_mature (MonoVTable *vtable, MonoError *error);

MonoObject*
mono_object_new_fast_checked (MonoVTable *vtable, MonoError *error);

MonoObject *
ves_icall_object_new_fast (MonoVTable *vtable);

MonoObject *
mono_object_clone_checked (MonoObject *obj, MonoError *error);

MonoObject *
mono_object_isinst_checked (MonoObject *obj, MonoClass *klass, MonoError *error);

MonoObject *
mono_object_isinst_mbyref_checked   (MonoObject *obj, MonoClass *klass, MonoError *error);

MonoString *
mono_string_new_size_checked (MonoDomain *domain, gint32 len, MonoError *error);

MonoString*
mono_ldstr_checked (MonoDomain *domain, MonoImage *image, uint32_t str_index, MonoError *error);

MonoString*
mono_string_new_len_checked (MonoDomain *domain, const char *text, guint length, MonoError *error);

MonoString*
mono_string_new_checked (MonoDomain *domain, const char *text, MonoError *merror);

MonoString *
mono_string_new_utf16_checked (MonoDomain *domain, const guint16 *text, gint32 len, MonoError *error);

MonoString *
mono_string_from_utf16_checked (mono_unichar2 *data, MonoError *error);

MonoString *
mono_string_from_utf32_checked (mono_unichar4 *data, MonoError *error);

char*
mono_ldstr_utf8 (MonoImage *image, guint32 idx, MonoError *error);

gboolean
mono_runtime_object_init_checked (MonoObject *this_obj, MonoError *error);

MonoObject*
mono_runtime_try_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);

MonoObject*
mono_runtime_invoke_checked (MonoMethod *method, void *obj, void **params, MonoError *error);

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

MonoArray*
mono_runtime_get_main_args_checked (MonoError *error);

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

MonoReflectionMethod*
ves_icall_MonoMethod_MakeGenericMethod_impl (MonoReflectionMethod *rmethod, MonoArray *types);

gint32
ves_icall_ModuleBuilder_getToken (MonoReflectionModuleBuilder *mb, MonoObject *obj, gboolean create_open_instance);

gint32
ves_icall_ModuleBuilder_getMethodToken (MonoReflectionModuleBuilder *mb,
										MonoReflectionMethod *method,
										MonoArray *opt_param_types);

void
ves_icall_ModuleBuilder_WriteToFile (MonoReflectionModuleBuilder *mb, HANDLE file);

void
ves_icall_ModuleBuilder_build_metadata (MonoReflectionModuleBuilder *mb);

void
ves_icall_ModuleBuilder_RegisterToken (MonoReflectionModuleBuilder *mb, MonoObject *obj, guint32 token);

MonoObject*
ves_icall_ModuleBuilder_GetRegisteredToken (MonoReflectionModuleBuilder *mb, guint32 token);

void
ves_icall_AssemblyBuilder_basic_init (MonoReflectionAssemblyBuilder *assemblyb);

MonoReflectionModule*
ves_icall_AssemblyBuilder_InternalAddModule (MonoReflectionAssemblyBuilder *ab, MonoString *fileName);

void
ves_icall_TypeBuilder_create_generic_class (MonoReflectionTypeBuilder *tb);

MonoArray*
ves_icall_CustomAttributeBuilder_GetBlob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues);

void
ves_icall_DynamicMethod_create_dynamic_method (MonoReflectionDynamicMethod *mb);

MonoBoolean
ves_icall_TypeBuilder_get_IsGenericParameter (MonoReflectionTypeBuilder *tb);

void
ves_icall_EnumBuilder_setup_enum_type (MonoReflectionType *enumtype,
									   MonoReflectionType *t);

MonoReflectionType*
ves_icall_ModuleBuilder_create_modified_type (MonoReflectionTypeBuilder *tb, MonoString *smodifiers);

void
ves_icall_ModuleBuilder_basic_init (MonoReflectionModuleBuilder *moduleb);

guint32
ves_icall_ModuleBuilder_getUSIndex (MonoReflectionModuleBuilder *module, MonoString *str);

void
ves_icall_ModuleBuilder_set_wrappers_type (MonoReflectionModuleBuilder *moduleb, MonoReflectionType *type);

void
ves_icall_GenericTypeParameterBuilder_initialize_generic_parameter (MonoReflectionGenericParam *gparam);

MonoReflectionMethod*
ves_icall_MethodBuilder_MakeGenericMethod (MonoReflectionMethod *rmethod, MonoArray *types);

#endif /* __MONO_OBJECT_INTERNALS_H__ */
