//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// CacheLoad.h
// 

//
// Class for returning the memory image where the image lives
//
//*****************************************************************************
#ifndef __CACHELOAD__H__
#define __CACHELOAD__H__


#undef  INTERFACE   
#define INTERFACE ICacheLoad
DECLARE_INTERFACE_(ICacheLoad, IUnknown)
{
    STDMETHOD(GetCachedImaged)(
       LPVOID* pImage);

    STDMETHOD(SetCachedImaged)(
       LPVOID pImage);
};

#endif
