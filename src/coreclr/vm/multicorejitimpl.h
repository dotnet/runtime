// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: MultiCoreJITImpl.h
//

//
// Multicore JIT internal implementation header file
//
// ======================================================================================

#ifdef _DEBUG

#define MULTICOREJIT_LOGGING

#else
// Enable direct logging through OutputDebugMessage in ret build for perf investigation, to be disabled when checkin

// #define MULTICOREJIT_LOGGING

#endif

// Bits 0xff0000 are reserved method flags. Currently only first bit is used.
const unsigned METHOD_FLAGS_MASK       = 0xff0000;
const unsigned JIT_BY_APP_THREAD_TAG   = 0x10000;   // tag, that indicates whether method is jitted by application thread(1) or background thread(0)
// Tags 0xfe0000 are currently free

const unsigned RECORD_TYPE_OFFSET      = 24;        // offset of type of record

const unsigned MAX_MODULES             = 0x1000;    // maximum allowed number of modules (2^12 values)
const unsigned MODULE_MASK             = 0xffff;    // mask to get module index from packed data

const unsigned MODULE_LEVEL_OFFSET     = 16;        // offset of module load level
const unsigned MAX_MODULE_LEVELS       = 0x100;     // maximum allowed number of module levels (2^8 values)

const unsigned MAX_METHODS             = 0x4000;    // Maximum allowed number of methods (2^14 values) (in principle this is also limited by "unsigned short" counters)

const unsigned SIGNATURE_LENGTH_MASK   = 0xffff;    // mask to get signature from packed data (2^16-1 max signature length)

const int      HEADER_W_COUNTER  = 14;              // Extra 16-bit counters in header for statistics: 28
const int      HEADER_D_COUNTER  = 3;               // Extra 32-bit counters in header for statistics: 12

const int      MULTICOREJITLIFE  = 60 * 1000;       // 60 seconds
const int      MAX_WALKBACK      = 128;

enum
{
    MULTICOREJIT_PROFILE_VERSION   = 102,

    MULTICOREJIT_HEADER_RECORD_ID           = 1,
    MULTICOREJIT_MODULE_RECORD_ID           = 2,
    MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID = 3,
    MULTICOREJIT_METHOD_RECORD_ID           = 4,
    MULTICOREJIT_GENERICMETHOD_RECORD_ID    = 5,
};

inline unsigned Pack8_24(unsigned up, unsigned low)
{
    LIMITED_METHOD_CONTRACT;

    return (up << 24) + low;
}

