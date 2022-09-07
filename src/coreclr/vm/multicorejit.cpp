// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: MultiCoreJIT.cpp
//

// ===========================================================================
// This file contains the implementation for MultiCore JIT (player in a separate file MultiCoreJITPlayer.cpp)
// ===========================================================================
//

#include "common.h"
#include "vars.hpp"
#include "eeconfig.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "eventtrace.h"
#include "array.h"
#include "fstream.h"
#include "hash.h"

#include "appdomain.hpp"
#include "qcall.h"

#include "eventtracebase.h"
#include "multicorejit.h"
#include "multicorejitimpl.h"

void MulticoreJitFireEtw(const WCHAR * pAction, const WCHAR * pTarget, int p1, int p2, int p3)
{
    LIMITED_METHOD_CONTRACT

    FireEtwMulticoreJit(GetClrInstanceId(), pAction, pTarget, p1, p2, p3);
}


void MulticoreJitFireEtwA(const WCHAR * pAction, const char * pTarget, int p1, int p2, int p3)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef FEATURE_EVENT_TRACE
    EX_TRY
    {
        if (EventEnabledMulticoreJit())
        {
            SString wTarget;

            wTarget.SetUTF8(pTarget);

            FireEtwMulticoreJit(GetClrInstanceId(), pAction, wTarget.GetUnicode(), p1, p2, p3);
        }
    }
    EX_CATCH
    { }
    EX_END_CATCH(SwallowAllExceptions);
#endif // FEATURE_EVENT_TRACE
}

void MulticoreJitFireEtwMethodCodeReturned(MethodDesc * pMethod)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    EX_TRY
    {
        if(pMethod)
        {
            // Get the module id.
            Module * pModule = pMethod->GetModule_NoLogging();
            ULONGLONG ullModuleID = (ULONGLONG)(TADDR) pModule;

            // Get the method id.
            ULONGLONG ullMethodID = (ULONGLONG)pMethod;

            // Fire the event.
            FireEtwMulticoreJitMethodCodeReturned(GetClrInstanceId(), ullModuleID, ullMethodID);
        }
    }
    EX_CATCH
    { }
    EX_END_CATCH(SwallowAllExceptions);
}

#ifdef MULTICOREJIT_LOGGING

// %s ANSI
// %S UNICODE
void _MulticoreJitTrace(const char * format, ...)
{
    static unsigned s_startTick = 0;

    WRAPPER_NO_CONTRACT;

    if (s_startTick == 0)
    {
        s_startTick = GetTickCount();
    }

    va_list args;
    va_start(args, format);

#ifdef LOGGING
    LogSpew2      (LF2_MULTICOREJIT, LL_INFO100, "Mcj ");
    LogSpew2Valist(LF2_MULTICOREJIT, LL_INFO100, format, args);
    LogSpew2      (LF2_MULTICOREJIT, LL_INFO100, ", (time=%d ms)\n", GetTickCount() - s_startTick);
#else

    // Following LogSpewValist(DWORD facility, DWORD level, const char *fmt, va_list args)
    char buffer[512];

    int len;

    len  =  sprintf_s(buffer,       ARRAY_SIZE(buffer),       "Mcj TID %04x: ", GetCurrentThreadId());
    len += _vsnprintf_s(buffer + len, ARRAY_SIZE(buffer) - len, format, args);
    len +=  sprintf_s(buffer + len, ARRAY_SIZE(buffer) - len, ", (time=%d ms)\r\n", GetTickCount() - s_startTick);

    OutputDebugStringA(buffer);
#endif

    va_end(args);

}

#endif


HRESULT MulticoreJitRecorder::WriteOutput()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;           // Called from AppDomain::Stop which is MODE_ANY
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

    if (m_JitInfoArray == nullptr || m_ModuleList == nullptr)
    {
        return S_OK;
    }

    // Go into preemptive mode for file operations
    GCX_PREEMP();

    {
        CFileStream fileStream;

        if (SUCCEEDED(hr = fileStream.OpenForWrite(m_fullFileName)))
        {
            hr = WriteOutput(& fileStream);
        }
    }

    return hr;
}


HRESULT WriteData(IStream * pStream, const void * pData, unsigned len)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END

    ULONG cbWritten;

    HRESULT hr = pStream->Write(pData, len, & cbWritten);

    if (SUCCEEDED(hr) && (cbWritten != len))
    {
        hr = E_FAIL;
    }

    return hr;
}

// Write string, round to to DWORD alignment
HRESULT WriteString(const void * pString, unsigned len, IStream * pStream)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    ULONG cbWritten = 0;

    HRESULT hr;

    hr = pStream->Write(pString, len, & cbWritten);

    if (SUCCEEDED(hr))
    {
        len = RoundUp(len) - len;

        if (len != 0)
        {
            cbWritten = 0;

            hr = pStream->Write(& cbWritten, len, & cbWritten);
        }
    }

    return hr;
}


//static
FileLoadLevel MulticoreJitManager::GetModuleFileLoadLevel(Module * pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    FileLoadLevel level = FILE_LOAD_CREATE; // min level

    if (pModule != NULL)
    {
        DomainAssembly * pDomainAssembly = pModule->GetDomainAssembly();

        if (pDomainAssembly != NULL)
        {
            level = pDomainAssembly->GetLoadLevel();
        }
    }

    return level;
}


