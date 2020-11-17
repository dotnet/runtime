// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// This file provides an explicit check of field layouts using some macro magic
// in VerifyLayouts.h. The goal is that if any field changes offset or type changes
// size then a build time assert should trigger. DO NOT change these definitions without
// reading the comments in VerifyLayouts.h and understanding what other code you need
// to change at the same time.
//
//
// AN EXAMPLE:
// You want to validate the fields in type CDerived
//
// class CFoo
// {
//    void* m_ptrField;
// }
//
// struct BigStruct
// {
//    DWORD m_one;
//    DWORD m_two;
//    DWORD m_three;
// }
//
// CDerived : CFoo
// {
//    DWORD m_cRef;
//    SomeOtherType* m_otherType;
// #ifdef _SOME_DEFINE
//    BigStruct m_struct;
// #endif //_SOME_DEFINE
// }
//
//
// and the layout validation is written as:
//
// BEGIN_TYPE(CDerived, sizeof(CFoo))                             // a) The first field starts at sizeof(CFoo) to account for base class data
//                                                                // b) Beware of vtable pointers, they take up space before the data fields too
//                                                                // c) Beware of using sizeof(some_other_type) unless you also explicitly verify
//                                                                //    the layout of that type here. Changing the base type would change the derived types
//                                                                //    layout and won't be caught unless the base type is explicitly verified.
//                                                                // d) sizeof(<primitive_type>) is fine - they never change over time and we know how
//                                                                //    to deal with platform pointer size differences
// FIELD(CDerived, m_cRef, 4)
// FIELD(CDerived, m_otherType, sizeof(void*))
// #ifdef _SOME_DEFINE_
// ALIGN_FIELD(CDerived, m_struct, sizeof(BigStruct), 4)          // We need to use the ALIGN_FIELD macro because the alignment isn't the same as
//                                                                // the field size. The alignment of a structure is typically the max alignment
//                                                                // of any member. The alignment of a primitive type is typically the size of the type.
// #endif _SOME_DEFINE_
// END_TYPE(CDerived, sizeof(void*)
//
//
// BEGIN_TYPE(CFoo, 0)                                            // We must verify this type because we used it to define the starting offset of CDerived
// FIELD(CFoo, m_ptrField, sizeof(void*))
// END_TYPE(CFoo, sizeof(void*))
//
//
// BEGIN_TYPE(BigStruct, 0)                                       // We must verify this type because we used sizeof(BigStruct) to define the size of
//                                                                // CDerived::m_struct field
// FIELD(BigStruct, m_one, 4)
// FIELD(BigStruct, m_two, 4)
// FIELD(BigStruct, m_thee, 4)
// END_TYPE(BigStruct, 4)
//
//
//
//
//  OTHER CONSIDERATIONS:
//
// 1) if the type layout is conditional on a define, just include the same define here in the layout verification
//    Make sure that the define is registered in the list of defines and the debugger reading code knows how to dynamically
//    adjust for it (See VerifyLayouts.h comment item (b) and (c))
//
// 2) If your type names use characters that aren't valid identifiers (such as the '<', '>' and ',' chars in templates)
//    then you need to provide an escaped name. Use the USING_ALIAS macros to do that:
//    USING_ALIAS(escapedTypeName, typeName)
//
//    If CFoo above had instead been CFoo<ULONG> we would write:
//    USING_ALIAS(CFoo__ULONG__, CFoo<ULONG>)
//    BEGIN_TYPE(CFoo__ULONG__, 0)
//    FIELD(CFoo__ULONG__, m_ptrField, sizeof(void*))
//    END_TYPE(CFoo__ULONG__, sizeof(void*))
//
//    The escapedTypeName is relatively arbitrary, but convention is to replace the illegal characters with double underscore
//    The name does show up in build error messages, so it should close to the real name for people to understand
//
//  3) If you have a bitfield in your type, the offsetof macro can't be used which will break the static asserts.
//     There is a special BITFIELD macro that can work around it:
//     BITFIELD(typeName, fieldName, expectedFieldOffset, fieldSize, fieldAlign)
//
//     The macro is just like FIELD execpt you must provide the offset yourself. Since you can't use offsetof on the field directly
//     the convention is to use the offset of the previous field and then add the size and alignment requirements. For example if your
//     type had this:
//     CMiniMdRW
//     {
//         ULONG m_cbSaveSize;
//         int m_fIsReadOnly : 1;
//         int m_bPreSaveDone : 1;
//         int m_bSaveCompressed : 1;
//         int m_bPostGSSMod : 1;
//     }
//
//     You could write
//         FIELD(CMiniMdRW, m_cbSaveSize, 4)
//         BITFIELD(CMiniMdRW, m_fIsReadOnly, offsetof(CMiniMdRW, m_cbSaveSize)+4, 4)
//
//     Don't include al the fields in the bitfield, just pick one as the canonical field name
//
//
//
//    HOW DO I DEBUG THIS STUFF WHEN THE BUILD DOESN'T WORK?
//
//    One way that has been effective for me is to write a few static_assert_no_msg entries manually in VerifyLayouts.h
//    You can use those to verify your assumptions such as:
//    static_assert_no_msg(sizeof(Foo) == 24)
//    static_assert_no_msg(offset(Foo, m_lastField) == 20)
//    static_assert_no_msg(offset_of_field_affter_Foo_m_lastField == 24)
//    Then rebuild and find out where the compiler disagress with you.
//
//    Another option is to run the source through the preprocessor
//
//
//






BEGIN_TYPE(MDInternalRW, 2*sizeof(void*))
FIELD(MDInternalRW, m_pStgdb, sizeof(void*))
FIELD(MDInternalRW, m_tdModule, 4)
FIELD(MDInternalRW, m_cRefs, 4)
FIELD(MDInternalRW, m_fOwnStgdb, 4)
FIELD(MDInternalRW, m_pUnk, sizeof(void*))
FIELD(MDInternalRW, m_pUserUnk, sizeof(void*))
FIELD(MDInternalRW, m_pIMetaDataHelper, sizeof(void*))
FIELD(MDInternalRW, m_pSemReadWrite, sizeof(void*))
FIELD(MDInternalRW, m_fOwnSem, 4)
END_TYPE(MDInternalRW, sizeof(void*))

BEGIN_TYPE(CLiteWeightStgdbRW, sizeof(CLiteWeightStgdb<CMiniMdRW>))
FIELD(CLiteWeightStgdbRW, m_cbSaveSize, 4)
FIELD(CLiteWeightStgdbRW, m_bSaveCompressed, 4)
FIELD(CLiteWeightStgdbRW, m_pImage, sizeof(void*))
FIELD(CLiteWeightStgdbRW, m_dwImageSize, 4)
FIELD(CLiteWeightStgdbRW, m_dwPEKind, 4)
FIELD(CLiteWeightStgdbRW, m_dwMachine, 4)
FIELD(CLiteWeightStgdbRW, m_pStreamList, sizeof(void*))
FIELD(CLiteWeightStgdbRW, m_pNextStgdb, sizeof(void*))
FIELD(CLiteWeightStgdbRW, m_eFileType, 4)
FIELD(CLiteWeightStgdbRW, m_wszFileName, sizeof(void*))
FIELD(CLiteWeightStgdbRW, m_dwDatabaseLFT, 4)
FIELD(CLiteWeightStgdbRW, m_dwDatabaseLFS, 4)
FIELD(CLiteWeightStgdbRW, m_pStgIO, sizeof(void*))
END_TYPE(CLiteWeightStgdbRW, 8)

