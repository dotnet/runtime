// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgPool.cpp
//

//
// Pools are used to reduce the amount of data actually required in the database.
// This allows for duplicate string and binary values to be folded into one
// copy shared by the rest of the database.  Strings are tracked in a hash
// table when insert/changing data to find duplicates quickly.  The strings
// are then persisted consecutively in a stream in the database format.
//
//*****************************************************************************
#include "stdafx.h"						// Standard include.
#include <stgpool.h>					// Our interface definitions.

int CStringPoolHash::Cmp(
	const void	*pData, 				// A string.
	void		*pItem)					// A hash item which refers to a string.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    LPCSTR p1 = reinterpret_cast<LPCSTR>(pData);
    LPCSTR p2;
    if (FAILED(m_Pool->GetString(reinterpret_cast<STRINGHASH*>(pItem)->iOffset, &p2)))
    {
        return -1;
    }
    return (strcmp(p1, p2));
} // int CStringPoolHash::Cmp()


int CBlobPoolHash::Cmp(
    const void *pData,					// A blob.
    void        *pItem)					// A hash item which refers to a blob.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    ULONG ul1;
    ULONG ul2;
    MetaData::DataBlob data2;

    // Get size of first item.
    ul1 = CPackedLen::GetLength(pData);
    // Adjust size to include the length of size field.
    ul1 += CPackedLen::Size(ul1);

    // Get the second item.
    if (FAILED(m_Pool->GetData(reinterpret_cast<BLOBHASH*>(pItem)->iOffset, &data2)))
    {
        return -1;
    }

    // Get and adjust size of second item.
    ul2 = CPackedLen::GetLength(data2.GetDataPointer());
    ul2 += CPackedLen::Size(ul2);

    if (ul1 < ul2)
        return (-1);
    else if (ul1 > ul2)
        return (1);
    return (memcmp(pData, data2.GetDataPointer(), ul1));
} // int CBlobPoolHash::Cmp()

int CGuidPoolHash::Cmp(const void *pData, void *pItem)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    GUID *p2;
    if (FAILED(m_Pool->GetGuid(reinterpret_cast<GUIDHASH*>(pItem)->iIndex, &p2)))
    {
        return -1;
    }
    return (memcmp(pData, p2, sizeof(GUID)));
} // int CGuidPoolHash::Cmp()

//
//
// CPackedLen
//
//


//*****************************************************************************
// Parse a length, return the data, store length.
//*****************************************************************************
void const *CPackedLen::GetData(		// Pointer to data, or 0 on error.
	void const	*pData, 				// First byte of length.
	ULONG		*pLength)				// Put length here, or -1 on error.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

	BYTE const	*pBytes = reinterpret_cast<BYTE const*>(pData);

	if ((*pBytes & 0x80) == 0x00)		// 0??? ????
	{
		*pLength = (*pBytes & 0x7f);
		return pBytes + 1;
	}

	if ((*pBytes & 0xC0) == 0x80)		// 10?? ????
	{
		*pLength = ((*pBytes & 0x3f) << 8 | *(pBytes+1));
		return pBytes + 2;
	}

	if ((*pBytes & 0xE0) == 0xC0)		// 110? ????
	{
		*pLength = ((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3));
		return pBytes + 4;
	}

	*pLength = (ULONG) -1;
	return 0;
} // void const *CPackedLen::GetData()

#ifndef MAX_PTR
#define MAX_PTR ((BYTE*)(~(SSIZE_T)0))
#endif

//*****************************************************************************
// Parse a length, return the data, store length.
//*****************************************************************************
HRESULT CPackedLen::SafeGetLength(  // S_OK, or error
    void const  *pDataSource,       // First byte of length.
    void const  *pDataSourceEnd,    // End of valid source data memory
    ULONG       *pLength,           // Length of data, if return S_OK
    void const **ppDataNext)        // Pointer immediately following encoded length
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    if (pDataSource == NULL ||
        pDataSourceEnd == NULL ||
        pDataSourceEnd < pDataSource ||
        ppDataNext == NULL ||
        pLength == NULL ||
        pDataSource > (MAX_PTR - 4))
    {
        return E_INVALIDARG;
    }

    BYTE const  *pBytes = reinterpret_cast<BYTE const*>(pDataSource);
    BYTE const  *pBytesEnd = reinterpret_cast<BYTE const*>(pDataSourceEnd);

    size_t cbAvail = pBytesEnd - pBytes;

    if (cbAvail < 1)
    {   // Fail if no source data available
        return COR_E_OVERFLOW;
    }

    if ((*pBytes & 0x80) == 0x00)       // 0??? ????
    {
        *pLength = (*pBytes & 0x7f);
        *ppDataNext = pBytes + 1;
        return S_OK;
    }

    if (cbAvail < 2)
    {   // Fail if not enough source data available
        return COR_E_OVERFLOW;
    }

    if ((*pBytes & 0xC0) == 0x80)       // 10?? ????
    {
        *pLength = ((*pBytes & 0x3f) << 8 | *(pBytes+1));
        *ppDataNext = pBytes + 2;
        return S_OK;
    }

    if (cbAvail < 4)
    {   // Fail if not enough source data available
        return COR_E_OVERFLOW;
    }

    if ((*pBytes & 0xE0) == 0xC0)       // 110? ????
    {
        *pLength = ((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3));
        *ppDataNext = pBytes + 4;
        return S_OK;;
    }

    return COR_E_OVERFLOW;
} // CPackedLen::GetLength

