// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Included repeatedly with different CDAC_* macro definitions.

CDAC_BASELINE("empty")

CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(DNMDData)
CDAC_TYPE_SIZE(sizeof(mdcdata_t))
CDAC_TYPE_FIELD(DNMDData, T_POINTER, Ptr, offsetof(mdcdata_t, ptr))
CDAC_TYPE_FIELD(DNMDData, T_NUINT, Size, offsetof(mdcdata_t, size))
CDAC_TYPE_END(DNMDData)

CDAC_TYPE_BEGIN(DNMDContext)
CDAC_TYPE_SIZE(sizeof(mdcxt_t))
CDAC_TYPE_FIELD(DNMDContext, T_UINT32, Magic, offsetof(mdcxt_t, magic))
CDAC_TYPE_FIELD(DNMDContext, T_UINT32, Flags, offsetof(mdcxt_t, context_flags))
CDAC_TYPE_FIELD(DNMDContext, T_POINTER, Version, offsetof(mdcxt_t, version))
CDAC_TYPE_FIELD(DNMDContext, TYPE(DNMDData), StringsHeap, offsetof(mdcxt_t, strings_heap))
CDAC_TYPE_FIELD(DNMDContext, TYPE(DNMDData), GuidHeap, offsetof(mdcxt_t, guid_heap))
CDAC_TYPE_FIELD(DNMDContext, TYPE(DNMDData), BlobHeap, offsetof(mdcxt_t, blob_heap))
CDAC_TYPE_FIELD(DNMDContext, TYPE(DNMDData), UserStringHeap, offsetof(mdcxt_t, user_string_heap))
CDAC_TYPE_FIELD(DNMDContext, T_POINTER, Tables, offsetof(mdcxt_t, tables))
CDAC_TYPE_END(DNMDContext)

CDAC_TYPE_BEGIN(DNMDTable)
CDAC_TYPE_SIZE(sizeof(mdtable_t))
CDAC_TYPE_FIELD(DNMDTable, TYPE(DNMDData), Data, offsetof(mdtable_t, data))
CDAC_TYPE_FIELD(DNMDTable, T_UINT32, RowCount, offsetof(mdtable_t, row_count))
CDAC_TYPE_FIELD(DNMDTable, T_UINT8, RowSize, offsetof(mdtable_t, row_size_bytes))
CDAC_TYPE_FIELD(DNMDTable, T_UINT8, Sorted, offsetof(mdtable_t, is_sorted))
CDAC_TYPE_FIELD(DNMDTable, T_UINT8, AddingNewRow, offsetof(mdtable_t, is_adding_new_row))
CDAC_TYPE_FIELD(DNMDTable, T_UINT8, TableId, offsetof(mdtable_t, table_id))
CDAC_TYPE_FIELD(DNMDTable, T_POINTER, Context, offsetof(mdtable_t, cxt))
CDAC_TYPE_END(DNMDTable)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()
CDAC_GLOBAL(DNMDContextMagic, T_UINT32, MDLIB_MAGIC_NUMBER)
CDAC_GLOBAL(DNMDTableCount, T_UINT32, (uint32_t)MDTABLE_MAX_COUNT)
CDAC_GLOBAL(DNMDLargeStringHeap, T_UINT32, mdc_large_string_heap)
CDAC_GLOBAL(DNMDLargeGuidHeap, T_UINT32, mdc_large_guid_heap)
CDAC_GLOBAL(DNMDLargeBlobHeap, T_UINT32, mdc_large_blob_heap)
CDAC_GLOBAL(DNMDMinimalDelta, T_UINT32, mdc_minimal_delta)
CDAC_GLOBAL(DNMDUncompressedTables, T_UINT32, mdc_uncompressed_table_heap)
CDAC_GLOBAL_CONTRACT(EcmaMetadata, c2)
CDAC_GLOBALS_END()
