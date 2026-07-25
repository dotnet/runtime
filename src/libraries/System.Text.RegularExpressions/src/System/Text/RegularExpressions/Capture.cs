// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>Represents the results from a single successful subexpression capture.</summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Capture"/> object is immutable and has no public constructor. Instances are returned
    /// through the <see cref="CaptureCollection"/> object, which is returned by the
    /// <see cref="Group.Captures"/> property. However, the <c>Match.Captures</c> property provides
    /// information about the same match as the
    /// <see cref="Match"/> object.
    /// </para>
    /// <para>
    /// If you do not apply a quantifier to a capturing group, the <see cref="Group.Captures"/>
    /// property returns a <see cref="CaptureCollection"/> with a single <see cref="Capture"/> object
    /// that provides information about the same capture as the <see cref="Group"/> object. If you do
    /// apply a quantifier to a capturing group, the <c>Group.Index</c>, <c>Group.Length</c>, and
    /// <c>Group.Value</c> properties provide information only about the last captured group, whereas
    /// the <see cref="Capture"/> objects in the <see cref="CaptureCollection"/> provide information
    /// about all subexpression captures.
    /// </para>
    /// </remarks>
    public class Capture
    {
        internal Capture(string? text, int index, int length)
        {
            Text = text;
            Index = index;
            Length = length;
        }

        /// <summary>
        /// Gets the position in the original string where the first character of the captured substring is found.
        /// </summary>
        /// <value>The zero-based starting position in the original string where the captured substring is found.</value>
        public int Index { get; private protected set; }

        /// <summary>Gets the length of the captured substring.</summary>
        /// <value>The length of the captured substring.</value>
        public int Length { get; private protected set; }

        /// <summary>The original string</summary>
        internal string? Text { get; set; }

        /// <summary>Gets the captured substring from the input string.</summary>
        /// <value>The substring that is captured by the match.</value>
        /// <remarks>
        /// If a call to the <see cref="Regex.Match(string)"/> or <see cref="Match.NextMatch"/> method fails to
        /// find a match, the value of the returned <c>Match.Value</c> property is <see cref="string.Empty"/>.
        /// If the regular expression engine is unable to match a capturing group, the value of the returned
        /// <c>Group.Value</c> property is <see cref="string.Empty"/>.
        /// </remarks>
        public string Value => Text is string text ? text.Substring(Index, Length) : string.Empty;

        /// <summary>Gets the captured span from the input string.</summary>
        /// <value>The span that is captured by the match.</value>
        public ReadOnlySpan<char> ValueSpan => Text is string text ? text.AsSpan(Index, Length) : [];

        /// <summary>
        /// Retrieves the captured substring from the input string by calling the
        /// <see cref="Value"/> property.
        /// </summary>
        /// <returns>The substring that was captured by the match.</returns>
        /// <remarks>
        /// <c>ToString</c> is actually an internal call to the <see cref="Value"/> property.
        /// </remarks>
        public override string ToString() => Value;

        /// <summary>The substring to the left of the capture</summary>
        internal ReadOnlyMemory<char> GetLeftSubstring() => Text is string text ? text.AsMemory(0, Index) : ReadOnlyMemory<char>.Empty;

        /// <summary>The substring to the right of the capture</summary>
        internal ReadOnlyMemory<char> GetRightSubstring() => Text is string text ? text.AsMemory(Index + Length, Text.Length - Index - Length) : ReadOnlyMemory<char>.Empty;
    }
}
