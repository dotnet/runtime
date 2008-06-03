/*
 * metadata.c: Routines for accessing the metadata
 *
 * Authors:
 *   Miguel de Icaza (miguel@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001-2002 Ximian, Inc.
 */

#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <glib.h>
#include "metadata.h"
#include "tabledefs.h"
#include "mono-endian.h"
#include "cil-coff.h"
#include "tokentype.h"
#include "metadata-internals.h"
#include "class-internals.h"
#include "class.h"

static gboolean do_mono_metadata_parse_type (MonoType *type, MonoImage *m, MonoGenericContainer *container,
					 const char *ptr, const char **rptr);

static gboolean do_mono_metadata_type_equal (MonoType *t1, MonoType *t2, gboolean signature_only);
static gboolean mono_metadata_class_equal (MonoClass *c1, MonoClass *c2, gboolean signature_only);
static gboolean mono_metadata_fnptr_equal (MonoMethodSignature *s1, MonoMethodSignature *s2, gboolean signature_only);
static gboolean _mono_metadata_generic_class_equal (const MonoGenericClass *g1, const MonoGenericClass *g2,
						    gboolean signature_only);
static void free_generic_inst (MonoGenericInst *ginst);
static void free_generic_class (MonoGenericClass *ginst);
static void free_inflated_method (MonoMethodInflated *method);

/*
 * This enumeration is used to describe the data types in the metadata
 * tables
 */
enum {
	MONO_MT_END,

	/* Sized elements */
	MONO_MT_UINT32,
	MONO_MT_UINT16,
	MONO_MT_UINT8,

	/* Index into Blob heap */
	MONO_MT_BLOB_IDX,

	/* Index into String heap */
	MONO_MT_STRING_IDX,

	/* GUID index */
	MONO_MT_GUID_IDX,

	/* Pointer into a table */
	MONO_MT_TABLE_IDX,

	/* HasConstant:Parent pointer (Param, Field or Property) */
	MONO_MT_CONST_IDX,

	/* HasCustomAttribute index.  Indexes any table except CustomAttribute */
	MONO_MT_HASCAT_IDX,
	
	/* CustomAttributeType encoded index */
	MONO_MT_CAT_IDX,

	/* HasDeclSecurity index: TypeDef Method or Assembly */
	MONO_MT_HASDEC_IDX,

	/* Implementation coded index: File, Export AssemblyRef */
	MONO_MT_IMPL_IDX,

	/* HasFieldMarshal coded index: Field or Param table */
	MONO_MT_HFM_IDX,

	/* MemberForwardedIndex: Field or Method */
	MONO_MT_MF_IDX,

	/* TypeDefOrRef coded index: typedef, typeref, typespec */
	MONO_MT_TDOR_IDX,

	/* MemberRefParent coded index: typeref, moduleref, method, memberref, typesepc, typedef */
	MONO_MT_MRP_IDX,

	/* MethodDefOrRef coded index: Method or Member Ref table */
	MONO_MT_MDOR_IDX,

	/* HasSemantic coded index: Event or Property */
	MONO_MT_HS_IDX,

	/* ResolutionScope coded index: Module, ModuleRef, AssemblytRef, TypeRef */
	MONO_MT_RS_IDX
};

const static unsigned char TableSchemas [] = {
#define ASSEMBLY_SCHEMA_OFFSET 0
	MONO_MT_UINT32,     /* "HashId" }, */
	MONO_MT_UINT16,     /* "Major" },  */
	MONO_MT_UINT16,     /* "Minor" }, */
	MONO_MT_UINT16,     /* "BuildNumber" }, */
	MONO_MT_UINT16,     /* "RevisionNumber" }, */
	MONO_MT_UINT32,     /* "Flags" }, */
	MONO_MT_BLOB_IDX,   /* "PublicKey" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_STRING_IDX, /* "Culture" }, */
	MONO_MT_END,

#define ASSEMBLYOS_SCHEMA_OFFSET ASSEMBLY_SCHEMA_OFFSET + 10
	MONO_MT_UINT32,     /* "OSPlatformID" }, */
	MONO_MT_UINT32,     /* "OSMajor" }, */
	MONO_MT_UINT32,     /* "OSMinor" }, */
	MONO_MT_END,

#define ASSEMBLYPROC_SCHEMA_OFFSET ASSEMBLYOS_SCHEMA_OFFSET + 4
	MONO_MT_UINT32,     /* "Processor" }, */
	MONO_MT_END,

#define ASSEMBLYREF_SCHEMA_OFFSET ASSEMBLYPROC_SCHEMA_OFFSET + 2
	MONO_MT_UINT16,     /* "Major" }, */
	MONO_MT_UINT16,     /* "Minor" }, */
	MONO_MT_UINT16,     /* "Build" }, */
	MONO_MT_UINT16,     /* "Revision" }, */
	MONO_MT_UINT32,     /* "Flags" }, */
	MONO_MT_BLOB_IDX,   /* "PublicKeyOrToken" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_STRING_IDX, /* "Culture" }, */
	MONO_MT_BLOB_IDX,   /* "HashValue" }, */
	MONO_MT_END,

#define ASSEMBLYREFOS_SCHEMA_OFFSET ASSEMBLYREF_SCHEMA_OFFSET + 10
	MONO_MT_UINT32,     /* "OSPlatformID" }, */
	MONO_MT_UINT32,     /* "OSMajorVersion" }, */
	MONO_MT_UINT32,     /* "OSMinorVersion" }, */
	MONO_MT_TABLE_IDX,  /* "AssemblyRef:AssemblyRef" }, */
	MONO_MT_END,

#define ASSEMBLYREFPROC_SCHEMA_OFFSET ASSEMBLYREFOS_SCHEMA_OFFSET + 5
	MONO_MT_UINT32,     /* "Processor" }, */
	MONO_MT_TABLE_IDX,  /* "AssemblyRef:AssemblyRef" }, */
	MONO_MT_END,

#define CLASS_LAYOUT_SCHEMA_OFFSET ASSEMBLYREFPROC_SCHEMA_OFFSET + 3
	MONO_MT_UINT16,     /* "PackingSize" }, */
	MONO_MT_UINT32,     /* "ClassSize" }, */
	MONO_MT_TABLE_IDX,  /* "Parent:TypeDef" }, */
	MONO_MT_END,

#define CONSTANT_SCHEMA_OFFSET CLASS_LAYOUT_SCHEMA_OFFSET + 4
	MONO_MT_UINT8,      /* "Type" }, */
	MONO_MT_UINT8,      /* "PaddingZero" }, */
	MONO_MT_CONST_IDX,  /* "Parent" }, */
	MONO_MT_BLOB_IDX,   /* "Value" }, */
	MONO_MT_END,

#define CUSTOM_ATTR_SCHEMA_OFFSET CONSTANT_SCHEMA_OFFSET + 5
	MONO_MT_HASCAT_IDX, /* "Parent" }, */
	MONO_MT_CAT_IDX,    /* "Type" }, */
	MONO_MT_BLOB_IDX,   /* "Value" }, */
	MONO_MT_END,

#define DECL_SEC_SCHEMA_OFFSET CUSTOM_ATTR_SCHEMA_OFFSET + 4
	MONO_MT_UINT16,     /* "Action" }, */
	MONO_MT_HASDEC_IDX, /* "Parent" }, */
	MONO_MT_BLOB_IDX,   /* "PermissionSet" }, */
	MONO_MT_END,

#define EVENTMAP_SCHEMA_OFFSET DECL_SEC_SCHEMA_OFFSET + 4
	MONO_MT_TABLE_IDX,  /* "Parent:TypeDef" }, */
	MONO_MT_TABLE_IDX,  /* "EventList:Event" }, */
	MONO_MT_END,

#define EVENT_SCHEMA_OFFSET EVENTMAP_SCHEMA_OFFSET + 3
	MONO_MT_UINT16,     /* "EventFlags#EventAttribute" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_TABLE_IDX,  /* "EventType" }, TypeDef or TypeRef  */
	MONO_MT_END,

#define EVENT_POINTER_SCHEMA_OFFSET EVENT_SCHEMA_OFFSET + 4
	MONO_MT_TABLE_IDX,  /* "Event" }, */
	MONO_MT_END,

#define EXPORTED_TYPE_SCHEMA_OFFSET EVENT_POINTER_SCHEMA_OFFSET + 2
	MONO_MT_UINT32,     /* "Flags" }, */
	MONO_MT_TABLE_IDX,  /* "TypeDefId" }, */
	MONO_MT_STRING_IDX, /* "TypeName" }, */
	MONO_MT_STRING_IDX, /* "TypeNameSpace" }, */
	MONO_MT_IMPL_IDX,   /* "Implementation" }, */
	MONO_MT_END,

#define FIELD_SCHEMA_OFFSET EXPORTED_TYPE_SCHEMA_OFFSET + 6
	MONO_MT_UINT16,     /* "Flags" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_BLOB_IDX,   /* "Signature" }, */
	MONO_MT_END,

#define FIELD_LAYOUT_SCHEMA_OFFSET FIELD_SCHEMA_OFFSET + 4
	MONO_MT_UINT32,     /* "Offset" }, */
	MONO_MT_TABLE_IDX,  /* "Field:Field" }, */
	MONO_MT_END,

#define FIELD_MARSHAL_SCHEMA_OFFSET FIELD_LAYOUT_SCHEMA_OFFSET + 3
	MONO_MT_HFM_IDX,    /* "Parent" }, */
	MONO_MT_BLOB_IDX,   /* "NativeType" }, */
	MONO_MT_END,

#define FIELD_RVA_SCHEMA_OFFSET FIELD_MARSHAL_SCHEMA_OFFSET + 3
	MONO_MT_UINT32,     /* "RVA" }, */
	MONO_MT_TABLE_IDX,  /* "Field:Field" }, */
	MONO_MT_END,

#define FIELD_POINTER_SCHEMA_OFFSET FIELD_RVA_SCHEMA_OFFSET + 3
	MONO_MT_TABLE_IDX,  /* "Field" }, */
	MONO_MT_END,

#define FILE_SCHEMA_OFFSET FIELD_POINTER_SCHEMA_OFFSET + 2
	MONO_MT_UINT32,     /* "Flags" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_BLOB_IDX,   /* "Value" },  */
	MONO_MT_END,

#define IMPLMAP_SCHEMA_OFFSET FILE_SCHEMA_OFFSET + 4
	MONO_MT_UINT16,     /* "MappingFlag" }, */
	MONO_MT_MF_IDX,     /* "MemberForwarded" }, */
	MONO_MT_STRING_IDX, /* "ImportName" }, */
	MONO_MT_TABLE_IDX,  /* "ImportScope:ModuleRef" }, */
	MONO_MT_END,

#define IFACEMAP_SCHEMA_OFFSET IMPLMAP_SCHEMA_OFFSET + 5
	MONO_MT_TABLE_IDX,  /* "Class:TypeDef" },  */
	MONO_MT_TDOR_IDX,  /* "Interface=TypeDefOrRef" }, */
	MONO_MT_END,

#define MANIFEST_SCHEMA_OFFSET IFACEMAP_SCHEMA_OFFSET + 3
	MONO_MT_UINT32,     /* "Offset" }, */
	MONO_MT_UINT32,     /* "Flags" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_IMPL_IDX,   /* "Implementation" }, */
	MONO_MT_END,

#define MEMBERREF_SCHEMA_OFFSET MANIFEST_SCHEMA_OFFSET + 5
	MONO_MT_MRP_IDX,    /* "Class" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_BLOB_IDX,   /* "Signature" }, */
	MONO_MT_END,

#define METHOD_SCHEMA_OFFSET MEMBERREF_SCHEMA_OFFSET + 4
	MONO_MT_UINT32,     /* "RVA" }, */
	MONO_MT_UINT16,     /* "ImplFlags#MethodImplAttributes" }, */
	MONO_MT_UINT16,     /* "Flags#MethodAttribute" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_BLOB_IDX,   /* "Signature" }, */
	MONO_MT_TABLE_IDX,  /* "ParamList:Param" }, */
	MONO_MT_END,

#define METHOD_IMPL_SCHEMA_OFFSET METHOD_SCHEMA_OFFSET + 7
	MONO_MT_TABLE_IDX,  /* "Class:TypeDef" }, */
	MONO_MT_MDOR_IDX,   /* "MethodBody" }, */
	MONO_MT_MDOR_IDX,   /* "MethodDeclaration" }, */
	MONO_MT_END,

#define METHOD_SEMA_SCHEMA_OFFSET METHOD_IMPL_SCHEMA_OFFSET + 4
	MONO_MT_UINT16,     /* "MethodSemantic" }, */
	MONO_MT_TABLE_IDX,  /* "Method:Method" }, */
	MONO_MT_HS_IDX,     /* "Association" }, */
	MONO_MT_END,

#define METHOD_POINTER_SCHEMA_OFFSET METHOD_SEMA_SCHEMA_OFFSET + 4
	MONO_MT_TABLE_IDX,  /* "Method" }, */
	MONO_MT_END,

#define MODULE_SCHEMA_OFFSET METHOD_POINTER_SCHEMA_OFFSET + 2
	MONO_MT_UINT16,     /* "Generation" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_GUID_IDX,   /* "MVID" }, */
	MONO_MT_GUID_IDX,   /* "EncID" }, */
	MONO_MT_GUID_IDX,   /* "EncBaseID" }, */
	MONO_MT_END,

#define MODULEREF_SCHEMA_OFFSET MODULE_SCHEMA_OFFSET + 6
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_END,

#define NESTED_CLASS_SCHEMA_OFFSET MODULEREF_SCHEMA_OFFSET + 2
	MONO_MT_TABLE_IDX,  /* "NestedClass:TypeDef" }, */
	MONO_MT_TABLE_IDX,  /* "EnclosingClass:TypeDef" }, */
	MONO_MT_END,

#define PARAM_SCHEMA_OFFSET NESTED_CLASS_SCHEMA_OFFSET + 3
	MONO_MT_UINT16,     /* "Flags" }, */
	MONO_MT_UINT16,     /* "Sequence" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_END,

#define PARAM_POINTER_SCHEMA_OFFSET PARAM_SCHEMA_OFFSET + 4
	MONO_MT_TABLE_IDX,  /* "Param" }, */
	MONO_MT_END,

#define PROPERTY_SCHEMA_OFFSET PARAM_POINTER_SCHEMA_OFFSET + 2
	MONO_MT_UINT16,     /* "Flags" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_BLOB_IDX,   /* "Type" }, */
	MONO_MT_END,

#define PROPERTY_POINTER_SCHEMA_OFFSET PROPERTY_SCHEMA_OFFSET + 4
	MONO_MT_TABLE_IDX, /* "Property" }, */
	MONO_MT_END,

#define PROPERTY_MAP_SCHEMA_OFFSET PROPERTY_POINTER_SCHEMA_OFFSET + 2
	MONO_MT_TABLE_IDX,  /* "Parent:TypeDef" }, */
	MONO_MT_TABLE_IDX,  /* "PropertyList:Property" }, */
	MONO_MT_END,

#define STDALON_SIG_SCHEMA_OFFSET PROPERTY_MAP_SCHEMA_OFFSET + 3
	MONO_MT_BLOB_IDX,   /* "Signature" }, */
	MONO_MT_END,

#define TYPEDEF_SCHEMA_OFFSET STDALON_SIG_SCHEMA_OFFSET + 2
	MONO_MT_UINT32,     /* "Flags" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_STRING_IDX, /* "Namespace" }, */
	MONO_MT_TDOR_IDX,   /* "Extends" }, */
	MONO_MT_TABLE_IDX,  /* "FieldList:Field" }, */
	MONO_MT_TABLE_IDX,  /* "MethodList:Method" }, */
	MONO_MT_END,

#define TYPEREF_SCHEMA_OFFSET TYPEDEF_SCHEMA_OFFSET + 7
	MONO_MT_RS_IDX,     /* "ResolutionScope=ResolutionScope" }, */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_STRING_IDX, /* "Namespace" }, */
	MONO_MT_END,

#define TYPESPEC_SCHEMA_OFFSET TYPEREF_SCHEMA_OFFSET + 4
	MONO_MT_BLOB_IDX,   /* "Signature" }, */
	MONO_MT_END,

#define GENPARAM_SCHEMA_OFFSET TYPESPEC_SCHEMA_OFFSET + 2
	MONO_MT_UINT16,     /* "Number" }, */
	MONO_MT_UINT16,     /* "Flags" }, */
	MONO_MT_TABLE_IDX,  /* "Owner" },  TypeDef or MethodDef */
	MONO_MT_STRING_IDX, /* "Name" }, */
	MONO_MT_END,

#define METHOD_SPEC_SCHEMA_OFFSET GENPARAM_SCHEMA_OFFSET + 5
	MONO_MT_MDOR_IDX,   /* "Method" }, */
	MONO_MT_BLOB_IDX,   /* "Signature" }, */
	MONO_MT_END,

#define GEN_CONSTRAINT_SCHEMA_OFFSET METHOD_SPEC_SCHEMA_OFFSET + 3
	MONO_MT_TABLE_IDX,  /* "GenericParam" }, */
	MONO_MT_TDOR_IDX,   /* "Constraint" }, */
	MONO_MT_END,

#define NULL_SCHEMA_OFFSET GEN_CONSTRAINT_SCHEMA_OFFSET + 3
	MONO_MT_END
};

/* Must be the same order as MONO_TABLE_* */
const static unsigned char
table_description [] = {
	MODULE_SCHEMA_OFFSET,
	TYPEREF_SCHEMA_OFFSET,
	TYPEDEF_SCHEMA_OFFSET,
	FIELD_POINTER_SCHEMA_OFFSET,
	FIELD_SCHEMA_OFFSET,
	METHOD_POINTER_SCHEMA_OFFSET,
	METHOD_SCHEMA_OFFSET,
	PARAM_POINTER_SCHEMA_OFFSET,
	PARAM_SCHEMA_OFFSET,
	IFACEMAP_SCHEMA_OFFSET,
	MEMBERREF_SCHEMA_OFFSET, /* 0xa */
	CONSTANT_SCHEMA_OFFSET,
	CUSTOM_ATTR_SCHEMA_OFFSET,
	FIELD_MARSHAL_SCHEMA_OFFSET,
	DECL_SEC_SCHEMA_OFFSET,
	CLASS_LAYOUT_SCHEMA_OFFSET,
	FIELD_LAYOUT_SCHEMA_OFFSET, /* 0x10 */
	STDALON_SIG_SCHEMA_OFFSET,
	EVENTMAP_SCHEMA_OFFSET,
	EVENT_POINTER_SCHEMA_OFFSET,
	EVENT_SCHEMA_OFFSET,
	PROPERTY_MAP_SCHEMA_OFFSET,
	PROPERTY_POINTER_SCHEMA_OFFSET,
	PROPERTY_SCHEMA_OFFSET,
	METHOD_SEMA_SCHEMA_OFFSET,
	METHOD_IMPL_SCHEMA_OFFSET,
	MODULEREF_SCHEMA_OFFSET, /* 0x1a */
	TYPESPEC_SCHEMA_OFFSET,
	IMPLMAP_SCHEMA_OFFSET,
	FIELD_RVA_SCHEMA_OFFSET,
	NULL_SCHEMA_OFFSET,
	NULL_SCHEMA_OFFSET,
	ASSEMBLY_SCHEMA_OFFSET, /* 0x20 */
	ASSEMBLYPROC_SCHEMA_OFFSET,
	ASSEMBLYOS_SCHEMA_OFFSET,
	ASSEMBLYREF_SCHEMA_OFFSET,
	ASSEMBLYREFPROC_SCHEMA_OFFSET,
	ASSEMBLYREFOS_SCHEMA_OFFSET,
	FILE_SCHEMA_OFFSET,
	EXPORTED_TYPE_SCHEMA_OFFSET,
	MANIFEST_SCHEMA_OFFSET,
	NESTED_CLASS_SCHEMA_OFFSET,
	GENPARAM_SCHEMA_OFFSET, /* 0x2a */
	METHOD_SPEC_SCHEMA_OFFSET,
	GEN_CONSTRAINT_SCHEMA_OFFSET
};

#ifdef HAVE_ARRAY_ELEM_INIT
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define TABLEDEF(a,b) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#include "mono/cil/tables.def"
#undef TABLEDEF
} tablestr = {
#define TABLEDEF(a,b) b,
#include "mono/cil/tables.def"
#undef TABLEDEF
};
static const gint16 tableidx [] = {
#define TABLEDEF(a,b) [a] = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "mono/cil/tables.def"
#undef TABLEDEF
};

#else
#define TABLEDEF(a,b) b,
static const char* const
mono_tables_names [] = {
#include "mono/cil/tables.def"
	NULL
};

#endif

/* Auxiliary structure used for caching inflated signatures */
typedef struct {
	MonoMethodSignature *sig;
	MonoGenericContext context;
} MonoInflatedMethodSignature;

/**
 * mono_meta_table_name:
 * @table: table index
 *
 * Returns: the name for the @table index
 */
