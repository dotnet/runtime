#ifndef _MONO_METADATA_METAPARSE_H_
#define _MONO_METADATA_METAPARSE_H__

typedef struct {
	guint32  cols [6];
	struct {
		guint32 first, last;
	} field;
	struct {
		guint32 first, last;
	} method;
} MonoTypedef;

void mono_typedef_get (MonoImage *image, guint32 tidx, MonoTypedef *ret);

#endif
