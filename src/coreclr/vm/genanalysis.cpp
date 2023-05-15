// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "genanalysis.h"
#include "eventpipeadapter.h"

GcGenAnalysisState gcGenAnalysisState = GcGenAnalysisState::Uninitialized;
EventPipeSession* gcGenAnalysisEventPipeSession = nullptr;
uint64_t gcGenAnalysisEventPipeSessionId = (uint64_t)-1;
GcGenAnalysisState gcGenAnalysisConfigured = GcGenAnalysisState::Uninitialized;
int64_t gcGenAnalysisGen = -1;
uint64_t gcGenAnalysisBytes = 0;
uint64_t gcGenAnalysisTime = 0;
int64_t gcGenAnalysisIndex = 0;
uint32_t gcGenAnalysisBufferMB = 0;
bool gcGenAnalysisTrace = true;
bool gcGenAnalysisDump = false;

/* static */ void GenAnalysis::Initialize()
{
#ifndef GEN_ANALYSIS_STRESS
    if (gcGenAnalysisConfigured == GcGenAnalysisState::Uninitialized)
    {
        bool match = true;
        CLRConfigStringHolder gcGenAnalysisCmd(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisCmd));
        if (gcGenAnalysisCmd != nullptr)
        {
            // Get the managed command line.
            LPCWSTR pCmdLine = GetCommandLineForDiagnostics();
            match = u16_strncmp(pCmdLine, gcGenAnalysisCmd, u16_strlen(gcGenAnalysisCmd)) == 0;
        }
        if (match && !CLRConfig::IsConfigOptionSpecified(W("GCGenAnalysisGen")))
        {
            match = false;
        }
        if (match && !CLRConfig::IsConfigOptionSpecified(W("GCGenAnalysisBytes")) &&
                     !CLRConfig::IsConfigOptionSpecified(W("GCGenAnalysisTimeUSec")) &&
                     !CLRConfig::IsConfigOptionSpecified(W("GCGenAnalysisTimeMSec")))
        {
            match = false;
        }
        if (match)
        {
            gcGenAnalysisBytes = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisBytes);
            gcGenAnalysisTime = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisTimeUSec) * 10;
            if (gcGenAnalysisTime == 0)
            {
                gcGenAnalysisTime = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisTimeMSec) * 10000;
            }
            gcGenAnalysisGen = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisGen);
            gcGenAnalysisIndex = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisIndex);
            gcGenAnalysisBufferMB = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeCircularMB);
            gcGenAnalysisTrace = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisTrace);
            gcGenAnalysisDump = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCGenAnalysisDump);
            gcGenAnalysisConfigured = GcGenAnalysisState::Enabled;
        }
        else
        {
            gcGenAnalysisConfigured = GcGenAnalysisState::Disabled;
        }
    }
    if ((gcGenAnalysisConfigured == GcGenAnalysisState::Enabled) && (gcGenAnalysisState == GcGenAnalysisState::Uninitialized))
#endif
    {
        if (gcGenAnalysisTrace)
        {
            EnableGenerationalAwareSession();
        }
        if (gcGenAnalysisDump)
        {
            gcGenAnalysisState = GcGenAnalysisState::Enabled;
        }
    }
}

/* static */ void GenAnalysis::EnableGenerationalAwareSession()
{
    WCHAR outputPath[MAX_PATH];
    ReplacePid(GENAWARE_TRACE_FILE_NAME, outputPath, MAX_PATH);

    NewArrayHolder<COR_PRF_EVENTPIPE_PROVIDER_CONFIG> pProviders;
    int providerCnt = 1;
    pProviders = new COR_PRF_EVENTPIPE_PROVIDER_CONFIG[providerCnt];
    const uint64_t GCHeapAndTypeNamesKeyword        = 0x00001000000; // This keyword is necessary for the type names
    const uint64_t GCHeapSurvivalAndMovementKeyword = 0x00000400000; // This keyword is necessary for the generation range data.
    const uint64_t GCHeapDumpKeyword                = 0x00000100000; // This keyword is necessary for enabling walking the heap
    const uint64_t TypeKeyword                      = 0x00000080000; // This keyword is necessary for enabling BulkType events
    const uint64_t keyword                          = GCHeapAndTypeNamesKeyword|GCHeapSurvivalAndMovementKeyword|GCHeapDumpKeyword|TypeKeyword;
    pProviders[0].providerName = W("Microsoft-Windows-DotNETRuntime");
    pProviders[0].keywords = keyword;
    pProviders[0].loggingLevel = (uint32_t)EP_EVENT_LEVEL_INFORMATIONAL;
    pProviders[0].filterData = nullptr;

    EventPipeProviderConfigurationAdapter configAdapter(pProviders, providerCnt);
    gcGenAnalysisEventPipeSessionId = EventPipeAdapter::Enable(
        outputPath,
        gcGenAnalysisBufferMB,
        configAdapter,
        EP_SESSION_TYPE_FILE,
        EP_SERIALIZATION_FORMAT_NETTRACE_V4,
        false,
        nullptr,
        nullptr,
        nullptr
    );
    if (gcGenAnalysisEventPipeSessionId > 0)
    {
        gcGenAnalysisEventPipeSession= EventPipeAdapter::GetSession(gcGenAnalysisEventPipeSessionId);
        EventPipeAdapter::PauseSession(gcGenAnalysisEventPipeSession);
        EventPipeAdapter::StartStreaming(gcGenAnalysisEventPipeSessionId);
        gcGenAnalysisState = GcGenAnalysisState::Enabled;
    }
    else
    {
        gcGenAnalysisTrace = false;
    }
}
