// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        /// <summary>Returns a comparison that can be used to compare file and directory names for equality.</summary>
        internal static StringComparison GetComparison()
        {
            return StringComparison.Ordinal;
        }
    }
}
