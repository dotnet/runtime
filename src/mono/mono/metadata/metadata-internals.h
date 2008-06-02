
#ifndef __MONO_METADATA_INTERNALS_H__
#define __MONO_METADATA_INTERNALS_H__

#include "mono/metadata/image.h"
#include "mono/metadata/blob.h"
#include "mono/metadata/mempool.h"
#include "mono/metadata/domain-internals.h"
#include "mono/utils/mono-hash.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/monobitset.h"
#include "mono/utils/mono-property-hash.h"
#include "mono/utils/mono-value-hash.h"

#define MONO_SECMAN_FLAG_INIT(x)		(x & 0x2)
#define MONO_SECMAN_FLAG_GET_VALUE(x)		(x & 0x1)
#define MONO_SECMAN_FLAG_SET_VALUE(x,y)		do { x = ((y) ? 0x3 : 0x2); } while (0)

struct _MonoAssembly {
	/* 
	 * The number of appdomains which have this assembly loaded plus the number of 
	 * assemblies referencing this assembly through an entry in their image->references
	 * arrays. The later is needed because entries in the image->references array
	 * might point to assemblies which are only loaded in some appdomains, and without
	 * the additional reference, they can be freed at any time.
	 * The ref_count is initially 0.
	 */
	int ref_count; /* use atomic operations only */
	char *basedir;
	MonoAssemblyName aname;
	MonoDl *aot_module;
	MonoImage *image;
	GSList *friend_assembly_names;
	guint8 in_gac;
	guint8 dynamic;
	guint8 corlib_internal;
	gboolean ref_only;
	/* security manager flags (one bit is for lazy initialization) */
	guint32 ecma:2;		/* Has the ECMA key */
	guint32 aptc:2;		/* Has the [AllowPartiallyTrustedCallers] attributes */
	guint32 fulltrust:2;	/* Has FullTrust permission */
	guint32 unmanaged:2;	/* Has SecurityPermissionFlag.UnmanagedCode permission */
	guint32 skipverification:2;	/* Has SecurityPermissionFlag.SkipVerification permission */
};

typedef struct {
	const char* data;
	guint32  size;
} MonoStreamHeader;

struct _MonoTableInfo {
	const char *base;
	guint       rows     : 24;
	guint       row_size : 8;

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

#define REFERENCE_MISSING ((gpointer) -1)

typedef struct _MonoDllMap MonoDllMap;

struct _MonoImage {
	/*
	 * The number of assemblies which reference this MonoImage though their 'image'
	 * field plus the number of images which reference this MonoImage through their 
	 * 'modules' field, plus the number of threads holding temporary references to
	 * this image between calls of mono_image_open () and mono_image_close ().
	 */
	int   ref_count;
	char *raw_data;
	guint32 raw_data_len;
	guint8 raw_buffer_used    : 1;
	guint8 raw_data_allocated : 1;

#ifdef PLATFORM_WIN32
	/* Module was loaded using LoadLibrary. */
	guint8 is_module_handle : 1;
#endif

	/* Whenever this is a dynamically emitted module */
	guint8 dynamic : 1;

	/* Whenever this is a reflection only image */
	guint8 ref_only : 1;

	/* Whenever this image contains uncompressed metadata */
	guint8 uncompressed_metadata : 1;

	guint8 checked_module_cctor : 1;
	guint8 has_module_cctor : 1;

	guint8 idx_string_wide : 1;
	guint8 idx_guid_wide : 1;
	guint8 idx_blob_wide : 1;
			    
	char *name;
	const char *assembly_name;
	const char *module_name;
	char *version;
	gint16 md_version_major, md_version_minor;
	char *guid;
	void *image_info;
	MonoMemPool         *mempool;

	char                *raw_metadata;
			    
	MonoStreamHeader     heap_strings;
	MonoStreamHeader     heap_us;
	MonoStreamHeader     heap_blob;
	MonoStreamHeader     heap_guid;
	MonoStreamHeader     heap_tables;
			    
	const char          *tables_base;

	/**/
	MonoTableInfo        tables [MONO_TABLE_NUM];

	/*
	 * references is initialized only by using the mono_assembly_open
	 * function, and not by using the lowlevel mono_image_open.
	 *
	 * It is NULL terminated.
	 */
	MonoAssembly **references;

	MonoImage **modules;
	guint32 module_count;
	gboolean *modules_loaded;

	MonoImage **files;

	/*
	 * The Assembly this image was loaded from.
	 */
	MonoAssembly *assembly;

	/*
	 * Indexed by method tokens and typedef tokens.
	 */
	MonoValueHashTable *method_cache;
	MonoInternalHashTable class_cache;

	/* Indexed by memberref + methodspec tokens */
	GHashTable *methodref_cache;

