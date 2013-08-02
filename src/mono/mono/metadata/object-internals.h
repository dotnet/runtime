#ifndef __MONO_OBJECT_INTERNALS_H__
#define __MONO_OBJECT_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-stack-unwinding.h"
#include "mono/utils/mono-tls.h"

/* 
 * We should find a better place for this stuff. We can't put it in mono-compiler.h,
 * since that is included by libgc.
 */
#ifndef G_LIKELY
#define G_LIKELY(a) (a)
#define G_UNLIKELY(a) (a)
#endif

/*
 * glib defines this macro and uses it in the definition of G_LIKELY, and thus,
 * g_assert (). The macro expands to a complex piece of code, preventing some
 * gcc versions like 4.3.0 from handling the __builtin_expect construct properly,
 * causing the generation of the unlikely branch into the middle of the code.
 */
#ifdef _G_BOOLEAN_EXPR
#undef _G_BOOLEAN_EXPR
#define _G_BOOLEAN_EXPR(expr) ((gsize)(expr) != 0)
#endif

#if 1
#ifdef __GNUC__
#define mono_assert(expr)		   G_STMT_START{		  \
     if (!(expr))							  \
       {								  \
		MonoException *ex;					  \
		char *msg = g_strdup_printf ("file %s: line %d (%s): "	  \
		"assertion failed: (%s)", __FILE__, __LINE__,		  \
		__PRETTY_FUNCTION__, #expr);				  \
		ex = mono_get_exception_execution_engine (msg);		  \
		g_free (msg);						  \
		mono_raise_exception (ex);				  \
       };				}G_STMT_END

#define mono_assert_not_reached()		  G_STMT_START{		  \
     MonoException *ex;							  \
     char *msg = g_strdup_printf ("file %s: line %d (%s): "		  \
     "should not be reached", __FILE__, __LINE__, __PRETTY_FUNCTION__);	  \
     ex = mono_get_exception_execution_engine (msg);			  \
     g_free (msg);							  \
     mono_raise_exception (ex);						  \
}G_STMT_END
#else /* not GNUC */
#define mono_assert(expr)		   G_STMT_START{		  \
     if (!(expr))							  \
       {								  \
		MonoException *ex;					  \
		char *msg = g_strdup_printf ("file %s: line %d: "	  \
		"assertion failed: (%s)", __FILE__, __LINE__,		  \
		#expr);							  \
		ex = mono_get_exception_execution_engine (msg);		  \
		g_free (msg);						  \
		mono_raise_exception (ex);				  \
       };				}G_STMT_END

#define mono_assert_not_reached()		  G_STMT_START{		  \
     MonoException *ex;							  \
     char *msg = g_strdup_printf ("file %s: line %d): "			  \
     "should not be reached", __FILE__, __LINE__);			  \
     ex = mono_get_exception_execution_engine (msg);			  \
     g_free (msg);							  \
     mono_raise_exception (ex);						  \
}G_STMT_END
#endif
#else
#define mono_assert(expr) g_assert(expr)
#define mono_assert_not_reached() g_assert_not_reached() 
#endif

#define MONO_CHECK_ARG(arg, expr)		G_STMT_START{		  \
		if (G_UNLIKELY (!(expr)))							  \
       {								  \
		MonoException *ex;					  \
		char *msg = g_strdup_printf ("assertion `%s' failed",	  \
		#expr);							  \
		if (arg) {} /* check if the name exists */		  \
		ex = mono_get_exception_argument (#arg, msg);		  \
		g_free (msg);						  \
		mono_raise_exception (ex);				  \
       };				}G_STMT_END

#define MONO_CHECK_ARG_NULL(arg)	    G_STMT_START{		  \
		if (G_UNLIKELY (arg == NULL))						  \
       {								  \
		MonoException *ex;					  \
		if (arg) {} /* check if the name exists */		  \
		ex = mono_get_exception_argument_null (#arg);		  \
		mono_raise_exception (ex);				  \
       };				}G_STMT_END

/* 16 == default capacity */
#define mono_stringbuilder_capacity(sb) ((sb)->str ? ((sb)->str->length) : 16)

/* 
 * Macros which cache the results of lookups locally.
 * These should be used instead of the original versions, if the __GNUC__
 * restriction is acceptable.
 */

#ifdef __GNUC__

/* namespace and name should be a constant */
/* image must be mscorlib since other assemblies can be unloaded */
#define mono_class_from_name_cached(image,namespace,name) ({ \
			static MonoClass *tmp_klass; \
			if (!tmp_klass) { \
				g_assert (image == mono_defaults.corlib); \
				tmp_klass = mono_class_from_name ((image), (namespace), (name)); \
				g_assert (tmp_klass); \
			}; \
			tmp_klass; })
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
#define mono_array_new_cached(domain, eclass, size) mono_array_new_specific (mono_class_vtable ((domain), mono_array_class_get_cached ((eclass), 1)), (size))

