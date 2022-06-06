// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Kinds of <see cref="SymbolicRegexNode{S}"/>.</summary>
    internal enum SymbolicRegexNodeKind
    {
        /// <summary>An empty node that matches a zero-width input (e.g. <see cref="RegexNodeKind.Empty"/>).</summary>
        Epsilon,
        /// <summary>A node that matches a single character (i.e. <see cref="RegexNodeKind.One"/>, <see cref="RegexNodeKind.Notone"/>, or <see cref="RegexNodeKind.Set"/>).</summary>
        Singleton,
        /// <summary>A node that matches a sequence of nodes (i.e. <see cref="RegexNodeKind.Concatenate"/>).</summary>
        Concat,
        /// <summary>A node that matches a loop (e.g. <see cref="RegexNodeKind.Loop"/>, <see cref="RegexNodeKind.Lazyloop"/>, <see cref="RegexNodeKind.Setloop"/>, etc.).</summary>
        Loop,
        /// <summary>A node that matches if any of its nodes match and that matches them in a fixed order that mirrors how the backtracking engines operate (e.g. <see cref="RegexNodeKind.Alternate"/>).</summary>
        Alternate,

        /// <summary>A node that represents a beginning anchor (i.e. <see cref="RegexNodeKind.Beginning"/>).</summary>
        BeginningAnchor,
        /// <summary>A node that represents an ending anchor (i.e. <see cref="RegexNodeKind.End"/>).</summary>
        EndAnchor,
        /// <summary>A node that represents an ending \Z anchor (i.e. <see cref="RegexNodeKind.EndZ"/>).</summary>
        EndAnchorZ,
        /// <summary>A node that represents an anchor for the very first line or start-line after the very first \n arises as the reverse of <see cref="EndAnchorZ"/>.</summary>
        EndAnchorZReverse,
        /// <summary>A node that represents a beginning-of-line anchor (i.e. <see cref="RegexNodeKind.Bol"/>).</summary>
        BOLAnchor,
        /// <summary>A node that represents a end-of-line anchor (i.e. <see cref="RegexNodeKind.Eol"/>).</summary>
        EOLAnchor,
        /// <summary>A node that represents a word boundary anchor (i.e. <see cref="RegexNodeKind.Boundary"/>).</summary>
        BoundaryAnchor,
        /// <summary>A node that represents a word non-boundary anchor (i.e. <see cref="RegexNodeKind.NonBoundary"/>).</summary>
        NonBoundaryAnchor,

        /// <summary>Marker node that carries with it an indication of how long the fixed-length sequence is until that point.</summary>
        /// <remarks>
        /// <see cref="SymbolicRegexNode{S}._lower"/> stores the fixed length.  This node is used to avoid the second phase
        /// of matching (for non-IsMatch operations, which only have the first phase).  The first phase determines whether
        /// there is a match and its possible ending position, at which point the second phase matches in reverse to find
        /// the starting position, and then a third phase again matches forward from the now known starting position to
        /// find the guaranteed ending position of the match.  If we have a <see cref="FixedLengthMarker"/>, we can use it
        /// to avoid the second phase of matching by simply jumping backwards the fixed length to the starting position.
        /// </remarks>
        FixedLengthMarker,

        /// <summary>Effects to be applied when taking a transition.</summary>
        /// <remarks>
        /// Left child is the pattern itself and the right child is a concatenation of nodes whose effects should be applied.
        /// Effect nodes are created in the rule for concatenation in <see cref="SymbolicRegexNode{TSet}.CreateDerivative(TSet, uint)"/>,
        /// where they are used to represent additional operations that should be performed in the current position if
        /// the pattern in the left child is used to match the input. Since these Effect nodes are relative to the current
        /// position in the input, the effects from the right child must be applied in the transition that the derivative is
        /// being created for. This is done by translating effects to <see cref="DerivativeEffect"/> structs, which provide
        /// a more convenient form for storing and repeatedly executing the effects. Additionally, Effect nodes must be
        /// stripped away before creating the target state(s) for the transition, since an Effect doesn't itself have a
        /// derivative due to its "positionless" behavior.
        /// This representation of first recording effects in Effect nodes was chosen to allow simplification rules to kick
        /// in before any potentially unnecessary work to gather and manage effects is performed. Effect nodes allow
        /// delaying work until it is known that mapping CaptureStart and CaptureEnd nodes to <see cref="DerivativeEffect"/>
        /// is necessary.
        /// Note that the right child of an Effect node does not only contain CaptureStart and other nodes that have side
        /// effects. Rather, the right child can be any pattern and when mapping to <see cref="DerivativeEffect"/> the function
        /// <see cref="SymbolicRegexNode{TSet}.ApplyEffects"/> finds which effects would be encountered by the backtracking
        /// engines when taking a nullable path through the right child.
        /// </remarks>
        Effect,
        /// <summary>Indicates the start of a subcapture.</summary>
        /// <remarks><see cref="SymbolicRegexNode{S}._lower"/> stores the associated capture number.</remarks>
        CaptureStart,
        /// <summary>Indicates the end of a subcapture.</summary>
        /// <remarks><see cref="SymbolicRegexNode{S}._lower"/> stores the associated capture number.</remarks>
        CaptureEnd,

        /// <summary>
        /// This node disables backtracking simulation in derivatives for the pattern it contains. This is used for the the
        /// second reverse phase of the match generation algorithm, where its needed to ensure all paths are considered when
        /// walking backwards from a known final state.
        /// </summary>
        /// <remarks>
        /// If any other metadata nodes are needed that would have the same structure, having just one node kind for this and
        /// the other uses might make sense.
        /// </remarks>
        DisableBacktrackingSimulation,
    }
}
