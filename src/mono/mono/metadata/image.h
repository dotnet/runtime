#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

#include <stdio.h>
#include <glib.h>
#include <gmodule.h>

typedef struct _MonoImage MonoImage;

typedef struct {
	const char *name;
	const char *culture;
	const char *hash_value;
	const guint8* public_key;
	guint32 hash_alg;
	guint32 hash_len;
	guint32 flags;
	guint16 major, minor, build, revision;
} MonoAssemblyName;

typedef struct {
	int   ref_count;
	char *basedir;
	MonoAssemblyName aname;
	GModule *aot_module;
	MonoImage *image;
	/* Load files here */
	void *dynamic;
} MonoAssembly;

typedef struct {
	const char* data;
	guint32  size;
} MonoStreamHeader;

typedef struct {
	guint32   rows, row_size;
	const char *base;

	/*
	 * Tables contain up to 9 columns and the possible sizes of the
	 * fields in the documentation are 1, 2 and 4 bytes.  So we
	 * can encode in 2 bits the size.
	 *
	 * A 32 bit value can encode the resulting size
	 *
	 * The top eight bits encode the number of columns in the table.
	 * we only need 4, but 8 is aligned no shift required. 
	 */
	guint32   size_bitfield;
} MonoTableInfo;

struct _MonoImage {
	int   ref_count;
	FILE *f;
	/* if f is NULL the image was loaded from raw data */
	char *raw_data;
	guint32 raw_data_len;
	gboolean raw_data_allocated;
	char *name;
	const char *assembly_name;
	const char *module_name;
	const char *version;
	char *guid;
	void *image_info;

	char                *raw_metadata;
			    
	gboolean             idx_string_wide, idx_guid_wide, idx_blob_wide;
			    
	MonoStreamHeader     heap_strings;
	MonoStreamHeader     heap_us;
	MonoStreamHeader     heap_blob;
	MonoStreamHeader     heap_guid;
	MonoStreamHeader     heap_tables;
			    
	const char          *tables_base;

	MonoTableInfo        tables [64];

	/*
	 * references is initialized only by using the mono_assembly_open
	 * function, and not by using the lowlevel mono_image_open.
	 *
	 * It is NULL terminated.
	 */
	MonoAssembly **references;

	MonoImage **modules;

	/*
	 * The Assembly this image was loaded from.
	 */
	MonoAssembly *assembly;

	/*
	 * Indexed by method tokens and typedef tokens.
	 */
	GHashTable *method_cache;
	GHashTable *class_cache;

	/* indexed by a generic type instantiation */
	GHashTable *generics_cache;
	/* indexed by typespec tokens. */
	GHashTable *typespec_cache;
	/*
	 * Indexes namespaces to hash tables that map class name to typedef token.
	 */
	GHashTable *name_cache;

	/*
	 * Indexed by ((rank << 24) | (typedef & 0xffffff)), which limits us to a
	 * maximal rank of 255
	 */
	GHashTable *array_cache;

	/*
	 * indexed by MonoMethodSignature 
	 */
	GHashTable *delegate_begin_invoke_cache;
	GHashTable *delegate_end_invoke_cache;
	GHashTable *delegate_invoke_cache;

	/*
	 * indexed by MonoMethod pointers 
	 */
	GHashTable *runtime_invoke_cache;
	GHashTable *managed_wrapper_cache;
	GHashTable *native_wrapper_cache;
	GHashTable *remoting_invoke_cache;
	GHashTable *synchronized_cache;

	void *reflection_info;

	/*
	 * user_info is a public field and is not touched by the
	 * metadata engine
	 */
	void *user_info;
};

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
const char   *mono_image_strerror (MonoImageOpenStatus status);

int           mono_image_ensure_section     (MonoImage *image,
					     const char *section);
int           mono_image_ensure_section_idx (MonoImage *image,
					     int section);

guint32       mono_image_get_entry_point    (MonoImage *image);
const char   *mono_image_get_resource       (MonoImage *image, guint32 offset, guint32 *size);

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
