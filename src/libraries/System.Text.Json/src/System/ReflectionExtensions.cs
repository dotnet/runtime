// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;

namespace System.Text.Json.Reflection
{
    internal static partial class ReflectionExtensions
    {
        private static readonly Type s_nullableType = typeof(Nullable<>);

        /// <summary>
        /// Returns <see langword="true" /> when the given type is of type <see cref="Nullable{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullableOfT(this Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == s_nullableType;

        public static bool IsNullableType(this Type type) => !type.IsValueType || IsNullableOfT(type);

        /// <summary>
        /// Returns <see langword="true" /> when the given type is assignable from <paramref name="from"/> including support
        /// when <paramref name="from"/> is <see cref="Nullable{T}"/> by using the {T} generic parameter for <paramref name="from"/>.
        /// </summary>
        public static bool IsAssignableFromInternal(this Type type, Type from)
        {
            if (IsNullableOfT(from) && type.IsInterface)
            {
                return type.IsAssignableFrom(from.GetGenericArguments()[0]);
            }

            return type.IsAssignableFrom(from);
        }

        /// <summary>
        /// Returns <see langword="true" /> when either type is assignable to the other.
        /// </summary>
        public static bool IsInSubtypeRelationshipWith(this Type type, Type other) =>
            type.IsAssignableFromInternal(other) || other.IsAssignableFromInternal(type);

        private static bool HasJsonConstructorAttribute(ConstructorInfo constructorInfo)
            => constructorInfo.IsDefined(typeof(JsonConstructorAttribute), inherit: false);

        public static bool HasRequiredMemberAttribute(this MemberInfo memberInfo)
        {
            // For compiler related attributes we should only look at full type name rather than trying to do something different for version when attribute was introduced.
            // I.e. library is targeting netstandard2.0 with polyfilled attributes and is being consumed by an app targeting net7.0 or greater.
            return memberInfo.HasCustomAttributeWithName("System.Runtime.CompilerServices.RequiredMemberAttribute", inherit: false);
        }

        public static bool HasSetsRequiredMembersAttribute(this MemberInfo memberInfo)
        {
            // See comment for HasRequiredMemberAttribute for why we need to always only look at full name
            return memberInfo.HasCustomAttributeWithName("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute", inherit: false);
        }

        private static bool HasCustomAttributeWithName(this MemberInfo memberInfo, string fullName, bool inherit)
        {
            foreach (object attribute in memberInfo.GetCustomAttributes(inherit))
            {
                if (attribute.GetType().FullName == fullName)
                {
                    return true;
                }
            }

            return false;
        }

        public static TAttribute? GetUniqueCustomAttribute<TAttribute>(this MemberInfo memberInfo, bool inherit)
            where TAttribute : Attribute
        {
            object[] attributes = memberInfo.GetCustomAttributes(typeof(TAttribute), inherit);

            if (attributes.Length == 0)
            {
                return null;
            }

            if (attributes.Length == 1)
            {
                return (TAttribute)attributes[0];
            }

            ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateAttribute(typeof(TAttribute), memberInfo);
            return null;
        }

        /// <summary>
        /// Polyfill for BindingFlags.DoNotWrapExceptions
        /// </summary>
        public static object? CreateInstanceNoWrapExceptions(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] this Type type,
            Type[] parameterTypes,
            object?[] parameters)
        {
            ConstructorInfo ctorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null)!;
#if NET
            return ctorInfo.Invoke(BindingFlags.DoNotWrapExceptions, null, parameters, null);
#else
            try
            {
                return ctorInfo.Invoke(parameters);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw; // unreachable
            }
#endif
        }

#if !NET
        public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate =>
            (T)methodInfo.CreateDelegate(typeof(T));
