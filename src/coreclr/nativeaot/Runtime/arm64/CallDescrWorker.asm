;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

    TEXTAREA

;;-----------------------------------------------------------------------------
;; This helper routine enregisters the appropriate arguments and makes the
;; actual call.
;;
;;  INPUT: x0: pointer to CallDescrData struct
;;
;;-----------------------------------------------------------------------------
;;void RhCallDescrWorker(CallDescrData * pCallDescrData);
    NESTED_ENTRY RhCallDescrWorker

        PROLOG_SAVE_REG_PAIR   fp, lr, #-32!
        PROLOG_SAVE_REG_PAIR   x19, x20, #16

        ;; Save the value of SP before we start pushing any arguments
        mov     x20, sp

        mov     x19, x0 ; save pCallDescrData in x19

        ldr     w1, [x19, #OFFSETOF__CallDescrData__numStackSlots]
        cbz     w1, Ldonestack

        ;; Add frame padding to ensure frame size is a multiple of 16 (a requirement of the OS ABI).
        ;; We push two registers (above) and numStackSlots arguments (below). If this comes to an odd number
        ;; of slots we must pad with another. This simplifies to "if the low bit of numStackSlots is set,
        ;; extend the stack another eight bytes".
        ldr     x0, [x19, #OFFSETOF__CallDescrData__pSrc]
        add     x0, x0, x1 lsl #3               ; pSrcEnd=pSrc+8*numStackSlots
        ands    x2, x1, #1
        beq     Lstackloop

        ;; This loop copies numStackSlots words
        ;; from [pSrcEnd-8,pSrcEnd-16,...] to [sp-8,sp-16,...]

        ;; Pad and store one stack slot as number of slots are odd
        ldr     x4, [x0,#-8]!
        str     x4, [sp,#-16]!
        subs    x1, x1, #1
        beq     Ldonestack
Lstackloop
        ldp     x2, x4, [x0,#-16]!
        stp     x2, x4, [sp,#-16]!
        subs    x1, x1, #2
        bne     Lstackloop
Ldonestack

        ;; If FP arguments are supplied in registers (x9 != NULL) then initialize all of them from the pointer
        ;; given in x9.
        ldr     x9, [x19, #OFFSETOF__CallDescrData__pFloatArgumentRegisters]
        cbz     x9, LNoFloatingPoint
        ldp     d0, d1, [x9]
        ldp     d2, d3, [x9, #16]
        ldp     d4, d5, [x9, #32]
        ldp     d6, d7, [x9, #48]
LNoFloatingPoint

        ;; Copy [pArgumentRegisters, ..., pArgumentRegisters + 64]
        ;; into x0, ..., x7, x8

        ldr     x9, [x19, #OFFSETOF__CallDescrData__pArgumentRegisters]
        ldp     x0, x1, [x9]
        ldp     x2, x3, [x9, #16]
        ldp     x4, x5, [x9, #32]
        ldp     x6, x7, [x9, #48]
        ldr     x8, [x9, #64]

        ;; call pTarget
        ldr     x9, [x19, #OFFSETOF__CallDescrData__pTarget]
        blr     x9

    EXPORT_POINTER_TO_ADDRESS PointerToReturnFromCallDescrThunk

        ;; Symbol used to identify thunk call to managed function so the special
        ;; case unwinder can unwind through this function. Sadly we cannot directly
        ;; export this symbol right now because it confuses DIA unwinder to believe
        ;; it's the beginning of a new method, therefore we export the address
        ;; of an auxiliary variable holding the address instead.

        ldr     w3, [x19, #OFFSETOF__CallDescrData__fpReturnSize]

        ;; Unlike desktop returnValue is a pointer to a return buffer, not the buffer itself
        ldr     x19, [x19, #OFFSETOF__CallDescrData__pReturnBuffer]

        ;; Int return case
        cbz     w3, LIntReturn

        ;; Float return case
        cmp     w3, #4
        beq     LFloatOrDoubleReturn

        ;; Double return case
        cmp     w3, #8
        bne     LCheckHFAReturn

LFloatOrDoubleReturn
        str     d0, [x19]
        b       LReturnDone

LCheckHFAReturn
        cmp     w3, #16
        beq     LFloatOrDoubleHFAReturn
        cmp     w3, #32
        beq     LFloatOrDoubleHFAReturn
        b       LNoHFAReturn

LFloatOrDoubleHFAReturn
        ;;Single/Double HFAReturn  return case
        stp     d0, d1, [x19, #00]
        stp     d2, d3, [x19, #16]
        b       LReturnDone

LNoHFAReturn

        EMIT_BREAKPOINT ; Unreachable

LIntReturn
        ;; Save return value(s) into retbuf for int
        stp     x0, x1, [x19]

LReturnDone

#ifdef _DEBUG
        ;; Trash the floating point registers to ensure that the HFA return values
        ;; won't survive by accident
        ldp     d0, d1, [sp]
        ldp     d2, d3, [sp, #16]
#endif
        ;; Restore the value of SP
        mov     sp, x20

        EPILOG_RESTORE_REG_PAIR x19, x20, #16
        EPILOG_RESTORE_REG_PAIR fp, lr, #32!
        EPILOG_RETURN

    NESTED_END RhCallDescrWorker

    END
