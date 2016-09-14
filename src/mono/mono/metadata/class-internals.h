/* 
 * Copyright 2012 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_CLASS_INTERNALS_H__
#define __MONO_METADATA_CLASS_INTERNALS_H__

#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error.h"
#include "mono/sgen/gc-internal-agnostic.h"

#define MONO_CLASS_IS_ARRAY(c) ((c)->rank)

#define MONO_CLASS_HAS_STATIC_METADATA(klass) ((klass)->type_token && !(klass)->image->dynamic && !(klass)->generic_class)

#define MONO_DEFAULT_SUPERTABLE_SIZE 6

extern gboolean mono_print_vtable;
extern gboolean mono_align_small_structs;

typedef struct _MonoMethodWrapper MonoMethodWrapper;
typedef struct _MonoMethodInflated MonoMethodInflated;
typedef struct _MonoMethodPInvoke MonoMethodPInvoke;

/* Properties that applies to a group of structs should better use a higher number
 * to avoid colision with type specific properties.
 * 
 * This prop applies to class, method, property, event, assembly and image.
 */
#define MONO_PROP_DYNAMIC_CATTR 0x1000

#ifdef ENABLE_ICALL_EXPORT
#pragma GCC diagnostic ignored "-Wmissing-prototypes"
#define ICALL_EXPORT MONO_API
#else
#define ICALL_EXPORT static
#endif

typedef enum {
#define WRAPPER(e,n) MONO_WRAPPER_ ## e,
#include "wrapper-types.h"
#undef WRAPPER
	MONO_WRAPPER_NUM
} MonoWrapperType;

typedef enum {
	MONO_TYPE_NAME_FORMAT_IL,
	MONO_TYPE_NAME_FORMAT_REFLECTION,
	MONO_TYPE_NAME_FORMAT_FULL_NAME,
	MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
} MonoTypeNameFormat;

typedef enum {
	MONO_REMOTING_TARGET_UNKNOWN,
	MONO_REMOTING_TARGET_APPDOMAIN,
	MONO_REMOTING_TARGET_COMINTEROP
} MonoRemotingTarget;

#define MONO_METHOD_PROP_GENERIC_CONTAINER 0

struct _MonoMethod {
	guint16 flags;  /* method flags */
	guint16 iflags; /* method implementation flags */
	guint32 token;
	MonoClass *klass; /* To what class does this method belong */
	MonoMethodSignature *signature;
	/* name is useful mostly for debugging */
	const char *name;
	/* this is used by the inlining algorithm */
	unsigned int inline_info:1;
	unsigned int inline_failure:1;
	unsigned int wrapper_type:5;
	unsigned int string_ctor:1;
	unsigned int save_lmf:1;
	unsigned int dynamic:1; /* created & destroyed during runtime */
	unsigned int sre_method:1; /* created at runtime using Reflection.Emit */
	unsigned int is_generic:1; /* whenever this is a generic method definition */
	unsigned int is_inflated:1; /* whether we're a MonoMethodInflated */
	unsigned int skip_visibility:1; /* whenever to skip JIT visibility checks */
	unsigned int verification_success:1; /* whether this method has been verified successfully.*/
	signed int slot : 16;

	/*
	 * If is_generic is TRUE, the generic_container is stored in image->property_hash, 
	 * using the key MONO_METHOD_PROP_GENERIC_CONTAINER.
	 */
};

struct _MonoMethodWrapper {
	MonoMethod method;
	MonoMethodHeader *header;
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
 * Stores the default value / RVA of fields.
 * This information is rarely needed, so it is stored separately from 
 * MonoClassField.
 */
typedef struct MonoFieldDefaultValue {
	/*
	 * If the field is constant, pointer to the metadata constant
	 * value.
	 * If the field has an RVA flag, pointer to the data.
	 * Else, invalid.
	 */
	const char      *data;

	/* If the field is constant, the type of the constant. */
	MonoTypeEnum     def_type;
} MonoFieldDefaultValue;

/*
 * MonoClassField is just a runtime representation of the metadata for
 * field, it doesn't contain the data directly.  Static fields are
 * stored in MonoVTable->data.  Instance fields are allocated in the
 * objects after the object header.
 */
struct _MonoClassField {
	/* Type of the field */
	MonoType        *type;

	const char      *name;

	/* Type where the field was defined */
	MonoClass       *parent;

	/*
	 * Offset where this field is stored; if it is an instance
	 * field, it's the offset from the start of the object, if
	 * it's static, it's from the start of the memory chunk
	 * allocated for statics for the class.
	 * For special static fields, this is set to -1 during vtable construction.
	 */
	int              offset;
};

/* a field is ignored if it's named "_Deleted" and it has the specialname and rtspecialname flags set */
#define mono_field_is_deleted(field) (((field)->type->attrs & (FIELD_ATTRIBUTE_SPECIAL_NAME | FIELD_ATTRIBUTE_RT_SPECIAL_NAME)) \
				      && (strcmp (mono_field_get_name (field), "_Deleted") == 0))

/* a field is ignored if it's named "_Deleted" and it has the specialname and rtspecialname flags set */
/* Try to avoid loading the field's type */
#define mono_field_is_deleted_with_flags(field, flags) (((flags) & (FIELD_ATTRIBUTE_SPECIAL_NAME | FIELD_ATTRIBUTE_RT_SPECIAL_NAME)) \
				      && (strcmp (mono_field_get_name (field), "_Deleted") == 0))

typedef struct {
	MonoClassField *field;
	guint32 offset;
	MonoMarshalSpec *mspec;
} MonoMarshalField;

