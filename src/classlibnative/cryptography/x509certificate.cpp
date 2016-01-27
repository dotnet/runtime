// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
//  File: X509Certificate.cpp
//  

// 
//  Native method implementations and helper code for supporting CAPI based operations on X509 signatures
//
//---------------------------------------------------------------------------


#include "common.h"

#ifdef FEATURE_X509

#include "x509certificate.h"

#if !defined(FEATURE_CORECLR)
//
// Builds a certificate chain using the specified policy.
//

HRESULT X509Helper::BuildChain (PCCERT_CONTEXT pCertContext,
                                HCERTSTORE hCertStore, 
                                LPCSTR pszPolicy,
                                PCCERT_CHAIN_CONTEXT * ppChainContext) 
{
    CONTRACTL {
        THROWS;         // THROWS because the delay-loading of crypt32.dll may fail when we call CertGetCertificateChain()
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    CERT_CHAIN_PARA ChainPara = {0};
    LPSTR rgpszUsageIdentifier[1] = {NULL};

    // Initialize the structure size.
    ChainPara.cbSize = sizeof(ChainPara);

    // Check policy.
    if (CERT_CHAIN_POLICY_BASE == pszPolicy) {
        // No EKU for base policy.
    }
    else 
        return CERT_E_INVALID_POLICY;

    // Build the chain.
    if (!CertGetCertificateChain(NULL,
                                 pCertContext,
                                 NULL,
                                 hCertStore,
                                 &ChainPara,
                                 0,
                                 NULL,
                                 ppChainContext)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetCertificateChain failed.\n", HRESULT_FROM_GetLastError()));
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}

// 
// decodes an ASN encoded data. The caller is responsible for calling delete[] to free the allocated memory.
//

HRESULT X509Helper::DecodeObject(LPCSTR pszStructType,
                                 LPBYTE pbEncoded,
                                 DWORD cbEncoded,
                                 void** ppvDecoded,
                                 DWORD* pcbDecoded) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    DWORD cbDecoded = 0;
    if (!CryptDecodeObject(X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                        pszStructType,
                        pbEncoded,
                        cbEncoded,
                        0,
                        NULL,
                        &cbDecoded)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptDecodeObject failed.\n", HRESULT_FROM_GetLastError()));
        return HRESULT_FROM_GetLastError();
    }

    *ppvDecoded = (void*) new BYTE[cbDecoded];
    if (!CryptDecodeObject(X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                        pszStructType,
                        pbEncoded,
                        cbEncoded,
                        0,
                        *ppvDecoded,
                        &cbDecoded)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptDecodeObject failed.\n", HRESULT_FROM_GetLastError()));
        return HRESULT_FROM_GetLastError();
    }

    if (pcbDecoded)
        *pcbDecoded = cbDecoded;

    return S_OK;
}

//
// Deletes a key container given a certificate context.
//

BOOL X509Helper::DeleteKeyContainer (PCCERT_CONTEXT pCertContext) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    NewHolder<CRYPT_KEY_PROV_INFO> pProvInfo;
    DWORD cbData = 0;
    if (!CertGetCertificateContextProperty(pCertContext,
                                           CERT_KEY_PROV_INFO_PROP_ID,
                                           NULL,
                                           &cbData))
        return TRUE;

    pProvInfo = (CRYPT_KEY_PROV_INFO*) new BYTE[cbData];
    if (!CertGetCertificateContextProperty(pCertContext,
                                           CERT_KEY_PROV_INFO_PROP_ID,
                                           (void*) pProvInfo.GetValue(),
                                           &cbData))
        return FALSE;

    // First disassociate the key from the cert.
    if (!CertSetCertificateContextProperty(pCertContext,
                                           CERT_KEY_PROV_INFO_PROP_ID,
                                           0,
                                           NULL))
        return FALSE;

    HCRYPTPROV hProv = NULL;
    return CryptoHelper::WszCryptAcquireContext_SO_TOLERANT(&hProv,
                                  pProvInfo->pwszContainerName,
                                  pProvInfo->pwszProvName,
                                  pProvInfo->dwProvType,
                                  (pProvInfo->dwFlags & CRYPT_MACHINE_KEYSET) | CRYPT_DELETEKEYSET);
}
#endif // !FEATURE_CORECLR

// 
// Loads a certificate or store from a blob and returns that content type.
//

DWORD X509Helper::LoadFromBlob (CERT_BLOB* pCertBlob,
                                __in_opt WCHAR* pwszPassword,
                                DWORD dwFlags,
                                PCCERT_CONTEXT* pCertContext,
                                HCERTSTORE* phCertStore,
                                HCRYPTMSG* phCryptMsg) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    DWORD dwContentType = 0;
    GCX_PREEMP();
    if (!CryptQueryObject(CERT_QUERY_OBJECT_BLOB,
                             pCertBlob,
                             dwFlags,
                             X509_CERT_FORMAT_FLAGS,
                             0,
                             NULL,
                             &dwContentType,
                             NULL,
                             phCertStore,
                             phCryptMsg,
                             (const void **)pCertContext)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptQueryObject failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    return dwContentType;
}

// 
// Loads a certificate or store from a file and returns that content type.
//

