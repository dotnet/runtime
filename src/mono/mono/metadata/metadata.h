
#ifndef __MONO_METADATA_H__
#define __MONO_METADATA_H__

#include <glib.h>

#include <mono/metadata/blob.h>

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

void         mono_metadata_decode_row (metadata_tableinfo_t  *t,
				       int                    idx,
				       guint32               *res,
				       int                    res_size);

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
char          *mono_metadata_locate        (metadata_t *meta, int table, int idx);
char          *mono_metadata_locate_token  (metadata_t *meta, guint32 token);
					   
const char    *mono_metadata_string_heap   (metadata_t *meta, guint32 index);
const char    *mono_metadata_blob_heap     (metadata_t *meta, guint32 index);
const char    *mono_metadata_user_string   (metadata_t *meta, guint32 index);

/*
 * Functions to extract information from the Blobs
 */
const char  *mono_metadata_decode_value     (const char            *ptr,
                                             guint32               *len);
const char  *mono_metadata_decode_blob_size (const char            *xptr,
                                             int                   *size);

typedef enum {
	MONO_META_EXCEPTION_CLAUSE_NONE,
	MONO_META_EXCEPTION_CLAUSE_FILTER,
	MONO_META_EXCEPTION_CLAUSE_FINALLY,
	MONO_META_EXCEPTION_CLAUSE_FAULT
} MonoMetaExceptionEnum;

typedef enum {
	MONO_CALL_DEFAULT,
	MONO_CALL_C,
	MONO_CALL_STDCALL,
	MONO_CALL_THISCALL,
	MONO_CALL_FASTCALL,
	MONO_CALL_VARARG
} MonoCallConvention;

typedef struct {
	MonoMetaExceptionEnum kind;
	int n_clauses;
	void **clauses;
} MonoMetaExceptionHandler;

typedef struct _MonoType MonoType;
typedef struct _MonoFieldType MonoFieldType;
typedef struct _MonoArray MonoArray;
typedef struct _MonoMethodSignature MonoMethodSignature;

typedef struct {
	guchar mod;
	guint32 token;
} MonoCustomMod;

typedef struct {
	MonoType *type;
	int num_modifiers;
	MonoCustomMod modifiers[1]; /* this may grow */
} MonoModifiedType;

struct _MonoArray {
	MonoType *type;
	int rank;
	int numsizes;
	int numlobounds;
	int *sizes;
	int *lobounds;
};

struct _MonoType {
	guchar type; /* ElementTypeEnum */
	guchar custom_mod; /* for PTR and SZARRAY: use data.mtype instead of data.type */
	guchar byref; /* when included in a MonoRetType */
	guchar constraint; /* valid when included in a local var signature */
	union {
		guint32 token; /* for VALUETYPE and CLASS */
		MonoType *type;
		MonoModifiedType *mtype;
		MonoArray *array; /* for ARRAY */
		MonoMethodSignature *method;
	} data;
};

/*
 * A Field Type is a Type that might have an optional Custom Modifier.
 */
struct _MonoFieldType {
	MonoType type;
	MonoCustomMod custom_mod;
};

typedef struct {
	/* maybe use a union here: saves 4 bytes */
	MonoType *type; /* NULL for VOID */
	short param_attrs; /* 22.1.11 */
	char typedbyref;
	int num_modifiers;
	MonoCustomMod modifiers[1]; /* this may grow */
} MonoRetType;

/* MonoRetType is used also for params */
typedef MonoRetType MonoParam;

struct _MonoMethodSignature {
	char hasthis;
	char explicit_this;
	char call_convention;
	int param_count;
	int sentinelpos;
	MonoRetType *ret;
	MonoParam **params;
};

typedef struct {
	guint32     code_size;
	const char *code;
	short       max_stack;
	guint32     local_var_sig_tok;

	/* if local_var_sig_tok != 0, then the following apply: */
	unsigned int init_locals : 1;
	int         num_locals;
	MonoType  **locals;

	GList      *exception_handler_list;
} MonoMetaMethodHeader;

guint32     mono_metadata_parse_typedef_or_ref (metadata_t      *m,
                                                const char      *ptr,
                                                const char     **rptr);
int            mono_metadata_parse_custom_mod  (metadata_t      *m,
						MonoCustomMod   *dest,
						const char      *ptr,
						const char     **rptr);
MonoArray     *mono_metadata_parse_array       (metadata_t      *m,
						const char      *ptr,
						const char     **rptr);
void           mono_metadata_free_array        (MonoArray       *array);
MonoParam     *mono_metadata_parse_param       (metadata_t      *m,
						int              rettype,
						const char      *ptr,
						const char     **rptr);
void           mono_metadata_free_param        (MonoParam       *param);
MonoType      *mono_metadata_parse_type        (metadata_t      *m,
               					const char      *ptr,
               					const char     **rptr);
void           mono_metadata_free_type         (MonoType        *type);

MonoFieldType *mono_metadata_parse_field_type  (metadata_t      *m,
						const char      *ptr,
						const char      **rptr);
void           mono_metadata_free_field_type   (MonoFieldType   *field_type);
							

MonoMethodSignature  *mono_metadata_parse_method_signature (metadata_t            *m,
                                                            int                    def,
                                                            const char            *ptr,
                                                            const char           **rptr);
void                  mono_metadata_free_method_signature  (MonoMethodSignature   *method);

MonoMetaMethodHeader *mono_metadata_parse_mh (metadata_t *m, const char *ptr);
void                  mono_metadata_free_mh  (MonoMetaMethodHeader *mh);

/*
 * Makes a token based on a table and an index
 */
#define mono_metadata_make_token(table,idx) (((table) << 24)| idx)

/*
 * Returns the table index that this token encodes.
 */
#define mono_metadata_token_table(token) ((token) >> 24)

 /*
 * Returns the index that a token refers to
 */
#define mono_metadata_token_index(token) ((token & 0xffffff))


#define mono_metadata_token_code(token) ((token & 0xff000000))

/*
 * FIXME: put all of the table codes here
 */
enum {
	TOKEN_TABLE_XXX = 0
} MonoMetadataTableCodes;

#endif /* __MONO_METADATA_H__ */
