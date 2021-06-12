/**
 * \file
 */

#ifndef __MONO_METADATA_INTERNALS_H__
#define __MONO_METADATA_INTERNALS_H__

#include "mono/utils/mono-forward-internal.h"
#include "mono/metadata/image.h"
#include "mono/metadata/blob.h"
#include "mono/metadata/cil-coff.h"
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
#include "mono/utils/refcount.h"

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
	unsigned int has_cmods : 1;  
	unsigned int byref    : 1;
	unsigned int pinned   : 1;  /* valid when included in a local var signature */
};

typedef struct {
	unsigned int required : 1;
	MonoType *type;
} MonoSingleCustomMod;

/* Aggregate custom modifiers can happen if a generic VAR or MVAR is inflated,
 * and both the VAR and the type that will be used to inflated it have custom
 * modifiers, but they come from different images.  (e.g. inflating 'class G<T>
 * {void Test (T modopt(IsConst) t);}' with 'int32 modopt(IsLong)' where G is
 * in image1 and the int32 is in image2.)
 *
 * Moreover, we can't just store an image and a type token per modifier, because
 * Roslyn and C++/CLI sometimes create modifiers that mention generic parameters that must be inflated, like:
 *     void .CL1`1.Test(!0 modopt(System.Nullable`1<!0>))
 * So we have to store a resolved MonoType*.
 *
 * Because the types come from different images, we allocate the aggregate
 * custom modifiers container object in the mempool of a MonoImageSet to ensure
 * that it doesn't have dangling image pointers.
 */
typedef struct {
	uint8_t count;
	MonoSingleCustomMod modifiers[1]; /* Actual length is count */
} MonoAggregateModContainer;

/* ECMA says upto 64 custom modifiers.  It's possible we could see more at
 * runtime due to modifiers being appended together when we inflate type.  In
 * that case we should revisit the places where this define is used to make
 * sure that we don't blow up the stack (or switch to heap allocation for
 * temporaries).
 */
#define MONO_MAX_EXPECTED_CMODS 64

typedef struct {
	MonoType unmodified;
	gboolean is_aggregate;
	union {
		MonoCustomModContainer cmods;
		/* the actual aggregate modifiers are in a MonoImageSet mempool
		 * that includes all the images of all the modifier types and
		 * also the type that this aggregate container is a part of.*/
		MonoAggregateModContainer *amods;
	} mods;
} MonoTypeWithModifiers;

gboolean
mono_type_is_aggregate_mods (const MonoType *t);

static inline void
mono_type_with_mods_init (MonoType *dest, uint8_t num_mods, gboolean is_aggregate)
{
	if (num_mods == 0) {
		dest->has_cmods = 0;
		return;
	}
	dest->has_cmods = 1;
	MonoTypeWithModifiers *dest_full = (MonoTypeWithModifiers *)dest;
	dest_full->is_aggregate = !!is_aggregate;
	if (is_aggregate)
		dest_full->mods.amods = NULL;
	else
		dest_full->mods.cmods.count = num_mods;
}

MonoCustomModContainer *
mono_type_get_cmods (const MonoType *t);

MonoAggregateModContainer *
mono_type_get_amods (const MonoType *t);

void
mono_type_set_amods (MonoType *t, MonoAggregateModContainer *amods);

static inline uint8_t
mono_type_custom_modifier_count (const MonoType *t)
{
	if (!t->has_cmods)
		return 0;
	MonoTypeWithModifiers *full = (MonoTypeWithModifiers *)t;
	if (full->is_aggregate)
		return full->mods.amods->count;
	else
		return full->mods.cmods.count;
}

MonoType *
mono_type_get_custom_modifier (const MonoType *ty, uint8_t idx, gboolean *required, MonoError *error);

// Note: sizeof (MonoType) is dangerous. It can copy the num_mods
// field without copying the variably sized array. This leads to
// memory unsafety on the stack and/or heap, when we try to traverse
// this array.
//
// Use mono_sizeof_monotype 
// to get the size of the memory to copy.
#define MONO_SIZEOF_TYPE sizeof (MonoType)

size_t 
mono_sizeof_type_with_mods (uint8_t num_mods, gboolean aggregate);

