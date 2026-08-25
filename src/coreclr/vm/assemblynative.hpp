// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  AssemblyNative.hpp
**
** Purpose: Implements FCalls for managed Assembly class
**
**


**
===========================================================*/
#ifndef _ASSEMBLYNATIVE_H
#define _ASSEMBLYNATIVE_H

class CustomAssemblyBinder;

class AssemblyNative
{
public:

    static Assembly* LoadFromPEImage(AssemblyBinder* pBinder, PEImage *pImage, bool excludeAppPaths = false);

    // static FCALLs
    FCDECL0(static FC_BOOL_RET, IsTracingEnabled);

    //
    // instance FCALLs
    //

    FCDECL1(static FC_BOOL_RET, GetIsDynamic, Assembly* pAssembly);

    FCDECL1(static FC_BOOL_RET, GetIsCollectible, Assembly* pAssembly);
};

extern "C" uint32_t QCALLTYPE AssemblyNative_GetAssemblyCount();

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetEntryAssembly(QCall::ObjectHandleOnStack retAssembly);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetExecutingAssembly(QCall::StackCrawlMarkHandle stackMark, QCall::ObjectHandleOnStack retAssembly);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetLocale(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetHashAlgorithm(QCall::AssemblyHandle pAssembly, INT32* pReturnValue);



extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetSimpleName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retSimpleName);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetPublicKey(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retPublicKey);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetFlags(QCall::AssemblyHandle pAssembly, INT32* pReturnValue);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetFullName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetLocation(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetCodeBase(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString, BOOL* pReturnValue);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetResource(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, DWORD * length, BYTE ** pReturnValue);



extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetVersion(QCall::AssemblyHandle pAssembly, INT32* pMajorVersion, INT32* pMinorVersion, INT32*pBuildNumber, INT32* pRevisionNumber);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetTypeCore(QCall::AssemblyHandle pAssembly, LPCSTR szTypeName, LPCSTR* rgszNestedTypeNames, int32_t cNestedTypeNamesLength, QCall::ObjectHandleOnStack retType);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetTypeCoreIgnoreCase(QCall::AssemblyHandle pAssembly, LPCWSTR wszTypeName, LPCWSTR* rgwszNestedTypeNames, int32_t cNestedTypeNamesLength, QCall::ObjectHandleOnStack retType);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetForwardedType(QCall::AssemblyHandle pAssembly, mdToken mdtExternalType, QCall::ObjectHandleOnStack retType);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetManifestResourceInfo(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, QCall::ObjectHandleOnStack retAssembly, QCall::StringHandleOnStack retFileName, INT32* pReturnValue);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetModules(QCall::AssemblyHandle pAssembly, BOOL fLoadIfNotFound, BOOL fGetResourceModules, QCall::ObjectHandleOnStack retModules);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetModule(QCall::AssemblyHandle pAssembly, LPCWSTR wszFileName, QCall::ObjectHandleOnStack retModule);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetExportedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetForwardedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetManifestResourceNames(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retResourceNames);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetReferencedAssemblies(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retReferencedAssemblies);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetEntryPoint(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retMethod);



extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetImageRuntimeVersion(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_InitializeAssemblyLoadContext(INT_PTR ptrAssemblyLoadContext, BOOL fRepresentsTPALoadContext, BOOL fIsCollectible, INT_PTR* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_PrepareForAssemblyLoadContextRelease(INT_PTR ptrNativeAssemblyBinder, INT_PTR ptrManagedStrongAssemblyLoadContext);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_InternalLoad(NativeAssemblyNameParts* pAssemblyName, QCall::ObjectHandleOnStack requestingAssembly, QCall::StackCrawlMarkHandle stackMark,BOOL fThrowOnFileNotFound, QCall::ObjectHandleOnStack assemblyLoadContext, QCall::ObjectHandleOnStack retAssembly);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_LoadFromPath(INT_PTR ptrNativeAssemblyBinder, LPCWSTR pwzILPath, QCall::ObjectHandleOnStack retLoadedAssembly);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_LoadFromStream(INT_PTR ptrNativeAssemblyBinder, INT_PTR ptrAssemblyArray, INT32 cbAssemblyArrayLength, INT_PTR ptrSymbolArray, INT32 cbSymbolArrayLength, QCall::ObjectHandleOnStack retLoadedAssembly);
#ifdef TARGET_WINDOWS

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_LoadFromInMemoryModule(INT_PTR ptrNativeAssemblyBinder, INT_PTR hModule, QCall::ObjectHandleOnStack retLoadedAssembly);
#endif

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_GetLoadContextForAssembly(QCall::AssemblyHandle pAssembly, INT_PTR* pReturnValue);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_InternalTryGetRawMetadata(QCall::AssemblyHandle assembly, UINT8 **blobRef, INT32 *lengthRef, BOOL* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_TraceResolvingHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR alcName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_TraceAssemblyResolveHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked(LPCWSTR assemblyName, BOOL isTrackedAssembly, LPCWSTR requestingAssemblyPath, LPCWSTR requestedAssemblyPath);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_TraceSatelliteSubdirectoryPathProbed(LPCWSTR filePath, HRESULT hr);


extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_ApplyUpdate(QCall::AssemblyHandle assembly, UINT8* metadataDelta, INT32 metadataDeltaLength, UINT8* ilDelta, INT32 ilDeltaLength, UINT8* pdbDelta, INT32 pdbDeltaLength);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyNative_IsApplyUpdateSupported(BOOL* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE AssemblyName_InitializeAssemblySpec(NativeAssemblyNameParts* pAssemblyNameParts, BaseAssemblySpec* pAssemblySpec);

// See TypeMapLazyDictionary.cs for managed version.
struct CallbackContext final
{
    OBJECTREF _currAssembly;
    OBJECTREF _groupType;
    OBJECTREF _externalTypeMap;
    OBJECTREF _proxyTypeMap;
    OBJECTREF _creationException;
};

// See TypeMapLazyDictionary.cs for managed version.
struct ProcessAttributesCallbackArg final
{
    char const* Utf8String1;
    char const* Utf8String2;
    int32_t StringLen1;
    int32_t StringLen2;
};

extern "C" QCallExceptionStatus QCALLTYPE TypeMapLazyDictionary_ProcessAttributes(
    QCall::AssemblyHandle pAssembly,
    QCall::TypeHandle pTypeGroup,
    BOOL (*newExternalTypeEntry)(CallbackContext* context, ProcessAttributesCallbackArg* arg),
    BOOL (*newProxyTypeEntry)(CallbackContext* context, ProcessAttributesCallbackArg* arg),
    BOOL (*newPrecachedExternalTypeMap)(CallbackContext* context),
    BOOL (*newPrecachedProxyTypeMap)(CallbackContext* context),
    CallbackContext* context);

extern "C" QCallExceptionStatus QCALLTYPE TypeMapLazyDictionary_FindPrecachedExternalTypeMapEntry(
    QCall::ModuleHandle pModule,
    QCall::TypeHandle pGroupType,
    LPCUTF8 key, TADDR* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE TypeMapLazyDictionary_FindPrecachedProxyTypeMapEntry(
    QCall::ModuleHandle pModule,
    QCall::TypeHandle pGroupType,
    QCall::TypeHandle pType, TADDR* pReturnValue);

#endif
