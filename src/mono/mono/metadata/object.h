#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/cli/class.h>

typedef struct {
	MonoClass *klass;
} MonoObject;

MonoObject *mono_object_new  (MonoImage *image, guint32 type_token);
void        mono_object_free (MonoObject *o);
		      
#endif

