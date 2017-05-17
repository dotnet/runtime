// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: CLASS.INL
//


//

//
// ============================================================================

#ifndef _CLASS_INL_
#define _CLASS_INL_
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
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    m_numberEightBytes = 0;
#endif // UNIX_AMD64_ABI && FEATURE_UNIX_AMD64_STRUCT_PASSING    
}
#endif // !DACCESS_COMPILE

#endif  // _CLASS_INL_

