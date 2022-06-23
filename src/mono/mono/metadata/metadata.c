/**
 * \file
 * Routines for accessing the metadata
 *
 * Authors:
 *   Miguel de Icaza (miguel@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <glib.h>
#include <mono/metadata/metadata.h>
#include "tabledefs.h"
#include "mono-endian.h"
#include "cil-coff.h"
#include <mono/metadata/tokentype.h>
#include "class-internals.h"
#include "metadata-internals.h"
#include "reflection-internals.h"
#include "metadata-update.h"
#include <mono/metadata/class.h>
#include "marshal.h"
#include <mono/metadata/debug-helpers.h>
#include "abi-details.h"
#include "cominterop.h"
#include "components.h"
#include <mono/metadata/exception-internals.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/atomic.h>
#include <mono/utils/unlocked.h>
#include <mono/utils/mono-logger-internals.h>

/* Auxiliary structure used for caching inflated signatures */
typedef struct {
	MonoMethodSignature *sig;
	MonoGenericContext context;
} MonoInflatedMethodSignature;

static gboolean do_mono_metadata_parse_type (MonoType *type, MonoImage *m, MonoGenericContainer *container, gboolean transient,
					 const char *ptr, const char **rptr, MonoError *error);

static gboolean do_mono_metadata_type_equal (MonoType *t1, MonoType *t2, gboolean signature_only);
static gboolean mono_metadata_class_equal (MonoClass *c1, MonoClass *c2, gboolean signature_only);
static gboolean mono_metadata_fnptr_equal (MonoMethodSignature *s1, MonoMethodSignature *s2, gboolean signature_only);
static gboolean _mono_metadata_generic_class_equal (const MonoGenericClass *g1, const MonoGenericClass *g2,
						    gboolean signature_only);
static void free_generic_inst (MonoGenericInst *ginst);
static void free_generic_class (MonoGenericClass *ginst);
static void free_inflated_signature (MonoInflatedMethodSignature *sig);
static void free_aggregate_modifiers (MonoAggregateModContainer *amods);
static void mono_metadata_field_info_full (MonoImage *meta, guint32 index, guint32 *offset, guint32 *rva, MonoMarshalSpec **marshal_spec, gboolean alloc_from_image);

static MonoType* mono_signature_get_params_internal (MonoMethodSignature *sig, gpointer *iter);

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
	MONO_MT_RS_IDX,

	/* CustomDebugInformation parent encoded index */
	MONO_MT_HASCUSTDEBUG_IDX
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

#define ENCLOG_SCHEMA_OFFSET FIELD_RVA_SCHEMA_OFFSET + 3
	MONO_MT_UINT32,    /* "Token" }, */
	MONO_MT_UINT32,    /* "FuncCode" }, */
	MONO_MT_END,

#define ENCMAP_SCHEMA_OFFSET ENCLOG_SCHEMA_OFFSET + 3
	MONO_MT_UINT32,    /* "Token" }, */
	MONO_MT_END,

#define FIELD_POINTER_SCHEMA_OFFSET ENCMAP_SCHEMA_OFFSET + 2
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
	MONO_MT_TABLE_IDX,   /* Document */
	MONO_MT_BLOB_IDX,   /* SequencePoints */
	MONO_MT_END,

#define LOCALSCOPE_SCHEMA_OFFSET METHODBODY_SCHEMA_OFFSET + 3
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

#define LOCALCONSTANT_SCHEMA_OFFSET LOCALVARIABLE_SCHEMA_OFFSET + 4
	MONO_MT_STRING_IDX,  /* Name (String heap index) */
	MONO_MT_BLOB_IDX,    /* Signature (Blob heap index, LocalConstantSig blob) */
	MONO_MT_END,

#define IMPORTSCOPE_SCHEMA_OFFSET LOCALCONSTANT_SCHEMA_OFFSET + 3
	MONO_MT_TABLE_IDX, /* Parent (ImportScope row id or nil) */
	MONO_MT_BLOB_IDX,  /* Imports (Blob index, encoding: Imports blob) */
	MONO_MT_END,

#define ASYNCMETHOD_SCHEMA_OFFSET IMPORTSCOPE_SCHEMA_OFFSET + 3
	MONO_MT_TABLE_IDX, /* MoveNextMethod (MethodDef row id) */
	MONO_MT_TABLE_IDX, /* KickoffMethod (MethodDef row id) */
	MONO_MT_END,

#define CUSTOMDEBUGINFORMATION_SCHEMA_OFFSET ASYNCMETHOD_SCHEMA_OFFSET + 3
	MONO_MT_HASCUSTDEBUG_IDX, /* Parent (HasCustomDebugInformation coded index) */
	MONO_MT_GUID_IDX,  /* Kind (Guid heap index) */
	MONO_MT_BLOB_IDX,  /* Value (Blob heap index) */
	MONO_MT_END,

#define NULL_SCHEMA_OFFSET CUSTOMDEBUGINFORMATION_SCHEMA_OFFSET + 4
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
	ENCLOG_SCHEMA_OFFSET,
	ENCMAP_SCHEMA_OFFSET,
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
	LOCALVARIABLE_SCHEMA_OFFSET,
	LOCALCONSTANT_SCHEMA_OFFSET,
	IMPORTSCOPE_SCHEMA_OFFSET,
	ASYNCMETHOD_SCHEMA_OFFSET,
	CUSTOMDEBUGINFORMATION_SCHEMA_OFFSET
};

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
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
#define TABLEDEF(a,b) offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "mono/cil/tables.def"
#undef TABLEDEF
};

/* On legacy, if TRUE (but also see DISABLE_DESKTOP_LOADER #define), Mono will check
 * that the public key token, culture and version of a candidate assembly matches
 * the requested strong name. On netcore, it will check the culture and version.
 * If FALSE, as long as the name matches, the candidate will be allowed.
 */
static gboolean check_assembly_names_strictly = FALSE;

// Amount initially reserved in each imageset's mempool.
// FIXME: This number is arbitrary, a more practical number should be found
#define INITIAL_IMAGE_SET_SIZE    1024

/**
 * mono_meta_table_name:
 * \param table table index
 *
 * Returns the name of the given ECMA metadata logical format table
 * as described in ECMA 335, Partition II, Section 22.
 *
 * \returns the name for the \p table index
 */
const char *
mono_meta_table_name (int table)
{
	if ((table < 0) || (table > MONO_TABLE_LAST))
		return "";

	return (const char*)&tablestr + tableidx [table];
}

/* The guy who wrote the spec for this should not be allowed near a
 * computer again.

If  e is a coded token(see clause 23.1.7) that points into table ti out of n possible tables t0, .. tn-1,
then it is stored as e << (log n) & tag{ t0, .. tn-1}[ ti] using 2 bytes if the maximum number of
rows of tables t0, ..tn-1, is less than 2^16 - (log n), and using 4 bytes otherwise. The family of
finite maps tag{ t0, ..tn-1} is defined below. Note that to decode a physical row, you need the
inverse of this mapping.

 */
static int
rtsize (MonoImage *meta, int sz, int bits)
{
	if (G_UNLIKELY (meta->minimal_delta))
		return 4;
	if (sz < (1 << bits))
		return 2;
	else
		return 4;
}

static int
idx_size (MonoImage *meta, int idx)
{
	if (G_UNLIKELY (meta->minimal_delta))
		return 4;
	if (meta->referenced_tables && (meta->referenced_tables & ((guint64)1 << idx)))
		return meta->referenced_table_rows [idx] < 65536 ? 2 : 4;
	else
		return table_info_get_rows (&meta->tables [idx]) < 65536 ? 2 : 4;
}

static int
get_nrows (MonoImage *meta, int idx)
{
	if (meta->referenced_tables && (meta->referenced_tables & ((guint64)1 << idx)))
		return meta->referenced_table_rows [idx];
	else
		return table_info_get_rows (&meta->tables [idx]);
}

/* Reference: Partition II - 23.2.6 */
/**
 * mono_metadata_compute_size:
 * \param meta metadata context
 * \param tableindex metadata table number
 * \param result_bitfield pointer to \c guint32 where to store additional info
 *
 * \c mono_metadata_compute_size computes the length in bytes of a single
 * row in a metadata table. The size of each column is encoded in the
 * \p result_bitfield return value along with the number of columns in the table.
 * the resulting bitfield should be handed to the \c mono_metadata_table_size
 * and \c mono_metadata_table_count macros.
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
				n = MAX (get_nrows (meta, MONO_TABLE_METHOD), get_nrows (meta, MONO_TABLE_TYPEDEF));
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
			case MONO_TABLE_METHODBODY:
				g_assert (i == 0);
				field_size = idx_size (meta, MONO_TABLE_DOCUMENT); break;
			case MONO_TABLE_IMPORTSCOPE:
				g_assert(i == 0);
				field_size = idx_size (meta, MONO_TABLE_IMPORTSCOPE); break;
			case MONO_TABLE_STATEMACHINEMETHOD:
				g_assert(i == 0 || i == 1);
				field_size = idx_size(meta, MONO_TABLE_METHOD); break;
			default:
				g_error ("Can't handle MONO_MT_TABLE_IDX for table %d element %d", tableindex, i);
			}
			break;

			/*
			 * HasConstant: ParamDef, FieldDef, Property
			 */
		case MONO_MT_CONST_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_PARAM),
				 get_nrows (meta, MONO_TABLE_FIELD));
			n = MAX (n, get_nrows (meta, MONO_TABLE_PROPERTY));

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

			n = MAX (get_nrows (meta, MONO_TABLE_METHOD),
				 get_nrows (meta, MONO_TABLE_FIELD));
			n = MAX (n, get_nrows (meta, MONO_TABLE_TYPEREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_TYPEDEF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_PARAM));
			n = MAX (n, get_nrows (meta, MONO_TABLE_INTERFACEIMPL));
			n = MAX (n, get_nrows (meta, MONO_TABLE_MEMBERREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_MODULE));
			n = MAX (n, get_nrows (meta, MONO_TABLE_DECLSECURITY));
			n = MAX (n, get_nrows (meta, MONO_TABLE_PROPERTY));
			n = MAX (n, get_nrows (meta, MONO_TABLE_EVENT));
			n = MAX (n, get_nrows (meta, MONO_TABLE_STANDALONESIG));
			n = MAX (n, get_nrows (meta, MONO_TABLE_MODULEREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_TYPESPEC));
			n = MAX (n, get_nrows (meta, MONO_TABLE_ASSEMBLY));
			n = MAX (n, get_nrows (meta, MONO_TABLE_ASSEMBLYREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_FILE));
			n = MAX (n, get_nrows (meta, MONO_TABLE_EXPORTEDTYPE));
			n = MAX (n, get_nrows (meta, MONO_TABLE_MANIFESTRESOURCE));
			n = MAX (n, get_nrows (meta, MONO_TABLE_GENERICPARAM));
			n = MAX (n, get_nrows (meta, MONO_TABLE_GENERICPARAMCONSTRAINT));
			n = MAX (n, get_nrows (meta, MONO_TABLE_METHODSPEC));

			/* 5 bits to encode */
			field_size = rtsize (meta, n, 16-5);
			break;

			/*
			* HasCustomAttribute: points to any table but
			* itself.
			*/

		case MONO_MT_HASCUSTDEBUG_IDX:
			n = MAX(get_nrows (meta, MONO_TABLE_METHOD),
					get_nrows (meta, MONO_TABLE_FIELD));
			n = MAX(n, get_nrows (meta, MONO_TABLE_TYPEREF));
			n = MAX(n, get_nrows (meta, MONO_TABLE_TYPEDEF));
			n = MAX(n, get_nrows (meta, MONO_TABLE_PARAM));
			n = MAX(n, get_nrows (meta, MONO_TABLE_INTERFACEIMPL));
			n = MAX(n, get_nrows (meta, MONO_TABLE_MEMBERREF));
			n = MAX(n, get_nrows (meta, MONO_TABLE_MODULE));
			n = MAX(n, get_nrows (meta, MONO_TABLE_DECLSECURITY));
			n = MAX(n, get_nrows (meta, MONO_TABLE_PROPERTY));
			n = MAX(n, get_nrows (meta, MONO_TABLE_EVENT));
			n = MAX(n, get_nrows (meta, MONO_TABLE_STANDALONESIG));
			n = MAX(n, get_nrows (meta, MONO_TABLE_MODULEREF));
			n = MAX(n, get_nrows (meta, MONO_TABLE_TYPESPEC));
			n = MAX(n, get_nrows (meta, MONO_TABLE_ASSEMBLY));
			n = MAX(n, get_nrows (meta, MONO_TABLE_ASSEMBLYREF));
			n = MAX(n, get_nrows (meta, MONO_TABLE_FILE));
			n = MAX(n, get_nrows (meta, MONO_TABLE_EXPORTEDTYPE));
			n = MAX(n, get_nrows (meta, MONO_TABLE_MANIFESTRESOURCE));
			n = MAX(n, get_nrows (meta, MONO_TABLE_GENERICPARAM));
			n = MAX(n, get_nrows (meta, MONO_TABLE_GENERICPARAMCONSTRAINT));
			n = MAX(n, get_nrows (meta, MONO_TABLE_METHODSPEC));
			n = MAX(n, get_nrows (meta, MONO_TABLE_DOCUMENT));
			n = MAX(n, get_nrows (meta, MONO_TABLE_LOCALSCOPE));
			n = MAX(n, get_nrows (meta, MONO_TABLE_LOCALVARIABLE));
			n = MAX(n, get_nrows (meta, MONO_TABLE_LOCALCONSTANT));
			n = MAX(n, get_nrows (meta, MONO_TABLE_IMPORTSCOPE));

			/* 5 bits to encode */
			field_size = rtsize(meta, n, 16 - 5);
			break;

			/*
			 * CustomAttributeType: MethodDef, MemberRef.
			 */
		case MONO_MT_CAT_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_METHOD),
					 get_nrows (meta, MONO_TABLE_MEMBERREF));

			/* 3 bits to encode */
			field_size = rtsize (meta, n, 16-3);
			break;

			/*
			 * HasDeclSecurity: Typedef, MethodDef, Assembly
			 */
		case MONO_MT_HASDEC_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_TYPEDEF),
				 get_nrows (meta, MONO_TABLE_METHOD));
			n = MAX (n, get_nrows (meta, MONO_TABLE_ASSEMBLY));

			/* 2 bits to encode */
			field_size = rtsize (meta, n, 16-2);
			break;

			/*
			 * Implementation: File, AssemblyRef, ExportedType
			 */
		case MONO_MT_IMPL_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_FILE),
				 get_nrows (meta, MONO_TABLE_ASSEMBLYREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_EXPORTEDTYPE));

			/* 2 bits to encode tag */
			field_size = rtsize (meta, n, 16-2);
			break;

			/*
			 * HasFieldMarshall: FieldDef, ParamDef
			 */
		case MONO_MT_HFM_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_FIELD),
				 get_nrows (meta, MONO_TABLE_PARAM));

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
			break;

			/*
			 * MemberForwarded: FieldDef, MethodDef
			 */
		case MONO_MT_MF_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_FIELD),
				 get_nrows (meta, MONO_TABLE_METHOD));

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
			break;

			/*
			 * TypeDefOrRef: TypeDef, ParamDef, TypeSpec
			 * LAMESPEC
			 * It is TypeDef, _TypeRef_, TypeSpec, instead.
			 */
		case MONO_MT_TDOR_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_TYPEDEF),
				 get_nrows (meta, MONO_TABLE_TYPEREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_TYPESPEC));

			/* 2 bits to encode */
			field_size = rtsize (meta, n, 16-2);
			break;

			/*
			 * MemberRefParent: TypeDef, TypeRef, MethodDef, ModuleRef, TypeSpec, MemberRef
			 */
		case MONO_MT_MRP_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_TYPEDEF),
				 get_nrows (meta, MONO_TABLE_TYPEREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_METHOD));
			n = MAX (n, get_nrows (meta, MONO_TABLE_MODULEREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_TYPESPEC));

			/* 3 bits to encode */
			field_size = rtsize (meta, n, 16 - 3);
			break;

			/*
			 * MethodDefOrRef: MethodDef, MemberRef
			 */
		case MONO_MT_MDOR_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_METHOD),
				 get_nrows (meta, MONO_TABLE_MEMBERREF));

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
			break;

			/*
			 * HasSemantics: Property, Event
			 */
		case MONO_MT_HS_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_PROPERTY),
				 get_nrows (meta, MONO_TABLE_EVENT));

			/* 1 bit used to encode tag */
			field_size = rtsize (meta, n, 16-1);
			break;

			/*
			 * ResolutionScope: Module, ModuleRef, AssemblyRef, TypeRef
			 */
		case MONO_MT_RS_IDX:
			n = MAX (get_nrows (meta, MONO_TABLE_MODULE),
				 get_nrows (meta, MONO_TABLE_MODULEREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_ASSEMBLYREF));
			n = MAX (n, get_nrows (meta, MONO_TABLE_TYPEREF));

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

/* returns true if given index is not in bounds with provided table/index pair */
gboolean
mono_metadata_table_bounds_check_slow (MonoImage *image, int table_index, int token_index)
{
	if (G_LIKELY (GINT_TO_UINT32(token_index) <= table_info_get_rows (&image->tables [table_index])))
		return FALSE;

        if (G_LIKELY (!image->has_updates))
                return TRUE;

        return mono_metadata_update_table_bounds_check (image, table_index, token_index);
}

/**
 * mono_metadata_compute_table_bases:
 * \param meta metadata context to compute table values
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
		if (table_info_get_rows (table) == 0)
			continue;

		table->row_size = mono_metadata_compute_size (meta, i, &table->size_bitfield);
		table->base = base;
		base += table_info_get_rows (table) * table->row_size;
	}
}

/**
 * mono_metadata_locate:
 * \param meta metadata context
 * \param table table code.
 * \param idx index of element to retrieve from \p table.
 *
 * \returns a pointer to the \p idx element in the metadata table
 * whose code is \p table.
 */
const char *
mono_metadata_locate (MonoImage *meta, int table, int idx)
{
	/* FIXME: metadata-update */
	/* idx == 0 refers always to NULL */
	g_return_val_if_fail (idx > 0 && GINT_TO_UINT32(idx) <= table_info_get_rows (&meta->tables [table]), ""); /*FIXME shouldn't we return NULL here?*/

	return meta->tables [table].base + (meta->tables [table].row_size * (idx - 1));
}

/**
 * mono_metadata_locate_token:
 * \param meta metadata context
 * \param token metadata token
 *
 * \returns a pointer to the data in the metadata represented by the
 * token \p token .
 */
const char *
mono_metadata_locate_token (MonoImage *meta, guint32 token)
{
	return mono_metadata_locate (meta, token >> 24, token & 0xffffff);
}

static MonoStreamHeader *
get_string_heap (MonoImage *image)
{
	return &image->heap_strings;
}

static MonoStreamHeader *
get_user_string_heap (MonoImage *image)
{
	return &image->heap_us;
}

static MonoStreamHeader *
get_blob_heap (MonoImage *image)
{
	return &image->heap_blob;
}

static gboolean
mono_delta_heap_lookup (MonoImage *base_image, MetadataHeapGetterFunc get_heap, guint32 orig_index, MonoImage **image_out, guint32 *index_out)
{
        return mono_metadata_update_delta_heap_lookup (base_image, get_heap, orig_index, image_out, index_out);
}

/**
 * mono_metadata_string_heap:
 * \param meta metadata context
 * \param index index into the string heap.
 * \returns an in-memory pointer to the \p index in the string heap.
 */
const char *
mono_metadata_string_heap (MonoImage *meta, guint32 index)
{
	if (G_UNLIKELY (index >= meta->heap_strings.size && meta->has_updates)) {
		MonoImage *dmeta;
		guint32 dindex;
		gboolean ok = mono_delta_heap_lookup (meta, &get_string_heap, index, &dmeta, &dindex);
		g_assertf (ok, "Could not find token=0x%08x in string heap of assembly=%s and its delta images", index, meta && meta->name ? meta->name : "unknown image");
		meta = dmeta;
		index = dindex;
	}

	g_assertf (index < meta->heap_strings.size, " index = 0x%08x size = 0x%08x meta=%s ", index, meta->heap_strings.size, meta && meta->name ? meta->name : "unknown image" );
	g_return_val_if_fail (index < meta->heap_strings.size, "");
	return meta->heap_strings.data + index;
}

/**
 * mono_metadata_string_heap_checked:
 * \param meta metadata context
 * \param index index into the string heap.
 * \param error set on error
 * \returns an in-memory pointer to the \p index in the string heap.
 * On failure returns NULL and sets \p error.
 */
const char *
mono_metadata_string_heap_checked (MonoImage *meta, guint32 index, MonoError *error)
{
	if (mono_image_is_dynamic (meta))
	{
		MonoDynamicImage* img = (MonoDynamicImage*) meta;
		const char *image_name = meta && meta->name ? meta->name : "unknown image";
		if (G_UNLIKELY (!(index < img->sheap.index))) {
			mono_error_set_bad_image_by_name (error, image_name, "string heap index %ud out bounds %u: %s", index, img->sheap.index, image_name);
			return NULL;
		}
		return img->sheap.data + index;
	}

	if (G_UNLIKELY (index >= meta->heap_strings.size && meta->has_updates)) {
		MonoImage *dmeta;
		guint32 dindex;
		gboolean ok = mono_delta_heap_lookup (meta, &get_string_heap, index, &dmeta, &dindex);
		if (G_UNLIKELY (!ok)) {
			const char *image_name = meta && meta->name ? meta->name : "unknown image";
			mono_error_set_bad_image_by_name (error, image_name, "string heap index %ud out bounds %u: %s, also checked delta images", index, meta->heap_strings.size, image_name);

			return NULL;
		}
		meta = dmeta;
		index = dindex;
	}

	if (G_UNLIKELY (!(index < meta->heap_strings.size))) {
		const char *image_name = meta && meta->name ? meta->name : "unknown image";
		mono_error_set_bad_image_by_name (error, image_name, "string heap index %ud out bounds %u: %s", index, meta->heap_strings.size, image_name);
		return NULL;
	}
	return meta->heap_strings.data + index;
}

/**
 * mono_metadata_user_string:
 * \param meta metadata context
 * \param index index into the user string heap.
 * \returns an in-memory pointer to the \p index in the user string heap (<code>#US</code>).
 */
const char *
mono_metadata_user_string (MonoImage *meta, guint32 index)
{
	if (G_UNLIKELY (index >= meta->heap_us.size && meta->has_updates)) {
		MonoImage *dmeta;
		guint32 dindex;
		gboolean ok = mono_delta_heap_lookup (meta, &get_user_string_heap, index, &dmeta, &dindex);
		g_assertf (ok, "Could not find token=0x%08x in user string heap of assembly=%s and its delta images", index, meta && meta->name ? meta->name : "unknown image");
		meta = dmeta;
		index = dindex;
	}
	g_assert (index < meta->heap_us.size);
	g_return_val_if_fail (index < meta->heap_us.size, "");
	return meta->heap_us.data + index;
}

/**
 * mono_metadata_blob_heap:
 * \param meta metadata context
 * \param index index into the blob.
 * \returns an in-memory pointer to the \p index in the Blob heap.
 */