USING_ALIAS(CLiteWeightStgdb__CMiniMdRW__, CLiteWeightStgdb<CMiniMdRW>)
BEGIN_TYPE(CLiteWeightStgdb__CMiniMdRW__, 0)
ALIGN_FIELD(CLiteWeightStgdb__CMiniMdRW__, m_MiniMd, sizeof(CMiniMdRW), sizeof(void*))
FIELD(CLiteWeightStgdb__CMiniMdRW__, m_pvMd, sizeof(void*))
FIELD(CLiteWeightStgdb__CMiniMdRW__, m_cbMd, 4)
END_TYPE(CLiteWeightStgdb__CMiniMdRW__, sizeof(void*))

BEGIN_TYPE(CMiniMdRW, sizeof(CMiniMdTemplate<CMiniMdRW>))
FIELD(CMiniMdRW, m_pMemberRefHash, sizeof(void*))
FIELD(CMiniMdRW, m_pMemberDefHash, sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_pLookUpHashs, sizeof(void*)*TBL_COUNT, sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_StringPoolOffsetHash, sizeof(MapSHash<UINT32, UINT32>), 4)
FIELD(CMiniMdRW, m_pNamedItemHash, sizeof(void*))
FIELD(CMiniMdRW, m_maxRid, 4)
FIELD(CMiniMdRW, m_limRid, 4)
FIELD(CMiniMdRW, m_maxIx, 4)
FIELD(CMiniMdRW, m_limIx, 4)
FIELD(CMiniMdRW, m_eGrow, 4)
ALIGN_FIELD(CMiniMdRW, m_Tables, sizeof(RecordPool)*TBL_COUNT, sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_pVS, sizeof(void*)*TBL_COUNT, sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_StringHeap, sizeof(StgStringPool), sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_BlobHeap, sizeof(StgBlobPool), sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_UserStringHeap, sizeof(StgBlobPool), sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_GuidHeap, sizeof(StgGuidPool), sizeof(void*))
FIELD(CMiniMdRW, m_pHandler, sizeof(void*))
FIELD(CMiniMdRW, m_cbSaveSize, 4)
BITFIELD(CMiniMdRW, m_fIsReadOnly, offsetof(CMiniMdRW, m_cbSaveSize)+4, 4)
FIELD(CMiniMdRW, m_pMethodMap, sizeof(void*))
FIELD(CMiniMdRW, m_pFieldMap, sizeof(void*))
FIELD(CMiniMdRW, m_pPropertyMap, sizeof(void*))
FIELD(CMiniMdRW, m_pEventMap, sizeof(void*))
FIELD(CMiniMdRW, m_pParamMap, sizeof(void*))
FIELD(CMiniMdRW, m_pFilterTable, sizeof(void*))
FIELD(CMiniMdRW, m_pHostFilter, sizeof(void*))
FIELD(CMiniMdRW, m_pTokenRemapManager, sizeof(void*))
ALIGN_FIELD(CMiniMdRW, m_OptionValue, sizeof(OptionValue), 4)
ALIGN_FIELD(CMiniMdRW, m_StartupSchema, sizeof(CMiniMdSchema), 8)
ALIGN_FIELD(CMiniMdRW, m_bSortable, sizeof(BYTE)*TBL_COUNT, sizeof(BYTE))
#ifdef _DEBUG
FIELD(CMiniMdRW, dbg_m_pLock, sizeof(void*))
#endif
FIELD(CMiniMdRW, m_fMinimalDelta, 4)
FIELD(CMiniMdRW, m_rENCRecs, sizeof(void*))
END_TYPE(CMiniMdRW, 8)

BEGIN_TYPE(OptionValue, 0)
FIELD(OptionValue, m_DupCheck, 4)
FIELD(OptionValue, m_RefToDefCheck, 4)
FIELD(OptionValue, m_NotifyRemap, 4)
FIELD(OptionValue, m_UpdateMode, 4)
FIELD(OptionValue, m_ErrorIfEmitOutOfOrder, 4)
FIELD(OptionValue, m_ThreadSafetyOptions, 4)
FIELD(OptionValue, m_ImportOption, 4)
FIELD(OptionValue, m_LinkerOption, 4)
FIELD(OptionValue, m_GenerateTCEAdapters, 4)
FIELD(OptionValue, m_RuntimeVersion, sizeof(void*))
FIELD(OptionValue, m_MetadataVersion, 4)
FIELD(OptionValue, m_MergeOptions, 4)
FIELD(OptionValue, m_InitialSize, 4)
FIELD(OptionValue, m_LocalRefPreservation, 4)
END_TYPE(OptionValue, sizeof(void*))


