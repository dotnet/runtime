// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: clsload.inl
//

//

//
// ============================================================================


#ifndef _CLSLOAD_INL_
#define _CLSLOAD_INL_

inline PTR_Assembly ClassLoader::GetAssembly()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return m_pAssembly;
}

inline PTR_Module ClassLoader::ComputeLoaderModuleForFunctionPointer(TypeHandle* pRetAndArgTypes, DWORD NumArgsPlusRetType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;
    return ComputeLoaderModuleWorker(NULL,
                               0,
                               Instantiation(pRetAndArgTypes, NumArgsPlusRetType),
                               Instantiation());
}

inline PTR_Module ClassLoader::ComputeLoaderModuleForParamType(TypeHandle paramType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    //
    // Call GetLoaderModule directly instead of ComputeLoaderModuleWorker to avoid exponential recursion for collectible types
    //
    // It is safe to do it even during NGen because of we do not create duplicate copies of arrays and other param types
    // (see code:CEEPreloader::TriageTypeForZap).
    //
    return paramType.GetLoaderModule();
}

//******************************************************************************

inline void AccessCheckOptions::Initialize(
    AccessCheckType      accessCheckType,
    BOOL                 throwIfTargetIsInaccessible,
    MethodTable *        pTargetMT,
    MethodDesc *         pTargetMethod,
    FieldDesc *          pTargetField)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        // At most one of these can be non-NULL. They can all be NULL if:
        //   1. we are doing a normal accessibility check, or
        //   2. we are not going to throw an exception if the accessibility check fails
        PRECONDITION(accessCheckType == kNormalAccessibilityChecks ||
                     !throwIfTargetIsInaccessible ||
                     ((pTargetMT ? 1 : 0) + (pTargetMethod ? 1 : 0) + (pTargetField ? 1 : 0)) == 1);
        // m_pAccessContext can only be set for kRestrictedMemberAccess
        PRECONDITION(m_pAccessContext == NULL ||
                     accessCheckType == AccessCheckOptions::kRestrictedMemberAccess);
    }
    CONTRACTL_END;

    m_accessCheckType = accessCheckType;
    m_fThrowIfTargetIsInaccessible = throwIfTargetIsInaccessible;
    m_pTargetMT = pTargetMT;
    m_pTargetMethod = pTargetMethod;
    m_pTargetField = pTargetField;
}

//******************************************************************************

inline AccessCheckOptions::AccessCheckOptions(
    AccessCheckType      accessCheckType,
    DynamicResolver *    pAccessContext,
    BOOL                 throwIfTargetIsInaccessible,
    MethodTable *        pTargetMT) :
    m_pAccessContext(pAccessContext)
{
    WRAPPER_NO_CONTRACT;

    Initialize(
        accessCheckType,
        throwIfTargetIsInaccessible,
        pTargetMT,
        NULL,
        NULL);
}

inline AccessCheckOptions::AccessCheckOptions(
    AccessCheckType      accessCheckType,
    DynamicResolver *    pAccessContext,
    BOOL                 throwIfTargetIsInaccessible,
    MethodDesc *         pTargetMethod) :
    m_pAccessContext(pAccessContext)
{
    WRAPPER_NO_CONTRACT;

    Initialize(
        accessCheckType,
        throwIfTargetIsInaccessible,
        NULL,
        pTargetMethod,
        NULL);
}

inline AccessCheckOptions::AccessCheckOptions(
    AccessCheckType      accessCheckType,
    DynamicResolver *    pAccessContext,
    BOOL                 throwIfTargetIsInaccessible,
    FieldDesc *          pTargetField) :
    m_pAccessContext(pAccessContext)
{
    WRAPPER_NO_CONTRACT;

    Initialize(
        accessCheckType,
        throwIfTargetIsInaccessible,
        NULL,
        NULL,
        pTargetField);
}

#endif  // _CLSLOAD_INL_