	/*
	 * Indexed by fielddef and memberref tokens
	 */
	GHashTable *field_cache;

	/* indexed by typespec tokens. */
	GHashTable *typespec_cache;
	/* indexed by token */
	GHashTable *memberref_signatures;
	GHashTable *helper_signatures;

	/* Indexed by blob heap indexes */
	GHashTable *method_signatures;

	/*
	 * Indexes namespaces to hash tables that map class name to typedef token.
	 */
	GHashTable *name_cache;

	/*
	 * Indexed by MonoClass
	 */
	GHashTable *array_cache;
	GHashTable *ptr_cache;

	/*
	 * indexed by MonoMethodSignature 
	 */
	GHashTable *delegate_begin_invoke_cache;
	GHashTable *delegate_end_invoke_cache;
	GHashTable *delegate_invoke_cache;
	GHashTable *runtime_invoke_cache;

	/*
	 * indexed by SignatureMethodPair
	 */
	GHashTable *delegate_abstract_invoke_cache;

	/*
	 * indexed by MonoMethod pointers 
	 */
	GHashTable *runtime_invoke_direct_cache;
	GHashTable *managed_wrapper_cache;
	GHashTable *native_wrapper_cache;
	GHashTable *remoting_invoke_cache;
	GHashTable *synchronized_cache;
	GHashTable *unbox_wrapper_cache;
	GHashTable *cominterop_invoke_cache;
	GHashTable *cominterop_wrapper_cache;
	GHashTable *static_rgctx_invoke_cache; /* LOCKING: marshal lock */
	GHashTable *thunk_invoke_cache;

	/*
	 * indexed by MonoClass pointers
	 */
	GHashTable *ldfld_wrapper_cache;
	GHashTable *ldflda_wrapper_cache;
	GHashTable *stfld_wrapper_cache;
	GHashTable *isinst_cache;
	GHashTable *castclass_cache;
	GHashTable *proxy_isinst_cache;
	GHashTable *rgctx_template_hash; /* LOCKING: templates lock */

	/*
	 * indexed by token and MonoGenericContext pointer
	 */
	GHashTable *generic_class_cache;

	/* Contains rarely used fields of runtime structures belonging to this image */
	MonoPropertyHash *property_hash;

	void *reflection_info;

	/*
	 * user_info is a public field and is not touched by the
	 * metadata engine
	 */
	void *user_info;

	/* dll map entries */
	MonoDllMap *dll_map;

