//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetSystemInfo() function
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {
  
    SYSTEM_INFO TheSystemInfo;
    SYSTEM_INFO* pSystemInfo = &TheSystemInfo;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    GetSystemInfo(pSystemInfo);

    /* Ensure both valules are > than 0 */
    if(pSystemInfo->dwNumberOfProcessors < 1) 
    {
        Fail("ERROR: The dwNumberofProcessors values should be > 0.");
    } 

    if(pSystemInfo->dwPageSize < 1) 
    {
        Fail("ERROR: The dwPageSize should be greater than 0.");    
    }

    /* If this isn't WIN32, ensure all the other variables are 0 */
  
#if UNIX
    if(pSystemInfo->dwOemId != 0 ||  
       pSystemInfo->wProcessorArchitecture != 0 || 
       pSystemInfo->wReserved != 0 ||
       pSystemInfo->lpMinimumApplicationAddress != 0 || 
       pSystemInfo->lpMaximumApplicationAddress != 0 || 
       pSystemInfo->dwActiveProcessorMask != 0 ||
       pSystemInfo->dwProcessorType !=0 || 
       pSystemInfo->dwAllocationGranularity !=0 ||
       pSystemInfo->wProcessorLevel != 0 ||
       pSystemInfo->wProcessorRevision != 0) {
        Fail("ERROR: Under FreeBSD, OemId, ProcessorArchitecture, Reserved, "
             "MinimumApplicationAddress, MaximumApplicationAddress, "
             "ActiveProcessorMask, ProcessorType, AllocationGranularity, "
             "ProcessorLevel and ProcessorRevision should be equal to 0.");
      
    
    } 
#endif
  
    
    PAL_Terminate();
    return PASS;
}


