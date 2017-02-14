// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#include "common.h"

#include "security.h"
#include "field.h"
#include "comcallablewrapper.h"
#include "typeparse.h"
#include "appdomain.inl"
#include "mdaassistants.h"
#include "fstring.h"


HRESULT BlobToAttributeSet(BYTE* pBuffer, ULONG cbBuffer, CORSEC_ATTRSET* pAttrSet, DWORD dwAction);

#ifndef CROSSGEN_COMPILE

//
// Determine if a security action allows an optimization where an empty permission set can be represented as
// NULL. Some VM optimizations kick in if an empty permission set can be represented as NULL; however since
// some security actions have a semantic difference between not being specified at all and having an explicit
// empty permission set specified, permission sets associated with those actions must be represented as an
// empty object rather than as NULL.
//
// Arguments:
//    action - security action to check
//
// Return Value:
//    true if the security action may have an empty permission set optimized to NULL, false otherwise
//
// Notes:
//   The security actions which cannot have NULL represent an empty permission set are:
// 
//     * PermitOnly      - a PermitOnly set containing no permissions means that all demands should fail, as
//                         opposed to not having a PermitOnly set on a method.
//     * RequestOptional - not specifying a RequestOptional set is equivilent to having a RequestOptional set
//                         of FullTrust, rather than having an empty RequestOptional set.
//

// static
bool SecurityAttributes::ActionAllowsNullPermissionSet(CorDeclSecurity action)
{
    LIMITED_METHOD_CONTRACT;
    return action != dclPermitOnly && action != dclRequestOptional;
}

void SecurityAttributes::CopyEncodingToByteArray(IN PBYTE   pbData,
                                                IN DWORD   cbData,
                                                OUT OBJECTREF* pArray)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    U1ARRAYREF pObj;
    _ASSERTE(pArray);

    pObj = (U1ARRAYREF)AllocatePrimitiveArray(ELEMENT_TYPE_U1,cbData);          
    memcpyNoGCRefs(pObj->m_Array, pbData, cbData);
    *pArray = (OBJECTREF) pObj;        
}

void SecurityAttributes::CopyByteArrayToEncoding(IN U1ARRAYREF* pArray,
                                             OUT PBYTE*   ppbData,
                                             OUT DWORD*   pcbData)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pArray));
        PRECONDITION(CheckPointer(ppbData));
        PRECONDITION(CheckPointer(pcbData));
        PRECONDITION(*pArray != NULL);
    } CONTRACTL_END;

    DWORD size = (DWORD) (*pArray)->GetNumComponents();
    *ppbData = new BYTE[size];
    *pcbData = size;
        
    CopyMemory(*ppbData, (*pArray)->GetDirectPointerToNonObjectElements(), size);
}

//
// This is a public exported method
//

// Translate a set of security custom attributes into a serialized permission set blob.
HRESULT STDMETHODCALLTYPE TranslateSecurityAttributes(CORSEC_ATTRSET    *pAttrSet,
                            BYTE          **ppbOutput,
                            DWORD          *pcbOutput,
                            BYTE          **ppbNonCasOutput,
                            DWORD          *pcbNonCasOutput,
                            DWORD          *pdwErrorIndex)
{
    return E_NOTIMPL;
}


//
// This is a public exported method
//

