// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: perfmap.cpp
//

#include "common.h"

#if defined(FEATURE_PERFMAP) && !defined(DACCESS_COMPILE)
#include <clrconfignocache.h>
#include "perfmap.h"
#include "gdbjithelpers.h"
#include "pal.h"
#include <dn-stdio.h>


// The code addresses are actually native image offsets during crossgen. Print
// them as 32-bit numbers for consistent output when cross-targeting and to
// make the output more compact.

#define FMT_CODE_ADDR "%p"

#ifndef __ANDROID__
#define TEMP_DIRECTORY_PATH "/tmp"
#else
// On Android, "/tmp/" doesn't exist; temporary files should go to
// /data/local/tmp/
#define TEMP_DIRECTORY_PATH "/data/local/tmp"
#endif

namespace
{
    thread_local int t_jitDumpDebugInfoCallbackDepth = 0;

    // Scope guard to prevent recursive callbacks into the debug-info delegate.
    class JitDumpDebugInfoCallbackScope
    {
    public:
        JitDumpDebugInfoCallbackScope()
            : _entered(t_jitDumpDebugInfoCallbackDepth == 0)
        {
            if (_entered)
            {
                t_jitDumpDebugInfoCallbackDepth = 1;
            }
        }

        ~JitDumpDebugInfoCallbackScope()
        {
            if (_entered)
            {
                t_jitDumpDebugInfoCallbackDepth = 0;
            }
        }

        bool Entered() const
        {
            return _entered;
        }

    private:
        bool _entered;
    };

    // RAII container for sequence points and locals returned from the managed helper.
    struct PerfMapMethodDebugInfo
    {
        SequencePointInfo* points;
        int size;
        LocalVarInfo* locals;
        int localsSize;

        PerfMapMethodDebugInfo(int numPoints, int numLocals)
        {
            points = (SequencePointInfo*)CoTaskMemAlloc(sizeof(SequencePointInfo) * numPoints);
            if (points == nullptr)
            {
                COMPlusThrowOM();
            }

            memset(points, 0, sizeof(SequencePointInfo) * numPoints);
            size = numPoints;

            if (numLocals == 0)
            {
                locals = nullptr;
                localsSize = 0;
                return;
            }

            locals = (LocalVarInfo*)CoTaskMemAlloc(sizeof(LocalVarInfo) * numLocals);
            if (locals == nullptr)
            {
                CoTaskMemFree(points);
                COMPlusThrowOM();
            }

            memset(locals, 0, sizeof(LocalVarInfo) * numLocals);
            localsSize = numLocals;
        }

        ~PerfMapMethodDebugInfo()
        {
            if (locals != nullptr)
            {
                for (int i = 0; i < localsSize; i++)
                {
                    CoTaskMemFree(locals[i].name);
                }

                CoTaskMemFree(locals);
            }

            if (points != nullptr)
            {
                for (int i = 0; i < size; i++)
                {
                    CoTaskMemFree(points[i].fileName);
                }

                CoTaskMemFree(points);
            }
        }

        MethodDebugInfo* AsMethodDebugInfo()
        {
            return reinterpret_cast<MethodDebugInfo*>(this);
        }
    };

    // Header for serialized JIT dump debug info payload.
    struct JitDumpDebugInfoPayloadHeader
    {
        uint64_t codeAddress;
        uint64_t entryCount;
    };

    // Entry representing one native offset to source line mapping.
    struct JitDumpDebugInfoEntry
    {
        uint64_t codeAddress;
        uint32_t lineNumber;
        uint32_t discriminator;
    };

    constexpr ULONG32 HiddenLineNumber = 0x00feefee;

    // Allocator used by DebugInfoManager when materializing offset maps.
    BYTE* PerfMapDebugInfoNew(void*, size_t cBytes)
    {
        WRAPPER_NO_CONTRACT;

        size_t mappingCount = cBytes / sizeof(ICorDebugInfo::OffsetMapping);
        _ASSERTE(mappingCount * sizeof(ICorDebugInfo::OffsetMapping) == cBytes);

        return reinterpret_cast<BYTE*>(new ICorDebugInfo::OffsetMapping[mappingCount]);
    }

    template <typename T>
    void AppendBytes(StackSArray<BYTE>& buffer, const T& value)
    {
        WRAPPER_NO_CONTRACT;

        COUNT_T previousCount = buffer.GetCount();
        buffer.SetCount(previousCount + static_cast<COUNT_T>(sizeof(T)));
        memcpy(buffer.GetElements() + previousCount, &value, sizeof(T));
    }