const char *
mono_meta_table_name (int table)
{
	if ((table < 0) || (table > MONO_TABLE_LAST))
		return "";

#ifdef HAVE_ARRAY_ELEM_INIT
	return (const char*)&tablestr + tableidx [table];
#else
	return mono_tables_names [table];
#endif
}

/* The guy who wrote the spec for this should not be allowed near a
 * computer again.
 
If  e is a coded token(see clause 23.1.7) that points into table ti out of n possible tables t0, .. tn-1, 
then it is stored as e << (log n) & tag{ t0, .. tn-1}[ ti] using 2 bytes if the maximum number of 
rows of tables t0, ..tn-1, is less than 2^16 - (log n), and using 4 bytes otherwise. The family of 
finite maps tag{ t0, ..tn-1} is defined below. Note that to decode a physical row, you need the 
inverse of this mapping.

 */
#define rtsize(s,b) (((s) < (1 << (b)) ? 2 : 4))
#define idx_size(tableidx) (meta->tables [(tableidx)].rows < 65536 ? 2 : 4)

/* Reference: Partition II - 23.2.6 */
/*
 * mono_metadata_compute_size:
 * @meta: metadata context
 * @tableindex: metadata table number
 * @result_bitfield: pointer to guint32 where to store additional info
 * 
 * mono_metadata_compute_size() computes the lenght in bytes of a single
 * row in a metadata table. The size of each column is encoded in the
 * @result_bitfield return value along with the number of columns in the table.
 * the resulting bitfield should be handed to the mono_metadata_table_size()
 * and mono_metadata_table_count() macros.
 * This is a Mono runtime internal only function.
 */
int
mono_metadata_compute_size (MonoImage *meta, int tableindex, guint32 *result_bitfield)
{
	guint32 bitfield = 0;
	int size = 0, field_size = 0;
	int i, n, code;
	int shift = 0;
	const unsigned char *description = TableSchemas + table_description [tableindex];

	for (i = 0; (code = description [i]) != MONO_MT_END; i++){
		switch (code){
		case MONO_MT_UINT32:
			field_size = 4; break;
			
		case MONO_MT_UINT16:
			field_size = 2; break;
			
		case MONO_MT_UINT8:
			field_size = 1; break;
			
		case MONO_MT_BLOB_IDX:
			field_size = meta->idx_blob_wide ? 4 : 2; break;
			
		case MONO_MT_STRING_IDX:
			field_size = meta->idx_string_wide ? 4 : 2; break;
			
		case MONO_MT_GUID_IDX:
			field_size = meta->idx_guid_wide ? 4 : 2; break;

		case MONO_MT_TABLE_IDX:
			/* Uhm, a table index can point to other tables besides the current one
			 * so, it's not correct to use the rowcount of the current table to
			 * get the size for this column - lupus 
			 */
			switch (tableindex) {
			case MONO_TABLE_ASSEMBLYREFOS:
				g_assert (i == 3);
				field_size = idx_size (MONO_TABLE_ASSEMBLYREF); break;
			case MONO_TABLE_ASSEMBLYPROCESSOR:
				g_assert (i == 1);
				field_size = idx_size (MONO_TABLE_ASSEMBLYREF); break;
			case MONO_TABLE_CLASSLAYOUT:
				g_assert (i == 2);
				field_size = idx_size (MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_EVENTMAP:
				g_assert (i == 0 || i == 1);
				field_size = i ? idx_size (MONO_TABLE_EVENT):
					idx_size(MONO_TABLE_TYPEDEF); 
				break;
			case MONO_TABLE_EVENT:
				g_assert (i == 2);
				n = MAX (meta->tables [MONO_TABLE_TYPEDEF].rows, meta->tables [MONO_TABLE_TYPEREF].rows);
				n = MAX (n, meta->tables [MONO_TABLE_TYPESPEC].rows);
				/*This is a coded token for 3 tables, so takes 2 bits */
				field_size = rtsize (n, 16 - MONO_TYPEDEFORREF_BITS);
				break;
			case MONO_TABLE_EVENT_POINTER:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_EVENT); break;
			case MONO_TABLE_EXPORTEDTYPE:
				g_assert (i == 1);
				/* the index is in another metadata file, so it must be 4 */
				field_size = 4; break;
			case MONO_TABLE_FIELDLAYOUT:
				g_assert (i == 1);
				field_size = idx_size (MONO_TABLE_FIELD); break;
			case MONO_TABLE_FIELDRVA:
				g_assert (i == 1);
				field_size = idx_size (MONO_TABLE_FIELD); break;
			case MONO_TABLE_FIELD_POINTER:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_FIELD); break;
			case MONO_TABLE_IMPLMAP:
				g_assert (i == 3);
				field_size = idx_size (MONO_TABLE_MODULEREF); break;
			case MONO_TABLE_INTERFACEIMPL:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_METHOD:
				g_assert (i == 5);
				field_size = idx_size (MONO_TABLE_PARAM); break;
			case MONO_TABLE_METHODIMPL:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_METHODSEMANTICS:
				g_assert (i == 1);
				field_size = idx_size (MONO_TABLE_METHOD); break;
			case MONO_TABLE_METHOD_POINTER:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_METHOD); break;
			case MONO_TABLE_NESTEDCLASS:
				g_assert (i == 0 || i == 1);
				field_size = idx_size (MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_PARAM_POINTER:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_PARAM); break;
			case MONO_TABLE_PROPERTYMAP:
				g_assert (i == 0 || i == 1);
				field_size = i ? idx_size (MONO_TABLE_PROPERTY):
					idx_size(MONO_TABLE_TYPEDEF); 
				break;
			case MONO_TABLE_PROPERTY_POINTER:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_PROPERTY); break;
			case MONO_TABLE_TYPEDEF:
				g_assert (i == 4 || i == 5);
				field_size = i == 4 ? idx_size (MONO_TABLE_FIELD):
					idx_size(MONO_TABLE_METHOD);
				break;
			case MONO_TABLE_GENERICPARAM:
				g_assert (i == 2 || i == 4 || i == 5);
				if (i == 2) {
					n = MAX (meta->tables [MONO_TABLE_METHOD].rows, meta->tables [MONO_TABLE_TYPEDEF].rows);
					/*This is a coded token for 2 tables, so takes 1 bit */
					field_size = rtsize (n, 16 - MONO_TYPEORMETHOD_BITS);
				} else if (i == 4)
					field_size = idx_size (MONO_TABLE_TYPEDEF);
				else if (i == 5)
					field_size = idx_size (MONO_TABLE_TYPEDEF);
				break;

			case MONO_TABLE_GENERICPARAMCONSTRAINT:
				g_assert (i == 0);
				field_size = idx_size (MONO_TABLE_GENERICPARAM);
				break;
				
			default:
				g_assert_not_reached ();
			}
			break;

			/*
			 * HasConstant: ParamDef, FieldDef, Property
			 */
		case MONO_MT_CONST_IDX:
			n = MAX (meta->tables [MONO_TABLE_PARAM].rows,
				 meta->tables [MONO_TABLE_FIELD].rows);
			n = MAX (n, meta->tables [MONO_TABLE_PROPERTY].rows);

			/* 2 bits to encode tag */
			field_size = rtsize (n, 16-2);
			break;

			/*
			 * HasCustomAttribute: points to any table but
			 * itself.
			 */
		case MONO_MT_HASCAT_IDX:
			/*
			 * We believe that since the signature and
			 * permission are indexing the Blob heap,
			 * we should consider the blob size first
			 */
			/* I'm not a believer - lupus
			if (meta->idx_blob_wide){
				field_size = 4;
				break;
			}*/
			
			n = MAX (meta->tables [MONO_TABLE_METHOD].rows,
				 meta->tables [MONO_TABLE_FIELD].rows);
			n = MAX (n, meta->tables [MONO_TABLE_TYPEREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_TYPEDEF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_PARAM].rows);
			n = MAX (n, meta->tables [MONO_TABLE_INTERFACEIMPL].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MEMBERREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MODULE].rows);
			n = MAX (n, meta->tables [MONO_TABLE_DECLSECURITY].rows);
			n = MAX (n, meta->tables [MONO_TABLE_PROPERTY].rows);
			n = MAX (n, meta->tables [MONO_TABLE_EVENT].rows);
			n = MAX (n, meta->tables [MONO_TABLE_STANDALONESIG].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MODULEREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_TYPESPEC].rows);
			n = MAX (n, meta->tables [MONO_TABLE_ASSEMBLY].rows);
			n = MAX (n, meta->tables [MONO_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_FILE].rows);
			n = MAX (n, meta->tables [MONO_TABLE_EXPORTEDTYPE].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MANIFESTRESOURCE].rows);

			/* 5 bits to encode */
			field_size = rtsize (n, 16-5);
			break;

			/*
			 * CustomAttributeType: TypeDef, TypeRef, MethodDef, 
			 * MemberRef and String.  
			 */
		case MONO_MT_CAT_IDX:
			/* String is a heap, if it is wide, we know the size */
			/* See above, nope. 
			if (meta->idx_string_wide){
				field_size = 4;
				break;
			}*/
			
			n = MAX (meta->tables [MONO_TABLE_TYPEREF].rows,
				 meta->tables [MONO_TABLE_TYPEDEF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_METHOD].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MEMBERREF].rows);

			/* 3 bits to encode */
			field_size = rtsize (n, 16-3);
			break;

			/*
			 * HasDeclSecurity: Typedef, MethodDef, Assembly
			 */
		case MONO_MT_HASDEC_IDX:
			n = MAX (meta->tables [MONO_TABLE_TYPEDEF].rows,
				 meta->tables [MONO_TABLE_METHOD].rows);
			n = MAX (n, meta->tables [MONO_TABLE_ASSEMBLY].rows);

			/* 2 bits to encode */
			field_size = rtsize (n, 16-2);
			break;

			/*
			 * Implementation: File, AssemblyRef, ExportedType
			 */
		case MONO_MT_IMPL_IDX:
			n = MAX (meta->tables [MONO_TABLE_FILE].rows,
				 meta->tables [MONO_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_EXPORTEDTYPE].rows);

			/* 2 bits to encode tag */
			field_size = rtsize (n, 16-2);
			break;

			/*
			 * HasFieldMarshall: FieldDef, ParamDef
			 */
		case MONO_MT_HFM_IDX:
			n = MAX (meta->tables [MONO_TABLE_FIELD].rows,
				 meta->tables [MONO_TABLE_PARAM].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (n, 16-1);
			break;

			/*
			 * MemberForwarded: FieldDef, MethodDef
			 */
		case MONO_MT_MF_IDX:
			n = MAX (meta->tables [MONO_TABLE_FIELD].rows,
				 meta->tables [MONO_TABLE_METHOD].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (n, 16-1);
			break;

			/*
			 * TypeDefOrRef: TypeDef, ParamDef, TypeSpec
			 * LAMESPEC
			 * It is TypeDef, _TypeRef_, TypeSpec, instead.
			 */
		case MONO_MT_TDOR_IDX:
			n = MAX (meta->tables [MONO_TABLE_TYPEDEF].rows,
				 meta->tables [MONO_TABLE_TYPEREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_TYPESPEC].rows);

			/* 2 bits to encode */
			field_size = rtsize (n, 16-2);
			break;

			/*
			 * MemberRefParent: TypeDef, TypeRef, MethodDef, ModuleRef, TypeSpec, MemberRef
			 */
		case MONO_MT_MRP_IDX:
			n = MAX (meta->tables [MONO_TABLE_TYPEDEF].rows,
				 meta->tables [MONO_TABLE_TYPEREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_METHOD].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MODULEREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_TYPESPEC].rows);
			n = MAX (n, meta->tables [MONO_TABLE_MEMBERREF].rows);

			/* 3 bits to encode */
			field_size = rtsize (n, 16 - 3);
			break;
			
			/*
			 * MethodDefOrRef: MethodDef, MemberRef
			 */
		case MONO_MT_MDOR_IDX:
			n = MAX (meta->tables [MONO_TABLE_METHOD].rows,
				 meta->tables [MONO_TABLE_MEMBERREF].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (n, 16-1);
			break;
			
			/*
			 * HasSemantics: Property, Event
			 */
		case MONO_MT_HS_IDX:
			n = MAX (meta->tables [MONO_TABLE_PROPERTY].rows,
				 meta->tables [MONO_TABLE_EVENT].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (n, 16-1);
			break;

			/*
			 * ResolutionScope: Module, ModuleRef, AssemblyRef, TypeRef
			 */
		case MONO_MT_RS_IDX:
			n = MAX (meta->tables [MONO_TABLE_MODULE].rows,
				 meta->tables [MONO_TABLE_MODULEREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_TYPEREF].rows);

			/* 2 bits used to encode tag (ECMA spec claims 3) */
			field_size = rtsize (n, 16 - 2);
			break;
		}

		/*
		 * encode field size as follows (we just need to
		 * distinguish them).
		 *
		 * 4 -> 3
		 * 2 -> 1
		 * 1 -> 0
		 */
		bitfield |= (field_size-1) << shift;
		shift += 2;
		size += field_size;
		/*g_print ("table %02x field %d size %d\n", tableindex, i, field_size);*/
	}

	*result_bitfield = (i << 24) | bitfield;
	return size;
}

/**
 * mono_metadata_compute_table_bases:
 * @meta: metadata context to compute table values
 *
 * Computes the table bases for the metadata structure.
 * This is an internal function used by the image loader code.
 */
void
mono_metadata_compute_table_bases (MonoImage *meta)
{
	int i;
	const char *base = meta->tables_base;
	
	for (i = 0; i < MONO_TABLE_NUM; i++) {
		MonoTableInfo *table = &meta->tables [i];
		if (table->rows == 0)
			continue;

		table->row_size = mono_metadata_compute_size (meta, i, &table->size_bitfield);
		table->base = base;
		base += table->rows * table->row_size;
	}
}

/**
 * mono_metadata_locate:
 * @meta: metadata context
 * @table: table code.
 * @idx: index of element to retrieve from @table.
 *
 * Returns: a pointer to the @idx element in the metadata table
 * whose code is @table.
 */
const char *
mono_metadata_locate (MonoImage *meta, int table, int idx)
{
	/* idx == 0 refers always to NULL */
	g_return_val_if_fail (idx > 0 && idx <= meta->tables [table].rows, "");
	   
	return meta->tables [table].base + (meta->tables [table].row_size * (idx - 1));
}

/**
 * mono_metadata_locate_token:
 * @meta: metadata context
 * @token: metadata token
 *
 * Returns: a pointer to the data in the metadata represented by the
 * token #token.
 */
const char *
mono_metadata_locate_token (MonoImage *meta, guint32 token)
{
	return mono_metadata_locate (meta, token >> 24, token & 0xffffff);
}

/**
 * mono_metadata_string_heap:
 * @meta: metadata context
 * @index: index into the string heap.
 *
 * Returns: an in-memory pointer to the @index in the string heap.
 */
const char *
mono_metadata_string_heap (MonoImage *meta, guint32 index)
{
	g_return_val_if_fail (index < meta->heap_strings.size, "");
	return meta->heap_strings.data + index;
}

/**
 * mono_metadata_user_string:
 * @meta: metadata context
 * @index: index into the user string heap.
 *
 * Returns: an in-memory pointer to the @index in the user string heap ("#US").
 */
const char *
mono_metadata_user_string (MonoImage *meta, guint32 index)
{
	g_return_val_if_fail (index < meta->heap_us.size, "");
	return meta->heap_us.data + index;
}

/**
 * mono_metadata_blob_heap:
 * @meta: metadata context
 * @index: index into the blob.
 *
 * Returns: an in-memory pointer to the @index in the Blob heap.
 */
const char *
mono_metadata_blob_heap (MonoImage *meta, guint32 index)
{
	g_return_val_if_fail (index < meta->heap_blob.size, "");
	return meta->heap_blob.data + index;
}

/**
 * mono_metadata_guid_heap:
 * @meta: metadata context
 * @index: index into the guid heap.
 *
 * Returns: an in-memory pointer to the @index in the guid heap.
 */
const char *
mono_metadata_guid_heap (MonoImage *meta, guint32 index)
{
	--index;
	index *= 16; /* adjust for guid size and 1-based index */
	g_return_val_if_fail (index < meta->heap_guid.size, "");
	return meta->heap_guid.data + index;
}

static const unsigned char *
dword_align (const unsigned char *ptr)
{
#if SIZEOF_VOID_P == 8
	return (const unsigned char *) (((guint64) (ptr + 3)) & ~3);
#else
	return (const unsigned char *) (((guint32) (ptr + 3)) & ~3);
#endif
}

/**
 * mono_metadata_decode_row:
 * @t: table to extract information from.
 * @idx: index in table.
 * @res: array of @res_size cols to store the results in
 *
 * This decompresses the metadata element @idx in table @t
 * into the guint32 @res array that has res_size elements
 */
void
mono_metadata_decode_row (const MonoTableInfo *t, int idx, guint32 *res, int res_size)
{
	guint32 bitfield = t->size_bitfield;
	int i, count = mono_metadata_table_count (bitfield);
	const char *data;

	g_assert (idx < t->rows);
	data = t->base + idx * t->row_size;
	
	g_assert (res_size == count);
	
	for (i = 0; i < count; i++) {
		int n = mono_metadata_table_size (bitfield, i);

		switch (n){
		case 1:
			res [i] = *data; break;
		case 2:
			res [i] = read16 (data); break;
		case 4:
			res [i] = read32 (data); break;
		default:
			g_assert_not_reached ();
		}
		data += n;
	}
}

/**
 * mono_metadata_decode_row_col:
 * @t: table to extract information from.
 * @idx: index for row in table.
 * @col: column in the row.
 *
 * This function returns the value of column @col from the @idx
 * row in the table @t.
 */
guint32
mono_metadata_decode_row_col (const MonoTableInfo *t, int idx, guint col)
{
	guint32 bitfield = t->size_bitfield;
	int i;
	register const char *data; 
	register int n;
	
	g_assert (idx < t->rows);
	g_assert (col < mono_metadata_table_count (bitfield));
	data = t->base + idx * t->row_size;

	n = mono_metadata_table_size (bitfield, 0);
	for (i = 0; i < col; ++i) {
		data += n;
		n = mono_metadata_table_size (bitfield, i + 1);
	}
	switch (n) {
	case 1:
		return *data;
	case 2:
		return read16 (data);
	case 4:
		return read32 (data);
	default:
		g_assert_not_reached ();
	}
	return 0;
}

/**
 * mono_metadata_decode_blob_size:
 * @ptr: pointer to a blob object
 * @rptr: the new position of the pointer
 *
 * This decodes a compressed size as described by 23.1.4 (a blob or user string object)
 *
 * Returns: the size of the blob object
 */
guint32
mono_metadata_decode_blob_size (const char *xptr, const char **rptr)
{
	const unsigned char *ptr = (const unsigned char *)xptr;
	guint32 size;
	
	if ((*ptr & 0x80) == 0){
		size = ptr [0] & 0x7f;
		ptr++;
	} else if ((*ptr & 0x40) == 0){
		size = ((ptr [0] & 0x3f) << 8) + ptr [1];
		ptr += 2;
	} else {
		size = ((ptr [0] & 0x1f) << 24) +
			(ptr [1] << 16) +
			(ptr [2] << 8) +
			ptr [3];
		ptr += 4;
	}
	if (rptr)
		*rptr = (char*)ptr;
	return size;
}

/**
 * mono_metadata_decode_value:
 * @ptr: pointer to decode from
 * @rptr: the new position of the pointer
 *
 * This routine decompresses 32-bit values as specified in the "Blob and
 * Signature" section (22.2)
 *
 * Returns: the decoded value
 */
guint32
mono_metadata_decode_value (const char *_ptr, const char **rptr)
{
	const unsigned char *ptr = (const unsigned char *) _ptr;
	unsigned char b = *ptr;
	guint32 len;
	
	if ((b & 0x80) == 0){
		len = b;
		++ptr;
	} else if ((b & 0x40) == 0){
		len = ((b & 0x3f) << 8 | ptr [1]);
		ptr += 2;
	} else {
		len = ((b & 0x1f) << 24) |
			(ptr [1] << 16) |
			(ptr [2] << 8) |
			ptr [3];
		ptr += 4;
	}
	if (rptr)
		*rptr = (char*)ptr;
	
	return len;
}

/**
 * mono_metadata_decode_signed_value:
 * @ptr: pointer to decode from
 * @rptr: the new position of the pointer
 *
 * This routine decompresses 32-bit signed values
 * (not specified in the spec)
 *
 * Returns: the decoded value
 */
