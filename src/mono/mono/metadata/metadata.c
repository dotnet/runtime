/*
 * metadata.c: Routines for accessing the metadata
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include "metadata.h"

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
	{ MONO_MT_MDOR_IDX,  "MethodBody" },
	{ MONO_MT_MDOR_IDX,  "MethodDeclaration" },
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

const char *
mono_meta_table_name (int table)
{
	if ((table < 0) || (table > 0x29))
		return "";
	
	return tables [table].name;
}

#define rtsize(s,b) (((s) > (1 << (b)) ? 4 : 2))
		 
static int
compute_size (metadata_t *meta, MonoMetaTable *table, int rowcount)
{
	int tsize =  rowcount > 65536 ? 4 : 2;
	int size = 0;
	int i, n, code;

	for (i = 0; (code = table [i].code) != MONO_MT_END; i++){
		switch (code){
		case MONO_MT_UINT32:
			size += 4; break;
			
		case MONO_MT_UINT16:
			size += 2; break;
			
		case MONO_MT_UINT8:
			size += 1; break;
			
		case MONO_MT_BLOB_IDX:
			size += meta->idx_blob_wide ? 4 : 2; break;
			
		case MONO_MT_STRING_IDX:
			size += meta->idx_string_wide ? 4 : 2; break;
			
		case MONO_MT_GUID_IDX:
			size += meta->idx_string_wide ? 4 : 2; break;

		case MONO_MT_TABLE_IDX:
			size += tsize; break;

			/*
			 * HasConstant: ParamDef, FieldDef, Property
			 */
		case MONO_MT_CONST_IDX:
			n = MAX (meta->tables [META_TABLE_PARAM].rows,
				 meta->tables [META_TABLE_FIELD].rows);
			n = MAX (n, meta->tables [META_TABLE_PROPERTY].rows);

			/* 2 bits to encode tag */
			size += rtsize (n, 16-2);
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
			if (meta->idx_blob_wide){
				size += 4;
				break;
			}
			
			n = MAX (meta->tables [META_TABLE_METHOD].rows,
				 meta->tables [META_TABLE_FIELD].rows);
			n = MAX (n, meta->tables [META_TABLE_TYPEREF].rows);
			n = MAX (n, meta->tables [META_TABLE_TYPEDEF].rows);
			n = MAX (n, meta->tables [META_TABLE_PARAM].rows);
			n = MAX (n, meta->tables [META_TABLE_INTERFACEIMPL].rows);
			n = MAX (n, meta->tables [META_TABLE_MEMBERREF].rows);
			n = MAX (n, meta->tables [META_TABLE_MODULE].rows);
			/* Permission seems to be a blob heap pointer */
			n = MAX (n, meta->tables [META_TABLE_PROPERTY].rows);
			n = MAX (n, meta->tables [META_TABLE_EVENT].rows);
			/* Signature seems to be a blob heap pointer */
			n = MAX (n, meta->tables [META_TABLE_MODULEREF].rows);
			n = MAX (n, meta->tables [META_TABLE_TYPESPEC].rows);
			n = MAX (n, meta->tables [META_TABLE_ASSEMBLY].rows);
			n = MAX (n, meta->tables [META_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [META_TABLE_FILE].rows);
			n = MAX (n, meta->tables [META_TABLE_EXPORTEDTYPE].rows);
			n = MAX (n, meta->tables [META_TABLE_MANIFESTRESOURCE].rows);

			/* 5 bits to encode */
			size += rtsize (n, 16-5);
			break;

			/*
			 * CustomAttributeType: TypeDef, TypeRef, MethodDef, 
			 * MemberRef and String.  
			 */
		case MONO_MT_CAT_IDX:
			/* String is a heap, if it is wide, we know the size */
			if (meta->idx_string_wide){
				size += 4;
				break;
			}
			
			n = MAX (meta->tables [META_TABLE_TYPEREF].rows,
				 meta->tables [META_TABLE_TYPEDEF].rows);
			n = MAX (n, meta->tables [META_TABLE_METHOD].rows);
			n = MAX (n, meta->tables [META_TABLE_MEMBERREF].rows);

			/* 3 bits to encode */
			size += rtsize (n, 16-3);
			break;

			/*
			 * HasDeclSecurity: Typedef, MethodDef, Assembly
			 */
		case MONO_MT_HASDEC_IDX:
			n = MAX (meta->tables [META_TABLE_TYPEDEF].rows,
				 meta->tables [META_TABLE_METHOD].rows);
			n = MAX (n, meta->tables [META_TABLE_ASSEMBLY].rows);

			/* 2 bits to encode */
			size += rtsize (n, 16-2);
			break;

			/*
			 * Implementation: File, AssemblyRef, ExportedType
			 */
		case MONO_MT_IMPL_IDX:
			n = MAX (meta->tables [META_TABLE_FILE].rows,
				 meta->tables [META_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [META_TABLE_EXPORTEDTYPE].rows);

			/* 2 bits to encode tag */
			size += rtsize (n, 16-2);
			break;

			/*
			 * HasFieldMarshall: FieldDef, ParamDef
			 */
		case MONO_MT_HFM_IDX:
			n = MAX (meta->tables [META_TABLE_FIELD].rows,
				 meta->tables [META_TABLE_PARAM].rows);

			/* 1 bit used to encode tag */
			size += rtsize (n, 16-1);
			break;

			/*
			 * MemberForwarded: FieldDef, MethodDef
			 */
		case MONO_MT_MF_IDX:
			n = MAX (meta->tables [META_TABLE_FIELD].rows,
				 meta->tables [META_TABLE_METHOD].rows);

			/* 1 bit used to encode tag */
			size += rtsize (n, 16-1);
			break;

			/*
			 * TypeDefOrRef: TypeDef, ParamDef, TypeSpec
			 */
		case MONO_MT_TDOR_IDX:
			n = MAX (meta->tables [META_TABLE_TYPEDEF].rows,
				 meta->tables [META_TABLE_PARAM].rows);
			n = MAX (n, meta->tables [META_TABLE_TYPESPEC].rows);

			/* 2 bits to encode */
			size += rtsize (n, 16-2);
			break;

			/*
			 * MemberRefParent: TypeDef, TypeRef, ModuleDef, ModuleRef, TypeSpec
			 */
		case MONO_MT_MRP_IDX:
			n = MAX (meta->tables [META_TABLE_TYPEDEF].rows,
				 meta->tables [META_TABLE_TYPEREF].rows);
			n = MAX (n, meta->tables [META_TABLE_MODULE].rows);
			n = MAX (n, meta->tables [META_TABLE_MODULEREF].rows);
			n = MAX (n, meta->tables [META_TABLE_TYPESPEC].rows);

			/* 3 bits to encode */
			size += rtsize (n, 16 - 3);
			break;
			
		case MONO_MT_MDOR_IDX:

			/*
			 * MethodDefOrRef: MethodDef, MemberRef
			 */
		case MONO_MT_HS_IDX:
			n = MAX (meta->tables [META_TABLE_METHOD].rows,
				 meta->tables [META_TABLE_MEMBERREF].rows);

			/* 1 bit used to encode tag */
			size += rtsize (n, 16-1);
			break;

			/*
			 * ResolutionScope: Module, ModuleRef, AssemblyRef, TypeRef
			 */
		case MONO_MT_RS_IDX:
			n = MAX (meta->tables [META_TABLE_MODULE].rows,
				 meta->tables [META_TABLE_MODULEREF].rows);
			n = MAX (n, meta->tables [META_TABLE_ASSEMBLYREF].rows);
			n = MAX (n, meta->tables [META_TABLE_TYPEREF].rows);

			/* 3 bits used to encode tag */
			size += rtsize (n, 16 - 3);
			break;
		}
	}

	return size;
}

void
mono_metadata_compute_table_bases (metadata_t *meta)
{
	int i;
	char *base = meta->tables_base;
	
	for (i = 0; i < 64; i++){
		if (meta->tables [i].rows == 0)
			continue;

		meta->tables [i].row_size = compute_size (
			meta, tables [i].table, meta->tables [i].rows);
		meta->tables [i].base = base;
		base += meta->tables [i].rows * meta->tables [i].row_size;
	}
}
	
char *
mono_metadata_locate (metadata_t *meta, int table, int idx)
{
	/* idx == 0 refers always to NULL */
	   
	return meta->tables [table].base + (meta->tables [table].row_size * (idx - 1));
}

char *
mono_metadata_locate_token (metadata_t *meta, guint32 token)
{
	return mono_metadata_locate (meta, token >> 24, token & 0xffffff);
}
