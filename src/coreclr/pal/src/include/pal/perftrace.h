// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/perftrace.h

Abstract:
    Header file for PAL Performance trace utilities.



--*/

/*
Overview of PAL Performance utilities

 */

#ifndef _PAL_PERFTRACE_H_
#define _PAL_PERFTRACE_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#if PAL_PERF
#define PERF_ENTRY(x)  \
	ULONGLONG pal_perf_start_tick = 0;\
    PERFLogFunctionEntry( PAL_PERF_##x, &pal_perf_start_tick )
#define PERF_EXIT(x) \
	PERFLogFunctionExit( PAL_PERF_##x, &pal_perf_start_tick )
#define PERF_ENTRY_ONLY(x)  \
	PERFNoLatencyProfileEntry( PAL_PERF_##x )

BOOL PERFInitialize(LPWSTR command_line, LPWSTR exe_path) ;
void PERFTerminate( );
BOOL PERFAllocThreadInfo( );
void PERFLogFunctionExit(unsigned int pal_api_id, ULONGLONG *pal_perf_start_tick);
void PERFLogFunctionEntry(unsigned int pal_api_id, ULONGLONG *pal_perf_start_tick);
void PERFEnableThreadProfile(BOOL isInternal);
void PERFDisableThreadProfile(BOOL isInternal);
void PERFEnableProcessProfile( );
void PERFDisableProcessProfile( );
BOOL PERFIsProcessProfileEnabled( );
void PERFNoLatencyProfileEntry(unsigned int pal_api_id );
void PERFCalibrate(const char* msg);

#else  /* PAL_PERF */

#define PERF_ENTRY(x)
#define PERF_ENTRY_ONLY(x)
#define PERF_EXIT(x)

#endif /* PAL_PERF */

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_PERFTRACE_H_ */



