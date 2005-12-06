#ifndef __MONO_METADATA_CLASS_INTERBALS_H__
#define __MONO_METADATA_CLASS_INTERBALS_H__

#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>

#define MONO_CLASS_IS_ARRAY(c) ((c)->rank)

#define MONO_DEFAULT_SUPERTABLE_SIZE 6

extern gboolean mono_print_vtable;

typedef void     (*MonoStackWalkImpl) (MonoStackWalk func, gboolean do_il_offset, gpointer user_data);

typedef struct _MonoMethodNormal MonoMethodNormal;
typedef struct _MonoMethodWrapper MonoMethodWrapper;
typedef struct _MonoMethodInflated MonoMethodInflated;
typedef struct _MonoMethodPInvoke MonoMethodPInvoke;

/*
 * remember to update wrapper_type_names if you change something here
 */
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
	MONO_WRAPPER_LDFLD_REMOTE,
	MONO_WRAPPER_STFLD_REMOTE,
	MONO_WRAPPER_SYNCHRONIZED,
	MONO_WRAPPER_DYNAMIC_METHOD,
	MONO_WRAPPER_ISINST,
	MONO_WRAPPER_CASTCLASS,
	MONO_WRAPPER_PROXY_ISINST,
	MONO_WRAPPER_STELEMREF,
	MONO_WRAPPER_UNBOX,
	MONO_WRAPPER_LDFLDA,
	MONO_WRAPPER_UNKNOWN
} MonoWrapperType;

typedef enum {
	MONO_TYPE_NAME_FORMAT_IL,
	MONO_TYPE_NAME_FORMAT_REFLECTION,
	MONO_TYPE_NAME_FORMAT_FULL_NAME,
	MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
} MonoTypeNameFormat;

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
	MonoGenericContainer *generic_container;
	/* name is useful mostly for debugging */
	const char *name;
	/* this is used by the inlining algorithm */
	unsigned int inline_info:1;
	unsigned int uses_this:1;
	unsigned int wrapper_type:5;
	unsigned int string_ctor:1;
	unsigned int save_lmf:1;
	unsigned int dynamic:1; /* created & destroyed during runtime */
	unsigned int is_inflated:1; /* whether we're a MonoMethodInflated */
	signed int slot : 21;
};

struct _MonoMethodNormal {
	MonoMethod method;
	MonoMethodHeader *header;
};

struct _MonoMethodWrapper {
	MonoMethodNormal method;
	void *method_data;
};

struct _MonoMethodPInvoke {
	MonoMethod method;
	gpointer addr;
	/* add marshal info */
	guint16 piflags;  /* pinvoke flags */
	guint16 implmap_idx;  /* index into IMPLMAP */
};

/*
 * Inflated generic method.
 */
struct _MonoMethodInflated {
	union {
		MonoMethod method;
		MonoMethodNormal normal;
		MonoMethodPInvoke pinvoke;
	} method;
	MonoGenericContext *context;	/* The current context. */
	MonoMethod *declaring;		/* the generic method definition. */
	/* This is a big performance optimization:
	 *
	 * mono_class_inflate_generic_method() just creates a copy of the method
	 * and computes its new context, but it doesn't actually inflate the
	 * method's signature and header.  Very often, we don't actually need them
	 * (for instance because the method is stored in a class'es vtable).
	 *
	 * If the `inflated' field in non-NULL, mono_get_inflated_method() already
	 * inflated the signature and header and stored it there.
	 */
	MonoMethodInflated *inflated;
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
#define mono_field_is_deleted(field) (((field)->type->attrs & (FIELD_ATTRIBUTE_SPECIAL_NAME | FIELD_ATTRIBUTE_RT_SPECIAL_NAME)) \
				      && (strcmp ((field)->name, "_Deleted") == 0))

typedef struct {
	MonoClassField *field;
	guint32 offset;
	MonoMarshalSpec *mspec;
} MonoMarshalField;

