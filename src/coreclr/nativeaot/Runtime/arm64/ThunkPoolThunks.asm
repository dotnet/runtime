;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  STUBS & DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

THUNK_CODESIZE                      equ 0x10    ;; 3 instructions, 4 bytes each (and we also have 4 bytes of padding)
THUNK_DATASIZE                      equ 0x10    ;; 2 qwords

THUNK_POOL_NUM_THUNKS_PER_PAGE      equ 0x100   ;; 256 thunks per page

POINTER_SIZE                        equ 0x08

    MACRO
        NAMED_READONLY_DATA_SECTION $name, $areaAlias
        AREA    $areaAlias,DATA,READONLY
RO$name % 8
    MEND

    ;; This macro is used to declare the thunks data blocks. Unlike the macro above (which is just used for padding),
    ;; this macro needs to assign labels to each data block, so we can address them using PC-relative addresses.
    MACRO
        NAMED_READWRITE_DATA_SECTION $name, $areaAlias, $pageIndex
        AREA    $areaAlias,DATA
        THUNKS_DATA_PAGE_BLOCK $pageIndex
    MEND

    MACRO
        THUNK $index, $pageIndex
        ldr      x10, label_$index_P$pageIndex + POINTER_SIZE
        ldr      x12, label_$index_P$pageIndex
        br       x10

        brk     0xf000      ;; Stubs need to be 16-byte aligned for CFG table. Filling padding with a
                            ;; deterministic brk instruction, instead of having it just filled with zeros.
    MEND

    MACRO
        THUNK_DATA_BLOCK $index, $pageIndex

        ;; Each data block contains 2 qword cells. The data block is also labeled so it can be addressed
        ;; using PC relative instructions
label_$index_P$pageIndex
        DCQ 0
        DCQ 0
    MEND

    MACRO
        THUNKS_PAGE_BLOCK $pageIndex

    LCLA CurrentThunk
CurrentThunk SETA 0
    WHILE CurrentThunk < THUNK_POOL_NUM_THUNKS_PER_PAGE
        THUNK $CurrentThunk, $pageIndex
CurrentThunk SETA CurrentThunk + 1
    WEND
    MEND

    MACRO
        THUNKS_DATA_PAGE_BLOCK $pageIndex

    LCLA CurrentThunk
CurrentThunk SETA 0
    WHILE CurrentThunk < THUNK_POOL_NUM_THUNKS_PER_PAGE
        THUNK_DATA_BLOCK $CurrentThunk, $pageIndex
