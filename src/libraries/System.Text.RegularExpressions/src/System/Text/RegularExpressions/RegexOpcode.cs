// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>Opcodes written by <see cref="RegexWriter"/> and used by <see cref="RegexInterpreter"/> to process a regex.</summary>
    /// <remarks>
    /// <see cref="RegexInterpreterCode"/> stores an int[] containing all of the codes that make up the instructions for
    /// the interpreter to process the regular expression.  The array contains a packed sequence of operations,
    /// each of which is an <see cref="RegexOpcode"/> stored as an int, followed immediately by all of the operands
    /// required for that operation.  For example, the subexpression `a{2,7}[^b]` would be represented as the sequence
    ///     0 97 2 3 97 5 10 98
    /// which is interpreted as:
    ///     0  = opcode for Onerep (a{2, 7} is written out as a repeater for the minimum followed by a loop for the maximum minus the minimum)
    ///     97 = 'a'
    ///     2  = repeat count
    ///     3  = opcode for Oneloop
    ///     97 = 'a'
    ///     5  = max iteration count
    ///     10 = opcode for Notone
    ///     98 = 'b'
    /// </remarks>
    internal enum RegexOpcode
    {
        // Primitive operations

        /// <summary>Repeater of the specified character.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the repetition count.</remarks>
        Onerep = 0,

        /// <summary>Repeater of a single character other than the one specified.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the repetition count.</remarks>
        Notonerep = 1,

        /// <summary>Repeater of a single character matching the specified set</summary>
        /// <remarks>Operand 0 is index into the strings table of the character class description. Operand 1 is the repetition count.</remarks>
        Setrep = 2,

        /// <summary>Greedy loop of the specified character.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the max iteration count.</remarks>
        Oneloop = 3,

        /// <summary>Greedy loop of a single character other than the one specified.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the max iteration count.</remarks>
        Notoneloop = 4,

        /// <summary>Greedy loop of a single character matching the specified set</summary>
        /// <remarks>Operand 0 is index into the strings table of the character class description. Operand 1 is the repetition count.</remarks>
        Setloop = 5,

        /// <summary>Lazy loop of the specified character.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the max iteration count.</remarks>
        Onelazy = 6,

        /// <summary>Lazy loop of a single character other than the one specified.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the max iteration count.</remarks>
        Notonelazy = 7,

        /// <summary>Lazy loop of a single character matching the specified set</summary>
        /// <remarks>Operand 0 is index into the strings table of the character class description. Operand 1 is the repetition count.</remarks>
        Setlazy = 8,

        /// <summary>Single specified character.</summary>
        /// <remarks>Operand 0 is the character.</remarks>
        One = 9,

        /// <summary>Single character other than the one specified.</summary>
        /// <remarks>Operand 0 is the character.</remarks>
        Notone = 10,

        /// <summary>Single character matching the specified set.</summary>
        /// <remarks>Operand 0 is index into the strings table of the character class description.</remarks>
        Set = 11,

        /// <summary>Multiple characters in sequence.</summary>
        /// <remarks>Operand 0 is index into the strings table for the string of characters.</remarks>
        Multi = 12,

        /// <summary>Backreference to a capture group.</summary>
        /// <remarks>Operand 0 is the capture group number.</remarks>
        Backreference = 13,

        /// <summary>Beginning-of-line anchor (^ with RegexOptions.Multiline).</summary>
        Bol = 14,
        /// <summary>End-of-line anchor ($ with RegexOptions.Multiline).</summary>
        Eol = 15,
        /// <summary>Word boundary (\b).</summary>
        Boundary = 16,
        /// <summary>Word non-boundary (\B).</summary>
        NonBoundary = 17,
        /// <summary>Beginning-of-input anchor (\A).</summary>
        Beginning = 18,
        /// <summary>Start-of-input anchor (\G).</summary>
        Start = 19,
        /// <summary>End-of-input anchor (\Z).</summary>
        EndZ = 20,
        /// <summary>End-of-input anchor (\z).</summary>
        End = 21,
        /// <summary>Match nothing (fail).</summary>
        Nothing = 22,
        /// <summary>Word boundary (\b with RegexOptions.ECMAScript).</summary>
        ECMABoundary = 41,
        /// <summary>Word non-boundary (\B with RegexOptions.ECMAScript).</summary>
        NonECMABoundary = 42,

        /// <summary>Atomic loop of the specified character.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the max iteration count.</remarks>
        Oneloopatomic = 43,
        /// <summary>Atomic loop of a single character other than the one specified.</summary>
        /// <remarks>Operand 0 is the character. Operand 1 is the max iteration count.</remarks>
        Notoneloopatomic = 44,
        /// <summary>Atomic loop of a single character matching the specified set</summary>
        /// <remarks>Operand 0 is index into the strings table of the character class description. Operand 1 is the repetition count.</remarks>
        Setloopatomic = 45,

        /// <summary>Updates the bumpalong position to the current position.</summary>
        UpdateBumpalong = 46,

        // Primitive control structures

        /// <summary>Lazy branch in an alternation or conditional construct.</summary>
        /// <remarks>
        /// On first execution, the opcode records the current input position (via the tracking stack) and continues straight
        /// without taking the jump. When the matching that follows fails, backtracking will occur and the saved position is restored,
        /// at which point the interpreter will jump to the alternative branch (using the patched jump offset in operand 0).
        /// This opcode is used to implement alternation in a non-greedy (lazy) manner.
        /// </remarks>
        Lazybranch = 23,

        /// <summary>Branch in a quantified loop that uses a saved mark to decide whether to repeat or exit.</summary>
        /// <remarks>
        /// When executed, this opcode pops a previously saved input mark (from a <see cref="Setmark"/> or <see cref="Nullmark"/>)
        /// and compares it to the current input position. If the loop's inner expression has consumed input (non-empty match), it
        /// pushes updated state (saving the old mark and the current position) and jumps back (via the jump offset in operand 0)
        /// to repeat the loop. If no progress has been made (empty match), it records state for backtracking and proceeds.
        /// This opcode is used for greedy (non-lazy) quantified loops when no explicit counter is needed.
        /// </remarks>
        Branchmark = 24,

        /// <summary>Lazy branch in a quantified loop that uses a saved mark.</summary>
        /// <remarks>
        /// Similar in spirit to <see cref="Branchmark"/>, this opcode is used for lazy loops.
        /// It initially does not jump back to repeat the loop, preferring to let the overall match continue.
        /// However, it saves the loop state so that if subsequent matching fails, backtracking will re-enter the loop body.
        /// Special care is taken to handle empty matches so as to avoid infinite loops.
        /// </remarks>
        Lazybranchmark = 25,

        /// <summary>Initialize the loop counter for a quantifier when the minimum repetition is zero.</summary>
        /// <remarks>
        /// For quantified constructs with a minimum of zero (<see cref="RegexNode.M"/> == 0), this opcode pushes a counter
        /// value (-1) along with a marker (implicitly indicating no match so far) onto the grouping stack. The operand (always 0
        /// in this case) is used in later comparisons within a <see cref="Branchcount"/> or <see cref="Lazybranchcount"/> opcode.
        /// </remarks>
        Nullcount = 26,

        /// <summary>Initialize the loop counter for a quantifier with a positive minimum.</summary>
        /// <remarks>
        /// When the quantifier requires at least one match (M > 0), this opcode pushes the current input position as a marker and a
        /// counter value computed as (1 - M) onto the grouping stack. This counter will be adjusted in subsequent loop iterations
        /// (via <see cref="Branchcount"/> or <see cref="Lazybranchcount"/>) to decide whether the loop should continue.
        /// </remarks>
        Setcount = 27,

        /// <summary>Greedy counted branch for quantified loops.</summary>
        /// <remarks>
        /// This opcode is used for quantified loops that require a counter. When executed, it pops the previously stored marker and counter
        /// from the grouping stack, computes the difference between the current input position and the marker, and compares the counter
        /// against a limit (given in operand 1). If the counter indicates that more iterations are allowed (and the inner expression consumed
        /// input), it increments the counter, updates the marker with the new position, and jumps (via the jump offset in operand 0) to
        /// repeat the loop. Otherwise, the interpreter continues straight. On backtracking, the previous state is restored so that a decreased
        /// count may be tried.
        /// </remarks>
        Branchcount = 28,

        /// <summary>Lazy counted branch for quantified loops.</summary>
        /// <remarks>
        /// This opcode is the lazy counterpart to <see cref="Branchcount"/>. It is used in quantified loops that use a counter and prefer
        /// to exit the loop as early as possible. On initial execution it will choose the straight path (i.e. not repeating the loop) if
        /// the counter is nonnegative, but if the inner expression consumed input and the counter is below the maximum (given in operand 1),
        /// it will re-enter the loop on backtracking.
        /// </remarks>
        Lazybranchcount = 29,

        /// <summary>Push a null marker into the grouping stack for quantifiers with a minimum of zero when no explicit counter is needed.</summary>
        /// <remarks>
        /// This opcode is similar to <see cref="Nullcount"/> but is used in cases where the quantified construct does not require counting;
        /// it pushes a marker value (-1) onto the grouping stack to record the starting position. On backtracking, the marker is simply removed.
        /// </remarks>
        Nullmark = 30,

        /// <summary>Push the current input position onto the grouping stack.</summary>
        /// <remarks>
        /// Used by grouping constructs (for capturing or to detect empty matches in loops), this opcode saves the current input position
        /// so that later the interpreter can compare it to the current position to decide whether progress was made. It is the non-counting
        /// counterpart to <see cref="Setcount"/>.
        /// </remarks>
        Setmark = 31,

        /// <summary>Completes a capturing group.</summary>
        /// <remarks>
        /// When executed, this opcode pops a previously saved marker (the start position of the group) from the grouping stack and uses the
        /// current input position as the end position. Operand 0 specifies the capture slot number. If operand 1 is not -1 then a prior capture
        /// must have been made and a transfer of capture is performed. On backtracking, the capture is undone.
        /// </remarks>
        Capturemark = 32,

        /// <summary>Recall a previously saved marker.</summary>
        /// <remarks>
        /// This opcode restores the input position from a marker saved on the grouping stack (typically via a <see cref="Setmark"/> or
        /// <see cref="Nullmark"/>). It is used in lookaround constructs to revert the input position to the point where the lookaround began.
        /// On backtracking, the marker is re-pushed onto the grouping stack.
        /// </remarks>
        Getmark = 33,

        /// <summary>Mark the beginning of a non-backtracking / atomic region.</summary>
        /// <remarks>
        /// This opcode is used at the start of constructs that must not be re-entered on backtracking (such as lookahead/lookbehind or atomic groups).
        /// It saves the current backtracking state (including the current tracking and crawl positions) onto the grouping stack.
        /// When the region is later exited (by <see cref="Forejump"/>) the saved state is used to prevent further backtracking into the region.
        /// </remarks>
        Setjump = 34,

        /// <summary>Restore state for a non-backtracking / atomic region on backtracking.</summary>
        /// <remarks>
        /// Used in negative lookaround constructs, this opcode pops the saved backtracking and capture state (stored by a prior <see cref="Setjump"/>)
        /// and erases any changes made within the non-backtracking region. It thereby restores the state to what it was before entering the region.
        /// </remarks>
        Backjump = 35,

        /// <summary>Finalize a non-backtracking / atomic region.</summary>
        /// <remarks>
        /// This opcode is used at the end of lookaround or atomic group constructs to commit to the current matching path.
        /// It pops the saved state from the grouping stack (stored by <see cref="Setjump"/>), updates the tracking pointer (thereby
        /// discarding any backtracking state from within the region), and then continues execution. On backtracking from such a region,
        /// a variant of this opcode will undo any captures made.
        /// </remarks>
        Forejump = 36,

        /// <summary>Test whether a particular backreference has already matched.</summary>
        /// <remarks>
        /// Operand 0 is the capture group number to test. When executed, if the specified group has not captured any text,
        /// the match fails and control transfers to backtracking. Otherwise, execution continues. This opcode is used in conditional
        /// constructs where a branch is taken only if a given capture exists.
        /// </remarks>
        TestBackreference = 37,

        /// <summary>Unconditional jump.</summary>
        /// <remarks>
        /// Operand 0 holds the target offset. When executed, the interpreter jumps unconditionally to that location.
        /// This opcode is used to implement control flow for alternation and loop constructs.
        /// </remarks>
        Goto = 38,

        /// <summary>Halt the interpreter.</summary>
        /// <remarks>
        /// This opcode marks the end of the opcode stream. When reached, the matching process terminates and the result
        /// (whether a match was found) is returned.
        /// </remarks>
        Stop = 40,

        // Modifiers for alternate modes

        /// <summary>Mask to get unmodified ordinary operator.</summary>
        OperatorMask = 63,

        /// <summary>Indicates that we're reverse scanning.</summary>
        RightToLeft = 64,

        /// <summary>Indicates that we're backtracking.</summary>
        Backtracking = 128,

        /// <summary>Indicates that we're backtracking on a second branch.</summary>
        /// <remarks>
        /// In patterns with alternations or complex quantifiers, multiple backtracking paths may be available.
        /// This flag marks opcodes that are being processed on an alternate (or secondary) branch during backtracking,
        /// as opposed to the primary branch. The interpreter uses this flag to apply specialized state restoration
        /// or branch-selection logic when reverting from one branch to another.
        /// </remarks>
        BacktrackingSecond = 256,

        /// <summary>Indicates that we're case-insensitive.</summary>
        CaseInsensitive = 512,
    }
}
