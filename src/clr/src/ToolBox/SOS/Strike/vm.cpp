// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    vm.cxx

Abstract:

    This module contains an NTSD debugger extension for dumping various
    virtual memory statistics.

Revision History:

--*/

#ifdef FEATURE_PAL
#error This file is Win32 only.
#endif // #ifdef FEATURE_PAL

#include <tchar.h>


#include "strike.h"
#include "util.h"
#include "gcinfo.h"
#include "disasm.h"
#include <dbghelp.h>

#include "corhdr.h"
#include "cor.h"
#include "dacprivate.h"



//
// Private constants.
//

#define SMALL_REGION        (64 * 1024)
#define MEDIUM_REGION       (1 * 1024 * 1024)

#define IS_SMALL(c)         ((c) <= SMALL_REGION)
#define IS_MEDIUM(c)        (((c) > SMALL_REGION) && ((c) <= MEDIUM_REGION))
#define IS_LARGE(c)         ((c) > MEDIUM_REGION)

#define PRINTF_FORMAT_HEAD  "%-7s  %*s  %*s  %*s %*s  %*s\n"
#define PRINTF_FORMAT       "%-7s %*sK %*sK %*sK %*s %*sK\n"

#define CCH_ULONGLONG_COMMAS   _countof("18,446,744,073,709,551,616")
#define CCH_ULONGLONG_MINIMUM_COMMAS (CCH_ULONGLONG_COMMAS - 3)
#define CCH_ULONGLONG_BLOCKCOUNT_COMMAS    sizeof("1,000,000")


//
// Private types.
//

typedef struct _INDIVIDUAL_STAT
{
    SIZE_T MinimumSize;
    SIZE_T MaximumSize;
    SIZE_T TotalSize;
    SIZE_T BlockCount;

} INDIVIDUAL_STAT, *PINDIVIDUAL_STAT;

typedef struct _VM_STATS
{
    INDIVIDUAL_STAT Summary;
    INDIVIDUAL_STAT Small;
    INDIVIDUAL_STAT Medium;
    INDIVIDUAL_STAT Large;

} VM_STATS, *PVM_STATS;

typedef struct PROTECT_MASK
{
    DWORD Bit;
    PCSTR Name;

} PROTECT_MASK, *PPROTECT_MASK;


//
// Private globals.
//

PROTECT_MASK ProtectMasks[] =
    {
        {
            PAGE_NOACCESS,
            "NA"
        },

        {
            PAGE_NOCACHE,
            "NC"
        },

        {
            PAGE_GUARD,
            "G"
        },

        {
            PAGE_READONLY,
            "Rd"
        },

        {
            PAGE_READWRITE,
            "RdWr"
        },

        {
            PAGE_WRITECOPY,
            "WrCp"
        },

        {
            PAGE_EXECUTE,
            "Ex"
        },

        {
            PAGE_EXECUTE_READ,
            "ExRd"
        },

        {
            PAGE_EXECUTE_READWRITE,
            "ExRdWr"
        },

        {
            PAGE_EXECUTE_WRITECOPY,
            "ExWrCp"
        }
    };

#define NUM_PROTECT_MASKS (sizeof(ProtectMasks) / sizeof(ProtectMasks[0]))

//
// Private functions.
//


PSTR
ULongLongToString(
    IN ULONGLONG Value,
    __out_ecount (CCH_ULONGLONG_COMMAS) OUT PSTR Buffer
    )
{

    PSTR p1;
    PSTR p2;
    CHAR ch;
    INT digit;
    INT count;
    BOOL needComma;
    INT length;

    //
    // Handling zero specially makes everything else a bit easier.
    //

    if( Value == 0 ) {
        Buffer[0] = '0';
        Buffer[1] = '\0';
        return Buffer;
    }

    //
    // Pull the least signifigant digits off the value and store them
    // into the buffer. Note that this will store the digits in the
    // reverse order.
    //

    p1 = p2 = Buffer;
    count = 3;
    needComma = FALSE;

    while( Value != 0 ) {

        if( needComma ) {
            *p1++ = ',';
            needComma = FALSE;
        }

        digit = (INT)( Value % 10 );
        Value = Value / 10;

        *p1++ = '0' + (CHAR) digit;

        count--;
        if( count == 0 ) {
            count = 3;
            needComma = TRUE;
        }

    }

    length = (INT)(((size_t)p1) - ((size_t)Buffer));

    //
    // Reverse the digits in the buffer.
    //

    *p1-- = '\0';

    while( p1 > p2 ) {

        ch = *p1;
        *p1 = *p2;
        *p2 = ch;

        p2++;
        p1--;

    }

    return Buffer;

}   // ULongLongToString

