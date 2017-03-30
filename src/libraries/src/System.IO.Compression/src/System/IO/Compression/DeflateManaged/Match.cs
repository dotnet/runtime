// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    /// <summary>
    /// This class represents a match in the history window.
    /// </summary>
    internal sealed class Match
    {
        internal MatchState State { get; set; }
        internal int Position { get; set; }
        internal int Length { get; set; }
        internal byte Symbol { get; set; }
    }
}
