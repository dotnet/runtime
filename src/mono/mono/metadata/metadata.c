/*
 * metadata.c: Routines for accessing the metadata
 *
 * Authors:
 *   Miguel de Icaza (miguel@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
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
#include "verify-internals.h"
#include "class.h"
#include "marshal.h"
#include "debug-helpers.h"
#include "abi-details.h"
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/bsearch.h>

/* Auxiliary structure used for caching inflated signatures */
typedef struct {
	MonoMethodSignature *sig;
	MonoGenericContext context;
} MonoInflatedMethodSignature;

static gboolean do_mono_metadata_parse_type (MonoType *type, MonoImage *m, MonoGenericContainer *container, gboolean transient,
					 const char *ptr, const char **rptr);

static gboolean do_mono_metadata_type_equal (MonoType *t1, MonoType *t2, gboolean signature_only);
static gboolean mono_metadata_class_equal (MonoClass *c1, MonoClass *c2, gboolean signature_only);
static gboolean mono_metadata_fnptr_equal (MonoMethodSignature *s1, MonoMethodSignature *s2, gboolean signature_only);
static gboolean _mono_metadata_generic_class_equal (const MonoGenericClass *g1, const MonoGenericClass *g2,
						    gboolean signature_only);
static void free_generic_inst (MonoGenericInst *ginst);
static void free_generic_class (MonoGenericClass *ginst);
static void free_inflated_method (MonoMethodInflated *method);
static void free_inflated_signature (MonoInflatedMethodSignature *sig);
static void mono_metadata_field_info_full (MonoImage *meta, guint32 index, guint32 *offset, guint32 *rva, MonoMarshalSpec **marshal_spec, gboolean alloc_from_image);

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
	MONO_MT_TDOR_IDX,  /* "EventType" }, TypeDef or TypeRef or TypeSpec  */
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

#define DOCUMENT_SCHEMA_OFFSET GEN_CONSTRAINT_SCHEMA_OFFSET + 3
	MONO_MT_BLOB_IDX,   /* Name */
	MONO_MT_GUID_IDX,   /* HashAlgorithm */
	MONO_MT_BLOB_IDX,   /* Hash */
	MONO_MT_GUID_IDX,   /* Language */
	MONO_MT_END,

#define METHODBODY_SCHEMA_OFFSET DOCUMENT_SCHEMA_OFFSET + 5
	MONO_MT_BLOB_IDX,   /* SequencePoints */
	MONO_MT_END,

#define LOCALSCOPE_SCHEMA_OFFSET METHODBODY_SCHEMA_OFFSET + 2
	MONO_MT_TABLE_IDX,   /* Method */
	MONO_MT_TABLE_IDX,   /* ImportScope */
	MONO_MT_TABLE_IDX,   /* VariableList */
	MONO_MT_TABLE_IDX,   /* ConstantList */
	MONO_MT_UINT32,      /* StartOffset */
	MONO_MT_UINT32,      /* Length */
	MONO_MT_END,

#define LOCALVARIABLE_SCHEMA_OFFSET LOCALSCOPE_SCHEMA_OFFSET + 7
	MONO_MT_UINT16,      /* Attributes */
	MONO_MT_UINT16,      /* Index */
	MONO_MT_STRING_IDX,  /* Name */
	MONO_MT_END,

#define NULL_SCHEMA_OFFSET LOCALVARIABLE_SCHEMA_OFFSET + 4
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
	GEN_CONSTRAINT_SCHEMA_OFFSET,
	NULL_SCHEMA_OFFSET,
	NULL_SCHEMA_OFFSET,
	NULL_SCHEMA_OFFSET,
	DOCUMENT_SCHEMA_OFFSET, /* 0x30 */
	METHODBODY_SCHEMA_OFFSET,
	LOCALSCOPE_SCHEMA_OFFSET,
	LOCALVARIABLE_SCHEMA_OFFSET
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

