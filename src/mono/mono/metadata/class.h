/**
 * \file
 */

#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/utils/mono-error.h>

MONO_BEGIN_DECLS

typedef struct MonoVTable MonoVTable;

typedef struct _MonoClassField MonoClassField;
typedef struct _MonoProperty MonoProperty;
typedef struct _MonoEvent MonoEvent;

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass *
mono_class_get             (MonoImage *image, uint32_t type_token);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass *
mono_class_get_full        (MonoImage *image, uint32_t type_token, MonoGenericContext *context);

MONO_API MONO_RT_EXTERNAL_ONLY mono_bool
mono_class_init            (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoVTable *
mono_class_vtable          (MonoDomain *domain, MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY MonoClass *
mono_class_from_name       (MonoImage *image, const char* name_space, const char *name);

MONO_API MONO_RT_EXTERNAL_ONLY MonoClass *
mono_class_from_name_case  (MonoImage *image, const char* name_space, const char *name);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethod *
mono_class_get_method_from_name_flags (MonoClass *klass, const char *name, int param_count, int flags);

MONO_API MONO_RT_EXTERNAL_ONLY MonoClass *
mono_class_from_typeref    (MonoImage *image, uint32_t type_token);

MONO_API MonoClass *
mono_class_from_typeref_checked (MonoImage *image, uint32_t type_token, MonoError *error);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *image, mono_bool is_mvar);

MONO_API MONO_RT_EXTERNAL_ONLY MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context) /* MONO_DEPRECATED */;

MONO_API MONO_RT_EXTERNAL_ONLY
MonoMethod*
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoMethod *
mono_get_inflated_method (MonoMethod *method);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClassField*
mono_field_from_token      (MonoImage *image, uint32_t token, MonoClass **retklass, MonoGenericContext *context);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass *
mono_bounded_array_class_get (MonoClass *element_class, uint32_t rank, mono_bool bounded);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass *
mono_array_class_get       (MonoClass *element_class, uint32_t rank);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass *
mono_ptr_class_get         (MonoType *type);

MONO_API MonoClassField *
mono_class_get_field       (MonoClass *klass, uint32_t field_token);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name);

MONO_API uint32_t
mono_class_get_field_token (MonoClassField *field);

MONO_API uint32_t
mono_class_get_event_token (MonoEvent *event);

MONO_API MONO_RT_EXTERNAL_ONLY MonoProperty *
mono_class_get_property_from_name (MonoClass *klass, const char *name);

MONO_API uint32_t
mono_class_get_property_token (MonoProperty *prop);

MONO_API int32_t
mono_array_element_size    (MonoClass *ac);

MONO_API int32_t
mono_class_instance_size   (MonoClass *klass);

MONO_API int32_t
mono_class_array_element_size (MonoClass *klass);

MONO_API int32_t
mono_class_data_size       (MonoClass *klass);

MONO_API int32_t
mono_class_value_size      (MonoClass *klass, uint32_t *align);

MONO_API int32_t
mono_class_min_align       (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY MonoClass *
mono_class_from_mono_type  (MonoType *type);

MONO_API MONO_RT_EXTERNAL_ONLY mono_bool
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, 
						   mono_bool check_interfaces);

MONO_API MONO_RT_EXTERNAL_ONLY mono_bool
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass);

MONO_API MONO_RT_EXTERNAL_ONLY
void*
mono_ldtoken               (MonoImage *image, uint32_t token, MonoClass **retclass, MonoGenericContext *context);

MONO_API char*         
mono_type_get_name         (MonoType *type);

MONO_API MonoType*
mono_type_get_underlying_type (MonoType *type);