#endif

        /// <summary>
        /// Invokes <paramref name="methodInfo"/> without wrapping any exception thrown by the
        /// target method in a <see cref="TargetInvocationException"/>. This matches the behavior of
        /// the Reflection.Emit-based accessor, which emits direct calls into user code.
        /// </summary>
        public static object? InvokeNoWrapExceptions(this MethodInfo methodInfo, object? obj, object?[]? parameters)
        {
#if NET
            return methodInfo.Invoke(obj, BindingFlags.DoNotWrapExceptions, binder: null, parameters, culture: null);
#else
            try
            {
                return methodInfo.Invoke(obj, parameters);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw; // unreachable
            }
#endif
        }

        /// <summary>
        /// Invokes <paramref name="constructorInfo"/> without wrapping any exception thrown by the
        /// constructor in a <see cref="TargetInvocationException"/>. This matches the behavior of
        /// the Reflection.Emit-based accessor, which emits direct calls into user code.
        /// </summary>
        public static object InvokeNoWrapExceptions(this ConstructorInfo constructorInfo, object?[]? parameters)
        {
#if NET
            return constructorInfo.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters, culture: null);
#else
            try
            {
                return constructorInfo.Invoke(parameters);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw; // unreachable
            }
#endif
        }

        public static ParameterInfo GetGenericParameterDefinition(this ParameterInfo parameter)
        {
            if (parameter.Member is { DeclaringType.IsConstructedGenericType: true }
                                 or MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false })
            {
                var genericMethod = (MethodBase)parameter.Member.GetGenericMemberDefinition()!;
                return genericMethod.GetParameters()[parameter.Position];
            }

            return parameter;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
            Justification = "Looking up the generic member definition of the provided member.")]
        public static MemberInfo GetGenericMemberDefinition(this MemberInfo member)
        {
            if (member is Type type)
            {
                return type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
            }

            if (member.DeclaringType!.IsConstructedGenericType)
            {
                const BindingFlags AllMemberFlags =
                    BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic;

                Type genericTypeDef = member.DeclaringType.GetGenericTypeDefinition();
                foreach (MemberInfo genericMember in genericTypeDef.GetMember(member.Name, AllMemberFlags))
                {
                    if (genericMember.MetadataToken == member.MetadataToken)
                    {
                        return genericMember;
                    }
                }

                Debug.Fail("Unreachable code");
                throw new Exception();
            }

            if (member is MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false } method)
            {
                return method.GetGenericMethodDefinition();
            }

            return member;
        }

        /// <summary>
        /// Enumerates every ancestor of <paramref name="type"/> whose generic type definition
        /// matches <paramref name="baseTypeDefinition"/>. For interface bases this yields every
        /// implementing instantiation (a type can implement the same interface definition with
        /// different type arguments); for class bases it yields at most the first match found
        /// while walking the base-type chain (only one such instantiation is reachable).
        ///
        /// IMPORTANT: This implementation mirrors
        /// <c>System.Text.Json.SourceGeneration.RoslynExtensions.GetCompatibleGenericBaseTypes</c>
        /// in gen/Helpers/RoslynExtensions.cs. Any change to the enumeration order or matching
        /// rules MUST be applied on both sides to keep reflection and source-gen behaviour in sync.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The derived type was supplied via [JsonDerivedType] by the user, so its interface " +
                            "metadata is rooted at the attribute usage site and survives trimming. Callers are " +
                            "additionally annotated [RequiresUnreferencedCode] to flow this requirement outward.")]
        public static IEnumerable<Type> GetMatchingGenericBaseTypes(this Type type, Type baseTypeDefinition)
        {
            Debug.Assert(baseTypeDefinition.IsGenericTypeDefinition);

            if (baseTypeDefinition.IsInterface)
            {
                foreach (Type iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == baseTypeDefinition)
                    {
                        yield return iface;
                    }
                }

                // Note: do NOT yield break here. Type.GetInterfaces() does not include `type`
                // itself, so when `type` IS the interface we're looking for, the fall-through
                // to the BaseType walk below picks it up via the self-check on the first
                // iteration (Type.BaseType returns null for interfaces, so the loop
                // terminates immediately).
            }

            for (Type? current = type; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == baseTypeDefinition)
                {
                    yield return current;
                    yield break;
                }
            }
        }

        /// <summary>
        /// Attempts to unify a <paramref name="pattern"/> type (which may contain generic
        /// parameter references) with a <paramref name="target"/> type, recording bindings in
        /// <paramref name="substitution"/>. Returns <see langword="true"/> if the pattern matches
        /// the target under some extension of the current substitution.
        ///
        /// IMPORTANT: This implementation MIRRORS
        /// <c>System.Text.Json.SourceGeneration.RoslynExtensions.TryUnifyWith</c> in
        /// gen/Helpers/RoslynExtensions.cs. Any structural change (e.g. new type-kind handling,
        /// refined array/pointer rules) MUST be applied on both sides to keep reflection and
        /// source-gen behaviour in sync. The two implementations are exercised by:
        ///   * tests/.../PolymorphicTests.CustomTypeHierarchies.cs (reflection)
        ///   * tests/.../JsonSourceGeneratorDiagnosticsTests.cs (source-gen)
        ///
        /// Known intentional asymmetries with the source-gen mirror:
        ///   * This implementation distinguishes SZ arrays (<c>T[]</c>) from rank-1
        ///     multi-dimensional arrays (<c>T[*]</c>) via <c>Type.IsSZArray</c> on .NET. Roslyn
        ///     surfaces both as <c>IArrayTypeSymbol</c> with <c>Rank == 1</c>, and C#
        ///     <c>typeof()</c> attribute syntax only produces SZ arrays, so the source-gen
        ///     mirror has no equivalent check.
        ///   * This implementation has a branch for <c>Type.IsByRef</c>; Roslyn never surfaces
        ///     ref types as generic arguments, so the source-gen mirror has no equivalent.
        /// </summary>
        public static bool TryUnifyWith(this Type pattern, Type target, IDictionary<Type, Type> substitution)
        {
            if (pattern.IsGenericParameter)
            {
                if (substitution.TryGetValue(pattern, out Type? existing))
                {
                    return existing == target;
                }

                substitution[pattern] = target;
                return true;
            }

            if (pattern.IsArray)
            {
                if (!target.IsArray)
                {
                    return false;
                }

                if (pattern.GetArrayRank() != target.GetArrayRank())
                {
                    return false;
                }

                // Distinguish single-dim zero-based arrays (T[]) from non-SZ rank-1 arrays (T[*]).
#if NET
                if (pattern.IsSZArray != target.IsSZArray)
                {
                    return false;
                }
#endif

                return pattern.GetElementType()!.TryUnifyWith(target.GetElementType()!, substitution);
            }

            if (pattern.IsPointer)
            {
                if (!target.IsPointer)
                {
                    return false;
                }

                return pattern.GetElementType()!.TryUnifyWith(target.GetElementType()!, substitution);
            }

            if (pattern.IsByRef)
            {
                if (!target.IsByRef)
                {
                    return false;
                }

                return pattern.GetElementType()!.TryUnifyWith(target.GetElementType()!, substitution);
            }

            if (pattern.IsGenericType)
            {
                if (!target.IsGenericType || pattern.GetGenericTypeDefinition() != target.GetGenericTypeDefinition())
                {
                    return false;
                }

                Type[] patternArgs = pattern.GetGenericArguments();
                Type[] targetArgs = target.GetGenericArguments();
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

            return pattern == target;
        }

        // The following is a line-by-line port of the Roslyn C# compiler's own accessibility comparison — the
        // logic it runs to report the "inconsistent accessibility" diagnostics (CS0050/CS0060 and friends). An
        // inferred closed-hierarchy derived type is kept only when it is at least as visible as the base it is
        // registered under; otherwise there are call sites that can reference the base but not the derived type.
        // Rather than invent an accessibility metric (which would risk drifting from the language as new
        // modifiers are added), we reproduce the compiler's algorithm verbatim, using its terminology, so it
        // tracks C# accessibility as it evolves. The reflection resolver has no Roslyn Compilation, so the
        // algorithm is reproduced here over System.Type; gen/Helpers/RoslynExtensions.cs mirrors it over ISymbol.
        //
        // Ported from dotnet/roslyn @ 121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2:
        //   IsAtLeastAsVisibleAs / FindTypeLessVisibleThan / IsAsRestrictive:
        //     https://github.com/dotnet/roslyn/blob/121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2/src/Compilers/CSharp/Portable/Symbols/TypeSymbolExtensions.cs#L1048
        //   IsAccessibleViaInheritance:
        //     https://github.com/dotnet/roslyn/blob/121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2/src/Compilers/CSharp/Portable/Symbols/SymbolExtensions.cs#L48
        //   HasInternalAccessTo:
        //     https://github.com/dotnet/roslyn/blob/121e7dc868d26be12b9c3fb52b7b9d2ae41a1ac2/src/Compilers/CSharp/Portable/Binder/Semantics/AccessCheck.cs#L676

        /// <summary>
        /// Determines whether <paramref name="type"/> is at least as visible as <paramref name="sym"/>. Port of
        /// Roslyn's <c>TypeSymbolExtensions.IsAtLeastAsVisibleAs</c>; because a closed hierarchy relates two
        /// named types, the compound-type traversal (<c>FindTypeLessVisibleThan</c>/<c>Symbol.VisitType</c>)
        /// reduces to the single <c>IsAsRestrictive</c> check.
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public static bool IsAtLeastAsVisibleAs(this Type type, Type sym)
        {
            return IsAsRestrictive(type, sym);

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            static bool IsAsRestrictive(Type s1, Type sym2)
            {
                Accessibility acc1 = GetDeclaredAccessibility(s1);

                if (acc1 == Accessibility.Public)
                {
                    return true;
                }

                for (Type? s2 = sym2; s2 is not null; s2 = s2.DeclaringType)
                {
                    Accessibility acc2 = GetDeclaredAccessibility(s2);

                    switch (acc1)
                    {
                        case Accessibility.Internal:
                            // If s2 is private or internal, and is in an assembly that gives s1's assembly internal
                            // access, then this is at least as restrictive as s1's internal.
                            if (acc2 is Accessibility.Private or Accessibility.Internal or Accessibility.ProtectedAndInternal &&
                                HasInternalAccessTo(s2.Assembly, s1.Assembly))
                            {
                                return true;
                            }

                            break;

                        case Accessibility.ProtectedAndInternal:
                            // Since s1 is private protected, s2 must be more restrictive than both internal and
                            // protected. Do the "internal" test first (as above); if it passes, fall through to the
                            // "protected" test.
                            if (acc2 is Accessibility.Private or Accessibility.Internal or Accessibility.ProtectedAndInternal &&
                                HasInternalAccessTo(s2.Assembly, s1.Assembly))
                            {
                                goto case Accessibility.Protected;
                            }

                            break;

                        case Accessibility.Protected:
                        {
                            Type? parent1 = s1.DeclaringType;

                            if (parent1 is null)
                            {
                                // not helpful
                            }
                            else if (acc2 == Accessibility.Private)
                            {
                                // if s2 is private and within s1's parent or within a subclass of s1's parent,
                                // then this is at least as restrictive as s1's protected.
                                for (Type? parent2 = s2.DeclaringType; parent2 is not null; parent2 = parent2.DeclaringType)
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
                                Type? parent2 = s2.DeclaringType;
                                if (parent2 is not null && IsAccessibleViaInheritance(parent1, parent2))
                                {
                                    return true;
                                }
                            }

                            break;
                        }

                        case Accessibility.ProtectedOrInternal:
                        {
                            Type? parent1 = s1.DeclaringType;

                            if (parent1 is null)
                            {
                                break;
                            }

                            switch (acc2)
                            {
                                case Accessibility.Private:
                                    // if s2 is private and within a subclass of s1's parent, or within the same
                                    // assembly as s1, then this is at least as restrictive as s1's protected internal.
                                    if (HasInternalAccessTo(s2.Assembly, s1.Assembly))
                                    {
                                        return true;
                                    }

                                    for (Type? parent2 = s2.DeclaringType; parent2 is not null; parent2 = parent2.DeclaringType)
                                    {
                                        if (IsAccessibleViaInheritance(parent1, parent2))
                                        {
                                            return true;
                                        }
                                    }

                                    break;

                                case Accessibility.Internal:
                                    // If s2 is in an assembly that gives s1's assembly internal access, then this
                                    // is more restrictive than s1's protected internal.
                                    if (HasInternalAccessTo(s2.Assembly, s1.Assembly))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.Protected:
                                    // if s2 is protected, and its parent is a subclass of (or the same as) s1's
                                    // parent, then this is at least as restrictive as s1's protected internal.
                                    if (s2.DeclaringType is Type protectedParent2 && IsAccessibleViaInheritance(parent1, protectedParent2))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.ProtectedAndInternal:
                                    // if s2 is private protected, and its parent is a subclass of (or the same as)
                                    // s1's parent, or it is in the same assembly as s1, then this is at least as
                                    // restrictive as s1's protected internal.
                                    if (HasInternalAccessTo(s2.Assembly, s1.Assembly) ||
                                        (s2.DeclaringType is Type privateProtectedParent2 && IsAccessibleViaInheritance(parent1, privateProtectedParent2)))
                                    {
                                        return true;
                                    }

                                    break;

                                case Accessibility.ProtectedOrInternal:
                                    // if s2 is protected internal, and its parent is a subclass of (or the same as)
                                    // s1's parent, and it is in the same assembly as s1, then this is at least as
                                    // restrictive as s1's protected internal.
                                    if (HasInternalAccessTo(s2.Assembly, s1.Assembly) &&
                                        s2.DeclaringType is Type protectedOrInternalParent2 && IsAccessibleViaInheritance(parent1, protectedOrInternalParent2))
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
                                Type? parent1 = s1.DeclaringType;

                                if (parent1 is null)
                                {
                                    break;
                                }

                                Type parent1OriginalDefinition = OriginalDefinition(parent1);
                                for (Type? parent2 = s2.DeclaringType; parent2 is not null; parent2 = parent2.DeclaringType)
                                {
                                    if (ReferenceEquals(OriginalDefinition(parent2), parent1OriginalDefinition))
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

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            static bool IsAccessibleViaInheritance(Type superType, Type subType)
            {
                Type originalSuperType = OriginalDefinition(superType);
                for (Type? current = subType; current is not null; current = current.BaseType)
                {
                    if (ReferenceEquals(OriginalDefinition(current), originalSuperType))
                    {
                        return true;
                    }
                }

                if (originalSuperType.IsInterface)
                {
                    foreach (Type current in subType.GetInterfaces())
                    {
                        if (ReferenceEquals(OriginalDefinition(current), originalSuperType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            static bool HasInternalAccessTo(Assembly fromAssembly, Assembly toAssembly)
            {
                if (fromAssembly == toAssembly)
                {
                    return true;
                }

                // Reflection analog of Roslyn's AreInternalsVisibleToThisAssembly. Closed hierarchies are compiled
                // into a single assembly, so this branch only matters for the general faithfulness of the port; the
                // friend assembly is matched by simple name (the runtime already enforced strong-name identity when
                // it loaded the types).
                string? fromName = fromAssembly.GetName().Name;
                foreach (CustomAttributeData attribute in toAssembly.GetCustomAttributesData())
                {
                    if (attribute.AttributeType == typeof(InternalsVisibleToAttribute) &&
                        attribute.ConstructorArguments.Count > 0 &&
                        attribute.ConstructorArguments[0].Value is string friendName)
                    {
                        int comma = friendName.IndexOf(',');
                        string friendSimpleName = comma < 0 ? friendName : friendName.Substring(0, comma);
                        if (string.Equals(friendSimpleName, fromName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            static Type OriginalDefinition(Type type) =>
                type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;

            static Accessibility GetDeclaredAccessibility(Type type)
            {
                if (type.IsPublic || type.IsNestedPublic)
                {
                    return Accessibility.Public;
                }

                if (type.IsNestedFamORAssem)
                {
                    return Accessibility.ProtectedOrInternal; // protected internal
                }

                if (type.IsNestedFamily)
                {
                    return Accessibility.Protected;
                }

                if (type.IsNestedFamANDAssem)
                {
                    return Accessibility.ProtectedAndInternal; // private protected
                }

                if (type.IsNestedPrivate)
                {
                    return Accessibility.Private;
                }

                // Top-level non-public (IsNotPublic) and nested assembly (IsNestedAssembly) are both 'internal'.
                return Accessibility.Internal;
            }
        }

        /// <summary>
        /// Mirrors the members of Roslyn's <see cref="T:Microsoft.CodeAnalysis.Accessibility"/> that a C# type
        /// declaration can have, so the ported accessibility comparison uses the compiler's terminology.
        /// </summary>
        private enum Accessibility
        {
            Private,
            ProtectedAndInternal, // private protected
            Protected,
            Internal,
            ProtectedOrInternal, // protected internal
            Public,
        }
    }
}