CurrentThunk SETA CurrentThunk + 1
    WEND
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
    ;; Declaring all the data section first since they have labels referenced by the stubs sections, to prevent
    ;; compilation errors ("undefined symbols"). The stubs/data sections will be correctly laid out in the image
    ;; using using the explicit layout configurations (ndp\rh\src\runtime\DLLs\mrt100_sectionlayout.txt)
    ;;
    NAMED_READWRITE_DATA_SECTION ThunkData0, "|.tkd0|", 0
    NAMED_READWRITE_DATA_SECTION ThunkData1, "|.tkd1|", 1
    NAMED_READWRITE_DATA_SECTION ThunkData2, "|.tkd2|", 2
    NAMED_READWRITE_DATA_SECTION ThunkData3, "|.tkd3|", 3
    NAMED_READWRITE_DATA_SECTION ThunkData4, "|.tkd4|", 4
    NAMED_READWRITE_DATA_SECTION ThunkData5, "|.tkd5|", 5
    NAMED_READWRITE_DATA_SECTION ThunkData6, "|.tkd6|", 6
    NAMED_READWRITE_DATA_SECTION ThunkData7, "|.tkd7|", 7

    ;;
    ;; Thunk Stubs
    ;; NOTE: Keep the number of thunk blocks in sync with the value returned by
    ;; RhpGetNumThunkBlocksPerMapping below.
    ;;

    LEAF_ENTRY ThunkPool, "|.tks0|"
        THUNKS_PAGE_BLOCK 0
    LEAF_END ThunkPool

    LEAF_ENTRY ThunkPool1, "|.tks1|"
        THUNKS_PAGE_BLOCK 1
    LEAF_END ThunkPool1

    LEAF_ENTRY ThunkPool2, "|.tks2|"
        THUNKS_PAGE_BLOCK 2
    LEAF_END ThunkPool2

    LEAF_ENTRY ThunkPool3, "|.tks3|"
        THUNKS_PAGE_BLOCK 3
    LEAF_END ThunkPool3

    LEAF_ENTRY ThunkPool4, "|.tks4|"
        THUNKS_PAGE_BLOCK 4
    LEAF_END ThunkPool4

    LEAF_ENTRY ThunkPool5, "|.tks5|"
        THUNKS_PAGE_BLOCK 5
    LEAF_END ThunkPool5

    LEAF_ENTRY ThunkPool6, "|.tks6|"
        THUNKS_PAGE_BLOCK 6
    LEAF_END ThunkPool6

    LEAF_ENTRY ThunkPool7, "|.tks7|"
        THUNKS_PAGE_BLOCK 7
    LEAF_END ThunkPool7


    ;;
    ;; IntPtr RhpGetThunksBase()
    ;;
    ;; ARM64TODO: There is a bug in the arm64 assembler which ends up with mis-sorted Pdata entries
    ;; for the functions in this file.  As a work around, don't generate pdata for these small stubs.
    ;; All the "No_PDATA" variants need to be removed after MASM bug 516396 is fixed.
    LEAF_ENTRY_NO_PDATA RhpGetThunksBase
        ;; Return the address of the first thunk pool to the caller (this is really the base address)
        ldr     x0, =ThunkPool
        ret
    LEAF_END_NO_PDATA RhpGetThunksBase


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; General Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

    ;;
    ;; int RhpGetNumThunksPerBlock()
    ;;
    LEAF_ENTRY_NO_PDATA RhpGetNumThunksPerBlock
        mov     x0, THUNK_POOL_NUM_THUNKS_PER_PAGE
        ret
    LEAF_END_NO_PDATA RhpGetNumThunksPerBlock

    ;;
    ;; int RhpGetThunkSize()
    ;;
    LEAF_ENTRY_NO_PDATA RhpGetThunkSize
        mov     x0, THUNK_CODESIZE
        ret
    LEAF_END_NO_PDATA RhpGetThunkSize

    ;;
    ;; int RhpGetNumThunkBlocksPerMapping()
    ;;
    LEAF_ENTRY_NO_PDATA RhpGetNumThunkBlocksPerMapping
        mov     x0, 8
        ret
    LEAF_END_NO_PDATA RhpGetNumThunkBlocksPerMapping

    ;;
    ;; int RhpGetThunkBlockSize
    ;;
    LEAF_ENTRY_NO_PDATA RhpGetThunkBlockSize
        mov     x0, PAGE_SIZE * 2
        ret
    LEAF_END_NO_PDATA RhpGetThunkBlockSize

    ;;
    ;; IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress)
    ;;
    LEAF_ENTRY_NO_PDATA RhpGetThunkDataBlockAddress
        mov     x12, PAGE_SIZE - 1
        bic     x0, x0, x12
        mov     x12, PAGE_SIZE
        add     x0, x0, x12
        ret
    LEAF_END_NO_PDATA RhpGetThunkDataBlockAddress

    ;;
    ;; IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress)
    ;;
    LEAF_ENTRY_NO_PDATA RhpGetThunkStubsBlockAddress
        mov     x12, PAGE_SIZE - 1
        bic     x0, x0, x12
        mov     x12, PAGE_SIZE
        sub     x0, x0, x12
        ret
    LEAF_END_NO_PDATA RhpGetThunkStubsBlockAddress

    END
