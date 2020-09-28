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

// Make sure a record can fit within 2048 bytes, 511 methods now

const int      MAX_RECORD_SIZE   = 2048;
const unsigned MAX_JIT_COUNT     = (MAX_RECORD_SIZE - sizeof(unsigned)) / sizeof(unsigned);

const int      HEADER_W_COUNTER  = 14;              // Extra 16-bit counters in header for statistics: 28
const int      HEADER_D_COUNTER  = 3;               // Extra 32-bit counters in header for statistics: 12
const unsigned MAX_MODULES       = 512;             // Maximum number of modules

const unsigned MAX_METHOD_ARRAY  = 16384;           // Maximum number of methods

const int      MULTICOREJITLIFE  = 60 * 1000;       // 60 seconds

const int      MULTICOREJITBLOCKLIMIT = 10 * 1000;  // 10 seconds

                                                    //  8-bit module index

                                                    // Method JIT information: 8-bit module 4-bit flag 20-bit method index
const unsigned MODULE_DEPENDENCY = 0x800000;        //  1-bit module dependency mask
const unsigned JIT_BY_APP_THREAD = 0x400000;        //  1-bit application thread

const unsigned METHODINDEX_MASK  = 0x0FFFFF;        // 20-bit method index

                                                    // Dependendy information: 8-bit module 4-bit flag 4-bit unused 8-bit level 8-bit module
const unsigned LEVEL_SHIFT       = 8;
const unsigned LEVEL_MASK        = 0xFF;            //  8-bit file load level
const unsigned MODULE_MASK       = 0xFF;            //  8-bit dependent module index

const int      MAX_WALKBACK      = 128;

enum
{
    MULTICOREJIT_PROFILE_VERSION   = 101,

    MULTICOREJIT_HEADER_RECORD_ID  = 1,
    MULTICOREJIT_MODULE_RECORD_ID  = 2,
    MULTICOREJIT_JITINF_RECORD_ID  = 3
};


inline unsigned Pack8_24(unsigned up, unsigned low)
{
    LIMITED_METHOD_CONTRACT;

    return (up << 24) + low;
}

// Multicore JIT profile format

// <profile>::= <HeaderRecord> { <ModuleRecord> | <JitInfRecord> }
//
//  1. Each record is DWORD aligned
//  2. Each record starts with a DWORD <recordID> with Pack8_24(record type, record size)
//  3. Counter are just statistical information gathed (mainly during play back), good for quick diagnosis, not used to guide playback
//  4  Maximum number of modules supported is 256
//  5  Simple module name stored
//  6  Maximum method index: 20-bit, could extend to 22 bits
//  7  JIT_BY_APP_THREAD is for diagnosis only

// <HeaderRecord>::= <recordID> <version> <timeStamp> <moduleCount> <methodCount> <DependencyCount> <unsigned short counter>*14 <unsigned counter>*3
// <ModuleRecord>::= <recordID> <ModuleVersion> <JitMethodCount> <loadLevel> <lenModuleName> char*lenModuleName <padding>
// <JifInfRecord>::= <recordID> { <moduleDependency> | <methodJitInfo> }

// <moduleDependency>::
//    8-bit source module index,  current always 0 until we track per module dependency
//    8-bit flag                  MODULE_DEPENDENCY is 1
//    8-bit load level
//    8-bit target module index

// <methodJitInfo>::
//    8-bit module index,         current always 0 until we track per module dependency
//    4-bit flag                  MODULE_DEPENDENCY is 0, JIT_BY_APP_THREAD could be 1
//   20-bit method index


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

    bool MatchWithModule(ModuleVersion & version, bool & gotVersion, Module * pModule, bool & shouldAbort, bool fAppx) const;

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
    bool                               m_fAppxMode;

    Thread                           * m_pThread;

    unsigned                           m_nBlockingCount;
    unsigned                           m_nMissingModule;

    int                                m_nLoadedModuleCount;

    unsigned                           m_busyWith;

    unsigned                           m_headerModuleCount;
    unsigned                           m_moduleCount;
    PlayerModuleInfo                 * m_pModules;

    void JITMethod(Module * pModule, unsigned methodIndex);

    HRESULT HandleModuleRecord(const ModuleRecord * pModule);
    HRESULT HandleMethodRecord(unsigned * buffer, int count);

    bool CompileMethodDesc(Module * pModule, MethodDesc * pMD);

    HRESULT PlayProfile();

    bool GroupWaitForModuleLoad(int pos);

    bool ShouldAbort(bool fast) const;

    HRESULT JITThreadProc(Thread * pThread);

    static DWORD WINAPI StaticJITThreadProc(void *args);

    void TraceSummary();

    HRESULT UpdateModuleInfo();

    bool HandleModuleDependency(unsigned jitInfo);

    HRESULT ReadCheckFile(const WCHAR * pFileName);

    DomainAssembly * LoadAssembly(SString & assemblyName);

