#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>

#define MONO_CLASS_IS_ARRAY(c) (c->type_token == 0)

#define MONO_CLASS_STATIC_FIELDS_BASE(c) (c->data)

typedef struct {
	MonoType *type;
	int       offset;
	const char     *name;
	const char     *data;
	/* add marshal data, too */
} MonoClassField;

typedef struct {
	const char *name;
	MonoMethod *get;
	MonoMethod *set;
	guint32 attrs;
} MonoProperty;

struct _MonoClass {
	MonoImage *image;
	guint32    type_token;

	guint dummy           : 1; /* temorary hack */
	guint inited          : 1;
	guint valuetype       : 1; /* derives from System.ValueType */
	guint enumtype        : 1; /* derives from System.Enum */
	guint min_align       : 4;

	MonoClass  *parent;
	MonoClass  *nested_in;
	GList      *nested_classes;
	GList      *subclasses; /* list of all subclasses */

	const char *name;
	const char *name_space;
	
	guint       interface_id; /* unique inderface id (for interfaces) */
	guint       max_interface_id;
	gpointer   *interface_offsets;
	guint       interface_count;
	MonoClass **interfaces;

	/*
	 * Computed object instance size, total.
	 */
	int        instance_size;
	int        class_size;
	int        vtable_size; /* number of slots */

	/*
	 * relative numbering for fast type checking
	 */
	unsigned int baseval;
	unsigned int diffval;

	/*
	 * From the TypeDef table
	 */
	guint32    flags;
	struct {
		guint32 first, last;
		int count;
	} field, method, property;

	/*
	 * Field information: Type and location from object base
	 */
	MonoClassField *fields;

	MonoProperty *properties;

	MonoMethod **methods;

	/* The underlying type of the enum */
	MonoType *enum_basetype;
	/* element class for arrays and enum */
	MonoClass *element_class; 
	/* array dimension */
	guint32    rank;          

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType byval_arg;
	
	gpointer data;

	gpointer vtable [0];
};

typedef gpointer (*MonoTrampoline)       (MonoMethod *method);
typedef void     (*MonoRuntimeClassInit) (MonoClass *klass);

MonoClass *
mono_class_get             (MonoImage *image, guint32 type_token);

void
mono_class_init            (MonoClass *klass);

MonoClass *
mono_class_from_name       (MonoImage *image, const char* name_space, const char *name);

MonoClassField*
mono_field_from_memberref  (MonoImage *image, guint32 token, MonoClass **retklass);

MonoClass *
mono_array_class_get       (MonoClass *eclass, guint32 rank);

MonoClassField *
mono_class_get_field       (MonoClass *klass, guint32 field_token);

MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name);

gint32
mono_array_element_size    (MonoClass *ac);

gint32
mono_class_instance_size   (MonoClass *klass);

gint32
mono_class_value_size      (MonoClass *klass, guint32 *align);

gint32
mono_class_data_size       (MonoClass *klass);

MonoClass *
mono_class_from_mono_type  (MonoType *type);

gpointer
mono_ldtoken               (MonoImage *image, guint32 token, MonoClass **retclass);

void
mono_install_trampoline (MonoTrampoline func);

void
mono_install_runtime_class_init (MonoRuntimeClassInit func);

#endif /* _MONO_CLI_CLASS_H_ */