// Reads permission requests (if any) from the manifest of an assembly.
HRESULT STDMETHODCALLTYPE GetPermissionRequests(LPCWSTR   pwszFileName,
                      BYTE    **ppbMinimal,
                      DWORD    *pcbMinimal,
                      BYTE    **ppbOptional,
                      DWORD    *pcbOptional,
                      BYTE    **ppbRefused,
                      DWORD    *pcbRefused)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        IMetaDataDispenser         *pMD = NULL;
        IMetaDataAssemblyImport    *pMDAsmImport = NULL;
        IMetaDataImport            *pMDImport = NULL;
        mdAssembly                  mdAssembly;
        BYTE                       *pbMinimal = NULL;
        DWORD                       cbMinimal = 0;
        BYTE                       *pbOptional = NULL;
        DWORD                       cbOptional = 0;
        BYTE                       *pbRefused = NULL;
        DWORD                       cbRefused = 0;
        HCORENUM                    hEnumDcl = NULL;
        mdPermission                rPSets[dclMaximumValue + 1];
        DWORD                       dwSets;
        DWORD                       i;

        *ppbMinimal = NULL;
        *pcbMinimal = 0;
        *ppbOptional = NULL;
        *pcbOptional = 0;
        *ppbRefused = NULL;
        *pcbRefused = 0;

        // Get the meta data interface dispenser.
        hr = MetaDataGetDispenser(CLSID_CorMetaDataDispenser,
                                  IID_IMetaDataDispenserEx,
                                  (void **)&pMD);
        if (FAILED(hr))
            goto Error;

        // Open a scope on the assembly file.
        hr = pMD->OpenScope(pwszFileName,
                            0,
                            IID_IMetaDataAssemblyImport,
                            (IUnknown**)&pMDAsmImport);
        if (FAILED(hr))
            goto Error;

        // Determine the assembly token.
        hr = pMDAsmImport->GetAssemblyFromScope(&mdAssembly);
        if (FAILED(hr))
            goto Error;

        // QI for a normal import interface.
        hr = pMDAsmImport->QueryInterface(IID_IMetaDataImport, (void**)&pMDImport);
        if (FAILED(hr))
            goto Error;

        // Look for permission request sets hung off the assembly token.
        hr = pMDImport->EnumPermissionSets(&hEnumDcl,
                                           mdAssembly,
                                           dclActionNil,
                                           rPSets,
                                           dclMaximumValue + 1,
                                           &dwSets);
        if (FAILED(hr))
            goto Error;

        for (i = 0; i < dwSets; i++) {
            BYTE   *pbData;
            DWORD   cbData;
            DWORD   dwAction;

            pMDImport->GetPermissionSetProps(rPSets[i],
                                             &dwAction,
                                             (void const **)&pbData,
                                             &cbData);

            switch (dwAction) {
            case dclRequestMinimum:
                _ASSERTE(pbMinimal == NULL);
                pbMinimal = pbData;
                cbMinimal = cbData;
                break;
            case dclRequestOptional:
                _ASSERTE(pbOptional == NULL);
                pbOptional = pbData;
                cbOptional = cbData;
                break;
            case dclRequestRefuse:
                _ASSERTE(pbRefused == NULL);
                pbRefused = pbData;
                cbRefused = cbData;
                break;
            default:
                _ASSERTE(FALSE);
            }
        }

        pMDImport->CloseEnum(hEnumDcl);

        // Buffer the results (since we're about to close the metadata scope and
        // lose the original data).
        if (pbMinimal) {
            *ppbMinimal = new (nothrow) BYTE[cbMinimal];
            if (*ppbMinimal == NULL) {
                hr = E_OUTOFMEMORY;
                goto Error;
            }
            memcpy(*ppbMinimal, pbMinimal, cbMinimal);
            *pcbMinimal = cbMinimal;
        }

        if (pbOptional) {
            *ppbOptional = new (nothrow) BYTE[cbOptional];
            if (*ppbOptional == NULL) {
                hr = E_OUTOFMEMORY;
                goto Error;
            }
            memcpy(*ppbOptional, pbOptional, cbOptional);
            *pcbOptional = cbOptional;
        }

        if (pbRefused) {
            *ppbRefused = new (nothrow) BYTE[cbRefused];
            if (*ppbRefused == NULL) {
                hr = E_OUTOFMEMORY;
                goto Error;
            }
            memcpy(*ppbRefused, pbRefused, cbRefused);
            *pcbRefused = cbRefused;
        }

    Error:
        if (pMDImport)
            pMDImport->Release();
        if (pMDAsmImport)
            pMDAsmImport->Release();
        if (pMD)
            pMD->Release();
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// Load permission requests in their serialized form from assembly metadata.
// This consists of a required permissions set and optionally an optional and
// deny permission set.
void SecurityAttributes::LoadPermissionRequestsFromAssembly(IN IMDInternalImport*     pImport,
                                                            OUT OBJECTREF*   pReqdPermissions,
                                                            OUT OBJECTREF*   pOptPermissions,
                                                            OUT OBJECTREF*   pDenyPermissions)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pReqdPermissions));
        PRECONDITION(CheckPointer(pOptPermissions));
        PRECONDITION(CheckPointer(pDenyPermissions));
    } CONTRACTL_END;

    mdAssembly          mdAssembly;
    HRESULT             hr;

    *pReqdPermissions = NULL;
    *pOptPermissions = NULL;
    *pDenyPermissions = NULL;

    // It's OK to be called with a NULL assembly. This can happen in the code
    // path where we're just checking for a signature, nothing else. So just
    // return without doing anything.
    if (pImport == NULL)
        return;

    // Locate assembly metadata token since the various permission sets are
    // written as custom values against this token.
    if (pImport->GetAssemblyFromScope(&mdAssembly) != S_OK) {
        _ASSERT(FALSE);
        return;
    }

    struct _gc
    {
        OBJECTREF reqdPset;
        OBJECTREF optPset;
        OBJECTREF denyPset;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
                
    {
        GCX_COOP(); // because GetDeclaredPermissions may call into managed code
        GCPROTECT_BEGIN(gc);

        // Read and translate required permission set.
        hr = Security::GetDeclaredPermissions(pImport, mdAssembly, dclRequestMinimum, &gc.reqdPset, NULL);
        _ASSERT(SUCCEEDED(hr) || (hr == CLDB_E_RECORD_NOTFOUND));

        // Now the optional permission set.
        PsetCacheEntry *pOptPSCacheEntry = NULL;
        hr = Security::GetDeclaredPermissions(pImport, mdAssembly, dclRequestOptional, &gc.optPset, &pOptPSCacheEntry);
        _ASSERT(SUCCEEDED(hr) || (hr == CLDB_E_RECORD_NOTFOUND));

        // An empty permission set has semantic meaning if it is an assembly's optional permission set. 
        // If we have an optional set, then we need to make sure it is created.
        if (SUCCEEDED(hr) && gc.optPset == NULL && pOptPSCacheEntry != NULL)
        {
            gc.optPset = pOptPSCacheEntry->CreateManagedPsetObject(dclRequestOptional, /* createEmptySet */ true);
        }

        // And finally the refused permission set.
        hr = Security::GetDeclaredPermissions(pImport, mdAssembly, dclRequestRefuse, &gc.denyPset, NULL);
        _ASSERT(SUCCEEDED(hr) || (hr == CLDB_E_RECORD_NOTFOUND));

        *pReqdPermissions = gc.reqdPset;
        *pOptPermissions = gc.optPset;
        *pDenyPermissions = gc.denyPset;

        GCPROTECT_END();
    }
}

// Determine whether a RequestOptional or RequestRefused are made in the assembly manifest.
BOOL SecurityAttributes::RestrictiveRequestsInAssembly(IMDInternalImport* pImport)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    mdAssembly          mdAssembly;
    HRESULT             hr;
    HENUMInternal       hEnumDcl;

    // Locate assembly metadata token since the various permission sets are
    // written as custom values against this token.
    hr = pImport->GetAssemblyFromScope(&mdAssembly);
    if (FAILED(hr))
        return TRUE;

    hr = pImport->EnumPermissionSetsInit(mdAssembly,
                                         dclRequestRefuse,
                                         &hEnumDcl);

    BOOL bFoundRequestRefuse = (hr != CLDB_E_RECORD_NOTFOUND);
    pImport->EnumClose(&hEnumDcl);

    if (bFoundRequestRefuse)
        return TRUE;

    hr = pImport->EnumPermissionSetsInit(mdAssembly,
                                         dclRequestOptional,
                                         &hEnumDcl);
    BOOL bFoundRequestOptional = (hr != CLDB_E_RECORD_NOTFOUND);
    pImport->EnumClose(&hEnumDcl);

    return bFoundRequestOptional;
}
#endif // CROSSGEN_COMPILE

HRESULT SecurityAttributes::GetPermissionsFromMetaData(IN IMDInternalImport *pInternalImport,
                                               IN mdToken token,
                                               IN CorDeclSecurity action,
                                               OUT PBYTE* ppbPerm,
                                               OUT ULONG* pcbPerm)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;
    HRESULT hr = S_OK;
    mdPermission    tkPerm;
    void const **   ppData = const_cast<void const**> (reinterpret_cast<void**> (ppbPerm));
    DWORD dwActionDummy;
    // Get the blob for the CAS action from the security action table in metadata
    HENUMInternalHolder hEnumDcl(pInternalImport);
    if (hEnumDcl.EnumPermissionSetsInit(token,action))
    {
        _ASSERTE(pInternalImport->EnumGetCount(&hEnumDcl) == 1 && "Multiple permissions sets for the same declaration aren't currently supported.");        
        if (pInternalImport->EnumNext(&hEnumDcl, &tkPerm))
        {
            hr = pInternalImport->GetPermissionSetProps(
                    tkPerm,
                    &dwActionDummy,
                    ppData,
                pcbPerm);
            
            if (FAILED(hr) )
            {
                COMPlusThrowHR(hr);
            }
        }
        else
        {
            _ASSERTE(!"At least one enumeration expected");
        }
    }
    else
    {
        hr = CLDB_E_RECORD_NOTFOUND;
    }
    return hr;
}

