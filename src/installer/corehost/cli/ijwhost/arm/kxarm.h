;++
;
; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
;

;
; Module:
;
;   kxarm.w
;
; Abstract:
;
;   Contains ARM architecture constants and assembly macros.
;
;--

;   ARMSTUB: This file was copied from
;     //depot/fbl_core1_woa_dev/public/sdk/inc/kxarm.h
;   Remove once it is in SDK.

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

#define THUMB_BREAKPOINT                0xDEFE
#define THUMB_DEBUG_SERVICE             0xDEFD

#include "kxarmunw.h"


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
        
        
        ;
        ; Declare that all following code/data is to be put in the .text segment
        ;

        MACRO
        TEXTAREA
        AREA    |.text|,ALIGN=2,CODE,READONLY
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
        __ExportName $FuncName

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

        ; generate .pdata
        AREA    |.pdata|,ALIGN=2,READONLY
        DCD	    $__FuncStartLabel
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
        __ExportName $FuncName

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
        ; Macro to record a call record
        ;

        MACRO
        CAPSTART $arg1, $arg2
        
        ; _ARM_WORKITEM_ -- implement me

        MEND


        ;
        ; Macro to record a return record
        ;

        MACRO
        CAPEND $arg1
        
        ; _ARM_WORKITEM_ -- implement me

        MEND


        ;
        ; Macro to determine if d16-d31 are available for use. Result goes into $ResultReg.
        ;

        MACRO
        VFP_32REGS_AVAILABLE $ResultReg

#ifdef NTOS_KERNEL_RUNTIME
        ldr     $ResultReg, =USER_SHARED_DATA + UsProcessorFeatures + PF_ARM_VFP_32_REGISTERS_AVAILABLE           ; either use kernel mode base address
#else
        ldr     $ResultReg, =MM_SHARED_USER_DATA_VA + UsProcessorFeatures + PF_ARM_VFP_32_REGISTERS_AVAILABLE     ; or user-mode
#endif   
        ldrb    $ResultReg, [$ResultReg]

        MEND


        ;
        ; Macro to acquire a spin lock at address $Reg + $Offset. Clobbers {r0-r2}
        ;

        MACRO
        ACQUIRE_SPIN_LOCK $Reg, $Offset

        movs    r0, #1                                  ; we want to exchange with a 1
        dmb                                             ; memory barrier ahead of the loop
1
        ldrex   r1, [$Reg, $Offset]                     ; load the new value
        strex   r2, r0, [$Reg, $Offset]                 ; attempt to store the 1
        cmp     r2, #1                                  ; did we succeed before someone else did?
        beq     %B1                                     ; if not, try again
        cmp     r1, #0                                  ; was the lock previously owned?
        beq     %F3                                     ; if not, we're done
2
        yield                                           ; yield execution
        b       %B1                                     ; and try again
3
        dmb

        MEND


        ;
        ; Macro to release a spin lock at address $Reg + $Offset. If $ZeroReg is specified,
        ; that register is presumed to contain 0; otherwise, r0 is clobbered and used.
        ;
        
        MACRO
        RELEASE_SPIN_LOCK $Reg, $Offset, $ZeroReg

        LCLS    Zero
Zero    SETS    "$ZeroReg"
        IF (Zero == "")
Zero    SETS    "r0"
        movs    r0, #0                                  ; need a 0 value to store
        ENDIF
        str     $Zero, [$Reg, $Offset]                  ; store it
        
        MEND
        

        ;
        ; Macro to restore the interrupt enable state to what it was in the SPSR
        ; held by the $SpsrReg parameter.
        ;

        MACRO
        RESTORE_INTERRUPT_STATE $SpsrReg

        tst     $SpsrReg, #CPSRC_INT|CPSRC_FIQ          ; were interrupts enabled previously?
        bne     %F1                                     ; if not, skip
        cpsie   if                                      ; enable interrupts