    void AppendBytes(StackSArray<BYTE>& buffer, const void* data, COUNT_T size)
    {
        WRAPPER_NO_CONTRACT;

        COUNT_T previousCount = buffer.GetCount();
        buffer.SetCount(previousCount + size);
        memcpy(buffer.GetElements() + previousCount, data, size);
    }

    // Build a serialized jitdump debug-info payload from sequence points for a method.
    bool TryGetMethodJitDumpDebugInfo(MethodDesc* pMethod, PCODE pCode, BYTE** payload, size_t* payloadSize)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_PREEMPTIVE;
            PRECONDITION(pMethod != nullptr);
            PRECONDITION(pCode != nullptr);
            PRECONDITION(payload != nullptr);
            PRECONDITION(payloadSize != nullptr);
        }
        CONTRACTL_END;

        *payload = nullptr;
        *payloadSize = 0;

        if (pMethod->IsLCGMethod() || pMethod->IsDynamicMethod())
        {
            return false;
        }

        Module* pModule = pMethod->GetModule();
        if (pModule == nullptr)
        {
            return false;
        }

        DebugInfoRequest request;
        request.InitFromStartingAddr(pMethod, PCODEToPINSTR(pCode));

        ULONG32 nativeMapCount = 0;
        NewArrayHolder<ICorDebugInfo::OffsetMapping> nativeMap(nullptr);
        if (!DebugInfoManager::GetBoundariesAndVars(
                request,
                PerfMapDebugInfoNew,
                nullptr,
                BoundsType::Uninstrumented,
                &nativeMapCount,
                &nativeMap,
                nullptr,
                nullptr))
        {
            return false;
        }

        if (nativeMapCount == 0)
        {
            return false;
        }

        if (getInfoForMethodDelegate == nullptr)
        {
            return false;
        }

        JitDumpDebugInfoCallbackScope callbackScope;
        if (!callbackScope.Entered())
        {
            return false;
        }

        SString modulePath{pModule->GetPEAssembly()->GetPath()};
        if (modulePath.IsEmpty())
        {
            return false;
        }

        PerfMapMethodDebugInfo methodDebugInfo(nativeMapCount, 0);
        if (getInfoForMethodDelegate(modulePath.GetUTF8(), pMethod->GetMemberDef(), methodDebugInfo.AsMethodDebugInfo()) == 0)
        {
            return false;
        }

        if (methodDebugInfo.size == 0)
        {
            return false;
        }

        StackSArray<BYTE> serializedPayload;
        JitDumpDebugInfoPayloadHeader header = {};
        header.codeAddress = static_cast<uint64_t>(reinterpret_cast<TADDR>(pCode));
        header.entryCount = 0;
        AppendBytes(serializedPayload, header);

        char currentFileName[4 * MAX_LONGPATH] = {};
        ULONG32 currentLineNumber = 0;

        int sequencePointIndex = 0;

        for (ULONG32 nativeMapIndex = 0; nativeMapIndex < nativeMapCount; nativeMapIndex++)
        {
            const ULONG32 ilOffset = nativeMap[nativeMapIndex].ilOffset;

            if ((ilOffset == (ULONG32)ICorDebugInfo::NO_MAPPING) ||
                (ilOffset == (ULONG32)ICorDebugInfo::PROLOG) ||
                (ilOffset == (ULONG32)ICorDebugInfo::EPILOG))
            {
                continue;
            }

            while (sequencePointIndex + 1 < methodDebugInfo.size &&
                   (ULONG32)methodDebugInfo.points[sequencePointIndex + 1].ilOffset <= ilOffset)
            {
                sequencePointIndex++;
            }

            while (sequencePointIndex > 0 &&
                   methodDebugInfo.points[sequencePointIndex].lineNumber == (int)HiddenLineNumber)
            {
                sequencePointIndex--;
            }

            if ((methodDebugInfo.points[sequencePointIndex].lineNumber == 0) ||
                (methodDebugInfo.points[sequencePointIndex].lineNumber == (int)HiddenLineNumber) ||
                (methodDebugInfo.points[sequencePointIndex].fileName == nullptr))
            {
                continue;
            }

            int convertedLength = WideCharToMultiByte(
                CP_UTF8,
                0,
                methodDebugInfo.points[sequencePointIndex].fileName,
                -1,
                currentFileName,
                ARRAY_SIZE(currentFileName),
                nullptr,
                nullptr);

            if (convertedLength == 0)
            {
                currentFileName[0] = '\0';
                continue;
            }

            currentLineNumber = (ULONG32)methodDebugInfo.points[sequencePointIndex].lineNumber;

            JitDumpDebugInfoEntry entry = {};
            entry.codeAddress = static_cast<uint64_t>(reinterpret_cast<TADDR>(pCode) + nativeMap[nativeMapIndex].nativeOffset);
            entry.lineNumber = currentLineNumber;
            entry.discriminator = 0;

            AppendBytes(serializedPayload, entry);
            AppendBytes(serializedPayload, currentFileName, static_cast<COUNT_T>(strlen(currentFileName) + 1));
            header.entryCount++;
        }

        if (header.entryCount == 0)
        {
            return false;
        }

        memcpy(serializedPayload.GetElements(), &header, sizeof(header));

        *payloadSize = serializedPayload.GetCount();
        *payload = new BYTE[*payloadSize];
        memcpy(*payload, serializedPayload.GetElements(), *payloadSize);
        return true;
    }
}

