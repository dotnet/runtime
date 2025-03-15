// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

#ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS 

#define SecretArg_Reg x12
#define FirstArg_Reg x0
#define SecondArg_Reg x1
#define SecondArg_DwordReg w1
#define ThirdArg_Reg x2
#define ThirdArg_DwordReg w2
#define FourthArg_Reg x3

#define DATA_SLOT(field) [x12, #OFFSETOF__DynamicHelperStubArgs__ ## field]
#define GENERIC_DICT_DATA_SLOT(field)  [x12, #OFFSETOF__GenericDictionaryDynamicHelperStubData__ ## field]

        LEAF_ENTRY DynamicHelper_CallHelper_1Arg, _TEXT
                ldr    FirstArg_Reg, DATA_SLOT(Constant1)
                ldr    x12, DATA_SLOT(Helper)
                EPILOG_BRANCH_REG x12
        LEAF_END DynamicHelper_CallHelper_1Arg, _TEXT

        LEAF_ENTRY DynamicHelper_CallHelper_AddSecondArg, _TEXT
                ldr    SecondArg_Reg, DATA_SLOT(Constant1)
                ldr    x12, DATA_SLOT(Helper)
                EPILOG_BRANCH_REG x12
        LEAF_END DynamicHelper_CallHelper_AddSecondArg, _TEXT

        LEAF_ENTRY DynamicHelper_CallHelper_2Arg, _TEXT
                ldr    FirstArg_Reg, DATA_SLOT(Constant1)
                ldr    SecondArg_Reg, DATA_SLOT(Constant2)
                ldr    x12, DATA_SLOT(Helper)
                EPILOG_BRANCH_REG x12
        LEAF_END DynamicHelper_CallHelper_2Arg, _TEXT

        LEAF_ENTRY DynamicHelper_CallHelper_ArgMove, _TEXT
                mov    SecondArg_Reg, FirstArg_Reg
                ldr    FirstArg_Reg, DATA_SLOT(Constant1)
                ldr    x12, DATA_SLOT(Helper)
                EPILOG_BRANCH_REG x12
        LEAF_END DynamicHelper_CallHelper_ArgMove, _TEXT

        LEAF_ENTRY DynamicHelper_Return, _TEXT
                ret    lr
        LEAF_END DynamicHelper_Return, _TEXT

        LEAF_ENTRY DynamicHelper_ReturnConst, _TEXT
                mov    FirstArg_Reg, SecretArg_Reg
                ret    lr
        LEAF_END DynamicHelper_ReturnConst, _TEXT

        LEAF_ENTRY DynamicHelper_ReturnIndirConst, _TEXT
                ldr    FirstArg_Reg, [SecretArg_Reg, #0]
                ret    lr
        LEAF_END DynamicHelper_ReturnIndirConst, _TEXT

        LEAF_ENTRY DynamicHelper_ReturnIndirConstWithOffset, _TEXT
                ldr    FirstArg_Reg, DATA_SLOT(Constant1)
                ldr    FirstArg_Reg, [FirstArg_Reg]
                ldr    SecondArg_Reg, DATA_SLOT(Constant2)
                add    FirstArg_Reg, FirstArg_Reg, SecondArg_Reg
                ret    lr
        LEAF_END DynamicHelper_ReturnIndirConstWithOffset, _TEXT

        LEAF_ENTRY DynamicHelper_CallHelper_AddThirdArg, _TEXT
                ldr    ThirdArg_Reg, DATA_SLOT(Constant1)
                ldr    x12, DATA_SLOT(Helper)
                EPILOG_BRANCH_REG x12
        LEAF_END DynamicHelper_CallHelper_AddThirdArg, _TEXT

        LEAF_ENTRY DynamicHelper_CallHelper_AddThirdAndFourthArg, _TEXT
                ldr    ThirdArg_Reg, DATA_SLOT(Constant1)
                ldr    FourthArg_Reg, DATA_SLOT(Constant2)
                ldr    x12, DATA_SLOT(Helper)
                EPILOG_BRANCH_REG x12
        LEAF_END DynamicHelper_CallHelper_AddThirdAndFourthArg, _TEXT

        // Generic dictionaries can have 2 or 3 indirections  (5 indirs of 32bit size, and 2 8 byte quantities)  = 40 bytes
        // If it has 2 its for a Method, and the first indirection is always offsetof(InstantiatiedMethodDesc, m_pPerInstInfo)
        // If it has 3 its for a Class, and the first indirection is always MethodTable::GetOffsetOfPerInstInfo
        // It can also have 0, 0, to just return the class type
        // Test For Null Or Not (If not present, cannot have a size check)
        // SizeCheck or not (Only needed if size > Some number)
        //
        // Also special case where we just return the TypeHandle or MethodDesc itself
        // Should probably have special case for 1, 2, 3 generic arg of MethodDesc/MethodTable

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull, _TEXT
                // Save Generic Context
                mov    x4, FirstArg_Reg
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(SecondIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // SizeCheck
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(SizeOffset)
                ldr    ThirdArg_DwordReg, GENERIC_DICT_DATA_SLOT(SlotOffset)
                ldr    FourthArg_Reg, [FirstArg_Reg, SecondArg_Reg]
                cmp    FourthArg_Reg, ThirdArg_Reg
                b.ls   LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull_HelperCall)
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(LastIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // Null test
                cbz    FirstArg_Reg, LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull_HelperCall)
                ret    lr
LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull_HelperCall):
                mov    FirstArg_Reg, x4
                ldr    SecondArg_Reg, GENERIC_DICT_DATA_SLOT(HandleArgs)
                PREPARE_EXTERNAL_VAR g_pClassWithSlotAndModule, x3
                ldr     x3, [x3]
                EPILOG_BRANCH_REG x3
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_TestForNull, _TEXT
                // Save Generic Context
                mov    x4, FirstArg_Reg
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(SecondIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(LastIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // Null test
                cbz    FirstArg_Reg, LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Class_TestForNull_HelperCall)
                ret    lr
LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Class_TestForNull_HelperCall):
                mov    FirstArg_Reg, x4
                ldr    SecondArg_Reg, GENERIC_DICT_DATA_SLOT(HandleArgs)
                PREPARE_EXTERNAL_VAR g_pClassWithSlotAndModule, x3
                ldr     x3, [x3]
                EPILOG_BRANCH_REG x3
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class_TestForNull, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(SecondIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(LastIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull, _TEXT
                // Save Generic Context
                mov    x4, FirstArg_Reg
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // SizeCheck
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(SizeOffset)
                ldr    ThirdArg_DwordReg, GENERIC_DICT_DATA_SLOT(SlotOffset)
                ldr    FourthArg_Reg, [FirstArg_Reg, SecondArg_Reg]
                cmp    FourthArg_Reg, ThirdArg_Reg
                b.ls   LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull_HelperCall)
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(LastIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // Null test
                cbz    FirstArg_Reg, LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull_HelperCall)
                ret    lr
LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull_HelperCall):
                mov    FirstArg_Reg, x4
                ldr    SecondArg_Reg, GENERIC_DICT_DATA_SLOT(HandleArgs)
                PREPARE_EXTERNAL_VAR g_pMethodWithSlotAndModule, x3
                ldr     x3, [x3]
                EPILOG_BRANCH_REG x3
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_TestForNull, _TEXT
                // Save Generic Context
                mov    x4, FirstArg_Reg
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(LastIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                // Null test
                cbz    FirstArg_Reg, LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Method_TestForNull_HelperCall)
                ret    lr
