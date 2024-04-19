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

// static
extern "C" void QCALLTYPE AppDomain_CreateDynamicAssembly(QCall::ObjectHandleOnStack assemblyLoadContext, NativeAssemblyNameParts* pAssemblyNameParts, INT32 hashAlgorithm, INT32 access, QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    LOADERALLOCATORREF keepAlive = NULL;
    GCPROTECT_BEGIN(keepAlive);

    _ASSERTE(assemblyLoadContext.Get() != NULL);

    INT_PTR nativeAssemblyBinder = ((ASSEMBLYLOADCONTEXTREF)assemblyLoadContext.Get())->GetNativeAssemblyBinder();
    AssemblyBinder* pBinder = reinterpret_cast<AssemblyBinder*>(nativeAssemblyBinder);

    Assembly* pAssembly = Assembly::CreateDynamic(pBinder, pAssemblyNameParts, hashAlgorithm, access, &keepAlive);

    retAssembly.Set(pAssembly->GetExposedObject());

    GCPROTECT_END();

    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetLoadedAssemblies(QCall::ObjectHandleOnStack retAssemblies)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    PTRARRAYREF asmArray = NULL;
    GCPROTECT_BEGIN(asmArray);

    MethodTable * pAssemblyClass = CoreLibBinder::GetClass(CLASS__ASSEMBLY);

    AppDomain * pApp = GetAppDomain();

    // Allocate an array with as many elements as there are assemblies in this
    //  appdomain.  This will usually be correct, but there may be assemblies
    //  that are still loading, and those won't be included in the array of
    //  loaded assemblies.  When that happens, the array will have some trailing
    //  NULL entries; those entries will need to be trimmed.
    size_t nArrayElems = pApp->GetAssemblyCount();
    asmArray = (PTRARRAYREF) AllocateObjectArray(
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
            asmArray->SetAt(numAssemblies++, o);
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
            AsmArray2->SetAt(ix, asmArray->GetAt(ix));
        }

        asmArray = AsmArray2;
    }

    retAssemblies.Set(asmArray);

    GCPROTECT_END();

    END_QCALL;
}

extern "C" void QCALLTYPE String_IsInterned(QCall::StringHandleOnStack str)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    STRINGREF refString = str.Get();
    GCPROTECT_BEGIN(refString);
    STRINGREF* prefRetVal = GetAppDomain()->IsStringInterned(&refString);
    str.Set((prefRetVal != NULL) ? *prefRetVal : NULL);
    GCPROTECT_END();

    END_QCALL;
}

extern "C" void QCALLTYPE String_Intern(QCall::StringHandleOnStack str)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    STRINGREF refString = str.Get();
    GCPROTECT_BEGIN(refString);
    STRINGREF* stringVal = GetAppDomain()->GetOrInternString(&refString);
    str.Set(*stringVal);
    GCPROTECT_END();

    END_QCALL;
}
