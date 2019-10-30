// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: HotHeapWriter.h
// 

// 
// Class code:HeapIndex represents type of MetaData heap (#String, #GUID, #Blob, or #US).
// 
// ======================================================================================

#pragma once

namespace MetaData
{

// --------------------------------------------------------------------------------------
// 
// This class represents type of MetaData heap (#String, #GUID, #Blob, or #US).
// 
class HeapIndex
{
private:
    UINT32 m_Index;
public:
    enum
    {
        StringHeapIndex     = 0, 
        GuidHeapIndex       = 1, 
        BlobHeapIndex       = 2, 
        UserStringHeapIndex = 3, 
        
        CountHeapIndex, 
        InvalidHeapIndex
    };
    HeapIndex()
    {
        m_Index = InvalidHeapIndex;
    }
    HeapIndex(UINT32 index)
    {
        _ASSERTE(IsValid(index));
        m_Index = index;
    }
    void Set(UINT32 index)
    {
        _ASSERTE(IsValid(index));
        m_Index = index;
    }
    void SetInvalid()
    {
        m_Index = InvalidHeapIndex;
    }
    BOOL IsValid() const
    {
        return m_Index < CountHeapIndex;
    }
    static BOOL IsValid(UINT32 index)
    {
        return index < CountHeapIndex;
    }
    UINT32 Get() const
        { return m_Index; }
    
};  // class HeapIndex

};  // namespace MetaData
