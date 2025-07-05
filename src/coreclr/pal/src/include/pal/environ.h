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

#include <minipal/mutex.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++

Function:
  EnvironInitialize

Initialization function for the PAL environment code.
--*/
BOOL EnvironInitialize();

/*++
Function:
  EnvironGetUnsafe

Get the current environment. This is similar accessing
global environ variable and is not thread safe. This function
should only be called from code that guarantees environment won't
change while using returned pointer.
--*/
char **EnvironGetUnsafe();

/*++
Function:
  EnvironCheckenv

Check if environment variable with the given name exists in environment.
--*/
BOOL EnvironCheckenv(const char *name);

/*++
Function:
  EnvironGetenv

Get the value of environment variable with the given name.
Caller should free the returned string if it is not NULL.
--*/
char *EnvironGetenv(const char *name);

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