#else

#define mono_class_from_name_cached(image,namespace,name) mono_class_from_name ((image), (namespace), (name))
#define mono_class_get_field_from_name_cached(klass,name) mono_class_get_field_from_name ((klass), (name))
#define mono_array_class_get_cached(eclass,rank) mono_array_class_get ((eclass), (rank))
#define mono_array_new_cached(domain, eclass, size) mono_array_new_specific (mono_class_vtable ((domain), mono_array_class_get_cached ((eclass), 1)), (size))

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

typedef struct {
	MonoObject obj;
	MonoObject *identity;
} MonoMarshalByRefObject;

/* This is a copy of System.AppDomain */
struct _MonoAppDomain {
	MonoMarshalByRefObject mbr;
	MonoDomain *data;
};

typedef struct {
	MonoObject object;
	gint32 length;
	MonoString *str;
	MonoString *cached_str;
	gint32 max_capacity;
} MonoStringBuilder;

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
	/* Stores the IPs and the generic sharing infos
	   (vtable/MRGCTX) of the frames. */
	MonoArray  *trace_ips;
	MonoObject *inner_ex;
	MonoString *message;
	MonoString *help_link;
	MonoString *class_name;
	MonoString *stack_trace;
	MonoString *remote_stack_trace;
	gint32	    remote_stack_index;
	gint32	    hresult;
	MonoString *source;
	MonoObject *_data;
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
	MonoSystemException base;
	MonoString *msg;
	MonoString *type_name;
} MonoTypeLoadException;

typedef struct {
	MonoException base;
	MonoObject *wrapped_exception;
} MonoRuntimeWrappedException;

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
	MonoBoolean  disposed;
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

typedef struct {
	MonoObject obj;
	gint32 il_offset;
	gint32 native_offset;
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
	HANDLE	    start_notify;
	gpointer stack_ptr;
	gpointer *static_data;
	gpointer jit_data;
	void *thread_info; /*This is MonoThreadInfo*, but to simplify dependencies, let's make it a void* here. */
	MonoAppContext *current_appcontext;
	MonoException *pending_exception;
	MonoThread *root_domain_thread;
	MonoObject *_serialized_principal;
	int _serialized_principal_version;
	gpointer appdomain_refs;
	/* This is modified using atomic ops, so keep it a gint32 */
	gint32 interruption_requested;
	gpointer suspend_event;
	gpointer suspended_event;
	gpointer resume_event;
	CRITICAL_SECTION *synch_cs;
	MonoBoolean threadpool_thread;
	MonoBoolean thread_dump_requested;
	MonoBoolean thread_interrupt_requested;
	gpointer end_stack; /* This is only used when running in the debugger. */
	int stack_size;
	guint8	apartment_state;
	gint32 critical_region_level;
	gint32 managed_id;
	guint32 small_id;
	MonoThreadManageCallback manage_callback;
	gpointer interrupt_on_stop;
	gsize    flags;
	gpointer android_tid;
	gpointer thread_pinning_ref;
	gint32 ignore_next_signal;
	MonoMethod *async_invoke_method;
	/* 
	 * These fields are used to avoid having to increment corlib versions
	 * when a new field is added to this structure.
	 * Please synchronize any changes with InternalThread in Thread.cs, i.e. add the
	 * same field there.
	 */
	gpointer unused1;
	gpointer unused2;
};