//*****************************************************************************
// Parse a length, return the data, store length.
//*****************************************************************************
HRESULT CPackedLen::SafeGetData(    // S_OK, or error
    void const  *pDataSource,       // First byte of length.
    void const  *pDataSourceEnd,    // End of valid source data memory
    ULONG       *pcbData,           // Length of data
    void const **ppData)            // Start of data
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    HRESULT hr = S_OK;

    IfFailRet(SafeGetLength(pDataSource, pDataSourceEnd, pcbData, ppData));

    if (*pcbData == 0)
    {   // Zero length value means zero data, so no range checking required.
        return S_OK;
    }

    BYTE const *pbData = reinterpret_cast<BYTE const*>(*ppData);

    if (pbData + *pcbData < pbData)
    {   // First check for integer overflow
        return COR_E_OVERFLOW;
    }

    if (pDataSourceEnd < pbData + *pcbData)
    {   // Now check for data buffer overflow
        return COR_E_OVERFLOW;
    }

    return S_OK;
} // CPackedLen::GetLength

//*****************************************************************************
// Parse a length, return the data, store length.
//*****************************************************************************
HRESULT CPackedLen::SafeGetData(    // S_OK, or error
    void const  *pDataSource,       // First byte of data
    ULONG        cbDataSource,      // Count of valid bytes in data source
    ULONG       *pcbData,           // Length of data
    void const **ppData)            // Start of data
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    return SafeGetData(pDataSource, (void const *)((BYTE const *)pDataSource + cbDataSource), pcbData, ppData);
} // CPackedLen::GetLength

//*****************************************************************************
// Parse a length, return the length, pointer to actual bytes.
//*****************************************************************************
ULONG CPackedLen::GetLength(			// Length or -1 on error.
	void const	*pData, 				// First byte of length.
	void const	**ppCode)				// Put pointer to bytes here, if not 0.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

	BYTE const	*pBytes = reinterpret_cast<BYTE const*>(pData);

	if ((*pBytes & 0x80) == 0x00)		// 0??? ????
	{
		if (ppCode) *ppCode = pBytes + 1;
		return (*pBytes & 0x7f);
	}

	if ((*pBytes & 0xC0) == 0x80)		// 10?? ????
	{
		if (ppCode) *ppCode = pBytes + 2;
		return ((*pBytes & 0x3f) << 8 | *(pBytes+1));
	}

	if ((*pBytes & 0xE0) == 0xC0)		// 110? ????
	{
		if (ppCode) *ppCode = pBytes + 4;
		return ((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3));
	}

	return (ULONG) -1;
} // ULONG CPackedLen::GetLength()

//*****************************************************************************
// Parse a length, return the length, size of the length.
//*****************************************************************************
ULONG CPackedLen::GetLength(			// Length or -1 on error.
	void const	*pData, 				// First byte of length.
	int			*pSizeLen)				// Put size of length here, if not 0.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

	BYTE const	*pBytes = reinterpret_cast<BYTE const*>(pData);

	if ((*pBytes & 0x80) == 0x00)		// 0??? ????
	{
		if (pSizeLen) *pSizeLen = 1;
		return (*pBytes & 0x7f);
	}

	if ((*pBytes & 0xC0) == 0x80)		// 10?? ????
	{
		if (pSizeLen) *pSizeLen = 2;
		return ((*pBytes & 0x3f) << 8 | *(pBytes+1));
	}

	if ((*pBytes & 0xE0) == 0xC0)		// 110? ????
	{
		if (pSizeLen) *pSizeLen = 4;
		return ((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3));
	}

	return (ULONG) -1;
} // ULONG CPackedLen::GetLength()

//*****************************************************************************
// Encode a length.
//*****************************************************************************
void* CPackedLen::PutLength(			// First byte past length.
	void		*pData, 				// Pack the length here.
	ULONG		iLen)					// The length.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

	BYTE		*pBytes = reinterpret_cast<BYTE*>(pData);

	if (iLen <= 0x7F)
	{
		*pBytes = (BYTE)iLen;
		return pBytes + 1;
	}

	if (iLen <= 0x3FFF)
	{
		*pBytes = (BYTE)((iLen >> 8) | 0x80);
		*(pBytes+1) = iLen & 0xFF;
		return pBytes + 2;
	}

	_ASSERTE(iLen <= 0x1FFFFFFF);
	*pBytes = (iLen >> 24) | 0xC0;
	*(pBytes+1) = (iLen >> 16) & 0xFF;
	*(pBytes+2) = (iLen >> 8)  & 0xFF;
	*(pBytes+3) = iLen & 0xFF;
	return pBytes + 4;
} // void* CPackedLen::PutLength()
