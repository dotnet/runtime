#ifndef _MONONET_METADATA_IMAGE_H_ 
#define _MONONET_METADATA_IMAGE_H_

#include <stdio.h>
#include <glib.h>

typedef struct _MonoImage MonoImage;

typedef struct {
	int   ref_count;
	char *name;
	
	MonoImage *image;
	/* Load files here */
} MonoAssembly;

typedef struct {
	const char* data;
	guint32  size;
} MonoStreamHeader;

typedef struct {
	guint32   rows, row_size;
	char     *base;

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
	void *image_info;

	char                *raw_metadata;
			    
	gboolean             idx_string_wide, idx_guid_wide, idx_blob_wide;
			    
	MonoStreamHeader     heap_strings;
	MonoStreamHeader     heap_us;
	MonoStreamHeader     heap_blob;
	MonoStreamHeader     heap_guid;
	MonoStreamHeader     heap_tables;
			    
	char                *tables_base;

	MonoTableInfo        tables [64];

	/*
	 * references is initialized only by using the mono_assembly_open
	 * function, and not by using the lowlevel mono_image_open.
	 *
	 * It is NULL terminated.
	 */
	MonoAssembly **references;

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
MonoImage    *mono_image_loaded   (const char *name);
void          mono_image_close    (MonoImage *image);
const char   *mono_image_strerror (enum MonoImageOpenStatus status);

int           mono_image_ensure_section     (MonoImage *image,
					     const char *section);
int           mono_image_ensure_section_idx (MonoImage *image,
					     int section);
	
#endif
