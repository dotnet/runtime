// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+--------------------------------------------------------------------------
//

//
//---------------------------------------------------------------------------
//

//


#ifndef _X509CERTIFICATE_H_
#define _X509CERTIFICATE_H_

#include "cryptography.h"

#define CERT_DELETE_KEYSET_PROP_ID 101 // This value shall be defined in wincrypt.h later to avoid conflicts.

#if defined(FEATURE_CORECLR)
#define X509_CERT_CONTENT_FLAGS CERT_QUERY_CONTENT_FLAG_CERT
#define X509_CERT_FORMAT_FLAGS  CERT_QUERY_FORMAT_FLAG_BINARY
#else // !FEATURE_CORECLR
#define X509_CERT_CONTENT_FLAGS (\
    CERT_QUERY_CONTENT_FLAG_CERT | CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT | \
    CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED | CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED | \
    CERT_QUERY_CONTENT_FLAG_PFX)
#define X509_CERT_FORMAT_FLAGS  CERT_QUERY_FORMAT_FLAG_ALL
#endif // (FEATURE_CORECLR) else

FORCEINLINE void VoidCertFreeCertificateContext(PCCERT_CONTEXT pCert) { LIMITED_METHOD_CONTRACT; CertFreeCertificateContext(pCert); }
FORCEINLINE void VoidCertCloseStore(HCERTSTORE hCertStore) { LIMITED_METHOD_CONTRACT; CertCloseStore(hCertStore, 0); }
FORCEINLINE void VoidCryptMsgClose(HCRYPTMSG hCryptMsg) { LIMITED_METHOD_CONTRACT; CryptMsgClose(hCryptMsg); }
#if !defined(FEATURE_CORECLR)
FORCEINLINE void VoidCertFreeCertificateChain(PCCERT_CHAIN_CONTEXT pChainContext) { LIMITED_METHOD_CONTRACT; CertFreeCertificateChain(pChainContext); }
#endif // !FEATURE_CORECLR

typedef Wrapper<PCCERT_CONTEXT, DoNothing<PCCERT_CONTEXT>, VoidCertFreeCertificateContext, 0> HandleCertContextHolder;
typedef Wrapper<HCERTSTORE, DoNothing<HCERTSTORE>, VoidCertCloseStore, 0> HandleCertStoreHolder;
typedef Wrapper<HCRYPTMSG, DoNothing<HCRYPTMSG>, VoidCryptMsgClose, 0> HandleCryptMsgHolder;
#if !defined(FEATURE_CORECLR)
typedef Wrapper<PCCERT_CHAIN_CONTEXT, DoNothing<PCCERT_CHAIN_CONTEXT>, VoidCertFreeCertificateChain, 0> HandleCertChainHolder;
#endif // !FEATURE_CORECLR

class X509Helper {
public:
#if !defined(FEATURE_CORECLR)
    static HRESULT BuildChain (PCCERT_CONTEXT pCertContext, HCERTSTORE hCertStore,
                    LPCSTR pszPolicy, PCCERT_CHAIN_CONTEXT * ppChainContext);
    static HRESULT DecodeObject(LPCSTR pszStructType, LPBYTE pbEncoded,
                    DWORD cbEncoded, void** ppvDecoded, DWORD* pcbDecoded);
    static BOOL DeleteKeyContainer (PCCERT_CONTEXT pCertContext);
#endif // !FEATURE_CORECLR
    static DWORD LoadFromBlob (CERT_BLOB* pCertBlob, __in_opt WCHAR* pwszPassword, DWORD dwFlags,
                    PCCERT_CONTEXT* pCertContext, HCERTSTORE* phCertStore, HCRYPTMSG* phCryptMsg);
    static DWORD LoadFromFile (__in_z WCHAR* pwszFileName, __in_opt WCHAR* pwszPassword, DWORD dwFlags,
                    PCCERT_CONTEXT* pCertContext, HCERTSTORE* phCertStore, HCRYPTMSG* phCryptMsg);
    static HRESULT ReadFileIntoMemory(LPCWSTR wszFileName, LPBYTE* ppbBuffer, DWORD* pdwBufLen);
};