Volatile<bool> PerfMap::s_enabled = false;
Volatile<bool> PerfMap::s_dependenciesReady = false;
PerfMap * PerfMap::s_Current = nullptr;
bool PerfMap::s_ShowOptimizationTiers = false;
bool PerfMap::s_GroupStubsOfSameType = false;
bool PerfMap::s_IndividualAllocationStubReporting = false;
bool PerfMap::s_EmitDebugInfo = false;

unsigned PerfMap::s_StubsMapped = 0;
CrstStatic PerfMap::s_csPerfMap;

bool PerfMapLowGranularityStubs()
{
    return PerfMap::LowGranularityStubs();
}

// Initialize the map for the process - called from EEStartupHelper.
void PerfMap::Initialize()
{
    LIMITED_METHOD_CONTRACT;

    s_csPerfMap.Init(CrstPerfMap);

    PerfMapType perfMapType = (PerfMapType)CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapEnabled);
    PerfMap::Enable(perfMapType, false);
}

const char * PerfMap::InternalConstructPath()
{
#ifdef HOST_WINDOWS
    CLRConfigNoCache value = CLRConfigNoCache::Get("PerfMapJitDumpPath");
#else
    CLRConfigNoCache value = CLRConfigNoCache::Get("PerfMapJitDumpPath", /* noPrefix */ false, &PAL_getenv);
#endif
    if (value.IsSet())
    {
        return value.AsString();
    }
    return TEMP_DIRECTORY_PATH;
}

