#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/cli/class.h>

typedef struct {
	MonoClass *klass;
} MonoObject;

typedef struct {
	MonoObject obj;
	gint32 lower_bound;
	gint32 length;
	gint32 rank;
	gpointer vector;
} MonoArrayObject;

typedef struct {
	MonoObject obj;
	gint32 length;
	MonoArrayObject *c_str;
} MonoStringObject;

MonoObject *mono_object_new  (MonoImage *image, guint32 type_token);
void        mono_object_free (MonoObject *o);
		      
#endif