public:

    MulticoreJitProfilePlayer(ICLRPrivBinder * pBinderContext, LONG nSession, bool fAppxMode);

    ~MulticoreJitProfilePlayer();

    HRESULT ProcessProfile(const WCHAR * pFileName);

    HRESULT OnModule(Module * pModule);
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


class MulticoreJitRecorder
{
private:
    AppDomain               * m_pDomain;            // AutoStartProfile could be called from SystemDomain
    ICLRPrivBinder * m_pBinderContext;
    SString                   m_fullFileName;
    MulticoreJitPlayerStat  & m_stats;

    RecorderModuleInfo        m_ModuleList[MAX_MODULES];
    unsigned                  m_ModuleCount;
    unsigned                  m_ModuleDepCount;

    unsigned                  m_JitInfoArray[MAX_METHOD_ARRAY];
    LONG                      m_JitInfoCount;

    bool                      m_fFirstMethod;
    bool                      m_fAborted;
    bool                      m_fAppxMode;

#ifndef TARGET_UNIX
    static TP_TIMER         * s_delayedWriteTimer;
#endif // !TARGET_UNIX


    unsigned FindModule(Module * pModule);
    unsigned GetModuleIndex(Module * pModule);

    HRESULT WriteModuleRecord(IStream * pStream,  const RecorderModuleInfo & module);

    void RecordJitInfo(unsigned module, unsigned method);

    void AddAllModulesInAsm(DomainAssembly * pAssembly);

    HRESULT WriteOutput(IStream * pStream);

    HRESULT WriteOutput();

    void PreRecordFirstMethod();

#ifndef TARGET_UNIX
    static void CALLBACK WriteMulticoreJitProfiler(PTP_CALLBACK_INSTANCE pInstance, PVOID pvContext, PTP_TIMER pTimer);
#endif // !TARGET_UNIX

public:

    MulticoreJitRecorder(AppDomain * pDomain, ICLRPrivBinder * pBinderContext, bool fAppxMode)
        : m_stats(pDomain->GetMulticoreJitManager().GetStats())
    {
        LIMITED_METHOD_CONTRACT;

        m_pDomain           = pDomain;
        m_pBinderContext    = pBinderContext;
        m_JitInfoCount      = 0;
        m_ModuleCount       = 0;
        m_ModuleDepCount    = 0;

        m_fFirstMethod      = true;
        m_fAborted          = false;
        m_fAppxMode         = fAppxMode;


        m_stats.Clear();
    }

#ifndef TARGET_UNIX
    static bool CloseTimer()
    {
        LIMITED_METHOD_CONTRACT;

        TP_TIMER * pTimer = InterlockedExchangeT(& s_delayedWriteTimer, NULL);

        if (pTimer == NULL)
        {
            return false;
        }

        CloseThreadpoolTimer(pTimer);

        return true;
    }

    ~MulticoreJitRecorder()
    {
        LIMITED_METHOD_CONTRACT;

        CloseTimer();
    }
#endif // !TARGET_UNIX

    bool IsAtFullCapacity() const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_JitInfoCount >= (LONG) MAX_METHOD_ARRAY) ||
               (m_ModuleCount  >= MAX_MODULES);
    }

    void RecordMethodJit(MethodDesc * pMethod, bool application);

    MulticoreJitCodeInfo RequestMethodCode(MethodDesc * pMethod, MulticoreJitManager * pManager);

    HRESULT StartProfile(const WCHAR * pRoot, const WCHAR * pFileName, int suffix, LONG nSession);

    HRESULT StopProfile(bool appDomainShutdown);

    void AbortProfile();

    void RecordModuleLoad(Module * pModule, FileLoadLevel loadLevel);

    void AddModuleDependency(Module * pModule, FileLoadLevel loadLevel);
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

