#ifndef __MONO_METADATA_CLASS_INTERBALS_H__
#define __MONO_METADATA_CLASS_INTERBALS_H__

#include <mono/metadata/class.h>
#include <mono/io-layer/io-layer.h>

typedef void     (*MonoStackWalkImpl) (MonoStackWalk func, gpointer user_data);

typedef struct _MonoMethodNormal MonoMethodNormal;
typedef struct _MonoMethodWrapper MonoMethodWrapper;
typedef struct _MonoMethodInflated MonoMethodInflated;
typedef struct _MonoMethodPInvoke MonoMethodPInvoke;

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

struct _MonoMethod {
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
};

struct _MonoMethodNormal {
	MonoMethod method;
	MonoMethodHeader *header;
};

struct _MonoMethodWrapper {
	MonoMethodNormal method;
	GList *data;
};

struct _MonoMethodInflated {
	MonoMethodNormal nmethod;
	MonoGenericContext *context;
	MonoMethod *declaring;
};

struct _MonoMethodPInvoke {
	MonoMethod method;
	void  (*code) (void);
	/* add marshal info */
	guint16 piflags;  /* pinvoke flags */
	guint16 implmap_idx;  /* index into IMPLMAP */
};

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

void
mono_loader_init           (void);

void
mono_loader_lock           (void);

void
mono_loader_unlock           (void);

void 
mono_init_icall            (void);

gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id);

void
mono_install_stack_walk (MonoStackWalkImpl func);

MonoGenericParam *mono_metadata_load_generic_params (MonoImage *image, guint32 token, guint32 *num);

#endif /* __MONO_METADATA_CLASS_INTERBALS_H__ */

