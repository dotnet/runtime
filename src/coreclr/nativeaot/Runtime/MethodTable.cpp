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

    // Deal with the most common case of a bad pointer without an exception.
    if (this == NULL)
        REPORT_FAILURE();

    // MethodTable structures should be at least pointer aligned.
    if (dac_cast<TADDR>(this) & (sizeof(TADDR)-1))
        REPORT_FAILURE();

    // Verify object size is bigger than min_obj_size
    size_t minObjSize = get_BaseSize();
    if (get_ComponentSize() != 0)
    {
        // If it is an array, we will align the size to the nearest pointer alignment, even if there are
        // zero elements.  Our strings take advantage of this.
        minObjSize = (size_t)ALIGN_UP(minObjSize, sizeof(TADDR));
    }
    if (minObjSize < (3 * sizeof(TADDR)))
        REPORT_FAILURE();

    switch (get_Kind())
    {
    case CanonicalEEType:
    {
        // If the parent type is NULL this had better look like Object.
        if (!IsInterface() && (m_RelatedType.m_pBaseType == NULL))
        {
            if (IsRelatedTypeViaIAT() ||
                get_IsValueType() ||
                HasFinalizer() ||
                HasReferenceFields() ||
                HasGenericVariance())
            {
                REPORT_FAILURE();
            }
        }
        break;
    }

    case ClonedEEType:
    {
        // Cloned types must have a related type.
        if (m_RelatedType.m_ppCanonicalTypeViaIAT == NULL)
            REPORT_FAILURE();

        // Either we're dealing with a clone of String or a generic type. We can tell the difference based
        // on the component size.
        switch (get_ComponentSize())
        {
        case 0:
        {
            // Cloned generic type.
            if (!IsRelatedTypeViaIAT())
            {
                REPORT_FAILURE();
            }
            break;
        }

        case 2:
        {
            // Cloned string.
            if (get_IsValueType() ||
                HasFinalizer() ||
                HasReferenceFields() ||
                HasGenericVariance())
            {
                REPORT_FAILURE();
            }

            break;
        }

        default:
            // Apart from cloned strings we don't expected cloned types to have a component size.
            REPORT_FAILURE();
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
        if (get_ComponentSize() == 0)
            REPORT_FAILURE();

        if (get_IsValueType() ||
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
MethodTable::Kinds MethodTable::get_Kind()
{
	return (Kinds)(m_usFlags & (uint16_t)EETypeKindMask);
}

//-----------------------------------------------------------------------------------------------------------
MethodTable * MethodTable::get_CanonicalEEType()
{
	// cloned EETypes must always refer to types in other modules
	ASSERT(IsCloned());
    if (IsRelatedTypeViaIAT())
        return *PTR_PTR_EEType(reinterpret_cast<TADDR>(m_RelatedType.m_ppCanonicalTypeViaIAT));
    else
        return PTR_EEType(reinterpret_cast<TADDR>(m_RelatedType.m_pCanonicalType)); // in the R2R case, the link is direct rather than indirect via the IAT
}

//-----------------------------------------------------------------------------------------------------------
MethodTable * MethodTable::get_RelatedParameterType()
{
	ASSERT(IsParameterizedType());

	if (IsRelatedTypeViaIAT())
		return *PTR_PTR_EEType(reinterpret_cast<TADDR>(m_RelatedType.m_ppRelatedParameterTypeViaIAT));
	else
		return PTR_EEType(reinterpret_cast<TADDR>(m_RelatedType.m_pRelatedParameterType));
}
