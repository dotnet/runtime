#ifndef _MONO_CLI_CLI_H_
#define _MONO_CLI_CLI_H_ 1

#include <gmodule.h>
#include <ffi.h>
#include <mono/metadata/metadata.h>

typedef struct {
	guint16 iflags; /* method implementation flags */
	ffi_cif *cif;
	gpointer addr;
} MonoMethodPInvokeInfo;

typedef struct {
	guint16 flags; /* method flags */
	MonoImage *image;
	MonoMethodSignature  *signature;
	/* name is useful mostly for debugging */
	char *name; 
	union {
		MonoMetaMethodHeader *header;
		MonoMethodPInvokeInfo *piinfo;
	} data;
} MonoMethod;

MonoMethod        *mono_get_method    (MonoImage *image, guint32 token);
void               mono_free_method   (MonoMethod *method);

#include <mono/metadata/image.h>

MonoImage         *mono_load_image    (const char *fname, enum MonoImageOpenStatus *status);

#include <mono/cli/object.h>

#endif