bool ModuleVersion::GetModuleVersion(Module * pModule)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = E_FAIL;

    // GetMVID can throw exception
    EX_TRY
    {
        PEAssembly * pAsm = pModule->GetPEAssembly();

        if (pAsm != NULL)
        {
            // CorAssemblyFlags, only 16-bit used
            versionFlags = pAsm->GetFlags();

            _ASSERTE((versionFlags & 0x80000000) == 0);

            pAsm->GetVersion(&major, &minor, &build, &revision);

            pAsm->GetMVID(&mvid);

            hr = S_OK;
        }

        // If the load context is LOADFROM, store it in the flags.
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return SUCCEEDED(hr);
}

ModuleRecord::ModuleRecord(unsigned lenName, unsigned lenAsmName)
    : version{}
    , jitMethodCount{}
    , wLoadLevel{}
{
    LIMITED_METHOD_CONTRACT;

    recordID = Pack8_24(MULTICOREJIT_MODULE_RECORD_ID, sizeof(ModuleRecord));

    // Extra data
    lenModuleName = (unsigned short) lenName;
    lenAssemblyName = (unsigned short) lenAsmName;
    recordID += RoundUp(lenModuleName) + RoundUp(lenAssemblyName);
}


bool RecorderModuleInfo::SetModule(Module * pMod)
{
    STANDARD_VM_CONTRACT;

    pModule   = pMod;

    LPCUTF8 pModuleName = pMod->GetSimpleName();
    unsigned lenModuleName = (unsigned) strlen(pModuleName);
    simpleName.Set((const BYTE *) pModuleName, lenModuleName); // SBuffer::Set copies over name

    SString sAssemblyName;
    pMod->GetAssembly()->GetPEAssembly()->GetDisplayName(sAssemblyName);

    LPCUTF8 pAssemblyName = sAssemblyName.GetUTF8();
    unsigned lenAssemblyName = sAssemblyName.GetCount();
    assemblyName.Set((const BYTE *) pAssemblyName, lenAssemblyName);


    return  moduleVersion.GetModuleVersion(pMod);
}

/////////////////////////////////////////////////////
//
//      class   MulticoreJitRecorder
//
/////////////////////////////////////////////////////

HRESULT MulticoreJitRecorder::WriteModuleRecord(IStream * pStream, const RecorderModuleInfo & module)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr;

    const void * pModuleName = module.simpleName;
    unsigned lenModuleName = module.simpleName.GetSize();

    const void * pAssemblyName = module.assemblyName;
    unsigned lenAssemblyName = module.assemblyName.GetSize();

    ModuleRecord mod(lenModuleName, lenAssemblyName);

    mod.version        = module.moduleVersion;
    mod.jitMethodCount = module.methodCount;
    mod.wLoadLevel     = (unsigned short) module.loadLevel;
    mod.flags          = module.flags;

    hr = WriteData(pStream, & mod, sizeof(mod));

    if (SUCCEEDED(hr))
    {
        hr = WriteString(pModuleName, lenModuleName, pStream);

        if (SUCCEEDED(hr))
        {
            hr = WriteString(pAssemblyName, lenAssemblyName, pStream);
        }
    }

    return hr;
}


