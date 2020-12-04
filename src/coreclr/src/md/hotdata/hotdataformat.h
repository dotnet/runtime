// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: HotDataFormat.h
//

//
// Format of the hot data stored in the hot stream. The format consists of several structures:
//  * code:MetaData::HotMetaDataHeader, which contains reference to:
//    * code:MetaData::HotTablesDirectory, which contains array of references to:
//      * code:HotTableHeader
//    * code:MetaData::HotHeapsDirectory, which contains array of code:MetaData::HotHeapsDirectoryEntry,
//      each containig:
//      * index of the heap code:HeapIndex and a reference to:
//      * code:MetaData::HotHeapHeader, which contains reference to:
//        * index table, which contains sorted array of represented hot indexes in the heap
//        * value offsets table, which contains offsets of values for corresponding hot indexes in
//          previous table
//        * value heap, which contains the values (copied out) from the original cold heap
//
// ======================================================================================

#pragma once

#include "external.h"

// To avoid weird .h cycles, we have to include stgpool.h to get include of metamodelpub.h
#include <stgpool.h>
#include <metamodelpub.h>

namespace MetaData
{

// #HotMetaData
// To help with startup time, we create a section of metadata that is only that meta-data that was touched
// durring IBC profiling.  Given an offset into a pool this checks if we have any hot data associated with
// it.  If we do we return a pointer to it, otherwse we return NULL.

#include <pshpack1.h>

// --------------------------------------------------------------------------------------
//
// Top level hot data header.
// Ends at the end of MetaData hot stream (i.e. starts at offset end-8).
//
struct HotMetaDataHeader
{
    // Negative offset relative to the beginning of this structure.
    // Points to the END (!!!) of code:HotTablesDirectory structure.
    UINT32 m_nTablesDirectoryEnd_NegativeOffset;
    // Negative offset relative to the beginning of this structure.
    // Points to the (start of) code:HotHeapsDirectory structure.
    UINT32 m_nHeapsDirectoryStart_NegativeOffset;

};  // struct HotMetaDataHeader

// --------------------------------------------------------------------------------------
//
// This is the starting structure for hot data of tables.
// It's referenced (via reference to the end) from
// code:HotMetaDataHeader::m_nTablesDirectoryEnd_NegativeOffset.
//
struct HotTablesDirectory
{
    // Magic number (code:#const_nMagic) for format verification.
    UINT32 m_nMagic;
    // Array of signed offsets (should have negative or 0 value) relative to the beginning of this structute
    // for each MetaData table.
    // Points to the (start of) code:HotTableHeader structure.
    INT32 m_rgTableHeader_SignedOffset[TBL_COUNT];

    //#const_nMagic
    // Magic value "HOMD" in code:m_nMagic field.
    static const UINT32 const_nMagic = 0x484f4e44;

};  // struct HotTablesDirectory

// --------------------------------------------------------------------------------------
//
// Describes hot data in a table.
// Entry referenced (via reference to the start) from code:HotTablesDirectory::m_rgTableHeader_SignedOffset.
//
struct HotTableHeader
{
    UINT32 m_cTableRecordCount;
    // Can be 0 or sizeof(struct HotTableHeader)
    UINT32 m_nFirstLevelTable_PositiveOffset;
    // Can be 0
    UINT32 m_nSecondLevelTable_PositiveOffset;
    UINT32 m_offsIndexMappingTable;
    UINT32 m_offsHotData;
    UINT16 m_shiftCount;

};  // struct HotTableHeader

// --------------------------------------------------------------------------------------
//
// This is the starting structure for hot data of heaps (string, blob, guid and user string heap).
// The directory is an array of code:HotHeapsDirectoryEntry structures.
// It's referenced from code:HotMetaDataHeader::m_nHeapsDirectoryStart_NegativeOffset.
//
struct HotHeapsDirectory
{
    //code:HotHeapsDirectoryEntry m_rgEntries[*];

};  // struct HotHeapsDirectory

// --------------------------------------------------------------------------------------
//
// Describes one heap and its hot data.
// Entry in the hot heaps directory (code:HotHeapsDirectory).
//
struct HotHeapsDirectoryEntry
{
    // Index of the represented heap code:HeapIndex.
    UINT32 m_nHeapIndex;
    // Negative offset relative to the beginning of this structure.
    // Points to the (start of) code:HotHeapHeader structure.
    UINT32 m_nHeapHeaderStart_NegativeOffset;

};  // struct HotHeapsDirectoryEntry

// --------------------------------------------------------------------------------------
//
// Describes hot data in a heap.
// It's referenced from code:HotHeapsDirectoryEntry::m_nHeapHeaderStart_NegativeOffset.
//
struct HotHeapHeader
{
    // Negative offset relative to the beginning of this structure.
    // Points to a (start of) table of indexes (UINT32). This table is sorted, so binary search can be
    // performed. If an index is cached in hot data of this heap, then the index is present in this table
    // of indexes.
    UINT32 m_nIndexTableStart_NegativeOffset;
    // Negative offset relative to the beginning of this structure.
    // Points to a (start of) table of value offsets (UINT32). This table contains value for each iteam in
    // previous table of indexes. When an index is found in the previous table, then the value offset is
    // stored in this table at the same index.
    // The value offset is positive (!!!) offset relative to the start of heap values (see next member -
    // code:m_nValueHeapStart_NegativeOffset)
    UINT32 m_nValueOffsetTableStart_NegativeOffset;
    // Negative offset relative to the beginning of this structure.
    // Points to a (start of) values in the hot heap. This heap contains copies of values from the "normal"
    // (cold) heap. The values in this heap have therefore the same encoding as the values in corresponding
    // normal/cold heap.
    // Offsets into this heap are stored in value offset table (code:m_nValueOffsetTableStart_NegativeOffset)
    // as positive (!!!) offsets relative to the start of this hot value heap.
    UINT32 m_nValueHeapStart_NegativeOffset;

};  // struct HotHeapHeader

#include <poppack.h>

};  // namespace MetaData
