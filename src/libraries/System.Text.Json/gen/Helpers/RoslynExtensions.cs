// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    internal static class RoslynExtensions
    {
        private static readonly Func<ITypeSymbol, bool>? s_isClosedTypeAccessor = CreateIsClosedTypeAccessor();

        public static LanguageVersion? GetLanguageVersion(this Compilation compilation)
            => compilation is CSharpCompilation csc ? csc.LanguageVersion : null;

        public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, Type type)
        {
            Debug.Assert(!type.IsArray, "Resolution logic only capable of handling named types.");
            Debug.Assert(type.FullName != null);
            return compilation.GetBestTypeByMetadataName(type.FullName);
        }

        public static Location? GetLocation(this ISymbol typeSymbol)
            => typeSymbol.Locations.Length > 0 ? typeSymbol.Locations[0] : null;

        public static Location? GetLocation(this AttributeData attributeData)
        {
            SyntaxReference? reference = attributeData.ApplicationSyntaxReference;
            return reference?.SyntaxTree.GetLocation(reference.Span);
        }

        /// <summary>
        /// Returns true if the specified location is contained in one of the syntax trees in the compilation.
        /// </summary>
        public static bool ContainsLocation(this Compilation compilation, Location location)
            => location.SourceTree != null && compilation.ContainsSyntaxTree(location.SourceTree);

        /// <summary>
        /// Removes any type metadata that is erased at compile time, such as NRT annotations and tuple labels.
        /// </summary>
        public static ITypeSymbol EraseCompileTimeMetadata(this Compilation compilation, ITypeSymbol type)
        {
            if (type.NullableAnnotation is NullableAnnotation.Annotated)
            {
                type = type.WithNullableAnnotation(NullableAnnotation.None);
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                ITypeSymbol elementType = compilation.EraseCompileTimeMetadata(arrayType.ElementType);
                return compilation.CreateArrayTypeSymbol(elementType, arrayType.Rank);
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsTupleType)
                {
                    if (namedType.TupleElements.Length < 2)
                    {
                        return type;
                    }

                    ImmutableArray<ITypeSymbol> erasedElements = namedType.TupleElements
                        .Select(e => compilation.EraseCompileTimeMetadata(e.Type))
                        .ToImmutableArray();

                    type = compilation.CreateTupleTypeSymbol(erasedElements);
                }
                else if (namedType.IsGenericType)
                {
                    if (namedType.IsUnboundGenericType)
                    {
                        return namedType;
                    }

                    ImmutableArray<ITypeSymbol> typeArguments = namedType.TypeArguments;
                    INamedTypeSymbol? containingType = namedType.ContainingType;

                    if (containingType?.IsGenericType == true)
                    {
                        containingType = (INamedTypeSymbol)compilation.EraseCompileTimeMetadata(containingType);
                        type = namedType = containingType.GetTypeMembers().First(t => t.Name == namedType.Name && t.Arity == namedType.Arity);
                    }

                    if (typeArguments.Length > 0)
                    {
                        ITypeSymbol[] erasedTypeArgs = typeArguments
                            .Select(compilation.EraseCompileTimeMetadata)
                            .ToArray();

                        type = namedType.ConstructedFrom.Construct(erasedTypeArgs);
                    }
                }
            }

            return type;
        }

        public static bool CanUseDefaultConstructorForDeserialization(this ITypeSymbol type, out IMethodSymbol? constructorInfo)
        {
            if (type.IsAbstract || type.TypeKind is TypeKind.Interface || type is not INamedTypeSymbol namedType)
            {
                constructorInfo = null;
                return false;
            }

            constructorInfo = namedType.GetExplicitlyDeclaredInstanceConstructors().FirstOrDefault(ctor => ctor.DeclaredAccessibility is Accessibility.Public && ctor.Parameters.Length == 0);
            return constructorInfo != null || type.IsValueType;
        }

        public static IEnumerable<IMethodSymbol> GetExplicitlyDeclaredInstanceConstructors(this INamedTypeSymbol type)
            => type.Constructors.Where(ctor => !ctor.IsStatic && !(ctor.IsImplicitlyDeclared && type.IsValueType && ctor.Parameters.Length == 0));

        public static bool ContainsAttribute(this ISymbol memberInfo, INamedTypeSymbol? attributeType)
            => attributeType != null && memberInfo.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType));

        public static bool IsVirtual(this ISymbol symbol)
            => symbol.IsVirtual || symbol.IsOverride || symbol.IsAbstract;

        public static bool IsAssignableFrom(this ITypeSymbol? baseType, ITypeSymbol? type)
        {
            if (baseType is null || type is null)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(baseType, type))
            {
                return true;
            }

            if (baseType.TypeKind is TypeKind.Interface)
            {
                if (type.AllInterfaces.Contains(baseType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            for (INamedTypeSymbol? current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, current))
                {
                    return true;
                }
            }

            return false;
        }

        public static INamedTypeSymbol? GetCompatibleGenericBaseType(this ITypeSymbol type, INamedTypeSymbol? baseType)
        {
            if (baseType is null)
            {
                return null;
            }

            return type.GetCompatibleGenericBaseTypes(baseType).FirstOrDefault();
        }

        /// <summary>
        /// Enumerates every ancestor of <paramref name="type"/> whose original definition matches
        /// <paramref name="baseTypeDefinition"/>. For interface bases this yields every implementing
        /// instantiation (a type can implement the same interface definition with different type
        /// arguments); for class bases it yields at most the first match found while walking the
        /// base-type chain (only one such instantiation is reachable).
        ///
        /// IMPORTANT: This implementation mirrors
        /// <c>System.Text.Json.Reflection.ReflectionExtensions.GetMatchingGenericBaseTypes</c> in
        /// src/System/ReflectionExtensions.cs. Any change to the enumeration order or matching
        /// rules MUST be applied on both sides to keep reflection and source-gen behaviour in sync.
        /// </summary>
        public static IEnumerable<INamedTypeSymbol> GetCompatibleGenericBaseTypes(this ITypeSymbol type, INamedTypeSymbol baseTypeDefinition)
        {
            Debug.Assert(baseTypeDefinition.IsGenericTypeDefinition());

            if (baseTypeDefinition.TypeKind is TypeKind.Interface)
            {
                foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                {
                    if (IsMatchingGenericType(interfaceType, baseTypeDefinition))
                    {
                        yield return interfaceType;
                    }
                }

                // Note: do NOT yield break here. `AllInterfaces` does not include `type` itself,
                // so when `type` IS the interface we're looking for, the fall-through to the
                // BaseType walk below picks it up via the self-check on the first iteration
                // (interface symbols have a null BaseType, so the loop terminates immediately).
            }

            for (INamedTypeSymbol? current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
            {
                if (IsMatchingGenericType(current, baseTypeDefinition))
                {
                    yield return current;
                    yield break;
                }
            }

            static bool IsMatchingGenericType(INamedTypeSymbol candidate, INamedTypeSymbol baseType)
            {
                return candidate.IsGenericType && SymbolEqualityComparer.Default.Equals(candidate.ConstructedFrom, baseType);
            }
        }

        /// <summary>
        /// Returns the full set of type parameters that must be bound to construct
        /// <paramref name="typeDef"/>: the type parameters of every enclosing type
        /// (outermost first) followed by the type parameters declared on
        /// <paramref name="typeDef"/> itself.
        /// </summary>
        public static List<ITypeParameterSymbol> GetAllTypeParameters(this INamedTypeSymbol typeDef)
        {
            var result = new List<ITypeParameterSymbol>();
            AppendEnclosing(typeDef.ContainingType, result);
            result.AddRange(typeDef.TypeParameters);
            return result;

            static void AppendEnclosing(INamedTypeSymbol? enclosing, List<ITypeParameterSymbol> list)
            {
                if (enclosing is null)
                {
                    return;
                }

                AppendEnclosing(enclosing.ContainingType, list);
                list.AddRange(enclosing.TypeParameters);
            }
        }

        /// <summary>
        /// Attempts to unify a <paramref name="pattern"/> type (which may contain type-parameter
        /// references) with a <paramref name="target"/> type, recording bindings in
        /// <paramref name="substitution"/>. Returns <see langword="true"/> if the pattern matches
        /// the target under some extension of the current substitution.
        ///
        /// IMPORTANT: This implementation MIRRORS
        /// <c>System.Text.Json.Reflection.ReflectionExtensions.TryUnifyWith</c> in
        /// src/System/ReflectionExtensions.cs. Any structural change (e.g. new type-kind handling,
        /// refined array/pointer rules) MUST be applied on both sides to keep reflection and
        /// source-gen behaviour in sync. The two implementations are exercised by:
        ///   * tests/.../PolymorphicTests.CustomTypeHierarchies.cs (reflection)
        ///   * tests/.../JsonSourceGeneratorDiagnosticsTests.cs (source-gen)
        ///
        /// Known intentional asymmetries with the reflection mirror:
        ///   * Reflection distinguishes SZ arrays (<c>T[]</c>) from rank-1 multi-dimensional
        ///     arrays (<c>T[*]</c>) via <c>Type.IsSZArray</c>. Roslyn surfaces both as
        ///     <see cref="IArrayTypeSymbol"/> with <c>Rank == 1</c>, and C# <c>typeof()</c> syntax
        ///     only produces SZ arrays in attribute arguments, so this asymmetry is unobservable
        ///     from source.
        ///   * Reflection has a branch for <c>Type.IsByRef</c>; Roslyn never surfaces ref types
        ///     as generic arguments, so the branch has no source-gen counterpart.
        /// </summary>
        public static bool TryUnifyWith(this ITypeSymbol pattern, ITypeSymbol target, IDictionary<ITypeParameterSymbol, ITypeSymbol> substitution)
        {
            if (pattern is ITypeParameterSymbol patternParam)
            {
                if (substitution.TryGetValue(patternParam, out ITypeSymbol? existing))
                {
                    return SymbolEqualityComparer.Default.Equals(existing, target);
                }

                substitution[patternParam] = target;
                return true;
            }

            if (pattern is IArrayTypeSymbol patternArray)
            {
                return target is IArrayTypeSymbol targetArray
                    && patternArray.Rank == targetArray.Rank
                    && patternArray.ElementType.TryUnifyWith(targetArray.ElementType, substitution);
            }

            if (pattern is IPointerTypeSymbol patternPointer)
            {
                return target is IPointerTypeSymbol targetPointer
                    && patternPointer.PointedAtType.TryUnifyWith(targetPointer.PointedAtType, substitution);
            }

            if (pattern is INamedTypeSymbol { IsGenericType: true } patternNamed)
            {
                if (target is not INamedTypeSymbol { IsGenericType: true } targetNamed)
                {
                    return false;
                }

                if (!SymbolEqualityComparer.Default.Equals(patternNamed.OriginalDefinition, targetNamed.OriginalDefinition))
                {
                    return false;
                }

                // Walk ContainingType to mirror reflection's Type.GetGenericArguments() flattening
                // behaviour for nested generic types. For example, for Outer<int>.Box<T>, Roslyn
                // surfaces the enclosing 'int' on ContainingType.TypeArguments while
                // Type.GetGenericArguments() flattens enclosing + leaf into [int, T]. Without this
                // recursion, the source-gen resolver would (a) miss enclosing-type mismatches
                // (e.g. unify Outer<int>.Box<T> with Outer<string>.Box<int>, false accept), and
                // (b) fail to bind type parameters that only appear in the enclosing type
                // (e.g. Outer<T>.Box<int> against Outer<string>.Box<int>, false reject).
                INamedTypeSymbol? patternContaining = patternNamed.ContainingType;
                INamedTypeSymbol? targetContaining = targetNamed.ContainingType;
                if (patternContaining is not null && targetContaining is not null &&
                    !patternContaining.TryUnifyWith(targetContaining, substitution))
                {
                    return false;
                }

                ImmutableArray<ITypeSymbol> patternArgs = patternNamed.TypeArguments;
                ImmutableArray<ITypeSymbol> targetArgs = targetNamed.TypeArguments;
                if (patternArgs.Length != targetArgs.Length)
                {
                    return false;
                }

                for (int i = 0; i < patternArgs.Length; i++)
                {
                    if (!patternArgs[i].TryUnifyWith(targetArgs[i], substitution))
                    {
                        return false;
                    }
                }

                return true;
            }

            return SymbolEqualityComparer.Default.Equals(pattern, target);
        }

        /// <summary>
        /// Returns the type that results from applying <paramref name="substitution"/> to every
        /// type-parameter reference inside <paramref name="type"/>. Generic types and array types
        /// are rebuilt recursively; other types are returned unchanged. For nested generic types,
        /// the substitution is also applied to the containing type so that type parameters
        /// declared on the enclosing type are correctly rebound.
        /// </summary>
        public static ITypeSymbol SubstituteTypeParameters(this Compilation compilation, ITypeSymbol type, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitution)
        {
            if (type is ITypeParameterSymbol param)
            {
                return substitution.TryGetValue(param, out ITypeSymbol? mapped) ? mapped : type;
            }

            if (type is INamedTypeSymbol { IsGenericType: true } named)
            {
                // Walk ContainingType so substitutions can reach type parameters declared on
                // enclosing generic types (e.g. T in Outer<T>.Box<int> when substitution maps
                // T to string). Without this, the leaf would be rebuilt unchanged because
                // TypeArguments is leaf-only and OriginalDefinition.Construct(...) discards the
                // enclosing instantiation. Mirrors the reflection-side recursion through
                // Type.GetGenericArguments() / MakeGenericType which flatten enclosing+leaf.
                INamedTypeSymbol? containingType = named.ContainingType;
                INamedTypeSymbol? substitutedContaining = null;
                bool containingChanged = false;
                if (containingType is { IsGenericType: true })
                {
                    substitutedContaining = (INamedTypeSymbol)compilation.SubstituteTypeParameters(containingType, substitution);
                    containingChanged = !SymbolEqualityComparer.Default.Equals(substitutedContaining, containingType);
                }

                ImmutableArray<ITypeSymbol> args = named.TypeArguments;
                ITypeSymbol[]? newArgs = null;
                for (int i = 0; i < args.Length; i++)
                {
                    ITypeSymbol substituted = compilation.SubstituteTypeParameters(args[i], substitution);
                    if (!SymbolEqualityComparer.Default.Equals(substituted, args[i]))
                    {
                        newArgs ??= args.ToArray();
                        newArgs[i] = substituted;
                    }
                }

                if (newArgs is null && !containingChanged)
                {
                    return type;
                }

                ITypeSymbol[] leafArgs = newArgs ?? args.ToArray();

                if (substitutedContaining is null)
                {
                    return named.OriginalDefinition.Construct(leafArgs);
                }

                // Locate the nested definition inside the substituted containing type and
                // construct it with the substituted leaf args.
                INamedTypeSymbol nestedDef = substitutedContaining
                    .GetTypeMembers(named.Name, leafArgs.Length)
                    .Single(t => SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, named.OriginalDefinition));
                return leafArgs.Length == 0 ? nestedDef : nestedDef.Construct(leafArgs);
            }

            if (type is IArrayTypeSymbol array)
            {
                ITypeSymbol substituted = compilation.SubstituteTypeParameters(array.ElementType, substitution);
                return SymbolEqualityComparer.Default.Equals(substituted, array.ElementType)
                    ? type
                    : compilation.CreateArrayTypeSymbol(substituted, array.Rank);
            }

            return type;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="arg"/> satisfies a
        /// <c>where T : new()</c> constraint — i.e. it is a value type, or a non-abstract,
        /// non-static reference type with an accessible public parameterless constructor.
        /// </summary>
        public static bool SatisfiesNewConstraint(this ITypeSymbol arg)
        {
            if (arg.IsValueType)
            {
                return true;
            }

            if (arg is not INamedTypeSymbol named || named.IsAbstract || named.IsStatic)
            {
                return false;
            }

            foreach (IMethodSymbol ctor in named.InstanceConstructors)
            {
                if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Validates that every type parameter in <paramref name="parameters"/> has a substitution
        /// in <paramref name="substitution"/> that satisfies the parameter's declared constraints
        /// (reference type, value type, unmanaged, <c>new()</c>, and constraint types). Constraint
        /// types are themselves substituted before checking, to handle F-bounded constraints such
        /// as <c>where T : IFoo&lt;U&gt;</c>.
        ///
        /// Known intentional asymmetry with the reflection mirror: source-gen MUST reject
        /// managed value types (e.g. structs containing reference fields) for a
        /// <c>where T : unmanaged</c> constraint because emitting <c>Derived&lt;ManagedStruct&gt;</c>
        /// would produce a C# compile error (CS8377: the type 'T' must be a non-nullable value type,
        /// along with all fields at any level of nesting, in order to use it as parameter 'T').
        /// Reflection's <c>MakeGenericType</c> only enforces the underlying value-type part of the
        /// constraint at runtime (the <c>modreq</c> for <c>unmanaged</c> is not surfaced through
        /// standard reflection metadata), so it accepts managed structs in this scenario. This is
        /// an inherent reflection-vs-source-gen divergence that cannot be bridged without
        /// emitting invalid C# code.
        /// </summary>
        public static bool TryValidateGenericConstraints(
            this Compilation compilation,
            IReadOnlyList<ITypeParameterSymbol> parameters,
            IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitution,
            [NotNullWhen(false)] out ITypeParameterSymbol? failedParameter,
            out ITypeSymbol? failedArgument)
        {
            foreach (ITypeParameterSymbol param in parameters)
            {
                if (!substitution.TryGetValue(param, out ITypeSymbol? arg))
                {
                    failedParameter = param;
                    failedArgument = null;
                    return false;
                }

                if (param.HasReferenceTypeConstraint && !arg.IsReferenceType)
                {
                    failedParameter = param;
                    failedArgument = arg;
                    return false;
                }

                if (param.HasValueTypeConstraint)
                {
                    if (!arg.IsValueType || arg is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
                    {
                        failedParameter = param;
                        failedArgument = arg;
                        return false;
                    }
                }

                if (param.HasUnmanagedTypeConstraint && !arg.IsUnmanagedType)
                {
                    failedParameter = param;
                    failedArgument = arg;
                    return false;
                }

                if (param.HasConstructorConstraint && !arg.SatisfiesNewConstraint())
                {
                    failedParameter = param;
                    failedArgument = arg;
                    return false;
                }

                foreach (ITypeSymbol constraintType in param.ConstraintTypes)
                {
                    ITypeSymbol substituted = compilation.SubstituteTypeParameters(constraintType, substitution);

                    // Use HasImplicitConversion so generic variance is respected (e.g.
                    // `where T : IEnumerable<object>` is satisfied by `List<string>` via the
                    // covariant `IEnumerable<out T>`). The identity-based IsAssignableFrom helper
                    // would reject variance-satisfiable constraints, diverging from reflection's
                    // MakeGenericType (which delegates to the CLR type system, where interface
                    // variance is native).
                    if (!compilation.HasImplicitConversion(arg, substituted))
                    {
                        failedParameter = param;
                        failedArgument = arg;
                        return false;
                    }
                }
            }

            failedParameter = null;
            failedArgument = null;
            return true;
        }

        /// <summary>
        /// Constructs <paramref name="typeDef"/> using <paramref name="allArgs"/>, accounting for
        /// nesting: the leading args bind enclosing-type parameters (outermost first) and the
        /// trailing args bind <paramref name="typeDef"/>'s own parameters. Non-generic intermediate
        /// enclosing types still need to be re-resolved against the constructed outer so that
        /// references to their generic outers carry the supplied type arguments.
        /// </summary>
        public static INamedTypeSymbol ConstructWithEnclosingTypeArguments(this INamedTypeSymbol typeDef, IReadOnlyList<ITypeSymbol> allArgs)
        {
            int offset = 0;
            INamedTypeSymbol? constructedContaining = ConstructEnclosing(typeDef.ContainingType, allArgs, ref offset);

            int leafParamCount = typeDef.TypeParameters.Length;
            ITypeSymbol[] leafArgs = new ITypeSymbol[leafParamCount];
            for (int i = 0; i < leafParamCount; i++)
            {
                leafArgs[i] = allArgs[offset + i];
            }

            if (constructedContaining is not null)
            {
                INamedTypeSymbol nestedDef = constructedContaining
                    .GetTypeMembers(typeDef.Name, leafParamCount)
                    .Single(t => SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, typeDef));
                return leafParamCount == 0 ? nestedDef : nestedDef.Construct(leafArgs);
            }

            return leafParamCount == 0 ? typeDef : typeDef.Construct(leafArgs);

            static INamedTypeSymbol? ConstructEnclosing(INamedTypeSymbol? type, IReadOnlyList<ITypeSymbol> allArgs, ref int offset)
            {
                if (type is null)
                {
                    return null;
                }

                INamedTypeSymbol? outer = ConstructEnclosing(type.ContainingType, allArgs, ref offset);
                int paramCount = type.TypeParameters.Length;

                if (paramCount == 0)
                {
                    if (outer is null)
                    {
                        return type;
                    }

                    return outer.GetTypeMembers(type.Name, 0).Single(t => SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, type));
                }

                ITypeSymbol[] args = new ITypeSymbol[paramCount];
                for (int i = 0; i < paramCount; i++)
                {
                    args[i] = allArgs[offset + i];
                }

                offset += paramCount;

                if (outer is null)
                {
                    return type.Construct(args);
                }

                INamedTypeSymbol nestedDef = outer.GetTypeMembers(type.Name, paramCount).Single(t => SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, type));
                return nestedDef.Construct(args);
            }
        }

        public static bool IsGenericTypeDefinition(this ITypeSymbol type)
            => type is INamedTypeSymbol { IsGenericType: true } namedType && SymbolEqualityComparer.Default.Equals(namedType, namedType.ConstructedFrom);

        public static bool IsNumberType(this ITypeSymbol type)
        {
            return type.SpecialType is
                SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or
                SpecialType.System_Byte or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64 or
                SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal;
        }

        public static bool IsNullableType(this ITypeSymbol type)
            => !type.IsValueType || type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T;

        public static bool IsNullableValueType(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? elementType)
        {
            if (type.IsValueType && type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
            {
                elementType = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }

            elementType = null;
            return false;
        }

        public static ITypeSymbol GetMemberType(this ISymbol member)
        {
            Debug.Assert(member is IFieldSymbol or IPropertySymbol);
            return member is IFieldSymbol fs ? fs.Type : ((IPropertySymbol)member).Type;
        }

        public static bool IsOverriddenOrShadowedBy(this ISymbol member, ISymbol otherMember)
        {
            Debug.Assert(member is IFieldSymbol or IPropertySymbol);
            Debug.Assert(otherMember is IFieldSymbol or IPropertySymbol);
            return member.Name == otherMember.Name && member.ContainingType.IsAssignableFrom(otherMember.ContainingType);
        }

        public static bool MemberNameNeedsAtSign(this ISymbol symbol)
            => SyntaxFacts.GetKeywordKind(symbol.Name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(symbol.Name) != SyntaxKind.None;

        public static INamedTypeSymbol[] GetSortedTypeHierarchy(this ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol namedType)
            {
                return Array.Empty<INamedTypeSymbol>();
            }

            if (type.TypeKind != TypeKind.Interface)
            {
                var list = new List<INamedTypeSymbol>();
                for (INamedTypeSymbol? current = namedType; current != null; current = current.BaseType)
                {
                    list.Add(current);
                }

                return list.ToArray();
            }
            else
            {
                // Interface hierarchies support multiple inheritance.
                // For consistency with class hierarchy resolution order,
                // sort topologically from most derived to least derived.
                return JsonHelpers.TraverseGraphWithTopologicalSort<INamedTypeSymbol>(namedType, static t => t.AllInterfaces, SymbolEqualityComparer.Default);
            }
        }

        /// <summary>
        /// Returns the kind keyword corresponding to the specified declaration syntax node.
        /// </summary>
        public static string GetTypeKindKeyword(this TypeDeclarationSyntax typeDeclaration)
        {
            switch (typeDeclaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return "class";
                case SyntaxKind.InterfaceDeclaration:
                    return "interface";
                case SyntaxKind.StructDeclaration:
                    return "struct";
                case SyntaxKind.RecordDeclaration:
                    return "record";
                case SyntaxKind.RecordStructDeclaration:
                    return "record struct";
                case SyntaxKind.EnumDeclaration:
                    return "enum";
                case SyntaxKind.DelegateDeclaration:
                    return "delegate";
                default:
                    Debug.Fail("unexpected syntax kind");
                    return null;
            }
        }

        public static void ResolveNullabilityAnnotations(this IFieldSymbol field, out bool isGetterNonNullable, out bool isSetterNonNullable)
        {
            if (field.Type.IsNullableType())
            {
                // Because System.Text.Json cannot distinguish between nullable and non-nullable type parameters,
                // (e.g. the same metadata is being used for both KeyValuePair<string, string?> and KeyValuePair<string, string>),
                // we derive nullability annotations from the original definition of the field and not its instantiation.
                // This preserves compatibility with the capabilities of the reflection-based NullabilityInfo reader.
                field = field.OriginalDefinition;

                isGetterNonNullable = IsOutputTypeNonNullable(field, field.Type);
                isSetterNonNullable = IsInputTypeNonNullable(field, field.Type);
            }
            else
            {
                isGetterNonNullable = isSetterNonNullable = false;
            }
        }

        public static void ResolveNullabilityAnnotations(this IPropertySymbol property, out bool isGetterNonNullable, out bool isSetterNonNullable)
        {
            if (property.Type.IsNullableType())
            {
                // Because System.Text.Json cannot distinguish between nullable and non-nullable type parameters,
                // (e.g. the same metadata is being used for both KeyValuePair<string, string?> and KeyValuePair<string, string>),
                // we derive nullability annotations from the original definition of the field and not its instantiation.
                // This preserves compatibility with the capabilities of the reflection-based NullabilityInfo reader.
                property = property.OriginalDefinition;

                isGetterNonNullable = property.GetMethod != null && IsOutputTypeNonNullable(property, property.Type);
                isSetterNonNullable = property.SetMethod != null && IsInputTypeNonNullable(property, property.Type);
            }
            else
            {
                isGetterNonNullable = isSetterNonNullable = false;
            }
        }

        public static bool IsNullable(this IParameterSymbol parameter)
        {
            if (parameter.Type.IsNullableType())
            {
                // Because System.Text.Json cannot distinguish between nullable and non-nullable type parameters,
                // (e.g. the same metadata is being used for both KeyValuePair<string, string?> and KeyValuePair<string, string>),
                // we derive nullability annotations from the original definition of the field and not its instantiation.
                // This preserves compatibility with the capabilities of the reflection-based NullabilityInfo reader.
                parameter = parameter.OriginalDefinition;
                return !IsInputTypeNonNullable(parameter, parameter.Type);
            }

            return false;
        }

        private static bool IsOutputTypeNonNullable(this ISymbol symbol, ITypeSymbol returnType)
        {
            if (symbol.HasCodeAnalysisAttribute("MaybeNullAttribute"))
            {
                return false;
            }

            if (symbol.HasCodeAnalysisAttribute("NotNullAttribute"))
            {
                return true;
            }

            if (returnType is ITypeParameterSymbol { HasNotNullConstraint: false })
            {
                return false;
            }

            return returnType.NullableAnnotation is NullableAnnotation.NotAnnotated;
        }

        private static bool IsInputTypeNonNullable(this ISymbol symbol, ITypeSymbol inputType)
        {
            Debug.Assert(inputType.IsNullableType());

            if (symbol.HasCodeAnalysisAttribute("AllowNullAttribute"))
            {
                return false;
            }

            if (symbol.HasCodeAnalysisAttribute("DisallowNullAttribute"))
            {
                return true;
            }

            if (inputType is ITypeParameterSymbol { HasNotNullConstraint: false })
            {
                return false;
            }

            return inputType.NullableAnnotation is NullableAnnotation.NotAnnotated;
        }

        private static bool HasCodeAnalysisAttribute(this ISymbol symbol, string attributeName)
        {
            return symbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.Name == attributeName &&
                attr.AttributeClass.ContainingNamespace.ToDisplayString() == "System.Diagnostics.CodeAnalysis");
        }

        // Polyfill for the closed-type reflection APIs added to Roslyn in dotnet/roslyn#84045
        // (ITypeSymbol.IsClosed and ITypeSymbol.GetClosedDerivedTypeInfo, available from Roslyn 5.10).
        // The generator compiles against Roslyn 4.4, so it cannot reference those APIs directly. These
        // reconstruct the same information from APIs available in 4.4 so that closed-type polymorphism
        // inference works on any compiler that understands the 'closed' modifier, without depending on
        // whether Roslyn surfaces the reserved closed-type metadata to the symbol API (it currently
        // filters the compiler-emitted [IsClosedType] attribute out of ITypeSymbol.GetAttributes()).
        // The generator targets older Roslyn APIs, so the built-in APIs are accessed through light-up
        // until the generator's Roslyn floor is raised past 5.10.
        //
        // Roslyn implementation at dotnet/roslyn @ 18bf2c8709264bac6615856e507eb44ba2a026e2:
        //   Public ITypeSymbol implementation:
        //     https://github.com/dotnet/roslyn/blob/18bf2c8709264bac6615856e507eb44ba2a026e2/src/Compilers/CSharp/Portable/Symbols/PublicModel/TypeSymbol.cs#L207-L220
        //   Source-symbol candidate enumeration mirrored by these polyfills:
        //     https://github.com/dotnet/roslyn/blob/18bf2c8709264bac6615856e507eb44ba2a026e2/src/Compilers/CSharp/Portable/Symbols/Source/SourceMemberContainerSymbol.cs#L903-L961

        /// <summary>
        /// Polyfill for <c>ITypeSymbol.IsClosed</c>: returns <see langword="true"/> when
        /// <paramref name="type"/> is an abstract class declared with the <c>closed</c> modifier, whose
        /// hierarchy the language restricts to its declaring module. Detection is syntactic because the
        /// compiler-emitted <c>[IsClosedType]</c> attribute is filtered out of the symbol API. When
        /// available, the built-in API is used through light-up so metadata-only symbols are supported;
        /// otherwise source declarations fall back to syntax inspection.
        /// </summary>
        public static bool IsClosedType(this INamedTypeSymbol type)
        {
            if (type is not { TypeKind: TypeKind.Class, IsAbstract: true })
            {
                return false;
            }

            if (s_isClosedTypeAccessor is not null)
            {
                return s_isClosedTypeAccessor(type);
            }

            foreach (SyntaxReference syntaxReference in type.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is BaseTypeDeclarationSyntax declaration)
                {
                    foreach (SyntaxToken modifier in declaration.Modifiers)
                    {
                        // The 'closed' contextual keyword is tokenized as a dedicated SyntaxKind by
                        // closed-aware compilers, but that enum member does not exist in the Roslyn
                        // version the generator compiles against. Compare the token text instead, which
                        // is stable across compiler versions.
                        if (modifier.Text is "closed")
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static Func<ITypeSymbol, bool>? CreateIsClosedTypeAccessor()
        {
            // IsClosed is unavailable in the Roslyn reference assemblies used to compile the generator.
            MethodInfo? getter = typeof(ITypeSymbol).GetProperty("IsClosed")?.GetMethod;
            return getter is null
                ? null
                : (Func<ITypeSymbol, bool>)getter.CreateDelegate(typeof(Func<ITypeSymbol, bool>));
        }

        /// <summary>
        /// Polyfill for <c>ITypeSymbol.GetClosedDerivedTypeInfo().ClosedDerivedTypes</c>: reconstructs the
        /// immediate derived types of a closed hierarchy. The <c>closed</c> modifier constrains subtyping
        /// to the base type's declaring module, so the immediate derived types are exactly the same-module
        /// named types whose direct base type shares <paramref name="closedType"/>'s original definition —
        /// the set Roslyn records internally as <c>CandidateClosedSubtypeDefinitions</c>. Generic derived
        /// types are returned in unbound form so callers can unify them against the constructed base.
        /// Returns <see langword="null"/> when none are found. Derived types are yielded in module-scan
        /// order; callers that require a canonical ordering (for example, for deterministic generator
        /// output) order them by discriminator, where uniqueness is established.
        /// </summary>
        public static List<ITypeSymbol>? GetClosedDerivedTypes(this INamedTypeSymbol closedType)
        {
            INamedTypeSymbol baseDefinition = closedType.OriginalDefinition;
            List<ITypeSymbol>? derivedTypes = null;

            foreach (INamedTypeSymbol candidate in EnumerateNamedTypes(closedType.ContainingModule.GlobalNamespace))
            {
                if (candidate.BaseType is { } candidateBase &&
                    SymbolEqualityComparer.Default.Equals(candidateBase.OriginalDefinition, baseDefinition))
                {
                    ITypeSymbol derivedType = candidate.IsGenericType
                        ? candidate.ConstructUnboundGenericType()
                        : candidate;

                    (derivedTypes ??= new()).Add(derivedType);
                }
            }

            return derivedTypes;

            static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol namespaceSymbol)
            {
                foreach (INamespaceOrTypeSymbol member in namespaceSymbol.GetMembers())
                {
                    if (member is INamespaceSymbol childNamespace)
                    {
                        foreach (INamedTypeSymbol nestedType in EnumerateNamedTypes(childNamespace))
                        {
                            yield return nestedType;
                        }
                    }
                    else if (member is INamedTypeSymbol namedType)
                    {
                        yield return namedType;

                        foreach (INamedTypeSymbol nestedType in EnumerateNestedTypes(namedType))
                        {
                            yield return nestedType;
                        }
                    }
                }
            }

            static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
            {
                foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
                {
                    yield return nestedType;

                    foreach (INamedTypeSymbol deeperType in EnumerateNestedTypes(nestedType))
                    {
                        yield return deeperType;
                    }
                }
            }
        }

        // The following is a faithful port of the Roslyn C# compiler's own accessibility comparison — the
        // logic it runs to report the "inconsistent accessibility" diagnostics (CS0050/CS0060 and friends).
        // Callers use it to decide whether one type is at least as visible as another — for example, whether
        // an inferred closed-hierarchy derived type is at least as visible as the base it is registered under,
        // so that every location that can reference the base can also reference the derived type. Rather than
        // invent an accessibility metric (which would risk drifting from the language as new modifiers are
        // added), we reproduce the compiler's algorithm verbatim, using its terminology, so it tracks C#
        // accessibility as it evolves. The reflection resolver in DefaultJsonTypeInfoResolver.Helpers.cs
        // mirrors this over System.Type.
        //
        // Ported from dotnet/roslyn @ 121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2:
        //   IsAtLeastAsVisibleAs / FindTypeLessVisibleThan / IsAsRestrictive:
        //     https://github.com/dotnet/roslyn/blob/121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2/src/Compilers/CSharp/Portable/Symbols/TypeSymbolExtensions.cs#L1048
        //   IsAccessibleViaInheritance:
        //     https://github.com/dotnet/roslyn/blob/121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2/src/Compilers/CSharp/Portable/Symbols/SymbolExtensions.cs#L48
        //   HasInternalAccessTo:
        //     https://github.com/dotnet/roslyn/blob/121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2/src/Compilers/CSharp/Portable/Binder/Semantics/AccessCheck.cs#L676

        /// <summary>
        /// Determines whether <paramref name="type"/> is at least as visible as <paramref name="sym"/>. Port
        /// of Roslyn's <c>TypeSymbolExtensions.IsAtLeastAsVisibleAs</c>; because a closed hierarchy relates
        /// two named types, the compound-type traversal (<c>FindTypeLessVisibleThan</c>/<c>Symbol.VisitType</c>)
        /// reduces to the single <c>IsAsRestrictive</c> check.
        /// </summary>
        public static bool IsAtLeastAsVisibleAs(this ITypeSymbol type, ITypeSymbol sym)
        {
            return IsAsRestrictive(type, sym);

            static bool IsAsRestrictive(ISymbol s1, ISymbol sym2)
            {
                Accessibility acc1 = s1.DeclaredAccessibility;

                if (acc1 == Accessibility.Public)
                {
                    return true;
                }

                for (ISymbol s2 = sym2; s2.Kind != SymbolKind.Namespace; s2 = s2.ContainingSymbol!)
                {
                    Accessibility acc2 = s2.DeclaredAccessibility;

                    switch (acc1)
                    {
                        case Accessibility.Internal:
                            // If s2 is private or internal, and is in an assembly that gives s1's assembly
                            // internal access, then this is at least as restrictive as s1's internal.
                            if (acc2 is Accessibility.Private or Accessibility.Internal or Accessibility.ProtectedAndInternal &&
                                HasInternalAccessTo(s2.ContainingAssembly, s1.ContainingAssembly))
                            {
                                return true;
                            }

                            break;

                        case Accessibility.ProtectedAndInternal:
                            // Since s1 is private protected, s2 must be more restrictive than both internal and
                            // protected. Do the "internal" test first (as above); if it passes, fall through to
                            // the "protected" test.
                            if (acc2 is Accessibility.Private or Accessibility.Internal or Accessibility.ProtectedAndInternal &&
                                HasInternalAccessTo(s2.ContainingAssembly, s1.ContainingAssembly))
                            {
                                goto case Accessibility.Protected;
                            }

                            break;

                        case Accessibility.Protected:
                        {
                            INamedTypeSymbol? parent1 = s1.ContainingType;

                            if (parent1 is null)
                            {
                                // not helpful
                            }
                            else if (acc2 == Accessibility.Private)
                            {
                                // if s2 is private and within s1's parent or within a subclass of s1's
                                // parent, then this is at least as restrictive as s1's protected.
                                for (INamedTypeSymbol? parent2 = s2.ContainingType; parent2 is not null; parent2 = parent2.ContainingType)
                                {
                                    if (IsAccessibleViaInheritance(parent1, parent2))
                                    {
                                        return true;
                                    }
                                }
                            }
                            else if (acc2 is Accessibility.Protected or Accessibility.ProtectedAndInternal)
                            {
                                // if s2 is protected, and its parent is a subclass of (or the same as) s1's
                                // parent, then this is at least as restrictive as s1's protected.
                                INamedTypeSymbol? parent2 = s2.ContainingType;
                                if (parent2 is not null && IsAccessibleViaInheritance(parent1, parent2))
                                {
                                    return true;
                                }
                            }

                            break;
                        }

                        case Accessibility.ProtectedOrInternal:
                        {
                            INamedTypeSymbol? parent1 = s1.ContainingType;

                            if (parent1 is null)
                            {
                                break;
                            }

                            switch (acc2)
                            {
                                case Accessibility.Private:
                                    // if s2 is private and within a subclass of s1's parent, or within the
                                    // same assembly as s1, then this is at least as restrictive as s1's
                                    // protected internal.
                                    if (HasInternalAccessTo(s2.ContainingAssembly, s1.ContainingAssembly))
                                    {
                                        return true;
                                    }

                                    for (INamedTypeSymbol? parent2 = s2.ContainingType; parent2 is not null; parent2 = parent2.ContainingType)
                                    {
                                        if (IsAccessibleViaInheritance(parent1, parent2))
                                        {
                                            return true;
                                        }
                                    }

                                    break;

                                case Accessibility.Internal:
                                    // If s2 is in an assembly that gives s1's assembly internal access, then
                                    // this is more restrictive than s1's protected internal.
                                    if (HasInternalAccessTo(s2.ContainingAssembly, s1.ContainingAssembly))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.Protected:
                                    // if s2 is protected, and its parent is a subclass of (or the same as)
                                    // s1's parent, then this is at least as restrictive as s1's protected internal.
                                    if (s2.ContainingType is INamedTypeSymbol protectedParent2 &&
                                        IsAccessibleViaInheritance(parent1, protectedParent2))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.ProtectedAndInternal:
                                    // if s2 is private protected, and its parent is a subclass of (or the same
                                    // as) s1's parent, or it is in the same assembly as s1, then this is at
                                    // least as restrictive as s1's protected internal.
                                    if (HasInternalAccessTo(s2.ContainingAssembly, s1.ContainingAssembly) ||
                                        (s2.ContainingType is INamedTypeSymbol privateProtectedParent2 &&
                                         IsAccessibleViaInheritance(parent1, privateProtectedParent2)))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.ProtectedOrInternal:
                                    // if s2 is protected internal, and its parent is a subclass of (or the same
                                    // as) s1's parent, and it is in the same assembly as s1, then this is at
                                    // least as restrictive as s1's protected internal.
                                    if (HasInternalAccessTo(s2.ContainingAssembly, s1.ContainingAssembly) &&
                                        s2.ContainingType is INamedTypeSymbol protectedOrInternalParent2 &&
                                        IsAccessibleViaInheritance(parent1, protectedOrInternalParent2))
                                    {
                                        return true;
                                    }

                                    break;
                            }

                            break;
                        }

                        case Accessibility.Private:
                            if (acc2 == Accessibility.Private)
                            {
                                // if s2 is private, and it is within s1's parent, then this is at least as
                                // restrictive as s1's private.
                                INamedTypeSymbol? parent1 = s1.ContainingType;

                                if (parent1 is null)
                                {
                                    break;
                                }

                                INamedTypeSymbol parent1OriginalDefinition = parent1.OriginalDefinition;
                                for (INamedTypeSymbol? parent2 = s2.ContainingType; parent2 is not null; parent2 = parent2.ContainingType)
                                {
                                    if (SymbolEqualityComparer.Default.Equals(parent2.OriginalDefinition, parent1OriginalDefinition))
                                    {
                                        return true;
                                    }
                                }
                            }

                            break;
                    }
                }

                return false;
            }

            static bool IsAccessibleViaInheritance(INamedTypeSymbol superType, INamedTypeSymbol subType)
            {
                INamedTypeSymbol originalSuperType = superType.OriginalDefinition;
                for (INamedTypeSymbol? current = subType; current is not null; current = current.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, originalSuperType))
                    {
                        return true;
                    }
                }

                if (originalSuperType.TypeKind == TypeKind.Interface)
                {
                    foreach (INamedTypeSymbol current in subType.AllInterfaces)
                    {
                        if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, originalSuperType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            static bool HasInternalAccessTo(IAssemblySymbol fromAssembly, IAssemblySymbol toAssembly)
            {
                if (SymbolEqualityComparer.Default.Equals(fromAssembly, toAssembly))
                {
                    return true;
                }

                return toAssembly.GivesAccessTo(fromAssembly);
            }
        }
    }
}
