// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Controls how an <see cref="IConfigurationSource"/> participates in <c>{{…}}</c>
    /// reference resolution performed by the <see cref="IConfigurationRoot"/> built from the
    /// containing <see cref="IConfigurationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Values are totally ordered by participation: <see cref="Ignore"/> &lt; <see cref="Read"/>
    /// &lt; <see cref="Scan"/>. Sources default to <see cref="Scan"/> unless configured via
    /// <see cref="ReferenceResolutionConfigurationBuilderExtensions.SetReferenceMode(IConfigurationBuilder, ReferenceMode)"/>
    /// or one of its overloads. The reference-resolution engine is attached to the built root
    /// unless every source has been explicitly set to a non-<see cref="Scan"/> mode.
    /// </remarks>
    public enum ReferenceMode
    {
        /// <summary>
        /// The source is invisible to the reference-resolution engine: no <c>{{…}}</c>
        /// reference or section alias in another source can reach its values. Direct reads
        /// via the normal <see cref="IConfiguration"/> API still return its values.
        /// </summary>
        Ignore = 0,

        /// <summary>
        /// The source is a valid substitution target for references in other
        /// <see cref="Scan"/> sources. The source's own values are returned verbatim —
        /// <c>{{…}}</c> sequences are not interpreted. This is the default mode for
        /// sources that have not been configured explicitly.
        /// </summary>
        Read = 1,

        /// <summary>
        /// The source's values are scanned for <c>{{…}}</c> reference tokens and section
        /// aliases, and the source is exposed as a substitution target for other
        /// <see cref="Scan"/> sources. Marking at least one source with this value
        /// activates the reference-resolution engine.
        /// </summary>
        Scan = 2,
    }
}
