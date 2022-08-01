;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "kxarm.h"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  STUBS & DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

THUNK_CODESIZE                      equ 0x10    ;; 4-byte mov, 2-byte add, 4-byte str, 4-byte ldr, 2-byte branch
THUNK_DATASIZE                      equ 0x08    ;; 2 dwords

THUNK_POOL_NUM_THUNKS_PER_PAGE      equ 0xFA    ;; 250 thunks per page

PAGE_SIZE                           equ 0x1000  ;; 4K
POINTER_SIZE                        equ 0x04

    MACRO
        NAMED_READONLY_DATA_SECTION $name, $areaAlias
        AREA    $areaAlias,DATA,READONLY
RO$name % 4
    MEND

    MACRO
        NAMED_READWRITE_DATA_SECTION $name, $areaAlias
        AREA    $areaAlias,DATA
RW$name % 4
    MEND

    MACRO
        LOAD_DATA_ADDRESS $groupIndex, $index
        ALIGN       0x10                        ;; make sure we align to 16-byte boundary for CFG table

        ;; set r12 to beginning of data page : r12 <- pc - (THUNK_CODESIZE * current thunk's index - sizeof(mov+add instructions)) + PAGE_SIZE
        ;; fix offset of the data           : r12 <- r12 + (THUNK_DATASIZE * current thunk's index)
        mov.w     r12, PAGE_SIZE + ($groupIndex * THUNK_DATASIZE * 10 + THUNK_DATASIZE * $index) - (8 + $groupIndex * THUNK_CODESIZE * 10 + THUNK_CODESIZE * $index)
        add.n     r12, r12, pc
    MEND

    MACRO
        JUMP_TO_COMMON $groupIndex, $index
        ;; start                                        : r12 points to the current thunks first data cell in the data page
        ;; put r12 into the red zone                    : r12 isn't changed
        ;; set r12 to beginning of data page             : r12 <- r12 - (THUNK_DATASIZE * current thunk's index)
        ;; fix offset to point to last DWROD in page    : r12 <- r11 + PAGE_SIZE - POINTER_SIZE
        ;; jump to the location pointed at by the last dword in the data page
        str.w     r12, [sp, #-4]
        ldr.w     r12, [r12, #(PAGE_SIZE - POINTER_SIZE - ($groupIndex * THUNK_DATASIZE * 10 + THUNK_DATASIZE * $index))]
        bx.n      r12
    MEND

    MACRO
        TenThunks $groupIndex
        ;; Each thunk will load the address of its corresponding data (from the page that immediately follows)
        ;; and call a common stub. The address of the common stub is setup by the caller (last dword
        ;; in the thunks data section) depending on the 'kind' of thunks needed (interop, fat function pointers, etc...)

        ;; Each data block used by a thunk consists of two dword values:
        ;;      - Context: some value given to the thunk as context (passed in eax). Example for fat-fptrs: context = generic dictionary
        ;;      - Target : target code that the thunk eventually jumps to.

        LOAD_DATA_ADDRESS $groupIndex,0
        JUMP_TO_COMMON    $groupIndex,0

        LOAD_DATA_ADDRESS $groupIndex,1
        JUMP_TO_COMMON    $groupIndex,1

        LOAD_DATA_ADDRESS $groupIndex,2
        JUMP_TO_COMMON    $groupIndex,2

        LOAD_DATA_ADDRESS $groupIndex,3
        JUMP_TO_COMMON    $groupIndex,3

        LOAD_DATA_ADDRESS $groupIndex,4
        JUMP_TO_COMMON    $groupIndex,4

        LOAD_DATA_ADDRESS $groupIndex,5
        JUMP_TO_COMMON    $groupIndex,5

        LOAD_DATA_ADDRESS $groupIndex,6
        JUMP_TO_COMMON    $groupIndex,6

        LOAD_DATA_ADDRESS $groupIndex,7
        JUMP_TO_COMMON    $groupIndex,7

        LOAD_DATA_ADDRESS $groupIndex,8
        JUMP_TO_COMMON    $groupIndex,8

        LOAD_DATA_ADDRESS $groupIndex,9
        JUMP_TO_COMMON    $groupIndex,9
    MEND

    MACRO
        THUNKS_PAGE_BLOCK

        TenThunks 0
        TenThunks 1
        TenThunks 2
        TenThunks 3
        TenThunks 4
        TenThunks 5
        TenThunks 6
        TenThunks 7
        TenThunks 8
        TenThunks 9
        TenThunks 10
        TenThunks 11
        TenThunks 12
        TenThunks 13
        TenThunks 14
        TenThunks 15
        TenThunks 16
        TenThunks 17
        TenThunks 18
        TenThunks 19
        TenThunks 20
        TenThunks 21
        TenThunks 22
        TenThunks 23
        TenThunks 24
    MEND

    ;;
    ;; The first thunks section should be 64K aligned because it can get
    ;; mapped multiple  times in memory, and mapping works on allocation
    ;; granularity boundaries (we don't want to map more than what we need)
    ;;
    ;; The easiest way to do so is by having the thunks section at the
    ;; first 64K aligned virtual address in the binary. We provide a section
    ;; layout file to the linker to tell it how to layout the thunks sections
    ;; that we care about. (ndp\rh\src\runtime\DLLs\app\mrt100_app_sectionlayout.txt)
    ;;
    ;; The PE spec says images cannot have gaps between sections (other
    ;; than what is required by the section alignment value in the header),
    ;; therefore we need a couple of padding data sections (otherwise the
    ;; OS will not load the image).
    ;;

    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment0, "|.pad0|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment1, "|.pad1|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment2, "|.pad2|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment3, "|.pad3|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment4, "|.pad4|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment5, "|.pad5|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment6, "|.pad6|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment7, "|.pad7|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment8, "|.pad8|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment9, "|.pad9|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment10, "|.pad10|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment11, "|.pad11|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment12, "|.pad12|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment13, "|.pad13|"
    NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment14, "|.pad14|"

    ;;
    ;; Thunk Stubs
    ;; NOTE: Keep number of blocks in sync with macro/constant named 'NUM_THUNK_BLOCKS' in:
    ;;      - ndp\FxCore\src\System.Private.CoreLib\System\Runtime\InteropServices\ThunkPool.cs
    ;;      - ndp\rh\src\tools\rhbind\zapimage.h
    ;;
    LEAF_ENTRY ThunkPool, "|.tks0|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool

    NAMED_READWRITE_DATA_SECTION ThunkData0, "|.tkd0|"

    LEAF_ENTRY ThunkPool1, "|.tks1|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool1

    NAMED_READWRITE_DATA_SECTION ThunkData1, "|.tkd1|"

    LEAF_ENTRY ThunkPool2, "|.tks2|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool2

    NAMED_READWRITE_DATA_SECTION ThunkData2, "|.tkd2|"

    LEAF_ENTRY ThunkPool3, "|.tks3|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool3

    NAMED_READWRITE_DATA_SECTION ThunkData3, "|.tkd3|"

    LEAF_ENTRY ThunkPool4, "|.tks4|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool4

    NAMED_READWRITE_DATA_SECTION ThunkData4, "|.tkd4|"

    LEAF_ENTRY ThunkPool5, "|.tks5|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool5

    NAMED_READWRITE_DATA_SECTION ThunkData5, "|.tkd5|"

    LEAF_ENTRY ThunkPool6, "|.tks6|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool6

    NAMED_READWRITE_DATA_SECTION ThunkData6, "|.tkd6|"

    LEAF_ENTRY ThunkPool7, "|.tks7|"
        THUNKS_PAGE_BLOCK
    LEAF_END ThunkPool7

    NAMED_READWRITE_DATA_SECTION ThunkData7, "|.tkd7|"


    ;;
    ;; IntPtr RhpGetThunksBase()
    ;;
    LEAF_ENTRY RhpGetThunksBase
        ;; Return the address of the first thunk pool to the caller (this is really the base address)
        ldr     r0, =ThunkPool
        sub     r0, r0, #1
        bx      lr
    LEAF_END RhpGetThunksBase


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; General Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    ;;
    ;; int RhpGetNumThunksPerBlock()
    ;;
    LEAF_ENTRY RhpGetNumThunksPerBlock
        mov     r0, THUNK_POOL_NUM_THUNKS_PER_PAGE
        bx      lr
    LEAF_END RhpGetNumThunksPerBlock

    ;;
    ;; int RhpGetThunkSize()
    ;;
    LEAF_ENTRY RhpGetThunkSize
        mov     r0, THUNK_CODESIZE
        bx      lr
    LEAF_END RhpGetThunkSize

    ;;
    ;; int RhpGetNumThunkBlocksPerMapping()
    ;;
    LEAF_ENTRY RhpGetNumThunkBlocksPerMapping
        mov     r0, 8
        bx      lr
    LEAF_END RhpGetNumThunkBlocksPerMapping

    ;;
    ;; int RhpGetThunkBlockSize
    ;;
    LEAF_ENTRY RhpGetThunkBlockSize
        mov     r0, PAGE_SIZE * 2
        bx      lr
    LEAF_END RhpGetThunkBlockSize

    ;;
    ;; IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress)
    ;;
    LEAF_ENTRY RhpGetThunkDataBlockAddress
        mov     r12, PAGE_SIZE - 1
        bic     r0, r0, r12
        add     r0, PAGE_SIZE
        bx      lr
    LEAF_END RhpGetThunkDataBlockAddress

    ;;
    ;; IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress)
    ;;
    LEAF_ENTRY RhpGetThunkStubsBlockAddress
        mov     r12, PAGE_SIZE - 1
        bic     r0, r0, r12
        sub     r0, PAGE_SIZE
        bx      lr
    LEAF_END RhpGetThunkStubsBlockAddress

    END