HRESULT MulticoreJitRecorder::WriteOutput(IStream * pStream)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    _ASSERTE(m_JitInfoArray != nullptr);
    _ASSERTE(m_ModuleList != nullptr);

    // Preprocessing Methods
    LONG skipped = 0;

    for (LONG i = 0 ; i < m_JitInfoCount; i++)
    {
        if (m_JitInfoArray[i].IsModuleInfo())
        {
            // Module records don't need preprocessing
            continue;
        }

        MethodDesc * pMethod = m_JitInfoArray[i].GetMethodDescAndClean();

        if (m_JitInfoArray[i].IsGenericMethodInfo())
        {
            SigBuilder sigBuilder;

            BOOL fSuccess = false;
            EX_TRY
            {
                fSuccess = ZapSig::EncodeMethod(pMethod, NULL, &sigBuilder, (LPVOID)this, (ENCODEMODULE_CALLBACK)MulticoreJitManager::EncodeModuleHelper, NULL);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);

            if (!fSuccess)
            {
                skipped++;
                continue;
            }

            DWORD dwLength;
            BYTE * pBlob = (BYTE*)sigBuilder.GetSignature(&dwLength);
            if (dwLength >= SIGNATURE_LENGTH_MASK + 1)
            {
                skipped++;
                continue;
            }

            BYTE * pSignature = new (nothrow) BYTE[dwLength];
            if (pSignature == nullptr)
            {
                skipped++;
                continue;
            }

            memcpy(pSignature, pBlob, dwLength);
            m_JitInfoArray[i].PackSignatureForGenericMethod(pSignature, dwLength);
        }
        else
        {
            _ASSERTE(m_JitInfoArray[i].IsNonGenericMethodInfo());

            unsigned token = pMethod->GetMemberDef_NoLogging();
            m_JitInfoArray[i].PackTokenForNonGenericMethod(token);
        }
    }

    {
        HeaderRecord header;

        memset(&header, 0, sizeof(header));

        header.recordID       = Pack8_24(MULTICOREJIT_HEADER_RECORD_ID, sizeof(HeaderRecord));
        header.version        = MULTICOREJIT_PROFILE_VERSION;
        header.moduleCount    = m_ModuleCount;
        header.methodCount    = m_JitInfoCount - skipped - m_ModuleDepCount;
        header.moduleDepCount = m_ModuleDepCount;

        MulticoreJitCodeStorage & curStorage =  m_pDomain->GetMulticoreJitManager().GetMulticoreJitCodeStorage();

        // Stats about played profile, 14 short, 3 long = 40 bytes
        header.shortCounters[ 0] = m_stats.m_nTotalMethod;
        header.shortCounters[ 1] = m_stats.m_nHasNativeCode;
        header.shortCounters[ 2] = m_stats.m_nTryCompiling;
        header.shortCounters[ 3] = (unsigned short) curStorage.GetStored();
        header.shortCounters[ 4] = (unsigned short) curStorage.GetReturned();
        header.shortCounters[ 5] = m_stats.m_nFilteredMethods;
        header.shortCounters[ 6] = m_stats.m_nMissingModuleSkip;
        header.shortCounters[ 7] = m_stats.m_nTotalDelay;
        header.shortCounters[ 8] = m_stats.m_nDelayCount;
        header.shortCounters[ 9] = m_stats.m_nWalkBack;

        _ASSERTE(HEADER_W_COUNTER >= 14);

        header.longCounters[0] = m_stats.m_hr;

        _ASSERTE(HEADER_D_COUNTER >= 3);

        _ASSERTE((sizeof(header) % sizeof(unsigned)) == 0);

        hr = WriteData(pStream, & header, sizeof(header));
    }

    DWORD dwData = 0;

    for (unsigned i = 0; SUCCEEDED(hr) && (i < m_ModuleCount); i ++)
    {
        hr = WriteModuleRecord(pStream, m_ModuleList[i]);
    }

    for (LONG i = 0 ; i < m_JitInfoCount && SUCCEEDED(hr); i++)
    {
        if (m_JitInfoArray[i].IsModuleInfo())
        {
            // Module record
            _ASSERTE(m_JitInfoArray[i].IsFullyInitialized());

            DWORD data1 = m_JitInfoArray[i].GetRawModuleData();
            hr = WriteData(pStream, &data1, sizeof(data1));
        }
        else if (m_JitInfoArray[i].IsGenericMethodInfo())
        {
            // Method record
            DWORD data1 = m_JitInfoArray[i].GetRawMethodData1();
            unsigned short data2 = m_JitInfoArray[i].GetRawMethodData2Generic();
            BYTE * pSignature = m_JitInfoArray[i].GetRawMethodSignature();

            if (pSignature == nullptr)
            {
                // Skipped method
                continue;
            }

            DWORD sigSize = m_JitInfoArray[i].GetMethodSignatureSize();
            DWORD paddingSize = m_JitInfoArray[i].GetMethodRecordPaddingSize();

            hr = WriteData(pStream, &data1, sizeof(data1));
            if (SUCCEEDED(hr))
            {
                hr = WriteData(pStream, &data2, sizeof(data2));
            }
            if (SUCCEEDED(hr))
            {
                hr = WriteData(pStream, pSignature, sigSize);
            }
            if (SUCCEEDED(hr) && paddingSize > 0)
            {
                DWORD tmp = 0;
                hr = WriteData(pStream, &tmp, paddingSize);
            }
        }
        else
        {
            _ASSERTE(m_JitInfoArray[i].IsNonGenericMethodInfo());

            // Method record
            DWORD data1 = m_JitInfoArray[i].GetRawMethodData1();
            unsigned data2 = m_JitInfoArray[i].GetRawMethodData2NonGeneric();

            hr = WriteData(pStream, &data1, sizeof(data1));
            if (SUCCEEDED(hr))
            {
                hr = WriteData(pStream, &data2, sizeof(data2));
            }
        }
    }

    for (LONG i = 0; i < m_JitInfoCount; i++)
    {
        if (m_JitInfoArray[i].IsGenericMethodInfo())
        {
            delete[] m_JitInfoArray[i].GetRawMethodSignature();
        }
    }

    MulticoreJitTrace(("New profile: %d modules, %d methods", m_ModuleCount, m_JitInfoCount));

    _FireEtwMulticoreJit(W("WRITEPROFILE"), m_fullFileName.GetUnicode(), m_ModuleCount, m_JitInfoCount, 0);

    return hr;
}


unsigned MulticoreJitRecorder::FindModule(Module * pModule)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_ModuleList != nullptr);

    for (unsigned i = 0 ; i < m_ModuleCount; i ++)
    {
        if (m_ModuleList[i].pModule == pModule)
        {
            return i;
        }
    }

    return UINT_MAX;
}


// Find known module index, or add to module table
// Return UINT_MAX when table is full, or SetModule fails
unsigned MulticoreJitRecorder::GetOrAddModuleIndex(Module * pModule)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_ModuleList != nullptr);

    unsigned slot = FindModule(pModule);

    if ((slot == UINT_MAX) && (m_ModuleCount < MAX_MODULES))
    {
        slot = m_ModuleCount ++;

        if (! m_ModuleList[slot].SetModule(pModule))
        {
            return UINT_MAX;
        }
    }

    return slot;
}

void MulticoreJitRecorder::RecordMethodInfo(unsigned moduleIndex, MethodDesc * pMethod, bool application)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_JitInfoArray != nullptr);
    _ASSERTE(m_ModuleList != nullptr);

    if (m_JitInfoCount < (LONG) MAX_METHODS)
    {
        m_ModuleList[moduleIndex].methodCount++;
        m_JitInfoArray[m_JitInfoCount++].PackMethod(moduleIndex, pMethod, application);
    }
}

