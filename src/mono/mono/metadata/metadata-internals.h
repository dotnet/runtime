
#ifndef __MONO_METADATA_INTERNALS_H__
#define __MONO_METADATA_INTERNALS_H__

#include "mono/metadata/image.h"
#include "mono/metadata/blob.h"
#include "mono/metadata/mempool.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/mono-hash.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/monobitset.h"
#include "mono/utils/mono-property-hash.h"
#include "mono/utils/mono-value-hash.h"
#include <mono/utils/mono-error.h>
#include "mono/utils/mono-conc-hashtable.h"

struct _MonoType {
	union {
		MonoClass *klass; /* for VALUETYPE and CLASS */
		MonoType *type;   /* for PTR */
		MonoArrayType *array; /* for ARRAY */
		MonoMethodSignature *method;
		MonoGenericParam *generic_param; /* for VAR and MVAR */
		MonoGenericClass *generic_class; /* for GENERICINST */
	} data;
	unsigned int attrs    : 16; /* param attributes or field flags */
	MonoTypeEnum type     : 8;
	unsigned int num_mods : 6;  /* max 64 modifiers follow at the end */
	unsigned int byref    : 1;
	unsigned int pinned   : 1;  /* valid when included in a local var signature */
	MonoCustomMod modifiers [MONO_ZERO_LEN_ARRAY]; /* this may grow */
};

#define MONO_SIZEOF_TYPE (offsetof (struct _MonoType, modifiers))

#define MONO_SECMAN_FLAG_INIT(x)		(x & 0x2)
#define MONO_SECMAN_FLAG_GET_VALUE(x)		(x & 0x1)
#define MONO_SECMAN_FLAG_SET_VALUE(x,y)		do { x = ((y) ? 0x3 : 0x2); } while (0)

#define MONO_PUBLIC_KEY_TOKEN_LENGTH	17

#define MONO_PROCESSOR_ARCHITECTURE_NONE 0
#define MONO_PROCESSOR_ARCHITECTURE_MSIL 1
#define MONO_PROCESSOR_ARCHITECTURE_X86 2
#define MONO_PROCESSOR_ARCHITECTURE_IA64 3
#define MONO_PROCESSOR_ARCHITECTURE_AMD64 4
#define MONO_PROCESSOR_ARCHITECTURE_ARM 5

struct _MonoAssemblyName {
	const char *name;
	const char *culture;
	const char *hash_value;
	const mono_byte* public_key;
	// string of 16 hex chars + 1 NULL
	mono_byte public_key_token [MONO_PUBLIC_KEY_TOKEN_LENGTH];
	uint32_t hash_alg;
	uint32_t hash_len;
	uint32_t flags;
	uint16_t major, minor, build, revision, arch;
};

struct MonoTypeNameParse {
	char *name_space;
	char *name;
	MonoAssemblyName assembly;
	GList *modifiers; /* 0 -> byref, -1 -> pointer, > 0 -> array rank */
	GPtrArray *type_arguments;
	GList *nested;
};

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
	MonoImage *image;
	GSList *friend_assembly_names; /* Computed by mono_assembly_load_friends () */
	guint8 friend_assembly_names_inited;
	guint8 in_gac;
	guint8 dynamic;
	guint8 corlib_internal;
	gboolean ref_only;
	guint8 wrap_non_exception_throws;
	guint8 wrap_non_exception_throws_inited;
	guint8 jit_optimizer_disabled;
	guint8 jit_optimizer_disabled_inited;
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
	void *raw_data_handle;
	char *raw_data;
	guint32 raw_data_len;
	guint8 raw_buffer_used    : 1;
	guint8 raw_data_allocated : 1;
	guint8 fileio_used : 1;

#ifdef HOST_WIN32
	/* Module was loaded using LoadLibrary. */
	guint8 is_module_handle : 1;

	/* Module entry point is _CorDllMain. */
	guint8 has_entry_point : 1;
