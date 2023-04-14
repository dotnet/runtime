// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// Represents a XML file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    [RequiresDynamicCode(XmlDocumentDecryptor.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(XmlDocumentDecryptor.RequiresUnreferencedCodeMessage)]
    public class XmlStreamConfigurationSource : StreamConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="XmlStreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="XmlStreamConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
            => new XmlStreamConfigurationProvider(this);
    }
}
