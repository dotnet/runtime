/*
 * metadata.c: Routines for accessing the metadata
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <stdio.h> 
#include <stdlib.h>
#include <glib.h>
#include "metadata.h"
#include "tabledefs.h"
#include "endian.h"
#include "cil-coff.h"
#include "tokentype.h"
#include "private.h"
#include "class.h"

static void do_mono_metadata_parse_type (MonoType *type, MonoMetadata *m, const char *ptr, const char **rptr);

/*
 * Encoding of the "description" argument:
 *
 * identifier [CODE ARG]
 *
 * If CODE is ':', then a lookup on table ARG is performed
 * If CODE is '=', then a lookup in the aliased-table ARG is performed
 * If CODE is '#', then this encodes a flag, ARG is the flag name. 
 *
 * Aliased table for example is `CustomAttributeType' which depending on the
 * information might refer to different tables.
 */

static MonoMetaTable AssemblySchema [] = {
	{ MONO_MT_UINT32,     "HashId" },
	{ MONO_MT_UINT16,     "Major" },  
	{ MONO_MT_UINT16,     "Minor" },
	{ MONO_MT_UINT16,     "BuildNumber" },
	{ MONO_MT_UINT16,     "RevisionNumber" },
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_BLOB_IDX,   "PublicKey" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Culture" },
	{ MONO_MT_END, NULL }
};
	
