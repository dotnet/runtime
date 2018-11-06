// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// An XML file based <see cref="FileConfigurationSource"/>.
    /// </summary>
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