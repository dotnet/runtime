;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "ksarm64.h"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  STUBS & DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

THUNK_CODESIZE                      equ 0x10    ;; 3 instructions, 4 bytes each (and we also have 4 bytes of padding)
THUNK_DATASIZE                      equ 0x10    ;; 2 qwords

THUNK_POOL_NUM_THUNKS_PER_PAGE      equ 0xFA    ;; 250 thunks per page

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
        LOAD_DATA_ADDRESS $groupIndex, $index, $pageIndex

        ;; Set xip0 to the address of the current thunk's data block. This is done using labels.
        adr      xip0, label_$groupIndex_$index_P$pageIndex
    MEND

    MACRO
        JUMP_TO_COMMON $groupIndex, $index
        ;; start                                        : xip0 points to the current thunks first data cell in the data page
        ;; set xip0 to beginning of data page            : xip0 <- xip0 - (THUNK_DATASIZE * current thunk's index)
        ;; fix offset to point to last QWROD in page    : xip1 <- [xip0 + PAGE_SIZE - POINTER_SIZE]
        ;; tailcall to the location pointed at by the last qword in the data page
        ldr      xip1, [xip0, #(PAGE_SIZE - POINTER_SIZE - ($groupIndex * THUNK_DATASIZE * 10 + THUNK_DATASIZE * $index))]
        br       xip1

        brk     0xf000      ;; Stubs need to be 16-byte aligned for CFG table. Filling padding with a
                            ;; deterministic brk instruction, instead of having it just filled with zeros.
    MEND

    MACRO
        THUNK_LABELED_DATA_BLOCK $groupIndex, $index, $pageIndex

        ;; Each data block contains 2 qword cells. The data block is also labeled so it can be addressed
        ;; using PC relative instructions
label_$groupIndex_$index_P$pageIndex
        DCQ 0
        DCQ 0
    MEND

    MACRO
        TenThunks $groupIndex, $pageIndex

        ;; Each thunk will load the address of its corresponding data (from the page that immediately follows)
        ;; and call a common stub. The address of the common stub is setup by the caller (last qword
        ;; in the thunks data section) depending on the 'kind' of thunks needed (interop, fat function pointers, etc...)

        ;; Each data block used by a thunk consists of two qword values:
        ;;      - Context: some value given to the thunk as context. Example for fat-fptrs: context = generic dictionary
        ;;      - Target : target code that the thunk eventually jumps to.

        LOAD_DATA_ADDRESS $groupIndex,0,$pageIndex
        JUMP_TO_COMMON    $groupIndex,0

        LOAD_DATA_ADDRESS $groupIndex,1,$pageIndex
        JUMP_TO_COMMON    $groupIndex,1

        LOAD_DATA_ADDRESS $groupIndex,2,$pageIndex
        JUMP_TO_COMMON    $groupIndex,2

        LOAD_DATA_ADDRESS $groupIndex,3,$pageIndex
        JUMP_TO_COMMON    $groupIndex,3

        LOAD_DATA_ADDRESS $groupIndex,4,$pageIndex
        JUMP_TO_COMMON    $groupIndex,4

        LOAD_DATA_ADDRESS $groupIndex,5,$pageIndex
        JUMP_TO_COMMON    $groupIndex,5

        LOAD_DATA_ADDRESS $groupIndex,6,$pageIndex
        JUMP_TO_COMMON    $groupIndex,6

        LOAD_DATA_ADDRESS $groupIndex,7,$pageIndex
        JUMP_TO_COMMON    $groupIndex,7

        LOAD_DATA_ADDRESS $groupIndex,8,$pageIndex
        JUMP_TO_COMMON    $groupIndex,8

        LOAD_DATA_ADDRESS $groupIndex,9,$pageIndex
        JUMP_TO_COMMON    $groupIndex,9
    MEND

    MACRO
        TenThunkDataBlocks $groupIndex, $pageIndex

        ;; Similar to the thunks stubs block, we declare the thunks data blocks here

        THUNK_LABELED_DATA_BLOCK $groupIndex, 0, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 1, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 2, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 3, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 4, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 5, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 6, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 7, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 8, $pageIndex
        THUNK_LABELED_DATA_BLOCK $groupIndex, 9, $pageIndex
    MEND

    MACRO
        THUNKS_PAGE_BLOCK $pageIndex

        TenThunks 0, $pageIndex
        TenThunks 1, $pageIndex
        TenThunks 2, $pageIndex
        TenThunks 3, $pageIndex
        TenThunks 4, $pageIndex
        TenThunks 5, $pageIndex
        TenThunks 6, $pageIndex
        TenThunks 7, $pageIndex
        TenThunks 8, $pageIndex
        TenThunks 9, $pageIndex
        TenThunks 10, $pageIndex
        TenThunks 11, $pageIndex
        TenThunks 12, $pageIndex
        TenThunks 13, $pageIndex
        TenThunks 14, $pageIndex
        TenThunks 15, $pageIndex
        TenThunks 16, $pageIndex
        TenThunks 17, $pageIndex
        TenThunks 18, $pageIndex
        TenThunks 19, $pageIndex
        TenThunks 20, $pageIndex
        TenThunks 21, $pageIndex
        TenThunks 22, $pageIndex
        TenThunks 23, $pageIndex
        TenThunks 24, $pageIndex
    MEND

    MACRO
        THUNKS_DATA_PAGE_BLOCK $pageIndex

        TenThunkDataBlocks 0, $pageIndex
        TenThunkDataBlocks 1, $pageIndex
        TenThunkDataBlocks 2, $pageIndex
        TenThunkDataBlocks 3, $pageIndex
        TenThunkDataBlocks 4, $pageIndex
        TenThunkDataBlocks 5, $pageIndex
        TenThunkDataBlocks 6, $pageIndex
        TenThunkDataBlocks 7, $pageIndex
        TenThunkDataBlocks 8, $pageIndex
        TenThunkDataBlocks 9, $pageIndex
        TenThunkDataBlocks 10, $pageIndex
        TenThunkDataBlocks 11, $pageIndex
        TenThunkDataBlocks 12, $pageIndex
        TenThunkDataBlocks 13, $pageIndex
        TenThunkDataBlocks 14, $pageIndex
        TenThunkDataBlocks 15, $pageIndex
        TenThunkDataBlocks 16, $pageIndex
        TenThunkDataBlocks 17, $pageIndex
        TenThunkDataBlocks 18, $pageIndex
        TenThunkDataBlocks 19, $pageIndex
        TenThunkDataBlocks 20, $pageIndex
        TenThunkDataBlocks 21, $pageIndex
        TenThunkDataBlocks 22, $pageIndex
        TenThunkDataBlocks 23, $pageIndex
        TenThunkDataBlocks 24, $pageIndex
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
    ;; NOTE: Keep number of blocks in sync with macro/constant named 'NUM_THUNK_BLOCKS' in:
    ;;      - ndp\FxCore\src\System.Private.CoreLib\System\Runtime\InteropServices\ThunkPool.cs
    ;;      - ndp\rh\src\tools\rhbind\zapimage.h
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