void SecurityAttributes::CreateAndCachePermissions(
    IN PBYTE pbPerm,
    IN ULONG cbPerm,
    IN CorDeclSecurity action,
    OUT OBJECTREF *pDeclaredPermissions,
    OUT PsetCacheEntry **pPSCacheEntry)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    SecurityDeclarativeCache *pSDC;
    PsetCacheEntry* pPCE;

    pSDC = &(GetAppDomain()->m_pSecContext->m_pSecurityDeclarativeCache);


    pPCE = pSDC->CreateAndCachePset (pbPerm, cbPerm);
    if (pDeclaredPermissions) {
#ifdef CROSSGEN_COMPILE
        _ASSERTE(!"This codepath should be unreachable during crossgen");
        *pDeclaredPermissions = NULL;
#else
        *pDeclaredPermissions = pPCE->CreateManagedPsetObject (action);
#endif
    }
    if (pPSCacheEntry) {
        *pPSCacheEntry = pPCE;
    }
}

// Returns the declared PermissionSet for the specified action type.
HRESULT SecurityAttributes::GetDeclaredPermissions(IN IMDInternalImport *pInternalImport,
                                               IN mdToken token,
                                               IN CorDeclSecurity action,
                                               OUT OBJECTREF *pDeclaredPermissions,
                                               OUT PsetCacheEntry **pPSCacheEntry)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    HRESULT         hr = S_FALSE;
    PBYTE           pbPerm = NULL;
    ULONG           cbPerm = 0;

    

    _ASSERTE(action > dclActionNil && action <= dclMaximumValue);
    
    // Initialize the output parameters.
    if (pDeclaredPermissions)
        *pDeclaredPermissions = NULL;
    if(pPSCacheEntry)
        *pPSCacheEntry = NULL;
    
    bool bCas = !(action == dclNonCasDemand || action == dclNonCasLinkDemand || action == dclNonCasInheritance);

    hr = GetPermissionsFromMetaData(pInternalImport, token, action, &pbPerm, &cbPerm);
    if(pbPerm && cbPerm > 0)
    {
        CreateAndCachePermissions(pbPerm, cbPerm, action, pDeclaredPermissions, pPSCacheEntry);
    }
    else if(!bCas)
    {
        // We're looking for a non-CAS action which may be encoded with the corresponding CAS action
        // Pre-Whidbey, we used to encode CAS and non-CAS actions separately because we used to do 
        // declarative security processing at build time (we used to create a 
        // permset object corresponding to a declarative action, convert it into XML and then store the serialized
        // XML in the assembly). 
        //
        // In Whidbey the default is what we call LAZY declarative security (LAZY_DECL_SEC_FLAG below) - to not do any 
        // declarative security processing at build time (we just take the declarative annotiation and store it as a 
        // serialzied blob - no permsets created/converted to XML). And at runtime, we do the actual processing (create permsets etc.)
        //
        // What does this mean? It means that in Whidbey (and beyond), we cannot tell at build time if it is a declarative CAS action
        // or non-CAS action. So at runtime, we need to check the permset stored under the cas action for a non-CAS action.
        // Of course, we need to do this only if LAZY_DECL_SEC_FLAG is in effect.

        // Determine the corresponding CAS action
        CorDeclSecurity casAction = dclDemand;
        if(action == dclNonCasLinkDemand)
            casAction = dclLinktimeCheck;
        else if(action == dclNonCasInheritance)
            casAction = dclInheritanceCheck;

        // Get the blob for the CAS action from the security action table in metadata
        hr = GetPermissionsFromMetaData(pInternalImport, token, casAction, &pbPerm, &cbPerm);

        if(pbPerm && cbPerm > 0 && pbPerm[0] == LAZY_DECL_SEC_FLAG) // if it's a serialized CORSEC_ATTRSET
        {
            CreateAndCachePermissions(pbPerm, cbPerm, casAction, pDeclaredPermissions, pPSCacheEntry);
        }

    }

    return hr;
}

bool SecurityAttributes::IsHostProtectionAttribute(CORSEC_ATTRIBUTE* pAttr)
{
    static const char s_HostProtectionAttributeName[] = "System.Security.Permissions.HostProtectionAttribute, mscorlib";

    return (strncmp(pAttr->pName, s_HostProtectionAttributeName, sizeof(s_HostProtectionAttributeName)-1) == 0);
}

bool SecurityAttributes::IsBuiltInCASPermissionAttribute(CORSEC_ATTRIBUTE* pAttr)
{
    WRAPPER_NO_CONTRACT;
    static const char s_permissionsNamespace[] = "System.Security.Permissions.";
    if(strncmp(pAttr->pName, s_permissionsNamespace, sizeof(s_permissionsNamespace) - 1) != 0)
        return false; // not built-in permission
    static const char s_principalPermissionName[] = "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib";

    // ASSERT: at this point we know we are in builtin namespace...so compare with PrincipalPermissionAttribute
    if (strncmp(pAttr->pName, s_principalPermissionName, sizeof(s_principalPermissionName)-1) == 0)
        return false; // found a principal permission => Not a built-in CAS permission

    // special-case the unrestricted permission set attribute.
    static const char s_PermissionSetName[] = "System.Security.Permissions.PermissionSetAttribute, mscorlib";
    if (strncmp(pAttr->pName, s_PermissionSetName, sizeof(s_PermissionSetName)-1) == 0)
        return IsUnrestrictedPermissionSetAttribute(pAttr);

    return true; //built-in perm, but not principal perm => IsBuiltInCASPermissionAttribute
}

