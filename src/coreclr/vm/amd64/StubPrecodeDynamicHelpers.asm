; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include <AsmMacros.inc>
include AsmConstants.inc

ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS 

SecretArg_Reg equ r10
FirstArg_Reg equ rcx
SecondArg_Reg equ rdx
SecondArg_DwordReg equ edx
ThirdArg_Reg equ r8
ThirdArg_DwordReg equ r8d
FourthArg_Reg equ r9

DATA_SLOT macro field
    exitm @CatStr(r10, < + OFFSETOF__DynamicHelperStubArgs__>, field)
endm

GENERIC_DICT_DATA_SLOT macro field
    exitm @CatStr(r10, < + OFFSETOF__GenericDictionaryDynamicHelperStubData__>, field)
endm

extern g_pClassWithSlotAndModule:QWORD
extern g_pMethodWithSlotAndModule:QWORD

LEAF_ENTRY DynamicHelper_CallHelper_1Arg, _TEXT
        mov    FirstArg_Reg, QWORD PTR [DATA_SLOT(Constant1)]
        mov    rax, QWORD PTR [DATA_SLOT(Helper)]
        TAILJMP_RAX
LEAF_END DynamicHelper_CallHelper_1Arg, _TEXT

LEAF_ENTRY DynamicHelper_CallHelper_AddSecondArg, _TEXT
        mov    SecondArg_Reg, QWORD PTR [DATA_SLOT(Constant1)]
        mov    rax, QWORD PTR [DATA_SLOT(Helper)]
        TAILJMP_RAX
LEAF_END DynamicHelper_CallHelper_AddSecondArg, _TEXT

LEAF_ENTRY DynamicHelper_CallHelper_2Arg, _TEXT
        mov    FirstArg_Reg, QWORD PTR [DATA_SLOT(Constant1)]
        mov    SecondArg_Reg, QWORD PTR [DATA_SLOT(Constant2)]
        mov    rax, QWORD PTR [DATA_SLOT(Helper)]
        TAILJMP_RAX
LEAF_END DynamicHelper_CallHelper_2Arg, _TEXT

LEAF_ENTRY DynamicHelper_CallHelper_ArgMove, _TEXT
        mov    SecondArg_Reg, FirstArg_Reg
        mov    FirstArg_Reg, QWORD PTR [DATA_SLOT(Constant1)]
        mov    rax, QWORD PTR [DATA_SLOT(Helper)]
        TAILJMP_RAX
LEAF_END DynamicHelper_CallHelper_ArgMove, _TEXT

LEAF_ENTRY DynamicHelper_Return, _TEXT
        ret
LEAF_END DynamicHelper_Return, _TEXT

LEAF_ENTRY DynamicHelper_ReturnConst, _TEXT
        mov    rax, SecretArg_Reg
        ret
LEAF_END DynamicHelper_ReturnConst, _TEXT

LEAF_ENTRY DynamicHelper_ReturnIndirConst, _TEXT
        mov    rax, QWORD PTR [SecretArg_Reg]
        ret
LEAF_END DynamicHelper_ReturnIndirConst, _TEXT

LEAF_ENTRY DynamicHelper_ReturnIndirConstWithOffset, _TEXT
        mov    rax, QWORD PTR [DATA_SLOT(Constant1)]
        mov    rax, QWORD PTR [rax]
        add    rax, QWORD PTR [DATA_SLOT(Constant2)]
        ret
LEAF_END DynamicHelper_ReturnIndirConstWithOffset, _TEXT

LEAF_ENTRY DynamicHelper_CallHelper_AddThirdArg, _TEXT
        mov    ThirdArg_Reg, QWORD PTR [DATA_SLOT(Constant1)]
        mov    rax, QWORD PTR [DATA_SLOT(Helper)]
        TAILJMP_RAX
LEAF_END DynamicHelper_CallHelper_AddThirdArg, _TEXT

LEAF_ENTRY DynamicHelper_CallHelper_AddThirdAndFourthArg, _TEXT
        mov    ThirdArg_Reg, QWORD PTR [DATA_SLOT(Constant1)]
        mov    FourthArg_Reg, QWORD PTR [DATA_SLOT(Constant2)]
        mov    rax, QWORD PTR [DATA_SLOT(Helper)]
        TAILJMP_RAX
