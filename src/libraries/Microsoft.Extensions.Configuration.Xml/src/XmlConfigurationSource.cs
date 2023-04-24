// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// An XML file based <see cref="FileConfigurationSource"/>.
    /// </summary>
    [RequiresDynamicCode(XmlDocumentDecryptor.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(XmlDocumentDecryptor.RequiresUnreferencedCodeMessage)]
    public class XmlConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="XmlConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="XmlConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new XmlConfigurationProvider(this);
        }
    }
}
