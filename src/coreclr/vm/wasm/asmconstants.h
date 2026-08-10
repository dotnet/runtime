// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// asmconstants.h -
//
// This header defines field offsets and constants used by assembly code
// Be sure to rebuild clr/src/vm/ceemain.cpp after changing this file, to
// ensure that the constants match the expected C/C++ values

#ifndef ASMCONSTANTS_C_ASSERT
#define ASMCONSTANTS_C_ASSERT(cond)
#endif

#ifndef ASMCONSTANTS_RUNTIME_ASSERT
#define ASMCONSTANTS_RUNTIME_ASSERT(cond)
#endif

// Some constants are different in _DEBUG builds.  This macro factors out ifdefs from below.
#ifdef _DEBUG
#define DBG_FRE(dbg,fre) dbg
#else
#define DBG_FRE(dbg,fre) fre
#endif

#define DynamicHelperFrameFlags_ObjectArg   1
#define DynamicHelperFrameFlags_ObjectArg2  2

#define OFFSETOF__MethodTable__m_pPerInstInfo   DBG_FRE(0x24, 0x20)
ASMCONSTANTS_C_ASSERT(OFFSETOF__MethodTable__m_pPerInstInfo
                    == offsetof(MethodTable, m_pPerInstInfo));

#define OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo   DBG_FRE(0x28, 0x14)
ASMCONSTANTS_C_ASSERT(OFFSETOF__InstantiatedMethodDesc__m_pPerInstInfo
                    == offsetof(InstantiatedMethodDesc, m_pPerInstInfo));

#define OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__stubData 0x04
ASMCONSTANTS_C_ASSERT(OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__stubData
                    == offsetof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint, stubData));

#define OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir   0x4
ASMCONSTANTS_C_ASSERT(OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SecondIndir
                    == offsetof(GenericDictionaryDynamicHelperStubData, SecondIndir) + offsetof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint, stubData));

#define OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir     0x8
ASMCONSTANTS_C_ASSERT(OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__LastIndir
                    == offsetof(GenericDictionaryDynamicHelperStubData, LastIndir) + offsetof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint, stubData));

#define OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SizeOffset    0xC
ASMCONSTANTS_C_ASSERT(OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SizeOffset
                    == offsetof(GenericDictionaryDynamicHelperStubData, SizeOffset) + offsetof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint, stubData));

#define OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SlotOffset    0x10
ASMCONSTANTS_C_ASSERT(OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__SlotOffset
                    == offsetof(GenericDictionaryDynamicHelperStubData, SlotOffset) + offsetof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint, stubData));

#define OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs    0x14
ASMCONSTANTS_C_ASSERT(OFFSETOF__GenericDictionaryDynamicHelperStubData_PortableEntryPoint__HandleArgs
                    == offsetof(GenericDictionaryDynamicHelperStubData, HandleArgs) + offsetof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint, stubData));

#undef ASMCONSTANTS_RUNTIME_ASSERT
#undef ASMCONSTANTS_C_ASSERT
