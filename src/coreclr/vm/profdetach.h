// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ProfDetach.h
//

//
// Declaration of helper classes and structures used for Profiling API Detaching
//
// ======================================================================================

#ifndef __PROFDETACH_H__
#define __PROFDETACH_H__

#ifdef FEATURE_PROFAPI_ATTACH_DETACH

// The struct below is the medium by which RequestProfilerDetach communicates with
// the DetachThread about a profiler being detached.  Initial core attach /
// detach feature crew will have only one global instance of this struct.
// When we allow re-attach with neutered profilers, there will likely be a
// linked list of these, one per profiler in the act of being detached.
struct ProfilerDetachInfo
{
    ProfilerDetachInfo();
    void Init();

    // This is the profiler instance we're detaching.
    ProfilerInfo           *m_pProfilerInfo;

    // Time when profiler originally called RequestProfilerDetach()
    ULONGLONG               m_ui64DetachStartTime;

    // # milliseconds hint profiler specified in RequestProfilerDetach()
    DWORD                   m_dwExpectedCompletionMilliseconds;
};

//--------------------------------------------------------------------------
// Static-only class to coordinate initialization of the various profiling
// API detaching structures, plus other utility stuff.
//
class ProfilingAPIDetach
{
public:
    static HRESULT Initialize();

    static HRESULT RequestProfilerDetach(ProfilerInfo *profilerInfo, DWORD dwExpectedCompletionMilliseconds);

    static HRESULT CreateDetachThread();
    static DWORD WINAPI ProfilingAPIDetachThreadStart(LPVOID lpParameter);
    static void ExecuteEvacuationLoop();

    static BOOL IsEEToProfPtrRegisteredForDetach(EEToProfInterfaceImpl *pEEToProf);

private:
    static CQuickArrayList<ProfilerDetachInfo> s_profilerDetachInfos;

    // Signaled by RequestProfilerDetach() when there is detach work ready to be
    // done by the DetachThread
    static CLREvent           s_eventDetachWorkAvailable;

    static Volatile<BOOL>     s_profilerDetachThreadCreated;

    static void SleepWhileProfilerEvacuates(ProfilerDetachInfo *pDetachInfo);
    static void UnloadProfiler(ProfilerDetachInfo *pDetachInfo);

    // Prevent instantiation of ProfilingAPIDetach objects (should be static-only)
    ProfilingAPIDetach();
    ~ProfilingAPIDetach();
};

#endif // FEATURE_PROFAPI_ATTACH_DETACH

#endif //__PROFDETACH_H__