DWORD X509Helper::LoadFromFile (__in_z WCHAR* pwszFileName,
                                __in_opt WCHAR* pwszPassword,
                                DWORD dwFlags,
                                PCCERT_CONTEXT* pCertContext,
                                HCERTSTORE* phCertStore,
                                HCRYPTMSG* phCryptMsg) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    DWORD dwContentType = 0;
    GCX_PREEMP();
    if (!CryptQueryObject(CERT_QUERY_OBJECT_FILE,
                          pwszFileName,
                          dwFlags,
                          X509_CERT_FORMAT_FLAGS,
                          0,
                          NULL,
                          &dwContentType,
                          NULL,
                          phCertStore,
                          phCryptMsg,
                          (const void **)pCertContext)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptQueryObject failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    return dwContentType;
}

//
// Reads a file into memory. The caller is responsible for deleting the allocated memory.
//

HRESULT X509Helper::ReadFileIntoMemory (LPCWSTR wszFileName,
                                        LPBYTE* ppbBuffer,
                                        DWORD* pdwBufLen)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    HandleHolder hFile(WszCreateFile (wszFileName,
                                        GENERIC_READ,
                                        FILE_SHARE_READ,
                                        NULL,
                                        OPEN_EXISTING,
                                        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                        NULL));
    if (hFile == INVALID_HANDLE_VALUE)
        return HRESULT_FROM_GetLastError();

    DWORD dwFileLen = SafeGetFileSize(hFile, 0);
    if (dwFileLen == 0xFFFFFFFF)
        return HRESULT_FROM_GetLastError();

    _ASSERTE(ppbBuffer);
    *ppbBuffer = new BYTE[dwFileLen];
      
    if ((SetFilePointer(hFile, 0, NULL, FILE_BEGIN) == 0xFFFFFFFF) ||
        (!ReadFile(hFile, *ppbBuffer, dwFileLen, pdwBufLen, NULL))) {
        delete[] *ppbBuffer;
        *ppbBuffer = 0;
        return HRESULT_FROM_GetLastError();
    }

    _ASSERTE(dwFileLen == *pdwBufLen);
    return S_OK;
}

//
// Returns the name for the subject or issuer.
//  dwFlags : 0 for subject name or CERT_NAME_ISSUER_FLAG for issuer name.
//  dwDisplayType: display type.
//
// It is the caller's responsibility to free the allocated buffer.
//

WCHAR* COMX509Certificate::GetCertNameInfo(PCCERT_CONTEXT pCertContext, DWORD dwFlags, DWORD dwDisplayType, DWORD dwStrType) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    DWORD cchCount = 0;
    if ((cchCount = CertGetNameString(pCertContext,
                                      dwDisplayType,
                                      dwFlags,
                                      &dwStrType,
                                      NULL,
                                      0)) == 0) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetNameString failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    NewArrayHolder<WCHAR> pwszName(new WCHAR[cchCount]);
    if (!CertGetNameString(pCertContext,
                           dwDisplayType,
                           dwFlags,
                           &dwStrType,
                           pwszName,
                           cchCount)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetNameString failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    pwszName.SuppressRelease();
    return pwszName;
}

#if !defined(FEATURE_CORECLR)
//
// Finds the first certificate with a private key in a PFX store.
// If none has a private key, we take the first certificatee in the PFX store.
//

PCCERT_CONTEXT COMX509Certificate::FilterPFXStore (CERT_BLOB* pfxBlob, __in_z WCHAR* pwszPassword, DWORD dwFlags) {
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    GCX_PREEMP();

    HandleCertStoreHolder hCertStore = NULL;
    hCertStore = PFXImportCertStore(pfxBlob, pwszPassword, dwFlags);
    if (hCertStore == NULL) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: PFXImportCertStore failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    // now filter the PFX store
    PCCERT_CONTEXT pCertContext = NULL;
    PCCERT_CONTEXT pEnumContext = NULL;
    DWORD cb = 0;

    // Find the first cert with private key, if none, then simply take the very first cert.
    while ((pEnumContext = CertEnumCertificatesInStore(hCertStore, pEnumContext)) != NULL) {
        if (CertGetCertificateContextProperty(pEnumContext, CERT_KEY_PROV_INFO_PROP_ID, NULL, &cb)) {
            if (pCertContext != NULL) {
                if (CertGetCertificateContextProperty(pCertContext, CERT_KEY_PROV_INFO_PROP_ID, NULL, &cb)) {
                    X509Helper::DeleteKeyContainer(pEnumContext);
                } else {
                    CertFreeCertificateContext(pCertContext);
                    pCertContext = CertDuplicateCertificateContext(pEnumContext);
                }
            } else {
                pCertContext = CertDuplicateCertificateContext(pEnumContext);
            }
        } else {
            // Keep the first one.
            if (pCertContext == NULL) 
                pCertContext = CertDuplicateCertificateContext(pEnumContext);
        }
        // Don't free pEnumContext here, as CertEnumCertificatesInStore will do it for us
    }

    if (pCertContext == NULL)
        CryptoHelper::COMPlusThrowCrypto(ERROR_INVALID_PARAMETER);

    return pCertContext;
}

