// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "rhassert.h"
#include "rhbinder.h"
#include "MethodTable.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"

#include "CommonMacros.inl"
#include "MethodTable.inl"

#pragma warning(disable:4127) // C4127: conditional expression is constant

// Validate an MethodTable extracted from an object.
bool MethodTable::Validate(bool assertOnFail /* default: true */)
{
#define REPORT_FAILURE() do { if (assertOnFail) { ASSERT_UNCONDITIONALLY("MethodTable::Validate check failed"); } return false; } while (false)

    // MethodTable structures should be at least pointer aligned.
    if (dac_cast<TADDR>(this) & (sizeof(TADDR)-1))
        REPORT_FAILURE();

    // Verify object size is bigger than min_obj_size
    size_t minObjSize = GetBaseSize();
    if (HasComponentSize())
    {
        // If it is an array, we will align the size to the nearest pointer alignment, even if there are
        // zero elements.  Our strings take advantage of this.
        minObjSize = (size_t)ALIGN_UP(minObjSize, sizeof(TADDR));
    }
    if (minObjSize < (3 * sizeof(TADDR)))
        REPORT_FAILURE();

    switch (GetKind())
    {
    case CanonicalEEType:
    {
        // If the parent type is NULL this had better look like Object.
        if (!IsInterface() && (m_RelatedType.m_pBaseType == NULL))
        {
            if (IsValueType() ||
                HasFinalizer() ||
                HasReferenceFields() ||
                HasGenericVariance())
            {
                REPORT_FAILURE();
            }
        }
        break;
    }

    case ParameterizedEEType:
    {
        // The only parameter EETypes that can exist on the heap are arrays

        // Array types must have a related type.
        if (m_RelatedType.m_pRelatedParameterType == NULL)
            REPORT_FAILURE();

        // Component size cannot be zero in this case.
        if (GetComponentSize() == 0)
            REPORT_FAILURE();

        if (IsValueType() ||
            HasFinalizer() ||
            HasGenericVariance())
        {
            REPORT_FAILURE();
        }

        break;
    }

    case GenericTypeDefEEType:
    {
        // We should never see uninstantiated generic type definitions here
        // since we should never construct an object instance around them.
        REPORT_FAILURE();
    }

    default:
        // Should be unreachable.
        REPORT_FAILURE();
    }

#undef REPORT_FAILURE

    return true;
}

//-----------------------------------------------------------------------------------------------------------
MethodTable::Kinds MethodTable::GetKind()
{
	return (Kinds)(m_uFlags & (uint16_t)EETypeKindMask);
}

//-----------------------------------------------------------------------------------------------------------
MethodTable * MethodTable::GetRelatedParameterType()
{
	ASSERT(IsParameterizedType());

	return PTR_EEType(reinterpret_cast<TADDR>(m_RelatedType.m_pRelatedParameterType));
}
