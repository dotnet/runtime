// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          typeInfo                                         XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "_typeinfo.h"

bool Compiler::tiCompatibleWith(const typeInfo& child, const typeInfo& parent, bool normalisedForStack) const
{
#ifdef DEBUG
#if VERBOSE_VERIFY
    if (VERBOSE && tiVerificationNeeded)
    {
        printf("\n");
        printf(TI_DUMP_PADDING);
        printf("Verifying compatibility against types: ");
        child.Dump();
        printf(" and ");
        parent.Dump();
    }
#endif // VERBOSE_VERIFY
#endif // DEBUG

    bool compatible = typeInfo::tiCompatibleWith(info.compCompHnd, child, parent, normalisedForStack);

#ifdef DEBUG
#if VERBOSE_VERIFY
    if (VERBOSE && tiVerificationNeeded)
    {
        printf(compatible ? " [YES]" : " [NO]");
    }
#endif // VERBOSE_VERIFY
#endif // DEBUG

    return compatible;
}

bool Compiler::tiMergeCompatibleWith(const typeInfo& child, const typeInfo& parent, bool normalisedForStack) const
{
    return typeInfo::tiMergeCompatibleWith(info.compCompHnd, child, parent, normalisedForStack);
}

bool Compiler::tiMergeToCommonParent(typeInfo* pDest, const typeInfo* pSrc, bool* changed) const
{
#ifdef DEBUG
#if VERBOSE_VERIFY
    if (VERBOSE && tiVerificationNeeded)
    {
        printf("\n");
        printf(TI_DUMP_PADDING);
        printf("Attempting to merge types: ");
        pDest->Dump();
        printf(" and ");
        pSrc->Dump();
        printf("\n");
    }
#endif // VERBOSE_VERIFY
#endif // DEBUG

    bool mergeable = typeInfo::tiMergeToCommonParent(info.compCompHnd, pDest, pSrc, changed);

#ifdef DEBUG
#if VERBOSE_VERIFY
    if (VERBOSE && tiVerificationNeeded)
    {
        printf(TI_DUMP_PADDING);
        printf(mergeable ? "Merge successful" : "Couldn't merge types");
        if (*changed)
        {
            assert(mergeable);
            printf(", destination type changed to: ");
            pDest->Dump();
        }
        printf("\n");
    }
#endif // VERBOSE_VERIFY
#endif // DEBUG

    return mergeable;
}

static bool tiCompatibleWithByRef(COMP_HANDLE CompHnd, const typeInfo& child, const typeInfo& parent)
{
    assert(parent.IsByRef());

    if (!child.IsByRef())
    {
        return false;
    }

    if (child.IsReadonlyByRef() && !parent.IsReadonlyByRef())
    {
        return false;
    }

    // Byrefs are compatible if the underlying types are equivalent
    typeInfo childTarget  = ::DereferenceByRef(child);
    typeInfo parentTarget = ::DereferenceByRef(parent);

    if (typeInfo::AreEquivalent(childTarget, parentTarget))
    {
        return true;
    }

    // Make sure that both types have a valid m_cls
    if ((childTarget.IsType(TI_REF) || childTarget.IsType(TI_STRUCT)) &&
        (parentTarget.IsType(TI_REF) || parentTarget.IsType(TI_STRUCT)))
    {
        return CompHnd->areTypesEquivalent(childTarget.GetClassHandle(), parentTarget.GetClassHandle());
    }

    return false;
}

/*****************************************************************************
 * Verify child is compatible with the template parent.  Basically, that
 * child is a "subclass" of parent -it can be substituted for parent
 * anywhere.  Note that if parent contains fancy flags, such as "uninitialized"
 * , "is this ptr", or  "has byref local/field" info, then child must also
 * contain those flags, otherwise FALSE will be returned !
 *
 * Rules for determining compatibility:
 *
 * If parent is a primitive type or value class, then child must be the
 * same primitive type or value class.  The exception is that the built in
 * value classes System/Boolean etc. are treated as synonyms for
 * TI_BYTE etc.
 *
 * If parent is a byref of a primitive type or value class, then child
 * must be a byref of the same (rules same as above case).
 *
 * Byrefs are compatible only with byrefs.
 *
 * If parent is an object, child must be a subclass of it, implement it
 * (if it is an interface), or be null.
 *
 * If parent is an array, child must be the same or subclassed array.
 *
 * If parent is a null objref, only null is compatible with it.
 *
 * If the "uninitialized", "by ref local/field", "this pointer" or other flags
 * are different, the items are incompatible.
 *
 * parent CANNOT be an undefined (dead) item.
 *
 */

