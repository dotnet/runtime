#ifndef __METADATA_REFLECTION_H__
#define __METADATA_REFLECTION_H__

#include <mono/metadata/object.h>

typedef struct MonoTypeNameParse MonoTypeNameParse;

struct MonoTypeNameParse {
	char *name_space;
	char *name;
	MonoAssemblyName assembly;
	GList *modifiers; /* 0 -> byref, -1 -> pointer, > 0 -> array rank */
	GList *nested;
};

typedef struct {
	MonoMethod *ctor;
	guint32     data_size;
	const guchar* data;
} MonoCustomAttrEntry;

typedef struct {
	int num_attrs;
	int cached;
	MonoImage *image;
	MonoCustomAttrEntry attrs [MONO_ZERO_LEN_ARRAY];
} MonoCustomAttrInfo;

/* 
 * Information which isn't in the MonoMethod structure is stored here for
 * dynamic methods.
 */
typedef struct {
	char **param_names;
	MonoMarshalSpec **param_marshall;
	MonoCustomAttrInfo **param_cattr;
} MonoReflectionMethodAux;

int           mono_reflection_parse_type (char *name, MonoTypeNameParse *info);
MonoType*     mono_reflection_get_type   (MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase, gboolean *type_resolve);
MonoType*     mono_reflection_type_from_name (char *name, MonoImage *image);

MonoReflectionAssembly* mono_assembly_get_object (MonoDomain *domain, MonoAssembly *assembly);
MonoReflectionModule*   mono_module_get_object   (MonoDomain *domain, MonoImage *image);
MonoReflectionModule*   mono_module_file_get_object (MonoDomain *domain, MonoImage *image, int table_index);
MonoReflectionType*     mono_type_get_object     (MonoDomain *domain, MonoType *type);
MonoReflectionMethod*   mono_method_get_object   (MonoDomain *domain, MonoMethod *method, MonoClass *refclass);
MonoReflectionField*    mono_field_get_object    (MonoDomain *domain, MonoClass *klass, MonoClassField *field);
MonoReflectionProperty* mono_property_get_object (MonoDomain *domain, MonoClass *klass, MonoProperty *property);
MonoReflectionEvent*    mono_event_get_object    (MonoDomain *domain, MonoClass *klass, MonoEvent *event);
/* note: this one is slightly different: we keep the whole array of params in the cache */
MonoArray* mono_param_get_objects  (MonoDomain *domain, MonoMethod *method);

MonoArray*  mono_reflection_get_custom_attrs (MonoObject *obj);
MonoArray*  mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *porpValues, MonoArray *fields, MonoArray* fieldValues);

MonoArray*  mono_custom_attrs_construct (MonoCustomAttrInfo *cinfo);
MonoCustomAttrInfo* mono_custom_attrs_from_index    (MonoImage *image, guint32 idx);
MonoCustomAttrInfo* mono_custom_attrs_from_method   (MonoMethod *method);
MonoCustomAttrInfo* mono_custom_attrs_from_class    (MonoClass *klass);
MonoCustomAttrInfo* mono_custom_attrs_from_assembly (MonoAssembly *assembly);
MonoCustomAttrInfo* mono_custom_attrs_from_property (MonoClass *klass, MonoProperty *property);
MonoCustomAttrInfo* mono_custom_attrs_from_event    (MonoClass *klass, MonoEvent *event);
MonoCustomAttrInfo* mono_custom_attrs_from_field    (MonoClass *klass, MonoClassField *field);
MonoCustomAttrInfo* mono_custom_attrs_from_param    (MonoMethod *method, guint32 param);
void                mono_custom_attrs_free          (MonoCustomAttrInfo *ainfo);

#endif /* __METADATA_REFLECTION_H__ */

