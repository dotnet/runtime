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
	{ MONO_MT_TABLE_IDX,  "Parent" },
	{ MONO_MT_TABLE_IDX,  "Type=CustomAttributeType" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable DeclSecuritySchema [] = {
	{ MONO_MT_UINT16,     "Action" },
	{ MONO_MT_TABLE_IDX,  "Parent=HasDeclSecurity" },
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
	{ MONO_MT_TABLE_IDX,  "EventType" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable ExportedTypeSchema [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_TABLE_IDX,  "TypeDefId" },
	{ MONO_MT_STRING_IDX, "TypeName" },
	{ MONO_MT_STRING_IDX, "TypeNameSpace" },
	{ MONO_MT_TABLE_IDX,  "Implementation" },
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
	{ MONO_MT_TABLE_IDX,  "Parent" },
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
	{ MONO_MT_TABLE_IDX,  "MemberForwarded=MemberForwardedCodedIndex" },
	{ MONO_MT_STRING_IDX, "ImportName" },
	{ MONO_MT_TABLE_IDX,  "ImportScope:ModuleRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable InterfaceImplSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "Interface=TypeDefOrRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ManifestResourceSchema [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_TABLE_IDX,  "Implementation=Implementation" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MemberRefSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class=MemberRefParent" },
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
	{ MONO_MT_TABLE_IDX,  "ParamList" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodImplSchema [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "MethodBody=MethodDefOrRef" },
	{ MONO_MT_TABLE_IDX,  "MethodDeclaration=MethodDefOrRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodSemanticsSchema [] = {
	{ MONO_MT_UINT16,     "MethodSemantic" },
	{ MONO_MT_TABLE_IDX,  "Method:Method" },
	{ MONO_MT_TABLE_IDX,  "Association=HasSemantic" },
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
	{ MONO_MT_TABLE_IDX,  "Extends=TypeDefOrRef" },
	{ MONO_MT_TABLE_IDX,  "FieldList:Field" },
	{ MONO_MT_TABLE_IDX,  "MethodList:Method" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeRefSchema [] = {
	{ MONO_MT_TABLE_IDX,  "ResolutionScope=ResolutionScope" },
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