// Multicore JIT profile format.
//
// <profile>::= <HeaderRecord> { <ModuleRecord> | <JitInfRecord> }
//
//  1. Each record is DWORD aligned
//  2. Each record starts with a 1 byte recordType identifier
//  3. Counter are just statistical information gathed (mainly during play back), good for quick diagnosis, not used to guide playback
//  4. Maximum number of modules supported is MAX_MODULES
//  5. Maximum number of methods supported is MAX_METHODS
//  6. Simple module name stored
//  7. Method flag JIT_BY_APP_THREAD is for diagnosis only
//
// <HeaderRecord>::=     <recordType=MULTICOREJIT_HEADER_RECORD_ID> <3byte_recordSize> <version> <timeStamp> <moduleCount> <methodCount> <DependencyCount> <unsigned short counter>*14 <unsigned counter>*3
// <ModuleRecord>::=     <recordType=MULTICOREJIT_MODULE_RECORD_ID> <3byte_recordSize> <ModuleVersion> <JitMethodCount> <loadLevel> <lenModuleName> char*lenModuleName <padding>
// <ModuleDependency>::= <recordType=MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID> <loadLevel_1byte> <moduleIndex_2bytes>
// <GenericMethod>::=    <recordType=MULTICOREJIT_GENERICMETHOD_RECORD_ID> <methodFlags_1byte> <moduleIndex_2byte> <sigSize_2byte> <signature> <optional padding>
// <NonGenericMethod>::= <recordType=MULTICOREJIT_METHOD_RECORD_ID> <methodFlags_1byte> <moduleIndex_2byte> <methodToken_4byte>
//
//
// Actual profile has two representations: internal and the one, that is stored in file.
//
// I. Internal profile
//
//   Internal profile representation is stored in m_JitInfoArray and is used during profile gathering.
//   m_JitInfoArray is an array of RecorderInfo (12 bytes on 32-bit systems, 16 bytes on 64-bit systems), with MAX_METHODS elements.
//
//   1. Modules.
//     For modules RecorderInfo::data2 and RecorderInfo::ptr are set to 0. RecorderInfo::ptr == 0 is also a flag that RecorderInfo correponds to module.
//     RecorderInfo::data1 is non-zero and represents info for module.
//
//     Info for module includes module index and requested load level, with some additional data in higher bits (MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID tag).
//     - bits 0-15 store module index
//     - bits 16-23 store load level
//     - bits 24-31 store tag (MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID)
//
//   2. Methods.
//     For methods RecorderInfo::data2 is set to 0.
//     RecorderInfo::ptr is set to pointer to MethodDesc.
//     RecorderInfo::data1 is non-zero and represents additional info for method.
//
//     Info for method includes module index and method flags (like JIT_BY_APP_THREAD_TAG, etc.), with some additional data in higher bits (tag).
//     - bits 0-15 store module index
//     - bits 16-23 store method flags
//     - bits 24-31 store tag (MULTICOREJIT_METHOD_RECORD_ID or MULTICOREJIT_GENERICMETHOD_RECORD_ID).
//
// II. Profile in file
//
//   Preprocessing is performed right before profile saving to file.
//
//   1. Modules.
//     For modules, no preprocessing of RecorderInfo is required, RecorderInfo::data1 is written to file as JifInfRecord.
//
//   2. Methods.
//     2.1. For generic methods, binary signature is computed and RecorderInfo contents are changed.
//       a) RecorderInfo::data1 doesn't change.
//       b) RecorderInfo::data2 stores signature length.
//       c) RecorderInfo::ptr is replaced with pointer to method's binary signature.
//
//     2.2 For non-generic methods, method token is obtained and RecorderInfo contents are changed.
//       a) RecorderInfo::data1 doesn't change.
//       b) RecorderInfo::data2 stores method token.
//       c) RecorderInfo::ptr doesn't change.
//
//     File write order for generic methods: RecorderInfo::data1, RecorderInfo::data2, signature, extra alignment (this is optional). All of these represent JitInfRecord.
//     File write order for non-generic methods: RecorderInfo::data1, RecorderInfo::data2. All of these represent JitInfRecord.

struct HeaderRecord
{
    unsigned        recordID;
    unsigned        version;
    unsigned        timeStamp;
    unsigned        moduleCount;
    unsigned        methodCount;
    unsigned        moduleDepCount;
    unsigned short  shortCounters[HEADER_W_COUNTER];
    unsigned        longCounters [HEADER_D_COUNTER];
};


class ModuleVersion
{
public:
    unsigned short major;
    unsigned short minor;
    unsigned short build;
    unsigned short revision;

    unsigned       versionFlags         :31;
    unsigned       hasNativeImage:1;

    GUID           mvid;

    bool GetModuleVersion(Module * pModule);

    ModuleVersion()
    {
        LIMITED_METHOD_CONTRACT;

        memset(this, 0, sizeof(ModuleVersion));
    }

    bool MatchWith(const ModuleVersion & other) const
    {
        LIMITED_METHOD_CONTRACT;

        if ((major    == other.major) &&
            (minor    == other.minor) &&
            (build    == other.build) &&
            (revision == other.revision) &&
            (versionFlags    == other.versionFlags))
        {
            return memcmp(& mvid, & other.mvid, sizeof(mvid)) == 0;
        }

        return false;
    }

    bool NativeImageFlagDiff(const ModuleVersion & other) const
    {
        LIMITED_METHOD_CONTRACT;

        return hasNativeImage != other.hasNativeImage;
    }
};

inline unsigned RoundUp(unsigned val)
{
    LIMITED_METHOD_CONTRACT;

    return (val + 3) / 4 * 4;
}

