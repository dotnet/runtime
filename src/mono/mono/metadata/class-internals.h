/**
 * \file
 * Copyright 2012 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_CLASS_INTERNALS_H__
#define __MONO_METADATA_CLASS_INTERNALS_H__

#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/property-bag.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-error.h"
#include "mono/sgen/gc-internal-agnostic.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/mono-compiler.h"

#define MONO_CLASS_IS_ARRAY(c) (m_class_get_rank (c))

#define MONO_CLASS_HAS_STATIC_METADATA(klass) (m_class_get_type_token (klass) && !m_class_get_image (klass)->dynamic && !mono_class_is_ginst (klass))

#define MONO_DEFAULT_SUPERTABLE_SIZE 6

extern gboolean mono_print_vtable;
extern gboolean mono_align_small_structs;
extern gint32 mono_simd_register_size;

typedef struct _MonoMethodWrapper MonoMethodWrapper;
typedef struct _MonoMethodInflated MonoMethodInflated;
typedef struct _MonoMethodPInvoke MonoMethodPInvoke;
typedef struct _MonoDynamicMethod MonoDynamicMethod;

/* Properties that applies to a group of structs should better use a higher number
 * to avoid colision with type specific properties.
 * 
 * This prop applies to class, method, property, event, assembly and image.
 */
#define MONO_PROP_DYNAMIC_CATTR 0x1000

typedef enum {
#define WRAPPER(e,n) MONO_WRAPPER_ ## e,
#include "wrapper-types.h"
#undef WRAPPER
	MONO_WRAPPER_NUM
} MonoWrapperType;

#define MONO_METHOD_PROP_GENERIC_CONTAINER 0
/* verification success bit, protected by the image lock */
#define MONO_METHOD_PROP_VERIFICATION_SUCCESS 1
/* infrequent vtable layout bits protected by the loader lock */
#define MONO_METHOD_PROP_INFREQUENT_BITS 2

/* Infrequently accessed bits of method definitions stored in the image properties.
 * The method must not be inflated.
 *
 * LOCKING: Reading the bits acquires the image lock.  Writing the bits assumes
 * the loader lock is held.
 */
typedef struct _MonoMethodDefInfrequentBits {
	unsigned int is_reabstracted:1;  /* whenever this is a reabstraction of another interface */
	unsigned int is_covariant_override_impl:1; /* whether this is an override with a signature different from its declared method */
} MonoMethodDefInfrequentBits;

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
	unsigned int _unused : 2; /* unused */
	signed int slot : 16;

	/*
	 * If is_generic is TRUE, the generic_container is stored in image->property_hash, 
	 * using the key MONO_METHOD_PROP_GENERIC_CONTAINER.
	 */
};

struct _MonoMethodWrapper {
	MonoMethod method;
	MonoMethodHeader *header;
	MonoMemoryManager *mem_manager;
	void *method_data;
};

struct _MonoDynamicMethod {
	MonoMethodWrapper method;
	MonoAssembly *assembly;
};

