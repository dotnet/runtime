// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// 


#ifndef _CRYPTOGRAPHY_H_
#define _CRYPTOGRAPHY_H_

#include <wincrypt.h>

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
#include "fcall.h"

class DSA_CSP_Object : public Object {
public:
    U1ARRAYREF   m_P;            // ubyte[]
    U1ARRAYREF   m_Q;            // ubyte[]
    U1ARRAYREF   m_G;            // ubyte[]
    U1ARRAYREF   m_Y;            // ubyte[] - optional
    U1ARRAYREF   m_J;            // ubyte[] - optional
    U1ARRAYREF   m_X;            // ubyte[] - optional - private key
    U1ARRAYREF   m_seed;         // ubyte[] - optional - paired with counter
    DWORD        m_counter;      // DWORD - optional
};

class RSA_CSP_Object : public Object {
public:
    U1ARRAYREF   m_Exponent;    // ubyte[]
    U1ARRAYREF   m_Modulus;     // ubyte[]
    U1ARRAYREF   m_P;           // ubyte[] - optional
    U1ARRAYREF   m_Q;           // ubyte[] - optional
    U1ARRAYREF   m_dp;          // ubyte[] - optional
    U1ARRAYREF   m_dq;          // ubyte[] - optional
    U1ARRAYREF   m_InverseQ;    // ubyte[] - optional
    U1ARRAYREF   m_d;           // ubyte[] - optional
};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<DSA_CSP_Object> DSA_CSPREF;
typedef REF<RSA_CSP_Object> RSA_CSPREF;
#else  // !_DEBUG
typedef DSA_CSP_Object * DSA_CSPREF;
typedef RSA_CSP_Object * RSA_CSPREF;
#endif // _DEBUG

#endif // #if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)

#define DSS_MAGIC           0x31535344
#define DSS_PRIVATE_MAGIC   0x32535344
#define DSS_PUB_MAGIC_VER3  0x33535344
#define DSS_PRIV_MAGIC_VER3 0x34535344
#define RSA_PUB_MAGIC       0x31415352
#define RSA_PRIV_MAGIC      0x32415352

#define DSS_Q_LEN 20

// Keep in sync with managed definition in System.Security.Cryptography.Utils
#define CLR_KEYLEN              1
#define CLR_PUBLICKEYONLY       2
#define CLR_EXPORTABLE          3
#define CLR_REMOVABLE           4
#define CLR_HARDWARE            5
#define CLR_ACCESSIBLE          6
#define CLR_PROTECTED           7
#define CLR_UNIQUE_CONTAINER    8
#define CLR_ALGID               9
#define CLR_PP_CLIENT_HWND      10
#define CLR_PP_PIN              11

#define MAX_CACHE_DEFAULT_PROVIDERS 20
// size of a symmetric key block size. 8 is the only supported for now
#define BLOCK_LEN 8
 
// Dependency in managed : System/Security/Cryptography/Crypto.cs
#define CRYPTO_PADDING_NONE         1
#define CRYPTO_PADDING_PKCS5        2
#define CRYPTO_PADDING_Zeros        3
#define CRYPTO_PADDING_ANSI_X_923   4
#define CRYPTO_PADDING_ISO_10126    5

// These flags match those defined for the CspProviderFlags enum in 
// src/bcl/system/security/cryptography/CryptoAPITransform.cs

#define CSP_PROVIDER_FLAGS_USE_MACHINE_KEYSTORE      0x0001
#define CSP_PROVIDER_FLAGS_USE_DEFAULT_KEY_CONTAINER 0x0002
#define CSP_PROVIDER_FLAGS_USE_NON_EXPORTABLE_KEY    0x0004
#define CSP_PROVIDER_FLAGS_USE_EXISTING_KEY          0x0008
#define CSP_PROVIDER_FLAGS_USE_ARCHIVABLE_KEY        0x0010
#define CSP_PROVIDER_FLAGS_USE_USER_PROTECTED_KEY    0x0020
#define CSP_PROVIDER_FLAGS_USE_CRYPT_SILENT          0x0040
#define CSP_PROVIDER_FLAGS_CREATE_EPHEMERAL_KEY      0x0080


