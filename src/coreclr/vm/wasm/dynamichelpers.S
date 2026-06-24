// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"

.size   g_pClassWithSlotAndModule, 4
.size   g_pMethodWithSlotAndModule, 4


.functype       DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull (i32, i32, i32) -> (i32)
        .local i32
        block
        block
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
        i32.add
        i32.load        0                                                                       // Load the SecondDir indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SizeOffset
        i32.add
        i32.load        0                                                                       // Load the size of the actual GenericDictionary on the Class
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SlotOffset
        i32.lt_s
        br_if           0                                                                       // Branch to the logic to call the helper function if the size of the actual GenericDictionary on the Class is less than the SlotOffset
        local.get       3                                                                       // Load the SecondDir indirection
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        i32.add
        i32.load        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        br_if           1                                                                       // Branch to return if the result is not null
        end_block
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        i32.const       0
        i32.load        g_pClassWithSlotAndModule
        local.tee       2
        local.get       2
        i32.load        0
        call_indirect    __indirect_function_table, (i32, i32, i32, i32) -> (i32)
        local.set       3
        end_block
        local.get       3                                                                       // Return the result.
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull (i32, i32, i32) -> (i32)
        .local i32
        block
        block
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        local.tee       3                                                                       // Store the loaded GenericDictionary into local 3, but keep it on the stack as well
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SizeOffset
        i32.add
        i32.load        0                                                                       // Load the size of the actual GenericDictionary on the Method
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SlotOffset
        i32.lt_s
        br_if           0                                                                       // Branch to the logic to call the helper function if the size of the actual GenericDictionary on the Method is less than the SlotOffset
        local.get       3                                                                       // Load the GenericDictionary
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        i32.add
        i32.load        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        br_if           1                                                                       // Branch to return if the result is not null
        end_block
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        i32.const       0
        i32.load        g_pMethodWithSlotAndModule
        local.tee       2
        local.get       2
        i32.load        0
        call_indirect    __indirect_function_table, (i32, i32, i32, i32) -> (i32)
        local.set       3
        end_block
        local.get       3                                                                       // Return the result.
        return
        end_function