gint32
mono_metadata_decode_signed_value (const char *ptr, const char **rptr)
{
	guint32 uval = mono_metadata_decode_value (ptr, rptr);
	gint32 ival = uval >> 1;
	if (!(uval & 1))
		return ival;
	/* ival is a truncated 2's complement negative number.  */
	if (ival < 0x40)
		/* 6 bits = 7 bits for compressed representation (top bit is '0') - 1 sign bit */
		return ival - 0x40;
	if (ival < 0x2000)
		/* 13 bits = 14 bits for compressed representation (top bits are '10') - 1 sign bit */
		return ival - 0x2000;
	if (ival < 0x10000000)
		/* 28 bits = 29 bits for compressed representation (top bits are '110') - 1 sign bit */
		return ival - 0x10000000;
	g_assert (ival < 0x20000000);
	g_warning ("compressed signed value appears to use 29 bits for compressed representation: %x (raw: %8x)", ival, uval);
	return ival - 0x20000000;
}

/* 
 * Translates the given 1-based index into the Method, Field, Event, or Param tables
 * using the *Ptr tables in uncompressed metadata, if they are available.
 *
 * FIXME: The caller is not forced to call this function, which is error-prone, since 
 * forgetting to call it would only show up as a bug on uncompressed metadata.
 */
guint32
mono_metadata_translate_token_index (MonoImage *image, int table, guint32 idx)
{
	if (!image->uncompressed_metadata)
		return idx;

	switch (table) {
	case MONO_TABLE_METHOD:
		if (image->tables [MONO_TABLE_METHOD_POINTER].rows)
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_METHOD_POINTER], idx - 1, MONO_METHOD_POINTER_METHOD);
		else
			return idx;
	case MONO_TABLE_FIELD:
		if (image->tables [MONO_TABLE_FIELD_POINTER].rows)
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_FIELD_POINTER], idx - 1, MONO_FIELD_POINTER_FIELD);
		else
			return idx;
	case MONO_TABLE_EVENT:
		if (image->tables [MONO_TABLE_EVENT_POINTER].rows)
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_EVENT_POINTER], idx - 1, MONO_EVENT_POINTER_EVENT);
		else
			return idx;
	case MONO_TABLE_PROPERTY:
		if (image->tables [MONO_TABLE_PROPERTY_POINTER].rows)
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_PROPERTY_POINTER], idx - 1, MONO_PROPERTY_POINTER_PROPERTY);
		else
			return idx;
	case MONO_TABLE_PARAM:
		if (image->tables [MONO_TABLE_PARAM_POINTER].rows)
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_PARAM_POINTER], idx - 1, MONO_PARAM_POINTER_PARAM);
		else
			return idx;
	default:
		return idx;
	}
}

/**
 * mono_metadata_decode_table_row:
 *
 *   Same as mono_metadata_decode_row, but takes an IMAGE+TABLE ID pair, and takes
 * uncompressed metadata into account, so it should be used to access the
 * Method, Field, Param and Event tables when the access is made from metadata, i.e.
 * IDX is retrieved from a metadata table, like MONO_TYPEDEF_FIELD_LIST.
 */
void
mono_metadata_decode_table_row (MonoImage *image, int table, int idx, guint32 *res, int res_size)
{
	if (image->uncompressed_metadata)
		idx = mono_metadata_translate_token_index (image, table, idx + 1) - 1;

	mono_metadata_decode_row (&image->tables [table], idx, res, res_size);
}

/**
 * mono_metadata_decode_table_row_col:
 *
 *   Same as mono_metadata_decode_row_col, but takes an IMAGE+TABLE ID pair, and takes
 * uncompressed metadata into account, so it should be used to access the
 * Method, Field, Param and Event tables.
 */
guint32 mono_metadata_decode_table_row_col (MonoImage *image, int table, int idx, guint col)
{
	if (image->uncompressed_metadata)
		idx = mono_metadata_translate_token_index (image, table, idx + 1) - 1;

	return mono_metadata_decode_row_col (&image->tables [table], idx, col);
}

/*
 * mono_metadata_parse_typedef_or_ref:
 * @m: a metadata context.
 * @ptr: a pointer to an encoded TypedefOrRef in @m
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Returns: a token valid in the @m metadata decoded from
 * the compressed representation.
 */
guint32
mono_metadata_parse_typedef_or_ref (MonoImage *m, const char *ptr, const char **rptr)
{
	guint32 token;
	token = mono_metadata_decode_value (ptr, &ptr);
	if (rptr)
		*rptr = ptr;
	return mono_metadata_token_from_dor (token);
}

/*
 * mono_metadata_parse_custom_mod:
 * @m: a metadata context.
 * @dest: storage where the info about the custom modifier is stored (may be NULL)
 * @ptr: a pointer to (possibly) the start of a custom modifier list
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Checks if @ptr points to a type custom modifier compressed representation.
 *
 * Returns: #TRUE if a custom modifier was found, #FALSE if not.
 */
int
mono_metadata_parse_custom_mod (MonoImage *m, MonoCustomMod *dest, const char *ptr, const char **rptr)
{
	MonoCustomMod local;
	if ((*ptr == MONO_TYPE_CMOD_OPT) || (*ptr == MONO_TYPE_CMOD_REQD)) {
		if (!dest)
			dest = &local;
		dest->required = *ptr == MONO_TYPE_CMOD_REQD ? 1 : 0;
		dest->token = mono_metadata_parse_typedef_or_ref (m, ptr + 1, rptr);
		return TRUE;
	}
	return FALSE;
}

/*
 * mono_metadata_parse_array_full:
 * @m: a metadata context.
 * @ptr: a pointer to an encoded array description.
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Decodes the compressed array description found in the metadata @m at @ptr.
 *
 * Returns: a #MonoArrayType structure describing the array type
 * and dimensions. Memory is allocated from the image mempool.
 *
 * LOCKING: Assumes the loader lock is held.
 */
MonoArrayType *
mono_metadata_parse_array_full (MonoImage *m, MonoGenericContainer *container,
				const char *ptr, const char **rptr)
{
	int i;
	MonoArrayType *array = mono_mempool_alloc0 (m->mempool, sizeof (MonoArrayType));
	MonoType *etype;
	
	etype = mono_metadata_parse_type_full (m, container, MONO_PARSE_TYPE, 0, ptr, &ptr);
	if (!etype)
		return NULL;
	array->eklass = mono_class_from_mono_type (etype);
	array->rank = mono_metadata_decode_value (ptr, &ptr);

	array->numsizes = mono_metadata_decode_value (ptr, &ptr);
	if (array->numsizes)
		array->sizes = g_new0 (int, array->numsizes);
	for (i = 0; i < array->numsizes; ++i)
		array->sizes [i] = mono_metadata_decode_value (ptr, &ptr);

	array->numlobounds = mono_metadata_decode_value (ptr, &ptr);
	if (array->numlobounds)
		array->lobounds = g_new0 (int, array->numlobounds);
	for (i = 0; i < array->numlobounds; ++i)
		array->lobounds [i] = mono_metadata_decode_signed_value (ptr, &ptr);

	if (rptr)
		*rptr = ptr;
	return array;
}

MonoArrayType *
mono_metadata_parse_array (MonoImage *m, const char *ptr, const char **rptr)
{
	return mono_metadata_parse_array_full (m, NULL, ptr, rptr);
}

/*
 * mono_metadata_free_array:
 * @array: array description
 *
 * Frees the array description returned from mono_metadata_parse_array().
 */
void
mono_metadata_free_array (MonoArrayType *array)
{
	g_free (array->sizes);
	g_free (array->lobounds);
	g_free (array);
}

/*
 * need to add common field and param attributes combinations:
 * [out] param
 * public static
 * public static literal
 * private
 * private static
 * private static literal
 */
static const MonoType
builtin_types[] = {
	/* data, attrs, type,              nmods, byref, pinned */
	{{NULL}, 0,     MONO_TYPE_VOID,    0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_BOOLEAN, 0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_BOOLEAN, 0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_CHAR,    0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_CHAR,    0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_I1,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I1,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_U1,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_U1,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_I2,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I2,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_U2,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_U2,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_I4,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I4,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_U4,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_U4,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_I8,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I8,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_U8,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_U8,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_R4,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_R4,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_R8,      0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_R8,      0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_STRING,  0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_STRING,  0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_OBJECT,  0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_OBJECT,  0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_TYPEDBYREF,  0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I,       0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I,       0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_U,       0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_U,       0,     1,     0},
};

#define NBUILTIN_TYPES() (sizeof (builtin_types) / sizeof (builtin_types [0]))

static GHashTable *type_cache = NULL;
static GHashTable *generic_inst_cache = NULL;
static GHashTable *generic_class_cache = NULL;
static int next_generic_inst_id = 0;

/*
 * Protected by the loader lock.
 * It has a MonoMethodInflated* as key and value.
 * The key lookup will just access the declaring and context fields
 */
static GHashTable *generic_method_cache = NULL;

/*
 * Protected by the loader lock.
 * It has a MonoInflatedMethodSignature* as key and value.
 */
static GHashTable *generic_signature_cache = NULL;

static guint mono_generic_class_hash (gconstpointer data);

/*
 * MonoTypes with modifies are never cached, so we never check or use that field.
 */
static guint
mono_type_hash (gconstpointer data)
{
	const MonoType *type = (const MonoType *) data;
	if (type->type == MONO_TYPE_GENERICINST)
		return mono_generic_class_hash (type->data.generic_class);
	else
		return type->type | (type->byref << 8) | (type->attrs << 9);
}

static gint
mono_type_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoType *a = (const MonoType *) ka;
	const MonoType *b = (const MonoType *) kb;
	
	if (a->type != b->type || a->byref != b->byref || a->attrs != b->attrs || a->pinned != b->pinned)
		return 0;
	/* need other checks */
	return 1;
}

static guint
mono_generic_inst_hash (gconstpointer data)
{
	const MonoGenericInst *ginst = (const MonoGenericInst *) data;
	guint hash = 0;
	int i;
	
	for (i = 0; i < ginst->type_argc; ++i) {
		hash *= 13;
		hash += mono_metadata_type_hash (ginst->type_argv [i]);
	}

	return hash ^ (ginst->is_open << 8);
}

static gboolean
mono_generic_inst_equal_full (const MonoGenericInst *a, const MonoGenericInst *b, gboolean signature_only)
{
	int i;

	if (a->id && b->id) {
		if (a->id == b->id)
			return TRUE;
		if (!signature_only)
			return FALSE;
	}

	if (a->is_open != b->is_open || a->type_argc != b->type_argc)
		return FALSE;
	for (i = 0; i < a->type_argc; ++i) {
		if (!do_mono_metadata_type_equal (a->type_argv [i], b->type_argv [i], signature_only))
			return FALSE;
	}
	return TRUE;
}

static gboolean
mono_generic_inst_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoGenericInst *a = (const MonoGenericInst *) ka;
	const MonoGenericInst *b = (const MonoGenericInst *) kb;

	return mono_generic_inst_equal_full (a, b, FALSE);
}

static guint
mono_generic_class_hash (gconstpointer data)
{
	const MonoGenericClass *gclass = (const MonoGenericClass *) data;
	guint hash = mono_metadata_type_hash (&gclass->container_class->byval_arg);

	hash *= 13;
	hash += gclass->is_tb_open;
	hash += mono_metadata_generic_context_hash (&gclass->context);

	return hash;
}

static gboolean
mono_generic_class_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoGenericClass *a = (const MonoGenericClass *) ka;
	const MonoGenericClass *b = (const MonoGenericClass *) kb;

	return _mono_metadata_generic_class_equal (a, b, FALSE);
}

/**
 * mono_metadata_init:
 *
 * Initialize the global variables of this module.
 * This is a Mono runtime internal function.
 */
void
mono_metadata_init (void)
{
	int i;

	type_cache = g_hash_table_new (mono_type_hash, mono_type_equal);
	generic_inst_cache = g_hash_table_new_full (mono_generic_inst_hash, mono_generic_inst_equal, NULL, (GDestroyNotify)free_generic_inst);
	generic_class_cache = g_hash_table_new_full (mono_generic_class_hash, mono_generic_class_equal, NULL, (GDestroyNotify)free_generic_class);

	for (i = 0; i < NBUILTIN_TYPES (); ++i)
		g_hash_table_insert (type_cache, (gpointer) &builtin_types [i], (gpointer) &builtin_types [i]);
}

/**
 * mono_metadata_cleanup:
 *
 * Free all resources used by this module.
 * This is a Mono runtime internal function.
 */
void
mono_metadata_cleanup (void)
{
	g_hash_table_destroy (type_cache);
	g_hash_table_destroy (generic_inst_cache);
	g_hash_table_destroy (generic_class_cache);
	if (generic_method_cache)
		g_hash_table_destroy (generic_method_cache);
	if (generic_signature_cache)
		g_hash_table_destroy (generic_signature_cache);
	type_cache = NULL;
	generic_inst_cache = NULL;
	generic_class_cache = NULL;
	generic_method_cache = NULL;
	generic_signature_cache = NULL;
}

/**
 * mono_metadata_parse_type:
 * @m: metadata context
 * @mode: king of type that may be found at @ptr
 * @opt_attrs: optional attributes to store in the returned type
 * @ptr: pointer to the type representation
 * @rptr: pointer updated to match the end of the decoded stream
 * 
 * Decode a compressed type description found at @ptr in @m.
 * @mode can be one of MONO_PARSE_MOD_TYPE, MONO_PARSE_PARAM, MONO_PARSE_RET,
 * MONO_PARSE_FIELD, MONO_PARSE_LOCAL, MONO_PARSE_TYPE.
 * This function can be used to decode type descriptions in method signatures,
 * field signatures, locals signatures etc.
 *
 * To parse a generic type, `generic_container' points to the current class'es
 * (the `generic_container' field in the MonoClass) or the current generic method's
 * (the `generic_container' field in the MonoMethodNormal) generic container.
 * When we encounter any MONO_TYPE_VAR or MONO_TYPE_MVAR's, they're looked up in
 * this MonoGenericContainer.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Acquires the loader lock.
 *
 * Returns: a #MonoType structure representing the decoded type.
 */
MonoType*
mono_metadata_parse_type_full (MonoImage *m, MonoGenericContainer *container, MonoParseTypeMode mode,
			       short opt_attrs, const char *ptr, const char **rptr)
{
	MonoType *type, *cached;
	MonoType stype;
	gboolean byref = FALSE;
	gboolean pinned = FALSE;
	const char *tmp_ptr;
	int count = 0;
	gboolean found;

	mono_loader_lock ();
	/*
	 * According to the spec, custom modifiers should come before the byref
	 * flag, but the IL produced by ilasm from the following signature:
	 *   object modopt(...) &
	 * starts with a byref flag, followed by the modifiers. (bug #49802)
	 * Also, this type seems to be different from 'object & modopt(...)'. Maybe
	 * it would be better to treat byref as real type constructor instead of
	 * a modifier...
	 * Also, pinned should come before anything else, but some MSV++ produced
	 * assemblies violate this (#bug 61990).
	 */

	/* Count the modifiers first */
	tmp_ptr = ptr;
	found = TRUE;
	while (found) {
		switch (*tmp_ptr) {
		case MONO_TYPE_PINNED:
		case MONO_TYPE_BYREF:
			++tmp_ptr;
			break;
		case MONO_TYPE_CMOD_REQD:
		case MONO_TYPE_CMOD_OPT:
			count ++;
			mono_metadata_parse_custom_mod (m, NULL, tmp_ptr, &tmp_ptr);
			break;
		default:
			found = FALSE;
		}
	}

	if (count) {
		type = mono_mempool_alloc0 (m->mempool, sizeof (MonoType) + ((gint32)count - MONO_ZERO_LEN_ARRAY) * sizeof (MonoCustomMod));
		type->num_mods = count;
		if (count > 64)
			g_warning ("got more than 64 modifiers in type");
	} else {
		type = &stype;
		memset (type, 0, sizeof (MonoType));
	}

	/* Parse pinned, byref and custom modifiers */
	found = TRUE;
	count = 0;
	while (found) {
		switch (*ptr) {
		case MONO_TYPE_PINNED:
			pinned = TRUE;
			++ptr;
			break;
		case MONO_TYPE_BYREF:
			byref = TRUE;
			++ptr;
			break;
		case MONO_TYPE_CMOD_REQD:
		case MONO_TYPE_CMOD_OPT:
			mono_metadata_parse_custom_mod (m, &(type->modifiers [count]), ptr, &ptr);
			count ++;
			break;
		default:
			found = FALSE;
		}
	}
	
	type->attrs = opt_attrs;
	type->byref = byref;
	type->pinned = pinned ? 1 : 0;

	if (!do_mono_metadata_parse_type (type, m, container, ptr, &ptr)) {
		mono_loader_unlock ();
		return NULL;
	}

	if (rptr)
		*rptr = ptr;

	if (!type->num_mods) {
		/* no need to free type here, because it is on the stack */
		if ((type->type == MONO_TYPE_CLASS || type->type == MONO_TYPE_VALUETYPE) && !type->pinned && !type->attrs) {
			MonoType *ret = type->byref ? &type->data.klass->this_arg : &type->data.klass->byval_arg;

			/* Consider the case:

			     class Foo<T> { class Bar {} }
			     class Test : Foo<Test>.Bar {}

			   When Foo<Test> is being expanded, 'Test' isn't yet initialized.  It's actually in
			   a really pristine state: it doesn't even know whether 'Test' is a reference or a value type.

			   We ensure that the MonoClass is in a state that we can canonicalize to:

			     klass->byval_arg.data.klass == klass
			     klass->this_arg.data.klass == klass

			   If we can't canonicalize 'type', it doesn't matter, since later users of 'type' will do it.

			   LOCKING: even though we don't explicitly hold a lock, in the problematic case 'ret' is a field
			            of a MonoClass which currently holds the loader lock.  'type' is local.
			*/
			if (ret->data.klass == type->data.klass) {
				mono_loader_unlock ();
				return ret;
			}
		}
		/* No need to use locking since nobody is modifying the hash table */
		if ((cached = g_hash_table_lookup (type_cache, type))) {
			mono_loader_unlock ();
			return cached;
		}
	}
	
	/* printf ("%x %x %c %s\n", type->attrs, type->num_mods, type->pinned ? 'p' : ' ', mono_type_full_name (type)); */
	
	if (type == &stype) {
		type = mono_mempool_alloc (m->mempool, sizeof (MonoType));
		memcpy (type, &stype, sizeof (MonoType));
	}
	mono_loader_unlock ();
	return type;
}

/*
 * LOCKING: Assumes the loader lock is held.
 */
MonoType*
mono_metadata_parse_type (MonoImage *m, MonoParseTypeMode mode, short opt_attrs,
			  const char *ptr, const char **rptr)
{
	return mono_metadata_parse_type_full (m, NULL, mode, opt_attrs, ptr, rptr);
}

/*
 * mono_metadata_get_param_attrs:
 *
 *   Return the parameter attributes for the method whose MethodDef index is DEF. The 
 * returned memory needs to be freed by the caller. If all the param attributes are
 * 0, then NULL is returned.
 */
int*
mono_metadata_get_param_attrs (MonoImage *m, int def)
{
	MonoTableInfo *paramt = &m->tables [MONO_TABLE_PARAM];
	MonoTableInfo *methodt = &m->tables [MONO_TABLE_METHOD];
	guint32 cols [MONO_PARAM_SIZE];
	guint lastp, i, param_index = mono_metadata_decode_row_col (&m->tables [MONO_TABLE_METHOD], def - 1, MONO_METHOD_PARAMLIST);
	int *pattrs = NULL;

	if (def < methodt->rows)
		lastp = mono_metadata_decode_row_col (&m->tables [MONO_TABLE_METHOD], def, MONO_METHOD_PARAMLIST);
	else
		lastp = paramt->rows + 1;

	for (i = param_index; i < lastp; ++i) {
		mono_metadata_decode_row (&m->tables [MONO_TABLE_PARAM], i - 1, cols, MONO_PARAM_SIZE);
		if (cols [MONO_PARAM_FLAGS]) {
			if (!pattrs)
				pattrs = g_new0 (int, 1 + (lastp - param_index));
			pattrs [cols [MONO_PARAM_SEQUENCE]] = cols [MONO_PARAM_FLAGS];
		}
	}

	return pattrs;
}

/*
 * mono_metadata_parse_signature_full:
 * @image: metadata context
 * @generic_container: generic container
 * @toke: metadata token
 *
 * Decode a method signature stored in the STANDALONESIG table
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * Returns: a MonoMethodSignature describing the signature.
 */
MonoMethodSignature*
mono_metadata_parse_signature_full (MonoImage *image, MonoGenericContainer *generic_container, guint32 token)
{
	MonoTableInfo *tables = image->tables;
	guint32 idx = mono_metadata_token_index (token);
	guint32 sig;
	const char *ptr;

	if (image->dynamic)
		return mono_lookup_dynamic_token (image, token, NULL);

	g_assert (mono_metadata_token_table(token) == MONO_TABLE_STANDALONESIG);
		
	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	mono_metadata_decode_blob_size (ptr, &ptr);

	return mono_metadata_parse_method_signature_full (image, generic_container, 0, ptr, NULL); 
}

