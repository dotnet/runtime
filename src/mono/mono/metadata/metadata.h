
typedef struct {
	guint32 sh_offset;
	guint32 sh_size;
} stream_header_t;

typedef struct {
	char             *raw_metadata;

	stream_header_t   heap_strings;
	stream_header_t   heap_us;
	stream_header_t   heap_blob;
	stream_header_t   heap_guid;
	stream_header_t   heap_tables;

	guint32           rows [64];
} metadata_t;

enum {
	MONO_MT_END,

	/* Sized elements */
	MONO_MT_UINT32,
	MONO_MT_UINT16,
	MONO_MT_UINT8,

	/* Index into Blob heap */
	MONO_MT_BLOB_IDX,

	/* Index into String heap */
	MONO_MT_STRING_IDX,

	/* GUID index */
	MONO_MT_GUID_IDX,

	/* Pointer into a table */
	MONO_MT_TABLE_IDX,

	/* Constant:Parent pointer (Param, Field or Property) */
	MONO_MT_CONST_IDX, 
};

typedef struct {
	int   code;
	char *def;
} MonoMetaSchema;

const char *mono_meta_table_name (int table);