// Used to mark a module that was loaded in the LOADFROM context.
// First 16 bits are reserved for CorAssemblyFlags.  Use the last bit (bit 31) to allow for expansion of CorAssemblyFlags.
const unsigned int VERSIONFLAG_LOADCTX_LOADFROM = 0x40000000;

// Module record stored in the profile without the name

class ModuleRecord
{
public:
    unsigned       recordID;
    ModuleVersion  version;
    unsigned short jitMethodCount;
    unsigned short flags;
    unsigned short wLoadLevel;
    unsigned short lenModuleName;
    unsigned short lenAssemblyName;

    ModuleRecord(unsigned lenName = 0, unsigned lenAssemblyName = 0);

    bool MatchWithModule(ModuleVersion & version, bool & gotVersion, Module * pModule, bool & shouldAbort) const;

    unsigned ModuleNameLen() const
    {
        LIMITED_METHOD_CONTRACT;

        return lenModuleName;
    }

    const char * GetModuleName() const
    {
        LIMITED_METHOD_CONTRACT;

        return (const char *) (this + 1); // after this record
    }

    unsigned AssemblyNameLen() const
    {
        LIMITED_METHOD_CONTRACT;

        return lenAssemblyName;
    }

    const char * GetAssemblyName() const
    {
        return GetModuleName() + RoundUp(lenModuleName); // after the module name
    }

    void SetLoadLevel(FileLoadLevel loadLevel)
    {
        LIMITED_METHOD_CONTRACT;

        wLoadLevel = (unsigned short) loadLevel;
    }
};


class Module;
class AppDomain;

class PlayerModuleInfo;

// Module enumerator
class MulticoreJitModuleEnumerator
{
    virtual HRESULT OnModule(Module * pModule) = 0;

public:
    HRESULT EnumerateLoadedModules(AppDomain * pDomain);
    HRESULT HandleAssembly(DomainAssembly * pAssembly);
};


class PlayerModuleInfo;

// MulticoreJitProfilePlayer manages background thread, playing back profile, storing result into code stoage, and gather statistics information

class MulticoreJitProfilePlayer
{
friend class MulticoreJitRecorder;

private:
    ICLRPrivBinder * m_pBinderContext;
    LONG                               m_nMySession;
    unsigned                           m_nStartTime;
    BYTE                             * m_pFileBuffer;
    unsigned                           m_nFileSize;
    MulticoreJitPlayerStat           & m_stats;
    MulticoreJitCounter              & m_appdomainSession;
    bool                               m_shouldAbort;

    Thread                           * m_pThread;

    unsigned                           m_nBlockingCount;
    unsigned                           m_nMissingModule;

    int                                m_nLoadedModuleCount;

    unsigned                           m_headerModuleCount;
    unsigned                           m_moduleCount;
    PlayerModuleInfo                 * m_pModules;

    HRESULT HandleModuleRecord(const ModuleRecord * pMod);
    HRESULT HandleModuleInfoRecord(unsigned moduleTo, unsigned level);
    HRESULT HandleNonGenericMethodInfoRecord(unsigned moduleIndex, unsigned token);
    HRESULT HandleGenericMethodInfoRecord(unsigned moduleIndex, BYTE * signature, unsigned length);
    void CompileMethodInfoRecord(Module *pModule, MethodDesc *pMethod, bool isGeneric);

    bool CompileMethodDesc(Module * pModule, MethodDesc * pMD);
    HRESULT PlayProfile();

    bool ShouldAbort(bool fast) const;

    HRESULT JITThreadProc(Thread * pThread);

    static DWORD WINAPI StaticJITThreadProc(void *args);

    void TraceSummary();

    HRESULT UpdateModuleInfo();

    HRESULT ReadCheckFile(const WCHAR * pFileName);

    DomainAssembly * LoadAssembly(SString & assemblyName);

public:

    MulticoreJitProfilePlayer(ICLRPrivBinder * pBinderContext, LONG nSession);

    ~MulticoreJitProfilePlayer();

    HRESULT ProcessProfile(const WCHAR * pFileName);