/*
 * mono_metadata_parse_signature:
 * @image: metadata context
 * @toke: metadata token
 *
 * Decode a method signature stored in the STANDALONESIG table
 *
 * Returns: a MonoMethodSignature describing the signature.
 */
MonoMethodSignature*
mono_metadata_parse_signature (MonoImage *image, guint32 token)
{
	return mono_metadata_parse_signature_full (image, NULL, token);
}

/*
 * mono_metadata_signature_alloc:
 * @image: metadata context
 * @nparmas: number of parameters in the signature
 *
 * Allocate a MonoMethodSignature structure with the specified number of params.
 * The return type and the params types need to be filled later.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * Returns: the new MonoMethodSignature structure.
 */
MonoMethodSignature*
mono_metadata_signature_alloc (MonoImage *m, guint32 nparams)
{
	MonoMethodSignature *sig;

	mono_loader_lock ();
	sig = mono_mempool_alloc0 (m->mempool, sizeof (MonoMethodSignature) + ((gint32)nparams - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType*));
	sig->param_count = nparams;
	sig->sentinelpos = -1;
	mono_loader_unlock ();

	return sig;
}

MonoMethodSignature*
mono_metadata_signature_dup_full (MonoMemPool *mp, MonoMethodSignature *sig)
{
	int sigsize;

	sigsize = sizeof (MonoMethodSignature) + (sig->param_count - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType *);

	if (mp) {
		MonoMethodSignature *ret;
		mono_loader_lock ();
		ret = mono_mempool_alloc (mp, sigsize);
		mono_loader_unlock ();

		memcpy (ret, sig, sigsize);
		return ret;
	} else {
		return g_memdup (sig, sigsize);
	}
}

/*
 * mono_metadata_signature_dup:
 * @sig: method signature
 *
 * Duplicate an existing MonoMethodSignature so it can be modified.
 * This is a Mono runtime internal function.
 *
 * Returns: the new MonoMethodSignature structure.
 */
MonoMethodSignature*
mono_metadata_signature_dup (MonoMethodSignature *sig)
{
	return mono_metadata_signature_dup_full (NULL, sig);
}

/*
 * mono_metadata_parse_method_signature:
 * @m: metadata context
 * @generic_container: generics container
 * @def: the MethodDef index or 0 for Ref signatures.
 * @ptr: pointer to the signature metadata representation
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Decode a method signature stored at @ptr.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * Returns: a MonoMethodSignature describing the signature.
 */
MonoMethodSignature *
mono_metadata_parse_method_signature_full (MonoImage *m, MonoGenericContainer *container,
					   int def, const char *ptr, const char **rptr)
{
	MonoMethodSignature *method;
	int i, *pattrs = NULL;
	guint32 hasthis = 0, explicit_this = 0, call_convention, param_count;
	guint32 gen_param_count = 0;
	gboolean is_open = FALSE;

	if (*ptr & 0x10)
		gen_param_count = 1;
	if (*ptr & 0x20)
		hasthis = 1;
	if (*ptr & 0x40)
		explicit_this = 1;
	call_convention = *ptr & 0x0F;
	ptr++;
	if (gen_param_count)
		gen_param_count = mono_metadata_decode_value (ptr, &ptr);
	param_count = mono_metadata_decode_value (ptr, &ptr);

	if (def)
		pattrs = mono_metadata_get_param_attrs (m, def);
	method = mono_metadata_signature_alloc (m, param_count);
	method->hasthis = hasthis;
	method->explicit_this = explicit_this;
	method->call_convention = call_convention;
	method->generic_param_count = gen_param_count;

	if (call_convention != 0xa) {
		method->ret = mono_metadata_parse_type_full (m, container, MONO_PARSE_RET, pattrs ? pattrs [0] : 0, ptr, &ptr);
		if (!method->ret) {
			mono_metadata_free_method_signature (method);
			g_free (pattrs);
			return NULL;
		}
		is_open = mono_class_is_open_constructed_type (method->ret);
	}

	for (i = 0; i < method->param_count; ++i) {
		if (*ptr == MONO_TYPE_SENTINEL) {
			if (method->call_convention != MONO_CALL_VARARG || def)
				g_error ("found sentinel for methoddef or no vararg method");
			if (method->sentinelpos >= 0)
				g_error ("found sentinel twice in the same signature");
			method->sentinelpos = i;
			ptr++;
		}
		method->params [i] = mono_metadata_parse_type_full (m, container, MONO_PARSE_PARAM, pattrs ? pattrs [i+1] : 0, ptr, &ptr);
		if (!method->params [i]) {
			mono_metadata_free_method_signature (method);
			g_free (pattrs);
			return NULL;
		}
		if (!is_open)
			is_open = mono_class_is_open_constructed_type (method->params [i]);
	}

	/* The sentinel could be missing if the caller does not pass any additional arguments */
	if (!def && method->call_convention == MONO_CALL_VARARG && method->sentinelpos < 0)
		method->sentinelpos = method->param_count;

	method->has_type_parameters = is_open;

	if (def && (method->call_convention == MONO_CALL_VARARG))
		method->sentinelpos = method->param_count;

	g_free (pattrs);

	if (rptr)
		*rptr = ptr;
	/*
	 * Add signature to a cache and increase ref count...
	 */

	return method;
}

/*
 * mono_metadata_parse_method_signature:
 * @m: metadata context
 * @def: the MethodDef index or 0 for Ref signatures.
 * @ptr: pointer to the signature metadata representation
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Decode a method signature stored at @ptr.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * Returns: a MonoMethodSignature describing the signature.
 */
MonoMethodSignature *
mono_metadata_parse_method_signature (MonoImage *m, int def, const char *ptr, const char **rptr)
{
	return mono_metadata_parse_method_signature_full (m, NULL, def, ptr, rptr);
}

/*
 * mono_metadata_free_method_signature:
 * @sig: signature to destroy
 *
 * Free the memory allocated in the signature @sig.
 * This method needs to be robust and work also on partially-built
 * signatures, so it does extra checks.
 */
void
mono_metadata_free_method_signature (MonoMethodSignature *sig)
{
	/* Everything is allocated from mempools */
	/*
	int i;
	if (sig->ret)
		mono_metadata_free_type (sig->ret);
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i])
			mono_metadata_free_type (sig->params [i]);
	}
	*/
}

void
mono_metadata_free_inflated_signature (MonoMethodSignature *sig)
{
	int i;

	/* Allocated in inflate_generic_signature () */
	if (sig->ret)
		mono_metadata_free_type (sig->ret);
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i])
			mono_metadata_free_type (sig->params [i]);
	}
	g_free (sig);
}

static gboolean
inflated_method_equal (gconstpointer a, gconstpointer b)
{
	const MonoMethodInflated *ma = a;
	const MonoMethodInflated *mb = b;
	if (ma->declaring != mb->declaring)
		return FALSE;
	if (ma->is_mb_open != mb->is_mb_open)
		return FALSE;
	return mono_metadata_generic_context_equal (&ma->context, &mb->context);
}

static guint
inflated_method_hash (gconstpointer a)
{
	const MonoMethodInflated *ma = a;
	return (mono_metadata_generic_context_hash (&ma->context) ^ mono_aligned_addr_hash (ma->declaring)) + ma->is_mb_open;
}

static gboolean
inflated_signature_equal (gconstpointer a, gconstpointer b)
{
	const MonoInflatedMethodSignature *sig1 = a;
	const MonoInflatedMethodSignature *sig2 = b;

	/* sig->sig is assumed to be canonized */
	if (sig1->sig != sig2->sig)
		return FALSE;
	/* The generic instances are canonized */
	return mono_metadata_generic_context_equal (&sig1->context, &sig2->context);
}

static guint
inflated_signature_hash (gconstpointer a)
{
	const MonoInflatedMethodSignature *sig = a;

	/* sig->sig is assumed to be canonized */
	return mono_metadata_generic_context_hash (&sig->context) ^ mono_aligned_addr_hash (sig->sig);
}

/*static void
dump_ginst (MonoGenericInst *ginst)
{
	int i;
	char *name;

	g_print ("Ginst: <");
	for (i = 0; i < ginst->type_argc; ++i) {
		if (i != 0)
			g_print (", ");
		name = mono_type_get_name (ginst->type_argv [i]);
		g_print ("%s", name);
		g_free (name);
	}
	g_print (">");
}*/

static gboolean type_in_image (MonoType *type, MonoImage *image);

static gboolean
signature_in_image (MonoMethodSignature *sig, MonoImage *image)
{
	gpointer iter = NULL;
	MonoType *p;

	while ((p = mono_signature_get_params (sig, &iter)) != NULL)
		if (type_in_image (p, image))
			return TRUE;

	return type_in_image (mono_signature_get_return_type (sig), image);
}

static gboolean
ginst_in_image (MonoGenericInst *ginst, MonoImage *image)
{
	int i;

	for (i = 0; i < ginst->type_argc; ++i) {
		if (type_in_image (ginst->type_argv [i], image))
			return TRUE;
	}

	return FALSE;
}

static gboolean
gclass_in_image (MonoGenericClass *gclass, MonoImage *image)
{
	return gclass->container_class->image == image ||
		ginst_in_image (gclass->context.class_inst, image);
}

static gboolean
type_in_image (MonoType *type, MonoImage *image)
{
retry:
	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		return gclass_in_image (type->data.generic_class, image);
	case MONO_TYPE_PTR:
		type = type->data.type;
		goto retry;
	case MONO_TYPE_SZARRAY:
		type = &type->data.klass->byval_arg;
		goto retry;
	case MONO_TYPE_ARRAY:
		type = &type->data.array->eklass->byval_arg;
		goto retry;
	case MONO_TYPE_FNPTR:
		return signature_in_image (type->data.method, image);
	case MONO_TYPE_VAR:
		if (type->data.generic_param->owner) {
			g_assert (!type->data.generic_param->owner->is_method);
			return type->data.generic_param->owner->owner.klass->image == image;
		} else {
			return type->data.generic_param->image == image;
		}
	case MONO_TYPE_MVAR:
		if (type->data.generic_param->owner) {
			g_assert (type->data.generic_param->owner->is_method);
			if (!type->data.generic_param->owner->owner.method)
				/* RefEmit created generic param whose method is not finished */
				return FALSE;
			return type->data.generic_param->owner->owner.method->klass->image == image;
		} else {
			return type->data.generic_param->image == image;
		}
	default:
		/* At this point, we should've avoided all potential allocations in mono_class_from_mono_type () */
		return image == mono_class_from_mono_type (type)->image;
	}
}

typedef struct {
	MonoImage *image;
	GSList *list;
} CleanForImageUserData;

static gboolean
steal_gclass_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoGenericClass *gclass = key;
	CleanForImageUserData *user_data = data;

	if (!gclass_in_image (gclass, user_data->image))
		return FALSE;

	user_data->list = g_slist_prepend (user_data->list, gclass);
	return TRUE;
}

static gboolean
steal_ginst_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoGenericInst *ginst = key;
	CleanForImageUserData *user_data = data;

	if (!ginst_in_image (ginst, user_data->image))
		return FALSE;

	user_data->list = g_slist_prepend (user_data->list, ginst);
	return TRUE;
}

static gboolean
inflated_method_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = data;
	MonoMethodInflated *method = key;

	return method->declaring->klass->image == image ||
		(method->context.class_inst && ginst_in_image (method->context.class_inst, image)) ||
		(method->context.method_inst && ginst_in_image (method->context.method_inst, image));
}

static gboolean
inflated_signature_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = data;
	MonoInflatedMethodSignature *sig = key;

	return /* signature_in_image (sig->sig, image) || */
		(sig->context.class_inst && ginst_in_image (sig->context.class_inst, image)) ||
		(sig->context.method_inst && ginst_in_image (sig->context.method_inst, image));
}	

void
mono_metadata_clean_for_image (MonoImage *image)
{
	CleanForImageUserData ginst_data, gclass_data;
	GSList *l;

	/* The data structures could reference each other so we delete them in two phases */
	ginst_data.image = gclass_data.image = image;
	ginst_data.list = gclass_data.list = NULL;

	mono_loader_lock ();	
	/* Collect the items to delete and remove them from the hash table */
	g_hash_table_foreach_steal (generic_inst_cache, steal_ginst_in_image, &ginst_data);
	g_hash_table_foreach_steal (generic_class_cache, steal_gclass_in_image, &gclass_data);
	if (generic_method_cache)
		g_hash_table_foreach_remove (generic_method_cache, inflated_method_in_image, image);
	if (generic_signature_cache)
		g_hash_table_foreach_remove (generic_signature_cache, inflated_signature_in_image, image);
	/* Delete the removed items */
	for (l = ginst_data.list; l; l = l->next)
		free_generic_inst (l->data);
	for (l = gclass_data.list; l; l = l->next)
		free_generic_class (l->data);
	g_slist_free (ginst_data.list);
	g_slist_free (gclass_data.list);
	mono_class_unregister_image_generic_subclasses (image);
	mono_loader_unlock ();
}

static void
free_inflated_method (MonoMethodInflated *imethod)
{
	int i;
	MonoMethod *method = (MonoMethod*)imethod;

	if (method->signature)
		mono_metadata_free_inflated_signature (method->signature);

	if (!((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))) {
		MonoMethodNormal* mn = (MonoMethodNormal*) method;
		MonoMethodHeader *header = mn->header;

		if (header) {
			/* Allocated in inflate_generic_header () */
			for (i = 0; i < header->num_locals; ++i)
				mono_metadata_free_type (header->locals [i]);
			g_free (header->clauses);
			g_free (header);
		}
	}

	g_free (method);
}

static void
free_generic_inst (MonoGenericInst *ginst)
{
	int i;

	for (i = 0; i < ginst->type_argc; ++i)
		mono_metadata_free_type (ginst->type_argv [i]);
	g_free (ginst->type_argv);
	g_free (ginst);
}


static void
free_generic_class (MonoGenericClass *gclass)
{
	int i;

	/* FIXME: The dynamic case */
	if (gclass->cached_class && !gclass->cached_class->image->dynamic && !mono_generic_class_is_generic_type_definition (gclass)) {
		MonoClass *class = gclass->cached_class;

		/* Allocated in mono_class_init () */
		g_free (class->methods);
		g_free (class->properties);
		/* Allocated in mono_class_setup_fields () */
		if (class->fields) {
			for (i = 0; i < class->field.count; ++i) {
				g_free (class->fields [i].generic_info);
				mono_metadata_free_type (class->fields [i].type);
			}
		}
		/* Allocated in mono_generic_class_get_class () */
		g_free (class->interfaces);
		g_free (class);
	} else if (gclass->is_dynamic) {
		MonoDynamicGenericClass *dgclass = (MonoDynamicGenericClass *)gclass;

		for (i = 0; i < dgclass->count_fields; ++i) {
			MonoClassField *field = dgclass->fields + i;
			mono_metadata_free_type (field->type);
			if (field->generic_info) {
				mono_metadata_free_type (field->generic_info->generic_type);
				g_free (field->generic_info);
			}
			g_free ((char*)field->name);
		}
		for (i = 0; i < dgclass->count_properties; ++i) {
			MonoProperty *property = dgclass->properties + i;
			g_free ((char*)property->name);
		}
		for (i = 0; i < dgclass->count_events; ++i) {
			MonoEvent *event = dgclass->events + i;
			g_free ((char*)event->name);
		}
		
		g_free (dgclass->methods);
		g_free (dgclass->ctors);
		g_free (dgclass->fields);
		g_free (dgclass->properties);
		g_free (dgclass->events);
	}
	g_free (gclass);
}

static void
free_inflated_signature (MonoInflatedMethodSignature *sig)
{
	mono_metadata_free_inflated_signature (sig->sig);
	g_free (sig);
}

/*
 * LOCKING: assumes the loader lock is held.
 */
MonoMethodInflated*
mono_method_inflated_lookup (MonoMethodInflated* method, gboolean cache)
{
	if (cache) {
		if (!generic_method_cache)
			generic_method_cache = g_hash_table_new_full (inflated_method_hash, inflated_method_equal, NULL, (GDestroyNotify)free_inflated_method);
		g_hash_table_insert (generic_method_cache, method, method);
		return method;
	} else {
		if (generic_method_cache)
			return g_hash_table_lookup (generic_method_cache, method);
		return NULL;
	}
}

/*
 * mono_metadata_get_inflated_signature:
 *
 *   Given an inflated signature and a generic context, return a canonical copy of the 
 * signature. The returned signature might be equal to SIG or it might be a cached copy.
 */
MonoMethodSignature *
mono_metadata_get_inflated_signature (MonoMethodSignature *sig, MonoGenericContext *context)
{
	MonoInflatedMethodSignature helper;
	MonoInflatedMethodSignature *res;

	mono_loader_lock ();
	if (!generic_signature_cache)
		generic_signature_cache = g_hash_table_new_full (inflated_signature_hash, inflated_signature_equal, NULL, (GDestroyNotify)free_inflated_signature);

	helper.sig = sig;
	helper.context.class_inst = context->class_inst;
	helper.context.method_inst = context->method_inst;
	res = g_hash_table_lookup (generic_signature_cache, &helper);
	if (!res) {
		res = g_new0 (MonoInflatedMethodSignature, 1);
		res->sig = sig;
		res->context.class_inst = context->class_inst;
		res->context.method_inst = context->method_inst;
		g_hash_table_insert (generic_signature_cache, res, res);
	}

	mono_loader_unlock ();
	return res->sig;
}

/*
 * mono_metadata_get_generic_inst:
 *
 * Given a list of types, return a MonoGenericInst that represents that list.
 * The returned MonoGenericInst has its own copy of the list of types.  The list
 * passed in the argument can be freed, modified or disposed of.
 *
 */
MonoGenericInst *
mono_metadata_get_generic_inst (int type_argc, MonoType **type_argv)
{
	MonoGenericInst *ginst;
	MonoGenericInst helper;
	int i;

	helper.type_argc = type_argc;
	helper.type_argv = type_argv;
	helper.id = 0;

	for (i = 0; i < type_argc; ++i)
		if (mono_class_is_open_constructed_type (type_argv [i]))
			break;
	helper.is_open = (i < type_argc);

	/*dump_ginst (&helper);*/
	mono_loader_lock ();
	ginst = g_hash_table_lookup (generic_inst_cache, &helper);
	if (ginst) {
		mono_loader_unlock ();
		/*g_print (" found cached\n");*/
		return ginst;
	}

	ginst = g_new0 (MonoGenericInst, 1);
	ginst->type_argc = type_argc;
	ginst->type_argv = g_new (MonoType*, type_argc);
	ginst->id = ++next_generic_inst_id;
	ginst->is_open = helper.is_open;

	for (i = 0; i < type_argc; ++i)
		ginst->type_argv [i] = mono_metadata_type_dup (NULL, type_argv [i]);

	g_hash_table_insert (generic_inst_cache, ginst, ginst);

	mono_loader_unlock ();
	/*g_print (" inserted\n");*/
	return ginst;
}

static gboolean
mono_metadata_is_type_builder_generic_type_definition (MonoClass *container_class, MonoGenericInst *inst, gboolean is_dynamic)
{
	MonoGenericContainer *container = container_class->generic_container; 

	if (!is_dynamic || container_class->wastypebuilder || container->type_argc != inst->type_argc)
		return FALSE;
	return inst == container->context.class_inst;
}

/*
 * mono_metadata_lookup_generic_class:
 *
 * Returns a MonoGenericClass with the given properties.
 *
 */
MonoGenericClass *
mono_metadata_lookup_generic_class (MonoClass *container_class, MonoGenericInst *inst, gboolean is_dynamic)
{
	MonoGenericClass *gclass;
	MonoGenericClass helper;
	gboolean is_tb_open = mono_metadata_is_type_builder_generic_type_definition (container_class, inst, is_dynamic);

	helper.container_class = container_class;
	helper.context.class_inst = inst;
	helper.context.method_inst = NULL;
	helper.is_dynamic = is_dynamic; /* We use this in a hash lookup, which does not attempt to downcast the pointer */
	helper.is_tb_open = is_tb_open;
	helper.cached_class = NULL;

	mono_loader_lock ();

	gclass = g_hash_table_lookup (generic_class_cache, &helper);

	/* A tripwire just to keep us honest */
	g_assert (!helper.cached_class);

	if (gclass) {
		mono_loader_unlock ();
		return gclass;
	}

	if (is_dynamic) {
		MonoDynamicGenericClass *dgclass = g_new0 (MonoDynamicGenericClass, 1);
		gclass = &dgclass->generic_class;
		gclass->is_dynamic = 1;
	} else {
		gclass = g_new0 (MonoGenericClass, 1);
	}

	gclass->is_tb_open = is_tb_open;
	gclass->container_class = container_class;
	gclass->context.class_inst = inst;
	gclass->context.method_inst = NULL;
	if (inst == container_class->generic_container->context.class_inst && !is_tb_open)
		gclass->cached_class = container_class;

	g_hash_table_insert (generic_class_cache, gclass, gclass);

	mono_loader_unlock ();

	return gclass;
}

