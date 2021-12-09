;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

;;-----------------------------------------------------------------------------
;; This helper routine enregisters the appropriate arguments and makes the
;; actual call.
;;-----------------------------------------------------------------------------
;;void RhCallDescrWorker(CallDescrData * pCallDescrData);
        NESTED_ENTRY RhCallDescrWorker
        PROLOG_PUSH         {r4,r5,r7,lr}
        PROLOG_STACK_SAVE   r7

        mov     r5,r0 ; save pCallDescrData in r5

        ldr     r1, [r5,#OFFSETOF__CallDescrData__numStackSlots]
        cbz     r1, Ldonestack

        ;; Add frame padding to ensure frame size is a multiple of 8 (a requirement of the OS ABI).
        ;; We push four registers (above) and numStackSlots arguments (below). If this comes to an odd number
        ;; of slots we must pad with another. This simplifies to "if the low bit of numStackSlots is set,
        ;; extend the stack another four bytes".
        lsls    r2, r1, #2
        and     r3, r2, #4
        sub     sp, sp, r3

        ;; This loop copies numStackSlots words
        ;; from [pSrcEnd-4,pSrcEnd-8,...] to [sp-4,sp-8,...]
        ldr     r0, [r5,#OFFSETOF__CallDescrData__pSrc]
        add     r0,r0,r2
Lstackloop
        ldr     r2, [r0,#-4]!
        str     r2, [sp,#-4]!
        subs    r1, r1, #1
        bne     Lstackloop
Ldonestack

        ;; If FP arguments are supplied in registers (r3 != NULL) then initialize all of them from the pointer
        ;; given in r3. Do not use "it" since it faults in floating point even when the instruction is not executed.
        ldr     r3, [r5,#OFFSETOF__CallDescrData__pFloatArgumentRegisters]
        cbz     r3, LNoFloatingPoint
        vldm    r3, {s0-s15}
LNoFloatingPoint

        ;; Copy [pArgumentRegisters, ..., pArgumentRegisters + 12]
        ;; into r0, ..., r3

        ldr     r4, [r5,#OFFSETOF__CallDescrData__pArgumentRegisters]
        ldm     r4, {r0-r3}

        CHECK_STACK_ALIGNMENT

        ;; call pTarget
        ;; Note that remoting expect target in r4.
        ldr     r4, [r5,#OFFSETOF__CallDescrData__pTarget]
        blx     r4

        EXPORT_POINTER_TO_ADDRESS PointerToReturnFromCallDescrThunk

        ;; Symbol used to identify thunk call to managed function so the special
        ;; case unwinder can unwind through this function. Sadly we cannot directly
        ;; export this symbol right now because it confuses DIA unwinder to believe
        ;; it's the beginning of a new method, therefore we export the address
        ;; of an auxiliary variable holding the address instead.

        ldr     r3, [r5,#OFFSETOF__CallDescrData__fpReturnSize]

        ;; Save FP return value if appropriate
        cbz     r3, LFloatingPointReturnDone

        ;; Float return case
        ;; Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r3, #4
        bne     LNoFloatReturn
        vmov    r0, s0
        b       LFloatingPointReturnDone
LNoFloatReturn

        ;; Double return case
        ;; Do not use "it" since it faults in floating point even when the instruction is not executed.
        cmp     r3, #8
        bne     LNoDoubleReturn
        vmov    r0, r1, s0, s1
        b       LFloatingPointReturnDone
LNoDoubleReturn
; Unlike desktop returnValue is a pointer to a return buffer, not the buffer itself
        ldr     r2, [r5, #OFFSETOF__CallDescrData__pReturnBuffer]

        cmp     r3, #16
        bne     LNoFloatHFAReturn
        vstm    r2, {s0-s3}
        b       LReturnDone
LNoFloatHFAReturn

        cmp     r3, #32
        bne     LNoDoubleHFAReturn
        vstm    r2, {d0-d3}
        b       LReturnDone
LNoDoubleHFAReturn

        EMIT_BREAKPOINT ; Unreachable

LFloatingPointReturnDone

; Unlike desktop returnValue is a pointer to a return buffer, not the buffer itself
        ldr     r5, [r5, #OFFSETOF__CallDescrData__pReturnBuffer]

        ;; Save return value into retbuf
        str     r0, [r5, #(0)]
        str     r1, [r5, #(4)]

LReturnDone

#ifdef _DEBUG
        ;; trash the floating point registers to ensure that the HFA return values
        ;; won't survive by accident
        vldm    sp, {d0-d3}
#endif

        EPILOG_STACK_RESTORE    r7
        EPILOG_POP              {r4,r5,r7,pc}

        NESTED_END RhCallDescrWorker

        END
