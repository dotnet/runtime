// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>Represents the results from a single capturing group.</summary>
    /// <remarks>
    /// <para>
    /// A capturing group can capture zero, one, or more strings in a single match because of
    /// quantifiers. (For more information, see
    /// <see href="https://github.com/dotnet/docs/blob/main/docs/standard/base-types/quantifiers-in-regular-expressions.md">Quantifiers</see>.)
    /// All the substrings matched by a single capturing group are available from the
    /// <see cref="Captures"/> property. Information about the last substring captured can be accessed
    /// directly from the <c>Value</c> and <c>Index</c> properties. (That is, the <see cref="Group"/>
    /// instance is equivalent to the last item of the collection returned by the <see cref="Captures"/>
    /// property, which reflects the last capture made by the capturing group.)
    /// </para>
    /// </remarks>
    public class Group : Capture
    {
        internal static readonly Group s_emptyGroup = new Group(string.Empty, [], 0, string.Empty);

        internal readonly int[] _caps;
        internal int _capcount;
        internal CaptureCollection? _capcoll;

        internal Group(string? text, int[] caps, int capcount, string name)
            : base(text, capcount == 0 ? 0 : caps[(capcount - 1) * 2], capcount == 0 ? 0 : caps[(capcount * 2) - 1])
        {
            _caps = caps;
            _capcount = capcount;
            Name = name;
        }

        /// <summary>Gets a value indicating whether the match is successful.</summary>
        /// <value>
        /// <see langword="true"/> if the match is successful; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// The <c>Success</c> property is <see langword="true"/> if at least one substring was captured
        /// by this group. It is equivalent to the Boolean expression
        /// <c>Group.Captures.Count &gt; 0</c>.
        /// </remarks>
        public bool Success => _capcount != 0;

        /// <summary>Returns the name of the capturing group represented by the current instance.</summary>
        /// <value>The name of the capturing group represented by the current instance.</value>
        public string Name { get; }

        /// <summary>
        /// Gets a collection of all the captures matched by the capturing group, in
        /// innermost-leftmost-first order (or innermost-rightmost-first order if the regular expression
        /// is modified with the <see cref="RegexOptions.RightToLeft"/> option). The collection may have
        /// zero or more items.
        /// </summary>
        /// <value>The collection of substrings matched by the group.</value>
        /// <remarks>
        /// <para>
        /// If a quantifier is not applied to a capturing group, the collection returned by the
        /// <see cref="Captures"/> property contains a single <see cref="Capture"/> object that provides
        /// information about the same substring as the <see cref="Group"/> object.
        /// </para>
        /// <para>
        /// The real utility of the <see cref="Captures"/> property occurs when a quantifier is applied
        /// to a capturing group so that the group captures multiple substrings in a single regular
        /// expression. In this case, the <see cref="Group"/> object contains information about the last
        /// captured substring, whereas the <see cref="Captures"/> property contains information about
        /// all the substrings captured by the group.
        /// </para>
        /// </remarks>
        public CaptureCollection Captures => _capcoll ??= new CaptureCollection(this);

        /// <summary>
        /// Returns a <see cref="Group"/> object equivalent to the one supplied that is safe to share
        /// between multiple threads.
        /// </summary>
        /// <param name="inner">The input <see cref="Group"/> object.</param>
        /// <returns>A regular expression <see cref="Group"/> object.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inner"/> is <see langword="null"/>.
        /// </exception>
        public static Group Synchronized(Group inner)
        {
            if (inner == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
            }

            // force Captures to be computed.
            CaptureCollection capcoll = inner.Captures;
            if (inner.Success)
            {
                capcoll.ForceInitialized();
            }

            return inner;
        }
    }
}
