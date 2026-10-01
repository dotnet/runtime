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

THUNK_CODESIZE                      equ 10h     ;; 7-byte mov, 6-byte jmp, 3 bytes of nops
THUNK_DATASIZE                      equ 010h    ;; 2 qwords

THUNK_POOL_NUM_THUNKS_PER_PAGE      equ 100h    ;; 256 thunks per page

PAGE_SIZE                           equ 01000h  ;; 4K
POINTER_SIZE                        equ 08h


THUNK macro index, thunkPool
        ALIGN   10h                             ;; make sure we align to 16-byte boundary for CFG table

        ;; Each data block used by a thunk consists of two qword values:
        ;;      - Context: a value passed to the thunk target in r10.
        ;;      - Target : target code that the thunk eventually jumps to.

        mov     r10, qword ptr [thunkPool + PAGE_SIZE + (THUNK_DATASIZE * index)]
        jmp     qword ptr [thunkPool + PAGE_SIZE + POINTER_SIZE + (THUNK_DATASIZE * index)]
endm

THUNKS_PAGE_BLOCK macro thunkPool
ThunkIndex = 0
    while ThunkIndex lt THUNK_POOL_NUM_THUNKS_PER_PAGE
        THUNK ThunkIndex, thunkPool
ThunkIndex = ThunkIndex + 1
    endm
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
;; NOTE: Keep the number of thunk blocks in sync with the value returned by
;; RhpGetNumThunkBlocksPerMapping below.
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
