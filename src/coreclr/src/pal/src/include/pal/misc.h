// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/misc.h

Abstract:
    Header file for the initialization and clean up functions
    for the misc Win32 functions



--*/

#ifndef __MISC_H_
#define __MISC_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++
Function :

    PAL_rand

    Calls rand and mitigates the difference between RAND_MAX
    on Windows and FreeBSD.
--*/
int __cdecl PAL_rand(void);

/*++
Function :

    PAL_time
--*/
PAL_time_t __cdecl PAL_time(PAL_time_t*);

/*++
Function:
TIMEInitialize

Return value:
TRUE if initialize succeeded
FALSE otherwise

--*/
BOOL TIMEInitialize( void );

/*++
Function :
    MsgBoxInitialize

    Initialize the critical sections.

Return value:
    TRUE if initialize succeeded
    FALSE otherwise

--*/
BOOL MsgBoxInitialize( void );

/*++
Function :
    MsgBoxCleanup

    Deletes the critical sections.

--*/
void MsgBoxCleanup( void );

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* __MISC_H_ */
