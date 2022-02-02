// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// An XML file based <see cref="FileConfigurationSource"/>.
    /// </summary>
    public class XmlConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        /// When false (default), repeated elements are included as path of the
        /// configuration key along with their index. When true, the name of the
        /// repeated element is ignored and replaced with the index itself.
        /// </summary>
        /// <example>
        /// When false, a repeated element might produce keys like "root:repeat:0",
        /// "root:repeat:1". When true, the same repeated element produces keys like
        /// "root:0", "root:1"
        /// </example>
        public bool IgnoreElementNameForRepeats { get; set; }

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