#endif

	/* Whenever this is a dynamically emitted module */
	guint8 dynamic : 1;

	/* Whenever this is a reflection only image */
	guint8 ref_only : 1;

	/* Whenever this image contains uncompressed metadata */
	guint8 uncompressed_metadata : 1;

	/* Whenever this image contains metadata only without PE data */
	guint8 metadata_only : 1;

	guint8 checked_module_cctor : 1;
	guint8 has_module_cctor : 1;

	guint8 idx_string_wide : 1;
	guint8 idx_guid_wide : 1;
	guint8 idx_blob_wide : 1;

	/* Whenever this image is considered as platform code for the CoreCLR security model */
	guint8 core_clr_platform_code : 1;
			    
	char *name;
	const char *assembly_name;
	const char *module_name;
	char *version;
	gint16 md_version_major, md_version_minor;
	char *guid;
	void *image_info;
	MonoMemPool         *mempool; /*protected by the image lock*/

	char                *raw_metadata;
			    
	MonoStreamHeader     heap_strings;
	MonoStreamHeader     heap_us;
	MonoStreamHeader     heap_blob;
	MonoStreamHeader     heap_guid;
	MonoStreamHeader     heap_tables;
	MonoStreamHeader     heap_pdb;
			    
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
	int nreferences;

	MonoImage **modules;
	guint32 module_count;
	gboolean *modules_loaded;

	MonoImage **files; /*protected by the image lock*/

	gpointer aot_module;

	/*
	 * The Assembly this image was loaded from.
	 */
	MonoAssembly *assembly;

	/*
	 * Indexed by method tokens and typedef tokens.
	 */
	GHashTable *method_cache; /*protected by the image lock*/
	MonoInternalHashTable class_cache;

	/* Indexed by memberref + methodspec tokens */
	GHashTable *methodref_cache; /*protected by the image lock*/

	/*
	 * Indexed by fielddef and memberref tokens
	 */
	MonoConcurrentHashTable *field_cache; /*protected by the image lock*/

	/* indexed by typespec tokens. */
	GHashTable *typespec_cache; /* protected by the image lock */
	/* indexed by token */
	GHashTable *memberref_signatures;
	GHashTable *helper_signatures;

	/* Indexed by blob heap indexes */
	GHashTable *method_signatures;

	/*
	 * Indexes namespaces to hash tables that map class name to typedef token.
	 */
	GHashTable *name_cache;  /*protected by the image lock*/

	/*
	 * Indexed by MonoClass
	 */
	GHashTable *array_cache;
	GHashTable *ptr_cache;

	GHashTable *szarray_cache;
	/* This has a separate lock to improve scalability */
	mono_mutex_t szarray_cache_lock;

	/*
	 * indexed by MonoMethodSignature 
	 */
	GHashTable *delegate_begin_invoke_cache;
	GHashTable *delegate_end_invoke_cache;
	GHashTable *delegate_invoke_cache;
	GHashTable *runtime_invoke_cache;
	GHashTable *runtime_invoke_vtype_cache;

	/*
	 * indexed by SignaturePointerPair
	 */
	GHashTable *delegate_abstract_invoke_cache;
	GHashTable *delegate_bound_static_invoke_cache;
	GHashTable *native_func_wrapper_cache;

	/*
	 * indexed by MonoMethod pointers 
	 */
	GHashTable *runtime_invoke_direct_cache;
	GHashTable *runtime_invoke_vcall_cache;
	GHashTable *managed_wrapper_cache;
	GHashTable *native_wrapper_cache;
	GHashTable *native_wrapper_aot_cache;
	GHashTable *native_wrapper_check_cache;
	GHashTable *native_wrapper_aot_check_cache;
	GHashTable *native_func_wrapper_aot_cache;
	GHashTable *remoting_invoke_cache;
	GHashTable *synchronized_cache;
	GHashTable *unbox_wrapper_cache;
	GHashTable *cominterop_invoke_cache;
	GHashTable *cominterop_wrapper_cache; /* LOCKING: marshal lock */
	GHashTable *thunk_invoke_cache;
	GHashTable *wrapper_param_names;
	GHashTable *synchronized_generic_cache;
	GHashTable *array_accessor_cache;

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
	GHashTable *delegate_invoke_generic_cache;
	GHashTable *delegate_begin_invoke_generic_cache;
	GHashTable *delegate_end_invoke_generic_cache;

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
	/* protected by the classes lock */
	MonoBitSet *interface_bitset;

	/* when the image is being closed, this is abused as a list of
	   malloc'ed regions to be freed. */
	GSList *reflection_info_unregister_classes;

	/* List of image sets containing this image */
	/* Protected by image_sets_lock */
	GSList *image_sets;

	/* Caches for MonoClass-es representing anon generic params */
	MonoClass **var_cache_fast;
	MonoClass **mvar_cache_fast;
	GHashTable *var_cache_slow;
	GHashTable *mvar_cache_slow;
	GHashTable *var_cache_constrained;
	GHashTable *mvar_cache_constrained;

	/* Maps malloc-ed char* pinvoke scope -> MonoDl* */
	GHashTable *pinvoke_scopes;

	/* Maps malloc-ed char* pinvoke scope -> malloced-ed char* filename */
	GHashTable *pinvoke_scope_filenames;

	/* Indexed by MonoGenericParam pointers */
	GHashTable **gshared_types;
	/* The length of the above array */
	int gshared_types_len;

	/*
	 * No other runtime locks must be taken while holding this lock.
	 * It's meant to be used only to mutate and query structures part of this image.
	 */
	mono_mutex_t    lock;
};