typedef struct {
	guint32 native_size;
	guint32 num_fields;
	MonoMethod *ptr_to_str;
	MonoMethod *str_to_ptr;
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

/* type of exception being "on hold" for later processing (see exception_type) */
enum {
	MONO_EXCEPTION_NONE = 0,
	MONO_EXCEPTION_SECURITY_LINKDEMAND = 1,
	MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND = 2
	/* add other exception type */
};

/* This struct collects the info needed for the runtime use of a class,
 * like the vtables for a domain, the GC descriptor, etc.
 */
typedef struct {
	guint16 max_domain;
	/* domain_vtables is indexed by the domain id and the size is max_domain + 1 */
	MonoVTable *domain_vtables [MONO_ZERO_LEN_ARRAY];
} MonoClassRuntimeInfo;

struct _MonoClass {
	MonoImage *image;

	/* The underlying type of the enum */
	MonoType *enum_basetype;
	/* element class for arrays and enum */
	MonoClass *element_class; 
	/* used for subtype checks */
	MonoClass *cast_class; 
	/* array dimension */
	guint8     rank;          

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
	guint has_cctor       : 1; /* class has a cctor */
	guint dummy           : 1; /* temporary hack */
	/* next byte */
	guint has_references  : 1; /* it has GC-tracked references in the instance */
	guint has_static_refs : 1; /* it has static fields that are GC-tracked */
	guint no_special_static_fields : 1; /* has no thread/context static fields */

	guint8     exception_type;	/* MONO_EXCEPTION_* */
	void*      exception_data;	/* Additional information about the exception */
	guint32    declsec_flags;	/* declarative security attributes flags */

	MonoClass  *parent;
	MonoClass  *nested_in;
	GList      *nested_classes;

	guint32    type_token;
	const char *name;
	const char *name_space;
	
	/* for fast subtype checks */
	MonoClass **supertypes;
	guint16     idepth;

	guint16     interface_count;
	guint16     interface_id;        /* unique inderface id (for interfaces) */
	guint16     max_interface_id;
        gint       *interface_offsets;   
	MonoClass **interfaces;

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
		guint32 first, count;
	} field, method, property, event;

	/* loaded on demand */
	MonoMarshalType *marshal_info;

	/*
	 * Field information: Type and location from object base
	 */
	MonoClassField *fields;

	/* Initialized by a call to mono_class_setup_properties () */
	MonoProperty *properties;

	/* Initialized by a call to mono_class_setup_events () */
	MonoEvent *events;

	MonoMethod **methods;

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType byval_arg;

	MonoGenericClass *generic_class;
	MonoGenericContainer *generic_container;

	void *reflection_info;

	void *gc_descr;

	MonoClassRuntimeInfo *runtime_info;

	/* Generic vtable. Initialized by a call to mono_class_setup_vtable () */
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
        gpointer   *interface_offsets;   
        gpointer    data; /* to store static class data */
        gpointer    type; /* System.Type type for klass */
	guint16     max_interface_id;
	guint8      rank;
	guint remote          : 1; /* class is remotely activated */
	guint initialized     : 1; /* cctor has been run */
	/* do not add any fields after vtable, the structure is dynamically extended */
        gpointer    vtable [MONO_ZERO_LEN_ARRAY];	
};

/*
 * Generic instantiation data type encoding.
 */

/*
 * A particular generic instantiation:
 *
 * All instantiations are cached and we don't distinguish between class and method
 * instantiations here.
 */
struct _MonoGenericInst {
	guint id;			/* unique ID for debugging */
	guint type_argc    : 22;	/* number of type arguments */
	guint is_open      :  1;	/* if this is an open type */
	guint is_reference :  1;	/* if this is a reference type */
	MonoType **type_argv;
};

/*
 * A particular instantiation of a generic type.
 */
struct _MonoGenericClass {
	MonoGenericInst *inst;		/* the instantiation */
	MonoClass *container_class;	/* the generic type definition */
	MonoGenericContext *context;	/* current context */
	guint is_dynamic  : 1;		/* We're a MonoDynamicGenericClass */
	guint is_inflated : 1;		/* We're a MonoInflatedGenericClass */
};

/*
 * Performance optimization:
 * We don't create the `MonoClass' for a `MonoGenericClass' until we really
 * need it.
 */
struct _MonoInflatedGenericClass {
	MonoGenericClass generic_class;
	guint is_initialized   : 1;
	MonoClass *klass;
};

/*
 * This is used when instantiating a generic type definition which is
 * a TypeBuilder.
 */
