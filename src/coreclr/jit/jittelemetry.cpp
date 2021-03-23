// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
// <OWNER>clrjit</OWNER>
//
// This class abstracts the telemetry information collected for the JIT.
//
// Goals:
//    1. Telemetry information should be a NO-op when JIT level telemetry is disabled.
//    2. Data collection should be actionable.
//    3. Data collection should comply to privacy rules.
//    4. Data collection cannot impact JIT/OS performance.
//    5. Data collection volume should be manageable by our remote services.
//
// DESIGN CONCERNS:
//
// > To collect data, we use the TraceLogging API provided by Windows.
//
//   The brief workflow suggested is:
//     #include <TraceLoggingProvider.h>
//     TRACELOGGING_DEFINE_PROVIDER( // defines g_hProvider
//         g_hProvider,  // Name of the provider variable
//         "MyProvider", // Human-readable name of the provider
//         (0xb3864c38, 0x4273, 0x58c5, 0x54, 0x5b, 0x8b, 0x36, 0x08, 0x34, 0x34, 0x71)); // Provider GUID
//     int main(int argc, char* argv[]) // or DriverEntry for kernel-mode.
//     {
//         TraceLoggingRegister(g_hProvider, NULL, NULL, NULL); // NULLs only needed for C. Please do not include the
//                                                              // NULLs in C++ code.
//         TraceLoggingWrite(g_hProvider,
//            "MyEvent1",
//            TraceLoggingString(argv[0], "arg0"),
//            TraceLoggingInt32(argc));
//         TraceLoggingUnregister(g_hProvider);
//         return 0;
//     }
//
//     In summary, this involves:
//     1. Creating a binary/DLL local provider using:
//        TRACELOGGING_DEFINE_PROVIDER(g_hProvider, "ProviderName", providerId, [option])
//     2. Registering the provider instance
//        TraceLoggingRegister(g_hProvider)
//     3. Perform TraceLoggingWrite operations to write out data.
//     4. Unregister the provider instance.
//        TraceLoggingUnregister(g_hProvider)
//
//     A. Determining where to create the provider instance?
//        1) We use the same provider name/GUID as the CLR and the CLR creates its own DLL local provider handle.
//           For CLRJIT.dll, the question is, can the same provider name/GUIDs be shared across binaries?
//
//           Answer:
//           "For TraceLogging providers, it is okay to use the same provider GUID / name
//           in different binaries. Do not share the same provider handle across DLLs.
//           As long as you do not pass an hProvider from one DLL to another, TraceLogging
//           will properly keep track of the events."
//
//        2) CoreCLR is linked into the CLR. CLR already creates an instance, so where do we create the JIT's instance?
//            Answer:
//            "Ideally you would have one provider per DLL, but if you're folding distinct sets
//            of functionality into one DLL (like shell32.dll or similar sort of catch-all things)
//            you can have perhaps a few more providers per binary."
//
//    B. Determining where to register and unregister the provider instance?
//         1) For CLRJIT.dll we can register the provider instance during jitDllOnProcessAttach.
//            Since one of our goals is to turn telemetry off, we need to be careful about
//            referencing environment variables during the DLL load and unload path.
//            Referencing environment variables through ConfigDWORD uses UtilCode.
//            This roughly translates to InitUtilcode() being called before jitDllOnProcessAttach.
//
//            For CLRJIT.dll, compStartup is called on jitOnDllProcessAttach().
//
//         2) For CLRJIT.dll and CoreCLR, compShutdown will be called during jitOnDllProcessDetach().
//
//    C. Determining the data to collect:
//
//         IMPORTANT: Since telemetry data can be collected at any time after DLL load,
//         make sure you initialize the compiler state variables you access in telemetry
//         data collection. For example, if you are transmitting method names, then
//         make sure info.compMethodHnd is initialized at that point.
//
//         1) Tracking noway assert count:
//            After a noway assert is hit, in both min-opts and non-min-opts, we collect
//            info such as the JIT version, method hash being compiled, filename and
//            line number etc.
//
//         2) Tracking baseline for the noway asserts:
//            During DLL unload, we report the number of methods that were compiled by
//            the JIT per process both under normal mode and during min-opts. NOTE that
//            this is ON for all processes.
//
//         3) For the future, be aware of privacy, performance and actionability of the data.
//

#include "jitpch.h"
#include "compiler.h"

#ifdef FEATURE_TRACELOGGING
#include "TraceLoggingProvider.h"
#include "MicrosoftTelemetry.h"
#include "clrtraceloggingcommon.h"
#include "fxver.h"

// Since telemetry code could be called under a noway_assert, make sure,
// we don't call noway_assert again.
#undef noway_assert

