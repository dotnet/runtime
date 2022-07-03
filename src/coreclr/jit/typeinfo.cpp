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