bool typeInfo::tiCompatibleWith(COMP_HANDLE     CompHnd,
                                const typeInfo& child,
                                const typeInfo& parent,
                                bool            normalisedForStack)
{
    assert(child.IsDead() || !normalisedForStack || typeInfo::AreEquivalent(::NormaliseForStack(child), child));
    assert(parent.IsDead() || !normalisedForStack || typeInfo::AreEquivalent(::NormaliseForStack(parent), parent));

    if (typeInfo::AreEquivalent(child, parent))
    {
        return true;
    }

    if (parent.IsUnboxedGenericTypeVar() || child.IsUnboxedGenericTypeVar())
    {
        return false; // need to have had child == parent
    }
    else if (parent.IsType(TI_REF))
    {
        // An uninitialized objRef is not compatible to initialized.
        if (child.IsUninitialisedObjRef() && !parent.IsUninitialisedObjRef())
        {
            return false;
        }

        if (child.IsNullObjRef())
        { // NULL can be any reference type
            return true;
        }
        if (!child.IsType(TI_REF))
        {
            return false;
        }

        return CompHnd->canCast(child.m_cls, parent.m_cls);
    }
    else if (parent.IsType(TI_METHOD))
    {
        if (!child.IsType(TI_METHOD))
        {
            return false;
        }

        // Right now we don't bother merging method handles
        return false;
    }
    else if (parent.IsType(TI_STRUCT))
    {
        if (!child.IsType(TI_STRUCT))
        {
            return false;
        }

        // Structures are compatible if they are equivalent
        return CompHnd->areTypesEquivalent(child.m_cls, parent.m_cls);
    }
    else if (parent.IsByRef())
    {
        return tiCompatibleWithByRef(CompHnd, child, parent);
    }
#ifdef TARGET_64BIT
    // On 64-bit targets we have precise representation for native int, so these rules
    // represent the fact that the ECMA spec permits the implicit conversion
    // between an int32 and a native int.
    else if (parent.IsType(TI_INT) && typeInfo::AreEquivalent(nativeInt(), child))
    {
        return true;
    }
    else if (typeInfo::AreEquivalent(nativeInt(), parent) && child.IsType(TI_INT))
    {
        return true;
    }
#endif // TARGET_64BIT
    return false;
}

bool typeInfo::tiMergeCompatibleWith(COMP_HANDLE     CompHnd,
                                     const typeInfo& child,
                                     const typeInfo& parent,
                                     bool            normalisedForStack)
{
    if (!child.IsPermanentHomeByRef() && parent.IsPermanentHomeByRef())
    {
        return false;
    }

    return typeInfo::tiCompatibleWith(CompHnd, child, parent, normalisedForStack);
}

/*****************************************************************************
 * Merge pDest and pSrc to find some commonality (e.g. a common parent).
 * Copy the result to pDest, marking it dead if no commonality can be found.
 *
 * null ^ null                  -> null
 * Object ^ null                -> Object
 * [I4 ^ null                   -> [I4
 * InputStream ^ OutputStream   -> Stream
 * InputStream ^ NULL           -> InputStream
 * [I4 ^ Object                 -> Object
 * [I4 ^ [Object                -> Array
 * [I4 ^ [R8                    -> Array
 * [Foo ^ I4                    -> DEAD
 * [Foo ^ [I1                   -> Array
 * [InputStream ^ [OutputStream -> Array
 * DEAD ^ X                     -> DEAD
 * [Intfc ^ [OutputStream       -> Array
 * Intf ^ [OutputStream         -> Object
 * [[InStream ^ [[OutStream     -> Array
 * [[InStream ^ [OutStream      -> Array
 * [[Foo ^ [Object              -> Array
 *
 * Importantly:
 * [I1 ^ [U1                    -> either [I1 or [U1
 * etc.
 *
 * Also, System/Int32 and I4 merge -> I4, etc.
 *
 * Returns FALSE if the merge was completely incompatible (i.e. the item became
 * dead).
 *
 */

