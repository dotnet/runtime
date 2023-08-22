// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Provides access to portions of the Serialization Guard APIs since they're not publicly exposed via contracts.
    /// </summary>
    internal static partial class SerializationGuard
    {
        /// <summary>
        /// Provides access to the internal "ThrowIfDeserializationInProgress" method on <see cref="SerializationInfo"/>.
        /// No-ops if the Serialization Guard feature is disabled or unavailable.
        /// </summary>
        public static void ThrowIfDeserializationInProgress(string switchSuffix, ref int cachedValue)
        {
            ThrowIfDeserializationInProgress(null, switchSuffix, ref cachedValue);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ThrowIfDeserializationInProgress")]
            static extern void ThrowIfDeserializationInProgress(SerializationInfo? thisPtr, string switchSuffix, ref int cachedValue);
        }
    }
}
