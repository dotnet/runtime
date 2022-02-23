// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(char*, mono_disasm_code_one, (MonoDisHelper *dh, MonoMethod *method, const mono_byte *ip, const mono_byte** endp))
MONO_API_FUNCTION(char*, mono_disasm_code, (MonoDisHelper *dh, MonoMethod *method, const mono_byte *ip, const mono_byte* end))

MONO_API_FUNCTION(char*, mono_type_full_name, (MonoType *type))

MONO_API_FUNCTION(char*, mono_signature_get_desc, (MonoMethodSignature *sig, mono_bool include_namespace))

MONO_API_FUNCTION(char*, mono_context_get_desc, (MonoGenericContext *context))

MONO_API_FUNCTION(MonoMethodDesc*, mono_method_desc_new, (const char *name, mono_bool include_namespace))
MONO_API_FUNCTION(MonoMethodDesc*, mono_method_desc_from_method, (MonoMethod *method))
MONO_API_FUNCTION(void, mono_method_desc_free, (MonoMethodDesc *desc))
MONO_API_FUNCTION(mono_bool, mono_method_desc_match, (MonoMethodDesc *desc, MonoMethod *method))
MONO_API_FUNCTION(mono_bool, mono_method_desc_is_full, (MonoMethodDesc *desc))
MONO_API_FUNCTION(mono_bool, mono_method_desc_full_match, (MonoMethodDesc *desc, MonoMethod *method))
MONO_API_FUNCTION(MonoMethod*, mono_method_desc_search_in_class, (MonoMethodDesc *desc, MonoClass *klass))
MONO_API_FUNCTION(MonoMethod*, mono_method_desc_search_in_image, (MonoMethodDesc *desc, MonoImage *image))

MONO_API_FUNCTION(char*, mono_method_full_name, (MonoMethod *method, mono_bool signature))
MONO_API_FUNCTION(char*, mono_method_get_reflection_name, (MonoMethod *method))

MONO_API_FUNCTION(char*, mono_field_full_name, (MonoClassField *field))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_debugger_agent_unhandled_exception, (MonoException *e))
