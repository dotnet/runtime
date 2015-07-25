//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
Variables :

    palEnvironment: a global variable equivalent to environ on systems on 
                    which that exists, and a pointer to an array of environment 
                    strings on systems without environ.
    gcsEnvironment: critical section to synchronize access to palEnvironment
--*/
extern char **palEnvironment;
extern CRITICAL_SECTION gcsEnvironment;

/*++
Function :

    PAL_rand
    
    Calls rand and mitigates the difference between RAND_MAX 
    on Windows and FreeBSD.
--*/
int __cdecl PAL_rand(void);

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

/*++

Function:
  MiscInitialize

--*/
BOOL MiscInitialize();

/*++
Function:
  MiscCleanup

--*/
VOID MiscCleanup();

/*++
Function:
  MiscGetenv

Gets an environment variable's value from environ. The returned buffer
must not be modified or freed.
--*/
char *MiscGetenv(const char *name);

/*++
Function:
  MiscPutenv

Sets an environment variable's value by directly modifying environ.
Returns TRUE if the variable was set, or FALSE if malloc or realloc
failed or if the given string is malformed.
--*/
BOOL MiscPutenv(const char *string, BOOL deleteIfEmpty);

/*++
Function:
  MiscUnsetenv

Removes a variable from the environment. Does nothing if the variable
does not exist in the environment.
--*/
void MiscUnsetenv(const char *name);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* __MISC_H_ */