bool SecurityAttributes::IsUnrestrictedPermissionSetAttribute(CORSEC_ATTRIBUTE* pPerm)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BYTE const * pbBuffer = pPerm->pbValues;
    SIZE_T       cbBuffer = pPerm->cbValues;
    BYTE const * pbBufferEnd = pbBuffer + cbBuffer;

    if (cbBuffer < 2 * sizeof(BYTE))
        return false;

    // Get the field/property specifier
    if (*(BYTE*)pbBuffer == SERIALIZATION_TYPE_FIELD)
        return false;

    _ASSERTE(*(BYTE*)pbBuffer == SERIALIZATION_TYPE_PROPERTY);
    pbBuffer += sizeof(BYTE);
    cbBuffer -= sizeof(BYTE);

    // Get the value type
    DWORD dwType = *(BYTE*)pbBuffer;
    pbBuffer += sizeof(BYTE);
    cbBuffer -= sizeof(BYTE);
    if (dwType != SERIALIZATION_TYPE_BOOLEAN)
        return false;

    // Grab the field/property name and length.
    DWORD cbName;
    BYTE const * pbName;
    if (FAILED(CPackedLen::SafeGetData(pbBuffer,
                                       pbBufferEnd,
                                       &cbName,
                                       &pbName)))
    {
        return false;
    }

    PREFIX_ASSUME(pbName != NULL);

    // SafeGetData will ensure the name is within the buffer
    SIZE_T cbNameOffset = pbName - pbBuffer;
    _ASSERTE(FitsIn<DWORD>(cbNameOffset));
    DWORD dwLength = static_cast<DWORD>(cbNameOffset + cbName);
    pbBuffer += dwLength;
    cbBuffer -= dwLength;

    // Buffer the name of the property and null terminate it.
    DWORD allocLen = cbName + 1;
    if (allocLen < cbName)
        return false;

    LPSTR szName = (LPSTR)_alloca(allocLen);
    memcpy(szName, pbName, cbName);
    szName[cbName] = '\0';

    if (strcmp(szName, "Unrestricted") != 0)
        return false;

    // Make sure the value isn't "false"
    return (*pbBuffer != 0);
}

// This takes a PermissionSetAttribute blob and looks to see if it uses the "FILE" property.  If it
// does, then it loads the file now and modifies the attribute to use the XML property instead
// (because the file may not be available at runtime.)
HRESULT SecurityAttributes::FixUpPermissionSetAttribute(CORSEC_ATTRIBUTE* pPerm)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pPerm->wValues == 1 && strcmp(pPerm->pName, "System.Security.Permissions.PermissionSetAttribute") == 0);
    BYTE const * pbBuffer = pPerm->pbValues;
    SIZE_T       cbBuffer = pPerm->cbValues;
    BYTE const * pbBufferEnd = pbBuffer + cbBuffer;
    HRESULT hr;

    // Check we've got at least the field/property specifier and the
    // type code.
    _ASSERTE(cbBuffer >= (sizeof(BYTE) + sizeof(BYTE)));

    // Grab the field/property specifier.
    bool bIsField = *(BYTE*)pbBuffer == SERIALIZATION_TYPE_FIELD;
    _ASSERTE(bIsField || (*(BYTE*)pbBuffer == SERIALIZATION_TYPE_PROPERTY));
    pbBuffer += sizeof(BYTE);
    cbBuffer -= sizeof(BYTE);

    // Grab the value type.
    DWORD dwType = *(BYTE*)pbBuffer;
    pbBuffer += sizeof(BYTE);
    cbBuffer -= sizeof(BYTE);

    if(bIsField)
        return S_OK;
    if(dwType != SERIALIZATION_TYPE_STRING)
        return S_OK;

    // Grab the field/property name and length.
    ULONG cbName;
    BYTE const * pbName;
    IfFailRet(CPackedLen::SafeGetData(pbBuffer, pbBufferEnd, &cbName, &pbName));
    PREFIX_ASSUME(pbName != NULL);

    // SafeGetData ensures name is within buffer
    SIZE_T cbNameOffset = pbName - pbBuffer;
    _ASSERTE(FitsIn<DWORD>(cbNameOffset));
    DWORD dwLength = static_cast<DWORD>(cbNameOffset + cbName);
    pbBuffer += dwLength;
    cbBuffer -= dwLength;

    // Buffer the name of the property and null terminate it.
    DWORD allocLen = cbName + 1;
    LPSTR szName = (LPSTR)_alloca(allocLen);
    memcpy(szName, pbName, cbName);
    szName[cbName] = '\0';

    if(strcmp(szName, "File") != 0)
        return S_OK;
    if(*pbBuffer == 0xFF) // special case that represents NULL string
        return S_OK;

    IfFailRet(CPackedLen::SafeGetData(pbBuffer, pbBufferEnd, &cbName, &pbName));
    PREFIX_ASSUME(pbName != NULL);    

    // SafeGetData ensures name is within buffer
    cbNameOffset = pbName - pbBuffer;
    _ASSERTE(FitsIn<DWORD>(cbNameOffset));
    dwLength = static_cast<DWORD>(cbNameOffset + cbName);
    _ASSERTE(cbBuffer >= dwLength);

    // Open the file
    MAKE_WIDEPTR_FROMUTF8N(wszFileName, (LPCSTR)pbName, cbName);
    HandleHolder hFile(WszCreateFile (wszFileName,
                                        GENERIC_READ,
                                        FILE_SHARE_READ,
                                        NULL,
                                        OPEN_EXISTING,
                                        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                        NULL));
    if (hFile == INVALID_HANDLE_VALUE)
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    DWORD dwFileLen = SafeGetFileSize(hFile, 0);
    if (dwFileLen == 0xFFFFFFFF)
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);

    // Read the file
    BYTE* pFileBuffer = new (nothrow) BYTE[(dwFileLen + 4) * sizeof(BYTE)];
    if(!pFileBuffer)
        return E_OUTOFMEMORY;
    DWORD dwBytesRead;
    if ((SetFilePointer(hFile, 0, NULL, FILE_BEGIN) == 0xFFFFFFFF) ||
        (!ReadFile(hFile, pFileBuffer, dwFileLen, &dwBytesRead, NULL)))
    {
        delete [] pFileBuffer;
        return E_FAIL;
    }
    if(dwBytesRead < dwFileLen)
    {
        delete [] pFileBuffer;
        return E_FAIL;
    }

    // Make the new attribute blob
    BYTE* pNewAttrBuffer = new (nothrow) BYTE[(dwFileLen + 10) * 2 * sizeof(BYTE)];
    if(!pNewAttrBuffer)
        return E_OUTOFMEMORY;
    BYTE* pCurBuf = pNewAttrBuffer;
    *pCurBuf = (BYTE)SERIALIZATION_TYPE_PROPERTY;
    pCurBuf++;
    *pCurBuf = (BYTE)SERIALIZATION_TYPE_STRING;
    pCurBuf++;
    pCurBuf = (BYTE*)CPackedLen::PutLength(pCurBuf, 3);
    memcpy(pCurBuf, "Hex", 3);
    pCurBuf += 3;
    pCurBuf = (BYTE*)CPackedLen::PutLength(pCurBuf, dwFileLen * 2);
    DWORD n;
    BYTE b;
    for(n = 0; n < dwFileLen; n++)
    {
        b = (pFileBuffer[n] >> 4) & 0xf;
        *pCurBuf = (b < 10 ? '0' + b : 'a' + b - 10);
        pCurBuf++;
        b = pFileBuffer[n] & 0xf;
        *pCurBuf = (b < 10 ? '0' + b : 'a' + b - 10);
        pCurBuf++;
    }
    delete [] pFileBuffer;

    // We shouldn't have a serialized permission set that can be this large, but to be safe we'll ensure
    // that we fit in the output DWORD size.
    SIZE_T cbNewAttrSize = pCurBuf - pNewAttrBuffer;

    // Set the new values
    delete(pPerm->pbValues);
    pPerm->pbValues = pNewAttrBuffer;
    pPerm->cbValues = cbNewAttrSize;
    return S_OK;
}

