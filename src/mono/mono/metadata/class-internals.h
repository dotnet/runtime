#ifndef __MONO_METADATA_CLASS_INTERBALS_H__
#define __MONO_METADATA_CLASS_INTERBALS_H__

#include <mono/metadata/class.h>
#include <mono/io-layer/io-layer.h>

#define MONO_CLASS_IS_ARRAY(c) ((c)->rank)

#define MONO_DEFAULT_SUPERTABLE_SIZE 6

extern gboolean mono_print_vtable;

typedef void     (*MonoStackWalkImpl) (MonoStackWalk func, gboolean do_il_offset, gpointer user_data);

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
	MONO_WRAPPER_XDOMAIN_INVOKE,
	MONO_WRAPPER_XDOMAIN_DISPATCH,
	MONO_WRAPPER_LDFLD,
	MONO_WRAPPER_STFLD,
	MONO_WRAPPER_SYNCHRONIZED,
	MONO_WRAPPER_DYNAMIC_METHOD,
	MONO_WRAPPER_ISINST,
	MONO_WRAPPER_CASTCLASS,
	MONO_WRAPPER_PROXY_ISINST,
	MONO_WRAPPER_STELEMREF,
	MONO_WRAPPER_UNKNOWN
} MonoWrapperType;

typedef enum {
	MONO_REMOTING_TARGET_UNKNOWN,
	MONO_REMOTING_TARGET_APPDOMAIN
} MonoRemotingTarget;

struct _MonoMethod {
	guint16 flags;  /* method flags */
	guint16 iflags; /* method implementation flags */
	guint32 token;
	MonoClass *klass;
	MonoMethodSignature *signature;
	gpointer info; /* runtime info */
	/* name is useful mostly for debugging */
	const char *name;
	/* this is used by the inlining algorithm */
	unsigned int inline_info:1;
	unsigned int uses_this:1;
	unsigned int wrapper_type:5;
	unsigned int string_ctor:1;
	unsigned int save_lmf:1;
	unsigned int dynamic:1; /* created & destroyed during runtime */
	gint slot : 22;
};

struct _MonoMethodNormal {
	MonoMethod method;
	MonoGenericContainer *generic_container;
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
	gpointer addr;
	/* add marshal info */
	guint16 piflags;  /* pinvoke flags */
	guint16 implmap_idx;  /* index into IMPLMAP */
};

typedef struct {
	MonoType *generic_type;
	gpointer reflection_info;
} MonoInflatedField;

/*
 * MonoClassField is just a runtime representation of the metadata for
 * field, it doesn't contain the data directly.  Static fields are
 * stored in MonoVTable->data.  Instance fields are allocated in the
 * objects after the object header.
 */
struct _MonoClassField {
	/* Type of the field */
	MonoType        *type;

	/* If this is an instantiated generic type, this is the
	 * "original" type, ie. the MONO_TYPE_VAR or MONO_TYPE_GENERICINST
	 * it was instantiated from.
	 */
	MonoInflatedField  *generic_info;

	/*
	 * Offset where this field is stored; if it is an instance
	 * field, it's the offset from the start of the object, if
	 * it's static, it's from the start of the memory chunk
	 * allocated for statics for the class.
	 */
	int              offset;

	const char      *name;

	/*
	 * If the field is constant, pointer to the metadata constant
	 * value.
	 * If the field has an RVA flag, pointer to the data.
	 * Else, invalid.
	 */
	const char      *data;

	/* Type where the field was defined */
	MonoClass       *parent;

	/*
	 * If the field is constant, the type of the constant.
	 */
	MonoTypeEnum     def_type;
};

/* a field is ignored if it's named "_Deleted" and it has the specialname and rtspecialname flags set */
#define mono_field_is_deleted(field) ((field)->name[0] == '_' && ((field)->type->attrs & 0x600) && (strcmp ((field)->name, "_Deleted") == 0))

typedef struct {
	MonoClassField *field;
	guint32 offset;
	MonoMarshalSpec *mspec;
} MonoMarshalField;

typedef struct {
	guint32 native_size;
	guint32 num_fields;
	MonoMarshalField fields [MONO_ZERO_LEN_ARRAY];
} MonoMarshalType;

struct _MonoProperty {
	MonoClass *parent;
	const char *name;
	MonoMethod *get;
	MonoMethod *set;
	guint32 attrs;
};

