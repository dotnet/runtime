// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



inline BOOL ExecutionManager::IsCollectibleMethod(const METHODTOKEN& MethodToken)
{
    WRAPPER_NO_CONTRACT;
    return MethodToken.m_pRangeSection->flags & RangeSection::RANGE_SECTION_COLLECTIBLE;
}

inline TADDR IJitManager::JitTokenToModuleBase(const METHODTOKEN& MethodToken)
{
    return MethodToken.m_pRangeSection->LowAddress;
}