#define BUILD_STR1(x) #x
#define BUILD_STR2(x) BUILD_STR1(x)
#define BUILD_MACHINE BUILD_STR2(__BUILDMACHINE__)

// A DLL local instance of the DotNet provider
TRACELOGGING_DEFINE_PROVIDER(g_hClrJitProvider,
                             CLRJIT_PROVIDER_NAME,
                             CLRJIT_PROVIDER_ID,
                             TraceLoggingOptionMicrosoftTelemetry());

// Threshold to detect if we are hitting too many bad (noway) methods
// over good methods per process to prevent logging too much data.
static const double NOWAY_NOISE_RATIO = 0.6;            // Threshold of (bad / total) beyond which we'd stop
                                                        // logging. We'd restart if the pass rate improves.
static const unsigned NOWAY_SUFFICIENCY_THRESHOLD = 25; // Count of methods beyond which we'd apply percent
                                                        // threshold

// Initialize Telemetry State
volatile bool   JitTelemetry::s_fProviderRegistered    = false;
volatile UINT32 JitTelemetry::s_uMethodsCompiled       = 0;
volatile UINT32 JitTelemetry::s_uMethodsHitNowayAssert = 0;

// Constructor for telemetry state per compiler instance
JitTelemetry::JitTelemetry()
{
    Initialize(nullptr);
}

//------------------------------------------------------------------------
// Initialize: Initialize the object with the compiler instance
//
//  Description:
//     Compiler instance may not be fully initialized. If you are
//     tracking object data for telemetry, make sure they are initialized
//     in the compiler is ready.
//
void JitTelemetry::Initialize(Compiler* c)
{
    comp                = c;
    m_pszAssemblyName   = "";
    m_pszScopeName      = "";
    m_pszMethodName     = "";
    m_uMethodHash       = 0;
    m_fMethodInfoCached = false;
}

//------------------------------------------------------------------------
// IsTelemetryEnabled: Can we perform JIT telemetry
//
//  Return Value:
//      Returns "true" if COMPlus_JitTelemetry environment flag is
//      non-zero. Else returns "false".
//
//
/* static */
bool JitTelemetry::IsTelemetryEnabled()
{
    return JitConfig.JitTelemetry() != 0;
}

//------------------------------------------------------------------------
// NotifyDllProcessAttach: Notification for DLL load and static initializations
//
//  Description:
//     Register telemetry provider with the OS.
//
//  Note:
//     This method can be called twice in NGEN scenario.
//
void JitTelemetry::NotifyDllProcessAttach()
{
    if (!IsTelemetryEnabled())
    {
        return;
    }

    if (!s_fProviderRegistered)
    {
        // Register the provider.
        TraceLoggingRegister(g_hClrJitProvider);
        s_fProviderRegistered = true;
    }
}

//------------------------------------------------------------------------
// NotifyDllProcessDetach: Notification for DLL unload and teardown
//
//  Description:
//     Log the methods compiled data if telemetry is enabled and
//     Unregister telemetry provider with the OS.
//
void JitTelemetry::NotifyDllProcessDetach()
{
    if (!IsTelemetryEnabled())
    {
        return;
    }

    assert(s_fProviderRegistered); // volatile read

    // Unregister the provider.
    TraceLoggingUnregister(g_hClrJitProvider);
}

//------------------------------------------------------------------------
// NotifyEndOfCompilation: Notification for end of current method
//     compilation.
//
//  Description:
//      Increment static volatile counters for the current compiled method.
//      This is slightly inaccurate due to lack of synchronization around
//      the counters. Inaccuracy is the tradeoff for JITting cost.
//
//  Note:
//      1. Must be called post fully successful compilation of the method.
//      2. This serves as an effective baseline as how many methods compiled
//         successfully.
void JitTelemetry::NotifyEndOfCompilation()
{
    if (!IsTelemetryEnabled())
    {
        return;
    }

    s_uMethodsCompiled++; // volatile increment
}

