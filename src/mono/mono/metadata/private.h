#ifndef __MONO_METADATA_PRIVATE_H__
#define __MONO_METADATA_PRIVATE_H__

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

typedef struct {
	int   code;
	const char *def;
} MonoMetaTable;

const char *mono_meta_table_name (int table);

/* Internal functions */
void           mono_metadata_compute_table_bases (MonoImage *meta);

const MonoMetaTable *mono_metadata_get_table    (MonoMetaTableEnum table);

#endif /* __MONO_METADATA_PRIVATE_H__ */