    HRESULT OnModule(Module * pModule);

    Module * GetModuleFromIndex(DWORD ix) const;
};


struct RecorderModuleInfo
{
    Module       *  pModule;
    unsigned short  methodCount;
    unsigned short  flags;
    ModuleVersion   moduleVersion;
    SBuffer         simpleName;
    SBuffer         assemblyName;
    FileLoadLevel   loadLevel;

    RecorderModuleInfo()
    {
        LIMITED_METHOD_CONTRACT;

        pModule     = NULL;
        methodCount = 0;
        flags       = 0;
        loadLevel   = FILE_LOAD_CREATE;
    }

    bool SetModule(Module * pModule);
};

struct RecorderInfo
{
    unsigned data1;
    unsigned data2;
    BYTE *   ptr;

    RecorderInfo()
    {
        LIMITED_METHOD_CONTRACT;

        data1 = 0;
        data2 = 0;
        ptr  = nullptr;
    }

    bool IsPartiallyInitialized()
    {
        LIMITED_METHOD_CONTRACT;

        return data1 != 0;
    }

    bool IsGenericMethodInfo()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsPartiallyInitialized());
        return (data1 >> RECORD_TYPE_OFFSET) == MULTICOREJIT_GENERICMETHOD_RECORD_ID;
    }

    bool IsNonGenericMethodInfo()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsPartiallyInitialized());
        return (data1 >> RECORD_TYPE_OFFSET) == MULTICOREJIT_METHOD_RECORD_ID;
    }

    bool IsMethodInfo()
    {
        LIMITED_METHOD_CONTRACT;

        return IsGenericMethodInfo() || IsNonGenericMethodInfo();
    }

    bool IsModuleInfo()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsPartiallyInitialized());
        bool ret = (data1 >> RECORD_TYPE_OFFSET) == MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID;
        _ASSERTE(ret == !IsMethodInfo());
        return ret;
    }

    bool IsFullyInitialized()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsPartiallyInitialized());

        if (IsModuleInfo())
        {
            return true;
        }
        else
        {
            if (IsNonGenericMethodInfo())
            {
                return data2 != 0;
            }
            else
            {
                return data2 != 0 && ptr != nullptr;
            }
        }
    }

    unsigned GetRawModuleData()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsModuleInfo());
        return data1;
    }

    unsigned GetModuleIndex()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsModuleInfo());
        return data1 & MODULE_MASK;
    }

    int GetModuleLoadLevel()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsModuleInfo());
        return (data1 >> MODULE_LEVEL_OFFSET) & (MAX_MODULE_LEVELS - 1);
    }

    unsigned GetRawMethodData1()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsMethodInfo());
        return data1;
    }

    unsigned GetRawMethodData2NonGeneric()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsNonGenericMethodInfo());
        return data2;
    }

    unsigned short GetRawMethodData2Generic()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsGenericMethodInfo());
        _ASSERTE(data2 < SIGNATURE_LENGTH_MASK + 1);
        return (unsigned short) data2;
    }

    BYTE * GetRawMethodSignature()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsGenericMethodInfo());
        return ptr;
    }

    unsigned GetMethodSignatureSize()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsGenericMethodInfo());
        _ASSERTE(IsFullyInitialized());

        return data2;
    }

    unsigned GetMethodRecordPaddingSize()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsGenericMethodInfo());
        _ASSERTE(IsFullyInitialized());

        unsigned unalignedrecSize = GetMethodSignatureSize() + sizeof(DWORD) + sizeof(unsigned short);
        unsigned recSize = AlignUp(unalignedrecSize, sizeof(DWORD));
        unsigned paddingSize = recSize - unalignedrecSize;
        _ASSERTE(paddingSize < sizeof(unsigned));

        return paddingSize;
    }

    MethodDesc * GetMethodDescAndClean()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsMethodInfo());
        _ASSERTE(data2 == 0);
        _ASSERTE(ptr != nullptr);

        MethodDesc * ret = (MethodDesc*) ptr;
        ptr = nullptr;

        return ret;
    }

    void PackSignatureForGenericMethod(BYTE *pSignature, unsigned signatureLength)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsGenericMethodInfo());
        _ASSERTE(data2 == 0);
        _ASSERTE(ptr == nullptr);

        _ASSERTE(pSignature != nullptr);
        _ASSERTE(signatureLength > 0);

        data2 = signatureLength & SIGNATURE_LENGTH_MASK;
        ptr = pSignature;

        _ASSERTE(IsFullyInitialized());
    }

    void PackTokenForNonGenericMethod(unsigned token)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(IsNonGenericMethodInfo());
        _ASSERTE(data2 == 0);
        _ASSERTE(ptr == nullptr);

        data2 = token;

        _ASSERTE(IsFullyInitialized());
    }

    void PackMethod(unsigned moduleIndex, MethodDesc * pMethod, bool application)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(data1 == 0);
        _ASSERTE(data2 == 0);
        _ASSERTE(ptr == nullptr);

        _ASSERTE(moduleIndex < MAX_MODULES);
        _ASSERTE(pMethod != NULL);

        unsigned tag = MULTICOREJIT_METHOD_RECORD_ID;

        if (!pMethod->IsTypicalSharedInstantiation())
        {
            // Generic method
            tag = MULTICOREJIT_GENERICMETHOD_RECORD_ID;
        }

        data1 = Pack8_24(tag, moduleIndex);

        if (application)
        {
             // Jitted by application threads, not background thread
            data1 |= JIT_BY_APP_THREAD_TAG;
        }

        data2 = 0;
        // To avoid recording overhead, records only pointer to MethodDesc.
        ptr = (BYTE *) pMethod;

        _ASSERTE(IsMethodInfo());
    }

    void PackModule(FileLoadLevel needLevel, unsigned moduleIndex)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(data2 == 0);
        _ASSERTE(ptr == nullptr);

        _ASSERTE(((unsigned) needLevel) < MAX_MODULE_LEVELS);
        _ASSERTE(moduleIndex < MAX_MODULES);

        data1 = Pack8_24(MULTICOREJIT_MODULEDEPENDENCY_RECORD_ID, ((unsigned) needLevel << MODULE_LEVEL_OFFSET) | moduleIndex);
        data2 = 0;
        ptr = nullptr;

        _ASSERTE(IsModuleInfo());
    }
};