typedef struct {
	guint32 native_size, min_align;
	guint32 num_fields;
	MonoMethod *ptr_to_str;
	MonoMethod *str_to_ptr;
	MonoMarshalField fields [MONO_ZERO_LEN_ARRAY];
} MonoMarshalType;

#define MONO_SIZEOF_MARSHAL_TYPE (offsetof (MonoMarshalType, fields))

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
#ifndef MONO_SMALL_CONFIG
	MonoMethod **other;
#endif
	guint32 attrs;
};

/* type of exception being "on hold" for later processing (see exception_type) */
enum {
	MONO_EXCEPTION_NONE = 0,
	MONO_EXCEPTION_INVALID_PROGRAM = 3,
	MONO_EXCEPTION_UNVERIFIABLE_IL = 4,
	MONO_EXCEPTION_MISSING_METHOD = 5,
	MONO_EXCEPTION_MISSING_FIELD = 6,
	MONO_EXCEPTION_TYPE_LOAD = 7,
	MONO_EXCEPTION_FILE_NOT_FOUND = 8,
	MONO_EXCEPTION_METHOD_ACCESS = 9,
	MONO_EXCEPTION_FIELD_ACCESS = 10,
	MONO_EXCEPTION_GENERIC_SHARING_FAILED = 11,
	MONO_EXCEPTION_BAD_IMAGE = 12,
	MONO_EXCEPTION_OBJECT_SUPPLIED = 13, /*The exception object is already created.*/
	MONO_EXCEPTION_OUT_OF_MEMORY = 14,
	MONO_EXCEPTION_INLINE_FAILED = 15,
	MONO_EXCEPTION_MONO_ERROR = 16,
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

#define MONO_SIZEOF_CLASS_RUNTIME_INFO (sizeof (MonoClassRuntimeInfo) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

#define MONO_CLASS_PROP_EXCEPTION_DATA 0

/* 
 * This structure contains the rarely used fields of MonoClass
 * Since using just one field causes the whole structure to be allocated, it should
 * be used for fields which are only used in like 5% of all classes.
 */
typedef struct {
	struct {
#if MONO_SMALL_CONFIG
		guint16 first, count;
#else
		guint32 first, count;
#endif
	} property, event;

	/* Initialized by a call to mono_class_setup_properties () */
	MonoProperty *properties;

	/* Initialized by a call to mono_class_setup_events () */
	MonoEvent *events;

	guint32    declsec_flags;	/* declarative security attributes flags */

	/* Default values/RVA for fields and properties */
	/* Accessed using mono_class_get_field_default_value () / mono_field_get_data () */
	MonoFieldDefaultValue *field_def_values;
	MonoFieldDefaultValue *prop_def_values;

	GList      *nested_classes;
} MonoClassExt;

struct _MonoClass {
	/* element class for arrays and enum basetype for enums */
	MonoClass *element_class; 
	/* used for subtype checks */
	MonoClass *cast_class; 

	/* for fast subtype checks */
	MonoClass **supertypes;
	guint16     idepth;

	/* array dimension */
	guint8     rank;          

	int        instance_size; /* object instance size */

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
	/* size_inited is accessed without locks, so it needs a memory barrier */
	guint size_inited     : 1;
	guint valuetype       : 1; /* derives from System.ValueType */
	guint enumtype        : 1; /* derives from System.Enum */
	guint blittable       : 1; /* class is blittable */
	guint unicode         : 1; /* class uses unicode char when marshalled */
	guint wastypebuilder  : 1; /* class was created at runtime from a TypeBuilder */
	/* next byte */
	guint8 min_align;

	/* next byte */
	guint packing_size    : 4;
	guint ghcimpl         : 1; /* class has its own GetHashCode impl */ 
	guint has_finalize    : 1; /* class has its own Finalize impl */ 
#ifndef DISABLE_REMOTING
	guint marshalbyref    : 1; /* class is a MarshalByRefObject */
	guint contextbound    : 1; /* class is a ContextBoundObject */
#endif
	/* next byte */
	guint delegate        : 1; /* class is a Delegate */
	guint gc_descr_inited : 1; /* gc_descr is initialized */
	guint has_cctor       : 1; /* class has a cctor */
	guint has_references  : 1; /* it has GC-tracked references in the instance */
	guint has_static_refs : 1; /* it has static fields that are GC-tracked */
	guint no_special_static_fields : 1; /* has no thread/context static fields */
	/* directly or indirectly derives from ComImport attributed class.
	 * this means we need to create a proxy for instances of this class
	 * for COM Interop. set this flag on loading so all we need is a quick check
	 * during object creation rather than having to traverse supertypes
	 */
	guint is_com_object : 1; 
	guint nested_classes_inited : 1; /* Whenever nested_class is initialized */

	/* next byte*/
	guint interfaces_inited : 1; /* interfaces is initialized */
	guint simd_type : 1; /* class is a simd intrinsic type */
	guint is_generic : 1; /* class is a generic type definition */
	guint is_inflated : 1; /* class is a generic instance */
	guint has_finalize_inited    : 1; /* has_finalize is initialized */
	guint fields_inited : 1; /* fields is initialized */
	guint setup_fields_called : 1; /* to prevent infinite loops in setup_fields */

	guint8     exception_type;	/* MONO_EXCEPTION_* */

	/* Additional information about the exception */
	/* Stored as property MONO_CLASS_PROP_EXCEPTION_DATA */
	//void       *exception_data;

	MonoClass  *parent;
	MonoClass  *nested_in;

	MonoImage *image;
	const char *name;
	const char *name_space;

	guint32    type_token;
	int        vtable_size; /* number of slots */

	guint16     interface_count;
	guint16     interface_id;        /* unique inderface id (for interfaces) */
	guint16     max_interface_id;
	
