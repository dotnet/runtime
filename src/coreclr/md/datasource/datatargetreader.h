// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MD_DATA_TARGET_READER_
#define _MD_DATA_TARGET_READER_

#include "cor.h"
#include "cordebug.h"

class DataTargetReader;

class TargetObject
{
public:
    virtual HRESULT ReadFrom(DataTargetReader & reader) = 0;
};

class DataTargetReader
{
public:
    DataTargetReader(CORDB_ADDRESS remoteAddressCursor, ICorDebugDataTarget* pDataTarget, DWORD targetDefines, DWORD mdStructuresVersion);
    DataTargetReader(const DataTargetReader & otherReader);
    DataTargetReader & operator=(const DataTargetReader & rhs);
    ~DataTargetReader();

    HRESULT Read(TargetObject* pTargetObjectValue);
    HRESULT ReadPointer(TargetObject* pTargetObjectValue);
    HRESULT ReadPointer(CORDB_ADDRESS* pPointerValue);
    HRESULT SkipPointer();
    HRESULT Read8(BYTE* pByteValue);
    HRESULT Skip8();
    HRESULT Read32(ULONG32* pUlong32Value);
    HRESULT Skip32();
    HRESULT Read64(ULONG64* pUlong64Value);
    HRESULT Skip64();
    HRESULT ReadBytes(BYTE* pBuffer, DWORD cbBuffer);
    HRESULT SkipBytes(DWORD cbBuffer);
    void Align(DWORD alignmentBytes);
    void AlignBase();

    DataTargetReader CreateReaderAt(CORDB_ADDRESS remoteAddressCursor);

    DWORD GetMDStructuresVersion();
    BOOL IsDefined(DWORD define);


private:
    HRESULT GetRemotePointerSize(ULONG32* pPointerSize);
    ICorDebugDataTarget* m_pDataTarget;
    ULONG32 m_remotePointerSize;
    CORDB_ADDRESS m_remoteAddressCursor;
    ULONG32 m_currentStructureAlign;
    DWORD m_targetDefines;
    DWORD m_mdStructuresVersion;
};

#endif // _MD_DATA_TARGET_READER_
