#ifndef __MONO_OBJECT_INTERNALS_H__
#define __MONO_OBJECT_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mempool.h>
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"

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
     if (!(expr))							  \
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
     if (arg == NULL)							  \
       {								  \
		MonoException *ex;					  \
		if (arg) {} /* check if the name exists */		  \
		ex = mono_get_exception_argument_null (#arg);		  \
		mono_raise_exception (ex);				  \
       };				}G_STMT_END


/* 16 == default capacity */
#define mono_stringbuilder_capacity(sb) ((sb)->str ? ((sb)->str->length) : 16)

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

struct _MonoReflectionType {
	MonoObject object;
	MonoType  *type;
};

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

struct _MonoThread {
	MonoObject  obj;
	int         lock_thread_id; /* to be used as the pre-shifted thread id in thin locks */
	HANDLE	    handle;
	MonoArray  *cached_culture_info;
	gpointer    unused1;
	MonoBoolean threadpool_thread;
	gunichar2  *name;
	guint32	    name_len;
	guint32	    state;
	MonoException *abort_exc;
	MonoObject *abort_state;
	guint64 tid;	/* This is accessed as a gsize in the code (so it can hold a 64bit pointer on systems that need it), but needs to reserve 64 bits of space on all machines as it corresponds to a field in managed code */
	HANDLE	    start_notify;
	gpointer stack_ptr;
	gpointer *static_data;
	gpointer jit_data;
	gpointer lock_data;
	MonoAppContext *current_appcontext;
	int stack_size;
	MonoObject *start_obj;
	GSList *appdomain_refs;
	MonoBoolean interruption_requested;
	gpointer suspend_event;
	gpointer suspended_event;
	gpointer resume_event;
	CRITICAL_SECTION *synch_cs;
	guint8* serialized_culture_info;
	guint32 serialized_culture_info_len;
	guint8* serialized_ui_culture_info;
	guint32 serialized_ui_culture_info_len;
	MonoObject *execution_context;
	/* 
	 * These fields are used to avoid having to increment corlib versions
	 * when a new field is added to the unmanaged MonoThread structure.
	 */
	MonoBoolean thread_dump_requested;
	gpointer end_stack; /* This is only used when running in the debugger. */
	MonoBoolean thread_interrupt_requested;
	guint8	apartment_state;
	gssize small_id;    /* A small, unique id, used for the hazard
			       pointer table.  Should be changed to a
			       guint32 at the next corlib version bump. */
	MonoThreadManageCallback manage_callback;
	gpointer unused7;
};

typedef struct {
	MonoString *name;
	MonoReflectionType *type;
	MonoObject *value;
} MonoSerializationEntry;

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
	MonoString *FullDateTimePattern;
	MonoString *RFC1123Pattern;
	MonoString *SortableDateTimePattern;
	MonoString *UniversalSortableDateTimePattern;
	guint32 FirstDayOfWeek;
	MonoObject *Calendar;
	guint32 CalendarWeekRule;
	MonoArray *AbbreviatedDayNames;
	MonoArray *DayNames;
	MonoArray *MonthNames;
	MonoArray *AbbreviatedMonthNames;
	MonoArray *ShortDatePatterns;
	MonoArray *LongDatePatterns;
	MonoArray *ShortTimePatterns;
	MonoArray *LongTimePatterns;
	MonoArray *MonthDayPatterns;
	MonoArray *YearMonthPatterns;
	MonoArray *shortDayNames;
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
	gint32 specific_lcid;
	gint32 datetime_index;
	gint32 number_index;
	MonoBoolean use_user_override;
	MonoNumberFormatInfo *number_format;
	MonoDateTimeFormatInfo *datetime_format;
	MonoObject *textinfo;
	MonoString *name;
	MonoString *displayname;
	MonoString *englishname;
	MonoString *nativename;
	MonoString *iso3lang;
	MonoString *iso2lang;
	MonoString *icu_name;
	MonoString *win3lang;
	MonoString *territory;
	MonoCompareInfo *compareinfo;
	const gint32 *calendar_data;
	const void* text_info_data;
} MonoCultureInfo;

typedef struct {
	MonoObject obj;
	gint32 region_id;
	MonoString *iso2name;
	MonoString *iso3name;
	MonoString *win3name;
	MonoString *english_name;
	MonoString *currency_symbol;
	MonoString *iso_currency_symbol;
	MonoString *currency_english_name;
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
	guint16 intType;
} MonoInterfaceTypeAttribute;

/* used to free a dynamic method */
typedef void        (*MonoFreeMethodFunc)	 (MonoDomain *domain, MonoMethod *method);

/* Used to initialize the method pointers inside vtables */
typedef gboolean    (*MonoInitVTableFunc)    (MonoVTable *vtable);

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
mono_remoting_invoke	    (MonoObject *real_proxy, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args) MONO_INTERNAL;

MonoObject *
mono_message_invoke	    (MonoObject *target, MonoMethodMessage *msg, 
			     MonoObject **exc, MonoArray **out_args) MONO_INTERNAL;

MonoMethodMessage *
mono_method_call_message_new (MonoMethod *method, gpointer *params, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state) MONO_INTERNAL;