	guint16     interface_offsets_count;
	MonoClass **interfaces_packed;
	guint16    *interface_offsets_packed;
/* enabled only with small config for now: we might want to do it unconditionally */
#ifdef MONO_SMALL_CONFIG
#define COMPRESSED_INTERFACE_BITMAP 1
#endif
	guint8     *interface_bitmap;

	MonoClass **interfaces;

	union {
		int class_size; /* size of area for static fields */
		int element_size; /* for array types */
		int generic_param_token; /* for generic param types, both var and mvar */
	} sizes;

	/*
	 * From the TypeDef table
	 */
	guint32    flags;
	struct {
#if MONO_SMALL_CONFIG
		guint16 first, count;
#else
		guint32 first, count;
#endif
	} field, method;

	/* A GC handle pointing to the corresponding type builder/generic param builder */
	guint32 ref_info_handle;

	/* loaded on demand */
	MonoMarshalType *marshal_info;

	/*
	 * Field information: Type and location from object base
	 */
	MonoClassField *fields;

	MonoMethod **methods;

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType byval_arg;

	MonoGenericClass *generic_class;
	MonoGenericContainer *generic_container;

	MonoGCDescriptor gc_descr;

	MonoClassRuntimeInfo *runtime_info;

	/* next element in the class_cache hash list (in MonoImage) */
	MonoClass *next_class_cache;

	/* Generic vtable. Initialized by a call to mono_class_setup_vtable () */
	MonoMethod **vtable;

	/* Rarely used fields of classes */
	MonoClassExt *ext;
};

#ifdef COMPRESSED_INTERFACE_BITMAP
int mono_compress_bitmap (uint8_t *dest, const uint8_t *bitmap, int size);
int mono_class_interface_match (const uint8_t *bitmap, int id);
#else
#define mono_class_interface_match(bmap,uiid) ((bmap) [(uiid) >> 3] & (1 << ((uiid)&7)))
#endif

#define MONO_CLASS_IMPLEMENTS_INTERFACE(k,uiid) (((uiid) <= (k)->max_interface_id) && mono_class_interface_match ((k)->interface_bitmap, (uiid)))

#define MONO_VTABLE_AVAILABLE_GC_BITS 4

#ifdef DISABLE_REMOTING
#define mono_class_is_marshalbyref(klass) (FALSE)
#define mono_class_is_contextbound(klass) (FALSE)
#define mono_vtable_is_remote(vtable) (FALSE)
#define mono_vtable_set_is_remote(vtable,enable) do {} while (0)
#else
#define mono_class_is_marshalbyref(klass) ((klass)->marshalbyref)
#define mono_class_is_contextbound(klass) ((klass)->contextbound)
#define mono_vtable_is_remote(vtable) ((vtable)->remote)
#define mono_vtable_set_is_remote(vtable,enable) do { (vtable)->remote = enable ? 1 : 0; } while (0)
#endif

#ifdef DISABLE_COM
#define mono_class_is_com_object(klass) (FALSE)
#define mono_class_set_is_com_object(klass) do {} while (0)
#else
#define mono_class_is_com_object(klass) ((klass)->is_com_object)
#define mono_class_set_is_com_object(klass) do { (klass)->is_com_object = 1; } while (0)
#endif


MONO_API int mono_class_interface_offset (MonoClass *klass, MonoClass *itf);
int mono_class_interface_offset_with_variance (MonoClass *klass, MonoClass *itf, gboolean *non_exact_match);

typedef gpointer MonoRuntimeGenericContext;

/* the interface_offsets array is stored in memory before this struct */
struct MonoVTable {
	MonoClass  *klass;
	 /*
	 * According to comments in gc_gcj.h, this should be the second word in
	 * the vtable.
	 */
	MonoGCDescriptor gc_descr;
	MonoDomain *domain;  /* each object/vtable belongs to exactly one domain */
        gpointer    type; /* System.Type type for klass */
	guint8     *interface_bitmap;
	guint16     max_interface_id;
	guint8      rank;
	guint remote          : 1; /* class is remotely activated */
	guint initialized     : 1; /* cctor has been run */
	guint init_failed     : 1; /* cctor execution failed */
	guint has_static_fields : 1; /* pointer to the data stored at the end of the vtable array */
	guint gc_bits         : MONO_VTABLE_AVAILABLE_GC_BITS; /* Those bits are reserved for the usaged of the GC */

	guint32     imt_collisions_bitmap;
	MonoRuntimeGenericContext *runtime_generic_context;
	/* do not add any fields after vtable, the structure is dynamically extended */
	/* vtable contains function pointers to methods or their trampolines, at the
	 end there may be a slot containing the pointer to the static fields */
	gpointer    vtable [MONO_ZERO_LEN_ARRAY];	
};

#define MONO_SIZEOF_VTABLE (sizeof (MonoVTable) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

#define MONO_VTABLE_IMPLEMENTS_INTERFACE(vt,uiid) (((uiid) <= (vt)->max_interface_id) && mono_class_interface_match ((vt)->interface_bitmap, (uiid)))

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
#ifndef MONO_SMALL_CONFIG
	guint id;			/* unique ID for debugging */
#endif
	guint type_argc    : 22;	/* number of type arguments */
	guint is_open      :  1;	/* if this is an open type */
	MonoType *type_argv [MONO_ZERO_LEN_ARRAY];
};

#define MONO_SIZEOF_GENERIC_INST (sizeof (MonoGenericInst) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)
/*
 * The generic context: an instantiation of a set of class and method generic parameters.
 *
 * NOTE: Never allocate this directly on the heap.  It have to be either allocated on the stack,
 *	 or embedded within other objects.  Don't store pointers to this, because it may be on the stack.
 *	 If you really have to, ensure you store a pointer to the embedding object along with it.
 */
