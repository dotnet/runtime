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
static MonoMetaTable AssemblyTable [] = {
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
	
static MonoMetaTable AssemblyOSTable [] = {
	{ MONO_MT_UINT32,     "OSPlatformID" },
	{ MONO_MT_UINT32,     "OSMajor" },
	{ MONO_MT_UINT32,     "OSMinor" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyProcessorTable [] = {
	{ MONO_MT_UINT32,     "Processor" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyRefTable [] = {
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

static MonoMetaTable AssemblyRefOSTable [] = {
	{ MONO_MT_UINT32,     "OSPlatformID" },
	{ MONO_MT_UINT32,     "OSMajorVersion" },
	{ MONO_MT_UINT32,     "OSMinorVersion" },
	{ MONO_MT_TABLE_IDX,  "AssemblyRef:AssemblyRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable AssemblyRefProcessorTable [] = {
	{ MONO_MT_UINT32,     "Processor" },
	{ MONO_MT_TABLE_IDX,  "AssemblyRef:AssemblyRef" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable ClassLayoutTable [] = {
	{ MONO_MT_UINT16,     "PackingSize" },
	{ MONO_MT_UINT32,     "ClassSize" },
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ConstantTable [] = {
	{ MONO_MT_UINT8,      "Type" },
	{ MONO_MT_UINT8,      "PaddingZero" },
	{ MONO_MT_TABLE_IDX,  "Parent" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable CustomAttributeTable [] = {
	{ MONO_MT_TABLE_IDX,  "Parent" },
	{ MONO_MT_TABLE_IDX,  "Type=CustomAttributeType" },
	{ MONO_MT_BLOB_IDX,   "Value" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable DeclSecurityTable [] = {
	{ MONO_MT_UINT16,     "Action" },
	{ MONO_MT_TABLE_IDX,  "Parent=HasDeclSecurity" },
	{ MONO_MT_BLOB_IDX,   "PermissionSet" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable EventMapTable [] = {
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "EventList:Event" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable EventTable [] = {
	{ MONO_MT_UINT16,     "EventFlags#EventAttribute" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_TABLE_IDX,  "EventType" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable ExportedTypeTable [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_TABLE_IDX,  "TypeDefId" },
	{ MONO_MT_STRING_IDX, "TypeName" },
	{ MONO_MT_STRING_IDX, "TypeNameSpace" },
	{ MONO_MT_TABLE_IDX,  "Implementation" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable FieldTable [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }	
};
static MonoMetaTable FieldLayoutTable [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_TABLE_IDX,  "Field:Field" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable FieldMarshalTable [] = {
	{ MONO_MT_TABLE_IDX,  "Parent" },
	{ MONO_MT_BLOB_IDX,   "NativeType" },
	{ MONO_MT_END, NULL }	
};
static MonoMetaTable FieldRVATable [] = {
	{ MONO_MT_UINT32,     "RVA" },
	{ MONO_MT_TABLE_IDX,  "Field:Field" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable FileTable [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Value" }, 
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ImplMapTable [] = {
	{ MONO_MT_UINT16,     "MappingFlag" },
	{ MONO_MT_TABLE_IDX,  "MemberForwarded=MemberForwardedCodedIndex" },
	{ MONO_MT_STRING_IDX, "ImportName" },
	{ MONO_MT_TABLE_IDX,  "ImportScope:ModuleRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable InterfaceImplTable [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "Interface=TypeDefOrRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ManifestResourceTable [] = {
	{ MONO_MT_UINT32,     "Offset" },
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_TABLE_IDX,  "Implementation=Implementation" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MemberRefTable [] = {
	{ MONO_MT_TABLE_IDX,  "Class=MemberRefParent" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodTable [] = {
	{ MONO_MT_UINT32,     "RVA" },
	{ MONO_MT_UINT16,     "ImplFlags#MethodImplAttributes" },
	{ MONO_MT_UINT16,     "Flags#MethodAttribute" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_TABLE_IDX,  "ParamList" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodImplTable [] = {
	{ MONO_MT_TABLE_IDX,  "Class:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "MethodBody=MethodDefOrRef" },
	{ MONO_MT_TABLE_IDX,  "MethodDeclaration=MethodDefOrRef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable MethodSemanticsTable [] = {
	{ MONO_MT_UINT16,     "MethodSemantic" },
	{ MONO_MT_TABLE_IDX,  "Method:Method" },
	{ MONO_MT_TABLE_IDX,  "Association=HasSemantic" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ModuleTable [] = {
	{ MONO_MT_UINT16,     "Generation" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_GUID_IDX,   "MVID" },
	{ MONO_MT_GUID_IDX,   "EncID" },
	{ MONO_MT_GUID_IDX,   "EncBaseID" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ModuleRefTable [] = {
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable NestedClassTable [] = {
	{ MONO_MT_TABLE_IDX,  "NestedClass:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "EnclosingClass:TypeDef" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable ParamTable [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_UINT16,     "Sequence" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable PropertyTable [] = {
	{ MONO_MT_UINT16,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_BLOB_IDX,   "Type" },
	{ MONO_MT_END, NULL }	
};

static MonoMetaTable PropertyMapTable [] = {
	{ MONO_MT_TABLE_IDX,  "Parent:TypeDef" },
	{ MONO_MT_TABLE_IDX,  "PropertyList:Property" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable StandaloneSigTable [] = {
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeDefTable [] = {
	{ MONO_MT_UINT32,     "Flags" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Namespace" },
	{ MONO_MT_TABLE_IDX,  "Extends=TypeDefOrRef" },
	{ MONO_MT_TABLE_IDX,  "FieldList:Field" },
	{ MONO_MT_TABLE_IDX,  "MethodList:Method" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeRefTable [] = {
	{ MONO_MT_TABLE_IDX,  "ResolutionScope=ResolutionScope" },
	{ MONO_MT_STRING_IDX, "Name" },
	{ MONO_MT_STRING_IDX, "Namespace" },
	{ MONO_MT_END, NULL }
};

static MonoMetaTable TypeSpecTable [] = {
	{ MONO_MT_BLOB_IDX,   "Signature" },
	{ MONO_MT_END, NULL }
};

static struct {
	MonoMetaTable *table;
	const char    *name;
} tables [] = {
	/*  0 */ { ModuleTable,               "Module" },
	/*  1 */ { TypeRefTable,              "TypeRef" },
	/*  2 */ { TypeDefTable,              "TypeDef" },
	/*  3 */ { NULL,                      NULL },
	/*  4 */ { FieldTable,                "Field" },
	/*  5 */ { NULL,                      NULL },
	/*  6 */ { MethodTable,               "Method" },
	/*  7 */ { NULL,                      NULL },
	/*  8 */ { ParamTable,                "Param" },
	/*  9 */ { InterfaceImplTable,        "InterfaceImpl" },
	/*  A */ { MemberRefTable,            "MemberRef" },
	/*  B */ { ConstantTable,             "Constant" },
	/*  C */ { CustomAttributeTable,      "CustomAttribute" },
	/*  D */ { FieldMarshalTable,         "FieldMarshal" },
	/*  E */ { DeclSecurityTable,         "DeclSecurity" },
	/*  F */ { ClassLayoutTable,          "ClassLayout" },
	/* 10 */ { FieldLayoutTable,          "FieldLayout" },
	/* 11 */ { StandaloneSigTable,        "StandaloneSig" },
	/* 12 */ { EventMapTable,             "EventMap" },
	/* 13 */ { NULL,                      NULL },
	/* 14 */ { EventTable,                "Event" },
	/* 15 */ { PropertyMapTable,          "PropertyMap" },
	/* 16 */ { NULL,                      NULL },
	/* 17 */ { PropertyTable,             "PropertyTable" },
	/* 18 */ { MethodSemanticsTable,      "MethodSemantics" },
	/* 19 */ { MethodImplTable,           "MethodImpl" },
	/* 1A */ { ModuleRefTable,            "ModuleRef" },
	/* 1B */ { TypeSpecTable,             "TypeSpec" },
	/* 1C */ { ImplMapTable,              "ImplMap" },
	/* 1D */ { FieldRVATable,             "FieldRVA" },
	/* 1E */ { NULL,                      NULL },
	/* 1F */ { NULL,                      NULL },
	/* 20 */ { AssemblyTable,             "Assembly" },
	/* 21 */ { AssemblyProcessorTable,    "AssemblyProcessor" },
	/* 22 */ { AssemblyOSTable,           "AssemblyOS" },
	/* 23 */ { AssemblyRefTable,          "AssemblyRef" },
	/* 24 */ { AssemblyRefProcessorTable, "AssemblyRefProcessor" },
	/* 25 */ { AssemblyRefOSTable,        "AssemblyRefOS" },
	/* 26 */ { FileTable,                 "File" },
	/* 27 */ { ExportedTypeTable,         "ExportedType" },
	/* 28 */ { ManifestResourceTable,     "ManifestResource" },
	/* 29 */ { NestedClassTable,          "NestedClass" },
	/* 2A */ { NULL,                      NULL },
	/* 2B */ { NULL,                      NULL },
};

const char *
mono_meta_table_name (int table)
{
	if ((table < 0) || (table > 0x29))
		return "";
	
	return tables [table].name;
}
