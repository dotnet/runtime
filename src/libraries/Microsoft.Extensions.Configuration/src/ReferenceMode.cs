// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Controls how an <see cref="IConfigurationSource"/> participates in <c>ref(...) / fmt(...)</c>
    /// reference resolution performed by the <see cref="IConfigurationRoot"/> built from the
    /// containing <see cref="IConfigurationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Values are totally ordered by participation: <see cref="Ignore"/> &lt; <see cref="Read"/>
    /// &lt; <see cref="Scan"/>. Sources default to <see cref="Read"/> unless configured via
    /// <see cref="ReferenceResolutionConfigurationBuilderExtensions.SetReferenceMode(IConfigurationBuilder, ReferenceMode)"/>
    /// or one of its overloads. At least one source must be <see cref="Scan"/> for the
    /// reference-resolution engine to activate on the built root.
    /// </remarks>
    public enum ReferenceMode
    {
        /// <summary>
        /// The source is invisible to the reference-resolution engine: no <c>ref(...) / fmt(...)</c>
        /// reference or section alias in another source can reach its values. Direct reads
        /// via the normal <see cref="IConfiguration"/> API still return its values.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// The source is a valid substitution target for references in other
        /// <see cref="Scan"/> sources. The source's own values are returned verbatim —
        /// <c>ref(...) / fmt(...)</c> sequences are not interpreted. This is the default mode for
        /// sources that have not been configured explicitly.
        /// </summary>
        Read = 1,

        /// <summary>
        /// The source's values are scanned for <c>ref(...) / fmt(...)</c> reference tokens and section
        /// aliases, and the source is exposed as a substitution target for other
        /// <see cref="Scan"/> sources. Marking at least one source with this value
        /// activates the reference-resolution engine.
        /// </summary>
        Scan = 2,
    }
}
