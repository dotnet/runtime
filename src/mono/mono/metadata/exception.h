#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

#include <config.h>

#ifdef MONO_USE_EXC_TABLES
#define MONO_ARCH_SAVE_REGS __builtin_unwind_init ()
#else
#define MONO_ARCH_SAVE_REGS 
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/image.h>

typedef void (*MonoExceptionClassInitFunc)(MonoClass *klass);
typedef void (*MonoExceptionObjectInitFunc)(MonoObject *obj, MonoClass *klass);

extern void
mono_exception_install_handlers (MonoExceptionClassInitFunc class_init,
				 MonoExceptionObjectInitFunc obj_init);

extern MonoException *
mono_exception_from_name               (MonoImage *image, 
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
mono_get_exception_type_load           (void);

MonoException *
mono_get_exception_missing_method      (void);

MonoException *
mono_get_exception_not_implemented     (void);

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

#endif /* _MONO_METADATA_EXCEPTION_H_ */