struct _MonoGenericContext {
	/* The instantiation corresponding to the class generic parameters */
	MonoGenericInst *class_inst;
	/* The instantiation corresponding to the method generic parameters */
	MonoGenericInst *method_inst;
};

/*
 * Inflated generic method.
 */
struct _MonoMethodInflated {
	union {
		MonoMethod method;
		MonoMethodPInvoke pinvoke;
	} method;
	MonoMethod *declaring;		/* the generic method definition. */
	MonoGenericContext context;	/* The current instantiation */
	MonoImageSet *owner; /* The image set that the inflated method belongs to. */
};

/*
 * A particular instantiation of a generic type.
 */
struct _MonoGenericClass {
	MonoClass *container_class;	/* the generic type definition */
	MonoGenericContext context;	/* a context that contains the type instantiation doesn't contain any method instantiation */ /* FIXME: Only the class_inst member of "context" is ever used, so this field could be replaced with just a monogenericinst */
	guint is_dynamic  : 1;		/* Contains dynamic types */
	guint is_tb_open  : 1;		/* This is the fully open instantiation for a type_builder. Quite ugly, but it's temporary.*/
	guint need_sync   : 1;      /* Only if dynamic. Need to be synchronized with its container class after its finished. */
	MonoClass *cached_class;	/* if present, the MonoClass corresponding to the instantiation.  */

	/* 
	 * The image set which owns this generic class. Memory owned by the generic class
	 * including cached_class should be allocated from the mempool of the image set,
	 * so it is easy to free.
	 */
	MonoImageSet *owner;
};

/*
 * A type parameter.
 */
struct _MonoGenericParam {
	/*
	 * Type or method this parameter was defined in.
	 */
	MonoGenericContainer *owner;
	guint16 num;
	/*
	 * If != NULL, this is a generated generic param used by the JIT to implement generic
	 * sharing.
	 */
	MonoType *gshared_constraint;
};

/* Additional details about a MonoGenericParam */
/* Keep in sync with managed Mono.RuntimeStructs.GenericParamInfo */
typedef struct {
	MonoClass *pklass;		/* The corresponding `MonoClass'. */
	const char *name;

	// See GenericParameterAttributes
	guint16 flags;

	guint32 token;

	// Constraints on type parameters
	MonoClass** constraints; /* NULL means end of list */
} MonoGenericParamInfo;

typedef struct {
	MonoGenericParam param;
	MonoGenericParamInfo info;
} MonoGenericParamFull;

/*
 * The generic container.
 *
 * Stores the type parameters of a generic type definition or a generic method definition.
 */
struct _MonoGenericContainer {
	MonoGenericContext context;
	/* If we're a generic method definition in a generic type definition,
	   the generic container of the containing class. */
	MonoGenericContainer *parent;
	/* the generic type definition or the generic method definition corresponding to this container */
	/* Union rules: If is_anonymous, image field is valid; else if is_method, method field is valid; else klass is valid. */
	union {
		MonoClass *klass;
		MonoMethod *method;
		MonoImage *image;
	} owner;
	int type_argc    : 29; // Per the ECMA spec, this value is capped at 16 bits
	/* If true, we're a generic method, otherwise a generic type definition. */
	/* Invariant: parent != NULL => is_method */
	int is_method     : 1;
	/* If true, this container has no associated class/method and only the image is known. This can happen:
	   1. For the special anonymous containers kept by MonoImage.
	   2. During container creation via the mono_metadata_load_generic_params path-- in this case the caller
	      sets the owner, so temporarily while load_generic_params is completing the container is anonymous.
	   3. When user code creates a generic parameter via SRE, but has not yet set an owner. */
	int is_anonymous : 1;
	/* If false, all params in this container are full-size. If true, all params are just param structs. */
	/* This field is always == to the is_anonymous field, except in "temporary" cases (2) and (3) above. */
	/* TODO: Merge GenericParam and GenericParamFull, remove this field. Benefit is marginal. */
	int is_small_param : 1;
	/* Our type parameters. */
	MonoGenericParamFull *type_params;
};

static inline MonoGenericParam *
mono_generic_container_get_param (MonoGenericContainer *gc, int i)
{
	return (MonoGenericParam *) &gc->type_params [i];
}

static inline MonoGenericParamInfo *
mono_generic_container_get_param_info (MonoGenericContainer *gc, int i)
{
	return &gc->type_params [i].info;
}

static inline MonoGenericContainer *
mono_generic_param_owner (MonoGenericParam *p)
{
	return p->owner;
}

static inline int
mono_generic_param_num (MonoGenericParam *p)
{
	return p->num;
}

static inline gboolean
mono_generic_param_is_fullsize (MonoGenericParam *p)
{
	return !mono_generic_param_owner (p)->is_small_param;
}

static inline MonoGenericParamInfo *
mono_generic_param_info (MonoGenericParam *p)
{
	if (mono_generic_param_is_fullsize (p))
		return &((MonoGenericParamFull *) p)->info;
	return NULL;
}

static inline const char *
mono_generic_param_name (MonoGenericParam *p)
{
	if (mono_generic_param_is_fullsize (p))
		return ((MonoGenericParamFull *) p)->info.name;
	return NULL;
}

static inline MonoGenericContainer *
mono_type_get_generic_param_owner (MonoType *t)
{
	return mono_generic_param_owner (t->data.generic_param);
}

static inline int
mono_type_get_generic_param_num (MonoType *t)
{
	return mono_generic_param_num (t->data.generic_param);
}

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
	guint is_generic_container : 1;
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
	gconstpointer trampoline;
	MonoMethodSignature *sig;
	const char *c_symbol;
	MonoMethod *wrapper_method;
	gboolean no_raise;
} MonoJitICallInfo;