// if tkAssemblyRef is NULL, this assumes the type is in this assembly
// uszClassName should be a UTF8 string including both namespace and class
HRESULT GetFullyQualifiedTypeName(SString* pString, mdAssemblyRef tkAssemblyRef, __in_z CHAR* uszClassName, IMetaDataAssemblyImport *pImport, mdToken tkCtor)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Add class name
    MAKE_WIDEPTR_FROMUTF8(wszClassName, uszClassName);
    (*pString) += (LPCWSTR) wszClassName;
    if(IsNilToken(tkAssemblyRef))
        tkAssemblyRef = TokenFromRid(1, mdtAssembly);

    // Add a comma separator
    (*pString) += W(", ");

    DWORD dwDisplayFlags = ASM_DISPLAYF_VERSION | ASM_DISPLAYF_PUBLIC_KEY_TOKEN | ASM_DISPLAYF_CULTURE;
    HRESULT hr;
    AssemblySpec spec;
    StackSString name;

    IfFailRet(spec.Init((mdToken)tkAssemblyRef,pImport));
    spec.GetFileOrDisplayName(dwDisplayFlags,name);
    _ASSERTE(!name.IsEmpty() && "the assembly name should not be empty here");

    (*pString) += name;
    return S_OK;
}

HRESULT SecurityAttributes::SerializeAttribute(CORSEC_ATTRIBUTE* pAttr, BYTE* pBuffer, SIZE_T* pCount, IMetaDataAssemblyImport *pImport)
{
    // pBuffer can be NULL if the caller is only trying to determine the size of the serialized blob.  In that case, let's make a little temp buffer to facilitate CPackedLen::PutLength
    SIZE_T cbPos = *pCount;
    BYTE* pTempBuf = pBuffer;
    SIZE_T const* pTempPos = &cbPos;
    BYTE tempBuf[8];
    const SIZE_T zero = 0;
    if(!pTempBuf)
    {
        pTempBuf = tempBuf;
        pTempPos = &zero;
    }
    BYTE* pOldPos;

    // Get the fully qualified type name
    SString sType;
    HRESULT hr = GetFullyQualifiedTypeName(&sType, pAttr->tkAssemblyRef, pAttr->pName, pImport, pAttr->tkCtor);
    if(FAILED(hr))
        return hr;

    // Convert assembly name to UTF8.
    const WCHAR* wszTypeName = sType.GetUnicode();
    MAKE_UTF8PTR_FROMWIDE(uszTypeName, wszTypeName);
    DWORD dwUTF8TypeNameLen = (DWORD)strlen(uszTypeName);

    // Serialize the type name length
    pOldPos = &pTempBuf[*pTempPos];
    cbPos += (BYTE*)CPackedLen::PutLength(&pTempBuf[*pTempPos], dwUTF8TypeNameLen) - pOldPos;

    // Serialize the type name
    if(pBuffer)
        memcpy(&pBuffer[cbPos], uszTypeName, dwUTF8TypeNameLen);
    cbPos += dwUTF8TypeNameLen;

    // Serialize the size of the properties blob
    BYTE temp[32];
    SIZE_T cbSizeOfCompressedPropertiesCount = (BYTE*)CPackedLen::PutLength(temp, pAttr->wValues) - temp;
    pOldPos = &pTempBuf[*pTempPos];

    _ASSERTE(FitsIn<ULONG>(pAttr->cbValues + cbSizeOfCompressedPropertiesCount));
    ULONG propertiesLength = static_cast<ULONG>(pAttr->cbValues + cbSizeOfCompressedPropertiesCount);
    cbPos += (BYTE*)CPackedLen::PutLength(&pTempBuf[*pTempPos], propertiesLength) - pOldPos;

    // Serialize the count of properties
    pOldPos = &pTempBuf[*pTempPos];
    cbPos += (BYTE*)CPackedLen::PutLength(&pTempBuf[*pTempPos], pAttr->wValues) - pOldPos;

    // Serialize the properties blob
    if(pBuffer)
        memcpy(&pBuffer[cbPos], pAttr->pbValues, pAttr->cbValues);
    cbPos += pAttr->cbValues;

    *pCount = cbPos;
    return hr;
}

HRESULT SecurityAttributes::DeserializeAttribute(CORSEC_ATTRIBUTE *pAttr, BYTE* pBuffer, ULONG cbBuffer, SIZE_T* pPos)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    HRESULT hr;

    // Deserialize the size of the type name
    BYTE* pClassName;
    ULONG dwClassNameSize;
    BYTE* pBufferEnd = pBuffer + cbBuffer;
    IfFailRet(CPackedLen::SafeGetData((BYTE const *)&pBuffer[*pPos],
                                      (BYTE const *)pBufferEnd,
                                      &dwClassNameSize,
                                      (BYTE const **)&pClassName));
    (*pPos) += pClassName - &pBuffer[*pPos];

    // Deserialize the type name
    (*pPos) += dwClassNameSize;
    pAttr->pName = new (nothrow) CHAR[dwClassNameSize + 1];
    if(!pAttr->pName)
        return E_OUTOFMEMORY;
    memcpy(pAttr->pName, pClassName, dwClassNameSize);
    pAttr->pName[dwClassNameSize] = '\0';

    // Deserialize the CA blob size
    BYTE* pCABlob;
    ULONG cbCABlob;
    IfFailRet(CPackedLen::SafeGetData((BYTE const *)&pBuffer[*pPos],
                                      (BYTE const *)pBufferEnd,
                                      &cbCABlob,
                                      (BYTE const **)&pCABlob));

    (*pPos) += pCABlob - &pBuffer[*pPos];

    // Deserialize the CA blob value count
    BYTE* pCABlobValues;
    ULONG cCABlobValues;
    IfFailRet(CPackedLen::SafeGetLength((BYTE const *)&pBuffer[*pPos],
                                        (BYTE const *)pBufferEnd,
                                        &cCABlobValues,
                                        (BYTE const **)&pCABlobValues));

    (*pPos) += pCABlobValues - &pBuffer[*pPos];
    if (!FitsIn<WORD>(cCABlobValues))
        return COR_E_OVERFLOW;
    pAttr->wValues = static_cast<WORD>(cCABlobValues);

    // We know that pCABlobValues - pCABlob will be a positive result.
    if (cbCABlob < (ULONG)(pCABlobValues - pCABlob))
        return COR_E_OVERFLOW;

    pAttr->cbValues = cbCABlob - (pCABlobValues - pCABlob);

    // Deserialize the CA blob
    pAttr->pbValues = new (nothrow) BYTE[pAttr->cbValues];
    if(!pAttr->pbValues)
        return E_OUTOFMEMORY;
    memcpy(pAttr->pbValues, pCABlobValues, pAttr->cbValues);

    (*pPos) += pAttr->cbValues;

    return S_OK;
}

