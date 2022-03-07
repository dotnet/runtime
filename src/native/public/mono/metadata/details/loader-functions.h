// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethod *, mono_get_method, (MonoImage *image, uint32_t token, MonoClass *klass))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethod *, mono_get_method_full, (MonoImage *image, uint32_t token, MonoClass *klass, MonoGenericContext *context))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethod *, mono_get_method_constrained, (MonoImage *image, uint32_t token, MonoClass *constrained_class, MonoGenericContext *context, MonoMethod **cil_method))

MONO_API_FUNCTION(void, mono_free_method, (MonoMethod *method))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodSignature*, mono_method_get_signature_full, (MonoMethod *method, MonoImage *image, uint32_t token, MonoGenericContext *context))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodSignature*, mono_method_get_signature, (MonoMethod *method, MonoImage *image, uint32_t token))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodSignature*, mono_method_signature, (MonoMethod *method))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoMethodHeader*, mono_method_get_header, (MonoMethod *method))

MONO_API_FUNCTION(const char*, mono_method_get_name, (MonoMethod *method))

MONO_API_FUNCTION(MonoClass*, mono_method_get_class, (MonoMethod *method))

MONO_API_FUNCTION(uint32_t, mono_method_get_token, (MonoMethod *method))

MONO_API_FUNCTION(uint32_t, mono_method_get_flags, (MonoMethod *method, uint32_t *iflags))

MONO_API_FUNCTION(uint32_t, mono_method_get_index, (MonoMethod *method))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_add_internal_call, (const char *name, const void* method))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_dangerous_add_raw_internal_call, (const char *name, const void* method))

MONO_API_FUNCTION(void*, mono_lookup_internal_call, (MonoMethod *method))

MONO_API_FUNCTION(const char*, mono_lookup_icall_symbol, (MonoMethod *m))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_dllmap_insert, (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void*, mono_lookup_pinvoke_call, (MonoMethod *method, const char **exc_class, const char **exc_arg))

MONO_API_FUNCTION(void, mono_method_get_param_names, (MonoMethod *method, const char **names))

MONO_API_FUNCTION(uint32_t, mono_method_get_param_token, (MonoMethod *method, int idx))

MONO_API_FUNCTION(void, mono_method_get_marshal_info, (MonoMethod *method, MonoMarshalSpec **mspecs))

MONO_API_FUNCTION(mono_bool, mono_method_has_marshal_info, (MonoMethod *method))

MONO_API_FUNCTION(MonoMethod*, mono_method_get_last_managed, (void))

MONO_API_FUNCTION(void, mono_stack_walk, (MonoStackWalk func, void* user_data))

/* Use this if the IL offset is not needed: it's faster */
MONO_API_FUNCTION(void, mono_stack_walk_no_il, (MonoStackWalk func, void* user_data))

MONO_API_FUNCTION(void, mono_stack_walk_async_safe, (MonoStackWalkAsyncSafe func, void *initial_sig_context, void* user_data))

MONO_API_FUNCTION(MonoMethodHeader*, mono_method_get_header_checked, (MonoMethod *method, MonoError *error))