BEGIN_TYPE(StgBlobPool, sizeof(StgPool))
ALIGN_FIELD(StgBlobPool, m_Hash, sizeof(CBlobPoolHash), sizeof(void*))
END_TYPE(StgBlobPool, sizeof(void*))

BEGIN_TYPE(StgStringPool, sizeof(StgPool))
ALIGN_FIELD(StgStringPool, m_Hash, sizeof(CStringPoolHash), sizeof(void*))
FIELD(StgStringPool, m_bHash, sizeof(BOOL))
END_TYPE(StgStringPool, sizeof(void*))

BEGIN_TYPE(StgGuidPool, sizeof(StgPool))
ALIGN_FIELD(StgGuidPool, m_Hash, sizeof(CGuidPoolHash), sizeof(void*))
FIELD(StgGuidPool, m_bHash, sizeof(BOOL))
END_TYPE(StgGuidPool, sizeof(void*))

BEGIN_TYPE(RecordPool, sizeof(StgPool))
FIELD(RecordPool, m_cbRec, 4)
END_TYPE(RecordPool, sizeof(void*))

BEGIN_TYPE(StgPool, sizeof(StgPoolReadOnly))
FIELD(StgPool, m_ulGrowInc, 4)
FIELD(StgPool, m_pCurSeg, sizeof(void*))
FIELD(StgPool, m_cbCurSegOffset, 4)
BITFIELD(StgPool, m_bFree, offsetof(StgPool, m_cbCurSegOffset)+4, 4) // can't take offsetof on a bitfield so we have to provide the offset another way
FIELD(StgPool, m_nVariableAlignmentMask, 4)
FIELD(StgPool, m_cbStartOffsetOfEdit, 4)
FIELD(StgPool, m_fValidOffsetOfEdit, 4)
END_TYPE(StgPool, sizeof(void*))


BEGIN_TYPE(CStringPoolHash, sizeof(CChainedHash<STRINGHASH>))
FIELD(CStringPoolHash, m_Pool, sizeof(void*))
END_TYPE(CStringPoolHash, sizeof(void*))


BEGIN_TYPE(CBlobPoolHash, sizeof(CChainedHash<STRINGHASH>))
FIELD(CBlobPoolHash, m_Pool, sizeof(void*))
END_TYPE(CStringPoolHash, sizeof(void*))


BEGIN_TYPE(CGuidPoolHash, sizeof(CChainedHash<GUIDHASH>))
FIELD(CGuidPoolHash, m_Pool, sizeof(void*))
END_TYPE(CGuidPoolHash, sizeof(void*))


USING_ALIAS(MetaData__HotHeap, MetaData::HotHeap)
BEGIN_TYPE(MetaData__HotHeap, 0)
FIELD(MetaData__HotHeap, m_pHotHeapHeader, sizeof(void*))
END_TYPE(MetaData__HotHeap, sizeof(void*))


BEGIN_TYPE(StgPoolReadOnly, sizeof(StgPoolSeg) + sizeof(void*))                   //vtable pointer
ALIGN_FIELD(StgPoolReadOnly, m_HotHeap, sizeof(MetaData::HotHeap), sizeof(void*))
END_TYPE(StgPoolReadOnly, sizeof(void*))

USING_ALIAS(MapSHash__ULONG__ULONG, MapSHash<ULONG, ULONG>) // Create a using alias to avoid commas in the type name
BEGIN_TYPE(MapSHash__ULONG__ULONG, 0)
FIELD(MapSHash__ULONG__ULONG, m_table, sizeof(void*))
FIELD(MapSHash__ULONG__ULONG, m_tableSize, 4)
FIELD(MapSHash__ULONG__ULONG, m_tableCount, 4)
FIELD(MapSHash__ULONG__ULONG, m_tableOccupied, 4)
FIELD(MapSHash__ULONG__ULONG, m_tableMax, 4)
END_TYPE(MapSHash__ULONG__ULONG, sizeof(void*))


