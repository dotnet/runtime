// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    // ===================== XmlResolver =====================

    public class TCXmlResolver : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCXmlResolver(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        //BUG #304124
        [Fact]
        public void DefaultValueForXmlResolver_XmlUrlResolver()
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());

            manager.AddNamespace("t", "uri:tempuri");

            XmlSchemaValidator val = new XmlSchemaValidator(new NameTable(),
                                                            CreateSchemaSetFromXml("<root />"),
                                                            manager,
                                                            AllFlags);
            XmlSchemaInfo info = new XmlSchemaInfo();

            val.XmlResolver = new XmlUrlResolver(); //Adding this as the default resolver is null and not XmlUrlResolver anymore

            val.Initialize();
            val.ValidateElement("foo", "", null, "t:type1", null, "uri:tempuri " + Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE), null);
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("bar", "", null);
            val.ValidateEndOfAttributes(null);
            val.ValidateEndElement(null);
            val.ValidateEndElement(info);

            Assert.Equal(info.ContentType, XmlSchemaContentType.ElementOnly);
            Assert.True(info.SchemaType != null);

            return;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UsingCustomXmlResolverWith_SchemaLocation_NoNamespaceSchemaLocation(bool schemaLocation)
        {
            CXmlTestResolver res = new CXmlTestResolver();
            CResolverHolder holder = new CResolverHolder();

            res.CalledResolveUri += new XmlTestResolverEventHandler(holder.CallBackResolveUri);
            res.CalledGetEntity += new XmlTestResolverEventHandler(holder.CallBackGetEntity);

            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
            XmlSchemaValidator val = new XmlSchemaValidator(new NameTable(),
                                                            new XmlSchemaSet(),
                                                            manager,
                                                            AllFlags);
            val.XmlResolver = res;
            val.Initialize();

            if (schemaLocation)
            {
                manager.AddNamespace("t", "uri:tempuri");
                val.ValidateElement("foo", "", null, "t:type1", null, "uri:tempuri " + Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE), null);
            }
            else
            {
                val.ValidateElement("foo", "", null, "type1", null, null, XSDFILE_NO_TARGET_NAMESPACE);
            }

            Assert.True(holder.IsCalledResolveUri);
            Assert.True(holder.IsCalledGetEntity);

            return;
        }

        [Fact]
        public void InternalSchemaSetShouldUseSeparateXmlResolver()
        {
            CXmlTestResolver res = new CXmlTestResolver();
            CResolverHolder holder = new CResolverHolder();

            res.CalledResolveUri += new XmlTestResolverEventHandler(holder.CallBackResolveUri);
            res.CalledGetEntity += new XmlTestResolverEventHandler(holder.CallBackGetEntity);

            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
            XmlSchemaValidator val = new XmlSchemaValidator(new NameTable(),
                                                            new XmlSchemaSet(){ XmlResolver = new XmlUrlResolver()},
                                                            manager,
                                                            AllFlags);
            val.XmlResolver = res;

            val.Initialize();
            val.AddSchema(XmlSchema.Read(XmlReader.Create(Path.Combine(TestData, XSDFILE_VALIDATE_ATTRIBUTE)), null)); // this schema has xs:import
            val.ValidateElement("NoAttributesElement", "", null);

            Assert.True(!holder.IsCalledResolveUri);
            Assert.True(!holder.IsCalledGetEntity);

            return;
        }

        [Fact]
        public void SetResolverToCustomValidateSomethignChangeResolverThenVerify()
        {
            CXmlTestResolver res = new CXmlTestResolver();
            CResolverHolder holder = new CResolverHolder();

            res.CalledResolveUri += new XmlTestResolverEventHandler(holder.CallBackResolveUri);
            res.CalledGetEntity += new XmlTestResolverEventHandler(holder.CallBackGetEntity);

            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = new XmlSchemaValidator(new NameTable(),
                                                            new XmlSchemaSet(),
                                                            manager,
                                                            AllFlags);
            val.XmlResolver = res;
            val.Initialize();

            val.ValidateElement("foo", "", null, "type1", null, null, Path.Combine(TestData, XSDFILE_NO_TARGET_NAMESPACE));
            val.SkipToEndElement(null);

            Assert.True(holder.IsCalledResolveUri);
            Assert.True(holder.IsCalledGetEntity);

            val.XmlResolver = new XmlUrlResolver();
            holder.IsCalledGetEntity = false;
            holder.IsCalledResolveUri = false;

            val.ValidateElement("foo", "", null, "type1", null, null, Path.Combine(TestData, XSDFILE_NO_TARGET_NAMESPACE));

            Assert.True(!holder.IsCalledResolveUri);
            Assert.True(!holder.IsCalledGetEntity);

            return;
        }

        [Fact]
        public void SetResolverToCustomValidateSomethingSetResolverToNullThenVerify()
        {
            CXmlTestResolver res = new CXmlTestResolver();
            CResolverHolder holder = new CResolverHolder();

            res.CalledResolveUri += new XmlTestResolverEventHandler(holder.CallBackResolveUri);
            res.CalledGetEntity += new XmlTestResolverEventHandler(holder.CallBackGetEntity);

            XmlNamespaceManager manager = new XmlNamespaceManager(new NameTable());
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = new XmlSchemaValidator(new NameTable(),
                                                            new XmlSchemaSet(),
                                                            manager,
                                                            AllFlags);
            val.XmlResolver = res;
            val.Initialize();

            val.ValidateElement("foo", "", null, "type1", null, null, Path.Combine(TestData, XSDFILE_NO_TARGET_NAMESPACE));
            val.SkipToEndElement(null);

            Assert.True(holder.IsCalledResolveUri);
            Assert.True(holder.IsCalledGetEntity);

            manager.AddNamespace("t", "uri:tempuri");
            val.XmlResolver = null;

            try
            {
                val.ValidateElement("bar", "", null, "t:type1", null, "uri:tempuri " + Path.Combine(TestData, XSDFILE_TARGET_NAMESPACE), null);
                Assert.True(false);
            }
            catch (XmlSchemaValidationException)
            {
                //XmlExceptionVerifier.IsExceptionOk(e, "Sch_XsiTypeNotFound", new string[] { "uri:tempuri:type1" });
                return;
            }

            Assert.True(false);
        }
    }

    // ===================== LineInfoProvider =====================

    public class TCLineInfoProvider : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCLineInfoProvider(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("default")]
        [InlineData("custom")]
        public void Default_Custom_ValueForLineInfoProvider(String param)
        {
            string xmlSrc = "<root><foo>FooText</foo><bar>BarText</bar></root>";
            XmlSchemaInfo info = new XmlSchemaInfo();

            int lineNum = -1;
            int linePos = -1;

            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml(xmlSrc));

            switch (param)
            {
                case "default":
                    lineNum = 0;
                    linePos = 0;
                    break;

                case "custom":
                    lineNum = 1111;
                    linePos = 2222;
                    val.LineInfoProvider = new CDummyLineInfo(lineNum, linePos);
                    break;

                default:
                    Assert.True(false);
                    break;
            }

            val.Initialize();
            val.ValidateElement("root", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("foo", "", info);

            Assert.Equal(val.LineInfoProvider.LineNumber, lineNum);
            Assert.Equal(val.LineInfoProvider.LinePosition, linePos);

            val.SkipToEndElement(info);
            try
            {
                val.ValidateElement("bar2", "", info);
                Assert.True(false);
            }
            catch (XmlSchemaValidationException e)
            {
                Assert.Equal(e.LineNumber, lineNum);
                Assert.Equal(e.LinePosition, linePos);
            }

            return;
        }

        // BUG 304165
        [Fact]
        public void LineInfoProviderChangesDuringValidation()
        {
            string xmlSrc = "<root><foo>FooText</foo></root>";
            XmlSchemaInfo info = new XmlSchemaInfo();
            CValidationEventHolder holder = new CValidationEventHolder();

            int lineNum = -1;
            int linePos = -1;

            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml(xmlSrc));

            val.ValidationEventHandler += holder.CallbackA;
            val.Initialize();

            foreach (int i in new int[] { 1111, 1333, 0 })
            {
                lineNum = i;
                linePos = i * 2;
                if (i == 0)
                    val.LineInfoProvider = null;
                else
                    val.LineInfoProvider = new CDummyLineInfo(lineNum, linePos);

                val.ValidateElement("root", "", info);
                val.ValidateEndOfAttributes(null);
                val.ValidateElement("bar", "", info);
                Assert.True(holder.IsCalledA);

                Assert.Equal(holder.lastException.LineNumber, lineNum);
                Assert.Equal(holder.lastException.LinePosition, linePos);

                val.SkipToEndElement(info);
                val.SkipToEndElement(info);
                holder.IsCalledA = false;
            }

            return;
        }

        [Fact]
        public void XmlReaderAsALineInfoProvider()
        {
            string xmlSrc = "<root>\n" +
                            "  <foo>\n" +
                            "    FooText\n" +
                            "  </foo>\n" +
                            "</root>";
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml(xmlSrc));
            XmlReader r = XmlReader.Create(new StringReader(xmlSrc));

            val.LineInfoProvider = (r as IXmlLineInfo);

            val.Initialize();
            r.ReadStartElement("root");
            val.ValidateElement("root", "", info);
            val.ValidateEndOfAttributes(null);

            try
            {
                r.ReadStartElement("foo");
                val.ValidateElement("bar", "", info);
                Assert.True(false);
            }
            catch (XmlSchemaValidationException e)
            {
                Assert.Equal(e.LineNumber, 2);
                Assert.Equal(e.LinePosition, 8);
            }

            return;
        }
    }

    // ===================== SourceUri =====================

    public class TCSourceUri : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCSourceUri(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("default")]
        [InlineData("")]
        [InlineData("urn:tempuri")]
        [InlineData("\\\\wddata\\some\\path")]
        [InlineData("http://tempuri.com/schemas")]
        [InlineData("file://tempuri.com/schemas")]
        public void Default_Empty_RelativeUri_NetworkFolder_HTTP_FILE_ForSourceURI(String sourceUri)
        {
            string xmlSrc = "<root>foo</root>";
            Uri tempUri;
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml(xmlSrc));

            if (sourceUri != "default" && sourceUri != String.Empty && sourceUri != null)
            {
                tempUri = new Uri(sourceUri);
                val.SourceUri = tempUri;
            }
            else
                tempUri = null;

            Assert.Equal(tempUri, val.SourceUri);

            val.Initialize();
            try
            {
                val.ValidateElement("bar", "", info);
                Assert.True(false, "Validation Error - XmlSchemaValidationException wasn't thrown!");
            }
            catch (XmlSchemaValidationException e)
            {
                Assert.True((tempUri == null && e.SourceUri == null) || (tempUri.ToString() == e.SourceUri));
            }

            return;
        }

        [Fact]
        public void SourceUriChangesDuringValidation()
        {
            string xmlSrc = "<root><foo><bar/></foo></root>";
            Uri tempUri;
            XmlSchemaInfo info = new XmlSchemaInfo();

            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml(xmlSrc));

            tempUri = new Uri("file://tempuri.com/schemas");
            val.SourceUri = tempUri;

            val.Initialize();
            val.ValidateElement("root", "", info);
            val.ValidateEndOfAttributes(null);
            val.ValidateElement("foo", "", info);

            tempUri = new Uri("urn:relativepath");
            val.SourceUri = tempUri;

            Assert.Equal(tempUri, val.SourceUri);

            val.ValidateEndOfAttributes(null);
            try
            {
                val.ValidateElement("bar2", "", info);
                Assert.True(false);
            }
            catch (XmlSchemaValidationException e)
            {
                Assert.Equal(tempUri.ToString(), e.SourceUri);
            }

            return;
        }
    }

    // ===================== ValidationEvent handling =====================

    public class TCValidationEventHandling : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidationEventHandling(ITestOutputHelper output): base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("default")]
        [InlineData("null")]
        [InlineData("arbitrary")]
        [InlineData("reader")]
        [InlineData("document")]
        [InlineData("navigator")]
        public void Default_Null_ArbitraryObject_XmlReader_XmlDocument_XPathNavigator_ProvidedAsValidationEventSender(String param)
        {
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root>foo</root>"));
            CValidationEventHolder holder = new CValidationEventHolder();
            object sender = null;

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            switch (param)
            {
                case "default":
                    sender = val;
                    break;

                case "null":
                    sender = null;
                    break;

                case "arbitrary":
                    sender = new ArrayList();
                    break;

                case "reader":
                    sender = XmlReader.Create(new StringReader("<root/>"));
                    break;

                case "document":
                    sender = new XmlDocument();
                    break;

                case "navigator":
                    XmlDocument d = new XmlDocument();
                    sender = d.CreateNavigator();
                    break;

                default:
                    Assert.True(false);
                    break;
            }

            if (param != "default")
                val.ValidationEventSender = sender;

            val.Initialize();
            val.ValidateElement("bar", "", info);

            Assert.True(holder.IsCalledA);
            Assert.Equal(sender, holder.lastObjectSent);

            return;
        }

        [Fact]
        public void ErrorHandlingAfterValidationError()
        {
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root><foo/></root>"));
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("root", "", info);
            val.ValidateEndOfAttributes(null);

            val.ValidateElement("bar", "", info);
            Assert.True(holder.IsCalledA);
            val.SkipToEndElement(info);
            val.SkipToEndElement(info);

            holder.IsCalledA = false;
            val.ValidateElement("bar", "", info);
            Assert.True(holder.IsCalledA);

            return;
        }

        [Fact]
        public void SanityTestsForValidationEventHandler()
        {
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root>foo</root>"));
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);

            val.Initialize();
            val.ValidateElement("bar", "", info);

            Assert.True(holder.IsCalledA);

            return;
        }

        [Fact]
        public void MultipleEventHandlersAttachedToValidator()
        {
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root>foo</root>"));
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackA);
            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackB);

            val.Initialize();
            val.ValidateElement("bar", "", info);

            Assert.True(holder.IsCalledA);
            Assert.True(holder.IsCalledB);

            return;
        }

        [Fact]
        public void ValidationCallProducesValidationErrorInsideValidationEventHandler__Nesting()
        {
            XmlSchemaInfo info = new XmlSchemaInfo();
            XmlSchemaValidator val = CreateValidator(CreateSchemaSetFromXml("<root>foo</root>"));
            CValidationEventHolder holder = new CValidationEventHolder();

            val.ValidationEventHandler += new ValidationEventHandler(holder.CallbackNested);

            val.Initialize();
            val.ValidateElement("bar", "", info);

            Assert.Equal(holder.NestingDepth, 3);

            return;
        }
    }
}
