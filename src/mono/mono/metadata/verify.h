#ifndef __MONO_METADATA_VERIFY_H__
#define __MONO_METADATA_VERIFY_H__

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>

G_BEGIN_DECLS

typedef enum {
	MONO_VERIFY_OK =0,
	MONO_VERIFY_ERROR=1,
	MONO_VERIFY_WARNING=2,
	MONO_VERIFY_VERIFIABLE=4,
	MONO_VERIFY_CLS = 8,
	MONO_VERIFY_ALL = 15
} MonoVerifyStatus;

typedef struct {
	char            *message;
	MonoVerifyStatus status;
} MonoVerifyInfo;

GSList* mono_image_verify_tables (MonoImage *image, int level);
GSList* mono_method_verify       (MonoMethod *method, int level);
void    mono_free_verify_list    (GSList *list);
char*   mono_verify_corlib       (void);

G_END_DECLS

#endif  /* __MONO_METADATA_VERIFY_H__ */

