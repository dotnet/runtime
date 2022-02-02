// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// Represents an XML file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class XmlConfigurationProvider : FileConfigurationProvider
    {
        private readonly XmlConfigurationSource configSource;

        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public XmlConfigurationProvider(XmlConfigurationSource source) : base(source)
        {
            this.configSource = source;
        }

        internal XmlDocumentDecryptor Decryptor { get; set; } = XmlDocumentDecryptor.Instance;

        internal System.Collections.Generic.IDictionary<string, string> Data2 => Data;

        /// <summary>
        /// Loads the XML data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            Data = XmlStreamConfigurationProvider.Read(stream, Decryptor, this.configSource.IgnoreElementNameForRepeats);
        }
    }
}
