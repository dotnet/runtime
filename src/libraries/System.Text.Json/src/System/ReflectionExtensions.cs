// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public static bool HasRequiredMemberAttribute(this ICustomAttributeProvider memberInfo)
        {
            // For compiler related attributes we should only look at full type name rather than trying to do something different for version when attribute was introduced.
            // I.e. library is targetting netstandard2.0 with polyfilled attributes and is being consumed by app targetting net7.0.
            return memberInfo.HasCustomAttributeWithName("System.Runtime.CompilerServices.RequiredMemberAttribute", inherit: true);
        }

        public static bool HasSetsRequiredMembersAttribute(this ICustomAttributeProvider memberInfo)
        {
            // See comment for HasRequiredMemberAttribute for why we need to always only look at full name
            return memberInfo.HasCustomAttributeWithName("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute", inherit: true);
        }

        private static bool HasCustomAttributeWithName(this ICustomAttributeProvider memberInfo, string fullName, bool inherit)
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
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)] this Type type,
            Type[] parameterTypes,
            object?[] parameters)
        {
            ConstructorInfo ctorInfo = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null)!;
#if NETCOREAPP
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
    }
}