#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO) || defined(FEATURE_X509)
class CryptoHelper {
public:
    static void COMPlusThrowCrypto (HRESULT hr);
    static BOOL WszCryptAcquireContext_SO_TOLERANT (HCRYPTPROV *phProv, LPCWSTR pwszContainer, LPCWSTR pwszProvider, DWORD dwProvType, DWORD dwFlags);    
    static WCHAR* STRINGREFToUnicode (STRINGREF s);
    static WCHAR* AnsiToUnicode (__in_z char* pszAnsi);
    static void ByteArrayToU1ARRAYREF (LPBYTE pb, DWORD cb, U1ARRAYREF* u1);
    static BYTE* U1ARRAYREFToByteArray (U1ARRAYREF u1);
    static char* UnicodeToAnsi (__in_z WCHAR* pwszUnicode);
#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
    static WCHAR* GetRandomKeyContainer ();    
    static inline void memrev (LPBYTE pb, DWORD cb);
    static BOOL CryptGenKey_SO_TOLERANT (HCRYPTPROV hProv, ALG_ID Algid, DWORD dwFlags, HCRYPTKEY* phKey);
#endif // FEATURE_CRYPTO    

    static LPCWSTR UpgradeDSS(DWORD dwProvType, __in_z LPCWSTR wszProvider);
    static LPCWSTR UpgradeRSA(DWORD dwProvType, __in_z LPCWSTR wszProvider);

    // Since crytpo classes use safe handles where the handles are really pointers to structures, we
    // need to ensure that they weren't freed and set to NULL if the handle was used in an unsafe
    // multithreaded way. This method unpacks the handle, ensuring it doesn't contain a NULL pointer.
    template<class T>
    static T * DereferenceSafeHandle(const SAFEHANDLE &handle)
    {
        CONTRACT(T *)
        {
            POSTCONDITION(RETVAL != NULL);
            THROWS;
            GC_TRIGGERS;
        }
        CONTRACT_END;

        T * pValue = static_cast<T *>(handle->GetHandle());
        if (!pValue)
        {
            LOG((LF_SECURITY, LL_INFO10000, "Attempt to access a NULL crypto handle, possible unsafe use of a crypto function from multiple threads"));
            COMPlusThrowCrypto(E_POINTER);
        }

        RETURN(pValue);
    }
};
#endif // FEATURE_CRYPTO || FEATURE_X509

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
typedef struct {
    BLOBHEADER          blob;
    union {
        DSSPRIVKEY_VER3         dss_priv_v3;
        DSSPUBKEY_VER3          dss_pub_v3;
        DSSPUBKEY               dss_v2;
        RSAPUBKEY               rsa;
    };
} KEY_HEADER;

