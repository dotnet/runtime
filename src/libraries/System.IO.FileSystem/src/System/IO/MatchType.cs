// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    public enum MatchType
    {
        /// <summary>
        /// Match using '*' and '?' wildcards.
        /// </summary>
        Simple,

        /// <summary>
        /// Match using Win32 DOS style matching semantics. '*', '?', '&lt;', '&gt;', and '"'
        /// are all considered wildcards.
        /// </summary>
        Win32
    }
}
