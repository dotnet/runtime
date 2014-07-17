#ifndef __METADATA_REFLECTION_H__
#define __METADATA_REFLECTION_H__

#include <mono/metadata/object.h>

MONO_BEGIN_DECLS

typedef struct MonoTypeNameParse MonoTypeNameParse;

typedef struct {
	MonoMethod *ctor;
	uint32_t     data_size;
	const mono_byte* data;
} MonoCustomAttrEntry;

typedef struct {
	int num_attrs;
	int cached;
	MonoImage *image;
	MonoCustomAttrEntry attrs [MONO_ZERO_LEN_ARRAY];
} MonoCustomAttrInfo;

#define MONO_SIZEOF_CUSTOM_ATTR_INFO (offsetof (MonoCustomAttrInfo, attrs))

/* 
 * Information which isn't in the MonoMethod structure is stored here for
 * dynamic methods.
 */
typedef struct {
	char **param_names;
	MonoMarshalSpec **param_marshall;
	MonoCustomAttrInfo **param_cattr;
	uint8_t** param_defaults;
	uint32_t *param_default_types;
	char *dllentry, *dll;
} MonoReflectionMethodAux;

typedef enum {
	ResolveTokenError_OutOfRange,
	ResolveTokenError_BadTable,
	ResolveTokenError_Other
} MonoResolveTokenError;

MONO_API int           mono_reflection_parse_type (char *name, MonoTypeNameParse *info);
MONO_API MonoType*     mono_reflection_get_type   (MonoImage* image, MonoTypeNameParse *info, mono_bool ignorecase, mono_bool *type_resolve);
MONO_API void          mono_reflection_free_type_info (MonoTypeNameParse *info);
MONO_API MonoType*     mono_reflection_type_from_name (char *name, MonoImage *image);
MONO_API uint32_t      mono_reflection_get_token (MonoObject *obj);

MONO_API MonoReflectionAssembly* mono_assembly_get_object (MonoDomain *domain, MonoAssembly *assembly);
MONO_API MonoReflectionModule*   mono_module_get_object   (MonoDomain *domain, MonoImage *image);
MONO_API MonoReflectionModule*   mono_module_file_get_object (MonoDomain *domain, MonoImage *image, int table_index);
MONO_API MonoReflectionType*     mono_type_get_object     (MonoDomain *domain, MonoType *type);
MONO_API MonoReflectionMethod*   mono_method_get_object   (MonoDomain *domain, MonoMethod *method, MonoClass *refclass);
MONO_API MonoReflectionField*    mono_field_get_object    (MonoDomain *domain, MonoClass *klass, MonoClassField *field);
MONO_API MonoReflectionProperty* mono_property_get_object (MonoDomain *domain, MonoClass *klass, MonoProperty *property);
MONO_API MonoReflectionEvent*    mono_event_get_object    (MonoDomain *domain, MonoClass *klass, MonoEvent *event);
/* note: this one is slightly different: we keep the whole array of params in the cache */
MONO_API MonoArray* mono_param_get_objects  (MonoDomain *domain, MonoMethod *method);
MONO_API MonoReflectionMethodBody* mono_method_body_get_object (MonoDomain *domain, MonoMethod *method);

MONO_API MonoObject *mono_get_dbnull_object (MonoDomain *domain);

MONO_API MonoArray*  mono_reflection_get_custom_attrs_by_type (MonoObject *obj, MonoClass *attr_klass, MonoError *error);
MONO_API MonoArray*  mono_reflection_get_custom_attrs (MonoObject *obj);
MONO_API MonoArray*  mono_reflection_get_custom_attrs_data (MonoObject *obj);
MONO_API MonoArray*  mono_reflection_get_custom_attrs_blob (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *porpValues, MonoArray *fields, MonoArray* fieldValues);

MONO_API MonoCustomAttrInfo* mono_reflection_get_custom_attrs_info (MonoObject *obj);
MONO_API MonoArray*  mono_custom_attrs_construct (MonoCustomAttrInfo *cinfo);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_index    (MonoImage *image, uint32_t idx);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_method   (MonoMethod *method);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_class    (MonoClass *klass);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_assembly (MonoAssembly *assembly);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_property (MonoClass *klass, MonoProperty *property);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_event    (MonoClass *klass, MonoEvent *event);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_field    (MonoClass *klass, MonoClassField *field);
MONO_API MonoCustomAttrInfo* mono_custom_attrs_from_param    (MonoMethod *method, uint32_t param);
MONO_API mono_bool           mono_custom_attrs_has_attr      (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass);
MONO_API MonoObject*         mono_custom_attrs_get_attr      (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass);
MONO_API void                mono_custom_attrs_free          (MonoCustomAttrInfo *ainfo);


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

MONO_API uint32_t mono_declsec_flags_from_method (MonoMethod *method);
MONO_API uint32_t mono_declsec_flags_from_class (MonoClass *klass);
MONO_API uint32_t mono_declsec_flags_from_assembly (MonoAssembly *assembly);

/* this structure MUST be kept in synch with RuntimeDeclSecurityEntry
 * located in /mcs/class/corlib/System.Security/SecurityFrame.cs */
typedef struct {
	char *blob;				/* pointer to metadata blob */
	uint32_t size;				/* size of the metadata blob */
	uint32_t index;
} MonoDeclSecurityEntry;

typedef struct {
	MonoDeclSecurityEntry demand;
	MonoDeclSecurityEntry noncasdemand;
	MonoDeclSecurityEntry demandchoice;
} MonoDeclSecurityActions;

MONO_API MonoBoolean mono_declsec_get_demands (MonoMethod *callee, MonoDeclSecurityActions* demands);
MONO_API MonoBoolean mono_declsec_get_linkdemands (MonoMethod *callee, MonoDeclSecurityActions* klass, MonoDeclSecurityActions* cmethod);
MONO_API MonoBoolean mono_declsec_get_inheritdemands_class (MonoClass *klass, MonoDeclSecurityActions* demands);
MONO_API MonoBoolean mono_declsec_get_inheritdemands_method (MonoMethod *callee, MonoDeclSecurityActions* demands);

MONO_API MonoBoolean mono_declsec_get_method_action (MonoMethod *method, uint32_t action, MonoDeclSecurityEntry *entry);
MONO_API MonoBoolean mono_declsec_get_class_action (MonoClass *klass, uint32_t action, MonoDeclSecurityEntry *entry);
MONO_API MonoBoolean mono_declsec_get_assembly_action (MonoAssembly *assembly, uint32_t action, MonoDeclSecurityEntry *entry);

MONO_API MonoType* mono_reflection_type_get_type (MonoReflectionType *reftype);

MONO_API MonoAssembly* mono_reflection_assembly_get_assembly (MonoReflectionAssembly *refassembly);

MONO_END_DECLS

#endif /* __METADATA_REFLECTION_H__ */
