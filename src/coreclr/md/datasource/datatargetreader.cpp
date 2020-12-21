// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "stdafx.h"
#include "datatargetreader.h"


DataTargetReader::DataTargetReader(CORDB_ADDRESS remoteAddressCursor, ICorDebugDataTarget* pDataTarget, DWORD targetDefines, DWORD mdStructuresVersion)
: m_remotePointerSize(0),
m_currentStructureAlign(1),
m_targetDefines(targetDefines),
m_mdStructuresVersion(mdStructuresVersion)
{
    m_remoteAddressCursor = remoteAddressCursor;
    m_pDataTarget = pDataTarget;
    m_pDataTarget->AddRef();
}
DataTargetReader::DataTargetReader(const DataTargetReader & otherReader)
{
    m_pDataTarget = otherReader.m_pDataTarget;
    m_pDataTarget->AddRef();
    m_remotePointerSize = otherReader.m_remotePointerSize;
    m_remoteAddressCursor = otherReader.m_remoteAddressCursor;
    m_targetDefines = otherReader.m_targetDefines;
    m_mdStructuresVersion = otherReader.m_mdStructuresVersion;
}
DataTargetReader & DataTargetReader::operator=(const DataTargetReader & otherReader)
{
    if (this != &otherReader)
    {
        m_pDataTarget = otherReader.m_pDataTarget;
        m_pDataTarget->AddRef();
        m_remotePointerSize = otherReader.m_remotePointerSize;
        m_remoteAddressCursor = otherReader.m_remoteAddressCursor;
        m_targetDefines = otherReader.m_targetDefines;
        m_mdStructuresVersion = otherReader.m_mdStructuresVersion;
    }
    return *this;
}
DataTargetReader::~DataTargetReader()
{
    m_pDataTarget->Release();
    m_pDataTarget = NULL;
}

HRESULT DataTargetReader::ReadPointer(CORDB_ADDRESS* pPointerValue)
{
    HRESULT hr = S_OK;
    if (m_remotePointerSize == 0)
    {
        IfFailRet(GetRemotePointerSize(&m_remotePointerSize));
    }
    _ASSERTE(m_remotePointerSize == 4 || m_remotePointerSize == 8);
    *pPointerValue = 0;
    if (m_remotePointerSize == 4)
        return Read32((ULONG32*)pPointerValue);
    else
        return Read64((ULONG64*)pPointerValue);
}
HRESULT DataTargetReader::SkipPointer()
{
    HRESULT hr = S_OK;
    if (m_remotePointerSize == 0)
    {
        IfFailRet(GetRemotePointerSize(&m_remotePointerSize));
    }
    _ASSERTE(m_remotePointerSize == 4 || m_remotePointerSize == 8);
    Align(m_remotePointerSize);
    return SkipBytes(m_remotePointerSize);
}
HRESULT DataTargetReader::Read8(BYTE* pByteValue)
{
    return ReadBytes(pByteValue, 1);
}
HRESULT DataTargetReader::Skip8()
{
    return SkipBytes(1);
}
HRESULT DataTargetReader::Read32(ULONG32* pUlong32Value)
{
    Align(4);
    return ReadBytes((BYTE*)pUlong32Value, sizeof(ULONG32));
}
HRESULT DataTargetReader::Skip32()
{
    Align(4);
    return SkipBytes(sizeof(ULONG32));
}
HRESULT DataTargetReader::Read64(ULONG64* pUlong64Value)
{
    Align(8);
    return ReadBytes((BYTE*)pUlong64Value, sizeof(ULONG64));
}
HRESULT DataTargetReader::Skip64()
{
    Align(8);
    return SkipBytes(sizeof(ULONG64));
}

