#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

#include <stdio.h>
#include <glib.h>
#include <gmodule.h>

typedef struct _MonoImage MonoImage;
typedef struct _MonoAssembly MonoAssembly;
typedef struct _MonoTableInfo MonoTableInfo;

#define MONO_PUBLIC_KEY_TOKEN_LENGTH	17

typedef struct {
	const char *name;
	const char *culture;
	const char *hash_value;
	const guint8* public_key;
	// string of 16 hex chars + 1 NULL
	guchar public_key_token [MONO_PUBLIC_KEY_TOKEN_LENGTH];
	guint32 hash_alg;
	guint32 hash_len;
	guint32 flags;
	guint16 major, minor, build, revision;
} MonoAssemblyName;

typedef enum {
	MONO_IMAGE_OK,
	MONO_IMAGE_ERROR_ERRNO,
	MONO_IMAGE_MISSING_ASSEMBLYREF,
	MONO_IMAGE_IMAGE_INVALID
} MonoImageOpenStatus;

void          mono_images_init    (void);

MonoImage    *mono_image_open     (const char *fname,
				   MonoImageOpenStatus *status);
MonoImage    *mono_image_open_from_data (char *data, guint32 data_len, gboolean need_copy,
                                         MonoImageOpenStatus *status);
MonoImage    *mono_image_loaded   (const char *name);
MonoImage    *mono_image_loaded_by_guid (const char *guid);
void          mono_image_init     (MonoImage *image);
void          mono_image_close    (MonoImage *image);
void          mono_image_addref   (MonoImage *image);
const char   *mono_image_strerror (MonoImageOpenStatus status);

int           mono_image_ensure_section     (MonoImage *image,
					     const char *section);
int           mono_image_ensure_section_idx (MonoImage *image,
					     int section);

guint32       mono_image_get_entry_point    (MonoImage *image);
const char   *mono_image_get_resource       (MonoImage *image, guint32 offset, guint32 *size);
MonoImage*    mono_image_load_file_for_image (MonoImage *image, int fileidx);

const char*   mono_image_get_name       (MonoImage *image);
const char*   mono_image_get_filename   (MonoImage *image);
MonoAssembly* mono_image_get_assembly   (MonoImage *image);
gboolean      mono_image_is_dynamic     (MonoImage *image);
char*         mono_image_rva_map        (MonoImage *image, guint32 rva);

const MonoTableInfo *mono_image_get_table_info (MonoImage *image, int table_id);
int                  mono_image_get_table_rows (MonoImage *image, int table_id);
int                  mono_table_info_get_rows  (const MonoTableInfo *table);

/* This actually returns a MonoPEResourceDataEntry *, but declaring it
 * causes an include file loop.
 */
gpointer      mono_image_lookup_resource (MonoImage *image, guint32 res_id,
					  guint32 lang_id, gunichar2 *name);

const char*   mono_image_get_public_key  (MonoImage *image, guint32 *size);
const char*   mono_image_get_strong_name (MonoImage *image, guint32 *size);
guint32       mono_image_strong_name_position (MonoImage *image, guint32 *size);
void          mono_image_add_to_name_cache (MonoImage *image, 
											const char *nspace, 
											const char *name, guint32 idx);

#endif