void
mono_class_setup_supertypes (MonoClass *klass);

void
mono_class_setup_fields_locking (MonoClass *klass);

/* WARNING
 * Only call this function if you can ensure both @klass and @parent
 * have supertype information initialized.
 * This can be accomplished by mono_class_setup_supertypes or mono_class_init.
 * If unsure, use mono_class_has_parent.
 */
static inline gboolean
mono_class_has_parent_fast (MonoClass *klass, MonoClass *parent)
{
	return (klass->idepth >= parent->idepth) && (klass->supertypes [parent->idepth - 1] == parent);
}

static inline gboolean
mono_class_has_parent (MonoClass *klass, MonoClass *parent)
{
	if (G_UNLIKELY (!klass->supertypes))
		mono_class_setup_supertypes (klass);

	if (G_UNLIKELY (!parent->supertypes))
		mono_class_setup_supertypes (parent);

	return mono_class_has_parent_fast (klass, parent);
}

typedef struct {
	MonoVTable *default_vtable;
	MonoVTable *xdomain_vtable;
	MonoClass *proxy_class;
	char* proxy_class_name;
	uint32_t interface_count;
	MonoClass *interfaces [MONO_ZERO_LEN_ARRAY];
} MonoRemoteClass;

#define MONO_SIZEOF_REMOTE_CLASS (sizeof (MonoRemoteClass) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

typedef struct {
	guint64 new_object_count;
	size_t initialized_class_count;
	size_t generic_vtable_count;
	size_t used_class_count;
	size_t method_count;
	size_t class_vtable_size;
	size_t class_static_data_size;
	size_t generic_instance_count;
	size_t generic_class_count;
	size_t inflated_method_count;
	size_t inflated_method_count_2;
	size_t inflated_type_count;
	size_t generics_metadata_size;
	size_t delegate_creations;
	size_t imt_tables_size;
	size_t imt_number_of_tables;
	size_t imt_number_of_methods;
	size_t imt_used_slots;
	size_t imt_slots_with_collisions;
	size_t imt_max_collisions_in_slot;
	size_t imt_method_count_when_max_collisions;
	size_t imt_trampolines_size;
	size_t jit_info_table_insert_count;
	size_t jit_info_table_remove_count;
	size_t jit_info_table_lookup_count;
	size_t generics_sharable_methods;
	size_t generics_unsharable_methods;
	size_t generics_shared_methods;
	size_t gsharedvt_methods;
	size_t minor_gc_count;
	size_t major_gc_count;
	size_t minor_gc_time_usecs;
	size_t major_gc_time_usecs;
	gboolean enabled;
} MonoStats;

/* 
 * new structure to hold performace counters values that are exported
 * to managed code.
 * Note: never remove fields from this structure and only add them to the end.
 * Size of fields and type should not be changed as well.
 */
typedef struct {
	/* JIT category */
	guint32 jit_methods;
	guint32 jit_bytes;
	guint32 jit_time;
	guint32 jit_failures;
	/* Exceptions category */
	guint32 exceptions_thrown;
	guint32 exceptions_filters;
	guint32 exceptions_finallys;
	guint32 exceptions_depth;
	guint32 aspnet_requests_queued;
	guint32 aspnet_requests;
	/* Memory category */
	guint32 gc_collections0;
	guint32 gc_collections1;
	guint32 gc_collections2;
	guint32 gc_promotions0;
	guint32 gc_promotions1;
	guint32 gc_promotion_finalizers;
	guint32 gc_gen0size;
	guint32 gc_gen1size;
	guint32 gc_gen2size;
	guint32 gc_lossize;
	guint32 gc_fin_survivors;
	guint32 gc_num_handles;
	guint32 gc_allocated;
	guint32 gc_induced;
	guint32 gc_time;
	guint32 gc_total_bytes;
	guint32 gc_committed_bytes;
	guint32 gc_reserved_bytes;
	guint32 gc_num_pinned;
	guint32 gc_sync_blocks;
	/* Remoting category */
	guint32 remoting_calls;
	guint32 remoting_channels;
	guint32 remoting_proxies;
	guint32 remoting_classes;
	guint32 remoting_objects;
	guint32 remoting_contexts;
	/* Loader category */
	guint32 loader_classes;
	guint32 loader_total_classes;
	guint32 loader_appdomains;
	guint32 loader_total_appdomains;
	guint32 loader_assemblies;
	guint32 loader_total_assemblies;
	guint32 loader_failures;
	guint32 loader_bytes;
	guint32 loader_appdomains_uloaded;
	/* Threads and Locks category  */
	guint32 thread_contentions;
	guint32 thread_queue_len;
	guint32 thread_queue_max;
	guint32 thread_num_logical;
	guint32 thread_num_physical;
	guint32 thread_cur_recognized;
	guint32 thread_num_recognized;
	/* Interop category */
	guint32 interop_num_ccw;
	guint32 interop_num_stubs;
	guint32 interop_num_marshals;
	/* Security category */
	guint32 security_num_checks;
	guint32 security_num_link_checks;
	guint32 security_time;
	guint32 security_depth;
	guint32 unused;
	/* Threadpool */
	guint64 threadpool_workitems;
	guint64 threadpool_ioworkitems;
	guint threadpool_threads;
	guint threadpool_iothreads;
} MonoPerfCounters;

extern MonoPerfCounters *mono_perfcounters;

MONO_API void mono_perfcounters_init (void);

/*
 * The definition of the first field in SafeHandle,
 * Keep in sync with SafeHandle.cs, this is only used
 * to access the `handle' parameter.
 */