/*
 * mono_metadata_inflate_generic_inst:
 *
 * Instantiate the generic instance @ginst with the context @context.
 *
 */
MonoGenericInst *
mono_metadata_inflate_generic_inst (MonoGenericInst *ginst, MonoGenericContext *context)
{
	MonoType **type_argv;
	MonoGenericInst *nginst;
	int i;

	if (!ginst->is_open)
		return ginst;

	type_argv = g_new0 (MonoType*, ginst->type_argc);

	for (i = 0; i < ginst->type_argc; i++)
		type_argv [i] = mono_class_inflate_generic_type (ginst->type_argv [i], context);

	nginst = mono_metadata_get_generic_inst (ginst->type_argc, type_argv);

	for (i = 0; i < ginst->type_argc; i++)
		mono_metadata_free_type (type_argv [i]);
	g_free (type_argv);

	return nginst;
}

MonoGenericInst *
mono_metadata_parse_generic_inst (MonoImage *m, MonoGenericContainer *container,
				  int count, const char *ptr, const char **rptr)
{
	MonoType **type_argv;
	MonoGenericInst *ginst;
	int i;

	type_argv = g_new0 (MonoType*, count);

	for (i = 0; i < count; i++) {
		MonoType *t = mono_metadata_parse_type_full (m, container, MONO_PARSE_TYPE, 0, ptr, &ptr);
		if (!t) {
			g_free (type_argv);
			return NULL;
		}
		type_argv [i] = t;
	}

	if (rptr)
		*rptr = ptr;

	ginst = mono_metadata_get_generic_inst (count, type_argv);

	g_free (type_argv);

	return ginst;
}

static gboolean
do_mono_metadata_parse_generic_class (MonoType *type, MonoImage *m, MonoGenericContainer *container,
				      const char *ptr, const char **rptr)
{
	MonoGenericInst *inst;
	MonoClass *gklass;
	MonoType *gtype;
	int count;

	gtype = mono_metadata_parse_type (m, MONO_PARSE_TYPE, 0, ptr, &ptr);
	if (gtype == NULL)
		return FALSE;

	gklass = mono_class_from_mono_type (gtype);
	if (!gklass->generic_container)
		return FALSE;

	count = mono_metadata_decode_value (ptr, &ptr);
	inst = mono_metadata_parse_generic_inst (m, container, count, ptr, &ptr);
	if (inst == NULL)
		return FALSE;

	if (rptr)
		*rptr = ptr;

	type->data.generic_class = mono_metadata_lookup_generic_class (gklass, inst, FALSE);
	return TRUE;
}

/*
 * select_container:
 * @gc: The generic container to normalize
 * @type: The kind of generic parameters the resulting generic-container should contain
 */

static MonoGenericContainer *
select_container (MonoGenericContainer *gc, MonoTypeEnum type)
{
	gboolean is_var = (type == MONO_TYPE_VAR);
	if (!gc)
		return NULL;

	g_assert (is_var || type == MONO_TYPE_MVAR);

	if (is_var) {
		if (gc->is_method || gc->parent)
			/*
			 * The current MonoGenericContainer is a generic method -> its `parent'
			 * points to the containing class'es container.
			 */
			return gc->parent;
	}

	return gc;
}

/* 
 * mono_metadata_parse_generic_param:
 * @generic_container: Our MonoClass's or MonoMethodNormal's MonoGenericContainer;
 *                     see mono_metadata_parse_type_full() for details.
 * Internal routine to parse a generic type parameter.
 * LOCKING: Assumes the loader lock is held.
 */
static MonoGenericParam *
mono_metadata_parse_generic_param (MonoImage *m, MonoGenericContainer *generic_container,
				   MonoTypeEnum type, const char *ptr, const char **rptr)
{
	int index = mono_metadata_decode_value (ptr, &ptr);
	if (rptr)
		*rptr = ptr;

	generic_container = select_container (generic_container, type);
	if (!generic_container) {
		/* Create dummy MonoGenericParam */
		MonoGenericParam *param = mono_mempool_alloc0 (m->mempool, sizeof (MonoGenericParam));
		param->name = mono_mempool_alloc0 (m->mempool, 8);
		sprintf ((char*)param->name, "%d", index);
		param->num = index;
		param->image = m;

		return param;
	}

	g_assert (index < generic_container->type_argc);
	return &generic_container->type_params [index];
}

/* 
 * do_mono_metadata_parse_type:
 * @type: MonoType to be filled in with the return value
 * @m: image context
 * @generic_context: generics_context
 * @ptr: pointer to the encoded type
 * @rptr: pointer where the end of the encoded type is saved
 * 
 * Internal routine used to "fill" the contents of @type from an 
 * allocated pointer.  This is done this way to avoid doing too
 * many mini-allocations (particularly for the MonoFieldType which
 * most of the time is just a MonoType, but sometimes might be augmented).
 *
 * This routine is used by mono_metadata_parse_type and
 * mono_metadata_parse_field_type
 *
 * This extracts a Type as specified in Partition II (22.2.12) 
 *
 * Returns: FALSE if the type could not be loaded
 */
static gboolean
do_mono_metadata_parse_type (MonoType *type, MonoImage *m, MonoGenericContainer *container,
			     const char *ptr, const char **rptr)
{
	gboolean ok = TRUE;
	type->type = mono_metadata_decode_value (ptr, &ptr);
	
	switch (type->type){
	case MONO_TYPE_VOID:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS: {
		guint32 token;
		token = mono_metadata_parse_typedef_or_ref (m, ptr, &ptr);
		type->data.klass = mono_class_get (m, token);
		if (!type->data.klass)
			return FALSE;
		break;
	}
	case MONO_TYPE_SZARRAY: {
		MonoType *etype = mono_metadata_parse_type_full (m, container, MONO_PARSE_MOD_TYPE, 0, ptr, &ptr);
		if (!etype)
			return FALSE;
		type->data.klass = mono_class_from_mono_type (etype);
		break;
	}
	case MONO_TYPE_PTR:
		type->data.type = mono_metadata_parse_type_full (m, container, MONO_PARSE_MOD_TYPE, 0, ptr, &ptr);
		if (!type->data.type)
			return FALSE;
		break;
	case MONO_TYPE_FNPTR:
		type->data.method = mono_metadata_parse_method_signature_full (m, container, 0, ptr, &ptr);
		break;
	case MONO_TYPE_ARRAY:
		type->data.array = mono_metadata_parse_array_full (m, container, ptr, &ptr);
		break;
	case MONO_TYPE_MVAR:
	case MONO_TYPE_VAR:
		type->data.generic_param = mono_metadata_parse_generic_param (m, container, type->type, ptr, &ptr);
		break;
	case MONO_TYPE_GENERICINST:
		ok = do_mono_metadata_parse_generic_class (type, m, container, ptr, &ptr);
		break;
	default:
		g_error ("type 0x%02x not handled in do_mono_metadata_parse_type", type->type);
	}
	
	if (rptr)
		*rptr = ptr;
	return ok;
}

/*
 * mono_metadata_free_type:
 * @type: type to free
 *
 * Free the memory allocated for type @type which is allocated on the heap.
 */
void
mono_metadata_free_type (MonoType *type)
{
	if (type >= builtin_types && type < builtin_types + NBUILTIN_TYPES ())
		return;
	
	switch (type->type){
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
		if (!type->data.klass)
			break;
		/* fall through */
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		if (type == &type->data.klass->byval_arg || type == &type->data.klass->this_arg)
			return;
		break;
	case MONO_TYPE_PTR:
		mono_metadata_free_type (type->data.type);
		break;
	case MONO_TYPE_FNPTR:
		mono_metadata_free_method_signature (type->data.method);
		break;
	case MONO_TYPE_ARRAY:
		mono_metadata_free_array (type->data.array);
		break;
	}

	g_free (type);
}

#if 0
static void
hex_dump (const char *buffer, int base, int count)
{
	int show_header = 1;
	int i;

	if (count < 0){
		count = -count;
		show_header = 0;
	}
	
	for (i = 0; i < count; i++){
		if (show_header)
			if ((i % 16) == 0)
				printf ("\n0x%08x: ", (unsigned char) base + i);

		printf ("%02x ", (unsigned char) (buffer [i]));
	}
	fflush (stdout);
}
#endif

/** 
 * @mh: The Method header
 * @ptr: Points to the beginning of the Section Data (25.3)
 */
static void
parse_section_data (MonoImage *m, MonoMethodHeader *mh, const unsigned char *ptr)
{
	unsigned char sect_data_flags;
	const unsigned char *sptr;
	int is_fat;
	guint32 sect_data_len;
	
	while (1) {
		/* align on 32-bit boundary */
		sptr = ptr = dword_align (ptr); 
		sect_data_flags = *ptr;
		ptr++;
		
		is_fat = sect_data_flags & METHOD_HEADER_SECTION_FAT_FORMAT;
		if (is_fat) {
			sect_data_len = (ptr [2] << 16) | (ptr [1] << 8) | ptr [0];
			ptr += 3;
		} else {
			sect_data_len = ptr [0];
			++ptr;
		}
		/*
		g_print ("flags: %02x, len: %d\n", sect_data_flags, sect_data_len);
		hex_dump (sptr, 0, sect_data_len+8);
		g_print ("\nheader: ");
		hex_dump (sptr-4, 0, 4);
		g_print ("\n");
		*/
		
		if (sect_data_flags & METHOD_HEADER_SECTION_EHTABLE) {
			const unsigned char *p = dword_align (ptr);
			int i;
			mh->num_clauses = is_fat ? sect_data_len / 24: sect_data_len / 12;
			/* we could just store a pointer if we don't need to byteswap */
			mh->clauses = mono_mempool_alloc0 (m->mempool, sizeof (MonoExceptionClause) * mh->num_clauses);
			for (i = 0; i < mh->num_clauses; ++i) {
				MonoExceptionClause *ec = &mh->clauses [i];
				guint32 tof_value;
				if (is_fat) {
					ec->flags = read32 (p);
					ec->try_offset = read32 (p + 4);
					ec->try_len = read32 (p + 8);
					ec->handler_offset = read32 (p + 12);
					ec->handler_len = read32 (p + 16);
					tof_value = read32 (p + 20);
					p += 24;
				} else {
					ec->flags = read16 (p);
					ec->try_offset = read16 (p + 2);
					ec->try_len = *(p + 4);
					ec->handler_offset = read16 (p + 5);
					ec->handler_len = *(p + 7);
					tof_value = read32 (p + 8);
					p += 12;
				}
				if (ec->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					ec->data.filter_offset = tof_value;
				} else if (ec->flags == MONO_EXCEPTION_CLAUSE_NONE) {
					ec->data.catch_class = tof_value? mono_class_get (m, tof_value): 0;
				} else {
					ec->data.catch_class = NULL;
				}
				/* g_print ("try %d: %x %04x-%04x %04x\n", i, ec->flags, ec->try_offset, ec->try_offset+ec->try_len, ec->try_len); */
			}

		}
		if (sect_data_flags & METHOD_HEADER_SECTION_MORE_SECTS)
			ptr += sect_data_len - 4; /* LAMESPEC: it seems the size includes the header */
		else
			return;
	}
}

/*
 * mono_metadata_parse_mh_full:
 * @m: metadata context
 * @generic_context: generics context
 * @ptr: pointer to the method header.
 *
 * Decode the method header at @ptr, including pointer to the IL code,
 * info about local variables and optional exception tables.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * Returns: a MonoMethodHeader allocated from the image mempool.
 */
MonoMethodHeader *
mono_metadata_parse_mh_full (MonoImage *m, MonoGenericContainer *container, const char *ptr)
{
	MonoMethodHeader *mh;
	unsigned char flags = *(const unsigned char *) ptr;
	unsigned char format = flags & METHOD_HEADER_FORMAT_MASK;
	guint16 fat_flags;
	guint32 local_var_sig_tok, max_stack, code_size, init_locals;
	const unsigned char *code;
	int hsize;
	
	g_return_val_if_fail (ptr != NULL, NULL);

	mono_loader_lock ();
	switch (format) {
	case METHOD_HEADER_TINY_FORMAT:
		mh = mono_mempool_alloc0 (m->mempool, sizeof (MonoMethodHeader));
		ptr++;
		mh->max_stack = 8;
		local_var_sig_tok = 0;
		mh->code_size = flags >> 2;
		mh->code = (unsigned char*)ptr;
		mono_loader_unlock ();
		return mh;
	case METHOD_HEADER_TINY_FORMAT1:
		mh = mono_mempool_alloc0 (m->mempool, sizeof (MonoMethodHeader));
		ptr++;
		mh->max_stack = 8;
		local_var_sig_tok = 0;

		/*
		 * The spec claims 3 bits, but the Beta2 is
		 * incorrect
		 */
		mh->code_size = flags >> 2;
		mh->code = (unsigned char*)ptr;
		mono_loader_unlock ();
		return mh;
	case METHOD_HEADER_FAT_FORMAT:
		fat_flags = read16 (ptr);
		ptr += 2;
		hsize = (fat_flags >> 12) & 0xf;
		max_stack = read16 (ptr);
		ptr += 2;
		code_size = read32 (ptr);
		ptr += 4;
		local_var_sig_tok = read32 (ptr);
		ptr += 4;

		if (fat_flags & METHOD_HEADER_INIT_LOCALS)
			init_locals = 1;
		else
			init_locals = 0;

		code = (unsigned char*)ptr;

		if (!(fat_flags & METHOD_HEADER_MORE_SECTS))
			break;

		/*
		 * There are more sections
		 */
		ptr = (char*)code + code_size;
		break;
	default:
		mono_loader_unlock ();
		return NULL;
	}
		       
	if (local_var_sig_tok) {
		MonoTableInfo *t = &m->tables [MONO_TABLE_STANDALONESIG];
		const char *locals_ptr;
		guint32 cols [MONO_STAND_ALONE_SIGNATURE_SIZE];
		int len=0, i, bsize;

		mono_metadata_decode_row (t, (local_var_sig_tok & 0xffffff)-1, cols, 1);
		locals_ptr = mono_metadata_blob_heap (m, cols [MONO_STAND_ALONE_SIGNATURE]);
		bsize = mono_metadata_decode_blob_size (locals_ptr, &locals_ptr);
		if (*locals_ptr != 0x07)
			g_warning ("wrong signature for locals blob");
		locals_ptr++;
		len = mono_metadata_decode_value (locals_ptr, &locals_ptr);
		mh = mono_mempool_alloc0 (m->mempool, sizeof (MonoMethodHeader) + (len - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType*));
		mh->num_locals = len;
		for (i = 0; i < len; ++i) {
			mh->locals [i] = mono_metadata_parse_type_full (
				m, container, MONO_PARSE_LOCAL, 0, locals_ptr, &locals_ptr);
			if (!mh->locals [i]) {
				mono_loader_unlock ();
				return NULL;
			}
		}
	} else {
		mh = mono_mempool_alloc0 (m->mempool, sizeof (MonoMethodHeader));
	}
	mh->code = code;
	mh->code_size = code_size;
	mh->max_stack = max_stack;
	mh->init_locals = init_locals;
	if (fat_flags & METHOD_HEADER_MORE_SECTS)
		parse_section_data (m, mh, (const unsigned char*)ptr);
	mono_loader_unlock ();
	return mh;
}

/*
 * mono_metadata_parse_mh:
 * @generic_context: generics context
 * @ptr: pointer to the method header.
 *
 * Decode the method header at @ptr, including pointer to the IL code,
 * info about local variables and optional exception tables.
 * This is a Mono runtime internal function.
 *
 * Returns: a MonoMethodHeader.
 */
MonoMethodHeader *
mono_metadata_parse_mh (MonoImage *m, const char *ptr)
{
	MonoMethodHeader *res;

	mono_loader_lock ();

	res = mono_metadata_parse_mh_full (m, NULL, ptr);

	mono_loader_unlock ();

	return res;
}

/*
 * mono_metadata_free_mh:
 * @mh: a method header
 *
 * Free the memory allocated for the method header.
 * This is a Mono runtime internal function.
 */
void
mono_metadata_free_mh (MonoMethodHeader *mh)
{
	/* Allocated from the mempool */
}

/*
 * mono_method_header_get_code:
 * @header: a MonoMethodHeader pointer
 * @code_size: memory location for returning the code size
 * @max_stack: memory location for returning the max stack
 *
 * Method header accessor to retreive info about the IL code properties:
 * a pointer to the IL code itself, the size of the code and the max number
 * of stack slots used by the code.
 *
 * Returns: pointer to the IL code represented by the method header.
 */
const unsigned char*
mono_method_header_get_code (MonoMethodHeader *header, guint32* code_size, guint32* max_stack)
{
	if (code_size)
		*code_size = header->code_size;
	if (max_stack)
		*max_stack = header->max_stack;
	return header->code;
}

/*
 * mono_method_header_get_locals:
 * @header: a MonoMethodHeader pointer
 * @num_locals: memory location for returning the number of local variables
 * @init_locals: memory location for returning the init_locals flag
 *
 * Method header accessor to retreive info about the local variables:
 * an array of local types, the number of locals and whether the locals
 * are supposed to be initialized to 0 on method entry
 *
 * Returns: pointer to an array of types of the local variables
 */
MonoType**
mono_method_header_get_locals (MonoMethodHeader *header, guint32* num_locals, gboolean *init_locals)
{
	if (num_locals)
		*num_locals = header->num_locals;
	if (init_locals)
		*init_locals = header->init_locals;
	return header->locals;
}

/*
 * mono_method_header_get_num_clauses:
 * @header: a MonoMethodHeader pointer
 *
 * Method header accessor to retreive the number of exception clauses.
 *
 * Returns: the number of exception clauses present
 */
int
mono_method_header_get_num_clauses (MonoMethodHeader *header)
{
	return header->num_clauses;
}

/*
 * mono_method_header_get_clauses:
 * @header: a MonoMethodHeader pointer
 * @method: MonoMethod the header belongs to
 * @iter: pointer to a iterator
 * @clause: pointer to a MonoExceptionClause structure which will be filled with the info
 *
 * Get the info about the exception clauses in the method. Set *iter to NULL to
 * initiate the iteration, then call the method repeatedly until it returns FALSE.
 * At each iteration, the structure pointed to by clause if filled with the
 * exception clause information.
 *
 * Returns: TRUE if clause was filled with info, FALSE if there are no more exception
 * clauses.
 */
int
mono_method_header_get_clauses (MonoMethodHeader *header, MonoMethod *method, gpointer *iter, MonoExceptionClause *clause)
{
	MonoExceptionClause *sc;
	/* later we'll be able to use this interface to parse the clause info on demand,
	 * without allocating anything.
	 */
	if (!iter || !header->num_clauses)
		return FALSE;
	if (!*iter) {
		*iter = sc = header->clauses;
		*clause = *sc;
		return TRUE;
	}
	sc = *iter;
	sc++;
	if (sc < header->clauses + header->num_clauses) {
		*iter = sc;
		*clause = *sc;
		return TRUE;
	}
	return FALSE;
}

/**
 * mono_metadata_parse_field_type:
 * @m: metadata context to extract information from
 * @ptr: pointer to the field signature
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Parses the field signature, and returns the type information for it. 
 *
 * Returns: The MonoType that was extracted from @ptr.
 */
MonoType *
mono_metadata_parse_field_type (MonoImage *m, short field_flags, const char *ptr, const char **rptr)
{
	return mono_metadata_parse_type (m, MONO_PARSE_FIELD, field_flags, ptr, rptr);
}

/**
 * mono_metadata_parse_param:
 * @m: metadata context to extract information from
 * @ptr: pointer to the param signature
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Parses the param signature, and returns the type information for it. 
 *
 * Returns: The MonoType that was extracted from @ptr.
 */
MonoType *
mono_metadata_parse_param (MonoImage *m, const char *ptr, const char **rptr)
{
	return mono_metadata_parse_type (m, MONO_PARSE_PARAM, 0, ptr, rptr);
}

/*
 * mono_metadata_token_from_dor:
 * @dor_token: A TypeDefOrRef coded index
 *
 * dor_token is a TypeDefOrRef coded index: it contains either
 * a TypeDef, TypeRef or TypeSpec in the lower bits, and the upper
 * bits contain an index into the table.
 *
 * Returns: an expanded token
 */