1
        MEND


        ;
        ; Macros to read/write coprocessor registers. These macros are preferred over
        ; raw mrc/mcr because they put the register parameter first and strip the 
        ; prefixes which allow them to use the same C preprocessor macros as the C
        ; code.
        ;

        MACRO
        CP_READ $rd, $coproc, $op1, $crn, $crm, $op2
        mrc     p$coproc, $op1, $rd, c$crn, c$crm, $op2 ; just shuffle params and add prefixes
        MEND


        MACRO
        CP_WRITE $rd, $coproc, $op1, $crn, $crm, $op2
        mcr     p$coproc, $op1, $rd, c$crn, c$crm, $op2 ; just shuffle params and add prefixes
        MEND


        ;
        ; Macros to read/write the TEB register
        ;
        
        MACRO
        TEB_READ $Reg
        CP_READ $Reg, CP15_CR13_THPR_USRRW              ; read from user r/w coprocessor register
        MEND

        
        MACRO
        TEB_WRITE $Reg
        CP_WRITE $Reg, CP15_CR13_THPR_USRRW             ; write to user r/w coprocessor register
        isb                                             ; force a synchronization
        MEND


        ;
        ; Macros to read/write the current thread register
        ;
        
        MACRO
        CURTHREAD_READ $Reg
        CP_READ $Reg, CP15_CR13_THPR_USRRO              ; read from user r/o coprocessor register
        MEND

        
        MACRO
        CURTHREAD_WRITE $Reg
        CP_WRITE $Reg, CP15_CR13_THPR_USRRO             ; write to user r/o coprocessor register
        isb                                             ; force a synchronization
        MEND


        ;
        ; Macros to read/write the PCR register
        ;
        
        MACRO
        PCR_READ $Reg
        CP_READ $Reg, CP15_CR13_THPR_SVCRW              ; read from svc r/w coprocessor register
        MEND
        
        
        MACRO
        PCR_WRITE $Reg
        CP_WRITE $Reg, CP15_CR13_THPR_SVCRW             ; write to svc r/w coprocessor register
        isb                                             ; for a synchronization
        MEND


        ;
        ; Macros to output special undefined opcodes that indicate breakpoints
        ; and debug services.
        ;

        MACRO
        EMIT_BREAKPOINT
        DCW     THUMB_BREAKPOINT                        ; undefined per ARM ARM
        MEND


        MACRO
        EMIT_DEBUG_SERVICE
        DCW     THUMB_DEBUG_SERVICE                     ; undefined per ARM ARM
        MEND


;
; Define move from DBGDSCR register macro
;
;   This macro retrieves the value from cp14 DBGDSCR register. This macro
;   hides the interface between ARM and cp14 (extended v/s memmapped cp14)
;
; Arguments:
;
;   Rx - Supplies register that coprocessor register value will be placed in.
;
; Implicit arguments:
;
;   None.
;
        MACRO
        DBGDSCR_READ $rd
        ldr   $rd, =KiExtendedCP14
        ldrb  $rd, [$rd]
        cmp   $rd, #FALSE
        beq   %F8
        CP_READ $rd, CP14_CR0_DBGDSCR
        b     %F9