//------------------------------------------------------------------------
// NotifyNowayAssert: Notification that a noway handling is under-way.
//
//  Arguments:
//      filename - The JIT source file name's absolute path at the time of
//                 building the JIT.
//      line     - The line number where the noway assert was hit.
//
//  Description:
//      If telemetry is enabled, then obtain data to collect from the
//      compiler or the VM and use the tracelogging APIs to write out.
//
void JitTelemetry::NotifyNowayAssert(const char* filename, unsigned line)
{
    if (!IsTelemetryEnabled())
    {
        return;
    }

    s_uMethodsHitNowayAssert++;

    // Check if our assumption that noways are rare is invalid for this
    // process. If so, return early than logging too much data.
    unsigned noways   = s_uMethodsHitNowayAssert;
    unsigned attempts = max(1, s_uMethodsCompiled + noways);
    double   ratio    = (noways / ((double)attempts));
    if (noways > NOWAY_SUFFICIENCY_THRESHOLD && ratio > NOWAY_NOISE_RATIO)
    {
        return;
    }

    assert(comp);

    UINT32      nowayIndex = s_uMethodsHitNowayAssert;
    UINT32      codeSize   = 0;
    INT32       minOpts    = -1;
    const char* lastPhase  = "";
    if (comp != nullptr)
    {
        codeSize  = comp->info.compILCodeSize;
        minOpts   = comp->opts.IsMinOptsSet() ? comp->opts.MinOpts() : -1;
        lastPhase = PhaseNames[comp->mostRecentlyActivePhase];
    }

    CacheCurrentMethodInfo();

    TraceLoggingWrite(g_hClrJitProvider, "CLRJIT.NowayAssert",

                      TraceLoggingUInt32(codeSize, "IL_CODE_SIZE"), TraceLoggingInt32(minOpts, "MINOPTS_MODE"),
                      TraceLoggingString(lastPhase, "PREVIOUS_COMPLETED_PHASE"),

                      TraceLoggingString(m_pszAssemblyName, "ASSEMBLY_NAME"),
                      TraceLoggingString(m_pszMethodName, "METHOD_NAME"),
                      TraceLoggingString(m_pszScopeName, "METHOD_SCOPE"),
                      TraceLoggingUInt32(m_uMethodHash, "METHOD_HASH"),

                      TraceLoggingString(filename, "FILENAME"), TraceLoggingUInt32(line, "LINE"),
                      TraceLoggingUInt32(nowayIndex, "NOWAY_INDEX"),

                      TraceLoggingString(TARGET_READABLE_NAME, "ARCH"),
                      TraceLoggingString(VER_FILEVERSION_STR, "VERSION"), TraceLoggingString(BUILD_MACHINE, "BUILD"),
                      TraceLoggingString(VER_COMMENTS_STR, "FLAVOR"),

                      TraceLoggingKeyword(MICROSOFT_KEYWORD_MEASURES));
}

//------------------------------------------------------------------------
// CacheCurrentMethodInfo: Cache the method/assembly/scope name info.
//
//  Description:
//      Obtain the method information if not already cached, for the
//      method under compilation from the compiler. This includes:
//
//          Method name, assembly name, scope name, method hash.
//
void JitTelemetry::CacheCurrentMethodInfo()
{
    if (m_fMethodInfoCached)
    {
        return;
    }

    assert(comp);
    if (comp != nullptr)
    {
        comp->compGetTelemetryDefaults(&m_pszAssemblyName, &m_pszScopeName, &m_pszMethodName, &m_uMethodHash);
        assert(m_pszAssemblyName);
        assert(m_pszScopeName);
        assert(m_pszMethodName);
    }

    // Set cached to prevent getting this twice.
    m_fMethodInfoCached = true;
}

//------------------------------------------------------------------------
// compGetTelemetryDefaults: Obtain information specific to telemetry
//      from the JIT-interface.
//
//  Arguments:
//      assemblyName - Pointer to hold assembly name upon return
//      scopeName    - Pointer to hold scope name upon return
//      methodName   - Pointer to hold method name upon return
//      methodHash   - Pointer to hold method hash upon return
//
//  Description:
//      Obtains from the JIT EE interface the information for the
//      current method under compilation.
//
//  Warning:
//      The eeGetMethodName call could be expensive for generic
//      methods, so call this method only when there is less impact
//      to throughput.
//
void Compiler::compGetTelemetryDefaults(const char** assemblyName,
                                        const char** scopeName,
                                        const char** methodName,
                                        unsigned*    methodHash)
{
    if (info.compMethodHnd != nullptr)
    {
        __try
        {

            // Expensive calls, call infrequently or in exceptional scenarios.
            *methodHash = info.compCompHnd->getMethodHash(info.compMethodHnd);
            *methodName = eeGetMethodName(info.compMethodHnd, scopeName);

            // SuperPMI needs to implement record/replay of these method calls.
            *assemblyName = info.compCompHnd->getAssemblyName(
                info.compCompHnd->getModuleAssembly(info.compCompHnd->getClassModule(info.compClassHnd)));
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
        }
    }

    // If the JIT interface methods init-ed these values to nullptr,
    // make sure they are set to empty string.
    if (*methodName == nullptr)
    {
        *methodName = "";
    }
    if (*scopeName == nullptr)
    {
        *scopeName = "";
    }
    if (*assemblyName == nullptr)
    {
        *assemblyName = "";
    }
}

#endif // FEATURE_TRACELOGGING
