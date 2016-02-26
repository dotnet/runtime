// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/environ.h

Abstract:
    Header file for functions manipulating environment variables


--*/

#ifndef __ENVIRON_H_
#define __ENVIRON_H_

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

Function:
  EnvironInitialize

--*/
BOOL EnvironInitialize();

/*++
Function:
  EnvironGetenv

Gets an environment variable's value.
--*/
char *EnvironGetenv(const char *name);

/*++
Function:
  EnvironPutenv

Sets an environment variable's value.
Returns TRUE if the variable was set, or FALSE if malloc or realloc
failed or if the given string is malformed.
--*/
BOOL EnvironPutenv(const char *string, BOOL deleteIfEmpty);

/*++
Function:
  EnvironUnsetenv

Removes a variable from the environment. Does nothing if the variable
does not exist in the environment.
--*/
void EnvironUnsetenv(const char *name);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* __ENVIRON_H_ */