// We need to define this unmanaged memory structure to hold 
// all the information relevant to the CSP in order to guarantee
// critical finalization of the resources
typedef struct CRYPT_PROV_CTX {
private:
    // We implicitely assume this method is not going to do a LoadLibrary
    HRESULT DeleteKeyContainer() {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;

        GCX_PREEMP();

        HCRYPTPROV hProv = NULL;
        if (!CryptoHelper::WszCryptAcquireContext_SO_TOLERANT(&hProv, m_pwszContainer, m_pwszProvider,
                m_dwType, (m_dwFlags & CRYPT_MACHINE_KEYSET) | CRYPT_DELETEKEYSET))
            return HRESULT_FROM_GetLastError();
        return S_OK;
    }

public:
    HCRYPTPROV     m_hProv;
    LPCWSTR        m_pwszContainer;
    LPCWSTR        m_pwszProvider;
    DWORD          m_dwType;
    DWORD          m_dwFlags;
    BOOL           m_fPersistKeyInCsp;
    BOOL           m_fReleaseProvider;
    Volatile<ULONG> m_refCount;

    CRYPT_PROV_CTX() : 
        m_hProv(0), 
        m_pwszContainer(NULL), 
        m_pwszProvider(NULL),
        m_fReleaseProvider(TRUE), 
        m_dwType(0),
        m_dwFlags(0), 
        m_fPersistKeyInCsp(TRUE),
        m_refCount(1) {
        LIMITED_METHOD_CONTRACT;
    }

    // This can be called twice. Also it can be called by multiple threads. The only
    // invariant that needs to be enforced is that when the refCount reaches 0, the 
    // object is not going to be referenced anymore by a CRYPT_KEY_CTX or a CRYPT_HASH_CTX.
    // But this is true in the way we use this in the managed side.
    void Release () {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;

        ULONG cbRef = InterlockedDecrement((LONG*)&m_refCount);
        if (cbRef == 0) {
            // Make sure not to delete a key that we want to keep in the key container or an ephemeral key
            if (m_fPersistKeyInCsp == FALSE && !(m_dwFlags & CRYPT_VERIFYCONTEXT)) {
                // We cannot throw if we fail to delete the key container, since this code runs on the
                // finalizer thread and any exception will tear the process
                DeleteKeyContainer();
            }

            if (m_pwszContainer) {
                delete[] m_pwszContainer;
                m_pwszContainer = NULL;
            }

            // The provider strings are allocated per process, so 
            // we should not free m_pwszProvider unless specified otherwise
            if (m_fReleaseProvider) {
                if (m_pwszProvider) {
                    delete[] m_pwszProvider;
                    m_pwszProvider = NULL;
                }
            }

            // We need to free the CSP handle -- make sure not to throw since this code could be on the
            // finalizer thread.
            if (m_hProv != 0) 
            {
                CryptReleaseContext(m_hProv, 0);
                m_hProv = 0;
            }

            delete this;
        }
    }

} CRYPT_PROV_CTX;

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
// A key handle needs to be freed before the provider handle
// it was loaded into is freed; so we need to keep a pointer to 
// the CRYPT_PROV_CTX and make the managed SafeKeyHandle contain
// a pointer to the CRYPT_KEY_CTX pointer
typedef struct CRYPT_KEY_CTX {
public:
    CRYPT_PROV_CTX  *m_pProvCtx;
    HCRYPTKEY       m_hKey;
    DWORD           m_dwKeySpec;
    BOOL            m_fPublicOnly;

    CRYPT_KEY_CTX(CRYPT_PROV_CTX * pProvCtx, HCRYPTKEY hKey) : 
        m_pProvCtx(pProvCtx),
        m_hKey(hKey),
        m_dwKeySpec(0),
        m_fPublicOnly(FALSE) {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(hKey != NULL);
            PRECONDITION(CheckPointer(m_pProvCtx));
            PRECONDITION((m_pProvCtx->m_refCount >= 1)); // We can't acquire a dead CRYPT_PROV_CTX
        } CONTRACTL_END;

        InterlockedIncrement((LONG*)&m_pProvCtx->m_refCount);
    }

    void Release () {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;

        // We cannot throw if CryptDestroyKey fails, since this can be executed on the finalizer thread
        if (m_hKey)
            CryptDestroyKey(m_hKey);

        // We need to release the reference to the CSP handle 
        if (m_pProvCtx)
            m_pProvCtx->Release();

        delete this;
    }

} CRYPT_KEY_CTX;

