// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test5.c
**
** Purpose: Do more complex overlapping.  Move a section of memory back so
** that it actually ends up overlapping itself.
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
    char* NewAddress;
    char* SectionToMove;
    char TheMemory[MEMORY_AMOUNT];
    int i;
    
    if(PAL_Initialize(argc, argv))
    {
        return FAIL;
    }
    
    NewAddress = TheMemory;
    SectionToMove = TheMemory+50;

    /* Put some data into the first 50 bytes */
    memset(TheMemory, 'X', 50);

    /* Put some data into the rest of the memory */
    memset(SectionToMove, 'Z', MEMORY_AMOUNT-50); 
 
    /* Move the section in the middle of TheMemory back to the start of
       TheMemory -- but have it also overlap itself.  (ie. the section
       to be move is overlapping itself)
    */
    RtlMoveMemory(NewAddress, SectionToMove, MEMORY_AMOUNT-50);

    /* Check to ensure the moved data didn't get corrupted     */
    for(i=0; i<MEMORY_AMOUNT-50; ++i)
    {
        if(NewAddress[i] != 'Z')
        {
            Fail("ERROR: When the memory was moved to a new location, the "
                 "data which was stored in it was somehow corrupted.  "
                 "Character %d should have been 'Z' but instead is %c.\n",
                 i, NewAddress[i]);
        }
    }
  
    PAL_Terminate();
    return PASS;
}
