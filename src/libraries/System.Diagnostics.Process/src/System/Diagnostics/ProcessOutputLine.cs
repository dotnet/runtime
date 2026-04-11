// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Represents a single line of output from a process, along with whether it came from standard error.
    /// </summary>
    public readonly struct ProcessOutputLine
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessOutputLine"/> struct.
        /// </summary>
        /// <param name="content">The text content of the output line.</param>
        /// <param name="standardError"><see langword="true"/> if this line came from standard error; <see langword="false"/> if it came from standard output.</param>
        public ProcessOutputLine(string content, bool standardError)
        {
            ArgumentNullException.ThrowIfNull(content);
            Content = content;
            StandardError = standardError;
        }

        /// <summary>
        /// Gets the text content of the output line.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets a value indicating whether this line came from the standard error stream.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this line came from standard error; <see langword="false"/> if it came from standard output.
        /// </value>
        public bool StandardError { get; }
    }
}