struct _MonoDynamicGenericClass {
	MonoInflatedGenericClass generic_class;
	MonoType *parent;
	int count_ifaces;
	MonoType **ifaces;
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
	guint initialized;
};

/*
 * A particular instantiation of a generic method.
 */
struct _MonoGenericMethod {
	MonoGenericInst *inst;			/* the instantiation */
	MonoGenericClass *generic_class;	/* if we're in a generic type */
	MonoGenericContainer *container;	/* type parameters */
	gpointer reflection_info;
};

/*
 * The generic context.
 */
struct _MonoGenericContext {
	/*
	 * The current container:
	 *
	 * If we're in a generic method, the generic method definition's container.
	 * Otherwise the generic type's container.
	 */
	MonoGenericContainer *container;
	/* The current generic class */
	MonoGenericClass *gclass;
	/* The current generic method */
	MonoGenericMethod *gmethod;
};

/*
 * The generic container.
 *
 * Stores the type parameters of a generic type definition or a generic method definition.
 */
struct _MonoGenericContainer {
	MonoGenericContext context;
	/* If we're a generic method definition, the containing class'es context. */
	MonoGenericContainer *parent;
	/* If we're a generic method definition, caches all their instantiations. */
	GHashTable *method_hash;
	/* If we're a generic type definition, its `MonoClass'. */
	MonoClass *klass;
	int type_argc    : 6;
	/* If true, we're a generic method, otherwise a generic type definition. */
	int is_method    : 1;
	/* Our type parameters. */
	MonoGenericParam *type_params;
	/* Cache for MonoTypes */
	MonoType **types;
};

/*
 * A type parameter.
 */
struct _MonoGenericParam {
	MonoGenericContainer *owner;	/* Type or method this parameter was defined in. */
	MonoClass *pklass;		/* The corresponding `MonoClass'. */
	MonoMethod *method;		/* If we're a method type parameter, the method. */
	const char *name;
	guint16 flags;
	guint16 num;
	MonoClass** constraints; /* NULL means end of list */
};

/*
 * Class information which might be cached by the runtime in the AOT file for
 * example. Caching this allows us to avoid computing a generic vtable
 * (class->vtable) in most cases, saving time and avoiding creation of lots of
 * MonoMethod structures.
 */
typedef struct MonoCachedClassInfo {
	guint32 vtable_size;
	guint has_finalize : 1;
	guint ghcimpl : 1;
	guint has_cctor : 1;
	guint has_nested_classes : 1;
	guint blittable : 1;
	guint has_references : 1;
	guint has_static_refs : 1;
	guint no_special_static_fields : 1;
	guint32 cctor_token;
	MonoImage *finalize_image;
	guint32 finalize_token;
	guint32 instance_size;
	guint32 class_size;
	guint32 packing_size;
	guint32 min_align;
} MonoCachedClassInfo;

typedef struct {
	const char *name;
	gconstpointer func;
	gconstpointer wrapper;
	MonoMethodSignature *sig;
} MonoJitICallInfo;

/*
 * Information about a type load error encountered by the loader.
 */
typedef enum {
	MONO_LOADER_ERROR_TYPE,
	MONO_LOADER_ERROR_METHOD,
	MONO_LOADER_ERROR_FIELD
} MonoLoaderErrorKind;

typedef struct {
	MonoLoaderErrorKind kind;
	char *class_name, *assembly_name; /* If kind == TYPE */
	MonoClass *klass; /* If kind != TYPE */
	const char *member_name; /* If kind != TYPE */
} MonoLoaderError;

#define mono_class_has_parent(klass,parent) (((klass)->idepth >= (parent)->idepth) && ((klass)->supertypes [(parent)->idepth - 1] == (parent)))

typedef struct {
	gulong new_object_count;
	gulong initialized_class_count;
	gulong used_class_count;
	gulong class_vtable_size;
	gulong class_static_data_size;
	gulong generic_instance_count;
	gulong generic_class_count;
	gulong inflated_method_count;
	gulong inflated_method_count_2;
	gulong inflated_type_count;
	gulong generics_metadata_size;
	gboolean enabled;
} MonoStats;

extern MonoStats mono_stats;