unsigned MulticoreJitRecorder::RecordModuleInfo(Module * pModule)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_ModuleList != nullptr);

    // pModule could be unknown at this point (modules not enumerated, no event received yet)
    unsigned moduleIndex = GetOrAddModuleIndex(pModule);

    if (moduleIndex == UINT_MAX)
    {
        return UINT_MAX;
    }

    if (m_fFirstMethod)
    {
        PreRecordFirstMethod();
    }

    // Make sure level for current module is recorded properly
    // Module dependency for generic and stub as well as regular method are handled in JitInfo.
    // Any module dependencies for all types of methods would be handled with JitInfo before they are attempted to be multicorejitted.
    if (m_ModuleList[moduleIndex].loadLevel != FILE_ACTIVE)
    {
        FileLoadLevel needLevel = MulticoreJitManager::GetModuleFileLoadLevel(pModule);

        if (m_ModuleList[moduleIndex].loadLevel < needLevel)
        {
            m_ModuleList[moduleIndex].loadLevel = needLevel;

            // Update load level
            RecordOrUpdateModuleInfo(needLevel, moduleIndex);
        }
    }

    return moduleIndex;
}

void MulticoreJitRecorder::RecordOrUpdateModuleInfo(FileLoadLevel needLevel, unsigned moduleIndex)
{
    LIMITED_METHOD_CONTRACT;

    if (m_JitInfoArray != nullptr && m_JitInfoCount < (LONG) MAX_METHODS)
    {
        // Due to incremental loading, there are quite a few RecordModuleLoad coming with increasing load level, merge
        // Previous record and current record both represent modules
        if (m_JitInfoCount > 0
            && m_JitInfoArray[m_JitInfoCount - 1].IsModuleInfo()
            && m_JitInfoArray[m_JitInfoCount - 1].GetModuleIndex() == moduleIndex)
        {
            if (needLevel > m_JitInfoArray[m_JitInfoCount - 1].GetModuleLoadLevel())
            {
                m_JitInfoArray[m_JitInfoCount - 1].PackModule(needLevel, moduleIndex);
            }

            return; // no new record
        }

        m_ModuleDepCount++;
        m_JitInfoArray[m_JitInfoCount++].PackModule(needLevel, moduleIndex);
    }
}

class MulticoreJitRecorderModuleEnumerator : public MulticoreJitModuleEnumerator
{
    MulticoreJitRecorder * m_pRecorder;

    HRESULT OnModule(Module * pModule)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            CAN_TAKE_LOCK;
        }
        CONTRACTL_END;

        if (MulticoreJitManager::IsSupportedModule(pModule, false))
        {
            m_pRecorder->AddModuleDependency(pModule, MulticoreJitManager::GetModuleFileLoadLevel(pModule));
        }

        return S_OK;
    }

public:
    MulticoreJitRecorderModuleEnumerator(MulticoreJitRecorder * pRecorder)
    {
        m_pRecorder = pRecorder;
    }
};


// The whole AppDomain is depending on pModule
void MulticoreJitRecorder::AddModuleDependency(Module * pModule, FileLoadLevel loadLevel)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_ModuleList != nullptr);

    MulticoreJitTrace(("AddModuleDependency(%s, %d)", pModule->GetSimpleName(), loadLevel));

    _FireEtwMulticoreJitA(W("ADDMODULEDEPENDENCY"), pModule->GetSimpleName(), loadLevel, 0, 0);

    unsigned moduleTo = GetOrAddModuleIndex(pModule);

    if (moduleTo == UINT_MAX)
    {
        return;
    }

    if (m_ModuleList[moduleTo].loadLevel < loadLevel)
    {
        m_ModuleList[moduleTo].loadLevel = loadLevel;

        // Update load level
        RecordOrUpdateModuleInfo(loadLevel, moduleTo);
    }
}

DWORD MulticoreJitRecorder::EncodeModule(Module * pReferencedModule)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_ModuleList != nullptr);

    unsigned slot = GetOrAddModuleIndex(pReferencedModule);
    FileLoadLevel loadLevel = MulticoreJitManager::GetModuleFileLoadLevel(pReferencedModule);

    if (slot == UINT_MAX)
    {
        return ENCODE_MODULE_FAILED;
    }

    if (m_ModuleList[slot].loadLevel < loadLevel)
    {
        m_ModuleList[slot].loadLevel = loadLevel;

        // Update load level
        RecordOrUpdateModuleInfo(loadLevel, slot);
    }

    // This increment is required, because we need to increase methodCount for all referenced modules for generic method.
    // RecordMethodInfo will only increment this counter for pMethod->GetModule_NoLogging.
    m_ModuleList[slot].methodCount++;

    return (DWORD) slot;
}

// Enumerate all modules within an assembly, call OnModule virtual method
HRESULT MulticoreJitModuleEnumerator::HandleAssembly(DomainAssembly * pAssembly)
{
    STANDARD_VM_CONTRACT;

    Module * pModule = pAssembly->GetModule();
    return OnModule(pModule);
}


