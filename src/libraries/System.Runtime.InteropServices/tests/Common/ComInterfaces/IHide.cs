// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("023EA72A-ECAA-4B65-9D96-2122CFADE16C")]
    internal partial interface IHide
    {
        int SameMethod();
        int DifferentMethod();
    }

    [GeneratedComInterface]
    [Guid("5293B3B1-4994-425C-803E-A21A5011E077")]
    internal partial interface IHide2 : IHide
    {
        new int SameMethod();
        int DifferentMethod2();
    }

    internal interface UnrelatedInterfaceWithSameMethod
    {
        int SameMethod();
        int DifferentMethod3();
    }

    [GeneratedComInterface]
    [Guid("5DD35432-4987-488D-94F1-7682D7E4405C")]
    internal partial interface IHide3 : IHide2, UnrelatedInterfaceWithSameMethod
    {
        new int SameMethod();
        new int DifferentMethod3();
    }

    [GeneratedComClass]
    [Guid("2D36BD6D-C80E-4F00-86E9-8D1B4A0CB59A")]
    /// <summary>
    /// Implements IHides3 and returns the expected VTable index for each method.
    /// </summary>
    internal partial class HideBaseMethods : IHide3
    {
        int IHide.SameMethod() => 3;
        int IHide.DifferentMethod() => 4;
        int IHide2.SameMethod() => 5;
        int IHide2.DifferentMethod2() => 6;
        int IHide3.SameMethod() => 7;
        int IHide3.DifferentMethod3() => 8;
        int UnrelatedInterfaceWithSameMethod.SameMethod() => -1;
        int UnrelatedInterfaceWithSameMethod.DifferentMethod3() => -1;
    }
}