typedef struct {
	MonoObject  base;
	void       *handle;
} MonoSafeHandle;

/*
 * Keep in sync with HandleRef.cs
 */
typedef struct {
	MonoObject *wrapper;
	void       *handle;
} MonoHandleRef;

extern MonoStats mono_stats;

typedef gpointer (*MonoRemotingTrampoline)       (MonoDomain *domain, MonoMethod *method, MonoRemotingTarget target, MonoError *error);
typedef gpointer (*MonoDelegateTrampoline)       (MonoDomain *domain, MonoClass *klass);

typedef gboolean (*MonoGetCachedClassInfo) (MonoClass *klass, MonoCachedClassInfo *res);

typedef gboolean (*MonoGetClassFromName) (MonoImage *image, const char *name_space, const char *name, MonoClass **res);

static inline gboolean
method_is_dynamic (MonoMethod *method)
{
#ifdef DISABLE_REFLECTION_EMIT
	return FALSE;
#else
	return method->dynamic;
#endif
}

void
mono_classes_init (void);

void
mono_classes_cleanup (void);

void
mono_class_layout_fields   (MonoClass *klass, int instance_size);

void
mono_class_setup_interface_offsets (MonoClass *klass);

void
mono_class_setup_vtable_general (MonoClass *klass, MonoMethod **overrides, int onum, GList *in_setup);

void
mono_class_setup_vtable (MonoClass *klass);

void
mono_class_setup_methods (MonoClass *klass);

void
mono_class_setup_mono_type (MonoClass *klass);

void
mono_class_setup_parent    (MonoClass *klass, MonoClass *parent);

MonoMethod*
mono_class_get_method_by_index (MonoClass *klass, int index);

MonoMethod*
mono_class_get_inflated_method (MonoClass *klass, MonoMethod *method);

MonoMethod*
mono_class_get_vtable_entry (MonoClass *klass, int offset);

GPtrArray*
mono_class_get_implemented_interfaces (MonoClass *klass, MonoError *error);

int
mono_class_get_vtable_size (MonoClass *klass);

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
mono_class_field_is_special_static (MonoClassField *field);

guint32
mono_class_field_get_special_static_type (MonoClassField *field);

gboolean
mono_class_has_special_static_fields (MonoClass *klass);

const char*
mono_class_get_field_default_value (MonoClassField *field, MonoTypeEnum *def_type);

const char*
mono_class_get_property_default_value (MonoProperty *property, MonoTypeEnum *def_type);

void
mono_install_delegate_trampoline (MonoDelegateTrampoline func);

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token, MonoGenericContext *context, MonoError *error);

gpointer
mono_lookup_dynamic_token_class (MonoImage *image, guint32 token, gboolean check_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

gpointer
mono_runtime_create_jump_trampoline (MonoDomain *domain, MonoMethod *method, gboolean add_sync_wrapper, MonoError *error);

gpointer
mono_runtime_create_delegate_trampoline (MonoClass *klass);

void
mono_install_get_cached_class_info (MonoGetCachedClassInfo func);

void
mono_install_get_class_from_name (MonoGetClassFromName func);

MonoGenericContext*
mono_class_get_context (MonoClass *klass);

MonoMethodSignature*
mono_method_signature_checked (MonoMethod *m, MonoError *err);

MonoGenericContext*
mono_method_get_context_general (MonoMethod *method, gboolean uninflated);

MonoGenericContext*
mono_method_get_context (MonoMethod *method);

/* Used by monodis, thus cannot be MONO_INTERNAL */
MONO_API MonoGenericContainer*
mono_method_get_generic_container (MonoMethod *method);

MonoGenericContext*
mono_generic_class_get_context (MonoGenericClass *gclass);

MonoClass*
mono_generic_class_get_class (MonoGenericClass *gclass);

void
mono_method_set_generic_container (MonoMethod *method, MonoGenericContainer* container);

MonoMethod*
mono_class_inflate_generic_method_full (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context);

MonoMethod*
mono_class_inflate_generic_method_full_checked (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context, MonoError *error);

MonoMethod *
mono_class_inflate_generic_method_checked (MonoMethod *method, MonoGenericContext *context, MonoError *error);

MonoImageSet *
mono_metadata_get_image_set_for_method (MonoMethodInflated *method);

MONO_API MonoMethodSignature *
mono_metadata_get_inflated_signature (MonoMethodSignature *sig, MonoGenericContext *context);

MonoType*
mono_class_inflate_generic_type_with_mempool (MonoImage *image, MonoType *type, MonoGenericContext *context, MonoError *error);

MonoType*
mono_class_inflate_generic_type_checked (MonoType *type, MonoGenericContext *context, MonoError *error);

MONO_API void
mono_metadata_free_inflated_signature (MonoMethodSignature *sig);

MonoMethodSignature*
mono_inflate_generic_signature (MonoMethodSignature *sig, MonoGenericContext *context, MonoError *error);

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
	MonoClass *manualresetevent_class;
	MonoClass *typehandle_class;
	MonoClass *fieldhandle_class;
	MonoClass *methodhandle_class;
	MonoClass *systemtype_class;
	MonoClass *runtimetype_class;
	MonoClass *exception_class;
	MonoClass *threadabortexception_class;
	MonoClass *thread_class;
	MonoClass *internal_thread_class;
#ifndef DISABLE_REMOTING
	MonoClass *transparent_proxy_class;
	MonoClass *real_proxy_class;
	MonoClass *marshalbyrefobject_class;
	MonoClass *iremotingtypeinfo_class;
#endif
	MonoClass *mono_method_message_class;
	MonoClass *appdomain_class;
	MonoClass *field_info_class;
	MonoClass *method_info_class;
	MonoClass *stringbuilder_class;
	MonoClass *math_class;
	MonoClass *stack_frame_class;
	MonoClass *stack_trace_class;
	MonoClass *marshal_class;
	MonoClass *typed_reference_class;
	MonoClass *argumenthandle_class;
	MonoClass *monitor_class;
	MonoClass *generic_ilist_class;
	MonoClass *generic_nullable_class;
	MonoClass *handleref_class;
	MonoClass *attribute_class;
	MonoClass *customattribute_data_class;
	MonoClass *critical_finalizer_object; /* MAYBE NULL */
	MonoClass *generic_ireadonlylist_class;
	MonoClass *threadpool_wait_callback_class;
	MonoMethod *threadpool_perform_wait_callback_method;
} MonoDefaults;

