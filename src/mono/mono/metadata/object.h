#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/metadata/class.h>

typedef struct {
	MonoClass *klass;
} MonoObject;

typedef struct {
	guint32 length;
	guint32 lower_bound;
} MonoArrayBounds;

typedef struct {
	MonoObject obj;
	gpointer vector;
	MonoArrayBounds *bounds;
} MonoArrayObject;

typedef struct {
	MonoObject obj;
	MonoArrayObject *c_str;
	gint32 length;
} MonoStringObject;

MonoObject *
mono_object_new       (MonoImage *image, guint32 type_token);

MonoObject *
mono_new_szarray      (MonoImage *image, guint32 etype, guint32 n);

MonoObject *
mono_new_utf16_string (const char *text, gint32 len);

MonoObject *
mono_new_string       (const char *text);

void       
mono_object_free      (MonoObject *o);

MonoObject *
mono_value_box        (MonoImage *image, guint32 type, gpointer val);
		      
MonoObject *
mono_object_clone     (MonoObject *obj);

#endif

