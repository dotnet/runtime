// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "getappdomainstaticaddress.h"
#include <string>
#include <assert.h>
#include <inttypes.h>
#include <sstream>


using std::thread;
using std::shared_ptr;
using std::map;
using std::make_pair;
using std::mutex;
using std::lock_guard;
using std::wstring;
using std::vector;

// Prints a lot to the console for easier tracking
#define DEBUG_OUT false

GetAppDomainStaticAddress::GetAppDomainStaticAddress() :
    refCount(0),
    failures(0),
    successes(0),
    collectibleCount(0),
    nonCollectibleCount(0),
    jitEventCount(0),
    gcTriggerThread(),
    gcWaitEvent(),
    classADMap(),
    classADMapLock()
{

}

GetAppDomainStaticAddress::~GetAppDomainStaticAddress()
{

}

GUID GetAppDomainStaticAddress::GetClsid()
{
    // {604D76F0-2AF2-48E0-B196-80C972F6AFB7}
    GUID clsid = { 0x604D76F0, 0x2AF2, 0x48E0, {0xB1, 0x96, 0x80, 0xC9, 0x72, 0xF6, 0xAF, 0xB7 } };
    return clsid;
}

HRESULT GetAppDomainStaticAddress::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    printf("Initialize profiler!\n");

    HRESULT hr = pICorProfilerInfoUnk->QueryInterface(IID_ICorProfilerInfo10, (void**)&pCorProfilerInfo);
    if (hr != S_OK)
    {
        printf("Got HR %X from QI for ICorProfilerInfo4", hr);
        ++failures;
        return E_FAIL;
    }

    pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_GC |
                                   COR_PRF_MONITOR_CLASS_LOADS |
                                   COR_PRF_MONITOR_MODULE_LOADS |
                                   COR_PRF_MONITOR_JIT_COMPILATION |
                                   COR_PRF_DISABLE_ALL_NGEN_IMAGES, 0);

    auto gcTriggerLambda = [&]()
    {
        pCorProfilerInfo->InitializeCurrentThread();

        while (true)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));

            gcWaitEvent.Wait();

            if (!IsRuntimeExecutingManagedCode())
            {
                if (DEBUG_OUT)
                {
                    printf("Runtime has not started executing managed code yet.\n");
                }
                continue;
            }

            printf("Forcing GC\n");
            HRESULT hr = pCorProfilerInfo->ForceGC();
            if (FAILED(hr))
            {
                printf("Error forcing GC... hr=0x%x \n", hr);
                ++failures;
                continue;
            }
        }
    };

    gcTriggerThread = thread(gcTriggerLambda);
    gcWaitEvent.Signal();

    return S_OK;
}

HRESULT GetAppDomainStaticAddress::Shutdown()
{
    Profiler::Shutdown();

    gcWaitEvent.Reset();

    if (this->pCorProfilerInfo != nullptr)
    {
        this->pCorProfilerInfo->Release();
        this->pCorProfilerInfo = nullptr;
    }

    if(failures == 0 && successes > 0 && collectibleCount > 0 && nonCollectibleCount > 0)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        printf("Test failed number of failures=%d successes=%d collectibleCount=%d nonCollectibleCount=%d\n",
            failures.load(), successes.load(), collectibleCount.load(), nonCollectibleCount.load());
    }
    fflush(stdout);

    return S_OK;
}

HRESULT GetAppDomainStaticAddress::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    constexpr size_t nameLen = 1024;
    WCHAR name[nameLen];
    HRESULT hr = pCorProfilerInfo->GetModuleInfo2(moduleId,
                                                 NULL,
                                                nameLen,
                                                NULL,
                                                name,
                                                NULL,
                                                NULL);
    if (FAILED(hr))
    {
        printf("GetModuleInfo2 failed with hr=0x%x\n", hr);
        ++failures;
    }

    if (DEBUG_OUT)
    {
        wprintf(L"Module 0x%" PRIxPTR " (%s) loaded\n", moduleId, name);
    }

    return S_OK;
}