// Enum all loaded modules within pDomain, call OnModule virtual method
HRESULT MulticoreJitModuleEnumerator::EnumerateLoadedModules(AppDomain * pDomain)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    AppDomain::AssemblyIterator appIt = pDomain->IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));

    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (appIt.Next(pDomainAssembly.This()) && SUCCEEDED(hr))
    {
        {
            hr = HandleAssembly(pDomainAssembly);
        }
    }

    return hr;
}



// static: single instance within a process

#ifndef TARGET_UNIX
TP_TIMER * MulticoreJitRecorder::s_delayedWriteTimer; // = NULL;

// static
void CALLBACK
MulticoreJitRecorder::WriteMulticoreJitProfiler(PTP_CALLBACK_INSTANCE pInstance, PVOID pvContext, PTP_TIMER pTimer)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
    } CONTRACTL_END;

    MulticoreJitManager * pManager = (MulticoreJitManager *) pvContext;
    pManager->WriteMulticoreJitProfiler();
}

#endif // !TARGET_UNIX

void MulticoreJitRecorder::PreRecordFirstMethod()
{
    STANDARD_VM_CONTRACT;

    // When first method is added to an AppDomain, add all currently loaded modules as dependent modules

    m_fFirstMethod = false;

    {
        MulticoreJitRecorderModuleEnumerator enumerator(this);

        enumerator.EnumerateLoadedModules(m_pDomain);
    }

    // When running under CoreCLR for K, AppDomain is normally not shut down properly (CLR in hybrid case, or Alt-F4 shutdown),
    // So we only allow writing out after profileWriteTimeout seconds
    {
        // Get the timeout in seconds.
        int profileWriteTimeout = (int)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MultiCoreJitProfileWriteDelay);

#ifndef TARGET_UNIX
        // Using the same threadpool timer used by UsageLog to write out profile when running under CoreCLR.
        MulticoreJitManager & manager = m_pDomain->GetMulticoreJitManager();
        s_delayedWriteTimer = CreateThreadpoolTimer(MulticoreJitRecorder::WriteMulticoreJitProfiler, &manager, NULL);

        if (s_delayedWriteTimer != NULL)
        {
            ULARGE_INTEGER msDelay;

            // SetThreadpoolTimer needs delay to be given in 100 ns unit, negative
            msDelay.QuadPart = (ULONGLONG) -(profileWriteTimeout * 10 * 1000 * 1000);
            FILETIME ftDueTime;
            ftDueTime.dwLowDateTime = msDelay.u.LowPart;
            ftDueTime.dwHighDateTime = msDelay.u.HighPart;

            // This will either set the timer to happen in profileWriteTimeout seconds, or reset the timer so the same will happen.
            // This function is safe to call
            SetThreadpoolTimer(s_delayedWriteTimer, &ftDueTime, 0, 2000 /* large 2000 ms window for executing this timer is acceptable as the timing here is very much not critical */);
        }
#endif // !TARGET_UNIX
    }
}


void MulticoreJitRecorder::RecordMethodJitOrLoad(MethodDesc * pMethod, bool application)
{
    STANDARD_VM_CONTRACT;

    Module * pModule = pMethod->GetModule_NoLogging();

    // Skip methods from non-supported modules
    if (! MulticoreJitManager::IsSupportedModule(pModule, true))
    {
        return;
    }

    unsigned moduleIndex = RecordModuleInfo(pModule);

    if (moduleIndex == UINT_MAX)
    {
        return;
    }

    RecordMethodInfo(moduleIndex, pMethod, application);
}


// Called from AppDomain::RaiseAssemblyResolveEvent, make it simple

void MulticoreJitRecorder::AbortProfile()
{
    LIMITED_METHOD_CONTRACT;

    // Increment session ID tells background thread to stop
    m_pDomain->GetMulticoreJitManager().GetProfileSession().Increment();

    m_fAborted       = true;  // Do not save output when StopProfile is called
}


