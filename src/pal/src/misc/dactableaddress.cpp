//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    dactableaddress.cpp

Abstract:

    Functions to write and read DAC global pointer table address between the coreclr
    and DAC/debugger processes.

NOTE:

   These functions are temporary until a better way to plumb the DAC table
   address from the debuggee to debugger processes is implemented.

Revision History:


--*/

#include "pal/palinternal.h"
#include <sys/types.h>
#include <unistd.h>
#include <stdio.h>

/*++
Function
    PAL_PublishDacTableAddress

Parameters

    address
       [in] address of dac table
    size
       [in] size of dac table

Return Values
   pal errors

--*/
PALIMPORT
DWORD
PALAPI
PAL_PublishDacTableAddress(
    IN PVOID address,
    IN ULONG size)
{
    DWORD ret = NO_ERROR;

    // TODO - 3/5/15 - the DAC side needs the debuggee pid
    // pid_t pid = getpid();
    pid_t pid = 0;
    char fileName[100]; 
    snprintf(fileName, sizeof(fileName), "/tmp/%d_dacTable", pid);

    FILE *file = fopen(fileName, "w+");
    if (file != nullptr) 
    {
        char dacTableAddress[100]; 
        snprintf(dacTableAddress, sizeof(dacTableAddress), "%p %d\n", address, size);

        if (fputs(dacTableAddress, file) < 0)
        {
            ret = ERROR_INVALID_DATA;
        }

        fclose(file);
    }
    else
    {
        ret = ERROR_FILE_NOT_FOUND;
    }

    return ret;
}


/*++
Function
    PAL_GetDacTableAddress

Parameters

    pid
       [in] process id to get the data
    pAddress
       [out] pointer to put DAC table address
    pSize
       [out] pointer to put DAC table size

Return Values
   pal errors

--*/
PALIMPORT
DWORD
PALAPI
PAL_GetDacTableAddress(
    IN DWORD pid,
    OUT PVOID *pAddress,
    OUT PULONG pSize)
{
    DWORD ret = NO_ERROR;

    char fileName[100]; 
    snprintf(fileName, sizeof(fileName), "/tmp/%d_dacTable", pid);

    FILE *file = fopen(fileName, "r");
    if (file != nullptr) 
    {
        char data[100]; 
        if (fgets(data, sizeof(data), file) != nullptr)
        {
            if (sscanf(data, "%p %d\n", pAddress, pSize) != 2)
            {
                ret = ERROR_INVALID_DATA;
            }
        }
        else
        {
            ret = ERROR_INVALID_DATA;
        }

        fclose(file);
    }
    else
    {
        ret = ERROR_FILE_NOT_FOUND;
    }
    return ret;
}

/*++
Function
    PAL_CleanupDacTableAddress

Parameters
    None

Return Values
   None

--*/
PALIMPORT
VOID
PALAPI
PAL_CleanupDacTableAddress()
{
    //pid_t pid = getpid();
    pid_t pid = 0;
    char fileName[100]; 
    snprintf(fileName, sizeof(fileName), "/tmp/%d_dacTable", pid);

    remove(fileName);
}
