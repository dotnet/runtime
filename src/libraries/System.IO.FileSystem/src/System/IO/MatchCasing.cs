// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    public enum MatchCasing
    {
        /// <summary>
        /// Match the default casing for the given platform
        /// </summary>
        PlatformDefault,

        /// <summary>
        /// Match respecting character casing
        /// </summary>
        CaseSensitive,

        /// <summary>
        /// Match ignoring character casing
        /// </summary>
        CaseInsensitive
    }
}
