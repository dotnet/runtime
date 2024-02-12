// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