typedef gpointer (*MonoTrampoline)       (MonoMethod *method);
typedef gpointer (*MonoRemotingTrampoline)       (MonoMethod *method, MonoRemotingTarget target);
typedef gpointer (*MonoDelegateTrampoline)       (MonoMethod *method, gpointer addr);

typedef gpointer (*MonoLookupDynamicToken) (MonoImage *image, guint32 token, MonoClass **handle_class);

typedef gboolean (*MonoGetCachedClassInfo) (MonoClass *klass, MonoCachedClassInfo *res);

void
mono_classes_init (void);

void
mono_class_layout_fields   (MonoClass *klass);

void
mono_class_setup_vtable_general (MonoClass *klass, MonoMethod **overrides, int onum);

void
mono_class_setup_vtable (MonoClass *klass);

void
mono_class_setup_methods (MonoClass *klass);

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

gboolean
mono_class_get_overrides_full (MonoImage *image, guint32 type_token, MonoMethod ***overrides, gint32 *num_overrides,
			       MonoGenericContext *generic_context);

MonoMethod*
mono_class_get_cctor (MonoClass *klass);

MonoMethod*
mono_class_get_finalizer (MonoClass *klass);

gboolean
mono_class_needs_cctor_run (MonoClass *klass, MonoMethod *caller);

gboolean
mono_class_has_special_static_fields (MonoClass *klass);

void
mono_install_trampoline (MonoTrampoline func);

void
mono_install_remoting_trampoline (MonoRemotingTrampoline func);

void
mono_install_delegate_trampoline (MonoDelegateTrampoline func);

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token);

gpointer
mono_lookup_dynamic_token_class (MonoImage *image, guint32 token, MonoClass **handle_class);

void
mono_install_lookup_dynamic_token (MonoLookupDynamicToken func);

void
mono_install_get_cached_class_info (MonoGetCachedClassInfo func);

MonoInflatedGenericClass*
mono_get_inflated_generic_class (MonoGenericClass *gclass);

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
	MonoClass *runtimesecurityframe_class;
	MonoClass *executioncontext_class;
	MonoClass *generic_array_class;
	MonoClass *generic_nullable_class;
} MonoDefaults;

extern MonoDefaults mono_defaults;

void
mono_loader_init           (void);

void
mono_loader_lock           (void);

void
mono_loader_unlock         (void);

void
mono_loader_set_error_type_load (char *class_name, char *assembly_name);

void
mono_loader_set_error_method_load (MonoClass *klass, const char *member_name);

void
mono_loader_set_error_field_load (MonoClass *klass, const char *member_name);

MonoLoaderError*
mono_loader_get_last_error (void);

void
mono_loader_clear_error    (void);

void 
mono_icall_init            (void);

void
mono_icall_cleanup         (void);

gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id);

void
mono_install_stack_walk (MonoStackWalkImpl func);

gboolean
mono_metadata_has_generic_params (MonoImage *image, guint32 token);

MonoGenericContainer *
mono_metadata_load_generic_params (MonoImage *image, guint32 token,
				   MonoGenericContainer *parent_container);

void
mono_metadata_load_generic_param_constraints (MonoImage *image, guint32 token,
					      MonoGenericContainer *container);

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

MonoMethodSignature*
mono_class_inflate_generic_signature (MonoImage *image, MonoMethodSignature *sig, MonoGenericContext *context);

MonoMethodSignature *
mono_method_signature_full (MonoMethod *image, MonoGenericContainer *container);

MonoGenericClass *
mono_get_shared_generic_class (MonoGenericContainer *container, gboolean is_dynamic);

gboolean
mono_class_set_failure (MonoClass *klass, guint32 ex_type, void *ex_data);

MonoException*
mono_class_get_exception_for_failure (MonoClass *klass);

char*
mono_type_get_name_full (MonoType *type, MonoTypeNameFormat format);

char*
mono_type_get_full_name (MonoClass *class);

MonoArrayType *mono_dup_array_type (MonoArrayType *a);
MonoMethodSignature *mono_metadata_signature_deep_dup (MonoMethodSignature *sig);

gboolean mono_class_is_nullable (MonoClass *klass);
MonoClass *mono_class_get_nullable_param (MonoClass *klass);

#endif /* __MONO_METADATA_CLASS_INTERBALS_H__ */