class MulticoreJitRecorder
{
private:
    AppDomain               * m_pDomain;            // AutoStartProfile could be called from SystemDomain
    ICLRPrivBinder * m_pBinderContext;
    SString                   m_fullFileName;
    MulticoreJitPlayerStat  & m_stats;

    RecorderModuleInfo        * m_ModuleList;
    unsigned                  m_ModuleCount;
    unsigned                  m_ModuleDepCount;

    RecorderInfo              * m_JitInfoArray;
    LONG                      m_JitInfoCount;

    bool                      m_fFirstMethod;
    bool                      m_fAborted;

#ifndef TARGET_UNIX
    static TP_TIMER         * s_delayedWriteTimer;
#endif // !TARGET_UNIX

    unsigned FindModule(Module * pModule);
    unsigned GetOrAddModuleIndex(Module * pModule);

    HRESULT WriteModuleRecord(IStream * pStream,  const RecorderModuleInfo & module);

    void RecordMethodInfo(unsigned moduleIndex, MethodDesc * pMethod, bool application);
    unsigned RecordModuleInfo(Module * pModule);
    void RecordOrUpdateModuleInfo(FileLoadLevel needLevel, unsigned moduleIndex);

    void AddAllModulesInAsm(DomainAssembly * pAssembly);

    HRESULT WriteOutput(IStream * pStream);

    HRESULT WriteOutput();

    void PreRecordFirstMethod();

#ifndef TARGET_UNIX
    static void CALLBACK WriteMulticoreJitProfiler(PTP_CALLBACK_INSTANCE pInstance, PVOID pvContext, PTP_TIMER pTimer);
#endif // !TARGET_UNIX

public:

    MulticoreJitRecorder(AppDomain * pDomain, ICLRPrivBinder * pBinderContext, bool fRecorderActive)
        : m_stats(pDomain->GetMulticoreJitManager().GetStats())
        , m_ModuleList(nullptr)
        , m_JitInfoArray(nullptr)
    {
        LIMITED_METHOD_CONTRACT;

        m_pDomain           = pDomain;
        m_pBinderContext    = pBinderContext;

        if (fRecorderActive)
        {
            m_ModuleList        = new (nothrow) RecorderModuleInfo[MAX_MODULES];
        }
        m_ModuleCount       = 0;

        m_ModuleDepCount    = 0;

        if (fRecorderActive)
        {
            m_JitInfoArray      = new (nothrow) RecorderInfo[MAX_METHODS];
        }
        m_JitInfoCount      = 0;

        m_fFirstMethod      = true;
        m_fAborted          = false;


        m_stats.Clear();
    }

#ifndef TARGET_UNIX
    static void CloseTimer()
    {
        LIMITED_METHOD_CONTRACT;

        TP_TIMER * pTimer = InterlockedExchangeT(& s_delayedWriteTimer, NULL);
        if (pTimer != NULL)
        {
            CloseThreadpoolTimer(pTimer);
        }
    }
#endif // !TARGET_UNIX

    ~MulticoreJitRecorder()
    {
        LIMITED_METHOD_CONTRACT;

        delete[] m_ModuleList;
        delete[] m_JitInfoArray;

#ifndef TARGET_UNIX
        CloseTimer();
#endif // !TARGET_UNIX
    }

    bool CanGatherProfile()
    {
        LIMITED_METHOD_CONTRACT;

        return m_ModuleList != NULL && m_JitInfoArray != NULL;
    }

    bool IsAtFullCapacity() const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_JitInfoCount >= (LONG) MAX_METHODS) ||
               (m_ModuleCount  >= MAX_MODULES);
    }

    void RecordMethodJitOrLoad(MethodDesc * pMethod, bool application);

    MulticoreJitCodeInfo RequestMethodCode(MethodDesc * pMethod, MulticoreJitManager * pManager);

    HRESULT StartProfile(const WCHAR * pRoot, const WCHAR * pFileName, int suffix, LONG nSession);

    HRESULT StopProfile(bool appDomainShutdown);

    void AbortProfile();

    void RecordModuleLoad(Module * pModule, FileLoadLevel loadLevel);

    void AddModuleDependency(Module * pModule, FileLoadLevel loadLevel);

    DWORD EncodeModule(Module * pReferencedModule);
};

#ifdef MULTICOREJIT_LOGGING

void _MulticoreJitTrace(const char * format, ...);

#define MulticoreJitTrace(x)      do { _MulticoreJitTrace x; } while (0)

#else

#define MulticoreJitTrace(x)

#endif

extern unsigned g_MulticoreJitDelay;                             // Delay in StartProfile
extern bool     g_MulticoreJitEnabled;                           // Enable/Disable feature


inline bool PrivateEtwEnabled()
{
#ifdef FEATURE_EVENT_TRACE
    return ETW_PROVIDER_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER) != 0;
#else // FEATURE_EVENT_TRACE
    return FALSE;
#endif // FEATURE_EVENT_TRACE
}

void MulticoreJitFireEtw(const WCHAR * pAction, const WCHAR * pTarget, int p1, int p2, int p3);

void MulticoreJitFireEtwA(const WCHAR * pAction, const char * pTarget, int p1, int p2, int p3);

void MulticoreJitFireEtwMethodCodeReturned(MethodDesc * pMethod);

#define _FireEtwMulticoreJit(String1, String2, Int1, Int2, Int3)  if (PrivateEtwEnabled()) MulticoreJitFireEtw (String1, String2, Int1, Int2, Int3)
#define _FireEtwMulticoreJitA(String1, String2, Int1, Int2, Int3) if (PrivateEtwEnabled()) MulticoreJitFireEtwA(String1, String2, Int1, Int2, Int3)
#define _FireEtwMulticoreJitMethodCodeReturned(pMethod) if(PrivateEtwEnabled()) MulticoreJitFireEtwMethodCodeReturned(pMethod)