HRESULT GetAppDomainStaticAddress::ModuleUnloadStarted(ModuleID moduleId)
{
    lock_guard<mutex> guard(classADMapLock);
    constexpr size_t nameLen = 1024;
    WCHAR name[nameLen];
    HRESULT hr = pCorProfilerInfo->GetModuleInfo2(moduleId,
                                                 NULL,
                                                nameLen,
                                                NULL,
                                                name,
                                                NULL,
                                                NULL);
    if (FAILED(hr))
    {
        printf("GetModuleInfo2 failed with hr=0x%x\n", hr);
        ++failures;
        return E_FAIL;
    }

    if (DEBUG_OUT)
    {
        wprintf(L"Module 0x%" PRIxPTR " (%s) unload started\n", moduleId, name);
    }

    for (auto it = classADMap.begin(); it != classADMap.end(); )
    {
        ClassID classId = it->first;

        ModuleID modId;
        hr = pCorProfilerInfo->GetClassIDInfo(classId, &modId, NULL);
        if (FAILED(hr))
        {
            printf("Failed to get ClassIDInfo hr=0x%x\n", hr);
            ++failures;
            return E_FAIL;
        }

        if (modId == moduleId)
        {
            if (DEBUG_OUT)
            {
                printf("ClassID 0x%" PRIxPTR " being removed due to parent module unloading\n", classId);
            }

            it = classADMap.erase(it);
            continue;
        }

        // Now check the generic arguments
        bool shouldEraseClassId = false;
        vector<ClassID> genericTypes = GetGenericTypeArgs(classId);
        for (auto genericIt = genericTypes.begin(); genericIt != genericTypes.end(); ++genericIt)
        {
            ClassID typeArg = *genericIt;
            ModuleID typeArgModId;

            if (DEBUG_OUT)
            {
                printf("Checking generic argument 0x%" PRIxPTR " of class 0x%" PRIxPTR "\n", typeArg, classId);
            }

            hr = pCorProfilerInfo->GetClassIDInfo(typeArg, &typeArgModId, NULL);
            if (FAILED(hr))
            {
                printf("Failed to get ClassIDInfo hr=0x%x\n", hr);
                ++failures;
                return E_FAIL;
            }

            if (typeArgModId == moduleId)
            {
                if (DEBUG_OUT)
                {
                    wprintf(L"ClassID 0x%" PRIxPTR " (%s) being removed due to generic argument 0x%" PRIxPTR " (%s) belonging to the parent module 0x%" PRIxPTR " unloading\n",
                            classId, GetClassIDName(classId).ToCStr(), typeArg, GetClassIDName(typeArg).ToCStr(), typeArgModId);
                }

                shouldEraseClassId = true;
                break;
            }
        }

        if (shouldEraseClassId)
        {
            it = classADMap.erase(it);
        }
        else
        {
            ++it;
        }
    }

    return S_OK;
}

HRESULT GetAppDomainStaticAddress::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    HRESULT hr = S_OK;

    ThreadID threadId = NULL;
    AppDomainID appDomainId = NULL;
    CorElementType baseElemType;
    ClassID        baseClassId;
    ULONG          cRank;

    // We don't care about array classes, so skip them.

    hr = pCorProfilerInfo->IsArrayClass(
        classId,
        &baseElemType,
        &baseClassId,
        &cRank);
    if (hr == S_OK)
    {
        return S_OK;
    }


    hr = pCorProfilerInfo->GetCurrentThreadID(&threadId);
    if (FAILED(hr))
    {
        printf("GetCurrentThreadID returned 0x%x\n", hr);
        ++failures;
        return hr;
    }

    hr = pCorProfilerInfo->GetThreadAppDomain(threadId, &appDomainId);
    if (FAILED(hr))
    {
        printf("GetThreadAppDomain returned 0x%x for ThreadID 0x%" PRIxPTR "\n", hr, threadId);
        ++failures;
        return hr;
    }

    lock_guard<mutex> guard(classADMapLock);
    classADMap.insert(make_pair(classId, appDomainId));

    ModuleID modId;
    hr = pCorProfilerInfo->GetClassIDInfo2(classId,
                                          &modId,
                                          NULL,
                                          NULL,
                                          NULL,
                                          NULL,
                                          NULL);
    if (FAILED(hr))
    {
        printf("GetClassIDInfo2 returned 0x%x for ClassID 0x%" PRIxPTR "\n", hr, classId);
        ++failures;
    }

    wstring name = GetClassIDName(classId).ToWString();

    if (DEBUG_OUT)
    {
        wprintf(L"Class 0x%" PRIxPTR " (%s) loaded from module 0x%" PRIxPTR "\n", classId, name.c_str(), modId);
    }

    return hr;
}