VOID
InitVmStats(
    OUT PVM_STATS Stats
    )
{
    ZeroMemory( Stats, sizeof(*Stats) );
    Stats->Summary.MinimumSize = (SIZE_T)-1L;
    Stats->Small.MinimumSize = (SIZE_T)-1L;
    Stats->Medium.MinimumSize = (SIZE_T)-1L;
    Stats->Large.MinimumSize = (SIZE_T)-1L;

}   // InitVmStats

VOID
UpdateIndividualStat(
    IN OUT PINDIVIDUAL_STAT Stat,
    IN SIZE_T BlockSize
    )
{
    Stat->BlockCount++;
    Stat->TotalSize += BlockSize;

    if( BlockSize > Stat->MaximumSize ) {
        Stat->MaximumSize = BlockSize;
    }

    if( BlockSize < Stat->MinimumSize ) {
        Stat->MinimumSize = BlockSize;
    }

}   // UpdateIndividualStat

VOID
UpdateVmStats(
    IN OUT PVM_STATS Stats,
    IN SIZE_T BlockSize
    )
{
    UpdateIndividualStat( &Stats->Summary, BlockSize );

    if( IS_SMALL(BlockSize) ) {
        UpdateIndividualStat( &Stats->Small, BlockSize );
    }

    if( IS_MEDIUM(BlockSize) ) {
        UpdateIndividualStat( &Stats->Medium, BlockSize );
    }

    if( IS_LARGE(BlockSize) ) {
        UpdateIndividualStat( &Stats->Large, BlockSize );
    }

}   // UpdateVmStats

VOID
PrintVmStatsHeader(
    VOID
    )
{
    ExtOut(
        PRINTF_FORMAT_HEAD,
        "TYPE",
        CCH_ULONGLONG_MINIMUM_COMMAS,
        "MINIMUM",
        CCH_ULONGLONG_COMMAS,
        "MAXIMUM",
        CCH_ULONGLONG_COMMAS,
        "AVERAGE",
        CCH_ULONGLONG_BLOCKCOUNT_COMMAS,
        "BLK COUNT",
        CCH_ULONGLONG_COMMAS,
        "TOTAL"
        );

    ExtOut(
        PRINTF_FORMAT_HEAD,
        "~~~~",
        CCH_ULONGLONG_MINIMUM_COMMAS,
        "~~~~~~~",
        CCH_ULONGLONG_COMMAS,
        "~~~~~~~",
        CCH_ULONGLONG_COMMAS,
        "~~~~~~~",
        CCH_ULONGLONG_BLOCKCOUNT_COMMAS,
        "~~~~~~~~~",
        CCH_ULONGLONG_COMMAS,
        "~~~~~"
        );

}   // PrintVmStatsHeader

#define BYTES_TO_K(x) (x/1024)

VOID
PrintIndividualStat(
    ___in __in_z IN PCSTR Name,
    IN PINDIVIDUAL_STAT Stat
    )
{
    SIZE_T average;
    SIZE_T minsize;
    CHAR minStr[CCH_ULONGLONG_COMMAS];
    CHAR maxStr[CCH_ULONGLONG_COMMAS];
    CHAR avgStr[CCH_ULONGLONG_COMMAS];
    CHAR countStr[CCH_ULONGLONG_COMMAS];
    CHAR totalStr[CCH_ULONGLONG_COMMAS];

    if( Stat->BlockCount == 0 ) {
        average = 0;
        minsize = 0;
    } else {
        average = Stat->TotalSize / Stat->BlockCount;
        minsize = Stat->MinimumSize;
    }

    ExtOut(
        PRINTF_FORMAT,
        Name,
        CCH_ULONGLONG_MINIMUM_COMMAS,
        ULongLongToString(
            (ULONGLONG)BYTES_TO_K(minsize),
            minStr
            ),
        CCH_ULONGLONG_COMMAS,
        ULongLongToString(
            (ULONGLONG)BYTES_TO_K(Stat->MaximumSize),
            maxStr
            ),
        CCH_ULONGLONG_COMMAS,
        ULongLongToString(
            (ULONGLONG)BYTES_TO_K(average),
            avgStr
            ),
        CCH_ULONGLONG_BLOCKCOUNT_COMMAS,
        ULongLongToString(
            (ULONGLONG)Stat->BlockCount,
            countStr
            ),
        CCH_ULONGLONG_COMMAS,
        ULongLongToString(
            (ULONGLONG)BYTES_TO_K(Stat->BlockCount * average),
            totalStr
            )            
        );

}   // PrintIndividualStat