class COMX509Certificate {
private:
#if !defined(FEATURE_CORECLR)
    static PCCERT_CONTEXT FilterPFXStore (CERT_BLOB* pfxBlob, __in_z WCHAR* pwszPassword, DWORD dwFlags);
#endif // !FEATURE_CORECLR
    static WCHAR* GetCertNameInfo(PCCERT_CONTEXT pCertContext, DWORD dwNameType, DWORD dwDisplayType, DWORD dwStrType);
#if !defined(FEATURE_CORECLR)
    static PCCERT_CONTEXT GetSignerInPKCS7Store (HCERTSTORE hCertStore, HCRYPTMSG hCryptMsg);
#endif // !FEATURE_CORECLR

public:
    static FCDECL2(void, DuplicateCertContext, INT_PTR handle, SafeHandle** ppCertUNSAFE);
    static FCDECL1(void, FreePCertContext, INT_PTR pCertCtx);
    static FCDECL1(U1Array*, GetCertRawData, SafeHandle* pCertUNSAFE);
    static FCDECL2(void, GetDateNotAfter, SafeHandle* pCertUNSAFE, FILETIME* pFileTime);
    static FCDECL2(void, GetDateNotBefore, SafeHandle* pCertUNSAFE, FILETIME* pFileTime);
    static FCDECL2(StringObject*, GetIssuerName, SafeHandle* pCertUNSAFE, CLR_BOOL fLegacyV1Mode);
    static FCDECL1(StringObject*, GetPublicKeyOid, SafeHandle* pCertUNSAFE);
    static FCDECL1(U1Array*, GetPublicKeyParameters, SafeHandle* pCertUNSAFE);
    static FCDECL1(U1Array*, GetPublicKeyValue, SafeHandle* pCertUNSAFE);
    static FCDECL1(U1Array*, GetSerialNumber, SafeHandle* pCertUNSAFE);
    static FCDECL3(StringObject*, GetSubjectInfo, SafeHandle* pCertUNSAFE, DWORD dwDisplayType, CLR_BOOL fLegacyV1Mode);
    static FCDECL1(U1Array*, GetThumbprint, SafeHandle* pCertUNSAFE);
    static FCDECL5(void, LoadCertFromBlob, U1Array* dataUNSAFE, __in_z WCHAR* pwszPassword, DWORD dwFlags, CLR_BOOL persistKeySet, SafeHandle** ppCertUNSAFE);
    static FCDECL5(void, LoadCertFromFile, StringObject* fileNameUNSAFE, __in_z WCHAR* pwszPassword, DWORD dwFlags, CLR_BOOL persistKeySet, SafeHandle** ppCertUNSAFE);
    static FCDECL1(DWORD, QueryCertBlobType, U1Array* dataUNSAFE);
    static FCDECL1(DWORD, QueryCertFileType, StringObject* fileNameUNSAFE);
};

#define X509_STORE_CONTENT_FLAGS (\
    CERT_QUERY_CONTENT_FLAG_CERT | CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT | \
    CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED | CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED | \
    CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED | CERT_QUERY_CONTENT_FLAG_PFX | \
    CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE)

#define NULL_ASN_TAG 0x05

// Keep in sync with System.Security.Cryptography.X509Certificates.X509ContentType
enum X509_ASSERTION_CONTENT_TYPE {
    UNKNOWN_TYPE                = 0x00,
    X509_CERT_TYPE              = 0x01,
    X509_SERIALIZED_CERT_TYPE   = 0x02,
    X509_PFX_TYPE               = 0x03,
    X509_SERIALIZED_STORE_TYPE  = 0x04,
    X509_PKCS7_TYPE             = 0x05,
    X509_AUTHENTICODE_TYPE      = 0x06
};

// We need to define this unmanaged memory structure to hold 
// all the information relevant to the cert context in order to guarantee
// critical finalization of the resources
typedef struct CERT_CTX {
public:
    PCCERT_CONTEXT m_pCtx;
#if !defined(FEATURE_CORECLR)
    BOOL           m_fDelKeyContainer;
#endif // !FEATURE_CORECLR

    CERT_CTX(PCCERT_CONTEXT pCertContext) : 
        m_pCtx(pCertContext)
#if !defined(FEATURE_CORECLR)
        ,m_fDelKeyContainer(FALSE)
#endif // !FEATURE_CORECLR
        {
        LIMITED_METHOD_CONTRACT;
    }

    // This method should not be called twice. 
    BOOL Release () {
        WRAPPER_NO_CONTRACT;
#if !defined(FEATURE_CORECLR)
        if (m_fDelKeyContainer)
            X509Helper::DeleteKeyContainer(m_pCtx);
#endif // !FEATURE_CORECLR

        // We need to free the cert context.
        if (m_pCtx != NULL) 
            if (!CertFreeCertificateContext(m_pCtx))
                return FALSE;

        m_pCtx = 0;
        delete this;
        return TRUE;
    }

} CERT_CTX;

#ifndef FEATURE_CORECLR
class COMX509Store {
public:
    static FCDECL2(void, AddCertificate, SafeHandle* hStoreUNSAFE, SafeHandle* pCertUNSAFE);
    static FCDECL3(U1Array*, ExportCertificatesToBlob, SafeHandle* hStoreUNSAFE, DWORD dwContentType, __in_z WCHAR* pwszPassword);
    static FCDECL1(void, FreeCertStoreContext, INT_PTR hCertStore);
    static FCDECL4(void, OpenX509Store, DWORD dwType, DWORD dwFlags, StringObject* storeNameUNSAFE, SafeHandle** phStoreUNSAFE);
    // RemoveCertificate is an FCALL that is not consumed by managed code
    // nor is it referred to by native code. This #ifdef makes it be dead code, which it
    // would otherwise not be and require CertFindCertificateInStore, which is otherwise
    // not required, and thus does not need to get implemented in the PAL.
    static FCDECL2(void, RemoveCertificate, SafeHandle* hStoreUNSAFE, SafeHandle* pCertUNSAFE);
};
#endif // !FEATURE_CORECLR

#endif // !_X509CERTIFICATE_H_
