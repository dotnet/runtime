// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 


#ifndef __SECURITYATTRIBUTES_H__
#define __SECURITYATTRIBUTES_H__

#include "vars.hpp"
#include "eehash.h"
#include "corperm.h"

class SecurityDescriptor;
class AssemblySecurityDescriptor;
class SecurityStackWalk;
class COMCustomAttribute;
class PsetCacheEntry;
struct TokenDeclActionInfo;

extern HRESULT BlobToAttributeSet(BYTE* pBuffer, ULONG cbBuffer, CORSEC_ATTRSET* pAttrSet, DWORD dwAction);

namespace SecurityAttributes
{
    // Retrieves a previously loaded PermissionSet 
    // object index (this will work even if the permission set was loaded in
    // a different appdomain).
    OBJECTREF GetPermissionSet(DWORD dwIndex, DWORD dwAction);

    // Locate the index of a permission set in the cache (returns false if the
    // permission set has not yet been seen and decoded).
    BOOL LookupPermissionSet(IN PBYTE       pbPset,
                                    IN DWORD       cbPset,
                                    OUT DWORD     *pdwSetIndex);

    // Creates a new permission set
    OBJECTREF CreatePermissionSet(BOOL fTrusted);


    // Uses new to create the byte array that is returned.
    void CopyByteArrayToEncoding(IN U1ARRAYREF* pArray,
                                        OUT PBYTE* pbData,
                                        OUT DWORD* cbData);


    // Generic routine, use with encoding calls that 
    // use the EncodePermission client data
    // Uses new to create the byte array that is returned.
    void CopyEncodingToByteArray(IN PBYTE   pbData,
                                        IN DWORD   cbData,
                                        IN OBJECTREF* pArray);

    BOOL RestrictiveRequestsInAssembly(IMDInternalImport* pImport);

    // Returns the declared PermissionSet or PermissionSetCollection for the
    // specified action type.
    HRESULT GetDeclaredPermissions(IN IMDInternalImport *pInternalImport,
                                          IN mdToken token, // token for method, class, or assembly
                                          IN CorDeclSecurity action, // SecurityAction
                                          OUT OBJECTREF *pDeclaredPermissions, // The returned PermissionSet for that SecurityAction
                                          OUT PsetCacheEntry **pPSCacheEntry = NULL); // The cache entry for the PermissionSet blob.


    HRESULT TranslateSecurityAttributesHelper(
                                CORSEC_ATTRSET    *pAttrSet,
                                BYTE          **ppbOutput,
                                DWORD          *pcbOutput,
                                BYTE          **ppbNonCasOutput,
                                DWORD          *pcbNonCasOutput,
                                DWORD          *pdwErrorIndex);

    HRESULT FixUpPermissionSetAttribute(CORSEC_ATTRIBUTE* pPerm);
    HRESULT SerializeAttribute(CORSEC_ATTRIBUTE* pAttr, BYTE* pBuffer, SIZE_T* pCount, IMetaDataAssemblyImport *pImport);
    HRESULT DeserializeAttribute(CORSEC_ATTRIBUTE *pAttr, BYTE* pBuffer, ULONG cbBuffer, SIZE_T* pPos);
        
    inline bool ContainsBuiltinCASPermsOnly(CORSEC_ATTRSET* pAttrSet);

    inline bool ContainsBuiltinCASPermsOnly(CORSEC_ATTRSET* pAttrSet, bool* pHostProtectionOnly);

    void CreateAndCachePermissions(IN PBYTE pbPerm,
                                          IN ULONG cbPerm,
                                          IN CorDeclSecurity action,
                                          OUT OBJECTREF *pDeclaredPermissions,
                                          OUT PsetCacheEntry **pPSCacheEntry);
    
    HRESULT GetPermissionsFromMetaData(IN IMDInternalImport *pInternalImport,
                                              IN mdToken token,
                                              IN CorDeclSecurity action,
                                              OUT PBYTE* ppbPerm,
                                              OUT ULONG* pcbPerm);

    bool IsUnrestrictedPermissionSetAttribute(CORSEC_ATTRIBUTE* pAttr);
    bool IsBuiltInCASPermissionAttribute(CORSEC_ATTRIBUTE* pAttr);
    bool IsHostProtectionAttribute(CORSEC_ATTRIBUTE* pAttr);

    void LoadPermissionRequestsFromAssembly(IN IMDInternalImport *pImport,
                                                   OUT OBJECTREF*   pReqdPermissions,
                                                   OUT OBJECTREF*   pOptPermissions,
                                                   OUT OBJECTREF*   pDenyPermissions);

    // Insert a decoded permission set into the cache. Duplicates are discarded.
    void InsertPermissionSet(IN PBYTE pbPset,
                                    IN DWORD cbPset,
                                    IN OBJECTREF orPset,
                                    OUT DWORD *pdwSetIndex);

    Assembly* LoadAssemblyFromToken(IMetaDataAssemblyImport *pImport, mdAssemblyRef tkAssemblyRef);
    Assembly* LoadAssemblyFromNameString(__in_z WCHAR* pAssemblyName);
    HRESULT AttributeSetToManaged(OBJECTREF* /*OUT*/obj, CORSEC_ATTRSET* pAttrSet, OBJECTREF* pThrowable, DWORD* pdwErrorIndex, bool bLazy);
    HRESULT SetAttrFieldsAndProperties(CORSEC_ATTRIBUTE *pAttr, OBJECTREF* pThrowable, MethodTable* pMT, OBJECTREF* pObj);
    HRESULT SetAttrField(BYTE** ppbBuffer, SIZE_T* pcbBuffer, DWORD dwType, TypeHandle hEnum, MethodTable* pMT, __in_z LPSTR szName, OBJECTREF* pObj, DWORD dwLength, BYTE* pbName, DWORD cbName, CorElementType eEnumType);
    HRESULT SetAttrProperty(BYTE** ppbBuffer, SIZE_T* pcbBuffer, MethodTable* pMT, DWORD dwType, __in_z LPSTR szName, OBJECTREF* pObj, DWORD dwLength, BYTE* pbName, DWORD cbName, CorElementType eEnumType);
    void AttrArrayToPermissionSet(OBJECTREF* attrArray, bool fSerialize, DWORD attrCount, BYTE **ppbOutput, DWORD *pcbOutput, BYTE **ppbNonCasOutput, DWORD *pcbNonCasOutput, bool fAllowEmptyPermissionSet, OBJECTREF* pPermSet);
    void AttrSetBlobToPermissionSets(IN BYTE* pbRawPermissions, IN DWORD cbRawPermissions, OUT OBJECTREF* pObj, IN DWORD dwAction);



    bool ActionAllowsNullPermissionSet(CorDeclSecurity action);
}

#define LAZY_DECL_SEC_FLAG '.'

#endif // __SECURITYATTRIBUTES_H__

