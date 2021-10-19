// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary><see cref="RegexRunnerFactory"/> for symbolic regexes.</summary>
    internal sealed class SymbolicRegexRunnerFactory : RegexRunnerFactory
    {
        /// <summary>Shared runner instance.</summary>
        internal readonly SymbolicRegexRunner _runner;

        /// <summary>Initializes the factory.</summary>
        /// <param name="runner">Shared runner instance.</param>
        public SymbolicRegexRunnerFactory(SymbolicRegexRunner runner) => _runner = runner;

        /// <summary>Creates a <see cref="RegexRunner"/> object.</summary>
        /// <remarks>
        /// <see cref="SymbolicRegexRunner"/> instances are observably immutable and thread-safe.
        /// All calls to <see cref="CreateInstance"/> return the same shared instance.
        /// </remarks>
        protected internal override RegexRunner CreateInstance() => _runner;
    }
}