VOID
PrintVmStats(
    ___in __in_z IN PCSTR Name,
    IN PVM_STATS Stats
    )
{
    ExtOut( "%s:\n", Name );

    PrintIndividualStat( "Small", &Stats->Small );
    PrintIndividualStat( "Medium", &Stats->Medium );
    PrintIndividualStat( "Large", &Stats->Large );
    PrintIndividualStat( "Summary", &Stats->Summary );

    ExtOut( "\n" );

}   // PrintVmStats

PSTR
VmProtectToString(
    IN DWORD Protect,
    __out_ecount(capacity_Buffer) OUT PSTR Buffer,
    size_t capacity_Buffer
    )
{
    INT i;
    PPROTECT_MASK mask;

    Buffer[0] = '\0';

    for( i = 0, mask = &ProtectMasks[0] ;
        (i < NUM_PROTECT_MASKS) && (Protect != 0) ;
        i++, mask++ ) {
        if( mask->Bit & Protect ) {
            Protect &= ~mask->Bit;
            if( Buffer[0] != '\0' ) {
                strcat_s(Buffer,capacity_Buffer, "|" );
            }
            strcat_s( Buffer, capacity_Buffer, mask->Name );
        }
    }

    if( Protect != 0 ) {
        if( Buffer[0] != '\0' ) {
            strcat_s( Buffer, capacity_Buffer, "|" );
        }
        size_t len_Buffer = strlen(Buffer);
        size_t cbSizeInBytes = 0;
        if (!ClrSafeInt<size_t>::subtraction(capacity_Buffer, len_Buffer, cbSizeInBytes))
        {
            ExtOut("<integer underflow>\n");
            return Buffer;
        }
        sprintf_s( Buffer + len_Buffer, cbSizeInBytes, "%08lx", Protect );
    }

    return Buffer;

}   // VmProtectToString

PSTR
VmStateToString(
    IN DWORD State,
    __out_ecount(capacity_Buffer) OUT PSTR Buffer,
    size_t capacity_Buffer
    )
{
    PCSTR result;
    CHAR invalidStr[sizeof("12345678")];

    switch( State )
    {
    case MEM_COMMIT:
        result = "Commit";
        break;

    case MEM_RESERVE:
        result = "Reserve";
        break;

    case MEM_FREE:
        result = "Free";
        break;

    default:
        sprintf_s(invalidStr,_countof(invalidStr), "%08lx", State );
        result = invalidStr;
        break;
    }

    strcpy_s( Buffer, capacity_Buffer, result );
    return Buffer;

}   // VmStateToString

PSTR
VmTypeToString(
    IN DWORD Type,
    __out_ecount(capacity_Buffer) OUT PSTR Buffer,
    size_t capacity_Buffer
    )
{
    PCSTR result;
    CHAR invalidStr[sizeof("12345678")];

    switch( Type )
    {
    case MEM_PRIVATE:
        result = "Private";
        break;

    case MEM_MAPPED:
        result = "Mapped";
        break;

    case MEM_IMAGE:
        result = "Image";
        break;

    case 0:
        result = "";
        break;

    default:
        sprintf_s(invalidStr,_countof(invalidStr), "%08lx", Type );
        result = invalidStr;
        break;
    }
    strcpy_s( Buffer,capacity_Buffer,  result );
    return Buffer;

}   // VmTypeToString

/************************************************************
 * Dump Virtual Memory Info
 ************************************************************/

void vmstat()

/*++

Routine Description:

    This function is called as an NTSD extension to format and dump
    virtual memory statistics.

Arguments:

Return Value:

    None.

--*/