HRESULT MulticoreJitRecorder::StopProfile(bool appDomainShutdown)
{
    CONTRACTL
    {
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Increment session ID tells background thread to stop
    MulticoreJitManager & manager = m_pDomain->GetMulticoreJitManager();

    manager.GetProfileSession().Increment();

    if (! m_fAborted && ! m_fullFileName.IsEmpty())
    {
        hr = WriteOutput();
    }

    MulticoreJitTrace(("StopProfile: Save new profile to %S, hr=0x%x", m_fullFileName.GetUnicode(), hr));

    return hr;
}


// suffix (>= 0) is used for AutoStartProfile, to support multiple AppDomains. It's set to -1 for normal API call path
HRESULT MulticoreJitRecorder::StartProfile(const WCHAR * pRoot, const WCHAR * pFile, int suffix, LONG nSession)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_FALSE;

    if ((pRoot == NULL) || (pFile == NULL))
    {
        return E_INVALIDARG;
    }

    MulticoreJitTrace(("StartProfile('%S', '%S', %d)", pRoot, pFile, suffix));

    size_t lenFile = wcslen(pFile);

    // Options (only AutoStartProfile using environment variable, for testing)
    // ([d|D]main-thread-delay)
    if ((suffix >= 0) && (lenFile >= 3) && (pFile[0]=='('))// AutoStartProfile, using environment variable
    {
        pFile ++;
        lenFile --;

        while ((lenFile > 0) && isalpha(pFile[0]))
        {
            switch (pFile[0])
            {
            case 'd':
            case 'D':
                g_MulticoreJitEnabled = false;
                break;

            default:
                break;
            }

            pFile ++;
            lenFile --;
        }

        if ((lenFile > 0) && isdigit(* pFile))
        {
            g_MulticoreJitDelay = 0;

            while ((lenFile > 0) && isdigit(* pFile))
            {
                g_MulticoreJitDelay = g_MulticoreJitDelay * 10 + (int) (* pFile - '0');

                pFile ++;
                lenFile --;
            }
        }

        // End of options
        if ((lenFile > 0) && (* pFile == ')'))
        {
            pFile ++;
            lenFile --;
        }
    }

    MulticoreJitTrace(("g_MulticoreJitEnabled   = %d, disable/enable Mcj feature", g_MulticoreJitEnabled));

    if (g_MulticoreJitEnabled && (lenFile > 0))
    {
        m_fullFileName = pRoot;

        // Append separator if root does not end with one
        unsigned len = m_fullFileName.GetCount();

        if ((len != 0) && (m_fullFileName[len - 1] != W('\\')))
        {
            m_fullFileName.Append(W('\\'));
        }

        m_fullFileName.Append(pFile);

        // Suffix for AutoStartProfile, used for multiple appdomain
        if (suffix >= 0)
        {
            m_fullFileName.Append(W('_'));
            m_fullFileName.Append(SystemDomain::System()->DefaultDomain()->GetFriendlyName());
            m_fullFileName.Append(W('_'));
            m_fullFileName.Append(m_pDomain->GetFriendlyName());
            m_fullFileName.Append(W('_'));

            WCHAR buff[MaxSigned32BitDecString + 1];
            FormatInteger(buff, ARRAY_SIZE(buff), "%d", suffix);
            m_fullFileName.Append(buff);

            m_fullFileName.Append(W(".prof"));
        }

        NewHolder<MulticoreJitProfilePlayer> player(new (nothrow) MulticoreJitProfilePlayer(
            m_pBinder,
            nSession));

        if (player == NULL)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
            HRESULT hr1 = S_OK;

            EX_TRY
            {
                hr1 = player->ProcessProfile(m_fullFileName);
            }
            EX_CATCH_HRESULT(hr1);

            // If ProcessProfile succeeds, the background thread is responsible for deleting it when it finishes; otherwise, delete now
            if (SUCCEEDED(hr1))
            {
                if (g_MulticoreJitDelay > 0)
                {
                    MulticoreJitTrace(("Delay main thread %d ms", g_MulticoreJitDelay));

                    ClrSleepEx(g_MulticoreJitDelay, FALSE);
                }

                player.SuppressRelease();
            }

            MulticoreJitTrace(("ProcessProfile('%S') returns %x", m_fullFileName.GetUnicode(), hr1));

            // Ignore error, even when we can't play back the file, we can still record new one

            // If file exists, but profile header can't be read, pass error to caller (ignored by caller for non Appx)
            if (hr1 == COR_E_BADIMAGEFORMAT)
            {
                hr = hr1;
            }
        }
    }

    MulticoreJitTrace(("StartProfile('%S', '%S', %d) returns %x", pRoot, pFile, suffix, hr));

    _FireEtwMulticoreJit(W("STARTPROFILE"), m_fullFileName.GetUnicode(), hr, 0, 0);

    return hr;
}


// Module load call back, record new module information, update play-back module list
void MulticoreJitRecorder::RecordModuleLoad(Module * pModule, FileLoadLevel loadLevel)
{
    STANDARD_VM_CONTRACT;

    if (pModule != NULL)
    {
        if (! m_fFirstMethod) // If m_fFirstMethod flag is still on, defer calling AddModuleDependency until first method JIT
        {
            AddModuleDependency(pModule, loadLevel);
        }
    }
}


// Call back from MethodDesc::MakeJitWorker for
MulticoreJitCodeInfo MulticoreJitRecorder::RequestMethodCode(MethodDesc * pMethod, MulticoreJitManager * pManager)
{
    STANDARD_VM_CONTRACT;

    // Disable it when profiler is running

#ifdef PROFILING_SUPPORTED

    _ASSERTE(! CORProfilerTrackJITInfo());

#endif

    _ASSERTE(! pMethod->IsDynamicMethod());

    MulticoreJitCodeInfo codeInfo = pManager->GetMulticoreJitCodeStorage().QueryAndRemoveMethodCode(pMethod);

    if (!codeInfo.IsNull() && pManager->IsRecorderActive()) // recorder may be off when player is on (e.g. for Appx)
    {
        RecordMethodJitOrLoad(pMethod, false); // JITTed by background thread, returned to application
    }

    return codeInfo;
}

//////////////////////////////////////////////////////////
//
// class MulticoreJitManager: attachment to AppDomain
//
//
//////////////////////////////////////////////////////////


// API Function: SettProfileRoot, store information with MulticoreJitManager class
// Threading: protected by InterlockedExchange(m_fMulticoreJITEnabled)

void MulticoreJitManager::SetProfileRoot(const WCHAR * pProfilePath)
{
    STANDARD_VM_CONTRACT;

#ifdef PROFILING_SUPPORTED

    if (CORProfilerTrackJITInfo())
    {
        return;
    }

#endif

    unsigned minNumCpus = (unsigned)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MultiCoreJitMinNumCpus);

    if (g_SystemInfo.dwNumberOfProcessors >= minNumCpus)
    {
        if (InterlockedCompareExchange(& m_fSetProfileRootCalled, SETPROFILEROOTCALLED, 0) == 0) // Only allow the first call per appdomain
        {
            m_profileRoot = pProfilePath;
        }
    }
}


