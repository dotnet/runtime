#ifndef _MONO_METADATA_LOADER_H_
#define _MONO_METADATA_LOADER_H_ 1

#include <ffi.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>

typedef struct _MonoClass MonoClass;

typedef struct {
	guint16 flags;  /* method flags */
	guint16 iflags; /* method implementation flags */
	MonoClass *klass;
	MonoMethodSignature *signature;
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

typedef struct {
	MonoImage *corlib;
	guint32    array_token;
	guint32    string_token;
	guint32    char_token;
} MonoDefaults;

extern MonoDefaults mono_defaults;

void
mono_init                  (void);

MonoMethod *
mono_get_method            (MonoImage *image, guint32 token, MonoClass *klass);

void               
mono_free_method           (MonoMethod *method);

MonoImage *
mono_load_image            (const char *fname, enum MonoImageOpenStatus *status);

void
mono_add_internal_call     (const char *name, gpointer method);

gpointer
mono_lookup_internal_call  (const char *name);

#endif
