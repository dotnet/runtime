#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

typedef struct {
} MonoClass;

typedef struct {
	MonoClass *klass;
} MonoObject;

void *mono_object_new (MonoImage *image, guint32 type_token);
		      
#endif
