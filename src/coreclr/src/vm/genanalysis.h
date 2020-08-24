// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// #define GEN_ANALYSIS_STRESS

#ifndef __GENANALYSIS_H__
#define __GENANALYSIS_H__

#include "eventpipe.h"
#include "eventpipesession.h"

enum GcGenAnalysisState
{
    Uninitialized = 0,
    Enabled = 1,
    Disabled = 2,
    Done = 3,
};

#define GENAWARE_FILE_NAME W("gcgenaware.nettrace")
#define GENAWARE_COMPLETION_FILE_NAME "gcgenaware.nettrace.completed"

extern bool s_forcedGCInProgress;
extern GcGenAnalysisState gcGenAnalysisState;
extern EventPipeSession* gcGenAnalysisEventPipeSession;
extern uint64_t gcGenAnalysisEventPipeSessionId;
extern GcGenAnalysisState gcGenAnalysisConfigured;
extern int64_t gcGenAnalysisGen;
extern int64_t gcGenAnalysisBytes;
extern int64_t gcGenAnalysisIndex;

class GenAnalysis
{
public:
    static void Initialize();
    static void EnableGenerationalAwareSession();
};

#endif 
