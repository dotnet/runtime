// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Provides the core data describing a parsed <see cref="RegexNode"/> tree, along with necessary
    /// information about captures in the tree and computed optimizations about its structure.
    /// </summary>
    internal sealed class RegexTree
    {
        /// <summary>The options associated with the regular expression.</summary>
        public readonly RegexOptions Options;
        /// <summary>The root node of the parsed <see cref="RegexNode"/> tree.</summary>
        public readonly RegexNode Root;
        /// <summary>The "find" optimizations computed for the regular expression to quickly find the next viable location to start looking for a match.</summary>
        public readonly RegexFindOptimizations FindOptimizations;
        /// <summary>The number of captures in the regex.</summary>
        public readonly int CaptureCount;
        /// <summary>If the pattern has backreferences and uses IgnoreCase, we cache the Culture at creation time to use it at match time.</summary>
        public readonly CultureInfo? Culture;
        /// <summary>A list of all the captures' names.</summary>
        /// <remarks>
        /// For numbered (implicitly or explicitly) captures, these are string representations of the numbers.  This may be null if all captures were numbered
        /// and dense, e.g. for `(a)(bc)(def)` and `(?&lt;1&gt;a)(?&lt;2&gt;bc)(?&lt;3&gt;def)` this will be null, but it will be non-null for
        /// `(?&lt;1&gt;a)(?&lt;2&gt;bc)(?&lt;4&gt;def)` as well as for `(?&lt;2&gt;a)(?&lt;3&gt;bc)(?&lt;4&gt;def)`, as the groups now have a gap in the numbering.
        /// </remarks>
        public readonly string[]? CaptureNames;
        /// <summary>A mapping of capture group name to capture group number.</summary>
        /// <remarks>This is null iff <see cref="CaptureNames"/> is not null.</remarks>
        public readonly Hashtable? CaptureNameToNumberMapping;
        /// <summary>A mapping of capture group number to the associated name slot in <see cref="CaptureNames"/>.</summary>
        /// <remarks>
        /// This is non-null if the mapping is sparse. If non-null, each key/value pair entry represents one capture group, where the key is the
        /// capture group number and the value is the index into <see cref="CaptureNames"/> for that capture group.
        /// </remarks>
        public readonly Hashtable? CaptureNumberSparseMapping;

        internal RegexTree(RegexNode root, int captureCount, string[]? captureNames, Hashtable? captureNameToNumberMapping, Hashtable? captureNumberSparseMapping, RegexOptions options, CultureInfo? culture)
        {
#if DEBUG
            // Asserts to both demonstrate and validate the relationships between the various capture data structures.
            Debug.Assert(captureNumberSparseMapping is null || captureNames is not null);
            Debug.Assert((captureNames is null) == (captureNameToNumberMapping is null));
            Debug.Assert(captureNames is null || captureCount == captureNames.Length);
            Debug.Assert(captureNumberSparseMapping is null || captureCount == captureNumberSparseMapping.Count);
            Debug.Assert(captureNameToNumberMapping is null || captureCount == captureNameToNumberMapping.Count);
            if (captureNames is not null)
            {
                Debug.Assert(captureNameToNumberMapping is not null);
                for (int i = 0; i < captureNames.Length; i++)
                {
                    string captureName = captureNames[i];

                    int? captureNumber = captureNameToNumberMapping[captureName] as int?;
                    Debug.Assert(captureNumber is not null);

                    if (captureNumberSparseMapping is not null)
                    {
                        captureNumber = captureNumberSparseMapping[captureNumber] as int?;
                        Debug.Assert(captureNumber is not null);
                    }

                    Debug.Assert(captureNumber == i);
                }
            }
#endif

            Root = root;
            Culture = culture;
            CaptureNumberSparseMapping = captureNumberSparseMapping;
            CaptureCount = captureCount;
            CaptureNameToNumberMapping = captureNameToNumberMapping;
            CaptureNames = captureNames;
            Options = options;
            FindOptimizations = new RegexFindOptimizations(root, options);
        }
    }
}
