
typedef struct {
	guint32 sh_offset;
	guint32 sh_size;
} stream_header_t;

enum {
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
	META_TABLE_NESTEDCLASS
};

typedef struct {
	guint32   rows, row_size;
	char     *base;
} metadata_tableinfo_t;

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
void  mono_metadata_compute_table_bases (metadata_t *meta);

char *mono_metadata_locate       (metadata_t *meta, int table, int idx);
char *mono_metadata_locate_token (metadata_t *meta, guint32 token);
