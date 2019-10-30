// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __STRONGNAME_HOLDERS_H__
#define __STRONGNAME_HOLDERS_H__

#include <holder.h>
#include <strongname.h>
#include <wincrypt.h>

//
// Holder classes for types returned from and used in strong name APIs
//

// Holder for any memory allocated by the strong name APIs
template<class T>
void VoidStrongNameFreeBuffer(__in T *pBuffer)
{
    StrongNameFreeBuffer(reinterpret_cast<BYTE *>(pBuffer));
}
NEW_WRAPPER_TEMPLATE1(StrongNameBufferHolder, VoidStrongNameFreeBuffer<_TYPE>);

#if defined(CROSSGEN_COMPILE) && !defined(PLATFORM_UNIX)
// Holder for HCRYPTPROV handles directly allocated from CAPI
inline void ReleaseCapiProvider(HCRYPTPROV hProv)
{
    CryptReleaseContext(hProv, 0);
}
typedef Wrapper<HCRYPTPROV, DoNothing, ReleaseCapiProvider, 0> CapiProviderHolder;

inline void ReleaseCapiKey(HCRYPTKEY hKey)
{
    CryptDestroyKey(hKey);
}
typedef Wrapper<HCRYPTKEY, DoNothing, ReleaseCapiKey, 0> CapiKeyHolder;

inline void ReleaseCapiHash(HCRYPTHASH hHash)
{
    CryptDestroyHash(hHash);
}
typedef Wrapper<HCRYPTHASH, DoNothing, ReleaseCapiHash, 0> CapiHashHolder;
#endif // defined(CROSSGEN_COMPILE) && !defined(PLATFORM_UNIX)

#if SNAPI_INTERNAL

// Context structure tracking information for a loaded assembly.
struct SN_LOAD_CTX
{
    HANDLE              m_hFile;        // Open file handle
    HANDLE              m_hMap;         // Mapping file handle
    BYTE               *m_pbBase;       // Base address of mapped file
    DWORD               m_dwLength;     // Length of file in bytes
    IMAGE_NT_HEADERS32  *m_pNtHeaders;   // Address of NT headers
    IMAGE_COR20_HEADER *m_pCorHeader;   // Address of COM+ 2.0 header
    BYTE               *m_pbSignature;  // Address of signature blob
    DWORD               m_cbSignature;  // Size of signature blob
    BOOLEAN             m_fReadOnly;    // File mapped for read-only access
    BOOLEAN             m_fPreMapped;   // File was already mapped for us
    PEDecoder           *m_pedecoder;    // PEDecoder corresponding to this file
    SN_LOAD_CTX() { ZeroMemory(this, sizeof(*this)); }
};

BOOLEAN LoadAssembly(SN_LOAD_CTX *pLoadCtx, LPCWSTR szFilePath, DWORD inFlags = 0, BOOLEAN fRequireSignature = TRUE);
BOOLEAN UnloadAssembly(SN_LOAD_CTX *pLoadCtx);

// Holder for loading an assembly into an SN_LOAD_CTX
class StrongNameAssemblyLoadHolder
{
private:
    SN_LOAD_CTX m_snLoadCtx;
    bool m_fLoaded;

public:
    StrongNameAssemblyLoadHolder(LPCWSTR wszAssembly, bool fReadOnly)
    {
        m_snLoadCtx.m_fReadOnly = !!fReadOnly;
        m_fLoaded = !!LoadAssembly(&m_snLoadCtx, wszAssembly);
    }

    ~StrongNameAssemblyLoadHolder()
    {
        if (m_fLoaded)
        {
            UnloadAssembly(&m_snLoadCtx);
        }
    }

public:
    SN_LOAD_CTX *GetLoadContext()
    {
        return &m_snLoadCtx;
    }

    bool IsLoaded()
    {
        return m_fLoaded;
    }
};

#endif // SNAPI_INTERNAL

#endif // !__STRONGNAME_HOLDERS_H__
