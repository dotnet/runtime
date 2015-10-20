//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: CLASS.INL
//


//

//
// ============================================================================

#ifndef _CLASS_INL_
#define _CLASS_INL_
#include "constrainedexecutionregion.h"
//***************************************************************************************
inline PTR_MethodDescChunk EEClass::GetChunks()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pChunks.GetValueMaybeNull(PTR_HOST_MEMBER_TADDR(EEClass, this, m_pChunks));
}

//***************************************************************************************
inline DWORD EEClass::SomeMethodsRequireInheritanceCheck()
{
    return (m_VMFlags & VMFLAG_METHODS_REQUIRE_INHERITANCE_CHECKS);
}

//***************************************************************************************
inline void EEClass::SetSomeMethodsRequireInheritanceCheck()
{
    m_VMFlags = m_VMFlags | VMFLAG_METHODS_REQUIRE_INHERITANCE_CHECKS;
}

//*******************************************************************************
#ifndef DACCESS_COMPILE 
// Set default values for optional fields.
inline void EEClassOptionalFields::Init()
{
    LIMITED_METHOD_CONTRACT;
    m_pDictLayout = NULL;
    m_pVarianceInfo = NULL;
#ifdef FEATURE_COMINTEROP
    m_pSparseVTableMap = NULL;
    m_pCoClassForIntf = TypeHandle();
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    m_pClassFactory = NULL;
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    m_WinRTRedirectedTypeIndex = WinMDAdapter::RedirectedTypeIndex_Invalid;
#endif // FEATURE_COMINTEROP
    m_cbModuleDynamicID = MODULE_NON_DYNAMIC_STATICS;
    m_dwReliabilityContract = RC_NULL;
    m_SecProps = 0;
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    m_numberEightBytes = 0;
#endif // UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING    
}
#endif // !DACCESS_COMPILE

#endif  // _CLASS_INL_

