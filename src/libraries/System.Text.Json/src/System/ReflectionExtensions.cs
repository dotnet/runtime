// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
