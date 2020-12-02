// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "genanalysis.h"

GcGenAnalysisState gcGenAnalysisState = GcGenAnalysisState::Uninitialized;
EventPipeSession* gcGenAnalysisEventPipeSession = nullptr;
uint64_t gcGenAnalysisEventPipeSessionId = (uint64_t)-1;
GcGenAnalysisState gcGenAnalysisConfigured = GcGenAnalysisState::Uninitialized;
int64_t gcGenAnalysisGen = -1;
int64_t gcGenAnalysisBytes = 0;
int64_t gcGenAnalysisIndex = 0;
uint32_t gcGenAnalysisBufferMB = 0;

/* static */ void GenAnalysis::Initialize()
{
#ifndef GEN_ANALYSIS_STRESS
    if (gcGenAnalysisConfigured == GcGenAnalysisState::Uninitialized)
    {
        bool match = true;
        CLRConfigStringHolder gcGenAnalysisCmd(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCGenAnalysisCmd));
        if (gcGenAnalysisCmd != nullptr)
        {
            // Get the managed command line.
            LPCWSTR pCmdLine = GetCommandLineForDiagnostics();
            match = wcsncmp(pCmdLine, gcGenAnalysisCmd, wcslen(gcGenAnalysisCmd)) == 0;
        }
        if (match && !CLRConfig::IsConfigOptionSpecified(W("GCGenAnalysisGen")))
        {
            match = false;
        }
        if (match && !CLRConfig::IsConfigOptionSpecified(W("GCGenAnalysisBytes")))
        {
            match = false;
        }
        if (match)
        {
            gcGenAnalysisBytes = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCGenAnalysisBytes);
            gcGenAnalysisGen = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCGenAnalysisGen);
            gcGenAnalysisIndex = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_GCGenAnalysisIndex);
            gcGenAnalysisBufferMB = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeCircularMB);
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
        EnableGenerationalAwareSession();
    }    
}

/* static */ void GenAnalysis::EnableGenerationalAwareSession()
{
    LPCWSTR outputPath = nullptr;
    outputPath = GENAWARE_FILE_NAME;
    NewHolder<EventPipeProviderConfiguration> pProviders = nullptr;
    int providerCnt = 1;
    pProviders = new EventPipeProviderConfiguration[providerCnt];
    const uint64_t GCHeapAndTypeNamesKeyword        = 0x00001000000; // This keyword is necessary for the type names
    const uint64_t GCHeapSurvivalAndMovementKeyword = 0x00000400000; // This keyword is necessary for the generation range data.
    const uint64_t GCHeapDumpKeyword                = 0x00000100000; // This keyword is necessary for enabling walking the heap
    const uint64_t TypeKeyword                      = 0x00000080000; // This keyword is necessary for enabling BulkType events
    const uint64_t keyword                          = GCHeapAndTypeNamesKeyword|GCHeapSurvivalAndMovementKeyword|GCHeapDumpKeyword|TypeKeyword;
    pProviders[0] = EventPipeProviderConfiguration(W("Microsoft-Windows-DotNETRuntime"), keyword, 5, nullptr);
    gcGenAnalysisEventPipeSessionId = EventPipe::Enable(
        outputPath,
        gcGenAnalysisBufferMB,
        pProviders,
        providerCnt,
        EventPipeSessionType::File,
        EventPipeSerializationFormat::NetTraceV4,
        false,
        nullptr
    );
    if (gcGenAnalysisEventPipeSessionId > 0)
    {
        gcGenAnalysisEventPipeSession= EventPipe::GetSession(gcGenAnalysisEventPipeSessionId);
        gcGenAnalysisEventPipeSession->Pause();
        EventPipe::StartStreaming(gcGenAnalysisEventPipeSessionId);
        gcGenAnalysisState = GcGenAnalysisState::Enabled;
    }
}
