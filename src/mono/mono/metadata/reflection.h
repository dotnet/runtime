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
} MonoDynamicStream;

typedef struct {
	char *name;
	char *fname;
	GList *types;
} MonoModuleBuilder;

typedef struct {
	char *name;
	char *nspace;
	int attrs;
	int has_default_ctor;
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
	MonoType *ret;
	MonoType **params;
} MonoMethodBuilder;

typedef struct {
	guint32 rows;
	guint32 row_size; /*  calculated later with column_sizes */
	guint32 columns;
	guint32 column_sizes [9]; 
	guint32 *values; /* rows * columns */
} MonoDynamicTable;

typedef struct {
	GHashTable *hash;
	char *data;
	guint32 index;
	guint32 alloc_size;
} MonoStringHeap;

typedef struct {
	char *name;
	MonoAssembly *assembly;
	GList *modules;
	MonoStringHeap sheap;
	MonoDynamicStream us;
	MonoDynamicStream blob;
	MonoDynamicStream tstream;
	MonoDynamicStream guid;
	MonoDynamicTable tables [64];
} MonoDynamicAssembly;

int           mono_image_get_header (MonoDynamicAssembly *assembly, char *buffer, int maxsize);

#endif /* __METADATA_REFLECTION_H__ */

