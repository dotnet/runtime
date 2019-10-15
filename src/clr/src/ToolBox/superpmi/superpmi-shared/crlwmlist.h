//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// crlwmlist.h - List of all LightWeightMap in CompileResult.
// To use, #define LWM(map, key, value) to something.
// If you need to distinguish DenseLightWeightMap, #define DENSELWM(map, value) as well.
//----------------------------------------------------------

#ifndef LWM
#error Define LWM before including this file.
#endif

// If the key is needed, then DENSELWM must be defined.
#ifndef DENSELWM
#define DENSELWM(map, value) LWM(map, this_is_an_error, value)
#endif

LWM(AddressMap, DWORDLONG, CompileResult::Agnostic_AddressMap)
LWM(AllocMethodBlockCounts, DWORD, CompileResult::Agnostic_AllocMethodBlockCounts)
LWM(AllocGCInfo, DWORD, CompileResult::Agnostic_AllocGCInfo)
LWM(AllocMem, DWORD, CompileResult::Agnostic_AllocMemDetails)
DENSELWM(AllocUnwindInfo, CompileResult::Agnostic_AllocUnwindInfo)
DENSELWM(AssertLog, DWORD)
DENSELWM(CallLog, DWORD)
DENSELWM(ClassMustBeLoadedBeforeCodeIsRun, DWORDLONG)
LWM(CompileMethod, DWORD, CompileResult::Agnostic_CompileMethodResults)
DENSELWM(MessageLog, DWORD)
DENSELWM(MethodMustBeLoadedBeforeCodeIsRun, DWORDLONG)
DENSELWM(ProcessName, DWORD)
LWM(RecordCallSite, DWORD, CompileResult::Agnostic_RecordCallSite)
DENSELWM(RecordRelocation, CompileResult::Agnostic_RecordRelocation)
DENSELWM(ReportFatalError, DWORD)
DENSELWM(ReportInliningDecision, CompileResult::Agnostic_ReportInliningDecision)
DENSELWM(ReportTailCallDecision, CompileResult::Agnostic_ReportTailCallDecision)
DENSELWM(ReserveUnwindInfo, CompileResult::Agnostic_ReserveUnwindInfo)
LWM(SetBoundaries, DWORD, CompileResult::Agnostic_SetBoundaries)
LWM(SetEHcount, DWORD, DWORD)
LWM(SetEHinfo, DWORD, CompileResult::Agnostic_CORINFO_EH_CLAUSE2)
LWM(SetMethodAttribs, DWORDLONG, DWORD)
LWM(SetVars, DWORD, CompileResult::Agnostic_SetVars)

#undef LWM
#undef DENSELWM
