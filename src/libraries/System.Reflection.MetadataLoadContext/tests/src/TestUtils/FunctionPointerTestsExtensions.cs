// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The runtime tests have the name spacespace and methods to facilitate sharing.
namespace System.Reflection.Tests
{
    internal static class FunctionPointerTestsExtensions
    {
        public static bool IsMetadataLoadContext => true;

        /// <summary>
        /// Do a type comparison; RO Types compare via == or .Equals, not ReferenceEquals
        /// </summary>
        public static bool IsFunctionPointerEqual(this Type type, Type other) => type == other && type.Equals(other);

        /// <summary>
        /// Do a type comparison; RO Types compare via == or .Equals, not ReferenceEquals
        /// </summary>
        public static bool IsFunctionPointerNotEqual(this Type type, Type other) => type != other && !type.Equals(other);
    }
}
