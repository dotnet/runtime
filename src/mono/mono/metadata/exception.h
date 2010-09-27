#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

/* here for compat: should not be used anymore */
#define MONO_ARCH_SAVE_REGS 

#include <mono/metadata/object.h>
#include <mono/metadata/image.h>

MONO_BEGIN_DECLS

extern MonoException *
mono_exception_from_name               (MonoImage *image, 
					const char* name_space, 
					const char *name);

MonoException *
mono_exception_from_token              (MonoImage *image, uint32_t token);

MonoException *
mono_exception_from_name_two_strings (MonoImage *image, const char *name_space,
				      const char *name, MonoString *a1, MonoString *a2);

MonoException *
mono_exception_from_name_msg	       (MonoImage *image, const char *name_space,
					const char *name, const char *msg);

MonoException *
mono_exception_from_token_two_strings (MonoImage *image, uint32_t token,
						   MonoString *a1, MonoString *a2);

extern MonoException *
mono_exception_from_name_domain        (MonoDomain *domain, MonoImage *image, 
					const char* name_space, 
					const char *name);

MonoException *
mono_get_exception_divide_by_zero      (void);

MonoException *
mono_get_exception_security            (void);

MonoException *
mono_get_exception_arithmetic          (void);

MonoException *
mono_get_exception_overflow            (void);

MonoException *
mono_get_exception_null_reference      (void);

MonoException *
mono_get_exception_execution_engine    (const char *msg);

MonoException *
mono_get_exception_thread_abort        (void);

MonoException *
mono_get_exception_thread_state        (const char *msg);

MonoException *
mono_get_exception_thread_interrupted  (void);

MonoException *
mono_get_exception_serialization       (const char *msg);

MonoException *
mono_get_exception_invalid_cast        (void);

MonoException *
mono_get_exception_invalid_operation (const char *msg);

MonoException *
mono_get_exception_index_out_of_range  (void);

MonoException *
mono_get_exception_array_type_mismatch (void);

MonoException *
mono_get_exception_type_load           (MonoString *class_name, char *assembly_name);

MonoException *
mono_get_exception_missing_method      (const char *class_name, const char *member_name);

MonoException *
mono_get_exception_missing_field       (const char *class_name, const char *member_name);

MonoException *
mono_get_exception_not_implemented     (const char *msg);

MonoException *
mono_get_exception_not_supported       (const char *msg);

MonoException*
mono_get_exception_argument_null       (const char *arg);

MonoException *
mono_get_exception_argument            (const char *arg, const char *msg);

MonoException *
mono_get_exception_argument_out_of_range (const char *arg);

MonoException *
mono_get_exception_io                    (const char *msg);

MonoException *
mono_get_exception_file_not_found        (MonoString *fname);

MonoException *
mono_get_exception_file_not_found2       (const char *msg, MonoString *fname);

MonoException *
mono_get_exception_type_initialization (const char *type_name, MonoException *inner);

MonoException *
mono_get_exception_synchronization_lock (const char *msg);

MonoException *
mono_get_exception_cannot_unload_appdomain (const char *msg);

MonoException *
mono_get_exception_appdomain_unloaded (void);

MonoException *
mono_get_exception_bad_image_format (const char *msg);

MonoException *
mono_get_exception_bad_image_format2 (const char *msg, MonoString *fname);

MonoException *
mono_get_exception_stack_overflow (void);

MonoException *
mono_get_exception_out_of_memory (void);

MonoException *
mono_get_exception_field_access (void);

MonoException *
mono_get_exception_method_access (void);

MonoException *
mono_get_exception_reflection_type_load (MonoArray *types, MonoArray *exceptions);

MonoException *
mono_get_exception_runtime_wrapped (MonoObject *wrapped_exception);

MONO_END_DECLS

#endif /* _MONO_METADATA_EXCEPTION_H_ */
