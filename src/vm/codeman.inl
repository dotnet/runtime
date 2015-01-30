//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//



inline BOOL ExecutionManager::IsCollectibleMethod(const METHODTOKEN& MethodToken)
{
    WRAPPER_NO_CONTRACT;
    return MethodToken.m_pRangeSection->flags & RangeSection::RANGE_SECTION_COLLECTIBLE;
}

inline TADDR IJitManager::JitTokenToModuleBase(const METHODTOKEN& MethodToken)
{
    return MethodToken.m_pRangeSection->LowAddress;
}
