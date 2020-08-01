// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Tests;
using System.Xml;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Xml.Test
{
    public class XmlConfigurationTest
    {
        [Fact]
        public void LoadValidXmlFromStreamProvider()
        {
            var xml = @"
                <settings>
                    <Data.Setting>
                        <DefaultConnection>
                            <Connection.String>Test.Connection.String</Connection.String>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data.Setting>
                </settings>";
            var config = new ConfigurationBuilder().AddXmlStream(TestStreamHelpers.StringToStream(xml)).Build();

            Assert.Equal("Test.Connection.String", config["DATA.SETTING:DEFAULTCONNECTION:CONNECTION.STRING"]);
            Assert.Equal("SqlClient", config["DATA.SETTING:DefaultConnection:Provider"]);
            Assert.Equal("AnotherTestConnectionString", config["data.setting:inventory:connectionstring"]);
            Assert.Equal("MySql", config["Data.setting:Inventory:Provider"]);
        }

        [Fact]
        public void ReloadThrowsFromStreamProvider()
        {
            var xml = @"
                <settings>
                </settings>";
            var config = new ConfigurationBuilder().AddXmlStream(TestStreamHelpers.StringToStream(xml)).Build();
            Assert.Throws<InvalidOperationException>(() => config.Reload());
        }

        [Fact]
        public void LoadKeyValuePairsFromValidXml()
        {
            var xml = @"
                <settings>
                    <Data.Setting>
                        <DefaultConnection>
                            <Connection.String>Test.Connection.String</Connection.String>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data.Setting>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("Test.Connection.String", xmlConfigSrc.Get("DATA.SETTING:DEFAULTCONNECTION:CONNECTION.STRING"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("DATA.SETTING:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("data.setting:inventory:connectionstring"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data.setting:Inventory:Provider"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Key1></Key1>
    <Key2 Key3="""" />
</settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal(string.Empty, xmlConfigSrc.Get("Key1"));
            Assert.Equal(string.Empty, xmlConfigSrc.Get("Key2:Key3"));
        }

        [Fact]
        public void CommonAttributesContributeToKeyValuePairs()
        {
            var xml =
@"<settings Port=""8008"">
    <Data>
        <DefaultConnection
            ConnectionString=""TestConnectionString""
            Provider=""SqlClient""/>
        <Inventory
            ConnectionString=""AnotherTestConnectionString""
            Provider=""MySql""/>
    </Data>
</settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("8008", xmlConfigSrc.Get("Port"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportMixingChildElementsAndAttributes()
        {
            var xml =
                @"<settings Port='8008'>
                    <Data>
                        <DefaultConnection Provider='SqlClient'>
                            <ConnectionString>TestConnectionString</ConnectionString>
                        </DefaultConnection>
                        <Inventory ConnectionString='AnotherTestConnectionString'>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("8008", xmlConfigSrc.Get("Port"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void NameAttributeContributesToPrefix()
        {
            var xml =
                @"<settings>
                    <Data Name='DefaultConnection'>
                        <ConnectionString>TestConnectionString</ConnectionString>
                        <Provider>SqlClient</Provider>
                    </Data>
                    <Data Name='Inventory'>
                        <ConnectionString>AnotherTestConnectionString</ConnectionString>
                        <Provider>MySql</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("DefaultConnection", xmlConfigSrc.Get("Data:DefaultConnection:Name"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("Inventory", xmlConfigSrc.Get("Data:Inventory:Name"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void NameAttributeInRootElementContributesToPrefix()
        {
            var xml =
                @"<settings Name='Data'>
                    <DefaultConnection>
                        <ConnectionString>TestConnectionString</ConnectionString>
                        <Provider>SqlClient</Provider>
                    </DefaultConnection>
                    <Inventory>
                        <ConnectionString>AnotherTestConnectionString</ConnectionString>
                        <Provider>MySql</Provider>
                    </Inventory>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("Data", xmlConfigSrc.Get("Data:Name"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportMixingNameAttributesAndCommonAttributes()
        {
            var xml =
                @"<settings>
                    <Data Name='DefaultConnection'
                          ConnectionString='TestConnectionString'
                          Provider='SqlClient' />
                    <Data Name='Inventory' ConnectionString='AnotherTestConnectionString'>
                          <Provider>MySql</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("DefaultConnection", xmlConfigSrc.Get("Data:DefaultConnection:Name"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("Inventory", xmlConfigSrc.Get("Data:Inventory:Name"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportCDATAAsTextNode()
        {
            var xml =
                @"<settings>
                    <Data>
                        <Inventory>
                            <Provider><![CDATA[SpecialStringWith<>]]></Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("SpecialStringWith<>", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var xml =
                @"<!-- Comments --> <settings>
                    <Data> <!-- Comments -->
                        <DefaultConnection>
                            <ConnectionString><!-- Comments -->TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings><!-- Comments -->";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreXMLDeclaration()
        {
            var xml =
                @"<?xml version='1.0' encoding='UTF-8'?>
                <settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreProcessingInstructions()
        {
            var xml =
                @"<?xml version='1.0' encoding='UTF-8'?>
                <?xml-stylesheet type='text/xsl' href='style1.xsl'?>
                    <settings>
                        <?xml-stylesheet type='text/xsl' href='style2.xsl'?>
                        <Data>
                            <DefaultConnection>
                                <ConnectionString>TestConnectionString</ConnectionString>
                                <Provider>SqlClient</Provider>
                            </DefaultConnection>
                            <Inventory>
                                <ConnectionString>AnotherTestConnectionString</ConnectionString>
                                <Provider>MySql</Provider>
                            </Inventory>
                        </Data>
                    </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());

            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml));

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void ThrowExceptionWhenFindDTD()
        {
            var xml =
                @"<!DOCTYPE DefaultConnection[
                    <!ELEMENT DefaultConnection (ConnectionString,Provider)>
                    <!ELEMENT ConnectionString (#PCDATA)>
                    <!ELEMENT Provider (#PCDATA)>
                ]>
                <settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";

            using (new ThreadCultureChange("en-GB"))
            {
                var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());
                var isMono = Type.GetType("Mono.Runtime") != null;
                var expectedMsg = isMono ? "Document Type Declaration (DTD) is prohibited in this XML.  Line 1, position 10." : "For security reasons DTD is prohibited in this XML document. "
                    + "To enable DTD processing set the DtdProcessing property on XmlReaderSettings "
                    + "to Parse and pass the settings into XmlReader.Create method.";

                var exception = Assert.Throws<System.Xml.XmlException>(() => xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml)));

                Assert.Equal(expectedMsg, exception.Message);
            }
        }

        [Fact]
        public void ThrowExceptionWhenFindNamespace()
        {
            var xml =
                @"<settings xmlns:MyNameSpace='http://microsoft.com/wwa/mynamespace'>
                    <MyNameSpace:Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </MyNameSpace:Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());
            var expectedMsg = SR.Format(SR.Error_NamespaceIsNotSupported, SR.Format(SR.Msg_LineInfo, 1, 11));

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(SR.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddXmlFile(path: null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(SR.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddXmlFile(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
        {
            var xml =
                @"<settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                    </Data>
                    <Data Name='DefaultConnection' ConnectionString='NewConnectionString'>
                        <Provider>NewProvider</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());
            var expectedMsg = SR.Format(SR.Error_KeyIsDuplicated, "Data:DefaultConnection:ConnectionString",
                SR.Format(SR.Msg_LineInfo, 8, 52));

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xml)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void XmlConfiguration_Throws_On_Missing_Configuration_File()
        {
            var ex = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddXmlFile("NotExistingConfig.xml", optional: false).Build());
            Assert.StartsWith($"The configuration file 'NotExistingConfig.xml' was not found and is not optional. The physical path is '", ex.Message);
        }

        [Fact]
        public void XmlConfiguration_Does_Not_Throw_On_Optional_Configuration()
        {
            var config = new ConfigurationBuilder().AddXmlFile("NotExistingConfig.xml", optional: true).Build();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        public void LoadKeyValuePairsFromValidEncryptedXml()
        {
            var xml = @"
                <settings>
                    <Data.Setting>
                        <DefaultConnection>
                            <Connection.String>Test.Connection.String</Connection.String>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data.Setting>
                </settings>";

            // This AES key will be used to encrypt the 'Inventory' element
            var aes = Aes.Create();
            aes.KeySize = 128;
            aes.GenerateKey();

            // Perform the encryption
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            var encryptedXml = new EncryptedXml(xmlDocument);
            encryptedXml.AddKeyNameMapping("myKey", aes);
            var elementToEncrypt = (XmlElement)xmlDocument.SelectSingleNode("//Inventory");
            EncryptedXml.ReplaceElement(elementToEncrypt, encryptedXml.Encrypt(elementToEncrypt, "myKey"), content: false);

            // Quick sanity check: the document should no longer contain an 'Inventory' element
            Assert.Null(xmlDocument.SelectSingleNode("//Inventory"));

            // Arrange
            var xmlConfigSrc = new XmlConfigurationProvider(new XmlConfigurationSource());
            xmlConfigSrc.Decryptor = new XmlDocumentDecryptor(doc =>
            {
                var innerEncryptedXml = new EncryptedXml(doc);
                innerEncryptedXml.AddKeyNameMapping("myKey", aes);
                return innerEncryptedXml;
            });

            // Act
            xmlConfigSrc.Load(TestStreamHelpers.StringToStream(xmlDocument.OuterXml));

            // Assert
            Assert.Equal("Test.Connection.String", xmlConfigSrc.Get("DATA.SETTING:DEFAULTCONNECTION:CONNECTION.STRING"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("DATA.SETTING:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("data.setting:inventory:connectionstring"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data.setting:Inventory:Provider"));
        }
    }
}
