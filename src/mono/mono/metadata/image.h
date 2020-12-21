/**
 * \file
 */

#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

#include <stdio.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-error.h>
#include <mono/metadata/object-forward.h>

MONO_BEGIN_DECLS

typedef struct _MonoAssembly MonoAssembly;
typedef struct _MonoAssemblyName MonoAssemblyName;
typedef struct _MonoTableInfo MonoTableInfo;

typedef enum {
	MONO_IMAGE_OK,
	MONO_IMAGE_ERROR_ERRNO,
	MONO_IMAGE_MISSING_ASSEMBLYREF,
	MONO_IMAGE_IMAGE_INVALID
} MonoImageOpenStatus;

MONO_API void          mono_images_init    (void);
MONO_API void          mono_images_cleanup (void);

MONO_API MonoImage    *mono_image_open     (const char *fname,
				   MonoImageOpenStatus *status);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage             *mono_image_open_full (const char *fname,
				   MonoImageOpenStatus *status, mono_bool refonly);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage             *mono_pe_file_open     (const char *fname,
				     MonoImageOpenStatus *status);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage    *mono_image_open_from_data (char *data, uint32_t data_len, mono_bool need_copy,
                                         MonoImageOpenStatus *status);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage    *mono_image_open_from_data_full (char *data, uint32_t data_len, mono_bool need_copy,
                                         MonoImageOpenStatus *status, mono_bool refonly);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage    *mono_image_open_from_data_with_name (char *data, uint32_t data_len, mono_bool need_copy,
                                                   MonoImageOpenStatus *status, mono_bool refonly, const char *name);
MONO_API void          mono_image_fixup_vtable (MonoImage *image);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage             *mono_image_loaded   (const char *name);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage             *mono_image_loaded_full   (const char *name, mono_bool refonly);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage             *mono_image_loaded_by_guid (const char *guid);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoImage             *mono_image_loaded_by_guid_full (const char *guid, mono_bool refonly);
MONO_API void          mono_image_init     (MonoImage *image);
MONO_API void          mono_image_close    (MonoImage *image);
MONO_API void          mono_image_addref   (MonoImage *image);
MONO_API const char   *mono_image_strerror (MonoImageOpenStatus status);

MONO_API int           mono_image_ensure_section     (MonoImage *image,
					     const char *section);
MONO_API int           mono_image_ensure_section_idx (MonoImage *image,
					     int section);

MONO_API uint32_t       mono_image_get_entry_point    (MonoImage *image);
MONO_API const char   *mono_image_get_resource       (MonoImage *image, uint32_t offset, uint32_t *size);
MONO_API MONO_RT_EXTERNAL_ONLY MonoImage*    mono_image_load_file_for_image (MonoImage *image, int fileidx);

MONO_API MONO_RT_EXTERNAL_ONLY MonoImage*    mono_image_load_module (MonoImage *image, int idx);

MONO_API const char*   mono_image_get_name       (MonoImage *image);
MONO_API const char*   mono_image_get_filename   (MonoImage *image);
MONO_API const char *  mono_image_get_guid       (MonoImage *image);
MONO_API MonoAssembly* mono_image_get_assembly   (MonoImage *image);
MONO_API mono_bool     mono_image_is_dynamic     (MonoImage *image);
MONO_API char*         mono_image_rva_map        (MonoImage *image, uint32_t rva);

MONO_API const MonoTableInfo *mono_image_get_table_info (MonoImage *image, int table_id);
MONO_API int                  mono_image_get_table_rows (MonoImage *image, int table_id);
MONO_API int                  mono_table_info_get_rows  (const MonoTableInfo *table);

/* This actually returns a MonoPEResourceDataEntry *, but declaring it
 * causes an include file loop.
 */
MONO_API void*      mono_image_lookup_resource (MonoImage *image, uint32_t res_id,
					  uint32_t lang_id, mono_unichar2 *name);

MONO_API const char*   mono_image_get_public_key  (MonoImage *image, uint32_t *size);
MONO_API const char*   mono_image_get_strong_name (MonoImage *image, uint32_t *size);
MONO_API uint32_t       mono_image_strong_name_position (MonoImage *image, uint32_t *size);
MONO_API void          mono_image_add_to_name_cache (MonoImage *image, 
			const char *nspace, const char *name, uint32_t idx);
MONO_API mono_bool     mono_image_has_authenticode_entry (MonoImage *image);

mono_bool mono_has_pdb_checksum (char *raw_data, uint32_t raw_data_len);

MONO_END_DECLS

#endif
