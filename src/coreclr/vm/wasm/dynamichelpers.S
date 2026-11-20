// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"

// Dictionary entries and table indices are pointers. Stub offsets and dictionary
// sizes remain 32-bit. Wasm branch predicates are always i32.
#ifdef TARGET_WASM64
#define PTR_TYPE i64
#define PTR_LOAD i64.load
#define PTR_ADD i64.add
#define PTR_CONST i64.const
#define LOAD_OFFSET i64.load32_u
#define PTR_TO_CONDITION i64.eqz ; i32.eqz
#define PTR_SLOT_1 8
#define PTR_SLOT_2 16
#define PTR_SLOT_3 24
        .functype CallDictionaryLookupHelper (i64, i64, i64, i64) -> (i64)
#else
#define PTR_TYPE i32
#define PTR_LOAD i32.load
#define PTR_ADD i32.add
#define PTR_CONST i32.const
#define LOAD_OFFSET i32.load
#define PTR_TO_CONDITION
#define PTR_SLOT_1 4
#define PTR_SLOT_2 8
#define PTR_SLOT_3 12
#endif

.macro CALL_LOOKUP_HELPER
#ifdef TARGET_WASM64
        call CallDictionaryLookupHelper
#else
        local.tee 2
        local.get 2
        i32.load 0
        call_indirect __indirect_function_table, (i32, i32, i32, i32) -> (i32)
#endif
.endm

.size   g_pClassWithSlotAndModule, WASM_POINTER_SIZE
.size   g_pMethodWithSlotAndModule, WASM_POINTER_SIZE