// API Function: StartProfile
// Threading: protected by m_playerLock
void MulticoreJitManager::StartProfile(AppDomain * pDomain, AssemblyBinder *pBinder, const WCHAR * pProfile, int suffix)
{
    CONTRACTL
    {
        THROWS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (m_fSetProfileRootCalled != SETPROFILEROOTCALLED)
    {
        MulticoreJitTrace(("StartProfile fail: SetProfileRoot not called/failed"));
        _FireEtwMulticoreJit(W("STARTPROFILE"), W("No SetProfileRoot"), 0, 0, 0);
        return;
    }

    // Check if need extra processor for multicore JIT feature
    _ASSERTE(g_SystemInfo.dwNumberOfProcessors >= (unsigned)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MultiCoreJitMinNumCpus));

#ifdef PROFILING_SUPPORTED

    if (CORProfilerTrackJITInfo())
    {
        MulticoreJitTrace(("StartProfile fail: CORProfilerTrackJITInfo on"));
        _FireEtwMulticoreJit(W("STARTPROFILE"), W("Profiling On"), 0, 0, 0);
        return;
    }

#endif
    CrstHolder hold(& m_playerLock);

    // Stop current profiling first, delete current m_pMulticoreJitRecorder if any
    StopProfile(false);

    if ((pProfile != NULL) && (pProfile[0] != 0)) // Ignore empty file name, just same as StopProfile
    {
        MulticoreJitRecorder * pRecorder = new (nothrow) MulticoreJitRecorder(
            pDomain,
            pBinder);

        if (pRecorder != NULL)
        {
            bool gatherProfile = (int)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MultiCoreJitNoProfileGather) == 0;

            m_pMulticoreJitRecorder = pRecorder;

            LONG sessionID = m_ProfileSession.Increment();

            HRESULT hr = m_pMulticoreJitRecorder->StartProfile(m_profileRoot, pProfile, suffix, sessionID);

            MulticoreJitTrace(("MulticoreJitRecorder session %d created: %x", sessionID, hr));

            if ((hr == COR_E_BADIMAGEFORMAT) || (SUCCEEDED(hr) && gatherProfile)) // Ignore COR_E_BADIMAGEFORMAT, always record new profile
            {
                m_pMulticoreJitRecorder->Activate();

                m_fRecorderActive = m_pMulticoreJitRecorder->CanGatherProfile();
            }

            _FireEtwMulticoreJit(W("STARTPROFILE"), W("Recorder"), m_fRecorderActive, hr, 0);
        }
    }
}


// Threading: protected by m_playerLock
void MulticoreJitManager::AbortProfile()
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (m_fSetProfileRootCalled != SETPROFILEROOTCALLED)
    {
        return;
    }

    CrstHolder hold(& m_playerLock);

    if (m_pMulticoreJitRecorder != NULL)
    {
        MulticoreJitTrace(("AbortProfile"));

        _FireEtwMulticoreJit(W("ABORTPROFILE"), W(""), 0, 0, 0);

        m_fRecorderActive = false;

        m_pMulticoreJitRecorder->AbortProfile();
    }

    // Disable the feature within the AppDomain
    m_fSetProfileRootCalled = -1;
}


// Stop current profiling, could be called automatically from AppDomain shut down
// Threading: protected by m_playerLock
void MulticoreJitManager::StopProfile(bool appDomainShutdown)
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (m_fSetProfileRootCalled != SETPROFILEROOTCALLED)
    {
        return;
    }

    MulticoreJitRecorder * pRecorder;

    if (appDomainShutdown)
    {
        // In the app domain shut down code path, need to hold m_playerLock critical section to wait for other thread to finish using recorder
        CrstHolder hold(& m_playerLock);

        pRecorder = InterlockedExchangeT(& m_pMulticoreJitRecorder, NULL);
    }
    else
    {
        // When called from StartProfile, should not take critical section because it's already entered

        pRecorder = InterlockedExchangeT(& m_pMulticoreJitRecorder, NULL);
    }

    if (pRecorder != NULL)
    {
        m_fRecorderActive = false;

        EX_TRY
        {
            pRecorder->StopProfile(appDomainShutdown);
        }
        EX_CATCH
        {
            MulticoreJitTrace(("StopProfile(%d) throws exception", appDomainShutdown));
        }
        EX_END_CATCH(SwallowAllExceptions);

        delete pRecorder;
    }

    MulticoreJitTrace(("StopProfile(%d) returns", appDomainShutdown));
}


LONG g_nMulticoreAutoStart = 0;

// Threading: calls into StartProfile
void MulticoreJitManager::AutoStartProfile(AppDomain * pDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    CLRConfigStringHolder wszProfile(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MultiCoreJitProfile));

    if ((wszProfile != NULL) && (wszProfile[0] != 0))
    {
        int suffix = (int) InterlockedIncrement(& g_nMulticoreAutoStart);

        SetProfileRoot(W("")); // Fake a SetProfileRoot call

        StartProfile(
            pDomain,
            NULL,
            wszProfile,
            suffix);
    }
}


// Constructor

