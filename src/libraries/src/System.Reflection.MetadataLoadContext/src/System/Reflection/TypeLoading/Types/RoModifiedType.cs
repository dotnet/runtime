// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// This is used to represent a ModifiedType. It is quite ill-behaved so the only time it is created is by the EcmaModifiedTypeProvider. 
    /// It is only used to implement the GetCustomModifiers apis.
    /// </summary>
    internal sealed class RoModifiedType : RoWrappedType
    {
        internal RoModifiedType(RoType modifier, RoType unmodifiedType, bool isRequired)
            : base(unmodifiedType)
        {
            Debug.Assert(modifier != null);

            Modifier = modifier;
            IsRequired = isRequired;
        }

        internal RoType Modifier { get; }
        internal bool IsRequired { get; }
    }
}
