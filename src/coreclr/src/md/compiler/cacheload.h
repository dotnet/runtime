// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
