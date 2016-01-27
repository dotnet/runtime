// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#ifndef _CERTIFICATECACHE_H_
#define _CERTIFICATECACHE_H_

#include "corpermp.h"
#include "crst.h"

#define MAX_CACHED_CERTIFICATES 10

enum EnumCertificateAdditionFlags {
    Success         = 0,
    CacheSaturated  = 1,
    AlreadyExists   = 2
};

class CertificateCache {
public:
    EnumCertificateAdditionFlags AddEntry (COR_TRUST* pCertificate, DWORD* pIndex);
    COR_TRUST* GetEntry (DWORD index);
    BOOL Contains (COR_TRUST* pCertificate);

    CertificateCache ();
    ~CertificateCache ();

private:
    DWORD       m_dwNumEntries;
    COR_TRUST*  m_Entry [MAX_CACHED_CERTIFICATES];
    CrstStatic  m_CertificateCacheCrst;

    DWORD FindEntry (COR_TRUST* pCertificate);
};

#endif //_CERTIFICATECACHE_H_
