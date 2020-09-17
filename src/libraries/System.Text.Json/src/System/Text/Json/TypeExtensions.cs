// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Text.Json
{
    internal static class TypeExtensions
    {
        private static readonly Type s_NullableType = typeof(Nullable<>);

        /// <summary>
        /// Returns <see langword="true" /> when the given type is of type <see cref="Nullable{T}"/>.
        /// </summary>
        public static bool IsNullableOfT(this Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == s_NullableType;

        /// <summary>
        /// Returns <see langword="true" /> when the given type is either a reference type or of type <see cref="Nullable{T}"/>.
        /// </summary>
        /// <remarks>This calls <see cref="Type.IsValueType"/> which is slow. If knowledge already exists
        /// that the type is a value type, call <see cref="IsNullableOfT"/> instead.</remarks>
        public static bool CanBeNull(this Type type) =>
            !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == s_NullableType);

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
    }
}
