// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

LWM(AllocGCInfo, DWORD, Agnostic_AllocGCInfo)
LWM(AllocMem, DWORD, Agnostic_AllocMemDetails)
DENSELWM(AllocUnwindInfo, Agnostic_AllocUnwindInfo)
DENSELWM(AssertLog, DWORD)
DENSELWM(CallLog, DWORD)
DENSELWM(ClassMustBeLoadedBeforeCodeIsRun, DWORDLONG)
LWM(CompileMethod, DWORD, Agnostic_CompileMethodResults)
DENSELWM(MessageLog, DWORD)
DENSELWM(MethodMustBeLoadedBeforeCodeIsRun, DWORDLONG)
DENSELWM(ProcessName, DWORD)
LWM(RecordCallSiteWithSignature, DWORD, Agnostic_RecordCallSite)
LWM(RecordCallSiteWithoutSignature, DWORD, DWORDLONG)
DENSELWM(RecordRelocation, Agnostic_RecordRelocation)
DENSELWM(ReportFatalError, DWORD)
DENSELWM(ReportInliningDecision, Agnostic_ReportInliningDecision)
DENSELWM(ReportTailCallDecision, Agnostic_ReportTailCallDecision)
DENSELWM(ReserveUnwindInfo, Agnostic_ReserveUnwindInfo)
LWM(SetBoundaries, DWORD, Agnostic_SetBoundaries)
LWM(SetEHcount, DWORD, DWORD)
LWM(SetEHinfo, DWORD, Agnostic_CORINFO_EH_CLAUSE)
LWM(SetMethodAttribs, DWORDLONG, DWORD)
LWM(SetVars, DWORD, Agnostic_SetVars)
LWM(SetPatchpointInfo, DWORD, Agnostic_SetPatchpointInfo)
DENSELWM(CrSigInstHandleMap, DWORDLONG)

#undef LWM
#undef DENSELWM
