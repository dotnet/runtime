// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef PORTABLE_PDB_H
#define PORTABLE_PDB_H

#include "ilasmpch.h"

class PortablePdbWritter
{
public:
    PortablePdbWritter();
    ~PortablePdbWritter();
    HRESULT             Init(IMetaDataEmit2* pdbEmitter);
    IMetaDataEmit2*     GetEmitter();
    GUID*               GetGuid();
    ULONG               GetTimestamp();

private:
    GUID                m_guid;
    ULONG               m_timestamp;
    IMetaDataEmit2*     m_pdbEmitter;
};

#endif
