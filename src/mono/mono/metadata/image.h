#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

#include <stdio.h>
#include <glib.h>

typedef struct _MonoImage MonoImage;

typedef struct {
	const char *name;
	const char *culture;
	const char *hash_value;
	guint32 hash_len;
	guint32 flags;
	gint16 major, minor, build, revision;
} MonoAssemblyName;

typedef struct {
	int   ref_count;
	char *basedir;
	MonoAssemblyName aname;

	MonoImage *image;
	MonoImage **modules;
	/* Load files here */
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
	char *name;
	const char *assembly_name;
	const char *module_name;
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

	/*
	 * The Assembly this image was loaded from.
	 */
	MonoAssembly *assembly;

	/*
	 * Indexed by method tokens and typedef tokens.
	 */
	GHashTable *method_cache;
	GHashTable *class_cache;
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

MonoImage    *mono_image_open     (const char *fname,
				   MonoImageOpenStatus *status);
MonoImage    *mono_image_loaded   (const char *name);
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

#endif
