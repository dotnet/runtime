;++
;
; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
;
;   Contains ARM architecture constants and assembly macros.
;
;--

;
; The ARM assembler uses a baroque syntax that is documented as part
; of the online Windows CE documentation.  The syntax derives from
; ARM's own assembler and was chosen to allow the migration of
; specific assembly code bases, namely ARM's floating point runtime.
; While this compatibility is no longer strictly necessary, the
; syntax lives on....
;
; Highlights:
;      * Assembler is white space sensitive.  Symbols are defined by putting
;        them in the first column
;      * The macro definition mechanism is very primitive
;
; To augment the assembler, assembly files are run through CPP (as they are
; on IA64).  This works well for constants but not structural components due
; to the white space sensitivity.
;
; For now, we use a mix of native assembler and CPP macros.
;

#undef TRUE
#undef FALSE

#define TRUE 1
#define FALSE 0

#include "kxarm64unw.h"

        ;
        ; Global variables
        ;

        ; Current function names and labels
        GBLS    __FuncStartLabel
        GBLS    __FuncEpilog1StartLabel
        GBLS    __FuncEpilog2StartLabel
        GBLS    __FuncEpilog3StartLabel
        GBLS    __FuncEpilog4StartLabel
        GBLS    __FuncXDataLabel
        GBLS    __FuncXDataPrologLabel
        GBLS    __FuncXDataEpilog1Label
        GBLS    __FuncXDataEpilog2Label
        GBLS    __FuncXDataEpilog3Label
        GBLS    __FuncXDataEpilog4Label
        GBLS    __FuncXDataEndLabel
        GBLS    __FuncEndLabel

        ; other globals relating to the current function
        GBLS    __FuncArea
        GBLS    __FuncExceptionHandler


        ;
        ; Helper macro: generate the various labels we will use internally
        ; for a function
        ;
        ; Output is placed in the various __Func*Label globals
        ;

        MACRO
        __DeriveFunctionLabels $FuncName

__FuncStartLabel        SETS "|$FuncName|"
__FuncEndLabel          SETS "|$FuncName._end|"
__FuncEpilog1StartLabel SETS "|$FuncName._epilog1_start|"
__FuncEpilog2StartLabel SETS "|$FuncName._epilog2_start|"
__FuncEpilog3StartLabel SETS "|$FuncName._epilog3_start|"
__FuncEpilog4StartLabel SETS "|$FuncName._epilog4_start|"
__FuncXDataLabel        SETS "|$FuncName._xdata|"
__FuncXDataPrologLabel  SETS "|$FuncName._xdata_prolog|"
__FuncXDataEpilog1Label SETS "|$FuncName._xdata_epilog1|"
__FuncXDataEpilog2Label SETS "|$FuncName._xdata_epilog2|"
__FuncXDataEpilog3Label SETS "|$FuncName._xdata_epilog3|"
__FuncXDataEpilog4Label SETS "|$FuncName._xdata_epilog4|"
__FuncXDataEndLabel     SETS "|$FuncName._xdata_end|"

        MEND


        ;
        ; Helper macro: create a global label for the given name,
        ; decorate it, and export it for external consumption.
        ;

        MACRO
        __ExportName $FuncName

        LCLS    Name
Name    SETS    "|$FuncName|"
        ALIGN   4
        EXPORT  $Name
$Name
        MEND

        MACRO
        __ExportProc $FuncName

        LCLS    Name
Name    SETS    "|$FuncName|"
        ALIGN   4
        EXPORT  $Name
$Name   PROC
        MEND


        ;
        ; Declare that all following code/data is to be put in the .text segment
        ;

        MACRO
        TEXTAREA
#if defined(_CONTROL_FLOW_GUARD)
        AREA    |.text|,ALIGN=4,CODE,READONLY
#else
        AREA    |.text|,ALIGN=2,CODE,READONLY