const char *
mono_metadata_blob_heap (MonoImage *meta, guint32 index)
{
	/* Some tools can produce assemblies with a size 0 Blob stream. If a
	 * blob value is optional, if the index == 0 and heap_blob.size == 0
	 * assertion is hit, consider updating caller to use
	 * mono_metadata_blob_heap_null_ok and handling a null return value. */
	g_assert (!(index == 0 && meta->heap_blob.size == 0));
	if (G_UNLIKELY (index >= meta->heap_blob.size && meta->has_updates)) {
		MonoImage *dmeta;
		guint32 dindex;
		gboolean ok = mono_delta_heap_lookup (meta, &get_blob_heap, index, &dmeta, &dindex);
		g_assertf (ok, "Could not find token=0x%08x in blob heap of assembly=%s and its delta images", index, meta && meta->name ? meta->name : "unknown image");
		meta = dmeta;
		index = dindex;
	}
	g_assert (index < meta->heap_blob.size);
	return meta->heap_blob.data + index;
}

/**
 * mono_metadata_blob_heap_null_ok:
 * \param meta metadata context
 * \param index index into the blob.
 * \return an in-memory pointer to the \p index in the Blob heap.
 * If the Blob heap is empty or missing and index is 0 returns NULL, instead of asserting.
 */
const char *
mono_metadata_blob_heap_null_ok (MonoImage *meta, guint32 index)
{
	if (G_UNLIKELY (index == 0 && meta->heap_blob.size == 0))
		return NULL;
	else
		return mono_metadata_blob_heap (meta, index);
}

/**
 * mono_metadata_blob_heap_checked:
 * \param meta metadata context
 * \param index index into the blob.
 * \param error set on error
 * \returns an in-memory pointer to the \p index in the Blob heap.  On failure sets \p error and returns NULL;
 * If the Blob heap is empty or missing and \p index is 0 returns NULL, without setting error.
 *
 */
const char *
mono_metadata_blob_heap_checked (MonoImage *meta, guint32 index, MonoError *error)
{
	if (mono_image_is_dynamic (meta)) {
		MonoDynamicImage* img = (MonoDynamicImage*) meta;
		const char *image_name = meta && meta->name ? meta->name : "unknown image";
		if (G_UNLIKELY (!(index < img->blob.index))) {
			mono_error_set_bad_image_by_name (error, image_name, "blob heap index %u out of bounds %u: %s", index, img->blob.index, image_name);
			return NULL;
		}
		if (G_UNLIKELY (index == 0 && img->blob.alloc_size == 0))
			return NULL;
		return img->blob.data + index;
	}
	if (G_UNLIKELY (index == 0 && meta->heap_blob.size == 0))
		return NULL;
	if (G_UNLIKELY (index >= meta->heap_blob.size && meta->has_updates)) {
		MonoImage *dmeta;
		guint32 dindex;
		gboolean ok = mono_delta_heap_lookup (meta, &get_blob_heap, index, &dmeta, &dindex);
		if (G_UNLIKELY(!ok)) {
			const char *image_name = meta && meta->name ? meta->name : "unknown image";
			mono_error_set_bad_image_by_name (error, image_name, "Could not find token=0x%08x in blob heap of assembly=%s and its delta images", index, image_name);
			return NULL;
		}
		meta = dmeta;
		index = dindex;
	}
	if (G_UNLIKELY (!(index < meta->heap_blob.size))) {
		const char *image_name = meta && meta->name ? meta->name : "unknown image";
		mono_error_set_bad_image_by_name (error, image_name, "blob heap index %u out of bounds %u: %s", index, meta->heap_blob.size, image_name);
		return NULL;
	}
	return meta->heap_blob.data + index;
}

/**
 * mono_metadata_guid_heap:
 * \param meta metadata context
 * \param index index into the guid heap.
 * \returns an in-memory pointer to the \p index in the guid heap.
 */
const char *
mono_metadata_guid_heap (MonoImage *meta, guint32 index)
{
	/* EnC TODO: lookup in DeltaInfo:delta_image_last.  Unlike the other heaps, the GUID heaps are always full in every delta, even in minimal delta images. */
	--index;
	index *= 16; /* adjust for guid size and 1-based index */
	g_return_val_if_fail (index < meta->heap_guid.size, "");
	return meta->heap_guid.data + index;
}

static const unsigned char *
dword_align (const unsigned char *ptr)
{
	return (const unsigned char *) (((gsize) (ptr + 3)) & ~3);
}

static void
mono_metadata_decode_row_slow (const MonoTableInfo *t, int idx, guint32 *res, int res_size);

/**
 * mono_metadata_decode_row:
 * \param t table to extract information from.
 * \param idx index in table.
 * \param res array of \p res_size cols to store the results in
 *
 * This decompresses the metadata element \p idx in table \p t
 * into the \c guint32 \p res array that has \p res_size elements
 */
void
mono_metadata_decode_row (const MonoTableInfo *t, int idx, guint32 *res, int res_size)
{
	if (G_UNLIKELY (mono_metadata_has_updates ())) {
		mono_metadata_decode_row_slow (t, idx, res, res_size);
	} else {
		mono_metadata_decode_row_raw (t, idx, res, res_size);
	}
}

void
mono_metadata_decode_row_slow (const MonoTableInfo *t, int idx, guint32 *res, int res_size)
{
	g_assert (idx >= 0);
	mono_image_effective_table (&t, idx);
	mono_metadata_decode_row_raw (t, idx, res, res_size);
}

/**
 * same as mono_metadata_decode_row, but ignores potential delta images
 */
