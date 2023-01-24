;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

.586
.model  flat
option  casemap:none
.code

include AsmMacros.inc

;; -----------------------------------------------------------------------------------------------------------
;; standard macros
;; -----------------------------------------------------------------------------------------------------------
LEAF_ENTRY macro Name, Section
    Section segment para 'CODE'
    public  Name
    Name    proc
endm

NAMED_LEAF_ENTRY macro Name, Section, SectionAlias
    Section segment para alias(SectionAlias) 'CODE'
    public  Name
    Name    proc
endm

LEAF_END macro Name, Section
    Name    endp
    Section ends
endm

NAMED_READONLY_DATA_SECTION macro Section, SectionAlias
    Section segment para alias(SectionAlias) read 'DATA'
    DD 0
    Section ends
endm

NAMED_READWRITE_DATA_SECTION macro Section, SectionAlias
    Section segment para alias(SectionAlias) read write 'DATA'
    DD 0
    Section ends
endm


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;  STUBS & DATA SECTIONS  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

THUNK_CODESIZE                      equ 20h     ;; 5-byte call, 1 byte pop, 6-byte lea, 6-byte jmp, 14 bytes of padding
THUNK_DATASIZE                      equ 08h     ;; 2 dwords

THUNK_POOL_NUM_THUNKS_PER_PAGE      equ 078h    ;; 120 thunks per page

PAGE_SIZE                           equ 01000h  ;; 4K
POINTER_SIZE                        equ 04h


GET_CURRENT_IP macro
        ALIGN   10h                             ;; make sure we align to 16-byte boundary for CFG table
        call    @F
    @@: pop     eax
endm

