// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "rhassert.h"
#include "rhbinder.h"
#include "MethodTable.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "ObjectLayout.h"

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
        MethodTable* pBaseType = GetNonArrayBaseType();

        // If the parent type is NULL this had better look like Object.
        if (pBaseType == NULL)
        {
            if (IsValueType() ||
                HasFinalizer() ||
                HasReferenceFields() ||
                HasGenericVariance())
            {
                REPORT_FAILURE();
            }
        }
        else if (GetNumVtableSlots() == 0
            && !IsGCStaticMethodTable())
        {
            // We only really expect zero vtable slots for GCStatic MethodTables, however
            // to cover the unlikely case that we managed to trim out Equals/GetHashCode/ToString,
            // check an invariant that says if a derived type has zero vtable slots, all bases need
            // to have zero slots.
            MethodTable* pCurrentType = pBaseType;
            do
            {
                if (pCurrentType->GetNumVtableSlots() > GetNumVtableSlots())
                {
                    REPORT_FAILURE();
                }
                pCurrentType = pCurrentType->GetNonArrayBaseType();
            }
            while (pCurrentType != NULL);
        }
        break;
    }

    case ParameterizedEEType:
    {
        // The only parameter EETypes that can exist on the heap are arrays

        // Array types must have a related type.
        MethodTable* pParameterType = GetRelatedParameterType();
        if (pParameterType == NULL)
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

        // Zero vtable slots is suspicious. To cover the unlikely case that we managed to trim
        // out Equals/GetHashCode/ToString, compare with number of slots of Object class.
        if (GetNumVtableSlots() == 0)
        {
            // Drill into the type to find System.Object

            MethodTable* pCurrentType = pParameterType;

            while (pCurrentType->IsParameterizedType())
                pCurrentType = pCurrentType->GetRelatedParameterType();

            // We don't have facilities to unwrap function pointers, so just skip for now.
            // We won't get System.Object from interfaces, skip too.
            if (!pCurrentType->IsFunctionPointer() && !pCurrentType->IsInterface())
            {
                do
                {
                    MethodTable* pBaseType = pCurrentType->GetNonArrayBaseType();
                    if (pBaseType == NULL)
                    {
                        // Found System.Object, now compare number of slots
                        if (pCurrentType->GetNumVtableSlots() > GetNumVtableSlots())
                        {
                            REPORT_FAILURE();
                        }
                    }

                    pCurrentType = pBaseType;
                } while (pCurrentType != NULL);
            }
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
	return (Kinds)(m_uFlags & EETypeKindMask);
}

//-----------------------------------------------------------------------------------------------------------
MethodTable * MethodTable::GetRelatedParameterType()
{
	ASSERT(IsParameterizedType());

	return PTR_EEType(reinterpret_cast<TADDR>(m_RelatedType.m_pRelatedParameterType));
}

//-----------------------------------------------------------------------------------------------------------
uint32_t MethodTable::GetArrayRank()
{
    ASSERT(IsArray());
    uint32_t boundsSize = GetParameterizedTypeShape() - SZARRAY_BASE_SIZE;
    if (boundsSize > 0)
    {
        // Multidim array case: Base size includes space for two Int32s
        // (upper and lower bound) per each dimension of the array.
        return boundsSize / (2 * sizeof(uint32_t));
    }
    return 1;
}