void
mono_metadata_decode_row_raw (const MonoTableInfo *t, int idx, guint32 *res, int res_size)
{
	guint32 bitfield = t->size_bitfield;
	int i, count = mono_metadata_table_count (bitfield);
	const char *data;

	g_assert (GINT_TO_UINT32(idx) < table_info_get_rows (t));
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
 * mono_metadata_decode_row_checked:
 * \param image the \c MonoImage the table belongs to
 * \param t table to extract information from.
 * \param idx index in the table.
 * \param res array of \p res_size cols to store the results in
 * \param error set on bounds error
 *
 *
 * This decompresses the metadata element \p idx in the table \p t
 * into the \c guint32 \p res array that has \p res_size elements.
 *
 * \returns TRUE if the read succeeded. Otherwise sets \p error and returns FALSE.
 */
gboolean
mono_metadata_decode_row_checked (const MonoImage *image, const MonoTableInfo *t, int idx, guint32 *res, int res_size, MonoError *error)
{
	const char *image_name = image && image->name ? image->name : "unknown image";

	g_assert (idx >= 0);
	mono_image_effective_table (&t, idx);

	guint32 bitfield = t->size_bitfield;
	int i, count = mono_metadata_table_count (bitfield);

	if (G_UNLIKELY (! (GINT_TO_UINT32(idx) < table_info_get_rows (t) && idx >= 0))) {
		mono_error_set_bad_image_by_name (error, image_name, "row index %d out of bounds: %d rows: %s", idx, table_info_get_rows (t), image_name);
		return FALSE;
	}
	const char *data = t->base + idx * t->row_size;

	if (G_UNLIKELY (res_size != count)) {
		mono_error_set_bad_image_by_name (error, image_name, "res_size %d != count %d: %s", res_size, count, image_name);
		return FALSE;
	}

	for (i = 0; i < count; i++) {
		int n = mono_metadata_table_size (bitfield, i);

		switch (n) {
		case 1:
			res [i] = *data; break;
		case 2:
			res [i] = read16 (data); break;
		case 4:
			res [i] = read32 (data); break;
		default:
			mono_error_set_bad_image_by_name (error, image_name, "unexpected table [%d] size %d: %s", i, n, image_name);
			return FALSE;
		}
		data += n;
	}

	return TRUE;
}

gboolean
mono_metadata_decode_row_dynamic_checked (const MonoDynamicImage *image, const MonoDynamicTable *t, guint idx, guint32 *res, int res_size, MonoError *error)
{
	int i, count = t->columns;

	const char *image_name = image && image->image.name ? image->image.name : "unknown image";

	if (G_UNLIKELY (! (idx < t->rows && idx >= 0))) {
		mono_error_set_bad_image_by_name (error, image_name, "row index %d out of bounds: %d rows: %s", idx, t->rows, image_name);
		return FALSE;
	}
	guint32 *data = t->values + (idx + 1) * count;

	if (G_UNLIKELY (res_size != count)) {
		mono_error_set_bad_image_by_name (error, image_name, "res_size %d != count %d: %s", res_size, count, image_name);
		return FALSE;
	}

	for (i = 0; i < count; i++) {
		res [i] = *data;
		data++;
	}

	return TRUE;
}

static guint32
mono_metadata_decode_row_col_raw (const MonoTableInfo *t, int idx, guint col);
static guint32
mono_metadata_decode_row_col_slow (const MonoTableInfo *t, int idx, guint col);

/**
 * mono_metadata_decode_row_col:
 * \param t table to extract information from.
 * \param idx index for row in table.
 * \param col column in the row.
 *
 * This function returns the value of column \p col from the \p idx
 * row in the table \p t .
 */
guint32
mono_metadata_decode_row_col (const MonoTableInfo *t, int idx, guint col)
{
	if (G_UNLIKELY (mono_metadata_has_updates ())) {
		return mono_metadata_decode_row_col_slow (t, idx, col);
	} else {
		return mono_metadata_decode_row_col_raw (t, idx, col);
	}
}

guint32
mono_metadata_decode_row_col_slow (const MonoTableInfo *t, int idx, guint col)
{
	g_assert (idx >= 0);
	mono_image_effective_table (&t, idx);
	return mono_metadata_decode_row_col_raw (t, idx, col);
}

/**
 * mono_metadata_decode_row_col_raw:
 *
 * Same as \c mono_metadata_decode_row_col but doesn't look for the effective
 * table on metadata updates.
 */
guint32
mono_metadata_decode_row_col_raw (const MonoTableInfo *t, int idx, guint col)
{
	const char *data;
	int n;

	guint32 bitfield = t->size_bitfield;

	g_assert (GINT_TO_UINT32(idx) < table_info_get_rows (t));
	g_assert (col < mono_metadata_table_count (bitfield));
	data = t->base + idx * t->row_size;

	n = mono_metadata_table_size (bitfield, 0);
	for (guint i = 0; i < col; ++i) {
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
 * \param ptr pointer to a blob object
 * \param rptr the new position of the pointer
 *
 * This decodes a compressed size as described by 24.2.4 (#US and #Blob a blob or user string object)
 *
 * \returns the size of the blob object
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
 * \param ptr pointer to decode from
 * \param rptr the new position of the pointer
 *
 * This routine decompresses 32-bit values as specified in the "Blob and
 * Signature" section (23.2)
 *
 * \returns the decoded value
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
 * \param ptr pointer to decode from
 * \param rptr the new position of the pointer
 *
 * This routine decompresses 32-bit signed values
 * (not specified in the spec)
 *
 * \returns the decoded value
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

/**
 * mono_metadata_translate_token_index:
 * Translates the given 1-based index into the \c Method, \c Field, \c Event, or \c Param tables
 * using the \c *Ptr tables in uncompressed metadata, if they are available.
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
		if (table_info_get_rows (&image->tables [MONO_TABLE_METHOD_POINTER]))
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_METHOD_POINTER], idx - 1, MONO_METHOD_POINTER_METHOD);
		else
			return idx;
	case MONO_TABLE_FIELD:
		if (table_info_get_rows (&image->tables [MONO_TABLE_FIELD_POINTER]))
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_FIELD_POINTER], idx - 1, MONO_FIELD_POINTER_FIELD);
		else
			return idx;
	case MONO_TABLE_EVENT:
		if (table_info_get_rows (&image->tables [MONO_TABLE_EVENT_POINTER]))
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_EVENT_POINTER], idx - 1, MONO_EVENT_POINTER_EVENT);
		else
			return idx;
	case MONO_TABLE_PROPERTY:
		if (table_info_get_rows (&image->tables [MONO_TABLE_PROPERTY_POINTER]))
			return mono_metadata_decode_row_col (&image->tables [MONO_TABLE_PROPERTY_POINTER], idx - 1, MONO_PROPERTY_POINTER_PROPERTY);
		else
			return idx;
	case MONO_TABLE_PARAM:
		if (table_info_get_rows (&image->tables [MONO_TABLE_PARAM_POINTER]))
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
 * Same as \c mono_metadata_decode_row, but takes an \p image + \p table ID pair, and takes
 * uncompressed metadata into account, so it should be used to access the
 * \c Method, \c Field, \c Param and \c Event tables when the access is made from metadata, i.e.
 * \p idx is retrieved from a metadata table, like \c MONO_TYPEDEF_FIELD_LIST.
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
 * Same as \c mono_metadata_decode_row_col, but takes an \p image + \p table ID pair, and takes
 * uncompressed metadata into account, so it should be used to access the
 * \c Method, \c Field, \c Param and \c Event tables.
 */
guint32 mono_metadata_decode_table_row_col (MonoImage *image, int table, int idx, guint col)
{
	if (image->uncompressed_metadata)
		idx = mono_metadata_translate_token_index (image, table, idx + 1) - 1;

	return mono_metadata_decode_row_col (&image->tables [table], idx, col);
}

/**
 * mono_metadata_parse_typedef_or_ref:
 * \param m a metadata context.
 * \param ptr a pointer to an encoded TypedefOrRef in \p m
 * \param rptr pointer updated to match the end of the decoded stream
 * \returns a token valid in the \p m metadata decoded from
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

/**
 * mono_metadata_parse_custom_mod:
 * \param m a metadata context.
 * \param dest storage where the info about the custom modifier is stored (may be NULL)
 * \param ptr a pointer to (possibly) the start of a custom modifier list
 * \param rptr pointer updated to match the end of the decoded stream
 *
 * Checks if \p ptr points to a type custom modifier compressed representation.
 *
 * \returns TRUE if a custom modifier was found, FALSE if not.
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
									gboolean transient, const char *ptr, const char **rptr, MonoError *error)
{
	int i;
	MonoArrayType *array;
	MonoType *etype;

	etype = mono_metadata_parse_type_checked (m, container, 0, FALSE, ptr, &ptr, error); //FIXME this doesn't respect @transient
	if (!etype)
		return NULL;

	array = transient ? (MonoArrayType *)g_malloc0 (sizeof (MonoArrayType)) : (MonoArrayType *)mono_image_alloc0 (m, sizeof (MonoArrayType));
	array->eklass = mono_class_from_mono_type_internal (etype);
	array->rank = GUINT32_TO_UINT8 (mono_metadata_decode_value (ptr, &ptr));

	array->numsizes = GUINT32_TO_UINT8 (mono_metadata_decode_value (ptr, &ptr));
	if (array->numsizes)
		array->sizes = transient ? (int *)g_malloc0 (sizeof (int) * array->numsizes) : (int *)mono_image_alloc0 (m, sizeof (int) * array->numsizes);
	for (i = 0; i < array->numsizes; ++i)
		array->sizes [i] = mono_metadata_decode_value (ptr, &ptr);

	array->numlobounds = GUINT32_TO_UINT8 (mono_metadata_decode_value (ptr, &ptr));
	if (array->numlobounds)
		array->lobounds = transient ? (int *)g_malloc0 (sizeof (int) * array->numlobounds) : (int *)mono_image_alloc0 (m, sizeof (int) * array->numlobounds);
	for (i = 0; i < array->numlobounds; ++i)
		array->lobounds [i] = mono_metadata_decode_signed_value (ptr, &ptr);

	if (rptr)
		*rptr = ptr;
	return array;
}

/**
 * mono_metadata_parse_array:
 */
MonoArrayType *
mono_metadata_parse_array (MonoImage *m, const char *ptr, const char **rptr)
{
	ERROR_DECL (error);
	MonoArrayType *ret = mono_metadata_parse_array_internal (m, NULL, FALSE, ptr, rptr, error);
	mono_error_cleanup (error);

	return ret;
}

/**
 * mono_metadata_free_array:
 * \param array array description
 *
 * Frees the array description returned from \c mono_metadata_parse_array.
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

static GHashTable *type_cache = NULL;
static gint32 next_generic_inst_id = 0;

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
		return type->type | ((m_type_is_byref (type) ? 1 : 0) << 8) | (type->attrs << 9);
}

static gint
mono_type_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoType *a = (const MonoType *) ka;
	const MonoType *b = (const MonoType *) kb;

	if (a->type != b->type || m_type_is_byref (a) != m_type_is_byref (b) || a->attrs != b->attrs || a->pinned != b->pinned)
		return 0;
	/* need other checks */
	return 1;
}

guint
mono_metadata_generic_inst_hash (gconstpointer data)
{
	const MonoGenericInst *ginst = (const MonoGenericInst *) data;
	guint hash = 0;
	g_assert (ginst);
	g_assert (ginst->type_argv);

	for (guint i = 0; i < ginst->type_argc; ++i) {
		hash *= 13;
		g_assert (ginst->type_argv [i]);
		hash += mono_metadata_type_hash (ginst->type_argv [i]);
	}

	return hash ^ (ginst->is_open << 8);
}

static gboolean
mono_generic_inst_equal_full (const MonoGenericInst *a, const MonoGenericInst *b, gboolean signature_only)
{
	// An optimization: if the ids of two insts are the same, we know they are the same inst and don't check contents.
	// Furthermore, because we perform early de-duping, if the ids differ, we know the contents differ.
#ifndef MONO_SMALL_CONFIG // Optimization does not work in MONO_SMALL_CONFIG: There are no IDs
	if (a->id && b->id) { // "id 0" means "object has no id"-- de-duping hasn't been performed yet, must check contents.
		if (a->id == b->id)
			return TRUE;
		// In signature-comparison mode id equality implies object equality, but this is not true for inequality.
		// Two separate objects could have signature-equavalent contents.
		if (!signature_only)
			return FALSE;
	}
#endif

	if (a->is_open != b->is_open || a->type_argc != b->type_argc)
		return FALSE;
	for (guint i = 0; i < a->type_argc; ++i) {
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
	guint hash = mono_metadata_type_hash (m_class_get_byval_arg (gclass->container_class));

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

	/* We guard against double initialization due to how pedump in verification mode works.
	Until runtime initialization is properly factored to work with what it needs we need workarounds like this.
	FIXME: https://bugzilla.xamarin.com/show_bug.cgi?id=58793
	*/
	static gboolean inited;

	if (inited)
		return;
	inited = TRUE;

	type_cache = g_hash_table_new (mono_type_hash, mono_type_equal);

	for (i = 0; i < G_N_ELEMENTS (builtin_types); ++i)
		g_hash_table_insert (type_cache, (gpointer) &builtin_types [i], (gpointer) &builtin_types [i]);

	mono_metadata_update_init ();
}

/*
 * Make a pass over the metadata signature blob starting at \p tmp_ptr and count the custom modifiers.
 */
static int
count_custom_modifiers (MonoImage *m, const char *tmp_ptr)
{
	int count = 0;
	gboolean found = TRUE;
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
	return count;
}

/*
 * Decode the (expected \p count, possibly 0) custom modifiers as well as the "byref" and "pinned"
 * markers from the metadata stream \p ptr and put them into \p cmods
 *
 * Sets \p rptr past the end of the parsed metadata.  Sets \p pinned and \p byref if those modifiers
 * were present.
 */
static void
decode_custom_modifiers (MonoImage *m, MonoCustomModContainer *cmods, int count, const char *ptr, const char **rptr, gboolean *pinned, gboolean *byref)
{
	gboolean found = TRUE;
	/* cmods are encoded in reverse order from how we normally see them.
	 * "int32 modopt (Foo) modopt (Bar)" is encoded as "cmod_opt [typedef_or_ref "Bar"] cmod_opt [typedef_or_ref "Foo"] I4"
	 */
	while (found) {
		switch (*ptr) {
		case MONO_TYPE_PINNED:
			*pinned = TRUE;
			++ptr;
			break;
		case MONO_TYPE_BYREF:
			*byref = TRUE;
			++ptr;
			break;
		case MONO_TYPE_CMOD_REQD:
		case MONO_TYPE_CMOD_OPT:
			g_assert (count > 0);
			mono_metadata_parse_custom_mod (m, &(cmods->modifiers [--count]), ptr, &ptr);
			break;
		default:
			found = FALSE;
		}
	}

	// either there were no cmods, or else we iterated through all of cmods backwards to populate it.
	g_assert (count == 0);
	*rptr = ptr;
}

/*
 * Allocate the memory necessary to hold a \c MonoType with \p count custom modifiers.
 * If \p transient is true, allocate from the heap, otherwise allocate from the mempool of image \p m
 */
static MonoType *
alloc_type_with_cmods (MonoImage *m, gboolean transient, int count)
{
	g_assert (count > 0 && count <= G_MAXUINT8);

	MonoType *type;
	uint8_t count8 = GINT_TO_UINT8 (count);
	size_t size = mono_sizeof_type_with_mods (count8, FALSE);
	type = transient ? (MonoType *)g_malloc0 (size) : (MonoType *)mono_image_alloc0 (m, (guint)size);
	type->has_cmods = TRUE;

	MonoCustomModContainer *cmods = mono_type_get_cmods (type);
	cmods->count = count8;
	cmods->image = m;

	return type;
}

/*
 * If \p transient is true, free \p type, otherwise no-op
 */
static void
free_parsed_type (MonoType *type, gboolean transient)
{
	if (transient)
		mono_metadata_free_type (type);
}

/*
 * Try to find a pre-allocated version of the given \p type.
 * Returns true and sets \p canonical_type if found, otherwise return false.
 *
 * For classes and valuetypes, this returns their embedded byval_arg or
 * this_arg types.  For base types, it returns the global versions.
 */
static gboolean
try_get_canonical_type (MonoType *type, MonoType **canonical_type)
{
	/* Note: If the type has any attribtues or modifiers the function currently returns false,
	 * although there's no fundamental reason we can't have cached copies in those instances (or
	 * indeed cached arrays, pointers or some generic instances).  However in that case there's
	 * limited utility in returning a cached copy because the parsing code in
	 * do_mono_metadata_parse_type could have allocated some mempool or heap memory already.
	 *
	 * This function should be kept closely in sync with mono_metadata_free_type so that it
	 * doesn't try to free canonical MonoTypes (which might not even be heap allocated).
	 */
	g_assert (!type->has_cmods);
	if ((type->type == MONO_TYPE_CLASS || type->type == MONO_TYPE_VALUETYPE) && !type->pinned && !type->attrs) {
		MonoType *ret = m_type_is_byref (type) ? m_class_get_this_arg (type->data.klass) : m_class_get_byval_arg (type->data.klass);

		/* Consider the case:

		   class Foo<T> { class Bar {} }
		   class Test : Foo<Test>.Bar {}

		   When Foo<Test> is being expanded, 'Test' isn't yet initialized.  It's actually in
		   a really pristine state: it doesn't even know whether 'Test' is a reference or a value type.

		   We ensure that the MonoClass is in a state that we can canonicalize to:

		   klass->_byval_arg.data.klass == klass
		   klass->this_arg.data.klass == klass

		   If we can't canonicalize 'type', it doesn't matter, since later users of 'type' will do it.

		   LOCKING: even though we don't explicitly hold a lock, in the problematic case 'ret' is a field
		   of a MonoClass which currently holds the loader lock.  'type' is local.
		*/
		if (ret->data.klass == type->data.klass) {
			*canonical_type = ret;
			return TRUE;
		}
	}

	/* Maybe it's one of the globaly-known basic types */
	MonoType *cached;
	/* No need to use locking since nobody is modifying the hash table */
	if ((cached = (MonoType *)g_hash_table_lookup (type_cache, type))) {
		*canonical_type = cached;
		return TRUE;
	}

	return FALSE;
}

/*
 * Fill in \p type (expecting \p cmod_count custom modifiers) by parsing it from the metadata stream pointed at by \p ptr.
 *
 * On success returns true and sets \p rptr past the parsed stream data.  On failure return false and sets \p error.
 */
static gboolean
do_mono_metadata_parse_type_with_cmods (MonoType *type, int cmod_count, MonoImage *m, MonoGenericContainer *container,
					guint32 opt_attrs, gboolean transient, const char *ptr, const char **rptr, MonoError *error)
{
	gboolean byref= FALSE;
	gboolean pinned = FALSE;

	error_init (error);

	/* Iterate again, but now parse pinned, byref and custom modifiers */
	decode_custom_modifiers (m, mono_type_get_cmods (type), cmod_count, ptr, &ptr, &pinned, &byref);

	type->attrs = opt_attrs;
	type->byref__ = byref;
	type->pinned = pinned ? 1 : 0;

	if (!do_mono_metadata_parse_type (type, m, container, transient, ptr, &ptr, error))
		return FALSE;

	if (rptr)
		*rptr = ptr;
	return TRUE;
}

MONO_DISABLE_WARNING(4701) /* potentially uninitialized local variable 'stype' used */

/**
 * mono_metadata_parse_type:
 * \param m metadata context
 * \param mode kind of type that may be found at \p ptr
 * \param opt_attrs optional attributes to store in the returned type
 * \param ptr pointer to the type representation
 * \param rptr pointer updated to match the end of the decoded stream
 * \param transient whenever to allocate the result from the heap or from a mempool
 *
 * Decode a compressed type description found at \p ptr in \p m .
 * \p mode can be one of \c MONO_PARSE_MOD_TYPE, \c MONO_PARSE_PARAM, \c MONO_PARSE_RET,
 * \c MONO_PARSE_FIELD, \c MONO_PARSE_LOCAL, \c MONO_PARSE_TYPE.
 * This function can be used to decode type descriptions in method signatures,
 * field signatures, locals signatures etc.
 *
 * To parse a generic type, \c generic_container points to the current class'es
 * (the \c generic_container field in the <code>MonoClass</code>) or the current generic method's
 * (stored in <code>image->property_hash</code>) generic container.
 * When we encounter a \c MONO_TYPE_VAR or \c MONO_TYPE_MVAR, it's looked up in
 * this \c MonoGenericContainer.
 *
 * LOCKING: Acquires the loader lock.
 *
 * \returns a \c MonoType structure representing the decoded type.
 */
static MonoType*
mono_metadata_parse_type_internal (MonoImage *m, MonoGenericContainer *container,
								   guint32 opt_attrs, gboolean transient, const char *ptr, const char **rptr, MonoError *error)
{
	MonoType *type;
	MonoType stype;
	int count = 0; // Number of mod arguments

	gboolean allocated = FALSE;

	error_init (error);

	/*
	 * Q: What's going on with `stype` and `allocated`?  A: A very common case is that we're
	 * parsing "int" or "string" or "Dictionary<K,V>" non-transiently.  In that case we don't
	 * want to flood the mempool with millions of copies of MonoType 'int' (etc).  So we parse
	 * it into a stack variable and try_get_canonical_type, below.  As long as the type is
	 * normal, we will avoid having to make an extra copy in the mempool.
	 */

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
	count = count_custom_modifiers (m, ptr);

	if (count) { // There are mods, so the MonoType will be of nonstandard size.
		allocated = TRUE;
		if (count > 64) {
			mono_error_set_bad_image (error, m, "Invalid type with more than 64 modifiers");
			return NULL;
		}
		type = alloc_type_with_cmods (m, transient, count);
	} else {     // The type is of standard size, so we can allocate it on the stack.
		type = &stype;
		memset (type, 0, MONO_SIZEOF_TYPE);
	}

	if (!do_mono_metadata_parse_type_with_cmods (type, count, m, container, opt_attrs, transient, ptr, rptr, error)) {
		if (allocated)
			free_parsed_type (type, transient);
		return NULL;
	}


	// Possibly we can return an already-allocated type instead of the one we decoded
	if (!allocated && !transient) {
		/* no need to free type here, because it is on the stack */
		MonoType *ret_type = NULL;
		if (try_get_canonical_type (type, &ret_type))
			return ret_type;
	}

	/* printf ("%x %x %c %s\n", type->attrs, type->num_mods, type->pinned ? 'p' : ' ', mono_type_full_name (type)); */

	// Otherwise return the type we decoded
	if (!allocated) { // Type was allocated on the stack, so we need to copy it to safety
		type = transient ? (MonoType *)g_malloc (MONO_SIZEOF_TYPE) : (MonoType *)mono_image_alloc (m, MONO_SIZEOF_TYPE);
		memcpy (type, &stype, MONO_SIZEOF_TYPE);
	}
	g_assert (type != &stype);
	return type;
}

MONO_RESTORE_WARNING

MonoType*
mono_metadata_parse_type_checked (MonoImage *m, MonoGenericContainer *container,
							   guint32 opt_attrs, gboolean transient, const char *ptr, const char **rptr, MonoError *error)
{
	return mono_metadata_parse_type_internal (m, container, opt_attrs, transient, ptr, rptr, error);
}

/*
 * LOCKING: Acquires the loader lock.
 */
MonoType*
mono_metadata_parse_type (MonoImage *m, MonoParseTypeMode mode, short opt_attrs,
			  const char *ptr, const char **rptr)
{
	ERROR_DECL (error);
	MonoType * type = mono_metadata_parse_type_internal (m, NULL, opt_attrs, FALSE, ptr, rptr, error);
	mono_error_cleanup (error);
	return type;
}

gboolean
mono_metadata_method_has_param_attrs (MonoImage *m, int def)
{
	MonoTableInfo *paramt = &m->tables [MONO_TABLE_PARAM];
	MonoTableInfo *methodt = &m->tables [MONO_TABLE_METHOD];
	guint lastp, i, param_index = mono_metadata_decode_row_col (methodt, def - 1, MONO_METHOD_PARAMLIST);

	if (param_index == 0)
		return FALSE;

	/* FIXME: metadata-update */
	if (GINT_TO_UINT32(def) < table_info_get_rows (methodt))
		lastp = mono_metadata_decode_row_col (methodt, def, MONO_METHOD_PARAMLIST);
	else
		lastp = table_info_get_rows (&m->tables [MONO_TABLE_PARAM]) + 1;

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
mono_metadata_get_param_attrs (MonoImage *m, int def, guint32 param_count)
{
	MonoTableInfo *paramt = &m->tables [MONO_TABLE_PARAM];
	MonoTableInfo *methodt = &m->tables [MONO_TABLE_METHOD];
	guint32 cols [MONO_PARAM_SIZE];
	guint lastp, i, param_index = mono_metadata_decode_row_col (methodt, def - 1, MONO_METHOD_PARAMLIST);
	int *pattrs = NULL;

	/* hot reload deltas may specify 0 for the param table index */
	if (param_index == 0)
		return NULL;

	/* FIXME: metadata-update */
	if (GINT_TO_UINT32(def) < mono_metadata_table_num_rows (m, MONO_TABLE_METHOD))
		lastp = mono_metadata_decode_row_col (methodt, def, MONO_METHOD_PARAMLIST);
	else
		lastp = table_info_get_rows (paramt) + 1;

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


/**
 * mono_metadata_parse_signature:
 * \param image metadata context
 * \param token metadata token
 *
 * Decode a method signature stored in the \c StandAloneSig table
 *
 * \returns a \c MonoMethodSignature describing the signature.
 */
MonoMethodSignature*
mono_metadata_parse_signature (MonoImage *image, guint32 token)
{
	ERROR_DECL (error);
	MonoMethodSignature *ret;
	ret = mono_metadata_parse_signature_checked (image, token, error);
	mono_error_cleanup (error);
	return ret;
}

/*
 * mono_metadata_parse_signature_checked:
 * @image: metadata context
 * @token: metadata token
 * @error: set on error
 *
 * Decode a method signature stored in the STANDALONESIG table
 *
 * Returns: a MonoMethodSignature describing the signature. On failure
 * returns NULL and sets @error.
 */
MonoMethodSignature*
mono_metadata_parse_signature_checked (MonoImage *image, guint32 token, MonoError *error)
{

	error_init (error);
	MonoTableInfo *tables = image->tables;
	guint32 idx = mono_metadata_token_index (token);
	guint32 sig;
	const char *ptr;

	if (image_is_dynamic (image)) {
		return (MonoMethodSignature *)mono_lookup_dynamic_token (image, token, NULL, error);
	}

	g_assert (mono_metadata_token_table(token) == MONO_TABLE_STANDALONESIG);

	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	mono_metadata_decode_blob_size (ptr, &ptr);

	return mono_metadata_parse_method_signature_full (image, NULL, 0, ptr, NULL, error);
}

/**
 * mono_metadata_signature_alloc:
 * \param image metadata context
 * \param nparams number of parameters in the signature
 *
 * Allocate a \c MonoMethodSignature structure with the specified number of params.
 * The return type and the params types need to be filled later.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * \returns the new \c MonoMethodSignature structure.
 */
MonoMethodSignature*
mono_metadata_signature_alloc (MonoImage *m, guint32 nparams)
{
	MonoMethodSignature *sig;

	sig = (MonoMethodSignature *)mono_image_alloc0 (m, MONO_SIZEOF_METHOD_SIGNATURE + ((gint32)nparams) * sizeof (MonoType*));
	sig->param_count = GUINT32_TO_UINT16 (nparams);
	sig->sentinelpos = -1;

	return sig;
}

static MonoMethodSignature*
mono_metadata_signature_dup_internal (MonoImage *image, MonoMemPool *mp, MonoMemoryManager *mem_manager,
									  MonoMethodSignature *sig, size_t padding)
{
	size_t sigsize, sig_header_size;
	MonoMethodSignature *ret;
	sigsize = sig_header_size = MONO_SIZEOF_METHOD_SIGNATURE + sig->param_count * sizeof (MonoType *) + padding;
	if (sig->ret)
		sigsize += mono_sizeof_type (sig->ret);

	if (image) {
		ret = (MonoMethodSignature *)mono_image_alloc (image, (guint)sigsize);
	} else if (mp) {
		ret = (MonoMethodSignature *)mono_mempool_alloc (mp, (unsigned int)sigsize);
	} else if (mem_manager) {
		ret = (MonoMethodSignature *)mono_mem_manager_alloc (mem_manager, (guint)sigsize);
	} else {
		ret = (MonoMethodSignature *)g_malloc (sigsize);
	}

	memcpy (ret, sig, sig_header_size - padding);

	// Copy return value because of ownership semantics.
	if (sig->ret) {
		// Danger! Do not alter padding use without changing the dup_add_this below
		intptr_t end_of_header = (intptr_t)( (char*)(ret) + sig_header_size);
		ret->ret = (MonoType *)end_of_header;
		memcpy (ret->ret, sig->ret, mono_sizeof_type (sig->ret));
	}

	return ret;
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
	ret = mono_metadata_signature_dup_internal (image, NULL, NULL, sig, sizeof (MonoType *));

	ret->param_count = sig->param_count + 1;
	ret->hasthis = FALSE;

	for (int i = sig->param_count - 1; i >= 0; i --)
		ret->params [i + 1] = sig->params [i];
	ret->params [0] = m_class_is_valuetype (klass) ? m_class_get_this_arg (klass) : m_class_get_byval_arg (klass);

	for (int i = sig->param_count - 1; i >= 0; i --)
		g_assert(ret->params [i + 1]->type == sig->params [i]->type && ret->params [i+1]->type != MONO_TYPE_END);
	g_assert (ret->ret->type == sig->ret->type && ret->ret->type != MONO_TYPE_END);

	return ret;
}

MonoMethodSignature*
mono_metadata_signature_dup_full (MonoImage *image, MonoMethodSignature *sig)
{
	MonoMethodSignature *ret = mono_metadata_signature_dup_internal (image, NULL, NULL, sig, 0);

	for (int i = 0 ; i < sig->param_count; i ++)
		g_assert (ret->params [i]->type == sig->params [i]->type);
	g_assert (ret->ret->type == sig->ret->type);

	return ret;
}

/*The mempool is accessed without synchronization*/
MonoMethodSignature*
mono_metadata_signature_dup_mempool (MonoMemPool *mp, MonoMethodSignature *sig)
{
	return mono_metadata_signature_dup_internal (NULL, mp, NULL, sig, 0);
}

MonoMethodSignature*
mono_metadata_signature_dup_mem_manager (MonoMemoryManager *mem_manager, MonoMethodSignature *sig)
{
	return mono_metadata_signature_dup_internal (NULL, NULL, mem_manager, sig, 0);
}

/**
 * mono_metadata_signature_dup:
 * \param sig method signature
 *
 * Duplicate an existing \c MonoMethodSignature so it can be modified.
 * This is a Mono runtime internal function.
 *
 * \returns the new \c MonoMethodSignature structure.
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

/**
 * metadata_signature_set_modopt_call_conv:
 *
 * Reads the custom attributes from \p cmod_type and adds them to the signature \p sig.
 *
 * This follows the C# unmanaged function pointer encoding.
 * The modopts are from the System.Runtime.CompilerServices namespace and all have a name of the form CallConvXXX.
 *
 * The calling convention will be one of:
 * Cdecl, Thiscall, Stdcall, Fastcall
 * plus an optional SuppressGCTransition
 */
static void
metadata_signature_set_modopt_call_conv (MonoMethodSignature *sig, MonoType *cmod_type, MonoError *error)
{
	uint8_t count = mono_type_custom_modifier_count (cmod_type);
	if (count == 0)
		return;
	int base_callconv = sig->call_convention;
	gboolean suppress_gc_transition = sig->suppress_gc_transition;
	for (uint8_t i = 0; i < count; ++i) {
		gboolean req = FALSE;
		MonoType *cmod = mono_type_get_custom_modifier (cmod_type, i, &req, error);
		return_if_nok (error);
		/* callconv is a modopt, not a modreq */
		if (req)
			continue;
		/* shouldn't be a valuetype, array, gparam, gtd, ginst etc */
		if (cmod->type != MONO_TYPE_CLASS)
			continue;
		MonoClass *cmod_klass = mono_class_from_mono_type_internal (cmod);
		if (m_class_get_image (cmod_klass) != mono_defaults.corlib)
			continue;
		if (strcmp (m_class_get_name_space (cmod_klass), "System.Runtime.CompilerServices"))
			continue;
		const char *name = m_class_get_name (cmod_klass);
		if (strstr (name, "CallConv") != name)
			continue;
		name += strlen ("CallConv"); /* skip the prefix */

		/* Check for the known base unmanaged calling conventions */
		if (!strcmp (name, "Cdecl")) {
			base_callconv = MONO_CALL_C;
			continue;
		} else if (!strcmp (name, "Stdcall")) {
			base_callconv = MONO_CALL_STDCALL;
			continue;
		} else if (!strcmp (name, "Thiscall")) {
			base_callconv = MONO_CALL_THISCALL;
			continue;
		} else if (!strcmp (name, "Fastcall")) {
			base_callconv = MONO_CALL_FASTCALL;
			continue;
		}

		/* Check for known calling convention modifiers */
		if (!strcmp (name, "SuppressGCTransition")) {
			suppress_gc_transition = TRUE;
			continue;
		}
	}
	sig->call_convention = base_callconv;
	sig->suppress_gc_transition = suppress_gc_transition;
}

/**
 * mono_metadata_parse_method_signature_full:
 * \param m metadata context
 * \param generic_container: generics container
 * \param def the \c MethodDef index or 0 for \c Ref signatures.
 * \param ptr pointer to the signature metadata representation
 * \param rptr pointer updated to match the end of the decoded stream
 * \param error set on error
 *
 *
 * Decode a method signature stored at \p ptr.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * \returns a \c MonoMethodSignature describing the signature.  On error sets
 * \p error and returns \c NULL.
 */
MonoMethodSignature *
mono_metadata_parse_method_signature_full (MonoImage *m, MonoGenericContainer *container,
					   int def, const char *ptr, const char **rptr, MonoError *error)
{
	MonoMethodSignature *method;
	int *pattrs = NULL;
	guint32 hasthis = 0, explicit_this = 0, call_convention, param_count;
	guint32 gen_param_count = 0;
	gboolean is_open = FALSE;

	error_init (error);

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

	switch (method->call_convention) {
	case MONO_CALL_DEFAULT:
	case MONO_CALL_VARARG:
		method->pinvoke = 0;
		break;
	case MONO_CALL_C:
	case MONO_CALL_STDCALL:
	case MONO_CALL_THISCALL:
	case MONO_CALL_FASTCALL:
	case MONO_CALL_UNMANAGED_MD:
		method->pinvoke = 1;
		break;
	}

	if (call_convention != 0xa) {
		method->ret = mono_metadata_parse_type_checked (m, container, pattrs ? pattrs [0] : 0, FALSE, ptr, &ptr, error);
		if (!method->ret) {
			mono_metadata_free_method_signature (method);
			g_free (pattrs);
			return NULL;
		}
		is_open = mono_class_is_open_constructed_type (method->ret);
		if (G_UNLIKELY (method->ret->has_cmods && method->call_convention == MONO_CALL_UNMANAGED_MD)) {
			/* calling convention encoded in modopts */
			metadata_signature_set_modopt_call_conv (method, method->ret, error);
			if (!is_ok (error)) {
				g_free (pattrs);
				return NULL;
			}
		}
	}

	for (guint16 i = 0; i < method->param_count; ++i) {
		if (*ptr == MONO_TYPE_SENTINEL) {
			if (method->call_convention != MONO_CALL_VARARG || def) {
				mono_error_set_bad_image (error, m, "Found sentinel for methoddef or no vararg");
				g_free (pattrs);
				return NULL;
			}
			if (method->sentinelpos >= 0) {
				mono_error_set_bad_image (error, m, "Found sentinel twice in the same signature.");
				g_free (pattrs);
				return NULL;
			}
			method->sentinelpos = i;
			ptr++;
		}
		method->params [i] = mono_metadata_parse_type_checked (m, container, pattrs ? pattrs [i+1] : 0, FALSE, ptr, &ptr, error);
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

/**
 * mono_metadata_parse_method_signature:
 * \param m metadata context
 * \param def the \c MethodDef index or 0 for \c Ref signatures.
 * \param ptr pointer to the signature metadata representation
 * \param rptr pointer updated to match the end of the decoded stream
 *
 * Decode a method signature stored at \p ptr.
 * This is a Mono runtime internal function.
 *
 * LOCKING: Assumes the loader lock is held.
 *
 * \returns a \c MonoMethodSignature describing the signature.
 */
MonoMethodSignature *
mono_metadata_parse_method_signature (MonoImage *m, int def, const char *ptr, const char **rptr)
{
	/*
	 * This function MUST NOT be called by runtime code as it does error handling incorrectly.
	 * Use mono_metadata_parse_method_signature_full instead.
	 * It's ok to asser on failure as we no longer use it.
	 */
	ERROR_DECL (error);
	MonoMethodSignature *ret;
	ret = mono_metadata_parse_method_signature_full (m, NULL, def, ptr, rptr, error);
	mono_error_assert_ok (error);

	return ret;
}

/**
 * mono_metadata_free_method_signature:
 * \param sig signature to destroy
 *
 * Free the memory allocated in the signature \p sig.
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
	const MonoMethodInflated *ma = (const MonoMethodInflated *)a;
	const MonoMethodInflated *mb = (const MonoMethodInflated *)b;
	if (ma->declaring != mb->declaring)
		return FALSE;
	return mono_metadata_generic_context_equal (&ma->context, &mb->context);
}

static guint
inflated_method_hash (gconstpointer a)
{
	const MonoMethodInflated *ma = (const MonoMethodInflated *)a;
	return (mono_metadata_generic_context_hash (&ma->context) ^ mono_aligned_addr_hash (ma->declaring));
}

static gboolean
inflated_signature_equal (gconstpointer a, gconstpointer b)
{
	const MonoInflatedMethodSignature *sig1 = (const MonoInflatedMethodSignature *)a;
	const MonoInflatedMethodSignature *sig2 = (const MonoInflatedMethodSignature *)b;

	/* sig->sig is assumed to be canonized */
	if (sig1->sig != sig2->sig)
		return FALSE;
	/* The generic instances are canonized */
	return mono_metadata_generic_context_equal (&sig1->context, &sig2->context);
}

static guint
inflated_signature_hash (gconstpointer a)
{
	const MonoInflatedMethodSignature *sig = (const MonoInflatedMethodSignature *)a;

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

static gboolean
aggregate_modifiers_equal (gconstpointer ka, gconstpointer kb)
{
	MonoAggregateModContainer *amods1 = (MonoAggregateModContainer *)ka;
	MonoAggregateModContainer *amods2 = (MonoAggregateModContainer *)kb;
	if (amods1->count != amods2->count)
		return FALSE;
	for (int i = 0; i < amods1->count; ++i) {
		if (amods1->modifiers [i].required != amods2->modifiers [i].required)
			return FALSE;
		if (!mono_metadata_type_equal_full (amods1->modifiers [i].type, amods2->modifiers [i].type, TRUE))
			return FALSE;
	}
	return TRUE;
}

static guint
aggregate_modifiers_hash (gconstpointer a)
{
	const MonoAggregateModContainer *amods = (const MonoAggregateModContainer *)a;
	guint hash = 0;
	for (int i = 0; i < amods->count; ++i)
	{
		// hash details borrowed from mono_metadata_generic_inst_hash
		hash *= 13;
		hash ^= (amods->modifiers [i].required << 8);
		hash += mono_metadata_type_hash (amods->modifiers [i].type);
	}

	return hash;
}

static gboolean type_in_image (MonoType *type, MonoImage *image);
static gboolean aggregate_modifiers_in_image (MonoAggregateModContainer *amods, MonoImage *image);

static gboolean
signature_in_image (MonoMethodSignature *sig, MonoImage *image)
{
	gpointer iter = NULL;
	MonoType *p;

	while ((p = mono_signature_get_params_internal (sig, &iter)) != NULL)
		if (type_in_image (p, image))
			return TRUE;

	return type_in_image (mono_signature_get_return_type_internal (sig), image);
}

static gboolean
ginst_in_image (MonoGenericInst *ginst, MonoImage *image)
{
	for (guint i = 0; i < ginst->type_argc; ++i) {
		if (type_in_image (ginst->type_argv [i], image))
			return TRUE;
	}

	return FALSE;
}

static gboolean
gclass_in_image (MonoGenericClass *gclass, MonoImage *image)
{
	return m_class_get_image (gclass->container_class) == image ||
		ginst_in_image (gclass->context.class_inst, image);
}

static gboolean
type_in_image (MonoType *type, MonoImage *image)
{
retry:
	if (type->has_cmods && mono_type_is_aggregate_mods (type))
		if (aggregate_modifiers_in_image (mono_type_get_amods (type), image))
			return TRUE;

	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		return gclass_in_image (type->data.generic_class, image);
	case MONO_TYPE_PTR:
		type = type->data.type;
		goto retry;
	case MONO_TYPE_SZARRAY:
		type = m_class_get_byval_arg (type->data.klass);
		goto retry;
	case MONO_TYPE_ARRAY:
		type = m_class_get_byval_arg (type->data.array->eklass);
		goto retry;
	case MONO_TYPE_FNPTR:
		return signature_in_image (type->data.method, image);
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (image == mono_get_image_for_generic_param (type->data.generic_param))
			return TRUE;
		else if (type->data.generic_param->gshared_constraint) {
			type = type->data.generic_param->gshared_constraint;
			goto retry;
		}
		return FALSE;
	default:
		/* At this point, we should've avoided all potential allocations in mono_class_from_mono_type_internal () */
		return image == m_class_get_image (mono_class_from_mono_type_internal (type));
	}
}

gboolean
mono_type_in_image (MonoType *type, MonoImage *image)
{
	return type_in_image (type, image);
}

gboolean
aggregate_modifiers_in_image (MonoAggregateModContainer *amods, MonoImage *image)
{
	for (int i = 0; i < amods->count; i++)
		if (type_in_image (amods->modifiers [i].type, image))
			return TRUE;
	return FALSE;
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

static void
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
	for (guint i = 0; i < ginst->type_argc; ++i) {
		collect_type_images (ginst->type_argv [i], data);
	}
}

static void
collect_gclass_images (MonoGenericClass *gclass, CollectData *data)
{
	add_image (m_class_get_image (gclass->container_class), data);
	if (gclass->context.class_inst)
		collect_ginst_images (gclass->context.class_inst, data);
}

static void
collect_signature_images (MonoMethodSignature *sig, CollectData *data)
{
	gpointer iter = NULL;
	MonoType *p;

	collect_type_images (mono_signature_get_return_type_internal (sig), data);
	while ((p = mono_signature_get_params_internal (sig, &iter)) != NULL)
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

	add_image (m_class_get_image (method->declaring->klass), data);
	if (method->context.class_inst)
		collect_ginst_images (method->context.class_inst, data);
	if (method->context.method_inst)
		collect_ginst_images (method->context.method_inst, data);
	/*
	 * Dynamic assemblies have no references, so the images they depend on can be unloaded before them.
	 */
	if (image_is_dynamic (m_class_get_image (m->klass)))
		collect_signature_images (mono_method_signature_internal (m), data);
}

static void
collect_aggregate_modifiers_images (MonoAggregateModContainer *amods, CollectData *data)
{
	for (int i = 0; i < amods->count; ++i)
		collect_type_images (amods->modifiers [i].type, data);
}

static void
collect_type_images (MonoType *type, CollectData *data)
{
retry:
	if (G_UNLIKELY (type->has_cmods && mono_type_is_aggregate_mods (type))) {
		collect_aggregate_modifiers_images (mono_type_get_amods (type), data);
	}

	switch (type->type) {
	case MONO_TYPE_GENERICINST:
		collect_gclass_images (type->data.generic_class, data);
		break;
	case MONO_TYPE_PTR:
		type = type->data.type;
		goto retry;
	case MONO_TYPE_SZARRAY:
		type = m_class_get_byval_arg (type->data.klass);
		goto retry;
	case MONO_TYPE_ARRAY:
		type = m_class_get_byval_arg (type->data.array->eklass);
		goto retry;
	case MONO_TYPE_FNPTR:
		collect_signature_images (type->data.method, data);
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
	{
		MonoImage *image = mono_get_image_for_generic_param (type->data.generic_param);
		add_image (image, data);
		type = type->data.generic_param->gshared_constraint;
		if (type)
			goto retry;
		break;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		add_image (m_class_get_image (mono_class_from_mono_type_internal (type)), data);
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
	MonoGenericClass *gclass = (MonoGenericClass *)key;
	CleanForImageUserData *user_data = (CleanForImageUserData *)data;

	g_assert (gclass_in_image (gclass, user_data->image));

	user_data->list = g_slist_prepend (user_data->list, gclass);
	return TRUE;
}

static gboolean
steal_ginst_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoGenericInst *ginst = (MonoGenericInst *)key;
	CleanForImageUserData *user_data = (CleanForImageUserData *)data;

	// This doesn't work during corlib compilation
	//g_assert (ginst_in_image (ginst, user_data->image));

	user_data->list = g_slist_prepend (user_data->list, ginst);
	return TRUE;
}

static gboolean
steal_aggregate_modifiers_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoAggregateModContainer *amods = (MonoAggregateModContainer *)key;
	CleanForImageUserData *user_data = (CleanForImageUserData *)data;

	g_assert (aggregate_modifiers_in_image (amods, user_data->image));

	user_data->list = g_slist_prepend (user_data->list, amods);
	return TRUE;
}

static gboolean
inflated_method_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = (MonoImage *)data;
	MonoMethodInflated *method = (MonoMethodInflated *)key;

	// FIXME:
	// https://bugzilla.novell.com/show_bug.cgi?id=458168
	g_assert (m_class_get_image (method->declaring->klass) == image ||
		(method->context.class_inst && ginst_in_image (method->context.class_inst, image)) ||
			  (method->context.method_inst && ginst_in_image (method->context.method_inst, image)) || (((MonoMethod*)method)->signature && signature_in_image (mono_method_signature_internal ((MonoMethod*)method), image)));

	return TRUE;
}

static gboolean
inflated_signature_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = (MonoImage *)data;
	MonoInflatedMethodSignature *sig = (MonoInflatedMethodSignature *)key;

	return signature_in_image (sig->sig, image) ||
		(sig->context.class_inst && ginst_in_image (sig->context.class_inst, image)) ||
		(sig->context.method_inst && ginst_in_image (sig->context.method_inst, image));
}

static gboolean
class_in_image (gpointer key, gpointer value, gpointer data)
{
	MonoImage *image = (MonoImage *)data;
	MonoClass *klass = (MonoClass *)key;

	g_assert (type_in_image (m_class_get_byval_arg (klass), image));

	return TRUE;
}

static void
check_gmethod (gpointer key, gpointer value, gpointer data)
{
	MonoMethodInflated *method = (MonoMethodInflated *)key;
	MonoImage *image = (MonoImage *)data;

	if (method->context.class_inst)
		g_assert (!ginst_in_image (method->context.class_inst, image));
	if (method->context.method_inst)
		g_assert (!ginst_in_image (method->context.method_inst, image));
	if (((MonoMethod*)method)->signature)
		g_assert (!signature_in_image (mono_method_signature_internal ((MonoMethod*)method), image));
}

static void
free_generic_inst (MonoGenericInst *ginst)
{
	/* The ginst itself is allocated from the image set mempool */
	for (guint i = 0; i < ginst->type_argc; ++i)
		mono_metadata_free_type (ginst->type_argv [i]);
}

static void
free_generic_class (MonoGenericClass *gclass)
{
	/* The gclass itself is allocated from the image set mempool */
	if (gclass->cached_class && m_class_get_interface_id (gclass->cached_class))
		mono_unload_interface_id (gclass->cached_class);
}

static void
free_inflated_signature (MonoInflatedMethodSignature *sig)
{
	mono_metadata_free_inflated_signature (sig->sig);
}

static void
free_aggregate_modifiers (MonoAggregateModContainer *amods)
{
	for (int i = 0; i < amods->count; i++)
		mono_metadata_free_type (amods->modifiers [i].type);
	/* the container itself is allocated in the image set mempool */
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

	helper.sig = sig;
	helper.context.class_inst = context->class_inst;
	helper.context.method_inst = context->method_inst;

	collect_data_init (&data);
	collect_inflated_signature_images (&helper, &data);
	MonoMemoryManager *mm = mono_mem_manager_get_generic (data.images, data.nimages);
	collect_data_free (&data);

	mono_mem_manager_lock (mm);

	if (!mm->gsignature_cache)
		mm->gsignature_cache = g_hash_table_new_full (inflated_signature_hash, inflated_signature_equal, NULL, (GDestroyNotify)free_inflated_signature);
	res = (MonoInflatedMethodSignature *)g_hash_table_lookup (mm->gsignature_cache, &helper);
	if (!res) {
		res = mono_mem_manager_alloc0 (mm, sizeof (MonoInflatedMethodSignature));
		res->sig = sig;
		res->context.class_inst = context->class_inst;
		res->context.method_inst = context->method_inst;
		g_hash_table_insert (mm->gsignature_cache, res, res);
	}

	mono_mem_manager_unlock (mm);

	return res->sig;
}

MonoMemoryManager *
mono_metadata_get_mem_manager_for_type (MonoType *type)
{
	MonoMemoryManager *mm;
	CollectData image_set_data;

	collect_data_init (&image_set_data);
	collect_type_images (type, &image_set_data);
	mm = mono_mem_manager_get_generic (image_set_data.images, image_set_data.nimages);
	collect_data_free (&image_set_data);

	return mm;
}

MonoMemoryManager *
mono_metadata_get_mem_manager_for_class (MonoClass *klass)
{
	return mono_metadata_get_mem_manager_for_type (m_class_get_byval_arg (klass));
}

MonoMemoryManager *
mono_metadata_get_mem_manager_for_method (MonoMethodInflated *method)
{
	MonoMemoryManager *mm;
	CollectData image_set_data;

	collect_data_init (&image_set_data);
	collect_method_images (method, &image_set_data);
	mm = mono_mem_manager_get_generic (image_set_data.images, image_set_data.nimages);
	collect_data_free (&image_set_data);

	return mm;
}

static MonoMemoryManager *
mono_metadata_get_mem_manager_for_aggregate_modifiers (MonoAggregateModContainer *amods)
{
	MonoMemoryManager *mm;
	CollectData image_set_data;
	collect_data_init (&image_set_data);
	collect_aggregate_modifiers_images (amods, &image_set_data);
	mm = mono_mem_manager_get_generic (image_set_data.images, image_set_data.nimages);
	collect_data_free (&image_set_data);

	return mm;
}

static gboolean
type_is_gtd (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		return mono_class_is_gtd (type->data.klass);
	default:
		return FALSE;
	}
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

	for (i = 0; i < type_argc; ++i)
		if (mono_class_is_open_constructed_type (type_argv [i]))
			break;
	is_open = (i < type_argc);

	ginst = (MonoGenericInst *)g_alloca (size);
	memset (ginst, 0, MONO_SIZEOF_GENERIC_INST);
	ginst->is_open = is_open;
	ginst->type_argc = type_argc;
	memcpy (ginst->type_argv, type_argv, type_argc * sizeof (MonoType *));

	for (i = 0; i < type_argc; ++i) {
		MonoType *t = ginst->type_argv [i];
		if (type_is_gtd (t)) {
			ginst->type_argv [i] = mono_class_gtd_get_canonical_inst (t->data.klass);
		}
	}

	return mono_metadata_get_canonical_generic_inst (ginst);
}

/**
 * mono_metadata_get_canonical_generic_inst:
 * \param candidate an arbitrary generic instantiation
 *
 * \returns the canonical generic instantiation that represents the given
 * candidate by identifying the image set for the candidate instantiation and
 * finding the instance in the image set or adding a copy of the given instance
 * to the image set.
 *
 * The returned MonoGenericInst has its own copy of the list of types.  The list
 * passed in the argument can be freed, modified or disposed of.
 *
 */
MonoGenericInst *
mono_metadata_get_canonical_generic_inst (MonoGenericInst *candidate)
{
	CollectData data;
	int type_argc = candidate->type_argc;
	gboolean is_open = candidate->is_open;

	collect_data_init (&data);
	collect_ginst_images (candidate, &data);
	MonoMemoryManager *mm = mono_mem_manager_get_generic (data.images, data.nimages);
	collect_data_free (&data);

	mono_mem_manager_lock (mm);

	if (!mm->ginst_cache)
		mm->ginst_cache = g_hash_table_new_full (mono_metadata_generic_inst_hash, mono_metadata_generic_inst_equal, NULL, (GDestroyNotify)free_generic_inst);

	MonoGenericInst *ginst = (MonoGenericInst *)g_hash_table_lookup (mm->ginst_cache, candidate);
	if (!ginst) {
		int size = MONO_SIZEOF_GENERIC_INST + type_argc * sizeof (MonoType *);
		ginst = (MonoGenericInst *)mono_mem_manager_alloc0 (mm, size);
#ifndef MONO_SMALL_CONFIG
		ginst->id = mono_atomic_inc_i32 (&next_generic_inst_id);
#endif
		ginst->is_open = is_open;
		ginst->type_argc = type_argc;

		// FIXME: Dup into the mem manager
		for (int i = 0; i < type_argc; ++i)
			ginst->type_argv [i] = mono_metadata_type_dup (NULL, candidate->type_argv [i]);

		g_hash_table_insert (mm->ginst_cache, ginst, ginst);
	}

	mono_mem_manager_unlock (mm);

	return ginst;
}

MonoAggregateModContainer *
mono_metadata_get_canonical_aggregate_modifiers (MonoAggregateModContainer *candidate)
{
	g_assert (candidate->count > 0);
	MonoMemoryManager *mm = mono_metadata_get_mem_manager_for_aggregate_modifiers (candidate);

	mono_mem_manager_lock (mm);

	if (!mm->aggregate_modifiers_cache)
		mm->aggregate_modifiers_cache = g_hash_table_new_full (aggregate_modifiers_hash, aggregate_modifiers_equal, NULL, (GDestroyNotify)free_aggregate_modifiers);

	MonoAggregateModContainer *amods = (MonoAggregateModContainer *)g_hash_table_lookup (mm->aggregate_modifiers_cache, candidate);
	if (!amods) {
		size_t size = mono_sizeof_aggregate_modifiers (candidate->count);
		amods = (MonoAggregateModContainer *)mono_mem_manager_alloc0 (mm, (guint)size);
		amods->count = candidate->count;
		for (int i = 0; i < candidate->count; ++i) {
			amods->modifiers [i].required = candidate->modifiers [i].required;
			amods->modifiers [i].type = mono_metadata_type_dup (NULL, candidate->modifiers [i].type);
		}

		g_hash_table_insert (mm->aggregate_modifiers_cache, amods, amods);
	}
	mono_mem_manager_unlock (mm);
	return amods;
}

static gboolean
mono_metadata_is_type_builder_generic_type_definition (MonoClass *container_class, MonoGenericInst *inst, gboolean is_dynamic)
{
	MonoGenericContainer *container = mono_class_get_generic_container (container_class);

	if (!is_dynamic || m_class_was_typebuilder (container_class) || container->type_argc != inst->type_argc)
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
	CollectData data;

	g_assert (mono_class_get_generic_container (container_class)->type_argc == inst->type_argc);

	memset (&helper, 0, sizeof(helper)); // act like g_new0
	helper.container_class = container_class;
	helper.context.class_inst = inst;
	helper.is_dynamic = is_dynamic; /* We use this in a hash lookup, which does not attempt to downcast the pointer */
	helper.is_tb_open = is_tb_open;

	collect_data_init (&data);
	collect_gclass_images (&helper, &data);
	MonoMemoryManager *mm = mono_mem_manager_get_generic (data.images, data.nimages);
	collect_data_free (&data);

	if (!mm->gclass_cache) {
		mono_mem_manager_lock (mm);
		if (!mm->gclass_cache) {
			MonoConcurrentHashTable *cache = mono_conc_hashtable_new_full (mono_generic_class_hash, mono_generic_class_equal, NULL, (GDestroyNotify)free_generic_class);
			mono_memory_barrier ();
			mm->gclass_cache = cache;
		}
		mono_mem_manager_unlock (mm);
	}

	gclass = (MonoGenericClass *)mono_conc_hashtable_lookup (mm->gclass_cache, &helper);

	/* A tripwire just to keep us honest */
	g_assert (!helper.cached_class);

	if (gclass)
		return gclass;

	mono_mem_manager_lock (mm);

	gclass = mono_mem_manager_alloc0 (mm, sizeof (MonoGenericClass));
	if (is_dynamic)
		gclass->is_dynamic = 1;

	gclass->is_tb_open = is_tb_open;
	gclass->container_class = container_class;
	gclass->context.class_inst = inst;
	gclass->context.method_inst = NULL;
	gclass->owner = mm;
	if (inst == mono_class_get_generic_container (container_class)->context.class_inst && !is_tb_open)
		gclass->cached_class = container_class;

	MonoGenericClass *gclass2 = (MonoGenericClass*)mono_conc_hashtable_insert (mm->gclass_cache, gclass, gclass);
	if (!gclass2)
		gclass2 = gclass;

	// g_hash_table_insert (set->gclass_cache, gclass, gclass);

	mono_mem_manager_unlock (mm);

	return gclass2;
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
	guint count = 0;

	error_init (error);

	if (!ginst->is_open)
		return ginst;

	type_argv = g_new0 (MonoType*, ginst->type_argc);

	for (guint i = 0; i < ginst->type_argc; i++) {
		type_argv [i] = mono_class_inflate_generic_type_checked (ginst->type_argv [i], context, error);
		if (!is_ok (error))
			goto cleanup;
		++count;
	}

	nginst = mono_metadata_get_generic_inst (ginst->type_argc, type_argv);

cleanup:
	for (guint i = 0; i < count; i++)
		mono_metadata_free_type (type_argv [i]);
	g_free (type_argv);

	return nginst;
}

MonoGenericInst *
mono_metadata_parse_generic_inst (MonoImage *m, MonoGenericContainer *container,
				  int count, const char *ptr, const char **rptr, MonoError *error)
{
	MonoType **type_argv;
	MonoGenericInst *ginst = NULL;
	int i, parse_count = 0;

	error_init (error);
	type_argv = g_new0 (MonoType*, count);

	for (i = 0; i < count; i++) {
		/* this can be a transient type, mono_metadata_get_generic_inst will allocate
		 * a canonical one, if needed.
		 */
		MonoType *t = mono_metadata_parse_type_checked (m, container, 0, TRUE, ptr, &ptr, error);
		if (!t)
			goto cleanup;
		type_argv [i] = t;
		parse_count++;
	}

	if (rptr)
		*rptr = ptr;

	g_assert (parse_count == count);
	ginst = mono_metadata_get_generic_inst (count, type_argv);

cleanup:
	for (i = 0; i < parse_count; i++)
		mono_metadata_free_type (type_argv [i]);
	g_free (type_argv);

	return ginst;
}

static gboolean
do_mono_metadata_parse_generic_class (MonoType *type, MonoImage *m, MonoGenericContainer *container,
				      const char *ptr, const char **rptr, MonoError *error)
{
	MonoGenericInst *inst;
	MonoClass *gklass;
	MonoType *gtype;
	int count;

	error_init (error);

	// XXX how about transient?
	gtype = mono_metadata_parse_type_checked (m, NULL, 0, FALSE, ptr, &ptr, error);
	if (gtype == NULL)
		return FALSE;

	gklass = mono_class_from_mono_type_internal (gtype);
	if (!mono_class_is_gtd (gklass)) {
		mono_error_set_bad_image (error, m, "Generic instance with non-generic definition");
		return FALSE;
	}

	count = mono_metadata_decode_value (ptr, &ptr);
	inst = mono_metadata_parse_generic_inst (m, container, count, ptr, &ptr, error);
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

MonoGenericContainer *
mono_get_anonymous_container_for_image (MonoImage *image, gboolean is_mvar)
{
	MonoGenericContainer **container_pointer;
	if (is_mvar)
		container_pointer = &image->anonymous_generic_method_container;
	else
		container_pointer = &image->anonymous_generic_class_container;
	MonoGenericContainer *result = *container_pointer;

	// This container has never been created; make it now.
	if (!result)
	{
		// Note this is never deallocated anywhere-- it exists for the lifetime of the image it's allocated from
		result = (MonoGenericContainer *)mono_image_alloc0 (image, sizeof (MonoGenericContainer));
		result->owner.image = image;
		result->is_anonymous = TRUE;
		result->is_method = is_mvar;

		// If another thread already made a container, use that and leak this new one.
		// (Technically it would currently be safe to just assign instead of CASing.)
		MonoGenericContainer *exchange = (MonoGenericContainer *)mono_atomic_cas_ptr ((volatile gpointer *)container_pointer, result, NULL);
		if (exchange)
			result = exchange;
	}
	return result;
}

#define FAST_GPARAM_CACHE_SIZE 16

static MonoGenericParam*
lookup_anon_gparam (MonoImage *image, MonoGenericContainer *container, gint32 param_num, gboolean is_mvar)
{
	if (param_num >= 0 && param_num < FAST_GPARAM_CACHE_SIZE) {
		MonoGenericParam *cache = is_mvar ? image->mvar_gparam_cache_fast : image->var_gparam_cache_fast;
		if (!cache)
			return NULL;
		return &cache[param_num];
	} else {
		MonoGenericParam key;
		memset (&key, 0, sizeof (key));
		key.owner = container;
		key.num = GINT32_TO_UINT16 (param_num);
		key.gshared_constraint = NULL;
		MonoConcurrentHashTable *cache = is_mvar ? image->mvar_gparam_cache : image->var_gparam_cache;
		if (!cache)
			return NULL;
		return (MonoGenericParam*)mono_conc_hashtable_lookup (cache, &key);
	}
}

static MonoGenericParam*
publish_anon_gparam_fast (MonoImage *image, MonoGenericContainer *container, gint32 param_num)
{
	g_assert (param_num >= 0 && param_num < FAST_GPARAM_CACHE_SIZE);
	MonoGenericParam **cache = container->is_method ? &image->mvar_gparam_cache_fast : &image->var_gparam_cache_fast;
	if (!*cache) {
		mono_image_lock (image);
		if (!*cache) {
			*cache = (MonoGenericParam*)mono_image_alloc0 (image, sizeof (MonoGenericParam) * FAST_GPARAM_CACHE_SIZE);
			for (guint16 i = 0; i < FAST_GPARAM_CACHE_SIZE; ++i) {
				MonoGenericParam *param = &(*cache)[i];
				param->owner = container;
				param->num = i;
			}
		}
		mono_image_unlock (image);
	}
	return &(*cache)[param_num];
}

/*
 * publish_anon_gparam_slow:
 *
 * Publish \p gparam anonymous generic parameter to the anon gparam cache for \p image.
 *
 * LOCKING: takes the image lock.
 */
static MonoGenericParam*
publish_anon_gparam_slow (MonoImage *image, MonoGenericParam *gparam)
{
	MonoConcurrentHashTable **cache = gparam->owner->is_method ? &image->mvar_gparam_cache : &image->var_gparam_cache;
	if (!*cache) {
		mono_image_lock (image);
		if (!*cache) {
			MonoConcurrentHashTable *ht = mono_conc_hashtable_new ((GHashFunc)mono_metadata_generic_param_hash,
										(GEqualFunc) mono_metadata_generic_param_equal);
			mono_atomic_store_release (cache, ht);
		}
		mono_image_unlock (image);
	}
	MonoGenericParam *other = (MonoGenericParam*)mono_conc_hashtable_insert (*cache, gparam, gparam);
	// If another thread published first return their param, otherwise return ours.
	return other ? other : gparam;
}

/**
 * mono_metadata_create_anon_gparam:
 * \param image the MonoImage that owns the anonymous generic parameter
 * \param param_num the parameter number
 * \param is_mvar TRUE if this is a method generic parameter, FALSE if it's a class generic parameter.
 *
 * Returns: a new, or exisisting \c MonoGenericParam for an anonymous generic parameter with the given properties.
 *
 * LOCKING: takes the image lock.
 */
MonoGenericParam*
mono_metadata_create_anon_gparam (MonoImage *image, gint32 param_num, gboolean is_mvar)
{
	MonoGenericContainer *container = mono_get_anonymous_container_for_image (image, is_mvar);
	MonoGenericParam *gparam = lookup_anon_gparam (image, container, param_num, is_mvar);
	if (gparam)
		return gparam;
	if (param_num >= 0 && param_num < FAST_GPARAM_CACHE_SIZE) {
		return publish_anon_gparam_fast (image, container, param_num);
	} else {
		// Create a candidate generic param and try to insert it in the cache.
		// If multiple threads both try to publish the same param, all but one
		// will leak, but that's okay.
		gparam = (MonoGenericParam*)mono_image_alloc0 (image, sizeof (MonoGenericParam));
		gparam->owner = container;
		gparam->num = GINT32_TO_UINT16 (param_num);

		return publish_anon_gparam_slow (image, gparam);
	}
}

/*
 * mono_metadata_parse_generic_param:
 * @generic_container: Our MonoClass's or MonoMethod's MonoGenericContainer;
 *                     see mono_metadata_parse_type_checked() for details.
 * Internal routine to parse a generic type parameter.
 * LOCKING: Acquires the loader lock
 */
static MonoGenericParam *
mono_metadata_parse_generic_param (MonoImage *m, MonoGenericContainer *generic_container,
				   MonoTypeEnum type, const char *ptr, const char **rptr, MonoError *error)
{
	int index = mono_metadata_decode_value (ptr, &ptr);
	if (rptr)
		*rptr = ptr;

	error_init (error);

	generic_container = select_container (generic_container, type);
	if (!generic_container) {
		gboolean is_mvar = FALSE;
		switch (type)
		{
			case MONO_TYPE_VAR:
				break;
			case MONO_TYPE_MVAR:
				is_mvar = TRUE;
				break;
			default:
				g_error ("Cerating generic param object with invalid MonoType"); // This is not a generic param
		}

		return mono_metadata_create_anon_gparam (m, index, is_mvar);
	}

	if (index >= generic_container->type_argc) {
		mono_error_set_bad_image (error, m, "Invalid generic %s parameter index %d, max index is %d",
			generic_container->is_method ? "method" : "type",
			index, generic_container->type_argc);
		return NULL;
	}

	//This can't return NULL
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
	if ((cached = (MonoType *)g_hash_table_lookup (type_cache, type)))
		return cached;

	switch (type->type){
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		if (type == m_class_get_byval_arg (type->data.klass))
			return type;
		if (type == m_class_get_this_arg (type->data.klass))
			return type;
		break;
	default:
		break;
	}

	return NULL;
}

static gboolean
compare_type_literals (MonoImage *image, int class_type, int type_type, MonoError *error)
{
	error_init (error);

	/* _byval_arg.type can be zero if we're decoding a type that references a class been loading.
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

	if (type_type == MONO_TYPE_CLASS) {
		if (class_type == MONO_TYPE_STRING || class_type == MONO_TYPE_OBJECT)
			return TRUE;
		//XXX stringify this argument
		mono_error_set_bad_image (error, image, "Expected reference type but got type kind %d", class_type);
		return FALSE;
	}

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
		//XXX stringify this argument
		mono_error_set_bad_image (error, image, "Expected value type but got type kind %d", class_type);
		return FALSE;
	}
}

static gboolean
verify_var_type_and_container (MonoImage *image, int var_type, MonoGenericContainer *container, MonoError *error)
{
	error_init (error);
	if (var_type == MONO_TYPE_MVAR) {
		if (!container->is_method) { //MVAR and a method container
			mono_error_set_bad_image (error, image, "MVAR parsed in a context without a method container");
			return FALSE;
		}
	} else {
		if (!(!container->is_method || //VAR and class container
			(container->is_method && container->parent))) { //VAR and method container with parent
			mono_error_set_bad_image (error, image, "VAR parsed in a context without a class container");
			return FALSE;
		}
	}
	return TRUE;
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
							 gboolean transient, const char *ptr, const char **rptr, MonoError *error)
{
	error_init (error);

	type->type = (MonoTypeEnum)mono_metadata_decode_value (ptr, &ptr);

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
		MonoClass *klass;
		token = mono_metadata_parse_typedef_or_ref (m, ptr, &ptr);
		klass = mono_class_get_checked (m, token, error);
		type->data.klass = klass;
		if (!klass)
			return FALSE;

		if (!compare_type_literals (m, m_class_get_byval_arg (klass)->type, type->type, error))
			return FALSE;

		break;
	}
	case MONO_TYPE_SZARRAY: {
		MonoType *etype = mono_metadata_parse_type_checked (m, container, 0, transient, ptr, &ptr, error);
		if (!etype)
			return FALSE;

		type->data.klass = mono_class_from_mono_type_internal (etype);

		if (transient)
			mono_metadata_free_type (etype);

		g_assert (type->data.klass); //This was previously a check for NULL, but mcfmt should never fail. It can return a borken MonoClass, but should return at least something.
		break;
	}
	case MONO_TYPE_PTR: {
		type->data.type = mono_metadata_parse_type_checked (m, container, 0, transient, ptr, &ptr, error);
		if (!type->data.type)
			return FALSE;
		break;
	}
	case MONO_TYPE_FNPTR: {
		type->data.method = mono_metadata_parse_method_signature_full (m, container, 0, ptr, &ptr, error);
		if (!type->data.method)
			return FALSE;
		break;
	}
	case MONO_TYPE_ARRAY: {
		type->data.array = mono_metadata_parse_array_internal (m, container, transient, ptr, &ptr, error);
		if (!type->data.array)
			return FALSE;
		break;
	}
	case MONO_TYPE_MVAR:
	case MONO_TYPE_VAR: {
		if (container && !verify_var_type_and_container (m, type->type, container, error))
			return FALSE;

		type->data.generic_param = mono_metadata_parse_generic_param (m, container, type->type, ptr, &ptr, error);
		if (!type->data.generic_param)
			return FALSE;

		break;
	}
	case MONO_TYPE_GENERICINST: {
		if (!do_mono_metadata_parse_generic_class (type, m, container, ptr, &ptr, error))
			return FALSE;
		break;
	}
	default:
		mono_error_set_bad_image (error, m, "type 0x%02x not handled in do_mono_metadata_parse_type on image %s", type->type, m->name);
		return FALSE;
	}

	if (rptr)
		*rptr = ptr;
	return TRUE;
}

/**
 * mono_metadata_free_type:
 * \param type type to free
 *
 * Free the memory allocated for type \p type which is allocated on the heap.
 */
void
mono_metadata_free_type (MonoType *type)
{
	/* Note: keep in sync with do_mono_metadata_parse_type and try_get_canonical_type which
	 * allocate memory or try to avoid allocating memory. */
	if (type >= builtin_types && type < builtin_types + G_N_ELEMENTS (builtin_types))
		return;

	switch (type->type){
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
		if (!type->data.klass)
			break;
		/* fall through */
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		if (type == m_class_get_byval_arg (type->data.klass) || type == m_class_get_this_arg (type->data.klass))
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
parse_section_data (MonoImage *m, int *num_clauses, const unsigned char *ptr, MonoError *error)
{
	unsigned char sect_data_flags;
	int is_fat;
	guint32 sect_data_len;
	MonoExceptionClause* clauses = NULL;

	error_init (error);

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
			clauses = (MonoExceptionClause *)g_malloc0 (sizeof (MonoExceptionClause) * (*num_clauses));
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
						ec->data.catch_class = mono_class_get_checked (m, tof_value, error);
						if (!is_ok (error)) {
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
 * Returns: TRUE if the header was properly decoded.
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

	summary->code = NULL;
	summary->code_size = 0;
	summary->max_stack = 0;
	summary->has_clauses = FALSE;
	summary->has_locals = FALSE;

	/*FIXME extract this into a MACRO and share it with mono_method_get_header*/
	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) || (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return FALSE;

	if (method->wrapper_type != MONO_WRAPPER_NONE || method->sre_method) {
		MonoMethodHeader *header =  ((MonoMethodWrapper *)method)->header;
		if (!header)
			return FALSE;
		summary->code = header->code;
		summary->code_size = header->code_size;
		summary->max_stack = header->max_stack;
		summary->has_clauses = header->num_clauses > 0;
		summary->has_locals = header->num_locals > 0;
		return TRUE;
	}


	idx = mono_metadata_token_index (method->token);
	img = m_class_get_image (method->klass);
	rva = mono_metadata_decode_row_col (&img->tables [MONO_TABLE_METHOD], idx - 1, MONO_METHOD_RVA);

	ptr = mono_image_rva_map (img, rva);
	if (!ptr)
		return FALSE;

	flags = *(const unsigned char *)ptr;
	format = flags & METHOD_HEADER_FORMAT_MASK;

	switch (format) {
	case METHOD_HEADER_TINY_FORMAT:
		ptr++;
		summary->max_stack = 8;
		summary->code = (unsigned char *) ptr;
		summary->code_size = flags >> 2;
		break;
	case METHOD_HEADER_FAT_FORMAT:
		fat_flags = read16 (ptr);
		ptr += 2;
		summary->max_stack = read16 (ptr);
		ptr += 2;
		summary->code_size = read32 (ptr);
		ptr += 4;
		summary->has_locals = !!read32 (ptr);
		ptr += 4;
		if (fat_flags & METHOD_HEADER_MORE_SECTS)
			summary->has_clauses = TRUE;
		summary->code = (unsigned char *) ptr;
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
mono_metadata_parse_mh_full (MonoImage *m, MonoGenericContainer *container, const char *ptr, MonoError *error)
{
	MonoMethodHeader *mh = NULL;
	unsigned char flags = *(const unsigned char *) ptr;
	unsigned char format = flags & METHOD_HEADER_FORMAT_MASK;
	guint16 fat_flags;
	guint16 max_stack;
	guint32 local_var_sig_tok, code_size, init_locals;
	const unsigned char *code;
	MonoExceptionClause* clauses = NULL;
	int num_clauses = 0;
	MonoTableInfo *t = &m->tables [MONO_TABLE_STANDALONESIG];
	guint32 cols [MONO_STAND_ALONE_SIGNATURE_SIZE];

	error_init (error);

	if (!ptr) {
		mono_error_set_bad_image (error, m, "Method header with null pointer");
		return NULL;
	}

	switch (format) {
	case METHOD_HEADER_TINY_FORMAT:
		mh = (MonoMethodHeader *)g_malloc0 (MONO_SIZEOF_METHOD_HEADER);
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
		mono_error_set_bad_image (error, m, "Invalid method header format %d", format);
		return NULL;
	}

	if (local_var_sig_tok) {
		int idx = mono_metadata_token_index (local_var_sig_tok) - 1;
		if (mono_metadata_table_bounds_check (m, MONO_TABLE_STANDALONESIG, idx + 1)) {
			mono_error_set_bad_image (error, m, "Invalid method header local vars signature token 0x%08x", idx);
			goto fail;
		}
		mono_metadata_decode_row (t, idx, cols, MONO_STAND_ALONE_SIGNATURE_SIZE);

	}
	if (fat_flags & METHOD_HEADER_MORE_SECTS) {
		clauses = parse_section_data (m, &num_clauses, (const unsigned char*)ptr, error);
		goto_if_nok (error, fail);
	}
	if (local_var_sig_tok) {
		const char *locals_ptr;
		guint16 len=0, i;

		locals_ptr = mono_metadata_blob_heap (m, cols [MONO_STAND_ALONE_SIGNATURE]);
		mono_metadata_decode_blob_size (locals_ptr, &locals_ptr);
		if (*locals_ptr != 0x07)
			g_warning ("wrong signature for locals blob");
		locals_ptr++;
		len = GUINT32_TO_UINT16 (mono_metadata_decode_value (locals_ptr, &locals_ptr));
		mh = (MonoMethodHeader *)g_malloc0 (MONO_SIZEOF_METHOD_HEADER + len * sizeof (MonoType*) + num_clauses * sizeof (MonoExceptionClause));
		mh->num_locals = len;
		for (i = 0; i < len; ++i) {
			mh->locals [i] = mono_metadata_parse_type_internal (m, container, 0, TRUE, locals_ptr, &locals_ptr, error);
			goto_if_nok (error, fail);
		}
	} else {
		mh = (MonoMethodHeader *)g_malloc0 (MONO_SIZEOF_METHOD_HEADER + num_clauses * sizeof (MonoExceptionClause));
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

/**
 * mono_metadata_parse_mh:
 * \param generic_context generics context
 * \param ptr pointer to the method header.
 *
 * Decode the method header at \p ptr, including pointer to the IL code,
 * info about local variables and optional exception tables.
 *
 * \returns a transient \c MonoMethodHeader allocated from the heap.
 */
MonoMethodHeader *
mono_metadata_parse_mh (MonoImage *m, const char *ptr)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = mono_metadata_parse_mh_full (m, NULL, ptr, error);
	mono_error_cleanup (error);
	return header;
}

/**
 * mono_metadata_free_mh:
 * \param mh a method header
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
	if (mh && mh->is_transient) {
		for (i = 0; i < mh->num_locals; ++i)
			mono_metadata_free_type (mh->locals [i]);
		g_free (mh);
	}
}

/**
 * mono_method_header_get_code:
 * \param header a \c MonoMethodHeader pointer
 * \param code_size memory location for returning the code size
 * \param max_stack memory location for returning the max stack
 *
 * Method header accessor to retrieve info about the IL code properties:
 * a pointer to the IL code itself, the size of the code and the max number
 * of stack slots used by the code.
 *
 * \returns pointer to the IL code represented by the method header.
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

/**
 * mono_method_header_get_locals:
 * \param header a \c MonoMethodHeader pointer
 * \param num_locals memory location for returning the number of local variables
 * \param init_locals memory location for returning the init_locals flag
 *
 * Method header accessor to retrieve info about the local variables:
 * an array of local types, the number of locals and whether the locals
 * are supposed to be initialized to 0 on method entry
 *
 * \returns pointer to an array of types of the local variables
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
 * Method header accessor to retrieve the number of exception clauses.
 *
 * Returns: the number of exception clauses present
 */
int
mono_method_header_get_num_clauses (MonoMethodHeader *header)
{
	return header->num_clauses;
}

/**
 * mono_method_header_get_clauses:
 * \param header a \c MonoMethodHeader pointer
 * \param method \c MonoMethod the header belongs to
 * \param iter pointer to a iterator
 * \param clause pointer to a \c MonoExceptionClause structure which will be filled with the info
 *
 * Get the info about the exception clauses in the method. Set \c *iter to NULL to
 * initiate the iteration, then call the method repeatedly until it returns FALSE.
 * At each iteration, the structure pointed to by clause if filled with the
 * exception clause information.
 *
 * \returns TRUE if clause was filled with info, FALSE if there are no more exception
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
	sc = (MonoExceptionClause *)*iter;
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
 * \param m metadata context to extract information from
 * \param ptr pointer to the field signature
 * \param rptr pointer updated to match the end of the decoded stream
 *
 * Parses the field signature, and returns the type information for it.
 *
 * \returns The \c MonoType that was extracted from \p ptr .
 */
MonoType *
mono_metadata_parse_field_type (MonoImage *m, short field_flags, const char *ptr, const char **rptr)
{
	ERROR_DECL (error);
	MonoType * type = mono_metadata_parse_type_internal (m, NULL, field_flags, FALSE, ptr, rptr, error);
	mono_error_cleanup (error);
	return type;
}

/**
 * mono_metadata_parse_param:
 * \param m metadata context to extract information from
 * \param ptr pointer to the param signature
 * \param rptr pointer updated to match the end of the decoded stream
 *
 * Parses the param signature, and returns the type information for it.
 *
 * \returns The \c MonoType that was extracted from \p ptr .
 */
MonoType *
mono_metadata_parse_param (MonoImage *m, const char *ptr, const char **rptr)
{
	ERROR_DECL (error);
	MonoType * type = mono_metadata_parse_type_internal (m, NULL, 0, FALSE, ptr, rptr, error);
	mono_error_cleanup (error);
	return type;
}

/**
 * mono_metadata_token_from_dor:
 * \param dor_token A \c TypeDefOrRef coded index
 *
 * \p dor_token is a \c TypeDefOrRef coded index: it contains either
 * a \c TypeDef, \c TypeRef or \c TypeSpec in the lower bits, and the upper
 * bits contain an index into the table.
 *
 * \returns an expanded token
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
	guint32 idx;			/* The index that we are trying to locate */
	guint32 col_idx;		/* The index in the row where idx may be stored */
	MonoTableInfo *t;		/* pointer to the table */
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
	int typedef_index = GPTRDIFF_TO_INT ((bb - loc->t->base) / loc->t->row_size);
	guint32 col, col_next;

	col = mono_metadata_decode_row_col (loc->t, typedef_index, loc->col_idx);

	if (loc->idx < col)
		return -1;

	/*
	 * Need to check that the next row is valid.
	 */
	g_assert (typedef_index >= 0);
	if (GINT_TO_UINT32(typedef_index) + 1 < table_info_get_rows (loc->t)) {
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
	guint32 table_index = GPTRDIFF_TO_INT ((bb - loc->t->base) / loc->t->row_size);
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
	guint32 table_index = GPTRDIFF_TO_UINT32 ((bb - loc->t->base) / loc->t->row_size);
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
	int rows = table_info_get_rows (ptrdef);
	int i;

	/* Use a linear search to find our index in the table */
	for (i = 0; i < rows; i ++)
		/* All the Ptr tables have the same structure */
		if (mono_metadata_decode_row_col (ptrdef, i, 0) == idx)
			break;

	if (i < rows)
		return i + 1;
	else
		return idx;
}

/**
 * mono_metadata_typedef_from_field:
 * \param meta metadata context
 * \param index FieldDef token
 *
 * \returns the 1-based index into the \c TypeDef table of the type that
 * declared the field described by \p index, or 0 if not found.
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

	/* if it's not in the base image, look in the hot reload table */
	gboolean added = (loc.idx > table_info_get_rows (&meta->tables [MONO_TABLE_FIELD]));
	if (added) {
		uint32_t res = mono_component_hot_reload()->field_parent (meta, loc.idx);
		return res; /* 0 if not found, otherwise 1-based */
	}

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, typedef_locator))
		return 0;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/**
 * mono_metadata_typedef_from_method:
 * \param meta metadata context
 * \param index \c MethodDef token
 * \returns the 1-based index into the \c TypeDef table of the type that
 * declared the method described by \p index.  0 if not found.
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

	/* if it's not in the base image, look in the hot reload table */
	gboolean added = (loc.idx > table_info_get_rows (&meta->tables [MONO_TABLE_METHOD]));
	if (added) {
		uint32_t res = mono_component_hot_reload ()->method_parent (meta, loc.idx);
		return res; /* 0 if not found, otherwise 1-based */
	}

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, typedef_locator))
		return 0;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/**
 * mono_metadata_interfaces_from_typedef_full:
 * \param meta metadata context
 * \param index typedef token
 * \param interfaces Out parameter used to store the interface array
 * \param count Out parameter used to store the number of interfaces
 * \param heap_alloc_result if TRUE the result array will be \c g_malloc'd
 * \param context The generic context
 * \param error set on error
 *
 * The array of interfaces that the \p index typedef token implements is returned in
 * \p interfaces. The number of elements in the array is returned in \p count.
 *
 * \returns \c TRUE on success, \c FALSE on failure and sets \p error.
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

	error_init (error);

	if (!tdef->base && !meta->has_updates)
		return TRUE;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_INTERFACEIMPL_CLASS;
	loc.t = tdef;
	loc.result = 0;

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;

	if (!found && !meta->has_updates)
		return TRUE;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (meta, tdef, &loc, table_locator)) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "NO Found interfaces for class 0x%08x", index);
			return TRUE;
		}
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_METADATA_UPDATE, "Found interfaces for class 0x%08x starting at 0x%08x", index, loc.result);
	}

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
	guint32 rows = mono_metadata_table_num_rows (meta, MONO_TABLE_INTERFACEIMPL);
	while (pos < rows) {
		mono_metadata_decode_row (tdef, pos, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		++pos;
	}

	if (heap_alloc_result)
		result = g_new0 (MonoClass*, pos - start);
	else
		result = (MonoClass **)mono_image_alloc0 (meta, sizeof (MonoClass*) * (pos - start));

	pos = start;
	while (pos < rows) {
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

/**
 * mono_metadata_interfaces_from_typedef:
 * \param meta metadata context
 * \param index typedef token
 * \param count Out parameter used to store the number of interfaces
 *
 * The array of interfaces that the \p index typedef token implements is returned in
 * \p interfaces. The number of elements in the array is returned in \p count. The returned
 * array is allocated with \c g_malloc and the caller must free it.
 *
 * LOCKING: Acquires the loader lock .
 *
 * \returns the interface array on success, NULL on failure.
 */
MonoClass**
mono_metadata_interfaces_from_typedef (MonoImage *meta, guint32 index, guint *count)
{
	ERROR_DECL (error);
	MonoClass **interfaces = NULL;
	gboolean rv;

	rv = mono_metadata_interfaces_from_typedef_full (meta, index, &interfaces, count, TRUE, NULL, error);
	mono_error_assert_ok (error);
	if (rv)
		return interfaces;
	else
		return NULL;
}

/**
 * mono_metadata_nested_in_typedef:
 * \param meta metadata context
 * \param index typedef token
 * \returns the 1-based index into the TypeDef table of the type
 * where the type described by \p index is nested.
 * Returns 0 if \p index describes a non-nested type.
 */
guint32
mono_metadata_nested_in_typedef (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_NESTEDCLASS];
	locator_t loc;

	if (!tdef->base && !meta->has_updates)
		return 0;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_NESTED_CLASS_NESTED;
	loc.t = tdef;
	loc.result = 0;

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;
	if (!found && !meta->has_updates)
		return 0;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (meta, tdef, &loc, table_locator))
			return 0;
	}

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return mono_metadata_decode_row_col (tdef, loc.result, MONO_NESTED_CLASS_ENCLOSING) | MONO_TOKEN_TYPE_DEF;
}

/**
 * mono_metadata_nesting_typedef:
 * \param meta metadata context
 * \param index typedef token
 * \returns the 1-based index into the \c TypeDef table of the first type
 * that is nested inside the type described by \p index. The search starts at
 * \p start_index. Returns 0 if no such type is found.
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

	guint32 rows = mono_metadata_table_num_rows (meta, MONO_TABLE_NESTEDCLASS);
	while (start <= rows) {
		if (class_index == mono_metadata_decode_row_col (tdef, start - 1, MONO_NESTED_CLASS_ENCLOSING))
			break;
		else
			start++;
	}

	if (start > rows)
		return 0;
	else
		return start;
}

/**
 * mono_metadata_packing_from_typedef:
 * \param meta metadata context
 * \param index token representing a type
 * \returns the info stored in the \c ClassLayout table for the given typedef token
 * into the \p packing and \p size pointers.
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

	/* FIXME: metadata-update */

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator))
		return 0;

	mono_metadata_decode_row (tdef, loc.result, cols, MONO_CLASS_LAYOUT_SIZE);
	if (packing)
		*packing = cols [MONO_CLASS_LAYOUT_PACKING_SIZE];
	if (size)
		*size = cols [MONO_CLASS_LAYOUT_CLASS_SIZE];

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/**
 * mono_metadata_custom_attrs_from_index:
 * \param meta metadata context
 * \param index token representing the parent
 * \returns: the 1-based index into the \c CustomAttribute table of the first
 * attribute which belongs to the metadata object described by \p index.
 * Returns 0 if no such attribute is found.
 */
guint32
mono_metadata_custom_attrs_from_index (MonoImage *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	locator_t loc;

	if (!tdef->base && !meta->has_updates)
		return 0;

	loc.idx = index;
	loc.col_idx = MONO_CUSTOM_ATTR_PARENT;
	loc.t = tdef;
	loc.result = 0;

	/* FIXME: Index translation */

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;
	if (!found && !meta->has_updates)
		return 0;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (meta, tdef, &loc, table_locator))
			return 0;
	}

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_CUSTOM_ATTR_PARENT) == index))
		loc.result --;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/**
 * mono_metadata_declsec_from_index:
 * \param meta metadata context
 * \param index token representing the parent
 * \returns the 0-based index into the \c DeclarativeSecurity table of the first
 * attribute which belongs to the metadata object described by \p index.
 * Returns \c -1 if no such attribute is found.
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

	/* FIXME: metadata-update */

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, declsec_locator))
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

	/* FIXME: metadata-update */

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator))
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