HRESULT GetAppDomainStaticAddress::ClassUnloadStarted(ClassID classId)
{
    lock_guard<mutex> guard(classADMapLock);

    mdTypeDef unloadClassToken;
    HRESULT hr = pCorProfilerInfo->GetClassIDInfo2(classId,
                                                  NULL,
                                                  &unloadClassToken,
                                                  NULL,
                                                  0,
                                                  NULL,
                                                  NULL);
    if (FAILED(hr))
    {
        printf("GetClassIDInfo2 failed with hr=0x%x\n", hr);
        ++failures;
    }

    if (DEBUG_OUT)
    {
        wprintf(L"Class 0x%" PRIxPTR " (%s) unload started\n", classId, GetClassIDName(classId).ToCStr());
    }

    for (auto it = classADMap.begin(); it != classADMap.end(); ++it)
    {
        ClassID mapClass = it->first;
        mdTypeDef mapClassToken;
        hr = pCorProfilerInfo->GetClassIDInfo2(mapClass,
                                              NULL,
                                              &mapClassToken,
                                              NULL,
                                              0,
                                              NULL,
                                              NULL);
        if (mapClass == classId || mapClassToken == unloadClassToken)
        {
            it = classADMap.erase(it);
        }
    }

    return S_OK;
}

HRESULT GetAppDomainStaticAddress::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    ++jitEventCount;
    return S_OK;
}