struct _MonoEvent {
	MonoClass *parent;
	const char *name;
	MonoMethod *add;
	MonoMethod *remove;
	MonoMethod *raise;
	MonoMethod **other;
	guint32 attrs;
};

struct _MonoClass {
	MonoImage *image;

	/* The underlying type of the enum */
	MonoType *enum_basetype;
	/* element class for arrays and enum */
	MonoClass *element_class; 
	/* used for subtype checks */
	MonoClass *cast_class; 
	/* array dimension */
	guint32    rank;          

	guint inited          : 1;
	/* We use init_pending to detect cyclic calls to mono_class_init */
	guint init_pending    : 1;

	/* A class contains static and non static data. Static data can be
	 * of the same type as the class itselfs, but it does not influence
	 * the instance size of the class. To avoid cyclic calls to 
	 * mono_class_init (from mono_class_instance_size ()) we first 
	 * initialise all non static fields. After that we set size_inited 
	 * to 1, because we know the instance size now. After that we 
	 * initialise all static fields.
	 */
	guint size_inited     : 1;
	guint valuetype       : 1; /* derives from System.ValueType */
	guint enumtype        : 1; /* derives from System.Enum */
	guint blittable       : 1; /* class is blittable */
	guint unicode         : 1; /* class uses unicode char when marshalled */
	guint wastypebuilder  : 1; /* class was created at runtime from a TypeBuilder */
	/* next byte */
	guint min_align       : 4;
	guint packing_size    : 4;
	/* next byte */
	guint ghcimpl         : 1; /* class has its own GetHashCode impl */ 
	guint has_finalize    : 1; /* class has its own Finalize impl */ 
	guint marshalbyref    : 1; /* class is a MarshalByRefObject */
	guint contextbound    : 1; /* class is a ContextBoundObject */
	guint delegate        : 1; /* class is a Delegate */
	guint gc_descr_inited : 1; /* gc_descr is initialized */
	guint dummy           : 1; /* temporary hack */
	guint32 declsec_flags;     /* declarative security attributes flags */

	MonoClass  *parent;
	MonoClass  *nested_in;
	GList      *nested_classes;

	guint32    type_token;
	const char *name;
	const char *name_space;
	
	guint       interface_count;
	guint       interface_id;        /* unique inderface id (for interfaces) */
	guint       max_interface_id;
        gint       *interface_offsets;   
	MonoClass **interfaces;

	/* for fast subtype checks */
	guint       idepth;
	MonoClass **supertypes;

	/*
	 * Computed object instance size, total.
	 */
	int        instance_size;
	int        class_size;
	int        vtable_size; /* number of slots */

	/*
	 * From the TypeDef table
	 */
	guint32    flags;
	struct {
		guint32 first, last;
		int count;
	} field, method, property, event;

	/* loaded on demand */
	MonoMarshalType *marshal_info;

	/*
	 * Field information: Type and location from object base
	 */
	MonoClassField *fields;

	MonoProperty *properties;
	
	MonoEvent *events;

	MonoMethod **methods;

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType byval_arg;

	MonoGenericClass *generic_class;
	MonoGenericContainer *generic_container;

	void *reflection_info;

	void *gc_descr;
	guint64 gc_bitmap;

	MonoMethod *ptr_to_str;
	MonoMethod *str_to_ptr;

	MonoVTable *cached_vtable;
        MonoMethod **vtable;	
};

struct MonoVTable {
	MonoClass  *klass;
    /*
	 * According to comments in gc_gcj.h, this should be the second word in
	 * the vtable.
	 */
	void *gc_descr; 	
	MonoDomain *domain;  /* each object/vtable belongs to exactly one domain */
	guint32       max_interface_id;
        gpointer   *interface_offsets;   
        gpointer    data; /* to store static class data */
        gpointer    type; /* System.Type type for klass */
	guint remote          : 1; /* class is remotely activated */
	guint initialized     : 1; /* cctor has been run */
	/* do not add any fields after vtable, the structure is dynamically extended */
        gpointer    vtable [MONO_ZERO_LEN_ARRAY];	
};

/*
 * Generic instantiation data type encoding.
 */
struct _MonoGenericInst {
	int id;
	int type_argc  : 23;
	int is_open    : 1;
	MonoType **type_argv;
};