void
mono_method_return_message_restore (MonoMethod *method, gpointer *params, MonoArray *out_args) MONO_INTERNAL;

void
mono_delegate_ctor	    (MonoObject *this_obj, MonoObject *target, gpointer addr) MONO_INTERNAL;

void*
mono_class_get_allocation_ftn (MonoVTable *vtable, gboolean for_box, gboolean *pass_size_in_words) MONO_INTERNAL;

void
mono_runtime_free_method    (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;

/* runtime initialization functions */
typedef void (*MonoExceptionFunc) (MonoException *ex);

void
mono_install_handler	    (MonoExceptionFunc func) MONO_INTERNAL;

void	    
mono_install_runtime_invoke (MonoInvokeFunc func) MONO_INTERNAL;

void	    
mono_install_compile_method (MonoCompileFunc func) MONO_INTERNAL;

void
mono_install_free_method    (MonoFreeMethodFunc func) MONO_INTERNAL;

void
mono_type_initialization_init (void) MONO_INTERNAL;

void
mono_type_initialization_cleanup (void) MONO_INTERNAL;

guint32
mono_thread_get_tls_key    (void) MONO_INTERNAL;

gint32
mono_thread_get_tls_offset (void) MONO_INTERNAL;

guint32
mono_domain_get_tls_key    (void) MONO_INTERNAL;

gint32
mono_domain_get_tls_offset (void) MONO_INTERNAL;

/* Reflection and Reflection.Emit support */

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
	gpointer method_ptr;
	gpointer invoke_impl;
	MonoObject *target;
	MonoMethod *method;
	MonoObject *target_type;
	MonoString *method_name;
	gpointer delegate_trampoline;
	MonoReflectionMethod *method_info;
	MonoReflectionMethod *original_method_info;
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

struct _MonoReflectionEvent {
	MonoObject object;
	MonoClass *klass;
	MonoEvent *event;
};

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
	MonoReflectionType *extype;
	gint32 type;
	gint32 start;
	gint32 len;
	gint32 filter_offset;
} MonoILExceptionBlock;

typedef struct {
	MonoObject object;
	MonoReflectionType *catch_type;
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
	MonoReflectionType *type;
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
	MonoReflectionType *marshaltyperef;
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
	MonoReflectionType *rtype;
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
	MonoReflectionMethod *override_method;
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
} MonoReflectionAssemblyBuilder;

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoReflectionType *type;
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
	MonoReflectionType *type;
	MonoArray *parameters;
	MonoArray *cattrs;
	MonoObject *def_value;
	MonoReflectionMethodBuilder *set_method;
	MonoReflectionMethodBuilder *get_method;
	gint32 table_idx;
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
	MonoReflectionType *parent;
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
	MonoReflectionTypeBuilder *generic_type;
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
	MonoReflectionFieldBuilder *fb;
} MonoReflectionFieldOnTypeBuilderInst;

typedef struct {
	MonoObject object;
	MonoReflectionGenericClass *inst;
	MonoReflectionCtorBuilder *cb;
} MonoReflectionCtorOnTypeBuilderInst;

typedef struct {
	MonoObject object;
	MonoReflectionGenericClass *inst;
	MonoReflectionMethodBuilder *mb;
} MonoReflectionMethodOnTypeBuilderInst;

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

void          mono_image_create_pefile (MonoReflectionModuleBuilder *module, HANDLE file) MONO_INTERNAL;
void          mono_image_basic_init (MonoReflectionAssemblyBuilder *assembly) MONO_INTERNAL;
MonoReflectionModule * mono_image_load_module_dynamic (MonoReflectionAssemblyBuilder *assembly, MonoString *file_name) MONO_INTERNAL;
guint32       mono_image_insert_string (MonoReflectionModuleBuilder *module, MonoString *str) MONO_INTERNAL;
guint32       mono_image_create_token  (MonoDynamicImage *assembly, MonoObject *obj, gboolean create_methodspec, gboolean register_token) MONO_INTERNAL;
guint32       mono_image_create_method_token (MonoDynamicImage *assembly, MonoObject *obj, MonoArray *opt_param_types) MONO_INTERNAL;
void          mono_image_module_basic_init (MonoReflectionModuleBuilder *module) MONO_INTERNAL;
void          mono_image_register_token (MonoDynamicImage *assembly, guint32 token, MonoObject *obj) MONO_INTERNAL;
void          mono_dynamic_image_free (MonoDynamicImage *image) MONO_INTERNAL;

