// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public static partial class CastingHelper
    {
        /// <summary>
        /// Check if <paramref name="otherType"/> is a canonical type that <paramref name="thisType"/>
        /// can be cast to. __Canon accepts any reference type; __UniversalCanon accepts any type.
        /// Pointers, byrefs, and function pointers are not valid instantiation arguments.
        /// </summary>
        private static bool IsCanonicalCastTarget(TypeDesc thisType, TypeDesc otherType)
        {
            TypeSystemContext context = thisType.Context;

            if (context.IsCanonicalDefinitionType(otherType, CanonicalFormKind.Universal))
                return true;

            if (context.IsCanonicalDefinitionType(otherType, CanonicalFormKind.Specific))
                return thisType.IsGCPointer;

            return false;
        }

        /// <summary>
        /// Check if two type arguments can be considered matching because one (or both) is canonical.
        /// __Canon matches any reference type; __UniversalCanon matches any type.
        /// </summary>
        private static bool IsCanonicalTypeArgMatch(TypeDesc type, TypeDesc otherType)
        {
            TypeSystemContext context = type.Context;

            if (context.IsCanonicalDefinitionType(otherType, CanonicalFormKind.Universal))
                return true;

            if (context.IsCanonicalDefinitionType(otherType, CanonicalFormKind.Specific))
                return type.IsGCPointer || context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any);

            if (context.IsCanonicalDefinitionType(type, CanonicalFormKind.Universal))
                return true;

            if (context.IsCanonicalDefinitionType(type, CanonicalFormKind.Specific))
                return otherType.IsGCPointer || context.IsCanonicalDefinitionType(otherType, CanonicalFormKind.Any);

            // For non-leaf types (e.g., Arg2<string> vs Arg2<__Canon>), check if they are
            // canon-equivalent: same type definition with canon-compatible type arguments.
            if (IsCanonEquivalent(type, otherType))
                return true;

            // For parameterized types like arrays (string[] vs __Canon[]),
            // recursively match the element/parameter type.
            if (type is ParameterizedType paramType && otherType is ParameterizedType otherParamType
                && type.Category == otherType.Category)
            {
                if (type is ArrayType arrayType && otherType is ArrayType otherArrayType
                    && arrayType.Rank != otherArrayType.Rank)
                    return false;

                return IsCanonicalTypeArgMatch(paramType.ParameterType, otherParamType.ParameterType);
            }

            return false;
        }

        /// <summary>
        /// Check if two types are equivalent considering canonical type matching rules.
        /// Same type definition with all type arguments either equal or canon-compatible.
        /// </summary>
        private static bool IsCanonEquivalent(TypeDesc thisType, TypeDesc otherType)
        {
            if (!thisType.HasSameTypeDefinition(otherType))
                return false;

            Instantiation thisInst = thisType.Instantiation;
            Instantiation otherInst = otherType.Instantiation;

            if (thisInst.Length == 0)
                return false;

            for (int i = 0; i < thisInst.Length; i++)
            {
                if (thisInst[i] == otherInst[i])
                    continue;

                if (!IsCanonicalTypeArgMatch(thisInst[i], otherInst[i]))
                    return false;
            }

            return true;
        }
    }
}
