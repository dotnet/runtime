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


.functype       DynamicHelper_GenericDictionaryLookup_Class_TestForNull (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Class_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Class_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Class_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_TestForNull (i32, i32, i32) -> (i32)
        .local i32
        block
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
        i32.add
        i32.load        0                                                                       // Load the SecondDir indirection
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        i32.add
        i32.load        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        br_if           0                                                                       // Branch to return if the result is not null
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


.functype       DynamicHelper_GenericDictionaryLookup_Class (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class
.globl          DynamicHelper_GenericDictionaryLookup_Class
.type           DynamicHelper_GenericDictionaryLookup_Class,@function
DynamicHelper_GenericDictionaryLookup_Class:
        .functype       DynamicHelper_GenericDictionaryLookup_Class (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
        i32.add
        i32.load        0                                                                       // Load the SecondDir indirection
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        i32.add
        i32.load        0                                                                       // Load the last indirection
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_0 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_0,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_0
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_0
.type           DynamicHelper_GenericDictionaryLookup_Class_0_0,@function
DynamicHelper_GenericDictionaryLookup_Class_0_0:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_0 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        i32.load        0                                                                       // Load the SecondDir indirection (offset 0)
        i32.load        0                                                                       // Load the last indirection (offset 0)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_1 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_1,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_1
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_1
.type           DynamicHelper_GenericDictionaryLookup_Class_0_1,@function
DynamicHelper_GenericDictionaryLookup_Class_0_1:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_1 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        i32.load        0                                                                       // Load the SecondDir indirection (offset 0)
        i32.load        0x4                                                                     // Load the last indirection (offset sizeof(TADDR) * 1)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_2 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_2,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_2
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_2
.type           DynamicHelper_GenericDictionaryLookup_Class_0_2,@function
DynamicHelper_GenericDictionaryLookup_Class_0_2:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_2 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        i32.load        0                                                                       // Load the SecondDir indirection (offset 0)
        i32.load        0x8                                                                     // Load the last indirection (offset sizeof(TADDR) * 2)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_3 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_3,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_3
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_3
.type           DynamicHelper_GenericDictionaryLookup_Class_0_3,@function
DynamicHelper_GenericDictionaryLookup_Class_0_3:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_3 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__MethodTable__m_pPerInstInfo
        i32.load        0                                                                       // Load the SecondDir indirection (offset 0)
        i32.load        0xC                                                                     // Load the last indirection (offset sizeof(TADDR) * 3)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_TestForNull (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Method_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Method_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Method_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_TestForNull (i32, i32, i32) -> (i32)
        .local i32
        block
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        i32.add
        i32.load        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        br_if           0                                                                       // Branch to return if the result is not null
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


.functype       DynamicHelper_GenericDictionaryLookup_Method (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method
.globl          DynamicHelper_GenericDictionaryLookup_Method
.type           DynamicHelper_GenericDictionaryLookup_Method,@function
DynamicHelper_GenericDictionaryLookup_Method:
        .functype       DynamicHelper_GenericDictionaryLookup_Method (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        i32.add
        i32.load        0                                                                       // Load the last indirection
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_0 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_0,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_0
.globl          DynamicHelper_GenericDictionaryLookup_Method_0
.type           DynamicHelper_GenericDictionaryLookup_Method_0,@function
DynamicHelper_GenericDictionaryLookup_Method_0:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_0 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        i32.load        0                                                                       // Load the last indirection (offset 0)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_1 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_1,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_1
.globl          DynamicHelper_GenericDictionaryLookup_Method_1
.type           DynamicHelper_GenericDictionaryLookup_Method_1,@function
DynamicHelper_GenericDictionaryLookup_Method_1:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_1 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        i32.load        0x4                                                                     // Load the last indirection (offset sizeof(TADDR) * 1)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_2 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_2,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_2
.globl          DynamicHelper_GenericDictionaryLookup_Method_2
.type           DynamicHelper_GenericDictionaryLookup_Method_2,@function
DynamicHelper_GenericDictionaryLookup_Method_2:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_2 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        i32.load        0x8                                                                     // Load the last indirection (offset sizeof(TADDR) * 2)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_3 (i32, i32, i32) -> (i32)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_3,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_3
.globl          DynamicHelper_GenericDictionaryLookup_Method_3
.type           DynamicHelper_GenericDictionaryLookup_Method_3,@function
DynamicHelper_GenericDictionaryLookup_Method_3:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_3 (i32, i32, i32) -> (i32)
        local.get       1                                                                       // Load the GenericContext
        i32.load        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        i32.load        0xC                                                                     // Load the last indirection (offset sizeof(TADDR) * 3)
        return
        end_function
