// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    public static partial class TypeSystemConstraintsHelpers
    {
        private static bool IsSpecialTypeMeetingConstraint(TypeDesc type, GenericConstraints constraint)
        {
            TypeSystemContext context = type.Context;

            return constraint switch
            {
                GenericConstraints.ReferenceTypeConstraint => context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any),
                GenericConstraints.DefaultConstructorConstraint => context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any),
                GenericConstraints.NotNullableValueTypeConstraint => context.IsCanonicalDefinitionType(type, CanonicalFormKind.Universal),
                _ => throw new UnreachableException()
            };
        }

        /// <summary>
        /// Checks if <paramref name="instantiationParam"/> can satisfy the type constraint
        /// <paramref name="instantiatedConstraintType"/> when the param or constraint IS a canonical
        /// definition type (__Canon or __UniversalCanon). Handles wildcard semantics only;
        /// structural matching (interface walking, base chain, variance) is in CastingHelper.
        /// </summary>
        private static bool CanCastToConstraintWithCanon(TypeDesc instantiationParam, TypeDesc instantiatedConstraintType)
        {
            TypeSystemContext context = instantiationParam.Context;

            // If the instantiation param is a canonical definition type (__Canon or __UniversalCanon),
            // it acts as a wildcard — any concrete type substituted at runtime will be validated then.
            if (context.IsCanonicalDefinitionType(instantiationParam, CanonicalFormKind.Any))
                return true;

            // If the constraint type itself is a canonical definition type, check compatibility directly.
            // E.g., "where T : U" with U=__Canon means T must be a plausible match for __Canon.
            if (context.IsCanonicalDefinitionType(instantiatedConstraintType, CanonicalFormKind.Universal))
                return true;
            if (context.IsCanonicalDefinitionType(instantiatedConstraintType, CanonicalFormKind.Specific))
                return instantiationParam.IsGCPointer;

            return false;
        }
    }
}
