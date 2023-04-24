// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "debuginfo.h"

//------------------------------------------------------------------------
// EncodeSourceTypes:
//   Encode the JIT-EE source type for an ILLocation.
//
// Returns:
//   The JIT-EE interface source type.
//
// Remarks:
//   We currently encode only calls and stack empty location.
//
ICorDebugInfo::SourceTypes ILLocation::EncodeSourceTypes() const
{
    int source = 0;
    if (IsStackEmpty())
    {
        source |= ICorDebugInfo::STACK_EMPTY;
    }

    if (IsCall())
    {
        source |= ICorDebugInfo::CALL_INSTRUCTION;
    }

    return static_cast<ICorDebugInfo::SourceTypes>(source);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// Dump: Print a textual representation of this ILLocation.
//
// Notes:
//    For invalid ILLocations, we print '???'.
//    Otherwise the offset and flags are printed in the format 0xabc[EC].
//
void ILLocation::Dump() const
{
    if (!IsValid())
    {
        printf("???");
    }
    else
    {
        printf("0x%03X[", GetOffset());
        printf("%c", IsStackEmpty() ? 'E' : '-');
        printf("%c", IsCall() ? 'C' : '-');
        printf("]");
    }
}

//------------------------------------------------------------------------
// Dump: Print a textual representation of this DebugInfo.
//
// Parameters:
//     recurse - print the full path back to the root, separated by arrows.
//
// Notes:
//    The DebugInfo is printed in the format
//
//        INL02 @ 0xabc[EC]
//
//    Before '@' is the ordinal of the inline context, then comes the IL
//    offset, and then comes the IL location flags (stack Empty, isCall).
//
//    If 'recurse' is specified then dump the full DebugInfo path to the
//    root in the format
//
//        INL02 @ 0xabc[EC] <- INL01 @ 0x123[EC] <- ... <- INLRT @ 0x456[EC]
//
//    with the left most entry being the inner most inlined statement.
void DebugInfo::Dump(bool recurse) const
{
    InlineContext* context = GetInlineContext();
    if (context != nullptr)
    {
        if (context->IsRoot())
        {
            printf("INLRT @ ");
        }
        else if (context->GetOrdinal() != 0)
        {
            printf(FMT_INL_CTX " @ ", context->GetOrdinal());
        }
    }

    GetLocation().Dump();

    DebugInfo par;
    if (recurse && GetParent(&par))
    {
        printf(" <- ");
        par.Dump(recurse);
    }
}

//------------------------------------------------------------------------
// Validate: Validate this DebugInfo instance.
//
// Notes:
//    This validates that if there is DebugInfo, then it looks sane by checking
//    that the IL location correctly points to the beginning of an IL instruction.
//
void DebugInfo::Validate() const
{
    DebugInfo di = *this;
    do
    {
        if (!di.IsValid())
            continue;

        bool isValidOffs = di.GetLocation().GetOffset() < di.GetInlineContext()->GetILSize();
        if (isValidOffs)
        {
            bool isValidStart = di.GetInlineContext()->GetILInstsSet()->bitVectTest(di.GetLocation().GetOffset());
            assert(isValidStart &&
                   "Detected invalid debug info: IL offset does not refer to the start of an IL instruction");
        }
        else
        {
            assert(!"Detected invalid debug info: IL offset is out of range");
        }

    } while (di.GetParent(&di));
}
#endif

//------------------------------------------------------------------------
// GetParent: Get debug info for the parent statement that inlined the
// statement for this debug info.
//
// Parameters:
//     parent [out] - Debug info for the location that inlined this statement.
//
// Return Value:
//     True if the current debug info is valid and has a parent; otherwise false.
//     On false return, the 'parent' parameter is unaffected.
//
bool DebugInfo::GetParent(DebugInfo* parent) const
{
    if ((m_inlineContext == nullptr) || m_inlineContext->IsRoot())
        return false;

    *parent = DebugInfo(m_inlineContext->GetParent(), m_inlineContext->GetLocation());
    return true;
}

//------------------------------------------------------------------------
// GetRoot: Get debug info for the statement in the root function that
// eventually led to this debug info through inlines.
//
// Return Value:
//    If this DebugInfo instance is valid, returns a DebugInfo instance
//    representing the call in the root function that eventually inlined the
//    statement this DebugInfo describes.
//
//   If this DebugInfo instance is invalid, returns an invalid DebugInfo instance.
//
DebugInfo DebugInfo::GetRoot() const
{
    DebugInfo result = *this;
    while (result.GetParent(&result))
    {
    }

    return result;
}
