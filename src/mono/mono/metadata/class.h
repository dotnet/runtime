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
	MonoClass **interfaces;

	const char *name;
	const char *name_space;
	
	/*
	 * Computed object instance size, total.
	 */
	int        instance_size;
	int        class_size;

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

	/* used as the type of the this argument and when passing the arg by value */
	MonoType this_arg;
	MonoType byval_arg;
	
	/* 
	 * Static class data 
	 */
	gpointer data;
};

typedef struct {
	MonoClass  klass;
	MonoClass *element_class; /* element class */
	guint32    rank;          /* array dimension */
} MonoArrayClass;

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
mono_array_element_size    (MonoArrayClass *ac);

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
