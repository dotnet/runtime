#ifndef _MONO_METADATA_LOADER_H_
#define _MONO_METADATA_LOADER_H_ 1

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/io-layer/io-layer.h>

typedef enum {
	MONO_WRAPPER_NONE,
	MONO_WRAPPER_DELEGATE_INVOKE,
	MONO_WRAPPER_DELEGATE_BEGIN_INVOKE,
	MONO_WRAPPER_DELEGATE_END_INVOKE,
	MONO_WRAPPER_RUNTIME_INVOKE,
	MONO_WRAPPER_NATIVE_TO_MANAGED,
	MONO_WRAPPER_MANAGED_TO_NATIVE,
	MONO_WRAPPER_REMOTING_INVOKE,
	MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK,
	MONO_WRAPPER_LDFLD,
	MONO_WRAPPER_STFLD,
	MONO_WRAPPER_SYNCHRONIZED,
	MONO_WRAPPER_DYNAMIC_METHOD,
	MONO_WRAPPER_UNKNOWN
} MonoWrapperType;

typedef struct {
	guint16 flags;  /* method flags */
	guint16 iflags; /* method implementation flags */
	guint32 token;
	MonoClass *klass;
	MonoMethodSignature *signature;
	gpointer addr;
	gpointer info; /* runtime info */
	gpointer remoting_tramp; 
	gint slot;
	/* name is useful mostly for debugging */
	const char *name;
	/* this is used by the inlining algorithm */
	unsigned int inline_info:1;
	unsigned int uses_this:1;
	unsigned int wrapper_type:4;
	unsigned int string_ctor:1;
	unsigned int save_lmf:1;
	gint16 inline_count;
} MonoMethod;

typedef struct {
	MonoMethod method;
	MonoMethodHeader *header;
} MonoMethodNormal;

typedef struct {
	MonoMethodNormal method;
	GList *data;
} MonoMethodWrapper;

typedef struct {
	MonoMethodNormal nmethod;
	MonoGenericContext *context;
	MonoMethod *declaring;
} MonoMethodInflated;

typedef struct {
	MonoMethod method;
	void  (*code) (void);
	/* add marshal info */
	guint16 piflags;  /* pinvoke flags */
	guint16 implmap_idx;  /* index into IMPLMAP */
} MonoMethodPInvoke;

typedef struct {
	MonoImage *corlib;
	MonoClass *object_class;
	MonoClass *byte_class;
	MonoClass *void_class;
	MonoClass *boolean_class;
	MonoClass *sbyte_class;
	MonoClass *int16_class;
	MonoClass *uint16_class;
	MonoClass *int32_class;
	MonoClass *uint32_class;
	MonoClass *int_class;
	MonoClass *uint_class;
	MonoClass *int64_class;
	MonoClass *uint64_class;
	MonoClass *single_class;
	MonoClass *double_class;
	MonoClass *char_class;
	MonoClass *string_class;
	MonoClass *enum_class;
	MonoClass *array_class;
	MonoClass *delegate_class;
	MonoClass *multicastdelegate_class;
	MonoClass *asyncresult_class;
	MonoClass *waithandle_class;
	MonoClass *typehandle_class;
	MonoClass *fieldhandle_class;
	MonoClass *methodhandle_class;
	MonoClass *monotype_class;
	MonoClass *exception_class;
	MonoClass *threadabortexception_class;
	MonoClass *thread_class;
	MonoClass *transparent_proxy_class;
	MonoClass *real_proxy_class;
	MonoClass *mono_method_message_class;
	MonoClass *appdomain_class;
	MonoClass *field_info_class;
	MonoClass *method_info_class;
	MonoClass *stringbuilder_class;
	MonoClass *math_class;
	MonoClass *stack_frame_class;
	MonoClass *stack_trace_class;
	MonoClass *marshal_class;
	MonoClass *iserializeable_class;
	MonoClass *serializationinfo_class;
	MonoClass *streamingcontext_class;
	MonoClass *typed_reference_class;
	MonoClass *argumenthandle_class;
	MonoClass *marshalbyrefobject_class;
	MonoClass *monitor_class;
	MonoClass *iremotingtypeinfo_class;
} MonoDefaults;

extern MonoDefaults mono_defaults;

/*
 * This lock protects the hash tables inside MonoImage used by the metadata 
 * loading functions in class.c and loader.c.
 */
extern CRITICAL_SECTION loader_mutex;

#define mono_loader_lock() EnterCriticalSection (&loader_mutex);
#define mono_loader_unlock() LeaveCriticalSection (&loader_mutex);

typedef gboolean (*MonoStackWalk)     (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data);
typedef void     (*MonoStackWalkImpl) (MonoStackWalk func, gpointer user_data);

void
mono_loader_init           (void);

void 
mono_init_icall            (void);

MonoMethod *
mono_get_method            (MonoImage *image, guint32 token, MonoClass *klass);

void               
mono_free_method           (MonoMethod *method);

MonoMethodSignature* 
mono_method_get_signature  (MonoMethod *method, MonoImage *image, guint32 token);

MonoImage *
mono_load_image            (const char *fname, MonoImageOpenStatus *status);

void
mono_add_internal_call     (const char *name, gconstpointer method);

gpointer
mono_lookup_internal_call (MonoMethod *method);

int 
mono_dllmap_lookup (const char *dll, const char* func, const char **rdll, const char **rfunc);

void
mono_dllmap_insert (const char *dll, const char *func, const char *tdll, const char *tfunc);

gpointer
mono_lookup_pinvoke_call (MonoMethod *method, const char **exc_class, const char **exc_arg);

void
mono_method_get_param_names (MonoMethod *method, const char **names);

void
mono_method_get_marshal_info (MonoMethod *method, MonoMarshalSpec **mspecs);

gboolean
mono_method_has_marshal_info (MonoMethod *method);

gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id);

MonoMethod*
mono_method_get_last_managed  (void);

void
mono_stack_walk         (MonoStackWalk func, gpointer user_data);

void
mono_install_stack_walk (MonoStackWalkImpl func);

void
mono_loader_wine_init   (void);

MonoGenericParam *mono_metadata_load_generic_params (MonoImage *image, guint32 token, guint32 *num);

#endif
