// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is used to allow sharing of assembly code between NativeAOT and CoreCLR, which have different conventions about how to ensure that constants offsets are accessible

#ifdef TARGET_WINDOWS
#include "ksarm64.h"
#include "asmconstants.h"
#include "asmmacros.h"

    IMPORT g_lowest_address
    IMPORT g_highest_address
    IMPORT g_ephemeral_low
    IMPORT g_ephemeral_high
    IMPORT g_card_table

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    IMPORT g_card_bundle_table
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    IMPORT g_write_watch_table
#endif

    IMPORT RhpGcAlloc
    IMPORT RhExceptionHandling_FailedAllocation

;;-----------------------------------------------------------------------------
;; Macro for loading a 64-bit constant by a minimal number of instructions
;; Since the asssembles doesn't support 64 bit arithmetics in expressions,
;; the value is passed in as lo, hi pair.
    MACRO
        MOVL64 $Reg, $ConstantLo, $ConstantHi

        LCLS MovInstr
MovInstr SETS "movz"

         IF ((($ConstantHi):SHR:16):AND:0xffff) != 0
         $MovInstr $Reg, #((($Constant):SHR:16):AND:0xffff), lsl #48
MovInstr SETS "movk"
         ENDIF

         IF (($ConstantHi):AND:0xffff) != 0
         $MovInstr $Reg, #(($ConstantHi):AND:0xffff), lsl #32
MovInstr SETS "movk"
         ENDIF

        IF ((($ConstantLo):SHR:16):AND:0xffff) != 0
        $MovInstr $Reg, #((($ConstantLo):SHR:16):AND:0xffff), lsl #16
MovInstr SETS "movk"
        ENDIF

        $MovInstr $Reg, #(($ConstantLo):AND:0xffff)
    MEND

;;-----------------------------------------------------------------------------
;; Macro for loading a 64bit value of a global variable into a register
    MACRO
        PREPARE_EXTERNAL_VAR_INDIRECT $Name, $Reg

        adrp $Reg, $Name
        ldr  $Reg, [$Reg, $Name]
    MEND

;; ---------------------------------------------------------------------------- -
;; Macro for loading a 32bit value of a global variable into a register
    MACRO
        PREPARE_EXTERNAL_VAR_INDIRECT_W $Name, $RegNum

        adrp x$RegNum, $Name
        ldr  w$RegNum, [x$RegNum, $Name]
    MEND

;; ---------------------------------------------------------------------------- -
;;
;; Macro to add a memory barrier. Equal to __sync_synchronize().
;;

    MACRO
        InterlockedOperationBarrier

        dmb         ish
    MEND

#else
#include "asmconstants.h"
#include "unixasmmacros.inc"

.macro PREPARE_EXTERNAL_VAR_INDIRECT Name, HelperReg
#if defined(__APPLE__)
        adrp \HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  \HelperReg, [\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
        ldr  \HelperReg, [\HelperReg]
#else
        adrp \HelperReg, C_FUNC(\Name)
        ldr  \HelperReg, [\HelperReg, :lo12:C_FUNC(\Name)]
#endif
.endm

.macro PREPARE_EXTERNAL_VAR_INDIRECT_W Name, HelperReg
#if defined(__APPLE__)
        adrp x\HelperReg, C_FUNC(\Name)@GOTPAGE
        ldr  x\HelperReg, [x\HelperReg, C_FUNC(\Name)@GOTPAGEOFF]
        ldr  w\HelperReg, [x\HelperReg]
#else
        adrp x\HelperReg, C_FUNC(\Name)
        ldr  w\HelperReg, [x\HelperReg, :lo12:C_FUNC(\Name)]
#endif
.endm

#endif

