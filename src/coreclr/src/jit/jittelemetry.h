// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
// <OWNER>clrjit</OWNER>
#pragma once

#ifdef FEATURE_TRACELOGGING

class Compiler;

class JitTelemetry
{
public:
    // Notify DLL load.
    static void NotifyDllProcessAttach();

    // Notify DLL unload.
    static void NotifyDllProcessDetach();

    // Constructor
    JitTelemetry();

    // Initialize with compiler instance
    void Initialize(Compiler* comp);

    // Notification of end of compilation of the current method.
    void NotifyEndOfCompilation();

    // Notification of noway_assert.
    void NotifyNowayAssert(const char* filename, unsigned line);

    // Is telemetry enabled through COMPlus_JitTelemetry?
    static bool IsTelemetryEnabled();

private:
    // Obtain current method information from VM and cache for
    // future uses.
    void CacheCurrentMethodInfo();

    //
    //--------------------------------------------------------------------------------
    // The below per process counters are updated without synchronization or
    // thread-safety to avoid interfering with the JIT throughput. Accuracy
    // of these counters will be traded-off for throughput.
    //

    // Methods compiled per DLL unload
    static volatile UINT32 s_uMethodsCompiled;

    // Methods compiled per DLL unload that hit noway assert (per process)
    static volatile UINT32 s_uMethodsHitNowayAssert;
    //--------------------------------------------------------------------------------

    // Has the provider been registered already (per process)
    static volatile bool s_fProviderRegistered;

    // Cached value of current method hash.
    unsigned m_uMethodHash;

    // Cached value of current assembly name.
    const char* m_pszAssemblyName;

    // Cached value of current scope name, i.e., "Program.Foo" in "Program.Foo:Main"
    const char* m_pszScopeName;

    // Cached value of current method name, i.e., "Main" in "Program.Foo:Main"
    const char* m_pszMethodName;

    // Have we already cached the method/scope/assembly names?
    bool m_fMethodInfoCached;

    // Compiler instance.
    Compiler* comp;
};

#endif // FEATURE_TRACELOGGING