struct _MonoMethodPInvoke {
	MonoMethod method;
	gpointer addr;
	/* add marshal info */
	union {
		guint16 piflags;  /* pinvoke flags */
		guint16 icflags;  /* icall flags */
	};
	guint32 implmap_idx;  /* index into IMPLMAP */
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
	/* Do not access directly, use m_field_get_parent */
	/* We use the lowest bits of the pointer to store some flags, see m_field_get_meta_flags */
	uintptr_t	parent_and_flags;

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
	MonoPropertyBagItem head;

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
typedef enum {
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
} MonoExceptionType;

typedef struct {
	MonoPropertyBagItem head;

	MonoProperty *properties;
	guint32 first, count;
	MonoFieldDefaultValue *def_values;
} MonoClassPropertyInfo;

typedef struct {
	MonoPropertyBagItem head;

	/* Initialized by a call to mono_class_setup_events () */
	MonoEvent *events;
	guint32 first, count;
} MonoClassEventInfo;

typedef enum {
	MONO_CLASS_DEF = 1, /* non-generic type */
	MONO_CLASS_GTD, /* generic type definition */
	MONO_CLASS_GINST, /* generic instantiation */
	MONO_CLASS_GPARAM, /* generic parameter */
	MONO_CLASS_ARRAY, /* vector or array, bounded or not */
	MONO_CLASS_POINTER, /* pointer or function pointer*/
	MONO_CLASS_GC_FILLER = 0xAC /* not a real class kind - used for sgen nursery filler arrays */
} MonoTypeKind;

typedef struct _MonoClassDef MonoClassDef;
typedef struct _MonoClassGtd MonoClassGtd;
typedef struct _MonoClassGenericInst MonoClassGenericInst;
typedef struct _MonoClassGenericParam MonoClassGenericParam;
typedef struct _MonoClassArray MonoClassArray;
typedef struct _MonoClassPointer MonoClassPointer;

union _MonoClassSizes {
		int class_size; /* size of area for static fields */
		int element_size; /* for array types */
		int generic_param_token; /* for generic param types, both var and mvar */
};

/* enabled only with small config for now: we might want to do it unconditionally */
#ifdef MONO_SMALL_CONFIG
#define COMPRESSED_INTERFACE_BITMAP 1
#endif


#ifdef ENABLE_CHECKED_BUILD_PRIVATE_TYPES
#define MONO_CLASS_DEF_PRIVATE 1
#endif

/* Hide _MonoClass definition in checked build mode to ensure that
 * it is only accessed via getter and setter methods.
 */
#ifndef MONO_CLASS_DEF_PRIVATE
#include "class-private-definition.h"
#endif

/* If MonoClass definition is hidden, just declare the getters.
 * Otherwise, define them as static inline functions.
 *
 * In-tree profilers are allowed to use the getters.  So if we're compiling
 * with --enable-checked-build=private_types, mark the symbols with
 * MONO_PROFILER_API
 */
#ifdef MONO_CLASS_DEF_PRIVATE
#define MONO_CLASS_GETTER(funcname, rettype, optref, argtype, fieldname) MONO_PROFILER_API rettype funcname (argtype *klass);
#else
#define MONO_CLASS_GETTER(funcname, rettype, optref, argtype, fieldname) static inline rettype funcname (argtype *klass) { return optref klass-> fieldname ; }
#endif
#define MONO_CLASS_OFFSET(funcname, argtype, fieldname) /*nothing*/
#include "class-getters.h"
#undef MONO_CLASS_GETTER
#undef MONO_CLASS_OFFSET

#ifdef COMPRESSED_INTERFACE_BITMAP
int mono_compress_bitmap (uint8_t *dest, const uint8_t *bitmap, int size);
int mono_class_interface_match (const uint8_t *bitmap, int id);
#else
#define mono_class_interface_match(bmap,uiid) ((bmap) [(uiid) >> 3] & (1 << ((uiid)&7)))
#endif

#define MONO_CLASS_IMPLEMENTS_INTERFACE(k,uiid) (((uiid) <= m_class_get_max_interface_id (k)) && mono_class_interface_match (m_class_get_interface_bitmap (k), (uiid)))

#define MONO_VTABLE_AVAILABLE_GC_BITS 4

#ifdef DISABLE_COM
#define mono_class_is_com_object(klass) (FALSE)
#else
#define mono_class_is_com_object(klass) (m_class_is_com_object (klass))
#endif


MONO_API int mono_class_interface_offset (MonoClass *klass, MonoClass *itf);
MONO_COMPONENT_API int mono_class_interface_offset_with_variance (MonoClass *klass, MonoClass *itf, gboolean *non_exact_match);

typedef gpointer MonoRuntimeGenericContext;

typedef enum {
	/* array or string */
	MONO_VT_FLAG_ARRAY_OR_STRING = (1 << 0),
	MONO_VT_FLAG_HAS_REFERENCES = (1 << 1),
	MONO_VT_FLAG_ARRAY_IS_PRIMITIVE = (1 << 2),
} MonoVTableFlags;

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
	guint32     max_interface_id;
	guint8      rank;
	/* Keep this a guint8, the jit depends on it */
	guint8      initialized; /* cctor has been run */
	/* Keep this a guint8, the jit depends on it */
	guint8      flags; /* MonoVTableFlags */
	guint init_failed     : 1; /* cctor execution failed */
	guint has_static_fields : 1; /* pointer to the data stored at the end of the vtable array */
	guint gc_bits         : MONO_VTABLE_AVAILABLE_GC_BITS; /* Those bits are reserved for the usaged of the GC */

	guint32     imt_collisions_bitmap;
	MonoRuntimeGenericContext *runtime_generic_context;
	/* Maintained by the Execution Engine */
	gpointer ee_data;
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
	gint32 id;			/* unique ID for debugging */
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
		MonoMethodWrapper wrapper;
	} method;
	MonoMethod *declaring;		/* the generic method definition. */
	MonoGenericContext context;	/* The current instantiation */
	MonoMemoryManager *owner; /* The mem manager that the inflated method belongs to. */
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

	/* The mem manager which owns this generic class. */
	MonoMemoryManager *owner;
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

	MonoGenericParamInfo info;
};

typedef MonoGenericParam MonoGenericParamFull;

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
	   2. When user code creates a generic parameter via SRE, but has not yet set an owner. */
	int is_anonymous : 1;
	/* Our type parameters. If this is a special anonymous container (case 1, above), this field is not valid, use mono_metadata_create_anon_gparam ()  */
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

static inline MonoGenericParamInfo *
mono_generic_param_info (MonoGenericParam *p)
{
	return &((MonoGenericParamFull *) p)->info;
}

static inline const char *
mono_generic_param_name (MonoGenericParam *p)
{
	return ((MonoGenericParamFull *) p)->info.name;
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
	guint has_weak_fields : 1;
	guint32 cctor_token;
	MonoImage *finalize_image;
	guint32 finalize_token;
	guint32 instance_size;
	guint32 class_size;
	guint32 packing_size;
	guint32 min_align;
} MonoCachedClassInfo;

typedef struct {
	// Name and func fields double as "inited".
	// That is, any initialized MonoJitICallInfo must
	// have both of them to be non-NULL.
	const char *name;
	gconstpointer func;
	gconstpointer wrapper;
	gconstpointer trampoline;
	MonoMethodSignature *sig;
	const char *c_symbol;
	MonoMethod *wrapper_method;
} MonoJitICallInfo;

MONO_COMPONENT_API void
mono_class_setup_supertypes (MonoClass *klass);

/* WARNING
 * Only call this function if you can ensure both @klass and @parent
 * have supertype information initialized.
 * This can be accomplished by mono_class_setup_supertypes or mono_class_init.
 * If unsure, use mono_class_has_parent.
 */