guint32
mono_metadata_token_from_dor (guint32 dor_index)
{
	guint32 table, idx;

	table = dor_index & 0x03;
	idx = dor_index >> 2;

	switch (table){
	case 0: /* TypeDef */
		return MONO_TOKEN_TYPE_DEF | idx;
	case 1: /* TypeRef */
		return MONO_TOKEN_TYPE_REF | idx;
	case 2: /* TypeSpec */
		return MONO_TOKEN_TYPE_SPEC | idx;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

/*
 * We use this to pass context information to the row locator
 */
typedef struct {
	int idx;			/* The index that we are trying to locate */
	int col_idx;		/* The index in the row where idx may be stored */
	MonoTableInfo *t;	/* pointer to the table */
	guint32 result;
} locator_t;

/*
 * How the row locator works.
 *
 *   Table A
 *   ___|___
 *   ___|___         Table B
 *   ___|___------>  _______
 *   ___|___         _______
 *   
 * A column in the rows of table A references an index in table B.
 * For example A may be the TYPEDEF table and B the METHODDEF table.
 * 
 * Given an index in table B we want to get the row in table A
 * where the column n references our index in B.
 *
 * In the locator_t structure:
 * 	t is table A
 * 	col_idx is the column number
 * 	index is the index in table B
 * 	result will be the index in table A
 *
 * Examples:
 * Table A		Table B		column (in table A)
 * TYPEDEF		METHODDEF   MONO_TYPEDEF_METHOD_LIST
 * TYPEDEF		FIELD		MONO_TYPEDEF_FIELD_LIST
 * PROPERTYMAP	PROPERTY	MONO_PROPERTY_MAP_PROPERTY_LIST
 * INTERFIMPL	TYPEDEF   	MONO_INTERFACEIMPL_CLASS
 * METHODSEM	PROPERTY	ASSOCIATION (encoded index)
 *
 * Note that we still don't support encoded indexes.
 *
 */
static int
typedef_locator (const void *a, const void *b)
{
	locator_t *loc = (locator_t *) a;
	const char *bb = (const char *) b;
	int typedef_index = (bb - loc->t->base) / loc->t->row_size;
	guint32 col, col_next;

	col = mono_metadata_decode_row_col (loc->t, typedef_index, loc->col_idx);

	if (loc->idx < col)
		return -1;

	/*
	 * Need to check that the next row is valid.
	 */
	if (typedef_index + 1 < loc->t->rows) {
		col_next = mono_metadata_decode_row_col (loc->t, typedef_index + 1, loc->col_idx);
		if (loc->idx >= col_next)
			return 1;

		if (col == col_next)
			return 1; 
	}

	loc->result = typedef_index;
	
	return 0;
}

static int
table_locator (const void *a, const void *b)
{
	locator_t *loc = (locator_t *) a;
	const char *bb = (const char *) b;
	guint32 table_index = (bb - loc->t->base) / loc->t->row_size;
	guint32 col;
	
	col = mono_metadata_decode_row_col (loc->t, table_index, loc->col_idx);

	if (loc->idx == col) {
		loc->result = table_index;
		return 0;
	}
	if (loc->idx < col)
		return -1;
	else 
		return 1;
}

static int
declsec_locator (const void *a, const void *b)
{
	locator_t *loc = (locator_t *) a;
	const char *bb = (const char *) b;
	guint32 table_index = (bb - loc->t->base) / loc->t->row_size;
	guint32 col;

	col = mono_metadata_decode_row_col (loc->t, table_index, loc->col_idx);

	if (loc->idx == col) {
		loc->result = table_index;
		return 0;
	}
	if (loc->idx < col)
		return -1;
	else
		return 1;
}

/**
 * search_ptr_table:
 *
 *  Return the 1-based row index in TABLE, which must be one of the *Ptr tables, 
 * which contains IDX.
 */
static guint32
search_ptr_table (MonoImage *image, int table, int idx)
{
	MonoTableInfo *ptrdef = &image->tables [table];
	int i;

	/* Use a linear search to find our index in the table */
	for (i = 0; i < ptrdef->rows; i ++)
		/* All the Ptr tables have the same structure */
		if (mono_metadata_decode_row_col (ptrdef, i, 0) == idx)
			break;

	if (i < ptrdef->rows)
		return i + 1;
	else
		return idx;
}

/**
 * mono_metadata_typedef_from_field:
 * @meta: metadata context
 * @index: FieldDef token
 *
 * Returns: the 1-based index into the TypeDef table of the type that
 * declared the field described by @index, or 0 if not found.
 */
guint32
mono_metadata_typedef_from_field (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_TYPEDEF];
	locator_t loc;

	if (!tdef->base)
		return 0;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_TYPEDEF_FIELD_LIST;
	loc.t = tdef;

	if (meta->uncompressed_metadata)
		loc.idx = search_ptr_table (meta, MONO_TABLE_FIELD_POINTER, loc.idx);

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		g_assert_not_reached ();

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/*
 * mono_metadata_typedef_from_method:
 * @meta: metadata context
 * @index: MethodDef token
 *
 * Returns: the 1-based index into the TypeDef table of the type that
 * declared the method described by @index.  0 if not found.
 */
guint32
mono_metadata_typedef_from_method (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_TYPEDEF];
	locator_t loc;
	
	if (!tdef->base)
		return 0;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_TYPEDEF_METHOD_LIST;
	loc.t = tdef;

	if (meta->uncompressed_metadata)
		loc.idx = search_ptr_table (meta, MONO_TABLE_METHOD_POINTER, loc.idx);

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		g_assert_not_reached ();

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/*
 * mono_metadata_interfaces_from_typedef_full:
 * @meta: metadata context
 * @index: typedef token
 * 
 * The array of interfaces that the @index typedef token implements is returned in
 * @interfaces. The number of elemnts in the array is returned in @count.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * Returns: TRUE on success, FALSE on failure.
 */
gboolean
mono_metadata_interfaces_from_typedef_full (MonoImage *meta, guint32 index, MonoClass ***interfaces, guint *count, MonoGenericContext *context)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_INTERFACEIMPL];
	locator_t loc;
	guint32 start, pos;
	guint32 cols [MONO_INTERFACEIMPL_SIZE];
	MonoClass **result;

	*interfaces = NULL;
	*count = 0;

	if (!tdef->base)
		return TRUE;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_INTERFACEIMPL_CLASS;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return TRUE;

	start = loc.result;
	/*
	 * We may end up in the middle of the rows... 
	 */
	while (start > 0) {
		if (loc.idx == mono_metadata_decode_row_col (tdef, start - 1, MONO_INTERFACEIMPL_CLASS))
			start--;
		else
			break;
	}
	pos = start;
	while (pos < tdef->rows) {
		mono_metadata_decode_row (tdef, pos, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		++pos;
	}

	mono_loader_lock ();
	result = mono_mempool_alloc0 (meta->mempool, sizeof (MonoClass*) * (pos - start));
	mono_loader_unlock ();

	pos = start;
	while (pos < tdef->rows) {
		MonoClass *iface;
		
		mono_metadata_decode_row (tdef, pos, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		iface = mono_class_get_full (
			meta, mono_metadata_token_from_dor (cols [MONO_INTERFACEIMPL_INTERFACE]), context);
		if (iface == NULL)
			return FALSE;
		result [pos - start] = iface;
		++pos;
	}
	*count = pos - start;
	*interfaces = result;
	return TRUE;
}

MonoClass**
mono_metadata_interfaces_from_typedef (MonoImage *meta, guint32 index, guint *count)
{
	MonoClass **interfaces;
	gboolean rv;

	mono_loader_lock ();
	rv = mono_metadata_interfaces_from_typedef_full (meta, index, &interfaces, count, NULL);
	mono_loader_unlock ();
	if (rv)
		return interfaces;
	else
		return NULL;
}

/*
 * mono_metadata_nested_in_typedef:
 * @meta: metadata context
 * @index: typedef token
 * 
 * Returns: the 1-based index into the TypeDef table of the type
 * where the type described by @index is nested.
 * Retruns 0 if @index describes a non-nested type.
 */
guint32
mono_metadata_nested_in_typedef (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_NESTEDCLASS];
	locator_t loc;
	
	if (!tdef->base)
		return 0;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_NESTED_CLASS_NESTED;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return mono_metadata_decode_row_col (tdef, loc.result, MONO_NESTED_CLASS_ENCLOSING) | MONO_TOKEN_TYPE_DEF;
}

/*
 * mono_metadata_nesting_typedef:
 * @meta: metadata context
 * @index: typedef token
 * 
 * Returns: the 1-based index into the TypeDef table of the first type
 * that is nested inside the type described by @index. The search starts at
 * @start_index.  returns 0 if no such type is found.
 */
guint32
mono_metadata_nesting_typedef (MonoImage *meta, guint32 index, guint32 start_index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_NESTEDCLASS];
	guint32 start;
	guint32 class_index = mono_metadata_token_index (index);
	
	if (!tdef->base)
		return 0;

	start = start_index;

	while (start <= tdef->rows) {
		if (class_index == mono_metadata_decode_row_col (tdef, start - 1, MONO_NESTED_CLASS_ENCLOSING))
			break;
		else
			start++;
	}

	if (start > tdef->rows)
		return 0;
	else
		return start;
}

/*
 * mono_metadata_packing_from_typedef:
 * @meta: metadata context
 * @index: token representing a type
 * 
 * Returns: the info stored in the ClassLAyout table for the given typedef token
 * into the @packing and @size pointers.
 * Returns 0 if the info is not found.
 */
guint32
mono_metadata_packing_from_typedef (MonoImage *meta, guint32 index, guint32 *packing, guint32 *size)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_CLASSLAYOUT];
	locator_t loc;
	guint32 cols [MONO_CLASS_LAYOUT_SIZE];
	
	if (!tdef->base)
		return 0;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_CLASS_LAYOUT_PARENT;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	mono_metadata_decode_row (tdef, loc.result, cols, MONO_CLASS_LAYOUT_SIZE);
	if (packing)
		*packing = cols [MONO_CLASS_LAYOUT_PACKING_SIZE];
	if (size)
		*size = cols [MONO_CLASS_LAYOUT_CLASS_SIZE];

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/*
 * mono_metadata_custom_attrs_from_index:
 * @meta: metadata context
 * @index: token representing the parent
 * 
 * Returns: the 1-based index into the CustomAttribute table of the first 
 * attribute which belongs to the metadata object described by @index.
 * Returns 0 if no such attribute is found.
 */
guint32
mono_metadata_custom_attrs_from_index (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	locator_t loc;
	
	if (!tdef->base)
		return 0;

	loc.idx = index;
	loc.col_idx = MONO_CUSTOM_ATTR_PARENT;
	loc.t = tdef;

	/* FIXME: Index translation */

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_CUSTOM_ATTR_PARENT) == index))
		loc.result --;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/*
 * mono_metadata_declsec_from_index:
 * @meta: metadata context
 * @index: token representing the parent
 * 
 * Returns: the 0-based index into the DeclarativeSecurity table of the first 
 * attribute which belongs to the metadata object described by @index.
 * Returns -1 if no such attribute is found.
 */
guint32
mono_metadata_declsec_from_index (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_DECLSECURITY];
	locator_t loc;

	if (!tdef->base)
		return -1;

	loc.idx = index;
	loc.col_idx = MONO_DECL_SECURITY_PARENT;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, declsec_locator))
		return -1;

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_DECL_SECURITY_PARENT) == index))
		loc.result --;

	return loc.result;
}

#ifdef DEBUG
static void
mono_backtrace (int limit)
{
	void *array[limit];
	char **names;
	int i;
	backtrace (array, limit);
	names = backtrace_symbols (array, limit);
	for (i =0; i < limit; ++i) {
		g_print ("\t%s\n", names [i]);
	}
	g_free (names);
}
#endif

#ifndef __GNUC__
/*#define __alignof__(a) sizeof(a)*/
#define __alignof__(type) G_STRUCT_OFFSET(struct { char c; type x; }, x)
#endif

/*
 * mono_type_size:
 * @t: the type to return the size of
 *
 * Returns: the number of bytes required to hold an instance of this
 * type in memory
 */
int
mono_type_size (MonoType *t, int *align)
{
	if (!t) {
		*align = 1;
		return 0;
	}
	if (t->byref) {
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	}

	switch (t->type){
	case MONO_TYPE_VOID:
		*align = 1;
		return 0;
	case MONO_TYPE_BOOLEAN:
		*align = __alignof__(gint8);
		return 1;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*align = __alignof__(gint8);
		return 1;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		*align = __alignof__(gint16);
		return 2;		
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*align = __alignof__(gint32);
		return 4;
	case MONO_TYPE_R4:
		*align = __alignof__(float);
		return 4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*align = __alignof__(gint64);
		return 8;		
	case MONO_TYPE_R8:
		*align = __alignof__(double);
		return 8;		
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_STRING:
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_OBJECT:
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_VALUETYPE: {
		if (t->data.klass->enumtype)
			return mono_type_size (t->data.klass->enum_basetype, align);
		else
			return mono_class_value_size (t->data.klass, (guint32*)align);
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_TYPEDBYREF:
		return mono_class_value_size (mono_defaults.typed_reference_class, (guint32*)align);
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoClass *container_class = gclass->container_class;

		// g_assert (!gclass->inst->is_open);

		if (container_class->valuetype) {
			if (container_class->enumtype)
				return mono_type_size (container_class->enum_basetype, align);
			else
				return mono_class_value_size (mono_class_from_mono_type (t), (guint32*)align);
		} else {
			*align = __alignof__(gpointer);
			return sizeof (gpointer);
		}
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* FIXME: Martin, this is wrong. */
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	default:
		g_error ("mono_type_size: type 0x%02x unknown", t->type);
	}
	return 0;
}

/*
 * mono_type_stack_size:
 * @t: the type to return the size it uses on the stack
 *
 * Returns: the number of bytes required to hold an instance of this
 * type on the runtime stack
 */
int
mono_type_stack_size (MonoType *t, int *align)
{
	return mono_type_stack_size_internal (t, align, FALSE);
}

int
mono_type_stack_size_internal (MonoType *t, int *align, gboolean allow_open)
{
	int tmp;

	g_assert (t != NULL);

	if (!align)
		align = &tmp;

	if (t->byref) {
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	}

	switch (t->type){
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_TYPEDBYREF:
		*align = __alignof__(gpointer);
		return sizeof (gpointer) * 3;
	case MONO_TYPE_R4:
		*align = __alignof__(float);
		return sizeof (float);		
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*align = __alignof__(gint64);
		return sizeof (gint64);		
	case MONO_TYPE_R8:
		*align = __alignof__(double);
		return sizeof (double);
	case MONO_TYPE_VALUETYPE: {
		guint32 size;

		if (t->data.klass->enumtype)
			return mono_type_stack_size_internal (t->data.klass->enum_basetype, align, allow_open);
		else {
			size = mono_class_value_size (t->data.klass, (guint32*)align);

			*align = *align + __alignof__(gpointer) - 1;
			*align &= ~(__alignof__(gpointer) - 1);

			size += sizeof (gpointer) - 1;
			size &= ~(sizeof (gpointer) - 1);

			return size;
		}
	}
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoClass *container_class = gclass->container_class;

		if (!allow_open)
			g_assert (!gclass->context.class_inst->is_open);

		if (container_class->valuetype) {
			if (container_class->enumtype)
				return mono_type_stack_size_internal (container_class->enum_basetype, align, allow_open);
			else {
				guint32 size = mono_class_value_size (mono_class_from_mono_type (t), (guint32*)align);

				*align = *align + __alignof__(gpointer) - 1;
				*align &= ~(__alignof__(gpointer) - 1);

				size += sizeof (gpointer) - 1;
				size &= ~(sizeof (gpointer) - 1);

				return size;
			}
		} else {
			*align = __alignof__(gpointer);
			return sizeof (gpointer);
		}
	}
	default:
		g_error ("type 0x%02x unknown", t->type);
	}
	return 0;
}

gboolean
mono_type_generic_inst_is_valuetype (MonoType *type)
{
	g_assert (type->type == MONO_TYPE_GENERICINST);
	return type->data.generic_class->container_class->valuetype;
}

gboolean
mono_metadata_generic_class_is_valuetype (MonoGenericClass *gclass)
{
	return gclass->container_class->valuetype;
}

static gboolean
_mono_metadata_generic_class_equal (const MonoGenericClass *g1, const MonoGenericClass *g2, gboolean signature_only)
{
	MonoGenericInst *i1 = g1->context.class_inst;
	MonoGenericInst *i2 = g2->context.class_inst;

	if (g1->is_dynamic != g2->is_dynamic)
		return FALSE;
	if (!mono_metadata_class_equal (g1->container_class, g2->container_class, signature_only))
		return FALSE;
	if (!mono_generic_inst_equal_full (i1, i2, signature_only))
		return FALSE;
	return g1->is_tb_open == g2->is_tb_open;
}

static gboolean
_mono_metadata_generic_class_container_equal (const MonoGenericClass *g1, MonoClass *c2, gboolean signature_only)
{
	MonoGenericInst *i1 = g1->context.class_inst;
	MonoGenericInst *i2 = c2->generic_container->context.class_inst;

	if (!mono_metadata_class_equal (g1->container_class, c2, signature_only))
		return FALSE;
	if (!mono_generic_inst_equal_full (i1, i2, signature_only))
		return FALSE;
	return !g1->is_tb_open;
}

guint
mono_metadata_generic_context_hash (const MonoGenericContext *context)
{
	/* FIXME: check if this seed is good enough */
	guint hash = 0xc01dfee7;
	if (context->class_inst)
		hash = ((hash << 5) - hash) ^ context->class_inst->id;
	if (context->method_inst)
		hash = ((hash << 5) - hash) ^ context->method_inst->id;
	return hash;
}

gboolean
mono_metadata_generic_context_equal (const MonoGenericContext *g1, const MonoGenericContext *g2)
{
	return g1->class_inst == g2->class_inst && g1->method_inst == g2->method_inst;
}

/*
 * mono_metadata_type_hash:
 * @t1: a type
 *
 * Computes an hash value for @t1 to be used in GHashTable.
 */
guint
mono_metadata_type_hash (MonoType *t1)
{
	guint hash = t1->type;

	hash |= t1->byref << 6; /* do not collide with t1->type values */
	switch (t1->type) {
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
		/* check if the distribution is good enough */
		return ((hash << 5) - hash) ^ g_str_hash (t1->data.klass->name);
	case MONO_TYPE_PTR:
		return ((hash << 5) - hash) ^ mono_metadata_type_hash (t1->data.type);
	case MONO_TYPE_ARRAY:
		return ((hash << 5) - hash) ^ mono_metadata_type_hash (&t1->data.array->eklass->byval_arg);
	case MONO_TYPE_GENERICINST:
		return ((hash << 5) - hash) ^ mono_generic_class_hash (t1->data.generic_class);
	}
	return hash;
}

static gboolean
mono_metadata_generic_param_equal (MonoGenericParam *p1, MonoGenericParam *p2, gboolean signature_only)
{
	if (p1 == p2)
		return TRUE;
	if (p1->num != p2->num)
		return FALSE;

	if (p1->owner == p2->owner)
		return TRUE;

	/*
	 * If `signature_only' is true, we're comparing two (method) signatures.
	 * In this case, the owner of two type parameters doesn't need to match.
	 */

	return signature_only;
}

static gboolean
mono_metadata_class_equal (MonoClass *c1, MonoClass *c2, gboolean signature_only)
{
	if (c1 == c2)
		return TRUE;
	if (c1->generic_class && c2->generic_class)
		return _mono_metadata_generic_class_equal (c1->generic_class, c2->generic_class, signature_only);
	if (c1->generic_class && c2->generic_container)
		return _mono_metadata_generic_class_container_equal (c1->generic_class, c2, signature_only);
	if (c1->generic_container && c2->generic_class)
		return _mono_metadata_generic_class_container_equal (c2->generic_class, c1, signature_only);
	if ((c1->byval_arg.type == MONO_TYPE_VAR) && (c2->byval_arg.type == MONO_TYPE_VAR))
		return mono_metadata_generic_param_equal (
			c1->byval_arg.data.generic_param, c2->byval_arg.data.generic_param, signature_only);
	if ((c1->byval_arg.type == MONO_TYPE_MVAR) && (c2->byval_arg.type == MONO_TYPE_MVAR))
		return mono_metadata_generic_param_equal (
			c1->byval_arg.data.generic_param, c2->byval_arg.data.generic_param, signature_only);
	if (signature_only &&
	    (c1->byval_arg.type == MONO_TYPE_SZARRAY) && (c2->byval_arg.type == MONO_TYPE_SZARRAY))
		return mono_metadata_class_equal (c1->byval_arg.data.klass, c2->byval_arg.data.klass, signature_only);
	return FALSE;
}

static gboolean
mono_metadata_fnptr_equal (MonoMethodSignature *s1, MonoMethodSignature *s2, gboolean signature_only)
{
	gpointer iter1 = 0, iter2 = 0;

	if (s1 == s2)
		return TRUE;
	if (s1->call_convention != s2->call_convention)
		return FALSE;
	if (s1->sentinelpos != s2->sentinelpos)
		return FALSE;
	if (s1->hasthis != s2->hasthis)
		return FALSE;
	if (s1->explicit_this != s2->explicit_this)
		return FALSE;
	if (! do_mono_metadata_type_equal (s1->ret, s2->ret, signature_only))
		return FALSE;
	if (s1->param_count != s2->param_count)
		return FALSE;

	while (TRUE) {
		MonoType *t1 = mono_signature_get_params (s1, &iter1);
		MonoType *t2 = mono_signature_get_params (s2, &iter2);

		if (t1 == NULL || t2 == NULL)
			return (t1 == t2);
		if (! do_mono_metadata_type_equal (t1, t2, signature_only))
			return FALSE;
	}
}