// A hash handle needs to be freed before the provider handle
// it was loaded into is freed; so we need to keep a pointer to 
// the CRYPT_PROV_CTX and make the managed SafeHashHandle contain
// a pointer to the CRYPT_HASH_CTX pointer
typedef struct CRYPT_HASH_CTX {
public:
    CRYPT_PROV_CTX  *m_pProvCtx;
    HCRYPTHASH      m_hHash;

    CRYPT_HASH_CTX(CRYPT_PROV_CTX * pProvCtx, HCRYPTHASH hHash) : 
        m_pProvCtx(pProvCtx),
        m_hHash(hHash) {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(hHash != NULL);
            PRECONDITION(CheckPointer(m_pProvCtx));
            PRECONDITION((m_pProvCtx->m_refCount >= 1)); // We can't acquire a dead CRYPT_PROV_CTX
        } CONTRACTL_END;

        InterlockedIncrement((LONG*)&m_pProvCtx->m_refCount);
    }

    void Release () {
        CONTRACTL {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        } CONTRACTL_END;

        // We cannot throw if CryptDestroyHash fails since this code could run on the finalizer thread
        if (m_hHash)
            CryptDestroyHash(m_hHash);

        // We need to release the reference to the CSP handle 
        if (m_pProvCtx)
            m_pProvCtx->Release();

        delete this;
    }

} CRYPT_HASH_CTX;
#endif // FEATURE_CRYPTO

class COMCryptography
{
public:

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
    //
    // QCalls from RSACryptoServiceProvider
    //

    // Decrypt a symmetric key using the private key in pKeyContext
    static
    void QCALLTYPE DecryptKey(__in CRYPT_KEY_CTX *pKeyContext,
                              __in_bcount(cbEncryptedKey) BYTE *pbEncryptedKey,
                              DWORD cbEncryptedKey,
                              BOOL fOAEP,
                              QCall::ObjectHandleOnStack ohRetDecryptedKey);

    // Encrypt a symmetric key using the public key in pKeyContext
    static
    void QCALLTYPE EncryptKey(__in CRYPT_KEY_CTX *pKeyContext,
                              __in_bcount(cbKey) BYTE *pbKey,
                              DWORD cbKey,
                              BOOL fOAEP,
                              QCall::ObjectHandleOnStack ohRetEncryptedKey);

    //
    // SafeHandle release QCALLS
    //
#endif // FEATURE_CRYPTO
    // Release our handle to a CSP, potentially deleting the referenced key.
    static
    void QCALLTYPE FreeCsp(__in_opt CRYPT_PROV_CTX *pProviderContext);

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
    // Release our handle to a hash, potentially also releasing the provider
    static
    void QCALLTYPE FreeHash(__in_opt CRYPT_HASH_CTX *pHashContext);

    // Release our handle to a key, potentially also releasing the provider
    static
    void QCALLTYPE FreeKey(__in_opt CRYPT_KEY_CTX *pKeyContext);

    //
    // Util QCALLS
    //

    static
    CRYPT_HASH_CTX * CreateHash(CRYPT_PROV_CTX * pProvCtx, DWORD dwHashType);

    static
    void QCALLTYPE DeriveKey(CRYPT_PROV_CTX * pProvCtx, DWORD dwCalgKey, DWORD dwCalgHash, 
                             LPCBYTE pbPwd, DWORD cbPwd, DWORD dwFlags, LPBYTE pbIVIn, DWORD cbIVIn,
                             QCall::ObjectHandleOnStack retKey);

    static
    void QCALLTYPE EndHash(CRYPT_HASH_CTX * pHashCtx, QCall::ObjectHandleOnStack retHash);

    static
    void QCALLTYPE ExportCspBlob(CRYPT_KEY_CTX * pKeyCtx, DWORD dwBlobType, QCall::ObjectHandleOnStack retBlob);
#endif // FEATURE_CRYPTO

    static
    void QCALLTYPE GetBytes(CRYPT_PROV_CTX * pProvCtx, BYTE * pbOut, INT32 cb);

    static
    void QCALLTYPE GetNonZeroBytes(CRYPT_PROV_CTX * pProvCtx, BYTE * pbOut, INT32 cb);

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
    static
    BOOL QCALLTYPE GetPersistKeyInCsp(CRYPT_PROV_CTX * pProvCtx);

    static
    void QCALLTYPE HashData(CRYPT_HASH_CTX * pHashCtx, LPCBYTE pData, DWORD cbData, DWORD dwStart, DWORD dwSize);

    static
    BOOL QCALLTYPE SearchForAlgorithm(CRYPT_PROV_CTX * pProvCtx, DWORD dwAlgID, DWORD dwKeyLength);

