// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
    internal static class NullabilityHelpers
    {
        /// <summary>
        /// Suppresses CS8777: "Parameter 'name' must have a non-null value when exiting."
        /// Used when a method accepts a 'ref' parameter that should be non-null on
        /// method exit.
        /// </summary>
        [Conditional("NEVER")]
        public static void SuppressNonNullAssignmentWarning<T>([NotNull] ref T value)
            where T : class?
        {
            Unsafe.SkipInit(out value!); // no-op
        }
    }
}