LEAF_END DynamicHelper_CallHelper_AddThirdAndFourthArg, _TEXT

; Generic dictionaries can have 2 or 3 indirections  (5 indirs of 32bit size, and 2 8 byte quantities)  = 40 bytes
; If it has 2 its for a Method, and the first indirection is always offsetof(InstantiatiedMethodDesc, m_pPerInstInfo)
; If it has 3 its for a Class, and the first indirection is always MethodTable::GetOffsetOfPerInstInfo
; It can also have 0, 0, to just return the class type
; Test For Null Or Not (If not present, cannot have a size check)
; SizeCheck or not (Only needed if size > Some number)
;
; Also special case where we just return the TypeHandle or MethodDesc itself
; Should probably have special case for 1, 2, 3 generic arg of MethodDesc/MethodTable

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SecondIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; SizeCheck
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SizeOffset)]
        mov    ThirdArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SlotOffset)]
        cmp    qword ptr[rax + SecondArg_Reg], ThirdArg_Reg
        jle    DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull_HelperCall
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(LastIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; Null test
        test   rax, rax
        je     DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull_HelperCall
        ret
DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull_HelperCall:
        mov    SecondArg_Reg, QWORD PTR [GENERIC_DICT_DATA_SLOT(HandleArgs)]
        mov    rax, QWORD PTR [g_pClassWithSlotAndModule]
        TAILJMP_RAX
LEAF_END DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_TestForNull, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SecondIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(LastIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; Null test
        test   rax, rax
        je     DynamicHelper_GenericDictionaryLookup_Class_TestForNull_HelperCall
        ret
DynamicHelper_GenericDictionaryLookup_Class_TestForNull_HelperCall:
        mov    SecondArg_Reg, QWORD PTR [GENERIC_DICT_DATA_SLOT(HandleArgs)]
        mov    rax, QWORD PTR [g_pClassWithSlotAndModule]
        TAILJMP_RAX
LEAF_END DynamicHelper_GenericDictionaryLookup_Class_TestForNull, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SecondIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(LastIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Class, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; SizeCheck
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SizeOffset)]
        mov    ThirdArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(SlotOffset)]
        cmp    qword ptr[rax + SecondArg_Reg], ThirdArg_Reg
        jle    DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull_HelperCall
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(LastIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; Null test
        test   rax, rax
        je     DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull_HelperCall
        ret
DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull_HelperCall:
        mov    SecondArg_Reg, QWORD PTR [GENERIC_DICT_DATA_SLOT(HandleArgs)]
        mov    rax, QWORD PTR [g_pMethodWithSlotAndModule]
        TAILJMP_RAX
LEAF_END DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_TestForNull, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(LastIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ; Null test
        test   rax, rax
        je     DynamicHelper_GenericDictionaryLookup_Method_TestForNull_HelperCall
        ret
DynamicHelper_GenericDictionaryLookup_Method_TestForNull_HelperCall:
        mov    SecondArg_Reg, QWORD PTR [GENERIC_DICT_DATA_SLOT(HandleArgs)]
        mov    rax, QWORD PTR [g_pMethodWithSlotAndModule]
        TAILJMP_RAX
LEAF_END DynamicHelper_GenericDictionaryLookup_Method_TestForNull, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; Standard Indirection
        mov    SecondArg_DwordReg, DWORD PTR [GENERIC_DICT_DATA_SLOT(LastIndir)]
        mov    rax, QWORD PTR [SecondArg_Reg+rax]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Method, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_0, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_0, _TEXT


LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_1, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax + 8h]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_1, _TEXT


LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_2, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax + 10h]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_2, _TEXT


LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_3, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__MethodTable__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax + 18h]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_3, _TEXT

LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_0, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Method_0, _TEXT


LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_1, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax + 08h]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Method_1, _TEXT


LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_2, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax + 10h]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Method_2, _TEXT


LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_3, _TEXT
        ; First indirection
        mov    rax, QWORD PTR [FirstArg_Reg+OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
        ; Standard Indirection
        mov    rax, QWORD PTR [rax + 18h]
        ret
LEAF_END DynamicHelper_GenericDictionaryLookup_Method_3, _TEXT

endif ;; FEATURE_STUBPRECODE_DYNAMIC_HELPERS 
        end