// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

class CLRPrivBinderAssemblyLoadContext;

class AssemblyNative
{
    friend class Assembly;
    friend class BaseDomain;
    friend class DomainAssembly;

public:
    // static FCALLs
    static
    void QCALLTYPE GetEntryAssembly(QCall::ObjectHandleOnStack retAssembly);
   
    static
    void QCALLTYPE GetExecutingAssembly(QCall::StackCrawlMarkHandle stackMark, QCall::ObjectHandleOnStack retAssembly);

    static FCDECL6(Object*,         Load,                       AssemblyNameBaseObject* assemblyNameUNSAFE, 
                                                                StringObject* codeBaseUNSAFE, 
                                                                AssemblyBaseObject* requestingAssemblyUNSAFE,
                                                                StackCrawlMark* stackMark,
                                                                CLR_BOOL fThrowOnFileNotFound,
                                                                AssemblyLoadContextBaseObject *assemblyLoadContextUNSAFE);

    //
    // instance FCALLs
    //

    static 
    void QCALLTYPE GetLocale(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);

    static
    INT32 QCALLTYPE GetHashAlgorithm(QCall::AssemblyHandle pAssembly);


    static 
    void QCALLTYPE GetSimpleName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retSimpleName);

    static 
    void QCALLTYPE GetPublicKey(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retPublicKey);

    static
    INT32 QCALLTYPE GetFlags(QCall::AssemblyHandle pAssembly);

    static 
    void QCALLTYPE GetFullName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);

    static 
    void QCALLTYPE GetLocation(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);

    static
    void QCALLTYPE GetCodeBase(QCall::AssemblyHandle pAssembly, BOOL fCopiedName, QCall::StringHandleOnStack retString);

    static 
    BYTE * QCALLTYPE GetResource(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, DWORD * length);

    static
    FCDECL1(FC_BOOL_RET, IsDynamic, AssemblyBaseObject * pAssemblyUNSAFE);

    static
    void QCALLTYPE GetVersion(QCall::AssemblyHandle pAssembly, INT32* pMajorVersion, INT32* pMinorVersion, INT32*pBuildNumber, INT32* pRevisionNumber);

    static 
    void QCALLTYPE GetType(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, BOOL bThrowOnError, BOOL bIgnoreCase, QCall::ObjectHandleOnStack retType, QCall::ObjectHandleOnStack keepAlive, QCall::ObjectHandleOnStack pAssemblyLoadContext);

    static
    void QCALLTYPE GetForwardedType(QCall::AssemblyHandle pAssembly, mdToken mdtExternalType, QCall::ObjectHandleOnStack retType);

    static 
    INT32 QCALLTYPE GetManifestResourceInfo(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, QCall::ObjectHandleOnStack retAssembly, QCall::StringHandleOnStack retFileName);

    static
    void QCALLTYPE GetModules(QCall::AssemblyHandle pAssembly, BOOL fLoadIfNotFound, BOOL fGetResourceModules, QCall::ObjectHandleOnStack retModules);

    static
    void QCALLTYPE GetModule(QCall::AssemblyHandle pAssembly, LPCWSTR wszFileName, QCall::ObjectHandleOnStack retModule);

    static
    void QCALLTYPE GetExportedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes);

    static
    void QCALLTYPE GetForwardedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes);

    static FCDECL1(Object*, GetManifestResourceNames, AssemblyBaseObject * pAssemblyUNSAFE);
    static FCDECL1(Object*, GetReferencedAssemblies, AssemblyBaseObject * pAssemblyUNSAFE);

    static 
    void QCALLTYPE GetEntryPoint(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retMethod);

    static FCDECL1(ReflectModuleBaseObject *, GetInMemoryAssemblyModule, AssemblyBaseObject * pAssemblyUNSAFE);

    static 
    void QCALLTYPE GetImageRuntimeVersion(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString);

    static BOOL QCALLTYPE GetIsCollectible(QCall::AssemblyHandle pAssembly);

    static FCDECL0(uint32_t, GetAssemblyCount);

    //
    // PEFile QCalls
    // 

    static INT_PTR QCALLTYPE InitializeAssemblyLoadContext(INT_PTR ptrManagedAssemblyLoadContext, BOOL fRepresentsTPALoadContext, BOOL fIsCollectible);
    static void QCALLTYPE PrepareForAssemblyLoadContextRelease(INT_PTR ptrNativeAssemblyLoadContext, INT_PTR ptrManagedStrongAssemblyLoadContext);
    static void QCALLTYPE LoadFromPath(INT_PTR ptrNativeAssemblyLoadContext, LPCWSTR pwzILPath, LPCWSTR pwzNIPath, QCall::ObjectHandleOnStack retLoadedAssembly);
    static INT_PTR QCALLTYPE InternalLoadUnmanagedDllFromPath(LPCWSTR unmanagedLibraryPath);
    static void QCALLTYPE LoadFromStream(INT_PTR ptrNativeAssemblyLoadContext, INT_PTR ptrAssemblyArray, INT32 cbAssemblyArrayLength, INT_PTR ptrSymbolArray, INT32 cbSymbolArrayLength, QCall::ObjectHandleOnStack retLoadedAssembly);
#ifndef FEATURE_PAL
    static void QCALLTYPE LoadFromInMemoryModule(INT_PTR ptrNativeAssemblyLoadContext, INT_PTR hModule, QCall::ObjectHandleOnStack retLoadedAssembly);
#endif
    static Assembly* LoadFromPEImage(ICLRPrivBinder* pBinderContext, PEImage *pILImage, PEImage *pNIImage);
#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
    static void QCALLTYPE LoadTypeForWinRTTypeNameInContext(INT_PTR ptrAssemblyLoadContext, LPCWSTR pwzTypeName, QCall::ObjectHandleOnStack retType);
#endif
    static INT_PTR QCALLTYPE GetLoadContextForAssembly(QCall::AssemblyHandle pAssembly);

    static BOOL QCALLTYPE InternalTryGetRawMetadata(QCall::AssemblyHandle assembly, UINT8 **blobRef, INT32 *lengthRef);
};

#endif