/* MonoClass accessors */
MONO_API MonoImage*
mono_class_get_image         (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass*
mono_class_get_element_class (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
mono_bool
mono_class_is_valuetype      (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
mono_bool
mono_class_is_enum          (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY MonoType*
mono_class_enum_basetype    (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClass*
mono_class_get_parent        (MonoClass *klass);

MONO_API MonoClass*
mono_class_get_nesting_type  (MonoClass *klass);

MONO_API int
mono_class_get_rank          (MonoClass *klass);

MONO_API uint32_t
mono_class_get_flags         (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
const char*
mono_class_get_name          (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
const char*
mono_class_get_namespace     (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY MonoType*
mono_class_get_type          (MonoClass *klass);

MONO_API uint32_t
mono_class_get_type_token    (MonoClass *klass);

MONO_API MonoType*
mono_class_get_byref_type    (MonoClass *klass);

MONO_API int
mono_class_num_fields        (MonoClass *klass);

MONO_API int
mono_class_num_methods       (MonoClass *klass);

MONO_API int
mono_class_num_properties    (MonoClass *klass);

MONO_API int
mono_class_num_events        (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoClassField*
mono_class_get_fields        (MonoClass* klass, void **iter);

MONO_API MonoMethod*
mono_class_get_methods       (MonoClass* klass, void **iter);

MONO_API MonoProperty*
mono_class_get_properties    (MonoClass* klass, void **iter);

MONO_API MonoEvent*
mono_class_get_events        (MonoClass* klass, void **iter);

MONO_API MonoClass*
mono_class_get_interfaces    (MonoClass* klass, void **iter);

MONO_API MonoClass*
mono_class_get_nested_types  (MonoClass* klass, void **iter);

MONO_API MONO_RT_EXTERNAL_ONLY
mono_bool
mono_class_is_delegate       (MonoClass* klass);

MONO_API MONO_RT_EXTERNAL_ONLY mono_bool
mono_class_implements_interface (MonoClass* klass, MonoClass* iface);

/* MonoClassField accessors */
MONO_API const char*
mono_field_get_name   (MonoClassField *field);

MONO_API MonoType*
mono_field_get_type   (MonoClassField *field);

MONO_API MonoClass*
mono_field_get_parent (MonoClassField *field);

MONO_API uint32_t
mono_field_get_flags  (MonoClassField *field);

MONO_API uint32_t
mono_field_get_offset  (MonoClassField *field);

MONO_API const char *
mono_field_get_data  (MonoClassField *field);

/* MonoProperty acessors */
MONO_API const char*
mono_property_get_name       (MonoProperty *prop);

MONO_API MonoMethod*
mono_property_get_set_method (MonoProperty *prop);

MONO_API MonoMethod*
mono_property_get_get_method (MonoProperty *prop);

MONO_API MonoClass*
mono_property_get_parent     (MonoProperty *prop);

MONO_API uint32_t
mono_property_get_flags      (MonoProperty *prop);

/* MonoEvent accessors */
MONO_API const char*
mono_event_get_name          (MonoEvent *event);

MONO_API MonoMethod*
mono_event_get_add_method    (MonoEvent *event);

MONO_API MonoMethod*
mono_event_get_remove_method (MonoEvent *event);

MONO_API MonoMethod*
mono_event_get_remove_method (MonoEvent *event);

MONO_API MonoMethod*
mono_event_get_raise_method  (MonoEvent *event);

MONO_API MonoClass*
mono_event_get_parent        (MonoEvent *event);

MONO_API uint32_t
mono_event_get_flags         (MonoEvent *event);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethod *
mono_class_get_method_from_name (MonoClass *klass, const char *name, int param_count);

MONO_API char *
mono_class_name_from_token (MonoImage *image, uint32_t type_token);

MONO_API mono_bool
mono_method_can_access_field (MonoMethod *method, MonoClassField *field);

MONO_API mono_bool
mono_method_can_access_method (MonoMethod *method, MonoMethod *called);

MONO_API mono_bool
mono_class_is_nullable (MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY MonoClass*
mono_class_get_nullable_param (MonoClass *klass);

MONO_END_DECLS

#endif /* _MONO_CLI_CLASS_H_ */
