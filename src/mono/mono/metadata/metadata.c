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
#include <glib.h>
#include "metadata.h"
#include "tabledefs.h"
#include "mono-endian.h"
#include "cil-coff.h"
#include "tokentype.h"
#include "metadata-internals.h"
#include "class-internals.h"
#include "private.h"
#include "class.h"

static void do_mono_metadata_parse_type (MonoType *type, MonoImage *m, const char *ptr, const char **rptr);

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

const static MonoMetaTable AssemblySchema [] = {
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
	
const static MonoMetaTable AssemblyOSSchema [] = {
	{ MONO_MT_UINT32,     "OSPlatformID" },
	{ MONO_MT_UINT32,     "OSMajor" },
	{ MONO_MT_UINT32,     "OSMinor" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable AssemblyProcessorSchema [] = {
	{ MONO_MT_UINT32,     "Processor" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable AssemblyRefSchema [] = {
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

const static MonoMetaTable AssemblyRefOSSchema [] = {
	{ MONO_MT_UINT32,     "OSPlatformID" },
	{ MONO_MT_UINT32,     "OSMajorVersion" },
	{ MONO_MT_UINT32,     "OSMinorVersion" },
	{ MONO_MT_TABLE_IDX,  "AssemblyRef:AssemblyRef" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable AssemblyRefProcessorSchema [] = {
	{ MONO_MT_UINT32,     "Processor" },
	{ MONO_MT_TABLE_IDX,  "AssemblyRef:AssemblyRef" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable ClassLayoutSchema [] = {
	{ MONO_MT_UINT16,     "PackingSize" },
	{ MONO_MT_UINT32,     "ClassSize" },
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable ConstantSchema [] = {
	{ MONO_MT_UINT8,      "Type" },
	{ MONO_MT_UINT8,      "PaddingZero" },
	{ MONO_MT_CONST_IDX,  "Parent" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable CustomAttributeSchema [] = {
	{ MONO_MT_HASCAT_IDX, "Parent" },
	{ MONO_MT_CAT_IDX,    "Type" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable DeclSecuritySchema [] = {
	{ MONO_MT_UINT16,     "Action" },
	{ MONO_MT_HASDEC_IDX, "Parent" },
	{ MONO_MT_BLOB_IDX,   "PermissionSet" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable EventMapSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "EventList:Event" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable EventSchema [] = {
	{ MONO_MT_UINT16,     "EventFlags#EventAttribute" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_TABLE_IDX,  "EventType" }, /* TypeDef or TypeRef */
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable ExportedTypeSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_TABLE_IDX,  "TypeDefId" },
	{ MONO_MT_STRING_IDX, "TypeName" },
	{ MONO_MT_STRING_IDX, "TypeNameSpace" },
	{ MONO_MT_IMPL_IDX,   "Implementation" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable FieldSchema [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }	
};
const static MonoMetaTable FieldLayoutSchema [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_TABLE_IDX,  "Field:Field" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable FieldMarshalSchema [] = {
	{ MONO_MT_HFM_IDX,    "Parent" },
	{ MONO_MT_BLOB_IDX,   "NativeType" },
	{ MONO_MT_END, NULL }	
};
const static MonoMetaTable FieldRVASchema [] = {
	{ MONO_MT_UINT32,     "RVA" },
	{ MONO_MT_TABLE_IDX,  "Field:Field" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable FileSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Value" }, 
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable ImplMapSchema [] = {
	{ MONO_MT_UINT16,     "MappingFlag" },
	{ MONO_MT_MF_IDX,     "MemberForwarded" },
	{ MONO_MT_STRING_IDX, "ImportName" },
	{ MONO_MT_TABLE_IDX,  "ImportScope:ModuleRef" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable InterfaceImplSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" }, 
	{ MONO_MT_TDOR_IDX,  "Interface=TypeDefOrRef" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable ManifestResourceSchema [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_IMPL_IDX,   "Implementation" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable MemberRefSchema [] = {
	{ MONO_MT_MRP_IDX,    "Class" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable MethodSchema [] = {
	{ MONO_MT_UINT32,     "RVA" },
	{ MONO_MT_UINT16,     "ImplFlags#MethodImplAttributes" },
	{ MONO_MT_UINT16,     "Flags#MethodAttribute" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_TABLE_IDX,  "ParamList:Param" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable MethodImplSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" },
	{ MONO_MT_MDOR_IDX,   "MethodBody" },
	{ MONO_MT_MDOR_IDX,   "MethodDeclaration" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable MethodSemanticsSchema [] = {
	{ MONO_MT_UINT16,     "MethodSemantic" },
	{ MONO_MT_TABLE_IDX,  "Method:Method" },
	{ MONO_MT_HS_IDX,     "Association" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable ModuleSchema [] = {
	{ MONO_MT_UINT16,     "Generation" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_GUID_IDX,   "MVID" },
	{ MONO_MT_GUID_IDX,   "EncID" },
	{ MONO_MT_GUID_IDX,   "EncBaseID" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable ModuleRefSchema [] = {
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable NestedClassSchema [] = {
	{ MONO_MT_TABLE_IDX,  "NestedClass:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "EnclosingClass:TypeDef" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable ParamSchema [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_UINT16,     "Sequence" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable PropertySchema [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Type" },
	{ MONO_MT_END, NULL }	
};

const static MonoMetaTable PropertyMapSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "PropertyList:Property" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable StandaloneSigSchema [] = {
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable TypeDefSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Namespace" },
	{ MONO_MT_TDOR_IDX,   "Extends" },
	{ MONO_MT_TABLE_IDX,  "FieldList:Field" },
	{ MONO_MT_TABLE_IDX,  "MethodList:Method" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable TypeRefSchema [] = {
	{ MONO_MT_RS_IDX,     "ResolutionScope=ResolutionScope" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Namespace" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable TypeSpecSchema [] = {
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable GenericParamSchema [] = {
	{ MONO_MT_UINT16,     "Number" },
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_TABLE_IDX,  "Owner" }, /* TypeDef or MethodDef */
	{ MONO_MT_STRING_IDX, "Name" },

	/* soon to be removed */
	{ MONO_MT_TABLE_IDX,  "Kind" }, 
	
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable MethodSpecSchema [] = {
	{ MONO_MT_MDOR_IDX,   "Method" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

const static MonoMetaTable GenericParamConstraintSchema [] = {
	{ MONO_MT_TABLE_IDX,  "GenericParam" },
	{ MONO_MT_TDOR_IDX,   "Constraint" },
	{ MONO_MT_END, NULL }
};

const static struct {
	const MonoMetaTable *table;
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
	/* 2A */ { GenericParamSchema,         "GenericParam" },
	/* 2B */ { MethodSpecSchema,           "MethodSpec" },
	/* 2C */ { GenericParamConstraintSchema, "GenericParamConstraint" },
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
	if ((table < 0) || (table > 0x2c))
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
 */
int
mono_metadata_compute_size (MonoImage *meta, int tableindex, guint32 *result_bitfield)
{
	guint32 bitfield = 0;
	int size = 0, field_size;
	int i, n, code;
	int shift = 0;
	const MonoMetaTable *table = tables [tableindex].table;

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
				/* the index is in another metadata file, so it must be 4 */
				field_size = 4; break;
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
			case MONO_TABLE_GENERICPARAM:
				g_assert (i == 2 || i == 4 || i == 5);
				if (i == 2)
					field_size = MAX (idx_size (MONO_TABLE_METHOD), idx_size (MONO_TABLE_TYPEDEF));
				else if (i == 4)
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
			if (tableindex != MONO_TABLE_EXPORTEDTYPE && field_size != idx_size (tableindex))
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
mono_metadata_compute_table_bases (MonoImage *meta)
{
	int i;
	const char *base = meta->tables_base;
	
	for (i = 0; i < 64; i++){
		if (meta->tables [i].rows == 0)
			continue;

		meta->tables [i].row_size = mono_metadata_compute_size (
			meta, i, &meta->tables [i].size_bitfield);
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
 * Returns a pointer to the data in the metadata represented by the
 * token #token.
 */
const char *
mono_metadata_locate_token (MonoImage *meta, guint32 token)
{
	return mono_metadata_locate (meta, token >> 24, token & 0xffffff);
}

/**
 * mono_metadata_get_table:
 * @table: table to retrieve
 *
 * Returns the MonoMetaTable structure for table @table
 */
const MonoMetaTable *
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

static const char *
dword_align (const char *ptr)
{
#if SIZEOF_VOID_P == 8
	return (const char *) (((guint64) (ptr + 3)) & ~3);
#else
	return (const char *) (((guint32) (ptr + 3)) & ~3);
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
mono_metadata_decode_row (MonoTableInfo *t, int idx, guint32 *res, int res_size)
{
	guint32 bitfield = t->size_bitfield;
	int i, count = mono_metadata_table_count (bitfield);
	const char *data = t->base + idx * t->row_size;
	
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
	register const char *data = t->base + idx * t->row_size;
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
		*rptr = ptr;
	
	return len;
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
	if ((*ptr == MONO_TYPE_CMOD_OPT) ||
	    (*ptr == MONO_TYPE_CMOD_REQD)) {
		if (!dest)
			dest = &local;
		dest->required = *ptr == MONO_TYPE_CMOD_REQD ? 1 : 0;
		dest->token = mono_metadata_parse_typedef_or_ref (m, ptr + 1, rptr);
		return TRUE;
	}
	return FALSE;
}

/*
 * mono_metadata_parse_array:
 * @m: a metadata context.
 * @ptr: a pointer to an encoded array description.
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Decodes the compressed array description found in the metadata @m at @ptr.
 *
 * Returns: a #MonoArrayType structure describing the array type
 * and dimensions.
 */
MonoArrayType *
mono_metadata_parse_array (MonoImage *m, const char *ptr, const char **rptr)
{
	int i;
	MonoArrayType *array = g_new0 (MonoArrayType, 1);
	MonoType *etype;
	
	etype = mono_metadata_parse_type (m, MONO_PARSE_TYPE, 0, ptr, &ptr);
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
		array->lobounds [i] = mono_metadata_decode_value (ptr, &ptr);

	if (rptr)
		*rptr = ptr;
	return array;
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
};

#define NBUILTIN_TYPES() (sizeof (builtin_types) / sizeof (builtin_types [0]))

static GHashTable *type_cache = NULL;

/*
 * MonoTypes with modifies are never cached, so we never check or use that field.
 */
static guint
mono_type_hash (gconstpointer data)
{
	const MonoType *type = (const MonoType *) data;
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

/**
 * mono_metadata_init:
 *
 *  Initialize the global variables of this module.
 */
void
mono_metadata_init (void)
{
	int i;

	type_cache = g_hash_table_new (mono_type_hash, mono_type_equal);

	for (i = 0; i < NBUILTIN_TYPES (); ++i)
		g_hash_table_insert (type_cache, &builtin_types [i], &builtin_types [i]);
}

/*
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
 * Returns: a #MonoType structure representing the decoded type.
 */
MonoType*
mono_metadata_parse_type (MonoImage *m, MonoParseTypeMode mode, short opt_attrs, const char *ptr, const char **rptr)
{
	MonoType *type, *cached;
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
		type = g_malloc0 (sizeof (MonoType) + ((gint32)count - MONO_ZERO_LEN_ARRAY) * sizeof (MonoCustomMod));
		type->num_mods = count;
		if (count > 64)
			g_warning ("got more than 64 modifiers in type");
	}
	else
		/*
		 * Later we can avoid doing this allocation.
		 */
		type = g_new0 (MonoType, 1);

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
			count ++;
			mono_metadata_parse_custom_mod (m, &(type->modifiers [count]), ptr, &ptr);
			break;
		default:
			found = FALSE;
		}
	}
	
	type->attrs = opt_attrs;
	type->byref = byref;
	type->pinned = pinned ? 1 : 0;

	do_mono_metadata_parse_type (type, m, ptr, &ptr);

	if (rptr)
		*rptr = ptr;

	/* No need to use locking since nobody is modifying the hash table */
	if (mode != MONO_PARSE_PARAM && !type->num_mods && (cached = g_hash_table_lookup (type_cache, type))) {
		mono_metadata_free_type (type);
		return cached;
	} else {
		return type;
	}
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
MonoMethodSignature *
mono_metadata_parse_signature (MonoImage *image, guint32 token)
{
	MonoTableInfo *tables = image->tables;
	guint32 idx = mono_metadata_token_index (token);
	guint32 sig;
	const char *ptr;

	if (image->dynamic)
		return mono_lookup_dynamic_token (image, token);

	g_assert (mono_metadata_token_table(token) == MONO_TABLE_STANDALONESIG);
		
	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	mono_metadata_decode_blob_size (ptr, &ptr);

	return mono_metadata_parse_method_signature (image, FALSE, ptr, NULL); 
}

MonoMethodSignature*
mono_metadata_signature_alloc (MonoImage *m, guint32 nparams)
{
	MonoMethodSignature *sig;

	/* later we want to allocate signatures with mempools */
	sig = g_malloc0 (sizeof (MonoMethodSignature) + ((gint32)nparams - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType*));
	sig->param_count = nparams;
	sig->sentinelpos = -1;

	return sig;
}

MonoMethodSignature*
mono_metadata_signature_dup (MonoMethodSignature *sig)
{
	int sigsize;

	sigsize = sizeof (MonoMethodSignature) + sig->param_count * sizeof (MonoType *);
	return g_memdup (sig, sigsize);
}

/*
 * mono_metadata_parse_method_signature:
 * @m: metadata context
 * @def: the MethodDef index or 0 for Ref signatures.
 * @ptr: pointer to the signature metadata representation
 * @rptr: pointer updated to match the end of the decoded stream
 *
 * Decode a method signature stored at @ptr.
 *
 * Returns: a MonoMethodSignature describing the signature.
 */
MonoMethodSignature *
mono_metadata_parse_method_signature (MonoImage *m, int def, const char *ptr, const char **rptr)
{
	MonoMethodSignature *method;
	int i, ret_attrs = 0, *pattrs = NULL;
	guint32 hasthis = 0, explicit_this = 0, call_convention, param_count;
	guint32 gen_param_count = 0;

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
	pattrs = g_new0 (int, param_count);

	if (def) {
		MonoTableInfo *paramt = &m->tables [MONO_TABLE_PARAM];
		MonoTableInfo *methodt = &m->tables [MONO_TABLE_METHOD];
		guint32 cols [MONO_PARAM_SIZE];
		guint lastp, param_index = mono_metadata_decode_row_col (methodt, def - 1, MONO_METHOD_PARAMLIST);

		if (def < methodt->rows)
			lastp = mono_metadata_decode_row_col (methodt, def, MONO_METHOD_PARAMLIST);
		else
			lastp = paramt->rows + 1;
		for (i = param_index; i < lastp; ++i) {
			mono_metadata_decode_row (paramt, i - 1, cols, MONO_PARAM_SIZE);
			if (!cols [MONO_PARAM_SEQUENCE])
				ret_attrs = cols [MONO_PARAM_FLAGS];
			else
				pattrs [cols [MONO_PARAM_SEQUENCE] - 1] = cols [MONO_PARAM_FLAGS];
		}
	}
	method = mono_metadata_signature_alloc (m, param_count);
	method->hasthis = hasthis;
	method->explicit_this = explicit_this;
	method->call_convention = call_convention;
	method->generic_param_count = gen_param_count;
	if (call_convention != 0xa)
		method->ret = mono_metadata_parse_type (m, MONO_PARSE_RET, ret_attrs, ptr, &ptr);

	if (method->param_count) {
		method->sentinelpos = -1;
		
		for (i = 0; i < method->param_count; ++i) {
			if (*ptr == MONO_TYPE_SENTINEL) {
				if (method->call_convention != MONO_CALL_VARARG || def)
						g_error ("found sentinel for methoddef or no vararg method");
				method->sentinelpos = i;
				ptr++;
			}
			method->params [i] = mono_metadata_parse_type (m, MONO_PARSE_PARAM, pattrs [i], ptr, &ptr);
		}
	}

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
 * mono_metadata_free_method_signature:
 * @sig: signature to destroy
 *
 * Free the memory allocated in the signature @sig.
 */
void
mono_metadata_free_method_signature (MonoMethodSignature *sig)
{
	int i;
	mono_metadata_free_type (sig->ret);
	for (i = 0; i < sig->param_count; ++i)
		mono_metadata_free_type (sig->params [i]);

	g_free (sig);
}

static void
do_mono_metadata_parse_generic_inst (MonoType *type, MonoImage *m, const char *ptr, const char **rptr)
{
	MonoGenericInst *ginst = g_new0 (MonoGenericInst, 1);
	MonoGenericInst *cached;
	int i, count;

	type->data.generic_inst = ginst;

	ginst->context = g_new0 (MonoGenericContext, 1);
	ginst->context->ginst = ginst;

	ginst->klass = g_new0 (MonoClass, 1);

	ginst->generic_type = mono_metadata_parse_type (m, MONO_PARSE_TYPE, 0, ptr, &ptr);
	ginst->type_argc = count = mono_metadata_decode_value (ptr, &ptr);
	ginst->type_argv = g_new0 (MonoType*, count);

	/*
	 * Create the klass before parsing the type arguments.
	 * This is required to support "recursive" definitions.
	 * See mcs/tests/gen-23.cs for an example.
	 */

	ginst->init_pending = TRUE;

	mono_class_create_generic (ginst);

	for (i = 0; i < ginst->type_argc; i++) {
		MonoType *t = mono_metadata_parse_type (m, MONO_PARSE_TYPE, 0, ptr, &ptr);

		ginst->type_argv [i] = t;
		if (!ginst->is_open)
			ginst->is_open = mono_class_is_open_constructed_type (t);
	}


	if (rptr)
		*rptr = ptr;

	/*
	 * We may be called multiple times on different metadata to create the same
	 * instantiated type.  This happens for instance if we're part of a method or
	 * local variable signature.
	 *
	 * It's important to return the same MonoGenericInst * for each particualar
	 * instantiation of a generic type (ie "Stack<Int32>") to make static fields
	 * work.
	 *
	 * According to the spec ($26.1.5), a static variable in a generic class
	 * declaration is shared amongst all instances of the same closed constructed
	 * type.
	 */

	cached = g_hash_table_lookup (m->generic_inst_cache, ginst);
	if (cached) {
		g_free (ginst->klass);
		g_free (ginst->type_argv);
		g_free (ginst);

		type->data.generic_inst = cached;
		return;
	} else {
		g_hash_table_insert (m->generic_inst_cache, ginst, ginst);

		mono_stats.generic_instance_count++;
		mono_stats.generics_metadata_size += sizeof (MonoGenericInst) +
			sizeof (MonoGenericContext) +
			ginst->type_argc * sizeof (MonoType);
	}

	ginst->init_pending = FALSE;
}

static MonoGenericParam *
mono_metadata_parse_generic_param (MonoImage *m, const char *ptr, const char **rptr)
{
	MonoGenericParam *generic_param = g_new0 (MonoGenericParam, 1);
	
	generic_param->num = mono_metadata_decode_value (ptr, &ptr);

	if (rptr)
		*rptr = ptr;

	return generic_param;
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
do_mono_metadata_parse_type (MonoType *type, MonoImage *m, const char *ptr, const char **rptr)
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
	case MONO_TYPE_SZARRAY: {
		MonoType *etype = mono_metadata_parse_type (m, MONO_PARSE_MOD_TYPE, 0, ptr, &ptr);
		type->data.klass = mono_class_from_mono_type (etype);
		mono_metadata_free_type (etype);
		break;
	}
	case MONO_TYPE_PTR:
		type->data.type = mono_metadata_parse_type (m, MONO_PARSE_MOD_TYPE, 0, ptr, &ptr);
		break;
	case MONO_TYPE_FNPTR:
		type->data.method = mono_metadata_parse_method_signature (m, 0, ptr, &ptr);
		break;
	case MONO_TYPE_ARRAY:
		type->data.array = mono_metadata_parse_array (m, ptr, &ptr);
		break;

	case MONO_TYPE_MVAR:
	case MONO_TYPE_VAR:
		type->data.generic_param = mono_metadata_parse_generic_param (m, ptr, &ptr);
		break;

	case MONO_TYPE_GENERICINST:
		do_mono_metadata_parse_generic_inst (type, m, ptr, &ptr);
		break;
		
	default:
		g_error ("type 0x%02x not handled in do_mono_metadata_parse_type", type->type);
	}
	
	if (rptr)
		*rptr = ptr;
}

/*
 * mono_metadata_free_type:
 * @type: type to free
 *
 * Free the memory allocated for type @type.
 */
void
mono_metadata_free_type (MonoType *type)
{
	if (type >= builtin_types && type < builtin_types + NBUILTIN_TYPES ())
		return;
	switch (type->type){
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

/*
 * mono_metadata_parse_mh:
 * @m: metadata context
 * @ptr: pointer to the method header.
 *
 * Decode the method header at @ptr, including pointer to the IL code,
 * info about local variables and optional exception tables.
 *
 * Returns: a MonoMethodHeader.
 */
MonoMethodHeader *
mono_metadata_parse_mh (MonoImage *m, const char *ptr)
{
	MonoMethodHeader *mh;
	unsigned char flags = *(const unsigned char *) ptr;
	unsigned char format = flags & METHOD_HEADER_FORMAT_MASK;
	guint16 fat_flags;
	guint32 local_var_sig_tok, max_stack, code_size, init_locals;
	const unsigned char *code;
	int hsize;
	
	g_return_val_if_fail (ptr != NULL, NULL);

	switch (format){
	case METHOD_HEADER_TINY_FORMAT:
		mh = g_new0 (MonoMethodHeader, 1);
		ptr++;
		mh->max_stack = 8;
		local_var_sig_tok = 0;
		mh->code_size = flags >> 2;
		mh->code = ptr;
		return mh;
	case METHOD_HEADER_TINY_FORMAT1:
		mh = g_new0 (MonoMethodHeader, 1);
		ptr++;
		mh->max_stack = 8;
		local_var_sig_tok = 0;

		/*
		 * The spec claims 3 bits, but the Beta2 is
		 * incorrect
		 */
		mh->code_size = flags >> 2;
		mh->code = ptr;
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

		code = ptr;

		if (!(fat_flags & METHOD_HEADER_MORE_SECTS))
			break;

		/*
		 * There are more sections
		 */
		ptr = code + code_size;
		
		break;
		
	default:
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
		mh = g_malloc0 (sizeof (MonoMethodHeader) + (len - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType*));
		mh->num_locals = len;
		for (i = 0; i < len; ++i)
			mh->locals [i] = mono_metadata_parse_type (m, MONO_PARSE_LOCAL, 0, locals_ptr, &locals_ptr);
	} else {
		mh = g_new0 (MonoMethodHeader, 1);
	}
	mh->code = code;
	mh->code_size = code_size;
	mh->max_stack = max_stack;
	mh->init_locals = init_locals;
	if (fat_flags & METHOD_HEADER_MORE_SECTS)
		parse_section_data (mh, (const unsigned char*)ptr);
	return mh;
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
	for (i = 0; i < mh->num_locals; ++i)
		mono_metadata_free_type (mh->locals[i]);
	g_free (mh->clauses);
	g_free (mh);
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

/*
 * mono_metadata_typedef_from_field:
 * @meta: metadata context
 * @index: FieldDef token
 *
 * Returns the 1-based index into the TypeDef table of the type that
 * declared the field described by @index.
 * Returns 0 if not found.
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
 * Returns the 1-based index into the TypeDef table of the type that
 * declared the method described by @index.
 * Returns 0 if not found.
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

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, typedef_locator))
		g_assert_not_reached ();

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
	return loc.result + 1;
}

/*
 * mono_metadata_interfaces_from_typedef:
 * @meta: metadata context
 * @index: typedef token
 * 
 * Returns and array of interfaces that the @index typedef token implements.
 * The number of elemnts in the array is returned in @count.
 */
MonoClass**
mono_metadata_interfaces_from_typedef (MonoImage *meta, guint32 index, guint *count)
{
	MonoTableInfo *tdef = &meta->tables [MONO_TABLE_INTERFACEIMPL];
	locator_t loc;
	guint32 start, i;
	guint32 cols [MONO_INTERFACEIMPL_SIZE];
	MonoClass **result;
	
	*count = 0;

	if (!tdef->base)
		return NULL;

	loc.idx = mono_metadata_token_index (index);
	loc.col_idx = MONO_INTERFACEIMPL_CLASS;
	loc.t = tdef;

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return NULL;

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
	result = NULL;
	i = 0;
	while (start < tdef->rows) {
		mono_metadata_decode_row (tdef, start, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		result = g_renew (MonoClass*, result, i + 1);
		result [i] = mono_class_get (meta, mono_metadata_token_from_dor (cols [MONO_INTERFACEIMPL_INTERFACE]));
		*count = ++i;
		++start;
	}
	return result;
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
 * @start_index.
 * Returns 0 if no such type is found.
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
 * Returns the info stored in the ClassLAyout table for the given typedef token
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

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return 0;

	/* Find the first entry by searching backwards */
	while ((loc.result > 0) && (mono_metadata_decode_row_col (tdef, loc.result - 1, MONO_CUSTOM_ATTR_PARENT) == index))
		loc.result --;

	/* loc_result is 0..1, needs to be mapped to table index (that is +1) */
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
			return mono_class_value_size (t->data.klass, align);
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
		*align = __alignof__(gpointer);
		return sizeof (gpointer);
	case MONO_TYPE_TYPEDBYREF:
		return mono_class_value_size (mono_defaults.typed_reference_class, align);
	case MONO_TYPE_GENERICINST: {
		MonoGenericInst *ginst = t->data.generic_inst;
		MonoClass *iclass = mono_class_from_mono_type (ginst->generic_type);
		return mono_type_size (&iclass->byval_arg, align);
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
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
mono_type_stack_size (MonoType *t, gint *align)
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
			return mono_type_stack_size (t->data.klass->enum_basetype, align);
		else {
			size = mono_class_value_size (t->data.klass, align);

			*align = *align + __alignof__(gpointer) - 1;
			*align &= ~(__alignof__(gpointer) - 1);

			size += sizeof (gpointer) - 1;
			size &= ~(sizeof (gpointer) - 1);

			return size;
		}
	}
	case MONO_TYPE_GENERICINST: {
		MonoGenericInst *ginst = t->data.generic_inst;
		MonoClass *iclass = mono_class_from_mono_type (ginst->generic_type);
		return mono_type_stack_size (&iclass->byval_arg, align);
	}
	default:
		g_error ("type 0x%02x unknown", t->type);
	}
	return 0;
}

guint
mono_metadata_generic_inst_hash (MonoGenericInst *ginst)
{
	return mono_metadata_type_hash (ginst->generic_type);
}

gboolean
mono_metadata_generic_inst_equal (MonoGenericInst *g1, MonoGenericInst *g2)
{
	int i;

	if (g1->type_argc != g2->type_argc)
		return FALSE;
	if (!mono_metadata_type_equal (g1->generic_type, g2->generic_type))
		return FALSE;
	for (i = 0; i < g1->type_argc; ++i) {
		if (!mono_metadata_type_equal (g1->type_argv [i], g2->type_argv [i]))
			return FALSE;
	}
	return TRUE;
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
		return ((hash << 5) - hash) ^ mono_metadata_generic_inst_hash (t1->data.generic_inst);
	}
	return hash;
}

static gboolean
mono_metadata_class_equal (MonoClass *c1, MonoClass *c2)
{
	if (c1 == c2)
		return TRUE;
	if (c1->generic_inst && c2->generic_inst)
		return mono_metadata_generic_inst_equal (c1->generic_inst, c2->generic_inst);
	if ((c1->byval_arg.type == MONO_TYPE_VAR) && (c2->byval_arg.type == MONO_TYPE_VAR))
		return c1->byval_arg.data.generic_param->num == c2->byval_arg.data.generic_param->num;
	if ((c1->byval_arg.type == MONO_TYPE_MVAR) && (c2->byval_arg.type == MONO_TYPE_MVAR))
		return c1->byval_arg.data.generic_param->num == c2->byval_arg.data.generic_param->num;
	return FALSE;
}

/*
 * mono_metadata_type_equal:
 * @t1: a type
 * @t2: another type
 *
 * Determine if @t1 and @t2 represent the same type.
 * Returns: #TRUE if @t1 and @t2 are equal.
 */
gboolean
mono_metadata_type_equal (MonoType *t1, MonoType *t2)
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
	case MONO_TYPE_TYPEDBYREF:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
		return mono_metadata_class_equal (t1->data.klass, t2->data.klass);
	case MONO_TYPE_PTR:
		return mono_metadata_type_equal (t1->data.type, t2->data.type);
	case MONO_TYPE_ARRAY:
		if (t1->data.array->rank != t2->data.array->rank)
			return FALSE;
		return mono_metadata_class_equal (t1->data.array->eklass, t2->data.array->eklass);
	case MONO_TYPE_GENERICINST:
		return mono_metadata_generic_inst_equal (t1->data.generic_inst,
							 t2->data.generic_inst);
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		return t1->data.generic_param->num == t2->data.generic_param->num;
	default:
		g_error ("implement type compare for %0x!", t1->type);
		return FALSE;
	}

	return FALSE;
}

/*
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

	if (sig1->hasthis != sig2->hasthis ||
	    sig1->param_count != sig2->param_count)
		return FALSE;

	for (i = 0; i < sig1->param_count; i++) { 
		MonoType *p1 = sig1->params[i];
		MonoType *p2 = sig2->params[i];
		
		/* if (p1->attrs != p2->attrs)
			return FALSE;
		*/
		if (!mono_metadata_type_equal (p1, p2))
			return FALSE;
	}

	if (!mono_metadata_type_equal (sig1->ret, sig2->ret))
		return FALSE;
	return TRUE;
}

guint
mono_signature_hash (MonoMethodSignature *sig)
{
	guint i, res = sig->ret->type;

	for (i = 0; i < sig->param_count; i++)
		res = (res << 5) - res + sig->params[i]->type;

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
	MonoType *type;

	mono_loader_lock ();

	if ((type = g_hash_table_lookup (image->typespec_cache, GUINT_TO_POINTER (type_spec)))) {
		mono_loader_unlock ();
		return type;
	}

	t = &image->tables [MONO_TABLE_TYPESPEC];
	
	mono_metadata_decode_row (t, idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (image, cols [MONO_TYPESPEC_SIGNATURE]);
	len = mono_metadata_decode_value (ptr, &ptr);

	type = g_new0 (MonoType, 1);

	g_hash_table_insert (image->typespec_cache, GUINT_TO_POINTER (type_spec), type);

	if (*ptr == MONO_TYPE_BYREF) {
		type->byref = 1; 
		ptr++;
	}

	do_mono_metadata_parse_type (type, image, ptr, &ptr);

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
		if (ptr - start <= len)
			res->data.array_data.elem_type = *ptr++;
		if (ptr - start <= len)
			res->data.array_data.param_num = mono_metadata_decode_value (ptr, &ptr);
		if (ptr - start <= len)
			res->data.array_data.num_elem = mono_metadata_decode_value (ptr, &ptr);
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

	return res;
}

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
				g_error ("cant marshal string to native type %02x", mspec->native);
			}
		} 	
		*conv = MONO_MARSHAL_CONV_STR_LPTSTR;
		return MONO_NATIVE_LPTSTR; 
	case MONO_TYPE_PTR: return MONO_NATIVE_UINT;
	case MONO_TYPE_VALUETYPE: /*FIXME*/
		if (type->data.klass->enumtype) {
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		return MONO_NATIVE_STRUCT;
	case MONO_TYPE_SZARRAY: 
	case MONO_TYPE_ARRAY: 
		if (mspec) {
			switch (mspec->native) {
			case MONO_NATIVE_BYVALARRAY:
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
				else
					/* Fall through */
					;
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
		*conv = MONO_MARSHAL_CONV_OBJECT_STRUCT;
		return MONO_NATIVE_STRUCT;
	}
	case MONO_TYPE_FNPTR: return MONO_NATIVE_FUNC;
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

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return NULL;

	return mono_metadata_blob_heap (meta, mono_metadata_decode_row_col (tdef, loc.result, MONO_FIELD_MARSHAL_NATIVE_TYPE));
}

static MonoMethod*
method_from_method_def_or_ref (MonoImage *m, guint32 tok)
{
	guint32 idx = tok >> MONO_METHODDEFORREF_BITS;
	switch (tok & MONO_METHODDEFORREF_MASK) {
	case MONO_METHODDEFORREF_METHODDEF:
		return mono_get_method (m, MONO_TOKEN_METHOD_DEF | idx, NULL);
	case MONO_METHODDEFORREF_METHODREF:
		return mono_get_method (m, MONO_TOKEN_MEMBER_REF | idx, NULL);
	}
	g_assert_not_reached ();
	return NULL;
}

MonoMethod**
mono_class_get_overrides (MonoImage *image, guint32 type_token, gint32 *num_overrides)
{
	locator_t loc;
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_METHODIMPL];
	guint32 start, end;
	gint32 i, num;
	guint32 cols [MONO_METHODIMPL_SIZE];
	MonoMethod **result;

	if (num_overrides)
		*num_overrides = 0;

	if (!tdef->base)
		return NULL;

	loc.t = tdef;
	loc.col_idx = MONO_METHODIMPL_CLASS;
	loc.idx = mono_metadata_token_index (type_token);

	if (!bsearch (&loc, tdef->base, tdef->rows, tdef->row_size, table_locator))
		return NULL;

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
		mono_metadata_decode_row (tdef, start + i, cols, MONO_METHODIMPL_SIZE);
		result [i * 2] = method_from_method_def_or_ref (image, cols [MONO_METHODIMPL_DECLARATION]);
		result [i * 2 + 1] = method_from_method_def_or_ref (image, cols [MONO_METHODIMPL_BODY]);
	}

	if (num_overrides)
		*num_overrides = num;
	return result;
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

static MonoClass**
get_constraints (MonoImage *image, int owner)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	guint32 cols [MONO_GENPARCONSTRAINT_SIZE];
	guint32 i, token, found;
	MonoClass *klass, **res;
	GList *cons = NULL, *tmp;
	

	found = 0;
	for (i = 0; i < tdef->rows; ++i) {
		mono_metadata_decode_row (tdef, i, cols, MONO_GENPARCONSTRAINT_SIZE);
		if (cols [MONO_GENPARCONSTRAINT_GENERICPAR] == owner) {
			token = mono_metadata_token_from_dor (cols [MONO_GENPARCONSTRAINT_CONSTRAINT]);
			klass = mono_class_get (image, token);
			cons = g_list_append (cons, klass);
			++found;
		} else {
			/* contiguous list finished */
			if (found)
				break;
		}
	}
	if (!found)
		return NULL;
	res = g_new0 (MonoClass*, found + 1);
	for (i = 0, tmp = cons; i < found; ++i, tmp = tmp->next) {
		res [i] = tmp->data;
	}
	g_list_free (cons);
	return res;
}

MonoGenericParam *
mono_metadata_load_generic_params (MonoImage *image, guint32 token, guint32 *num)
{
	MonoTableInfo *tdef  = &image->tables [MONO_TABLE_GENERICPARAM];
	guint32 cols [MONO_GENERICPARAM_SIZE];
	guint32 i, owner, last_num, n;
	MonoGenericParam *params;

	if (mono_metadata_token_table (token) == MONO_TABLE_TYPEDEF)
		owner = MONO_TYPEORMETHOD_TYPE;
	else if (mono_metadata_token_table (token) == MONO_TABLE_METHOD)
		owner = MONO_TYPEORMETHOD_METHOD;
	else {
		g_error ("wrong token %x to load_generics_params", token);
	}
	owner |= mono_metadata_token_index (token) << MONO_TYPEORMETHOD_BITS;
	if (num)
		*num = 0;
	if (!tdef->base)
		return NULL;

	for (i = 0; i < tdef->rows; ++i) {
		mono_metadata_decode_row (tdef, i, cols, MONO_GENERICPARAM_SIZE);
		if (cols [MONO_GENERICPARAM_OWNER] == owner)
			break;
	}
	last_num = 0;
	if (i >= tdef->rows)
		return NULL;
	params = NULL;
	n = 0;
	do {
		n++;
		params = g_realloc (params, sizeof (MonoGenericParam) * n);
		params [n - 1].pklass = NULL;
		params [n - 1].method = NULL;
		params [n - 1].flags = cols [MONO_GENERICPARAM_FLAGS];
		params [n - 1].num = cols [MONO_GENERICPARAM_NUMBER];
		params [n - 1].name = mono_metadata_string_heap (image, cols [MONO_GENERICPARAM_NAME]);
		params [n - 1].constraints = get_constraints (image, i + 1);
		if (++i >= tdef->rows)
			break;
		mono_metadata_decode_row (tdef, i, cols, MONO_GENERICPARAM_SIZE);
	} while (cols [MONO_GENERICPARAM_OWNER] == owner);
	
	if (num)
		*num = n;
	return params;
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