/*
 * mono_metadata_type_equal:
 * @t1: a type
 * @t2: another type
 *
 * Determine if @t1 and @t2 represent the same type.
 * Returns: #TRUE if @t1 and @t2 are equal.
 */
static gboolean
do_mono_metadata_type_equal (MonoType *t1, MonoType *t2, gboolean signature_only)
{
	if (t1->type != t2->type || t1->byref != t2->byref)
		return FALSE;

	switch (t1->type) {
	case MONO_TYPE_VOID:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_STRING:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
		return mono_metadata_class_equal (t1->data.klass, t2->data.klass, signature_only);
	case MONO_TYPE_PTR:
		return do_mono_metadata_type_equal (t1->data.type, t2->data.type, signature_only);
	case MONO_TYPE_ARRAY:
		if (t1->data.array->rank != t2->data.array->rank)
			return FALSE;
		return mono_metadata_class_equal (t1->data.array->eklass, t2->data.array->eklass, signature_only);
	case MONO_TYPE_GENERICINST:
		return _mono_metadata_generic_class_equal (
			t1->data.generic_class, t2->data.generic_class, signature_only);
	case MONO_TYPE_VAR:
		return mono_metadata_generic_param_equal (
			t1->data.generic_param, t2->data.generic_param, signature_only);
	case MONO_TYPE_MVAR:
		return mono_metadata_generic_param_equal (
			t1->data.generic_param, t2->data.generic_param, signature_only);
	case MONO_TYPE_FNPTR:
		return mono_metadata_fnptr_equal (t1->data.method, t2->data.method, signature_only);
	default:
		g_error ("implement type compare for %0x!", t1->type);
		return FALSE;
	}

	return FALSE;
}

gboolean
mono_metadata_type_equal (MonoType *t1, MonoType *t2)
{
	return do_mono_metadata_type_equal (t1, t2, FALSE);
}

/**
 * mono_metadata_type_equal_full:
 * @t1: a type
 * @t2: another type
 * @signature_only: if signature only comparison should be made
 *
 * Determine if @t1 and @t2 are signature compatible if @signature_only is #TRUE, otherwise
 * behaves the same way as mono_metadata_type_equal.
 * The function mono_metadata_type_equal(a, b) is just a shortcut for mono_metadata_type_equal_full(a, b, FALSE).
 * Returns: #TRUE if @t1 and @t2 are equal taking @signature_only into account.
 */
gboolean
mono_metadata_type_equal_full (MonoType *t1, MonoType *t2, gboolean signature_only)
{
	return do_mono_metadata_type_equal (t1, t2, signature_only);
}

/**
 * mono_metadata_signature_equal:
 * @sig1: a signature
 * @sig2: another signature
 *
 * Determine if @sig1 and @sig2 represent the same signature, with the
 * same number of arguments and the same types.
 * Returns: #TRUE if @sig1 and @sig2 are equal.
 */
gboolean
mono_metadata_signature_equal (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	int i;

	if (sig1->hasthis != sig2->hasthis || sig1->param_count != sig2->param_count)
		return FALSE;

	if (sig1->generic_param_count != sig2->generic_param_count)
		return FALSE;

	/*
	 * We're just comparing the signatures of two methods here:
	 *
	 * If we have two generic methods `void Foo<U> (U u)' and `void Bar<V> (V v)',
	 * U and V are equal here.
	 *
	 * That's what the `signature_only' argument of do_mono_metadata_type_equal() is for.
	 */

	for (i = 0; i < sig1->param_count; i++) { 
		MonoType *p1 = sig1->params[i];
		MonoType *p2 = sig2->params[i];
		
		/* if (p1->attrs != p2->attrs)
			return FALSE;
		*/
		if (!do_mono_metadata_type_equal (p1, p2, TRUE))
			return FALSE;
	}

	if (!do_mono_metadata_type_equal (sig1->ret, sig2->ret, TRUE))
		return FALSE;
	return TRUE;
}

/**
 * mono_metadata_type_dup:
 * @mp: mempool to use
 * @original: type to duplicate
 *
 * Returns: copy of type allocated from mempool (or from the heap, if @mp is null).
 */
MonoType *
mono_metadata_type_dup (MonoMemPool *mp, const MonoType *o)
{
	MonoType *r = NULL;
	int sizeof_o = sizeof (MonoType);
	if (o->num_mods)
		sizeof_o += (o->num_mods - MONO_ZERO_LEN_ARRAY) * sizeof (MonoCustomMod);

	mono_loader_lock ();
	r = mp ? mono_mempool_alloc0 (mp, sizeof_o) : g_malloc (sizeof_o);
	mono_loader_unlock ();

	memcpy (r, o, sizeof_o);

	if (o->type == MONO_TYPE_PTR) {
		r->data.type = mono_metadata_type_dup (mp, o->data.type);
	} else if (o->type == MONO_TYPE_ARRAY) {
		r->data.array = mono_dup_array_type (mp, o->data.array);
	} else if (o->type == MONO_TYPE_FNPTR) {
		/*FIXME the dup'ed signature is leaked mono_metadata_free_type*/
		r->data.method = mono_metadata_signature_deep_dup (mp, o->data.method);
	}
	return r;
}

guint
mono_signature_hash (MonoMethodSignature *sig)
{
	guint i, res = sig->ret->type;

	for (i = 0; i < sig->param_count; i++)
		res = (res << 5) - res + mono_type_hash (sig->params[i]);

	return res;
}

/*
 * mono_metadata_encode_value:
 * @value: value to encode
 * @buf: buffer where to write the compressed representation
 * @endbuf: pointer updated to point at the end of the encoded output
 *
 * Encodes the value @value in the compressed representation used
 * in metadata and stores the result in @buf. @buf needs to be big
 * enough to hold the data (4 bytes).
 */
void
mono_metadata_encode_value (guint32 value, char *buf, char **endbuf)
{
	char *p = buf;
	
	if (value < 0x80)
		*p++ = value;
	else if (value < 0x4000) {
		p [0] = 0x80 | (value >> 8);
		p [1] = value & 0xff;
		p += 2;
	} else {
		p [0] = (value >> 24) | 0xc0;
		p [1] = (value >> 16) & 0xff;
		p [2] = (value >> 8) & 0xff;
		p [3] = value & 0xff;
		p += 4;
	}
	if (endbuf)
		*endbuf = p;
}

/*
 * mono_metadata_field_info:
 * @meta: the Image the field is defined in
 * @index: the index in the field table representing the field
 * @offset: a pointer to an integer where to store the offset that 
 * may have been specified for the field in a FieldLayout table
 * @rva: a pointer to the RVA of the field data in the image that
 * may have been defined in a FieldRVA table
 * @marshal_spec: a pointer to the marshal spec that may have been 
 * defined for the field in a FieldMarshal table.
 *
 * Gather info for field @index that may have been defined in the FieldLayout, 
 * FieldRVA and FieldMarshal tables.
 * Either of offset, rva and marshal_spec can be NULL if you're not interested 
 * in the data.
 */
void
mono_metadata_field_info (MonoImage *meta, guint32 index, guint32 *offset, guint32 *rva, 
			  MonoMarshalSpec **marshal_spec)
{
	MonoTableInfo *tdef;
	locator_t loc;

	loc.idx = index + 1;
	if (meta->uncompressed_metadata)
		loc.idx = search_ptr_table (meta, MONO_TABLE_FIELD_POINTER, loc.idx);

	if (offset) {
		tdef = &meta->tables [MONO_TABLE_FIELDLAYOUT];

		loc.col_idx = MONO_FIELD_LAYOUT_FIELD;
		loc.t = tdef;

		if (tdef->base && bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator)) {
			*offset = mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_LAYOUT_OFFSET);
		} else {
			*offset = (guint32)-1;
		}
	}
	if (rva) {
		tdef = &meta->tables [MONO_TABLE_FIELDRVA];

		loc.col_idx = MONO_FIELD_RVA_FIELD;
		loc.t = tdef;
		
		if (tdef->base && bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator)) {
			/*
			 * LAMESPEC: There is no signature, no nothing, just the raw data.
			 */
			*rva = mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_RVA_RVA);
		} else {
			*rva = 0;
		}
	}
	if (marshal_spec) {
		const char *p;
		
		if ((p = mono_metadata_get_marshal_info (meta, index, TRUE))) {
			*marshal_spec = mono_metadata_parse_marshal_spec (meta, p);
		}
	}

}

/*
 * mono_metadata_get_constant_index:
 * @meta: the Image the field is defined in
 * @index: the token that may have a row defined in the constants table
 * @hint: possible position for the row
 *
 * @token must be a FieldDef, ParamDef or PropertyDef token.
 *
 * Returns: the index into the Constants table or 0 if not found.
 */
guint32
mono_metadata_get_constant_index (MonoImage *meta, guint32 token, guint32 hint)
{
	MonoTableInfo *tdef;
	locator_t loc;
	guint32 index = mono_metadata_token_index (token);

	tdef = &meta->tables [MONO_TABLE_CONSTANT];
	index <<= MONO_HASCONSTANT_BITS;
	switch (mono_metadata_token_table (token)) {
	case MONO_TABLE_FIELD:
		index |= MONO_HASCONSTANT_FIEDDEF;
		break;
	case MONO_TABLE_PARAM:
		index |= MONO_HASCONSTANT_PARAM;
		break;
	case MONO_TABLE_PROPERTY:
		index |= MONO_HASCONSTANT_PROPERTY;
		break;
	default:
		g_warning ("Not a valid token for the constant table: 0x%08x", token);
		return 0;
	}
	loc.idx = index;
	loc.col_idx = MONO_CONSTANT_PARENT;
	loc.t = tdef;

	/* FIXME: Index translation */

	if ((hint > 0) && (hint < tdef->rows) && (mono_metadata_decode_row_col (tdef, hint - 1, MONO_CONSTANT_PARENT) == index))
		return hint;

	if (tdef->base && bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator)) {
		return loc.result + 1;
	}
	return 0;
}

/*
 * mono_metadata_events_from_typedef:
 * @meta: metadata context
 * @index: 0-based index (in the TypeDef table) describing a type
 *
 * Returns: the 0-based index in the Event table for the events in the
 * type. The last event that belongs to the type (plus 1) is stored
 * in the @end_idx pointer.
 */
guint32
mono_metadata_events_from_typedef (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint32 start, end;
	MonoTableInfo *tdef  = &meta->tables [MONO_TABLE_EVENTMAP];

	*end_idx = 0;
	
	if (!tdef->base)
		return 0;

	loc.t = tdef;
	loc.col_idx = MONO_EVENT_MAP_PARENT;
	loc.idx = index + 1;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;
	
	start = mono_metadata_decode_row_col (tdef, loc.result, MONO_EVENT_MAP_EVENTLIST);
	if (loc.result + 1 < tdef->rows) {
		end = mono_metadata_decode_row_col (tdef, loc.result + 1, MONO_EVENT_MAP_EVENTLIST) - 1;
	} else {
		end = meta->tables [MONO_TABLE_EVENT].rows;
	}

	*end_idx = end;
	return start - 1;
}

/*
 * mono_metadata_methods_from_event:
 * @meta: metadata context
 * @index: 0-based index (in the Event table) describing a event
 *
 * Returns: the 0-based index in the MethodDef table for the methods in the
 * event. The last method that belongs to the event (plus 1) is stored
 * in the @end_idx pointer.
 */
guint32
mono_metadata_methods_from_event   (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint start, end;
	guint32 cols [MONO_METHOD_SEMA_SIZE];
	MonoTableInfo *msemt = &meta->tables [MONO_TABLE_METHODSEMANTICS];

	*end_idx = 0;
	if (!msemt->base)
		return 0;

	if (meta->uncompressed_metadata)
	    index = search_ptr_table (meta, MONO_TABLE_EVENT_POINTER, index + 1) - 1;

	loc.t = msemt;
	loc.col_idx = MONO_METHOD_SEMA_ASSOCIATION;
	loc.idx = ((index + 1) << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT; /* Method association coded index */

	if (!bsearch (&loc, msemt->base, msemt->rows, msemt->row_size, table_locator))
		return 0;

	start = loc.result;
	/*
	 * We may end up in the middle of the rows... 
	 */
	while (start > 0) {
		if (loc.idx == mono_metadata_decode_row_col (msemt, start - 1, MONO_METHOD_SEMA_ASSOCIATION))
			start--;
		else
			break;
	}
	end = start + 1;
	while (end < msemt->rows) {
		mono_metadata_decode_row (msemt, end, cols, MONO_METHOD_SEMA_SIZE);
		if (cols [MONO_METHOD_SEMA_ASSOCIATION] != loc.idx)
			break;
		++end;
	}
	*end_idx = end;
	return start;
}

/*
 * mono_metadata_properties_from_typedef:
 * @meta: metadata context
 * @index: 0-based index (in the TypeDef table) describing a type
 *
 * Returns: the 0-based index in the Property table for the properties in the
 * type. The last property that belongs to the type (plus 1) is stored
 * in the @end_idx pointer.
 */
guint32
mono_metadata_properties_from_typedef (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint32 start, end;
	MonoTableInfo *tdef  = &meta->tables [MONO_TABLE_PROPERTYMAP];

	*end_idx = 0;
	
	if (!tdef->base)
		return 0;

	loc.t = tdef;
	loc.col_idx = MONO_PROPERTY_MAP_PARENT;
	loc.idx = index + 1;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;
	
	start = mono_metadata_decode_row_col (tdef, loc.result, MONO_PROPERTY_MAP_PROPERTY_LIST);
	if (loc.result + 1 < tdef->rows) {
		end = mono_metadata_decode_row_col (tdef, loc.result + 1, MONO_PROPERTY_MAP_PROPERTY_LIST) - 1;
	} else {
		end = meta->tables [MONO_TABLE_PROPERTY].rows;
	}

	*end_idx = end;
	return start - 1;
}

/*
 * mono_metadata_methods_from_property:
 * @meta: metadata context
 * @index: 0-based index (in the PropertyDef table) describing a property
 *
 * Returns: the 0-based index in the MethodDef table for the methods in the
 * property. The last method that belongs to the property (plus 1) is stored
 * in the @end_idx pointer.
 */
guint32
mono_metadata_methods_from_property   (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint start, end;
	guint32 cols [MONO_METHOD_SEMA_SIZE];
	MonoTableInfo *msemt = &meta->tables [MONO_TABLE_METHODSEMANTICS];

	*end_idx = 0;
	if (!msemt->base)
		return 0;

	if (meta->uncompressed_metadata)
	    index = search_ptr_table (meta, MONO_TABLE_PROPERTY_POINTER, index + 1) - 1;

	loc.t = msemt;
	loc.col_idx = MONO_METHOD_SEMA_ASSOCIATION;
	loc.idx = ((index + 1) << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_PROPERTY; /* Method association coded index */

	if (!bsearch (&loc, msemt->base, msemt->rows, msemt->row_size, table_locator))
		return 0;

	start = loc.result;
	/*
	 * We may end up in the middle of the rows... 
	 */
	while (start > 0) {
		if (loc.idx == mono_metadata_decode_row_col (msemt, start - 1, MONO_METHOD_SEMA_ASSOCIATION))
			start--;
		else
			break;
	}
	end = start + 1;
	while (end < msemt->rows) {
		mono_metadata_decode_row (msemt, end, cols, MONO_METHOD_SEMA_SIZE);
		if (cols [MONO_METHOD_SEMA_ASSOCIATION] != loc.idx)
			break;
		++end;
	}
	*end_idx = end;
	return start;
}

guint32
mono_metadata_implmap_from_method (MonoImage *meta, guint32 method_idx)
{
	locator_t loc;
	MonoTableInfo *tdef  = &meta->tables [MONO_TABLE_IMPLMAP];

	if (!tdef->base)
		return 0;

	/* No index translation seems to be needed */

	loc.t = tdef;
	loc.col_idx = MONO_IMPLMAP_MEMBER;
	loc.idx = ((method_idx + 1) << MONO_MEMBERFORWD_BITS) | MONO_MEMBERFORWD_METHODDEF;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	return loc.result + 1;
}

/**
 * @image: context where the image is created
 * @type_spec:  typespec token
 *
 * Creates a MonoType representing the TypeSpec indexed by the @type_spec
 * token.
 */
MonoType *
mono_type_create_from_typespec (MonoImage *image, guint32 type_spec)
{
	guint32 idx = mono_metadata_token_index (type_spec);
	MonoTableInfo *t;
	guint32 cols [MONO_TYPESPEC_SIZE];
	const char *ptr;
	guint32 len;
	MonoType *type, *type2;

	mono_loader_lock ();

	type = g_hash_table_lookup (image->typespec_cache, GUINT_TO_POINTER (type_spec));
	if (type) {
		mono_loader_unlock ();
		return type;
	}

	t = &image->tables [MONO_TABLE_TYPESPEC];

	mono_metadata_decode_row (t, idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (image, cols [MONO_TYPESPEC_SIGNATURE]);
	len = mono_metadata_decode_value (ptr, &ptr);

	type = mono_mempool_alloc0 (image->mempool, sizeof (MonoType));

	if (*ptr == MONO_TYPE_BYREF) {
		type->byref = 1;
		ptr++;
	}

	if (!do_mono_metadata_parse_type (type, image, NULL, ptr, &ptr)) {
		mono_loader_unlock ();
		return NULL;
	}

	type2 = g_hash_table_lookup (image->typespec_cache, GUINT_TO_POINTER (type_spec));

	if (type2) {
		mono_loader_unlock ();
		return type2;
	}

	g_hash_table_insert (image->typespec_cache, GUINT_TO_POINTER (type_spec), type);

	mono_loader_unlock ();

	return type;
}

MonoMarshalSpec *
mono_metadata_parse_marshal_spec (MonoImage *image, const char *ptr)
{
	MonoMarshalSpec *res;
	int len;
	const char *start = ptr;

	/* fixme: this is incomplete, but I cant find more infos in the specs */

	res = g_new0 (MonoMarshalSpec, 1);
	
	len = mono_metadata_decode_value (ptr, &ptr);
	res->native = *ptr++;

	if (res->native == MONO_NATIVE_LPARRAY) {
		res->data.array_data.param_num = -1;
		res->data.array_data.num_elem = -1;
		res->data.array_data.elem_mult = -1;

		if (ptr - start <= len)
			res->data.array_data.elem_type = *ptr++;
		if (ptr - start <= len)
			res->data.array_data.param_num = mono_metadata_decode_value (ptr, &ptr);
		if (ptr - start <= len)
			res->data.array_data.num_elem = mono_metadata_decode_value (ptr, &ptr);
		if (ptr - start <= len) {
			/*
			 * LAMESPEC: Older spec versions say this parameter comes before 
			 * num_elem. Never spec versions don't talk about elem_mult at
			 * all, but csc still emits it, and it is used to distinguish
			 * between param_num being 0, and param_num being omitted.
			 * So if (param_num == 0) && (num_elem > 0), then
			 * elem_mult == 0 -> the array size is num_elem
			 * elem_mult == 1 -> the array size is @param_num + num_elem
			 */
			res->data.array_data.elem_mult = mono_metadata_decode_value (ptr, &ptr);
		}
	} 

	if (res->native == MONO_NATIVE_BYVALTSTR) {
		if (ptr - start <= len)
			res->data.array_data.num_elem = mono_metadata_decode_value (ptr, &ptr);
	}

	if (res->native == MONO_NATIVE_BYVALARRAY) {
		if (ptr - start <= len)
			res->data.array_data.num_elem = mono_metadata_decode_value (ptr, &ptr);
	}
	
	if (res->native == MONO_NATIVE_CUSTOM) {
		/* skip unused type guid */
		len = mono_metadata_decode_value (ptr, &ptr);
		ptr += len;
		/* skip unused native type name */
		len = mono_metadata_decode_value (ptr, &ptr);
		ptr += len;
		/* read custom marshaler type name */
		len = mono_metadata_decode_value (ptr, &ptr);
		res->data.custom_data.custom_name = g_strndup (ptr, len);		
		ptr += len;
		/* read cookie string */
		len = mono_metadata_decode_value (ptr, &ptr);
		res->data.custom_data.cookie = g_strndup (ptr, len);
	}

	if (res->native == MONO_NATIVE_SAFEARRAY) {
		res->data.safearray_data.elem_type = 0;
		res->data.safearray_data.num_elem = 0;
		if (ptr - start <= len)
			res->data.safearray_data.elem_type = *ptr++;
		if (ptr - start <= len)
			res->data.safearray_data.num_elem = *ptr++;
	}
	return res;
}

void 
mono_metadata_free_marshal_spec (MonoMarshalSpec *spec)
{
	if (spec->native == MONO_NATIVE_CUSTOM) {
		g_free (spec->data.custom_data.custom_name);
		g_free (spec->data.custom_data.cookie);
	}
	g_free (spec);
}

/**
 * mono_type_to_unmanaged:
 *
 * Returns: A MonoMarshalNative enumeration value (MONO_NATIVE_) value
 * describing the underlying native reprensetation of the type.
 * 
 * In addition the value pointed by
 * "conv" will contain the kind of marshalling required for this
 * particular type one of the MONO_MARSHAL_CONV_ enumeration values.
 */
guint32
mono_type_to_unmanaged (MonoType *type, MonoMarshalSpec *mspec, gboolean as_field,
			gboolean unicode, MonoMarshalConv *conv) 
{
	MonoMarshalConv dummy_conv;
	int t = type->type;

	if (!conv)
		conv = &dummy_conv;

	*conv = MONO_MARSHAL_CONV_NONE;

	if (type->byref)
		return MONO_NATIVE_UINT;

handle_enum:
	switch (t) {
	case MONO_TYPE_BOOLEAN: 
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_VARIANTBOOL:
				*conv = MONO_MARSHAL_CONV_BOOL_VARIANTBOOL;
				return MONO_NATIVE_VARIANTBOOL;
			case MONO_NATIVE_BOOLEAN:
				*conv = MONO_MARSHAL_CONV_BOOL_I4;
				return MONO_NATIVE_BOOLEAN;
			case MONO_NATIVE_I1:
			case MONO_NATIVE_U1:
				return mspec->native;
			default:
				g_error ("cant marshal bool to native type %02x", mspec->native);
			}
		}
		*conv = MONO_MARSHAL_CONV_BOOL_I4;
		return MONO_NATIVE_BOOLEAN;
	case MONO_TYPE_CHAR: return MONO_NATIVE_U2;
	case MONO_TYPE_I1: return MONO_NATIVE_I1;
	case MONO_TYPE_U1: return MONO_NATIVE_U1;
	case MONO_TYPE_I2: return MONO_NATIVE_I2;
	case MONO_TYPE_U2: return MONO_NATIVE_U2;
	case MONO_TYPE_I4: return MONO_NATIVE_I4;
	case MONO_TYPE_U4: return MONO_NATIVE_U4;
	case MONO_TYPE_I8: return MONO_NATIVE_I8;
	case MONO_TYPE_U8: return MONO_NATIVE_U8;
	case MONO_TYPE_R4: return MONO_NATIVE_R4;
	case MONO_TYPE_R8: return MONO_NATIVE_R8;
	case MONO_TYPE_STRING:
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_BSTR:
				*conv = MONO_MARSHAL_CONV_STR_BSTR;
				return MONO_NATIVE_BSTR;
			case MONO_NATIVE_LPSTR:
				*conv = MONO_MARSHAL_CONV_STR_LPSTR;
				return MONO_NATIVE_LPSTR;
			case MONO_NATIVE_LPWSTR:
				*conv = MONO_MARSHAL_CONV_STR_LPWSTR;
				return MONO_NATIVE_LPWSTR;
			case MONO_NATIVE_LPTSTR:
				*conv = MONO_MARSHAL_CONV_STR_LPTSTR;
				return MONO_NATIVE_LPTSTR;
			case MONO_NATIVE_ANSIBSTR:
				*conv = MONO_MARSHAL_CONV_STR_ANSIBSTR;
				return MONO_NATIVE_ANSIBSTR;
			case MONO_NATIVE_TBSTR:
				*conv = MONO_MARSHAL_CONV_STR_TBSTR;
				return MONO_NATIVE_TBSTR;
			case MONO_NATIVE_BYVALTSTR:
				if (unicode)
					*conv = MONO_MARSHAL_CONV_STR_BYVALWSTR;
				else
					*conv = MONO_MARSHAL_CONV_STR_BYVALSTR;
				return MONO_NATIVE_BYVALTSTR;
			default:
				g_error ("Can not marshal string to native type '%02x': Invalid managed/unmanaged type combination (String fields must be paired with LPStr, LPWStr, BStr or ByValTStr).", mspec->native);
			}
		} 	
		if (unicode) {
			*conv = MONO_MARSHAL_CONV_STR_LPWSTR;
			return MONO_NATIVE_LPWSTR; 
		}
		else {
			*conv = MONO_MARSHAL_CONV_STR_LPSTR;
			return MONO_NATIVE_LPSTR; 
		}
	case MONO_TYPE_PTR: return MONO_NATIVE_UINT;
	case MONO_TYPE_VALUETYPE: /*FIXME*/
		if (type->data.klass->enumtype) {
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		if (type->data.klass == mono_defaults.handleref_class){
			*conv = MONO_MARSHAL_CONV_HANDLEREF;
			return MONO_NATIVE_INT;
		}
		return MONO_NATIVE_STRUCT;
	case MONO_TYPE_SZARRAY: 
	case MONO_TYPE_ARRAY: 
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_BYVALARRAY:
				if ((type->data.klass->element_class == mono_defaults.char_class) && !unicode)
					*conv = MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY;
				else
					*conv = MONO_MARSHAL_CONV_ARRAY_BYVALARRAY;
				return MONO_NATIVE_BYVALARRAY;
			case MONO_NATIVE_SAFEARRAY:
				*conv = MONO_MARSHAL_CONV_ARRAY_SAVEARRAY;
				return MONO_NATIVE_SAFEARRAY;
			case MONO_NATIVE_LPARRAY:				
				*conv = MONO_MARSHAL_CONV_ARRAY_LPARRAY;
				return MONO_NATIVE_LPARRAY;
			default:
				g_error ("cant marshal array as native type %02x", mspec->native);
			}
		} 	

		*conv = MONO_MARSHAL_CONV_ARRAY_LPARRAY;
		return MONO_NATIVE_LPARRAY;
	case MONO_TYPE_I: return MONO_NATIVE_INT;
	case MONO_TYPE_U: return MONO_NATIVE_UINT;
	case MONO_TYPE_CLASS: 
	case MONO_TYPE_OBJECT: {
		/* FIXME : we need to handle ArrayList and StringBuilder here, probably */
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_STRUCT:
				return MONO_NATIVE_STRUCT;
			case MONO_NATIVE_CUSTOM:
				return MONO_NATIVE_CUSTOM;
			case MONO_NATIVE_INTERFACE:
				*conv = MONO_MARSHAL_CONV_OBJECT_INTERFACE;
				return MONO_NATIVE_INTERFACE;
			case MONO_NATIVE_IDISPATCH:
				*conv = MONO_MARSHAL_CONV_OBJECT_IDISPATCH;
				return MONO_NATIVE_IDISPATCH;
			case MONO_NATIVE_IUNKNOWN:
				*conv = MONO_MARSHAL_CONV_OBJECT_IUNKNOWN;
				return MONO_NATIVE_IUNKNOWN;
			case MONO_NATIVE_FUNC:
				if (t == MONO_TYPE_CLASS && (type->data.klass == mono_defaults.multicastdelegate_class ||
											 type->data.klass == mono_defaults.delegate_class || 
											 type->data.klass->parent == mono_defaults.multicastdelegate_class)) {
					*conv = MONO_MARSHAL_CONV_DEL_FTN;
					return MONO_NATIVE_FUNC;
				}
				/* Fall through */
			default:
				g_error ("cant marshal object as native type %02x", mspec->native);
			}
		}
		if (t == MONO_TYPE_CLASS && (type->data.klass == mono_defaults.multicastdelegate_class ||
					     type->data.klass == mono_defaults.delegate_class || 
					     type->data.klass->parent == mono_defaults.multicastdelegate_class)) {
			*conv = MONO_MARSHAL_CONV_DEL_FTN;
			return MONO_NATIVE_FUNC;
		}
		if (mono_defaults.safehandle_class && type->data.klass == mono_defaults.safehandle_class){
			*conv = MONO_MARSHAL_CONV_SAFEHANDLE;
			return MONO_NATIVE_INT;
		}
		*conv = MONO_MARSHAL_CONV_OBJECT_STRUCT;
		return MONO_NATIVE_STRUCT;
	}
	case MONO_TYPE_FNPTR: return MONO_NATIVE_FUNC;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		t = type->type;
		goto handle_enum;
	case MONO_TYPE_TYPEDBYREF:
	default:
		g_error ("type 0x%02x not handled in marshal", t);
	}
	return MONO_NATIVE_MAX;
}