static inline gboolean
mono_class_has_parent_fast (MonoClass *klass, MonoClass *parent)
{
	return (m_class_get_idepth (klass) >= m_class_get_idepth (parent)) && (m_class_get_supertypes (klass) [m_class_get_idepth (parent) - 1] == parent);
}

static inline gboolean
mono_class_has_parent (MonoClass *klass, MonoClass *parent)
{
	if (G_UNLIKELY (!m_class_get_supertypes (klass)))
		mono_class_setup_supertypes (klass);

	if (G_UNLIKELY (!m_class_get_supertypes (parent)))
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
	gint32 initialized_class_count;
	gint32 generic_vtable_count;
	gint32 used_class_count;
	gint32 method_count;
	gint32 class_vtable_size;
	gint32 class_static_data_size;
	gint32 generic_class_count;
	gint32 inflated_method_count;
	gint32 inflated_type_count;
	gint32 delegate_creations;
	gint32 imt_tables_size;
	gint32 imt_number_of_tables;
	gint32 imt_number_of_methods;
	gint32 imt_used_slots;
	gint32 imt_slots_with_collisions;
	gint32 imt_max_collisions_in_slot;
	gint32 imt_method_count_when_max_collisions;
	gint32 imt_trampolines_size;
	gint32 jit_info_table_insert_count;
	gint32 jit_info_table_remove_count;
	gint32 jit_info_table_lookup_count;
	gint32 generics_sharable_methods;
	gint32 generics_unsharable_methods;
	gint32 generics_shared_methods;
	gint32 gsharedvt_methods;
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
	gint32 jit_methods;
	gint32 jit_bytes;
	gint32 jit_time;
	gint32 jit_failures;
	/* Exceptions category */
	gint32 exceptions_thrown;
	gint32 exceptions_filters;
	gint32 exceptions_finallys;
	gint32 exceptions_depth;
	gint32 aspnet_requests_queued;
	gint32 aspnet_requests;
	/* Memory category */
	gint32 gc_collections0;
	gint32 gc_collections1;
	gint32 gc_collections2;
	gint32 gc_promotions0;
	gint32 gc_promotions1;
	gint32 gc_promotion_finalizers;
	gint64 gc_gen0size;
	gint64 gc_gen1size;
	gint64 gc_gen2size;
	gint32 gc_lossize;
	gint32 gc_fin_survivors;
	gint32 gc_num_handles;
	gint32 gc_allocated;
	gint32 gc_induced;
	gint32 gc_time;
	gint64 gc_total_bytes;
	gint64 gc_committed_bytes;
	gint64 gc_reserved_bytes;
	gint32 gc_num_pinned;
	gint32 gc_sync_blocks;
	/* Loader category */
	gint32 loader_classes;
	gint32 loader_total_classes;
	gint32 loader_appdomains;
	gint32 loader_total_appdomains;
	gint32 loader_assemblies;
	gint32 loader_total_assemblies;
	gint32 loader_failures;
	gint32 loader_bytes;
	gint32 loader_appdomains_uloaded;
	/* Threads and Locks category  */
	gint32 thread_contentions;
	gint32 thread_queue_len;
	gint32 thread_queue_max;
	gint32 thread_num_logical;
	gint32 thread_num_physical;
	gint32 thread_cur_recognized;
	gint32 thread_num_recognized;
	/* Interop category */
	gint32 interop_num_ccw;
	gint32 interop_num_stubs;
	gint32 interop_num_marshals;
	/* Security category */
	gint32 security_num_checks;
	gint32 security_num_link_checks;
	gint32 security_time;
	gint32 security_depth;
	gint32 unused;
	/* Threadpool */
	gint32 threadpool_threads;
	gint64 threadpool_workitems;
	gint64 threadpool_ioworkitems;
	gint32 threadpool_iothreads;
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

MonoMethod*
mono_class_get_method_by_index (MonoClass *klass, int index);

MonoMethod*
mono_class_get_inflated_method (MonoClass *klass, MonoMethod *method, MonoError *error);

MonoMethod*
mono_class_get_vtable_entry (MonoClass *klass, int offset);

GPtrArray*
mono_class_get_implemented_interfaces (MonoClass *klass, MonoError *error);

int
mono_class_get_vtable_size (MonoClass *klass);

MONO_COMPONENT_API gboolean
mono_class_is_open_constructed_type (MonoType *t);

void
mono_class_get_overrides_full (MonoImage *image, guint32 type_token, MonoMethod ***overrides, gint32 *num_overrides, MonoGenericContext *generic_context, MonoError *error);

MonoMethod*
mono_class_get_cctor (MonoClass *klass);

MonoMethod*
mono_class_get_finalizer (MonoClass *klass);

gboolean
mono_class_needs_cctor_run (MonoClass *klass, MonoMethod *caller);

MONO_COMPONENT_API gboolean
mono_class_field_is_special_static (MonoClassField *field);

MONO_COMPONENT_API guint32
mono_class_field_get_special_static_type (MonoClassField *field);

gboolean
mono_class_has_special_static_fields (MonoClass *klass);

const char*
mono_class_get_field_default_value (MonoClassField *field, MonoTypeEnum *def_type);

MONO_COMPONENT_API MonoProperty* 
mono_class_get_property_from_name_internal (MonoClass *klass, const char *name);

const char*
mono_class_get_property_default_value (MonoProperty *property, MonoTypeEnum *def_type);

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token, MonoGenericContext *context, MonoError *error);

