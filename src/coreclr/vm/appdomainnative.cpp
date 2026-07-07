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
#include "../binder/inc/applicationcontext.hpp"
#include <corehost/host_runtime_contract.h>
#include "stringarraylist.h"

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
        CollectibleAssemblyHolder<Assembly *> pAssembly;

        while (i.Next(pAssembly.This()) && (numAssemblies < nArrayElems))
        {
            // Do not change this code.  This is done this way to
            //  prevent a GC hole in the SetObjectReference() call.  The compiler
            //  is free to pick the order of evaluation.
            OBJECTREF o = (OBJECTREF)pAssembly->GetExposedObject();
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

namespace
{
    // Append all paths from a StringArrayList into 'output', separated by PATH_SEPARATOR_CHAR_W.
    void AppendStringArrayList(StringArrayList* pList, SString& output)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        _ASSERTE(pList != NULL);

        for (DWORD i = 0; i < pList->GetCount(); ++i)
        {
            if (i != 0)
                output.Append(PATH_SEPARATOR_CHAR_W);

            output.Append(pList->Get(i));
        }
    }
}

// Get the value of a known host property from the binder/AppDomain state.
extern "C" BOOL QCALLTYPE AppContext_TryGetHostPropertyValue(LPCWSTR name, QCall::StringHandleOnStack retValue)
{
    QCALL_CONTRACT;

    BOOL found = FALSE;

    BEGIN_QCALL;

    AppDomain* pDomain = AppDomain::GetCurrentDomain();
    DefaultAssemblyBinder* pBinder = pDomain->GetDefaultBinder();
    BINDER_SPACE::ApplicationContext* pAppContext = pBinder->GetAppContext();

    if (u16_strcmp(name, _T(HOST_PROPERTY_TRUSTED_PLATFORM_ASSEMBLIES)) == 0)
    {
        if (pAppContext->IsTpaListProvided())
        {
            BINDER_SPACE::SimpleNameToFileNameMap* pMap = pAppContext->GetTpaList();
            _ASSERTE(pMap != NULL);

            SString result;
            BINDER_SPACE::SimpleNameToFileNameMap::Iterator i = pMap->Begin();
            BINDER_SPACE::SimpleNameToFileNameMap::Iterator end = pMap->End();
            while (i != end)
            {
                if (i->m_wszILFileName != NULL)
                {
                    if (!result.IsEmpty())
                        result.Append(PATH_SEPARATOR_CHAR_W);

                    result.Append(i->m_wszILFileName);
                }

                ++i;
            }

            if (!result.IsEmpty())
            {
                retValue.Set(result);
                found = TRUE;
            }
        }
    }
    else if (u16_strcmp(name, _T(HOST_PROPERTY_NATIVE_DLL_SEARCH_DIRECTORIES)) == 0)
    {
        SString result;
        AppDomain::PathIterator iter = pDomain->IterateNativeDllSearchDirectories();
        while (iter.Next())
        {
            if (!result.IsEmpty())
                result.Append(PATH_SEPARATOR_CHAR_W);

            result.Append(*iter.GetPath());
        }

        if (!result.IsEmpty())
        {
            retValue.Set(result);
            found = TRUE;
        }
    }
    else if (u16_strcmp(name, _T(HOST_PROPERTY_PLATFORM_RESOURCE_ROOTS)) == 0)
    {
        StringArrayList* pList = pAppContext->GetPlatformResourceRoots();
        if (pList != NULL && pList->GetCount() > 0)
        {
            SString result;
            AppendStringArrayList(pList, result);
            retValue.Set(result);
            found = TRUE;
        }
    }
    else if (u16_strcmp(name, _T(HOST_PROPERTY_APP_PATHS)) == 0)
    {
        StringArrayList* pList = pAppContext->GetAppPaths();
        if (pList != NULL && pList->GetCount() > 0)
        {
            SString result;
            AppendStringArrayList(pList, result);
            retValue.Set(result);
            found = TRUE;
        }
    }
    else
    {
        // Caller is expected to only request known properties.
        _ASSERTE(!"AppContext_TryGetHostPropertyValue called with unknown name");
    }

    END_QCALL;

    return found;
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
