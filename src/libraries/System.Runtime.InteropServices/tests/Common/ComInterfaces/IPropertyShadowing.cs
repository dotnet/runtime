// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("92541FF7-D3EE-4797-A691-638D89C3C9F6")]
    internal partial interface IPropertyShadowingBase
    {
        int Shadowed { get; set; }

        int NotShadowed { get; set; }
    }

    [GeneratedComInterface]
    [Guid("96AC9769-EF1B-4A91-A6FC-B59770A3CA05")]
    internal partial interface IPropertyShadowingDerived : IPropertyShadowingBase
    {
        new int Shadowed { get; set; }
    }

    [GeneratedComClass]
    [Guid("E51ACEC5-A7FA-4572-94FC-08B5F54405EE")]
    /// <summary>
    /// Implements <see cref="IPropertyShadowingDerived"/> with explicit interface
    /// implementations so that the base and derived <c>Shadowed</c> property accessors
    /// write to distinct backing fields. This lets tests verify that each property
    /// occupies its own vtable slot and that QI navigation routes correctly.
    /// </summary>
    internal partial class PropertyShadowingImpl : IPropertyShadowingDerived
    {
        private int _baseShadowed;
        private int _derivedShadowed;
        private int _notShadowed;

        int IPropertyShadowingBase.Shadowed
        {
            get => _baseShadowed;
            set => _baseShadowed = value;
        }

        int IPropertyShadowingDerived.Shadowed
        {
            get => _derivedShadowed;
            set => _derivedShadowed = value;
        }

        public int NotShadowed
        {
            get => _notShadowed;
            set => _notShadowed = value;
        }

        internal int BaseShadowedSink => _baseShadowed;

        internal int DerivedShadowedSink => _derivedShadowed;
    }
}
