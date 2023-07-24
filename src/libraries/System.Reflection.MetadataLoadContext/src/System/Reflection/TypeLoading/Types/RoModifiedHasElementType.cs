// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.TypeLoading;

namespace System.Reflection
{
    /// <summary>
    /// An array, pointer or reference type that is modified.
    /// </summary>
    internal sealed class RoModifiedHasElementType : RoModifiedType
    {
        private readonly RoModifiedType? _elementModifiedType;

        public RoModifiedHasElementType(RoType unmodifiedType) : base(unmodifiedType)
        {
            Debug.Assert(unmodifiedType.HasElementType);
            _elementModifiedType = Create(unmodifiedType.GetRoElementType()!);
        }

        internal sealed override RoType? GetRoElementType() => _elementModifiedType;
    }
}
