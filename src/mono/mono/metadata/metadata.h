
#ifndef __MONO_METADATA_H__
#define __MONO_METADATA_H__

#include <glib.h>

#include <mono/metadata/blob.h>
#include <mono/metadata/row-indexes.h>

#ifdef __GNUC__
#define MONO_ZERO_LEN_ARRAY 0
#else
#define MONO_ZERO_LEN_ARRAY 1
#endif

typedef enum {
	MONO_TABLE_MODULE,
	MONO_TABLE_TYPEREF,
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_UNUSED1,
	MONO_TABLE_FIELD,
	MONO_TABLE_UNUSED2,
	MONO_TABLE_METHOD,
	MONO_TABLE_UNUSED3,
	MONO_TABLE_PARAM,
	MONO_TABLE_INTERFACEIMPL,
	MONO_TABLE_MEMBERREF,
	MONO_TABLE_CONSTANT,
	MONO_TABLE_CUSTOMATTRIBUTE,
	MONO_TABLE_FIELDMARSHAL,
	MONO_TABLE_DECLSECURITY,
	MONO_TABLE_CLASSLAYOUT,
	MONO_TABLE_FIELDLAYOUT,
	MONO_TABLE_STANDALONESIG,
	MONO_TABLE_EVENTMAP,
	MONO_TABLE_UNUSED4,
	MONO_TABLE_EVENT,
	MONO_TABLE_PROPERTYMAP,
	MONO_TABLE_UNUSED5,
	MONO_TABLE_PROPERTY,
	MONO_TABLE_METHODSEMANTICS,
	MONO_TABLE_METHODIMPL,
	MONO_TABLE_MODULEREF,
	MONO_TABLE_TYPESPEC,
	MONO_TABLE_IMPLMAP,
	MONO_TABLE_FIELDRVA,
	MONO_TABLE_UNUSED6,
	MONO_TABLE_UNUSED7,
	MONO_TABLE_ASSEMBLY,
	MONO_TABLE_ASSEMBLYPROCESSOR,
	MONO_TABLE_ASSEMBLYOS,
	MONO_TABLE_ASSEMBLYREF,
	MONO_TABLE_ASSEMBLYREFPROCESSOR,
	MONO_TABLE_ASSEMBLYREFOS,
	MONO_TABLE_FILE,
	MONO_TABLE_EXPORTEDTYPE,
	MONO_TABLE_MANIFESTRESOURCE,
	MONO_TABLE_NESTEDCLASS,

#define MONO_TABLE_LAST MONO_TABLE_NESTEDCLASS
} MonoMetaTableEnum;

typedef enum {
	MONO_EXCEPTION_CLAUSE_NONE,
	MONO_EXCEPTION_CLAUSE_FILTER,
	MONO_EXCEPTION_CLAUSE_FINALLY,
	MONO_EXCEPTION_CLAUSE_FAULT = 4
} MonoExceptionEnum;

typedef enum {
	MONO_CALL_DEFAULT,
	MONO_CALL_C,
	MONO_CALL_STDCALL,
	MONO_CALL_THISCALL,
	MONO_CALL_FASTCALL,
	MONO_CALL_VARARG
} MonoCallConvention;

typedef struct {
	guint32  offset;
	guint32  size;
} MonoStreamHeader;

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
} MonoTableInfo;

void         mono_metadata_decode_row (MonoTableInfo         *t,
				       int                    idx,
				       guint32               *res,
				       int                    res_size);

guint32      mono_metadata_decode_row_col (MonoTableInfo *t, 
					   int            idx, 
					   guint          col);

/*
 * This macro is used to extract the size of the table encoded in
 * the size_bitfield of MonoTableInfo.
 */
#define mono_metadata_table_size(bitfield,table) ((((bitfield) >> ((table)*2)) & 0x3) + 1)
#define mono_metadata_table_count(bitfield) ((bitfield) >> 24)

typedef struct {
	char                *raw_metadata;
			    
	gboolean             idx_string_wide, idx_guid_wide, idx_blob_wide;
			    
	MonoStreamHeader     heap_strings;
	MonoStreamHeader     heap_us;
	MonoStreamHeader     heap_blob;
	MonoStreamHeader     heap_guid;
	MonoStreamHeader     heap_tables;
			    
	char                *tables_base;

	MonoTableInfo        tables [64];
} MonoMetadata;

