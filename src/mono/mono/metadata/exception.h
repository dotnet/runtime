#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

/* here for compat: should not be used anymore */
#define MONO_ARCH_SAVE_REGS 

#include <mono/metadata/object.h>
#include <mono/metadata/image.h>

extern MonoException *
mono_exception_from_name               (MonoImage *image, 
					const char* name_space, 
					const char *name);

MonoException *
mono_exception_from_name_two_strings (MonoImage *image, const char *name_space,
				      const char *name, MonoString *a1, MonoString *a2);

MonoException *
mono_exception_from_name_msg	       (MonoImage *image, const char *name_space,
					const char *name, const guchar *msg);

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
mono_get_exception_execution_engine    (const guchar *msg);

MonoException *
mono_get_exception_thread_abort        (void);

MonoException *
mono_get_exception_thread_state        (const guchar *msg);

MonoException *
mono_get_exception_serialization       (const guchar *msg);

MonoException *
mono_get_exception_invalid_cast        (void);

MonoException *
mono_get_exception_index_out_of_range  (void);

MonoException *
mono_get_exception_array_type_mismatch (void);

MonoException *
mono_get_exception_type_load           (MonoString *type_name);

MonoException *
mono_get_exception_missing_method      (void);

MonoException *
mono_get_exception_not_implemented     (const guchar *msg);

MonoException*
mono_get_exception_argument_null       (const guchar *arg);

MonoException *
mono_get_exception_argument            (const guchar *arg, const guchar *msg);

MonoException *
mono_get_exception_argument_out_of_range (const guchar *arg);

MonoException *
mono_get_exception_io                    (const guchar *msg);

MonoException *
mono_get_exception_file_not_found        (MonoString *fname);

MonoException *
mono_get_exception_file_not_found2       (const guchar *msg, MonoString *fname);

MonoException *
mono_get_exception_type_initialization (const gchar *type_name, MonoException *inner);

MonoException *
mono_get_exception_synchronization_lock (const guchar *msg);

MonoException *
mono_get_exception_cannot_unload_appdomain (const guchar *msg);

MonoException *
mono_get_exception_appdomain_unloaded (void);

MonoException *
mono_get_exception_bad_image_format (const guchar *msg);

MonoException *
mono_get_exception_stack_overflow (void);

#endif /* _MONO_METADATA_EXCEPTION_H_ */
