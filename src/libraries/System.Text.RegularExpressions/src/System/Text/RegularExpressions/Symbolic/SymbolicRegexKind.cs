// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Kinds of symbolic regexes</summary>
    internal enum SymbolicRegexKind
    {
        StartAnchor = 1,
        EndAnchor = 2,
        Epsilon = 4,
        Singleton = 8,
        Or = 0x10,
        Concat = 0x20,
        Loop = 0x40,
        Not = 0x80,
        And = 0x100,
        WatchDog = 0x200,
        BOLAnchor = 0x400,
        EOLAnchor = 0x800,
        WBAnchor = 0x1000,
        NWBAnchor = 0x2000,
        EndAnchorZ = 0x4000,
        /// <summary>Anchor for very first line or start-line after very first \n arises as the reverse of EndAnchorZ</summary>
        EndAnchorZRev = 0x8000,
    }
}
