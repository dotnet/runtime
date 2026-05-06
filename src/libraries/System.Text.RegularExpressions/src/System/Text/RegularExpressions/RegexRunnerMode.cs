// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>Represents the mode of execution for a <see cref="RegexRunner"/>.</summary>
    internal enum RegexRunnerMode
    {
        /// <summary>The runner need only determine whether the input has a match; no additional information is required.</summary>
        /// <remarks>This mode is used by Regex.IsMatch.</remarks>
        ExistenceRequired,

        /// <summary>The runner needs to determine the next location and length of a match in the input; no additional information is required.</summary>
        /// <remarks>This mode is used by Regex.Count, Regex.EnumerateMatches, and Regex.Replace (when the replacement doesn't involve backreferences).</remarks>
        BoundsRequired,

        /// <summary>The runner needs to determine the next location and length of a match in the input, as well as the full details on all captures.</summary>
        /// <remarks>This mode is used by Regex.Match, Regex.Matches, Regex.Split, and Regex.Replace (when the replacement involves backreferences).</remarks>
        FullMatchRequired,
    }
}
