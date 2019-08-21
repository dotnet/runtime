// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class XmlConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddXmlFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider provider, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddXmlFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.Xml.XmlConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddXmlFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddXmlFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddXmlFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddXmlStream(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.IO.Stream stream) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.Xml
{
    public partial class XmlConfigurationProvider : Microsoft.Extensions.Configuration.FileConfigurationProvider
    {
        public XmlConfigurationProvider(Microsoft.Extensions.Configuration.Xml.XmlConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.FileConfigurationSource)) { }
        public override void Load(System.IO.Stream stream) { }
    }
    public partial class XmlConfigurationSource : Microsoft.Extensions.Configuration.FileConfigurationSource
    {
        public XmlConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
    public partial class XmlDocumentDecryptor
    {
        public static readonly Microsoft.Extensions.Configuration.Xml.XmlDocumentDecryptor Instance;
        protected XmlDocumentDecryptor() { }
        public System.Xml.XmlReader CreateDecryptingXmlReader(System.IO.Stream input, System.Xml.XmlReaderSettings settings) { throw null; }
        protected virtual System.Xml.XmlReader DecryptDocumentAndCreateXmlReader(System.Xml.XmlDocument document) { throw null; }
    }
    public partial class XmlStreamConfigurationProvider : Microsoft.Extensions.Configuration.StreamConfigurationProvider
    {
        public XmlStreamConfigurationProvider(Microsoft.Extensions.Configuration.Xml.XmlStreamConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.StreamConfigurationSource)) { }
        public override void Load(System.IO.Stream stream) { }
        public static System.Collections.Generic.IDictionary<string, string> Read(System.IO.Stream stream, Microsoft.Extensions.Configuration.Xml.XmlDocumentDecryptor decryptor) { throw null; }
    }
    public partial class XmlStreamConfigurationSource : Microsoft.Extensions.Configuration.StreamConfigurationSource
    {
        public XmlStreamConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
