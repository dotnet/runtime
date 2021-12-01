// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CLASS.INL
//

#ifndef _CLASS_INL_
#define _CLASS_INL_
//***************************************************************************************
inline PTR_MethodDescChunk EEClass::GetChunks()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_pChunks;
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
#endif // FEATURE_COMINTEROP
    m_cbModuleDynamicID = MODULE_NON_DYNAMIC_STATICS;
#if defined(UNIX_AMD64_ABI)
    m_numberEightBytes = 0;
#endif // UNIX_AMD64_ABI
}
#endif // !DACCESS_COMPILE

#endif  // _CLASS_INL_

