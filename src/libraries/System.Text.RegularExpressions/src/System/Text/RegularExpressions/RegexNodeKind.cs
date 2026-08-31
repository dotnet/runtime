// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>Specifies the kind of a <see cref="RegexNode"/>.</summary>
    internal enum RegexNodeKind : byte
    {
        /// <summary>Unknown node type.</summary>
        /// <remarks>This should never occur on an actual node, and instead is used as a sentinel.</remarks>
        Unknown = 0,

        // The following are leaves (no children) and correspond to primitive operations in the regular expression.

        /// <summary>A specific character, e.g. `a`.</summary>
        /// <remarks>The character is specified in <see cref="RegexNode.Ch"/>.</remarks>
        One = RegexOpcode.One,
        /// <summary>Anything other than a specific character, e.g. `.` when not in <see cref="RegexOptions.Singleline"/> mode, or `[^a]`.</summary>
        /// <remarks>The character is specified in <see cref="RegexNode.Ch"/>.</remarks>
        Notone = RegexOpcode.Notone,
        /// <summary>A character class / set, e.g. `[a-z1-9]` or `\w`.</summary>
        /// <remarks>The <see cref="RegexCharClass"/> set string is specified in <see cref="RegexNode.Str"/>.</remarks>
        Set = RegexOpcode.Set,

        /// <summary>A sequence of at least two specific characters, e.g. `abc`.</summary>
        /// <remarks>The characters are specified in <see cref="RegexNode.Str"/>.  This is purely a representational optimization, equivalent to multiple <see cref="One"/> nodes concatenated together.</remarks>
        Multi = RegexOpcode.Multi,

        /// <summary>A loop around a specific character, e.g. `a*`.</summary>
        /// <remarks>
        /// The character is specified in <see cref="RegexNode.Ch"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.
        /// This is purely a representational optimization, equivalent to a <see cref="Loop"/> wrapped around a <see cref="One"/>.
        /// </remarks>
        Oneloop = RegexOpcode.Oneloop,
        /// <summary>A loop around anything other than a specific character, e.g. `.*` when not in <see cref="RegexOptions.Singleline"/> mode, or `[^a]*`.</summary>
        /// <remarks>The character is specified in <see cref="RegexNode.Ch"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.</remarks>
        /// This is purely a representational optimization, equivalent to a <see cref="Loop"/> wrapped around a <see cref="Notone"/>.
        Notoneloop = RegexOpcode.Notoneloop,
        /// <summary>A loop around a character class / set, e.g. `[a-z1-9]*` or `\w*`.</summary>
        /// <remarks>The <see cref="RegexCharClass"/> set string is specified in <see cref="RegexNode.Str"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.</remarks>
        /// This is purely a representational optimization, equivalent to a <see cref="Loop"/> wrapped around a <see cref="Set"/>.
        Setloop = RegexOpcode.Setloop,

        /// <summary>A lazy loop around a specific character, e.g. `a*?`.</summary>
        /// <remarks>The character is specified in <see cref="RegexNode.Ch"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.</remarks>
        /// This is purely a representational optimization, equivalent to a <see cref="Lazyloop"/> wrapped around a <see cref="One"/>.
        Onelazy = RegexOpcode.Onelazy,
        /// <summary>A lazy loop around anything other than a specific character, e.g. `.*?` when not in <see cref="RegexOptions.Singleline"/> mode, or `[^a]*?`.</summary>
        /// <remarks>The character is specified in <see cref="RegexNode.Ch"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.</remarks>
        /// This is purely a representational optimization, equivalent to a <see cref="Lazyloop"/> wrapped around a <see cref="Notone"/>.
        Notonelazy = RegexOpcode.Notonelazy,
        /// <summary>A lazy loop around a character class / set, e.g. `[a-z1-9]*?` or `\w?`.</summary>
        /// <remarks>The <see cref="RegexCharClass"/> set string is specified in <see cref="RegexNode.Str"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.</remarks>
        /// This is purely a representational optimization, equivalent to a <see cref="Lazyloop"/> wrapped around a <see cref="Set"/>.
        Setlazy = RegexOpcode.Setlazy,

        /// <summary>An atomic loop around a specific character, e.g. `(?> a*)`.</summary>
        /// <remarks>
        /// The character is specified in <see cref="RegexNode.Ch"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.
        /// This is purely a representational optimization, equivalent to a <see cref="Atomic"/> wrapped around a <see cref="Oneloop"/>.
        /// </remarks>
        Oneloopatomic = RegexOpcode.Oneloopatomic,
        /// <summary>An atomic loop around anything other than a specific character, e.g. `(?>.*)` when not in <see cref="RegexOptions.Singleline"/> mode.</summary>
        /// <remarks>
        /// The character is specified in <see cref="RegexNode.Ch"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.
        /// This is purely a representational optimization, equivalent to a <see cref="Atomic"/> wrapped around a <see cref="Notoneloop"/>.
        /// </remarks>
        Notoneloopatomic = RegexOpcode.Notoneloopatomic,
        /// <summary>An atomic loop around a character class / set, e.g. `(?>\d*)`.</summary>
        /// <remarks>
        /// The <see cref="RegexCharClass"/> set string is specified in <see cref="RegexNode.Str"/>, the minimum number of iterations in <see cref="RegexNode.M"/>, and the maximum number of iterations in <see cref="RegexNode.N"/>.
        /// This is purely a representational optimization, equivalent to a <see cref="Atomic"/> wrapped around a <see cref="Setloop"/>.
        /// </remarks>
        Setloopatomic = RegexOpcode.Setloopatomic,

        /// <summary>A backreference, e.g. `\1`.</summary>
        /// <remarks>The capture group number referenced is stored in <see cref="RegexNode.M"/>.</remarks>
        Backreference = RegexOpcode.Backreference,

        /// <summary>A beginning-of-line anchor, e.g. `^` in <see cref="RegexOptions.Multiline"/> mode.</summary>
        Bol = RegexOpcode.Bol,
        /// <summary>An end-of-line anchor, e.g. `$` in <see cref="RegexOptions.Multiline"/> mode.</summary>
        Eol = RegexOpcode.Eol,
        /// <summary>A word boundary anchor, e.g. `\b`.</summary>
        Boundary = RegexOpcode.Boundary,
        /// <summary>Not a word boundary anchor, e.g. `\B`.</summary>
        NonBoundary = RegexOpcode.NonBoundary,
        /// <summary>A word boundary anchor, e.g. `\b` in <see cref="RegexOptions.ECMAScript"/> mode.</summary>
        ECMABoundary = RegexOpcode.ECMABoundary,
        /// <summary>Not a word boundary anchor, e.g. `\B` in <see cref="RegexOptions.ECMAScript"/> mode..</summary>
        NonECMABoundary = RegexOpcode.NonECMABoundary,
        /// <summary>A beginning-of-string anchor, e.g. `\A`, or `^` when not in <see cref="RegexOptions.Multiline"/> mode.</summary>
        Beginning = RegexOpcode.Beginning,
        /// <summary>A start anchor, e.g. `\G`.</summary>
        Start = RegexOpcode.Start,
        /// <summary>A end-of-string-or-before-ending-newline anchor, e.g. `\Z`, or `$` when not in <see cref="RegexOptions.Multiline"/> mode.</summary>
        EndZ = RegexOpcode.EndZ,
        /// <summary>A end-of-string-only anchor, e.g. `\z`.</summary>
        End = RegexOpcode.End,

        /// <summary>A fabricated node injected during analyses to signal a location in the matching where the engine may set the next bumpalong position to the current position.</summary>
        UpdateBumpalong = RegexOpcode.UpdateBumpalong,

        /// <summary>Fails when matching an empty string, e.g. `(?!)`.</summary>
        Nothing = RegexOpcode.Nothing,
        /// <summary>Matches the empty string, e.g. ``.</summary>
        Empty = 23,

        // The following are interior nodes (have at least one child) and correspond to control structures composing other operations.

        /// <summary>An alternation between branches, e.g. `ab|cd`.</summary>
        /// <remarks>
        /// Each child represents one branch, in lexical order.  A valid alternation contains at
        /// least two children: if an alternation contains only a single child, it can be replaced
        /// by that child, and if an alternation has no children, it can be replaced by <see cref="Nothing"/>.
        /// </remarks>
        Alternate = 24,
        /// <summary>A sequence / concatenation of nodes, e.g. a[bc].</summary>
        /// <remarks>
        /// Each child represents one node in the sequence, in lexical order.  A valid concatenation contains at
        /// least two children: if a concatenation contains only a single child, it can be replaced
        /// by that child, and if a concatenation has no children, it can be replaced by <see cref="Empty"/>.
        /// </remarks>
        Concatenate = 25,

        /// <summary>A loop around an arbitrary <see cref="RegexNode"/>, e.g. `(ab|cd)*`.</summary>
        /// <remarks>
        /// One and only one child, the expression in the loop. The minimum number of iterations is in <see cref="RegexNode.M"/>,
        /// and the maximum number of iterations is in <see cref="RegexNode.N"/>.
        /// </remarks>
        Loop = 26,
        /// <summary>A lazy loop around an arbitrary <see cref="RegexNode"/>, e.g. `(ab|cd)*?`.</summary>
        /// <remarks>
        /// One and only one child, the expression in the loop. The minimum number of iterations is in <see cref="RegexNode.M"/>,
        /// and the maximum number of iterations is in <see cref="RegexNode.N"/>.
        /// </remarks>
        Lazyloop = 27,

        /// <summary>A capture group, e.g. `(\w*)`.</summary>
        /// <remarks>
        /// One and only one child, the expression in the capture. <see cref="RegexNode.M"/> is the number of the capture, and if a balancing
        /// group, <see cref="RegexNode.N"/> is the uncapture.
        /// </remarks>
        Capture = 28,
        /// <summary>A non-capturing group, e.g. `(?:ab|cd)`.</summary>
        /// <remarks>
        /// One and only one child, the expression in the group. Groups are irrelevant after parsing and can be replaced entirely by their child.
        /// These should not be in a valid tree returned from the parsing / reduction phases of processing.
        /// </remarks>
        Group = 29,
        /// <summary>An atomic group, e.g. `(?>ab|cd)`.</summary>
        /// <remarks>One and only one child, the expression in the group.</remarks>
        Atomic = 32,

        /// <summary>
        /// A positive lookaround assertion: lookahead if <see cref="RegexOptions.RightToLeft"/> is not set and lookbehind if
        /// <see cref="RegexOptions.RightToLeft"/> is set, e.g. `(?=abc)` or `(?&lt;=abc)`.</summary>
        /// <remarks>One and only one child, the expression in the assertion.</remarks>
        PositiveLookaround = 30,
        /// <summary>
        /// A negative lookaround assertion: lookahead if <see cref="RegexOptions.RightToLeft"/> is not set and lookbehind if
        /// <see cref="RegexOptions.RightToLeft"/> is set, e.g. `(?!abc)` or `(?&lt;!abc)`.</summary>
        /// <remarks>One and only one child, the expression in the assertion.</remarks>
        NegativeLookaround = 31,

        /// <summary>A backreference conditional, e.g. `(?(1)abc|def)`.</summary>
        /// <remarks>
        /// Two children, the first to use if the reference capture group matched and the second to use if it didn't.
        /// The referenced capture group number is stored in <see cref="RegexNode.M"/>.
        /// </remarks>
        BackreferenceConditional = 33,
        /// <summary>An expression conditional, e.g. `(?(\d{3})123456|abc)`.</summary>
        /// <remarks>
        /// Three children. The first is the expression to evaluate as a positive lookahead assertion, the second is
        /// the expression to match if the positive lookahead assertion was successful, and the third is the expression
        /// to match if the positive lookahead assertion was unsuccessful.
        /// </remarks>
        ExpressionConditional = 34,
    }
}
