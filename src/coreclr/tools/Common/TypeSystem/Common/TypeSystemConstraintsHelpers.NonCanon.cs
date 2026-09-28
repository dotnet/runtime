// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    public static partial class TypeSystemConstraintsHelpers
    {
        private static bool IsSpecialTypeMeetingConstraint(TypeDesc type, GenericConstraints constraint)
            => false;

        private static bool CanCastToConstraintWithCanon(TypeDesc instantiationParam, TypeDesc instantiatedConstraintType)
            => false;
    }
}