size_t 
mono_sizeof_type (const MonoType *ty);

size_t
mono_sizeof_aggregate_modifiers (uint8_t num_mods);

MonoAggregateModContainer *
mono_metadata_get_canonical_aggregate_modifiers (MonoAggregateModContainer *candidate);

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
	int32_t major, minor, build, revision, arch;
	//Add members for correct work with mono_stringify_assembly_name
	MonoBoolean without_version;
	MonoBoolean without_culture;
	MonoBoolean without_public_key_token;
};

struct MonoTypeNameParse {
	char *name_space;
	char *name;
	MonoAssemblyName assembly;
	GList *modifiers; /* 0 -> byref, -1 -> pointer, > 0 -> array rank */
	GPtrArray *type_arguments;
	GList *nested;
};


typedef enum MonoAssemblyContextKind {
	/* Default assembly context: Load(String) and assembly references */
	MONO_ASMCTX_DEFAULT = 0,
	/* LoadFrom context: LoadFrom() and references */
	MONO_ASMCTX_LOADFROM = 1,
	/* Individual assembly context (.NET Framework docs call this "not in
	 * any context"): LoadFile(String) and Load(byte[]) are here.
	 */
	MONO_ASMCTX_INDIVIDUAL = 2,
	/* Used internally by the runtime, not visible to managed code */
	MONO_ASMCTX_INTERNAL = 3,

	MONO_ASMCTX_LAST = 3
} MonoAssemblyContextKind;

typedef struct _MonoAssemblyContext {
	MonoAssemblyContextKind kind;
} MonoAssemblyContext;

struct _MonoAssembly {
	/* 
	 * The number of appdomains which have this assembly loaded plus the number of 
	 * assemblies referencing this assembly through an entry in their image->references
	 * arrays. The latter is needed because entries in the image->references array
	 * might point to assemblies which are only loaded in some appdomains, and without
	 * the additional reference, they can be freed at any time.
	 * The ref_count is initially 0.
	 */
	gint32 ref_count; /* use atomic operations only */
	char *basedir;
	MonoAssemblyName aname;
	MonoImage *image;
	GSList *friend_assembly_names; /* Computed by mono_assembly_load_friends () */
	GSList *ignores_checks_assembly_names; /* Computed by mono_assembly_load_friends () */
	guint8 friend_assembly_names_inited;
	guint8 dynamic;
	MonoAssemblyContext context;
	guint8 wrap_non_exception_throws;
	guint8 wrap_non_exception_throws_inited;
	guint8 jit_optimizer_disabled;
	guint8 jit_optimizer_disabled_inited;
	/* security manager flags (one bit is for lazy initialization) */
	guint32 skipverification:2;	/* Has SecurityPermissionFlag.SkipVerification permission */
};

typedef struct {
	const char* data;
	guint32  size;
} MonoStreamHeader;

struct _MonoTableInfo {
	const char *base;
	guint       rows_     : 24;	/* don't access directly, use table_info_get_rows */

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

typedef struct {
	gboolean (*match) (MonoImage*);
	gboolean (*load_pe_data) (MonoImage*);
	gboolean (*load_cli_data) (MonoImage*);
	gboolean (*load_tables) (MonoImage*);
} MonoImageLoader;

/* Represents the physical bytes for an image (usually in the file system, but
 * could be in memory).
 *
 * The MonoImageStorage owns the raw data for an image and is responsible for
 * cleanup.
 *
 * May be shared by multiple MonoImage objects if they opened the same
 * underlying file or byte blob in memory.
 *
 * There is an abstract string key (usually a file path, but could be formed in
 * other ways) that is used to share MonoImageStorage objects among images.
 *
 */
typedef struct {
	MonoRefCount ref;

	/* key used for lookups.  owned by this image storage. */
	char *key;

	/* If the raw data was allocated from a source such as mmap, the allocator may store resource tracking information here. */
	void *raw_data_handle;
	char *raw_data;
	guint32 raw_data_len;
	/* data was allocated with mono_file_map and must be unmapped */
	guint8 raw_buffer_used    : 1;
	/* data was allocated with malloc and must be freed */
	guint8 raw_data_allocated : 1;
	/* data was allocated with mono_file_map_fileio */
	guint8 fileio_used : 1;

#ifdef HOST_WIN32
	/* Module was loaded using LoadLibrary. */
	guint8 is_module_handle : 1;

	/* Module entry point is _CorDllMain. */
	guint8 has_entry_point : 1;
#endif
} MonoImageStorage;

struct _MonoImage {
	/*
	 * This count is incremented during these situations:
	 *   - An assembly references this MonoImage through its 'image' field
	 *   - This MonoImage is present in the 'files' field of an image
	 *   - This MonoImage is present in the 'modules' field of an image
	 *   - A thread is holding a temporary reference to this MonoImage between
	 *     calls to mono_image_open and mono_image_close ()
	 */
	int   ref_count;