struct _MonoThread {
	MonoObject obj;
	struct _MonoInternalThread *internal_thread;
	MonoObject *start_obj;
	MonoObject *ec_to_set;
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
	MonoBoolean readOnly;
	MonoString *decimalFormats;
	MonoString *currencyFormats;
	MonoString *percentFormats;
	MonoString *digitPattern;
	MonoString *zeroPattern;
	gint32 currencyDecimalDigits;
	MonoString *currencyDecimalSeparator;
	MonoString *currencyGroupSeparator;
	MonoArray *currencyGroupSizes;
	gint32 currencyNegativePattern;
	gint32 currencyPositivePattern;
	MonoString *currencySymbol;
	MonoString *naNSymbol;
	MonoString *negativeInfinitySymbol;
	MonoString *negativeSign;
	guint32 numberDecimalDigits;
	MonoString *numberDecimalSeparator;
	MonoString *numberGroupSeparator;
	MonoArray *numberGroupSizes;
	gint32 numberNegativePattern;
	gint32 percentDecimalDigits;
	MonoString *percentDecimalSeparator;
	MonoString *percentGroupSeparator;
	MonoArray *percentGroupSizes;
	gint32 percentNegativePattern;
	gint32 percentPositivePattern;
	MonoString *percentSymbol;
	MonoString *perMilleSymbol;
	MonoString *positiveInfinitySymbol;
	MonoString *positiveSign;
} MonoNumberFormatInfo;

typedef struct {
	MonoObject obj;
	gint32 lcid;
	MonoString *icu_name;
	gpointer ICU_collator;
} MonoCompareInfo;

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
	gpointer (*get_vtable_trampoline) (int slot_index);
	gpointer (*get_imt_trampoline) (int imt_slot_index);
	void     (*set_cast_details) (MonoClass *from, MonoClass *to);
	void     (*debug_log) (int level, MonoString *category, MonoString *message);
	gboolean (*debug_log_is_enabled) (void);
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
} MonoRuntimeExceptionHandlingCallbacks;


/* used to free a dynamic method */
typedef void        (*MonoFreeMethodFunc)	 (MonoDomain *domain, MonoMethod *method);

/* Used to initialize the method pointers inside vtables */
typedef gboolean    (*MonoInitVTableFunc)    (MonoVTable *vtable);

void mono_set_pending_exception (MonoException *exc) MONO_INTERNAL;

/* remoting and async support */

MonoAsyncResult *
mono_async_result_new	    (MonoDomain *domain, HANDLE handle, 
			     MonoObject *state, gpointer data, MonoObject *object_data) MONO_INTERNAL;

MonoWaitHandle *
mono_wait_handle_new	    (MonoDomain *domain, HANDLE handle) MONO_INTERNAL;

HANDLE
mono_wait_handle_get_handle (MonoWaitHandle *handle) MONO_INTERNAL;

void
mono_message_init	    (MonoDomain *domain, MonoMethodMessage *this_obj, 
			     MonoReflectionMethod *method, MonoArray *out_args) MONO_INTERNAL;

MonoObject *
mono_message_invoke	    (MonoObject *target, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args) MONO_INTERNAL;

MonoMethodMessage *
mono_method_call_message_new (MonoMethod *method, gpointer *params, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state) MONO_INTERNAL;

void
mono_method_return_message_restore (MonoMethod *method, gpointer *params, MonoArray *out_args) MONO_INTERNAL;

void
mono_delegate_ctor_with_method (MonoObject *this, MonoObject *target, gpointer addr, MonoMethod *method) MONO_INTERNAL;

void
mono_delegate_ctor	    (MonoObject *this_obj, MonoObject *target, gpointer addr) MONO_INTERNAL;

void*
mono_class_get_allocation_ftn (MonoVTable *vtable, gboolean for_box, gboolean *pass_size_in_words) MONO_INTERNAL;