#endif

        MEND


        ;
        ; Declare that all following code/data is to be put in the .data segment
        ;

        MACRO
        DATAAREA
        AREA    |.data|,DATA
        MEND


        ;
        ; Declare that all following code/data is to be put in the .rdata segment
        ;

        MACRO
        RODATAAREA
        AREA    |.rdata|,DATA,READONLY
        MEND


        ;
        ; Macro for indicating the start of a nested function. Nested functions
        ; imply a prolog, epilog, and unwind codes.
        ;

        MACRO
        NESTED_ENTRY $FuncName, $AreaName, $ExceptHandler

        ; compute the function's labels
        __DeriveFunctionLabels $FuncName

        ; determine the area we will put the function into
__FuncArea   SETS    "|.text|"
        IF "$AreaName" != ""
__FuncArea   SETS    "$AreaName"
        ENDIF

        ; set up the exception handler itself
__FuncExceptionHandler SETS ""
        IF "$ExceptHandler" != ""
__FuncExceptionHandler SETS    "|$ExceptHandler|"
        ENDIF

        ; switch to the specified area
        AREA    $__FuncArea,CODE,READONLY

        ; export the function name
        __ExportProc $FuncName

        ; flush any pending literal pool stuff
        ROUT

        ; reset the state of the unwind code tracking
        __ResetUnwindState

        MEND


        ;
        ; Macro for indicating the end of a nested function. We generate the
        ; .pdata and .xdata records here as necessary.
        ;

        MACRO
        NESTED_END $FuncName

        ; mark the end of the function
$__FuncEndLabel
        LTORG
        ENDP

        ; generate .pdata
        AREA    |.pdata|,ALIGN=2,READONLY
        DCD     $__FuncStartLabel
        RELOC   2                                       ; make this relative to image base

        DCD     $__FuncXDataLabel
        RELOC   2                                       ; make this relative to image base

        ; generate .xdata
        __EmitUnwindXData

        ; back to the original area
        AREA    $__FuncArea,CODE,READONLY

        ; reset the labels
__FuncStartLabel SETS    ""
__FuncEndLabel  SETS    ""

        MEND


        ;
        ; Macro for indicating the start of a leaf function.
        ;

        MACRO
        LEAF_ENTRY $FuncName, $AreaName

        ; compute the function's labels
        __DeriveFunctionLabels $FuncName

        ; determine the area we will put the function into
__FuncArea   SETS    "|.text|"
        IF "$AreaName" != ""
__FuncArea   SETS    "$AreaName"
        ENDIF

        ; switch to the specified area
        AREA    $__FuncArea,CODE,READONLY

        ; export the function name
        __ExportProc $FuncName

        ; flush any pending literal pool stuff
        ROUT

        MEND


        ;
        ; Macro for indicating the end of a leaf function.
        ;

        MACRO
        LEAF_END $FuncName

        ; mark the end of the function
$__FuncEndLabel
        LTORG
        ENDP

        ; reset the labels
__FuncStartLabel SETS    ""
__FuncEndLabel  SETS    ""

        MEND


        ;
        ; Macro for indicating an alternate entry point into a function.
        ;

        MACRO
        ALTERNATE_ENTRY $FuncName

        ; export the entry point's name
        __ExportName $FuncName

        ; flush any pending literal pool stuff
        ROUT

        MEND


        ;
        ; Macro for getting the address of a data item.
        ;
        
        MACRO
        ADDROF $Reg, $Variable
        
        adrp    $Reg, $Variable                 ; get the page address first
        add     $Reg, $Reg, $Variable           ; add in the low bits
        
        MEND


        ;
        ; Macro for loading a 32-bit constant.
        ;
        
        MACRO
        MOVL32 $Reg, $Variable
        
        IF ((($Variable):SHR:16):AND:0xffff) == 0
        movz    $Reg, #$Variable
        ELIF ((($Variable):SHR:0):AND:0xffff) == 0
        movz    $Reg, #((($Variable):SHR:16):AND:0xffff), lsl #16
        ELSE
        movz    $Reg, #(($Variable):AND:0xffff)
        movk    $Reg, #((($Variable):SHR:16):AND:0xffff), lsl #16
        ENDIF
        
        MEND


