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
        /// <summary>
        /// Is the type blittable according to source generator invariants.
        /// </summary>
        /// <remarks>
        /// Source generation attempts to reconcile the notion of blittability
        /// with the C# notion of an "unmanaged" type. This can be accomplished through
        /// the use of DisableRuntimeMarshallingAttribute.
        /// </remarks>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true if considered blittable, otherwise false.</returns>
        public static bool IsConsideredBlittable(this ITypeSymbol type)
        {
            unsafe
            {
                return IsBlittableWorker(type, ImmutableHashSet.Create<ITypeSymbol>(SymbolEqualityComparer.Default), &IsConsideredBlittableWorker);
            }

            static bool IsConsideredBlittableWorker(ITypeSymbol t, ImmutableHashSet<ITypeSymbol> seenTypes)
            {
                return t.IsUnmanagedType;
            }
        }

        /// <summary>
        /// Is the type strictly blittable.
        /// </summary>
        /// <remarks>
        /// Source generation uses a heavily restricted defintion for strictly blittable.
        /// The definition is based on the built-in marshallers blittable definition but further
        /// restricts the definition to require only uses primitive types (not including char or bool)
        /// and do types defined in the source being compiled.
        /// </remarks>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns true if strictly blittable, otherwise false.</returns>
        public static bool IsStrictlyBlittable(this ITypeSymbol type)
        {
            unsafe
            {
                return IsBlittableWorker(type, ImmutableHashSet.Create<ITypeSymbol>(SymbolEqualityComparer.Default), &IsStrictlyBlittableWorker);
            }

            static unsafe bool IsStrictlyBlittableWorker(ITypeSymbol t, ImmutableHashSet<ITypeSymbol> seenTypes)
            {
                if (t.SpecialType is not SpecialType.None)
                {
                    return t.SpecialType.IsAlwaysBlittable();
                }
                else if (t.IsValueType)
                {
                    // If the containing assembly for the type is backed by metadata (non-null),
                    // then the type is not internal and therefore coming from a reference assembly
                    // that we can not confirm is strictly blittable.
                    if (t.ContainingAssembly is not null
                        && t.ContainingAssembly.GetMetadata() is not null)
                    {
                        return false;
                    }

                    return t.HasOnlyBlittableFields(seenTypes, &IsStrictlyBlittableWorker);
                }

                return false;
            }
        }

        private static unsafe bool IsBlittableWorker(this ITypeSymbol type, ImmutableHashSet<ITypeSymbol> seenTypes, delegate*<ITypeSymbol, ImmutableHashSet<ITypeSymbol>, bool> isBlittable)
        {
            // Assume that type parameters that can be blittable are blittable.
            // We'll re-evaluate blittability for generic fields of generic types at instantation time.
            if (type.TypeKind == TypeKind.TypeParameter && !type.IsReferenceType)
            {
                return true;
            }
            if (type.IsAutoLayout() || !isBlittable(type, seenTypes))
            {
                return false;
            }

            foreach (AttributeData attr in type.GetAttributes())
            {
                if (attr.AttributeClass is null)
                {
                    continue;
                }
                else if (attr.AttributeClass.ToDisplayString() == "System.Runtime.InteropServices.NativeMarshallingAttribute")
                {
                    // Types marked with NativeMarshallingAttribute require marshalling by definition.
                    return false;
                }
            }
            return true;
        }

        private static bool IsAutoLayout(this ITypeSymbol type)
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

        private static unsafe bool HasOnlyBlittableFields(this ITypeSymbol type, ImmutableHashSet<ITypeSymbol> seenTypes, delegate*<ITypeSymbol, ImmutableHashSet<ITypeSymbol>, bool> isBlittable)
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
                    if (!IsBlittableWorker(field.Type, seenTypes.Add(type), isBlittable))
                    {
                        return false;
                    }
                }
            }

            return true;
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