HRESULT GetAppDomainStaticAddress::GarbageCollectionFinished()
{
    HRESULT hr = S_OK;
    lock_guard<mutex> guard(classADMapLock);

    for (ClassAppDomainMap::iterator iCADM = classADMap.begin();
            iCADM != classADMap.end();
            iCADM++)
    {
        ClassID classId = iCADM->first;
        AppDomainID appDomainId = iCADM->second;

        if (DEBUG_OUT)
        {
            printf("Calling GetClassIDInfo2 on classId 0x%" PRIxPTR "\n", classId);
            fflush(stdout);
        }

        ModuleID classModuleId = NULL;
        hr = pCorProfilerInfo->GetClassIDInfo2(classId,
                                    &classModuleId,
                                    NULL,
                                    NULL,
                                    NULL,
                                    NULL,
                                    NULL);
        if (FAILED(hr))
        {
            printf("GetClassIDInfo2 returned 0x%x for ClassID 0x%" PRIxPTR "\n", hr, classId);
            ++failures;
            continue;
        }

        COMPtrHolder<IMetaDataImport> pIMDImport;

        hr = pCorProfilerInfo->GetModuleMetaData(classModuleId,
                                        ofRead,
                                        IID_IMetaDataImport,
                                        (IUnknown **)&pIMDImport);
        if (hr == CORPROF_E_DATAINCOMPLETE)
        {
            // Module is being unloaded...
            continue;
        }
        else if (FAILED(hr))
        {
            printf("GetModuleMetaData returned 0x%x  for ModuleID 0x%" PRIxPTR "\n", hr, classModuleId);
            ++failures;
            continue;
        }

        HCORENUM hEnum = NULL;
        mdTypeDef token = NULL;
        mdFieldDef fieldTokens[SHORT_LENGTH];
        ULONG cTokens = NULL;

        if (DEBUG_OUT)
        {
            printf("Calling GetClassIDInfo2 (again?) on classId 0x%" PRIxPTR "\n", classId);
            fflush(stdout);
        }

        // Get class token to enum all field    s from MetaData.  (Needed for statics)
        hr = pCorProfilerInfo->GetClassIDInfo2(classId,
                                            NULL,
                                            &token,
                                            NULL,
                                            NULL,
                                            NULL,
                                            NULL);
        if (hr == CORPROF_E_DATAINCOMPLETE)
        {
            // Class load not complete.  We can not inspect yet.
            continue;
        }
        else if (FAILED(hr))
        {
            printf("GetClassIDInfo2returned 0x%x\n", hr);
            ++failures;
            continue;
        }

        // Enum all fields of the class from the MetaData
        hr = pIMDImport->EnumFields(&hEnum,
                                            token,
                                            fieldTokens,
                                            SHORT_LENGTH,
                                            &cTokens);
        if (FAILED(hr))
        {
            printf("IMetaDataImport::EnumFields returned 0x%x\n", hr);
            ++failures;
            continue;
        }

        for (ULONG i = 0; i < cTokens; i++)
        {
            mdTypeDef fieldClassToken = NULL;
            WCHAR tokenName[256];
            ULONG nameLength = NULL;
            DWORD fieldAttributes = NULL;
            PCCOR_SIGNATURE pvSig = NULL;
            ULONG cbSig = NULL;
            DWORD corElementType = NULL;

            hr = pIMDImport->GetFieldProps(fieldTokens[i],
                                            &fieldClassToken,
                                            tokenName,
                                            256,
                                            &nameLength,
                                            &fieldAttributes,
                                            &pvSig,
                                            &cbSig,
                                            &corElementType,
                                            NULL,
                                            NULL);

            if (FAILED(hr))
            {
                printf("GetFieldProps returned 0x%x for Field %d\n", hr, i);
                ++failures;
                continue;
            }

            if ((IsFdStatic(fieldAttributes)) && (!IsFdLiteral(fieldAttributes)))
            {
                COR_PRF_STATIC_TYPE fieldInfo = COR_PRF_FIELD_NOT_A_STATIC;
                hr = pCorProfilerInfo->GetStaticFieldInfo(classId, fieldTokens[i], &fieldInfo);
                if (FAILED(hr))
                {
                    wprintf(L"GetStaticFieldInfo returned HR=0x%x for field %x (%s)\n", hr, fieldTokens[i], tokenName);
                    ++failures;
                    continue;
                }

                if (fieldInfo & COR_PRF_FIELD_APP_DOMAIN_STATIC)
                {
                    PVOID staticOffSet = NULL;

                    if (DEBUG_OUT)
                    {
                        printf("Calling GetAppDomainStaticAddress on classId=0x%" PRIxPTR "\n", classId);
                        fflush(stdout);
                    }

                    hr = pCorProfilerInfo->GetAppDomainStaticAddress(classId,
                                                fieldTokens[i],
                                                appDomainId,
                                                &staticOffSet);

                    if (FAILED(hr) && (hr != CORPROF_E_DATAINCOMPLETE))
                    {
                        printf("GetAppDomainStaticAddress Failed HR 0x%x\n", hr);
                        ++failures;
                        continue;
                    }
                    else if (hr != CORPROF_E_DATAINCOMPLETE)
                    {
                        String moduleName = GetModuleIDName(classModuleId);
                        if (EndsWith(moduleName, WCHAR("unloadlibrary.dll")))
                        {
                            ++collectibleCount;
                        }
                        else
                        {
                            ++nonCollectibleCount;
                        }
                    }
                }
            }
        }
    }

    printf("Garbage collection finished\n");
    ++successes;
    return hr;
}

bool GetAppDomainStaticAddress::IsRuntimeExecutingManagedCode()
{
    return jitEventCount.load() > 0;
}

std::vector<ClassID> GetAppDomainStaticAddress::GetGenericTypeArgs(ClassID classId)
{
    HRESULT hr = S_OK;
    constexpr size_t typeIdArgsLen = 10;
    ClassID typeArgs[typeIdArgsLen];
    ULONG32 typeArgsCount;
    hr = pCorProfilerInfo->GetClassIDInfo2(classId,
                                          NULL,
                                          NULL,
                                          NULL,
                                          typeIdArgsLen,
                                          &typeArgsCount,
                                          typeArgs);
    if (FAILED(hr))
    {
        printf("Error calling GetClassIDInfo2 hr=0x%x\n", hr);
        ++failures;
    }

    vector<ClassID> types;
    for (ULONG32 i = 0; i < typeArgsCount; ++i)
    {
        types.push_back(typeArgs[i]);
    }

    return types;
}