/**
 * mono_type_size:
 * \param t the type to return the size of
 * \returns The number of bytes required to hold an instance of this
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
	if (m_type_is_byref (t)) {
		*align = MONO_ABI_ALIGNOF (gpointer);
		return MONO_ABI_SIZEOF (gpointer);
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
		return MONO_ABI_SIZEOF (gpointer);
	case MONO_TYPE_VALUETYPE: {
		if (m_class_is_enumtype (t->data.klass))
			return mono_type_size (mono_class_enum_basetype_internal (t->data.klass), align);
		else
			return mono_class_value_size (t->data.klass, (guint32*)align);
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
		*align = MONO_ABI_ALIGNOF (gpointer);
		return MONO_ABI_SIZEOF (gpointer);
	case MONO_TYPE_TYPEDBYREF:
		return mono_class_value_size (mono_defaults.typed_reference_class, (guint32*)align);
	case MONO_TYPE_GENERICINST: {
		MonoGenericClass *gclass = t->data.generic_class;
		MonoClass *container_class = gclass->container_class;

		// g_assert (!gclass->inst->is_open);

		if (m_class_is_valuetype (container_class)) {
			if (m_class_is_enumtype (container_class))
				return mono_type_size (mono_class_enum_basetype_internal (container_class), align);
			else
				return mono_class_value_size (mono_class_from_mono_type_internal (t), (guint32*)align);
		} else {
			*align = MONO_ABI_ALIGNOF (gpointer);
			return MONO_ABI_SIZEOF (gpointer);
		}
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (!t->data.generic_param->gshared_constraint || t->data.generic_param->gshared_constraint->type == MONO_TYPE_VALUETYPE) {
			*align = MONO_ABI_ALIGNOF (gpointer);
			return MONO_ABI_SIZEOF (gpointer);
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

/**
 * mono_type_stack_size:
 * \param t the type to return the size it uses on the stack
 * \returns The number of bytes required to hold an instance of this
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
	int stack_slot_size = TARGET_SIZEOF_VOID_P;
	int stack_slot_align = TARGET_SIZEOF_VOID_P;

	g_assert (t != NULL);

	if (!align)
		align = &tmp;

	if (m_type_is_byref (t)) {
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

		if (m_class_is_enumtype (t->data.klass))
			return mono_type_stack_size_internal (mono_class_enum_basetype_internal (t->data.klass), align, allow_open);
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

		if (m_class_is_valuetype (container_class)) {
			if (m_class_is_enumtype (container_class))
				return mono_type_stack_size_internal (mono_class_enum_basetype_internal (container_class), align, allow_open);
			else {
				guint32 size = mono_class_value_size (mono_class_from_mono_type_internal (t), (guint32*)align);

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
	return m_class_is_valuetype (type->data.generic_class->container_class);
}

/**
 * mono_metadata_generic_class_is_valuetype:
 */