/**
 * mono_meta_table_name:
 * @table: table index
 *
 * Returns the name of the given ECMA metadata logical format table
 * as described in ECMA 335, Partition II, Section 22.
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
#define rtsize(meta,s,b) (((s) < (1 << (b)) ? 2 : 4))
#define idx_size(meta,tableidx) ((meta)->tables [(tableidx)].rows < 65536 ? 2 : 4)

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
				field_size = idx_size (meta, MONO_TABLE_ASSEMBLYREF); break;
			case MONO_TABLE_ASSEMBLYREFPROCESSOR:
				g_assert (i == 1);
				field_size = idx_size (meta, MONO_TABLE_ASSEMBLYREF); break;
			case MONO_TABLE_CLASSLAYOUT:
				g_assert (i == 2);
				field_size = idx_size (meta, MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_EVENTMAP:
				g_assert (i == 0 || i == 1);
				field_size = i ? idx_size (meta, MONO_TABLE_EVENT):
					idx_size (meta, MONO_TABLE_TYPEDEF);
				break;
			case MONO_TABLE_EVENT_POINTER:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_EVENT); break;
			case MONO_TABLE_EXPORTEDTYPE:
				g_assert (i == 1);
				/* the index is in another metadata file, so it must be 4 */
				field_size = 4; break;
			case MONO_TABLE_FIELDLAYOUT:
				g_assert (i == 1);
				field_size = idx_size (meta, MONO_TABLE_FIELD); break;
			case MONO_TABLE_FIELDRVA:
				g_assert (i == 1);
				field_size = idx_size (meta, MONO_TABLE_FIELD); break;
			case MONO_TABLE_FIELD_POINTER:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_FIELD); break;
			case MONO_TABLE_IMPLMAP:
				g_assert (i == 3);
				field_size = idx_size (meta, MONO_TABLE_MODULEREF); break;
			case MONO_TABLE_INTERFACEIMPL:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_METHOD:
				g_assert (i == 5);
				field_size = idx_size (meta, MONO_TABLE_PARAM); break;
			case MONO_TABLE_METHODIMPL:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_METHODSEMANTICS:
				g_assert (i == 1);
				field_size = idx_size (meta, MONO_TABLE_METHOD); break;
			case MONO_TABLE_METHOD_POINTER:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_METHOD); break;
			case MONO_TABLE_NESTEDCLASS:
				g_assert (i == 0 || i == 1);
				field_size = idx_size (meta, MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_PARAM_POINTER:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_PARAM); break;
			case MONO_TABLE_PROPERTYMAP:
				g_assert (i == 0 || i == 1);
				field_size = i ? idx_size (meta, MONO_TABLE_PROPERTY):
					idx_size (meta, MONO_TABLE_TYPEDEF);
				break;
			case MONO_TABLE_PROPERTY_POINTER:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_PROPERTY); break;
			case MONO_TABLE_TYPEDEF:
				g_assert (i == 4 || i == 5);
				field_size = i == 4 ? idx_size (meta, MONO_TABLE_FIELD):
					idx_size (meta, MONO_TABLE_METHOD);
				break;
			case MONO_TABLE_GENERICPARAM:
				g_assert (i == 2);
				n = MAX (meta->tables [MONO_TABLE_METHOD].rows, meta->tables [MONO_TABLE_TYPEDEF].rows);
				/*This is a coded token for 2 tables, so takes 1 bit */
				field_size = rtsize (meta, n, 16 - MONO_TYPEORMETHOD_BITS);
				break;
			case MONO_TABLE_GENERICPARAMCONSTRAINT:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_GENERICPARAM);
				break;
			case MONO_TABLE_LOCALSCOPE:
				switch (i) {
				case 0:
					// FIXME: This table is in another file
					field_size = idx_size (meta, MONO_TABLE_METHOD);
					break;
				case 1:
					field_size = idx_size (meta, MONO_TABLE_IMPORTSCOPE);
					break;
				case 2:
					field_size = idx_size (meta, MONO_TABLE_LOCALVARIABLE);
					break;
				case 3:
					field_size = idx_size (meta, MONO_TABLE_LOCALCONSTANT);
					break;
				default:
					g_assert_not_reached ();
					break;
				}
				break;
			default:
				g_error ("Can't handle MONO_MT_TABLE_IDX for table %d element %d", tableindex, i);
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
			field_size = rtsize (meta, n, 16-2);
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
			field_size = rtsize (meta, n, 16-5);
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
			field_size = rtsize (meta, n, 16-3);
			break;

			/*
			 * HasDeclSecurity: Typedef, MethodDef, Assembly
			 */
		case MONO_MT_HASDEC_IDX:
			n = MAX (meta->tables [MONO_TABLE_TYPEDEF].rows,
				 meta->tables [MONO_TABLE_METHOD].rows);
			n = MAX (n, meta->tables [MONO_TABLE_ASSEMBLY].rows);

			/* 2 bits to encode */
			field_size = rtsize (meta, n, 16-2);
			break;

			/*
			 * Implementation: File, AssemblyRef, ExportedType
			 */
		case MONO_MT_IMPL_IDX:
			n = MAX (meta->tables [MONO_TABLE_FILE].rows,
				 meta->tables [MONO_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [MONO_TABLE_EXPORTEDTYPE].rows);

			/* 2 bits to encode tag */
			field_size = rtsize (meta, n, 16-2);
			break;

			/*
			 * HasFieldMarshall: FieldDef, ParamDef
			 */
		case MONO_MT_HFM_IDX:
			n = MAX (meta->tables [MONO_TABLE_FIELD].rows,
				 meta->tables [MONO_TABLE_PARAM].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
			break;

			/*
			 * MemberForwarded: FieldDef, MethodDef
			 */
		case MONO_MT_MF_IDX:
			n = MAX (meta->tables [MONO_TABLE_FIELD].rows,
				 meta->tables [MONO_TABLE_METHOD].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
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
			field_size = rtsize (meta, n, 16-2);
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

			/* 3 bits to encode */
			field_size = rtsize (meta, n, 16 - 3);
			break;
			
			/*
			 * MethodDefOrRef: MethodDef, MemberRef
			 */
		case MONO_MT_MDOR_IDX:
			n = MAX (meta->tables [MONO_TABLE_METHOD].rows,
				 meta->tables [MONO_TABLE_MEMBERREF].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
			break;
			
			/*
			 * HasSemantics: Property, Event
			 */
		case MONO_MT_HS_IDX:
			n = MAX (meta->tables [MONO_TABLE_PROPERTY].rows,
				 meta->tables [MONO_TABLE_EVENT].rows);

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
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
			field_size = rtsize (meta, n, 16 - 2);
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
	g_return_val_if_fail (idx > 0 && idx <= meta->tables [table].rows, ""); /*FIXME shouldn't we return NULL here?*/
	   
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
	g_assert (index < meta->heap_strings.size);
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
	g_assert (index < meta->heap_us.size);
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
	g_assert (index < meta->heap_blob.size);
	g_return_val_if_fail (index < meta->heap_blob.size, "");/*FIXME shouldn't we return NULL and check for index == 0?*/
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
	g_assert (idx >= 0);
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
 * mono_metadata_parse_array_internal:
 * @m: a metadata context.
 * @transient: whenever to allocate data from the heap
 * @ptr: a pointer to an encoded array description.
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Decodes the compressed array description found in the metadata @m at @ptr.
 *
 * Returns: a #MonoArrayType structure describing the array type
 * and dimensions. Memory is allocated from the heap or from the image mempool, depending
 * on the value of @transient.
 *
 * LOCKING: Acquires the loader lock
 */
static MonoArrayType *
mono_metadata_parse_array_internal (MonoImage *m, MonoGenericContainer *container,
									gboolean transient, const char *ptr, const char **rptr)
{
	int i;
	MonoArrayType *array;
	MonoType *etype;
	
	array = transient ? g_malloc0 (sizeof (MonoArrayType)) : mono_image_alloc0 (m, sizeof (MonoArrayType));
	etype = mono_metadata_parse_type_full (m, container, MONO_PARSE_TYPE, 0, ptr, &ptr);
	if (!etype)
		return NULL;
	array->eklass = mono_class_from_mono_type (etype);
	array->rank = mono_metadata_decode_value (ptr, &ptr);

	array->numsizes = mono_metadata_decode_value (ptr, &ptr);
	if (array->numsizes)
		array->sizes = transient ? g_malloc0 (sizeof (int) * array->numsizes) : mono_image_alloc0 (m, sizeof (int) * array->numsizes);
	for (i = 0; i < array->numsizes; ++i)
		array->sizes [i] = mono_metadata_decode_value (ptr, &ptr);

	array->numlobounds = mono_metadata_decode_value (ptr, &ptr);
	if (array->numlobounds)
		array->lobounds = transient ? g_malloc0 (sizeof (int) * array->numlobounds) : mono_image_alloc0 (m, sizeof (int) * array->numlobounds);
	for (i = 0; i < array->numlobounds; ++i)
		array->lobounds [i] = mono_metadata_decode_signed_value (ptr, &ptr);

	if (rptr)
		*rptr = ptr;
	return array;
}

MonoArrayType *
mono_metadata_parse_array_full (MonoImage *m, MonoGenericContainer *container,
								const char *ptr, const char **rptr)
{
	return mono_metadata_parse_array_internal (m, container, FALSE, ptr, rptr);
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
static int next_generic_inst_id = 0;

/* Protected by image_sets_mutex */
static MonoImageSet *mscorlib_image_set;
/* Protected by image_sets_mutex */
static GPtrArray *image_sets;
static mono_mutex_t image_sets_mutex;

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

guint
mono_metadata_generic_inst_hash (gconstpointer data)
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

#ifndef MONO_SMALL_CONFIG
	if (a->id && b->id) {
		if (a->id == b->id)
			return TRUE;
		if (!signature_only)
			return FALSE;
	}
#endif

	if (a->is_open != b->is_open || a->type_argc != b->type_argc)
		return FALSE;
	for (i = 0; i < a->type_argc; ++i) {
		if (!do_mono_metadata_type_equal (a->type_argv [i], b->type_argv [i], signature_only))
			return FALSE;
	}
	return TRUE;
}

gboolean
mono_metadata_generic_inst_equal (gconstpointer ka, gconstpointer kb)
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

	for (i = 0; i < NBUILTIN_TYPES (); ++i)
		g_hash_table_insert (type_cache, (gpointer) &builtin_types [i], (gpointer) &builtin_types [i]);

	mono_mutex_init_recursive (&image_sets_mutex);
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
	type_cache = NULL;
	g_ptr_array_free (image_sets, TRUE);
	image_sets = NULL;
	mono_mutex_destroy (&image_sets_mutex);
}

/**
 * mono_metadata_parse_type:
 * @m: metadata context
 * @mode: king of type that may be found at @ptr
 * @opt_attrs: optional attributes to store in the returned type
 * @ptr: pointer to the type representation
 * @rptr: pointer updated to match the end of the decoded stream
 * @transient: whenever to allocate the result from the heap or from a mempool
 * 
 * Decode a compressed type description found at @ptr in @m.
 * @mode can be one of MONO_PARSE_MOD_TYPE, MONO_PARSE_PARAM, MONO_PARSE_RET,
 * MONO_PARSE_FIELD, MONO_PARSE_LOCAL, MONO_PARSE_TYPE.
 * This function can be used to decode type descriptions in method signatures,
 * field signatures, locals signatures etc.
 *
 * To parse a generic type, `generic_container' points to the current class'es
 * (the `generic_container' field in the MonoClass) or the current generic method's
 * (stored in image->property_hash) generic container.
 * When we encounter any MONO_TYPE_VAR or MONO_TYPE_MVAR's, they're looked up in
 * this MonoGenericContainer.
 *
 * LOCKING: Acquires the loader lock.
 *
 * Returns: a #MonoType structure representing the decoded type.
 */
static MonoType*
mono_metadata_parse_type_internal (MonoImage *m, MonoGenericContainer *container, MonoParseTypeMode mode,
								   short opt_attrs, gboolean transient, const char *ptr, const char **rptr)
{
	MonoType *type, *cached;
	MonoType stype;
	gboolean byref = FALSE;
	gboolean pinned = FALSE;
	const char *tmp_ptr;
	int count = 0;
	gboolean found;

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
		int size;

		size = MONO_SIZEOF_TYPE + ((gint32)count) * sizeof (MonoCustomMod);
		type = transient ? g_malloc0 (size) : mono_image_alloc0 (m, size);
		type->num_mods = count;
		if (count > 64)
			g_warning ("got more than 64 modifiers in type");
	} else {
		type = &stype;
		memset (type, 0, MONO_SIZEOF_TYPE);
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

	if (!do_mono_metadata_parse_type (type, m, container, transient, ptr, &ptr)) {
		return NULL;
	}

	if (rptr)
		*rptr = ptr;

	if (!type->num_mods && !transient) {
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
				return ret;
			}
		}
		/* No need to use locking since nobody is modifying the hash table */
		if ((cached = g_hash_table_lookup (type_cache, type))) {
			return cached;
		}
	}
	
	/* printf ("%x %x %c %s\n", type->attrs, type->num_mods, type->pinned ? 'p' : ' ', mono_type_full_name (type)); */
	
	if (type == &stype) {
		type = transient ? g_malloc (MONO_SIZEOF_TYPE) : mono_image_alloc (m, MONO_SIZEOF_TYPE);
		memcpy (type, &stype, MONO_SIZEOF_TYPE);
	}
	return type;
}

MonoType*
mono_metadata_parse_type_full (MonoImage *m, MonoGenericContainer *container, MonoParseTypeMode mode,
							   short opt_attrs, const char *ptr, const char **rptr)
{
	return mono_metadata_parse_type_internal (m, container, mode, opt_attrs, FALSE, ptr, rptr);
}

/*
 * LOCKING: Acquires the loader lock.
 */
MonoType*
mono_metadata_parse_type (MonoImage *m, MonoParseTypeMode mode, short opt_attrs,
			  const char *ptr, const char **rptr)
{
	return mono_metadata_parse_type_full (m, NULL, mode, opt_attrs, ptr, rptr);
}

gboolean
mono_metadata_method_has_param_attrs (MonoImage *m, int def)
{
	MonoTableInfo *paramt = &m->tables [MONO_TABLE_PARAM];
	MonoTableInfo *methodt = &m->tables [MONO_TABLE_METHOD];
	guint lastp, i, param_index = mono_metadata_decode_row_col (methodt, def - 1, MONO_METHOD_PARAMLIST);

	if (def < methodt->rows)
		lastp = mono_metadata_decode_row_col (methodt, def, MONO_METHOD_PARAMLIST);
	else
		lastp = m->tables [MONO_TABLE_PARAM].rows + 1;

	for (i = param_index; i < lastp; ++i) {
		guint32 flags = mono_metadata_decode_row_col (paramt, i - 1, MONO_PARAM_FLAGS);
		if (flags)
			return TRUE;
	}

	return FALSE;
}

/*
 * mono_metadata_get_param_attrs:
 *
 * @m The image to loader parameter attributes from
 * @def method def token (one based)
 * @param_count number of params to decode including the return value
 *
 *   Return the parameter attributes for the method whose MethodDef index is DEF. The 
 * returned memory needs to be freed by the caller. If all the param attributes are
 * 0, then NULL is returned.
 */
int*
mono_metadata_get_param_attrs (MonoImage *m, int def, int param_count)
{
	MonoTableInfo *paramt = &m->tables [MONO_TABLE_PARAM];
	MonoTableInfo *methodt = &m->tables [MONO_TABLE_METHOD];
	guint32 cols [MONO_PARAM_SIZE];
	guint lastp, i, param_index = mono_metadata_decode_row_col (methodt, def - 1, MONO_METHOD_PARAMLIST);
	int *pattrs = NULL;

	if (def < methodt->rows)
		lastp = mono_metadata_decode_row_col (methodt, def, MONO_METHOD_PARAMLIST);
	else
		lastp = paramt->rows + 1;

	for (i = param_index; i < lastp; ++i) {
		mono_metadata_decode_row (paramt, i - 1, cols, MONO_PARAM_SIZE);
		if (cols [MONO_PARAM_FLAGS]) {
			if (!pattrs)
				pattrs = g_new0 (int, param_count);
			/* at runtime we just ignore this kind of malformed file:
			* the verifier can signal the error to the user
			*/
			if (cols [MONO_PARAM_SEQUENCE] >= param_count)
				continue;
			pattrs [cols [MONO_PARAM_SEQUENCE]] = cols [MONO_PARAM_FLAGS];
		}
	}

	return pattrs;
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
	MonoError error;
	MonoMethodSignature *ret;
	MonoTableInfo *tables = image->tables;
	guint32 idx = mono_metadata_token_index (token);
	guint32 sig;
	const char *ptr;

	if (image_is_dynamic (image))
		return mono_lookup_dynamic_token (image, token, NULL);

	g_assert (mono_metadata_token_table(token) == MONO_TABLE_STANDALONESIG);
		
	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	mono_metadata_decode_blob_size (ptr, &ptr);

	ret = mono_metadata_parse_method_signature_full (image, NULL, 0, ptr, NULL, &error);
	mono_error_cleanup (&error); /*FIXME don't swallow the error message*/
	return ret;
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

	sig = mono_image_alloc0 (m, MONO_SIZEOF_METHOD_SIGNATURE + ((gint32)nparams) * sizeof (MonoType*));
	sig->param_count = nparams;
	sig->sentinelpos = -1;

	return sig;
}

static MonoMethodSignature*
mono_metadata_signature_dup_internal_with_padding (MonoImage *image, MonoMemPool *mp, MonoMethodSignature *sig, size_t padding)
{
	int sigsize, sig_header_size;
	MonoMethodSignature *ret;
	sigsize = sig_header_size = MONO_SIZEOF_METHOD_SIGNATURE + sig->param_count * sizeof (MonoType *) + padding;
	if (sig->ret)
		sigsize += sizeof (MonoType);

	if (image) {
		ret = mono_image_alloc (image, sigsize);
	} else if (mp) {
		ret = mono_mempool_alloc (mp, sigsize);
	} else {
		ret = g_malloc (sigsize);
	}

	memcpy (ret, sig, sig_header_size - padding);

	// Copy return value because of ownership semantics.
	if (sig->ret) {
		// Danger! Do not alter padding use without changing the dup_add_this below
		intptr_t end_of_header = (intptr_t)( (char*)(ret) + sig_header_size);
		ret->ret = (MonoType *)end_of_header;
		*ret->ret = *sig->ret;
	}

	return ret;
}

static MonoMethodSignature*
mono_metadata_signature_dup_internal (MonoImage *image, MonoMemPool *mp, MonoMethodSignature *sig)
{
	return mono_metadata_signature_dup_internal_with_padding (image, mp, sig, 0);
}
/*
 * signature_dup_add_this:
 *
 *  Make a copy of @sig, adding an explicit this argument.
 */
MonoMethodSignature*
mono_metadata_signature_dup_add_this (MonoImage *image, MonoMethodSignature *sig, MonoClass *klass)
{
	MonoMethodSignature *ret;
	ret = mono_metadata_signature_dup_internal_with_padding (image, NULL, sig, sizeof (MonoType *));

	ret->param_count = sig->param_count + 1;
	ret->hasthis = FALSE;

	for (int i = sig->param_count - 1; i >= 0; i --)
		ret->params [i + 1] = sig->params [i];
	ret->params [0] = klass->valuetype ? &klass->this_arg : &klass->byval_arg;

	for (int i = sig->param_count - 1; i >= 0; i --)
		g_assert(ret->params [i + 1]->type == sig->params [i]->type && ret->params [i+1]->type != MONO_TYPE_END);
	g_assert (ret->ret->type == sig->ret->type && ret->ret->type != MONO_TYPE_END);

	return ret;
}



MonoMethodSignature*
mono_metadata_signature_dup_full (MonoImage *image, MonoMethodSignature *sig)
{
	MonoMethodSignature *ret = mono_metadata_signature_dup_internal (image, NULL, sig);

	for (int i = 0 ; i < sig->param_count; i ++)
		g_assert(ret->params [i]->type == sig->params [i]->type);
	g_assert (ret->ret->type == sig->ret->type);

	return ret;
}

/*The mempool is accessed without synchronization*/
MonoMethodSignature*
mono_metadata_signature_dup_mempool (MonoMemPool *mp, MonoMethodSignature *sig)
{
	return mono_metadata_signature_dup_internal (NULL, mp, sig);
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
 * mono_metadata_signature_size:
 *
 *   Return the amount of memory allocated to SIG.
 */
guint32
mono_metadata_signature_size (MonoMethodSignature *sig)
{
	return MONO_SIZEOF_METHOD_SIGNATURE + sig->param_count * sizeof (MonoType *);
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
					   int def, const char *ptr, const char **rptr, MonoError *error)
{
	MonoMethodSignature *method;
	int i, *pattrs = NULL;
	guint32 hasthis = 0, explicit_this = 0, call_convention, param_count;
	guint32 gen_param_count = 0;
	gboolean is_open = FALSE;

	mono_error_init (error);

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
		pattrs = mono_metadata_get_param_attrs (m, def, param_count + 1); /*Must be + 1 since signature's param count doesn't account for the return value */

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
			if (mono_loader_get_last_error ())
				mono_error_set_from_loader_error (error);
			else
				mono_error_set_bad_image (error, m, "Could not parse return type signature");
			return NULL;
		}
		is_open = mono_class_is_open_constructed_type (method->ret);
	}

	for (i = 0; i < method->param_count; ++i) {
		if (*ptr == MONO_TYPE_SENTINEL) {
			if (method->call_convention != MONO_CALL_VARARG || def) {
				mono_loader_assert_no_error ();
				mono_error_set_bad_image (error, m, "Found sentinel for methoddef or no vararg");
				g_free (pattrs);
				return NULL;
			}
			if (method->sentinelpos >= 0) {
				mono_loader_assert_no_error ();
				mono_error_set_bad_image (error, m, "Found sentinel twice in the same signature.");
				g_free (pattrs);
				return NULL;
			}
			method->sentinelpos = i;
			ptr++;
		}
		method->params [i] = mono_metadata_parse_type_full (m, container, MONO_PARSE_PARAM, pattrs ? pattrs [i+1] : 0, ptr, &ptr);
		if (!method->params [i]) {
			if (mono_loader_get_last_error ())
				mono_error_set_from_loader_error (error);
			else
				mono_error_set_bad_image (error, m, "Could not parse type argument %d on method signature", i);
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

	mono_loader_assert_no_error ();
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
	MonoError error;
	MonoMethodSignature *ret;
	ret = mono_metadata_parse_method_signature_full (m, NULL, def, ptr, rptr, &error);
	if (!ret) {
		mono_loader_set_error_from_mono_error (&error);
		mono_error_cleanup (&error); /*FIXME don't swallow the error message*/
	}
	return ret;
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
	if (ma->method.method.is_mb_open != mb->method.method.is_mb_open)
		return FALSE;
	return mono_metadata_generic_context_equal (&ma->context, &mb->context);
}

static guint
inflated_method_hash (gconstpointer a)
{
	const MonoMethodInflated *ma = a;
	return (mono_metadata_generic_context_hash (&ma->context) ^ mono_aligned_addr_hash (ma->declaring)) + ma->method.method.is_mb_open;
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
	case MONO_TYPE_VAR: {
		MonoGenericContainer *container = mono_type_get_generic_param_owner (type);
		if (container) {
			g_assert (!container->is_method);
			/*
			 * FIXME: The following check is here solely
			 * for monodis, which uses the internal
			 * function
			 * mono_metadata_load_generic_params().  The
			 * caller of that function needs to fill in
			 * owner->klass or owner->method of the
			 * returned struct, but monodis doesn't do
			 * that.  The image unloading depends on that,
			 * however, so a crash results without this
			 * check.
			 */
			if (!container->owner.klass)
				return container->image == image;
			return container->owner.klass->image == image;
		} else {
			return type->data.generic_param->image == image;
		}
	}
	case MONO_TYPE_MVAR: {
		MonoGenericContainer *container = mono_type_get_generic_param_owner (type);
		if (type->data.generic_param->image == image)
			return TRUE;
		if (container) {
			g_assert (container->is_method);
			if (!container->owner.method)
				/* RefEmit created generic param whose method is not finished */
				return container->image == image;
			return container->owner.method->klass->image == image;
		} else {
			return type->data.generic_param->image == image;
		}
	}
	default:
		/* At this point, we should've avoided all potential allocations in mono_class_from_mono_type () */
		return image == mono_class_from_mono_type (type)->image;
	}
}

static inline void
image_sets_lock (void)
{
	mono_mutex_lock (&image_sets_mutex);
}

static inline void
image_sets_unlock (void)
{
	mono_mutex_unlock (&image_sets_mutex);
}

/*
 * get_image_set:
 *
 *   Return a MonoImageSet representing the set of images in IMAGES.
 */
static MonoImageSet*
get_image_set (MonoImage **images, int nimages)
{
	int i, j, k;
	MonoImageSet *set;
	GSList *l;

	/* Common case */
	if (nimages == 1 && images [0] == mono_defaults.corlib && mscorlib_image_set)
		return mscorlib_image_set;

	/* Happens with empty generic instances */
	if (nimages == 0)
		return mscorlib_image_set;

	image_sets_lock ();

	if (!image_sets)
		image_sets = g_ptr_array_new ();

	if (images [0] == mono_defaults.corlib && nimages > 1)
		l = images [1]->image_sets;
	else
		l = images [0]->image_sets;

	set = NULL;
	for (; l; l = l->next) {
		set = l->data;

		if (set->nimages == nimages) {
			for (j = 0; j < nimages; ++j) {
				for (k = 0; k < nimages; ++k)
					if (set->images [k] == images [j])
						break;
				if (k == nimages)
					/* Not found */
					break;
			}
			if (j == nimages)
				/* Found */
				break;
		}
	}

	if (!l) {
		/* Not found */
		set = g_new0 (MonoImageSet, 1);
		set->nimages = nimages;
		set->images = g_new0 (MonoImage*, nimages);
		mono_mutex_init_recursive (&set->lock);
		for (i = 0; i < nimages; ++i)
			set->images [i] = images [i];
		set->gclass_cache = g_hash_table_new_full (mono_generic_class_hash, mono_generic_class_equal, NULL, (GDestroyNotify)free_generic_class);
		set->ginst_cache = g_hash_table_new_full (mono_metadata_generic_inst_hash, mono_metadata_generic_inst_equal, NULL, (GDestroyNotify)free_generic_inst);
		set->gmethod_cache = g_hash_table_new_full (inflated_method_hash, inflated_method_equal, NULL, (GDestroyNotify)free_inflated_method);
		set->gsignature_cache = g_hash_table_new_full (inflated_signature_hash, inflated_signature_equal, NULL, (GDestroyNotify)free_inflated_signature);
	
		for (i = 0; i < nimages; ++i)
			set->images [i]->image_sets = g_slist_prepend (set->images [i]->image_sets, set);

		g_ptr_array_add (image_sets, set);
	}

	if (nimages == 1 && images [0] == mono_defaults.corlib) {
		mono_memory_barrier ();
		mscorlib_image_set = set;
	}

	image_sets_unlock ();

	return set;
}

static void
delete_image_set (MonoImageSet *set)
{
	int i;

	g_hash_table_destroy (set->gclass_cache);
	g_hash_table_destroy (set->ginst_cache);
	g_hash_table_destroy (set->gmethod_cache);
	g_hash_table_destroy (set->gsignature_cache);

	image_sets_lock ();

	for (i = 0; i < set->nimages; ++i)
		set->images [i]->image_sets = g_slist_remove (set->images [i]->image_sets, set);

	g_ptr_array_remove (image_sets, set);

	image_sets_unlock ();

	if (set->mempool)
		mono_mempool_destroy (set->mempool);
	g_free (set->images);
	mono_mutex_destroy (&set->lock);
	g_free (set);
}

static void
mono_image_set_lock (MonoImageSet *set)
{
	mono_mutex_lock (&set->lock);
}

static void
mono_image_set_unlock (MonoImageSet *set)
{
	mono_mutex_unlock (&set->lock);
}

gpointer
mono_image_set_alloc (MonoImageSet *set, guint size)
{
	gpointer res;

	mono_image_set_lock (set);
	if (!set->mempool)
		set->mempool = mono_mempool_new_size (1024);
	res = mono_mempool_alloc (set->mempool, size);
	mono_image_set_unlock (set);

	return res;
}

gpointer
mono_image_set_alloc0 (MonoImageSet *set, guint size)
{
	gpointer res;

	mono_image_set_lock (set);
	if (!set->mempool)
		set->mempool = mono_mempool_new_size (1024);
	res = mono_mempool_alloc0 (set->mempool, size);
	mono_image_set_unlock (set);

	return res;
}

char*
mono_image_set_strdup (MonoImageSet *set, const char *s)
{
	char *res;

	mono_image_set_lock (set);
	if (!set->mempool)
		set->mempool = mono_mempool_new_size (1024);
	res = mono_mempool_strdup (set->mempool, s);
	mono_image_set_unlock (set);

	return res;
}

/* 
 * Structure used by the collect_..._images functions to store the image list.
 */
typedef struct {
	MonoImage *image_buf [64];
	MonoImage **images;
	int nimages, images_len;
} CollectData;

static void
collect_data_init (CollectData *data)
{
	data->images = data->image_buf;
	data->images_len = 64;
	data->nimages = 0;
}

static void
collect_data_free (CollectData *data)
{
	if (data->images != data->image_buf)
		g_free (data->images);
}

static void
enlarge_data (CollectData *data)
{
	int new_len = data->images_len < 16 ? 16 : data->images_len * 2;
	MonoImage **d = g_new (MonoImage *, new_len);

	// FIXME: test this
	g_assert_not_reached ();
	memcpy (d, data->images, data->images_len);
	if (data->images != data->image_buf)
		g_free (data->images);
	data->images = d;
	data->images_len = new_len;
}

static inline void
add_image (MonoImage *image, CollectData *data)
{
	int i;

	/* The arrays are small, so use a linear search instead of a hash table */
	for (i = 0; i < data->nimages; ++i)
		if (data->images [i] == image)
			return;

	if (data->nimages == data->images_len)
		enlarge_data (data);

	data->images [data->nimages ++] = image;
}

static void
collect_type_images (MonoType *type, CollectData *data);

static void
collect_ginst_images (MonoGenericInst *ginst, CollectData *data)
{
	int i;

	for (i = 0; i < ginst->type_argc; ++i) {
		collect_type_images (ginst->type_argv [i], data);
	}
}

static void
collect_gclass_images (MonoGenericClass *gclass, CollectData *data)
{
	add_image (gclass->container_class->image, data);
	if (gclass->context.class_inst)
		collect_ginst_images (gclass->context.class_inst, data);
}

static void
collect_signature_images (MonoMethodSignature *sig, CollectData *data)
{
	gpointer iter = NULL;
	MonoType *p;

	collect_type_images (mono_signature_get_return_type (sig), data);
	while ((p = mono_signature_get_params (sig, &iter)) != NULL)
		collect_type_images (p, data);
}

static void
collect_inflated_signature_images (MonoInflatedMethodSignature *sig, CollectData *data)
{
	collect_signature_images (sig->sig, data);
	if (sig->context.class_inst)
		collect_ginst_images (sig->context.class_inst, data);
	if (sig->context.method_inst)
		collect_ginst_images (sig->context.method_inst, data);
}

static void
collect_method_images (MonoMethodInflated *method, CollectData *data)
{
	MonoMethod *m = method->declaring;

	add_image (method->declaring->klass->image, data);
	if (method->context.class_inst)
		collect_ginst_images (method->context.class_inst, data);
	if (method->context.method_inst)
		collect_ginst_images (method->context.method_inst, data);
	/*
	 * Dynamic assemblies have no references, so the images they depend on can be unloaded before them.
	 */
	if (image_is_dynamic (m->klass->image))
		collect_signature_images (mono_method_signature (m), data);
}

static void
collect_type_images (MonoType *type, CollectData *data)
{
retry:
	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		collect_gclass_images (type->data.generic_class, data);
		break;
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
		//return signature_in_image (type->data.method, image);
		g_assert_not_reached ();
	case MONO_TYPE_VAR: {
		MonoGenericContainer *container = mono_type_get_generic_param_owner (type);
		if (container) {
			g_assert (!container->is_method);
			/*
			 * FIXME: The following check is here solely
			 * for monodis, which uses the internal
			 * function
			 * mono_metadata_load_generic_params().  The
			 * caller of that function needs to fill in
			 * owner->klass or owner->method of the
			 * returned struct, but monodis doesn't do
			 * that.  The image unloading depends on that,
			 * however, so a crash results without this
			 * check.
			 */
			if (!container->owner.klass)
				add_image (container->image, data);
			else
				add_image (container->owner.klass->image, data);
		} else {
			add_image (type->data.generic_param->image, data);
		}
	}
		break;
	case MONO_TYPE_MVAR: {
		MonoGenericContainer *container = mono_type_get_generic_param_owner (type);
		if (type->data.generic_param->image)
			add_image (type->data.generic_param->image, data);
		if (container) {
			if (!container->owner.method) {
				/* RefEmit created generic param whose method is not finished */
				add_image (container->image, data);
			} else {
				g_assert (container->is_method);
				add_image (container->owner.method->klass->image, data);
			}
		} else {
			add_image (type->data.generic_param->image, data);
		}
	}
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		add_image (mono_class_from_mono_type (type)->image, data);
		break;
	default:
		add_image (mono_defaults.corlib, data);
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

	g_assert (gclass_in_image (gclass, user_data->image));

	user_data->list = g_slist_prepend (user_data->list, gclass);
	return TRUE;
}

static gboolean
steal_ginst_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoGenericInst *ginst = key;
	CleanForImageUserData *user_data = data;

	// This doesn't work during corlib compilation
	//g_assert (ginst_in_image (ginst, user_data->image));

	user_data->list = g_slist_prepend (user_data->list, ginst);
	return TRUE;
}