void
mono_runtime_free_method    (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;

void	    
mono_install_runtime_invoke (MonoInvokeFunc func) MONO_INTERNAL;

void	    
mono_install_compile_method (MonoCompileFunc func) MONO_INTERNAL;

void
mono_install_free_method    (MonoFreeMethodFunc func) MONO_INTERNAL;

void
mono_install_callbacks      (MonoRuntimeCallbacks *cbs) MONO_INTERNAL;

MonoRuntimeCallbacks*
mono_get_runtime_callbacks (void) MONO_INTERNAL;

void
mono_install_eh_callbacks (MonoRuntimeExceptionHandlingCallbacks *cbs) MONO_INTERNAL;

MonoRuntimeExceptionHandlingCallbacks *
mono_get_eh_callbacks (void) MONO_INTERNAL;

void
mono_raise_exception_with_context (MonoException *ex, MonoContext *ctx) MONO_INTERNAL;

void
mono_type_initialization_init (void) MONO_INTERNAL;

void
mono_type_initialization_cleanup (void) MONO_INTERNAL;

int
mono_thread_kill           (MonoInternalThread *thread, int signal) MONO_INTERNAL;

MonoNativeTlsKey
mono_thread_get_tls_key    (void) MONO_INTERNAL;

gint32
mono_thread_get_tls_offset (void) MONO_INTERNAL;

MonoNativeTlsKey
mono_domain_get_tls_key    (void) MONO_INTERNAL;

gint32
mono_domain_get_tls_offset (void) MONO_INTERNAL;

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

/* 
 * Make sure the argument, which should be a System.Type is a System.MonoType object 
 * or equivalent, and not an instance of 
 * a user defined subclass of System.Type. This should be used in places were throwing
 * an exception is safe.
 */
#define CHECK_MONOTYPE(obj) do { \
	if (!IS_MONOTYPE (obj)) \
		mono_raise_exception (mono_get_exception_not_supported ("User defined subclasses of System.Type are not yet supported")); \
	} while (0)

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
	/* 
	 * If non-NULL, this points to a memory location which stores the address of 
	 * the compiled code of the method, or NULL if it is not yet compiled.
	 */
	guint8 **method_code;
	MonoReflectionMethod *method_info;
	MonoReflectionMethod *original_method_info;
	MonoObject *data;
};

typedef struct _MonoMulticastDelegate MonoMulticastDelegate;
struct _MonoMulticastDelegate {
	MonoDelegate delegate;
	MonoMulticastDelegate *prev;
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
	MonoString *dll;
	MonoString *entry_point;
	MonoBoolean exact_spelling;
	MonoBoolean preserve_sig;
	MonoBoolean set_last_error;
	MonoBoolean best_fit_mapping;
	MonoBoolean throw_on_unmappable;
} MonoReflectionDllImportAttribute;

typedef struct {
	MonoObject object;
	gint32 call_conv;
	gint32 charset;
	MonoBoolean set_last_error;
	MonoBoolean best_fit_mapping;
	MonoBoolean throw_on_unmappable;
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

void          mono_image_create_pefile (MonoReflectionModuleBuilder *module, HANDLE file) MONO_INTERNAL;
void          mono_image_basic_init (MonoReflectionAssemblyBuilder *assembly) MONO_INTERNAL;
MonoReflectionModule * mono_image_load_module_dynamic (MonoReflectionAssemblyBuilder *assembly, MonoString *file_name) MONO_INTERNAL;
guint32       mono_image_insert_string (MonoReflectionModuleBuilder *module, MonoString *str) MONO_INTERNAL;
guint32       mono_image_create_token  (MonoDynamicImage *assembly, MonoObject *obj, gboolean create_methodspec, gboolean register_token) MONO_INTERNAL;
guint32       mono_image_create_method_token (MonoDynamicImage *assembly, MonoObject *obj, MonoArray *opt_param_types) MONO_INTERNAL;
void          mono_image_module_basic_init (MonoReflectionModuleBuilder *module) MONO_INTERNAL;
void          mono_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObject *obj) MONO_INTERNAL;
void          mono_dynamic_image_free (MonoDynamicImage *image) MONO_INTERNAL;
void          mono_image_set_wrappers_type (MonoReflectionModuleBuilder *mb, MonoReflectionType *type) MONO_INTERNAL;
void          mono_dynamic_image_release_gc_roots (MonoDynamicImage *image) MONO_INTERNAL;