#ifdef DISABLE_REMOTING
#define mono_class_is_transparent_proxy(klass) (FALSE)
#define mono_class_is_real_proxy(klass) (FALSE)
#define mono_object_is_transparent_proxy(object) (FALSE)
#else
MonoRemoteClass*
mono_remote_class (MonoDomain *domain, MonoString *class_name, MonoClass *proxy_class, MonoError *error);

void
mono_install_remoting_trampoline (MonoRemotingTrampoline func);

#define mono_class_is_transparent_proxy(klass) ((klass) == mono_defaults.transparent_proxy_class)
#define mono_class_is_real_proxy(klass) ((klass) == mono_defaults.real_proxy_class)
#define mono_object_is_transparent_proxy(object) (((MonoObject*)object)->vtable->klass == mono_defaults.transparent_proxy_class)
#endif


#define GENERATE_GET_CLASS_WITH_CACHE_DECL(shortname) \
MonoClass* mono_class_get_##shortname##_class (void);

#define GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL(shortname) \
MonoClass* mono_class_try_get_##shortname##_class (void);

#define GENERATE_GET_CLASS_WITH_CACHE(shortname,namespace,name) \
MonoClass*	\
mono_class_get_##shortname##_class (void)	\
{	\
	static MonoClass *tmp_class;	\
	MonoClass *klass = tmp_class;	\
	if (!klass) {	\
		klass = mono_class_load_from_name (mono_defaults.corlib, #namespace, #name);	\
		mono_memory_barrier ();	\
		tmp_class = klass;	\
	}	\
	return klass;	\
}

#define GENERATE_TRY_GET_CLASS_WITH_CACHE(shortname,namespace,name) \
MonoClass*	\
mono_class_try_get_##shortname##_class (void)	\
{	\
	static volatile MonoClass *tmp_class;	\
	static volatile gboolean inited;	\
	MonoClass *klass = (MonoClass *)tmp_class;	\
	mono_memory_barrier ();	\
	if (!inited) {	\
		klass = mono_class_try_load_from_name (mono_defaults.corlib, #namespace, #name);	\
		tmp_class = klass;	\
		mono_memory_barrier ();	\
		inited = TRUE;	\
	}	\
	return klass;	\
}

GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL (safehandle)

#ifndef DISABLE_COM

GENERATE_GET_CLASS_WITH_CACHE_DECL (interop_proxy)
GENERATE_GET_CLASS_WITH_CACHE_DECL (idispatch)
GENERATE_GET_CLASS_WITH_CACHE_DECL (iunknown)
GENERATE_GET_CLASS_WITH_CACHE_DECL (com_object)
GENERATE_GET_CLASS_WITH_CACHE_DECL (variant)

#endif

extern MonoDefaults mono_defaults;

void
mono_loader_init           (void);

void
mono_loader_cleanup        (void);

void
mono_loader_lock           (void) MONO_LLVM_INTERNAL;

void
mono_loader_unlock         (void) MONO_LLVM_INTERNAL;

void
mono_loader_lock_track_ownership (gboolean track);

gboolean
mono_loader_lock_is_owned_by_self (void);

void
mono_loader_lock_if_inited (void);

void
mono_loader_unlock_if_inited (void);

void
mono_reflection_init       (void);

void
mono_icall_init            (void);

void
mono_icall_cleanup         (void);

gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id);

gboolean
mono_metadata_has_generic_params (MonoImage *image, guint32 token);

MONO_API MonoGenericContainer *
mono_metadata_load_generic_params (MonoImage *image, guint32 token,
				   MonoGenericContainer *parent_container);

MONO_API gboolean
mono_metadata_load_generic_param_constraints_checked (MonoImage *image, guint32 token,
					      MonoGenericContainer *container, MonoError *error);

MonoMethodSignature*
mono_create_icall_signature (const char *sigstr);

MonoJitICallInfo *
mono_register_jit_icall (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save);

MonoJitICallInfo *
mono_register_jit_icall_full (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save, gboolean no_raise, const char *c_symbol);

void
mono_register_jit_icall_wrapper (MonoJitICallInfo *info, gconstpointer wrapper);

MonoJitICallInfo *
mono_find_jit_icall_by_name (const char *name) MONO_LLVM_INTERNAL;

MonoJitICallInfo *
mono_find_jit_icall_by_addr (gconstpointer addr) MONO_LLVM_INTERNAL;

GHashTable*
mono_get_jit_icall_info (void);

const char*
mono_lookup_jit_icall_symbol (const char *name);

gboolean
mono_class_set_failure (MonoClass *klass, guint32 ex_type, void *ex_data);

gpointer
mono_class_get_exception_data (MonoClass *klass);

MonoException*
mono_class_get_exception_for_failure (MonoClass *klass);

char*
mono_type_get_name_full (MonoType *type, MonoTypeNameFormat format);

char*
mono_type_get_full_name (MonoClass *klass);

char *
mono_method_get_name_full (MonoMethod *method, gboolean signature, gboolean ret, MonoTypeNameFormat format);

char *
mono_method_get_full_name (MonoMethod *method);

MonoArrayType *mono_dup_array_type (MonoImage *image, MonoArrayType *a);
MonoMethodSignature *mono_metadata_signature_deep_dup (MonoImage *image, MonoMethodSignature *sig);

MONO_API void
mono_image_init_name_cache (MonoImage *image);

gboolean mono_class_is_nullable (MonoClass *klass);
MonoClass *mono_class_get_nullable_param (MonoClass *klass);

/* object debugging functions, for use inside gdb */
MONO_API void mono_object_describe        (MonoObject *obj);
MONO_API void mono_object_describe_fields (MonoObject *obj);
MONO_API void mono_value_describe_fields  (MonoClass* klass, const char* addr);
MONO_API void mono_class_describe_statics (MonoClass* klass);

/* method debugging functions, for use inside gdb */
MONO_API void mono_method_print_code (MonoMethod *method);

char *mono_signature_full_name (MonoMethodSignature *sig);

/*Enum validation related functions*/
MONO_API gboolean
mono_type_is_valid_enum_basetype (MonoType * type);

MONO_API gboolean
mono_class_is_valid_enum (MonoClass *klass);

MonoType *
mono_type_get_checked        (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error);

gboolean
mono_generic_class_is_generic_type_definition (MonoGenericClass *gklass);

MonoMethod*
mono_class_get_method_generic (MonoClass *klass, MonoMethod *method);

MonoType*
mono_type_get_basic_type_from_generic (MonoType *type);

gboolean
mono_method_can_access_method_full (MonoMethod *method, MonoMethod *called, MonoClass *context_klass);

gboolean
mono_method_can_access_field_full (MonoMethod *method, MonoClassField *field, MonoClass *context_klass);

gboolean
mono_class_can_access_class (MonoClass *access_class, MonoClass *target_class);

MonoClass *
mono_class_get_generic_type_definition (MonoClass *klass);

gboolean
mono_class_has_parent_and_ignore_generics (MonoClass *klass, MonoClass *parent);

int
mono_method_get_vtable_slot (MonoMethod *method);

int
mono_method_get_vtable_index (MonoMethod *method);

MonoMethod*
mono_method_search_in_array_class (MonoClass *klass, const char *name, MonoMethodSignature *sig);

void
mono_class_setup_interface_id (MonoClass *klass);

MonoGenericContainer*
mono_class_get_generic_container (MonoClass *klass);

MonoGenericClass*
mono_class_get_generic_class (MonoClass *klass);

void
mono_class_alloc_ext (MonoClass *klass);

void
mono_class_setup_interfaces (MonoClass *klass, MonoError *error);

MonoClassField*
mono_class_get_field_from_name_full (MonoClass *klass, const char *name, MonoType *type);

MonoVTable*
mono_class_vtable_full (MonoDomain *domain, MonoClass *klass, MonoError *error);

gboolean
mono_class_is_assignable_from_slow (MonoClass *target, MonoClass *candidate);

gboolean
mono_class_has_variant_generic_params (MonoClass *klass);

gboolean
mono_class_is_variant_compatible (MonoClass *klass, MonoClass *oklass, gboolean check_for_reference_conv);

gboolean mono_is_corlib_image (MonoImage *image);

MonoType*
mono_field_get_type_checked (MonoClassField *field, MonoError *error);

MonoClassField*
mono_class_get_fields_lazy (MonoClass* klass, gpointer *iter);

gboolean
mono_class_check_vtable_constraints (MonoClass *klass, GList *in_setup);

gboolean
mono_class_has_finalizer (MonoClass *klass);

void
mono_unload_interface_id (MonoClass *klass);

GPtrArray*
mono_class_get_methods_by_name (MonoClass *klass, const char *name, guint32 bflags, gboolean ignore_case, gboolean allow_ctors, MonoError *error);

char*
mono_class_full_name (MonoClass *klass);

MonoClass*
mono_class_inflate_generic_class_checked (MonoClass *gklass, MonoGenericContext *context, MonoError *error);

MonoClass *
mono_class_get_checked (MonoImage *image, guint32 type_token, MonoError *error);

MonoClass *
mono_class_get_and_inflate_typespec_checked (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error);

MonoClass *
mono_class_from_name_checked (MonoImage *image, const char* name_space, const char *name, MonoError *error);

MonoClass *
mono_class_from_name_case_checked (MonoImage *image, const char* name_space, const char *name, MonoError *error);

MonoClassField*
mono_field_from_token_checked (MonoImage *image, uint32_t token, MonoClass **retklass, MonoGenericContext *context, MonoError *error);

gpointer
mono_ldtoken_checked (MonoImage *image, guint32 token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

MonoClass *
mono_class_from_generic_parameter_internal (MonoGenericParam *param);

MonoImage *
get_image_for_generic_param (MonoGenericParam *param);

char *
make_generic_name_string (MonoImage *image, int num);

MonoClass *
mono_class_load_from_name (MonoImage *image, const char* name_space, const char *name) MONO_LLVM_INTERNAL;

MonoClass*
mono_class_try_load_from_name (MonoImage *image, const char* name_space, const char *name);

void
mono_error_set_for_class_failure (MonoError *orerror, MonoClass *klass);

static inline guint8
mono_class_get_failure (MonoClass *klass)
{
	g_assert (klass != NULL);
	return klass->exception_type;
}

static inline gboolean
mono_class_has_failure (MonoClass *klass)
{
	g_assert (klass != NULL);
	return mono_class_get_failure (klass) != MONO_EXCEPTION_NONE;
}

#endif /* __MONO_METADATA_CLASS_INTERNALS_H__ */
