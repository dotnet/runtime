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
        // TODO: Figure out what these comments mean / what these control structures actually do :)

        /// <summary>back     jump            straight first.</summary>
        Lazybranch = 23,
        /// <summary>back     jump            branch first for loop.</summary>
        Branchmark = 24,
        /// <summary>back     jump            straight first for loop.</summary>
        Lazybranchmark = 25,
        /// <summary>back     val             set counter, null mark.</summary>
        Nullcount = 26,
        /// <summary>back     val             set counter, make mark</summary>
        Setcount = 27,
        /// <summary>back     jump,limit      branch++ if zero&lt;=c&lt;limit.</summary>
        Branchcount = 28,
        /// <summary>back     jump,limit      same, but straight first.</summary>
        Lazybranchcount = 29,
        /// <summary>back                     save position.</summary>
        Nullmark = 30,
        /// <summary>back                     save position.</summary>
        Setmark = 31,
        /// <summary>back     group           define group.</summary>
        Capturemark = 32,
        /// <summary>back                     recall position.</summary>
        Getmark = 33,
        /// <summary>back                     save backtrack state.</summary>
        Setjump = 34,
        /// <summary>zap back to saved state.</summary>
        Backjump = 35,
        /// <summary>zap backtracking state.</summary>
        Forejump = 36,
        /// <summary>Backtrack if ref undefined.</summary>
        TestBackreference = 37,
        /// <summary>jump            just go.</summary>
        Goto = 38,
        /// <summary>done!</summary>
        Stop = 40,

        // Modifiers for alternate modes

        /// <summary>Mask to get unmodified ordinary operator.</summary>
        OperatorMask = 63,
        /// <summary>Indicates that we're reverse scanning.</summary>
        RightToLeft = 64,
        /// <summary>Indicates that we're backtracking.</summary>
        Backtracking = 128,
        /// <summary>Indicates that we're backtracking on a second branch.</summary>
        BacktrackingSecond = 256,
        /// <summary>Indicates that we're case-insensitive</summary>
        CaseInsensitive = 512,
    }
}
