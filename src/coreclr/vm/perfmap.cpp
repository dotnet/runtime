// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: perfmap.cpp
//

#include "common.h"

#if defined(FEATURE_PERFMAP) && !defined(DACCESS_COMPILE)
#include <clrconfignocache.h>
#include "perfmap.h"
#include "perfinfo.h"
#include "pal.h"


// The code addresses are actually native image offsets during crossgen. Print
// them as 32-bit numbers for consistent output when cross-targeting and to
// make the output more compact.

#define FMT_CODE_ADDR "%p"

Volatile<bool> PerfMap::s_enabled = false;
PerfMap * PerfMap::s_Current = nullptr;
bool PerfMap::s_ShowOptimizationTiers = false;
unsigned PerfMap::s_StubsMapped = 0;
CrstStatic PerfMap::s_csPerfMap;

// Initialize the map for the process - called from EEStartupHelper.
void PerfMap::Initialize()
{
    LIMITED_METHOD_CONTRACT;

    s_csPerfMap.Init(CrstPerfMap);

    PerfMapType perfMapType = (PerfMapType)CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapEnabled);
    PerfMap::Enable(perfMapType, false);
}

void PerfMap::Enable(PerfMapType type, bool sendExisting)
{
    LIMITED_METHOD_CONTRACT;

    if (type == PerfMapType::DISABLED)
    {
        return;
    }

    {
        CrstHolder ch(&(s_csPerfMap));

        if (s_Current == nullptr && (type == PerfMapType::ALL || type == PerfMapType::PERFMAP))
        {
            s_Current = new PerfMap();
            int signalNum = (int) CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapIgnoreSignal);

            if (signalNum > 0)
            {
                PAL_IgnoreProfileSignal(signalNum);
            }

            if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapShowOptimizationTiers) != 0)
            {
                s_ShowOptimizationTiers = true;
            }

            int currentPid = GetCurrentProcessId();
            s_Current->OpenFileForPid(currentPid);
            s_enabled = true;
        }

        if (!PAL_PerfJitDump_IsStarted() && (type == PerfMapType::ALL || type == PerfMapType::JITDUMP))
        {   
            const char* jitdumpPath;
            char jitdumpPathBuffer[4096];

            CLRConfigNoCache value = CLRConfigNoCache::Get("PerfMapJitDumpPath");
            if (value.IsSet())
            {
                jitdumpPath = value.AsString();
            }
            else
            {
                GetTempPathA(sizeof(jitdumpPathBuffer) - 1, jitdumpPathBuffer);
                jitdumpPath = jitdumpPathBuffer;
            }

            PAL_PerfJitDump_Start(jitdumpPath);

            if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapShowOptimizationTiers) != 0)
            {
                s_ShowOptimizationTiers = true;
            }
            
            s_enabled = true;
        }
    }

    if (sendExisting)
    {
        AppDomain::AssemblyIterator assemblyIterator = GetAppDomain()->IterateAssembliesEx(
            (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        while (assemblyIterator.Next(pDomainAssembly.This()))
        {
            CollectibleAssemblyHolder<Assembly *> pAssembly = pDomainAssembly->GetAssembly();
            PerfMap::LogImageLoad(pAssembly->GetPEAssembly());

            // PerfMap does not log R2R methods so only proceed if we are emitting jitdumps
            if (type == PerfMapType::ALL || type == PerfMapType::JITDUMP)
            {
                Module *pModule = pAssembly->GetModule();
                if (pModule->IsReadyToRun())
                {
                    ReadyToRunInfo::MethodIterator mi(pModule->GetReadyToRunInfo());

                    while (mi.Next())
                    {
                        // Call GetMethodDesc_NoRestore instead of GetMethodDesc to avoid restoring methods.
                        MethodDesc *hotDesc = (MethodDesc *)mi.GetMethodDesc_NoRestore();
                        if (hotDesc != nullptr && hotDesc->GetNativeCode() != NULL)
                        {
                            PerfMap::LogPreCompiledMethod(hotDesc, hotDesc->GetNativeCode());
                        }
                    }
                }
            }
        }

        {
            CodeVersionManager::LockHolder codeVersioningLockHolder;

            EEJitManager::CodeHeapIterator heapIterator(nullptr);
            while (heapIterator.Next())
            {
                MethodDesc * pMethod = heapIterator.GetMethod();
                if (pMethod == nullptr)
                {
                    continue;
                }

                PCODE codeStart = PINSTRToPCODE(heapIterator.GetMethodCode());
                NativeCodeVersion nativeCodeVersion;
#ifdef FEATURE_CODE_VERSIONING
                nativeCodeVersion = pMethod->GetCodeVersionManager()->GetNativeCodeVersion(pMethod, codeStart);;
                if (nativeCodeVersion.IsNull() && codeStart != pMethod->GetNativeCode())
                {
                    continue;
                }
#else // FEATURE_CODE_VERSIONING
                if (codeStart != pMethod->GetNativeCode())
                {
                    continue;
                }
#endif // FEATURE_CODE_VERSIONING

                EECodeInfo codeInfo(codeStart);
                IJitManager::MethodRegionInfo methodRegionInfo;
                codeInfo.GetMethodRegionInfo(&methodRegionInfo);
                _ASSERTE(methodRegionInfo.hotStartAddress == codeStart);
                    
                PrepareCodeConfig config(!nativeCodeVersion.IsNull() ? nativeCodeVersion : NativeCodeVersion(pMethod), FALSE, FALSE);
                PerfMap::LogJITCompiledMethod(pMethod, codeStart, methodRegionInfo.hotSize, &config);
            }
        }
    }
}

// Disable the map for the process - called from EEShutdownHelper.
void PerfMap::Disable()
{
    LIMITED_METHOD_CONTRACT;

    if (s_enabled)
    {
        CrstHolder ch(&(s_csPerfMap));

        s_enabled = false;
        if (s_Current != nullptr)
        {
            delete s_Current;
            s_Current = nullptr;
        }

        // PAL_PerfJitDump_Finish is lock protected and can safely be called multiple times
        PAL_PerfJitDump_Finish();
    }
}

// Construct a new map for the process.
PerfMap::PerfMap()
{
    LIMITED_METHOD_CONTRACT;

    // Initialize with no failures.
    m_ErrorEncountered = false;
    m_PerfInfo = nullptr;
}

// Clean-up resources.
PerfMap::~PerfMap()
{
    LIMITED_METHOD_CONTRACT;

    delete m_FileStream;
    m_FileStream = nullptr;

    delete m_PerfInfo;
    m_PerfInfo = nullptr;
}

void PerfMap::OpenFileForPid(int pid)
{
    // Build the path to the map file on disk.
    WCHAR tempPath[MAX_LONGPATH+1];
    if(!GetTempPathW(MAX_LONGPATH, tempPath))
    {
        return;
    }

    SString path;
    path.Append(tempPath);
    path.AppendPrintf("perf-%d.map", pid);

    // Open the map file for writing.
    OpenFile(path);

    m_PerfInfo = new PerfInfo(pid);
}

// Open the specified destination map file.
void PerfMap::OpenFile(SString& path)
{
    STANDARD_VM_CONTRACT;

    // Open the file stream.
    m_FileStream = new (nothrow) CFileStream();
    if(m_FileStream != nullptr)
    {
        HRESULT hr = m_FileStream->OpenForWrite(path.GetUnicode());
        if(FAILED(hr))
        {
            delete m_FileStream;
            m_FileStream = nullptr;
        }
    }
}

// Write a line to the map file.
void PerfMap::WriteLine(SString& line)
{
    STANDARD_VM_CONTRACT;
#ifdef _DEBUG
    _ASSERTE(s_csPerfMap.OwnedByCurrentThread());
#endif

    if (m_FileStream == nullptr || m_ErrorEncountered)
    {
        return;
    }

    EX_TRY
    {
        // Write the line.
        // The PAL already takes a lock when writing, so we don't need to do so here.
        const char * strLine = line.GetUTF8();
        ULONG inCount = line.GetCount();
        ULONG outCount;
        m_FileStream->Write(strLine, inCount, &outCount);

        if (inCount != outCount)
        {
            // This will cause us to stop writing to the file.
            // The file will still remain open until shutdown so that we don't have to take a lock at this level when we touch the file stream.
            m_ErrorEncountered = true;
        }

    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

void PerfMap::LogImageLoad(PEAssembly * pPEAssembly)
{
    CrstHolder ch(&(s_csPerfMap));

    if (s_enabled && s_Current != nullptr)
    {
        s_Current->LogImage(pPEAssembly);
    }
}

// Log an image load to the map.
void PerfMap::LogImage(PEAssembly * pPEAssembly)
{
    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pPEAssembly != nullptr);
    } CONTRACTL_END;


    if (m_FileStream == nullptr || m_ErrorEncountered)
    {
        // A failure occurred, do not log.
        return;
    }

    EX_TRY
    {
        CHAR szSignature[GUID_STR_BUFFER_LEN];
        GetNativeImageSignature(pPEAssembly, szSignature, ARRAY_SIZE(szSignature));

        m_PerfInfo->LogImage(pPEAssembly, szSignature);
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

void PerfMap::LogJITCompiledMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize, PrepareCodeConfig *pConfig)
{
    LIMITED_METHOD_CONTRACT;

    CONTRACTL{
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pMethod != nullptr);
        PRECONDITION(pCode != nullptr);
        PRECONDITION(codeSize > 0);
    } CONTRACTL_END;

    if (!s_enabled)
    {
        return;
    }

    const char *optimizationTier = nullptr;
    if (s_ShowOptimizationTiers)
    {
        optimizationTier = PrepareCodeConfig::GetJitOptimizationTierStr(pConfig, pMethod);
    }

    // Logging failures should not cause any exceptions to flow upstream.
    EX_TRY
    {
        // Get the full method signature.
        SString name;
        pMethod->GetFullMethodInfo(name);

        // Build the map file line.
        if (optimizationTier != nullptr && s_ShowOptimizationTiers)
        {
            name.AppendPrintf("[%s]", optimizationTier);
        }
        SString line;
        line.Printf(FMT_CODE_ADDR " %x %s\n", pCode, codeSize, name.GetUTF8());

        {
            CrstHolder ch(&(s_csPerfMap));

            if(s_Current != nullptr)
            {
                s_Current->WriteLine(line);
            }

            PAL_PerfJitDump_LogMethod((void*)pCode, codeSize, name.GetUTF8(), nullptr, nullptr);
        }
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);

}

// Log a pre-compiled method to the perfmap.
void PerfMap::LogPreCompiledMethod(MethodDesc * pMethod, PCODE pCode)
{
    LIMITED_METHOD_CONTRACT;

    if (!s_enabled)
    {
        return;
    }

    // Get information about the NGEN'd method code.
    EECodeInfo codeInfo(pCode);
    _ASSERTE(codeInfo.IsValid());

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    // Logging failures should not cause any exceptions to flow upstream.
    EX_TRY
    {
        // Get the full method signature.
        SString name;
        pMethod->GetFullMethodInfo(name);

        if (s_ShowOptimizationTiers)
        {
            name.Append(W("[PreJIT]"));
        }

        // NGEN can split code between hot and cold sections which are separate in memory.
        // Emit an entry for each section if it is used.
        if (methodRegionInfo.hotSize > 0)
        {
            CrstHolder ch(&(s_csPerfMap));
            PAL_PerfJitDump_LogMethod((void*)methodRegionInfo.hotStartAddress, methodRegionInfo.hotSize, name.GetUTF8(), nullptr, nullptr);
        }

        if (methodRegionInfo.coldSize > 0)
        {
            CrstHolder ch(&(s_csPerfMap));

            if (s_ShowOptimizationTiers)
            {
                pMethod->GetFullMethodInfo(name);
                name.Append(W("[PreJit-cold]"));
            }

            PAL_PerfJitDump_LogMethod((void*)methodRegionInfo.coldStartAddress, methodRegionInfo.coldSize, name.GetUTF8(), nullptr, nullptr);
        }
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

// Log a set of stub to the map.
void PerfMap::LogStubs(const char* stubType, const char* stubOwner, PCODE pCode, size_t codeSize)
{
    LIMITED_METHOD_CONTRACT;

    if (!s_enabled)
    {
        return;
    }

    // Logging failures should not cause any exceptions to flow upstream.
    EX_TRY
    {
        if(!stubOwner)
        {
            stubOwner = "?";
        }
        if(!stubType)
        {
            stubType = "?";
        }

        SString name;
        // Build the map file line.
        name.Printf("stub<%d> %s<%s>", ++(s_StubsMapped), stubType, stubOwner);
        SString line;
        line.Printf(FMT_CODE_ADDR " %x %s\n", pCode, codeSize, name.GetUTF8());

        {
            CrstHolder ch(&(s_csPerfMap));

            if(s_Current != nullptr)
            {
                s_Current->WriteLine(line);
            }

            PAL_PerfJitDump_LogMethod((void*)pCode, codeSize, name.GetUTF8(), nullptr, nullptr);
        }
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
}

void PerfMap::GetNativeImageSignature(PEAssembly * pPEAssembly, CHAR * pszSig, unsigned int nSigSize)
{
    CONTRACTL{
        PRECONDITION(pPEAssembly != nullptr);
        PRECONDITION(pszSig != nullptr);
        PRECONDITION(nSigSize >= GUID_STR_BUFFER_LEN);
    } CONTRACTL_END;

    // We use the MVID as the signature, since ready to run images
    // don't have a native image signature.
    GUID mvid;
    pPEAssembly->GetMVID(&mvid);
    if(!GuidToLPSTR(mvid, pszSig, nSigSize))
    {
        pszSig[0] = '\0';
    }
}
#endif // FEATURE_PERFMAP && !DACCESS_COMPILE