/*
 * Generic instances depend on many images, and they need to be deleted if one
 * of the images they depend on is unloaded. For example,
 * List<Foo> depends on both List's image and Foo's image.
 * A MonoImageSet is the owner of all generic instances depending on the same set of
 * images.
 */
typedef struct {
	int nimages;
	MonoImage **images;

	GHashTable *gclass_cache, *ginst_cache, *gmethod_cache, *gsignature_cache;

	mono_mutex_t    lock;

	/*
	 * Memory for generic instances owned by this image set should be allocated from
	 * this mempool, using the mono_image_set_alloc family of functions.
	 */
	MonoMemPool         *mempool;
} MonoImageSet;

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
	MonoDomain *domain;
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
	MonoGHashTable *handleref_managed;
	MonoGHashTable *tokens;
	GHashTable *blob_cache;
	GHashTable *standalonesig_cache;
	GList *array_methods;
	GPtrArray *gen_params;
	MonoGHashTable *token_fixups;
	GHashTable *method_to_table_idx;
	GHashTable *field_to_table_idx;
	GHashTable *method_aux_hash;
	GHashTable *vararg_aux_hash;
	MonoGHashTable *generic_def_objects;
	MonoGHashTable *methodspec;
	/*
	 * Maps final token values to the object they describe.
	 */
	MonoGHashTable *remapped_tokens;
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
	MonoClass *wrappers_type; /*wrappers are bound to this type instead of <Module>*/
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
	gint32 domain_id; /*Needed to unload per-domain binding*/
} MonoAssemblyBindingInfo;

