using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
                // A recursive struct type isn't blittable.
                // It's also illegal in C#, but I believe that source generators run
                // before that is detected, so we check here to avoid a stack overflow.
                return false;
            }

            foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
            {
                if (!field.IsStatic)
                {
                    bool fieldBlittable = field switch
                    {
                        { Type: { IsReferenceType: true } } => false,
                        { Type: IPointerTypeSymbol ptr } => IsConsideredBlittable(ptr.PointedAtType),
                        { Type: IFunctionPointerTypeSymbol } => true,
                        not { Type: { SpecialType: SpecialType.None } } => IsSpecialTypeBlittable(field.Type.SpecialType),
                        // Assume that type parameters that can be blittable are blittable.
                        // We'll re-evaluate blittability for generic fields of generic types at instantation time.
                        { Type: ITypeParameterSymbol } => true,
                        { Type: { IsValueType: false } } => false,
                        _ => IsConsideredBlittable(field.Type, seenTypes.Add(type))
                    };

                    if (!fieldBlittable)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsSpecialTypeBlittable(SpecialType specialType)
         => specialType switch
         {
            SpecialType.System_Void
            or SpecialType.System_SByte
            or SpecialType.System_Byte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_IntPtr
            or SpecialType.System_UIntPtr => true,
            _ => false
         };

        public static bool IsConsideredBlittable(this ITypeSymbol type) => IsConsideredBlittable(type, ImmutableHashSet.Create<ITypeSymbol>(SymbolEqualityComparer.Default));

        private static bool IsConsideredBlittable(this ITypeSymbol type, ImmutableHashSet<ITypeSymbol> seenTypes)
        {
            if (type.SpecialType != SpecialType.None)
            {
                return IsSpecialTypeBlittable(type.SpecialType);
            }

            if (type.TypeKind is TypeKind.FunctionPointer or TypeKind.Pointer)
            {
                return true;
            }

            if (type.IsReferenceType)
            {
                return false;
            }

            if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: ITypeSymbol underlyingType })
            {
                return underlyingType.IsConsideredBlittable(seenTypes);
            }

            bool hasNativeMarshallingAttribute = false;
            bool hasGeneratedMarshallingAttribute = false;
            // [TODO]: Match attributes on full name or symbol, not just on type name.
            foreach (var attr in type.GetAttributes())
            {
                if (attr.AttributeClass is null)
                {
                    continue;
                }
                if (attr.AttributeClass.Name == "BlittableTypeAttribute")
                {
                    if (type is INamedTypeSymbol { IsGenericType: true } generic)
                    {
                        // If the type is generic, we inspect the fields again
                        // to determine blittability of this instantiation
                        // since we are guaranteed that if a type has generic fields,
                        // they will be present in the contract assembly to ensure
                        // that recursive structs can be identified at build time.
                        return generic.HasOnlyBlittableFields(seenTypes);
                    }
                    return true;
                }
                else if (attr.AttributeClass.Name == "GeneratedMarshallingAttribute")
                {
                    hasGeneratedMarshallingAttribute = true;
                }
                else if (attr.AttributeClass.Name == "NativeMarshallingAttribute")
                {
                    hasNativeMarshallingAttribute = true;
                }
            }

            if (hasGeneratedMarshallingAttribute && !hasNativeMarshallingAttribute)
            {
                // The struct type has generated marshalling via a source generator.
                // We can't guarantee that we can see the results of the struct source generator,
                // so we re-calculate if the type is blittable here.
                return type.HasOnlyBlittableFields(seenTypes);
            }

            if (type is INamedTypeSymbol namedType
                && namedType.DeclaringSyntaxReferences.Length != 0
                && !namedType.IsExposedOutsideOfCurrentCompilation())
            {
                // If a type is declared in the current compilation and not exposed outside of it,
                // we will allow it to be considered blittable if its fields are considered blittable.
                return type.HasOnlyBlittableFields(seenTypes);
            }
            return false;
        }

        public static bool IsAutoLayout(this INamedTypeSymbol type, ITypeSymbol structLayoutAttributeType)
        {
            foreach (var attr in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(structLayoutAttributeType, attr.AttributeClass))
                {
                    return (LayoutKind)(int)attr.ConstructorArguments[0].Value! == LayoutKind.Auto;
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
            return type switch
            {
                SpecialType.System_SByte
                or SpecialType.System_Byte
                or SpecialType.System_Int16
                or SpecialType.System_UInt16
                or SpecialType.System_Int32
                or SpecialType.System_UInt32
                or SpecialType.System_Int64
                or SpecialType.System_UInt64
                or SpecialType.System_IntPtr
                or SpecialType.System_UIntPtr => true,
                _ => false
            };
        }

        public static bool IsExposedOutsideOfCurrentCompilation(this INamedTypeSymbol type)
        {
            for (; type is not null; type = type.ContainingType)
            {
                Accessibility accessibility = type.DeclaredAccessibility;

                if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal or Accessibility.Private or Accessibility.Friend or Accessibility.ProtectedAndFriend)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
