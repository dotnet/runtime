// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// Provides configuration key-value pairs that are obtained from an INI file.
    /// </summary>
    /// <remarks>
    /// <para>INI files are simple line structures (<a href="https://en.wikipedia.org/wiki/INI_file">INI files on Wikipedia</a>).</para>
    /// <para>The following is an example of an INI file:</para>
    /// <code lang="ini">
    /// [Section:Header]
    /// key1=value1
    /// key2 = " value2 "
    /// ; comment
    /// # comment
    /// / comment
    /// </code>
    /// </remarks>
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