#if defined(_CAPKERN)

        ;
        ; Icecap entrypoints
        ;

        IMPORT _CAP_Start_Profiling
        IMPORT _CAP_End_Profiling


        ;
        ; Macro to record a call record
        ;

        MACRO
        CAPSTART $arg1, $arg2

        push {r0,r1}
        ldr r0, =$arg1
        ldr r1, =$arg2
        bl _CAP_Start_Profiling
        pop {r0,r1}

        MEND


        ;
        ; Macro to record a return record
        ;

        MACRO
        CAPEND $arg1

        push {r0}
        sub sp, sp, #4
        ldr r0, =$arg1
        bl _CAP_End_Profiling
        add sp, sp, #4
        pop {r0}

        MEND

#else

        MACRO
        CAPSTART $arg1, $arg2
        MEND

        MACRO
        CAPEND $arg1
        MEND

#endif


        ;
        ; Macro to acquire a spin lock at address $Reg + $Offset. Clobbers {r0-r2}
        ;

; ARM64_WORKITEM : should we use acquire/release semantics instead of DMB?

        MACRO
        ACQUIRE_SPIN_LOCK $Reg, $Offset

        mov     x0, #1                                  ; we want to exchange with a 1
        dmb                                             ; memory barrier ahead of the loop
1
        ldxr    x1, [$Reg, $Offset]                     ; load the new value
        stxr    x2, x0, [$Reg, $Offset]                 ; attempt to store the 1
        cbnz    x2, %B1                                 ; did we succeed before someone else did?
        cbz     x1, %F3                                 ; was the lock previously owned? if not, we're done
        yield                                           ; yield execution
        b       %B1                                     ; and try again
3
        dmb

        MEND


        ;
        ; Macro to release a spin lock at address $Reg + $Offset.
        ;

; ARM64_WORKITEM : should we use acquire/release semantics instead of DMB?

        MACRO
        RELEASE_SPIN_LOCK $Reg, $Offset

        dmb
        str     xzr, [$Reg, $Offset]                    ; store 0

        MEND


        ;
        ; Macro to increment a 64-bit statistic.
        ;

        MACRO
        INCREMENT_STAT $AddrReg, $Temp1, $Temp2

1       ldxr    $Temp1, [$AddrReg]                      ; load current value
        add     $Temp1, $Temp1, #1                      ; increment
        stxr    $Temp2, $Temp1, [$AddrReg]              ; attempt to store
        cbnz    $Temp2, %B1                             ; loop until it works?

        MEND


        ;
        ; Macros to enable/disable interrupts.
        ;
        
        MACRO
        ENABLE_INTERRUPTS
        msr     DAIFClr, #2                             ; enable interrupts
        MEND

        MACRO
        DISABLE_INTERRUPTS
        msr     DAIFSet, #2                             ; disable interrupts
        MEND


        ;
        ; Macros to read/write the current IRQL
        ;
        ; N.B. These macros do not do hardware and software IRQL processing.
        ;

        MACRO
        GET_IRQL $Irql
        mrs     $Irql, #ARM64_TPIDRRO_EL0               ; read IRQL
        MEND

        MACRO
        RAISE_IRQL $Reg, $NewIrql

#if DBG

        GET_IRQL $Reg                                   ; get old IRQL
        cmp      $Reg, #$NewIrql                        ; check if old IRQL is lower or same
        bls      %F1                                    ; if LS, valid transition

        EMIT_BREAKPOINT                                 ; break
1
#endif

        mov     $Reg, #$NewIrql
        msr     #ARM64_TPIDRRO_EL0, $Reg                ; update IRQL
        MEND


        ;
        ; Macros to output special undefined opcodes that indicate breakpoints
        ; and debug services.
        ;

        MACRO
        EMIT_BREAKPOINT
        brk     #ARM64_BREAKPOINT
        MEND


        MACRO
        EMIT_DEBUG_SERVICE
        brk     #ARM64_DEBUG_SERVICE
        MEND

        MACRO
        FASTFAIL $FastFailCode
        mov     x0, $FastFailCode
        brk     #ARM64_FASTFAIL
        MEND

        ;
        ; Macro to align a Control Flow Guard valid call target.
        ;

        MACRO
        CFG_ALIGN
#if defined(_CONTROL_FLOW_GUARD)
        ALIGN 16
#endif
        MEND