gboolean
mono_metadata_generic_class_is_valuetype (MonoGenericClass *gclass)
{
	return m_class_is_valuetype (gclass->container_class);
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
	MonoGenericInst *i2 = mono_class_get_generic_container (c2)->context.class_inst;

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

/**
 * mono_metadata_type_hash:
 * \param t1 a type
 * Computes a hash value for \p t1 to be used in \c GHashTable.
 * The returned hash is guaranteed to be the same across executions.
 */
guint
mono_metadata_type_hash (MonoType *t1)
{
	guint hash = t1->type;

	hash |= (m_type_is_byref (t1) ? 1 : 0) << 6; /* do not collide with t1->type values */
	switch (t1->type) {
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY: {
		MonoClass *klass = t1->data.klass;
		/*
		 * Dynamic classes must not be hashed on their type since it can change
		 * during runtime. For example, if we hash a reference type that is
		 * later made into a valuetype.
		 *
		 * This is specially problematic with generic instances since they are
		 * inserted in a bunch of hash tables before been finished.
		 */
		if (image_is_dynamic (m_class_get_image (klass)))
			return ((m_type_is_byref (t1) ? 1 : 0) << 6) | mono_metadata_str_hash (m_class_get_name (klass));
		return ((hash << 5) - hash) ^ mono_metadata_str_hash (m_class_get_name (klass));
	}
	case MONO_TYPE_PTR:
		return ((hash << 5) - hash) ^ mono_metadata_type_hash (t1->data.type);
	case MONO_TYPE_ARRAY:
		return ((hash << 5) - hash) ^ mono_metadata_type_hash (m_class_get_byval_arg (t1->data.array->eklass));
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
	if (!p->owner->is_anonymous)
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
	 */
	if (mono_generic_param_owner (p1) == mono_generic_param_owner (p2))
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
	if (mono_class_is_ginst (c1) && mono_class_is_ginst (c2))
		return _mono_metadata_generic_class_equal (mono_class_get_generic_class (c1), mono_class_get_generic_class (c2), signature_only);
	if (mono_class_is_ginst (c1) && mono_class_is_gtd (c2))
		return _mono_metadata_generic_class_container_equal (mono_class_get_generic_class (c1), c2, signature_only);
	if (mono_class_is_gtd (c1) && mono_class_is_ginst (c2))
		return _mono_metadata_generic_class_container_equal (mono_class_get_generic_class (c2), c1, signature_only);
	MonoType *c1_type = m_class_get_byval_arg (c1);
	MonoType *c2_type = m_class_get_byval_arg (c2);
	if ((c1_type->type == MONO_TYPE_VAR) && (c2_type->type == MONO_TYPE_VAR))
		return mono_metadata_generic_param_equal_internal (
			c1_type->data.generic_param, c2_type->data.generic_param, signature_only);
	if ((c1_type->type == MONO_TYPE_MVAR) && (c2_type->type == MONO_TYPE_MVAR))
		return mono_metadata_generic_param_equal_internal (
			c1_type->data.generic_param, c2_type->data.generic_param, signature_only);
	if (signature_only &&
	    (c1_type->type == MONO_TYPE_SZARRAY) && (c2_type->type == MONO_TYPE_SZARRAY))
		return mono_metadata_class_equal (c1_type->data.klass, c2_type->data.klass, signature_only);
	if (signature_only &&
	    (c1_type->type == MONO_TYPE_ARRAY) && (c2_type->type == MONO_TYPE_ARRAY))
		return do_mono_metadata_type_equal (c1_type, c2_type, signature_only);
	if (signature_only &&
		(c1_type->type == MONO_TYPE_PTR) && (c2_type->type == MONO_TYPE_PTR))
		return do_mono_metadata_type_equal (c1_type->data.type, c2_type->data.type, signature_only);
	if (signature_only &&
		(c1_type->type == MONO_TYPE_FNPTR) && (c2_type->type == MONO_TYPE_FNPTR))
		return mono_metadata_fnptr_equal (c1_type->data.method, c2_type->data.method, signature_only);
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
		MonoType *t1 = mono_signature_get_params_internal (s1, &iter1);
		MonoType *t2 = mono_signature_get_params_internal (s2, &iter2);

		if (t1 == NULL || t2 == NULL)
			return (t1 == t2);
		if (! do_mono_metadata_type_equal (t1, t2, signature_only))
			return FALSE;
	}
}

static gboolean
mono_metadata_custom_modifiers_equal (MonoType *t1, MonoType *t2, gboolean signature_only)
{
	// ECMA 335, 7.1.1:
	// The CLI itself shall treat required and optional modifiers in the same manner.
	// Two signatures that differ only by the addition of a custom modifier
	// (required or optional) shall not be considered to match.
	uint8_t count = mono_type_custom_modifier_count (t1);
	if (count != mono_type_custom_modifier_count (t2))
		return FALSE;

	for (uint8_t i=0; i < count; i++) {
		// FIXME: propagate error to caller
		ERROR_DECL (error);
		gboolean cm1_required, cm2_required;

		MonoType *cm1_type = mono_type_get_custom_modifier (t1, i, &cm1_required, error);
		mono_error_assert_ok (error);
		MonoType *cm2_type = mono_type_get_custom_modifier (t2, i, &cm2_required, error);
		mono_error_assert_ok (error);

		if (cm1_required != cm2_required)
			return FALSE;

		if (!do_mono_metadata_type_equal (cm1_type, cm2_type, signature_only))
			return FALSE;
	}
	return TRUE;
}

/*
 * mono_metadata_type_equal:
 * @t1: a type
 * @t2: another type
 * @signature_only: If true, treat ginsts as equal which are instantiated separately but have equal positional value
 *
 * Determine if @t1 and @t2 represent the same type.
 * Returns: #TRUE if @t1 and @t2 are equal.
 */
static gboolean
do_mono_metadata_type_equal (MonoType *t1, MonoType *t2, gboolean signature_only)
{
	if (t1->type != t2->type || m_type_is_byref (t1) != m_type_is_byref (t2))
		return FALSE;

	gboolean cmod_reject = FALSE;

	if (t1->has_cmods != t2->has_cmods)
		cmod_reject = TRUE;
	else if (t1->has_cmods && t2->has_cmods) {
		cmod_reject = !mono_metadata_custom_modifiers_equal (t1, t2, signature_only);
	}

	gboolean result = FALSE;

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
		result = TRUE;
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
		result = mono_metadata_class_equal (t1->data.klass, t2->data.klass, signature_only);
		break;
	case MONO_TYPE_PTR:
		result = do_mono_metadata_type_equal (t1->data.type, t2->data.type, signature_only);
		break;
	case MONO_TYPE_ARRAY:
		if (t1->data.array->rank != t2->data.array->rank)
			result = FALSE;
		else
			result = mono_metadata_class_equal (t1->data.array->eklass, t2->data.array->eklass, signature_only);
		break;
	case MONO_TYPE_GENERICINST:
		result = _mono_metadata_generic_class_equal (
			t1->data.generic_class, t2->data.generic_class, signature_only);
		break;
	case MONO_TYPE_VAR:
		result = mono_metadata_generic_param_equal_internal (
			t1->data.generic_param, t2->data.generic_param, signature_only);
		break;
	case MONO_TYPE_MVAR:
		result = mono_metadata_generic_param_equal_internal (
			t1->data.generic_param, t2->data.generic_param, signature_only);
		break;
	case MONO_TYPE_FNPTR:
		result = mono_metadata_fnptr_equal (t1->data.method, t2->data.method, signature_only);
		break;
	default:
		g_error ("implement type compare for %0x!", t1->type);
		return FALSE;
	}

	return result && !cmod_reject;
}

