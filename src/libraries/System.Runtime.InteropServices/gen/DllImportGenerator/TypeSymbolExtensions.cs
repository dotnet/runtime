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
    static class TypeSymbolExtensions
    {
        public static bool HasOnlyBlittableFields(this ITypeSymbol type)
        {
            foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
            {
                bool? fieldBlittable = field switch
                {
                    { IsStatic : true } => null,
                    { Type : { IsReferenceType : true }} => false,
                    { Type : IPointerTypeSymbol ptr } => IsConsideredBlittable(ptr.PointedAtType),
                    { Type : IFunctionPointerTypeSymbol } => true,
                    not { Type : { SpecialType : SpecialType.None }} => IsSpecialTypeBlittable(field.Type.SpecialType),
                    // Assume that type parameters that can be blittable are blittable.
                    // We'll re-evaluate blittability for generic fields of generic types at instantation time.
                    { Type : ITypeParameterSymbol } => true,
                    { Type : { IsValueType : false }} => false,
                    // A recursive struct type isn't blittable.
                    // It's also illegal in C#, but I believe that source generators run
                    // before that is detected, so we check here to avoid a stack overflow.
                    // [TODO]: Handle mutual recursion.
                    _ => !SymbolEqualityComparer.Default.Equals(field.Type, type) && IsConsideredBlittable(field.Type)
                };

                if (fieldBlittable is false)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSpecialTypeBlittable(SpecialType specialType)
         => specialType switch
         {
            SpecialType.System_SByte
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

        public static bool IsConsideredBlittable(this ITypeSymbol type)
        {
            if (type.SpecialType != SpecialType.None)
            {
                return IsSpecialTypeBlittable(type.SpecialType);
            }

            if (type.TypeKind == TypeKind.FunctionPointer)
            {
                return true;
            }

            if (!type.IsValueType || type.IsReferenceType)
            {
                return false;
            }

            if (type is IPointerTypeSymbol { PointedAtType: ITypeSymbol pointedAtType })
            {
                return pointedAtType.IsConsideredBlittable();
            }

            if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: ITypeSymbol underlyingType })
            {
                return underlyingType!.IsConsideredBlittable();
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
                        return generic.HasOnlyBlittableFields();
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
                return type.HasOnlyBlittableFields();
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

        public static bool IsIntegralType(this ITypeSymbol type)
        {
            return type.SpecialType switch
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
    }
}
