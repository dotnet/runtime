
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

