#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>

typedef struct MonoVTable MonoVTable;

typedef struct _MonoClassField MonoClassField;
typedef struct _MonoProperty MonoProperty;
typedef struct _MonoEvent MonoEvent;

typedef struct {
	MonoVTable *vtable;
	MonoClass *proxy_class;
	guint interface_count;
	MonoClass **interfaces;
	char* proxy_class_name;
} MonoRemoteClass;

MonoClass *
mono_class_get             (MonoImage *image, guint32 type_token);

MonoClass *
mono_class_get_full        (MonoImage *image, guint32 type_token, MonoGenericContext *context);

void
mono_class_init            (MonoClass *klass);

MonoVTable *
mono_class_vtable          (MonoDomain *domain, MonoClass *klass);

MonoClass *
mono_class_from_name       (MonoImage *image, const char* name_space, const char *name);

MonoClass *
mono_class_from_name_case  (MonoImage *image, const char* name_space, const char *name);

MonoClass * 
mono_class_from_typeref    (MonoImage *image, guint32 type_token);

MonoClass *
mono_class_from_generic_parameter (MonoGenericParam *param, MonoImage *image, gboolean is_mvar);

MonoClass*
mono_class_create_generic  (MonoGenericInst *ginst);

MonoType*
mono_class_inflate_generic_type (MonoType *type, MonoGenericContext *context);

MonoMethod*
mono_class_inflate_generic_method (MonoMethod *method, MonoGenericContext *context, MonoClass *klass);

MonoClassField*
mono_field_from_token      (MonoImage *image, guint32 token, MonoClass **retklass, MonoGenericContext *context);

MonoClass *
mono_bounded_array_class_get (MonoClass *element_class, guint32 rank, gboolean bounded);

MonoClass *
mono_array_class_get       (MonoClass *element_class, guint32 rank);

MonoClass *
mono_ptr_class_get         (MonoType *type);

MonoClassField *
mono_class_get_field       (MonoClass *klass, guint32 field_token);

MonoClassField *
mono_class_get_field_from_name (MonoClass *klass, const char *name);

MonoProperty*
mono_class_get_property_from_name (MonoClass *klass, const char *name);

gint32
mono_array_element_size    (MonoClass *ac);

gint32
mono_class_instance_size   (MonoClass *klass);

gint32
mono_class_array_element_size (MonoClass *klass);

gint32
mono_class_data_size       (MonoClass *klass);

gint32
mono_class_value_size      (MonoClass *klass, guint32 *align);

gint32
mono_class_min_align       (MonoClass *klass);

MonoClass *
mono_class_from_mono_type  (MonoType *type);

gboolean
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, 
						   gboolean check_interfaces);

gboolean
mono_class_is_assignable_from (MonoClass *klass, MonoClass *oklass);

gpointer
mono_ldtoken               (MonoImage *image, guint32 token, MonoClass **retclass, MonoGenericContext *context);

char*         
mono_type_get_name         (MonoType *type);

char*         
mono_type_get_full_name    (MonoType *type);

MonoType*
mono_type_get_underlying_type (MonoType *type);

/* MonoClass accessors */
MonoImage*
mono_class_get_image         (MonoClass *klass);

MonoClass*
mono_class_get_element_class (MonoClass *klass);

gboolean
mono_class_is_valuetype      (MonoClass *klass);

gboolean
mono_class_is_enum          (MonoClass *klass);

MonoType*
mono_class_enum_basetype    (MonoClass *klass);

MonoClass*
mono_class_get_parent        (MonoClass *klass);

MonoClass*
mono_class_get_nesting_type  (MonoClass *klass);

int
mono_class_get_rank          (MonoClass *klass);

guint32
mono_class_get_flags         (MonoClass *klass);

const char*
mono_class_get_name          (MonoClass *klass);

const char*
mono_class_get_namespace     (MonoClass *klass);

MonoType*
mono_class_get_type          (MonoClass *klass);

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
mono_class_get_fields        (MonoClass* klass, gpointer *iter);

MonoMethod*
mono_class_get_methods       (MonoClass* klass, gpointer *iter);

MonoProperty*
mono_class_get_properties    (MonoClass* klass, gpointer *iter);

MonoEvent*
mono_class_get_events        (MonoClass* klass, gpointer *iter);

MonoClass*
mono_class_get_interfaces    (MonoClass* klass, gpointer *iter);

MonoClass*
mono_class_get_nested_types  (MonoClass* klass, gpointer *iter);

/* MonoClassField accessors */
const char*
mono_field_get_name   (MonoClassField *field);

MonoType*
mono_field_get_type   (MonoClassField *field);

MonoClass*
mono_field_get_parent (MonoClassField *field);

guint32
mono_field_get_flags  (MonoClassField *field);

/* MonoProperty acessors */
const char*
mono_property_get_name       (MonoProperty *prop);

MonoMethod*
mono_property_get_set_method (MonoProperty *prop);

MonoMethod*
mono_property_get_get_method (MonoProperty *prop);

MonoClass*
mono_property_get_parent     (MonoProperty *prop);

guint32
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

guint32
mono_event_get_flags         (MonoEvent *event);


#endif /* _MONO_CLI_CLASS_H_ */
