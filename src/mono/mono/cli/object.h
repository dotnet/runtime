#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

typedef struct {
} MonoClass;

typedef struct {
	MonoClass *klass;
} MonoObject;

void mono_object_new (metadata_t *m, int type
#endif