MulticoreJitManager::MulticoreJitManager()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    m_pMulticoreJitRecorder = NULL;
    m_fSetProfileRootCalled = 0;
    m_fAutoStartCalled      = 0;
    m_fRecorderActive       = false;

    m_playerLock.Init(CrstMulticoreJitManager, (CrstFlags)(CRST_TAKEN_DURING_SHUTDOWN));
    m_MulticoreJitCodeStorage.Init();
}


// Threading: uses Release to free object
MulticoreJitManager::~MulticoreJitManager()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pMulticoreJitRecorder != NULL)
    {
        delete m_pMulticoreJitRecorder;

        m_pMulticoreJitRecorder = NULL;
    }

    m_playerLock.Destroy();
}


// Threading: protected by m_playerLock

void MulticoreJitManager::RecordModuleLoad(Module * pModule, FileLoadLevel loadLevel)
{
    STANDARD_VM_CONTRACT;



    if (m_fRecorderActive)
    {
        if(IsSupportedModule(pModule, false)) // Filter out unsupported module
        {
            CrstHolder hold(& m_playerLock);

            if (m_pMulticoreJitRecorder != NULL)
            {
                m_pMulticoreJitRecorder->RecordModuleLoad(pModule, loadLevel);
            }
        }
        else
        {
            _FireEtwMulticoreJitA(W("UNSUPPORTEDMODULE"), pModule->GetSimpleName(), 0, 0, 0);
        }
    }
}


// Call back from MethodDesc::MakeJitWorker for
// Threading: protected by m_playerLock

MulticoreJitCodeInfo MulticoreJitManager::RequestMethodCode(MethodDesc * pMethod)
{
    STANDARD_VM_CONTRACT;

    CrstHolder hold(& m_playerLock);

    if (m_pMulticoreJitRecorder != NULL)
    {
        MulticoreJitCodeInfo requestedCodeInfo = m_pMulticoreJitRecorder->RequestMethodCode(pMethod, this);
        if(!requestedCodeInfo.IsNull())
        {
            _FireEtwMulticoreJitMethodCodeReturned(pMethod);
        }

        return requestedCodeInfo;
    }

    return MulticoreJitCodeInfo();
}


// Call back from MethodDesc::MakeJitWorker for
// Threading: protected by m_playerLock

void MulticoreJitManager::RecordMethodJitOrLoad(MethodDesc * pMethod)
{
    STANDARD_VM_CONTRACT;

    CrstHolder hold(& m_playerLock);

    if (m_pMulticoreJitRecorder != NULL)
    {
        m_pMulticoreJitRecorder->RecordMethodJitOrLoad(pMethod, true);

        if (m_pMulticoreJitRecorder->IsAtFullCapacity())
        {
            m_fRecorderActive = false;
        }
    }
}


// static
bool MulticoreJitManager::IsMethodSupported(MethodDesc * pMethod)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return pMethod->HasILHeader() &&
           !pMethod->IsDynamicMethod() &&
           !pMethod->GetLoaderAllocator()->IsCollectible();
}


// static
// Stop all multicore Jitting profile, called from EEShutDown
void MulticoreJitManager::StopProfileAll()
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    AppDomainIterator domain(TRUE);

    while (domain.Next())
    {
        AppDomain * pDomain = domain.GetDomain();

        if (pDomain != NULL)
        {
            pDomain->GetMulticoreJitManager().StopProfile(true);
        }
    }
}

#ifndef TARGET_UNIX
void MulticoreJitManager::WriteMulticoreJitProfiler()
{
    CONTRACTL
    {
        THROWS;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder hold(& m_playerLock);
    StopProfile(false);
}
#endif // !TARGET_UNIX

// static
// Stop all multicore Jitting in the current process, called from ProfilingAPIUtility::LoadProfiler
void MulticoreJitManager::DisableMulticoreJit()
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifdef PROFILING_SUPPORTED

    AppDomainIterator domain(TRUE);

    while (domain.Next())
    {
        AppDomain * pDomain = domain.GetDomain();

        if (pDomain != NULL)
        {
            pDomain->GetMulticoreJitManager().AbortProfile();
        }
    }

#endif
}

// static
DWORD MulticoreJitManager::EncodeModuleHelper(void * pModuleContext, Module * pReferencedModule)
{
    STANDARD_VM_CONTRACT;

    if (pModuleContext == NULL || pReferencedModule == NULL)
    {
        return ENCODE_MODULE_FAILED;
    }
    return ((MulticoreJitRecorder*)pModuleContext)->EncodeModule(pReferencedModule);
}

//---------------------------------------------------------------------------------------
//
// MultiCore JIT
//
// Arguments:
//    wszProfile  - profile name
//    ptrNativeAssemblyBinder - the binding context
//
extern "C" void QCALLTYPE MultiCoreJIT_InternalStartProfile(_In_z_ LPCWSTR wszProfile, INT_PTR ptrNativeAssemblyBinder)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    AppDomain * pDomain = GetAppDomain();

    AssemblyBinder *pBinder = reinterpret_cast<AssemblyBinder *>(ptrNativeAssemblyBinder);

    pDomain->GetMulticoreJitManager().StartProfile(
        pDomain,
        pBinder,
        wszProfile);

    END_QCALL;
}


extern "C" void QCALLTYPE MultiCoreJIT_InternalSetProfileRoot(_In_z_ LPCWSTR wszProfilePath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    AppDomain * pDomain = GetAppDomain();

    pDomain->GetMulticoreJitManager().SetProfileRoot(wszProfilePath);

    END_QCALL;
}