static gboolean
inflated_method_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = data;
	MonoMethodInflated *method = key;

	// FIXME:
	// https://bugzilla.novell.com/show_bug.cgi?id=458168
	g_assert (method->declaring->klass->image == image ||
		(method->context.class_inst && ginst_in_image (method->context.class_inst, image)) ||
			  (method->context.method_inst && ginst_in_image (method->context.method_inst, image)) || (((MonoMethod*)method)->signature && signature_in_image (mono_method_signature ((MonoMethod*)method), image)));

	return TRUE;
}

static gboolean
inflated_signature_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = data;
	MonoInflatedMethodSignature *sig = key;

	return signature_in_image (sig->sig, image) ||
		(sig->context.class_inst && ginst_in_image (sig->context.class_inst, image)) ||
		(sig->context.method_inst && ginst_in_image (sig->context.method_inst, image));
}	

static void
check_gmethod (gpointer key, gpointer value, gpointer data)
{
	MonoMethodInflated *method = key;
	MonoImage *image = data;

	if (method->context.class_inst)
		g_assert (!ginst_in_image (method->context.class_inst, image));
	if (method->context.method_inst)
		g_assert (!ginst_in_image (method->context.method_inst, image));
	if (((MonoMethod*)method)->signature)
		g_assert (!signature_in_image (mono_method_signature ((MonoMethod*)method), image));
}

