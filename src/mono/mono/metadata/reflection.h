#ifndef __METADATA_REFLECTION_H__
#define __METADATA_REFLECTION_H__

#include <mono/metadata/image.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/utils/mono-hash.h>

typedef struct {
	GHashTable *hash;
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

/*
 * The followinbg structure must match the C# implementation in our corlib.
 */

struct _MonoReflectionMethod {
	MonoObject object;
	MonoMethod *method;
	MonoString *name;
	MonoReflectionType *reftype;
};

struct _MonoDelegate {
	MonoObject object;
	MonoObject *target_type;
	MonoObject *target;
	MonoString *method_name;
	gpointer method_ptr;
	gpointer delegate_trampoline;
	MonoReflectionMethod *method_info;
};

typedef struct _MonoMulticastDelegate MonoMulticastDelegate;
struct _MonoMulticastDelegate {
	MonoDelegate delegate;
	MonoMulticastDelegate *prev;
};

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
	MonoClass *klass;
	MonoEvent *event;
} MonoReflectionEvent;

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
	MonoObject object;
	MonoAssembly *assembly;
} MonoReflectionAssembly;

typedef struct {
	MonoReflectionType *utype;
	MonoArray *values;
	MonoArray *names;
} MonoEnumInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoReflectionType *ret;
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
	MonoString *name;
	MonoReflectionMethod *add_method;
	MonoReflectionMethod *remove_method;
	MonoReflectionMethod *raise_method;
	guint32 attrs;
} MonoEventInfo;

typedef struct {
	MonoReflectionType *parent;
	MonoReflectionType *type;
	MonoString *name;
	guint32 attrs;
} MonoFieldInfo;

typedef struct {
	MonoString *name;
	MonoString *name_space;
	MonoReflectionType *etype;
	MonoReflectionType *nested_in;
	MonoReflectionAssembly *assembly;
	guint32 rank;
	MonoBoolean isprimitive;
} MonoTypeInfo;

typedef struct {
	MonoObject *member;
	gint32 code_pos;
} MonoReflectionILTokenInfo;

typedef struct {
	MonoObject object;
	MonoArray *code;
	MonoObject *mbuilder;
	gint32 code_len;
	gint32 max_stack;
	gint32 cur_stack;
	MonoArray *locals;
	MonoArray *ex_handlers;
	gint32 num_token_fixups;
	MonoArray *token_fixups;
} MonoReflectionILGen;

typedef struct {
	MonoArray *handlers;
	gint32 start;
	gint32 len;
	gint32 label;
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
	gint32 count;
	gint32 type;
	gint32 eltype;
} MonoReflectionMarshal;

typedef struct {
	MonoObject object;
	MonoObject* methodb;
	MonoString *name;
	MonoArray *cattrs;
	MonoReflectionMarshal *marshal_info;
	guint32 attrs;
	int position;
	guint32 table_idx;
} MonoReflectionParamBuilder;

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoReflectionILGen *ilgen;
	MonoArray *parameters;
	guint32 attrs;
	guint32 iattrs;
	guint32 table_idx;
	guint32 call_conv;
	MonoObject *type;
	MonoArray *pinfo;
	MonoArray *cattrs;
	MonoBoolean init_locals;
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
	MonoArray *cattrs;
	MonoReflectionMethod *override_method;
	MonoString *dll;
	MonoString *dllentry;
	guint32 charset;
	guint32 native_cc;
	guint32 call_conv;
	MonoBoolean init_locals;
} MonoReflectionMethodBuilder;

typedef struct {
	MonoObject object;
	MonoMethod *mhandle;
	MonoReflectionType *parent;
	MonoReflectionType *ret;
	MonoArray *parameters;
	MonoString *name;
	guint32 table_idx;
	guint32 call_conv;
} MonoReflectionArrayMethod;

enum {
	MONO_SECTION_TEXT,
	MONO_SECTION_RSRC,
	MONO_SECTION_RELOC,
	MONO_SECTION_MAX
};

