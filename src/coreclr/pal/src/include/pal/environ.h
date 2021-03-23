// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

Initialization function for the PAL environment code.
--*/
BOOL EnvironInitialize();

/*++
Function:
  EnvironGetenv

Get the value of environment variable with the given name.
--*/
char *EnvironGetenv(const char *name, BOOL copyValue = TRUE);

/*++
Function:
  EnvironPutenv

Add the environment variable string provided to the PAL version
of the environment.
--*/
BOOL EnvironPutenv(const char *string, BOOL deleteIfEmpty);

/*++
Function:
  EnvironUnsetenv

Remove the environment variable with the given name from the PAL
version of the environment if it exists.
--*/
void EnvironUnsetenv(const char *name);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* __ENVIRON_H_ */

