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
            => constructorInfo.GetCustomAttribute<JsonConstructorAttribute>() != null;

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
            object? result = null;
            try
            {
                result = ctorInfo.Invoke(parameters);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }

            return result;
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

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="derivedParam"/>'s declared constraints
        /// match <paramref name="baseParam"/>'s declared constraints exactly, after applying
        /// <paramref name="substitution"/> to type-constraint references. Used by the polymorphism
        /// resolver to validate that every closure of the base type that respects the base's
        /// constraints would also be a valid closure of the open derived type.
        ///
        /// IMPORTANT: This implementation MIRRORS
        /// <c>System.Text.Json.SourceGeneration.RoslynExtensions.AreConstraintsEquivalent</c>.
        /// Any change to the equivalence rules must be applied on both sides.
        ///
        /// Known intentional asymmetry with the source-gen mirror: the C# <c>unmanaged</c>
        /// constraint is encoded as a modreq and is not surfaced through the standard
        /// reflection API, so this check cannot enforce that a derived <c>unmanaged</c>
        /// constraint is matched by a base <c>unmanaged</c> constraint. The polymorphism
        /// resolver falls back on <see cref="Type.MakeGenericType"/> to surface any
        /// remaining constraint violations at closure time.
        /// </summary>
        [RequiresUnreferencedCode("Reflects over derived and base generic parameter constraint types.")]
        [RequiresDynamicCode("Substitutes type parameters in constraint types.")]
        public static bool AreConstraintsEquivalent(
            Type derivedParam,
            Type baseParam,
            IReadOnlyDictionary<Type, Type> substitution)
        {
            Debug.Assert(derivedParam.IsGenericParameter);
            Debug.Assert(baseParam.IsGenericParameter);

            const GenericParameterAttributes Mask = GenericParameterAttributes.SpecialConstraintMask;
            if ((derivedParam.GenericParameterAttributes & Mask) != (baseParam.GenericParameterAttributes & Mask))
            {
                return false;
            }

            Type[] derivedConstraints = derivedParam.GetGenericParameterConstraints();
            Type[] baseConstraints = baseParam.GetGenericParameterConstraints();
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
            var baseSet = new HashSet<Type>(baseConstraints);
            foreach (Type derivedConstraint in derivedConstraints)
            {
                Type substituted = SubstituteTypeParameters(derivedConstraint, substitution);
                if (!baseSet.Remove(substituted))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Walks a <see cref="Type"/> tree and substitutes any occurrence of a type parameter
        /// (whose key is present in <paramref name="substitution"/>) with its mapped value.
        /// </summary>
        [RequiresUnreferencedCode("Reflects over the type tree to build a substituted constraint type.")]
        [RequiresDynamicCode("Constructs a new substituted generic type via MakeGenericType.")]
        private static Type SubstituteTypeParameters(Type type, IReadOnlyDictionary<Type, Type> substitution)
        {
            if (type.IsGenericParameter)
            {
                return substitution.TryGetValue(type, out Type? mapped) ? mapped : type;
            }

            if (type.IsArray)
            {
                Type element = type.GetElementType()!;
                Type substitutedElement = SubstituteTypeParameters(element, substitution);
                if (substitutedElement == element)
                {
                    return type;
                }

#if NET
                return type.IsSZArray ? substitutedElement.MakeArrayType() : substitutedElement.MakeArrayType(type.GetArrayRank());
#else
                int rank = type.GetArrayRank();
                return rank == 1 ? substitutedElement.MakeArrayType() : substitutedElement.MakeArrayType(rank);
#endif
            }

            if (type.IsPointer)
            {
                Type element = type.GetElementType()!;
                Type substitutedElement = SubstituteTypeParameters(element, substitution);
                return substitutedElement == element ? type : substitutedElement.MakePointerType();
            }

            if (type.IsByRef)
            {
                Type element = type.GetElementType()!;
                Type substitutedElement = SubstituteTypeParameters(element, substitution);
                return substitutedElement == element ? type : substitutedElement.MakeByRefType();
            }

            if (type.IsGenericType)
            {
                Type[] args = type.GetGenericArguments();
                Type[]? newArgs = null;
                for (int i = 0; i < args.Length; i++)
                {
                    Type substitutedArg = SubstituteTypeParameters(args[i], substitution);
                    if (substitutedArg != args[i])
                    {
                        newArgs ??= (Type[])args.Clone();
                        newArgs[i] = substitutedArg;
                    }
                }

                return newArgs is null ? type : type.GetGenericTypeDefinition().MakeGenericType(newArgs);
            }

            return type;
        }
    }
}