const char*
mono_metadata_get_marshal_info (MonoImage *meta, guint32 idx, gboolean is_field)
{
	locator_t loc;
	MonoTableInfo *tdef  = &meta->tables [MONO_TABLE_FIELDMARSHAL];

	if (!tdef->base)
		return NULL;

	loc.t = tdef;
	loc.col_idx = MONO_FIELD_MARSHAL_PARENT;
	loc.idx = ((idx + 1) << MONO_HAS_FIELD_MARSHAL_BITS) | (is_field? MONO_HAS_FIELD_MARSHAL_FIELDSREF: MONO_HAS_FIELD_MARSHAL_PARAMDEF);

	/* FIXME: Index translation */

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return NULL;

	return mono_metadata_blob_heap (meta, mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_MARSHAL_NATIVE_TYPE));
}

static MonoMethod*
method_from_method_def_or_ref (MonoImage *m, guint32 tok, MonoGenericContext *context)
{
	guint32 idx = tok >> MONO_METHODDEFORREF_BITS;

	switch (tok & MONO_METHODDEFORREF_MASK) {
	case MONO_METHODDEFORREF_METHODDEF:
		return mono_get_method_full (m, MONO_TOKEN_METHOD_DEF | idx, NULL, context);
	case MONO_METHODDEFORREF_METHODREF:
		return mono_get_method_full (m, MONO_TOKEN_MEMBER_REF | idx, NULL, context);
	}
	g_assert_not_reached ();
	return NULL;
}

/*
 * mono_class_get_overrides_full:
 *
 *   Return the method overrides belonging to class @type_token in @overrides, and
 * the number of overrides in @num_overrides.
 *
 * Returns: TRUE on success, FALSE on failure.
 */
gboolean
mono_class_get_overrides_full (MonoImage *image, guint32 type_token, MonoMethod ***overrides, gint32 *num_overrides,
			       MonoGenericContext *generic_context)
{
	locator_t loc;
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_METHODIMPL];
	guint32 start, end;
	gint32 i, num;
	guint32 cols [MONO_METHODIMPL_SIZE];
	MonoMethod **result;
	gint32 ok = TRUE;
	
	*overrides = NULL;
	if (num_overrides)
		*num_overrides = 0;

	if (!tdef->base)
		return TRUE;

	loc.t = tdef;
	loc.col_idx = MONO_METHODIMPL_CLASS;
	loc.idx = mono_metadata_token_index (type_token);

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return TRUE;

	start = loc.result;
	end = start + 1;
	/*
	 * We may end up in the middle of the rows... 
	 */
	while (start > 0) {
		if (loc.idx == mono_metadata_decode_row_col (tdef, start - 1, MONO_METHODIMPL_CLASS))
			start--;
		else
			break;
	}
	while (end < tdef->rows) {
		if (loc.idx == mono_metadata_decode_row_col (tdef, end, MONO_METHODIMPL_CLASS))
			end++;
		else
			break;
	}
	num = end - start;
	result = g_new (MonoMethod*, num * 2);
	for (i = 0; i < num; ++i) {
		MonoMethod *method;

		mono_metadata_decode_row (tdef, start + i, cols, MONO_METHODIMPL_SIZE);
		method = method_from_method_def_or_ref (
			image, cols [MONO_METHODIMPL_DECLARATION], generic_context);
		if (method == NULL)
			ok = FALSE;
		result [i * 2] = method;
		method = method_from_method_def_or_ref (
			image, cols [MONO_METHODIMPL_BODY], generic_context);
		if (method == NULL)
			ok = FALSE;
		result [i * 2 + 1] = method;
	}

	*overrides = result;
	if (num_overrides)
		*num_overrides = num;
	return ok;
}

/**
 * mono_guid_to_string:
 *
 * Converts a 16 byte Microsoft GUID to the standard string representation.
 */
char *
mono_guid_to_string (const guint8 *guid)
{
	return g_strdup_printf ("%02X%02X%02X%02X-%02X%02X-%02X%02X-%02X%02X-%02X%02X%02X%02X%02X%02X", 
				guid[3], guid[2], guid[1], guid[0],
				guid[5], guid[4],
				guid[7], guid[6],
				guid[8], guid[9],
				guid[10], guid[11], guid[12], guid[13], guid[14], guid[15]);
}

static gboolean
get_constraints (MonoImage *image, int owner, MonoClass ***constraints, MonoGenericContainer *container)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	guint32 cols [MONO_GENPARCONSTRAINT_SIZE];
	guint32 i, token, found;
	MonoClass *klass, **res;
	GList *cons = NULL, *tmp;
	MonoGenericContext *context = &container->context;

	*constraints = NULL;
	found = 0;
	for (i = 0; i < tdef->rows; ++i) {
		mono_metadata_decode_row (tdef, i, cols, MONO_GENPARCONSTRAINT_SIZE);
		if (cols [MONO_GENPARCONSTRAINT_GENERICPAR] == owner) {
			token = mono_metadata_token_from_dor (cols [MONO_GENPARCONSTRAINT_CONSTRAINT]);
			klass = mono_class_get_full (image, token, context);
			cons = g_list_append (cons, klass);
			++found;
		} else {
			/* contiguous list finished */
			if (found)
				break;
		}
	}
	if (!found)
		return TRUE;
	res = g_new0 (MonoClass*, found + 1);
	for (i = 0, tmp = cons; i < found; ++i, tmp = tmp->next) {
		res [i] = tmp->data;
	}
	g_list_free (cons);
	*constraints = res;
	return TRUE;
}

/*
 * mono_metadata_get_generic_param_row:
 *
 * @image:
 * @token: TypeOrMethodDef token, owner for GenericParam
 * @owner: coded token, set on return
 * 
 * Returns: 1-based row-id in the GenericParam table whose
 * owner is @token. 0 if not found.
 */
guint32
mono_metadata_get_generic_param_row (MonoImage *image, guint32 token, guint32 *owner)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAM];
	guint32 cols [MONO_GENERICPARAM_SIZE];
	guint32 i;

	g_assert (owner);
	if (!tdef->base)
		return 0;

	if (mono_metadata_token_table (token) == MONO_TABLE_TYPEDEF)
		*owner = MONO_TYPEORMETHOD_TYPE;
	else if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
		*owner = MONO_TYPEORMETHOD_METHOD;
	else {
		g_error ("wrong token %x to get_generic_param_row", token);
		return 0;
	}
	*owner |= mono_metadata_token_index (token) << MONO_TYPEORMETHOD_BITS;

	for (i = 0; i < tdef->rows; ++i) {
		mono_metadata_decode_row (tdef, i, cols, MONO_GENERICPARAM_SIZE);
		if (cols [MONO_GENERICPARAM_OWNER] == *owner)
			return i + 1;
	}

	return 0;
}

gboolean
mono_metadata_has_generic_params (MonoImage *image, guint32 token)
{
	guint32 owner;
	return mono_metadata_get_generic_param_row (image, token, &owner);
}

/*
 * mono_metadata_load_generic_param_constraints:
 *
 * Load the generic parameter constraints for the newly created generic type or method
 * represented by @token and @container.  The @container is the new container which has
 * been returned by a call to mono_metadata_load_generic_params() with this @token.
 */
void
mono_metadata_load_generic_param_constraints (MonoImage *image, guint32 token,
					      MonoGenericContainer *container)
{
	guint32 start_row, i, owner;
	if (! (start_row = mono_metadata_get_generic_param_row (image, token, &owner)))
		return;
	for (i = 0; i < container->type_argc; i++)
		get_constraints (image, start_row + i, &container->type_params [i].constraints, container);
}

/*
 * mono_metadata_load_generic_params:
 *
 * Load the type parameters from the type or method definition @token.
 *
 * Use this method after parsing a type or method definition to figure out whether it's a generic
 * type / method.  When parsing a method definition, @parent_container points to the generic container
 * of the current class, if any.
 *
 * Note: This method does not load the constraints: for typedefs, this has to be done after fully
 *       creating the type.
 *
 * Returns: NULL if @token is not a generic type or method definition or the new generic container.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 */
MonoGenericContainer *
mono_metadata_load_generic_params (MonoImage *image, guint32 token, MonoGenericContainer *parent_container)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAM];
	guint32 cols [MONO_GENERICPARAM_SIZE];
	guint32 i, owner = 0, n;
	MonoGenericContainer *container;
	MonoGenericParam *params;
	MonoGenericContext *context;

	if (!(i = mono_metadata_get_generic_param_row (image, token, &owner)))
		return NULL;
	mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	params = NULL;
	n = 0;
	container = mono_mempool_alloc0 (image->mempool, sizeof (MonoGenericContainer));
	do {
		n++;
		params = g_realloc (params, sizeof (MonoGenericParam) * n);
		params [n - 1].owner = container;
		params [n - 1].pklass = NULL;
		params [n - 1].flags = cols [MONO_GENERICPARAM_FLAGS];
		params [n - 1].num = cols [MONO_GENERICPARAM_NUMBER];
		params [n - 1].name = mono_metadata_string_heap (image, cols [MONO_GENERICPARAM_NAME]);
		params [n - 1].constraints = NULL;
		if (++i > tdef->rows)
			break;
		mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	} while (cols [MONO_GENERICPARAM_OWNER] == owner);

	container->type_argc = n;
	container->type_params = mono_mempool_alloc0 (image->mempool, sizeof (MonoGenericParam) * n);
	memcpy (container->type_params, params, sizeof (MonoGenericParam) * n);
	g_free (params);
	container->parent = parent_container;

	if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
		container->is_method = 1;

	g_assert (container->parent == NULL || container->is_method);

	context = &container->context;
	if (container->is_method) {
		context->class_inst = container->parent ? container->parent->context.class_inst : NULL;
		context->method_inst = mono_get_shared_generic_inst (container);
	} else {
		context->class_inst = mono_get_shared_generic_inst (container);
	}

	return container;
}

MonoGenericInst *
mono_get_shared_generic_inst (MonoGenericContainer *container)
{
	MonoType **type_argv;
	MonoType *helper;
	MonoGenericInst *nginst;
	int i;

	type_argv = g_new0 (MonoType *, container->type_argc);
	helper = g_new0 (MonoType, container->type_argc);

	for (i = 0; i < container->type_argc; i++) {
		MonoType *t = &helper [i];

		t->type = container->is_method ? MONO_TYPE_MVAR : MONO_TYPE_VAR;
		t->data.generic_param = &container->type_params [i];

		type_argv [i] = t;
	}

	nginst = mono_metadata_get_generic_inst (container->type_argc, type_argv);

	g_free (type_argv);
	g_free (helper);

	return nginst;
}

gboolean
mono_type_is_byref (MonoType *type)
{
	return type->byref;
}

int
mono_type_get_type (MonoType *type)
{
	return type->type;
}

/* For MONO_TYPE_FNPTR */
MonoMethodSignature*
mono_type_get_signature (MonoType *type)
{
	return type->data.method;
}

/* For MONO_TYPE_CLASS, VALUETYPE */
MonoClass*
mono_type_get_class (MonoType *type)
{
	return type->data.klass;
}

/* For MONO_TYPE_ARRAY */
MonoArrayType*
mono_type_get_array_type (MonoType *type)
{
	return type->data.array;
}

/* For MONO_TYPE_PTR */
MonoType*
mono_type_get_ptr_type (MonoType *type)
{
	return type->data.type;
}

MonoClass*
mono_type_get_modifiers (MonoType *type, gboolean *is_required, gpointer *iter)
{
	/* FIXME: implement */
	return NULL;
}

MonoType*
mono_signature_get_return_type (MonoMethodSignature *sig)
{
	return sig->ret;
}

MonoType*
mono_signature_get_params (MonoMethodSignature *sig, gpointer *iter)
{
	MonoType** type;
	if (!iter)
		return NULL;
	if (!*iter) {
		/* start from the first */
		if (sig->param_count) {
			*iter = &sig->params [0];
			return sig->params [0];
		} else {
			/* no method */
			return NULL;
		}
	}
	type = *iter;
	type++;
	if (type < &sig->params [sig->param_count]) {
		*iter = type;
		return *type;
	}
	return NULL;
}

guint32
mono_signature_get_param_count (MonoMethodSignature *sig)
{
	return sig->param_count;
}

guint32
mono_signature_get_call_conv (MonoMethodSignature *sig)
{
	return sig->call_convention;
}

int
mono_signature_vararg_start (MonoMethodSignature *sig)
{
	return sig->sentinelpos;
}

gboolean
mono_signature_is_instance (MonoMethodSignature *sig)
{
	return sig->hasthis;
}

gboolean
mono_signature_explicit_this (MonoMethodSignature *sig)
{
	return sig->explicit_this;
}

/* for use with allocated memory blocks (assumes alignment is to 8 bytes) */
guint
mono_aligned_addr_hash (gconstpointer ptr)
{
	return GPOINTER_TO_UINT (ptr) >> 3;
}

