
#ifndef __MONO_METADATA_H__
#define __MONO_METADATA_H__

#include <glib.h>

#include <mono/metadata/blob.h>
#include <mono/metadata/row-indexes.h>
#include <mono/metadata/image.h>

#ifdef __GNUC__
#define MONO_ZERO_LEN_ARRAY 0
#else
#define MONO_ZERO_LEN_ARRAY 1
#endif

typedef struct _MonoClass MonoClass;

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
	MONO_TABLE_NESTEDCLASS

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

int mono_metadata_compute_size (MonoMetadata   *meta,
                                int             tableindex,
                                guint32        *result_bitfield);

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
guint32 mono_metadata_nested_in_typedef   (MonoMetadata *meta, guint32 index);
guint32 mono_metadata_nesting_typedef     (MonoMetadata *meta, guint32 index);

MonoClass** mono_metadata_interfaces_from_typedef (MonoMetadata *meta, guint32 index, guint *count);

guint32     mono_metadata_properties_from_typedef (MonoMetadata *meta, guint32 index, guint *end);
guint32     mono_metadata_methods_from_property   (MonoMetadata *meta, guint32 index, guint *end);

void        mono_metadata_field_info (MonoMetadata *meta, 
				      guint32       index,
				      guint32      *offset,
				      const char  **rva,
				      const char  **marshal_info);

guint32     mono_metadata_get_constant_index (MonoMetadata *meta, guint32 token);

/*
 * Functions to extract information from the Blobs
 */
guint32 mono_metadata_decode_value     (const char            *ptr,
                                        const char           **rptr);
guint32 mono_metadata_decode_blob_size (const char            *ptr,
                                        const char           **rptr);

void mono_metadata_encode_value (guint32 value, char *bug, char **endbuf);

#define MONO_OFFSET_IN_CLAUSE(clause,offset) \
	((clause)->try_offset <= (offset) && (offset) < ((clause)->try_offset + (clause)->try_len))
#define MONO_OFFSET_IN_HANDLER(clause,offset) \
	((clause)->handler_offset <= (offset) && (offset) < ((clause)->handler_offset + (clause)->handler_len))

typedef struct {
	guint32 flags;
	guint32 try_offset;
	guint32 try_len;
	guint32 handler_offset;
	guint32 handler_len;
	guint32 token_or_filter;
} MonoExceptionClause;

typedef struct _MonoType MonoType;
typedef struct _MonoArrayType MonoArrayType;
typedef struct _MonoMethodSignature MonoMethodSignature;

typedef struct {
	unsigned int required : 1;
	unsigned int token    : 31;
} MonoCustomMod;

struct _MonoArrayType {
	MonoType *type;
	int rank;
	int numsizes;
	int numlobounds;
	int *sizes;
	int *lobounds;
};

struct _MonoType {
	union {
		MonoClass *klass; /* for VALUETYPE and CLASS */
		MonoType *type;   /* for PTR and SZARRAY */
		MonoArrayType *array; /* for ARRAY */
		MonoMethodSignature *method;
	} data;
	unsigned int attrs    : 16; /* param attributes or field flags */
	unsigned int type     : 8;  /* ElementTypeEnum */
	unsigned int num_mods : 6;  /* max 64 modifiers follow at the end */
	unsigned int byref    : 1;
	unsigned int pinned   : 1;  /* valid when included in a local var signature */
	MonoCustomMod modifiers [MONO_ZERO_LEN_ARRAY]; /* this may grow */
};

struct _MonoMethodSignature {
	unsigned int  hasthis : 1;
	unsigned int  explicit_this   : 1;
	unsigned int  call_convention : 6;
	unsigned int  ref_count : 24;
	guint16       param_count;
	guint16       sentinelpos;
	MonoType     *ret;
	MonoType     *params [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	guint32      code_size;
	const unsigned char  *code;
	guint16      max_stack;
	unsigned int num_clauses : 15;
	/* if num_locals != 0, then the following apply: */
	unsigned int init_locals : 1;
	guint16      num_locals;
	MonoExceptionClause *clauses;
	MonoType    *locals [MONO_ZERO_LEN_ARRAY];
} MonoMethodHeader;

typedef enum {
	MONO_PARSE_TYPE,
	MONO_PARSE_MOD_TYPE,
	MONO_PARSE_LOCAL,
	MONO_PARSE_PARAM,
	MONO_PARSE_RET,
	MONO_PARSE_FIELD
} MonoParseTypeMode;

guint32     mono_metadata_parse_typedef_or_ref (MonoMetadata      *m,
                                                const char      *ptr,
                                                const char     **rptr);
int            mono_metadata_parse_custom_mod  (MonoMetadata      *m,
						MonoCustomMod   *dest,
						const char      *ptr,
						const char     **rptr);
MonoArrayType *mono_metadata_parse_array       (MonoMetadata      *m,
						const char      *ptr,
						const char     **rptr);
void           mono_metadata_free_array        (MonoArrayType     *array);
MonoType      *mono_metadata_parse_type        (MonoMetadata      *m,
						MonoParseTypeMode  mode,
						short              opt_attrs,
						const char        *ptr,
						const char       **rptr);
MonoType      *mono_metadata_parse_param       (MonoMetadata      *m,
						const char      *ptr,
						const char      **rptr);
MonoType      *mono_metadata_parse_ret_type    (MonoMetadata      *m,
						const char      *ptr,
						const char      **rptr);
MonoType      *mono_metadata_parse_field_type  (MonoMetadata      *m,
		                                short            field_flags,
						const char      *ptr,
						const char      **rptr);
void           mono_metadata_free_type         (MonoType        *type);
int            mono_type_size                  (MonoType        *type, 
						int             *alignment);
int            mono_type_stack_size            (MonoType        *type, 
						int             *alignment);

gboolean       mono_metadata_type_equal        (MonoType *t1, MonoType *t2);

MonoMethodSignature  *mono_metadata_parse_method_signature (MonoMetadata            *m,
                                                            int                    def,
                                                            const char            *ptr,
                                                            const char           **rptr);
void                  mono_metadata_free_method_signature  (MonoMethodSignature   *method);

gboolean          mono_metadata_signature_equal (MonoMethodSignature *sig1, 
						 MonoMethodSignature *sig2);

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
