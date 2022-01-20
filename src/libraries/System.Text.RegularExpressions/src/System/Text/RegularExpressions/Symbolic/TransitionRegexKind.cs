// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Kinds of transition regexes. Transition regexes maintain a DNF form that pushes all intersections and complements to the leaves.</summary>
    internal enum TransitionRegexKind
    {
        Leaf,
        Conditional,
        Union,
        Lookaround
    }
}
