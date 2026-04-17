// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Text.RegularExpressions
{
    /// <summary>Creates a <see cref="RegexRunner"/> for a <see cref="Regex"/>.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class RegexRunnerFactory
    {
        /// <summary>Initializes a new instance of the <see cref="RegexRunnerFactory"/> class.</summary>
        protected RegexRunnerFactory() { }

        /// <summary>Creates a <see cref="RegexRunner"/> instance for the <see cref="Regex"/>.</summary>
        /// <returns>A <see cref="RegexRunner"/> instance.</returns>
        protected internal abstract RegexRunner CreateInstance();
    }
}