LOCAL_LABEL(DynamicHelper_GenericDictionaryLookup_Method_TestForNull_HelperCall):
                mov    FirstArg_Reg, x4
                ldr    SecondArg_Reg, GENERIC_DICT_DATA_SLOT(HandleArgs)
                PREPARE_EXTERNAL_VAR g_pMethodWithSlotAndModule, x3
                ldr     x3, [x3]
                EPILOG_BRANCH_REG x3
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method_TestForNull, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // Standard Indirection
                ldr    SecondArg_DwordReg, GENERIC_DICT_DATA_SLOT(LastIndir)
                ldr    FirstArg_Reg, [SecondArg_Reg, FirstArg_Reg]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_0, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_0, _TEXT


        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_1, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #0x8]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_1, _TEXT


        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_2, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #0x10]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_2, _TEXT


        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Class_0_3, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__MethodTable__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #0x18]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Class_0_3, _TEXT

        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_0, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method_0, _TEXT


        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_1, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #0x8]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method_1, _TEXT


        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_2, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #0x10]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method_2, _TEXT


        LEAF_ENTRY DynamicHelper_GenericDictionaryLookup_Method_3, _TEXT
                // First indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo]
                // Standard Indirection
                ldr    FirstArg_Reg, [FirstArg_Reg, #0x18]
                ret    lr
        LEAF_END DynamicHelper_GenericDictionaryLookup_Method_3, _TEXT

#endif //// FEATURE_STUBPRECODE_DYNAMIC_HELPERS 
