#ifndef __METADATA_REFLECTION_H__
#define __METADATA_REFLECTION_H__

#include <mono/metadata/image.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class.h>

typedef struct {
	char *name;
	char *data;
	guint32 alloc_size; /* malloced bytes */
	guint32 index;
	guint32 offset; /* from start of metadata */
} MonoDynamicStream;

typedef struct {
	char *name;
	char *fname;
	GList *types;
	guint32 table_idx;
} MonoModuleBuilder;

typedef struct {
	char *name;
	char *nspace;
	int attrs;
	int has_default_ctor;
	guint32 table_idx;
	MonoType *base;
	GList *methods;
	GList *fields;
	GList *properties;
} MonoTypeBuilder;

typedef struct {
	char *name;
	guint32 attrs;
	guint32 callconv;
	guint32 nparams;
	guint32 table_idx;
	MonoType *ret;
	MonoType **params;
	char *code;
	guint32 code_size;
} MonoMethodBuilder;

typedef struct {
	guint32 rows;
	guint32 row_size; /*  calculated later with column_sizes */
	guint32 columns;
	guint32 column_sizes [9]; 
	guint32 *values; /* rows * columns */
	guint32 next_idx;
} MonoDynamicTable;

typedef struct {
	GHashTable *hash;
	char *data;
	guint32 index;
	guint32 alloc_size;
	guint32 offset; /* from start of metadata */
} MonoStringHeap;

typedef struct {
	MonoAssembly assembly;
	char *name;
	GList *modules;
	guint32 meta_size;
	guint32 text_rva;
	guint32 metadata_rva;
	MonoMethodBuilder *entry_point;
	GHashTable *typeref;
	MonoStringHeap sheap;
	MonoDynamicStream code; /* used to store method headers and bytecode */
	MonoDynamicStream us;
	MonoDynamicStream blob;
	MonoDynamicStream tstream;
	MonoDynamicStream guid;
	MonoDynamicTable tables [64];
} MonoDynamicAssembly;

int           mono_image_get_header (MonoDynamicAssembly *assembly, char *buffer, int maxsize);

#endif /* __METADATA_REFLECTION_H__ */

