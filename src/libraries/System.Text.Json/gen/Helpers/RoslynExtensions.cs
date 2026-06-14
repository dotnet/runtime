// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    internal static class RoslynExtensions
    {
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
        /// Returns <see langword="true"/> if <paramref name="derivedParam"/>'s declared constraints
        /// match <paramref name="baseParam"/>'s declared constraints exactly, after applying
        /// <paramref name="substitution"/> to type-constraint references. Used by the
        /// "uniform derived registration" check to verify that a derived type's parameter
        /// constraints will be satisfied for every valid specialization of the base.
        ///
        /// "Exact match" is used in place of one-sided constraint subsumption because in the
        /// uniform-applicability regime the two are equivalent (C# already forces a derived
        /// parameter that's identified with a base parameter to declare at least the base's
        /// constraints), and an equality check is simpler, easier to reason about, and
        /// forward-compatible with new C# constraint kinds.
        ///
        /// <list type="bullet">
        ///   <item>Special constraint flags (<c>class</c>, <c>struct</c>, <c>unmanaged</c>,
        ///         <c>new()</c>) must match exactly.</item>
        ///   <item>The set of type constraints must match exactly after substitution
        ///         (order-independent). For F-bounded constraints, the substitution is
        ///         applied to nested type-parameter positions as well, so a derived
        ///         <c>where T : IComparable&lt;T&gt;</c> matches a base
        ///         <c>where U : IComparable&lt;U&gt;</c> only after the substitution
        ///         <c>{ T -&gt; U }</c> has been applied to both occurrences.</item>
        ///   <item>The compile-time-only <c>notnull</c> constraint is intentionally ignored
        ///         (it is not surfaced via reflection and is not runtime-enforced). As a
        ///         consequence, two registrations whose constraints differ only in the
        ///         presence of <c>notnull</c> are accepted as equivalent.</item>
        /// </list>
        ///
        /// IMPORTANT: This implementation MIRRORS the reflection-side
        /// <c>System.Text.Json.Reflection.ReflectionExtensions.AreConstraintsEquivalent</c>.
        /// Any change to the equivalence rules MUST be applied on both sides.
        /// </summary>
        public static bool AreConstraintsEquivalent(
            this Compilation compilation,
            ITypeParameterSymbol derivedParam,
            ITypeParameterSymbol baseParam,
            IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitution)
        {
            if (derivedParam.HasReferenceTypeConstraint != baseParam.HasReferenceTypeConstraint ||
                derivedParam.HasValueTypeConstraint != baseParam.HasValueTypeConstraint ||
                derivedParam.HasUnmanagedTypeConstraint != baseParam.HasUnmanagedTypeConstraint ||
                derivedParam.HasConstructorConstraint != baseParam.HasConstructorConstraint)
            {
                return false;
            }

            ImmutableArray<ITypeSymbol> derivedConstraints = derivedParam.ConstraintTypes;
            ImmutableArray<ITypeSymbol> baseConstraints = baseParam.ConstraintTypes;
            if (derivedConstraints.Length != baseConstraints.Length)
            {
                return false;
            }

            if (derivedConstraints.Length == 0)
            {
                return true;
            }

            // Compare type-constraint sets order-independently. Substitute each derived
            // constraint into base-parameter terms via the canonical mapping, then check
            // that the resulting multiset matches the base's constraint set. Length parity
            // was already established above, so removing each substituted derived constraint
            // from a fresh copy of the base set proves equality iff every Remove succeeds.
            var baseSet = new HashSet<ITypeSymbol>(baseConstraints, SymbolEqualityComparer.Default);
            foreach (ITypeSymbol derivedConstraint in derivedConstraints)
            {
                ITypeSymbol substituted = compilation.SubstituteTypeParameters(derivedConstraint, substitution);
                if (!baseSet.Remove(substituted))
                {
                    return false;
                }
            }

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

        /// <summary>
        /// Validates that <paramref name="unboundDerived"/> applies uniformly to every
        /// specialization of the open generic base type whose definition matches
        /// <c>constructedBase.OriginalDefinition</c>, and (when it does) produces the closed
        /// derived type for the closure identified by <paramref name="constructedBase"/>.
        ///
        /// "Uniform" means: there is a single canonical substitution mapping each derived
        /// type parameter to a base type parameter that simultaneously satisfies every
        /// matching ancestor of the derived type, with every derived constraint exactly
        /// matching the constraints on the corresponding base parameter. Per-ancestor
        /// unifications are computed independently and then verified to coincide -- this is
        /// what catches asymmetric multi-interface implementations like
        /// <c>D&lt;U1, U2&gt; : IBase&lt;U1, U2&gt;, IBase&lt;U2, U1&gt;</c>, where each
        /// ancestor admits a unifier on its own but no single canonical answer covers both.
        /// Registrations that pin a particular specialization
        /// (e.g. <c>Derived&lt;T&gt; : Base&lt;T, int&gt;</c>) are rejected: such registrations
        /// would silently work for one base specialization and break for another, which we
        /// treat as a misregistration regardless of which specialization is currently being
        /// generated.
        ///
        /// Returns <see langword="true"/> with <paramref name="resolvedDerivedType"/> set to
        /// the closed derived type, or <see langword="false"/> with a localized
        /// <paramref name="failureReason"/> drawn from <c>SR.Polymorphism_OpenGeneric_*</c>
        /// (suitable for inclusion in the <c>SYSLIB1229</c> message).
        ///
        /// IMPORTANT: This implementation MIRRORS the reflection-side resolver
        /// <c>DefaultJsonTypeInfoResolver.Helpers.TryResolveOpenGenericDerivedType</c> in
        /// src/System/Text/Json/Serialization/Metadata/DefaultJsonTypeInfoResolver.Helpers.cs.
        /// Both implementations -- the per-ancestor unification, the canonical-substitution
        /// consistency check, and the constraint-equivalence rules -- must be kept in lockstep
        /// so source-gen and reflection produce the same closed type for the same registration.
        /// </summary>
        public static bool TryResolveOpenGenericDerivedType(
            this Compilation compilation,
            INamedTypeSymbol unboundDerived,
            INamedTypeSymbol constructedBase,
            out INamedTypeSymbol? resolvedDerivedType,
            out string? failureReason)
        {
            Debug.Assert(unboundDerived.IsUnboundGenericType);
            Debug.Assert(constructedBase.IsGenericType);

            resolvedDerivedType = null;
            failureReason = null;

            INamedTypeSymbol derivedDefinition = unboundDerived.OriginalDefinition;
            INamedTypeSymbol baseDefinition = constructedBase.OriginalDefinition;

            // Every ancestor of the derived type definition whose original definition matches
            // the base type definition. For classes there is at most one such ancestor; for
            // interfaces a derived type can implement the same interface definition multiple
            // times with different type arguments.
            List<INamedTypeSymbol> matchingBases = derivedDefinition
                .GetCompatibleGenericBaseTypes(baseDefinition)
                .ToList();

            if (matchingBases.Count == 0)
            {
                failureReason = SR.Polymorphism_OpenGeneric_Reason_NotAssignable;
                return false;
            }

            // The complete set of derived parameters that must be bound (enclosing + leaf).
            List<ITypeParameterSymbol> requiredDerivedParams = derivedDefinition.GetAllTypeParameters();
            List<ITypeParameterSymbol> baseParams = baseDefinition.GetAllTypeParameters();
            var baseParamSet = new HashSet<ITypeParameterSymbol>(baseParams, SymbolEqualityComparer.Default);

            // Per-ancestor independent substitutions; the uniform answer must be a single
            // canonical substitution agreed upon by every ancestor.
            Dictionary<ITypeParameterSymbol, ITypeSymbol>? canonical = null;

            foreach (INamedTypeSymbol ancestor in matchingBases)
            {
                var substitution = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(
                    requiredDerivedParams.Count, SymbolEqualityComparer.Default);

                if (!ancestor.TryUnifyWith(baseDefinition, substitution))
                {
                    // No unifier exists. Some position pins a concrete type (e.g. Base<T, int>)
                    // or a constructed pattern (e.g. Base<List<T>>) that cannot match the base
                    // type parameter at that position (the rigid target).
                    failureReason = SR.Polymorphism_OpenGeneric_Reason_NonUniformPinning;
                    return false;
                }

                foreach (ITypeParameterSymbol p in requiredDerivedParams)
                {
                    if (!substitution.TryGetValue(p, out ITypeSymbol? mapped))
                    {
                        // E.g. D<U1, U2> : IBase<U1> -- U2 is not bound by this ancestor.
                        failureReason = string.Format(
                            CultureInfo.InvariantCulture,
                            SR.Polymorphism_OpenGeneric_Reason_UnboundParameter,
                            p.Name);
                        return false;
                    }

                    if (mapped is not ITypeParameterSymbol mappedBaseParam || !baseParamSet.Contains(mappedBaseParam))
                    {
                        // Defensive: a unifier exists but it would map a derived parameter to
                        // something other than one of the base's own type parameters (i.e. the
                        // result isn't a pure renaming). With the rigid-target unification used
                        // here this is essentially unreachable -- TryUnifyWith binds derived
                        // parameters only against the base definition's own type arguments,
                        // which are all base parameters -- but we report it separately from
                        // NonUniformPinning so any future relaxation of TryUnifyWith (e.g.
                        // binding into nested constructed targets) surfaces with a precise
                        // diagnostic instead of getting silently lumped under pinning.
                        failureReason = SR.Polymorphism_OpenGeneric_Reason_NonUniformUnification;
                        return false;
                    }
                }

                if (canonical is null)
                {
                    canonical = substitution;
                }
                else if (!SubstitutionsEqual(canonical, substitution))
                {
                    // Two ancestors agree on independent bindings but produce different
                    // (derived -> base) mappings, e.g. D<U1, U2> : IBase<U1, U2>, IBase<U2, U1>.
                    // There is no single canonical answer for an arbitrary base closure.
                    failureReason = SR.Polymorphism_OpenGeneric_Reason_AmbiguousMatch;
                    return false;
                }
            }

            Debug.Assert(canonical is not null);

            // Constraint equivalence: every derived parameter's constraints must exactly
            // match the constraints on the mapped base parameter (after substitution) so
            // that any valid closure of the base also yields a valid closure of the
            // derived. See ReflectionExtensions.AreConstraintsEquivalent for the rationale
            // behind exact match (vs one-sided subsumption).
            foreach (ITypeParameterSymbol derivedParam in requiredDerivedParams)
            {
                var mappedBaseParam = (ITypeParameterSymbol)canonical[derivedParam];
                if (!compilation.AreConstraintsEquivalent(derivedParam, mappedBaseParam, canonical))
                {
                    failureReason = string.Format(
                        CultureInfo.InvariantCulture,
                        SR.Polymorphism_OpenGeneric_Reason_ConstraintMismatch,
                        derivedParam.Name,
                        mappedBaseParam.Name);
                    return false;
                }
            }

            // Closure construction: substitute the canonical mapping then specialize each
            // base parameter to the actual closed-base type argument.
            var baseParamPosition = new Dictionary<ITypeParameterSymbol, int>(SymbolEqualityComparer.Default);
            for (int i = 0; i < baseParams.Count; i++)
            {
                baseParamPosition[baseParams[i]] = i;
            }

            List<ITypeSymbol> constructedBaseAllArgs = GetAllTypeArguments(constructedBase);

            var closedArgs = new ITypeSymbol[requiredDerivedParams.Count];
            for (int i = 0; i < requiredDerivedParams.Count; i++)
            {
                var mappedBaseParam = (ITypeParameterSymbol)canonical[requiredDerivedParams[i]];
                closedArgs[i] = constructedBaseAllArgs[baseParamPosition[mappedBaseParam]];
            }

            resolvedDerivedType = derivedDefinition.ConstructWithEnclosingTypeArguments(closedArgs);
            return true;

            static bool SubstitutionsEqual(
                Dictionary<ITypeParameterSymbol, ITypeSymbol> a,
                Dictionary<ITypeParameterSymbol, ITypeSymbol> b)
            {
                if (a.Count != b.Count)
                {
                    return false;
                }

                foreach (KeyValuePair<ITypeParameterSymbol, ITypeSymbol> kvp in a)
                {
                    if (!b.TryGetValue(kvp.Key, out ITypeSymbol? otherValue) ||
                        !SymbolEqualityComparer.Default.Equals(kvp.Value, otherValue))
                    {
                        return false;
                    }
                }

                return true;
            }

            static List<ITypeSymbol> GetAllTypeArguments(INamedTypeSymbol type)
            {
                var result = new List<ITypeSymbol>();
                Append(type.ContainingType, result);
                result.AddRange(type.TypeArguments);
                return result;

                static void Append(INamedTypeSymbol? enclosing, List<ITypeSymbol> list)
                {
                    if (enclosing is null)
                    {
                        return;
                    }

                    Append(enclosing.ContainingType, list);
                    list.AddRange(enclosing.TypeArguments);
                }
            }
        }

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

        /// <summary>
        /// Returns an <see cref="IEqualityComparer{T}"/> for value tuples of two elements that
        /// delegates equality and hashing of each tuple component to the corresponding element
        /// comparer. Useful when neither component has a usable default comparer (e.g. Roslyn
        /// symbol types, where <see cref="SymbolEqualityComparer.Default"/> must be used).
        /// </summary>
        public static IEqualityComparer<(T1, T2)> CreateTupleComparer<T1, T2>(
            IEqualityComparer<T1> firstComparer,
            IEqualityComparer<T2> secondComparer)
        {
            return new TupleComparer<T1, T2>(firstComparer, secondComparer);
        }

        private sealed class TupleComparer<T1, T2> : IEqualityComparer<(T1, T2)>
        {
            private readonly IEqualityComparer<T1> _firstComparer;
            private readonly IEqualityComparer<T2> _secondComparer;

            public TupleComparer(IEqualityComparer<T1> firstComparer, IEqualityComparer<T2> secondComparer)
            {
                _firstComparer = firstComparer;
                _secondComparer = secondComparer;
            }

            public bool Equals((T1, T2) x, (T1, T2) y) =>
                _firstComparer.Equals(x.Item1, y.Item1) &&
                _secondComparer.Equals(x.Item2, y.Item2);

            public int GetHashCode((T1, T2) obj)
            {
                int h1 = obj.Item1 is null ? 0 : _firstComparer.GetHashCode(obj.Item1);
                int h2 = obj.Item2 is null ? 0 : _secondComparer.GetHashCode(obj.Item2);
                return unchecked(h1 * 397 ^ h2);
            }
        }
    }
}
