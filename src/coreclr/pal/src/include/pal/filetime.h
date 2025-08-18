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

FILETIME FILEUnixTimeToFileTime( time_t sec, long nsec );

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_FILE_H_ */