{

    NTSTATUS status;
    ULONG64 address;
    MEMORY_BASIC_INFORMATION64 memInfo;
    VM_STATS freeStats;
    VM_STATS reserveStats;
    VM_STATS commitStats;
    VM_STATS privateStats;
    VM_STATS mappedStats;
    VM_STATS imageStats;
        
    //
    // Setup.
    //
    
    InitVmStats( &freeStats );
    InitVmStats( &reserveStats );
    InitVmStats( &commitStats );
    InitVmStats( &privateStats );
    InitVmStats( &mappedStats );
    InitVmStats( &imageStats );

    address = 0;

    //
    // Scan the virtual address space.
    //

    for( ; ; ) {
        status = g_ExtData2->QueryVirtual(address, &memInfo);        

        if( !NT_SUCCESS(status) ) {
            break;
        }

        //
        // Interpret the memory state.
        //

        SIZE_T regionSize = (SIZE_T) memInfo.RegionSize;

        switch( memInfo.State ) {
            
        case MEM_FREE:
            UpdateVmStats( &freeStats, regionSize );
            break;

        case MEM_RESERVE:
            UpdateVmStats( &reserveStats, regionSize );
            break;

        case MEM_COMMIT:
            UpdateVmStats( &commitStats, regionSize );
            break;
        }

        //
        // Interpret the memory type.
        //

        switch( memInfo.Type ) {
        case MEM_PRIVATE:
            UpdateVmStats( &privateStats, regionSize );
            break;

        case MEM_MAPPED:
            UpdateVmStats( &mappedStats, regionSize );
            break;

        case MEM_IMAGE:
            UpdateVmStats( &imageStats, regionSize );
            break;
        }

        //
        // Advance to the next block.
        //

        address += memInfo.RegionSize;
    }

    //
    // Dump it.
    //

    PrintVmStatsHeader();
    PrintVmStats( "Free", &freeStats );
    PrintVmStats( "Reserve", &reserveStats );
    PrintVmStats( "Commit", &commitStats );
    PrintVmStats( "Private", &privateStats );
    PrintVmStats( "Mapped", &mappedStats );
    PrintVmStats( "Image", &imageStats );

}   // DECLARE_API( vmstat )


void vmmap()

/*++

Routine Description:

    This function is called as an NTSD extension to format and dump
    the debugee's virtual memory address space.

Arguments:

Return Value:

    None.

--*/

{

    NTSTATUS status;
    ULONG64 address;
    MEMORY_BASIC_INFORMATION64 memInfo;
    CHAR protectStr[32];
    CHAR aprotectStr[32];
    CHAR stateStr[16];
    CHAR typeStr[16];

    //
    // Setup.
    //
    
    address = 0;

    ExtOut(
        "%-*s %-*s %-*s  %-13s %-13s %-8s %-8s\n",
        sizeof(PVOID) * 2,
        "Start",
        sizeof(PVOID) * 2,
        "Stop",
        sizeof(PVOID) * 2,
        "Length",
        "AllocProtect",
        "Protect",
        "State",
        "Type"
        );

    //
    // Scan the virtual address space.
    //

    for( ; ; ) {

        if (IsInterrupt())
            break;
        
        status = g_ExtData2->QueryVirtual(address, &memInfo);
                
        if( !NT_SUCCESS(status) ) {
            break;
        }

        //
        // Dump the current entry.
        //

        ExtOut(
            "%p-%p %p  %-13s %-13s %-8s %-8s\n",
            SOS_PTR(memInfo.BaseAddress),
            SOS_PTR(((ULONG_PTR)memInfo.BaseAddress + memInfo.RegionSize - 1)),
            SOS_PTR(memInfo.RegionSize),
            VmProtectToString( memInfo.AllocationProtect, aprotectStr, _countof(aprotectStr) ),
            VmProtectToString( memInfo.Protect, protectStr, _countof(protectStr)  ),
            VmStateToString( memInfo.State, stateStr, _countof(stateStr) ),
            VmTypeToString( memInfo.Type, typeStr , _countof(typeStr))
            );

        //
        // Advance to the next block.
        //

        address += memInfo.RegionSize;
    }

}   // DECLARE_API( vmmap )
