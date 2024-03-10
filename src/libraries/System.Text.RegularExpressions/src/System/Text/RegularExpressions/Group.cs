// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the results from a single capturing group. A capturing group can
    /// capture zero, one, or more strings in a single match because of quantifiers, so
    /// Group supplies a collection of Capture objects.
    /// </summary>
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

        /// <summary>Indicates whether the match is successful.</summary>
        public bool Success => _capcount != 0;

        public string Name { get; }

        /// <summary>
        /// Returns a collection of all the captures matched by the capturing
        /// group, in innermost-leftmost-first order (or innermost-rightmost-first order if
        /// compiled with the "r" option). The collection may have zero or more items.
        /// </summary>
        public CaptureCollection Captures => _capcoll ??= new CaptureCollection(this);

        /// <summary>
        /// Returns a Group object equivalent to the one supplied that is safe to share between multiple threads.
        /// </summary>
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
