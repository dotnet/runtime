//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 
// File: HotMetaData.h
// 

// 
// Class code:MetaData::HotMetaData represents a reader of hot data in MetaData hot stream.
// 
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// Forward declaration
class HotHeapsDirectoryIterator;

// --------------------------------------------------------------------------------------
// 
// This class represents a reader of hot data in MetaData hot stream.
// 
class HotMetaData
{
private:
    DataBuffer m_Data;
    
public:
    // Creates reader for MetaData hot stream of format file:HotDataFormat.h.
    __checkReturn 
    HRESULT Initialize(DataBuffer data);
    
    // Returns iterator of stored hot heaps (code:HotHeap).
    __checkReturn 
    HRESULT GetHeapsDirectoryIterator(HotHeapsDirectoryIterator *pHeapsDirectoryIterator);
    
};  // class HotMetaData

};  // namespace MetaData