.functype       DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        .local PTR_TYPE
        block
        block
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the SecondDir indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SizeOffset
        PTR_ADD
        i32.load        0                                                                       // Load the size of the actual GenericDictionary on the Class
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SlotOffset
        i32.le_s
        br_if           0                                                                       // Branch to the logic to call the helper function if the size of the actual GenericDictionary on the Class is less than or equal to the SlotOffset
        local.get       3                                                                       // Load the SecondDir indirection
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        PTR_TO_CONDITION
        br_if           1                                                                       // Branch to return if the result is not null
        end_block
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        PTR_LOAD        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        PTR_CONST       0
        PTR_LOAD        g_pClassWithSlotAndModule
        CALL_LOOKUP_HELPER
        local.set       3
        end_block
        local.get       3                                                                       // Return the result.
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        .local PTR_TYPE
        block
        block
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        local.tee       3                                                                       // Store the loaded GenericDictionary into local 3, but keep it on the stack as well
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SizeOffset
        PTR_ADD
        i32.load        0                                                                       // Load the size of the actual GenericDictionary on the Method
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        i32.load        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SlotOffset
        i32.le_s
        br_if           0                                                                       // Branch to the logic to call the helper function if the size of the actual GenericDictionary on the Method is less than or equal to the SlotOffset
        local.get       3                                                                       // Load the GenericDictionary
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        PTR_TO_CONDITION
        br_if           1                                                                       // Branch to return if the result is not null
        end_block
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        PTR_LOAD        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        PTR_CONST       0
        PTR_LOAD        g_pMethodWithSlotAndModule
        CALL_LOOKUP_HELPER
        local.set       3
        end_block
        local.get       3                                                                       // Return the result.
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Class_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Class_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Class_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        .local PTR_TYPE
        block
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the SecondDir indirection
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        PTR_TO_CONDITION
        br_if           0                                                                       // Branch to return if the result is not null
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        PTR_LOAD        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        PTR_CONST       0
        PTR_LOAD        g_pClassWithSlotAndModule
        CALL_LOOKUP_HELPER
        local.set       3
        end_block
        local.get       3                                                                       // Return the result.
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class
.globl          DynamicHelper_GenericDictionaryLookup_Class
.type           DynamicHelper_GenericDictionaryLookup_Class,@function
DynamicHelper_GenericDictionaryLookup_Class:
        .functype       DynamicHelper_GenericDictionaryLookup_Class (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the SecondDir indirection
        local.get       2                                                                       // Load the GenericClassDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the last indirection
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_0 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_0,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_0
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_0
.type           DynamicHelper_GenericDictionaryLookup_Class_0_0,@function
DynamicHelper_GenericDictionaryLookup_Class_0_0:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_0 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        PTR_LOAD        0                                                                       // Load the SecondDir indirection (offset 0)
        PTR_LOAD        0                                                                       // Load the last indirection (offset 0)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_1 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_1,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_1
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_1
.type           DynamicHelper_GenericDictionaryLookup_Class_0_1,@function
DynamicHelper_GenericDictionaryLookup_Class_0_1:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_1 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        PTR_LOAD        0                                                                       // Load the SecondDir indirection (offset 0)
        PTR_LOAD        PTR_SLOT_1                                                                     // Load the last indirection (offset sizeof(TADDR) * 1)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_2 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_2,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_2
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_2
.type           DynamicHelper_GenericDictionaryLookup_Class_0_2,@function
DynamicHelper_GenericDictionaryLookup_Class_0_2:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_2 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        PTR_LOAD        0                                                                       // Load the SecondDir indirection (offset 0)
        PTR_LOAD        PTR_SLOT_2                                                                     // Load the last indirection (offset sizeof(TADDR) * 2)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_0_3 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_0_3,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_0_3
.globl          DynamicHelper_GenericDictionaryLookup_Class_0_3
.type           DynamicHelper_GenericDictionaryLookup_Class_0_3,@function
DynamicHelper_GenericDictionaryLookup_Class_0_3:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_0_3 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__MethodTable__m_pPerInstInfo
        PTR_LOAD        0                                                                       // Load the SecondDir indirection (offset 0)
        PTR_LOAD        PTR_SLOT_3                                                                     // Load the last indirection (offset sizeof(TADDR) * 3)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_TestForNull,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_TestForNull
.globl          DynamicHelper_GenericDictionaryLookup_Method_TestForNull
.type           DynamicHelper_GenericDictionaryLookup_Method_TestForNull,@function
DynamicHelper_GenericDictionaryLookup_Method_TestForNull:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_TestForNull (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        .local PTR_TYPE
        block
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the last indirection
        local.tee       3                                                                       // Store the loaded indirection into local 3, but keep it on the stack as well
        PTR_TO_CONDITION
        br_if           0                                                                       // Branch to return if the result is not null
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        PTR_LOAD        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        PTR_CONST       0
        PTR_LOAD        g_pMethodWithSlotAndModule
        CALL_LOOKUP_HELPER
        local.set       3
        end_block
        local.get       3                                                                       // Return the result.
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method
.globl          DynamicHelper_GenericDictionaryLookup_Method
.type           DynamicHelper_GenericDictionaryLookup_Method,@function
DynamicHelper_GenericDictionaryLookup_Method:
        .functype       DynamicHelper_GenericDictionaryLookup_Method (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        local.get       2                                                                       // Load the GenericMethodDictionaryDynamicHelperStubData parameter
        LOAD_OFFSET        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
        PTR_ADD
        PTR_LOAD        0                                                                       // Load the last indirection
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_0 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_0,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_0
.globl          DynamicHelper_GenericDictionaryLookup_Method_0
.type           DynamicHelper_GenericDictionaryLookup_Method_0,@function
DynamicHelper_GenericDictionaryLookup_Method_0:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_0 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        PTR_LOAD        0                                                                       // Load the last indirection (offset 0)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_1 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_1,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_1
.globl          DynamicHelper_GenericDictionaryLookup_Method_1
.type           DynamicHelper_GenericDictionaryLookup_Method_1,@function
DynamicHelper_GenericDictionaryLookup_Method_1:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_1 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        PTR_LOAD        PTR_SLOT_1                                                                     // Load the last indirection (offset sizeof(TADDR) * 1)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_2 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_2,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_2
.globl          DynamicHelper_GenericDictionaryLookup_Method_2
.type           DynamicHelper_GenericDictionaryLookup_Method_2,@function
DynamicHelper_GenericDictionaryLookup_Method_2:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_2 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        PTR_LOAD        PTR_SLOT_2                                                                     // Load the last indirection (offset sizeof(TADDR) * 2)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_3 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_3,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_3
.globl          DynamicHelper_GenericDictionaryLookup_Method_3
.type           DynamicHelper_GenericDictionaryLookup_Method_3,@function
DynamicHelper_GenericDictionaryLookup_Method_3:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_3 (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       1                                                                       // Load the GenericContext
        PTR_LOAD        OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
        PTR_LOAD        PTR_SLOT_3                                                                     // Load the last indirection (offset sizeof(TADDR) * 3)
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Class_UseHelper (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Class_UseHelper,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Class_UseHelper
.globl          DynamicHelper_GenericDictionaryLookup_Class_UseHelper
.type           DynamicHelper_GenericDictionaryLookup_Class_UseHelper,@function
DynamicHelper_GenericDictionaryLookup_Class_UseHelper:
        .functype       DynamicHelper_GenericDictionaryLookup_Class_UseHelper (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericDictionaryDynamicHelperStubData parameter
        PTR_LOAD        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        PTR_CONST       0
        PTR_LOAD        g_pClassWithSlotAndModule
        CALL_LOOKUP_HELPER
        return
        end_function


.functype       DynamicHelper_GenericDictionaryLookup_Method_UseHelper (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
.section        .text.DynamicHelper_GenericDictionaryLookup_Method_UseHelper,"",@
.hidden         DynamicHelper_GenericDictionaryLookup_Method_UseHelper
.globl          DynamicHelper_GenericDictionaryLookup_Method_UseHelper
.type           DynamicHelper_GenericDictionaryLookup_Method_UseHelper,@function
DynamicHelper_GenericDictionaryLookup_Method_UseHelper:
        .functype       DynamicHelper_GenericDictionaryLookup_Method_UseHelper (PTR_TYPE, PTR_TYPE, PTR_TYPE) -> (PTR_TYPE)
        local.get       0                                                                       // Load the stack pointer
        local.get       1                                                                       // Load the GenericContext
        local.get       2                                                                       // Load the GenericDictionaryDynamicHelperStubData parameter
        PTR_LOAD        OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
        PTR_CONST       0
        PTR_LOAD        g_pMethodWithSlotAndModule
        CALL_LOOKUP_HELPER
        return
        end_function