8
        PCR_READ $rd
        ldr   $rd, [$rd, #PcHalReserved]
        ldr   $rd, [$rd, #MEMMAP_DBGDSCR_EXT_NUM * 4]
9
        MEND

;
;   This macro enabled monitor mode bit in cp14 DBGDSCR register. It also
;   hides the interface between ARM and cp14 (extended v/s memmapped cp14)
;
; Arguments:
;
;   Rx - Supplies scratch register
;
;   update - "enable" to enable monitor mode
;
; Implicit arguments:
;
;   None.
;
        MACRO
        DBGDSCR_UPDATE $rd, $update
        LCLS Update
Update SETS "$update"

        ldr   $rd, =KiExtendedCP14
        ldrb  $rd, [$rd]
        cmp   $rd, #FALSE
        beq   %F8
        CP_READ $rd, CP14_CR0_DBGDSCR

     IF Update == "enable"
        orr   $rd, $rd, #DBGDSCR_MON_EN_BIT
     ELSE
        bic   $rd, $rd, #DBGDSCR_MON_EN_BIT
     ENDIF

        CP_WRITE $rd, CP14_CR0_DBGDSCR
        b     %F9
8
        PCR_READ $rd
        ldr   $rd, [$rd, #PcHalReserved]
        ldr   $rd, [$rd, #MEMMAP_DBGDSCR_EXT_NUM * 4]

     IF Update == "enable"
        orr   $rd, $rd, #DBGDSCR_MON_EN_BIT
     ELSE
        bic   $rd, $rd, #DBGDSCR_MON_EN_BIT
     ENDIF

        str   $rd, [$rd, #MEMMAP_DBGDSCR_EXT_NUM * 4]
9
        MEND


        ;
        ; Macro to generate an exception frame; this is intended to
        ; be used within the prolog of a function.
        ;

        MACRO
        GENERATE_EXCEPTION_FRAME
        PROLOG_PUSH         {r4-r11, lr}                ; save non-volatile registers
        PROLOG_STACK_ALLOC  ExR4                        ; allocate remainder of exception frame
        MEND


        ;
        ; Macro to restore from an exception frame; this is intended to
        ; be used within the epilog of a function.
        ;

        MACRO
        RESTORE_EXCEPTION_FRAME
        EPILOG_STACK_FREE    ExR4                       ; adjust SP to point to non-volatile registers
        EPILOG_POP           {r4-r11, lr}               ; restore non-volatile registers
        MEND


        ;
        ; Macro to save the floating-point exception state. Designed to be
        ; called after GENERATE_EXCEPTION_FRAME. Clobbers {r4-r5}.
        ;

        MACRO
        SAVE_FP_EXCEPTION_STATE
        
        vmrs    r4, fpexc                               ; fetch FPEXC
        tst     r4, #ARM_FPEXC_GLOBAL_ENABLE_VFP        ; are we enabled?
        beq     %F1                                     ; if not, skip it
        add     r5, sp, #ExD                            ; point r5 to D8 in the exception frame
        vstm    r5!, {d8-d15}                           ; save the lower 8 registers
        PCR_READ r4                                     ; get a pointer to the PCR
        ldr     r4, [r4, #PcFeatureBits]                ; get the feature bits
        tst     r4, #KF_VFP_32REG                       ; do we save 32 registers?
        beq     %F1                                     ; if not, skip over the last store
        vstm    r5, {d16-d31}                           ; store the upper 16 registers as well
1
        MEND


        ;
        ; Macro to restore the floating-point exception state. Designed to be
        ; called before RESTORE_EXCEPTION_FRAME. Clobbers {r4-r5}.
        ;

        MACRO
        RESTORE_FP_EXCEPTION_STATE

        vmrs    r4, fpexc                               ; fetch FPEXC
        tst     r4, #ARM_FPEXC_GLOBAL_ENABLE_VFP        ; are we enabled?
        beq     %F1                                     ; if not, skip it
        add     r5, sp, #ExD                            ; point r5 to D8 in the exception frame
        vldm    r5!, {d8-d15}                           ; restore the lower 8 registers
        PCR_READ r4                                     ; get a pointer to the PCR
        ldr     r4, [r4, #PcFeatureBits]                ; get the feature bits
        tst     r4, #KF_VFP_32REG                       ; do we save 32 registers?
        beq     %F1                                     ; skip over the last load
        vldm    r5, {d16-d31}                           ; load the upper 16 registers as well
1
        MEND


        ;
        ; Macro to generate a trap frame on the supervisor mode stack which
        ; is the current thread's kernel stack.
        ;
        ; The ARM architecture has no architecturally defined trap frame.
        ; However, the trap frame has been designed to make use of the SRS/RFE
        ; instructions as well as ARM calling convention that uses r0-r3, and 
        ; r12 as volatile registers.
        ;
        ; Arguments:
        ;
        ;   $TrapType - value to be stored into the trap frame's 
        ;       ExceptionActive field
        ;
        ;   $Environ - execution environment; can be one of:
        ;
        ;       Boot:     implicitly assumes that this macro is invoked in bootloader
        ;       Kernel:   implicitly assumes we are coming from kernel mode
        ;       User:     implicitly assumes we are coming from user mode
        ;       Volatile: could be coming from either kernel or user mode
        ;
        ;   $PcOffset - amount to subtract from PC to adjust for offset
        ;       generated by the processor
        ;
        ;
        ;   $CheckSS - Set to "Yes" to perform a check for hardware single-step setup
        ;
        ;   $ModeValue - the mode to switch to; by default this is SVC mode
        ;

        MACRO
        GENERATE_SVC_TRAP_FRAME $TrapType, $Environ, $PcOffset, $CheckSS, $ModeValue
        
        ; create a local string with the environment value, and verify it
        LCLS Environment
        LCLS CheckSingleStep
Environment SETS "$Environ"
CheckSingleStep SETS "$CheckSS"
        ASSERT (Environment == "Boot" || Environment == "Kernel" || Environment == "Volatile" || Environment == "User")

        ; determine the target mode (default to SVC)
        LCLS Mode
Mode    SETS    "0x13"                                  ; mode 0x13 is CPSRM_SVC
        IF "$ModeValue" != ""
Mode    SETS    "$ModeValue"
        ENDIF

        ; push the PC and CPSR to the SVC stack and switch to SVC mode
        PROLOG_PUSH_TRAP_FRAME                          ; inform the unwinder we have a trap frame
        srsfd   #$Mode!                                 ; Save PC, CPSR to target stack

        IF CheckSingleStep == "Yes"
            ;
            ; This is a prefetch-abort. Check if it corresponds to a single-step event.
            ;
                POST_SINGLESTEP_FIXUP
        ENDIF

        cpsid   if, #$Mode                              ; Switch to target mode, interrupts off
        
        ; save r0-r3, then recover our original SP in r2 and set r3 equal to the trap type
        push    {r0-r3}                                 ; save r2-r3
        add     r3, sp, #24                             ; recover original SP in r3 (saved 4 regs plus 2 from SRS)
        movs    r0, #$TrapType                          ; set r0 to the exception type with a 0 in the high byte

        ; align the stack if necessary (coming from user mode we can assume it is aligned)
        IF (Environment == "Boot" || Environment == "Kernel" || Environment == "Volatile")

        tst     r3, #4                                  ; see if we were aligned
        beq     %F1                                     ; if we're aligned, skip ahead
        pop     {r0-r3}                                 ; restore r0-r3
        push    {r0-r4}                                 ; push them back, with an extra r4 to realign things
        add     r3, sp, #28                             ; re-recover original SP (same as above, plus 4)
        ldr     r0, =($TrapType + 0x400)                ; set r0 to the exception type with a 4 in the high byte
1
        ENDIF

        ; finish building the trap frame
        sub     sp, sp, #TrR0                           ; reserve rest of trap frame
        str     r12, [sp, #TrR12]                       ; save r12
        strh    r0, [sp, #TrExceptionActive]            ; set the exception type and stack adjust (in neighboring bytes)
        ldr     r0, [r3, #-4]                           ; load CPSR of previous mode (relative to original stack in r3)
        ldr     r1, [r3, #-8]                           ; load original PC (relative to original stack in r3)
        mov     r2, lr                                  ; get return address in r2

        IF $PcOffset > 1
        subs    r1, r1, #($PcOffset - 1)                ; adjust PC for offset, plus 1 to set the low bit
        ELSE
        adds    r1, r1, #(1 - $PcOffset)                ; adjust PC for offset, plus 1 to set the low bit
        ENDIF

        str     r0, [sp, #TrCpsr]                       ; store CPSR to aligned location
        str     r1, [sp, #TrPc]                         ; store modified PC to aligned location
    
        ; verify that we are still in thumb mode
        IF (Environment == "Volatile" || Environment == "User")

        tst     r0, #CPSRC_THUMB                        ; check CPSR for thumb mode
        beq.w   KiDetectedArmMode                       ; if not set, handle it
 
        ENDIF


        ; save the FP state if the VFP is live
        IF (Environment != "Boot")

        vmrs    r1, fpexc                               ; fetch FPEXC
        tst     r1, #ARM_FPEXC_GLOBAL_ENABLE_VFP        ; are we enabled?
        beq     %F2                                     ; if not, skip it
        add     r1, sp, #TrD0                           ; point r3 to D0 in the trap frame
        vstm    r1, {d0-d7}                             ; save the registers
        vmrs    r1, fpscr                               ; load floating point control/status
        str     r1, [sp, #TrFpscr]                      ; and store it
2
        ENDIF
        
        ; determine which path to follow below
        IF (Environment == "Volatile")

        tst     r0, #0xf                                ; did we come from user mode?
        bne     %F3                                     ; if not, skip this next bit

        ENDIF
    
        ; user-mode path
        IF (Environment == "Volatile" || Environment == "User")

        ; save the debug register state as well, if necessary
        CURTHREAD_READ r1                               ; get current thread address
        ldrb    r0, [r1, #ThDebugActive]                ; get debug active flag
        cmp     r0, #FALSE                              ; test if debug/cblk enabled
        SAVE_DEBUG_REGISTERS                            ; clobbers volatile regs

        ; switch to SYS mode to get the user mode SP and LR
        cps     #CPSRM_SYS                              ; go to SYS mode to get user state
        mov     r3, sp                                  ; get user mode SP
        mov     r2, lr                                  ; get user mode LR
        cps     #$Mode                                  ; back to target mode
3
        ENDIF

        ; common path: store the original SP and LR
        str     r3, [sp, #TrSp]                         ; store original SP to the trap frame
        str     r2, [sp, #TrLr]                         ; and original LR

        ; _ARM_WORKITEM_ - remove me once we understand the user mode exception issues
        ; record the trap frame for debugging
#if DBG
        IF (Environment == "Volatile" || Environment == "User")
        mov     r0, sp
        bl      KeRecordTrapFrame
        ENDIF
#endif
        MEND


        ;
        ; Macro to restore the volatile state, and if necessary, restore the
        ; user debug state, deallocate the trap frame, and return from the trap.
        ;
        ; Arguments:
        ;
        ;   $Environ - Determines what state is restored and what tests are made. Valid
        ;       values are:
        ;
        ;           Boot - restore state for a service executed from bootloader
        ;           Volatile - restore state for a trap or interrupt.
        ;           KernelService - restore state for a service executed from kernel mode.
        ;           UserService - restore state for a service executed from user mode.
        ;
        ;
        ;   $CheckSS - Set to "Yes" to perform a check for hardware single-step setup
        ;
        ;       Note that both KernelService and UserService presume the return value
        ;       in in r0 upon entry.
        ;

        MACRO
$rst    RESTORE_TRAP_STATE $Environ, $CheckSS

        ; create a local string with the environment value, and verify it
        LCLS Environment
        LCLS CheckSingleStep
Environment SETS "$Environ"
CheckSingleStep SETS "$CheckSS"
        ASSERT (Environment == "Boot" || Environment == "Volatile" || Environment == "KernelService" || Environment == "UserService")

        clrex                                           ; clear interlocked state

        IF (Environment == "KernelService")

        ; simple case: return from a kernel mode service
        ; note that r0 is presumed to hold the return value
        ldr     lr, [sp, #TrPc]                         ; restore target PC
        cpsie   if                                      ; re-enable interrupts
        EPILOG_STACK_FREE KTRAP_FRAME_LENGTH            ; deallocate stack
        EPILOG_RETURN                                   ; return
        MEXIT
        
        ENDIF

        cpsid   if                                      ; disable interrupts
    
        ; check for kernel versus user mode return
        IF (Environment == "Volatile")
    
        ldr     r1, [sp, #TrCpsr]                       ; get target CPSR
        tst     r1, #0xf                                ; was it user?
        beq     %F4                                     ; if so, skip past the kernel restore
        
        ; reload volatile FP registers ... carefully
        vmrs    r1, fpexc                               ; fetch FPEXC
        tst     r1, #ARM_FPEXC_GLOBAL_ENABLE_VFP        ; are we enabled?
        beq     %F3                                     ; if not, skip it
        add     r3, sp, #TrD0                           ; point to D0 in the trap frame
        vldm    r3, {d0-d7}                             ; load the registers
        ldr     r0, [sp, #TrFpscr]                      ; load floating point control/status
        vmsr    fpscr, r0                               ; and write it back
3
        ENDIF

        IF (Environment == "Boot" || Environment == "Volatile")
        
        ; return to kernel mode from trap/exception
        ldr     r1, [sp, #TrPc]                         ; read Cpsr, Pc, StackAdjust
        ldr     r2, [sp, #TrCpsr]                       ;
        ldrb    r3, [sp, #TrStackAdjust]                ;
        bic     r1, r1, #1                              ; clear the PC's low bit
        cmp     r3, #0                                  ; compare against 0 for final restore
        add     r3, r3, sp                              ; readjust the stack
        str     r1, [r3, #TrPc]                         ; write Cpsr, Pc back at correct offset
        str     r2, [r3, #TrCpsr]                       ;
        ldr     lr, [sp, #TrLr]                         ; restore LR

        IF CheckSingleStep == "Yes"
            PRE_SINGLESTEP_SETUP                        ; preserves flags
        ENDIF

        ; do not add anything here (see PreSingleStepSetup for side-effects)

        ldr     r12, [sp, #TrR12]                       ; restore r12
        add     sp, sp, #TrR0                           ; prepare SP
        pop     {r0-r3}                                 ; restore volatile registers
        addne   sp, sp, #4                              ; unalign stack if needed
        rfe     sp!                                     ; restore from exception
4
        ENDIF

        ; beyond this point is all user-mode return
        IF (Environment == "Volatile" || Environment == "UserService")

        IF (Environment == "UserService")

        ; user service case: scrub user mode volatile registers upon return
        str     r0, [sp, #TrR0]                         ; save return value
        movs    r0, #0                                  ; scrub volatile integer registers in the trap frame
        str     r0, [sp, #TrR1]                         ;
        str     r0, [sp, #TrR2]                         ;
        str     r0, [sp, #TrR3]                         ;
        str     r0, [sp, #TrR12]                        ;

        ; scrub volatile FP regs
        movs    r1, #0                                  ; use r1:r0 as a 64-bit 0
        strd    r0, r1, [sp, #TrD0]                     ; scrub
        strd    r0, r1, [sp, #TrD1]                     ;
        strd    r0, r1, [sp, #TrD2]                     ;
        strd    r0, r1, [sp, #TrD3]                     ;
        strd    r0, r1, [sp, #TrD4]                     ;
        strd    r0, r1, [sp, #TrD5]                     ;
        strd    r0, r1, [sp, #TrD6]                     ;
        strd    r0, r1, [sp, #TrD7]                     ;

        ENDIF
    
        ; check for user-mode APCs
        CURTHREAD_READ r1                               ; get current thread address
        ldrb    r0, [r1, #ThApcState + AsUserApcPending]
        cmp     r0, #FALSE                              ; APC pending?
        beq     %F1                                     ; if eq, no user APC pending

        movs    r0, #APC_LEVEL                          ; get APC level
        bl      KfSetIrql                               ; set IRQL to APC level

        cpsie   if                                      ; allow interrupts
        bl      KiInitiateUserApc                       ; initiate APC execution
        cpsid   if                                      ; disable interrupts

        movs    r0, #PASSIVE_LEVEL                      ; get PASSIVE level
        movs    r1, #0
        bl      KfLowerIrql                             ; set IRQL to PASSIVE level

        CURTHREAD_READ r1                               ; re-get current thread address
1
        ; check if the thread is a Scheduled UMS Thread or profiling is active.
        ldr     r0, [r1, #ThLock]
        tst     r0, #(THREAD_FLAGS_CYCLE_PROFILING_LOCK)
        beq     %F2                                     ; if z, profiling and UMS are not enabled

        ldrb    r1, [r1, #ThThreadControlFlags]
        tst     r1, #THREAD_FLAGS_CYCLE_PROFILING       ; check for profiling
        beq     %F99                                    ; if z, profiling is not enabled
        bl      KiCopyCounters
99
; _ARM_WORKITEM_ -- UMS support

2
        ; reload volatile FP registers ... carefully
        vmrs    r0, fpexc                               ; fetch FPEXC
        tst     r0, #ARM_FPEXC_GLOBAL_ENABLE_VFP        ; are we enabled?
        beq     %F3                                     ; if not, skip it
        add     r3, sp, #TrD0                           ; point to D0 in the trap frame
        vldm    r3, {d0-d7}                             ; load the registers
        ldr     r0, [sp, #TrFpscr]                      ; load floating point control/status
        vmsr    fpscr, r0                               ; and write it back
3
        ;     
        ; restore user-mode debug regs if required and [en/dis]able h/w debugging
        ;
        RESTORE_DEBUG_REGISTERS

        ldr     r0, [sp, #TrPc]                         ; read PC
        bic     r0, r0, #1                              ; clear the PC's low bit
        str     r0, [sp, #TrPc]                         ; write it back

        mov     r0, sp
        cps     #CPSRM_SYS                              ; go to SYS mode to modify user state
        ldr     sp, [r0, #TrSp]                         ; restore user mode SP
        ldr     lr, [r0, #TrLr]                         ; restore user mode LR
        cps     #CPSRM_SVC                              ; back to SVC mode
        
        ldr     r12, [sp, #TrR12]                       ; restore r12
        add     sp, sp, #TrR0                           ; prepare SP
        pop     {r0-r3}                                 ; restore volatile registers
        rfe     sp!                                     ; restore from exception

        ENDIF

        MEND