bool typeInfo::tiMergeToCommonParent(COMP_HANDLE CompHnd, typeInfo* pDest, const typeInfo* pSrc, bool* changed)
{
    assert(pSrc->IsDead() || typeInfo::AreEquivalent(::NormaliseForStack(*pSrc), *pSrc));
    assert(pDest->IsDead() || typeInfo::AreEquivalent(::NormaliseForStack(*pDest), *pDest));

    // Merge the auxiliary information like "this" pointer tracking, etc...

    // Remember the pre-state, so we can tell if it changed.
    *changed              = false;
    DWORD destFlagsBefore = pDest->m_flags;

    // This bit is only set if both pDest and pSrc have it set
    pDest->m_flags &= (pSrc->m_flags | ~TI_FLAG_THIS_PTR);

    // This bit is set if either pDest or pSrc have it set
    pDest->m_flags |= (pSrc->m_flags & TI_FLAG_UNINIT_OBJREF);

    // This bit is set if either pDest or pSrc have it set
    pDest->m_flags |= (pSrc->m_flags & TI_FLAG_BYREF_READONLY);

    // If the byref wasn't permanent home in both sides, then merge won't have the bit set
    pDest->m_flags &= (pSrc->m_flags | ~TI_FLAG_BYREF_PERMANENT_HOME);

    if (pDest->m_flags != destFlagsBefore)
    {
        *changed = true;
    }

    // OK the main event.  Merge the main types
    if (typeInfo::AreEquivalent(*pDest, *pSrc))
    {
        return true;
    }

    if (pDest->IsUnboxedGenericTypeVar() || pSrc->IsUnboxedGenericTypeVar())
    {
        // Should have had *pDest == *pSrc
        goto FAIL;
    }
    if (pDest->IsType(TI_REF))
    {
        if (pSrc->IsType(TI_NULL))
        { // NULL can be any reference type
            return true;
        }
        if (!pSrc->IsType(TI_REF))
        {
            goto FAIL;
        }

        // Ask the EE to find the common parent,  This always succeeds since System.Object always works
        CORINFO_CLASS_HANDLE pDestClsBefore = pDest->m_cls;
        pDest->m_cls                        = CompHnd->mergeClasses(pDest->GetClassHandle(), pSrc->GetClassHandle());
        if (pDestClsBefore != pDest->m_cls)
        {
            *changed = true;
        }
        return true;
    }
    else if (pDest->IsType(TI_NULL))
    {
        if (pSrc->IsType(TI_REF)) // NULL can be any reference type
        {
            *pDest   = *pSrc;
            *changed = true;
            return true;
        }
        goto FAIL;
    }
    else if (pDest->IsType(TI_STRUCT))
    {
        if (pSrc->IsType(TI_STRUCT) && CompHnd->areTypesEquivalent(pDest->GetClassHandle(), pSrc->GetClassHandle()))
        {
            return true;
        }
        goto FAIL;
    }
    else if (pDest->IsByRef())
    {
        return tiCompatibleWithByRef(CompHnd, *pSrc, *pDest);
    }
#ifdef TARGET_64BIT
    // On 64-bit targets we have precise representation for native int, so these rules
    // represent the fact that the ECMA spec permits the implicit conversion
    // between an int32 and a native int.
    else if (typeInfo::AreEquivalent(*pDest, typeInfo::nativeInt()) && pSrc->IsType(TI_INT))
    {
        return true;
    }
    else if (typeInfo::AreEquivalent(*pSrc, typeInfo::nativeInt()) && pDest->IsType(TI_INT))
    {
        *pDest   = *pSrc;
        *changed = true;
        return true;
    }
#endif // TARGET_64BIT

FAIL:
    *pDest = typeInfo();
    return false;
}

#ifdef DEBUG
#if VERBOSE_VERIFY
// Utility method to have a detailed dump of a TypeInfo object
void typeInfo::Dump() const
{
    char flagsStr[8];

    flagsStr[0] = ((m_flags & TI_FLAG_UNINIT_OBJREF) != 0) ? 'U' : '-';
    flagsStr[1] = ((m_flags & TI_FLAG_BYREF) != 0) ? 'B' : '-';
    flagsStr[2] = ((m_flags & TI_FLAG_BYREF_READONLY) != 0) ? 'R' : '-';
    flagsStr[3] = ((m_flags & TI_FLAG_NATIVE_INT) != 0) ? 'N' : '-';
    flagsStr[4] = ((m_flags & TI_FLAG_THIS_PTR) != 0) ? 'T' : '-';
    flagsStr[5] = ((m_flags & TI_FLAG_BYREF_PERMANENT_HOME) != 0) ? 'P' : '-';
    flagsStr[6] = ((m_flags & TI_FLAG_GENERIC_TYPE_VAR) != 0) ? 'G' : '-';
    flagsStr[7] = '\0';

    printf("[%s(%X) {%s}]", tiType2Str(m_bits.type), m_cls, flagsStr);
}
#endif // VERBOSE_VERIFY
#endif // DEBUG
