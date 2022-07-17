// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Interop.Analyzers
{
    internal static class SyntaxGeneratorExtensions
    {
        public static SyntaxNode GetEnumValueAsFlagsExpression(this SyntaxGenerator gen, ITypeSymbol enumType, object value, bool includeZeroValueFlags)
        {
            if (enumType.TypeKind != TypeKind.Enum)
            {
                throw new ArgumentException(nameof(enumType));
            }

            SpecialType underlyingType = ((INamedTypeSymbol)enumType).EnumUnderlyingType.SpecialType;

            if (!underlyingType.IsIntegralType())
            {
                return gen.CastExpression(gen.TypeExpression(underlyingType), gen.LiteralExpression(value));
            }

            ulong valueToMatch = GetNumericValue(value);

            ulong currentlyMatchedFlags = 0;
            SyntaxNode? enumValueSyntax = null;
            foreach (ISymbol member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } enumValue)
                {
                    ulong fieldNumericValue = GetNumericValue(enumValue.ConstantValue);
                    if (fieldNumericValue == 0 && !includeZeroValueFlags)
                    {
                        continue;
                    }
                    if ((fieldNumericValue & valueToMatch) == fieldNumericValue)
                    {
                        currentlyMatchedFlags |= fieldNumericValue;
                        enumValueSyntax = enumValueSyntax is null
                            ? gen.MemberAccessExpression(gen.TypeExpression(enumType), enumValue.Name)
                            : gen.BitwiseOrExpression(enumValueSyntax, gen.MemberAccessExpression(gen.TypeExpression(enumType), enumValue.Name));
                    }
                }
            }

            // Unable to represent the value as the enum flags. Just use the literal value cast as the enum type.
            if (currentlyMatchedFlags != valueToMatch)
            {
                return gen.CastExpression(gen.TypeExpression(underlyingType), gen.LiteralExpression(value));
            }

            return enumValueSyntax;

            static ulong GetNumericValue(object value) => value switch
            {
                byte or ushort or uint or ulong => Convert.ToUInt64(value),
                sbyte or short or int or long => (ulong)Convert.ToInt64(value),
                _ => throw new UnreachableException()
            };
        }
    }
}
