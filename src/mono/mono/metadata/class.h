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
} MonoClassField;

struct _MonoClass {
	MonoImage *image;
	guint32    type_token;

	guint dummy           : 1; /* temorary hack */
	guint inited          : 1;
	guint metadata_inited : 1;
	guint valuetype       : 1; /* derives from System.ValueType */
	guint enumtype        : 1; /* derives from System.Enum */

	MonoClass *parent;

	const char *name;
	const char *name_space;
	
	guint  interface_id; /* unique inderface id (for interfaces) */
	gint  *interface_offsets;
	guint  interface_count;
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
		guint32 first, last;
		int count;
	} field, method;

	/*
	 * Field information: Type and location from object base
	 */
	MonoClassField *fields;

	MonoMethod **methods;

	/* for arrays */
	MonoClass *element_class; /* element class */
	guint32    rank;          /* array dimension */

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType byval_arg;
	
	gpointer data;

	gpointer vtable [0];
};

MonoClass *
mono_class_get             (MonoImage *image, guint32 type_token);

void
mono_class_metadata_init   (MonoClass *klass);

MonoClass *
mono_class_from_name       (MonoImage *image, const char* name_space, const char *name);

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

#endif /* _MONO_CLI_CLASS_H_ */