	/* interfaces IDs from this image */
	MonoBitSet *interface_bitset;
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
	guint8  row_size; /*  calculated later with column_sizes */
	guint8  columns;
	guint32 next_idx;
	guint32 *values; /* rows * columns */
} MonoDynamicTable;

struct _MonoDynamicAssembly {
	MonoAssembly assembly;
	char *strong_name;
	guint32 strong_name_size;
	guint8 run;
	guint8 save;
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
	GHashTable *blob_cache;
	GList *array_methods;
	GPtrArray *gen_params;
	MonoGHashTable *token_fixups;
	GHashTable *method_to_table_idx;
	GHashTable *field_to_table_idx;
	GHashTable *method_aux_hash;
	MonoGHashTable *generic_def_objects;
	gboolean run;
	gboolean save;
	gboolean initial_image;
	guint32 pe_kind, machine;
	char *strong_name;
	guint32 strong_name_size;
	char *win32_res;
	guint32 win32_res_size;
	guint8 *public_key;
	int public_key_len;
	MonoDynamicStream sheap;
	MonoDynamicStream code; /* used to store method headers and bytecode */
	MonoDynamicStream resources; /* managed embedded resources */
	MonoDynamicStream us;
	MonoDynamicStream blob;
	MonoDynamicStream tstream;
	MonoDynamicStream guid;
	MonoDynamicTable tables [MONO_TABLE_NUM];
};

/* Contains information about assembly binding */
typedef struct _MonoAssemblyBindingInfo {
	char *name;
	char *culture;
	guchar public_key_token [MONO_PUBLIC_KEY_TOKEN_LENGTH];
	int major;
	int minor;
	AssemblyVersionSet old_version_bottom;
	AssemblyVersionSet old_version_top;
	AssemblyVersionSet new_version;
	guint has_old_version_bottom : 1;
	guint has_old_version_top : 1;
	guint has_new_version : 1;
	guint is_valid : 1;
} MonoAssemblyBindingInfo;

struct _MonoMethodHeader {
	guint32      code_size;
	const unsigned char  *code;
	guint16      max_stack;
	unsigned int num_clauses : 15;
	/* if num_locals != 0, then the following apply: */
	unsigned int init_locals : 1;
	guint16      num_locals;
	MonoExceptionClause *clauses;
	MonoType    *locals [MONO_ZERO_LEN_ARRAY];
};

/* for use with allocated memory blocks (assumes alignment is to 8 bytes) */
guint mono_aligned_addr_hash (gconstpointer ptr) MONO_INTERNAL;

void
mono_image_check_for_module_cctor (MonoImage *image) MONO_INTERNAL;

void
mono_metadata_clean_for_image (MonoImage *image) MONO_INTERNAL;

void
mono_metadata_cleanup (void);

const char *   mono_meta_table_name              (int table) MONO_INTERNAL;
void           mono_metadata_compute_table_bases (MonoImage *meta) MONO_INTERNAL;

gboolean
mono_metadata_interfaces_from_typedef_full  (MonoImage             *image,
											 guint32                table_index,
											 MonoClass           ***interfaces,
											 guint                 *count,
											 MonoGenericContext    *context) MONO_INTERNAL;

MonoArrayType *
mono_metadata_parse_array_full              (MonoImage             *image,
					     MonoGenericContainer  *container,
					     const char            *ptr,
					     const char           **rptr) MONO_INTERNAL;

MonoType *
mono_metadata_parse_type_full               (MonoImage             *image,
					     MonoGenericContainer  *container,
					     MonoParseTypeMode      mode,
					     short                  opt_attrs,
					     const char            *ptr,
					     const char           **rptr);

MonoMethodSignature *
mono_metadata_parse_signature_full          (MonoImage             *image,
					     MonoGenericContainer  *generic_container,
					     guint32                token) MONO_INTERNAL;

MonoMethodSignature *
mono_metadata_parse_method_signature_full   (MonoImage             *image,
					     MonoGenericContainer  *generic_container,
					     int                     def,
					     const char             *ptr,
					     const char            **rptr);

MonoMethodHeader *
mono_metadata_parse_mh_full                 (MonoImage             *image,
					     MonoGenericContainer  *container,
					     const char            *ptr);

int* mono_metadata_get_param_attrs          (MonoImage *m, int def);

guint
mono_metadata_generic_context_hash          (const MonoGenericContext *context) MONO_INTERNAL;

gboolean
mono_metadata_generic_context_equal         (const MonoGenericContext *g1,
					     const MonoGenericContext *g2) MONO_INTERNAL;

MonoGenericInst *
mono_metadata_parse_generic_inst            (MonoImage             *image,
					     MonoGenericContainer  *container,
					     int                    count,
					     const char            *ptr,
					     const char           **rptr) MONO_INTERNAL;

MonoGenericInst *
mono_metadata_get_generic_inst              (int 		    type_argc,
					     MonoType 		  **type_argv) MONO_INTERNAL;

MonoGenericClass *
mono_metadata_lookup_generic_class          (MonoClass		   *gclass,
					     MonoGenericInst	   *inst,
					     gboolean		    is_dynamic) MONO_INTERNAL;

MonoGenericInst *
mono_metadata_inflate_generic_inst          (MonoGenericInst       *ginst,
					     MonoGenericContext    *context) MONO_INTERNAL;

void mono_dynamic_stream_reset  (MonoDynamicStream* stream) MONO_INTERNAL;
void mono_assembly_addref       (MonoAssembly *assembly) MONO_INTERNAL;
void mono_assembly_load_friends (MonoAssembly* ass) MONO_INTERNAL;
gboolean mono_assembly_has_skip_verification (MonoAssembly* ass) MONO_INTERNAL;

gboolean mono_public_tokens_are_equal (const unsigned char *pubt1, const unsigned char *pubt2) MONO_INTERNAL;

void mono_config_parse_publisher_policy (const char *filename, MonoAssemblyBindingInfo *binding_info) MONO_INTERNAL;

gboolean
mono_assembly_name_parse_full 		     (const char	   *name,
					      MonoAssemblyName	   *aname,
					      gboolean save_public_key,
					      gboolean *is_version_defined,
						  gboolean *is_token_defined) MONO_INTERNAL;

guint32 mono_metadata_get_generic_param_row (MonoImage *image, guint32 token, guint32 *owner);

void mono_unload_interface_ids (MonoBitSet *bitset) MONO_INTERNAL;


MonoType *mono_metadata_type_dup (MonoMemPool *mp, const MonoType *original) MONO_INTERNAL;
MonoMethodSignature  *mono_metadata_signature_dup_full (MonoMemPool *mp,MonoMethodSignature *sig) MONO_INTERNAL;

MonoGenericInst *
mono_get_shared_generic_inst (MonoGenericContainer *container) MONO_INTERNAL;

int
mono_type_stack_size_internal (MonoType *t, int *align, gboolean allow_open) MONO_INTERNAL;

gboolean
mono_metadata_type_equal_full (MonoType *t1, MonoType *t2, gboolean signature_only) MONO_INTERNAL;


#endif /* __MONO_METADATA_INTERNALS_H__ */