//
// Finds the signer certificate in a PKCS7 signed store.
//

PCCERT_CONTEXT COMX509Certificate::GetSignerInPKCS7Store (HCERTSTORE hCertStore, HCRYPTMSG hCryptMsg)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    // make sure that there is at least one signer of the certificate store
    DWORD dwSigners;
    DWORD cbSigners = sizeof(dwSigners);
    if (!CryptMsgGetParam(hCryptMsg,
                          CMSG_SIGNER_COUNT_PARAM,
                          0,
                          &dwSigners,
                          &cbSigners))
    {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptMsgGetParam(CMSG_SIGNER_COUNT_PARAM) failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    if (dwSigners == 0)
        CryptoHelper::COMPlusThrowCrypto(CRYPT_E_SIGNER_NOT_FOUND);

    // get the first signer from the store, and use that as the loaded certificate
    DWORD cbData = 0;
    if (!CryptMsgGetParam(hCryptMsg,
                          CMSG_SIGNER_INFO_PARAM,
                          0,
                          NULL,
                          &cbData))
    {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptMsgGetParam(CMS_SIGNER_INFO_PARAM) failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    NewArrayHolder<CMSG_SIGNER_INFO> pCmsgSigner = (CMSG_SIGNER_INFO*) new BYTE[cbData];
    if (!CryptMsgGetParam(hCryptMsg,
                          CMSG_SIGNER_INFO_PARAM,
                          0,
                          pCmsgSigner,
                          &cbData))
    {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptMsgGetParam failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    CERT_INFO CertInfo;
    CertInfo.Issuer = pCmsgSigner->Issuer;
    CertInfo.SerialNumber = pCmsgSigner->SerialNumber;
    PCCERT_CONTEXT pCertContext = CertFindCertificateInStore(hCertStore,
                                                             X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                                                             0,
                                                             CERT_FIND_SUBJECT_CERT,
                                                             (LPVOID) &CertInfo,
                                                             NULL);

    if (pCertContext == NULL)
    {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertFindCertificateInStore failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    return pCertContext;
}
#endif // !FEATURE_CORECLR

//
// FCALL methods
//

//
// The certificate context ref count is incremented by CertDuplicateCertificateContext, 
// so it is the caller's responsibility to free the context.
//

FCIMPL2(void, COMX509Certificate::DuplicateCertContext, INT_PTR handle, SafeHandle** ppCertUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE pCertSAFE = (SAFEHANDLE) *ppCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(pCertSAFE);

    PCCERT_CONTEXT pCertContext = (PCCERT_CONTEXT) handle;
    PCCERT_CONTEXT pCertDup = NULL;
    BOOL bDelKeyContainer = FALSE;
    {
        GCX_PREEMP();
        if ((pCertDup = CertDuplicateCertificateContext(pCertContext)) == NULL)
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE));

        DATA_BLOB blob;
        DWORD cbData = 0;
        if (CertGetCertificateContextProperty(pCertDup, 
                                              CERT_DELETE_KEYSET_PROP_ID, // This value should be defined in wincrypt.h
                                                                          // as well to avoid conflicts.
                                              (void*) &blob,
                                              &cbData)) 
            bDelKeyContainer = TRUE;
    }

    CERT_CTX* pCert = new CERT_CTX(pCertDup);
#if !defined(FEATURE_CORECLR)
    pCert->m_fDelKeyContainer = bDelKeyContainer;
#endif // !FEATURE_CORECLR

    pCertSAFE->SetHandle((void*) pCert);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Free a handle to a CERT_CTX structure. Critical finalizer method for SafeCertContextHandle.
// 

FCIMPL1(void, COMX509Certificate::FreePCertContext, INT_PTR pCertCtx)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    BOOL bRet = TRUE;
    CERT_CTX* pCert = (CERT_CTX*) pCertCtx;

    if (pCert) 
        bRet = pCert->Release();

    // Add this assert to debug failures to free resources
    _ASSERTE(bRet);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// ASN encoded certificate as a byte array.
//

FCIMPL1(U1Array*, COMX509Certificate::GetCertRawData, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;
    U1ARRAYREF pbRawData = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_2(pbRawData, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    CryptoHelper::ByteArrayToU1ARRAYREF(pCert->m_pCtx->pbCertEncoded, 
                                        pCert->m_pCtx->cbCertEncoded, 
                                        &pbRawData);

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(pbRawData);
}
FCIMPLEND

//
// Returns the NotAfter field.
//

FCIMPL2(void, COMX509Certificate::GetDateNotAfter, SafeHandle* pCertUNSAFE, FILETIME* pFileTime)
{
    FCALL_CONTRACT;

    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    pFileTime->dwLowDateTime = pCert->m_pCtx->pCertInfo->NotAfter.dwLowDateTime;
    pFileTime->dwHighDateTime = pCert->m_pCtx->pCertInfo->NotAfter.dwHighDateTime;

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Returns the NotBefore field.
//

FCIMPL2(void, COMX509Certificate::GetDateNotBefore, SafeHandle* pCertUNSAFE, FILETIME* pFileTime)
{
    FCALL_CONTRACT;

    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    pFileTime->dwLowDateTime = pCert->m_pCtx->pCertInfo->NotBefore.dwLowDateTime; 
    pFileTime->dwHighDateTime = pCert->m_pCtx->pCertInfo->NotBefore.dwHighDateTime;

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Issuer name as a string.
//

FCIMPL2(StringObject*, COMX509Certificate::GetIssuerName, SafeHandle* pCertUNSAFE, CLR_BOOL fLegacyV1Mode)
{
    FCALL_CONTRACT;

    STRINGREF issuerString = NULL;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(issuerString, pCertSAFE);

    DWORD dwStrType = CERT_X500_NAME_STR;
    if (!fLegacyV1Mode)
        dwStrType |= CERT_NAME_STR_REVERSE_FLAG;

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    DWORD cchCount = 0;
    if ((cchCount = CertGetNameString(pCert->m_pCtx,
                                      CERT_NAME_RDN_TYPE,
                                      CERT_NAME_ISSUER_FLAG,
                                      &dwStrType,
                                      NULL,
                                      0)) == 0) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetNameString failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    NewArrayHolder<WCHAR> pwszIssuerName(new WCHAR[cchCount]);
    if (!CertGetNameString(pCert->m_pCtx,
                           CERT_NAME_RDN_TYPE,
                           CERT_NAME_ISSUER_FLAG,
                           &dwStrType,
                           pwszIssuerName,
                           cchCount)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetNameString failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    issuerString = StringObject::NewString(pwszIssuerName);

    HELPER_METHOD_FRAME_END();
    return (StringObject*) OBJECTREFToObject(issuerString);
}
FCIMPLEND

//
// Returns the public key friendly name.
// 

FCIMPL1(StringObject*, COMX509Certificate::GetPublicKeyOid, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF oidString = NULL;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(oidString, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    NewArrayHolder<WCHAR> pwszOid(CryptoHelper::AnsiToUnicode((char*) pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId));
    oidString = StringObject::NewString(pwszOid);

    HELPER_METHOD_FRAME_END();
    return (StringObject*) OBJECTREFToObject(oidString);
}
FCIMPLEND

//
// Returns the public key ASN encoded parameters.
//

FCIMPL1(U1Array*, COMX509Certificate::GetPublicKeyParameters, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    U1ARRAYREF pbParameters = NULL;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(pbParameters, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    BOOL bInheritedParams = FALSE;

#if defined(FEATURE_CORECLR)
    // We'll support RSA-based certificates only in Telesto, not DSS.
    // This is in lieu of looking up the OID info, as it would require implementing
    // CryptFindOIDInfo in the PAL, along with the tables of OID infos relating to
    // at least the OIDs whose Algid's are CALG_DSS_SIGN.
    _ASSERTE(pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId != NULL);
    if(strncmp(szOID_RSA_RSA, pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId, COUNTOF(szOID_RSA_RSA)) != 0)
        CryptoHelper::COMPlusThrowCrypto(COR_E_PLATFORMNOTSUPPORTED);
#else // FEATURE_CORECLR
    PCCRYPT_OID_INFO pOidInfo = NULL;
    {
        GCX_PREEMP();

        // check to see if this is the most common case -- szOID_RSA_RSA. If so, we know that this is not
        // a DSS cert, so we don't need to get extra information about the OID to determine that.  If it
        // is not, then we can first check in the public key OID group before falling back to check in
        // all OID groups.
        _ASSERTE(pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId != NULL);
        if(strncmp(szOID_RSA_RSA, pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId, COUNTOF(szOID_RSA_RSA)) != 0)
        {
            pOidInfo = CryptFindOIDInfo(CRYPT_OID_INFO_OID_KEY, pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId, CRYPT_PUBKEY_ALG_OID_GROUP_ID);
            if(pOidInfo == NULL)
                pOidInfo = CryptFindOIDInfo(CRYPT_OID_INFO_OID_KEY, pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId, 0);
        }
    }

    //
    // DSS certificates may not have the DSS parameters in the certificate. In this case, we try to build
    // the certificate chain and propagate the parameters down from the certificate chain.
    //

    if (pOidInfo != NULL && pOidInfo->Algid == CALG_DSS_SIGN) {
        if ((pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.cbData == 0) ||
            (*pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.pbData == NULL_ASN_TAG)) {
            // Build the chain to inherit parameters in the property, if not already inherited.
            DWORD cbData = 0;
            HRESULT hr = S_OK;
            if (!CertGetCertificateContextProperty(pCert->m_pCtx, 
                                                   CERT_PUBKEY_ALG_PARA_PROP_ID, 
                                                   NULL, 
                                                   &cbData)) {
                // build the chain and ignore any errors during the chain building
                HandleCertChainHolder pChainContext = NULL;
                hr = X509Helper::BuildChain(pCert->m_pCtx, NULL, CERT_CHAIN_POLICY_BASE, &pChainContext);
                if (SUCCEEDED(hr)) {
                    if (!CertGetCertificateContextProperty(pCert->m_pCtx, 
                                                        CERT_PUBKEY_ALG_PARA_PROP_ID, 
                                                        NULL, 
                                                        &cbData)) {
                        hr = HRESULT_FROM_GetLastError();
                        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CERT_PUBKEY_ALG_PARA_PROP_ID property not found.\n", hr));
                    }
                }
            }
            if (FAILED(hr))
                CryptoHelper::COMPlusThrowCrypto(hr);

            // The property exists; get it for real.
            NewArrayHolder<BYTE> pbData = new BYTE[cbData];
            if (!CertGetCertificateContextProperty(pCert->m_pCtx, 
                                                   CERT_PUBKEY_ALG_PARA_PROP_ID, 
                                                   (void*) pbData.GetValue(), 
                                                   &cbData))
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

            CryptoHelper::ByteArrayToU1ARRAYREF(pbData, cbData, &pbParameters);
            bInheritedParams = TRUE;
        } 
    }
#endif // (FEATURE_CORECLR) else

    if (!bInheritedParams) {
        CryptoHelper::ByteArrayToU1ARRAYREF(pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.pbData,
                                            pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.cbData, 
                                            &pbParameters);
    }

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(pbParameters);
}
FCIMPLEND

//
// Returns the public key ASN encoded value.
//

FCIMPL1(U1Array*, COMX509Certificate::GetPublicKeyValue, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    U1ARRAYREF pbKeyValue = NULL;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(pbKeyValue, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    CryptoHelper::ByteArrayToU1ARRAYREF(pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.PublicKey.pbData,
                                        pCert->m_pCtx->pCertInfo->SubjectPublicKeyInfo.PublicKey.cbData, 
                                        &pbKeyValue);

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(pbKeyValue);
}
FCIMPLEND

//
// Serial number as a byte array.
//

FCIMPL1(U1Array*, COMX509Certificate::GetSerialNumber, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;
    U1ARRAYREF pbSerialNumber = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_2(pbSerialNumber, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    CryptoHelper::ByteArrayToU1ARRAYREF(pCert->m_pCtx->pCertInfo->SerialNumber.pbData, 
                                        pCert->m_pCtx->pCertInfo->SerialNumber.cbData, 
                                        &pbSerialNumber);

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(pbSerialNumber);
}
FCIMPLEND

//
// Subject info as a string.
//

FCIMPL3(StringObject*, COMX509Certificate::GetSubjectInfo, SafeHandle* pCertUNSAFE, DWORD dwDisplayType, CLR_BOOL fLegacyV1Mode)
{
    FCALL_CONTRACT;

    STRINGREF subjectString = NULL;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_2(subjectString, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    DWORD dwStrType = CERT_X500_NAME_STR;
    if (!fLegacyV1Mode)
        dwStrType |= CERT_NAME_STR_REVERSE_FLAG;
    NewArrayHolder<WCHAR> pwszSubjName(GetCertNameInfo(pCert->m_pCtx, 0, dwDisplayType, dwStrType));
    subjectString = StringObject::NewString(pwszSubjName);

    HELPER_METHOD_FRAME_END();
    return (StringObject*) OBJECTREFToObject(subjectString);
}
FCIMPLEND

//
// Returns the thumbprint of the certificate.
//

FCIMPL1(U1Array*, COMX509Certificate::GetThumbprint, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;
    U1ARRAYREF pbThumbprint = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_2(pbThumbprint, pCertSAFE);

    SafeHandleHolder shh(&pCertSAFE);
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    DWORD dwSize = 0;
    if(!CertGetCertificateContextProperty(pCert->m_pCtx,
                                          CERT_SHA1_HASH_PROP_ID,
                                          NULL,
                                          &dwSize)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetCertificateContextProperty failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    pbThumbprint = (U1ARRAYREF) AllocatePrimitiveArray(ELEMENT_TYPE_U1, dwSize);
    if(!CertGetCertificateContextProperty(pCert->m_pCtx, 
                                          CERT_SHA1_HASH_PROP_ID,
                                          pbThumbprint->m_Array,
                                          &dwSize)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertGetCertificateContextProperty failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(pbThumbprint);
}
FCIMPLEND

//
// Opens the blob and gets its type, then loads a certificate from it. Depending on the blob type, 
// the blob can contain 1 or more certificates. If more than 1, we select the most likely choice. 
//

FCIMPL5(void, COMX509Certificate::LoadCertFromBlob, U1Array* dataUNSAFE, 
        __in_z WCHAR* pwszPassword, DWORD dwFlags, CLR_BOOL persistKeySet, SafeHandle** ppCertUNSAFE);
{
    FCALL_CONTRACT;

    struct _gc {
        U1ARRAYREF dataSAFE;
        SAFEHANDLE pCertSAFE;
    } gc;

    gc.dataSAFE = (U1ARRAYREF) dataUNSAFE;
    gc.pCertSAFE = (SAFEHANDLE) *ppCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    NewArrayHolder<BYTE> buffer(CryptoHelper::U1ARRAYREFToByteArray(gc.dataSAFE));
    CERT_BLOB certBlob = {gc.dataSAFE->GetNumComponents(), buffer};
    HandleCertContextHolder pCertContext(NULL);

    HandleCertStoreHolder hCertStore(NULL);
    HandleCryptMsgHolder hCryptMsg(NULL);

    DWORD dwContentType; dwContentType = X509Helper::LoadFromBlob(&certBlob,
                                                   pwszPassword,
                                                   X509_CERT_CONTENT_FLAGS,
                                                   &pCertContext,
                                                   &hCertStore,
                                                   &hCryptMsg);

#if !defined(FEATURE_CORECLR)
    if (dwContentType == CERT_QUERY_CONTENT_PKCS7_SIGNED 
        || dwContentType == CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED) {
        pCertContext = GetSignerInPKCS7Store(hCertStore, hCryptMsg);
    } else if (dwContentType == CERT_QUERY_CONTENT_PFX) {
        pCertContext = FilterPFXStore(&certBlob, pwszPassword, dwFlags);
    }
#endif // !FEATURE_CORECLR

    CERT_CTX* pCert = new CERT_CTX(pCertContext);
#if !defined(FEATURE_CORECLR)
    if (dwContentType == CERT_QUERY_CONTENT_PFX)
        pCert->m_fDelKeyContainer = (persistKeySet == FALSE);
#endif // !FEATURE_CORECLR

    gc.pCertSAFE->SetHandle((void*) pCert);
    pCertContext.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Opens the file and gets its type, then loads a certificate from it. Depending on the blob type, 
// the blob can contain 1 or more certificates. If more than 1, we select the most likely choice. 
//

FCIMPL5(void, COMX509Certificate::LoadCertFromFile, StringObject* fileNameUNSAFE, 
        __in_z WCHAR* pwszPassword, DWORD dwFlags, CLR_BOOL persistKeySet, SafeHandle** ppCertUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc
    {
        STRINGREF fileNameSAFE;
        SAFEHANDLE pCertSAFE;
    } gc;

    gc.fileNameSAFE = (STRINGREF) fileNameUNSAFE;
    gc.pCertSAFE = (SAFEHANDLE) *ppCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    NewArrayHolder<WCHAR> pwszFileName(CryptoHelper::STRINGREFToUnicode(gc.fileNameSAFE));

    HandleCertStoreHolder hCertStore(NULL);
    HandleCryptMsgHolder hCryptMsg(NULL);

    HandleCertContextHolder pCertContext(NULL);
    DWORD dwContentType; dwContentType = X509Helper::LoadFromFile(pwszFileName,
                                                   pwszPassword,
                                                   X509_CERT_CONTENT_FLAGS,
                                                   &pCertContext,
                                                   &hCertStore,
                                                   &hCryptMsg);

#if !defined(FEATURE_CORECLR)
    if (dwContentType == CERT_QUERY_CONTENT_PKCS7_SIGNED || dwContentType == CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED) {
        pCertContext = GetSignerInPKCS7Store(hCertStore, hCryptMsg);
    } else if (dwContentType == CERT_QUERY_CONTENT_PFX) {
        NewArrayHolder<BYTE> pb = NULL; 
        DWORD cb = 0;
        // read the file
        HRESULT hr = X509Helper::ReadFileIntoMemory(gc.fileNameSAFE->GetBuffer(), &pb, &cb);
        if (FAILED(hr)) {
            LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: ReadFileIntoMemory failed.\n", hr));
            CryptoHelper::COMPlusThrowCrypto(hr);
        }
        CERT_BLOB certBlob = {cb, pb};
        pCertContext = FilterPFXStore(&certBlob, pwszPassword, dwFlags);
    }
#endif // !FEATURE_CORECLR

    CERT_CTX* pCert = new CERT_CTX(pCertContext);
#if !defined(FEATURE_CORECLR)
    if (dwContentType == CERT_QUERY_CONTENT_PFX)
        pCert->m_fDelKeyContainer = (persistKeySet == FALSE);
#endif // !FEATURE_CORECLR

    gc.pCertSAFE->SetHandle((void*) pCert);
    pCertContext.SuppressRelease();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// This method opens the blob and returns its type.
//

FCIMPL1(DWORD, COMX509Certificate::QueryCertBlobType, U1Array* dataUNSAFE)
{
    FCALL_CONTRACT;

    DWORD dwContentType = 0;
    U1ARRAYREF dataSAFE = (U1ARRAYREF) dataUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(dataSAFE);

    NewArrayHolder<BYTE> buffer(CryptoHelper::U1ARRAYREFToByteArray(dataSAFE));
    CERT_BLOB certBlob = {dataSAFE->GetNumComponents(), buffer};

    {
        GCX_PREEMP();
        if (!CryptQueryObject(CERT_QUERY_OBJECT_BLOB,
                              &certBlob,
                              X509_CERT_CONTENT_FLAGS,
                              X509_CERT_FORMAT_FLAGS,
                              0,
                              NULL,
                              &dwContentType,
                              NULL,
                              NULL,
                              NULL,
                              NULL)) {
            LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptQueryObject failed.\n", HRESULT_FROM_GetLastError()));
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        }
    }

    HELPER_METHOD_FRAME_END();
    return dwContentType;

}
FCIMPLEND

//
// This method opens the file and returns its type.
//

FCIMPL1(DWORD, COMX509Certificate::QueryCertFileType, StringObject* fileNameUNSAFE)
{
    FCALL_CONTRACT;

    DWORD dwContentType = 0;
    STRINGREF fileNameSAFE = (STRINGREF) fileNameUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(fileNameSAFE);

    NewArrayHolder<WCHAR> buffer(CryptoHelper::STRINGREFToUnicode(fileNameSAFE));

    {
        GCX_PREEMP();
        if (!CryptQueryObject(CERT_QUERY_OBJECT_FILE,
                              buffer,
                              X509_CERT_CONTENT_FLAGS,
                              X509_CERT_FORMAT_FLAGS,
                              0,
                              NULL,
                              &dwContentType,
                              NULL,
                              NULL,
                              NULL,
                              NULL)) {
            LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CryptQueryObject failed.\n", HRESULT_FROM_GetLastError()));
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        }
    }

    HELPER_METHOD_FRAME_END();
    return dwContentType;
    
}
FCIMPLEND

//
// FCALL methods
//

#if !defined(FEATURE_CORECLR)
//
// Add a certificate to the store.
// Added certificates are not persisted for non-system stores.
//

FCIMPL2(void, COMX509Store::AddCertificate, SafeHandle* hStoreUNSAFE, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE hStoreSAFE = (SAFEHANDLE) hStoreUNSAFE;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(hStoreSAFE, pCertSAFE);

    SafeHandleHolder shh1(&hStoreSAFE);
    SafeHandleHolder shh2(&pCertSAFE);

    HCERTSTORE hCertStore = (HCERTSTORE) hStoreSAFE->GetHandle();
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    GCX_PREEMP();
    if (!CertAddCertificateLinkToStore(hCertStore, 
                                       pCert->m_pCtx, 
                                       CERT_STORE_ADD_ALWAYS, 
                                       NULL)) {
        LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertAddCertificateContextToStore failed.\n", HRESULT_FROM_GetLastError()));
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Exports a memory store of certificates into a byte array. 
//

FCIMPL3(U1Array*, COMX509Store::ExportCertificatesToBlob, SafeHandle* hStoreUNSAFE, DWORD dwContentType, __in_z WCHAR* pwszPassword)
{
    FCALL_CONTRACT;

    struct _gc
    {
        U1ARRAYREF pbBlob;
        SAFEHANDLE hStoreSAFE;
    } gc;

    gc.pbBlob = NULL;
    gc.hStoreSAFE = (SAFEHANDLE) hStoreUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    SafeHandleHolder shh(&gc.hStoreSAFE);
    HCERTSTORE hCertStore = (HCERTSTORE) gc.hStoreSAFE->GetHandle();

    HandleCertContextHolder pEnumContext(NULL);
    NewArrayHolder<BYTE> pbEncoded(NULL);
    DWORD dwSaveAs = CERT_STORE_SAVE_AS_PKCS7;
    CRYPT_DATA_BLOB DataBlob = {0, NULL};

    switch(dwContentType) {
    case X509_CERT_TYPE:
        pEnumContext = CertEnumCertificatesInStore(hCertStore, pEnumContext);
        if (pEnumContext.GetValue() != NULL)
            CryptoHelper::ByteArrayToU1ARRAYREF(pEnumContext->pbCertEncoded,
                                                pEnumContext->cbCertEncoded,
                                                &gc.pbBlob);
        break;

    case X509_SERIALIZED_CERT_TYPE:
        pEnumContext = CertEnumCertificatesInStore(hCertStore, pEnumContext);
        if (pEnumContext.GetValue() != NULL) {
            DWORD cbEncoded = 0;
            if (!CertSerializeCertificateStoreElement(pEnumContext,
                                                      0,
                                                      NULL,
                                                      &cbEncoded)) {
                LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertSerializeCertificateStoreElement failed.\n", HRESULT_FROM_GetLastError()));
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
            }
            pbEncoded = new BYTE[cbEncoded];
            if (!CertSerializeCertificateStoreElement(pEnumContext,
                                                      0,
                                                      pbEncoded,
                                                      &cbEncoded)) {
                LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertSerializeCertificateStoreElement failed.\n", HRESULT_FROM_GetLastError()));
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
            }
            CryptoHelper::ByteArrayToU1ARRAYREF(pbEncoded, cbEncoded, &gc.pbBlob);
        }
        break;

    case X509_PFX_TYPE:
        {
            GCX_PREEMP();
            if (!PFXExportCertStore(hCertStore,
                                    &DataBlob,
                                    pwszPassword,
                                    EXPORT_PRIVATE_KEYS | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY)) {
                LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: PFXExportCertStorePFXExportCertStore failed.\n", HRESULT_FROM_GetLastError()));
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
            }
            pbEncoded = new BYTE[DataBlob.cbData];
            DataBlob.pbData = pbEncoded;
            if (!PFXExportCertStore(hCertStore,
                                    &DataBlob,
                                    pwszPassword,
                                    EXPORT_PRIVATE_KEYS | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY)) {
                LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: PFXExportCertStorePFXExportCertStore failed.\n", HRESULT_FROM_GetLastError()));
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
            }
        }
        CryptoHelper::ByteArrayToU1ARRAYREF(DataBlob.pbData, DataBlob.cbData, &gc.pbBlob);
        break;

    case X509_SERIALIZED_STORE_TYPE:
        dwSaveAs = CERT_STORE_SAVE_AS_STORE;
        // falling through
    case X509_PKCS7_TYPE:
        {
            GCX_PREEMP();
            // determine the required length
            if (!CertSaveStore(hCertStore,
                               X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                               dwSaveAs,
                               CERT_STORE_SAVE_TO_MEMORY,
                               (void *) &DataBlob,
                               0)) {
                LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertSaveStore failed.\n", HRESULT_FROM_GetLastError()));
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
            }
            pbEncoded = new BYTE[DataBlob.cbData];
            DataBlob.pbData = pbEncoded;
            // now save the store to a memory blob
            if (!CertSaveStore(hCertStore,
                               X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                               dwSaveAs,
                               CERT_STORE_SAVE_TO_MEMORY,
                               (void *) &DataBlob,
                               0)) {
                LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertSaveStore failed.\n", HRESULT_FROM_GetLastError()));
                CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
            }
        }
        CryptoHelper::ByteArrayToU1ARRAYREF(DataBlob.pbData, DataBlob.cbData, &gc.pbBlob);
        break;

    default:
        COMPlusThrow(kCryptographicException, W("Cryptography_X509_InvalidContentType"));
    }

    HELPER_METHOD_FRAME_END();
    return (U1Array*) OBJECTREFToObject(gc.pbBlob);
}
FCIMPLEND

//
// Free an HCERTSTORE handle. Critical finalizer method for SafeCertStoreHandle.
//

FCIMPL1(void, COMX509Store::FreeCertStoreContext, INT_PTR hCertStore)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    BOOL bRet = TRUE;
    HCERTSTORE hStore = (HCERTSTORE) hCertStore;
    if (hStore)
        bRet = CertCloseStore(hStore, 0);

    // Add this assert to debug failures to free resources
    _ASSERTE(bRet);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//
// Open a certificate store.
//

FCIMPL4(void, COMX509Store::OpenX509Store, DWORD dwType, DWORD dwFlags, StringObject* storeNameUNSAFE, SafeHandle** phStoreUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF storeNameSAFE = (STRINGREF) storeNameUNSAFE;
    SAFEHANDLE hStoreSAFE = (SAFEHANDLE) *phStoreUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(storeNameSAFE, hStoreSAFE);

    NewArrayHolder<WCHAR> pwszStoreName(NULL);
    if (storeNameSAFE != NULL) {
        DWORD dwSize = storeNameSAFE->GetStringLength();
        if (dwSize > 0)
            pwszStoreName = CryptoHelper::STRINGREFToUnicode(storeNameSAFE);
    }

    HCERTSTORE hCertStore = NULL;
    {
        GCX_PREEMP();
        hCertStore = CertOpenStore((LPCSTR)(size_t)dwType, 
                                    X509_ASN_ENCODING | PKCS_7_ASN_ENCODING, 
                                    NULL, 
                                    dwFlags | CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG, 
                                    pwszStoreName);
        if (hCertStore == NULL) {
            LOG((LF_SECURITY, LL_INFO10000, "Error [%#x]: CertOpenStore failed.\n", HRESULT_FROM_GetLastError()));
            CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());
        }
    }

    hStoreSAFE->SetHandle(hCertStore);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// COMX509Store::RemoveCertificate is an FCALL that is not consumed by managed code
// nor is it referred to by native code. This #ifdef makes it be dead code, which it
// would otherwise not be and require CertFindCertificateInStore, which is otherwise
// not required, and thus does not need to get implemented in the PAL.

//
// Remove a certificate from the store.
// Removed certificates are not persisted for non-system stores.
//

FCIMPL2(void, COMX509Store::RemoveCertificate, SafeHandle* hStoreUNSAFE, SafeHandle* pCertUNSAFE)
{
    FCALL_CONTRACT;

    SAFEHANDLE hStoreSAFE = (SAFEHANDLE) hStoreUNSAFE;
    SAFEHANDLE pCertSAFE = (SAFEHANDLE) pCertUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_2(hStoreSAFE, pCertSAFE);

    SafeHandleHolder shh1(&hStoreSAFE);
    SafeHandleHolder shh2(&pCertSAFE);

    HCERTSTORE hCertStore = (HCERTSTORE) hStoreSAFE->GetHandle();
    CERT_CTX* pCert = CryptoHelper::DereferenceSafeHandle<CERT_CTX>(pCertSAFE);

    // Find the certificate in the store.
    PCCERT_CONTEXT pCert2 = NULL;
    if ((pCert2 = CertFindCertificateInStore(hCertStore, 
                                             X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                                             0, 
                                             CERT_FIND_EXISTING, 
                                             (const void *) pCert->m_pCtx,
                                             NULL)) == NULL)
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    // Remove from the store.
    if (!CertDeleteCertificateFromStore(pCert2))
        // CertDeleteCertificateFromStore always releases the context regardless of success 
        // or failure so we don't need to manually release it
        CryptoHelper::COMPlusThrowCrypto(HRESULT_FROM_GetLastError());

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#endif // !FEATURE_CORECLR

#endif // FEATURE_X509
