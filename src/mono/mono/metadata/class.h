#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>

#define MONO_CLASS_IS_ARRAY(c) ((c)->rank)

#define MONO_DEFAULT_SUPERTABLE_SIZE 6

extern gboolean mono_print_vtable;
typedef struct MonoVTable MonoVTable;

typedef struct {
	MonoTypeEnum type;
	gpointer value;
} MonoConstant;

/*
 * MonoClassField is just a runtime representation of the metadata for
 * field, it doesn't contain the data directly.  Static fields are
 * stored in MonoVTable->data.  Instance fields are allocated in the
 * objects after the object header.
 */
typedef struct {
	/* Type of the field */
	MonoType        *type;

	/*
	 * Offset where this field is stored; if it is an instance
	 * field, it's the offset from the start of the object, if
	 * it's static, it's from the start of the memory chunk
	 * allocated for statics for the class.
	 */
	int              offset;

	const char      *name;

	/*
	 * Pointer to the data (from the RVA address, valid only for
	 * fields with the has rva flag).
	 */
	const char      *data;

	/* Type where the field was defined */
	MonoClass       *parent;

	/*
	 * If the field is constant, pointer to the metadata where the
	 * constant value can be loaded.
	 */
	MonoConstant    *def_value;
} MonoClassField;

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

typedef struct {
	const char *name;
	MonoMethod *get;
	MonoMethod *set;
	guint32 attrs;
} MonoProperty;

typedef struct {
	const char *name;
	MonoMethod *add;
	MonoMethod *remove;
	MonoMethod *raise;
	MonoMethod **other;
	guint32 attrs;
} MonoEvent;

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

	MonoType *generic_inst;
	MonoGenericParam *gen_params;
	guint16 num_gen_params;

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
	guint       max_interface_id;
        gpointer   *interface_offsets;   
        gpointer    data; /* to store static class data */
	guint remote          : 1; /* class is remotely activated */
	guint initialized     : 1; /* cctor has been run */
	guint initializing    : 1; /* cctor is running */
	/* do not add any fields after vtable, the structure is dynamically extended */
        gpointer    vtable [MONO_ZERO_LEN_ARRAY];	
};

struct _MonoGenericParam {
	MonoClass *pklass;
	MonoMethod *method;
	const char *name;
	guint16 flags;
	guint16 num;
	MonoClass** constraints; /* NULL means end of list */
};

#define mono_class_has_parent(klass,parent) ((klass)->idepth >= (parent)->idepth) && ((klass)->supertypes [(parent)->idepth - 1] == (parent))

typedef struct {
	gulong new_object_count;
	gulong initialized_class_count;
	gulong used_class_count;
	gulong class_vtable_size;
	gulong class_static_data_size;
} MonoStats;

extern MonoStats mono_stats;

typedef gpointer (*MonoTrampoline)       (MonoMethod *method);

typedef gpointer (*MonoLookupDynamicToken) (MonoImage *image, guint32 token);

void
mono_classes_init (void);

MonoClass *
mono_class_get             (MonoImage *image, guint32 type_token);

void
mono_class_init            (MonoClass *klass);

void
mono_class_layout_fields   (MonoClass *klass);

void
mono_class_setup_vtable    (MonoClass *klass, MonoMethod **overrides, int onum);

MonoVTable *
mono_class_vtable          (MonoDomain *domain, MonoClass *klass);

MonoVTable *
mono_class_proxy_vtable    (MonoDomain *domain, MonoClass *klass);

void
mono_class_setup_mono_type (MonoClass *klass);

void
mono_class_setup_parent    (MonoClass *klass, MonoClass *parent);

void
mono_class_setup_supertypes (MonoClass *klass);

MonoClass *
mono_class_from_name       (MonoImage *image, const char* name_space, const char *name);

MonoClass *
mono_class_from_name_case  (MonoImage *image, const char* name_space, const char *name);

MonoClass * 
mono_class_from_typeref    (MonoImage *image, guint32 type_token);

MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *image, gboolean is_mvar);

MonoClass*
mono_class_from_generic    (MonoType *gtype, gboolean inflate_methods);

MonoMethod*
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericInst *tgen, MonoGenericInst *mgen);

MonoClassField*
mono_field_from_memberref  (MonoImage *image, guint32 token, MonoClass **retklass);

MonoClassField*
mono_field_from_token      (MonoImage *image, guint32 token, MonoClass **retklass);

MonoMethod**
mono_class_get_overrides   (MonoImage *image, guint32 type_token, gint32 *num_overrides);

MonoClass *
mono_array_class_get       (MonoClass *element_class, guint32 rank);

MonoClass *
mono_ptr_class_get         (MonoType *type);

MonoClassField *
mono_class_get_field       (MonoClass *klass, guint32 field_token);

MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name);

MonoProperty*
mono_class_get_property_from_name (MonoClass *klass, const char *name);

gint32
mono_array_element_size    (MonoClass *ac);

gint32
mono_class_instance_size   (MonoClass *klass);

gint32
mono_class_array_element_size (MonoClass *klass);

gint32
mono_class_data_size       (MonoClass *klass);

gint32
mono_class_value_size      (MonoClass *klass, guint32 *align);

gint32
mono_class_min_align       (MonoClass *klass);

MonoClass *
mono_class_from_mono_type  (MonoType *type);

gboolean
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, 
						   gboolean check_interfaces);

gboolean
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass);

gboolean
mono_class_needs_cctor_run (MonoClass *klass, MonoMethod *caller);

gpointer
mono_ldtoken               (MonoImage *image, guint32 token, MonoClass **retclass);

char*         
mono_type_get_name         (MonoType *type);

void
mono_install_trampoline (MonoTrampoline func);

void
mono_install_remoting_trampoline (MonoTrampoline func);

gpointer
mono_lookup_dynamic_token (MonoImage *image, guint32 token);

void
mono_install_lookup_dynamic_token (MonoLookupDynamicToken func);

void    
mono_install_get_config_dir(void);

#endif /* _MONO_CLI_CLASS_H_ */