void PerfMap::InitializeConfiguration()
{
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapShowOptimizationTiers) != 0)
    {
        s_ShowOptimizationTiers = true;
    }

    DWORD granularity = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapStubGranularity);
    s_GroupStubsOfSameType = (granularity & 1) != 1;
    s_IndividualAllocationStubReporting = (granularity & 2) != 0;
    s_EmitDebugInfo = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapEmitDebugInfo) != 0;
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

        const char* basePath = InternalConstructPath();

        if (s_Current == nullptr && (type == PerfMapType::ALL || type == PerfMapType::PERFMAP))
        {
            s_Current = new PerfMap();
            int signalNum = (int) CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_PerfMapIgnoreSignal);

            if (signalNum > 0)
            {
                PAL_IgnoreProfileSignal(signalNum);
            }

            InitializeConfiguration();

            int currentPid = GetCurrentProcessId();
            s_Current->OpenFileForPid(currentPid, basePath);
            s_enabled = true;
        }

        if (!PAL_PerfJitDump_IsStarted() && (type == PerfMapType::ALL || type == PerfMapType::JITDUMP))
        {
            PAL_PerfJitDump_Start(basePath);

            InitializeConfiguration();

            s_enabled = true;
        }
    }

    if (sendExisting)
    {
        // When Enable is called very early in startup (e.g., via DiagnosticServer IPC before
        // SystemDomain::Attach and ExecutionManager::Init), the AppDomain and EEJitManager
        // may not exist yet. We use s_dependenciesReady (a Volatile<bool>) to guard against
        // this, rather than null-checking individual pointers which would have race conditions
        // due to non-Volatile statics like m_pEEJitManager.
        // Safe to skip: no assemblies are loaded and no code is JIT'd at that point.
        if (!s_dependenciesReady)
        {
            return;
        }

        AppDomain::AssemblyIterator assemblyIterator = GetAppDomain()->IterateAssembliesEx(
            (AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));
        CollectibleAssemblyHolder<Assembly *> pAssembly;
        while (assemblyIterator.Next(pAssembly.This()))
        {
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
                        if (hotDesc != nullptr && hotDesc->GetNativeCode() != (PCODE)NULL)
                        {
                            PerfMap::LogPreCompiledMethod(hotDesc, hotDesc->GetNativeCode());
                        }
                    }
                }
            }
        }

        {
#ifdef FEATURE_CODE_VERSIONING
            CodeVersionManager::LockHolder codeVersioningLockHolder;
#endif // FEATURE_CODE_VERSIONING
            CodeHeapIterator heapIterator = ExecutionManager::GetEEJitManager()->GetCodeHeapIterator();
            while (heapIterator.Next())
            {
                MethodDesc * pMethod = heapIterator.GetMethod();
                if (pMethod == nullptr)
                {
                    continue;
                }

                PCODE codeStart = PINSTRToPCODE(heapIterator.GetMethodCode());
#ifdef FEATURE_CODE_VERSIONING
                NativeCodeVersion nativeCodeVersion;
                nativeCodeVersion = pMethod->GetCodeVersionManager()->GetNativeCodeVersion(pMethod, codeStart);
                if (nativeCodeVersion.IsNull() && codeStart != pMethod->GetNativeCode())
                {
                    continue;
                }
#endif // FEATURE_CODE_VERSIONING

                EECodeInfo codeInfo(codeStart);
                IJitManager::MethodRegionInfo methodRegionInfo;
                codeInfo.GetMethodRegionInfo(&methodRegionInfo);
                _ASSERTE(methodRegionInfo.hotStartAddress == codeStart);
                _ASSERTE(methodRegionInfo.hotSize > 0);

#ifdef FEATURE_CODE_VERSIONING
                PrepareCodeConfig config(!nativeCodeVersion.IsNull() ? nativeCodeVersion : NativeCodeVersion(pMethod), FALSE, FALSE);
#else
                PrepareCodeConfig config(NativeCodeVersion(pMethod), FALSE, FALSE);
#endif // FEATURE_CODE_VERSIONING
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

// Signal that all dependencies (AppDomain, ExecutionManager) are ready.
// This method must be called before any code is JITed or restored from R2R image.
void PerfMap::SignalDependenciesReady()
{
    LIMITED_METHOD_CONTRACT;

    s_dependenciesReady = true;
}

// Construct a new map for the process.
PerfMap::PerfMap()
{
    LIMITED_METHOD_CONTRACT;

    // Initialize with no failures.
    m_ErrorEncountered = false;
}

// Clean-up resources.
PerfMap::~PerfMap()
{
    LIMITED_METHOD_CONTRACT;

    fclose(m_fp);
    m_fp = nullptr;
}

void PerfMap::OpenFileForPid(int pid, const char* basePath)
{
    SString fullPath;
    fullPath.Printf("%s/perf-%d.map", basePath, pid);

    // Open the map file for writing.
    OpenFile(fullPath);
}

// Open the specified destination map file.
void PerfMap::OpenFile(SString& path)
{
    STANDARD_VM_CONTRACT;

    // Open the file stream.
    if (fopen_lp(&m_fp, path.GetUnicode(), W("w")) != 0)
        m_fp = nullptr;
}

// Write a line to the map file.
void PerfMap::WriteLine(SString& line)
{
    STANDARD_VM_CONTRACT;
#ifdef _DEBUG
    _ASSERTE(s_csPerfMap.OwnedByCurrentThread());
#endif

    if (m_fp == nullptr || m_ErrorEncountered)
    {
        return;
    }

    EX_TRY
    {
        // Write the line.
        if (fprintf(m_fp, "%s", line.GetUTF8()) < 0)
        {
            // This will cause us to stop writing to the file.
            // The file will still remain open until shutdown so that we don't have to take a lock at this level when we touch the file stream.
            m_ErrorEncountered = true;
        }
    }
    EX_CATCH{} EX_END_CATCH
}

void PerfMap::LogJITCompiledMethod(MethodDesc * pMethod, PCODE pCode, size_t codeSize, PrepareCodeConfig *pConfig)
{
    LIMITED_METHOD_CONTRACT;

    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
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

    NewArrayHolder<BYTE> jitDumpDebugInfo(nullptr);
    size_t jitDumpDebugInfoSize = 0;

    // Logging failures should not cause any exceptions to flow upstream.
    EX_TRY
    {
        if (PAL_PerfJitDump_IsStarted() && s_EmitDebugInfo)
        {
            TryGetMethodJitDumpDebugInfo(pMethod, pCode, &jitDumpDebugInfo, &jitDumpDebugInfoSize);
        }

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

            PAL_PerfJitDump_LogMethod(
                (void*)pCode,
                codeSize,
                name.GetUTF8(),
                jitDumpDebugInfo.GetValue(),
                jitDumpDebugInfoSize,
                nullptr,
                0,
                /*reportCodeBlock*/true);
        }
    }
    EX_CATCH{} EX_END_CATCH

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
            PAL_PerfJitDump_LogMethod((void*)methodRegionInfo.hotStartAddress, methodRegionInfo.hotSize, name.GetUTF8(), nullptr, 0, nullptr, 0, /*reportCodeBlock*/true);
        }

        if (methodRegionInfo.coldSize > 0)
        {
            CrstHolder ch(&(s_csPerfMap));

            if (s_ShowOptimizationTiers)
            {
                pMethod->GetFullMethodInfo(name);
                name.Append(W("[PreJit-cold]"));
            }

            PAL_PerfJitDump_LogMethod((void*)methodRegionInfo.coldStartAddress, methodRegionInfo.coldSize, name.GetUTF8(), nullptr, 0, nullptr, 0, /*reportCodeBlock*/true);
        }
    }
    EX_CATCH{} EX_END_CATCH
}

