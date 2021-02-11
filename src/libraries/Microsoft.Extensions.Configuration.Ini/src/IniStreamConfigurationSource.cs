// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// Represents an INI file as an <see cref="IConfigurationSource"/>.
    /// Files are simple line structures (<a href="https://en.wikipedia.org/wiki/INI_file">INI Files on Wikipedia</a>)
    /// </summary>
    /// <examples>
    /// [Section:Header]
    /// key1=value1
    /// key2 = " value2 "
    /// ; comment
    /// # comment
    /// / comment
    /// </examples>
    public class IniStreamConfigurationSource : StreamConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="IniConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="IniConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
            => new IniStreamConfigurationProvider(this);
    }
}
