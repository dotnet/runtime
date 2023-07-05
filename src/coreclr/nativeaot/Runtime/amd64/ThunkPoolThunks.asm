;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

;; -----------------------------------------------------------------------------------------------------------
;;#include "asmmacros.inc"
;; -----------------------------------------------------------------------------------------------------------

LEAF_ENTRY macro Name, Section
    Section segment para 'CODE'
    align   16
    public  Name
    Name    proc
endm

NAMED_LEAF_ENTRY macro Name, Section, SectionAlias
    Section segment para alias(SectionAlias) 'CODE'
    align   16
    public  Name
    Name    proc
endm

LEAF_END macro Name, Section
    Name    endp
    Section ends
endm

NAMED_READONLY_DATA_SECTION macro Section, SectionAlias
    Section segment alias(SectionAlias) read 'DATA'
    align   16
    DQ 0
    Section ends
endm

NAMED_READWRITE_DATA_SECTION macro Section, SectionAlias
    Section segment alias(SectionAlias) read write 'DATA'
    align   16
    DQ 0
    Section ends
endm



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  STUBS & DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

THUNK_CODESIZE                      equ 10h     ;; 7-byte lea, 6-byte jmp, 3 bytes of nops
THUNK_DATASIZE                      equ 010h    ;; 2 qwords

THUNK_POOL_NUM_THUNKS_PER_PAGE      equ 0FAh    ;; 250 thunks per page

PAGE_SIZE                           equ 01000h  ;; 4K
POINTER_SIZE                        equ 08h