void        mono_reflection_setup_internal_class  (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_create_internal_class (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_setup_generic_class   (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_create_generic_class  (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

MonoReflectionType* mono_reflection_create_runtime_class  (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides) MONO_INTERNAL;

void mono_reflection_create_dynamic_method (MonoReflectionDynamicMethod *m) MONO_INTERNAL;
void mono_reflection_destroy_dynamic_method (MonoReflectionDynamicMethod *mb) MONO_INTERNAL;

void        mono_reflection_initialize_generic_parameter (MonoReflectionGenericParam *gparam) MONO_INTERNAL;
void        mono_reflection_create_unmanaged_type (MonoReflectionType *type) MONO_INTERNAL;
void        mono_reflection_register_with_runtime (MonoReflectionType *type) MONO_INTERNAL;

void        mono_reflection_create_custom_attr_data_args (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoArray **typed_args, MonoArray **named_args, CattrNamedArg **named_arg_info, MonoError *error) MONO_INTERNAL;
MonoMethodSignature * mono_reflection_lookup_signature (MonoImage *image, MonoMethod *method, guint32 token) MONO_INTERNAL;

MonoArray* mono_param_get_objects_internal  (MonoDomain *domain, MonoMethod *method, MonoClass *refclass) MONO_INTERNAL;

MonoClass*
mono_class_bind_generic_parameters (MonoClass *klass, int type_argc, MonoType **types, gboolean is_dynamic) MONO_INTERNAL;
MonoType*
mono_reflection_bind_generic_parameters (MonoReflectionType *type, int type_argc, MonoType **types) MONO_INTERNAL;
MonoReflectionMethod*
mono_reflection_bind_generic_method_parameters (MonoReflectionMethod *method, MonoArray *types) MONO_INTERNAL;
void
mono_reflection_generic_class_initialize (MonoReflectionGenericClass *type, MonoArray *fields) MONO_INTERNAL;
MonoReflectionEvent *
mono_reflection_event_builder_get_event_info (MonoReflectionTypeBuilder *tb, MonoReflectionEventBuilder *eb) MONO_INTERNAL;

MonoArray  *mono_reflection_sighelper_get_signature_local (MonoReflectionSigHelper *sig) MONO_INTERNAL;

MonoArray  *mono_reflection_sighelper_get_signature_field (MonoReflectionSigHelper *sig) MONO_INTERNAL;

MonoReflectionMarshalAsAttribute* mono_reflection_marshal_as_attribute_from_marshal_spec (MonoDomain *domain, MonoClass *klass, MonoMarshalSpec *spec) MONO_INTERNAL;

gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context) MONO_INTERNAL;

gboolean
mono_reflection_call_is_assignable_to (MonoClass *klass, MonoClass *oklass) MONO_INTERNAL;

gboolean
mono_reflection_is_valid_dynamic_token (MonoDynamicImage *image, guint32 token) MONO_INTERNAL;

void
mono_reflection_resolve_custom_attribute_data (MonoReflectionMethod *method, MonoReflectionAssembly *assembly, gpointer data, guint32 data_length, MonoArray **ctor_args, MonoArray ** named_args) MONO_INTERNAL;

MonoType*
mono_reflection_type_get_handle (MonoReflectionType *ref) MONO_INTERNAL;

void
mono_reflection_free_dynamic_generic_class (MonoGenericClass *gclass) MONO_INTERNAL;

void
mono_image_build_metadata (MonoReflectionModuleBuilder *module) MONO_INTERNAL;

int
mono_get_constant_value_from_blob (MonoDomain* domain, MonoTypeEnum type, const char *blob, void *value) MONO_INTERNAL;

void
mono_release_type_locks (MonoInternalThread *thread) MONO_INTERNAL;

char *
mono_string_to_utf8_mp	(MonoMemPool *mp, MonoString *s, MonoError *error) MONO_INTERNAL;

char *
mono_string_to_utf8_image (MonoImage *image, MonoString *s, MonoError *error) MONO_INTERNAL;


MonoArray*
mono_array_clone_in_domain (MonoDomain *domain, MonoArray *array) MONO_INTERNAL;

void
mono_array_full_copy (MonoArray *src, MonoArray *dest) MONO_INTERNAL;

gboolean
mono_array_calc_byte_len (MonoClass *class, uintptr_t len, uintptr_t *res) MONO_INTERNAL;

#ifndef DISABLE_REMOTING
MonoObject *
mono_remoting_invoke	    (MonoObject *real_proxy, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args) MONO_INTERNAL;

gpointer
mono_remote_class_vtable (MonoDomain *domain, MonoRemoteClass *remote_class, MonoRealProxy *real_proxy) MONO_INTERNAL;

void
mono_upgrade_remote_class (MonoDomain *domain, MonoObject *tproxy, MonoClass *klass) MONO_INTERNAL;
#endif

gpointer
mono_create_ftnptr (MonoDomain *domain, gpointer addr) MONO_INTERNAL;

gpointer
mono_get_addr_from_ftnptr (gpointer descr) MONO_INTERNAL;

void
mono_nullable_init (guint8 *buf, MonoObject *value, MonoClass *klass) MONO_INTERNAL;

MonoObject*
mono_nullable_box (guint8 *buf, MonoClass *klass) MONO_INTERNAL;

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

typedef gpointer (*MonoImtThunkBuilder) (MonoVTable *vtable, MonoDomain *domain,
		MonoIMTCheckItem **imt_entries, int count, gpointer fail_trunk);

void
mono_install_imt_thunk_builder (MonoImtThunkBuilder func) MONO_INTERNAL;

void
mono_vtable_build_imt_slot (MonoVTable* vtable, int imt_slot) MONO_INTERNAL;

guint32
mono_method_get_imt_slot (MonoMethod *method) MONO_INTERNAL;

void
mono_method_add_generic_virtual_invocation (MonoDomain *domain, MonoVTable *vtable,
											gpointer *vtable_slot,
											MonoMethod *method, gpointer code) MONO_INTERNAL;

gpointer
mono_method_alloc_generic_virtual_thunk (MonoDomain *domain, int size) MONO_INTERNAL;

typedef enum {
	MONO_UNHANDLED_POLICY_LEGACY,
	MONO_UNHANDLED_POLICY_CURRENT
} MonoRuntimeUnhandledExceptionPolicy;

MonoRuntimeUnhandledExceptionPolicy
mono_runtime_unhandled_exception_policy_get (void) MONO_INTERNAL;
void
mono_runtime_unhandled_exception_policy_set (MonoRuntimeUnhandledExceptionPolicy policy) MONO_INTERNAL;

MonoVTable *
mono_class_try_get_vtable (MonoDomain *domain, MonoClass *class) MONO_INTERNAL;

MonoException *
mono_runtime_class_init_full (MonoVTable *vtable, gboolean raise_exception) MONO_INTERNAL;

void
mono_method_clear_object (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;

void
mono_class_compute_gc_descriptor (MonoClass *class) MONO_INTERNAL;

gsize*
mono_class_compute_bitmap (MonoClass *class, gsize *bitmap, int size, int offset, int *max_set, gboolean static_fields) MONO_INTERNAL;

MonoObject*
mono_object_xdomain_representation (MonoObject *obj, MonoDomain *target_domain, MonoObject **exc) MONO_INTERNAL;

gboolean
mono_class_is_reflection_method_or_constructor (MonoClass *class) MONO_INTERNAL;

MonoObject *
mono_get_object_from_blob (MonoDomain *domain, MonoType *type, const char *blob) MONO_INTERNAL;

gpointer
mono_class_get_ref_info (MonoClass *klass) MONO_INTERNAL;

void
mono_class_set_ref_info (MonoClass *klass, gpointer obj) MONO_INTERNAL;

void
mono_class_free_ref_info (MonoClass *klass) MONO_INTERNAL;

MonoObject *
mono_object_new_pinned (MonoDomain *domain, MonoClass *klass) MONO_INTERNAL;

void
mono_field_static_get_value_for_thread (MonoInternalThread *thread, MonoVTable *vt, MonoClassField *field, void *value) MONO_INTERNAL;

/* exported, used by the debugger */
MONO_API void *
mono_vtable_get_static_field_data (MonoVTable *vt);

char *
mono_string_to_utf8_ignore (MonoString *s) MONO_INTERNAL;

char *
mono_string_to_utf8_image_ignore (MonoImage *image, MonoString *s) MONO_INTERNAL;

char *
mono_string_to_utf8_mp_ignore (MonoMemPool *mp, MonoString *s) MONO_INTERNAL;

gboolean
mono_monitor_is_il_fastpath_wrapper (MonoMethod *method) MONO_INTERNAL;

char *
mono_exception_get_native_backtrace (MonoException *exc) MONO_INTERNAL;

MonoString *
ves_icall_Mono_Runtime_GetNativeStackTrace (MonoException *exc) MONO_INTERNAL;

char *
mono_exception_get_managed_backtrace (MonoException *exc) MONO_INTERNAL;

#endif /* __MONO_OBJECT_INTERNALS_H__ */


