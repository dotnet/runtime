
typedef struct {
	guint32  sh_offset;
	guint32  sh_size;
} stream_header_t;

typedef enum {
	META_TABLE_MODULE,
	META_TABLE_TYPEREF,
	META_TABLE_TYPEDEF,
	META_TABLE_UNUSED1,
	META_TABLE_FIELD,
	META_TABLE_UNUSED2,
	META_TABLE_METHOD,
	META_TABLE_UNUSED3,
	META_TABLE_PARAM,
	META_TABLE_INTERFACEIMPL,
	META_TABLE_MEMBERREF,
	META_TABLE_CONSTANT,
	META_TABLE_CUSTOMATTRIBUTE,
	META_TABLE_FIELDMARSHAL,
	META_TABLE_DECLSECURITY,
	META_TABLE_CLASSLAYOUT,
	META_TABLE_FIELDLAYOUT,
	META_TABLE_STANDALONESIG,
	META_TABLE_EVENTMAP,
	META_TABLE_UNUSED4,
	META_TABLE_EVENT,
	META_TABLE_PROPERTYMAP,
	META_TABLE_UNUSED5,
	META_TABLE_PROPERTY,
	META_TABLE_METHODSEMANTICS,
	META_TABLE_METHODIMPL,
	META_TABLE_MODULEREF,
	META_TABLE_TYPESPEC,
	META_TABLE_IMPLMAP,
	META_TABLE_FIELDRVA,
	META_TABLE_UNUSED6,
	META_TABLE_UNUSED7,
	META_TABLE_ASSEMBLY,
	META_TABLE_ASSEMBLYPROCESSOR,
	META_TABLE_ASSEMBLYOS,
	META_TABLE_ASSEMBLYREF,
	META_TABLE_ASSEMBLYREFPROCESSOR,
	META_TABLE_ASSEMBLYREFOS,
	META_TABLE_FILE,
	META_TABLE_EXPORTEDTYPE,
	META_TABLE_MANIFESTRESOURCE,
	META_TABLE_NESTEDCLASS,

#define META_TABLE_LAST META_TABLE_NESTEDCLASS
} MetaTableEnum;

typedef struct {
	guint32   rows, row_size;
	char     *base;

	/*
	 * Tables contain up to 9 rows and the possible sizes of the
	 * fields in the documentation are 1, 2 and 4 bytes.  So we
	 * can encode in 2 bits the size.
	 *
	 * A 32 bit value can encode the resulting size
	 *
	 * The top eight bits encode the number of columns in the table.
	 * we only need 4, but 8 is aligned no shift required. 
	 */
	guint32   size_bitfield;
} metadata_tableinfo_t;

/*
 * This macro is used to extract the size of the table encoded in
 * the size_bitfield of metadata_tableinfo_t.
 */
#define meta_table_size(bitfield,table) ((((bitfield) >> ((table)*2)) & 0x3) + 1)
#define meta_table_count(bitfield) ((bitfield) >> 24)

typedef struct {
	char                *raw_metadata;
			    
	gboolean             idx_string_wide, idx_guid_wide, idx_blob_wide;
			    
	stream_header_t      heap_strings;
	stream_header_t      heap_us;
	stream_header_t      heap_blob;
	stream_header_t      heap_guid;
	stream_header_t      heap_tables;
			    
	char                *tables_base;

	metadata_tableinfo_t tables [64];
} metadata_t;

/*
 * This enumeration is used to describe the data types in the metadata
 * tables
 */
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

	/* HasConstant:Parent pointer (Param, Field or Property) */
	MONO_MT_CONST_IDX,

	/* HasCustomAttribute index.  Indexes any table except CustomAttribute */
	MONO_MT_HASCAT_IDX,
	
	/* CustomAttributeType encoded index */
	MONO_MT_CAT_IDX,

	/* HasDeclSecurity index: TypeDef Method or Assembly */
	MONO_MT_HASDEC_IDX,

	/* Implementation coded index: File, Export AssemblyRef */
	MONO_MT_IMPL_IDX,

	/* HasFieldMarshal coded index: Field or Param table */
	MONO_MT_HFM_IDX,

	/* MemberForwardedIndex: Field or Method */
	MONO_MT_MF_IDX,

	/* TypeDefOrRef coded index: typedef, typeref, typespec */
	MONO_MT_TDOR_IDX,

	/* MemberRefParent coded index: typeref, moduleref, method, memberref, typesepc, typedef */
	MONO_MT_MRP_IDX,

	/* MethodDefOrRef coded index: Method or Member Ref table */
	MONO_MT_MDOR_IDX,

	/* HasSemantic coded index: Event or Property */
	MONO_MT_HS_IDX,

	/* ResolutionScope coded index: Module, ModuleRef, AssemblytRef, TypeRef */
	MONO_MT_RS_IDX
};

typedef struct {
	int   code;
	char *def;
} MonoMetaTable;

const char *mono_meta_table_name (int table);

/* Internal functions */
void           mono_metadata_compute_table_bases (metadata_t *meta);

MonoMetaTable *mono_metadata_get_table    (MetaTableEnum table);

/*
 *
 */
char          *mono_metadata_locate       (metadata_t *meta, int table, int idx);
char          *mono_metadata_locate_token (metadata_t *meta, guint32 token);

const char    *mono_metadata_string_heap  (metadata_t *meta, guint32 index);
const char    *mono_metadata_blob_heap    (metadata_t *meta, guint32 index);

typedef enum {
	MONO_META_EXCEPTION_CLAUSE_NONE,
	MONO_META_EXCEPTION_CLAUSE_FILTER,
	MONO_META_EXCEPTION_CLAUSE_FINALLY,
	MONO_META_EXCEPTION_CLAUSE_FAULT
} MonoMetaExceptionEnum;


typedef struct {
	MonoMetaExceptionEnum kind;
	int n_clauses;
	void **clauses;
} MonoMetaExceptionHandler;

typedef struct {
	guint32     code_size;
	const char *code;
	short       max_stack;
	guint32     local_var_sig_tok;

	/* if local_var_sig_tok != 0, then the following apply: */
	unsigned int init_locals : 1;

	GList      *exception_handler_list;
} MonoMetaMethodHeader;

MonoMetaMethodHeader *mono_metadata_parse_mh (const char *ptr);
void                  mono_metadata_free_mh  (MonoMetaMethodHeader *mh);