/*
 *
 */
char          *mono_metadata_locate        (MonoMetadata *meta, int table, int idx);
char          *mono_metadata_locate_token  (MonoMetadata *meta, guint32 token);
					   
const char    *mono_metadata_string_heap   (MonoMetadata *meta, guint32 index);
const char    *mono_metadata_blob_heap     (MonoMetadata *meta, guint32 index);
const char    *mono_metadata_user_string   (MonoMetadata *meta, guint32 index);

guint32 mono_metadata_typedef_from_field  (MonoMetadata *meta, guint32 index);
guint32 mono_metadata_typedef_from_method (MonoMetadata *meta, guint32 index);

/*
 * Functions to extract information from the Blobs
 */
guint32 mono_metadata_decode_value     (const char            *ptr,
                                        const char           **rptr);
guint32 mono_metadata_decode_blob_size (const char            *ptr,
                                        const char           **rptr);

typedef struct {
	guint32 flags;
	guint32 try_offset;
	guint32 try_len;
	guint32 handler_offset;
	guint32 handler_len;
	guint32 token_or_filter;
} MonoExceptionClause;

typedef struct _MonoType MonoType;
typedef struct _MonoArray MonoArray;
typedef struct _MonoMethodSignature MonoMethodSignature;

typedef struct {
	guchar mod;
	guint32 token;
} MonoCustomMod;

typedef struct {
	MonoType *type;
	int num_modifiers;
	MonoCustomMod modifiers [MONO_ZERO_LEN_ARRAY]; /* this may grow */
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

typedef struct {
	/* maybe use a union here: saves 4 bytes */
	MonoType *type; /* NULL for VOID */
	short param_attrs; /* 22.1.11 */
	char typedbyref;
	char num_modifiers;
	MonoCustomMod modifiers [MONO_ZERO_LEN_ARRAY]; /* this may grow */
} MonoRetType;

/* MonoRetType is used also for params and fields */
typedef MonoRetType MonoParam;
typedef MonoRetType MonoFieldType;

struct _MonoMethodSignature {
	char          hasthis;
	char          explicit_this;
	char          call_convention;
	guint16       param_count;
	guint16       sentinelpos;
	MonoRetType  *ret;
	MonoParam   **params;
	guint32       params_size;
};

typedef struct {
	guint32      code_size;
	const unsigned char  *code;
	guint16      max_stack;
	unsigned int num_clauses : 15;
	/* if num_locals != 0, then the following apply: */
	unsigned int init_locals : 1;
	guint16      num_locals;
	MonoType   **locals;
	guint32      locals_size;
	MonoExceptionClause *clauses;
} MonoMethodHeader;

guint32     mono_metadata_parse_typedef_or_ref (MonoMetadata      *m,
                                                const char      *ptr,
                                                const char     **rptr);
int            mono_metadata_parse_custom_mod  (MonoMetadata      *m,
						MonoCustomMod   *dest,
						const char      *ptr,
						const char     **rptr);
MonoArray     *mono_metadata_parse_array       (MonoMetadata      *m,
						const char      *ptr,
						const char     **rptr);
void           mono_metadata_free_array        (MonoArray       *array);
MonoParam     *mono_metadata_parse_param       (MonoMetadata      *m,
						int              rettype,
						const char      *ptr,
						const char     **rptr);
void           mono_metadata_free_param        (MonoParam       *param);
MonoType      *mono_metadata_parse_type        (MonoMetadata      *m,
               					const char      *ptr,
               					const char     **rptr);
void           mono_metadata_free_type         (MonoType        *type);
int            mono_type_size                  (MonoType        *type, 
						int             *alignment);

MonoFieldType *mono_metadata_parse_field_type  (MonoMetadata      *m,
						const char      *ptr,
						const char      **rptr);
							

MonoMethodSignature  *mono_metadata_parse_method_signature (MonoMetadata            *m,
                                                            int                    def,
                                                            const char            *ptr,
                                                            const char           **rptr);
void                  mono_metadata_free_method_signature  (MonoMethodSignature   *method);

MonoMethodHeader *mono_metadata_parse_mh (MonoMetadata *m, const char *ptr);
void              mono_metadata_free_mh  (MonoMethodHeader *mh);

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

guint32 mono_metadata_token_from_dor (guint32 dor_index);

#endif /* __MONO_METADATA_H__ */
