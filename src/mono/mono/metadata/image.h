#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

typedef struct _MonoImage MonoImage;

struct _MonoImage {
	int   ref_count;
	FILE *f;
	char *name;
	void *image_info;

	/*
	 * references is initialized only by using the mono_assembly_open
	 * function, and not by using the lowlevel mono_image_open.
	 *
	 * It is NULL terminated.
	 */
	MonoImage **references;

	/*
	 * user_info is a public field and is not touched by the
	 * metadata engine
	 */
	void *user_info;
};

enum MonoImageOpenStatus {
	MONO_IMAGE_OK,
	MONO_IMAGE_ERROR_ERRNO,
	MONO_IMAGE_MISSING_ASSEMBLYREF,
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