BEGIN_TYPE(StgPoolSeg, 0)
FIELD(StgPoolSeg, m_pSegData, sizeof(void*))
FIELD(StgPoolSeg, m_pNextSeg, sizeof(void*))
FIELD(StgPoolSeg, m_cbSegSize, 4)
FIELD(StgPoolSeg, m_cbSegNext, 4)
END_TYPE(StgPoolSeg, sizeof(void*))

USING_ALIAS(CCHainedHash_STRINGHASH, CChainedHash<STRINGHASH>)
BEGIN_TYPE(CCHainedHash_STRINGHASH, sizeof(void*))         // vtable pointer
FIELD(CCHainedHash_STRINGHASH, m_rgData, sizeof(void*))
FIELD(CCHainedHash_STRINGHASH, m_iBuckets, 4)
FIELD(CCHainedHash_STRINGHASH, m_iSize, 4)
FIELD(CCHainedHash_STRINGHASH, m_iCount, 4)
FIELD(CCHainedHash_STRINGHASH, m_iMaxChain, 4)
FIELD(CCHainedHash_STRINGHASH, m_iFree, 4)
END_TYPE(CCHainedHash_STRINGHASH, sizeof(void*))


BEGIN_TYPE(CMiniColDef, 0)
FIELD(CMiniColDef, m_Type, 1)
FIELD(CMiniColDef, m_oColumn, 1)
FIELD(CMiniColDef, m_cbColumn, 1)
END_TYPE(CMiniColDef, 1)


BEGIN_TYPE(CMiniTableDef, 0)
FIELD(CMiniTableDef, m_pColDefs, sizeof(void*))
FIELD(CMiniTableDef, m_cCols, 1)
FIELD(CMiniTableDef, m_iKey, 1)
FIELD(CMiniTableDef, m_cbRec, 1)
END_TYPE(CMiniTableDef, sizeof(void*))

BEGIN_TYPE(CMiniMdBase, 8)                                           //vtable ptr and first field 8-byte alignment
ALIGN_FIELD(CMiniMdBase, m_Schema, sizeof(CMiniMdSchema), 8)
FIELD(CMiniMdBase, m_TblCount, 4)
FIELD(CMiniMdBase, m_fVerifiedByTrustedSource, 4)
ALIGN_FIELD(CMiniMdBase, m_TableDefs, sizeof(CMiniTableDef)*TBL_COUNT, sizeof(void*))
FIELD(CMiniMdBase, m_iStringsMask, 4)
FIELD(CMiniMdBase, m_iGuidsMask, 4)
FIELD(CMiniMdBase, m_iBlobsMask, 4)
END_TYPE(CMiniMdBase, 8)


BEGIN_TYPE(CMiniMdSchemaBase, 0)
FIELD(CMiniMdSchemaBase, m_ulReserved, 4)
FIELD(CMiniMdSchemaBase, m_major, 1)
FIELD(CMiniMdSchemaBase, m_minor, 1)
FIELD(CMiniMdSchemaBase, m_heaps, 1)
FIELD(CMiniMdSchemaBase, m_rid, 1)
FIELD(CMiniMdSchemaBase, m_maskvalid, 8)
FIELD(CMiniMdSchemaBase, m_sorted, 8)
END_TYPE(CMiniMdSchemaBase, 8)

BEGIN_TYPE(CMiniMdSchema, sizeof(CMiniMdSchemaBase))
ALIGN_FIELD(CMiniMdSchema, m_cRecs, 4*TBL_COUNT, 4)
FIELD(CMiniMdSchema, m_ulExtra, 4)
END_TYPE(CMiniMdSchema, 8)
