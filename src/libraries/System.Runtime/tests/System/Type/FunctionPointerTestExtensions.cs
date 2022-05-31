// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Tests.Types
{
    internal static class FunctionPointerTestExtensions
    {
        /// <summary>
        /// A noop to allow tests to be shared with MetadataLoadContext.
        /// </summary>
        public static Type Project(this Type type) => type;

        /// <summary>
        /// Check for type equality; runtime Types compare via Equals and ReferenceEquals
        /// </summary>
        public static bool IsEqualOrReferenceEquals(this Type type, Type other) => type.Equals(other) && ReferenceEquals(type, other);
    }
}