HRESULT DataTargetReader::ReadBytes(BYTE* pBuffer, DWORD cbBuffer)
{
    HRESULT hr = S_OK;
    ULONG32 cbTotalRead = 0;
    CORDB_ADDRESS m_tempRemoteAddressCursor = m_remoteAddressCursor;
    while (cbTotalRead < cbBuffer)
    {
        ULONG32 cbRead = 0;
        if(FAILED(m_pDataTarget->ReadVirtual(m_remoteAddressCursor + cbTotalRead,
            pBuffer + cbTotalRead,
            cbBuffer - cbTotalRead,
            &cbRead)))
            return CORDBG_E_READVIRTUAL_FAILURE;
        if (cbRead == 0)
            return CORDBG_E_READVIRTUAL_FAILURE;
        cbTotalRead += cbRead;
    }

    // on success only, move the cursor
    m_remoteAddressCursor += cbTotalRead;
    return S_OK;
}
HRESULT DataTargetReader::SkipBytes(DWORD cbRead)
{
    m_remoteAddressCursor += cbRead;
    return S_OK;
}

#ifndef MAX
#define MAX(a,b) ((a)>(b) ? (a) : (b))
#endif

HRESULT DataTargetReader::Read(TargetObject* pTargetObject)
{
    ULONG32 previousAlign = m_currentStructureAlign;
    m_currentStructureAlign = 1;
    HRESULT hr = pTargetObject->ReadFrom(*this);
    if (SUCCEEDED(hr))
    {
        // increase the structure size to a multiple of the maximum alignment of any of its members
        Align(m_currentStructureAlign);
    }
    m_currentStructureAlign = MAX(previousAlign, m_currentStructureAlign);
    return hr;
}

HRESULT DataTargetReader::ReadPointer(TargetObject* pTargetObject)
{
    HRESULT hr = S_OK;
    CORDB_ADDRESS pointerValue;
    IfFailRet(ReadPointer(&pointerValue));

    DataTargetReader reader = CreateReaderAt(pointerValue);
    return pTargetObject->ReadFrom(reader);
}

void DataTargetReader::Align(DWORD alignmentBytes)
{
    m_remoteAddressCursor = AlignUp(m_remoteAddressCursor, alignmentBytes);
    m_currentStructureAlign = MAX(m_currentStructureAlign, alignmentBytes);
}

void DataTargetReader::AlignBase()
{
    // Align structs based on the largest field size
    // This is the default for MSVC compilers
    // This is forced on other platforms by the DAC_ALIGNAS macro
    Align(m_currentStructureAlign);
}

HRESULT DataTargetReader::GetRemotePointerSize(ULONG32* pPointerSize)
{
    HRESULT hr = S_OK;
    CorDebugPlatform platform;
    IfFailRet(m_pDataTarget->GetPlatform(&platform));
    if ((platform == CORDB_PLATFORM_WINDOWS_X86) || (platform == CORDB_PLATFORM_POSIX_X86) || (platform == CORDB_PLATFORM_MAC_X86))
        *pPointerSize = 4;
    else if ((platform == CORDB_PLATFORM_WINDOWS_AMD64) || (platform == CORDB_PLATFORM_POSIX_AMD64) || (platform == CORDB_PLATFORM_MAC_AMD64))
        *pPointerSize = 8;
    else if ((platform == CORDB_PLATFORM_WINDOWS_ARM) || (platform == CORDB_PLATFORM_POSIX_ARM))
        *pPointerSize = 4;
    else if ((platform == CORDB_PLATFORM_WINDOWS_ARM64) || (platform == CORDB_PLATFORM_POSIX_ARM64))
        *pPointerSize = 8;
    else
        return CORDBG_E_UNSUPPORTED;
    return S_OK;
}

DWORD DataTargetReader::GetMDStructuresVersion()
{
    return m_mdStructuresVersion;
}

BOOL DataTargetReader::IsDefined(DWORD define)
{
    return (m_targetDefines & define) == define;
}

DataTargetReader DataTargetReader::CreateReaderAt(CORDB_ADDRESS remoteAddressCursor)
{
    DataTargetReader newReader(remoteAddressCursor, m_pDataTarget, m_targetDefines, m_mdStructuresVersion);
    return newReader;
}