gpointer
mono_lookup_dynamic_token_class (MonoImage *image, guint32 token, gboolean check_token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

gpointer
mono_runtime_create_delegate_trampoline (MonoClass *klass);

void
mono_install_get_cached_class_info (MonoGetCachedClassInfo func);

void
mono_install_get_class_from_name (MonoGetClassFromName func);

MONO_PROFILER_API MonoGenericContext*
mono_class_get_context (MonoClass *klass);

MONO_PROFILER_API MonoMethodSignature*
mono_method_signature_checked_slow (MonoMethod *m, MonoError *err);

MONO_PROFILER_API MonoMethodSignature*
mono_method_signature_internal_slow (MonoMethod *m);

/**
 * mono_method_signature_checked:
 *
 * Return the signature of the method M. On failure, returns NULL, and ERR is set.
 */
static inline MonoMethodSignature*
mono_method_signature_checked (MonoMethod *m, MonoError *error)
{
	error_init (error);
	MonoMethodSignature* sig = m->signature;
	return sig ? sig : mono_method_signature_checked_slow (m, error);
}

/**
 * mono_method_signature_internal:
 * \returns the signature of the method \p m. On failure, returns NULL.
 */
static inline MonoMethodSignature*
mono_method_signature_internal (MonoMethod *m)
{
	MonoMethodSignature* sig = m->signature;
	return sig ? sig : mono_method_signature_internal_slow (m);
}

MonoGenericContext*
mono_method_get_context_general (MonoMethod *method, gboolean uninflated);

MONO_PROFILER_API MonoGenericContext*
mono_method_get_context (MonoMethod *method);

/* Used by monodis, thus cannot be MONO_INTERNAL */
MONO_API MonoGenericContainer*
mono_method_get_generic_container (MonoMethod *method);

MonoGenericContext*
mono_generic_class_get_context (MonoGenericClass *gclass);

void
mono_method_set_generic_container (MonoMethod *method, MonoGenericContainer* container);

void
mono_method_set_verification_success (MonoMethod *method);

gboolean
mono_method_get_verification_success (MonoMethod *method);

const MonoMethodDefInfrequentBits *
mono_method_lookup_infrequent_bits (MonoMethod *methoddef);

MonoMethodDefInfrequentBits *
mono_method_get_infrequent_bits (MonoMethod *methoddef);

gboolean
mono_method_get_is_reabstracted (MonoMethod *method);

void
mono_method_set_is_reabstracted (MonoMethod *methoddef);

gboolean
mono_method_get_is_covariant_override_impl (MonoMethod *method);

void
mono_method_set_is_covariant_override_impl (MonoMethod *methoddef);

MONO_COMPONENT_API MonoMethod*
mono_class_inflate_generic_method_full_checked (MonoMethod *method, MonoClass *klass_hint, MonoGenericContext *context, MonoError *error);

MONO_COMPONENT_API MonoMethod *
mono_class_inflate_generic_method_checked (MonoMethod *method, MonoGenericContext *context, MonoError *error);

MonoMemoryManager *
mono_metadata_get_mem_manager_for_type (MonoType *type);

MonoMemoryManager *
mono_metadata_get_mem_manager_for_class (MonoClass *klass);

MonoMemoryManager*
mono_metadata_get_mem_manager_for_method (MonoMethodInflated *method);

MONO_API MonoMethodSignature *
mono_metadata_get_inflated_signature (MonoMethodSignature *sig, MonoGenericContext *context);

MonoType*
mono_class_inflate_generic_type_with_mempool (MonoImage *image, MonoType *type, MonoGenericContext *context, MonoError *error);

MONO_COMPONENT_API MonoType*
mono_class_inflate_generic_type_checked (MonoType *type, MonoGenericContext *context, MonoError *error);

MONO_API void
mono_metadata_free_inflated_signature (MonoMethodSignature *sig);

MonoMethodSignature*
mono_inflate_generic_signature (MonoMethodSignature *sig, MonoGenericContext *context, MonoError *error);

MonoClass*
mono_generic_param_get_base_type (MonoClass *klass);

typedef struct {
	MonoImage *corlib;
	MonoClass *object_class;
	MonoClass *object_class_array; // used via token pasting in mono_array_class_get_cached
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
	MonoClass *manualresetevent_class;
	MonoClass *typehandle_class;
	MonoClass *fieldhandle_class;
	MonoClass *methodhandle_class;
	MonoClass *systemtype_class;
	MonoClass *runtimetype_class;
	MonoClass *runtimetype_class_array; // used via token pasting in mono_array_class_get_cached
	MonoClass *exception_class;
	MonoClass *threadabortexception_class;
	MonoClass *thread_class;
	MonoClass *internal_thread_class;
	MonoClass *autoreleasepool_class;
	MonoClass *mono_method_message_class;
	MonoClass *field_info_class;
	MonoClass *method_info_class;
	MonoClass *stack_frame_class;
	MonoClass *marshal_class;
	MonoClass *typed_reference_class;
	MonoClass *argumenthandle_class;
	MonoClass *monitor_class;
	MonoClass *generic_ilist_class;
	MonoClass *generic_nullable_class;
	MonoClass *attribute_class;
	MonoClass *attribute_class_array; // used via token pasting in mono_array_class_get_cached
	MonoClass *critical_finalizer_object; /* MAYBE NULL */
	MonoClass *generic_ireadonlylist_class;
	MonoClass *generic_ienumerator_class;
	MonoClass *alc_class;
	MonoClass *appcontext_class;
} MonoDefaults;

#define GENERATE_GET_CLASS_WITH_CACHE_DECL(shortname) \
MonoClass* mono_class_get_##shortname##_class (void);

#define GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL(shortname) \
MonoClass* mono_class_try_get_##shortname##_class (void);

// GENERATE_GET_CLASS_WITH_CACHE attempts mono_class_load_from_name whenever
// its cache is null. i.e. potentially repeatedly, though it is expected to succeed
// the first time.
//
#define GENERATE_GET_CLASS_WITH_CACHE(shortname,name_space,name) \
MonoClass*	\
mono_class_get_##shortname##_class (void)	\
{	\
	static MonoClass *tmp_class;	\
	MonoClass *klass = tmp_class;	\
	if (!klass) {	\
		klass = mono_class_load_from_name (mono_defaults.corlib, name_space, name);	\
		mono_memory_barrier ();	/* FIXME excessive? */ \
		tmp_class = klass;	\
	}	\
	return klass;	\
}

// GENERATE_TRY_GET_CLASS_WITH_CACHE attempts mono_class_load_from_name approximately
// only once. i.e. if it fails, it will return null and not retry.
// In a race it might try a few times, but not indefinitely.
//
// FIXME This maybe has excessive volatile/barriers.
//
#define GENERATE_TRY_GET_CLASS_WITH_CACHE(shortname,name_space,name) \
MonoClass*	\
mono_class_try_get_##shortname##_class (void)	\
{	\
	static volatile MonoClass *tmp_class;	\
	static volatile gboolean inited;	\
	MonoClass *klass = (MonoClass *)tmp_class;	\
	mono_memory_barrier ();	\
	if (!inited) {	\
		klass = mono_class_try_load_from_name (mono_defaults.corlib, name_space, name);	\
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

MonoClass* mono_class_get_appdomain_class (void);

GENERATE_GET_CLASS_WITH_CACHE_DECL (appdomain_unloaded_exception)
GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL (appdomain_unloaded_exception)

GENERATE_GET_CLASS_WITH_CACHE_DECL (valuetype)

GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL(handleref)

GENERATE_GET_CLASS_WITH_CACHE_DECL (assembly_load_context)
GENERATE_GET_CLASS_WITH_CACHE_DECL (native_library)

/* If you need a MonoType, use one of the mono_get_*_type () functions in class-inlines.h */
extern MonoDefaults mono_defaults;

void
mono_loader_init           (void);

void
mono_loader_cleanup        (void);

MONO_COMPONENT_API void
mono_loader_lock           (void);

MONO_COMPONENT_API void
mono_loader_unlock         (void);

MONO_COMPONENT_API void
mono_loader_lock_track_ownership (gboolean track);

MONO_COMPONENT_API gboolean
mono_loader_lock_is_owned_by_self (void);

void
mono_loader_lock_if_inited (void);

void
mono_loader_unlock_if_inited (void);

void
mono_reflection_init       (void);

void
mono_icall_init            (void);

MONO_COMPONENT_API gpointer
mono_method_get_wrapper_data (MonoMethod *method, guint32 id);

gboolean
mono_metadata_has_generic_params (MonoImage *image, guint32 token);

MONO_API MonoGenericContainer *
mono_metadata_load_generic_params (MonoImage *image, guint32 token,
				   MonoGenericContainer *parent_container,
				   gpointer real_owner);

MONO_API gboolean
mono_metadata_load_generic_param_constraints_checked (MonoImage *image, guint32 token,
					      MonoGenericContainer *container, MonoError *error);

// This is the "real" function for registering JIT icalls. All others are one line wrappers that call it,
// i.e. filling in info or c_symbol.
void
mono_register_jit_icall_info (MonoJitICallInfo *info, gconstpointer func, const char *name,
			      MonoMethodSignature *sig, gboolean no_wrapper, const char *c_symbol);

#ifdef __cplusplus
template <typename T>
inline void
mono_register_jit_icall_info (MonoJitICallInfo *info, T func, const char *name, MonoMethodSignature *sig, gboolean no_wrapper, const char *c_symbol)
{
	mono_register_jit_icall_info (info, (gconstpointer)func, name, sig, no_wrapper, c_symbol);
}
#endif // __cplusplus

#define mono_register_jit_icall(func, sig, no_wrapper) (mono_register_jit_icall_info (&mono_get_jit_icall_info ()->func, func, #func, (sig), (no_wrapper), NULL))

gboolean
mono_class_set_type_load_failure (MonoClass *klass, const char * fmt, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

MonoException*
mono_class_get_exception_for_failure (MonoClass *klass);

char*
mono_identifier_escape_type_name_chars (const char* identifier);

char*
mono_type_get_full_name (MonoClass *klass);

MONO_COMPONENT_API
char *
mono_method_get_name_full (MonoMethod *method, gboolean signature, gboolean ret, MonoTypeNameFormat format);

MONO_PROFILER_API char *
mono_method_get_full_name (MonoMethod *method);

const char*
mono_wrapper_type_to_str (guint32 wrapper_type);

MonoArrayType *mono_dup_array_type (MonoImage *image, MonoArrayType *a);
MonoMethodSignature *mono_metadata_signature_deep_dup (MonoImage *image, MonoMethodSignature *sig);

MONO_API void
mono_image_init_name_cache (MonoImage *image);

MonoClass*
mono_class_get_nullable_param_internal (MonoClass *klass);

/* object debugging functions, for use inside gdb */
MONO_API void mono_object_describe        (MonoObject *obj);
MONO_API void mono_object_describe_fields (MonoObject *obj);
MONO_API void mono_value_describe_fields  (MonoClass* klass, const char* addr);
MONO_API void mono_class_describe_statics (MonoClass* klass);

/* method debugging functions, for use inside gdb */
MONO_API void mono_method_print_code (MonoMethod *method);

MONO_PROFILER_API char *mono_signature_full_name (MonoMethodSignature *sig);

/*Enum validation related functions*/
MONO_API gboolean
mono_type_is_valid_enum_basetype (MonoType * type);

MONO_API gboolean
mono_class_is_valid_enum (MonoClass *klass);

MONO_PROFILER_API gboolean
mono_type_is_primitive (MonoType *type);

MonoType *
mono_type_get_checked        (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error);

gboolean
mono_generic_class_is_generic_type_definition (MonoGenericClass *gklass);

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
mono_method_get_base_method (MonoMethod *method, gboolean definition, MonoError *error);

MonoMethod*
mono_method_search_in_array_class (MonoClass *klass, const char *name, MonoMethodSignature *sig);

void
mono_class_setup_interface_id (MonoClass *klass);

MONO_COMPONENT_API MonoGenericContainer*
mono_class_get_generic_container (MonoClass *klass);

gpointer
mono_class_alloc (MonoClass *klass, int size);

gpointer
mono_class_alloc0 (MonoClass *klass, int size);

#define mono_class_alloc0(klass, size) (g_cast (mono_class_alloc0 ((klass), (size))))

MONO_COMPONENT_API void
mono_class_setup_interfaces (MonoClass *klass, MonoError *error);

MONO_COMPONENT_API MonoClassField*
mono_class_get_field_from_name_full (MonoClass *klass, const char *name, MonoType *type);

MONO_COMPONENT_API MonoVTable*
mono_class_vtable_checked (MonoClass *klass, MonoError *error);

void
mono_class_is_assignable_from_checked (MonoClass *klass, MonoClass *oklass, gboolean *result, MonoError *error);

void
mono_class_signature_is_assignable_from (MonoClass *klass, MonoClass *oklass, gboolean *result, MonoError *error);

gboolean
mono_class_is_assignable_from_slow (MonoClass *target, MonoClass *candidate);

gboolean
mono_class_has_variant_generic_params (MonoClass *klass);

gboolean
mono_class_is_variant_compatible (MonoClass *klass, MonoClass *oklass, gboolean check_for_reference_conv);

gboolean 
mono_class_is_subclass_of_internal (MonoClass *klass, MonoClass *klassc, gboolean check_interfaces);

MONO_COMPONENT_API mono_bool
mono_class_is_assignable_from_internal (MonoClass *klass, MonoClass *oklass);

gboolean
mono_byref_type_is_assignable_from (MonoType *type, MonoType *ctype, gboolean signature_assignment);

gboolean mono_is_corlib_image (MonoImage *image);

MonoType*
mono_field_get_type_checked (MonoClassField *field, MonoError *error);

MonoType*
mono_field_get_type_internal (MonoClassField *field);

MONO_COMPONENT_API MonoClassField*
mono_class_get_fields_internal (MonoClass* klass, gpointer *iter);

MonoClassField*
mono_class_get_fields_lazy (MonoClass* klass, gpointer *iter);

gboolean
mono_class_check_vtable_constraints (MonoClass *klass, GList *in_setup);

gboolean
mono_class_has_finalizer (MonoClass *klass);

void
mono_unload_interface_id (MonoClass *klass);

MONO_COMPONENT_API GPtrArray*
mono_class_get_methods_by_name (MonoClass *klass, const char *name, guint32 bflags, guint32 mlisttype, gboolean allow_ctors, MonoError *error);

char*
mono_class_full_name (MonoClass *klass);

MonoClass*
mono_class_inflate_generic_class_checked (MonoClass *gklass, MonoGenericContext *context, MonoError *error);

MONO_PROFILER_API MonoClass *
mono_class_get_checked (MonoImage *image, guint32 type_token, MonoError *error);

MonoClass *
mono_class_get_and_inflate_typespec_checked (MonoImage *image, guint32 type_token, MonoGenericContext *context, MonoError *error);

MONO_COMPONENT_API MonoClass *
mono_class_from_name_checked (MonoImage *image, const char* name_space, const char *name, MonoError *error);

MonoClass *
mono_class_from_name_case_checked (MonoImage *image, const char* name_space, const char *name, MonoError *error);

MONO_PROFILER_API MonoClass *
mono_class_from_mono_type_internal (MonoType *type);

MONO_COMPONENT_API MonoClassField*
mono_field_from_token_checked (MonoImage *image, uint32_t token, MonoClass **retklass, MonoGenericContext *context, MonoError *error);

MONO_COMPONENT_API gpointer
mono_ldtoken_checked (MonoImage *image, guint32 token, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

MonoImage *
mono_get_image_for_generic_param (MonoGenericParam *param);

char *
mono_make_generic_name_string (MonoImage *image, int num);

MONO_COMPONENT_API MonoClass *
mono_class_load_from_name (MonoImage *image, const char* name_space, const char *name);

MONO_COMPONENT_API MonoClass*
mono_class_try_load_from_name (MonoImage *image, const char* name_space, const char *name);

void
mono_error_set_for_class_failure (MonoError *orerror, const MonoClass *klass);

gboolean
mono_class_has_failure (const MonoClass *klass);

/* Kind specific accessors */
MONO_COMPONENT_API MonoGenericClass*
mono_class_get_generic_class (MonoClass *klass);

MonoGenericClass*
mono_class_try_get_generic_class (MonoClass *klass);

void
mono_class_set_flags (MonoClass *klass, guint32 flags);

MonoGenericContainer*
mono_class_try_get_generic_container (MonoClass *klass);

void
mono_class_set_generic_container (MonoClass *klass, MonoGenericContainer *container);

MonoType*
mono_class_gtd_get_canonical_inst (MonoClass *klass);

guint32
mono_class_get_first_method_idx (MonoClass *klass);

void
mono_class_set_first_method_idx (MonoClass *klass, guint32 idx);

guint32
mono_class_get_first_field_idx (MonoClass *klass);

void
mono_class_set_first_field_idx (MonoClass *klass, guint32 idx);

guint32
mono_class_get_method_count (MonoClass *klass);

void
mono_class_set_method_count (MonoClass *klass, guint32 count);

guint32
mono_class_get_field_count (MonoClass *klass);

void
mono_class_set_field_count (MonoClass *klass, guint32 count);

MonoMarshalType*
mono_class_get_marshal_info (MonoClass *klass);

void
mono_class_set_marshal_info (MonoClass *klass, MonoMarshalType *marshal_info);

MonoGCHandle
mono_class_get_ref_info_handle (MonoClass *klass);

MonoGCHandle
mono_class_set_ref_info_handle (MonoClass *klass, gpointer value);

MonoErrorBoxed*
mono_class_get_exception_data (MonoClass *klass);

void
mono_class_set_exception_data (MonoClass *klass, MonoErrorBoxed *value);

GList*
mono_class_get_nested_classes_property (MonoClass *klass);

void
mono_class_set_nested_classes_property (MonoClass *klass, GList *value);

MonoClassPropertyInfo*
mono_class_get_property_info (MonoClass *klass);

void
mono_class_set_property_info (MonoClass *klass, MonoClassPropertyInfo *info);

MonoClassEventInfo*
mono_class_get_event_info (MonoClass *klass);

void
mono_class_set_event_info (MonoClass *klass, MonoClassEventInfo *info);

MonoFieldDefaultValue*
mono_class_get_field_def_values (MonoClass *klass);

MonoFieldDefaultValue*
mono_class_get_field_def_values_with_swizzle (MonoClass *klass, int swizzle);

void
mono_class_set_field_def_values (MonoClass *klass, MonoFieldDefaultValue *values);

void
mono_class_set_field_def_values_with_swizzle (MonoClass *klass, MonoFieldDefaultValue *values, int swizzle);

guint32
mono_class_get_declsec_flags (MonoClass *klass);

void
mono_class_set_declsec_flags (MonoClass *klass, guint32 value);

void
mono_class_set_is_com_object (MonoClass *klass);

void
mono_class_set_weak_bitmap (MonoClass *klass, int nbits, gsize *bits);

gsize*
mono_class_get_weak_bitmap (MonoClass *klass, int *nbits);

gboolean
mono_class_has_dim_conflicts (MonoClass *klass);

void
mono_class_set_dim_conflicts (MonoClass *klass, GSList *conflicts);

GSList*
mono_class_get_dim_conflicts (MonoClass *klass);

MONO_COMPONENT_API MonoMethod *
mono_class_get_method_from_name_checked (MonoClass *klass, const char *name, int param_count, int flags, MonoError *error);

MONO_COMPONENT_API gboolean
mono_method_has_no_body (MonoMethod *method);

// FIXME Replace all internal callers of mono_method_get_header_checked with
// mono_method_get_header_internal; the difference is in error initialization.
//
// And then mark mono_method_get_header_checked as MONO_RT_EXTERNAL_ONLY MONO_API.
//
// Internal callers expected to use ERROR_DECL. External callers are not.
MONO_COMPONENT_API MonoMethodHeader*
mono_method_get_header_internal (MonoMethod *method, MonoError *error);

MonoType*
mono_class_find_enum_basetype (MonoClass *klass, MonoError *error);

gboolean
mono_class_set_failure (MonoClass *klass, MonoErrorBoxed *boxed_error);

gboolean
mono_class_set_type_load_failure_causedby_class (MonoClass *klass, const MonoClass *caused_by, const gchar* msg);

gboolean mono_class_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res);

MonoMethod* mono_find_method_in_metadata (MonoClass *klass, const char *name, int param_count, int flags);

int
mono_class_get_object_finalize_slot (void);

MonoMethod *
mono_class_get_default_finalize_method (void);

const char *
mono_field_get_rva (MonoClassField *field, int swizzle);

void
mono_field_resolve_type (MonoClassField *field, MonoError *error);

gboolean
mono_type_has_exceptions (MonoType *type);

void
mono_class_set_nonblittable (MonoClass *klass);

gboolean
mono_class_publish_gc_descriptor (MonoClass *klass, MonoGCDescriptor gc_descr);

void
mono_class_compute_gc_descriptor (MonoClass *klass);

gboolean
mono_class_init_checked (MonoClass *klass, MonoError *error);

MonoType*
mono_class_enum_basetype_internal (MonoClass *klass);

gboolean
mono_method_is_constructor (MonoMethod *method);

gboolean
mono_class_has_default_constructor (MonoClass *klass, gboolean public_only);

gboolean
mono_method_has_unmanaged_callers_only_attribute (MonoMethod *method);

// There are many ways to do on-demand initialization.
//   Some allow multiple concurrent initializations. Some do not.
//   Some allow multiple concurrent writes to the global. Some do not.
//
// Booleans or names capturing these factors would be desirable.
//	RacyInit?
//
// This form allows both such races, on the understanding that,
// even if the initialization occurs multiple times, every result is equivalent,
// and the goal is not to initialize no more than once, but for the steady state
// to stop rerunning the initialization.
//
// It may be desirable to replace this with mono_lazy_initialize, etc.
//
// These macros cannot be wrapped in do/while as they inject "name" into invoking scope.
//
#define MONO_STATIC_POINTER_INIT(type, name)					\
	static type *static_ ## name;						\
	type *name; 								\
	name = static_ ## name;							\
	if (!name) {								\
		/* Custom code here to initialize name */
#define MONO_STATIC_POINTER_INIT_END(type, name)				\
		if (name) {							\
			/* Success, commit to static. */			\
			mono_atomic_store_seq (&static_ ## name, name);		\
		}								\
	}									\

/* Metadata flags for MonoClassField.  These are stored in the lowest bits of a pointer, so there
 * can't be too many. */
enum {
	/* This MonoClassField was added by EnC metadata update, it's not part of the
	 * MonoClass:fields array, and at runtime it is not stored like ordinary instance or static
	 * fields. */
	MONO_CLASS_FIELD_META_FLAG_FROM_UPDATE = 0x01u,

	/* Lowest 2 bits of a pointer reserved for flags */
	MONO_CLASS_FIELD_META_FLAG_MASK = 0x03u,
};

static inline MonoClass *
m_field_get_parent (MonoClassField *field)
{
	return (MonoClass*)(field->parent_and_flags & ~MONO_CLASS_FIELD_META_FLAG_MASK);
}

static inline unsigned int
m_field_get_meta_flags (MonoClassField *field)
{
	return (unsigned int)(field->parent_and_flags & MONO_CLASS_FIELD_META_FLAG_MASK);
}

void
m_field_set_parent (MonoClassField *field, MonoClass *klass);

void
m_field_set_meta_flags (MonoClassField *field, unsigned int flags);

static inline gboolean
m_field_get_offset (MonoClassField *field)
{
	g_assert (m_class_is_fields_inited (m_field_get_parent (field)));
	return field->offset;
}

static inline gboolean
m_field_is_from_update (MonoClassField *field)
{
	return (m_field_get_meta_flags (field) & MONO_CLASS_FIELD_META_FLAG_FROM_UPDATE) != 0;
}

/*
 * Memory allocation for images/classes/methods
 *
 *   These should be used to allocate memory whose lifetime is equal to
 * the lifetime of the image/class/method.
 */

static inline MonoMemoryManager*
mono_mem_manager_get_ambient (void)
{
	// FIXME: All callers should get a MemoryManager from their callers or context
	return (MonoMemoryManager *)mono_alc_get_default ()->memory_manager;
}

static inline MonoMemoryManager*
m_image_get_mem_manager (MonoImage *image)
{
	MonoAssemblyLoadContext *alc = mono_image_get_alc (image);
	if (!alc)
		alc = mono_alc_get_default ();
	return alc->memory_manager;
}

static inline void *
m_image_alloc (MonoImage *image, guint size)
{
	return mono_mem_manager_alloc (m_image_get_mem_manager (image), size);
}

static inline void *
m_image_alloc0 (MonoImage *image, guint size)
{
	return mono_mem_manager_alloc0 (m_image_get_mem_manager (image), size);
}

static inline MonoMemoryManager*
m_class_get_mem_manager (MonoClass *klass)
{
	if (m_class_get_class_kind (klass) == MONO_CLASS_GINST)
		return mono_class_get_generic_class (klass)->owner;
	if (m_class_get_rank (klass))
		return m_class_get_mem_manager (m_class_get_element_class (klass));
	MonoAssemblyLoadContext *alc = mono_image_get_alc (m_class_get_image (klass));
	if (alc)
		return alc->memory_manager;
	else
		/* Dynamic assemblies */
		return mono_mem_manager_get_ambient ();
}

static inline void *
m_class_alloc (MonoClass *klass, guint size)
{
	return mono_mem_manager_alloc (m_class_get_mem_manager (klass), size);
}

static inline void *
m_class_alloc0 (MonoClass *klass, guint size)
{
	return mono_mem_manager_alloc0 (m_class_get_mem_manager (klass), size);
}

static inline MonoMemoryManager*
m_method_get_mem_manager (MonoMethod *method)
{
	if (method->is_inflated)
		return ((MonoMethodInflated*)method)->owner;
	else if (method->wrapper_type && ((MonoMethodWrapper*)method)->mem_manager)
		return ((MonoMethodWrapper*)method)->mem_manager;
	else
		return m_class_get_mem_manager (method->klass);
}

static inline void *
m_method_alloc (MonoMethod *method, guint size)
{
	return mono_mem_manager_alloc (m_method_get_mem_manager (method), size);
}

static inline void *
m_method_alloc0 (MonoMethod *method, guint size)
{
	return mono_mem_manager_alloc0 (m_method_get_mem_manager (method), size);
}

// Enum and static storage for JIT icalls.
#include "jit-icall-reg.h"

/*Now that everything has been defined, let's include the inline functions */
#include <mono/metadata/class-inlines.h>

#endif /* __MONO_METADATA_CLASS_INTERNALS_H__ */
