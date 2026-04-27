// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Represents a single line of text read from a process's standard output or standard error stream.
    /// </summary>
    public readonly struct ProcessOutputLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessOutputLine"/> struct.
        /// </summary>
        /// <param name="content">The text content of the output line.</param>
        /// <param name="standardError">
        /// <see langword="true" /> if the line was read from standard error;
        /// otherwise, <see langword="false" />.
        /// </param>
        public ProcessOutputLine(string content, bool standardError)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            StandardError = standardError;
        }

        /// <summary>
        /// Gets the text content of the output line.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets a value that indicates whether the line was read from standard error.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the line was read from standard error;
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool StandardError { get; }
    }
}
