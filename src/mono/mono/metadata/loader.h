/**
 * \file
 */

#ifndef _MONO_METADATA_LOADER_H_
#define _MONO_METADATA_LOADER_H_ 1

#include <mono/utils/mono-forward.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/utils/mono-error.h>

MONO_BEGIN_DECLS

typedef mono_bool (*MonoStackWalk)     (MonoMethod *method, int32_t native_offset, int32_t il_offset, mono_bool managed, void* data);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethod *
mono_get_method             (MonoImage *image, uint32_t token, MonoClass *klass);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethod *
mono_get_method_full        (MonoImage *image, uint32_t token, MonoClass *klass,
			     MonoGenericContext *context);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethod *
mono_get_method_constrained (MonoImage *image, uint32_t token, MonoClass *constrained_class,
			     MonoGenericContext *context, MonoMethod **cil_method);

MONO_API void               
mono_free_method           (MonoMethod *method);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethodSignature*
mono_method_get_signature_full (MonoMethod *method, MonoImage *image, uint32_t token,
				MonoGenericContext *context);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethodSignature*
mono_method_get_signature  (MonoMethod *method, MonoImage *image, uint32_t token);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethodSignature*
mono_method_signature      (MonoMethod *method);

MONO_API MONO_RT_EXTERNAL_ONLY MonoMethodHeader*
mono_method_get_header     (MonoMethod *method);

MONO_API const char*
mono_method_get_name       (MonoMethod *method);

MONO_API MonoClass*
mono_method_get_class      (MonoMethod *method);

MONO_API uint32_t
mono_method_get_token      (MonoMethod *method);

MONO_API uint32_t
mono_method_get_flags      (MonoMethod *method, uint32_t *iflags);

MONO_API uint32_t
mono_method_get_index      (MonoMethod *method);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_add_internal_call     (const char *name, const void* method);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_dangerous_add_raw_internal_call (const char *name, const void* method);

MONO_API void*
mono_lookup_internal_call (MonoMethod *method);

MONO_API const char*
mono_lookup_icall_symbol (MonoMethod *m);

MONO_API void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc);

MONO_API void*
mono_lookup_pinvoke_call (MonoMethod *method, const char **exc_class, const char **exc_arg);

MONO_API void
mono_method_get_param_names (MonoMethod *method, const char **names);

MONO_API uint32_t
mono_method_get_param_token (MonoMethod *method, int idx);

MONO_API void
mono_method_get_marshal_info (MonoMethod *method, MonoMarshalSpec **mspecs);

MONO_API mono_bool
mono_method_has_marshal_info (MonoMethod *method);

MONO_API MonoMethod*
mono_method_get_last_managed  (void);

MONO_API void
mono_stack_walk         (MonoStackWalk func, void* user_data);

/* Use this if the IL offset is not needed: it's faster */
MONO_API void
mono_stack_walk_no_il   (MonoStackWalk func, void* user_data);

typedef mono_bool (*MonoStackWalkAsyncSafe)     (MonoMethod *method, MonoDomain *domain, void *base_address, int offset, void* data);
MONO_API void
mono_stack_walk_async_safe   (MonoStackWalkAsyncSafe func, void *initial_sig_context, void* user_data);

MONO_API MonoMethodHeader*
mono_method_get_header_checked (MonoMethod *method, MonoError *error);

MONO_END_DECLS

#endif

