//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: test1.c
**
** Purpose: Simple test -- have two 128 blocks of memory allocated.  Then
** move one block to the other and check to see that the data was not
** corrupted.
**
**
**============================================================*/

#include <palsuite.h>

enum Memory 
{
    MEMORY_AMOUNT = 128
};

int __cdecl main(int argc, char *argv[])
{
    char NewAddress[MEMORY_AMOUNT];
    char OldAddress[MEMORY_AMOUNT];
    int i;
    char temp;
    
    if(PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Put some data into the block we'll be moving */
    memset(OldAddress, 'X', MEMORY_AMOUNT);

    /* Move the block to the NewAddress */
    RtlMoveMemory(NewAddress, OldAddress, MEMORY_AMOUNT);

    /* Check to ensure the data didn't get corrupted */
    for(i=0; i<MEMORY_AMOUNT; ++i)
    {
        if(NewAddress[i] != 'X')
        {
            temp = NewAddress[i];
            Fail("ERROR: When the memory was moved to a new location, the "
                 "data which was stored in it was somehow corrupted.  "
                 "Character %d should have been 'X' but instead is %c.\n",
                 i, temp);
        }
    }
  
    PAL_Terminate();
    return PASS;
}
