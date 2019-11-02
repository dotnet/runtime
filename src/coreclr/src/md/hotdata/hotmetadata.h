// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
