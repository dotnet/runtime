
#ifndef __MONO_METADATA_INTERNALS_H__
#define __MONO_METADATA_INTERNALS_H__

#include "mono/metadata/image.h"
#include "mono/utils/mono-hash.h"

struct _MonoAssembly {
	int   ref_count;
	char *basedir;
	gboolean in_gac;
	MonoAssemblyName aname;
	GModule *aot_module;
	MonoImage *image;
	/* Load files here */
	gboolean dynamic;
};

typedef struct {
	const char* data;
	guint32  size;
} MonoStreamHeader;

struct _MonoTableInfo {
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
};

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
	guint32 module_count;

	MonoImage **files;

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
	 * Indexed by fielddef and memberref tokens
	 */
	GHashTable *field_cache;

	/* indexed by typespec tokens. */
	GHashTable *typespec_cache;
	/* indexed by token */
	GHashTable *memberref_signatures;
	GHashTable *helper_signatures;

	/*
	 * Indexed by MonoGenericInst.
	 */
	GHashTable *generic_inst_cache;

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

	/* dll map entries */
	GHashTable *dll_map;

	/* Whenever this is a dynamically emitted module */
	gboolean dynamic;
};

enum {
	MONO_SECTION_TEXT,
	MONO_SECTION_RSRC,
	MONO_SECTION_RELOC,
	MONO_SECTION_MAX
};

typedef struct {
	GHashTable *hash;
	char *data;
	guint32 alloc_size; /* malloced bytes */
	guint32 index;
	guint32 offset; /* from start of metadata */
} MonoDynamicStream;

typedef struct {
	guint32 alloc_rows;
	guint32 rows;
	guint32 row_size; /*  calculated later with column_sizes */
	guint32 columns;
	guint32 column_sizes [9]; 
	guint32 *values; /* rows * columns */
	guint32 next_idx;
} MonoDynamicTable;

struct _MonoDynamicAssembly {
	MonoAssembly assembly;
	gboolean run;
	gboolean save;
	char *strong_name;
	guint32 strong_name_size;
};

struct _MonoDynamicImage {
	MonoImage image;
	guint32 meta_size;
	guint32 text_rva;
	guint32 metadata_rva;
	guint32 image_base;
	guint32 cli_header_offset;
	guint32 iat_offset;
	guint32 idt_offset;
	guint32 ilt_offset;
	guint32 imp_names_offset;
	struct {
		guint32 rva;
		guint32 size;
		guint32 offset;
		guint32 attrs;
	} sections [MONO_SECTION_MAX];
	GHashTable *typespec;
	GHashTable *typeref;
	GHashTable *handleref;
	MonoGHashTable *tokens;
	MonoGHashTable *blob_cache;
	GList *array_methods;
	GPtrArray *gen_params;
	MonoGHashTable *token_fixups;
	MonoGHashTable *method_to_table_idx;
	MonoGHashTable *field_to_table_idx;
	MonoGHashTable *method_aux_hash;
	gboolean run;
	gboolean save;
	gboolean initial_image;
	guint32 pe_kind, machine;
	char *strong_name;
	guint32 strong_name_size;
	char *win32_res;
	guint32 win32_res_size;
	MonoDynamicStream pefile;
	MonoDynamicStream sheap;
	MonoDynamicStream code; /* used to store method headers and bytecode */
	MonoDynamicStream resources; /* managed embedded resources */
	MonoDynamicStream us;
	MonoDynamicStream blob;
	MonoDynamicStream tstream;
	MonoDynamicStream guid;
	MonoDynamicTable tables [64];
};


#endif /* __MONO_METADATA_INTERNALS_H__ */