    static
    void QCALLTYPE SetKeyParamDw(CRYPT_KEY_CTX * pKeyCtx, DWORD dwParam, DWORD dwValue);

    static
    void QCALLTYPE SetKeyParamRgb(CRYPT_KEY_CTX * pKeyCtx, DWORD dwParam, LPCBYTE pValue, DWORD cbValue);

    static
    DWORD QCALLTYPE SetKeySetSecurityInfo(CRYPT_PROV_CTX * pProvCtx, DWORD dwSecurityInformation, LPCBYTE pSecurityDescriptor);

    static
    void QCALLTYPE SetPersistKeyInCsp(CRYPT_PROV_CTX * pProvCtx, BOOL fPersistKeyInCsp);

    static
    void QCALLTYPE SetProviderParameter(CRYPT_PROV_CTX * pProvCtx, DWORD dwKeySpec, DWORD dwProvParam, INT_PTR pbData);

    static
    void QCALLTYPE SignValue(CRYPT_KEY_CTX * pKeyCtx, DWORD dwKeySpec, DWORD dwCalgKey, DWORD dwCalgHash, 
                             LPCBYTE pbHash, DWORD cbHash, QCall::ObjectHandleOnStack retSignature);

    static 
    BOOL QCALLTYPE VerifySign(CRYPT_KEY_CTX * pKeyCtx, DWORD dwCalgKey, DWORD dwCalgHash, 
                              LPCBYTE pbHash, DWORD cbHash, LPCBYTE pbSignature, DWORD cbSignature);

#endif // FEATURE_CRYPTO

public:
    //
    // FCalls from System.Security.Cryptography.Utils
    //

