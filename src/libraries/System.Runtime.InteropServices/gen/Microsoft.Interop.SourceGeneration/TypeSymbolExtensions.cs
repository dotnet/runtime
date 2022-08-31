// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
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
        /// Source generation uses a heavily restricted definition for strictly blittable.
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
            // We'll re-evaluate blittability for generic fields of generic types at instantiation time.
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

        public static bool IsConstructedFromEqualTypes(this ITypeSymbol type, ITypeSymbol other)
        {
            return (type, other) switch
            {
                (INamedTypeSymbol namedType, INamedTypeSymbol namedOther) => SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, namedOther.ConstructedFrom),
                _ => SymbolEqualityComparer.Default.Equals(type, other)
            };
        }

        /// <summary>
        /// Reconstruct a possibly-nested type with the generic parameters of another type, accounting for type nesting and generic parameters split between different nesting levels.
        /// </summary>
        /// <param name="instantiatedTemplateType">The generic type from which to copy type arguments</param>
        /// <param name="unboundConstructedType">The type to recursively instantiate</param>
        /// <param name="numOriginalTypeArgumentsSubstituted">How many type parameters from <c><paramref name="unboundConstructedType"/>.ConstructedFrom</c> that needed to be substituted to fill the generic parameter list.</param>
        /// <param name="extraTypeArgumentsInTemplate">How many type parameters from <paramref name="instantiatedTemplateType"/>were unused.</param>
        /// <returns>A fully constructed type based on <c><paramref name="unboundConstructedType"/>.ConstructedFrom</c> with the generic arguments from <paramref name="instantiatedTemplateType"/>.</returns>
        public static INamedTypeSymbol ResolveUnboundConstructedTypeToConstructedType(this INamedTypeSymbol unboundConstructedType, INamedTypeSymbol instantiatedTemplateType, out int numOriginalTypeArgumentsSubstituted, out int extraTypeArgumentsInTemplate)
        {
            var (typeArgumentsToSubstitute, nullableAnnotationsToSubstitute) = instantiatedTemplateType.GetAllTypeArgumentsIncludingInContainingTypes();

            // Build us a list of the type nesting of unboundConstructedType, with the outermost containing type on the top
            // Use OriginalDefinition to get the generic definition for all containing types instead of having to unconstruct the generic at each loop iteration.
            Stack<INamedTypeSymbol> originalNestedTypes = new();
            for (INamedTypeSymbol originalTypeDefinition = unboundConstructedType.OriginalDefinition; originalTypeDefinition is not null; originalTypeDefinition = originalTypeDefinition.ContainingType)
            {
                originalNestedTypes.Push(originalTypeDefinition);
            }

            numOriginalTypeArgumentsSubstituted = 0;
            int currentArityOffset = 0;
            INamedTypeSymbol currentType = null;
            while (originalNestedTypes.Count > 0)
            {
                // Get the generic type definition to work with.
                if (currentType is null)
                {
                    // If we're starting with the outermost type, we can just use that provided symbol.
                    currentType = originalNestedTypes.Pop();
                }
                else
                {
                    // If the type was nested, we need to look it up again on the (possibly constructed generic) containing type.
                    INamedTypeSymbol originalNestedType = originalNestedTypes.Pop();
                    currentType = currentType.GetTypeMembers(originalNestedType.Name, originalNestedType.Arity).First();
                }

                if (currentType.TypeParameters.Length > 0)
                {
                    // We will try to substitute as many generic parameters as possible from typeArgumentsToSubstitute and nullableAnnotationsToSubstitute.
                    // If we run out of generic arguments to substitute, we will fill the rest of the generic arguments by propogating the corresponding type parameters from the type's generic definition.
                    // This will enable us to correctly construct a generic type from a generic type definition for all scenarios.
                    //
                    // Examples:
                    //   type arguments: [A, B, C]
                    //   target generic type: X<T, U, V>
                    //   result: X<A, B, C>
                    //   arguments remaining for any nested generic types: []
                    //
                    //   type arguments: [A, B, C]
                    //   target generic type: X<T, U>
                    //   result: X<A, B>
                    //   arguments remaining for any nested generic types: [C]
                    //
                    //   type arguments: [A, B]
                    //   target generic type: X<T, U, V>
                    //   result: X<A, B, V>
                    //   arguments remaining for any nested generic types: []
                    int numArgumentsToInsert = currentType.TypeParameters.Length;
                    var arguments = new ITypeSymbol[numArgumentsToInsert];
                    var annotations = new NullableAnnotation[numArgumentsToInsert];

                    int numArgumentsToCopy = Math.Min(numArgumentsToInsert, typeArgumentsToSubstitute.Length - currentArityOffset);

                    typeArgumentsToSubstitute.CopyTo(currentArityOffset, arguments, 0, numArgumentsToCopy);
                    nullableAnnotationsToSubstitute.CopyTo(currentArityOffset, annotations, 0, numArgumentsToCopy);
                    currentArityOffset += numArgumentsToCopy;

                    if (numArgumentsToCopy != numArgumentsToInsert)
                    {
                        int numArgumentsToPropogate = numArgumentsToInsert - numArgumentsToCopy;
                        // Record how many of the original generic type parameters we needed to use as arguments.
                        // This value represents how many generic arguments the instantiatedTemplateType type would need to have the same total number of generic parameters as unboundConstructedType,
                        // including accounting for nesting.
                        numOriginalTypeArgumentsSubstituted += numArgumentsToPropogate;
                        currentType.TypeParameters.CastArray<ITypeSymbol>().CopyTo(currentType.TypeParameters.Length - numArgumentsToPropogate, arguments, numArgumentsToCopy, numArgumentsToPropogate);
                    }

                    currentType = currentType.Construct(
                        ImmutableArray.CreateRange(arguments),
                        ImmutableArray.CreateRange(annotations));
                }
            }
            // Record how many type arguments we did not need to use from instantiatedTemplateType to instantiate unboundConstructedType.
            extraTypeArgumentsInTemplate = typeArgumentsToSubstitute.Length - currentArityOffset;

            return currentType;
        }

        public static (ImmutableArray<ITypeSymbol> TypeArguments, ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations) GetAllTypeArgumentsIncludingInContainingTypes(this INamedTypeSymbol genericType)
        {
            // Get the type arguments of the passed in type and all containing types
            // with the outermost type on the top of the stack and the innermost type on the bottom of the stack.
            Stack<(ImmutableArray<ITypeSymbol>, ImmutableArray<NullableAnnotation>)> genericTypesToSubstitute = new();
            for (INamedTypeSymbol instantiatedType = genericType; instantiatedType is not null; instantiatedType = instantiatedType.ContainingType)
            {
                genericTypesToSubstitute.Push((instantiatedType.TypeArguments, instantiatedType.TypeArgumentNullableAnnotations));
            }
            // Turn our stack of lists of type arguments into one list,
            // going from the first type argument of the outermost type to the last type argument of the innermost type.
            ImmutableArray<ITypeSymbol>.Builder typeArguments = ImmutableArray.CreateBuilder<ITypeSymbol>();
            ImmutableArray<NullableAnnotation>.Builder nullableAnnotations = ImmutableArray.CreateBuilder<NullableAnnotation>();
            while (genericTypesToSubstitute.Count != 0)
            {
                var (args, annotations) = genericTypesToSubstitute.Pop();
                typeArguments.AddRange(args);
                nullableAnnotations.AddRange(annotations);
            }
            return (typeArguments.ToImmutable(), nullableAnnotations.ToImmutable());
        }
    }
}
