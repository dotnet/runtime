#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

typedef struct {
	FILE *f;
	char *name;
	void *image_info;

	/*
	 * user_info is a public field and is not touched by the
	 * metadata engine
	 */
	void *user_info;
} MonoImage;

enum MonoImageOpenStatus {
	MONO_IMAGE_OK,
	MONO_IMAGE_ERROR_ERRNO,
	MONO_IMAGE_IMAGE_INVALID
};

MonoImage    *mono_image_open     (const char *fname,
				   enum MonoImageOpenStatus *status);
void          mono_image_close    (MonoImage *image);
const char   *mono_image_strerror (enum MonoImageOpenStatus status);


int           mono_image_ensure_section     (MonoImage *image,
					     const char *section);
int           mono_image_ensure_section_idx (MonoImage *image,
					     int section);
	
#endif
