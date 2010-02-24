#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>

MONO_BEGIN_DECLS

typedef struct MonoVTable MonoVTable;

typedef struct _MonoClassField MonoClassField;
typedef struct _MonoProperty MonoProperty;
typedef struct _MonoEvent MonoEvent;

MonoClass *
mono_class_get             (MonoImage *image, uint32_t type_token);

MonoClass *
mono_class_get_full        (MonoImage *image, uint32_t type_token, MonoGenericContext *context);

mono_bool
mono_class_init            (MonoClass *klass);

MonoVTable *
mono_class_vtable          (MonoDomain *domain, MonoClass *klass);

MonoClass *
mono_class_from_name       (MonoImage *image, const char* name_space, const char *name);

MonoClass *
mono_class_from_name_case  (MonoImage *image, const char* name_space, const char *name);

MonoMethod *
mono_class_get_method_from_name_flags (MonoClass *klass, const char *name, int param_count, int flags);

MonoClass * 
mono_class_from_typeref    (MonoImage *image, uint32_t type_token);

MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *image, mono_bool is_mvar);

MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context) /* MONO_DEPRECATED */;

MonoMethod*
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context);

MonoMethod *
mono_get_inflated_method (MonoMethod *method);

MonoClassField*
mono_field_from_token      (MonoImage *image, uint32_t token, MonoClass **retklass, MonoGenericContext *context);

MonoClass *
mono_bounded_array_class_get (MonoClass *element_class, uint32_t rank, mono_bool bounded);

MonoClass *
mono_array_class_get       (MonoClass *element_class, uint32_t rank);

MonoClass *
mono_ptr_class_get         (MonoType *type);

MonoClassField *
mono_class_get_field       (MonoClass *klass, uint32_t field_token);

MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name);

uint32_t
mono_class_get_field_token (MonoClassField *field);

uint32_t
mono_class_get_event_token (MonoEvent *event);

MonoProperty*
mono_class_get_property_from_name (MonoClass *klass, const char *name);

uint32_t
mono_class_get_property_token (MonoProperty *prop);

int32_t
mono_array_element_size    (MonoClass *ac);

int32_t
mono_class_instance_size   (MonoClass *klass);

int32_t
mono_class_array_element_size (MonoClass *klass);

int32_t
mono_class_data_size       (MonoClass *klass);

int32_t
mono_class_value_size      (MonoClass *klass, uint32_t *align);

int32_t
mono_class_min_align       (MonoClass *klass);

MonoClass *
mono_class_from_mono_type  (MonoType *type);

mono_bool
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, 
						   mono_bool check_interfaces);

mono_bool
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass);

void*
mono_ldtoken               (MonoImage *image, uint32_t token, MonoClass **retclass, MonoGenericContext *context);

char*         
mono_type_get_name         (MonoType *type);

MonoType*
mono_type_get_underlying_type (MonoType *type);

/* MonoClass accessors */
MonoImage*
mono_class_get_image         (MonoClass *klass);

MonoClass*
mono_class_get_element_class (MonoClass *klass);

mono_bool
mono_class_is_valuetype      (MonoClass *klass);

mono_bool
mono_class_is_enum          (MonoClass *klass);

MonoType*
mono_class_enum_basetype    (MonoClass *klass);

MonoClass*
mono_class_get_parent        (MonoClass *klass);

MonoClass*
mono_class_get_nesting_type  (MonoClass *klass);

int
mono_class_get_rank          (MonoClass *klass);

uint32_t
mono_class_get_flags         (MonoClass *klass);

const char*
mono_class_get_name          (MonoClass *klass);

const char*
mono_class_get_namespace     (MonoClass *klass);

MonoType*
mono_class_get_type          (MonoClass *klass);

uint32_t
mono_class_get_type_token    (MonoClass *klass);

MonoType*
mono_class_get_byref_type    (MonoClass *klass);

int
mono_class_num_fields        (MonoClass *klass);

int
mono_class_num_methods       (MonoClass *klass);

int
mono_class_num_properties    (MonoClass *klass);

int
mono_class_num_events        (MonoClass *klass);

MonoClassField*
mono_class_get_fields        (MonoClass* klass, void **iter);

MonoMethod*
mono_class_get_methods       (MonoClass* klass, void **iter);

MonoProperty*
mono_class_get_properties    (MonoClass* klass, void **iter);

MonoEvent*
mono_class_get_events        (MonoClass* klass, void **iter);

MonoClass*
mono_class_get_interfaces    (MonoClass* klass, void **iter);

MonoClass*
mono_class_get_nested_types  (MonoClass* klass, void **iter);

/* MonoClassField accessors */
const char*
mono_field_get_name   (MonoClassField *field);

MonoType*
mono_field_get_type   (MonoClassField *field);

MonoClass*
mono_field_get_parent (MonoClassField *field);

uint32_t
mono_field_get_flags  (MonoClassField *field);

uint32_t
mono_field_get_offset  (MonoClassField *field);

const char *
mono_field_get_data  (MonoClassField *field);

/* MonoProperty acessors */
const char*
mono_property_get_name       (MonoProperty *prop);

MonoMethod*
mono_property_get_set_method (MonoProperty *prop);

MonoMethod*
mono_property_get_get_method (MonoProperty *prop);

MonoClass*
mono_property_get_parent     (MonoProperty *prop);

uint32_t
mono_property_get_flags      (MonoProperty *prop);

/* MonoEvent accessors */
const char*
mono_event_get_name          (MonoEvent *event);

MonoMethod*
mono_event_get_add_method    (MonoEvent *event);

MonoMethod*
mono_event_get_remove_method (MonoEvent *event);

MonoMethod*
mono_event_get_remove_method (MonoEvent *event);

MonoMethod*
mono_event_get_raise_method  (MonoEvent *event);

MonoClass*
mono_event_get_parent        (MonoEvent *event);

uint32_t
mono_event_get_flags         (MonoEvent *event);

MonoMethod *
mono_class_get_method_from_name (MonoClass *klass, const char *name, int param_count);

char *
mono_class_name_from_token (MonoImage *image, uint32_t type_token);

mono_bool
mono_method_can_access_field (MonoMethod *method, MonoClassField *field);

mono_bool
mono_method_can_access_method (MonoMethod *method, MonoMethod *called);

MONO_END_DECLS

#endif /* _MONO_CLI_CLASS_H_ */