struct _MonoMethodHeader {
	const unsigned char  *code;
#ifdef MONO_SMALL_CONFIG
	guint16      code_size;
#else
	guint32      code_size;
#endif
	guint16      max_stack   : 15;
	unsigned int is_transient: 1; /* mono_metadata_free_mh () will actually free this header */
	unsigned int num_clauses : 15;
	/* if num_locals != 0, then the following apply: */
	unsigned int init_locals : 1;
	guint16      num_locals;
	MonoExceptionClause *clauses;
	MonoType    *locals [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	guint32      code_size;
	gboolean     has_clauses;
} MonoMethodHeaderSummary;

#define MONO_SIZEOF_METHOD_HEADER (sizeof (struct _MonoMethodHeader) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

struct _MonoMethodSignature {
	MonoType     *ret;
#ifdef MONO_SMALL_CONFIG
	guint8        param_count;
	gint8         sentinelpos;
	unsigned int  generic_param_count : 5;
#else
	guint16       param_count;
	gint16        sentinelpos;
	unsigned int  generic_param_count : 16;
#endif
	unsigned int  call_convention     : 6;
	unsigned int  hasthis             : 1;
	unsigned int  explicit_this       : 1;
	unsigned int  pinvoke             : 1;
	unsigned int  is_inflated         : 1;
	unsigned int  has_type_parameters : 1;
	MonoType     *params [MONO_ZERO_LEN_ARRAY];
};

/*
 * AOT cache configuration loaded from config files.
 * Doesn't really belong here.
 */
typedef struct {
	/*
	 * Enable aot caching for applications whose main assemblies are in
	 * this list.
	 */
	GSList *apps;
	GSList *assemblies;
	char *aot_options;
} MonoAotCacheConfig;

#define MONO_SIZEOF_METHOD_SIGNATURE (sizeof (struct _MonoMethodSignature) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

static inline gboolean
image_is_dynamic (MonoImage *image)
{
#ifdef DISABLE_REFLECTION_EMIT
	return FALSE;
#else
	return image->dynamic;
#endif
}

static inline gboolean
assembly_is_dynamic (MonoAssembly *assembly)
{
#ifdef DISABLE_REFLECTION_EMIT
	return FALSE;
#else
	return assembly->dynamic;
#endif
}

/* for use with allocated memory blocks (assumes alignment is to 8 bytes) */
guint mono_aligned_addr_hash (gconstpointer ptr);

void
mono_image_check_for_module_cctor (MonoImage *image);

gpointer
mono_image_alloc  (MonoImage *image, guint size);

gpointer
mono_image_alloc0 (MonoImage *image, guint size);

#define mono_image_new0(image,type,size) ((type *) mono_image_alloc0 (image, sizeof (type)* (size)))

char*
mono_image_strdup (MonoImage *image, const char *s);

GList*
g_list_prepend_image (MonoImage *image, GList *list, gpointer data);

GSList*
g_slist_append_image (MonoImage *image, GSList *list, gpointer data);

void
mono_image_lock (MonoImage *image);

void
mono_image_unlock (MonoImage *image);

gpointer
mono_image_property_lookup (MonoImage *image, gpointer subject, guint32 property);

void
mono_image_property_insert (MonoImage *image, gpointer subject, guint32 property, gpointer value);

void
mono_image_property_remove (MonoImage *image, gpointer subject);

gboolean
mono_image_close_except_pools (MonoImage *image);

void
mono_image_close_finish (MonoImage *image);

typedef void  (*MonoImageUnloadFunc) (MonoImage *image, gpointer user_data);

void
mono_install_image_unload_hook (MonoImageUnloadFunc func, gpointer user_data);

void
mono_remove_image_unload_hook (MonoImageUnloadFunc func, gpointer user_data);

void
mono_image_append_class_to_reflection_info_set (MonoClass *klass);

gpointer
mono_image_set_alloc  (MonoImageSet *set, guint size);

gpointer
mono_image_set_alloc0 (MonoImageSet *set, guint size);

char*
mono_image_set_strdup (MonoImageSet *set, const char *s);

#define mono_image_set_new0(image,type,size) ((type *) mono_image_set_alloc0 (image, sizeof (type)* (size)))

MonoType*
mono_metadata_get_shared_type (MonoType *type);

void
mono_metadata_clean_for_image (MonoImage *image);

void
mono_metadata_clean_generic_classes_for_image (MonoImage *image);

MONO_API void
mono_metadata_cleanup (void);

const char *   mono_meta_table_name              (int table);
void           mono_metadata_compute_table_bases (MonoImage *meta);

gboolean
mono_metadata_interfaces_from_typedef_full  (MonoImage             *image,
											 guint32                table_index,
											 MonoClass           ***interfaces,
											 guint                 *count,
											 gboolean               heap_alloc_result,
											 MonoGenericContext    *context,
											 MonoError *error);

MonoArrayType *
mono_metadata_parse_array_full              (MonoImage             *image,
					     MonoGenericContainer  *container,
					     const char            *ptr,
					     const char           **rptr);

MONO_API MonoType *
mono_metadata_parse_type_full               (MonoImage             *image,
					     MonoGenericContainer  *container,
					     MonoParseTypeMode      mode,
					     short                  opt_attrs,
					     const char            *ptr,
					     const char           **rptr);

MONO_API MonoMethodSignature *
mono_metadata_parse_method_signature_full   (MonoImage             *image,
					     MonoGenericContainer  *generic_container,
					     int                     def,
					     const char             *ptr,
					     const char            **rptr,
					     MonoError *error);

MONO_API MonoMethodHeader *
mono_metadata_parse_mh_full                 (MonoImage             *image,
					     MonoGenericContainer  *container,
					     const char            *ptr);

gboolean
mono_method_get_header_summary (MonoMethod *method, MonoMethodHeaderSummary *summary);

int* mono_metadata_get_param_attrs          (MonoImage *m, int def, int param_count);
gboolean mono_metadata_method_has_param_attrs (MonoImage *m, int def);

guint
mono_metadata_generic_context_hash          (const MonoGenericContext *context);

gboolean
mono_metadata_generic_context_equal         (const MonoGenericContext *g1,
					     const MonoGenericContext *g2);

MonoGenericInst *
mono_metadata_parse_generic_inst            (MonoImage             *image,
					     MonoGenericContainer  *container,
					     int                    count,
					     const char            *ptr,
					     const char           **rptr);

MonoGenericInst *
mono_metadata_get_generic_inst              (int 		    type_argc,
					     MonoType 		  **type_argv);

MonoGenericClass *
mono_metadata_lookup_generic_class          (MonoClass		   *gclass,
					     MonoGenericInst	   *inst,
					     gboolean		    is_dynamic);

MonoGenericInst * mono_metadata_inflate_generic_inst  (MonoGenericInst *ginst, MonoGenericContext *context, MonoError *error);

guint
mono_metadata_generic_param_hash (MonoGenericParam *p);

gboolean
mono_metadata_generic_param_equal (MonoGenericParam *p1, MonoGenericParam *p2);

void mono_dynamic_stream_reset  (MonoDynamicStream* stream);
void mono_assembly_addref       (MonoAssembly *assembly);
void mono_assembly_load_friends (MonoAssembly* ass);
gboolean mono_assembly_has_skip_verification (MonoAssembly* ass);

void mono_assembly_release_gc_roots (MonoAssembly *assembly);
gboolean mono_assembly_close_except_image_pools (MonoAssembly *assembly);
void mono_assembly_close_finish (MonoAssembly *assembly);


gboolean mono_public_tokens_are_equal (const unsigned char *pubt1, const unsigned char *pubt2);

void mono_config_parse_publisher_policy (const char *filename, MonoAssemblyBindingInfo *binding_info);
void mono_config_parse_assembly_bindings (const char *filename, int major, int minor, void *user_data,
					  void (*infocb)(MonoAssemblyBindingInfo *info, void *user_data));

gboolean
mono_assembly_name_parse_full 		     (const char	   *name,
					      MonoAssemblyName	   *aname,
					      gboolean save_public_key,
					      gboolean *is_version_defined,
						  gboolean *is_token_defined);

MONO_API guint32 mono_metadata_get_generic_param_row (MonoImage *image, guint32 token, guint32 *owner);

void mono_unload_interface_ids (MonoBitSet *bitset);


MonoType *mono_metadata_type_dup (MonoImage *image, const MonoType *original);
MonoMethodSignature  *mono_metadata_signature_dup_full (MonoImage *image,MonoMethodSignature *sig);
MonoMethodSignature  *mono_metadata_signature_dup_mempool (MonoMemPool *mp, MonoMethodSignature *sig);
MonoMethodSignature  *mono_metadata_signature_dup_add_this (MonoImage *image, MonoMethodSignature *sig, MonoClass *klass);

MonoGenericInst *
mono_get_shared_generic_inst (MonoGenericContainer *container);

int
mono_type_stack_size_internal (MonoType *t, int *align, gboolean allow_open);

MONO_API void            mono_type_get_desc (GString *res, MonoType *type, mono_bool include_namespace);

gboolean
mono_metadata_type_equal_full (MonoType *t1, MonoType *t2, gboolean signature_only);

MonoMarshalSpec *
mono_metadata_parse_marshal_spec_full (MonoImage *image, MonoImage *parent_image, const char *ptr);

guint	       mono_metadata_generic_inst_hash (gconstpointer data);
gboolean       mono_metadata_generic_inst_equal (gconstpointer ka, gconstpointer kb);

MONO_API void
mono_metadata_field_info_with_mempool (
					  MonoImage *meta, 
				      guint32       table_index,
				      guint32      *offset,
				      guint32      *rva,
				      MonoMarshalSpec **marshal_spec);

MonoClassField*
mono_metadata_get_corresponding_field_from_generic_type_definition (MonoClassField *field);

MonoEvent*
mono_metadata_get_corresponding_event_from_generic_type_definition (MonoEvent *event);

MonoProperty*
mono_metadata_get_corresponding_property_from_generic_type_definition (MonoProperty *property);

guint32
mono_metadata_signature_size (MonoMethodSignature *sig);

guint mono_metadata_str_hash (gconstpointer v1);

gboolean mono_image_load_pe_data (MonoImage *image);

gboolean mono_image_load_cli_data (MonoImage *image);

void mono_image_load_names (MonoImage *image);

MonoImage *mono_image_open_raw (const char *fname, MonoImageOpenStatus *status);

MonoImage *mono_image_open_metadata_only (const char *fname, MonoImageOpenStatus *status);

MonoException *mono_get_exception_field_access_msg (const char *msg);

MonoException *mono_get_exception_method_access_msg (const char *msg);

MonoMethod* method_from_method_def_or_ref (MonoImage *m, guint32 tok, MonoGenericContext *context, MonoError *error);

MonoMethod *mono_get_method_constrained_with_method (MonoImage *image, MonoMethod *method, MonoClass *constrained_class, MonoGenericContext *context, MonoError *error);
MonoMethod *mono_get_method_constrained_checked (MonoImage *image, guint32 token, MonoClass *constrained_class, MonoGenericContext *context, MonoMethod **cil_method, MonoError *error);

void mono_type_set_alignment (MonoTypeEnum type, int align);

MonoAotCacheConfig *mono_get_aot_cache_config (void);
MonoType *
mono_type_create_from_typespec_checked (MonoImage *image, guint32 type_spec, MonoError *error);

MonoMethodSignature*
mono_method_get_signature_checked (MonoMethod *method, MonoImage *image, guint32 token, MonoGenericContext *context, MonoError *error);
	
MonoMethod *
mono_get_method_checked (MonoImage *image, guint32 token, MonoClass *klass, MonoGenericContext *context, MonoError *error);

guint32
mono_metadata_localscope_from_methoddef (MonoImage *meta, guint32 index);

#endif /* __MONO_METADATA_INTERNALS_H__ */