// Log a set of stub to the map.
void PerfMap::LogStubs(const char* stubType, const char* stubOwner, PCODE pCode, size_t codeSize, PerfMapStubType stubAllocationType)
{
    LIMITED_METHOD_CONTRACT;

    if (!s_enabled)
    {
        return;
    }

    if (stubAllocationType != PerfMapStubType::Individual)
    {
        if ((stubAllocationType == PerfMapStubType::IndividualWithinBlock) != s_IndividualAllocationStubReporting)
        {
            return;
        }
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
        if (s_GroupStubsOfSameType)
        {
            name.Printf("stub %s<%s>", stubType, stubOwner);
        }
        else
        {
            name.Printf("stub<%d> %s<%s>", ++(s_StubsMapped), stubType, stubOwner);
        }
        SString line;
        line.Printf(FMT_CODE_ADDR " %x %s\n", pCode, codeSize, name.GetUTF8());

        {
            CrstHolder ch(&(s_csPerfMap));

            if(s_Current != nullptr)
            {
                s_Current->WriteLine(line);
            }

            // For block-level stub allocations, the memory may be reserved but not yet committed.
            // Emitting code bytes in that case can cause jitdump logging to fail, and the bytes
            // are optional in the jitdump specification.
            //
            // Even when the memory is committed, block-level stubs are reported at commit time
            // before the actual stub code has been written, so the code bytes would be zeros or
            // uninitialized. We therefore skip code bytes for block allocations entirely.
            PAL_PerfJitDump_LogMethod((void*)pCode, codeSize, name.GetUTF8(), nullptr, 0, nullptr, 0, /*reportCodeBlock*/ stubAllocationType != PerfMapStubType::Block);
        }
    }
    EX_CATCH{} EX_END_CATCH
}

void PerfMap::GetNativeImageSignature(PEAssembly * pPEAssembly, CHAR * pszSig, unsigned int nSigSize)
{
    CONTRACTL{
        PRECONDITION(pPEAssembly != nullptr);
        PRECONDITION(pszSig != nullptr);
        PRECONDITION(nSigSize >= MINIPAL_GUID_BUFFER_LEN);
    } CONTRACTL_END;

    // We use the MVID as the signature, since ready to run images
    // don't have a native image signature.
    GUID mvid;
    pPEAssembly->GetMVID(&mvid);
    minipal_guid_as_string(mvid, pszSig, nSigSize);
}

void ReportStubBlock(void* start, size_t size, StubCodeBlockKind kind)
{
    WRAPPER_NO_CONTRACT;

    PerfMap::LogStubs(__FUNCTION__, GetStubCodeBlockKindString(kind), (PCODE)start, size, PerfMapStubType::Block);
}

#endif // FEATURE_PERFMAP && !DACCESS_COMPILE