	MonoImageStorage *storage;

	/* Aliases storage->raw_data when storage is non-NULL. Otherwise NULL. */
	char *raw_data;
	guint32 raw_data_len;

	/* Whenever this is a dynamically emitted module */
	guint8 dynamic : 1;

	/* Whenever this image contains uncompressed metadata */
	guint8 uncompressed_metadata : 1;

	/* Whenever this image contains metadata only without PE data */
	guint8 metadata_only : 1;

	/*  Whether this image belongs to load-from context */
	guint8 load_from_context: 1;

	guint8 checked_module_cctor : 1;
	guint8 has_module_cctor : 1;

	guint8 idx_string_wide : 1;
	guint8 idx_guid_wide : 1;
	guint8 idx_blob_wide : 1;

	/* Whenever this image is considered as platform code for the CoreCLR security model */
	guint8 core_clr_platform_code : 1;

	/* Whether a #JTD stream was present. Indicates that this image was a minimal delta and its heaps only include the new heap entries */
	guint8 minimal_delta : 1;

	/* The path to the file for this image or an arbitrary name for images loaded from data. */
	char *name;

	/* The path to the file for this image or NULL */
	char *filename;

	/* The assembly name reported in the file for this image (expected to be NULL for a netmodule) */
	const char *assembly_name;

	/* The module name reported in the file for this image (could be NULL for a malformed file) */
	const char *module_name;
	guint32 time_date_stamp;

	char *version;
	gint16 md_version_major, md_version_minor;
	char *guid;
	MonoCLIImageInfo    *image_info;
	MonoMemPool         *mempool; /*protected by the image lock*/

	char                *raw_metadata;
			    
	MonoStreamHeader     heap_strings;
	MonoStreamHeader     heap_us;
	MonoStreamHeader     heap_blob;
	MonoStreamHeader     heap_guid;
	MonoStreamHeader     heap_tables;
	MonoStreamHeader     heap_pdb;
			    
	const char          *tables_base;

	/* For PPDB files */
	guint64 referenced_tables;
	int *referenced_table_rows;

	/**/
	MonoTableInfo        tables [MONO_TABLE_NUM];

	/*
	 * references is initialized only by using the mono_assembly_open
	 * function, and not by using the lowlevel mono_image_open.
	 *
	 * Protected by the image lock.
	 *
	 * It is NULL terminated.
	 */
	MonoAssembly **references;
	int nreferences;

	/* Code files in the assembly. The main assembly has a "file" table and also a "module"
	 * table, where the module table is a subset of the file table. We track both lists,
	 * and because we can lazy-load them at different times we reference-increment both.
	 */
	/* No netmodules in netcore, but for System.Reflection.Emit support we still use modules */
	MonoImage **modules;
	guint32 module_count;
	gboolean *modules_loaded;

	MonoImage **files;
	guint32 file_count;

	MonoAotModule *aot_module;

	guint8 aotid[16];

	/*
	 * The Assembly this image was loaded from.
	 */
	MonoAssembly *assembly;

	/*
	 * The AssemblyLoadContext that this image was loaded into.
	 */
	MonoAssemblyLoadContext *alc;

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
	MonoConcurrentHashTable *typespec_cache; /* protected by the image lock */
	/* indexed by token */
	GHashTable *memberref_signatures;

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
	 * indexed by SignaturePointerPair
	 */
	GHashTable *native_func_wrapper_cache;