    static FCDECL2(void, _AcquireCSP, Object* cspParametersUNSAFE, SafeHandle** hProvUNSAFE);
    static FCDECL3(HRESULT, _OpenCSP, Object* cspParametersUNSAFE, DWORD dwFlags, SafeHandle** hProvUNSAFE);
    static FCDECL0(StringObject*, _GetRandomKeyContainer);
    static LPCWSTR GetDefaultProvider(DWORD dwType);    

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
    static FCDECL3(void, _CreateCSP, Object* cspParametersUNSAFE, CLR_BOOL randomKeyContainer, SafeHandle** hProvUNSAFE);
    static FCDECL8(DWORD, _DecryptData, SafeHandle* hKeyUNSAFE, U1Array* dataUNSAFE, INT32 dwOffset, INT32 dwCount, U1Array** outputUNSAFE, INT32 dwOutputOffset, DWORD dwPaddingMode, CLR_BOOL fLast);
    static FCDECL8(DWORD, _EncryptData, SafeHandle* hKeyUNSAFE, U1Array* dataUNSAFE, INT32 dwOffset, INT32 dwCount, U1Array** outputUNSAFE, INT32 dwOutputOffset, DWORD dwPaddingMode, CLR_BOOL fLast);
    static FCDECL3(void, _ExportKey, SafeHandle* hKeyUNSAFE, DWORD dwBlobType, Object* theKeyUNSAFE);
    static FCDECL5(void, _GenerateKey, SafeHandle* hProvUNSAFE, DWORD dwCalg, DWORD dwFlags, DWORD dwKeySize, SafeHandle** hKeyUNSAFE);
    static FCDECL0(FC_BOOL_RET, _GetEnforceFipsPolicySetting);
    static FCDECL2(U1Array*, _GetKeyParameter, SafeHandle* hKeyUNSAFE, DWORD dwKeyParam);
    static FCDECL3(U1Array*, _GetKeySetSecurityInfo, SafeHandle* hProvUNSAFE, DWORD dwSecurityInformation, DWORD* pdwErrorCode);
    static FCDECL3(Object*, _GetProviderParameter, SafeHandle* hKeyUNSAFE, DWORD dwKeySpec, DWORD dwKeyParam);
    static FCDECL3(HRESULT, _GetUserKey, SafeHandle* hProvUNSAFE, DWORD dwKeySpec, SafeHandle** hKeyUNSAFE);
    static FCDECL5(void, _ImportBulkKey, SafeHandle* hProvUNSAFE, DWORD dwCalg, CLR_BOOL useSalt, U1Array* rgbKeyUNSAFE, SafeHandle** hKeyUNSAFE);
    static FCDECL4(DWORD, _ImportCspBlob, U1Array* rawDataUNSAFE, SafeHandle* hProvUNSAFE, DWORD dwFlags, SafeHandle** hKeyUNSAFE);
    static FCDECL5(void, _ImportKey, SafeHandle* hProvUNSAFE, DWORD dwCalg, DWORD dwFlags, Object* refKeyUNSAFE, SafeHandle** hKeyUNSAFE);
    static FCDECL0(FC_BOOL_RET, _ProduceLegacyHMACValues);
#endif // FEATURE_CRYPTO

private:
    static HRESULT OpenCSP(OBJECTREF * pSafeThis, DWORD dwFlags, CRYPT_PROV_CTX * pProvCtxStruct);
    static DWORD MapCspProviderFlags (DWORD dwFlags);

#if defined(FEATURE_CRYPTO) || defined(FEATURE_LEGACYNETCFCRYPTO)
    static HRESULT MSProviderCryptImportKey(HCRYPTPROV hProv, LPBYTE rgbSymKey, DWORD cbSymKey, DWORD dwFlags, HCRYPTKEY * phkey);
    static HRESULT ExponentOfOneImport(HCRYPTPROV hProv, LPBYTE rgbKeyMaterial, DWORD cbKeyMaterial, DWORD dwKeyAlg, DWORD dwFlags, HCRYPTKEY * phkey);
    static HRESULT PlainTextKeyBlobImport(HCRYPTPROV hProv, LPBYTE rgbKeyMaterial, DWORD cbKeyMaterial, DWORD dwKeyAlg, DWORD dwFlags, HCRYPTKEY * phkey);
    static HRESULT LoadKey(LPBYTE rgbKeyMaterial, DWORD cbKeyMaterial, HCRYPTPROV hprov, DWORD dwCalg, DWORD dwFlags, HCRYPTKEY * phkey);
    static HRESULT UnloadKey(HCRYPTPROV hprov, HCRYPTKEY hkey, LPBYTE * ppb, DWORD * pcb);
    static inline DWORD ConvertByteArrayToDWORD (LPBYTE pb, DWORD cb);
    static inline void ConvertIntToByteArray(DWORD dwInput, LPBYTE * ppb, DWORD * pcb);
    static DWORD MapCspKeyFlags (DWORD dwFlags);
#endif //FEATURE_CRYPTO    

}; // class COMCryptography

// @telesto - with talk of registry access, sounds like this should be #ifdef out from Telesto?
//---------------------------------------------------------------------------------------
//
// Cache of CSP data we've already looked up in the registry
//
// Notes:
//    This cache is thread safe. If a CSP is not stored in the cache, it will
//    return NULL rather than throwing. Attempting to store multiple CSPs with
//    the same type will result in only the first CSP being stored.
//

class ProviderCache
{
public:
    // Associate a name with a CSP type
    static void CacheProvider(DWORD dwType, __in_z LPWSTR pwzProvider);

    // Get the name that's associated with the given type, NULL if there is no association setup
    static LPCWSTR GetProvider(DWORD dwType);

private:
    // The largest type of CSP that Windows defines. This should be updated as new CSP types are defined
    static const DWORD  MaxWindowsProviderType = PROV_RSA_AES;

    static ProviderCache    *s_pCache;      // singleton cache instance

    EEIntHashTable          m_htCache;      // Mapping between cached provider types and CSP names
    Crst                    m_crstCache;    // Lock guarding access to m_htCache

    ProviderCache();

    void InternalCacheProvider(DWORD dwType, __in_z LPWSTR pwzProvider);
    LPCWSTR InternalGetProvider(DWORD dwType);
};
#endif // FEATURE_CRYPTO -- review flags

#endif // !_CRYPTOGRPAPHY_H_
