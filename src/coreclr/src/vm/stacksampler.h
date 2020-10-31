// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    StackSampler.h

--*/

#ifndef __STACK_SAMPLER_H
#define __STACK_SAMPLER_H
#endif

#ifdef FEATURE_STACK_SAMPLING

class StackSampler
{
public:
    // Interface
    static void Init();
    static void RecordJittingInfo(MethodDesc* pMD, CORJIT_FLAGS flags);

private:

    // Methods
    StackSampler();
    ~StackSampler();

    static DWORD __stdcall SamplingThreadProc(void* arg);

    static StackWalkAction StackWalkCallback(CrawlFrame* pCf, VOID* data);

    StackWalkAction CrawlFrameVisitor(CrawlFrame* pCf, Thread* pMdThread);

    void ThreadProc();

    void JitFrequentMethodsInSamples();

    void JitAndCollectTrace(MethodDesc* pMD);

    void RecordJittingInfoInternal(MethodDesc* pMD, CORJIT_FLAGS flags);


    // Constants
    static const int s_knDefaultSamplingIntervalMsec = 100;
    static const int s_knDefaultNumMethods = 32;
    static const int s_knDefaultCountForImportance = 0;    // TODO: Set to some reasonable value.

    // Typedefs
    struct CountInfo;
    typedef MapSHash<MethodDesc*, CountInfo> CountInfoHash;
    typedef CountInfoHash::element_t CountInfoHashEntry;

    typedef MapSHash<MethodDesc*, CORJIT_FLAGS> JitInfoHash;
    typedef JitInfoHash::element_t JitInfoHashEntry;

    // Nested types
    struct CountInfo
    {
        unsigned uCount;
        bool fJitted;
        CountInfo() : uCount(0), fJitted(false) {}
    };

    // Fields
    Crst m_crstJitInfo;
    CountInfoHash m_countInfo;
    JitInfoHash m_jitInfo;
    Thread* m_pThread;
    unsigned m_nSampleEvery;
    unsigned m_nSampleAfter;
    unsigned m_nNumMethods;
};
#endif // FEATURE_STACK_SAMPLING