HRESULT AttributeSetToBlob(CORSEC_ATTRSET* pAttrSet, BYTE* pBuffer, SIZE_T* pCount, IMetaDataAssemblyImport *pImport, DWORD dwAction)
{
    STANDARD_VM_CONTRACT;

    // pBuffer can be NULL if the caller is only trying to determine the size of the serialized blob.  In that case, let's make a little temp buffer to facilitate CPackedLen::PutLength
    SIZE_T cbPos = 0;
    BYTE* pTempBuf = pBuffer;
    SIZE_T const *pTempPos = &cbPos;
    BYTE tempBuf[8];
    const SIZE_T zero = 0;
    if(!pTempBuf)
    {
        pTempBuf = tempBuf;
        pTempPos = &zero;
    }
    BYTE* pOldPos;
    HRESULT hr = S_OK;

    // Serialize a LAZY_DECL_SEC_FLAG to identify the blob format (as opposed to '<' which would indicate the older XML format)
    if(pBuffer)
        pBuffer[cbPos] = LAZY_DECL_SEC_FLAG;
    cbPos++;

    // Serialize the attribute count
    pOldPos = &pTempBuf[*pTempPos];
    cbPos += (BYTE*)CPackedLen::PutLength(&pTempBuf[*pTempPos], pAttrSet->dwAttrCount) - pOldPos;

    // Serialize the attributes
    DWORD i;
    for(i = 0; i < pAttrSet->dwAttrCount; i++)
    {
        // Get the attribute
        CORSEC_ATTRIBUTE *pAttr = &pAttrSet->pAttrs[i];

        // Perform any necessary fix-ups on it
        if(pAttr->wValues == 1 && strcmp(pAttr->pName, "System.Security.Permissions.PermissionSetAttribute") == 0)
            IfFailGo(SecurityAttributes::FixUpPermissionSetAttribute(pAttr));
        else if((dwAction == dclLinktimeCheck || 
                 dwAction == dclInheritanceCheck) &&
            strcmp(pAttr->pName, "System.Security.Permissions.PrincipalPermissionAttribute") == 0)
        {
            VMPostError(CORSECATTR_E_BAD_NONCAS);
            return CORSECATTR_E_BAD_NONCAS;
        }

        // Serialize it
        SIZE_T dwAttrSize = 0;
        IfFailGo(SecurityAttributes::SerializeAttribute(pAttr, pBuffer ? pBuffer + cbPos : NULL, &dwAttrSize, pImport));
        cbPos += dwAttrSize;
    }
    if(pCount != NULL)
        *pCount = cbPos;

ErrExit:
    if (FAILED(hr))
        VMPostError(CORSECATTR_E_FAILED_TO_CREATE_PERM); // Allows for the correct message to be printed by the compiler

    return hr;
}

HRESULT BlobToAttributeSet(BYTE* pBuffer, ULONG cbBuffer, CORSEC_ATTRSET* pAttrSet, DWORD dwAction)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;
    SIZE_T cbPos = 0;
    BYTE* pBufferEnd = pBuffer + cbBuffer;
    memset(pAttrSet, '\0', sizeof(CORSEC_ATTRSET));
    if (dwAction >= dclDemand && dwAction <= dclRequestRefuse)
        pAttrSet->dwAction = dwAction; // Already lies in the publicly visible range ( values that managed enum SecurityAction can take)
    else
    {
        // Map the action to a publicly visible value
        if (dwAction == dclNonCasDemand)
            pAttrSet->dwAction = dclDemand;    
        else if (dwAction == dclNonCasInheritance)
            pAttrSet->dwAction = dclInheritanceCheck;
        else if (dwAction == dclNonCasLinkDemand)
            pAttrSet->dwAction = dclLinktimeCheck;
        else
        {
            // We have an unexpected security action here.  It would be nice to fail, but for compatibility we need to simply
            // reset the action to Nil.
            pAttrSet->dwAction = dclActionNil;
        }
    }

    // Deserialize the LAZY_DECL_SEC_FLAG to identify serialization of CORSEC_ATTRSET (as opposed to '<' which would indicate a serialized permission as Xml)
    BYTE firstChar = pBuffer[cbPos];
    cbPos++;
    if(firstChar != LAZY_DECL_SEC_FLAG)
        return S_FALSE;

    // Deserialize the attribute count
    BYTE* pBufferNext;
    IfFailRet(CPackedLen::SafeGetLength((BYTE const *)&pBuffer[cbPos],
                                        (BYTE const *)pBufferEnd,
                                        &pAttrSet->dwAttrCount,
                                        (BYTE const **)&pBufferNext));

    cbPos += pBufferNext - &pBuffer[cbPos];
    if(pAttrSet->dwAttrCount > 0)
    {
        pAttrSet->pAttrs = new (nothrow) CORSEC_ATTRIBUTE[pAttrSet->dwAttrCount];
        if(!pAttrSet->pAttrs)
            return E_OUTOFMEMORY;
        pAttrSet->dwAllocated = pAttrSet->dwAttrCount;
    }

    // Deserialize the attributes
    DWORD i;
    for(i = 0; i < pAttrSet->dwAttrCount; i++)
    {
        CORSEC_ATTRIBUTE *pAttr = &pAttrSet->pAttrs[i];
        hr = SecurityAttributes::DeserializeAttribute(pAttr, pBuffer, cbBuffer, &cbPos);
        if(FAILED(hr))
            return hr;
    }

    return S_OK;
}

