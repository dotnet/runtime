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
	guint8** param_defaults;
	char *dllentry, *dll;
} MonoReflectionMethodAux;

typedef enum {
	ResolveTokenError_OutOfRange,
	ResolveTokenError_BadTable,
	ResolveTokenError_Other
} MonoResolveTokenError;

int           mono_reflection_parse_type (char *name, MonoTypeNameParse *info);
MonoType*     mono_reflection_get_type   (MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase, gboolean *type_resolve);
MonoType*     mono_reflection_type_from_name (char *name, MonoImage *image);
guint32       mono_reflection_get_token (MonoObject *obj);

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
MonoReflectionMethodBody* mono_method_body_get_object (MonoDomain *domain, MonoMethod *method);
MonoObject* mono_get_dbnull_object (MonoDomain *domain);

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


#define MONO_DECLSEC_ACTION_MIN		0x1
#define MONO_DECLSEC_ACTION_MAX		0x12

enum {
	MONO_DECLSEC_FLAG_REQUEST 			= 0x00000001,
	MONO_DECLSEC_FLAG_DEMAND			= 0x00000002,
	MONO_DECLSEC_FLAG_ASSERT			= 0x00000004,
	MONO_DECLSEC_FLAG_DENY				= 0x00000008,
	MONO_DECLSEC_FLAG_PERMITONLY			= 0x00000010,
	MONO_DECLSEC_FLAG_LINKDEMAND			= 0x00000020,
	MONO_DECLSEC_FLAG_INHERITANCEDEMAND		= 0x00000040,
	MONO_DECLSEC_FLAG_REQUEST_MINIMUM		= 0x00000080,
	MONO_DECLSEC_FLAG_REQUEST_OPTIONAL		= 0x00000100,
	MONO_DECLSEC_FLAG_REQUEST_REFUSE		= 0x00000200,
	MONO_DECLSEC_FLAG_PREJIT_GRANT			= 0x00000400,
	MONO_DECLSEC_FLAG_PREJIT_DENY			= 0x00000800,
	MONO_DECLSEC_FLAG_NONCAS_DEMAND			= 0x00001000,
	MONO_DECLSEC_FLAG_NONCAS_LINKDEMAND		= 0x00002000,
	MONO_DECLSEC_FLAG_NONCAS_INHERITANCEDEMAND	= 0x00004000,
	MONO_DECLSEC_FLAG_LINKDEMAND_CHOICE		= 0x00008000,
	MONO_DECLSEC_FLAG_INHERITANCEDEMAND_CHOICE	= 0x00010000,
	MONO_DECLSEC_FLAG_DEMAND_CHOICE			= 0x00020000
};

guint32 mono_declsec_flags_from_method (MonoMethod *method);
guint32 mono_declsec_flags_from_class (MonoClass *klass);
guint32 mono_declsec_flags_from_assembly (MonoAssembly *assembly);

typedef struct {
	char *blob;				/* pointer to metadata blob */
	guint32 size;				/* size of the metadata blob */
} MonoDeclSecurityEntry;

typedef struct {
	MonoDeclSecurityEntry demand;
	MonoDeclSecurityEntry noncasdemand;
	MonoDeclSecurityEntry demandchoice;
} MonoDeclSecurityActions;

MonoBoolean mono_declsec_get_demands (MonoMethod *callee, MonoDeclSecurityActions* demands);
MonoBoolean mono_declsec_get_method_action (MonoMethod *method, guint32 action, MonoDeclSecurityEntry *entry);
MonoBoolean mono_declsec_get_class_action (MonoClass *klass, guint32 action, MonoDeclSecurityEntry *entry);
MonoBoolean mono_declsec_get_assembly_action (MonoAssembly *assembly, guint32 action, MonoDeclSecurityEntry *entry);

#endif /* __METADATA_REFLECTION_H__ */
