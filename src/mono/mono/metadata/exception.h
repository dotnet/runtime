#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

#include <mono/metadata/object.h>
#include <mono/metadata/image.h>

typedef void (*MonoExceptionClassInitFunc)(MonoClass *klass);
typedef void (*MonoExceptionObjectInitFunc)(MonoObject *obj, MonoClass *klass);

extern void
mono_exception_install_handlers(MonoExceptionClassInitFunc class_init,
				MonoExceptionObjectInitFunc obj_init);

extern MonoObject *
mono_exception_from_name   (MonoImage *image, const char* name_space, const char *name);

#endif /* _MONO_METADATA_EXCEPTION_H_ */
