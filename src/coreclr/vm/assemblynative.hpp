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
    friend class Assembly;
    friend class BaseDomain;
    friend class DomainAssembly;

public:

    static Assembly* LoadFromPEImage(AssemblyBinder* pBinder, PEImage *pImage, bool excludeAppPaths = false);

    // static FCALLs
    static FCDECL0(FC_BOOL_RET, IsTracingEnabled);

    //
    // instance FCALLs
    //

    static
    FCDECL1(FC_BOOL_RET, IsDynamic, AssemblyBaseObject * pAssemblyUNSAFE);
    static FCDECL1(Object*, GetManifestResourceNames, AssemblyBaseObject * pAssemblyUNSAFE);
    static FCDECL1(Object*, GetReferencedAssemblies, AssemblyBaseObject * pAssemblyUNSAFE);
};

extern "C" uint32_t QCALLTYPE AssemblyNative_GetAssemblyCount();

extern "C" void QCALLTYPE AssemblyNative_GetEntryAssembly(QCall::ObjectHandleOnStack retAssembly);


extern "C" void QCALLTYPE AssemblyNative_GetExecutingAssembly(QCall::StackCrawlMarkHandle stackMark, QCall::ObjectHandleOnStack retAssembly);


extern "C" void QCALLTYPE AssemblyNative_GetLocale(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" INT32 QCALLTYPE AssemblyNative_GetHashAlgorithm(QCall::AssemblyHandle pAssembly);



extern "C" void QCALLTYPE AssemblyNative_GetSimpleName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retSimpleName);


extern "C" void QCALLTYPE AssemblyNative_GetPublicKey(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retPublicKey);


extern "C" INT32 QCALLTYPE AssemblyNative_GetFlags(QCall::AssemblyHandle pAssembly);


extern "C" void QCALLTYPE AssemblyNative_GetFullName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" void QCALLTYPE AssemblyNative_GetLocation(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" BOOL QCALLTYPE AssemblyNative_GetCodeBase(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" BYTE * QCALLTYPE AssemblyNative_GetResource(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, DWORD * length);



extern "C" void QCALLTYPE AssemblyNative_GetVersion(QCall::AssemblyHandle pAssembly, INT32* pMajorVersion, INT32* pMinorVersion, INT32*pBuildNumber, INT32* pRevisionNumber);


extern "C" void QCALLTYPE AssemblyNative_GetTypeCore(QCall::AssemblyHandle pAssembly, LPCSTR szTypeName, LPCSTR* rgszNestedTypeNames, int32_t cNestedTypeNamesLength, QCall::ObjectHandleOnStack retType);


extern "C" void QCALLTYPE AssemblyNative_GetTypeCoreIgnoreCase(QCall::AssemblyHandle pAssembly, LPCWSTR wszTypeName, LPCWSTR* rgwszNestedTypeNames, int32_t cNestedTypeNamesLength, QCall::ObjectHandleOnStack retType);


extern "C" void QCALLTYPE AssemblyNative_GetForwardedType(QCall::AssemblyHandle pAssembly, mdToken mdtExternalType, QCall::ObjectHandleOnStack retType);


extern "C" INT32 QCALLTYPE AssemblyNative_GetManifestResourceInfo(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, QCall::ObjectHandleOnStack retAssembly, QCall::StringHandleOnStack retFileName);


extern "C" void QCALLTYPE AssemblyNative_GetModules(QCall::AssemblyHandle pAssembly, BOOL fLoadIfNotFound, BOOL fGetResourceModules, QCall::ObjectHandleOnStack retModules);


extern "C" void QCALLTYPE AssemblyNative_GetModule(QCall::AssemblyHandle pAssembly, LPCWSTR wszFileName, QCall::ObjectHandleOnStack retModule);


extern "C" void QCALLTYPE AssemblyNative_GetExportedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes);


extern "C" void QCALLTYPE AssemblyNative_GetForwardedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes);


extern "C" void QCALLTYPE AssemblyNative_GetEntryPoint(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retMethod);



extern "C" void QCALLTYPE AssemblyNative_GetImageRuntimeVersion(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);


extern "C" BOOL QCALLTYPE AssemblyNative_GetIsCollectible(QCall::AssemblyHandle pAssembly);

extern "C" INT_PTR QCALLTYPE AssemblyNative_InitializeAssemblyLoadContext(INT_PTR ptrManagedAssemblyLoadContext, BOOL fRepresentsTPALoadContext, BOOL fIsCollectible);

extern "C" void QCALLTYPE AssemblyNative_PrepareForAssemblyLoadContextRelease(INT_PTR ptrNativeAssemblyBinder, INT_PTR ptrManagedStrongAssemblyLoadContext);

extern "C" void QCALLTYPE AssemblyNative_InternalLoad(NativeAssemblyNameParts* pAssemblyName, QCall::ObjectHandleOnStack requestingAssembly, QCall::StackCrawlMarkHandle stackMark,BOOL fThrowOnFileNotFound, QCall::ObjectHandleOnStack assemblyLoadContext, QCall::ObjectHandleOnStack retAssembly);

extern "C" void QCALLTYPE AssemblyNative_LoadFromPath(INT_PTR ptrNativeAssemblyBinder, LPCWSTR pwzILPath, LPCWSTR pwzNIPath, QCall::ObjectHandleOnStack retLoadedAssembly);

extern "C" void QCALLTYPE AssemblyNative_LoadFromStream(INT_PTR ptrNativeAssemblyBinder, INT_PTR ptrAssemblyArray, INT32 cbAssemblyArrayLength, INT_PTR ptrSymbolArray, INT32 cbSymbolArrayLength, QCall::ObjectHandleOnStack retLoadedAssembly);
#ifdef TARGET_WINDOWS

extern "C" void QCALLTYPE AssemblyNative_LoadFromInMemoryModule(INT_PTR ptrNativeAssemblyBinder, INT_PTR hModule, QCall::ObjectHandleOnStack retLoadedAssembly);
#endif

extern "C" INT_PTR QCALLTYPE AssemblyNative_GetLoadContextForAssembly(QCall::AssemblyHandle pAssembly);


extern "C" BOOL QCALLTYPE AssemblyNative_InternalTryGetRawMetadata(QCall::AssemblyHandle assembly, UINT8 **blobRef, INT32 *lengthRef);

extern "C" void QCALLTYPE AssemblyNative_TraceResolvingHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR alcName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath);

extern "C" void QCALLTYPE AssemblyNative_TraceAssemblyResolveHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath);

extern "C" void QCALLTYPE AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked(LPCWSTR assemblyName, bool isTrackedAssembly, LPCWSTR requestingAssemblyPath, LPCWSTR requestedAssemblyPath);

extern "C" void QCALLTYPE AssemblyNative_TraceSatelliteSubdirectoryPathProbed(LPCWSTR filePath, HRESULT hr);


extern "C" void QCALLTYPE AssemblyNative_ApplyUpdate(QCall::AssemblyHandle assembly, UINT8* metadataDelta, INT32 metadataDeltaLength, UINT8* ilDelta, INT32 ilDeltaLength, UINT8* pdbDelta, INT32 pdbDeltaLength);

extern "C" BOOL QCALLTYPE AssemblyNative_IsApplyUpdateSupported();

extern "C" void QCALLTYPE AssemblyName_InitializeAssemblySpec(NativeAssemblyNameParts* pAssemblyNameParts, BaseAssemblySpec* pAssemblySpec);

#endif
