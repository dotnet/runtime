// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/filetime.h

Abstract:

    Header file for utility functions having to do with file times.

Revision History:



--*/

#ifndef _PAL_FILETIME_H_
#define _PAL_FILETIME_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/* Provide consistent access to nanosecond fields, if they exist. */

#if HAVE_STAT_TIMESPEC

#define ST_ATIME_NSEC(statstruct) ((statstruct)->st_atimespec.tv_nsec)
#define ST_MTIME_NSEC(statstruct) ((statstruct)->st_mtimespec.tv_nsec)
#define ST_CTIME_NSEC(statstruct) ((statstruct)->st_ctimespec.tv_nsec)

#else /* HAVE_STAT_TIMESPEC */

#if HAVE_STAT_TIM

#define ST_ATIME_NSEC(statstruct) ((statstruct)->st_atim.tv_nsec)
#define ST_MTIME_NSEC(statstruct) ((statstruct)->st_mtim.tv_nsec)
#define ST_CTIME_NSEC(statstruct) ((statstruct)->st_ctim.tv_nsec)

#else /* HAVE_STAT_TIM */

#if HAVE_STAT_NSEC

#define ST_ATIME_NSEC(statstruct) ((statstruct)->st_atimensec)
#define ST_MTIME_NSEC(statstruct) ((statstruct)->st_mtimensec)
#define ST_CTIME_NSEC(statstruct) ((statstruct)->st_ctimensec)

#else /* HAVE_STAT_NSEC */

#define ST_ATIME_NSEC(statstruct) 0
#define ST_MTIME_NSEC(statstruct) 0
#define ST_CTIME_NSEC(statstruct) 0

#endif /* HAVE_STAT_NSEC */
#endif /* HAVE_STAT_TIM */
#endif /* HAVE_STAT_TIMESPEC */

FILETIME FILEUnixTimeToFileTime( time_t sec, long nsec );

#ifdef __APPLE__
#include <CoreFoundation/CFDate.h>

FILETIME FILECFAbsoluteTimeToFileTime( CFAbsoluteTime sec );
#endif // __APPLE__

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_FILE_H_ */











