#ifndef __METADATA_REFLECTION_H__
#define __METADATA_REFLECTION_H__

#include <mono/metadata/image.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>

typedef struct {
	char *name;
	char *data;
	guint32 alloc_size; /* malloced bytes */
	guint32 index;
	guint32 offset; /* from start of metadata */
} MonoDynamicStream;

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

/*
 * The followinbg structure must match the C# implementation in our corlib.
 */

typedef struct {
	MonoObject object;
	MonoType  *type;
} MonoReflectionType;

typedef struct {
	MonoObject object;
	MonoArray *code;
	MonoObject *mbuilder;
	gint32 code_len;
	gint32 max_stack;
	gint32 cur_stack;
	MonoArray *locals;
} MonoReflectionILGen;

typedef struct {
	MonoObject object;
	MonoReflectionType *type;
	MonoString *name;
} MonoReflectionLocalBuilder;

typedef struct {
	MonoObject object;
	MonoMethod *impl;
	MonoReflectionType *rtype;
	MonoArray *parameters;
	guint32 attrs;
	MonoString *name;
	guint32 table_idx;
	MonoArray *code;
	MonoReflectionILGen *ilgen;
	MonoObject *type;
} MonoReflectionMethodBuilder;

typedef struct {
	MonoAssembly assembly;
	guint32 meta_size;
	guint32 text_rva;
	guint32 metadata_rva;
	GHashTable *typeref;
	MonoStringHeap sheap;
	MonoDynamicStream code; /* used to store method headers and bytecode */
	MonoDynamicStream us;
	MonoDynamicStream blob;
	MonoDynamicStream tstream;
	MonoDynamicStream guid;
	MonoDynamicTable tables [64];
} MonoDynamicAssembly;

typedef struct {
	MonoObject object;
	MonoAssembly *assembly;
} MonoReflectionAssembly;

typedef struct {
	MonoReflectionAssembly assembly;
	MonoDynamicAssembly *dynamic_assembly;
	MonoReflectionMethodBuilder *entry_point;
	MonoArray *modules;
	MonoString *name;
} MonoReflectionAssemblyBuilder;

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoReflectionType *type;
	MonoString *name;
	MonoObject *def_value;
	gint32 offset;
	gint32 table_idx;
} MonoReflectionFieldBuilder;

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoString *name;
	MonoReflectionType *type;
	MonoArray *parameters;
	MonoObject *def_value;
	MonoReflectionMethodBuilder *set_method;
	MonoReflectionMethodBuilder *get_method;
	gint32 table_idx;
} MonoReflectionPropertyBuilder;

typedef struct {
	MonoReflectionType type;
	MonoString *name;
	MonoString *nspace;
	MonoReflectionType *parent;
	MonoArray *interfaces;
	MonoArray *methods;
	MonoArray *properties;
	MonoArray *fields;
	guint32 attrs;
	guint32 table_idx;
} MonoReflectionTypeBuilder;

typedef struct {
	MonoObject	obj;
	MonoImage  *image;
	MonoObject *assembly;
	MonoString *fqname;
	MonoString *name;
	MonoString *scopename;
} MonoReflectionModule;

typedef struct {
	MonoReflectionModule module;
	MonoArray *types;
	guint32    table_idx;
} MonoReflectionModuleBuilder;

int           mono_image_get_header (MonoReflectionAssemblyBuilder *assembly, char *buffer, int maxsize);

#endif /* __METADATA_REFLECTION_H__ */