// This function takes an array of COR_SECATTR (which wrap custom security attribute blobs) and
// converts it to an array of CORSEC_ATTRSET (which contains partially-parsed custom security attribute
// blobs grouped by SecurityAction).  Note that you must delete all the pPermissions that this allocates
// for each COR_SECATTR
HRESULT STDMETHODCALLTYPE GroupSecurityAttributesByAction(
                                CORSEC_ATTRSET /*OUT*/rPermSets[],
                                COR_SECATTR rSecAttrs[],
                                ULONG cSecAttrs,
                                mdToken tkObj,
                                ULONG *pulErrorAttr,
                                CMiniMdRW* pMiniMd,
                                IMDInternalImport* pInternalImport)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DWORD i, j, k;
    DWORD dwAction;
    BYTE* pData = NULL;
    CORSEC_ATTRIBUTE* pPerm;
    mdTypeDef tkParent;
    TypeDefRec* pTypeDefRec;
    MemberRefRec* pMemberRefRec;
    TypeRefRec* pTypeRefRec;
    SIZE_T cbAllocationSize;

    // If you are calling this at compile-time, you should pass in pMiniMd, and pInternalImport should be NULL
    // If you are calling this at run-time, you should pass in pInternalImport, and pMiniMd should be NULL
    _ASSERTE((pMiniMd && !pInternalImport) || (!pMiniMd && pInternalImport));

    // Calculate number and sizes of permission sets to produce. This depends on
    // the security action code encoded as the single parameter to the
    // constructor for each security custom attribute.
    for (i = 0; i < cSecAttrs; i++) 
    {
        if (pulErrorAttr)
            *pulErrorAttr = i;

        // Perform basic validation of the header of each security custom
        // attribute constructor call.
        pData = (BYTE*)rSecAttrs[i].pCustomAttribute;

        // Check minimum length.
        if (rSecAttrs[i].cbCustomAttribute < (sizeof(WORD) + sizeof(DWORD) + sizeof(WORD))) 
        {
            VMPostError(CORSECATTR_E_TRUNCATED);
            IfFailGo(CORSECATTR_E_TRUNCATED);
        }

        // Check version.
        if (GET_UNALIGNED_VAL16(pData) != 1) 
        {
            VMPostError(CORSECATTR_E_BAD_VERSION);
            IfFailGo(CORSECATTR_E_BAD_VERSION);
        }
        pData += sizeof(WORD);

        // Extract and check security action.
        if(pData[2] == SERIALIZATION_TYPE_PROPERTY) // check to see if it's a HostProtection attribute w/o an action
            dwAction = dclLinktimeCheck;
        else
            dwAction = GET_UNALIGNED_VAL32(pData);
        if (dwAction == dclActionNil || dwAction > dclMaximumValue) 
        {
            VMPostError(CORSECATTR_E_BAD_ACTION);
            IfFailGo(CORSECATTR_E_BAD_ACTION);
        }

        // All other declarative security only valid on types and methods.
        if (TypeFromToken(tkObj) == mdtAssembly) 
        {
            // Assemblies can only take permission requests.
            if (dwAction != dclRequestMinimum &&
                dwAction != dclRequestOptional &&
                dwAction != dclRequestRefuse) 
            {
                VMPostError(CORSECATTR_E_BAD_ACTION_ASM);
                IfFailGo(CORSECATTR_E_BAD_ACTION_ASM);
            }
        }
        else if (TypeFromToken(tkObj) == mdtTypeDef || TypeFromToken(tkObj) == mdtMethodDef) 
        {
            // Types and methods can only take declarative security.
            if (dwAction != dclRequest &&
                dwAction != dclDemand &&
                dwAction != dclAssert &&
                dwAction != dclDeny &&
                dwAction != dclPermitOnly &&
                dwAction != dclLinktimeCheck &&
                dwAction != dclInheritanceCheck) 
            {
                VMPostError(CORSECATTR_E_BAD_ACTION_OTHER);
                IfFailGo(CORSECATTR_E_BAD_ACTION_OTHER);
            }
        } 
        else 
        {
            // Permission sets can't be attached to anything else.
            VMPostError(CORSECATTR_E_BAD_PARENT);
            IfFailGo(CORSECATTR_E_BAD_PARENT);
        }

        rPermSets[dwAction].dwAttrCount++;
    }
    
    // Initialize the descriptor for each type of permission set we are going to
    // produce.
    for (i = 0; i <= dclMaximumValue; i++) 
    {
        if (rPermSets[i].dwAttrCount == 0)
            continue;
        
        rPermSets[i].tkObj = tkObj;
        rPermSets[i].dwAction = i;
        rPermSets[i].pImport = NULL;
        rPermSets[i].pAppDomain = NULL;
        rPermSets[i].pAttrs = new (nothrow) CORSEC_ATTRIBUTE[rPermSets[i].dwAttrCount];
        IfNullGo(rPermSets[i].pAttrs);
        
        // Initialize a descriptor for each permission within the permission set.
        for (j = 0, k = 0; j < rPermSets[i].dwAttrCount; j++, k++) 
        {
            // Locate the next security attribute that contributes to this
            // permission set.
            for (; k < cSecAttrs; k++) 
            {
                pData = (BYTE*)rSecAttrs[k].pCustomAttribute;
                if(pData[4] == SERIALIZATION_TYPE_PROPERTY) // check to see if it's a HostProtection attribute w/o an action
                    dwAction = dclLinktimeCheck;
                else
                    dwAction = GET_UNALIGNED_VAL32(pData + sizeof(WORD));
                if (dwAction == i)
                    break;
            }
            _ASSERTE(k < cSecAttrs);
            
            if (pulErrorAttr)
                *pulErrorAttr = k;
            
            // Initialize the permission.
            pPerm = &rPermSets[i].pAttrs[j];
            pPerm->tkCtor = rSecAttrs[k].tkCtor;
            pPerm->dwIndex = k;
            if(pData[4] == SERIALIZATION_TYPE_PROPERTY) // check to see if it's a HostProtection attribute w/o an action
            {
                _ASSERTE(!pPerm->pbValues);
                //pPerm->pbValues = pData + (sizeof (WORD) + sizeof(WORD));
                if (!ClrSafeInt<SIZE_T>::subtraction(rSecAttrs[k].cbCustomAttribute, (sizeof (WORD) + sizeof(WORD)), pPerm->cbValues))
                    return COR_E_OVERFLOW;
                pPerm->wValues = GET_UNALIGNED_VAL16(pData + sizeof (WORD));
                // Prefast overflow sanity check the addition.
                if (!ClrSafeInt<SIZE_T>::addition(pPerm->cbValues, sizeof(WORD), cbAllocationSize))
                    return COR_E_OVERFLOW;
                pPerm->pbValues = new (nothrow) BYTE[cbAllocationSize];
                if(!pPerm->pbValues)
                    return E_OUTOFMEMORY;
                memcpy(pPerm->pbValues, pData + (sizeof (WORD) + sizeof(WORD)), pPerm->cbValues);
            }
            else
            {
                _ASSERTE(!pPerm->pbValues);
                //pPerm->pbValues = pData + (sizeof (WORD) + sizeof(DWORD) + sizeof(WORD));
                if (!ClrSafeInt<SIZE_T>::subtraction(rSecAttrs[k].cbCustomAttribute, (sizeof (WORD) + sizeof (DWORD) + sizeof(WORD)), pPerm->cbValues))
                    return COR_E_OVERFLOW;
                pPerm->wValues = GET_UNALIGNED_VAL16(pData + sizeof (WORD) + sizeof(DWORD));
                // Prefast overflow sanity check the addition.
                if (!ClrSafeInt<SIZE_T>::addition(pPerm->cbValues, sizeof(WORD), cbAllocationSize))
                    return COR_E_OVERFLOW;
                pPerm->pbValues = new (nothrow) BYTE[cbAllocationSize];
                if(!pPerm->pbValues)
                    return E_OUTOFMEMORY;
                memcpy(pPerm->pbValues, pData + (sizeof (WORD) + sizeof(DWORD) + sizeof(WORD)), pPerm->cbValues);
            }
            
            CQuickBytes qbFullName;
            CHAR* szFullName = NULL;
            
            LPCSTR szTypeName;
            LPCSTR szTypeNamespace;
            
            // Follow the security custom attribute constructor back up to its
            // defining assembly (so we know how to load its definition). If the
            // token resolution scope is not defined, it's assumed to be
            // mscorlib.
            if (TypeFromToken(rSecAttrs[k].tkCtor) == mdtMethodDef) 
            {
                if (pMiniMd != NULL)
                {
                    // scratch buffer for full type name
                    szFullName = (CHAR*) qbFullName.AllocNoThrow((MAX_CLASSNAME_LENGTH+1) * sizeof(CHAR));
                    if(szFullName == NULL)
                        return E_OUTOFMEMORY;
                    
                    // grab the type that contains the security attribute constructor
                    IfFailGo(pMiniMd->FindParentOfMethodHelper(rSecAttrs[k].tkCtor, &tkParent));
                    
                    // scratch buffer for nested type names
                    CQuickBytes qbBuffer;
                    CHAR* szBuffer;
                    
                    CHAR* szName = NULL;
                    BOOL fFirstLoop = TRUE;
                    pTypeDefRec = NULL;
                    do
                    {
                        // get outer type name
                        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeDefRec));
                        IfFailGo(pMiniMd->getNameOfTypeDef(pTypeDefRec, (LPCSTR *)&szName));
                        
                        // If this is the first time through the loop, just assign values, otherwise build nested type name.
                        if (!fFirstLoop)
                        {
                            szBuffer = (CHAR*) qbBuffer.AllocNoThrow((MAX_CLASSNAME_LENGTH+1) * sizeof(CHAR));
                            if(szBuffer == NULL)
                                return E_OUTOFMEMORY;
                            
                            ns::MakeNestedTypeName(szBuffer, (MAX_CLASSNAME_LENGTH+1) * sizeof(CHAR), szName, szFullName);
                            szName = szBuffer;
                        } 
                        else
                        {
                            fFirstLoop = FALSE;
                        }
                        
                        // copy into buffer
                        size_t localLen = strlen(szName) + 1;
                        strcpy_s(szFullName, localLen, szName);
                        
                        // move to next parent
                        DWORD dwFlags = pMiniMd->getFlagsOfTypeDef(pTypeDefRec);
                        if (IsTdNested(dwFlags))
                        {
                            RID ridNestedRec;
                            IfFailGo(pMiniMd->FindNestedClassHelper(tkParent, &ridNestedRec));
                            _ASSERTE(!InvalidRid(ridNestedRec));
                            NestedClassRec *pNestedRec;
                            IfFailGo(pMiniMd->GetNestedClassRecord(ridNestedRec, &pNestedRec));
                            tkParent = pMiniMd->getEnclosingClassOfNestedClass(pNestedRec);
                        }
                        else
                        {
                            tkParent = NULL;
                        }
                    } while (tkParent != NULL);
                    
                    IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeDefRec, &szTypeNamespace));
                    szTypeName = szFullName;
                }
                else
                {
                    IfFailGo(pInternalImport->GetParentToken(rSecAttrs[k].tkCtor, &tkParent));
                    IfFailGo(pInternalImport->GetNameOfTypeDef(tkParent, &szTypeName, &szTypeNamespace));
                }
                pPerm->tkTypeRef = mdTokenNil;
                pPerm->tkAssemblyRef = mdTokenNil;
            }
            else
            {
                _ASSERTE(TypeFromToken(rSecAttrs[k].tkCtor) == mdtMemberRef);
                
                // Get the type ref
                if (pMiniMd != NULL)
                {
                    IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(rSecAttrs[k].tkCtor), &pMemberRefRec));
                    pPerm->tkTypeRef = pMiniMd->getClassOfMemberRef(pMemberRefRec);
                }
                else
                {
                    IfFailGo(pInternalImport->GetParentOfMemberRef(rSecAttrs[k].tkCtor, &pPerm->tkTypeRef));
                }
                
                _ASSERTE(TypeFromToken(pPerm->tkTypeRef) == mdtTypeRef);
                
                // Get an assembly ref
                pPerm->tkAssemblyRef = pPerm->tkTypeRef;
                pTypeRefRec = NULL;
                do
                {
                    if (pMiniMd != NULL)
                    {
                        IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(pPerm->tkAssemblyRef), &pTypeRefRec));
                        pPerm->tkAssemblyRef = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
                    }
                    else
                    {
                        IfFailGo(pInternalImport->GetResolutionScopeOfTypeRef(pPerm->tkAssemblyRef, &pPerm->tkAssemblyRef));
                    }
                    // loop because nested types have a resolution scope of the parent type rather than an assembly
                } while(TypeFromToken(pPerm->tkAssemblyRef) == mdtTypeRef);
                
                // Figure out the fully qualified type name
                if (pMiniMd != NULL)
                {
                    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szTypeNamespace));
                    IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szTypeName));
                }
                else
                {
                    IfFailGo(pInternalImport->GetNameOfTypeRef(pPerm->tkTypeRef, &szTypeNamespace, &szTypeName));
                }
            }
            
            CQuickBytes qb;
            CHAR* szTmp = (CHAR*) qb.AllocNoThrow((MAX_CLASSNAME_LENGTH+1) * sizeof(CHAR));
            if(szTmp == NULL)
                return E_OUTOFMEMORY;
            
            ns::MakePath(szTmp, MAX_CLASSNAME_LENGTH, szTypeNamespace, szTypeName);
            
            size_t len = strlen(szTmp) + 1;
            pPerm->pName = new (nothrow) CHAR[len];
            if(!pPerm->pName)
                return E_OUTOFMEMORY;
            strcpy_s(pPerm->pName, len, szTmp);
        }
    }
    
ErrExit:
    return hr;
}
