// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Tests
{
    internal static class FunctionPointerTestsExtensions
    {
        /// <summary>
        /// A noop to allow tests to be shared with MetadataLoadContext.
        /// </summary>
        public static Type Project(this Type type) => type;

        public static bool IsMetadataLoadContext => false;

        /// <summary>
        /// Check for type equality using all 3 equality checks
        /// </summary>
        public static bool IsFunctionPointerEqual(this Type type, Type other) =>
            (type == other) && type.Equals(other) && ReferenceEquals(type, other);

        /// <summary>
        /// Check for type equality using all 3 equality checks
        /// </summary>
        public static bool IsFunctionPointerNotEqual(this Type type, Type other) =>
            (type != other) && !type.Equals(other) && !ReferenceEquals(type, other);
    }
}
