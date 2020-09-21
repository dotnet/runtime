// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: WeakReferenceNative.h
**
**
===========================================================*/

#ifndef _WEAKREFERENCENATIVE_H
#define _WEAKREFERENCENATIVE_H

//
// The implementations of WeakReferenceNative and WeakReferenceOfTNative are identical, but the managed signatures
// are different. WeakReferenceOfTNative has strongly typed signatures. It is necessary for correct security transparancy
// annotations without compromising inlining (security critical code cannot be inlined into security neutral code).
//

class WeakReferenceNative
{
public:
    static FCDECL3(void, Create, WeakReferenceObject * pThis, Object * pTarget, CLR_BOOL trackResurrection);
    static FCDECL1(void, Finalize, WeakReferenceObject * pThis);
    static FCDECL1(Object *, GetTarget, WeakReferenceObject * pThis);
    static FCDECL2(void, SetTarget, WeakReferenceObject * pThis, Object * pTarget);
    static FCDECL1(FC_BOOL_RET, IsTrackResurrection, WeakReferenceObject * pThis);
    static FCDECL1(FC_BOOL_RET, IsAlive, WeakReferenceObject * pThis);
};

class WeakReferenceOfTNative
{
public:
    static FCDECL3(void, Create, WeakReferenceObject * pThis, Object * pTarget, CLR_BOOL trackResurrection);
    static FCDECL1(void, Finalize, WeakReferenceObject * pThis);
    static FCDECL1(Object *, GetTarget, WeakReferenceObject * pThis);
    static FCDECL2(void, SetTarget, WeakReferenceObject * pThis, Object * pTarget);
    static FCDECL1(FC_BOOL_RET, IsTrackResurrection, WeakReferenceObject * pThis);
};

#endif // _WEAKREFERENCENATIVE_H