struct _MonoGenericClass {
	MonoGenericInst *inst;
	MonoGenericContainer *container;
	MonoGenericContext *context;
	MonoClass *klass;
	MonoType *parent;
	int count_ifaces;
	MonoType **ifaces;
	MonoType *generic_type;
	MonoDynamicGenericClass *dynamic_info;
	guint initialized   : 1;
	guint init_pending  : 1;
	guint is_dynamic    : 1;
};

struct _MonoDynamicGenericClass {
	int count_methods;
	MonoMethod **methods;
	int count_ctors;
	MonoMethod **ctors;
	int count_fields;
	MonoClassField *fields;
	int count_properties;
	MonoProperty *properties;
	int count_events;
	MonoEvent *events;
};

struct _MonoGenericMethod {
	MonoGenericInst *inst;
	MonoGenericContainer *container;
	gpointer reflection_info;
};

struct _MonoGenericContext {
	MonoGenericContainer *container;
	MonoGenericClass *gclass;
	MonoGenericMethod *gmethod;
};

struct _MonoGenericContainer {
	MonoGenericContext context;
	MonoGenericContainer *parent;
	GHashTable *method_hash;
	MonoClass *klass;
	int type_argc    : 6;
	int is_method    : 1;
	int is_signature : 1;
	MonoGenericParam *type_params;
};

struct _MonoGenericParam {
	MonoGenericContainer *owner;
	MonoClass *pklass;
	MonoMethod *method;
	const char *name;
	guint16 flags;
	guint16 num;
	MonoClass** constraints; /* NULL means end of list */
};

typedef struct {
	const char *name;
	gconstpointer func;
	gconstpointer wrapper;
	MonoMethodSignature *sig;
} MonoJitICallInfo;

#define mono_class_has_parent(klass,parent) (((klass)->idepth >= (parent)->idepth) && ((klass)->supertypes [(parent)->idepth - 1] == (parent)))

typedef struct {
	gulong new_object_count;
	gulong initialized_class_count;
	gulong used_class_count;
	gulong class_vtable_size;
	gulong class_static_data_size;
	gulong generic_instance_count;
	gulong inflated_method_count;
	gulong inflated_type_count;
	gulong generics_metadata_size;
	gboolean enabled;
} MonoStats;

extern MonoStats mono_stats;

typedef gpointer (*MonoTrampoline)       (MonoMethod *method);
typedef gpointer (*MonoRemotingTrampoline)       (MonoMethod *method, MonoRemotingTarget target);

typedef gpointer (*MonoLookupDynamicToken) (MonoImage *image, guint32 token);

void
mono_classes_init (void);

void
mono_class_layout_fields   (MonoClass *klass);

void
mono_class_setup_vtable    (MonoClass *klass, MonoMethod **overrides, int onum);

void
mono_class_setup_mono_type (MonoClass *klass);

void
mono_class_setup_parent    (MonoClass *klass, MonoClass *parent);

void
mono_class_setup_supertypes (MonoClass *klass);

GPtrArray*
mono_class_get_implemented_interfaces (MonoClass *klass);

gboolean
mono_class_is_open_constructed_type (MonoType *t);

MonoMethod**
mono_class_get_overrides   (MonoImage *image, guint32 type_token, gint32 *num_overrides);

gboolean
mono_class_needs_cctor_run (MonoClass *klass, MonoMethod *caller);

void
mono_install_trampoline (MonoTrampoline func);

void
mono_install_remoting_trampoline (MonoRemotingTrampoline func);

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token);

void
mono_install_lookup_dynamic_token (MonoLookupDynamicToken func);

void
mono_class_create_generic (MonoGenericClass *gclass);

void
mono_class_create_generic_2 (MonoGenericClass *gclass);

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

MonoGenericContainer *mono_metadata_load_generic_params (MonoImage *image, guint32 token);

MonoMethodSignature*
mono_create_icall_signature (const char *sigstr);

MonoJitICallInfo *
mono_register_jit_icall (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save);

void
mono_register_jit_icall_wrapper (MonoJitICallInfo *info, gconstpointer wrapper);

MonoJitICallInfo *
mono_find_jit_icall_by_name (const char *name);

MonoJitICallInfo *
mono_find_jit_icall_by_addr (gconstpointer addr);

#endif /* __MONO_METADATA_CLASS_INTERBALS_H__ */