LOAD_DATA_ADDRESS macro groupIndex, index, thunkPool
        ALIGN   10h                             ;; make sure we align to 16-byte boundary for CFG table

        ;; set r10 to beginning of data page : r10 <- [thunkPool] + PAGE_SIZE
        ;; fix offset of the data           : r10 <- r10 + (THUNK_DATASIZE * current thunk's index)
        lea     r10, [thunkPool + PAGE_SIZE + (groupIndex * THUNK_DATASIZE * 10 + THUNK_DATASIZE * index)]
endm

JUMP_TO_COMMON macro groupIndex, index, thunkPool
        ;; jump to the location pointed at by the last qword in the data page
        jmp     qword ptr[thunkPool + PAGE_SIZE + PAGE_SIZE - POINTER_SIZE]
endm

TenThunks macro groupIndex, thunkPool
        ;; Each thunk will load the address of its corresponding data (from the page that immediately follows)
        ;; and call a common stub. The address of the common stub is setup by the caller (first qword
        ;; in the thunks data section, hence the +8's below) depending on the 'kind' of thunks needed (interop,
        ;; fat function pointers, etc...)

        ;; Each data block used by a thunk consists of two qword values:
        ;;      - Context: some value given to the thunk as context (passed in r10). Example for fat-fptrs: context = generic dictionary
        ;;      - Target : target code that the thunk eventually jumps to.

        LOAD_DATA_ADDRESS groupIndex,0,thunkPool
        JUMP_TO_COMMON    groupIndex,0,thunkPool

        LOAD_DATA_ADDRESS groupIndex,1,thunkPool
        JUMP_TO_COMMON    groupIndex,1,thunkPool

        LOAD_DATA_ADDRESS groupIndex,2,thunkPool
        JUMP_TO_COMMON    groupIndex,2,thunkPool

        LOAD_DATA_ADDRESS groupIndex,3,thunkPool
        JUMP_TO_COMMON    groupIndex,3,thunkPool

        LOAD_DATA_ADDRESS groupIndex,4,thunkPool
        JUMP_TO_COMMON    groupIndex,4,thunkPool

        LOAD_DATA_ADDRESS groupIndex,5,thunkPool
        JUMP_TO_COMMON    groupIndex,5,thunkPool

        LOAD_DATA_ADDRESS groupIndex,6,thunkPool
        JUMP_TO_COMMON    groupIndex,6,thunkPool

        LOAD_DATA_ADDRESS groupIndex,7,thunkPool
        JUMP_TO_COMMON    groupIndex,7,thunkPool

        LOAD_DATA_ADDRESS groupIndex,8,thunkPool
        JUMP_TO_COMMON    groupIndex,8,thunkPool

        LOAD_DATA_ADDRESS groupIndex,9,thunkPool
        JUMP_TO_COMMON    groupIndex,9,thunkPool
endm

THUNKS_PAGE_BLOCK macro thunkPool
        TenThunks 0,thunkPool
        TenThunks 1,thunkPool
        TenThunks 2,thunkPool
        TenThunks 3,thunkPool
        TenThunks 4,thunkPool
        TenThunks 5,thunkPool
        TenThunks 6,thunkPool
        TenThunks 7,thunkPool
        TenThunks 8,thunkPool
        TenThunks 9,thunkPool
        TenThunks 10,thunkPool
        TenThunks 11,thunkPool
        TenThunks 12,thunkPool
        TenThunks 13,thunkPool
        TenThunks 14,thunkPool
        TenThunks 15,thunkPool
        TenThunks 16,thunkPool
        TenThunks 17,thunkPool
        TenThunks 18,thunkPool
        TenThunks 19,thunkPool
        TenThunks 20,thunkPool
        TenThunks 21,thunkPool
        TenThunks 22,thunkPool
        TenThunks 23,thunkPool
        TenThunks 24,thunkPool
endm

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

NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment0, ".pad0"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment1, ".pad1"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment2, ".pad2"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment3, ".pad3"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment4, ".pad4"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment5, ".pad5"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment6, ".pad6"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment7, ".pad7"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment8, ".pad8"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment9, ".pad9"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment10, ".pad10"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment11, ".pad11"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment12, ".pad12"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment13, ".pad13"
NAMED_READONLY_DATA_SECTION PaddingFor64KAlignment14, ".pad14"

;;
;; Thunk Stubs
;; NOTE: Keep number of blocks in sync with macro/constant named 'NUM_THUNK_BLOCKS' in:
;;      - ndp\FxCore\src\System.Private.CoreLib\System\Runtime\InteropServices\ThunkPool.cs
;;      - ndp\rh\src\tools\rhbind\zapimage.h
;;
NAMED_LEAF_ENTRY ThunkPool, TKS0, ".tks0"
    THUNKS_PAGE_BLOCK ThunkPool
LEAF_END ThunkPool, TKS0

NAMED_READWRITE_DATA_SECTION ThunkData0, ".tkd0"

NAMED_LEAF_ENTRY ThunkPool1, TKS1, ".tks1"
    THUNKS_PAGE_BLOCK ThunkPool1
LEAF_END ThunkPool1, TKS1

NAMED_READWRITE_DATA_SECTION ThunkData1, ".tkd1"

NAMED_LEAF_ENTRY ThunkPool2, TKS2, ".tks2"
    THUNKS_PAGE_BLOCK ThunkPool2
LEAF_END ThunkPool2, TKS2

NAMED_READWRITE_DATA_SECTION ThunkData2, ".tkd2"

NAMED_LEAF_ENTRY ThunkPool3, TKS3, ".tks3"
    THUNKS_PAGE_BLOCK ThunkPool3
LEAF_END ThunkPool3, TKS3

NAMED_READWRITE_DATA_SECTION ThunkData3, ".tkd3"

NAMED_LEAF_ENTRY ThunkPool4, TKS4, ".tks4"
    THUNKS_PAGE_BLOCK ThunkPool4
LEAF_END ThunkPool4, TKS4

NAMED_READWRITE_DATA_SECTION ThunkData4, ".tkd4"

NAMED_LEAF_ENTRY ThunkPool5, TKS5, ".tks5"
    THUNKS_PAGE_BLOCK ThunkPool5
LEAF_END ThunkPool5, TKS5

NAMED_READWRITE_DATA_SECTION ThunkData5, ".tkd5"

NAMED_LEAF_ENTRY ThunkPool6, TKS6, ".tks6"
    THUNKS_PAGE_BLOCK ThunkPool6
LEAF_END ThunkPool6, TKS6

NAMED_READWRITE_DATA_SECTION ThunkData6, ".tkd6"

NAMED_LEAF_ENTRY ThunkPool7, TKS7, ".tks7"
    THUNKS_PAGE_BLOCK ThunkPool7
LEAF_END ThunkPool7, TKS7

NAMED_READWRITE_DATA_SECTION ThunkData7, ".tkd7"

;;
;; IntPtr RhpGetThunksBase()
;;
LEAF_ENTRY RhpGetThunksBase, _TEXT
        ;; Return the address of the first thunk pool to the caller (this is really the base address)
        lea     rax, [ThunkPool]
        ret
LEAF_END RhpGetThunksBase, _TEXT


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; General Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;
;; int RhpGetNumThunksPerBlock()
;;
LEAF_ENTRY RhpGetNumThunksPerBlock, _TEXT
        mov     rax, THUNK_POOL_NUM_THUNKS_PER_PAGE
        ret
LEAF_END RhpGetNumThunksPerBlock, _TEXT

;;
;; int RhpGetThunkSize()
;;
LEAF_ENTRY RhpGetThunkSize, _TEXT
        mov     rax, THUNK_CODESIZE
        ret
LEAF_END RhpGetThunkSize, _TEXT

;;
;; int RhpGetNumThunkBlocksPerMapping()
;;
LEAF_ENTRY RhpGetNumThunkBlocksPerMapping, _TEXT
        mov     rax, 8
        ret
LEAF_END RhpGetNumThunkBlocksPerMapping, _TEXT

;;
;; int RhpGetThunkBlockSize
;;
LEAF_ENTRY RhpGetThunkBlockSize, _TEXT
        mov     rax, PAGE_SIZE * 2
        ret
LEAF_END RhpGetThunkBlockSize, _TEXT

;;
;; IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress)
;;
LEAF_ENTRY RhpGetThunkDataBlockAddress, _TEXT
        mov     rax, rcx
        mov     rcx, PAGE_SIZE - 1
        not     rcx
        and     rax, rcx
        add     rax, PAGE_SIZE
        ret
LEAF_END RhpGetThunkDataBlockAddress, _TEXT

;;
;; IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress)
;;
LEAF_ENTRY RhpGetThunkStubsBlockAddress, _TEXT
        mov     rax, rcx
        mov     rcx, PAGE_SIZE - 1
        not     rcx
        and     rax, rcx
        sub     rax, PAGE_SIZE
        ret
LEAF_END RhpGetThunkStubsBlockAddress, _TEXT


end
