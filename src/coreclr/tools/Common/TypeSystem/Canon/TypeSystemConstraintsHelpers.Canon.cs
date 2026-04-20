// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        /// <paramref name="instantiatedConstraintType"/> when the constraint contains canonical types.
        /// Canonical types act as wildcards: __Canon matches any reference type,
        /// __UniversalCanon matches any type.
        /// </summary>
        private static bool CanCastToConstraintWithCanon(TypeDesc instantiationParam, TypeDesc instantiatedConstraintType)
        {
            TypeSystemContext context = instantiationParam.Context;

            // If the instantiation param is a canonical definition type (__Canon or __UniversalCanon),
            // it acts as a wildcard — any concrete type substituted at runtime will be validated then.
            // The special constraints (class, struct, new()) are already handled separately;
            // for type constraints we optimistically accept since we can't know the concrete type.
            if (context.IsCanonicalDefinitionType(instantiationParam, CanonicalFormKind.Any))
                return true;

            // If the constraint type itself is a canonical definition type, check compatibility directly.
            // E.g., "where T : U" with U=__Canon means T must be a plausible match for __Canon.
            if (context.IsCanonicalDefinitionType(instantiatedConstraintType, CanonicalFormKind.Universal))
                return true;
            if (context.IsCanonicalDefinitionType(instantiatedConstraintType, CanonicalFormKind.Specific))
                return instantiationParam.IsGCPointer;

            if (!instantiatedConstraintType.IsCanonicalSubtype(CanonicalFormKind.Any))
                return false;

            if (instantiatedConstraintType.IsInterface)
            {
                if (IsCanonCompatibleMatch(instantiationParam, instantiatedConstraintType))
                    return true;

                foreach (var iface in instantiationParam.RuntimeInterfaces)
                {
                    if (IsCanonCompatibleMatch(iface, instantiatedConstraintType))
                        return true;
                }
            }
            else
            {
                TypeDesc curType = instantiationParam;
                while (curType is not null)
                {
                    if (IsCanonCompatibleMatch(curType, instantiatedConstraintType))
                        return true;
                    curType = curType.BaseType;
                }
            }

            return false;
        }

        private static bool IsCanonCompatibleMatch(TypeDesc concreteType, TypeDesc canonType)
        {
            if (!concreteType.HasSameTypeDefinition(canonType))
                return false;

            Instantiation concreteInst = concreteType.Instantiation;
            Instantiation canonInst = canonType.Instantiation;
            Instantiation openInst = concreteType.GetTypeDefinition().Instantiation;

            for (int i = 0; i < concreteInst.Length; i++)
            {
                TypeDesc concreteArg = concreteInst[i];
                TypeDesc canonArg = canonInst[i];

                if (concreteArg == canonArg)
                    continue;

                if (IsCanonicalTypeArgMatch(concreteArg, canonArg))
                    continue;

                // Neither exact nor canon match — check if variance allows it
                GenericVariance variance = ((GenericParameterDesc)openInst[i]).Variance;
                switch (variance)
                {
                    case GenericVariance.Covariant:
                        if (!concreteArg.CanCastTo(canonArg))
                            return false;
                        break;

                    case GenericVariance.Contravariant:
                        if (!canonArg.CanCastTo(concreteArg))
                            return false;
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }

        private static bool IsCanonicalTypeArgMatch(TypeDesc concreteArg, TypeDesc canonArg)
        {
            TypeSystemContext context = canonArg.Context;

            // Leaf canonical definition type checks (__Canon, __UniversalCanon themselves)
            if (context.IsCanonicalDefinitionType(canonArg, CanonicalFormKind.Universal))
                return true;

            if (context.IsCanonicalDefinitionType(canonArg, CanonicalFormKind.Specific))
                return concreteArg.IsGCPointer || context.IsCanonicalDefinitionType(concreteArg, CanonicalFormKind.Any);

            if (context.IsCanonicalDefinitionType(concreteArg, CanonicalFormKind.Universal))
                return true;

            if (context.IsCanonicalDefinitionType(concreteArg, CanonicalFormKind.Specific))
                return canonArg.IsGCPointer || context.IsCanonicalDefinitionType(canonArg, CanonicalFormKind.Any);

            // For parameterized types containing canonical components (e.g., __Canon[] vs string[],
            // or List<__Canon> vs List<string>), canonicalize both sides and compare.
            // Guard: at least one side must actually contain canonical types to avoid false matches
            // (e.g., string and object both canonicalize to __Canon but aren't interchangeable).
            if (concreteArg.IsCanonicalSubtype(CanonicalFormKind.Any) || canonArg.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                if (concreteArg.ConvertToCanonForm(CanonicalFormKind.Specific) == canonArg.ConvertToCanonForm(CanonicalFormKind.Specific))
                    return true;

                // Specific canonicalization doesn't affect __UniversalCanon (a value type).
                // When Universal canon is involved, fall back to Universal canonicalization
                // where all types collapse to __UniversalCanon.
                if (concreteArg.IsCanonicalSubtype(CanonicalFormKind.Universal) || canonArg.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    return concreteArg.ConvertToCanonForm(CanonicalFormKind.Universal) == canonArg.ConvertToCanonForm(CanonicalFormKind.Universal);
            }

            return false;
        }
    }
}