	/*
	 * indexed by MonoMethod pointers 
	 */
	GHashTable *wrapper_param_names;
	GHashTable *array_accessor_cache;

	GHashTable *icall_wrapper_cache;
	GHashTable *rgctx_template_hash; /* LOCKING: templates lock */

	/* Contains rarely used fields of runtime structures belonging to this image */
	MonoPropertyHash *property_hash;

	void *reflection_info;

	/*
	 * user_info is a public field and is not touched by the
	 * metadata engine
	 */
	void *user_info;

#ifndef DISABLE_DLLMAP
	/* dll map entries */
	MonoDllMap *dll_map;
#endif

	/* interfaces IDs from this image */
	/* protected by the classes lock */
	MonoBitSet *interface_bitset;

	/* when the image is being closed, this is abused as a list of
	   malloc'ed regions to be freed. */
	GSList *reflection_info_unregister_classes;

	/* List of dependent image sets containing this image */
	/* Protected by image_sets_lock */
	GSList *image_sets;

	/* Caches for wrappers that DO NOT reference generic */
	/* arguments */
	MonoWrapperCaches wrapper_caches;

	/* Pre-allocated anon generic params for the first N generic
	 * parameters, for a small N */
	MonoGenericParam *var_gparam_cache_fast;
	MonoGenericParam *mvar_gparam_cache_fast;
	/* Anon generic parameters past N, if needed */
	MonoConcurrentHashTable *var_gparam_cache;
	MonoConcurrentHashTable *mvar_gparam_cache;

	/* The loader used to load this image */
	MonoImageLoader *loader;

	// Containers for MonoGenericParams associated with this image but not with any specific class or method. Created on demand.
	// This could happen, for example, for MonoTypes associated with TypeSpec table entries.
	MonoGenericContainer *anonymous_generic_class_container;
	MonoGenericContainer *anonymous_generic_method_container;

	gboolean weak_fields_inited;
	/* Contains 1 based indexes */
	GHashTable *weak_field_indexes;

#ifdef ENABLE_METADATA_UPDATE
	/* List of MonoImages of deltas.  Parent image owns 1 refcount ref of the delta image */
	GList *delta_image;
	/* Tail of delta_image for fast appends */
	GList *delta_image_last;

	/* Metadata delta images only */
	uint32_t generation; /* global update ID that added this delta image */

	/* Maps MethodDef token indices to something. In base images a boolean
	 * flag that there's an update for the method; in delta images a
	 * pointer into the RVA of the delta IL */
	GHashTable *method_table_update;


#endif

