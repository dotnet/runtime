#ifndef _MONO_CLI_OBJECT_H_
#define _MONO_CLI_OBJECT_H_

#include <mono/metadata/class.h>
#include <mono/metadata/threads-types.h>

typedef struct {
	MonoClass *klass;
	MonoThreadsSync synchronisation;
} MonoObject;

typedef struct {
	guint32 length;
	guint32 lower_bound;
} MonoArrayBounds;

typedef struct {
	MonoObject obj;
	gpointer vector;
	MonoArrayBounds *bounds;
} MonoArray;

typedef struct {
	MonoObject obj;
	MonoArray *c_str;
	gint32 length;
} MonoString;

#define mono_array_length(array) ((array)->bounds->length)
#define mono_array_addr(array,type,index) ( ((char*)(array)->vector) + sizeof (type) * (index) )
#define mono_array_addr_with_size(array,size,index) ( ((char*)(array)->vector) + (size) * (index) )
#define mono_array_get(array,type,index) ( *(type*)mono_array_addr ((array), type, (index)) ) 
#define mono_array_set(array,type,index,value)	\
	do {	\
		type *__p = (type *) mono_array_addr ((array), type, (index));	\
		*__p = (value);	\
	} while (0)

MonoObject *
mono_object_new             (MonoClass *klass);

MonoObject *
mono_object_new_from_token  (MonoImage *image, guint32 token);

MonoObject *
mono_array_new              (MonoClass *eclass, guint32 n);

MonoString*
mono_string_new_utf16       (const guint16 *text, gint32 len);

MonoString*
mono_ldstr                  (MonoImage *image, guint32 index);

MonoString*
mono_string_is_interned     (MonoString *str);

MonoString*
mono_string_intern          (MonoString *str);

MonoString*
mono_string_new             (const char *text);

char *
mono_string_to_utf8         (MonoString *string_obj);

void       
mono_object_free            (MonoObject *o);

MonoObject *
mono_value_box              (MonoClass *klass, gpointer val);
		      
MonoObject *
mono_object_clone           (MonoObject *obj);

gboolean
mono_object_isinst          (MonoObject *obj, MonoClass *klass);

#endif

