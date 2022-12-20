// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY int, mono_reflection_parse_type, (char *name, MonoTypeNameParse *info))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType*, mono_reflection_get_type, (MonoImage* image, MonoTypeNameParse *info, mono_bool ignorecase, mono_bool *type_resolve))
MONO_API_FUNCTION(void, mono_reflection_free_type_info, (MonoTypeNameParse *info))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType*, mono_reflection_type_from_name, (char *name, MonoImage *image))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY uint32_t, mono_reflection_get_token, (MonoObject *obj))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionAssembly*, mono_assembly_get_object, (MonoDomain *domain, MonoAssembly *assembly))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionModule*, mono_module_get_object, (MonoDomain *domain, MonoImage *image))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionModule*, mono_module_file_get_object, (MonoDomain *domain, MonoImage *image, int table_index))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionType*, mono_type_get_object, (MonoDomain *domain, MonoType *type))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionMethod*, mono_method_get_object, (MonoDomain *domain, MonoMethod *method, MonoClass *refclass))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionField*, mono_field_get_object, (MonoDomain *domain, MonoClass *klass, MonoClassField *field))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionProperty*, mono_property_get_object, (MonoDomain *domain, MonoClass *klass, MonoProperty *property))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionEvent*, mono_event_get_object, (MonoDomain *domain, MonoClass *klass, MonoEvent *event))
/* note: this one is slightly different: we keep the whole array of params in the cache */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoArray*, mono_param_get_objects, (MonoDomain *domain, MonoMethod *method))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionMethodBody*, mono_method_body_get_object, (MonoDomain *domain, MonoMethod *method))

MONO_API_FUNCTION(MonoObject *, mono_get_dbnull_object, (MonoDomain *domain))

MONO_API_FUNCTION(MonoArray*, mono_reflection_get_custom_attrs_by_type, (MonoObject *obj, MonoClass *attr_klass, MonoError *error))
MONO_API_FUNCTION(MonoArray*, mono_reflection_get_custom_attrs, (MonoObject *obj))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoArray*, mono_reflection_get_custom_attrs_data, (MonoObject *obj))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoArray*, mono_reflection_get_custom_attrs_blob, (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *porpValues, MonoArray *fields, MonoArray* fieldValues))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_reflection_get_custom_attrs_info, (MonoObject *obj))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoArray*, mono_custom_attrs_construct, (MonoCustomAttrInfo *cinfo))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_index, (MonoImage *image, uint32_t idx))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_method, (MonoMethod *method))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_class, (MonoClass *klass))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_assembly, (MonoAssembly *assembly))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_property, (MonoClass *klass, MonoProperty *property))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_event, (MonoClass *klass, MonoEvent *event))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_field, (MonoClass *klass, MonoClassField *field))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoCustomAttrInfo*, mono_custom_attrs_from_param, (MonoMethod *method, uint32_t param))
MONO_API_FUNCTION(mono_bool, mono_custom_attrs_has_attr, (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoObject*, mono_custom_attrs_get_attr, (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass))
MONO_API_FUNCTION(void, mono_custom_attrs_free, (MonoCustomAttrInfo *ainfo))


MONO_API_FUNCTION(uint32_t, mono_declsec_flags_from_method, (MonoMethod *method))
MONO_API_FUNCTION(uint32_t, mono_declsec_flags_from_class, (MonoClass *klass))
MONO_API_FUNCTION(uint32_t, mono_declsec_flags_from_assembly, (MonoAssembly *assembly))

MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_demands, (MonoMethod *callee, MonoDeclSecurityActions* demands))
MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_linkdemands, (MonoMethod *callee, MonoDeclSecurityActions* klass, MonoDeclSecurityActions* cmethod))
MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_inheritdemands_class, (MonoClass *klass, MonoDeclSecurityActions* demands))
MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_inheritdemands_method, (MonoMethod *callee, MonoDeclSecurityActions* demands))

MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_method_action, (MonoMethod *method, uint32_t action, MonoDeclSecurityEntry *entry))
MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_class_action, (MonoClass *klass, uint32_t action, MonoDeclSecurityEntry *entry))
MONO_API_FUNCTION(MonoBoolean, mono_declsec_get_assembly_action, (MonoAssembly *assembly, uint32_t action, MonoDeclSecurityEntry *entry))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoType*, mono_reflection_type_get_type, (MonoReflectionType *reftype))

MONO_API_FUNCTION(MonoAssembly*, mono_reflection_assembly_get_assembly, (MonoReflectionAssembly *refassembly))
