// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base type for RoModifiedType and RoPinnedType. These types are very ill-behaved so they are only produced in very specific circumstances
    /// and quickly peeled away once their usefulness has ended.
    /// </summary>
    internal abstract class RoWrappedType : RoStubType
    {
        internal RoWrappedType(RoType unmodifiedType)
        {
            Debug.Assert(unmodifiedType != null);
            UnmodifiedType = unmodifiedType;
        }

        internal RoType UnmodifiedType { get; }
    }
}
