// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#include "common.h"
#include "appdomain.hpp"
#include "appdomainnative.hpp"
#include "vars.hpp"
#include "eeconfig.h"
#include "appdomain.inl"
#include "eventtrace.h"
#include "../binder/inc/defaultassemblybinder.h"

#include "clr/fs/path.h"
using namespace clr::fs;

// static
void QCALLTYPE AppDomainNative::CreateDynamicAssembly(QCall::ObjectHandleOnStack assemblyName, QCall::StackCrawlMarkHandle stackMark, INT32 access, QCall::ObjectHandleOnStack assemblyLoadContext, QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL

    GCX_COOP();

    //<TODO>
    // @TODO: there MUST be a better way to do this...
    //</TODO>
    CreateDynamicAssemblyArgs   args;
    ZeroMemory(&args, sizeof(args));

    GCPROTECT_BEGIN((CreateDynamicAssemblyArgsGC&)args);

    args.assemblyName           = (ASSEMBLYNAMEREF)assemblyName.Get();
    args.loaderAllocator        = NULL;

    args.access                 = access;
    args.stackMark              = stackMark;

    Assembly*       pAssembly = nullptr;
    AssemblyBinder* pBinder = nullptr;

    if (assemblyLoadContext.Get() != NULL)
    {
        INT_PTR nativeAssemblyBinder = ((ASSEMBLYLOADCONTEXTREF)assemblyLoadContext.Get())->GetNativeAssemblyBinder();
        pBinder = reinterpret_cast<AssemblyBinder*>(nativeAssemblyBinder);
    }

    pAssembly = Assembly::CreateDynamic(GetAppDomain(), pBinder, &args);

    retAssembly.Set(pAssembly->GetExposedObject());

    GCPROTECT_END();

    END_QCALL;
}

FCIMPL0(Object*, AppDomainNative::GetLoadedAssemblies)
{
    FCALL_CONTRACT;

    struct _gc
    {
        PTRARRAYREF     AsmArray;
    } gc;

    gc.AsmArray = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    MethodTable * pAssemblyClass = CoreLibBinder::GetClass(CLASS__ASSEMBLY);

    AppDomain * pApp = GetAppDomain();

    // Allocate an array with as many elements as there are assemblies in this
    //  appdomain.  This will usually be correct, but there may be assemblies
    //  that are still loading, and those won't be included in the array of
    //  loaded assemblies.  When that happens, the array will have some trailing
    //  NULL entries; those entries will need to be trimmed.
    size_t nArrayElems = pApp->m_Assemblies.GetCount(pApp);
    gc.AsmArray = (PTRARRAYREF) AllocateObjectArray(
        (DWORD)nArrayElems,
        pAssemblyClass);

    size_t numAssemblies = 0;
    {
        // Iterate over the loaded assemblies in the appdomain, and add each one to
        //  to the array.  Quit when the array is full, in case assemblies have been
        //  loaded into this appdomain, on another thread.
        AppDomain::AssemblyIterator i = pApp->IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

        while (i.Next(pDomainAssembly.This()) && (numAssemblies < nArrayElems))
        {
            // Do not change this code.  This is done this way to
            //  prevent a GC hole in the SetObjectReference() call.  The compiler
            //  is free to pick the order of evaluation.
            OBJECTREF o = (OBJECTREF)pDomainAssembly->GetExposedAssemblyObject();
            if (o == NULL)
            {   // The assembly was collected and is not reachable from managed code anymore
                continue;
            }
            gc.AsmArray->SetAt(numAssemblies++, o);
            // If it is a collectible assembly, it is now referenced from the managed world, so we can
            // release the native reference in the holder
        }
    }

    // If we didn't fill the array, allocate a new array that is exactly the
    //  right size, and copy the data to it.
    if (numAssemblies < nArrayElems)
    {
        PTRARRAYREF AsmArray2;
        AsmArray2 = (PTRARRAYREF) AllocateObjectArray(
            (DWORD)numAssemblies,
            pAssemblyClass);

        for (size_t ix = 0; ix < numAssemblies; ++ix)
        {
            AsmArray2->SetAt(ix, gc.AsmArray->GetAt(ix));
        }

        gc.AsmArray = AsmArray2;
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(gc.AsmArray);
} // AppDomainNative::GetAssemblies
FCIMPLEND

FCIMPL1(Object*, AppDomainNative::IsStringInterned, StringObject* pStringUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF       refString   = ObjectToSTRINGREF(pStringUNSAFE);
    STRINGREF*      prefRetVal  = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refString);

    if (refString == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    prefRetVal = GetAppDomain()->IsStringInterned(&refString);

    HELPER_METHOD_FRAME_END();

    if (prefRetVal == NULL)
        return NULL;

    return OBJECTREFToObject(*prefRetVal);
}
FCIMPLEND

FCIMPL1(Object*, AppDomainNative::GetOrInternString, StringObject* pStringUNSAFE)
{
    FCALL_CONTRACT;

    STRINGREF    refRetVal  = NULL;
    STRINGREF    pString    = (STRINGREF)    pStringUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(pString);

    if (pString == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    STRINGREF* stringVal = GetAppDomain()->GetOrInternString(&pString);
    if (stringVal != NULL)
    {
        refRetVal = *stringVal;
    }

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND
