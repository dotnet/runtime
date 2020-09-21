;++
;
; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
;
; Module:
;
;   kxarmunw.w
;
; Abstract:
;
;   Contains ARM unwind code helper macros
;
;   This file is not really useful on its own without the support from
;   kxarm.h.
;
;--

;   ARMSTUB: This file was copied from
;     //depot/fbl_core1_woa_dev/public/sdk/inc/kxarmunw.h
;   Remove once it is in SDK.

;
; The following macros are defined here:
;
;   PROLOG_PUSH {reglist}
;   PROLOG_VPUSH {reglist}
;   PROLOG_PUSH_TRAP_FRAME
;   PROLOG_PUSH_MACHINE_FRAME
;   PROLOG_PUSH_CONTEXT
;   PROLOG_DECLARE_PROLOG_HELPER
;   PROLOG_STACK_ALLOC <amount>
;   PROLOG_STACK_SAVE <reg>
;   PROLOG_NOP <operation>
;
;   EPILOG_NOP <operation>
;   EPILOG_STACK_RESTORE <reg>
;   EPILOG_STACK_FREE <amount>
;   EPILOG_VPOP {reglist}
;   EPILOG_POP {reglist}
;   EPILOG_BRANCH <target>
;   EPILOG_BRANCH_REG <target>
;   EPILOG_RETURN
;

        ;
        ; Global variables
        ;

        ; results from __ParseIntRegister[List], __ParseVfpRegister[List]
        GBLS __ParsedRegisterString
        GBLA __ParsedRegisterMask
        
        ; results from __ComputeCodes[...]
        GBLS __ComputedCodes
        
        ; input and result from __MinMaxRegFromMask
        GBLA __RegInputMask
        GBLA __MinRegNum
        GBLA __MaxRegNum
        
        ; global state and accumulators
        GBLS __PrologUnwindString
        GBLS __PrologLastLabel
        GBLA __EpilogUnwindCount
        GBLS __Epilog1UnwindString
        GBLS __Epilog2UnwindString
        GBLS __Epilog3UnwindString
        GBLS __Epilog4UnwindString
        GBLL __EpilogStartNotDefined
        GBLA __RunningIndex
        GBLS __RunningLabel


        ;
        ; Helper macro: compute minimum/maximum register indexes from a mask
        ;
        ; Input goes into __RegInputMask
        ; Output is placed in __MinRegNum and __MaxRegNum
        ;

        MACRO
        __MinMaxRegFromMask

        LCLA CurMask

CurMask SETA __RegInputMask
__MinRegNum SETA -1
__MaxRegNum SETA -1

        WHILE CurMask != 0
        IF ((CurMask:AND:1) != 0) && (__MinRegNum == -1)
__MinRegNum SETA __MaxRegNum + 1
        ENDIF
CurMask SETA CurMask:SHR:1
__MaxRegNum SETA __MaxRegNum + 1
        WEND
        
        MEND
        

        ;
        ; Helper macro: emit an opcode with a generated label
        ;
        ; Output: Name of label is in $__RunningLabel
        ;
        
        MACRO
        __EmitRunningLabelAndOpcode $O1,$O2,$O3,$O4,$O5,$O6

__RunningLabel SETS "|Temp.$__RunningIndex|"
__RunningIndex SETA __RunningIndex + 1

        IF "$O6" != ""
$__RunningLabel $O1,$O2,$O3,$O4,$O5,$O6
        ELIF "$O5" != ""
$__RunningLabel $O1,$O2,$O3,$O4,$O5
        ELIF "$O4" != ""
$__RunningLabel $O1,$O2,$O3,$O4
        ELIF "$O3" != ""
$__RunningLabel $O1,$O2,$O3
        ELIF "$O2" != ""
$__RunningLabel $O1,$O2
        ELIF "$O1" != ""
$__RunningLabel $O1
        ELSE
$__RunningLabel
        ENDIF

        MEND


        ;
        ; Helper macro: append unwind codes to the prolog string
        ;
        ; Input is in __ComputedCodes
        ;
        
        MACRO
        __AppendPrologCodes
        
__PrologUnwindString SETS "$__ComputedCodes,$__PrologUnwindString"

        MEND


        ;
        ; Helper macro: append unwind codes to the epilog string
        ;
        ; Input is in __ComputedCodes
        ;
        
        MACRO
        __AppendEpilogCodes

        IF __EpilogUnwindCount == 1
__Epilog1UnwindString SETS "$__Epilog1UnwindString,$__ComputedCodes"
        ELIF __EpilogUnwindCount == 2
__Epilog2UnwindString SETS "$__Epilog2UnwindString,$__ComputedCodes"
        ELIF __EpilogUnwindCount == 3
__Epilog3UnwindString SETS "$__Epilog3UnwindString,$__ComputedCodes"
        ELIF __EpilogUnwindCount == 4
__Epilog4UnwindString SETS "$__Epilog4UnwindString,$__ComputedCodes"
        ENDIF

        MEND


        ;
        ; Helper macro: detect prolog end
        ;

        MACRO
        __DeclarePrologEnd

__PrologLastLabel SETS "$__RunningLabel"

        MEND


        ;
        ; Helper macro: detect epilog start
        ;

        MACRO
        __DeclareEpilogStart

        IF __EpilogStartNotDefined
__EpilogStartNotDefined SETL {false}
__EpilogUnwindCount SETA __EpilogUnwindCount + 1
        IF __EpilogUnwindCount == 1
$__FuncEpilog1StartLabel
        ELIF __EpilogUnwindCount == 2
$__FuncEpilog2StartLabel
        ELIF __EpilogUnwindCount == 3
$__FuncEpilog3StartLabel
        ELIF __EpilogUnwindCount == 4
$__FuncEpilog4StartLabel
        ELSE
        INFO    1, "Too many epilogues!"
        ENDIF
        ENDIF

        MEND
        
        
        ;
        ; Helper macro: specify epilog end
        ;

        MACRO
        __DeclareEpilogEnd

__EpilogStartNotDefined SETL {true}

        MEND
        
        
        ;
        ; Convoluted macro to parse a parameter that should be an integer register
        ; or register range, and return the string and mask
        ;
        ; Output is placed in __ParsedRegisterString and __ParsedRegisterMask
        ;

        MACRO
        __ParseIntRegister $Text
        
        LCLS CurText
        LCLS LReg
        LCLA LRegNum
        LCLA LRegMask
        LCLS RReg
        LCLA RRegNum
        LCLA RRegMask

CurText SETS "$Text"
LReg    SETS ""
LRegMask SETA 0
RReg    SETS ""
RRegMask SETA 0

        ; start with everything empty
__ParsedRegisterString SETS ""
__ParsedRegisterMask SETA 0

        ; strip leading open brace
        IF :LEN:CurText >= 1 && CurText:LEFT:1 == "{"
CurText SETS CurText:RIGHT:(:LEN:CurText - 1)
        ENDIF

        ; strip trailing close brace
        IF :LEN:CurText >= 1 && CurText:RIGHT:1 == "}"
CurText SETS CurText:LEFT:(:LEN:CurText - 1)
        ENDIF
        
        ; parse into register pair if 5 or more characters
        IF (:LEN:CurText) >= 5

        IF (CurText:LEFT:3):RIGHT:1 == "-"
LReg    SETS CurText:LEFT:2
RReg    SETS CurText:RIGHT:(:LEN:CurText - 3)
        ENDIF
        
        IF (CurText:LEFT:4):RIGHT:1 == "-"
LReg    SETS CurText:LEFT:3
RReg    SETS CurText:RIGHT:(:LEN:CurText - 4)
        ENDIF

        ; otherwise, parse as a single register
        ELSE
LReg    SETS CurText
RReg    SETS ""
        ENDIF
        
        ; fail if the registers aren't integer registers
        IF LReg != "lr" && LReg != "sp" && LReg != "pc" && LReg:LEFT:1 != "r"
        MEXIT
        ENDIF

        ; determine register masks
LRegNum SETA :RCONST:$LReg
LRegMask SETA 1:SHL:LRegNum

        ; if no right register, assign the single register
        IF RReg == ""
__ParsedRegisterString SETS LReg
__ParsedRegisterMask SETA LRegMask

        ; otherwise, validate the right register and generate the range
        ELSE
        IF RReg != "lr" && RReg != "sp" && RReg != "pc" && RReg:LEFT:1 != "r"
        MEXIT
        ENDIF
RRegNum SETA :RCONST:$RReg
RRegMask SETA 1:SHL:RRegNum
__ParsedRegisterString SETS LReg:CC:"-":CC:RReg
__ParsedRegisterMask SETA (RRegMask + RRegMask - 1) - (LRegMask - 1)
        ENDIF

        MEND


        ;
        ; Macro to parse a list of integer registers into a string and a mask
        ;
        ; Output is placed in __ParsedRegisterString and __ParsedRegisterMask
        ;

        MACRO 
        __ParseIntRegisterList $Func,$R1,$R2,$R3,$R4,$R5
        
        LCLS    OverallString
        LCLA    OverallMask
        
        __ParseIntRegister $R1
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R1"
        ENDIF
OverallMask SETA __ParsedRegisterMask
OverallString SETS __ParsedRegisterString

        IF "$R2" != ""
        __ParseIntRegister $R2
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R2"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

        IF "$R3" != ""
        __ParseIntRegister $R3
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R3"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

        IF "$R4" != ""
        __ParseIntRegister $R4
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R4"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

        IF "$R5" != ""
        __ParseIntRegister $R5
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R5"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

__ParsedRegisterMask SETA OverallMask
__ParsedRegisterString SETS OverallString
        
        MEND


        ;
        ; Convoluted macro to parse a parameter that should be a VFP register
        ; or register range, and return the string and mask
        ;
        ; Output is placed in __ParsedRegisterString and __ParsedRegisterMask
        ;

        MACRO
        __ParseVfpRegister $Text
        
        LCLS CurText
        LCLS LReg
        LCLA LRegNum
        LCLA LRegMask
        LCLS RReg
        LCLA RRegNum
        LCLA RRegMask

CurText SETS "$Text"
LReg    SETS ""
LRegMask SETA 0
RReg    SETS ""
RRegMask SETA 0

        ; start with everything empty
__ParsedRegisterString SETS ""
__ParsedRegisterMask SETA 0

        ; strip leading open brace
        IF :LEN:CurText >= 1 && CurText:LEFT:1 == "{"
CurText SETS CurText:RIGHT:(:LEN:CurText - 1)
        ENDIF

        ; strip trailing close brace
        IF :LEN:CurText >= 1 && CurText:RIGHT:1 == "}"
CurText SETS CurText:LEFT:(:LEN:CurText - 1)
        ENDIF
        
        ; parse into register pair if 5 or more characters
        IF (:LEN:CurText) >= 5

        IF (CurText:LEFT:3):RIGHT:1 == "-"
LReg    SETS CurText:LEFT:2
RReg    SETS CurText:RIGHT:(:LEN:CurText - 3)
        ENDIF
        
        IF (CurText:LEFT:4):RIGHT:1 == "-"
LReg    SETS CurText:LEFT:3
RReg    SETS CurText:RIGHT:(:LEN:CurText - 4)
        ENDIF

        ; otherwise, parse as a single register
        ELSE
LReg    SETS CurText
RReg    SETS ""
        ENDIF
        
        ; fail if the registers aren't VFP registers
        IF LReg:LEFT:1 != "d"
        MEXIT
        ENDIF

        ; determine register masks
LReg    SETS LReg:RIGHT:(:LEN:LReg - 1)
LRegNum SETA $LReg
LRegMask SETA 1:SHL:LRegNum

        ; if no right register, assign the single register
        IF RReg == ""
__ParsedRegisterString SETS "d":CC:LReg
__ParsedRegisterMask SETA LRegMask

        ; otherwise, validate the right register and generate the range
        ELSE
        IF RReg:LEFT:1 != "d"
        MEXIT
        ENDIF
RReg    SETS RReg:RIGHT:(:LEN:RReg - 1)
RRegNum SETA $RReg
RRegMask SETA 1:SHL:RRegNum
__ParsedRegisterString SETS "d":CC:LReg:CC:"-d":CC:RReg
__ParsedRegisterMask SETA (RRegMask + RRegMask - 1) - (LRegMask - 1)
        ENDIF

        MEND


        ;
        ; Macro to parse a list of VFP registers into a string and a mask
        ;
        ; Output is placed in __ParsedRegisterString and __ParsedRegisterMask
        ;

        MACRO 
        __ParseVfpRegisterList $Func,$R1,$R2,$R3,$R4,$R5
        
        LCLS    OverallString
        LCLA    OverallMask
        
        __ParseVfpRegister $R1
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R1"
        ENDIF
OverallMask SETA __ParsedRegisterMask
OverallString SETS __ParsedRegisterString

        IF "$R2" != ""
        __ParseVfpRegister $R2
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R2"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

        IF "$R3" != ""
        __ParseVfpRegister $R3
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R3"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

        IF "$R4" != ""
        __ParseVfpRegister $R4
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R4"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

        IF "$R5" != ""
        __ParseVfpRegister $R5
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Func: $R5"
        ENDIF
OverallMask SETA OverallMask:OR:__ParsedRegisterMask
OverallString SETS OverallString:CC:",":CC:__ParsedRegisterString
        ENDIF

__ParsedRegisterMask SETA OverallMask
__ParsedRegisterString SETS OverallString
        
        MEND


        ;
        ; Compute unwind codes for a PUSH or POP operation
        ;
        ; Input is in __ParsedRegisterMask
        ; Output is placed in __ComputedCodes
        ;
        
        MACRO
        __ComputePushPopCodes $Name,$FreeMask
        
        LCLA    MaskMinusFree
        LCLA    ByteVal
        LCLA    ByteVal2
        LCLA    FreeVal
        LCLA    LrVal

        ; See if LR/PC was included in the mask
FreeVal SETA    0
LrVal   SETA    0
        IF (__ParsedRegisterMask:AND:$FreeMask) != 0
FreeVal SETA    1
        ENDIF
        IF (__ParsedRegisterMask:AND:0x4000) != 0
LrVal   SETA    1
        ENDIF

        ; Compute a mask without LR/PC
MaskMinusFree SETA __ParsedRegisterMask:AND:(:NOT:$FreeMask)

        ; Determine minimum/maximum registers
__RegInputMask SETA MaskMinusFree
        __MinMaxRegFromMask

        ; single byte, 16-bit push r4-r[4-7]
        IF ((MaskMinusFree:AND:0xff1f) == 0x0010) && (((MaskMinusFree + 0x10):AND:MaskMinusFree) == 0)
ByteVal SETA    0xd0 :OR: (FreeVal:SHL:2) :OR: (__MaxRegNum:AND:3)
__ComputedCodes SETS "0x":CC:((:STR:ByteVal):RIGHT:2)

        ; single byte, 16-bit push r4-r[8-11]
        ELIF ((MaskMinusFree:AND:0xf01f) == 0x0010) && (((MaskMinusFree + 0x10):AND:MaskMinusFree) == 0)
ByteVal SETA    0xd8 :OR: (FreeVal:SHL:2) :OR: (__MaxRegNum:AND:3)
__ComputedCodes SETS "0x":CC:((:STR:ByteVal):RIGHT:2)

        ; double byte, 16-bit push r0-r7 via bitmask
        ELIF ((MaskMinusFree:AND:0xff00) == 0x0000)
ByteVal SETA    0xec :OR: FreeVal
__ComputedCodes SETS "0x":CC:((:STR:ByteVal):RIGHT:2):CC:",0x":CC:((:STR:MaskMinusFree):RIGHT:2)

        ; double byte, 32-bit push r0-r15 via bitmask
        ELIF ((MaskMinusFree:AND:0xa000) == 0x0000)
ByteVal SETA    0x80 :OR: (FreeVal:SHL:5) :OR: ((MaskMinusFree:SHR:8):AND:0x1f)
ByteVal2 SETA   MaskMinusFree:AND:0xff
__ComputedCodes SETS "0x":CC:((:STR:ByteVal):RIGHT:2):CC:",0x":CC:((:STR:ByteVal2):RIGHT:2)

        ; unsupported case
        ELSE
        INFO    1, "Invalid register sequence specified in $Name"
        ENDIF

        MEND
        
        
        ;
        ; Compute unwind codes for a VPUSH or VPOP operation
        ;
        ; Input is in __ParsedRegisterMask
        ; Output is placed in __ComputedCodes
        ;
        
        MACRO
        __ComputeVpushVpopCodes $Name
        
        LCLA    ByteVal

        ; Determine minimum/maximum registers
__RegInputMask SETA __ParsedRegisterMask
        __MinMaxRegFromMask
        
        ; Only contiguous sequences are supported
        IF ((__ParsedRegisterMask + (1:SHL:__MinRegNum)):AND:__ParsedRegisterMask) != 0
        INFO    1, "Discontiguous register sequence specified in PROLOG_VPUSH"

        ; single byte, 32-bit vpush d8-d[8-15]
        ELIF (__MinRegNum == 8) && (__MaxRegNum <= 15)
ByteVal SETA    0xe0 :OR: (__MaxRegNum:AND:7)
__ComputedCodes SETS "0x":CC:((:STR:ByteVal):RIGHT:2)

        ; double byte, 32-bit vpush d0-d15 via start/end values
        ELIF (__MinRegNum >= 0) && (__MaxRegNum <= 15)
ByteVal SETA    ((__MinRegNum:AND:15):SHL:4) :OR: (__MaxRegNum:AND:15)
__ComputedCodes SETS "0xF5,0x":CC:((:STR:ByteVal):RIGHT:2)

        ; double byte, 32-bit vpush d16-d31 via start/end values
        ELIF (__MinRegNum >= 16) && (__MaxRegNum <= 31)
ByteVal SETA    ((__MinRegNum:AND:15):SHL:4) :OR: (__MaxRegNum:AND:15)
__ComputedCodes SETS "0xF6,0x":CC:((:STR:ByteVal):RIGHT:2)

        ; unsupported case
        ELSE
        INFO    1, "Invalid register sequence specified in $Name"
        ENDIF

        MEND
        
        
        ;
        ; Compute unwind codes for a stack alloc/dealloc operation
        ;
        ; Output is placed in __ComputedCodes
        ;

        MACRO
        __ComputeStackAllocCodes $Name, $Amount
        
        LCLA    BytesDiv4
        LCLA    BytesHigh
        LCLA    BytesLow
BytesDiv4 SETA  ($Amount) / 4

        ; single byte, 16-bit add sp, sp, #x
        IF BytesDiv4 <= 0x7f
__ComputedCodes SETS "0x":CC:((:STR:BytesDiv4):RIGHT:2)

        ; double byte, 32-bit addw sp, sp, #x
        ELIF BytesDiv4 <= 0x3ff
BytesHigh SETA  (BytesDiv4:SHR:8):OR:0xe8
BytesLow SETA   BytesDiv4:AND:0xff
__ComputedCodes SETS "0x":CC:((:STR:BytesHigh):RIGHT:2):CC:",0x":CC:((:STR:BytesLow):RIGHT:2)

        ; don't support anything bigger
        ELSE
        INFO    1, "$Name too large for unwind code encoding"
        ENDIF
        
        MEND
        
        ;
        ; Compute unwind codes for a stack save/restore operation
        ;
        ; Output is placed in __ComputedCodes
        ;

        MACRO
        __ComputeStackSaveRestoreCodes $Name, $Register
        
        LCLA    ByteVal
        
        __ParseIntRegister $Register

        ; error if no valid register
        IF __ParsedRegisterMask == 0
        INFO    1, "Invalid register in $Name: $Register"

        ; determine min/max registers in mask
        ELSE
__RegInputMask SETA __ParsedRegisterMask
        __MinMaxRegFromMask

        ; error if we were passed a range
        IF __MinRegNum != __MaxRegNum
        INFO    1, "Register range not allowed in $Name: $Register"

        ; single byte, 16-bit mov rN, sp
        ELSE
ByteVal SETA    0xc0 :OR: __MinRegNum
__ComputedCodes SETS "0x":CC:((:STR:ByteVal):RIGHT:2)
        ENDIF
        ENDIF
        
        MEND

        
        ;
        ; Macro for an integer register PUSH operation in a prologue
        ;

        MACRO 
        PROLOG_PUSH $R1,$R2,$R3,$R4,$R5
        
        __ParseIntRegisterList "PROLOG_PUSH",$R1,$R2,$R3,$R4,$R5
        __EmitRunningLabelAndOpcode push {$__ParsedRegisterString}
        __DeclarePrologEnd
        
        __ComputePushPopCodes "PROLOG_PUSH",0x4000
        __AppendPrologCodes

        MEND


        ;
        ; Macro for a floating-point register VPUSH operation in a prologue
        ;

        MACRO 
        PROLOG_VPUSH $R1,$R2,$R3,$R4,$R5
        
        LCLA    ByteVal

        __ParseVfpRegisterList "PROLOG_VPUSH",$R1,$R2,$R3,$R4,$R5
        __EmitRunningLabelAndOpcode vpush {$__ParsedRegisterString}
        __DeclarePrologEnd

        __ComputeVpushVpopCodes "PROLOG_VPUSH"
        __AppendPrologCodes

        MEND
        

        ;
        ; Macro for indicating a trap frame lives above us
        ;

        MACRO
        PROLOG_PUSH_TRAP_FRAME
        
__ComputedCodes SETS "0xEE,0x00"
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for indicating a trap frame lives above us
        ;

        MACRO
        PROLOG_PUSH_MACHINE_FRAME
        
__ComputedCodes SETS "0xEE,0x01"
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for indicating a trap frame lives above us
        ;

        MACRO
        PROLOG_PUSH_CONTEXT
        
__ComputedCodes SETS "0xEE,0x02"
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for indicating a function is a prolog helper,
        ; and if unwound, should back up the LR to before an
        ; assumed 32-bit branch (used for __chkstk)
        ;

        MACRO
        PROLOG_DECLARE_PROLOG_HELPER
        
__ComputedCodes SETS "0xEE,0x03"
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for allocating space on the stack in the prolog
        ;

        MACRO
        PROLOG_STACK_ALLOC $Amount
        
        __EmitRunningLabelAndOpcode sub sp, sp, #$Amount
        __DeclarePrologEnd

        __ComputeStackAllocCodes "PROLOG_STACK_ALLOC", $Amount
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for saving the stack pointer in another register
        ;

        MACRO
        PROLOG_STACK_SAVE $Register
        
        __EmitRunningLabelAndOpcode mov $Register, sp
        __DeclarePrologEnd

        __ComputeStackSaveRestoreCodes "PROLOG_STACK_SAVE", $Register
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for including an arbitrary operation in the prolog
        ;

        MACRO
        PROLOG_NOP $O1,$O2,$O3,$O4
        
        __EmitRunningLabelAndOpcode $O1,$O2,$O3,$O4
        __DeclarePrologEnd

        IF ?$__RunningLabel == 2
__ComputedCodes SETS "0xFB"
        ELSE
__ComputedCodes SETS "0xFC"
        ENDIF
        __AppendPrologCodes
        
        MEND
        
        
        ;
        ; Macro for including an arbitrary operation in the epilog
        ;

        MACRO
        EPILOG_NOP $O1,$O2,$O3,$O4
        
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode $O1,$O2,$O3,$O4

        IF ?$__RunningLabel == 2
__ComputedCodes SETS "0xFB"
        ELSE
__ComputedCodes SETS "0xFC"
        ENDIF
        __AppendEpilogCodes
        
        MEND
        
        
        ;
        ; Macro for saving the stack pointer in another register
        ;

        MACRO
        EPILOG_STACK_RESTORE $Register
        
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode mov sp, $Register
        
        __ComputeStackSaveRestoreCodes "EPILOG_STACK_RESTORE", $Register
        __AppendEpilogCodes
        
        MEND
        
        
        ;
        ; Macro for deallocating space on the stack in the prolog
        ;

        MACRO
        EPILOG_STACK_FREE $Amount
        
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode add sp, sp, #$Amount
        
        __ComputeStackAllocCodes "EPILOG_STACK_FREE", $Amount
        __AppendEpilogCodes
        
        MEND
        
        
        ;
        ; Macro for an integer register POP operation in an epilogue
        ;

        MACRO 
        EPILOG_POP $R1,$R2,$R3,$R4,$R5

        __ParseIntRegisterList "EPILOG_POP",$R1,$R2,$R3,$R4,$R5
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode pop {$__ParsedRegisterString}

        __ComputePushPopCodes "EPILOG_POP",0x8000
        __AppendEpilogCodes
        
        IF (__ParsedRegisterMask:AND:0x8000) != 0
        __DeclareEpilogEnd
        ENDIF
        
        MEND


        ;
        ; Macro for a floating-point register VPOP operation in a prologue
        ;

        MACRO 
        EPILOG_VPOP $R1,$R2,$R3,$R4,$R5
        
        LCLA    ByteVal

        __ParseVfpRegisterList "EPILOG_VPOP",$R1,$R2,$R3,$R4,$R5
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode vpop {$__ParsedRegisterString}
        
        __ComputeVpushVpopCodes "EPILOG_VPOP"
        __AppendEpilogCodes
        
        MEND
        
        
        ;
        ; Macro for a b <target> end to the epilog (tail-call)
        ;

        MACRO
        EPILOG_BRANCH $Target
        
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode b $Target

        IF ?$__RunningLabel == 2
__ComputedCodes SETS "0xFD"
        ELSE
__ComputedCodes SETS "0xFE"
        ENDIF
        __AppendEpilogCodes

        __DeclareEpilogEnd

        MEND
        

        ;
        ; Macro for a bx register-style return in the epilog
        ;

        MACRO
        EPILOG_BRANCH_REG $Register
        
        __DeclareEpilogStart
        __EmitRunningLabelAndOpcode bx $Register

__ComputedCodes SETS "0xFD"
        __AppendEpilogCodes

        __DeclareEpilogEnd

        MEND
        

        ;
        ; Macro for a bx lr-style return in the epilog
        ;

        MACRO
        EPILOG_RETURN
        EPILOG_BRANCH_REG lr
        MEND
        

        ;
        ; Macro to reset the internal uwninding states
        ;

        MACRO
        __ResetUnwindState
__PrologUnwindString SETS ""
__EpilogUnwindCount SETA 0
__Epilog1UnwindString SETS ""
__Epilog2UnwindString SETS ""
__Epilog3UnwindString SETS ""
__Epilog4UnwindString SETS ""
__EpilogStartNotDefined SETL {true}
        MEND
        

        ;
        ; Macro to emit the xdata for unwinding
        ;

        MACRO
        __EmitUnwindXData
        
        LCLA    XBit
        LCLA    FBit

        ; determine 
FBit    SETA    0
        IF "$__PrologUnwindString" == ""
FBit    SETA    1:SHL:22
        ENDIF

XBit    SETA    0
        IF "$__FuncExceptionHandler" != ""
XBit    SETA    1:SHL:20
        ENDIF
        
        ;
        ; Append terminators where necessary
        ;
        IF __EpilogUnwindCount >= 1
__Epilog1UnwindString SETS __Epilog1UnwindString:RIGHT:(:LEN:__Epilog1UnwindString - 1)
        IF (:LEN:__Epilog1UnwindString) >= 5
        IF __Epilog1UnwindString:RIGHT:4 < "0xFD"
__Epilog1UnwindString SETS __Epilog1UnwindString:CC:",0xFF"
        ENDIF
        ENDIF
        ENDIF
        
        IF __EpilogUnwindCount >= 2
__Epilog2UnwindString SETS __Epilog2UnwindString:RIGHT:(:LEN:__Epilog2UnwindString - 1)
        IF (:LEN:__Epilog2UnwindString) >= 5
        IF __Epilog2UnwindString:RIGHT:4 < "0xFD"
__Epilog2UnwindString SETS __Epilog2UnwindString:CC:",0xFF"
        ENDIF
        ENDIF
        ENDIF
        
        IF __EpilogUnwindCount >= 3
__Epilog3UnwindString SETS __Epilog3UnwindString:RIGHT:(:LEN:__Epilog3UnwindString - 1)
        IF (:LEN:__Epilog3UnwindString) >= 5
        IF __Epilog3UnwindString:RIGHT:4 < "0xFD"
__Epilog3UnwindString SETS __Epilog3UnwindString:CC:",0xFF"
        ENDIF
        ENDIF
        ENDIF
        
        IF __EpilogUnwindCount >= 4
__Epilog4UnwindString SETS __Epilog4UnwindString:RIGHT:(:LEN:__Epilog4UnwindString - 1)
        IF (:LEN:__Epilog4UnwindString) >= 5
        IF __Epilog4UnwindString:RIGHT:4 < "0xFD"
__Epilog4UnwindString SETS __Epilog4UnwindString:CC:",0xFF"
        ENDIF
        ENDIF
        ENDIF

        ; optimize out the prolog string if it matches
        IF (:LEN:__Epilog1UnwindString) >= 6
        IF __Epilog1UnwindString:LEFT:(:LEN:__Epilog1UnwindString - 4) == __PrologUnwindString
__PrologUnwindString SETS ""
        ENDIF
        ENDIF

        IF "$__PrologUnwindString" != ""
__PrologUnwindString SETS __PrologUnwindString:CC:"0xFF"
        ENDIF

        ;
        ; Switch to the .xdata section, aligned to a DWORD
        ;
        AREA    |.xdata|,ALIGN=2,READONLY
        ALIGN   4

        ; declare the xdata header with unwind code size, epilog count, 
        ; exception bit, and function length
$__FuncXDataLabel
        DCD     ((($__FuncXDataEndLabel - $__FuncXDataPrologLabel)/4):SHL:28) :OR: (__EpilogUnwindCount:SHL:23) :OR: FBit :OR: XBit :OR: (($__FuncEndLabel - $__FuncStartLabel)/2)
        
        ; if we have an epilogue, output a single scope record
        IF __EpilogUnwindCount >= 1
        DCD     (($__FuncXDataEpilog1Label - $__FuncXDataPrologLabel):SHL:24) :OR: (14:SHL:20) :OR: (($__FuncEpilog1StartLabel - $__FuncStartLabel)/2)
        ENDIF
        IF __EpilogUnwindCount >= 2
        DCD     (($__FuncXDataEpilog2Label - $__FuncXDataPrologLabel):SHL:24) :OR: (14:SHL:20) :OR: (($__FuncEpilog2StartLabel - $__FuncStartLabel)/2)
        ENDIF
        IF __EpilogUnwindCount >= 3
        DCD     (($__FuncXDataEpilog3Label - $__FuncXDataPrologLabel):SHL:24) :OR: (14:SHL:20) :OR: (($__FuncEpilog3StartLabel - $__FuncStartLabel)/2)
        ENDIF
        IF __EpilogUnwindCount >= 4
        DCD     (($__FuncXDataEpilog4Label - $__FuncXDataPrologLabel):SHL:24) :OR: (14:SHL:20) :OR: (($__FuncEpilog4StartLabel - $__FuncStartLabel)/2)
        ENDIF
        
        ; output the prolog unwind string
$__FuncXDataPrologLabel
        IF "$__PrologUnwindString" != ""
        DCB     $__PrologUnwindString
        ENDIF
        
        ; if we have an epilogue, output the epilog unwind codes
        IF __EpilogUnwindCount >= 1
$__FuncXDataEpilog1Label
        DCB     $__Epilog1UnwindString,0xff
        ENDIF
        IF __EpilogUnwindCount >= 2
$__FuncXDataEpilog2Label
        DCB     $__Epilog2UnwindString,0xff
        ENDIF
        IF __EpilogUnwindCount >= 3
$__FuncXDataEpilog3Label
        DCB     $__Epilog3UnwindString,0xff
        ENDIF
        IF __EpilogUnwindCount >= 4
$__FuncXDataEpilog4Label
        DCB     $__Epilog4UnwindString,0xff
        ENDIF

        ALIGN   4
$__FuncXDataEndLabel

        ; output the exception handler information
        IF "$__FuncExceptionHandler" != ""
        DCD     $__FuncExceptionHandler
        RELOC   2                                       ; make this relative to image base
        ENDIF

        ; switch back to the original area
        AREA    $__FuncArea,CODE,READONLY

        MEND
