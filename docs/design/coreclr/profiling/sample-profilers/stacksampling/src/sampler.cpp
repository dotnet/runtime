// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "CorProfiler.h"
#include "sampler.h"
#include <thread>
#include <cwchar>
#include <cstdio>
#include <cinttypes>
#include <locale>
#include <codecvt>

using std::wstring_convert;
using std::codecvt_utf8;
using std::string;

ManualEvent Sampler::s_waitEvent;
Sampler *Sampler::s_instance = nullptr;

HRESULT __stdcall DoStackSnapshotStackSnapShotCallbackWrapper(
    FunctionID funcId,
    UINT_PTR ip,
    COR_PRF_FRAME_INFO frameInfo,
    ULONG32 contextSize,
    BYTE context[],
    void* clientData)
{
    return Sampler::Instance()->StackSnapshotCallback(funcId,
        ip,
        frameInfo,
        contextSize,
        context,
        clientData);
}

Sampler::Sampler(ICorProfilerInfo10* pProfInfo, CorProfiler *parent) :
    m_workerThread(DoSampling, pProfInfo, parent)
{
    Sampler::s_instance = this;
}

// static
void Sampler::DoSampling(ICorProfilerInfo10 *pProfInfo, CorProfiler *parent)
{
    Sampler::Instance()->corProfilerInfo = parent->corProfilerInfo;

    pProfInfo->InitializeCurrentThread();

    while (true)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(100));

        s_waitEvent.Wait();

        if (!parent->IsRuntimeExecutingManagedCode())
        {
            printf("Runtime has not started executing managed code yet.\n");
            continue;
        }

        printf("Suspending runtime\n");
        HRESULT hr = pProfInfo->SuspendRuntime();
        if (FAILED(hr))
        {
            printf("Error suspending runtime... hr=0x%x \n", hr);
            continue;
        }

        ICorProfilerThreadEnum* threadEnum = nullptr;
        hr = pProfInfo->EnumThreads(&threadEnum);
        if (FAILED(hr))
        {
            printf("Error getting thread enumerator\n");
            continue;
        }

        ThreadID threadID;
        ULONG numReturned;
        while ((hr = threadEnum->Next(1, &threadID, &numReturned)) == S_OK)
        {
            printf("Starting stack walk for managed thread id=0x%" PRIx64 "\n", (uint64_t)threadID);

            hr = pProfInfo->DoStackSnapshot(threadID,
                                            DoStackSnapshotStackSnapShotCallbackWrapper,
                                            COR_PRF_SNAPSHOT_REGISTER_CONTEXT,
                                            NULL,
                                            NULL,
                                            0);
            if (FAILED(hr))
            {
                if (hr == E_FAIL)
                {
                    printf("Managed thread id=0x%" PRIx64 " has no managed frames to walk \n", (uint64_t)threadID);
                }
                else
                {
                    printf("DoStackSnapshot for thread id=0x%" PRIx64 " failed with hr=0x%x \n", (uint64_t)threadID, hr);
                }
            }

            printf("Ending stack walk for managed thread id=0x%" PRIx64 "\n", (uint64_t)threadID);
        }

        printf("Resuming runtime\n");
        hr = pProfInfo->ResumeRuntime();
        if (FAILED(hr))
        {
            printf("ResumeRuntime failed with hr=0x%x \n", hr);
        }
    }
}

void Sampler::Start()
{
    s_waitEvent.Signal();
}

void Sampler::Stop()
{
    s_waitEvent.Reset();
}

HRESULT Sampler::StackSnapshotCallback(FunctionID funcId, UINT_PTR ip, COR_PRF_FRAME_INFO frameInfo, ULONG32 contextSize, BYTE context[], void* clientData)
{
    WSTRING functionName = GetFunctionName(funcId, frameInfo);

#if WIN32
    wstring_convert<codecvt_utf8<wchar_t>, wchar_t> convert;
#else // WIN32
    wstring_convert<codecvt_utf8<char16_t>, char16_t> convert;
#endif // WIN32

    string printable = convert.to_bytes(functionName);
    printf("    %s (funcId=0x%" PRIx64 ")\n", printable.c_str(), (uint64_t)funcId);
    return S_OK;
}

WSTRING Sampler::GetModuleName(ModuleID modId)
{
    WCHAR moduleFullName[STRING_LENGTH];
    ULONG nameLength = 0;
    AssemblyID assemID;

    if (modId == NULL)
    {
        printf("NULL modId passed to GetModuleName\n");
        return WSTR("Unknown");
    }

    HRESULT hr = corProfilerInfo->GetModuleInfo(modId,
                                                NULL,
                                                STRING_LENGTH,
                                                &nameLength,
                                                moduleFullName,
                                                &assemID);
    if (FAILED(hr))
    {
        printf("GetModuleInfo failed with hr=0x%x\n", hr);
        return WSTR("Unknown");
    }

    WCHAR *ptr = NULL;
    WCHAR *index = moduleFullName;
    // Find the last occurence of the \ character
    while (*index != 0)
    {
        if (*index == '\\' || *index == '/')
        {
            ptr = index;
        }

        ++index;
    }

    if (ptr == NULL)
    {
        return moduleFullName;
    }
    // Skip the last \ in the string
    ++ptr;

    WSTRING moduleName;
    while (*ptr != 0)
    {
        moduleName += *ptr;
        ++ptr;
    }

    return moduleName;
}


