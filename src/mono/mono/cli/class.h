#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>

typedef struct {
	MonoFieldType *type;
	int            offset;
	guint32        flags;
} MonoClassField;

typedef struct _MonoClass MonoClass;

struct _MonoClass {
	MonoImage *image;
	guint32    type_token;

	MonoClass *parent;
	
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
	/*
	 * After the fields, there is room for the static fields...
	 */
};

MonoClass *mono_class_get       (MonoImage *image, guint32 type_token);

int             mono_field_type_size (MonoFieldType *ft);
MonoClassField *mono_class_get_field (MonoClass *class, guint32 field_token);

#endif /* _MONO_CLI_CLASS_H_ */