/**
 * mono_metadata_type_equal:
 */
gboolean
mono_metadata_type_equal (MonoType *t1, MonoType *t2)
{
	return do_mono_metadata_type_equal (t1, t2, FALSE);
}

/**
 * mono_metadata_type_equal_full:
 * \param t1 a type
 * \param t2 another type
 * \param signature_only if signature only comparison should be made
 *
 * Determine if \p t1 and \p t2 are signature compatible if \p signature_only is TRUE, otherwise
 * behaves the same way as mono_metadata_type_equal.
 * The function mono_metadata_type_equal(a, b) is just a shortcut for mono_metadata_type_equal_full(a, b, FALSE).
 * \returns TRUE if \p t1 and \p t2 are equal taking \p signature_only into account.
 */
gboolean
mono_metadata_type_equal_full (MonoType *t1, MonoType *t2, gboolean signature_only)
{
	return do_mono_metadata_type_equal (t1, t2, signature_only);
}

enum {
	SIG_EQUIV_FLAG_NO_RET = 1,
};

gboolean
signature_equiv (MonoMethodSignature *sig1, MonoMethodSignature *sig2, int flags);


/**
 * mono_metadata_signature_equal:
 * \param sig1 a signature
 * \param sig2 another signature
 *
 * Determine if \p sig1 and \p sig2 represent the same signature, with the
 * same number of arguments and the same types.
 * \returns TRUE if \p sig1 and \p sig2 are equal.
 */
gboolean
mono_metadata_signature_equal (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	return signature_equiv (sig1, sig2, 0);
}

gboolean
mono_metadata_signature_equal_no_ret (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	return signature_equiv (sig1, sig2, SIG_EQUIV_FLAG_NO_RET);
}


gboolean
signature_equiv (MonoMethodSignature *sig1, MonoMethodSignature *sig2, int equiv_flags)
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

	if ((equiv_flags & SIG_EQUIV_FLAG_NO_RET) != 0)
		return TRUE;
	if (!do_mono_metadata_type_equal (sig1->ret, sig2->ret, TRUE))
		return FALSE;
	return TRUE;
}

MonoType *
mono_type_get_custom_modifier (const MonoType *ty, uint8_t idx, gboolean *required, MonoError *error)
{
	g_assert (ty->has_cmods);
	if (mono_type_is_aggregate_mods (ty)) {
		MonoAggregateModContainer *amods = mono_type_get_amods (ty);
		g_assert (idx < amods->count);
		MonoSingleCustomMod *cmod = &amods->modifiers [idx];
		if (required)
			*required = !!cmod->required;
		return cmod->type;
	} else {
		MonoCustomModContainer *cmods = mono_type_get_cmods (ty);
		g_assert (idx < cmods->count);
		MonoCustomMod *cmod = &cmods->modifiers [idx];
		if (required)
			*required = !!cmod->required;
		MonoImage *image = cmods->image;
		uint32_t token = cmod->token;
		return mono_type_get_checked (image, token, NULL, error);
	}
}


/**
 * mono_metadata_type_dup:
 * \param image image to alloc memory from
 * \param original type to duplicate
 * \returns copy of type allocated from the image's mempool (or from the heap, if \p image is null).
 */
MonoType *
mono_metadata_type_dup (MonoImage *image, const MonoType *o)
{
	return mono_metadata_type_dup_with_cmods (image, o, o);
}

static void
deep_type_dup_fixup (MonoImage *image, MonoType *r, const MonoType *o);

static uint8_t
custom_modifier_copy (MonoAggregateModContainer *dest, uint8_t dest_offset, const MonoType *source)
{
	if (mono_type_is_aggregate_mods (source)) {
		MonoAggregateModContainer *src_cmods = mono_type_get_amods (source);
		memcpy (&dest->modifiers [dest_offset], &src_cmods->modifiers[0], src_cmods->count * sizeof (MonoSingleCustomMod));
		dest_offset += src_cmods->count;
	} else {
		MonoCustomModContainer *src_cmods = mono_type_get_cmods (source);
		for (int i = 0; i < src_cmods->count; i++) {
			ERROR_DECL (error); // XXX FIXME: AK - propagate the error to the caller.
			MonoSingleCustomMod *cmod = &dest->modifiers [dest_offset++];
			cmod->type = mono_type_get_checked (src_cmods->image, src_cmods->modifiers [i].token, NULL, error);
			mono_error_assert_ok (error);
			cmod->required = src_cmods->modifiers [i].required;
		}
	}
	return dest_offset;
}

/* makes a dup of 'o' but also appends the custom modifiers from 'cmods_source' */
static MonoType *
do_metadata_type_dup_append_cmods (MonoImage *image, const MonoType *o, const MonoType *cmods_source)
{
	g_assert (o != cmods_source);
	g_assert (o->has_cmods);
	g_assert (cmods_source->has_cmods);
	if (!mono_type_is_aggregate_mods (o) &&
	    !mono_type_is_aggregate_mods (cmods_source) &&
	    mono_type_get_cmods (o)->image == mono_type_get_cmods (cmods_source)->image) {
		/* the uniform case: all the cmods are from the same image. */
		MonoCustomModContainer *o_cmods = mono_type_get_cmods (o);
		MonoCustomModContainer *extra_cmods = mono_type_get_cmods (cmods_source);
		uint8_t total_cmods = o_cmods->count + extra_cmods->count;
		gboolean aggregate = FALSE;
		size_t sizeof_dup = mono_sizeof_type_with_mods (total_cmods, aggregate);
		MonoType *r = image ? (MonoType *)mono_image_alloc0 (image, (guint)sizeof_dup) : (MonoType *)g_malloc0 (sizeof_dup);

		mono_type_with_mods_init (r, total_cmods, aggregate);

		/* copy the original type o, not including its modifiers */
		memcpy (r, o, mono_sizeof_type_with_mods (0, FALSE));
		deep_type_dup_fixup (image, r, o);

		/* The modifier order matters to Roslyn, they expect the extra cmods to come first:
		 *
		 * Suppose we substitute 'int32 modopt(IsLong)' for 'T' in 'void Test
		 * (T modopt(IsConst))'.  Roslyn expects the result to be 'void Test
		 * (int32 modopt(IsLong) modopt(IsConst))'.
		 *
		 * but! cmods are encoded in IL in reverse order, so 'int32 modopt(IsConst) modopt(IsLong)' is
		 * encoded as `cmod_opt [typeref IsLong] cmod_opt [typeref IsConst] I4`
		 * so in our array, extra_cmods (IsLong) come first, followed by o_cmods (IsConst)
		 *
		 * (Here 'o' is 'int32 modopt(IsLong)' and cmods_source is 'T modopt(IsConst)')
		 */
		/* append the modifiers from cmods_source and o */
		MonoCustomModContainer *r_container = mono_type_get_cmods (r);
		uint8_t dest_offset = 0;
		r_container->image = extra_cmods->image;

		memcpy (&r_container->modifiers [dest_offset], &o_cmods->modifiers [0], o_cmods->count * sizeof (MonoCustomMod));
		dest_offset += o_cmods->count;
		memcpy (&r_container->modifiers [dest_offset], &extra_cmods->modifiers [0], extra_cmods->count * sizeof (MonoCustomMod));
		dest_offset += extra_cmods->count;
		g_assert (dest_offset == total_cmods);

		return r;
	} else {
		/* The aggregate case: either o_cmods or extra_cmods has aggregate cmods, or they're both simple but from different images. */
		uint8_t total_cmods = 0;
		total_cmods += mono_type_custom_modifier_count (o);
		total_cmods += mono_type_custom_modifier_count (cmods_source);

		gboolean aggregate = TRUE;
		size_t sizeof_dup = mono_sizeof_type_with_mods (total_cmods, aggregate);

		/* FIXME: if image, and the images of the custom modifiers from
		 * o and cmods_source are all different, we need an image
		 * set... */
		MonoType *r = image ? (MonoType *)mono_image_alloc0 (image, (guint)sizeof_dup) : (MonoType*)g_malloc0 (sizeof_dup);

		mono_type_with_mods_init (r, total_cmods, aggregate);

		memcpy (r, o, mono_sizeof_type_with_mods (0, FALSE));
		deep_type_dup_fixup (image, r, o);

		/* Try not to blow up the stack. See comment on
		 * MONO_MAX_EXPECTED_CMODS.  Since here we're appending all the
		 * mods together, it's possible we'll end up with more than the
		 * maximum allowed.  If that ever happens in practice, we
		 * should redefine the bound and possibly make this function
		 * fail dynamically instead of asserting.
		 */
		g_assert (total_cmods < MONO_MAX_EXPECTED_CMODS);
		size_t r_container_size = mono_sizeof_aggregate_modifiers (total_cmods);
		MonoAggregateModContainer *r_container_candidate = g_alloca (r_container_size);
		memset (r_container_candidate, 0, r_container_size);
		uint8_t dest_offset = 0;

		dest_offset = custom_modifier_copy (r_container_candidate, dest_offset, o);
		dest_offset = custom_modifier_copy (r_container_candidate, dest_offset, cmods_source);
		g_assert (dest_offset == total_cmods);
		r_container_candidate->count = total_cmods;

		mono_type_set_amods (r, mono_metadata_get_canonical_aggregate_modifiers (r_container_candidate));

		return r;
	}
}

/**
 * Works the same way as mono_metadata_type_dup but pick cmods from @cmods_source
 */
MonoType *
mono_metadata_type_dup_with_cmods (MonoImage *image, const MonoType *o, const MonoType *cmods_source)
{
	if (o->has_cmods && o != cmods_source && cmods_source->has_cmods) {
		return do_metadata_type_dup_append_cmods (image, o, cmods_source);
	}

	MonoType *r = NULL;

	/* if we get here, either o and cmods_source alias, or else exactly one of them has cmods. */

	uint8_t num_mods = MAX (mono_type_custom_modifier_count (o), mono_type_custom_modifier_count (cmods_source));
	gboolean aggregate = mono_type_is_aggregate_mods (o) || mono_type_is_aggregate_mods (cmods_source);
	size_t sizeof_r = mono_sizeof_type_with_mods (num_mods, aggregate);

	r = image ? (MonoType *)mono_image_alloc0 (image, (guint)sizeof_r) : (MonoType *)g_malloc0 (sizeof_r);

	if (cmods_source->has_cmods) {
		/* FIXME: if it's aggregate what do we assert here? */
		g_assert (!image || (!aggregate && image == mono_type_get_cmods (cmods_source)->image));
		memcpy (r, cmods_source, mono_sizeof_type (cmods_source));
	}

	memcpy (r, o, mono_sizeof_type (o));

	/* reset custom mod count and aggregateness to be correct. */
	mono_type_with_mods_init (r, num_mods, aggregate);
	if (aggregate)
		mono_type_set_amods (r, mono_type_is_aggregate_mods (o) ? mono_type_get_amods (o) : mono_type_get_amods (cmods_source));
	deep_type_dup_fixup (image, r, o);
	return r;
}


static void
deep_type_dup_fixup (MonoImage *image, MonoType *r, const MonoType *o)
{
	if (o->type == MONO_TYPE_PTR) {
		r->data.type = mono_metadata_type_dup (image, o->data.type);
	} else if (o->type == MONO_TYPE_ARRAY) {
		r->data.array = mono_dup_array_type (image, o->data.array);
	} else if (o->type == MONO_TYPE_FNPTR) {
		/*FIXME the dup'ed signature is leaked mono_metadata_free_type*/
		r->data.method = mono_metadata_signature_deep_dup (image, o->data.method);
	}
}

