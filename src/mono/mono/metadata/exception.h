#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

#include <mono/metadata/object.h>
#include <mono/metadata/image.h>

typedef void (*MonoExceptionClassInitFunc)(MonoClass *klass);
typedef void (*MonoExceptionObjectInitFunc)(MonoObject *obj, MonoClass *klass);

extern void
mono_exception_install_handlers (MonoExceptionClassInitFunc class_init,
				 MonoExceptionObjectInitFunc obj_init);

extern MonoException *
mono_exception_from_name   (MonoImage *image, const char* name_space, const char *name);

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
mono_get_exception_execution_engine    (void);

MonoException *
mono_get_exception_invalid_cast        (void);

MonoException *
mono_get_exception_index_out_of_range  (void);

MonoException *
mono_get_exception_array_type_mismatch (void);

MonoException *
mono_get_exception_missing_method      (void);

MonoException*
mono_get_exception_argument_null       (const guchar *arg);

MonoException *
mono_get_exception_argument            (const guchar *arg, const guchar *msg);

MonoException *
mono_get_exception_io                  (const guchar *msg);

#endif /* _MONO_METADATA_EXCEPTION_H_ */
