// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.FileSystemGlobbing
{
    /// <summary>
    /// Represents a collection of <see cref="FilePatternMatch" />
    /// </summary>
    public class PatternMatchingResult
    {
        /// <summary>
        /// Initializes the result with a collection of <see cref="FilePatternMatch" />
        /// </summary>
        /// <param name="files">A collection of <see cref="FilePatternMatch" /></param>
        public PatternMatchingResult(IEnumerable<FilePatternMatch> files)
            : this(files, hasMatches: files.Any())
        {
            Files = files;
        }

        /// <summary>
        /// Initializes the result with a collection of <see cref="FilePatternMatch" />
        /// </summary>
        /// <param name="files">A collection of <see cref="FilePatternMatch" /></param>
        /// <param name="hasMatches">A value that determines if <see cref="PatternMatchingResult"/> has any matches.</param>
        public PatternMatchingResult(IEnumerable<FilePatternMatch> files, bool hasMatches)
        {
            ThrowHelper.ThrowIfNull(files);

            Files = files;
            HasMatches = hasMatches;
        }

        /// <summary>
        /// A collection of <see cref="FilePatternMatch" />
        /// </summary>
        public IEnumerable<FilePatternMatch> Files { get; set; }

        /// <summary>
        /// Gets a value that determines if this instance of <see cref="PatternMatchingResult"/> has any matches.
        /// </summary>
        public bool HasMatches { get; }
    }
}
