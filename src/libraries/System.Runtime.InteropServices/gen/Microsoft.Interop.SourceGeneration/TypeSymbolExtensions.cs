// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public static class TypeSymbolExtensions
    {
        public static bool HasOnlyBlittableFields(this ITypeSymbol type) => HasOnlyBlittableFields(type, ImmutableHashSet.Create<ITypeSymbol>(SymbolEqualityComparer.Default));

        private static bool HasOnlyBlittableFields(this ITypeSymbol type, ImmutableHashSet<ITypeSymbol> seenTypes)
        {
            if (seenTypes.Contains(type))
            {
                // A recursive struct type is illegal in C#, but source generators run before that is detected,
                // so we check here to avoid a stack overflow.
                return false;
            }

            foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.IsStatic)
                {
                    if (!IsConsideredBlittable(field.Type, seenTypes.Add(type)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsConsideredBlittable(this ITypeSymbol type) => IsConsideredBlittable(type, ImmutableHashSet.Create<ITypeSymbol>(SymbolEqualityComparer.Default));

        private static bool IsConsideredBlittable(this ITypeSymbol type, ImmutableHashSet<ITypeSymbol> seenTypes)
        {
            // Assume that type parameters that can be blittable are blittable.
            // We'll re-evaluate blittability for generic fields of generic types at instantation time.
            if (type.TypeKind == TypeKind.TypeParameter && !type.IsReferenceType)
            {
                return true;
            }
            if (!type.IsUnmanagedType || type.IsAutoLayout())
            {
                return false;
            }

            foreach (AttributeData attr in type.GetAttributes())
            {
                if (attr.AttributeClass is null)
                {
                    continue;
                }
                else if (attr.AttributeClass.ToDisplayString() == "System.Runtime.InteropServices.GeneratedMarshallingAttribute")
                {
                    // If we have generated struct marshalling,
                    // then the generated marshalling will be non-blittable when one of the fields is not unmanaged.
                    return type.HasOnlyBlittableFields(seenTypes);
                }
                else if (attr.AttributeClass.ToDisplayString() == "System.Runtime.InteropServices.NativeMarshallingAttribute")
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsAutoLayout(this ITypeSymbol type)
        {
            foreach (AttributeData attr in type.GetAttributes())
            {
                if (attr.AttributeClass.ToDisplayString() == "System.Runtime.InteropServices.StructLayoutAttribute")
                {
                    return attr.ConstructorArguments.Length == 1 && (LayoutKind)(int)attr.ConstructorArguments[0].Value! == LayoutKind.Auto;
                }
            }
            return type.IsReferenceType;
        }

        public static TypeSyntax AsTypeSyntax(this ITypeSymbol type)
        {
            return SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        public static bool IsIntegralType(this SpecialType type)
        {
            return type is SpecialType.System_SByte
                or SpecialType.System_Byte
                or SpecialType.System_Int16
                or SpecialType.System_UInt16
                or SpecialType.System_Int32
                or SpecialType.System_UInt32
                or SpecialType.System_Int64
                or SpecialType.System_UInt64
                or SpecialType.System_IntPtr
                or SpecialType.System_UIntPtr;
        }

        public static bool IsAlwaysBlittable(this SpecialType type)
        {
            return type is SpecialType.System_Void
                    or SpecialType.System_SByte
                    or SpecialType.System_Byte
                    or SpecialType.System_Int16
                    or SpecialType.System_UInt16
                    or SpecialType.System_Int32
                    or SpecialType.System_UInt32
                    or SpecialType.System_Int64
                    or SpecialType.System_UInt64
                    or SpecialType.System_IntPtr
                    or SpecialType.System_UIntPtr
                    or SpecialType.System_Single
                    or SpecialType.System_Double;
        }
    }
}