/**
 * mono_signature_hash:
 */
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
		*p++ = GUINT32_TO_CHAR (value);
	else if (value < 0x4000) {
		p [0] = GUINT32_TO_CHAR (0x80 | (value >> 8));
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

/**
 * mono_metadata_field_info:
 * \param meta the Image the field is defined in
 * \param index the index in the field table representing the field
 * \param offset a pointer to an integer where to store the offset that  may have been specified for the field in a FieldLayout table
 * \param rva a pointer to the RVA of the field data in the image that may have been defined in a \c FieldRVA table
 * \param marshal_spec a pointer to the marshal spec that may have been defined for the field in a \c FieldMarshal table.
 *
 * Gather info for field \p index that may have been defined in the \c FieldLayout,
 * \c FieldRVA and \c FieldMarshal tables.
 * Either of \p offset, \p rva and \p marshal_spec can be NULL if you're not interested
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

		/* FIXME: metadata-update */

		if (tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator)) {
			*offset = mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_LAYOUT_OFFSET);
		} else {
			*offset = (guint32)-1;
		}
	}
	if (rva) {
		tdef = &meta->tables [MONO_TABLE_FIELDRVA];

		loc.col_idx = MONO_FIELD_RVA_FIELD;
		loc.t = tdef;

		if (tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator)) {
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

/**
 * mono_metadata_get_constant_index:
 * \param meta the Image the field is defined in
 * \param index the token that may have a row defined in the constants table
 * \param hint possible position for the row
 *
 * \p token must be a \c FieldDef, \c ParamDef or \c PropertyDef token.
 *
 * \returns the index into the \c Constants table or 0 if not found.
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

	if ((hint > 0) && (hint < table_info_get_rows (tdef)) && (mono_metadata_decode_row_col (tdef, hint - 1, MONO_CONSTANT_PARENT) == index))
		return hint;

	if (tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator)) {
		return loc.result + 1;
	}

	if (G_UNLIKELY (meta->has_updates)) {
		if (mono_metadata_update_metadata_linear_search (meta, tdef, &loc, table_locator))
			return loc.result + 1;
	}
	return 0;
}

/**
 * mono_metadata_events_from_typedef:
 * \param meta metadata context
 * \param index 0-based index (in the \c TypeDef table) describing a type
 * \returns the 0-based index in the \c Event table for the events in the
 * type. The last event that belongs to the type (plus 1) is stored
 * in the \p end_idx pointer.
 */
guint32
mono_metadata_events_from_typedef (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint32 start, end;
	MonoTableInfo *tdef  = &meta->tables [MONO_TABLE_EVENTMAP];

	*end_idx = 0;

	if (!tdef->base && !meta->has_updates)
		return 0;

	loc.t = tdef;
	loc.col_idx = MONO_EVENT_MAP_PARENT;
	loc.idx = index + 1;
	loc.result = 0;

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;
	if (!found && !meta->has_updates)
		return 0;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found) {
			uint32_t count;
			if (metadata_update_get_typedef_skeleton_events (meta, mono_metadata_make_token (MONO_TABLE_TYPEDEF, index + 1), &start, &count)) {
				*end_idx = start + count - 1;
				return start - 1;
			} else {
				return 0;
			}
		}
	}

	start = mono_metadata_decode_row_col (tdef, loc.result, MONO_EVENT_MAP_EVENTLIST);
	if (loc.result + 1 < table_info_get_rows (tdef)) {
		end = mono_metadata_decode_row_col (tdef, loc.result + 1, MONO_EVENT_MAP_EVENTLIST) - 1;
	} else {
		end = table_info_get_rows (&meta->tables [MONO_TABLE_EVENT]);
	}

	*end_idx = end;
	return start - 1;
}

/**
 * mono_metadata_methods_from_event:
 * \param meta metadata context
 * \param index 0-based index (in the \c Event table) describing a event
 * \returns the 0-based index in the \c MethodDef table for the methods in the
 * event. The last method that belongs to the event (plus 1) is stored
 * in the \p end_idx pointer.
 */
guint32
mono_metadata_methods_from_event   (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint32 start, end;
	guint32 cols [MONO_METHOD_SEMA_SIZE];
	MonoTableInfo *msemt = &meta->tables [MONO_TABLE_METHODSEMANTICS];

	*end_idx = 0;
	if (!msemt->base && !meta->has_updates)
		return 0;

	if (meta->uncompressed_metadata)
	    index = search_ptr_table (meta, MONO_TABLE_EVENT_POINTER, index + 1) - 1;

	loc.t = msemt;
	loc.col_idx = MONO_METHOD_SEMA_ASSOCIATION;
	loc.idx = ((index + 1) << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_EVENT; /* Method association coded index */
	loc.result = 0;

	gboolean found = msemt->base && mono_binary_search (&loc, msemt->base, table_info_get_rows (msemt), msemt->row_size, table_locator) != NULL;

	if (!found && !meta->has_updates)
		return 0;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (meta, msemt, &loc, table_locator))
			return 0;
	}

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
	guint32 rows = mono_metadata_table_num_rows (meta, MONO_TABLE_METHODSEMANTICS);
	while (end < rows) {
		mono_metadata_decode_row (msemt, end, cols, MONO_METHOD_SEMA_SIZE);
		if (cols [MONO_METHOD_SEMA_ASSOCIATION] != loc.idx)
			break;
		++end;
	}
	*end_idx = GUINT32_TO_UINT(end);
	return start;
}

/**
 * mono_metadata_properties_from_typedef:
 * \param meta metadata context
 * \param index 0-based index (in the \c TypeDef table) describing a type
 * \returns the 0-based index in the \c Property table for the properties in the
 * type. The last property that belongs to the type (plus 1) is stored
 * in the \p end_idx pointer.
 */
guint32
mono_metadata_properties_from_typedef (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint32 start, end;
	MonoTableInfo *tdef  = &meta->tables [MONO_TABLE_PROPERTYMAP];

	*end_idx = 0;

	if (!tdef->base && !meta->has_updates)
		return 0;

	loc.t = tdef;
	loc.col_idx = MONO_PROPERTY_MAP_PARENT;
	loc.idx = index + 1;
	loc.result = 0;

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;

	if (!found && !meta->has_updates)
		return 0;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found) {
			uint32_t count;
			if (metadata_update_get_typedef_skeleton_properties (meta, mono_metadata_make_token (MONO_TABLE_TYPEDEF, index + 1), &start, &count)) {
				*end_idx = start + count - 1;
				return start - 1;
			} else {
				return 0;
			}
		}
	}

	start = mono_metadata_decode_row_col (tdef, loc.result, MONO_PROPERTY_MAP_PROPERTY_LIST);
	if (loc.result + 1 < mono_metadata_table_num_rows (meta, MONO_TABLE_PROPERTYMAP)) {
		end = mono_metadata_decode_row_col (tdef, loc.result + 1, MONO_PROPERTY_MAP_PROPERTY_LIST) - 1;
	} else {
		end = mono_metadata_table_num_rows (meta, MONO_TABLE_PROPERTY);
	}

	*end_idx = GUINT32_TO_UINT(end);
	return start - 1;
}

/**
 * mono_metadata_methods_from_property:
 * \param meta metadata context
 * \param index 0-based index (in the \c PropertyDef table) describing a property
 * \returns the 0-based index in the \c MethodDef table for the methods in the
 * property. The last method that belongs to the property (plus 1) is stored
 * in the \p end_idx pointer.
 */
guint32
mono_metadata_methods_from_property   (MonoImage *meta, guint32 index, guint *end_idx)
{
	locator_t loc;
	guint32 start, end;
	guint32 cols [MONO_METHOD_SEMA_SIZE];
	MonoTableInfo *msemt = &meta->tables [MONO_TABLE_METHODSEMANTICS];

	*end_idx = 0;
	if (!msemt->base && !meta->has_updates)
		return 0;

	if (meta->uncompressed_metadata)
	    index = search_ptr_table (meta, MONO_TABLE_PROPERTY_POINTER, index + 1) - 1;

	loc.t = msemt;
	loc.col_idx = MONO_METHOD_SEMA_ASSOCIATION;
	loc.idx = ((index + 1) << MONO_HAS_SEMANTICS_BITS) | MONO_HAS_SEMANTICS_PROPERTY; /* Method association coded index */
	loc.result = 0;

	gboolean found = msemt->base && mono_binary_search (&loc, msemt->base, table_info_get_rows (msemt), msemt->row_size, table_locator) != NULL;

	if (!found && !meta->has_updates)
		return 0;

	if (G_UNLIKELY (meta->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (meta, msemt, &loc, table_locator))
			return 0;
	}

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
	guint32 rows = mono_metadata_table_num_rows (meta, MONO_TABLE_METHODSEMANTICS);
	while (end < rows) {
		mono_metadata_decode_row (msemt, end, cols, MONO_METHOD_SEMA_SIZE);
		if (cols [MONO_METHOD_SEMA_ASSOCIATION] != loc.idx)
			break;
		++end;
	}
	*end_idx = GUINT32_TO_UINT(end);
	return start;
}

/**
 * mono_metadata_implmap_from_method:
 */
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

	/* FIXME: metadata-update */

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator))
		return 0;

	return loc.result + 1;
}

/**
 * mono_type_create_from_typespec:
 * \param image context where the image is created
 * \param type_spec  typespec token
 * \deprecated use \c mono_type_create_from_typespec_checked that has proper error handling
 *
 * Creates a \c MonoType representing the \c TypeSpec indexed by the \p type_spec
 * token.
 */
MonoType *
mono_type_create_from_typespec (MonoImage *image, guint32 type_spec)
{
	ERROR_DECL (error);
	MonoType *type = mono_type_create_from_typespec_checked (image, type_spec, error);
	if (!type)
		 g_error ("Could not create typespec %x due to %s", type_spec, mono_error_get_message (error));
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

	error_init (error);

	type = (MonoType *)mono_conc_hashtable_lookup (image->typespec_cache, GUINT_TO_POINTER (type_spec));
	if (type)
		return type;

	t = &image->tables [MONO_TABLE_TYPESPEC];

	mono_metadata_decode_row (t, idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (image, cols [MONO_TYPESPEC_SIGNATURE]);

	mono_metadata_decode_value (ptr, &ptr);

	type = mono_metadata_parse_type_checked (image, NULL, 0, TRUE, ptr, &ptr, error);
	if (!type)
		return NULL;

	type2 = mono_metadata_type_dup (image, type);
	mono_metadata_free_type (type);

	mono_image_lock (image);

	/* We might leak some data in the image mempool if found */
	type = (MonoType*)mono_conc_hashtable_insert (image->typespec_cache, GUINT_TO_POINTER (type_spec), type2);
	if (!type)
		type = type2;

	mono_image_unlock (image);

	return type;
}


static char*
mono_image_strndup (MonoImage *image, const char *data, guint len)
{
	char *res;
	if (!image)
		return g_strndup (data, len);
	res = (char *)mono_image_alloc (image, len + 1);
	memcpy (res, data, len);
	res [len] = 0;
	return res;
}

/**
 * mono_metadata_parse_marshal_spec:
 */
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
		res = (MonoMarshalSpec *)mono_image_alloc0 (image, sizeof (MonoMarshalSpec));
	else
		res = g_new0 (MonoMarshalSpec, 1);

	len = mono_metadata_decode_value (ptr, &ptr);
	res->native = (MonoMarshalNative)*ptr++;

	if (res->native == MONO_NATIVE_LPARRAY) {
		res->data.array_data.param_num = -1;
		res->data.array_data.num_elem = -1;
		res->data.array_data.elem_mult = -1;

		if (ptr - start <= len)
			res->data.array_data.elem_type = (MonoMarshalNative)*ptr++;
		if (ptr - start <= len)
			res->data.array_data.param_num = GUINT32_TO_INT16 (mono_metadata_decode_value (ptr, &ptr));
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
			res->data.array_data.elem_mult = GUINT32_TO_INT16 (mono_metadata_decode_value (ptr, &ptr));
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
		res->data.safearray_data.elem_type = (MonoMarshalVariant)0;
		res->data.safearray_data.num_elem = 0;
		if (ptr - start <= len)
			res->data.safearray_data.elem_type = (MonoMarshalVariant)*ptr++;
		if (ptr - start <= len)
			res->data.safearray_data.num_elem = *ptr++;
	}
	return res;
}

/**
 * mono_metadata_free_marshal_spec:
 */
void
mono_metadata_free_marshal_spec (MonoMarshalSpec *spec)
{
	if (!spec)
		return;

	if (spec->native == MONO_NATIVE_CUSTOM) {
		g_free (spec->data.custom_data.custom_name);
		g_free (spec->data.custom_data.cookie);
	}
	g_free (spec);
}

/**
 * mono_type_to_unmanaged:
 * The value pointed to by \p conv will contain the kind of marshalling required for this
 * particular type one of the \c MONO_MARSHAL_CONV_ enumeration values.
 * \returns A \c MonoMarshalNative enumeration value (<code>MONO_NATIVE_</code>) value
 * describing the underlying native reprensetation of the type.
 */
guint32 // FIXMEcxx MonoMarshalNative
mono_type_to_unmanaged (MonoType *type, MonoMarshalSpec *mspec, gboolean as_field,
			gboolean unicode, MonoMarshalConv *conv)
{
	MonoMarshalConv dummy_conv;
	int t = type->type;

	if (!conv)
		conv = &dummy_conv;

	*conv = MONO_MARSHAL_CONV_NONE;

	if (m_type_is_byref (type))
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
			case MONO_NATIVE_UTF8STR:
				*conv = MONO_MARSHAL_CONV_STR_UTF8STR;
				return MONO_NATIVE_UTF8STR;
			case MONO_NATIVE_BYVALTSTR:
				if (unicode)
					*conv = MONO_MARSHAL_CONV_STR_BYVALWSTR;
				else
					*conv = MONO_MARSHAL_CONV_STR_BYVALSTR;
				return MONO_NATIVE_BYVALTSTR;
			case MONO_NATIVE_CUSTOM:
				return MONO_NATIVE_CUSTOM;
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
		if (mspec && mspec->native == MONO_NATIVE_CUSTOM)
			return MONO_NATIVE_CUSTOM;

		if (m_class_is_enumtype (type->data.klass)) {
			t = mono_class_enum_basetype_internal (type->data.klass)->type;
			goto handle_enum;
		}
		if (type->data.klass == mono_class_try_get_handleref_class ()){
			*conv = MONO_MARSHAL_CONV_HANDLEREF;
			return MONO_NATIVE_INT;
		}
		return MONO_NATIVE_STRUCT;
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_BYVALARRAY:
				if ((m_class_get_element_class (type->data.klass) == mono_defaults.char_class) && !unicode)
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
			case MONO_NATIVE_CUSTOM:
				return MONO_NATIVE_CUSTOM;
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
				// [MarshalAs(UnmanagedType.Struct)]
				// object field;
				//
				// becomes a VARIANT
				//
				// [MarshalAs(UnmangedType.Struct)]
				// SomeClass field;
				//
				// becomes uses the CONV_OBJECT_STRUCT conversion
				if (t != MONO_TYPE_OBJECT)
					*conv = MONO_MARSHAL_CONV_OBJECT_STRUCT;
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
							     				m_class_get_parent (type->data.klass) == mono_defaults.multicastdelegate_class)) {
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
					     m_class_get_parent (type->data.klass) == mono_defaults.multicastdelegate_class)) {
			*conv = MONO_MARSHAL_CONV_DEL_FTN;
			return MONO_NATIVE_FUNC;
		}
		if (mono_class_try_get_safehandle_class () && type->data.klass != NULL &&
			mono_class_is_subclass_of_internal (type->data.klass,  mono_class_try_get_safehandle_class (), FALSE)){
			*conv = MONO_MARSHAL_CONV_SAFEHANDLE;
			return MONO_NATIVE_INT;
		}
#ifndef DISABLE_COM
		if (t == MONO_TYPE_CLASS && mono_cominterop_is_interface (type->data.klass)){
			*conv = MONO_MARSHAL_CONV_OBJECT_INTERFACE;
			return MONO_NATIVE_INTERFACE;
		}
#endif
		*conv = MONO_MARSHAL_CONV_OBJECT_STRUCT;
		return MONO_NATIVE_STRUCT;
	}
	case MONO_TYPE_FNPTR: return MONO_NATIVE_FUNC;
	case MONO_TYPE_GENERICINST:
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		t = type->type;
		goto handle_enum;
	case MONO_TYPE_TYPEDBYREF:
	default:
		g_error ("type 0x%02x not handled in marshal", t);
	}
	return MONO_NATIVE_MAX;
}

/**
 * mono_metadata_get_marshal_info:
 */
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

	/* FIXME: metadata-update */
	/* FIXME: Index translation */

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator))
		return NULL;

	return mono_metadata_blob_heap (meta, mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_MARSHAL_NATIVE_TYPE));
}

MonoMethod*
mono_method_from_method_def_or_ref (MonoImage *m, guint32 tok, MonoGenericContext *context, MonoError *error)
{
	MonoMethod *result = NULL;
	guint32 idx = tok >> MONO_METHODDEFORREF_BITS;

	error_init (error);

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
 *  Compute the method overrides belonging to class @type_token in @overrides, and the number of overrides in @num_overrides.
 *
 */
void
mono_class_get_overrides_full (MonoImage *image, guint32 type_token, MonoMethod ***overrides, gint32 *num_overrides, MonoGenericContext *generic_context, MonoError *error)
{
	locator_t loc;
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_METHODIMPL];
	guint32 start, end;
	gint32 i, num;
	guint32 cols [MONO_METHODIMPL_SIZE];
	MonoMethod **result;

	error_init (error);

	*overrides = NULL;
	if (num_overrides)
		*num_overrides = 0;

	if (!tdef->base)
		return;

	loc.t = tdef;
	loc.col_idx = MONO_METHODIMPL_CLASS;
	loc.idx = mono_metadata_token_index (type_token);

	/* FIXME metadata-update */

	if (!mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator))
		return;

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
	guint32 rows = table_info_get_rows (tdef);
	while (end < rows) {
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
		method = mono_method_from_method_def_or_ref (image, cols [MONO_METHODIMPL_DECLARATION], generic_context, error);
		if (!method)
			break;

		result [i * 2] = method;
		method = mono_method_from_method_def_or_ref (image, cols [MONO_METHODIMPL_BODY], generic_context, error);
		if (!method)
			break;

		result [i * 2 + 1] = method;
	}

	if (!is_ok (error)) {
		g_free (result);
		*overrides = NULL;
		if (num_overrides)
			*num_overrides = 0;
	} else {
		*overrides = result;
		if (num_overrides)
			*num_overrides = num;
	}
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

/**
 * mono_guid_to_string_minimal:
 *
 * Converts a 16 byte Microsoft GUID to lower case no '-' representation..
 */
