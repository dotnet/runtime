// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "comthreadpool.h"
#include "eeconfig.h"

/*****************************************************************************************************/
// Enumerates some runtime config variables that are used by CoreLib for initialization. The config variable index should start
// at 0 to begin enumeration. If a config variable at or after the specified config variable index is configured, returns the
// next config variable index to pass in on the next call to continue enumeration.
FCIMPL4(INT32, ThreadPoolNative::GetNextConfigUInt32Value,
    INT32 configVariableIndex,
    UINT32 *configValueRef,
    BOOL *isBooleanRef,
    LPCWSTR *appContextConfigNameRef)
{
    FCALL_CONTRACT;
    _ASSERTE(configVariableIndex >= 0);
    _ASSERTE(configValueRef != NULL);
    _ASSERTE(isBooleanRef != NULL);
    _ASSERTE(appContextConfigNameRef != NULL);

    auto TryGetConfig =
        [=](const CLRConfig::ConfigDWORDInfo &configInfo, bool isBoolean, const WCHAR *appContextConfigName) -> bool
    {
        bool wasNotConfigured = true;
        *configValueRef = CLRConfig::GetConfigValue(configInfo, &wasNotConfigured);
        if (wasNotConfigured)
        {
            return false;
        }

        *isBooleanRef = isBoolean;
        *appContextConfigNameRef = appContextConfigName;
        return true;
    };

    switch (configVariableIndex)
    {
        case 1: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_ForceMinWorkerThreads, false, W("System.Threading.ThreadPool.MinThreads"))) { return 2; } FALLTHROUGH;
        case 2: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_ForceMaxWorkerThreads, false, W("System.Threading.ThreadPool.MaxThreads"))) { return 3; } FALLTHROUGH;
        case 3: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_DisableStarvationDetection, true, W("System.Threading.ThreadPool.DisableStarvationDetection"))) { return 4; } FALLTHROUGH;
        case 4: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_DebugBreakOnWorkerStarvation, true, W("System.Threading.ThreadPool.DebugBreakOnWorkerStarvation"))) { return 5; } FALLTHROUGH;
        case 5: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking, true, W("System.Threading.ThreadPool.EnableWorkerTracking"))) { return 6; } FALLTHROUGH;
        case 6: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, false, W("System.Threading.ThreadPool.UnfairSemaphoreSpinLimit"))) { return 7; } FALLTHROUGH;

        case 7: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_Disable, true, W("System.Threading.ThreadPool.HillClimbing.Disable"))) { return 8; } FALLTHROUGH;
        case 8: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_WavePeriod, false, W("System.Threading.ThreadPool.HillClimbing.WavePeriod"))) { return 9; } FALLTHROUGH;
        case 9: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_TargetSignalToNoiseRatio, false, W("System.Threading.ThreadPool.HillClimbing.TargetSignalToNoiseRatio"))) { return 10; } FALLTHROUGH;
        case 10: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_ErrorSmoothingFactor, false, W("System.Threading.ThreadPool.HillClimbing.ErrorSmoothingFactor"))) { return 11; } FALLTHROUGH;
        case 11: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_WaveMagnitudeMultiplier, false, W("System.Threading.ThreadPool.HillClimbing.WaveMagnitudeMultiplier"))) { return 12; } FALLTHROUGH;
        case 12: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxWaveMagnitude, false, W("System.Threading.ThreadPool.HillClimbing.MaxWaveMagnitude"))) { return 13; } FALLTHROUGH;
        case 13: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_WaveHistorySize, false, W("System.Threading.ThreadPool.HillClimbing.WaveHistorySize"))) { return 14; } FALLTHROUGH;
        case 14: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_Bias, false, W("System.Threading.ThreadPool.HillClimbing.Bias"))) { return 15; } FALLTHROUGH;
        case 15: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxChangePerSecond, false, W("System.Threading.ThreadPool.HillClimbing.MaxChangePerSecond"))) { return 16; } FALLTHROUGH;
        case 16: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxChangePerSample, false, W("System.Threading.ThreadPool.HillClimbing.MaxChangePerSample"))) { return 17; } FALLTHROUGH;
        case 17: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxSampleErrorPercent, false, W("System.Threading.ThreadPool.HillClimbing.MaxSampleErrorPercent"))) { return 18; } FALLTHROUGH;
        case 18: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_SampleIntervalLow, false, W("System.Threading.ThreadPool.HillClimbing.SampleIntervalLow"))) { return 19; } FALLTHROUGH;
        case 19: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_SampleIntervalHigh, false, W("System.Threading.ThreadPool.HillClimbing.SampleIntervalHigh"))) { return 20; } FALLTHROUGH;
        case 20: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_GainExponent, false, W("System.Threading.ThreadPool.HillClimbing.GainExponent"))) { return 21; } FALLTHROUGH;

        default:
            *configValueRef = 0;
            *isBooleanRef = false;
            *appContextConfigNameRef = NULL;
            return -1;
    }
}
FCIMPLEND