LOAD_DATA_ADDRESS macro groupIndex, index
        ;; start                            : eax points to current instruction of the current thunk
        ;; set eax to beginning of data page : eax <- [eax - (size of the call instruction + (THUNK_CODESIZE * current thunk's index)) + PAGE_SIZE]
        ;; fix offset of the data           : eax <- eax + (THUNK_DATASIZE * current thunk's index)
        lea     eax,[eax - (5 + groupIndex * THUNK_CODESIZE * 10 + THUNK_CODESIZE * index) + PAGE_SIZE + (groupIndex * THUNK_DATASIZE * 10 + THUNK_DATASIZE * index)]
endm

JUMP_TO_COMMON macro groupIndex, index
        ;; start                                   : eax points to current thunk's data block
        ;; re-point eax to beginning of data page   : eax <- [eax - (THUNK_DATASIZE * current thunk's index)]
        ;; jump to the location pointed at by the last dword in the data page : jump [eax + PAGE_SIZE - POINTER_SIZE]
        jmp     dword ptr[eax - (groupIndex * THUNK_DATASIZE * 10 + THUNK_DATASIZE * index) + PAGE_SIZE - POINTER_SIZE]
endm

TenThunks macro groupIndex
        ;; Each thunk will load the address of its corresponding data (from the page that immediately follows)
        ;; and call a common stub. The address of the common stub is setup by the caller (last dword
        ;; in the thunks data section) depending on the 'kind' of thunks needed (interop, fat function pointers, etc...)

        ;; Each data block used by a thunk consists of two dword values:
        ;;      - Context: some value given to the thunk as context (passed in eax). Example for fat-fptrs: context = generic dictionary
        ;;      - Target : target code that the thunk eventually jumps to.

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,0
        JUMP_TO_COMMON    groupIndex,0

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,1
        JUMP_TO_COMMON    groupIndex,1

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,2
        JUMP_TO_COMMON    groupIndex,2

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,3
        JUMP_TO_COMMON    groupIndex,3

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,4
        JUMP_TO_COMMON    groupIndex,4

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,5
        JUMP_TO_COMMON    groupIndex,5

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,6
        JUMP_TO_COMMON    groupIndex,6

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,7
        JUMP_TO_COMMON    groupIndex,7

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,8
        JUMP_TO_COMMON    groupIndex,8

        GET_CURRENT_IP
        LOAD_DATA_ADDRESS groupIndex,9
        JUMP_TO_COMMON    groupIndex,9
endm

THUNKS_PAGE_BLOCK macro
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
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool, TKS0

NAMED_READWRITE_DATA_SECTION ThunkData0, ".tkd0"

NAMED_LEAF_ENTRY ThunkPool1, TKS1, ".tks1"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool1, TKS1

NAMED_READWRITE_DATA_SECTION ThunkData1, ".tkd1"

NAMED_LEAF_ENTRY ThunkPool2, TKS2, ".tks2"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool2, TKS2

NAMED_READWRITE_DATA_SECTION ThunkData2, ".tkd2"

NAMED_LEAF_ENTRY ThunkPool3, TKS3, ".tks3"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool3, TKS3

NAMED_READWRITE_DATA_SECTION ThunkData3, ".tkd3"

NAMED_LEAF_ENTRY ThunkPool4, TKS4, ".tks4"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool4, TKS4

NAMED_READWRITE_DATA_SECTION ThunkData4, ".tkd4"

NAMED_LEAF_ENTRY ThunkPool5, TKS5, ".tks5"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool5, TKS5

NAMED_READWRITE_DATA_SECTION ThunkData5, ".tkd5"

NAMED_LEAF_ENTRY ThunkPool6, TKS6, ".tks6"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool6, TKS6

NAMED_READWRITE_DATA_SECTION ThunkData6, ".tkd6"

NAMED_LEAF_ENTRY ThunkPool7, TKS7, ".tks7"
    THUNKS_PAGE_BLOCK
LEAF_END ThunkPool7, TKS7

NAMED_READWRITE_DATA_SECTION ThunkData7, ".tkd7"


;;
;; IntPtr RhpGetThunksBase()
;;
FASTCALL_FUNC RhpGetThunksBase, 0
        ;; Return the address of the first thunk pool to the caller (this is really the base address)
        lea     eax, [ThunkPool]
        ret
FASTCALL_ENDFUNC



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;; General Helpers ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;
;; int RhpGetNumThunksPerBlock()
;;
FASTCALL_FUNC RhpGetNumThunksPerBlock, 0
        mov     eax, THUNK_POOL_NUM_THUNKS_PER_PAGE
        ret
FASTCALL_ENDFUNC

;;
;; int RhpGetThunkSize()
;;
FASTCALL_FUNC RhpGetThunkSize, 0
        mov     eax, THUNK_CODESIZE
        ret
FASTCALL_ENDFUNC

;;
;; int RhpGetNumThunkBlocksPerMapping()
;;
FASTCALL_FUNC RhpGetNumThunkBlocksPerMapping, 0
        mov     eax, 8
        ret
FASTCALL_ENDFUNC

;;
;; int RhpGetThunkBlockSize
;;
FASTCALL_FUNC RhpGetThunkBlockSize, 0
        mov     eax, PAGE_SIZE * 2
        ret
FASTCALL_ENDFUNC

;;
;; IntPtr RhpGetThunkDataBlockAddress(IntPtr thunkStubAddress)
;;
FASTCALL_FUNC RhpGetThunkDataBlockAddress, 4
        mov     eax, ecx
        mov     ecx, PAGE_SIZE - 1
        not     ecx
        and     eax, ecx
        add     eax, PAGE_SIZE
        ret
FASTCALL_ENDFUNC

;;
;; IntPtr RhpGetThunkStubsBlockAddress(IntPtr thunkDataAddress)
;;
FASTCALL_FUNC RhpGetThunkStubsBlockAddress, 4
        mov     eax, ecx
        mov     ecx, PAGE_SIZE - 1
        not     ecx
        and     eax, ecx
        sub     eax, PAGE_SIZE
        ret
FASTCALL_ENDFUNC


end
