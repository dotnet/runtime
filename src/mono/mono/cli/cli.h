#ifndef _MONO_CLI_CLI_H_
#define _MONO_CLI_CLI_H_ 1

#include <ffi.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/cli/object.h>

typedef struct {
	guint16 flags;  /* method flags */
	guint16 iflags; /* method implementation flags */
	MonoImage *image;
	MonoMethodSignature  *signature;
	gpointer addr;
	/* name is useful mostly for debugging */
	const char *name; 
} MonoMethod;

typedef struct {
	MonoMethod method;
	MonoMethodHeader *header;
} MonoMethodNormal;

typedef struct {
	MonoMethod method;
	guint16 piflags;  /* pinvoke flags */
	ffi_cif *cif;
} MonoMethodPInvoke;


MonoMethod        *mono_get_method      (MonoImage *image, guint32 token);
void               mono_free_method     (MonoMethod *method);

guint32            mono_typedef_from_name (MonoImage *image, const char *name, 
					   const char *nspace, guint32 *mlist);

guint32            mono_get_string_class_info (guint *ttoken, MonoImage **cl);


MonoImage         *mono_load_image    (const char *fname, enum MonoImageOpenStatus *status);

gpointer           mono_lookup_internal_call (const char *name);

#endif