typedef struct {
	MonoAssembly assembly;
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
	GHashTable *typeref;
	GHashTable *handleref;
	MonoGHashTable *tokens;
	GHashTable *blob_cache;
	GList *array_methods;
	MonoGHashTable *token_fixups;
	MonoGHashTable *method_to_table_idx;
	MonoGHashTable *field_to_table_idx;
	char *strong_name;
	guint32 strong_name_size;
	MonoDynamicStream pefile;
	MonoDynamicStream sheap;
	MonoDynamicStream code; /* used to store method headers and bytecode */
	MonoDynamicStream resources; /* managed embedded resources */
	MonoDynamicStream us;
	MonoDynamicStream blob;
	MonoDynamicStream tstream;
	MonoDynamicStream guid;
	MonoDynamicTable tables [64];
} MonoDynamicAssembly;

typedef struct {
	MonoArray *data;
	MonoString *name;
	MonoString *filename;
	guint32 attrs;
} MonoReflectionResource;

typedef struct {
	MonoReflectionAssembly assembly;
	MonoDynamicAssembly *dynamic_assembly;
	MonoReflectionMethodBuilder *entry_point;
	MonoArray *modules;
	MonoString *name;
	MonoString *dir;
	MonoArray *cattrs;
	MonoArray *resources;
	MonoString *keyfile;
	MonoString *version;
	MonoString *culture;
	guint32 algid;
	guint32 flags;
	guint32 pekind;
	MonoBoolean delay_sign;
} MonoReflectionAssemblyBuilder;

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoReflectionType *type;
	MonoString *name;
	MonoObject *def_value;
	gint32 offset;
	gint32 table_idx;
	MonoReflectionType *typeb;
	MonoArray *rva_data;
	MonoArray *cattrs;
	MonoReflectionMarshal *marshal_info;
	MonoClassField *handle;
} MonoReflectionFieldBuilder;

typedef struct {
	MonoObject object;
	guint32 attrs;
	MonoString *name;
	MonoReflectionType *type;
	MonoArray *parameters;
	MonoArray *cattrs;
	MonoObject *def_value;
	MonoReflectionMethodBuilder *set_method;
	MonoReflectionMethodBuilder *get_method;
	gint32 table_idx;
} MonoReflectionPropertyBuilder;

typedef struct {
	MonoObject	obj;
	MonoImage  *image;
	MonoReflectionAssembly *assembly;
	MonoString *fqname;
	MonoString *name;
	MonoString *scopename;
} MonoReflectionModule;

typedef struct {
	MonoReflectionModule module;
	MonoArray *types;
	MonoArray *cattrs;
	MonoArray *guid;
	guint32    table_idx;
	MonoReflectionAssemblyBuilder *assemblyb;
	MonoArray *global_methods;
	MonoArray *global_fields;
} MonoReflectionModuleBuilder;

typedef struct {
	MonoReflectionType type;
	MonoString *name;
	MonoString *nspace;
	MonoReflectionType *parent;
	MonoReflectionType *nesting_type;
	MonoArray *interfaces;
	MonoArray *methods;
	MonoArray *ctors;
	MonoArray *properties;
	MonoArray *fields;
	MonoArray *events;
	MonoArray *cattrs;
	MonoArray *subtypes;
	guint32 attrs;
	guint32 table_idx;
	MonoReflectionModuleBuilder *module;
	gint32 class_size;
	gint32 packing_size;
} MonoReflectionTypeBuilder;

typedef struct {
	MonoObject  obj;
	MonoString *name;
	MonoString *codebase;
	gint32 major, minor, build, revision;
	/* FIXME: add missing stuff */
/*	CultureInfo cultureinfo;
	AssemblyNameFlags flags;
	AssemblyHashAlgorithm hashalg;
	StrongNameKeyPair keypair;
	AssemblyVersionCompatibility versioncompat;*/
	MonoObject  *cultureInfo;
	MonoObject  *flags;
	MonoObject  *hashalg;
	MonoObject  *keypair;
	MonoArray   *publicKey;
	MonoArray   *keyToken;
	MonoObject  *versioncompat;
} MonoReflectionAssemblyName;

