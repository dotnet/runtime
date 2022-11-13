// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include <windows.h>
#include <corhdr.h>
#include "corerror.h"
#include "pedecoder.h"

HRESULT CLiteWeightStgdbRW::FindImageMetaData(PVOID pImage, DWORD dwFileLength, BOOL bMappedImage, PVOID *ppMetaData, ULONG *pcbMetaData)
{
#ifndef DACCESS_COMPILE
    PEDecoder pe;

    // We need to use different PEDecoder initialization based on the type of data we give it.
    // We use the one with a 'bool' as the second argument when dealing with a mapped file,
    // and we use the one that takes a COUNT_T as the second argument when dealing with a
    // flat file.
    if (bMappedImage)
    {
        if (FAILED(pe.Init(pImage, false)) ||
            !pe.CheckNTHeaders())
        {
            return COR_E_BADIMAGEFORMAT;
        }
    }
    else
    {
        pe.Init(pImage, (COUNT_T)dwFileLength);
    }

    // Minimally validate image
    if (!pe.CheckCorHeader())
        return COR_E_BADIMAGEFORMAT;


    COUNT_T size = 0;

    *ppMetaData = (void *)pe.GetMetadata(&size);

    // Couldn't find any IL metadata in this image
    if (*ppMetaData == NULL)
        return CLDB_E_NO_DATA;

    if (pcbMetaData != NULL)
        *pcbMetaData = size;

    return S_OK;
#else
    DacNotImpl();
    return E_NOTIMPL;
#endif
} // CLiteWeightStgdbRW::FindImageMetaData
