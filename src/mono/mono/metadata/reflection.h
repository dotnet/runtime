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
	MonoMethod *method;
} MonoReflectionMethod;

typedef struct {
	MonoObject object;
	MonoClass *klass;
	MonoClassField *field;
} MonoReflectionField;

typedef struct {
	MonoObject object;
	MonoClass *klass;
	MonoProperty *property;
} MonoReflectionProperty;

typedef struct {
	MonoObject object;
	MonoReflectionType *ClassImpl;
	MonoObject *DefaultValueImpl;
	MonoObject *MemberImpl;
	MonoString *NameImpl;
	gint32 PositionImpl;
	guint32 AttrsImpl;
} MonoReflectionParameter;

typedef struct {
	MonoReflectionType *utype;
	MonoArray *values;
	MonoArray *names;
} MonoEnumInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoReflectionType *ret;
	MonoString *name;
	guint32 attrs;
	guint32 implattrs;
} MonoMethodInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoString *name;
	MonoReflectionMethod *get;
	MonoReflectionMethod *set;
	guint32 attrs;
} MonoPropertyInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoReflectionType *type;
	MonoString *name;
	guint32 attrs;
} MonoFieldInfo;

typedef struct {
	MonoString *name;
	MonoString *name_space;
	MonoReflectionType *parent;
	MonoReflectionType *etype;
	MonoArray *interfaces;
	MonoAssembly *assembly;
	guint32 attrs;
	guint32 rank;
} MonoTypeInfo;

typedef struct {
	MonoObject object;
	MonoArray *code;
	MonoObject *mbuilder;
	gint32 code_len;
	gint32 max_stack;
	gint32 cur_stack;
	MonoArray *locals;
	MonoArray *ex_handlers;
} MonoReflectionILGen;

typedef struct {
	MonoArray *handlers;
	gint32 start;
	gint32 len;
} MonoILExceptionInfo;

typedef struct {
	MonoReflectionType *extype;
	gint32 type;
	gint32 start;
	gint32 len;
	gint32 filter_offset;
} MonoILExceptionBlock;

typedef struct {
	MonoObject object;
	MonoReflectionType *type;
	MonoString *name;
} MonoReflectionLocalBuilder;

typedef struct {
	MonoObject object;
	MonoObject* methodb;
	MonoString *name;
	guint32 attrs;
	int position;
	guint32 table_idx;
} MonoReflectionParamBuilder;

typedef struct {
	MonoObject object;
	MonoReflectionILGen *ilgen;
	MonoArray *parameters;
	guint32 attrs;
	guint32 iattrs;
	guint32 table_idx;
	guint32 call_conv;
	MonoObject *type;
	MonoArray *pinfo;
} MonoReflectionCtorBuilder;

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoReflectionType *rtype;
	MonoArray *parameters;
	guint32 attrs;
	guint32 iattrs;
	MonoString *name;
	guint32 table_idx;
	MonoArray *code;
	MonoReflectionILGen *ilgen;
	MonoObject *type;
	MonoArray *pinfo;
	MonoReflectionMethod *override_method;
	MonoString *dll;
	MonoString *dllentry;
	guint32 charset;
	guint32 native_cc;
	guint32 call_conv;
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

typedef struct {
	MonoReflectionType type;
	MonoString *name;
	MonoString *nspace;
	MonoReflectionType *parent;
	MonoArray *interfaces;
	MonoArray *methods;
	MonoArray *ctors;
	MonoArray *properties;
	MonoArray *fields;
	MonoArray *subtypes;
	guint32 attrs;
	guint32 table_idx;
	MonoReflectionModuleBuilder *module;
} MonoReflectionTypeBuilder;

typedef struct {
	MonoObject  obj;
	MonoString *name;
	MonoString *codebase;
	MonoObject *version;
} MonoReflectionAssemblyName;

typedef struct {
	char *nest_name_space;
	char *nest_name;
	char *name_space;
	char *name;
	char *assembly;
	int rank; /* we may need more info than this */
	int isbyref;
	int ispointer;
} MonoTypeNameParse;

int           mono_reflection_parse_type (char *name, MonoTypeNameParse *info);

int           mono_image_get_header (MonoReflectionAssemblyBuilder *assembly, char *buffer, int maxsize);
void          mono_image_basic_init (MonoReflectionAssemblyBuilder *assembly);
guint32       mono_image_insert_string (MonoReflectionAssemblyBuilder *assembly, MonoString *str);
guint32       mono_image_create_token  (MonoReflectionAssemblyBuilder *assembly, MonoObject *obj);

MonoReflectionAssembly* mono_assembly_get_object (MonoAssembly *assembly);
MonoReflectionType*     mono_type_get_object     (MonoType *type);
MonoReflectionMethod*   mono_method_get_object   (MonoMethod *method);
MonoReflectionField*    mono_field_get_object    (MonoClass *klass, MonoClassField *field);
MonoReflectionProperty* mono_property_get_object (MonoClass *klass, MonoProperty *property);
/* note: this one is slightly different: we keep the whole array of params in the cache */
MonoReflectionParameter** mono_param_get_objects  (MonoMethod *method);

#endif /* __METADATA_REFLECTION_H__ */