/*
 * check_image_sets:
 *
 *   Run a consistency check on the image set data structures.
 */
static G_GNUC_UNUSED void
check_image_sets (MonoImage *image)
{
	int i;
	GSList *l = image->image_sets;

	if (!image_sets)
		return;

	for (i = 0; i < image_sets->len; ++i) {
		MonoImageSet *set = g_ptr_array_index (image_sets, i);

		if (!g_slist_find (l, set)) {
			g_hash_table_foreach (set->gmethod_cache, check_gmethod, image);
		}
	}
}

void
mono_metadata_clean_for_image (MonoImage *image)
{
	CleanForImageUserData ginst_data, gclass_data;
	GSList *l, *set_list;

	//check_image_sets (image);

	/*
	 * The data structures could reference each other so we delete them in two phases.
	 * This is required because of the hashing functions in gclass/ginst_cache.
	 */
	ginst_data.image = gclass_data.image = image;
	ginst_data.list = gclass_data.list = NULL;

	/* Collect the items to delete */
	/* delete_image_set () modifies the lists so make a copy */
	for (l = image->image_sets; l; l = l->next) {
		MonoImageSet *set = l->data;

		mono_image_set_lock (set);
		g_hash_table_foreach_steal (set->gclass_cache, steal_gclass_in_image, &gclass_data);
		g_hash_table_foreach_steal (set->ginst_cache, steal_ginst_in_image, &ginst_data);
		g_hash_table_foreach_remove (set->gmethod_cache, inflated_method_in_image, image);
		g_hash_table_foreach_remove (set->gsignature_cache, inflated_signature_in_image, image);
		mono_image_set_unlock (set);
	}

	/* Delete the removed items */
	for (l = ginst_data.list; l; l = l->next)
		free_generic_inst (l->data);
	for (l = gclass_data.list; l; l = l->next)
		free_generic_class (l->data);
	g_slist_free (ginst_data.list);
	g_slist_free (gclass_data.list);
	/* delete_image_set () modifies the lists so make a copy */
	set_list = g_slist_copy (image->image_sets);
	for (l = set_list; l; l = l->next) {
		MonoImageSet *set = l->data;

		delete_image_set (set);
	}
	g_slist_free (set_list);
}

static void
free_inflated_method (MonoMethodInflated *imethod)
{
	int i;
	MonoMethod *method = (MonoMethod*)imethod;

	mono_marshal_free_inflated_wrappers (method);

	if (method->signature)
		mono_metadata_free_inflated_signature (method->signature);

	if (!((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))) {
		MonoMethodHeader *header = imethod->header;

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

	/* The ginst itself is allocated from the image set mempool */
	for (i = 0; i < ginst->type_argc; ++i)
		mono_metadata_free_type (ginst->type_argv [i]);
}

static void
free_generic_class (MonoGenericClass *gclass)
{
	/* The gclass itself is allocated from the image set mempool */
	if (gclass->is_dynamic)
		mono_reflection_free_dynamic_generic_class (gclass);
	if (gclass->cached_class && gclass->cached_class->interface_id)
		mono_unload_interface_id (gclass->cached_class);
}

static void
free_inflated_signature (MonoInflatedMethodSignature *sig)
{
	mono_metadata_free_inflated_signature (sig->sig);
	g_free (sig);
}

MonoMethodInflated*
mono_method_inflated_lookup (MonoMethodInflated* method, gboolean cache)
{
	CollectData data;
	MonoImageSet *set;
	gpointer res;

	collect_data_init (&data);

	collect_method_images (method, &data);

	set = get_image_set (data.images, data.nimages);

	collect_data_free (&data);

	mono_image_set_lock (set);
	res = g_hash_table_lookup (set->gmethod_cache, method);
	if (!res && cache) {
		g_hash_table_insert (set->gmethod_cache, method, method);
		res = method;
	}

	mono_image_set_unlock (set);
	return res;
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
	CollectData data;
	MonoImageSet *set;

	helper.sig = sig;
	helper.context.class_inst = context->class_inst;
	helper.context.method_inst = context->method_inst;

	collect_data_init (&data);

	collect_inflated_signature_images (&helper, &data);

	set = get_image_set (data.images, data.nimages);

	collect_data_free (&data);

	mono_image_set_lock (set);

	res = g_hash_table_lookup (set->gsignature_cache, &helper);
	if (!res) {
		res = g_new0 (MonoInflatedMethodSignature, 1);
		res->sig = sig;
		res->context.class_inst = context->class_inst;
		res->context.method_inst = context->method_inst;
		g_hash_table_insert (set->gsignature_cache, res, res);
	}

	mono_image_set_unlock (set);

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
	gboolean is_open;
	int i;
	int size = MONO_SIZEOF_GENERIC_INST + type_argc * sizeof (MonoType *);
	CollectData data;
	MonoImageSet *set;

	for (i = 0; i < type_argc; ++i)
		if (mono_class_is_open_constructed_type (type_argv [i]))
			break;
	is_open = (i < type_argc);

	ginst = g_alloca (size);
	memset (ginst, 0, sizeof (MonoGenericInst));
	ginst->is_open = is_open;
	ginst->type_argc = type_argc;
	memcpy (ginst->type_argv, type_argv, type_argc * sizeof (MonoType *));

	collect_data_init (&data);

	collect_ginst_images (ginst, &data);

	set = get_image_set (data.images, data.nimages);

	collect_data_free (&data);

	mono_image_set_lock (set);

	ginst = g_hash_table_lookup (set->ginst_cache, ginst);
	if (!ginst) {
		ginst = mono_image_set_alloc0 (set, size);
#ifndef MONO_SMALL_CONFIG
		ginst->id = ++next_generic_inst_id;
#endif
		ginst->is_open = is_open;
		ginst->type_argc = type_argc;

		for (i = 0; i < type_argc; ++i)
			ginst->type_argv [i] = mono_metadata_type_dup (NULL, type_argv [i]);

		g_hash_table_insert (set->ginst_cache, ginst, ginst);
	}

	mono_image_set_unlock (set);
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
	MonoImageSet *set;
	CollectData data;

	helper.container_class = container_class;
	helper.context.class_inst = inst;
	helper.context.method_inst = NULL;
	helper.is_dynamic = is_dynamic; /* We use this in a hash lookup, which does not attempt to downcast the pointer */
	helper.is_tb_open = is_tb_open;
	helper.cached_class = NULL;

	collect_data_init (&data);

	collect_gclass_images (&helper, &data);

	set = get_image_set (data.images, data.nimages);

	collect_data_free (&data);

	mono_image_set_lock (set);

	gclass = g_hash_table_lookup (set->gclass_cache, &helper);

	/* A tripwire just to keep us honest */
	g_assert (!helper.cached_class);

	if (gclass) {
		mono_image_set_unlock (set);
		return gclass;
	}

	if (is_dynamic) {
		MonoDynamicGenericClass *dgclass = mono_image_set_new0 (set, MonoDynamicGenericClass, 1);
		gclass = &dgclass->generic_class;
		gclass->is_dynamic = 1;
	} else {
		gclass = mono_image_set_new0 (set, MonoGenericClass, 1);
	}

	gclass->is_tb_open = is_tb_open;
	gclass->container_class = container_class;
	gclass->context.class_inst = inst;
	gclass->context.method_inst = NULL;
	gclass->owner = set;
	if (inst == container_class->generic_container->context.class_inst && !is_tb_open)
		gclass->cached_class = container_class;

	g_hash_table_insert (set->gclass_cache, gclass, gclass);

	mono_image_set_unlock (set);

	return gclass;
}

/*
 * mono_metadata_inflate_generic_inst:
 *
 * Instantiate the generic instance @ginst with the context @context.
 * Check @error for success.
 *
 */
MonoGenericInst *
mono_metadata_inflate_generic_inst (MonoGenericInst *ginst, MonoGenericContext *context, MonoError *error)
{
	MonoType **type_argv;
	MonoGenericInst *nginst = NULL;
	int i, count = 0;

	mono_error_init (error);

	if (!ginst->is_open)
		return ginst;

	type_argv = g_new0 (MonoType*, ginst->type_argc);

	for (i = 0; i < ginst->type_argc; i++) {
		type_argv [i] = mono_class_inflate_generic_type_checked (ginst->type_argv [i], context, error);
		if (!mono_error_ok (error))
			goto cleanup;
		++count;
	}

	nginst = mono_metadata_get_generic_inst (ginst->type_argc, type_argv);

cleanup:
	for (i = 0; i < count; i++)
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
 * @generic_container: Our MonoClass's or MonoMethod's MonoGenericContainer;
 *                     see mono_metadata_parse_type_full() for details.
 * Internal routine to parse a generic type parameter.
 * LOCKING: Acquires the loader lock
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
		MonoGenericParam *param;

		param = mono_image_alloc0 (m, sizeof (MonoGenericParam));
		param->num = index;
		param->image = m;

		return param;
	}

	if (index >= generic_container->type_argc)
		return NULL;

	return mono_generic_container_get_param (generic_container, index);
}

/*
 * mono_metadata_get_shared_type:
 *
 *   Return a shared instance of TYPE, if available, NULL otherwise.
 * Shared MonoType instances help save memory. Their contents should not be modified
 * by the caller. They do not need to be freed as their lifetime is bound by either
 * the lifetime of the runtime (builtin types), or the lifetime of the MonoClass
 * instance they are embedded in. If they are freed, they should be freed using
 * mono_metadata_free_type () instead of g_free ().
 */