typedef struct {
	MonoObject  obj;
	MonoString *name;
	MonoReflectionType *type;
	MonoReflectionTypeBuilder *typeb;
	MonoArray *cattrs;
	MonoReflectionMethodBuilder *add_method;
	MonoReflectionMethodBuilder *remove_method;
	MonoReflectionMethodBuilder *raise_method;
	MonoArray *other_methods;
	guint32 attrs;
	guint32 table_idx;
} MonoReflectionEventBuilder;

typedef struct {
	MonoObject  obj;
	MonoReflectionMethod *ctor;
	MonoArray *data;
} MonoReflectionCustomAttr;

typedef struct MonoTypeNameParse MonoTypeNameParse;

struct MonoTypeNameParse {
	char *name_space;
	char *name;
	MonoAssemblyName assembly;
	GList *modifiers; /* 0 -> byref, -1 -> pointer, > 0 -> array rank */
	GList *nested;
};

typedef struct {
	MonoObject object;
	MonoReflectionModuleBuilder *module;
	MonoArray *arguments;
	guint32 type;
	MonoReflectionType *return_type;
	guint32 call_conv;
	guint32 unmanaged_call_conv;
} MonoReflectionSigHelper;



char*         mono_type_get_name         (MonoType *type);
int           mono_reflection_parse_type (char *name, MonoTypeNameParse *info);
MonoType*     mono_reflection_get_type   (MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase);
MonoType*     mono_reflection_type_from_name (char *name, MonoImage *image);

void          mono_image_create_pefile (MonoReflectionAssemblyBuilder *assembly);
void          mono_image_basic_init (MonoReflectionAssemblyBuilder *assembly);
guint32       mono_image_insert_string (MonoReflectionAssemblyBuilder *assembly, MonoString *str);
guint32       mono_image_create_token  (MonoDynamicAssembly *assembly, MonoObject *obj);
void          mono_image_module_basic_init (MonoReflectionModuleBuilder *module);

MonoReflectionAssembly* mono_assembly_get_object (MonoDomain *domain, MonoAssembly *assembly);
MonoReflectionModule*   mono_module_get_object   (MonoDomain *domain, MonoImage *image);
MonoReflectionType*     mono_type_get_object     (MonoDomain *domain, MonoType *type);
MonoReflectionMethod*   mono_method_get_object   (MonoDomain *domain, MonoMethod *method, MonoClass *refclass);
MonoReflectionField*    mono_field_get_object    (MonoDomain *domain, MonoClass *klass, MonoClassField *field);
MonoReflectionProperty* mono_property_get_object (MonoDomain *domain, MonoClass *klass, MonoProperty *property);
MonoReflectionEvent*    mono_event_get_object    (MonoDomain *domain, MonoClass *klass, MonoEvent *event);
/* note: this one is slightly different: we keep the whole array of params in the cache */
MonoReflectionParameter** mono_param_get_objects  (MonoDomain *domain, MonoMethod *method);

MonoArray*  mono_reflection_get_custom_attrs (MonoObject *obj);
MonoArray*  mono_reflection_get_custom_attrs_blob (MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *porpValues, MonoArray *fields, MonoArray* fieldValues);

void        mono_reflection_setup_internal_class  (MonoReflectionTypeBuilder *tb);

void        mono_reflection_create_internal_class (MonoReflectionTypeBuilder *tb);

MonoReflectionType* mono_reflection_create_runtime_class  (MonoReflectionTypeBuilder *tb);

MonoArray  *mono_reflection_sighelper_get_signature_local (MonoReflectionSigHelper *sig);

MonoArray  *mono_reflection_sighelper_get_signature_field (MonoReflectionSigHelper *sig);

gpointer
mono_reflection_lookup_dynamic_token (MonoImage *image, guint32 token);

void
mono_image_build_metadata (MonoReflectionAssemblyBuilder *assemblyb);

#endif /* __METADATA_REFLECTION_H__ */

