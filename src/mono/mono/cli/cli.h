#ifndef _MONO_CLI_CLI_H_
#define _MONO_CLI_CLI_H_ 1

#include <mono/metadata/metadata.h>

typedef struct {
	MonoMetaMethodHeader *header;
	MonoMethodSignature  *signature;
	
	guint32 name_idx; 
} MonoMethod;

MonoMethod        *mono_get_method    (MonoImage *image, guint32 token);
void               mono_free_method   (MonoMethod *method);

#include <mono/metadata/image.h>
MonoImage         *mono_load_image    (const char *fname, enum MonoImageOpenStatus *status);

#endif
