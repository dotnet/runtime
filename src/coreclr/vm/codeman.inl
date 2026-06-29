// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



inline BOOL ExecutionManager::IsCollectibleMethod(const METHODTOKEN& MethodToken)
{
    WRAPPER_NO_CONTRACT;
    return MethodToken.m_pRangeSection->_flags & RangeSection::RANGE_SECTION_COLLECTIBLE;
}

inline TADDR IJitManager::JitTokenToModuleRVABase(const METHODTOKEN& MethodToken)
{
#ifdef TARGET_WASM
    if (MethodToken.m_pRangeSection->_flags & RangeSection::RANGE_SECTION_VIRTUALIP)
        return (TADDR)MethodToken.m_pRangeSection->_pR2RModule->GetModuleBaseAddress();
#endif
    // For non-wasm, the rva base is always the same as the range base.
    return MethodToken.m_pRangeSection->_range.RangeStart();
}

inline TADDR IJitManager::JitTokenToModuleFunctionsBase(const METHODTOKEN& MethodToken)
{
    return MethodToken.m_pRangeSection->_range.RangeStart();
}
