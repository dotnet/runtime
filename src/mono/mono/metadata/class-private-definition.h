/**
 * \file Definitions of struct _MonoClass members
 *
 * NOTE: This file should NOT be included directly.
 */

#if defined(MONO_CLASS_DEF_PRIVATE) && !defined(REALLY_INCLUDE_CLASS_DEF)
#error struct _MonoClass definition should not be accessed directly
#endif

#ifndef __MONO_METADATA_CLASS_PRIVATE_DEFINITION_H__
#define __MONO_METADATA_CLASS_PRIVATE_DEFINITION_H__

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

	/* One of the values from MonoTypeKind */
	guint8     class_kind;

	int        instance_size; /* object instance size */

	guint inited          : 1;

	/* A class contains static and non static data. Static data can be
	 * of the same type as the class itselfs, but it does not influence
	 * the instance size of the class. To avoid cyclic calls to
	 * mono_class_init_internal (from mono_class_instance_size ()) we first
	 * initialise all non static fields. After that we set size_inited
	 * to 1, because we know the instance size now. After that we
	 * initialise all static fields.
	 */

	/* ALL BITFIELDS SHOULD BE WRITTEN WHILE HOLDING THE LOADER LOCK */
	guint size_inited     : 1;
	guint valuetype       : 1; /* derives from System.ValueType */
	guint enumtype        : 1; /* derives from System.Enum */
	guint blittable       : 1; /* class is blittable */
	guint unicode         : 1; /* class uses unicode char when marshalled */
	guint wastypebuilder  : 1; /* class was created at runtime from a TypeBuilder */
	guint is_array_special_interface : 1; /* gtd or ginst of once of the magic interfaces that arrays implement */
	guint is_byreflike    : 1; /* class is a valuetype and has System.Runtime.CompilerServices.IsByRefLikeAttribute */
	guint is_inlinearray    : 1; /* class is a valuetype and has System.Runtime.CompilerServices.InlineArrayAttribute */

	/* next byte */
	guint8 min_align;

	/* next byte */
	guint packing_size    : 4;
	guint ghcimpl         : 1; /* class has its own GetHashCode impl */
	guint has_finalize    : 1; /* class has its own Finalize impl */
	guint delegate        : 1; /* class is a Delegate */
	/* next byte */
	guint gc_descr_inited : 1; /* gc_descr is initialized */
	guint has_cctor       : 1; /* class has a cctor */
	guint has_references  : 1; /* it has GC-tracked references in the instance */
	guint has_ref_fields  : 1; /* it has byref fields */
	guint has_static_refs : 1; /* it has static fields that are GC-tracked */
	guint no_special_static_fields : 1; /* has no thread/context static fields */

	guint nested_classes_inited : 1; /* Whenever nested_class is initialized */

	/* next byte*/
	guint interfaces_inited : 1; /* interfaces is initialized */
	guint simd_type : 1; /* class is a simd intrinsic type */
	guint has_finalize_inited    : 1; /* has_finalize is initialized */
	guint fields_inited : 1; /* setup_fields () has finished */
	guint has_failure : 1; /* See mono_class_get_exception_data () for a MonoErrorBoxed with the details */
	guint has_weak_fields : 1; /* class has weak reference fields */
	guint has_dim_conflicts : 1; /* Class has conflicting default interface methods */
	guint any_field_has_auto_layout : 1; /* a field in this type's layout uses auto-layout */
	guint has_deferred_failure : 1;

	MonoClass  *parent;
	MonoClass  *nested_in;

	MonoImage *image;
	const char *name;
	const char *name_space;

	guint32    type_token;
	int        vtable_size; /* number of slots */

	guint16     interface_count;
	guint32     interface_id;        /* unique inderface id (for interfaces) */
	guint32     max_interface_id;

	guint16     interface_offsets_count;
	MonoClass **interfaces_packed;
	guint16    *interface_offsets_packed;
	guint8     *interface_bitmap;

	gint32 inlinearray_value; /* System.Runtime.CompilerServices.InlineArrayAttribute length value */

	MonoClass **interfaces;

	union _MonoClassSizes sizes;

	/*
	 * Field information: Type and location from object base
	 */
	MonoClassField *fields;

	MonoMethod **methods;

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType _byval_arg;

	MonoGCDescriptor gc_descr;

	MonoVTable *runtime_vtable;

	/* Generic vtable. Initialized by a call to mono_class_setup_vtable () */
	MonoMethod **vtable;

	/* Infrequently used items. See class-accessors.c: InfrequentDataKind for what goes into here. */
	MonoPropertyBag infrequent_data;
};

struct _MonoClassDef {
	MonoClass klass;
	guint32	flags;
	/*
	 * From the TypeDef table
	 */
	guint32 first_method_idx;
	guint32 first_field_idx;
	guint32 method_count, field_count;
	/* next element in the class_cache hash list (in MonoImage) */
	MonoClass *next_class_cache;
};

struct _MonoClassGtd {
	MonoClassDef klass;
	MonoGenericContainer *generic_container;
	/* The canonical GENERICINST where we instantiate a generic type definition with its own generic parameters.*/
	/* Suppose we have class T`2<A,B> {...}.  canonical_inst is the GTD T`2 applied to A and B. */
	MonoType canonical_inst;
};

struct _MonoClassGenericInst {
	MonoClass klass;
	MonoGenericClass *generic_class;
};

struct _MonoClassGenericParam {
	MonoClass klass;
};

struct _MonoClassArray {
	MonoClass klass;
	guint32 method_count;
};

struct _MonoClassPointer {
	MonoClass klass;
};


#endif