char *
mono_guid_to_string_minimal (const guint8 *guid)
{
	return g_strdup_printf ("%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x%02x",
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

	error_init (error);

	*constraints = NULL;
	found = 0;
	/* FIXME: metadata-update */
	guint32 rows = table_info_get_rows (tdef);
	for (i = 0; i < rows; ++i) {
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
	res = (MonoClass **)mono_image_alloc0 (image, sizeof (MonoClass*) * (found + 1));
	for (i = 0, tmp = cons; i < found; ++i, tmp = tmp->next) {
		res [i] = (MonoClass *)tmp->data;
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
	if (!tdef->base && !image->has_updates)
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
	loc.result = 0;

	gboolean found = tdef->base && mono_binary_search (&loc, tdef->base, table_info_get_rows (tdef), tdef->row_size, table_locator) != NULL;
	if (!found && !image->has_updates)
		return 0;

	if (G_UNLIKELY (image->has_updates)) {
		if (!found && !mono_metadata_update_metadata_linear_search (image, tdef, &loc, table_locator))
			return 0;
	}

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

	guint32 start_row, owner;
	error_init (error);

	if (! (start_row = mono_metadata_get_generic_param_row (image, token, &owner)))
		return TRUE;
	for (int i = 0; i < container->type_argc; i++) {
		if (!get_constraints (image, start_row + i, &mono_generic_container_get_param_info (container, i)->constraints, container, error)) {
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
mono_metadata_load_generic_params (MonoImage *image, guint32 token, MonoGenericContainer *parent_container, gpointer real_owner)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAM];
	guint32 cols [MONO_GENERICPARAM_SIZE];
	guint32 owner = 0, i;
	MonoGenericContainer *container;
	MonoGenericParamFull *params;
	MonoGenericContext *context;
	gboolean is_method = mono_metadata_token_table (token) == MONO_TABLE_METHOD;
	gboolean is_anonymous = real_owner == NULL;

	if (!(i = mono_metadata_get_generic_param_row (image, token, &owner)))
		return NULL;
	mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	params = NULL;
	container = (MonoGenericContainer *)mono_image_alloc0 (image, sizeof (MonoGenericContainer));
	container->is_anonymous = is_anonymous;
	if (is_anonymous) {
		container->owner.image = image;
	} else {
		if (is_method)
			container->owner.method = (MonoMethod*)real_owner;
		else
			container->owner.klass = (MonoClass*)real_owner;
	}
	/* first pass over the gparam table - just count how many params we own */
	guint32 type_argc = 0;
	guint32 i2 = i;
	do {
		type_argc++;
		if (++i2 > mono_metadata_table_num_rows (image, MONO_TABLE_GENERICPARAM))
			break;
		mono_metadata_decode_row (tdef, i2 - 1, cols, MONO_GENERICPARAM_SIZE);
	} while (cols [MONO_GENERICPARAM_OWNER] == owner);
	params = (MonoGenericParamFull *)mono_image_alloc0 (image, sizeof (MonoGenericParamFull) * type_argc);

	/* second pass, fill in the gparam data */
	guint32 n = 0;
	mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	do {
		n++;
		params [n - 1].owner = container;
		params [n - 1].num = GUINT32_TO_UINT16 (cols [MONO_GENERICPARAM_NUMBER]);
		params [n - 1].info.token = i | MONO_TOKEN_GENERIC_PARAM;
		params [n - 1].info.flags = GUINT32_TO_UINT16 (cols [MONO_GENERICPARAM_FLAGS]);
		params [n - 1].info.name = mono_metadata_string_heap (image, cols [MONO_GENERICPARAM_NAME]);
		if (params [n - 1].num != n - 1)
			g_warning ("GenericParam table unsorted or hole in generic param sequence: token %d", i);
		if (++i > mono_metadata_table_num_rows (image, MONO_TABLE_GENERICPARAM))
			break;
		mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	} while (cols [MONO_GENERICPARAM_OWNER] == owner);

	container->type_argc = type_argc;
	container->type_params = params;
	container->parent = parent_container;

	if (is_method)
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
 * \param type the \c MonoType operated on
 * \returns TRUE if \p type represents a type passed by reference,
 * FALSE otherwise.
 */
mono_bool
mono_type_is_byref (MonoType *type)
{
	mono_bool result;
	MONO_ENTER_GC_UNSAFE; // FIXME slow
	result = m_type_is_byref (type);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_type_get_type:
 * \param type the \c MonoType operated on
 * \returns the IL type value for \p type. This is one of the \c MonoTypeEnum
 * enum members like \c MONO_TYPE_I4 or \c MONO_TYPE_STRING.
 */
int
mono_type_get_type (MonoType *type)
{
	return mono_type_get_type_internal (type);
}

/**
 * mono_type_get_signature:
 * \param type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_FNPTR .
 * \returns the \c MonoMethodSignature pointer that describes the signature
 * of the function pointer \p type represents.
 */
MonoMethodSignature*
mono_type_get_signature (MonoType *type)
{
	return mono_type_get_signature_internal (type);
}

/**
 * mono_type_get_class:
 * \param type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_CLASS or a
 * \c MONO_TYPE_VALUETYPE . For more general functionality, use \c mono_class_from_mono_type_internal,
 * instead.
 * \returns the \c MonoClass pointer that describes the class that \p type represents.
 */
MonoClass*
mono_type_get_class (MonoType *type)
{
	/* FIXME: review the runtime users before adding the assert here */
	return mono_type_get_class_internal (type);
}

/**
 * mono_type_get_array_type:
 * \param type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_ARRAY .
 * \returns a \c MonoArrayType struct describing the array type that \p type
 * represents. The info includes details such as rank, array element type
 * and the sizes and bounds of multidimensional arrays.
 */
MonoArrayType*
mono_type_get_array_type (MonoType *type)
{
	return mono_type_get_array_type_internal (type);
}

/**
 * mono_type_get_ptr_type:
 * \pararm type the \c MonoType operated on
 * It is only valid to call this function if \p type is a \c MONO_TYPE_PTR .
 * \returns the \c MonoType pointer that describes the type that \p type
 * represents a pointer to.
 */
MonoType*
mono_type_get_ptr_type (MonoType *type)
{
	g_assert (type->type == MONO_TYPE_PTR);
	return type->data.type;
}

/**
 * mono_type_get_modifiers:
 */
MonoClass*
mono_type_get_modifiers (MonoType *type, gboolean *is_required, gpointer *iter)
{
	/* FIXME: implement */
	return NULL;
}

/**
 * mono_type_is_struct:
 * \param type the \c MonoType operated on
 * \returns TRUE if \p type is a struct, that is a \c ValueType but not an enum
 * or a basic type like \c System.Int32 . FALSE otherwise.
 */
mono_bool
mono_type_is_struct (MonoType *type)
{
	return (!m_type_is_byref (type) && ((type->type == MONO_TYPE_VALUETYPE &&
		!m_class_is_enumtype (type->data.klass)) || (type->type == MONO_TYPE_TYPEDBYREF) ||
		((type->type == MONO_TYPE_GENERICINST) &&
		mono_metadata_generic_class_is_valuetype (type->data.generic_class) &&
		!m_class_is_enumtype (type->data.generic_class->container_class))));
}

/**
 * mono_type_is_void:
 * \param type the \c MonoType operated on
 * \returns TRUE if \p type is \c System.Void . FALSE otherwise.
 */
mono_bool
mono_type_is_void (MonoType *type)
{
	return (type && (type->type == MONO_TYPE_VOID) && !m_type_is_byref (type));
}

/**
 * mono_type_is_pointer:
 * \param type the \c MonoType operated on
 * \returns TRUE if \p type is a managed or unmanaged pointer type. FALSE otherwise.
 */
mono_bool
mono_type_is_pointer (MonoType *type)
{
	return (type && ((m_type_is_byref (type) || (type->type == MONO_TYPE_I) || type->type == MONO_TYPE_STRING)
		|| (type->type == MONO_TYPE_SZARRAY) || (type->type == MONO_TYPE_CLASS) ||
		(type->type == MONO_TYPE_U) || (type->type == MONO_TYPE_OBJECT) ||
		(type->type == MONO_TYPE_ARRAY) || (type->type == MONO_TYPE_PTR) ||
		(type->type == MONO_TYPE_FNPTR)));
}

/**
 * mono_type_is_reference:
 * \param type the \c MonoType operated on
 * \returns TRUE if \p type represents an object reference. FALSE otherwise.
 */
mono_bool
mono_type_is_reference (MonoType *type)
{
	/* NOTE: changing this function to return TRUE more often may have
	 * consequences for generic sharing in the AOT compiler.  In
	 * particular, returning TRUE for generic parameters with a 'class'
	 * constraint may cause crashes.
	 */
	return (type && (((type->type == MONO_TYPE_STRING) ||
		(type->type == MONO_TYPE_SZARRAY) || (type->type == MONO_TYPE_CLASS) ||
		(type->type == MONO_TYPE_OBJECT) || (type->type == MONO_TYPE_ARRAY)) ||
		((type->type == MONO_TYPE_GENERICINST) &&
		!mono_metadata_generic_class_is_valuetype (type->data.generic_class))));
}

mono_bool
mono_type_is_generic_parameter (MonoType *type)
{
	return !m_type_is_byref (type) && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR);
}

/**
 * mono_signature_get_return_type:
 * \param sig the method signature inspected
 * \returns the return type of the method signature \p sig
 */
MonoType*
mono_signature_get_return_type (MonoMethodSignature *sig)
{
	MonoType *result;
	MONO_ENTER_GC_UNSAFE;
	result = sig->ret;
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_signature_get_params:
 * \param sig the method signature inspected
 * \param iter pointer to an iterator
 * Iterates over the parameters for the method signature \p sig.
 * A \c void* pointer must be initialized to NULL to start the iteration
 * and its address is passed to this function repeteadly until it returns
 * NULL.
 * \returns the next parameter type of the method signature \p sig,
 * NULL when finished.
 */
MonoType*
mono_signature_get_params (MonoMethodSignature *sig, gpointer *iter)
{
	MonoType *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_signature_get_params_internal (sig, iter);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoType*
mono_signature_get_params_internal (MonoMethodSignature *sig, gpointer *iter)
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
	type = (MonoType **)*iter;
	type++;
	if (type < &sig->params [sig->param_count]) {
		*iter = type;
		return *type;
	}
	return NULL;
}

/**
 * mono_signature_get_param_count:
 * \param sig the method signature inspected
 * \returns the number of parameters in the method signature \p sig.
 */
guint32
mono_signature_get_param_count (MonoMethodSignature *sig)
{
	return sig->param_count;
}

/**
 * mono_signature_get_call_conv:
 * \param sig the method signature inspected
 * \returns the call convention of the method signature \p sig.
 */
guint32
mono_signature_get_call_conv (MonoMethodSignature *sig)
{
	return sig->call_convention;
}

/**
 * mono_signature_vararg_start:
 * \param sig the method signature inspected
 * \returns the number of the first vararg parameter in the
 * method signature \param sig. \c -1 if this is not a vararg signature.
 */
int
mono_signature_vararg_start (MonoMethodSignature *sig)
{
	return sig->sentinelpos;
}

/**
 * mono_signature_is_instance:
 * \param sig the method signature inspected
 * \returns TRUE if this the method signature \p sig has an implicit
 * first instance argument. FALSE otherwise.
 */
gboolean
mono_signature_is_instance (MonoMethodSignature *sig)
{
	return sig->hasthis;
}

/**
 * mono_signature_param_is_out
 * \param sig the method signature inspected
 * \param param_num the 0-based index of the inspected parameter
 * \returns TRUE if the parameter is an out parameter, FALSE
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
 * \param sig the method signature inspected
 * \returns TRUE if this the method signature \p sig has an explicit
 * instance argument. FALSE otherwise.
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
	/* Same hashing we use for objects */
	return (GPOINTER_TO_UINT (ptr) >> 3) * 2654435761u;
}

/*
 * If @field belongs to an inflated generic class, return the corresponding field of the
 * generic type definition class.
 */
MonoClassField*
mono_metadata_get_corresponding_field_from_generic_type_definition (MonoClassField *field)
{
	MonoClass *gtd;
	ptrdiff_t offset;

	if (!mono_class_is_ginst (m_field_get_parent (field)))
		return field;

	/*
	 * metadata-update: nothing to do. can't add fields to existing generic
	 * classes; for new gtds added in updates, this is correct.
	 */
	gtd = mono_class_get_generic_class (m_field_get_parent (field))->container_class;
	offset = field - m_class_get_fields (m_field_get_parent (field));
	return m_class_get_fields (gtd) + offset;
}

/*
 * If @event belongs to an inflated generic class, return the corresponding event of the
 * generic type definition class.
 */
MonoEvent*
mono_metadata_get_corresponding_event_from_generic_type_definition (MonoEvent *event)
{
	MonoClass *gtd;
	ptrdiff_t offset;

	if (!mono_class_is_ginst (event->parent))
		return event;

	gtd = mono_class_get_generic_class (event->parent)->container_class;
	offset = event - mono_class_get_event_info (event->parent)->events;
	return mono_class_get_event_info (gtd)->events + offset;
}

/*
 * If @property belongs to an inflated generic class, return the corresponding property of the
 * generic type definition class.
 */
MonoProperty*
mono_metadata_get_corresponding_property_from_generic_type_definition (MonoProperty *property)
{
	MonoClassPropertyInfo *info;
	MonoClass *gtd;
	ptrdiff_t offset;

	if (!mono_class_is_ginst (property->parent))
		return property;

	info = mono_class_get_property_info (property->parent);
	gtd = mono_class_get_generic_class (property->parent)->container_class;
	offset = property - info->properties;
	return mono_class_get_property_info (gtd)->properties + offset;
}

MonoWrapperCaches*
mono_method_get_wrapper_cache (MonoMethod *method)
{
	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *)method;
		return &imethod->owner->wrapper_caches;
	} else {
		return &m_class_get_image (method->klass)->wrapper_caches;
	}
}

void
mono_loader_set_strict_assembly_name_check (gboolean enabled)
{
	check_assembly_names_strictly = enabled;
}

gboolean
mono_loader_get_strict_assembly_name_check (void)
{
	return check_assembly_names_strictly;
}


gboolean
mono_type_is_aggregate_mods (const MonoType *t)
{
	if (!t->has_cmods)
		return FALSE;

	MonoTypeWithModifiers *full = (MonoTypeWithModifiers *)t;

	return full->is_aggregate;
}

MonoCustomModContainer *
mono_type_get_cmods (const MonoType *t)
{
	if (!t->has_cmods)
		return NULL;

	MonoTypeWithModifiers *full = (MonoTypeWithModifiers *)t;

	g_assert (!full->is_aggregate);
	return &full->mods.cmods;
}

MonoAggregateModContainer *
mono_type_get_amods (const MonoType *t)
{
	if (!t->has_cmods)
		return NULL;

	MonoTypeWithModifiers *full = (MonoTypeWithModifiers *)t;

	g_assert (full->is_aggregate);
	return full->mods.amods;
}

size_t
mono_sizeof_aggregate_modifiers (uint8_t num_mods)
{
	size_t accum = 0;
	accum += offsetof (MonoAggregateModContainer, modifiers);
	accum += sizeof (MonoSingleCustomMod) * num_mods;
	return accum;
}

size_t
mono_sizeof_type_with_mods (uint8_t num_mods, gboolean is_aggregate)
{
	if (num_mods == 0)
		return sizeof (MonoType);
	size_t accum = 0;
	accum += offsetof (MonoTypeWithModifiers, mods);

	if (!is_aggregate) {
		accum += offsetof (struct _MonoCustomModContainer, modifiers);
		accum += sizeof (MonoCustomMod) * num_mods;
	} else {
		accum += offsetof (MonoAggregateModContainer, modifiers);
		accum += sizeof (MonoAggregateModContainer *);
	}
	return accum;
}

size_t
mono_sizeof_type (const MonoType *ty)
{
	if (ty->has_cmods) {
		if (!mono_type_is_aggregate_mods (ty)) {
			MonoCustomModContainer *cmods = mono_type_get_cmods (ty);
			return mono_sizeof_type_with_mods (cmods->count, FALSE);
		} else {
			MonoAggregateModContainer *amods = mono_type_get_amods (ty);
			return mono_sizeof_type_with_mods (amods->count, TRUE);
		}
	} else
		return sizeof (MonoType);
}

void
mono_type_set_amods (MonoType *t, MonoAggregateModContainer *amods)
{
	g_assert (t->has_cmods);
	MonoTypeWithModifiers *t_full = (MonoTypeWithModifiers*)t;
	g_assert (t_full->is_aggregate);
	g_assert (t_full->mods.amods == NULL);
	t_full->mods.amods = amods;
}

#ifndef DISABLE_COM
static void
mono_signature_append_class_name (GString *res, MonoClass *klass)
{
	if (!klass) {
		g_string_append (res, "<UNKNOWN>");
		return;
	}
	if (m_class_get_nested_in (klass)) {
		mono_signature_append_class_name (res, m_class_get_nested_in (klass));
		g_string_append_c (res, '+');
	}
	else if (*m_class_get_name_space (klass)) {
		g_string_append (res, m_class_get_name_space (klass));
		g_string_append_c (res, '.');
	}
	g_string_append (res, m_class_get_name (klass));
}

static void
mono_guid_signature_append_method (GString *res, MonoMethodSignature *sig);

static void
mono_guid_signature_append_type (GString *res, MonoType *type)
{
	int i;
	switch (type->type) {
	case MONO_TYPE_VOID:
		g_string_append (res, "void"); break;
	case MONO_TYPE_BOOLEAN:
		g_string_append (res, "bool"); break;
	case MONO_TYPE_CHAR:
		g_string_append (res, "wchar"); break;
	case MONO_TYPE_I1:
		g_string_append (res, "int8"); break;
	case MONO_TYPE_U1:
		g_string_append (res, "unsigned int8"); break;
	case MONO_TYPE_I2:
		g_string_append (res, "int16"); break;
	case MONO_TYPE_U2:
		g_string_append (res, "unsigned int16"); break;
	case MONO_TYPE_I4:
		g_string_append (res, "int32"); break;
	case MONO_TYPE_U4:
		g_string_append (res, "unsigned int32"); break;
	case MONO_TYPE_I8:
		g_string_append (res, "int64"); break;
	case MONO_TYPE_U8:
		g_string_append (res, "unsigned int64"); break;
	case MONO_TYPE_R4:
		g_string_append (res, "float32"); break;
	case MONO_TYPE_R8:
		g_string_append (res, "float64"); break;
	case MONO_TYPE_U:
		g_string_append (res, "unsigned int"); break;
	case MONO_TYPE_I:
		g_string_append (res, "int"); break;
	case MONO_TYPE_OBJECT:
		g_string_append (res, "class System.Object"); break;
	case MONO_TYPE_STRING:
		g_string_append (res, "class System.String"); break;
	case MONO_TYPE_TYPEDBYREF:
		g_string_append (res, "refany");
		break;
	case MONO_TYPE_VALUETYPE:
		g_string_append (res, "value class ");
		mono_signature_append_class_name (res, type->data.klass);
		break;
	case MONO_TYPE_CLASS:
		g_string_append (res, "class ");
		mono_signature_append_class_name (res, type->data.klass);
		break;
	case MONO_TYPE_SZARRAY:
		mono_guid_signature_append_type (res, m_class_get_byval_arg (type->data.klass));
		g_string_append (res, "[]");
		break;
	case MONO_TYPE_ARRAY:
		mono_guid_signature_append_type (res, m_class_get_byval_arg (type->data.array->eklass));
		g_string_append_c (res, '[');
		if (type->data.array->rank == 0) g_string_append (res, "??");
		for (i = 0; i < type->data.array->rank; ++i)
		{
			if (i > 0) g_string_append_c (res, ',');
			if (type->data.array->sizes[i] == 0 || type->data.array->lobounds[i] == 0) continue;
                        g_string_append_printf (res, "%d", type->data.array->lobounds[i]);
                        g_string_append (res, "...");
                        g_string_append_printf (res, "%d", type->data.array->lobounds[i] + type->data.array->sizes[i] + 1);
		}
		g_string_append_c (res, ']');
		break;
	case MONO_TYPE_MVAR:
	case MONO_TYPE_VAR:
		if (type->data.generic_param)
			g_string_append_printf (res, "%s%d", type->type == MONO_TYPE_VAR ? "!" : "!!", mono_generic_param_num (type->data.generic_param));
		else
			g_string_append (res, "<UNKNOWN>");
		break;
	case MONO_TYPE_GENERICINST: {
		MonoGenericContext *context;
		mono_guid_signature_append_type (res, m_class_get_byval_arg (type->data.generic_class->container_class));
		g_string_append (res, "<");
		context = &type->data.generic_class->context;
		if (context->class_inst) {
			for (i = 0; i < context->class_inst->type_argc; ++i) {
				if (i > 0)
					g_string_append (res, ",");
				mono_guid_signature_append_type (res, context->class_inst->type_argv [i]);
			}
		}
		else if (context->method_inst) {
			for (i = 0; i < context->method_inst->type_argc; ++i) {
				if (i > 0)
					g_string_append (res, ",");
				mono_guid_signature_append_type (res, context->method_inst->type_argv [i]);
			}
		}
		g_string_append (res, ">");
		break;
	}
	case MONO_TYPE_FNPTR:
		g_string_append (res, "fnptr ");
		mono_guid_signature_append_method (res, type->data.method);
		break;
	case MONO_TYPE_PTR:
		mono_guid_signature_append_type (res, type->data.type);
		g_string_append_c (res, '*');
		break;
	default:
		break;
	}
	if (m_type_is_byref (type)) g_string_append_c (res, '&');
}

static void
mono_guid_signature_append_method (GString *res, MonoMethodSignature *sig)
{
	int i, j;

	if (mono_signature_is_instance (sig)) g_string_append (res, "instance ");
	if (sig->generic_param_count) g_string_append (res, "generic ");

	switch (mono_signature_get_call_conv (sig))
	{
	case MONO_CALL_DEFAULT: break;
	case MONO_CALL_C: g_string_append (res, "unmanaged cdecl "); break;
	case MONO_CALL_STDCALL: g_string_append (res, "unmanaged stdcall "); break;
	case MONO_CALL_THISCALL: g_string_append (res, "unmanaged thiscall "); break;
	case MONO_CALL_FASTCALL: g_string_append (res, "unmanaged fastcall "); break;
	case MONO_CALL_VARARG: g_string_append (res, "vararg "); break;
	default: break;
	}

	mono_guid_signature_append_type (res, mono_signature_get_return_type_internal(sig));

	g_string_append_c (res, '(');
	for (i = 0, j = 0; i < sig->param_count && j < sig->param_count; ++i, ++j) {
		if (i > 0) g_string_append_c (res, ',');
		if (sig->params [j]->attrs & PARAM_ATTRIBUTE_IN)
		{
			/*.NET runtime "incorrectly" shifts the parameter signatures too...*/
			g_string_append (res, "required_modifier System.Runtime.InteropServices.InAttribute");
			if (++i == sig->param_count) break;
			g_string_append_c (res, ',');
		}
		mono_guid_signature_append_type (res, sig->params [j]);
	}
	g_string_append_c (res, ')');
}

static void
mono_generate_v3_guid_for_interface (MonoClass* klass, guint8* guid)
{
	/* COM+ Runtime GUID {69f9cbc9-da05-11d1-9408-0000f8083460} */
	static const guchar guid_name_space[] = {0x69,0xf9,0xcb,0xc9,0xda,0x05,0x11,0xd1,0x94,0x08,0x00,0x00,0xf8,0x08,0x34,0x60};

	MonoMD5Context ctx;
	MonoMethod *method;
	gpointer iter = NULL;
	guchar byte;
	glong items_read, items_written;
	int i;

	mono_md5_init (&ctx);
	mono_md5_update (&ctx, guid_name_space, sizeof(guid_name_space));

	GString *name = g_string_new ("");
	mono_signature_append_class_name (name, klass);
	gunichar2 *unicode_name = g_utf8_to_utf16 (name->str, name->len, &items_read, &items_written, NULL);
	mono_md5_update (&ctx, (guchar *)unicode_name, items_written * sizeof(gunichar2));

	g_free (unicode_name);
	g_string_free (name, TRUE);

	while ((method = mono_class_get_methods(klass, &iter)) != NULL)
	{
		ERROR_DECL (error);
		if (!mono_cominterop_method_com_visible(method)) continue;

		MonoMethodSignature *sig = mono_method_signature_checked (method, error);
		mono_error_assert_ok (error); /*FIXME proper error handling*/

		GString *res = g_string_new ("");
		mono_guid_signature_append_method (res, sig);
		mono_md5_update (&ctx, (guchar *)res->str, res->len);
		g_string_free (res, TRUE);

		for (i = 0; i < sig->param_count; ++i) {
			byte = sig->params [i]->attrs;
			mono_md5_update (&ctx, &byte, 1);
		}
	}

	byte = 0;
	if (mono_md5_ctx_byte_length (&ctx) & 1)
		mono_md5_update (&ctx, &byte, 1);
	mono_md5_final (&ctx, (guchar *)guid);

        guid[6] &= 0x0f;
        guid[6] |= 0x30; /* v3 (md5) */

	guid[8] &= 0x3F;
	guid[8] |= 0x80;

	*(guint32 *)(guid + 0) = GUINT32_FROM_BE(*(guint32 *)(guid + 0));
	*(guint16 *)(guid + 4) = GUINT16_FROM_BE(*(guint16 *)(guid + 4));
	*(guint16 *)(guid + 6) = GUINT16_FROM_BE(*(guint16 *)(guid + 6));
}
#endif

static gint
mono_unichar_xdigit_value (gunichar c)
{
	if (c >= 0x30 && c <= 0x39) /*0-9*/
		return (c - 0x30);
	if (c >= 0x41 && c <= 0x46) /*A-F*/
		return (c - 0x37);
	if (c >= 0x61 && c <= 0x66) /*a-f*/
		return (c - 0x57);
	return -1;
}

/**
 * mono_string_to_guid:
 *
 * Converts the standard string representation of a GUID
 * to a 16 byte Microsoft GUID.
 */
static void
mono_string_to_guid (MonoString* string, guint8 *guid) {
	gunichar2 * chars = mono_string_chars_internal (string);
	int i = 0;
	static const guint8 indexes[16] = {7, 5, 3, 1, 12, 10, 17, 15, 20, 22, 25, 27, 29, 31, 33, 35};

	for (i = 0; i < sizeof(indexes); i++)
		guid [i] = GINT_TO_UINT8 (mono_unichar_xdigit_value (chars [indexes [i]]) + (mono_unichar_xdigit_value (chars [indexes [i] - 1]) << 4));
}

static GENERATE_GET_CLASS_WITH_CACHE (guid_attribute, "System.Runtime.InteropServices", "GuidAttribute")

void
mono_metadata_get_class_guid (MonoClass* klass, guint8* guid, MonoError *error)
{
	MonoReflectionGuidAttribute *attr = NULL;
	MonoCustomAttrInfo *cinfo = mono_custom_attrs_from_class_checked (klass, error);
	if (!is_ok (error))
		return;
	if (cinfo) {
		attr = (MonoReflectionGuidAttribute*)mono_custom_attrs_get_attr_checked (cinfo, mono_class_get_guid_attribute_class (), error);
		if (!is_ok (error))
			return;
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	memset(guid, 0, 16);
	if (attr)
		mono_string_to_guid (attr->guid, guid);
#ifndef DISABLE_COM
	else if (mono_class_is_interface (klass))
		mono_generate_v3_guid_for_interface (klass, guid);
	else
		g_warning ("Generated GUIDs only implemented for interfaces!");
#endif
}
