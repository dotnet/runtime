#ifndef __MONO_METADATA_VERIFY_H__
#define __MONO_METADATA_VERIFY_H__

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>

typedef enum {
	MONO_VERIFY_OK,
	MONO_VERIFY_ERROR,
	MONO_VERIFY_WARNING,
	MONO_VERIFY_CLS = 4,
	MONO_VERIFY_ALL = 7
} MonoVerifyStatus;

typedef struct {
	char            *message;
	MonoVerifyStatus status;
} MonoVerifyInfo;

GSList* mono_image_verify_tables (MonoImage *image, int level);
void    mono_free_verify_list    (GSList *list);

#endif  /* __MONO_METADATA_VERIFY_H__ */