MonoType*
mono_metadata_get_shared_type (MonoType *type)
{
	MonoType *cached;

	/* No need to use locking since nobody is modifying the hash table */
	if ((cached = g_hash_table_lookup (type_cache, type)))
		return cached;

	switch (type->type){
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		if (type == &type->data.klass->byval_arg)
			return type;
		if (type == &type->data.klass->this_arg)
			return type;
		break;
	default:
		break;
	}

	return NULL;
}

static gboolean
compare_type_literals (int class_type, int type_type)
{
	/* byval_arg.type can be zero if we're decoding a type that references a class been loading.
	 * See mcs/test/gtest-440. and #650936.
	 * FIXME This better be moved to the metadata verifier as it can catch more cases.
	 */
	if (!class_type)
		return TRUE;
	/* NET 1.1 assemblies might encode string and object in a denormalized way.
	 * See #675464.
	 */
	if (class_type == type_type)
		return TRUE;

	if (type_type == MONO_TYPE_CLASS)
		return class_type == MONO_TYPE_STRING || class_type == MONO_TYPE_OBJECT;

	g_assert (type_type == MONO_TYPE_VALUETYPE);
	switch (class_type) {
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
	case MONO_TYPE_CLASS:
		return TRUE;
	default:
		return FALSE;
	}
}

/* 
 * do_mono_metadata_parse_type:
 * @type: MonoType to be filled in with the return value
 * @m: image context
 * @generic_context: generics_context
 * @transient: whenever to allocate data from the heap
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
							 gboolean transient, const char *ptr, const char **rptr)
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
		MonoClass *class;
		MonoError error;
		token = mono_metadata_parse_typedef_or_ref (m, ptr, &ptr);
		class = mono_class_get_checked (m, token, &error);
		type->data.klass = class;
		if (!class) {
			mono_loader_set_error_from_mono_error (&error);
			mono_error_cleanup (&error); /*FIXME don't swallow the error message*/
			return FALSE;
		}
		if (!compare_type_literals (class->byval_arg.type, type->type))
			return FALSE;
		break;
	}
	case MONO_TYPE_SZARRAY: {
		MonoType *etype = mono_metadata_parse_type_full (m, container, MONO_PARSE_MOD_TYPE, 0, ptr, &ptr);
		if (!etype)
			return FALSE;
		type->data.klass = mono_class_from_mono_type (etype);
		if (!type->data.klass)
			return FALSE;
		break;
	}
	case MONO_TYPE_PTR:
		type->data.type = mono_metadata_parse_type_internal (m, container, MONO_PARSE_MOD_TYPE, 0, transient, ptr, &ptr);
		if (!type->data.type)
			return FALSE;
		break;
	case MONO_TYPE_FNPTR: {
		MonoError error;
		type->data.method = mono_metadata_parse_method_signature_full (m, container, 0, ptr, &ptr, &error);
		if (!type->data.method) {
			mono_loader_set_error_from_mono_error (&error);
			mono_error_cleanup (&error); /*FIXME don't swallow the error message*/
			return FALSE;
		}
		break;
	}
	case MONO_TYPE_ARRAY:
		type->data.array = mono_metadata_parse_array_internal (m, container, transient, ptr, &ptr);
		if (!type->data.array)
			return FALSE;
		break;
	case MONO_TYPE_MVAR:
		if (container && !container->is_method)
			return FALSE;
	case MONO_TYPE_VAR:
		type->data.generic_param = mono_metadata_parse_generic_param (m, container, type->type, ptr, &ptr);
		if (!type->data.generic_param)
			return FALSE;
		break;
	case MONO_TYPE_GENERICINST:
		ok = do_mono_metadata_parse_generic_class (type, m, container, ptr, &ptr);
		break;
	default:
		g_warning ("type 0x%02x not handled in do_mono_metadata_parse_type on image %s", type->type, m->name);
		return FALSE;
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
	default:
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
 * @ptr: Points to the beginning of the Section Data (25.3)
 */
static MonoExceptionClause*
parse_section_data (MonoImage *m, int *num_clauses, const unsigned char *ptr)
{
	unsigned char sect_data_flags;
	int is_fat;
	guint32 sect_data_len;
	MonoExceptionClause* clauses = NULL;
	
	while (1) {
		/* align on 32-bit boundary */
		ptr = dword_align (ptr); 
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

		if (sect_data_flags & METHOD_HEADER_SECTION_EHTABLE) {
			const unsigned char *p = dword_align (ptr);
			int i;
			*num_clauses = is_fat ? sect_data_len / 24: sect_data_len / 12;
			/* we could just store a pointer if we don't need to byteswap */
			clauses = g_malloc0 (sizeof (MonoExceptionClause) * (*num_clauses));
			for (i = 0; i < *num_clauses; ++i) {
				MonoExceptionClause *ec = &clauses [i];
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
					ec->data.catch_class = NULL;
					if (tof_value) {
						MonoError error;
						ec->data.catch_class = mono_class_get_checked (m, tof_value, &error);
						if (!mono_error_ok (&error)) {
							mono_error_cleanup (&error); /* FIXME don't swallow the error */
							g_free (clauses);
							return NULL;
						}
					}
				} else {
					ec->data.catch_class = NULL;
				}
				/* g_print ("try %d: %x %04x-%04x %04x\n", i, ec->flags, ec->try_offset, ec->try_offset+ec->try_len, ec->try_len); */
			}

		}
		if (sect_data_flags & METHOD_HEADER_SECTION_MORE_SECTS)
			ptr += sect_data_len - 4; /* LAMESPEC: it seems the size includes the header */
		else
			return clauses;
	}
}

/*
 * mono_method_get_header_summary:
 * @method: The method to get the header.
 * @summary: Where to store the header
 *
 *
 * Returns: true if the header was properly decoded.
 */
gboolean
mono_method_get_header_summary (MonoMethod *method, MonoMethodHeaderSummary *summary)
{
	int idx;
	guint32 rva;
	MonoImage* img;
	const char *ptr;
	unsigned char flags, format;
	guint16 fat_flags;

	/*Only the GMD has a pointer to the metadata.*/
	while (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;

	summary->code_size = 0;
	summary->has_clauses = FALSE;

	/*FIXME extract this into a MACRO and share it with mono_method_get_header*/
	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return FALSE;

	if (method->wrapper_type != MONO_WRAPPER_NONE || method->sre_method) {
		MonoMethodHeader *header =  ((MonoMethodWrapper *)method)->header;
		if (!header)
			return FALSE;
		summary->code_size = header->code_size;
		summary->has_clauses = header->num_clauses > 0;
		return TRUE;
	}


	idx = mono_metadata_token_index (method->token);
	img = method->klass->image;
	rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);

	/*We must run the verifier since we'll be decoding it.*/
	if (!mono_verifier_verify_method_header (img, rva, NULL))
		return FALSE;

	ptr = mono_image_rva_map (img, rva);
	g_assert (ptr);

	flags = *(const unsigned char *)ptr;
	format = flags & METHOD_HEADER_FORMAT_MASK;

	switch (format) {
	case METHOD_HEADER_TINY_FORMAT:
		ptr++;
		summary->code_size = flags >> 2;
		break;
	case METHOD_HEADER_FAT_FORMAT:
		fat_flags = read16 (ptr);
		ptr += 4;
		summary->code_size = read32 (ptr);
		if (fat_flags & METHOD_HEADER_MORE_SECTS)
			summary->has_clauses = TRUE;
		break;
	default:
		return FALSE;
	}
	return TRUE;
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
 * LOCKING: Acquires the loader lock.
 *
 * Returns: a transient MonoMethodHeader allocated from the heap.
 */
MonoMethodHeader *
mono_metadata_parse_mh_full (MonoImage *m, MonoGenericContainer *container, const char *ptr)
{
	MonoMethodHeader *mh = NULL;
	unsigned char flags = *(const unsigned char *) ptr;
	unsigned char format = flags & METHOD_HEADER_FORMAT_MASK;
	guint16 fat_flags;
	guint32 local_var_sig_tok, max_stack, code_size, init_locals;
	const unsigned char *code;
	MonoExceptionClause* clauses = NULL;
	int num_clauses = 0;
	MonoTableInfo *t = &m->tables [MONO_TABLE_STANDALONESIG];
	guint32 cols [MONO_STAND_ALONE_SIGNATURE_SIZE];

	g_return_val_if_fail (ptr != NULL, NULL);

	switch (format) {
	case METHOD_HEADER_TINY_FORMAT:
		mh = g_malloc0 (MONO_SIZEOF_METHOD_HEADER);
		ptr++;
		mh->max_stack = 8;
		mh->is_transient = TRUE;
		local_var_sig_tok = 0;
		mh->code_size = flags >> 2;
		mh->code = (unsigned char*)ptr;
		return mh;
	case METHOD_HEADER_FAT_FORMAT:
		fat_flags = read16 (ptr);
		ptr += 2;
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
		return NULL;
	}

	if (local_var_sig_tok) {
		int idx = (local_var_sig_tok & 0xffffff)-1;
		if (idx >= t->rows || idx < 0)
			goto fail;
		mono_metadata_decode_row (t, idx, cols, 1);

		if (!mono_verifier_verify_standalone_signature (m, cols [MONO_STAND_ALONE_SIGNATURE], NULL))
			goto fail;
	}
	if (fat_flags & METHOD_HEADER_MORE_SECTS)
		clauses = parse_section_data (m, &num_clauses, (const unsigned char*)ptr);
	if (local_var_sig_tok) {
		const char *locals_ptr;
		int len=0, i;

		locals_ptr = mono_metadata_blob_heap (m, cols [MONO_STAND_ALONE_SIGNATURE]);
		mono_metadata_decode_blob_size (locals_ptr, &locals_ptr);
		if (*locals_ptr != 0x07)
			g_warning ("wrong signature for locals blob");
		locals_ptr++;
		len = mono_metadata_decode_value (locals_ptr, &locals_ptr);
		mh = g_malloc0 (MONO_SIZEOF_METHOD_HEADER + len * sizeof (MonoType*) + num_clauses * sizeof (MonoExceptionClause));
		mh->num_locals = len;
		for (i = 0; i < len; ++i) {
			mh->locals [i] = mono_metadata_parse_type_internal (m, container,
																MONO_PARSE_LOCAL, 0, TRUE, locals_ptr, &locals_ptr);
			if (!mh->locals [i])
				goto fail;
		}
	} else {
		mh = g_malloc0 (MONO_SIZEOF_METHOD_HEADER + num_clauses * sizeof (MonoExceptionClause));
	}
	mh->code = code;
	mh->code_size = code_size;
	mh->max_stack = max_stack;
	mh->is_transient = TRUE;
	mh->init_locals = init_locals;
	if (clauses) {
		MonoExceptionClause* clausesp = (MonoExceptionClause*)&mh->locals [mh->num_locals];
		memcpy (clausesp, clauses, num_clauses * sizeof (MonoExceptionClause));
		g_free (clauses);
		mh->clauses = clausesp;
		mh->num_clauses = num_clauses;
	}
	return mh;
fail:
	g_free (clauses);
	g_free (mh);
	return NULL;

}

/*
 * mono_metadata_parse_mh:
 * @generic_context: generics context
 * @ptr: pointer to the method header.
 *
 * Decode the method header at @ptr, including pointer to the IL code,
 * info about local variables and optional exception tables.
 *
 * Returns: a transient MonoMethodHeader allocated from the heap.
 */
MonoMethodHeader *
mono_metadata_parse_mh (MonoImage *m, const char *ptr)
{
	return mono_metadata_parse_mh_full (m, NULL, ptr);
}

/*
 * mono_metadata_free_mh:
 * @mh: a method header
 *
 * Free the memory allocated for the method header.
 */
void
mono_metadata_free_mh (MonoMethodHeader *mh)
{
	int i;

	/* If it is not transient it means it's part of a wrapper method,
	 * or a SRE-generated method, so the lifetime in that case is
	 * dictated by the method's own lifetime
	 */
	if (mh->is_transient) {
		for (i = 0; i < mh->num_locals; ++i)
			mono_metadata_free_type (mh->locals [i]);
		g_free (mh);
	}
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		return 0;

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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		return 0;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/*
 * mono_metadata_interfaces_from_typedef_full:
 * @meta: metadata context
 * @index: typedef token
 * @interfaces: Out parameter used to store the interface array
 * @count: Out parameter used to store the number of interfaces
 * @heap_alloc_result: if TRUE the result array will be g_malloc'd
 * @context: The generic context
 * 
 * The array of interfaces that the @index typedef token implements is returned in
 * @interfaces. The number of elements in the array is returned in @count. 
 *

 * Returns: TRUE on success, FALSE on failure.
 */
gboolean
mono_metadata_interfaces_from_typedef_full (MonoImage *meta, guint32 index, MonoClass ***interfaces, guint *count, gboolean heap_alloc_result, MonoGenericContext *context, MonoError *error)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_INTERFACEIMPL];
	locator_t loc;
	guint32 start, pos;
	guint32 cols [MONO_INTERFACEIMPL_SIZE];
	MonoClass **result;

	*interfaces = NULL;
	*count = 0;

	mono_error_init (error);

	if (!tdef->base)
		return TRUE;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_INTERFACEIMPL_CLASS;
	loc.t = tdef;

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

	if (heap_alloc_result)
		result = g_new0 (MonoClass*, pos - start);
	else
		result = mono_image_alloc0 (meta, sizeof (MonoClass*) * (pos - start));

	pos = start;
	while (pos < tdef->rows) {
		MonoClass *iface;
		
		mono_metadata_decode_row (tdef, pos, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		iface = mono_class_get_and_inflate_typespec_checked (
			meta, mono_metadata_token_from_dor (cols [MONO_INTERFACEIMPL_INTERFACE]), context, error);
		if (iface == NULL)
			return FALSE;
		result [pos - start] = iface;
		++pos;
	}
	*count = pos - start;
	*interfaces = result;
	return TRUE;
}

/*
 * @meta: metadata context
 * @index: typedef token
 * @count: Out parameter used to store the number of interfaces
 * 
 * The array of interfaces that the @index typedef token implements is returned in
 * @interfaces. The number of elements in the array is returned in @count. The returned
 * array is g_malloc'd and the caller must free it.
 *
 * LOCKING: Acquires the loader lock .
 *
 * Returns: the interface array on success, NULL on failure.
 */

MonoClass**
mono_metadata_interfaces_from_typedef (MonoImage *meta, guint32 index, guint *count)
{
	MonoError error;
	MonoClass **interfaces = NULL;
	gboolean rv;

	rv = mono_metadata_interfaces_from_typedef_full (meta, index, &interfaces, count, TRUE, NULL, &error);
	g_assert (mono_error_ok (&error)); /* FIXME dont swallow the error */
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
 * Returns 0 if @index describes a non-nested type.
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, declsec_locator))
		return -1;

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_DECL_SECURITY_PARENT) == index))
		loc.result --;

	return loc.result;
}

