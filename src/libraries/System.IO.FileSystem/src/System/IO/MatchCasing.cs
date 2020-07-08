// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
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