static MonoMetaTable AssemblyOSSchema [] = {
	{ MONO_MT_UINT32,     "OSPlatformID" },
	{ MONO_MT_UINT32,     "OSMajor" },
	{ MONO_MT_UINT32,     "OSMinor" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyProcessorSchema [] = {
	{ MONO_MT_UINT32,     "Processor" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyRefSchema [] = {
	{ MONO_MT_UINT16,     "Major" },
	{ MONO_MT_UINT16,     "Minor" },
	{ MONO_MT_UINT16,     "Build" },
	{ MONO_MT_UINT16,     "Revision" },
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_BLOB_IDX,   "PublicKeyOrToken" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Culture" },
	{ MONO_MT_BLOB_IDX,   "HashValue" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyRefOSSchema [] = {
	{ MONO_MT_UINT32,     "OSPlatformID" },
	{ MONO_MT_UINT32,     "OSMajorVersion" },
	{ MONO_MT_UINT32,     "OSMinorVersion" },
	{ MONO_MT_TABLE_IDX,  "AssemblyRef:AssemblyRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyRefProcessorSchema [] = {
	{ MONO_MT_UINT32,     "Processor" },
	{ MONO_MT_TABLE_IDX,  "AssemblyRef:AssemblyRef" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable ClassLayoutSchema [] = {
	{ MONO_MT_UINT16,     "PackingSize" },
	{ MONO_MT_UINT32,     "ClassSize" },
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ConstantSchema [] = {
	{ MONO_MT_UINT8,      "Type" },
	{ MONO_MT_UINT8,      "PaddingZero" },
	{ MONO_MT_CONST_IDX,  "Parent" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable CustomAttributeSchema [] = {
	{ MONO_MT_HASCAT_IDX, "Parent" },
	{ MONO_MT_CAT_IDX,    "Type" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable DeclSecuritySchema [] = {
	{ MONO_MT_UINT16,     "Action" },
	{ MONO_MT_HASDEC_IDX, "Parent" },
	{ MONO_MT_BLOB_IDX,   "PermissionSet" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable EventMapSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "EventList:Event" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable EventSchema [] = {
	{ MONO_MT_UINT16,     "EventFlags#EventAttribute" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_TABLE_IDX,  "EventType" }, /* TypeDef or TypeRef */
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable ExportedTypeSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_TABLE_IDX,  "TypeDefId" },
	{ MONO_MT_STRING_IDX, "TypeName" },
	{ MONO_MT_STRING_IDX, "TypeNameSpace" },
	{ MONO_MT_IMPL_IDX,   "Implementation" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable FieldSchema [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }	
};
static MonoMetaTable FieldLayoutSchema [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_TABLE_IDX,  "Field:Field" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable FieldMarshalSchema [] = {
	{ MONO_MT_HFM_IDX,    "Parent" },
	{ MONO_MT_BLOB_IDX,   "NativeType" },
	{ MONO_MT_END, NULL }	
};
static MonoMetaTable FieldRVASchema [] = {
	{ MONO_MT_UINT32,     "RVA" },
	{ MONO_MT_TABLE_IDX,  "Field:Field" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable FileSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Value" }, 
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ImplMapSchema [] = {
	{ MONO_MT_UINT16,     "MappingFlag" },
	{ MONO_MT_MF_IDX,     "MemberForwarded" },
	{ MONO_MT_STRING_IDX, "ImportName" },
	{ MONO_MT_TABLE_IDX,  "ImportScope:ModuleRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable InterfaceImplSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" }, 
	{ MONO_MT_TDOR_IDX,  "Interface=TypeDefOrRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ManifestResourceSchema [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_IMPL_IDX,   "Implementation" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MemberRefSchema [] = {
	{ MONO_MT_MRP_IDX,    "Class" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodSchema [] = {
	{ MONO_MT_UINT32,     "RVA" },
	{ MONO_MT_UINT16,     "ImplFlags#MethodImplAttributes" },
	{ MONO_MT_UINT16,     "Flags#MethodAttribute" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_TABLE_IDX,  "ParamList:Param" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodImplSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" },
	{ MONO_MT_MDOR_IDX,   "MethodBody" },
	{ MONO_MT_MDOR_IDX,   "MethodDeclaration" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodSemanticsSchema [] = {
	{ MONO_MT_UINT16,     "MethodSemantic" },
	{ MONO_MT_TABLE_IDX,  "Method:Method" },
	{ MONO_MT_HS_IDX,     "Association" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ModuleSchema [] = {
	{ MONO_MT_UINT16,     "Generation" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_GUID_IDX,   "MVID" },
	{ MONO_MT_GUID_IDX,   "EncID" },
	{ MONO_MT_GUID_IDX,   "EncBaseID" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ModuleRefSchema [] = {
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable NestedClassSchema [] = {
	{ MONO_MT_TABLE_IDX,  "NestedClass:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "EnclosingClass:TypeDef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ParamSchema [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_UINT16,     "Sequence" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable PropertySchema [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Type" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable PropertyMapSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "PropertyList:Property" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable StandaloneSigSchema [] = {
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeDefSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Namespace" },
	{ MONO_MT_TDOR_IDX,   "Extends" },
	{ MONO_MT_TABLE_IDX,  "FieldList:Field" },
	{ MONO_MT_TABLE_IDX,  "MethodList:Method" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeRefSchema [] = {
	{ MONO_MT_RS_IDX,     "ResolutionScope=ResolutionScope" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Namespace" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeSpecSchema [] = {
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

static struct {
	MonoMetaTable *table;
	const char    *name;
} tables [] = {
	/*  0 */ { ModuleSchema,               "Module" },
	/*  1 */ { TypeRefSchema,              "TypeRef" },
	/*  2 */ { TypeDefSchema,              "TypeDef" },
	/*  3 */ { NULL,                       NULL },
	/*  4 */ { FieldSchema,                "Field" },
	/*  5 */ { NULL,                       NULL },
	/*  6 */ { MethodSchema,               "Method" },
	/*  7 */ { NULL,                       NULL },
	/*  8 */ { ParamSchema,                "Param" },
	/*  9 */ { InterfaceImplSchema,        "InterfaceImpl" },
	/*  A */ { MemberRefSchema,            "MemberRef" },
	/*  B */ { ConstantSchema,             "Constant" },
	/*  C */ { CustomAttributeSchema,      "CustomAttribute" },
	/*  D */ { FieldMarshalSchema,         "FieldMarshal" },
	/*  E */ { DeclSecuritySchema,         "DeclSecurity" },
	/*  F */ { ClassLayoutSchema,          "ClassLayout" },
	/* 10 */ { FieldLayoutSchema,          "FieldLayout" },
	/* 11 */ { StandaloneSigSchema,        "StandaloneSig" },
	/* 12 */ { EventMapSchema,             "EventMap" },
	/* 13 */ { NULL,                       NULL },
	/* 14 */ { EventSchema,                "Event" },
	/* 15 */ { PropertyMapSchema,          "PropertyMap" },
	/* 16 */ { NULL,                       NULL },
	/* 17 */ { PropertySchema,             "PropertyTable" },
	/* 18 */ { MethodSemanticsSchema,      "MethodSemantics" },
	/* 19 */ { MethodImplSchema,           "MethodImpl" },
	/* 1A */ { ModuleRefSchema,            "ModuleRef" },
	/* 1B */ { TypeSpecSchema,             "TypeSpec" },
	/* 1C */ { ImplMapSchema,              "ImplMap" },
	/* 1D */ { FieldRVASchema,             "FieldRVA" },
	/* 1E */ { NULL,                       NULL },
	/* 1F */ { NULL,                       NULL },
	/* 20 */ { AssemblySchema,             "Assembly" },
	/* 21 */ { AssemblyProcessorSchema,    "AssemblyProcessor" },
	/* 22 */ { AssemblyOSSchema,           "AssemblyOS" },
	/* 23 */ { AssemblyRefSchema,          "AssemblyRef" },
	/* 24 */ { AssemblyRefProcessorSchema, "AssemblyRefProcessor" },
	/* 25 */ { AssemblyRefOSSchema,        "AssemblyRefOS" },
	/* 26 */ { FileSchema,                 "File" },
	/* 27 */ { ExportedTypeSchema,         "ExportedType" },
	/* 28 */ { ManifestResourceSchema,     "ManifestResource" },
	/* 29 */ { NestedClassSchema,          "NestedClass" },
	/* 2A */ { NULL,                       NULL },
	/* 2B */ { NULL,                       NULL },
};

/**
 * mono_meta_table_name:
 * @table: table index
 *
 * Returns the name for the @table index
 */
const char *
mono_meta_table_name (int table)
{
	if ((table < 0) || (table > 0x29))
		return "";
	
	return tables [table].name;
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
static int
compute_size (MonoMetadata *meta, MonoMetaTable *table, int tableindex, guint32 *result_bitfield)
{
	guint32 bitfield = 0;
	int size = 0, field_size;
	int i, n, code;
	int shift = 0;

	for (i = 0; (code = table [i].code) != MONO_MT_END; i++){
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
				field_size = MAX (idx_size (MONO_TABLE_TYPEDEF), idx_size(MONO_TABLE_TYPEREF));
				field_size = MAX (field_size, idx_size(MONO_TABLE_TYPESPEC));
				break;
			case MONO_TABLE_EXPORTEDTYPE:
				g_assert (i == 1);
				field_size = idx_size (MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_FIELDLAYOUT:
				g_assert (i == 1);
				field_size = idx_size (MONO_TABLE_FIELD); break;
			case MONO_TABLE_FIELDRVA:
				g_assert (i == 1);
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
			case MONO_TABLE_NESTEDCLASS:
				g_assert (i == 0 || i == 1);
				field_size = idx_size (MONO_TABLE_TYPEDEF); break;
			case MONO_TABLE_PROPERTYMAP:
				g_assert (i == 0 || i == 1);
				field_size = i ? idx_size (MONO_TABLE_PROPERTY):
					idx_size(MONO_TABLE_TYPEDEF); 
				break;
			case MONO_TABLE_TYPEDEF:
				g_assert (i == 4 || i == 5);
				field_size = i == 4 ? idx_size (MONO_TABLE_FIELD):
					idx_size(MONO_TABLE_METHOD); 
				break;
			default:
				g_assert_not_reached ();
			}
			if (field_size != idx_size (tableindex))
				g_warning ("size changed (%d to %d)", idx_size (tableindex), field_size);
			
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
			/* Permission seems to be a blob heap pointer */
			n = MAX (n, meta->tables [MONO_TABLE_PROPERTY].rows);
			n = MAX (n, meta->tables [MONO_TABLE_EVENT].rows);
			/* Signature seems to be a blob heap pointer */
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
			
		case MONO_MT_MDOR_IDX:

			/*
			 * MethodDefOrRef: MethodDef, MemberRef
			 */
		case MONO_MT_HS_IDX:
			n = MAX (meta->tables [MONO_TABLE_METHOD].rows,
				 meta->tables [MONO_TABLE_MEMBERREF].rows);

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
mono_metadata_compute_table_bases (MonoMetadata *meta)
{
	int i;
	char *base = meta->tables_base;
	
	for (i = 0; i < 64; i++){
		if (meta->tables [i].rows == 0)
			continue;

		meta->tables [i].row_size = compute_size (
			meta, tables [i].table, i,
			&meta->tables [i].size_bitfield);
		meta->tables [i].base = base;
		base += meta->tables [i].rows * meta->tables [i].row_size;
	}
}

/**
 * mono_metadata_locate:
 * @meta: metadata context
 * @table: table code.
 * @idx: index of element to retrieve from @table.
 *
 * Returns a pointer to the @idx element in the metadata table
 * whose code is @table.
 */
char *
mono_metadata_locate (MonoMetadata *meta, int table, int idx)
{
	/* idx == 0 refers always to NULL */
	g_return_val_if_fail (idx > 0 && idx <= meta->tables [table].rows, "");
	   
	return meta->tables [table].base + (meta->tables [table].row_size * (idx - 1));
}

char *
mono_metadata_locate_token (MonoMetadata *meta, guint32 token)
{
	return mono_metadata_locate (meta, token >> 24, token & 0xffffff);
}

/**
 * mono_metadata_get_table:
 * @table: table to retrieve
 *
 * Returns the MonoMetaTable structure for table @table
 */
MonoMetaTable *
mono_metadata_get_table (MonoMetaTableEnum table)
{
	int x = (int) table;

	g_return_val_if_fail ((x > 0) && (x <= MONO_TABLE_LAST), NULL);

	return tables [table].table;
}

/**
 * mono_metadata_string_heap:
 * @meta: metadata context
 * @index: index into the string heap.
 *
 * Returns: an in-memory pointer to the @index in the string heap.
 */
const char *
mono_metadata_string_heap (MonoMetadata *meta, guint32 index)
{
	g_return_val_if_fail (index < meta->heap_strings.size, "");
	return meta->raw_metadata + meta->heap_strings.offset + index;
}

const char *
mono_metadata_user_string (MonoMetadata *meta, guint32 index)
{
	g_return_val_if_fail (index < meta->heap_us.size, "");
	return meta->raw_metadata + meta->heap_us.offset + index;
}

/**
 * mono_metadata_blob_heap:
 * @meta: metadata context
 * @index: index into the blob.
 *
 * Returns: an in-memory pointer to the @index in the Blob heap.
 */
const char *
mono_metadata_blob_heap (MonoMetadata *meta, guint32 index)
{
	g_return_val_if_fail (index < meta->heap_blob.size, "");
	return meta->raw_metadata + meta->heap_blob.offset + index;
}

static const char *
dword_align (const char *ptr)
{
	return (const char *) (((guint32) (ptr + 3)) & ~3);
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
mono_metadata_decode_row (MonoTableInfo *t, int idx, guint32 *res, int res_size)
{
	guint32 bitfield = t->size_bitfield;
	int i, count = mono_metadata_table_count (bitfield);
	char *data = t->base + idx * t->row_size;
	
	g_assert (res_size == count);
	
	for (i = 0; i < count; i++){
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
mono_metadata_decode_row_col (MonoTableInfo *t, int idx, guint col)
{
	guint32 bitfield = t->size_bitfield;
	int i;
	register char *data = t->base + idx * t->row_size;
	register int n;
	
	g_assert (col < mono_metadata_table_count (bitfield));

	n = mono_metadata_table_size (bitfield, 0);
	for (i = 0; i < col; ++i) {
		data += n;
		n = mono_metadata_table_size (bitfield, i + 1);
	}
	switch (n){
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
	const unsigned char *ptr = xptr;
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
		*rptr = ptr;
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
	const unsigned char *ptr = (unsigned char *) _ptr;
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
		*rptr = ptr;
	
	return len;
}

guint32
mono_metadata_parse_typedef_or_ref (MonoMetadata *m, const char *ptr, const char **rptr)
{
	guint32 token;
	guint table;
	token = mono_metadata_decode_value (ptr, &ptr);

	switch (token & 0x03) {
	case 0: table = MONO_TABLE_TYPEDEF; break;
	case 1: table = MONO_TABLE_TYPEREF; break;
	case 2: table = MONO_TABLE_TYPESPEC; break;
	default: g_error ("Unhandled encoding for typedef-or-ref coded index");
	}
	if (rptr)
		*rptr = ptr;
	return (token >> 2) | table << 24;
}

int
mono_metadata_parse_custom_mod (MonoMetadata *m, MonoCustomMod *dest, const char *ptr, const char **rptr)
{
	MonoCustomMod local;
	if ((*ptr == MONO_TYPE_CMOD_OPT) ||
	    (*ptr == MONO_TYPE_CMOD_REQD)) {
		if (!dest)
			dest = &local;
		dest->required = *ptr == MONO_TYPE_CMOD_REQD ? 1 : 0;
		dest->token = mono_metadata_parse_typedef_or_ref (m, ptr + 1, &ptr);
		return TRUE;
	}
	return FALSE;
}

MonoArray *
mono_metadata_parse_array (MonoMetadata *m, const char *ptr, const char **rptr)
{
	int i;
	MonoArray *array = g_new0 (MonoArray, 1);
	
	array->type = mono_metadata_parse_type (m, MONO_PARSE_TYPE, 0, ptr, &ptr);
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
		array->lobounds [i] = mono_metadata_decode_value (ptr, &ptr);

	if (rptr)
		*rptr = ptr;
	return array;
}

void
mono_metadata_free_array (MonoArray *array)
{
	mono_metadata_free_type (array->type);
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
static MonoType
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
	{{NULL}, 0,     MONO_TYPE_TYPEDBYREF,  0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I,       0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_I,       0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_U,       0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_U,       0,     1,     0},
	{{NULL}, 0,     MONO_TYPE_OBJECT,  0,     0,     0},
	{{NULL}, 0,     MONO_TYPE_OBJECT,  0,     1,     0}
};

#define NBUILTIN_TYPES() (sizeof (builtin_types) / sizeof (builtin_types [0]))

static GHashTable *type_cache = NULL;

/*
 * MonoTypes with modifies are never cached, so we never check or use that field.
 */
static int
mono_type_hash (MonoType *type)
{
	return type->type | (type->byref << 8) | (type->attrs << 9);
}

static gboolean
mono_type_equal (MonoType *a, MonoType *b)
{
	if (a->type != b->type || a->byref != b->byref || a->attrs != b->attrs || a->pinned != b->pinned)
		return 0;
	/* need other checks */
	return 1;
}

MonoType*
mono_metadata_parse_type (MonoMetadata *m, MonoParseTypeMode mode, short opt_attrs, const char *ptr, const char **rptr)
{
	MonoType *type, *cached;

	if (!type_cache) {
		int i;
		type_cache = g_hash_table_new (mono_type_hash, mono_type_equal);
		for (i=0; i < NBUILTIN_TYPES (); ++i)
			g_hash_table_insert (type_cache, &builtin_types [i], &builtin_types [i]);
	}

	switch (mode) {
	case MONO_PARSE_MOD_TYPE:
	case MONO_PARSE_PARAM:
	case MONO_PARSE_RET:
	case MONO_PARSE_FIELD: {
		/* count the modifiers */
		const char *tmp_ptr = ptr;
		int count = 0;
		while (mono_metadata_parse_custom_mod (m, NULL, tmp_ptr, &tmp_ptr))
			count++;
		if (count) {
			type = g_malloc0 (sizeof (MonoType) + (count - MONO_ZERO_LEN_ARRAY) * sizeof (MonoCustomMod));
			type->num_mods = count;
			if (count > 64)
				g_warning ("got more than 64 modifiers in type");
			/* save them this time */
			count = 0;
			while (mono_metadata_parse_custom_mod (m, &(type->modifiers [count]), ptr, &ptr))
				count++;
			break;
		} /* fall through */
	}
	case MONO_PARSE_LOCAL:
	case MONO_PARSE_TYPE:
		/*
		 * Later we can avoid doing this allocation.
		 */
		type = g_new0 (MonoType, 1);
		break;
	default:
		g_assert_not_reached ();
	}
	
	type->attrs = opt_attrs;
	if (mode == MONO_PARSE_LOCAL) {
		/*
		 * check for pinned flag
		 */
		if (*ptr == MONO_TYPE_PINNED) {
			type->pinned = 1;
			++ptr;
		}
	}

	switch (*ptr) {
	case MONO_TYPE_BYREF: 
		if (mode == MONO_PARSE_FIELD)
			g_warning ("A field type cannot be byref");
		type->byref = 1; 
		ptr++;
		/* follow through */
	default:
		/*if (*ptr == MONO_TYPE_VOID && mode != MONO_PARSE_RET)
			g_error ("void not allowed in param");*/
		do_mono_metadata_parse_type (type, m, ptr, &ptr);
		break;
	}
	if (rptr)
		*rptr = ptr;
	if (mode != MONO_PARSE_PARAM && !type->num_mods && (cached = g_hash_table_lookup (type_cache, type))) {
		mono_metadata_free_type (type);
		return cached;
	} else {
		return type;
	}
}

MonoMethodSignature *
mono_metadata_parse_method_signature (MonoMetadata *m, int def, const char *ptr, const char **rptr)
{
	MonoMethodSignature *method = g_new0(MonoMethodSignature, 1);
	int i, align, offset = 0;

	if (*ptr & 0x20)
		method->hasthis = 1;
	if (*ptr & 0x40)
		method->explicit_this = 1;
	method->call_convention = *ptr & 0x0F;
	ptr++;
	method->param_count = mono_metadata_decode_value (ptr, &ptr);
	method->ret = mono_metadata_parse_type (m, MONO_PARSE_RET, 0, ptr, &ptr);

	if (method->hasthis)
		offset += sizeof(gpointer);
	if (method->param_count) {
		int size;
		
		method->params = g_new0 (MonoType*, method->param_count);
		method->sentinelpos = -1;
		
		for (i = 0; i < method->param_count; ++i) {
			if (*ptr == MONO_TYPE_SENTINEL) {
				if (method->call_convention != MONO_CALL_VARARG || def)
						g_error ("found sentinel for methoddef or no vararg method");
				method->sentinelpos = i;
				ptr++;
			}
			method->params [i] = mono_metadata_parse_type (m, MONO_PARSE_PARAM, 0, ptr, &ptr);
			size = mono_type_size (method->params [i], &align);
			offset += (offset % align);
			offset += size;
		}
	}
	method->params_size = offset;

	if (rptr)
		*rptr = ptr;
	return method;
}

void
mono_metadata_free_method_signature (MonoMethodSignature *method)
{
	int i;
	mono_metadata_free_type (method->ret);
	for (i = 0; i < method->param_count; ++i)
		mono_metadata_free_type (method->params [i]);

	g_free (method->params);
	g_free (method);
}

/* 
 * do_mono_metadata_parse_type:
 * @type: MonoType to be filled in with the return value
 * @
 * Internal routine used to "fill" the contents of @type from an 
 * allocated pointer.  This is done this way to avoid doing too
 * many mini-allocations (particularly for the MonoFieldType which
 * most of the time is just a MonoType, but sometimes might be augmented).
 *
 * This routine is used by mono_metadata_parse_type and
 * mono_metadata_parse_field_type
 *
 * This extracts a Type as specified in Partition II (22.2.12) 
 */
static void
do_mono_metadata_parse_type (MonoType *type, MonoMetadata *m, const char *ptr, const char **rptr)
{
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
		break;
	}
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
		type->data.type = mono_metadata_parse_type (m, MONO_PARSE_MOD_TYPE, 0, ptr, &ptr);
		break;
	case MONO_TYPE_FNPTR:
		type->data.method = mono_metadata_parse_method_signature (m, 0, ptr, &ptr);
		break;
	case MONO_TYPE_ARRAY:
		type->data.array = mono_metadata_parse_array (m, ptr, &ptr);
		break;
	default:
		g_error ("type 0x%02x not handled in mono_metadata_parse_type", type->type);
	}
	
	if (rptr)
		*rptr = ptr;
}

#if 0
/**
 * mono_metadata_parse_type:
 * @m: metadata context to scan
 * @ptr: pointer to encoded Type stream.
 * @rptr: the new position in the stream after parsing the type
 *
 * Returns: A MonoType structure that has the parsed information
 * from the type stored at @ptr in the metadata table @m.
 */
MonoType *
mono_metadata_parse_type (MonoMetadata *m, const char *ptr, const char **rptr)
{
	/* should probably be allocated in a memchunk */
	MonoType *type = g_new0(MonoType, 1);

	do_mono_metadata_parse_type (type, m, ptr, rptr);

	return type;
}
#endif

void
mono_metadata_free_type (MonoType *type)
{
	if (type >= builtin_types && type < builtin_types + NBUILTIN_TYPES ())
		return;
	switch (type->type){
	case MONO_TYPE_SZARRAY:
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

/** 
 * @mh: The Method header
 * @ptr: Points to the beginning of the Section Data (25.3)
 */
static void
parse_section_data (MonoMethodHeader *mh, const unsigned char *ptr)
{
	unsigned char sect_data_flags;
	const unsigned char *sptr;
	int is_fat;
	guint32 sect_data_len;
	
	while (1) {
		/* align on 32-bit boundary */
		/* FIXME: not 64-bit clean code */
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
			mh->clauses = g_new0 (MonoExceptionClause, mh->num_clauses);
			for (i = 0; i < mh->num_clauses; ++i) {
				MonoExceptionClause *ec = &mh->clauses [i];
				if (is_fat) {
					/* we could memcpy and byteswap */
					ec->flags = read32 (p);
					p += 4;
					ec->try_offset = read32 (p);
					p += 4;
					ec->try_len = read32 (p);
					p += 4;
					ec->handler_offset = read32 (p);
					p += 4;
					ec->handler_len = read32 (p);
					p += 4;
					ec->token_or_filter = read32 (p);
					p += 4;
				} else {
					ec->flags = read16 (p);
					p += 2;
					ec->try_offset = read16 (p);
					p += 2;
					ec->try_len = *p;
					++p;
					ec->handler_offset = read16 (p);
					p += 2;
					ec->handler_len = *p;
					++p;
					ec->token_or_filter = read32 (p);
					p += 4;
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

MonoMethodHeader *
mono_metadata_parse_mh (MonoMetadata *m, const char *ptr)
{
	MonoMethodHeader *mh;
	unsigned char flags = *(unsigned char *) ptr;
	unsigned char format = flags & METHOD_HEADER_FORMAT_MASK;
	guint16 fat_flags;
	guint32 local_var_sig_tok;
	int hsize;
	
	g_return_val_if_fail (ptr != NULL, NULL);

	mh = g_new0 (MonoMethodHeader, 1);
	switch (format){
	case METHOD_HEADER_TINY_FORMAT:
		ptr++;
		mh->max_stack = 8;
		local_var_sig_tok = 0;
		mh->code_size = flags >> 2;
		mh->code = ptr;
		break;
		
	case METHOD_HEADER_TINY_FORMAT1:
		ptr++;
		mh->max_stack = 8;
		local_var_sig_tok = 0;

		//
		// The spec claims 3 bits, but the Beta2 is
		// incorrect
		//
		mh->code_size = flags >> 2;
		mh->code = ptr;
		break;
		
	case METHOD_HEADER_FAT_FORMAT:
		fat_flags = read16 (ptr);
		ptr += 2;
		hsize = (fat_flags >> 12) & 0xf;
		mh->max_stack = *(guint16 *) ptr;
		ptr += 2;
		mh->code_size = *(guint32 *) ptr;
		ptr += 4;
		local_var_sig_tok = *(guint32 *) ptr;
		ptr += 4;

		if (fat_flags & METHOD_HEADER_INIT_LOCALS)
			mh->init_locals = 1;
		else
			mh->init_locals = 0;

		mh->code = ptr;

		if (!(fat_flags & METHOD_HEADER_MORE_SECTS))
			break;

		/*
		 * There are more sections
		 */
		ptr = mh->code + mh->code_size;
		
		parse_section_data (mh, (const unsigned char*)ptr);
		break;
		
	default:
		g_free (mh);
		return NULL;
	}
		       
	if (local_var_sig_tok) {
		MonoTableInfo *t = &m->tables [MONO_TABLE_STANDALONESIG];
		const char *ptr;
		guint32 cols [MONO_STAND_ALONG_SIGNATURE_SIZE];
		int len=0, i, bsize;
		guint offset = 0;

		mono_metadata_decode_row (t, (local_var_sig_tok & 0xffffff)-1, cols, 1);
		ptr = mono_metadata_blob_heap (m, cols [MONO_STAND_ALONG_SIGNATURE]);
		bsize = mono_metadata_decode_blob_size (ptr, &ptr);
		if (*ptr != 0x07)
			g_warning ("wrong signature for locals blob");
		ptr++;
		len = mono_metadata_decode_value (ptr, &ptr);
		mh->num_locals = len;
		if (!mh->num_locals)
			return mh;
		mh->locals = g_new (MonoType*, len);
		for (i = 0; i < len; ++i) {
			int val;
			int align;
			mh->locals [i] = mono_metadata_parse_type (m, MONO_PARSE_LOCAL, 0, ptr, &ptr);

			val = mono_type_size (mh->locals [i], &align);
			offset += (offset % align);
			offset += val;
		}
		mh->locals_size = offset;
	}
	return mh;
}

void
mono_metadata_free_mh (MonoMethodHeader *mh)
{
	int i;
	for (i = 0; i < mh->num_locals; ++i)
		mono_metadata_free_type (mh->locals[i]);
	g_free (mh->locals);
	g_free (mh->clauses);
	g_free (mh);
}

/**
 * mono_metadata_parse_field_type:
 * @m: metadata context to extract information from
 * @ptr: pointer to the field signature
 *
 * Parses the field signature, and returns the type information for it. 
 *
 * Returns: The MonoType that was extracted from @ptr.
 */
MonoType *
mono_metadata_parse_field_type (MonoMetadata *m, short field_flags, const char *ptr, const char **rptr)
{
	return mono_metadata_parse_type (m, MONO_PARSE_FIELD, field_flags, ptr, rptr);
}

MonoType *
mono_metadata_parse_param (MonoMetadata *m, const char *ptr, const char **rptr)
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
	int table, idx;

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

#define CSIZE(x) (sizeof (x) / 4)

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
 * METHODSEM	PROPERTY	ASSOCIATION (encoded index)
 *
 * Note that we still don't support encoded indexes.
 *
 */
static int
typedef_locator (const void *a, const void *b)
{
	locator_t *loc = (locator_t *) a;
	char *bb = (char *) b;
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

guint32
mono_metadata_typedef_from_field (MonoMetadata *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_TYPEDEF];
	locator_t loc;
	
	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_TYPEDEF_FIELD_LIST;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		g_assert_not_reached ();

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

guint32
mono_metadata_typedef_from_method (MonoMetadata *meta, guint32 index)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_TYPEDEF];
	locator_t loc;
	
	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_TYPEDEF_METHOD_LIST;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		g_assert_not_reached ();

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

#ifndef __GNUC__
#define __alignof__(a) sizeof(a)
#endif

/*
 * mono_type_size:
 * @t: the type to return the size of
 *
 * Returns: the number of bytes required to hold an instance of this
 * type in memory
 */
int
mono_type_size (MonoType *t, gint *align)
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
		*align = __alignof__(char);
		return sizeof (char);
		
	case MONO_TYPE_CHAR:
		*align = __alignof__(short);
		return sizeof (short);
		
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		*align = __alignof__(char);
		return 1;
		
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
		guint32 size;

		size = mono_class_value_size (t->data.klass, align);
		return size;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_TYPEDBYREF: /* we may want to use a struct {MonoType* type, void *data } instead ...*/
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	default:
		g_error ("type 0x%02x unknown", t->type);
	}
	return 0;
}

static gboolean
mono_metadata_type_equal (MonoMetadata *m1, MonoType *t1, MonoMetadata *m2, MonoType *t2)
{
	if (t1->type != t2->type ||
	    t1->byref != t2->byref)
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
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
		return t1->data.klass == t2->data.klass;
	default:
		g_error ("implement type compare for %0x!", t1->type);
		return FALSE;
	}

	return TRUE;
}

gboolean
mono_metadata_signature_equal (MonoMetadata *m1, MonoMethodSignature *sig1, 
			       MonoMetadata *m2, MonoMethodSignature *sig2)
{
	int i;

	if (sig1->hasthis != sig2->hasthis ||
	    sig1->param_count != sig2->param_count)
		return FALSE;

	for (i = 0; i < sig1->param_count; i++) { 
		MonoType *p1 = sig1->params[i];
		MonoType *p2 = sig2->params[i];
		
		if (p1->attrs != p2->attrs)
			return FALSE;
		
		if (!mono_metadata_type_equal (m1, p1, m2, p2))
			return FALSE;
	}

	return TRUE;
}