	/*
	 * No other runtime locks must be taken while holding this lock.
	 * It's meant to be used only to mutate and query structures part of this image.
	 */
	mono_mutex_t    lock;
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

/* "Dynamic" assemblies and images arise from System.Reflection.Emit */
struct _MonoDynamicAssembly {
	MonoAssembly assembly;
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
	GHashTable *blob_cache;
	GHashTable *standalonesig_cache;
	GList *array_methods;
	GHashTable *method_aux_hash;
	GHashTable *vararg_aux_hash;
	MonoGHashTable *generic_def_objects;
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
	MonoBitSet  *volatile_args;
	MonoBitSet  *volatile_locals;
	MonoType    *locals [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	const unsigned char *code;
	guint32      code_size;
	guint16      max_stack;
	gboolean     has_clauses;
	gboolean     has_locals;
} MonoMethodHeaderSummary;

// FIXME? offsetof (MonoMethodHeader, locals)?
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
	unsigned int  suppress_gc_transition : 1;
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

static inline int
table_info_get_rows (const MonoTableInfo *table)
{
	return table->rows_;
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

char*
mono_image_strdup_vprintf (MonoImage *image, const char *format, va_list args);

char*
mono_image_strdup_printf (MonoImage *image, const char *format, ...) MONO_ATTR_FORMAT_PRINTF(2,3);

GList*
mono_g_list_prepend_image (MonoImage *image, GList *list, gpointer data);

GSList*
mono_g_slist_append_image (MonoImage *image, GSList *list, gpointer data);

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
mono_install_image_loader (const MonoImageLoader *loader);

void
mono_image_append_class_to_reflection_info_set (MonoClass *klass);

#ifndef ENABLE_METADATA_UPDATE
static inline gboolean
mono_metadata_has_updates (void)
{
	return FALSE;
}

static inline void
mono_image_effective_table (const MonoTableInfo **t, int *idx)
{
}
#else /* ENABLE_METADATA_UPDATE */
extern int mono_metadata_update_has_updates_private;

/* returns TRUE if there's at least one update */
static inline gboolean
mono_metadata_has_updates (void)
{
	return mono_metadata_update_has_updates_private != 0;
}

void
mono_image_effective_table_slow (const MonoTableInfo **t, int *idx);

static inline void
mono_image_effective_table (const MonoTableInfo **t, int *idx)
{
	if (G_UNLIKELY (mono_metadata_has_updates ())) {
		if (G_UNLIKELY (*idx >= table_info_get_rows ((*t)))) {
			mono_image_effective_table_slow (t, idx);
		}
	}
}

int
mono_image_relative_delta_index (MonoImage *image_dmeta, int token);

void
mono_image_load_enc_delta (MonoImage *base_image, gconstpointer dmeta, uint32_t dmeta_len, gconstpointer dil, uint32_t dil_len, MonoError *error);
#endif /* ENABLE_METADATA_UPDATE */

gboolean
mono_image_load_cli_header (MonoImage *image, MonoCLIImageInfo *iinfo);

gboolean
mono_image_load_metadata (MonoImage *image, MonoCLIImageInfo *iinfo);

const char*
mono_metadata_string_heap_checked (MonoImage *meta, uint32_t table_index, MonoError *error);
const char *
mono_metadata_blob_heap_null_ok (MonoImage *meta, guint32 index);
const char*
mono_metadata_blob_heap_checked (MonoImage *meta, uint32_t table_index, MonoError *error);
gboolean
mono_metadata_decode_row_checked (const MonoImage *image, const MonoTableInfo *t, int idx, uint32_t *res, int res_size, MonoError *error);

gboolean
mono_metadata_decode_row_dynamic_checked (const MonoDynamicImage *image, const MonoDynamicTable *t, int idx, guint32 *res, int res_size, MonoError *error);

MonoType*
mono_metadata_get_shared_type (MonoType *type);

void
mono_metadata_clean_generic_classes_for_image (MonoImage *image);

#ifndef ENABLE_METADATA_UPDATE
static inline gboolean
mono_metadata_table_bounds_check (MonoImage *image, int table_index, int token_index)
{
	/* token_index is 1-based. TRUE means the token is out of bounds */
	return token_index > image->tables [table_index].rows_;
}
#else
gboolean
mono_metadata_table_bounds_check_slow (MonoImage *image, int table_index, int token_index);

/* token_index is 1-based */
static inline gboolean
mono_metadata_table_bounds_check (MonoImage *image, int table_index, int token_index)
{
	/* returns true if given index is not in bounds with provided table/index pair */
	if (G_LIKELY (token_index <= table_info_get_rows (&image->tables [table_index])))
		return FALSE;
	return mono_metadata_table_bounds_check_slow (image, table_index, token_index);
}
#endif

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
					     const char            *ptr,
						 MonoError *error);

MonoMethodSignature  *mono_metadata_parse_signature_checked (MonoImage *image, 
							     uint32_t    token,
							     MonoError *error);

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
					     const char           **rptr,
						 MonoError *error);

MonoGenericInst *
mono_metadata_get_generic_inst              (int 		    type_argc,
					     MonoType 		  **type_argv);

MonoGenericInst *
mono_metadata_get_canonical_generic_inst    (MonoGenericInst *candidate);

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
void mono_assembly_load_friends (MonoAssembly* ass);
gboolean mono_assembly_has_skip_verification (MonoAssembly* ass);

MONO_API gint32 
mono_assembly_addref (MonoAssembly *assembly);
gint32
mono_assembly_decref (MonoAssembly *assembly);

void mono_assembly_release_gc_roots (MonoAssembly *assembly);
gboolean mono_assembly_close_except_image_pools (MonoAssembly *assembly);
void mono_assembly_close_finish (MonoAssembly *assembly);


gboolean mono_public_tokens_are_equal (const unsigned char *pubt1, const unsigned char *pubt2);

void mono_config_parse_publisher_policy (const char *filename, MonoAssemblyBindingInfo *binding_info);

gboolean
mono_assembly_name_parse_full 		     (const char	   *name,
					      MonoAssemblyName	   *aname,
					      gboolean save_public_key,
					      gboolean *is_version_defined,
						  gboolean *is_token_defined);

gboolean
mono_assembly_fill_assembly_name_full (MonoImage *image, MonoAssemblyName *aname, gboolean copyBlobs);

MONO_API guint32 mono_metadata_get_generic_param_row (MonoImage *image, guint32 token, guint32 *owner);

MonoGenericParam*
mono_metadata_create_anon_gparam (MonoImage *image, gint32 param_num, gboolean is_mvar);

void mono_unload_interface_ids (MonoBitSet *bitset);


MonoType *mono_metadata_type_dup (MonoImage *image, const MonoType *original);
MonoType *mono_metadata_type_dup_with_cmods (MonoImage *image, const MonoType *original, const MonoType *cmods_source);

MonoMethodSignature  *mono_metadata_signature_dup_full (MonoImage *image,MonoMethodSignature *sig);
MonoMethodSignature  *mono_metadata_signature_dup_mempool (MonoMemPool *mp, MonoMethodSignature *sig);
MonoMethodSignature  *mono_metadata_signature_dup_mem_manager (MonoMemoryManager *mem_manager, MonoMethodSignature *sig);
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

gboolean
mono_metadata_signature_equal_no_ret (MonoMethodSignature *sig1, MonoMethodSignature *sig2);

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

MonoImage *mono_image_open_raw (MonoAssemblyLoadContext *alc, const char *fname, MonoImageOpenStatus *status);

MonoImage *mono_image_open_metadata_only (MonoAssemblyLoadContext *alc, const char *fname, MonoImageOpenStatus *status);

MonoImage *mono_image_open_from_data_internal (MonoAssemblyLoadContext *alc, char *data, guint32 data_len, gboolean need_copy, MonoImageOpenStatus *status, gboolean metadata_only, const char *name, const char *filename);

MonoException *mono_get_exception_field_access_msg (const char *msg);

MonoException *mono_get_exception_method_access_msg (const char *msg);

MonoMethod* mono_method_from_method_def_or_ref (MonoImage *m, guint32 tok, MonoGenericContext *context, MonoError *error);

MonoMethod *mono_get_method_constrained_with_method (MonoImage *image, MonoMethod *method, MonoClass *constrained_class, MonoGenericContext *context, MonoError *error);
MonoMethod *mono_get_method_constrained_checked (MonoImage *image, guint32 token, MonoClass *constrained_class, MonoGenericContext *context, MonoMethod **cil_method, MonoError *error);

void mono_type_set_alignment (MonoTypeEnum type, int align);

MonoType *
mono_type_create_from_typespec_checked (MonoImage *image, guint32 type_spec, MonoError *error);

MonoMethodSignature*
mono_method_get_signature_checked (MonoMethod *method, MonoImage *image, guint32 token, MonoGenericContext *context, MonoError *error);
	
MonoMethod *
mono_get_method_checked (MonoImage *image, guint32 token, MonoClass *klass, MonoGenericContext *context, MonoError *error);

guint32
mono_metadata_localscope_from_methoddef (MonoImage *meta, guint32 index);

void
mono_wrapper_caches_free (MonoWrapperCaches *cache);

MonoWrapperCaches*
mono_method_get_wrapper_cache (MonoMethod *method);

MonoWrapperCaches*
mono_method_get_wrapper_cache (MonoMethod *method);

MonoType*
mono_metadata_parse_type_checked (MonoImage *m, MonoGenericContainer *container, short opt_attrs, gboolean transient, const char *ptr, const char **rptr, MonoError *error);

MonoGenericContainer *
mono_get_anonymous_container_for_image (MonoImage *image, gboolean is_mvar);

void
mono_loader_register_module (const char *name, MonoDl *module);

void
mono_ginst_get_desc (GString *str, MonoGenericInst *ginst);

void
mono_loader_set_strict_assembly_name_check (gboolean enabled);

gboolean
mono_loader_get_strict_assembly_name_check (void);

gboolean
mono_type_in_image (MonoType *type, MonoImage *image);

gboolean
mono_type_is_valid_generic_argument (MonoType *type);

MonoAssemblyContextKind
mono_asmctx_get_kind (const MonoAssemblyContext *ctx);

void
mono_metadata_get_class_guid (MonoClass* klass, uint8_t* guid, MonoError *error);

#define MONO_CLASS_IS_INTERFACE_INTERNAL(c) ((mono_class_get_flags (c) & TYPE_ATTRIBUTE_INTERFACE) || mono_type_is_generic_parameter (m_class_get_byval_arg (c)))

static inline gboolean
m_image_is_raw_data_allocated (MonoImage *image)
{
	return image->storage ? image->storage->raw_data_allocated : FALSE;
}

static inline gboolean
m_image_is_fileio_used (MonoImage *image)
{
	return image->storage ? image->storage->fileio_used : FALSE;
}

#ifdef HOST_WIN32
static inline gboolean
m_image_is_module_handle (MonoImage *image)
{
	return image->storage ? image->storage->is_module_handle : FALSE;
}

static inline gboolean
m_image_has_entry_point (MonoImage *image)
{
	return image->storage ? image->storage->has_entry_point : FALSE;
}
#endif

static inline const char *
m_image_get_name (MonoImage *image)
{
	return image->name;
}

static inline const char *
m_image_get_filename (MonoImage *image)
{
	return image->filename;
}

static inline const char *
m_image_get_assembly_name (MonoImage *image)
{
	return image->assembly_name;
}

static inline
MonoAssemblyLoadContext *
mono_image_get_alc (MonoImage *image)
{
	return image->alc;
}

static inline
MonoAssemblyLoadContext *
mono_assembly_get_alc (MonoAssembly *assm)
{
	return mono_image_get_alc (assm->image);
}

static inline MonoType*
mono_signature_get_return_type_internal (MonoMethodSignature *sig)
{
	return sig->ret;
}

/**
 * mono_type_get_type_internal:
 * \param type the \c MonoType operated on
 * \returns the IL type value for \p type. This is one of the \c MonoTypeEnum
 * enum members like \c MONO_TYPE_I4 or \c MONO_TYPE_STRING.
 */
static inline int
mono_type_get_type_internal (MonoType *type)
{
	return type->type;
}

/**
 * mono_type_get_signature:
 * \param type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_FNPTR .
 * \returns the \c MonoMethodSignature pointer that describes the signature
 * of the function pointer \p type represents.
 */
static inline MonoMethodSignature*
mono_type_get_signature_internal (MonoType *type)
{
	g_assert (type->type == MONO_TYPE_FNPTR);
	return type->data.method;
}

/**
 * mono_type_is_byref_internal:
 * \param type the \c MonoType operated on
 * \returns TRUE if \p type represents a type passed by reference,
 * FALSE otherwise.
 */
static inline mono_bool
mono_type_is_byref_internal (MonoType *type)
{
	return type->byref;
}

/**
 * mono_type_get_class_internal:
 * \param type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_CLASS or a
 * \c MONO_TYPE_VALUETYPE . For more general functionality, use \c mono_class_from_mono_type_internal,
 * instead.
 * \returns the \c MonoClass pointer that describes the class that \p type represents.
 */
static inline MonoClass*
mono_type_get_class_internal (MonoType *type)
{
	/* FIXME: review the runtime users before adding the assert here */
	return type->data.klass;
}

/**
 * mono_type_get_array_type_internal:
 * \param type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_ARRAY .
 * \returns a \c MonoArrayType struct describing the array type that \p type
 * represents. The info includes details such as rank, array element type
 * and the sizes and bounds of multidimensional arrays.
 */
static inline MonoArrayType*
mono_type_get_array_type_internal (MonoType *type)
{
	return type->data.array;
}

#endif /* __MONO_METADATA_INTERNALS_H__ */