void        mono_reflection_setup_internal_class  (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_create_internal_class (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_setup_generic_class   (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_create_generic_class  (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

MonoReflectionType* mono_reflection_create_runtime_class  (MonoReflectionTypeBuilder *tb) MONO_INTERNAL;

void        mono_reflection_get_dynamic_overrides (MonoClass *klass, MonoMethod ***overrides, int *num_overrides) MONO_INTERNAL;

void mono_reflection_create_dynamic_method (MonoReflectionDynamicMethod *m) MONO_INTERNAL;
void mono_reflection_destroy_dynamic_method (MonoReflectionDynamicMethod *mb) MONO_INTERNAL;

void        mono_reflection_initialize_generic_parameter (MonoReflectionGenericParam *gparam) MONO_INTERNAL;

MonoClass*
mono_class_bind_generic_parameters (MonoClass *klass, int type_argc, MonoType **types, gboolean is_dynamic) MONO_INTERNAL;
MonoType*
mono_reflection_bind_generic_parameters (MonoReflectionType *type, int type_argc, MonoType **types) MONO_INTERNAL;
MonoReflectionMethod*
mono_reflection_bind_generic_method_parameters (MonoReflectionMethod *method, MonoArray *types) MONO_INTERNAL;
void
mono_reflection_generic_class_initialize (MonoReflectionGenericClass *type, MonoArray *methods, MonoArray *ctors, MonoArray *fields, MonoArray *properties, MonoArray *events) MONO_INTERNAL;
MonoReflectionEvent *
mono_reflection_event_builder_get_event_info (MonoReflectionTypeBuilder *tb, MonoReflectionEventBuilder *eb) MONO_INTERNAL;

MonoArray  *mono_reflection_sighelper_get_signature_local (MonoReflectionSigHelper *sig) MONO_INTERNAL;

MonoArray  *mono_reflection_sighelper_get_signature_field (MonoReflectionSigHelper *sig) MONO_INTERNAL;

MonoReflectionMarshal* mono_reflection_marshal_from_marshal_spec (MonoDomain *domain, MonoClass *klass, MonoMarshalSpec *spec) MONO_INTERNAL;

gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token, gboolean valid_token, MonoClass **handle_class, MonoGenericContext *context) MONO_INTERNAL;

gboolean
mono_reflection_call_is_assignable_to (MonoClass *klass, MonoClass *oklass) MONO_INTERNAL;

gboolean
mono_reflection_is_valid_dynamic_token (MonoDynamicImage *image, guint32 token) MONO_INTERNAL;

void
mono_image_build_metadata (MonoReflectionModuleBuilder *module) MONO_INTERNAL;

int
mono_get_constant_value_from_blob (MonoDomain* domain, MonoTypeEnum type, const char *blob, void *value) MONO_INTERNAL;

void
mono_release_type_locks (MonoThread *thread) MONO_INTERNAL;

char *
mono_string_to_utf8_mp	(MonoMemPool *mp, MonoString *s) MONO_INTERNAL;

MonoArray*
mono_array_clone_in_domain (MonoDomain *domain, MonoArray *array) MONO_INTERNAL;

void
mono_array_full_copy (MonoArray *src, MonoArray *dest) MONO_INTERNAL;

gpointer
mono_remote_class_vtable (MonoDomain *domain, MonoRemoteClass *remote_class, MonoRealProxy *real_proxy) MONO_INTERNAL;

void
mono_upgrade_remote_class (MonoDomain *domain, MonoObject *tproxy, MonoClass *klass) MONO_INTERNAL;

gpointer
mono_create_ftnptr (MonoDomain *domain, gpointer addr) MONO_INTERNAL;

gpointer
mono_get_addr_from_ftnptr (gpointer descr) MONO_INTERNAL;

void
mono_nullable_init (guint8 *buf, MonoObject *value, MonoClass *klass) MONO_INTERNAL;

MonoObject*
mono_nullable_box (guint8 *buf, MonoClass *klass) MONO_INTERNAL;

#define MONO_IMT_SIZE 19

typedef struct _MonoImtBuilderEntry {
	MonoMethod *method;
	struct _MonoImtBuilderEntry *next;
	int vtable_slot;
	int children;
} MonoImtBuilderEntry;

typedef struct _MonoIMTCheckItem MonoIMTCheckItem;

struct _MonoIMTCheckItem {
	MonoMethod       *method;
	int               check_target_idx;
	int               vtable_slot;
	guint8           *jmp_code;
	guint8           *code_target;
	guint8            is_equals;
	guint8            compare_done;
	guint8            chunk_size;
	guint8            short_branch;
};

typedef gpointer (*MonoImtThunkBuilder) (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count);

void
mono_install_imt_thunk_builder (MonoImtThunkBuilder func) MONO_INTERNAL;

void
mono_install_imt_trampoline (gpointer tramp) MONO_INTERNAL;

void
mono_install_vtable_trampoline (gpointer tramp) MONO_INTERNAL;

void
mono_vtable_build_imt_slot (MonoVTable* vtable, int imt_slot) MONO_INTERNAL;

guint32
mono_method_get_imt_slot (MonoMethod *method) MONO_INTERNAL;

typedef enum {
	MONO_UNHANLED_POLICY_LEGACY,
	MONO_UNHANLED_POLICY_CURRENT
} MonoRuntimeUnhandledExceptionPolicy;

MonoRuntimeUnhandledExceptionPolicy
mono_runtime_unhandled_exception_policy_get (void) MONO_INTERNAL;
void
mono_runtime_unhandled_exception_policy_set (MonoRuntimeUnhandledExceptionPolicy policy) MONO_INTERNAL;

#endif /* __MONO_OBJECT_INTERNALS_H__ */

