// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// An INI file based <see cref="ConfigurationProvider"/>.
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
    public class IniConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public IniConfigurationProvider(IniConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads the INI data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
            => Data = IniStreamConfigurationProvider.Read(stream);
    }
}
