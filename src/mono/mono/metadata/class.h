#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>

typedef struct {
	MonoFieldType *type;
	int            offset;
	guint32        flags;
} MonoClassFields;
	
typedef struct {
	MonoImage *image;
	guint32    type_token;

	/*
	 * Computed object instance size, total.
	 */
	int        instance_size;


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
	MonoClassFields *fields;
} MonoClass;

MonoClass *mono_class_get       (MonoImage *image, guint32 type_token);
void       mono_class_init      (void);

int        mono_field_type_size (MonoFieldType *ft);

#endif /* _MONO_CLI_CLASS_H_ */