WSTRING Sampler::GetClassName(ClassID classId)
{
    ModuleID modId;
    mdTypeDef classToken;
    ClassID parentClassID;
    ULONG32 nTypeArgs;
    ClassID typeArgs[SHORT_LENGTH];
    HRESULT hr = S_OK;

    if (classId == NULL)
    {
        printf("NULL classId passed to GetClassName\n");
        return WSTR("Unknown");
    }

    hr = corProfilerInfo->GetClassIDInfo2(classId,
                                &modId,
                                &classToken,
                                &parentClassID,
                                SHORT_LENGTH,
                                &nTypeArgs,
                                typeArgs);
    if (CORPROF_E_CLASSID_IS_ARRAY == hr)
    {
        // We have a ClassID of an array.
        return WSTR("ArrayClass");
    }
    else if (CORPROF_E_CLASSID_IS_COMPOSITE == hr)
    {
        // We have a composite class
        return WSTR("CompositeClass");
    }
    else if (CORPROF_E_DATAINCOMPLETE == hr)
    {
        // type-loading is not yet complete. Cannot do anything about it.
        return WSTR("DataIncomplete");
    }
    else if (FAILED(hr))
    {
        printf("GetClassIDInfo returned hr=0x%x for classID=0x%" PRIx64 "\n", hr, (uint64_t)classId);
        return WSTR("Unknown");
    }

    COMPtrHolder<IMetaDataImport> pMDImport;
    hr = corProfilerInfo->GetModuleMetaData(modId,
                                            (ofRead | ofWrite),
                                            IID_IMetaDataImport,
                                            (IUnknown **)&pMDImport );
    if (FAILED(hr))
    {
        printf("GetModuleMetaData failed with hr=0x%x\n", hr);
        return WSTR("Unknown");
    }


    WCHAR wName[LONG_LENGTH];
    DWORD dwTypeDefFlags = 0;
    hr = pMDImport->GetTypeDefProps(classToken,
                                    wName,
                                    LONG_LENGTH,
                                    NULL,
                                    &dwTypeDefFlags,
                                    NULL);
    if (FAILED(hr))
    {
        printf("GetTypeDefProps failed with hr=0x%x\n", hr);
        return WSTR("Unknown");
    }

    WSTRING name = GetModuleName(modId);
    name += WSTR(" ");
    name += wName;

    if (nTypeArgs > 0)
    {
        name += WSTR("<");
    }

    for(ULONG32 i = 0; i < nTypeArgs; i++)
    {
        name += GetClassName(typeArgs[i]);

        if ((i + 1) != nTypeArgs)
        {
            name += WSTR(", ");
        }
    }

    if (nTypeArgs > 0)
    {
        name += WSTR(">");
    }

    return name;
}

WSTRING Sampler::GetFunctionName(FunctionID funcID, const COR_PRF_FRAME_INFO frameInfo)
{
    if (funcID == NULL)
    {
        return WSTR("Unknown_Native_Function");
    }

    ClassID classId = NULL;
    ModuleID moduleId = NULL;
    mdToken token = NULL;
    ULONG32 nTypeArgs = NULL;
    ClassID typeArgs[SHORT_LENGTH];

    HRESULT hr = corProfilerInfo->GetFunctionInfo2(funcID,
                                                   frameInfo,
                                                   &classId,
                                                   &moduleId,
                                                   &token,
                                                   SHORT_LENGTH,
                                                   &nTypeArgs,
                                                   typeArgs);
    if (FAILED(hr))
    {
        printf("GetFunctionInfo2 failed with hr=0x%x\n", hr);
    }

    COMPtrHolder<IMetaDataImport> pIMDImport;
    hr = corProfilerInfo->GetModuleMetaData(moduleId,
                                            ofRead,
                                            IID_IMetaDataImport,
                                            (IUnknown **)&pIMDImport);
    if (FAILED(hr))
    {
        printf("GetModuleMetaData failed with hr=0x%x\n", hr);
    }

    WCHAR funcName[STRING_LENGTH];
    hr = pIMDImport->GetMethodProps(token,
                                    NULL,
                                    funcName,
                                    STRING_LENGTH,
                                    0,
                                    0,
                                    NULL,
                                    NULL,
                                    NULL,
                                    NULL);
    if (FAILED(hr))
    {
        printf("GetMethodProps failed with hr=0x%x\n", hr);
    }

    WSTRING name;

    // If the ClassID returned from GetFunctionInfo is 0, then the function is a shared generic function.
    if (classId != 0)
    {
        name += GetClassName(classId);
    }
    else
    {
        name += WSTR("SharedGenericFunction");
    }

    name += WSTR("::");

    name += funcName;

    // Fill in the type parameters of the generic method
    if (nTypeArgs > 0)
    {
        name += WSTR("<");
    }

    for(ULONG32 i = 0; i < nTypeArgs; i++)
    {
        name += GetClassName(typeArgs[i]);

        if ((i + 1) != nTypeArgs)
        {
            name += WSTR(", ");
        }
    }

    if (nTypeArgs > 0)
    {
        name += WSTR(">");
    }

    return name;
}
