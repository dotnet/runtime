// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Text.Json
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Returns <see langword="true" /> when the given type is of type <see cref="Nullable{T}"/>.
        /// </summary>
        public static bool IsNullableValueType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Returns <see langword="true" /> when the given type is either a reference type or of type <see cref="Nullable{T}"/>.
        /// </summary>
        public static bool IsNullableType(this Type type)
        {
            return !type.IsValueType || IsNullableValueType(type);
        }

        /// <summary>
        /// Returns <see langword="true" /> when the given type is assignable from <paramref name="from"/>.
        /// </summary>
        /// <remarks>
        /// Other than <see cref="Type.IsAssignableFrom(Type)"/> also returns <see langword="true" /> when <paramref name="type"/> is of type <see cref="Nullable{T}"/> where <see langword="T" /> : <see langword="IInterface" /> and <paramref name="from"/> is of type <see langword="IInterface" />.
        /// </remarks>
        public static bool IsAssignableFromInternal(this Type type, Type from)
        {
            if (IsNullableValueType(from) && type.IsInterface)
            {
                return type.IsAssignableFrom(from.GetGenericArguments()[0]);
            }

            return type.IsAssignableFrom(from);
        }
    }
}