/*
 * mono_metadata_localscope_from_methoddef:
 * @meta: metadata context
 * @index: methoddef index
 * 
 * Returns: the 1-based index into the LocalScope table of the first
 * scope which belongs to the method described by @index.
 * Returns 0 if no such row is found.
 */
guint32
mono_metadata_localscope_from_methoddef (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_LOCALSCOPE];
	locator_t loc;

	if (!tdef->base)
		return 0;

	loc.idx = index;
	loc.col_idx = MONO_LOCALSCOPE_METHOD;
	loc.t = tdef;

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_LOCALSCOPE_METHOD) == index))
		loc.result --;

	return loc.result + 1;
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

static int i8_align;

/*
 * mono_type_set_alignment:
 *
 *   Set the alignment used by runtime to layout fields etc. of type TYPE to ALIGN.
 * This should only be used in AOT mode since the resulting layout will not match the
 * host abi layout.
 */
void
mono_type_set_alignment (MonoTypeEnum type, int align)
{
	/* Support only a few types whose alignment is abi dependent */
	switch (type) {
	case MONO_TYPE_I8:
		i8_align = align;
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

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
	MonoTypeEnum simple_type;

	if (!t) {
		*align = 1;
		return 0;
	}
	if (t->byref) {
		*align = MONO_ABI_ALIGNOF (gpointer);
		return sizeof (gpointer);
	}

	simple_type = t->type;
 again:
	switch (simple_type) {
	case MONO_TYPE_VOID:
		*align = 1;
		return 0;
	case MONO_TYPE_BOOLEAN:
		*align = MONO_ABI_ALIGNOF (gint8);
		return 1;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*align = MONO_ABI_ALIGNOF (gint8);
		return 1;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		*align = MONO_ABI_ALIGNOF (gint16);
		return 2;		
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*align = MONO_ABI_ALIGNOF (gint32);
		return 4;
	case MONO_TYPE_R4:
		*align = MONO_ABI_ALIGNOF (float);
		return 4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*align = MONO_ABI_ALIGNOF (gint64);
		return 8;		
	case MONO_TYPE_R8:
		*align = MONO_ABI_ALIGNOF (double);
		return 8;		
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		*align = MONO_ABI_ALIGNOF (gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_STRING:
		*align = MONO_ABI_ALIGNOF (gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_OBJECT:
		*align = MONO_ABI_ALIGNOF (gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_VALUETYPE: {
		if (t->data.klass->enumtype)
			return mono_type_size (mono_class_enum_basetype (t->data.klass), align);
		else
			return mono_class_value_size (t->data.klass, (guint32*)align);
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
		*align = MONO_ABI_ALIGNOF (gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_TYPEDBYREF:
		return mono_class_value_size (mono_defaults.typed_reference_class, (guint32*)align);
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoClass *container_class = gclass->container_class;

		// g_assert (!gclass->inst->is_open);

		if (container_class->valuetype) {
			if (container_class->enumtype)
				return mono_type_size (mono_class_enum_basetype (container_class), align);
			else
				return mono_class_value_size (mono_class_from_mono_type (t), (guint32*)align);
		} else {
			*align = MONO_ABI_ALIGNOF (gpointer);
			return sizeof (gpointer);
		}
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (!t->data.generic_param->gshared_constraint || t->data.generic_param->gshared_constraint->type == MONO_TYPE_VALUETYPE) {
			*align = MONO_ABI_ALIGNOF (gpointer);
			return sizeof (gpointer);
		} else {
			/* The gparam can only match types given by gshared_constraint */
			return mono_type_size (t->data.generic_param->gshared_constraint, align);
			goto again;
		}
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
	MonoTypeEnum simple_type;
#if SIZEOF_VOID_P == SIZEOF_REGISTER
	int stack_slot_size = sizeof (gpointer);
	int stack_slot_align = MONO_ABI_ALIGNOF (gpointer);
#elif SIZEOF_VOID_P < SIZEOF_REGISTER
	int stack_slot_size = SIZEOF_REGISTER;
	int stack_slot_align = SIZEOF_REGISTER;
#endif

	g_assert (t != NULL);

	if (!align)
		align = &tmp;

	if (t->byref) {
		*align = stack_slot_align;
		return stack_slot_size;
	}

	simple_type = t->type;
	switch (simple_type) {
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
		*align = stack_slot_align;
		return stack_slot_size;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (allow_open);
		if (!t->data.generic_param->gshared_constraint || t->data.generic_param->gshared_constraint->type == MONO_TYPE_VALUETYPE) {
			*align = stack_slot_align;
			return stack_slot_size;
		} else {
			/* The gparam can only match types given by gshared_constraint */
			return mono_type_stack_size_internal (t->data.generic_param->gshared_constraint, align, allow_open);
		}
	case MONO_TYPE_TYPEDBYREF:
		*align = stack_slot_align;
		return stack_slot_size * 3;
	case MONO_TYPE_R4:
		*align = MONO_ABI_ALIGNOF (float);
		return sizeof (float);		
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*align = MONO_ABI_ALIGNOF (gint64);
		return sizeof (gint64);		
	case MONO_TYPE_R8:
		*align = MONO_ABI_ALIGNOF (double);
		return sizeof (double);
	case MONO_TYPE_VALUETYPE: {
		guint32 size;

		if (t->data.klass->enumtype)
			return mono_type_stack_size_internal (mono_class_enum_basetype (t->data.klass), align, allow_open);
		else {
			size = mono_class_value_size (t->data.klass, (guint32*)align);

			*align = *align + stack_slot_align - 1;
			*align &= ~(stack_slot_align - 1);

			size += stack_slot_size - 1;
			size &= ~(stack_slot_size - 1);

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
				return mono_type_stack_size_internal (mono_class_enum_basetype (container_class), align, allow_open);
			else {
				guint32 size = mono_class_value_size (mono_class_from_mono_type (t), (guint32*)align);

				*align = *align + stack_slot_align - 1;
				*align &= ~(stack_slot_align - 1);

				size += stack_slot_size - 1;
				size &= ~(stack_slot_size - 1);

				return size;
			}
		} else {
			*align = stack_slot_align;
			return stack_slot_size;
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
		hash = ((hash << 5) - hash) ^ mono_metadata_generic_inst_hash (context->class_inst);
	if (context->method_inst)
		hash = ((hash << 5) - hash) ^ mono_metadata_generic_inst_hash (context->method_inst);
	return hash;
}

gboolean
mono_metadata_generic_context_equal (const MonoGenericContext *g1, const MonoGenericContext *g2)
{
	return g1->class_inst == g2->class_inst && g1->method_inst == g2->method_inst;
}

/*
 * mono_metadata_str_hash:
 *
 *   This should be used instead of g_str_hash for computing hash codes visible
 * outside this module, since g_str_hash () is not guaranteed to be stable
 * (its not the same in eglib for example).
 */
guint
mono_metadata_str_hash (gconstpointer v1)
{
	/* Same as g_str_hash () in glib */
	char *p = (char *) v1;
	guint hash = *p;

	while (*p++) {
		if (*p)
			hash = (hash << 5) - hash + *p;
	}

	return hash;
} 

/*
 * mono_metadata_type_hash:
 * @t1: a type
 *
 * Computes an hash value for @t1 to be used in GHashTable.
 * The returned hash is guaranteed to be the same across executions.
 */
guint
mono_metadata_type_hash (MonoType *t1)
{
	guint hash = t1->type;

	hash |= t1->byref << 6; /* do not collide with t1->type values */
	switch (t1->type) {
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY: {
		MonoClass *class = t1->data.klass;
		/*
		 * Dynamic classes must not be hashed on their type since it can change
		 * during runtime. For example, if we hash a reference type that is
		 * later made into a valuetype.
		 *
		 * This is specially problematic with generic instances since they are
		 * inserted in a bunch of hash tables before been finished.
		 */
		if (image_is_dynamic (class->image))
			return (t1->byref << 6) | mono_metadata_str_hash (class->name);
		return ((hash << 5) - hash) ^ mono_metadata_str_hash (class->name);
	}
	case MONO_TYPE_PTR:
		return ((hash << 5) - hash) ^ mono_metadata_type_hash (t1->data.type);
	case MONO_TYPE_ARRAY:
		return ((hash << 5) - hash) ^ mono_metadata_type_hash (&t1->data.array->eklass->byval_arg);
	case MONO_TYPE_GENERICINST:
		return ((hash << 5) - hash) ^ mono_generic_class_hash (t1->data.generic_class);
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		return ((hash << 5) - hash) ^ mono_metadata_generic_param_hash (t1->data.generic_param);
	default:
		return hash;
	}
}

guint
mono_metadata_generic_param_hash (MonoGenericParam *p)
{
	guint hash;
	MonoGenericParamInfo *info;

	hash = (mono_generic_param_num (p) << 2);
	if (p->gshared_constraint)
		hash = ((hash << 5) - hash) ^ mono_metadata_type_hash (p->gshared_constraint);
	info = mono_generic_param_info (p);
	/* Can't hash on the owner klass/method, since those might not be set when this is called */
	if (info)
		hash = ((hash << 5) - hash) ^ info->token;
	return hash;
}

static gboolean
mono_metadata_generic_param_equal_internal (MonoGenericParam *p1, MonoGenericParam *p2, gboolean signature_only)
{
	if (p1 == p2)
		return TRUE;
	if (mono_generic_param_num (p1) != mono_generic_param_num (p2))
		return FALSE;
	if (p1->gshared_constraint && p2->gshared_constraint) {
		if (!mono_metadata_type_equal (p1->gshared_constraint, p2->gshared_constraint))
			return FALSE;
	} else {
		if (p1->gshared_constraint != p2->gshared_constraint)
			return FALSE;
	}

	/*
	 * We have to compare the image as well because if we didn't,
	 * the generic_inst_cache lookup wouldn't care about the image
	 * of generic params, so what could happen is that a generic
	 * inst with params from image A is put into the cache, then
	 * image B gets that generic inst from the cache, image A is
	 * unloaded, so the inst is deleted, but image B still retains
	 * a pointer to it.
	 *
	 * The AOT runtime doesn't set the image when it's decoding
	 * types, so we only compare it when the owner is NULL.
	 */
	if (mono_generic_param_owner (p1) == mono_generic_param_owner (p2) &&
	    (mono_generic_param_owner (p1) || p1->image == p2->image))
		return TRUE;

	/*
	 * If `signature_only' is true, we're comparing two (method) signatures.
	 * In this case, the owner of two type parameters doesn't need to match.
	 */

	return signature_only;
}

gboolean
mono_metadata_generic_param_equal (MonoGenericParam *p1, MonoGenericParam *p2)
{
	return mono_metadata_generic_param_equal_internal (p1, p2, TRUE);
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
		return mono_metadata_generic_param_equal_internal (
			c1->byval_arg.data.generic_param, c2->byval_arg.data.generic_param, signature_only);
	if ((c1->byval_arg.type == MONO_TYPE_MVAR) && (c2->byval_arg.type == MONO_TYPE_MVAR))
		return mono_metadata_generic_param_equal_internal (
			c1->byval_arg.data.generic_param, c2->byval_arg.data.generic_param, signature_only);
	if (signature_only &&
	    (c1->byval_arg.type == MONO_TYPE_SZARRAY) && (c2->byval_arg.type == MONO_TYPE_SZARRAY))
		return mono_metadata_class_equal (c1->byval_arg.data.klass, c2->byval_arg.data.klass, signature_only);
	if (signature_only &&
	    (c1->byval_arg.type == MONO_TYPE_ARRAY) && (c2->byval_arg.type == MONO_TYPE_ARRAY))
		return do_mono_metadata_type_equal (&c1->byval_arg, &c2->byval_arg, signature_only);
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
		return mono_metadata_generic_param_equal_internal (
			t1->data.generic_param, t2->data.generic_param, signature_only);
	case MONO_TYPE_MVAR:
		return mono_metadata_generic_param_equal_internal (
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
 * @image: image to alloc memory from
 * @original: type to duplicate
 *
 * Returns: copy of type allocated from the image's mempool (or from the heap, if @image is null).
 */
MonoType *
mono_metadata_type_dup (MonoImage *image, const MonoType *o)
{
	MonoType *r = NULL;
	int sizeof_o = MONO_SIZEOF_TYPE;
	if (o->num_mods)
		sizeof_o += o->num_mods  * sizeof (MonoCustomMod);

	r = image ? mono_image_alloc0 (image, sizeof_o) : g_malloc (sizeof_o);

	memcpy (r, o, sizeof_o);

	if (o->type == MONO_TYPE_PTR) {
		r->data.type = mono_metadata_type_dup (image, o->data.type);
	} else if (o->type == MONO_TYPE_ARRAY) {
		r->data.array = mono_dup_array_type (image, o->data.array);
	} else if (o->type == MONO_TYPE_FNPTR) {
		/*FIXME the dup'ed signature is leaked mono_metadata_free_type*/
		r->data.method = mono_metadata_signature_deep_dup (image, o->data.method);
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
	mono_metadata_field_info_full (meta, index, offset, rva, marshal_spec, FALSE);
}

void
mono_metadata_field_info_with_mempool (MonoImage *meta, guint32 index, guint32 *offset, guint32 *rva, 
			  MonoMarshalSpec **marshal_spec)
{
	mono_metadata_field_info_full (meta, index, offset, rva, marshal_spec, TRUE);
}

static void
mono_metadata_field_info_full (MonoImage *meta, guint32 index, guint32 *offset, guint32 *rva, 
				       MonoMarshalSpec **marshal_spec, gboolean alloc_from_image)
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

		if (tdef->base && mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator)) {
			*offset = mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_LAYOUT_OFFSET);
		} else {
			*offset = (guint32)-1;
		}
	}
	if (rva) {
		tdef = &meta->tables [MONO_TABLE_FIELDRVA];

		loc.col_idx = MONO_FIELD_RVA_FIELD;
		loc.t = tdef;
		
		if (tdef->base && mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator)) {
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
			*marshal_spec = mono_metadata_parse_marshal_spec_full (alloc_from_image ? meta : NULL, meta, p);
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

	if (tdef->base && mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator)) {
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

	if (!mono_binary_search (&loc, msemt->base, msemt->rows, msemt->row_size, table_locator))
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

	if (!mono_binary_search (&loc, msemt->base, msemt->rows, msemt->row_size, table_locator))
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	return loc.result + 1;
}

/**
 * @image: context where the image is created
 * @type_spec:  typespec token
 * @deprecated use mono_type_create_from_typespec_checked that has proper error handling
 *
 * Creates a MonoType representing the TypeSpec indexed by the @type_spec
 * token.
 */
MonoType *
mono_type_create_from_typespec (MonoImage *image, guint32 type_spec)
{
	MonoError error;
	MonoType *type = mono_type_create_from_typespec_checked (image, type_spec, &error);
	if (!type)
		 g_error ("Could not create typespec %x due to %s", type_spec, mono_error_get_message (&error));
	return type;
}

MonoType *
mono_type_create_from_typespec_checked (MonoImage *image, guint32 type_spec, MonoError *error)

{
	guint32 idx = mono_metadata_token_index (type_spec);
	MonoTableInfo *t;
	guint32 cols [MONO_TYPESPEC_SIZE];
	const char *ptr;
	MonoType *type, *type2;

	mono_error_init (error);

	mono_image_lock (image);
	type = g_hash_table_lookup (image->typespec_cache, GUINT_TO_POINTER (type_spec));
	mono_image_unlock (image);
	if (type)
		return type;

	t = &image->tables [MONO_TABLE_TYPESPEC];

	mono_metadata_decode_row (t, idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (image, cols [MONO_TYPESPEC_SIGNATURE]);

	if (!mono_verifier_verify_typespec_signature (image, cols [MONO_TYPESPEC_SIGNATURE], type_spec, NULL)) {
		mono_error_set_bad_image (error, image, "Could not verify type spec %08x.", type_spec);
		return NULL;
	}

	mono_metadata_decode_value (ptr, &ptr);

	type = mono_metadata_parse_type_internal (image, NULL, MONO_PARSE_TYPE, 0, TRUE, ptr, &ptr);
	if (!type) {
		if (mono_loader_get_last_error ())
			mono_error_set_from_loader_error (error);
		else
			mono_error_set_bad_image (error, image, "Could not parse type spec %08x.", type_spec);
		return NULL;
	}

	type2 = mono_metadata_type_dup (image, type);
	mono_metadata_free_type (type);

	mono_image_lock (image);
	type = g_hash_table_lookup (image->typespec_cache, GUINT_TO_POINTER (type_spec));
	/* We might leak some data in the image mempool if found */
	if (!type) {
		g_hash_table_insert (image->typespec_cache, GUINT_TO_POINTER (type_spec), type2);
		type = type2;
	}
	mono_image_unlock (image);

	return type;
}


static char*
mono_image_strndup (MonoImage *image, const char *data, guint len)
{
	char *res;
	if (!image)
		return g_strndup (data, len);
	res = mono_image_alloc (image, len + 1);
	memcpy (res, data, len);
	res [len] = 0;
	return res;
}

MonoMarshalSpec *
mono_metadata_parse_marshal_spec (MonoImage *image, const char *ptr)
{
	return mono_metadata_parse_marshal_spec_full (NULL, image, ptr);
}

/*
 * If IMAGE is non-null, memory will be allocated from its mempool, otherwise it will be allocated using malloc.
 * PARENT_IMAGE is the image containing the marshal spec.
 */
MonoMarshalSpec *
mono_metadata_parse_marshal_spec_full (MonoImage *image, MonoImage *parent_image, const char *ptr)
{
	MonoMarshalSpec *res;
	int len;
	const char *start = ptr;

	/* fixme: this is incomplete, but I cant find more infos in the specs */

	if (image)
		res = mono_image_alloc0 (image, sizeof (MonoMarshalSpec));
	else
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
		res->data.custom_data.custom_name = mono_image_strndup (image, ptr, len);		
		ptr += len;
		/* read cookie string */
		len = mono_metadata_decode_value (ptr, &ptr);
		res->data.custom_data.cookie = mono_image_strndup (image, ptr, len);
		res->data.custom_data.image = parent_image;
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
	case MONO_TYPE_CHAR:
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_U2:
			case MONO_NATIVE_U1:
				return mspec->native;
			default:
				g_error ("cant marshal char to native type %02x", mspec->native);
			}
		}
		return unicode ? MONO_NATIVE_U2 : MONO_NATIVE_U1;
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
			t = mono_class_enum_basetype (type->data.klass)->type;
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return NULL;

	return mono_metadata_blob_heap (meta, mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_MARSHAL_NATIVE_TYPE));
}

MonoMethod*
method_from_method_def_or_ref (MonoImage *m, guint32 tok, MonoGenericContext *context, MonoError *error)
{
	MonoMethod *result = NULL;
	guint32 idx = tok >> MONO_METHODDEFORREF_BITS;

	mono_error_init (error);

	switch (tok & MONO_METHODDEFORREF_MASK) {
	case MONO_METHODDEFORREF_METHODDEF:
		result = mono_get_method_checked (m, MONO_TOKEN_METHOD_DEF | idx, NULL, context, error);
		break;
	case MONO_METHODDEFORREF_METHODREF:
		result = mono_get_method_checked (m, MONO_TOKEN_MEMBER_REF | idx, NULL, context, error);
		break;
	default:
		mono_error_set_bad_image (error, m, "Invalid MethodDefOfRef token %x", tok);
	}

	return result;
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
	MonoError error;
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

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
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

		if (!mono_verifier_verify_methodimpl_row (image, start + i, &error)) {
			mono_error_cleanup (&error); /* FIXME don't swallow the error */
			ok = FALSE;
			break;
		}

		mono_metadata_decode_row (tdef, start + i, cols, MONO_METHODIMPL_SIZE);
		method = method_from_method_def_or_ref (
			image, cols [MONO_METHODIMPL_DECLARATION], generic_context, &error);
		if (method == NULL) {
			mono_error_cleanup (&error); /* FIXME don't swallow the error */
			ok = FALSE;
		}
		result [i * 2] = method;
		method = method_from_method_def_or_ref (
			image, cols [MONO_METHODIMPL_BODY], generic_context, &error);
		if (method == NULL) {
			mono_error_cleanup (&error); /* FIXME don't swallow the error */
			ok = FALSE;
		}
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
get_constraints (MonoImage *image, int owner, MonoClass ***constraints, MonoGenericContainer *container, MonoError *error)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	guint32 cols [MONO_GENPARCONSTRAINT_SIZE];
	guint32 i, token, found;
	MonoClass *klass, **res;
	GSList *cons = NULL, *tmp;
	MonoGenericContext *context = &container->context;

	mono_error_init (error);

	*constraints = NULL;
	found = 0;
	for (i = 0; i < tdef->rows; ++i) {
		mono_metadata_decode_row (tdef, i, cols, MONO_GENPARCONSTRAINT_SIZE);
		if (cols [MONO_GENPARCONSTRAINT_GENERICPAR] == owner) {
			token = mono_metadata_token_from_dor (cols [MONO_GENPARCONSTRAINT_CONSTRAINT]);
			klass = mono_class_get_and_inflate_typespec_checked (image, token, context, error);
			if (!klass) {
				g_slist_free (cons);
				return FALSE;
			}
			cons = g_slist_append (cons, klass);
			++found;
		} else {
			/* contiguous list finished */
			if (found)
				break;
		}
	}
	if (!found)
		return TRUE;
	res = mono_image_alloc0 (image, sizeof (MonoClass*) * (found + 1));
	for (i = 0, tmp = cons; i < found; ++i, tmp = tmp->next) {
		res [i] = tmp->data;
	}
	g_slist_free (cons);
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
	locator_t loc;

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

	loc.idx = *owner;
	loc.col_idx = MONO_GENERICPARAM_OWNER;
	loc.t = tdef;

	if (!mono_binary_search (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_GENERICPARAM_OWNER) == loc.idx))
		loc.result --;

	return loc.result + 1;
}

gboolean
mono_metadata_has_generic_params (MonoImage *image, guint32 token)
{
	guint32 owner;
	return mono_metadata_get_generic_param_row (image, token, &owner);
}

/*
 * Memory is allocated from IMAGE's mempool.
 */
gboolean
mono_metadata_load_generic_param_constraints_checked (MonoImage *image, guint32 token,
					      MonoGenericContainer *container, MonoError *error)
{

	guint32 start_row, i, owner;
	mono_error_init (error);

	if (! (start_row = mono_metadata_get_generic_param_row (image, token, &owner)))
		return TRUE;
	for (i = 0; i < container->type_argc; i++) {
		if (!get_constraints (image, start_row + i, &mono_generic_container_get_param_info (container, i)->constraints, container, error)) {
			mono_loader_assert_no_error ();
			return FALSE;
		}
	}
	return TRUE;
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
 * LOCKING: Acquires the loader lock
 *
 */
MonoGenericContainer *
mono_metadata_load_generic_params (MonoImage *image, guint32 token, MonoGenericContainer *parent_container)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAM];
	guint32 cols [MONO_GENERICPARAM_SIZE];
	guint32 i, owner = 0, n;
	MonoGenericContainer *container;
	MonoGenericParamFull *params;
	MonoGenericContext *context;

	if (!(i = mono_metadata_get_generic_param_row (image, token, &owner)))
		return NULL;
	mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	params = NULL;
	n = 0;
	container = mono_image_alloc0 (image, sizeof (MonoGenericContainer));
	container->image = image;
	do {
		n++;
		params = g_realloc (params, sizeof (MonoGenericParamFull) * n);
		memset (&params [n - 1], 0, sizeof (MonoGenericParamFull));
		params [n - 1].param.owner = container;
		params [n - 1].param.num = cols [MONO_GENERICPARAM_NUMBER];
		params [n - 1].info.token = i | MONO_TOKEN_GENERIC_PARAM;
		params [n - 1].info.flags = cols [MONO_GENERICPARAM_FLAGS];
		params [n - 1].info.name = mono_metadata_string_heap (image, cols [MONO_GENERICPARAM_NAME]);
		if (params [n - 1].param.num != n - 1)
			g_warning ("GenericParam table unsorted or hole in generic param sequence: token %d", i);
		if (++i > tdef->rows)
			break;
		mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	} while (cols [MONO_GENERICPARAM_OWNER] == owner);

	container->type_argc = n;
	container->type_params = mono_image_alloc0 (image, sizeof (MonoGenericParamFull) * n);
	memcpy (container->type_params, params, sizeof (MonoGenericParamFull) * n);
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
		t->data.generic_param = mono_generic_container_get_param (container, i);

		type_argv [i] = t;
	}

	nginst = mono_metadata_get_generic_inst (container->type_argc, type_argv);

	g_free (type_argv);
	g_free (helper);

	return nginst;
}

/**
 * mono_type_is_byref:
 * @type: the MonoType operated on
 *
 * Returns: #TRUE if @type represents a type passed by reference,
 * #FALSE otherwise.
 */
gboolean
mono_type_is_byref (MonoType *type)
{
	return type->byref;
}

/**
 * mono_type_get_type:
 * @type: the MonoType operated on
 *
 * Returns: the IL type value for @type. This is one of the MonoTypeEnum
 * enum members like MONO_TYPE_I4 or MONO_TYPE_STRING.
 */
int
mono_type_get_type (MonoType *type)
{
	return type->type;
}

/**
 * mono_type_get_signature:
 * @type: the MonoType operated on
 *
 * It is only valid to call this function if @type is a MONO_TYPE_FNPTR.
 *
 * Returns: the MonoMethodSignature pointer that describes the signature
 * of the function pointer @type represents.
 */
MonoMethodSignature*
mono_type_get_signature (MonoType *type)
{
	g_assert (type->type == MONO_TYPE_FNPTR);
	return type->data.method;
}

/**
 * mono_type_get_class:
 * @type: the MonoType operated on
 *
 * It is only valid to call this function if @type is a MONO_TYPE_CLASS or a
 * MONO_TYPE_VALUETYPE. For more general functionality, use mono_class_from_mono_type (),
 * instead
 *
 * Returns: the MonoClass pointer that describes the class that @type represents.
 */
MonoClass*
mono_type_get_class (MonoType *type)
{
	/* FIXME: review the runtime users before adding the assert here */
	return type->data.klass;
}

/**
 * mono_type_get_array_type:
 * @type: the MonoType operated on
 *
 * It is only valid to call this function if @type is a MONO_TYPE_ARRAY.
 *
 * Returns: a MonoArrayType struct describing the array type that @type
 * represents. The info includes details such as rank, array element type
 * and the sizes and bounds of multidimensional arrays.
 */
MonoArrayType*
mono_type_get_array_type (MonoType *type)
{
	return type->data.array;
}

/**
 * mono_type_get_ptr_type:
 * @type: the MonoType operated on
 *
 * It is only valid to call this function if @type is a MONO_TYPE_PTR.
 * instead
 *
 * Returns: the MonoType pointer that describes the type that @type
 * represents a pointer to.
 */
MonoType*
mono_type_get_ptr_type (MonoType *type)
{
	g_assert (type->type == MONO_TYPE_PTR);
	return type->data.type;
}

MonoClass*
mono_type_get_modifiers (MonoType *type, gboolean *is_required, gpointer *iter)
{
	/* FIXME: implement */
	return NULL;
}

/**
 * mono_type_is_struct:
 * @type: the MonoType operated on
 *
 * Returns: #TRUE is @type is a struct, that is a ValueType but not en enum
 * or a basic type like System.Int32. #FALSE otherwise.
 */
mono_bool
mono_type_is_struct (MonoType *type)
{
	return (!type->byref && ((type->type == MONO_TYPE_VALUETYPE &&
		!type->data.klass->enumtype) || (type->type == MONO_TYPE_TYPEDBYREF) ||
		((type->type == MONO_TYPE_GENERICINST) && 
		mono_metadata_generic_class_is_valuetype (type->data.generic_class) &&
		!type->data.generic_class->container_class->enumtype)));
}

/**
 * mono_type_is_void:
 * @type: the MonoType operated on
 *
 * Returns: #TRUE is @type is System.Void. #FALSE otherwise.
 */
mono_bool
mono_type_is_void (MonoType *type)
{
	return (type && (type->type == MONO_TYPE_VOID) && !type->byref);
}

/**
 * mono_type_is_pointer:
 * @type: the MonoType operated on
 *
 * Returns: #TRUE is @type is a managed or unmanaged pointer type. #FALSE otherwise.
 */
mono_bool
mono_type_is_pointer (MonoType *type)
{
	return (type && ((type->byref || (type->type == MONO_TYPE_I) || type->type == MONO_TYPE_STRING)
		|| (type->type == MONO_TYPE_SZARRAY) || (type->type == MONO_TYPE_CLASS) ||
		(type->type == MONO_TYPE_U) || (type->type == MONO_TYPE_OBJECT) ||
		(type->type == MONO_TYPE_ARRAY) || (type->type == MONO_TYPE_PTR) ||
		(type->type == MONO_TYPE_FNPTR)));
}

/**
 * mono_type_is_reference:
 * @type: the MonoType operated on
 *
 * Returns: #TRUE is @type represents an object reference . #FALSE otherwise.
 */
mono_bool
mono_type_is_reference (MonoType *type)
{
	return (type && (((type->type == MONO_TYPE_STRING) ||
		(type->type == MONO_TYPE_SZARRAY) || (type->type == MONO_TYPE_CLASS) ||
		(type->type == MONO_TYPE_OBJECT) || (type->type == MONO_TYPE_ARRAY)) ||
		((type->type == MONO_TYPE_GENERICINST) &&
		!mono_metadata_generic_class_is_valuetype (type->data.generic_class))));
}

/**
 * mono_signature_get_return_type:
 * @sig: the method signature inspected
 *
 * Returns: the return type of the method signature @sig
 */
MonoType*
mono_signature_get_return_type (MonoMethodSignature *sig)
{
	return sig->ret;
}

/**
 * mono_signature_get_params:
 * @sig: the method signature inspected
 * #iter: pointer to an iterator
 *
 * Iterates over the parameters for the method signature @sig.
 * A void* pointer must be initualized to #NULL to start the iteration
 * and it's address is passed to this function repeteadly until it returns
 * #NULL.
 *
 * Returns: the next parameter type of the method signature @sig,
 * #NULL when finished.
 */
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

/**
 * mono_signature_get_param_count:
 * @sig: the method signature inspected
 *
 * Returns: the number of parameters in the method signature @sig.
 */
guint32
mono_signature_get_param_count (MonoMethodSignature *sig)
{
	return sig->param_count;
}

/**
 * mono_signature_get_call_conv:
 * @sig: the method signature inspected
 *
 * Returns: the call convention of the method signature @sig.
 */
guint32
mono_signature_get_call_conv (MonoMethodSignature *sig)
{
	return sig->call_convention;
}

/**
 * mono_signature_vararg_start:
 * @sig: the method signature inspected
 *
 * Returns: the number of the first vararg parameter in the
 * method signature @sig. -1 if this is not a vararg signature.
 */
int
mono_signature_vararg_start (MonoMethodSignature *sig)
{
	return sig->sentinelpos;
}

/**
 * mono_signature_is_instance:
 * @sig: the method signature inspected
 *
 * Returns: #TRUE if this the method signature @sig has an implicit
 * first instance argument. #FALSE otherwise.
 */
gboolean
mono_signature_is_instance (MonoMethodSignature *sig)
{
	return sig->hasthis;
}

/**
 * mono_signature_param_is_out
 * @sig: the method signature inspected
 * @param_num: the 0-based index of the inspected parameter
 * 
 * Returns: #TRUE if the parameter is an out parameter, #FALSE
 * otherwise.
 */
mono_bool
mono_signature_param_is_out (MonoMethodSignature *sig, int param_num)
{
	g_assert (param_num >= 0 && param_num < sig->param_count);
	return (sig->params [param_num]->attrs & PARAM_ATTRIBUTE_OUT) != 0;
}

/**
 * mono_signature_explicit_this:
 * @sig: the method signature inspected
 *
 * Returns: #TRUE if this the method signature @sig has an explicit
 * instance argument. #FALSE otherwise.
 */
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

/*
 * If @field belongs to an inflated generic class, return the corresponding field of the
 * generic type definition class.
 */
MonoClassField*
mono_metadata_get_corresponding_field_from_generic_type_definition (MonoClassField *field)
{
	MonoClass *gtd;
	int offset;

	if (!field->parent->generic_class)
		return field;

	gtd = field->parent->generic_class->container_class;
	offset = field - field->parent->fields;
	return gtd->fields + offset;
}

/*
 * If @event belongs to an inflated generic class, return the corresponding event of the
 * generic type definition class.
 */
MonoEvent*
mono_metadata_get_corresponding_event_from_generic_type_definition (MonoEvent *event)
{
	MonoClass *gtd;
	int offset;

	if (!event->parent->generic_class)
		return event;

	gtd = event->parent->generic_class->container_class;
	offset = event - event->parent->ext->events;
	return gtd->ext->events + offset;
}

/*
 * If @property belongs to an inflated generic class, return the corresponding property of the
 * generic type definition class.
 */
MonoProperty*
mono_metadata_get_corresponding_property_from_generic_type_definition (MonoProperty *property)
{
	MonoClass *gtd;
	int offset;

	if (!property->parent->generic_class)
		return property;

	gtd = property->parent->generic_class->container_class;
	offset = property - property->parent->ext->properties;
	return gtd->ext->properties + offset;
}

