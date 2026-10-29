;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

;; Windows ARM64 assembler macros shared between CoreCLR and NativeAOT.

;; -----------------------------------------------------------------------------
;; Macro used to assign an alternate name to a symbol containing characters normally disallowed in a symbol
;; name (e.g. C++ decorated names).
    MACRO
      SETALIAS   $name, $symbol
        GBLS    $name
$name   SETS    "|$symbol|"
    MEND

;; -----------------------------------------------------------------------------
;;
;; Macro to get a pointer to a threadlocal symbol for the currently executing thread
;; The caller is responsible for declaring $variable (e.g. with EXTERN).
;;

__tls_array     equ 0x58    ;; offsetof(TEB, ThreadLocalStoragePointer)

    MACRO
        INLINE_GET_TLS_VAR $destReg, $trashReg, $variable

        EXTERN _tls_index

        ;; The following macro variables are just some assembler magic to get the name of the 32-bit version
        ;; of $trashReg. It does it by string manipulation. Replaces something like x3 with w3.
        LCLS TrashRegister32Bit
TrashRegister32Bit SETS "$trashReg"
TrashRegister32Bit SETS "w":CC:("$TrashRegister32Bit":RIGHT:((:LEN:TrashRegister32Bit) - 1))

        adrp        $destReg, _tls_index
        ldr         $TrashRegister32Bit, [$destReg, _tls_index]
        ldr         $destReg, [xpr, #__tls_array]
        ldr         $destReg, [$destReg, $TrashRegister32Bit uxtw #3]
        add         $destReg, $destReg, #0, lsl #0xC
        RELOC       0xA, $variable                          ;; IMAGE_REL_ARM64_SECREL_HIGH12A
        add         $destReg, $destReg, #0, lsl #0
        RELOC       0x9, $variable                          ;; IMAGE_REL_ARM64_SECREL_LOW12A
    MEND
