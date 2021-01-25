/**
 * \file
 */

#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

#include <mono/metadata/object-forward.h>
#include <mono/metadata/object.h>
#include <mono/metadata/image.h>

MONO_BEGIN_DECLS

MONO_API MonoException *
mono_exception_from_name               (MonoImage *image, 
					const char* name_space, 
					const char *name);

MONO_API MonoException *
mono_exception_from_token              (MonoImage *image, uint32_t token);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_exception_from_name_two_strings (MonoImage *image, const char *name_space,
				      const char *name, MonoString *a1, MonoString *a2);

MONO_API MonoException *
mono_exception_from_name_msg	       (MonoImage *image, const char *name_space,
					const char *name, const char *msg);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_exception_from_token_two_strings (MonoImage *image, uint32_t token,
						   MonoString *a1, MonoString *a2);

MONO_API MonoException *
mono_exception_from_name_domain        (MonoDomain *domain, MonoImage *image, 
					const char* name_space, 
					const char *name);

MONO_API MonoException *
mono_get_exception_divide_by_zero      (void);

MONO_API MonoException *
mono_get_exception_security            (void);

MONO_API MonoException *
mono_get_exception_arithmetic          (void);

MONO_API MonoException *
mono_get_exception_overflow            (void);

MONO_API MonoException *
mono_get_exception_null_reference      (void);

MONO_API MonoException *
mono_get_exception_execution_engine    (const char *msg);

MONO_API MonoException *
mono_get_exception_thread_abort        (void);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_thread_state        (const char *msg);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_thread_interrupted  (void);

MONO_API MonoException *
mono_get_exception_serialization       (const char *msg);

MONO_API MonoException *
mono_get_exception_invalid_cast        (void);

MONO_API MonoException *
mono_get_exception_invalid_operation (const char *msg);

MONO_API MonoException *
mono_get_exception_index_out_of_range  (void);

MONO_API MonoException *
mono_get_exception_array_type_mismatch (void);

MONO_API MonoException *
mono_get_exception_type_load           (MonoString *class_name, char *assembly_name);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_missing_method      (const char *class_name, const char *member_name);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_missing_field       (const char *class_name, const char *member_name);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_not_implemented     (const char *msg);

MONO_API MonoException *
mono_get_exception_not_supported       (const char *msg);

MONO_API MONO_RT_EXTERNAL_ONLY MonoException*
mono_get_exception_argument_null       (const char *arg);

MONO_API MonoException *
mono_get_exception_argument            (const char *arg, const char *msg);

MONO_API MonoException *
mono_get_exception_argument_out_of_range (const char *arg);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_io                    (const char *msg);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_file_not_found        (MonoString *fname);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_file_not_found2       (const char *msg, MonoString *fname);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_type_initialization (const char *type_name, MonoException *inner);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_synchronization_lock (const char *msg);

MONO_API MonoException *
mono_get_exception_cannot_unload_appdomain (const char *msg);

MONO_API MonoException *
mono_get_exception_appdomain_unloaded (void);

MONO_API MonoException *
mono_get_exception_bad_image_format (const char *msg);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_bad_image_format2 (const char *msg, MonoString *fname);

MONO_API MonoException *
mono_get_exception_stack_overflow (void);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_out_of_memory (void);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_field_access (void);

MONO_API MonoException *
mono_get_exception_method_access (void);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_reflection_type_load (MonoArray *types, MonoArray *exceptions);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoException *
mono_get_exception_runtime_wrapped (MonoObject *wrapped_exception);

/* Installs a function which is called when the runtime encounters an unhandled exception.
 * This hook isn't expected to return.
 * If no hook has been installed, the runtime will print a message before aborting.
 */
typedef void  (*MonoUnhandledExceptionFunc)         (MonoObject *exc, void *user_data);
MONO_API void mono_install_unhandled_exception_hook (MonoUnhandledExceptionFunc func, void *user_data);
void          mono_invoke_unhandled_exception_hook  (MonoObject *exc);

MONO_END_DECLS

#endif /* _MONO_METADATA_EXCEPTION_H_ */
