// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    /// <summary>Specifies the type of character casing to match.</summary>
    public enum MatchCasing
    {
        /// <summary>Matches using the default casing for the given platform.</summary>
        PlatformDefault,

        /// <summary>Matches respecting character casing.</summary>
        CaseSensitive,

        /// <summary>Matches ignoring character casing.</summary>
        CaseInsensitive
    }
}
