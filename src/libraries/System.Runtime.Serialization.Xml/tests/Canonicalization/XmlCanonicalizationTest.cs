// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    public static class XmlCanonicalizationTest
    {
        static XmlCanonicalizationTest()
        {
            TestConfigHelper.LoadAllTests(@"TestsConfig.xml");
        }

        [Fact]
        public static void C14NWriterNegativeTests()
        {
            const string TestTypeNullStream = "Null Stream";
            const string TestTypeNullElementInIncludePrefixes = "Null element in IncludePrefixes";

            TestCase tc = TestConfigHelper.GetTest("C14NWriterNegativeTests");

            foreach (var input in tc.Inputs)
            {
                string testType = input.Arguments[0].Value;
                if (testType == TestTypeNullStream)
                {
                    try
                    {
                        //creating XmlC14NWriter with a null stream;
                        XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(Stream.Null);
                        writer.StartCanonicalization(null, true, new string[] { "p1", "p2" });
                        Assert.False(true, "Error, creating XmlC14NWriter with a null stream should have thrown!");
                    }
                    catch (Exception ex)
                    {
                        //System.ArgumentNullException: {{ResLookup:;Value cannot be null.;ManagedString;mscorlib.dll;mscorlib;ArgumentNull_Generic}}
                        Assert.Equal(input.Arguments[1].Value, ex.GetType().FullName);
                    }
                }
                else if (testType == TestTypeNullElementInIncludePrefixes)
                {
                    MemoryStream ms1 = new MemoryStream();
                    MemoryStream ms2 = new MemoryStream();
                    XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms1);
                    try
                    {
                        //Creating the C14N writer with null elements in the IncludePrefixes array;
                        writer.WriteStartElement("p2", "Root", "http://namespace");
                        //Starting the canonicalization;
                        writer.StartCanonicalization(ms2, true, new string[] { "p1", null, "p2" });
                        //Writing the first element in the C14N writer;
                        writer.WriteStartElement("Wee");
                        //Writing some content to finish the start element. Last chance for it to throw
                        writer.WriteQualifiedName("foo", "http://namespace");
                        writer.WriteEndElement();
                        writer.EndCanonicalization();
                        writer.WriteEndElement();
                        Assert.False(true, "Error, creating XmlC14NWriter with null elements in include prefixes should have thrown!");
                    }
                    catch (Exception ex)
                    {
                        //System.ArgumentException: {{ResLookup:;The inclusive namespace prefix collection cannot contain null as one of the items.;ManagedString;System.Runtime.Serialization.dll;System.Runtime.Serialization;InvalidInclusivePrefixListCollection}}
                        Assert.Equal(input.Arguments[1].Value, ex.GetType().FullName);
                    }
                }
                else
                {
                    throw new ArgumentException("Don't know how to run test " + testType);
                }
            }
        }

        [Fact]
        public static void TestC14NInclusivePrefixes()
        {
            TestCase tc = TestConfigHelper.GetTest("TestC14NInclusivePrefixes");
            int count = 0;

            foreach (var input in tc.Inputs)
            {
                count++;
                string rwTypeStr = input.Arguments[0].Value;
                ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
                Encoding encoding = Encoding.GetEncoding(input.Arguments[1].Value);
                bool mustSupportV14N = input.Arguments[2].Value.ToLower() == "true";

                MemoryStream ms = new MemoryStream();
                XmlWriter w = ReaderWriterFactory.CreateXmlWriter(rwType, ms, encoding);
                XmlDictionaryWriter writer = w as XmlDictionaryWriter;
                if (writer == null)
                {
                    writer = XmlDictionaryWriter.CreateDictionaryWriter(w);
                }

                if (!writer.CanCanonicalize)
                {
                    Assert.False(mustSupportV14N,
                        string.Format("Error, writer {0},{1} should support C14N, but it doesn't!", rwTypeStr, encoding.ToString()));
                    continue;
                }

                string myDefaultNamespace = "http://mynamespace";
                string myNamespace1 = "http://mynamespace1";
                string myNamespace2 = "http://mynamespace2";
                string myNamespace3 = "http://mynamespace3";
                string myNamespace4 = "http://mynamespace4";
                writer.WriteStartElement("Root");
                writer.WriteXmlnsAttribute("p1", myNamespace1);
                writer.WriteAttributeString("p1", "a", null, "b");
                writer.WriteStartElement("", "Element1", myDefaultNamespace);
                writer.WriteAttributeString("p3", "c", myNamespace3, "d");
                writer.WriteStartElement("Element2");

                MemoryStream canonicalStream = new MemoryStream();

                writer.StartCanonicalization(canonicalStream, false, new string[] { "p3", "p2", "p1", "" });
                writer.WriteStartElement("pre", "Element3", myNamespace2);
                writer.WriteAttributeString("pre2", "attrName", myNamespace4, "attrValue");
                writer.WriteStartElement("Element4", "");
                writer.WriteStartAttribute("attr1");
                writer.WriteQualifiedName("foo", myNamespace1);
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("attr2");
                writer.WriteQualifiedName("bar", myNamespace3);
                writer.WriteEndAttribute();

                writer.WriteString("Hello world");

                writer.WriteEndElement(); // Element4
                writer.WriteEndElement(); // pre:Element3

                writer.EndCanonicalization();
                writer.WriteEndElement(); // Element2
                writer.WriteEndElement(); // Element1
                writer.WriteEndElement(); // Root
                writer.Flush();

                byte[] canonicalDoc = canonicalStream.ToArray();
                byte[] fullDoc = ms.ToArray();

                writer.Close(); // Finished creating the document

                XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
                transform.InclusiveNamespacesPrefixList = "p3 p2 p1 #default";
                transform.LoadInput(new MemoryStream(canonicalDoc));
                Stream transformedOutput = transform.GetOutput(typeof(Stream)) as Stream;
                byte[] outputFromSecurity = StreamToByteArray(transformedOutput);
                //Finished creating the doc from the security class

                Helper.DumpToFile(fullDoc);
                Helper.DumpToFile(canonicalDoc);
                Helper.DumpToFile(outputFromSecurity);
                Assert.True(Enumerable.SequenceEqual(outputFromSecurity, canonicalDoc), $"TestC14NInclusivePrefixes test variation #{count} failed");
            }
        }

        [Fact]
        public static void ReaderWriter_C14N_DifferentReadersWriters()
        {
            int count = 0;
            var params1 = TestConfigHelper.GetTest("ReaderWriter_C14N_DifferentReadersWriters_ParamGroup1");
            var params2 = TestConfigHelper.GetTest("ReaderWriter_C14N_DifferentReadersWriters_ParamGroup2");
            var params3 = TestConfigHelper.GetTest("ReaderWriter_C14N_DifferentReadersWriters_ParamGroup3");
            var params4 = TestConfigHelper.GetTest("ReaderWriter_C14N_DifferentReadersWriters_ParamGroup4");
            Transform transform;
            MemoryStream canonicalStream;
            MemoryStream ms;
            Stream transformedOutput;
            byte[] outputFromSecurity;
            byte[] outputFromIndigo;

            //TestC14NInMultipleWriters
            foreach (var input in params1.Inputs)
            {
                foreach (var input2 in params2.Inputs)
                {
                    foreach (var input3 in params3.Inputs)
                    {
                        count++;
                        string rwTypeStr = input.Arguments[0].Value;
                        ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
                        Encoding encoding = Encoding.GetEncoding((string)input.Arguments[1].Value);
                        string sampleXmlFileName = input2.Arguments[0].Value;
                        bool mustSupportV14N = input.Arguments[2].Value == "true";
                        string baselineFileName = input2.Arguments[1].Value;

                        bool testWithComments = input3.Arguments[0].Value == "true";

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.PreserveWhitespace = true;

                        if (testWithComments)
                        {
                            transform = new XmlDsigExcC14NWithCommentsTransform();
                        }
                        else
                        {
                            transform = new XmlDsigExcC14NTransform();
                        }
                        xmlDoc.Load(baselineFileName);
                        transform.LoadInput(xmlDoc);
                        transformedOutput = transform.GetOutput(typeof(Stream)) as Stream;
                        outputFromSecurity = StreamToByteArray(transformedOutput);

                        byte[] sampleXmlFileBytes = File.ReadAllBytes(sampleXmlFileName);

                        ms = new MemoryStream();
                        XmlWriter w = ReaderWriterFactory.CreateXmlWriter(rwType, ms, encoding);

                        canonicalStream = new MemoryStream();

                        XmlDictionaryWriter dicWriter = w as XmlDictionaryWriter;
                        if (dicWriter == null)
                        {
                            dicWriter = XmlDictionaryWriter.CreateDictionaryWriter(w);
                        }

                        if (!dicWriter.CanCanonicalize)
                        {
                            Assert.False(mustSupportV14N, "Error, writer should support C14N, but it doesn't!");
                            continue;
                        }

                        dicWriter.WriteStartElement("MyRoot");
                        dicWriter.StartCanonicalization(canonicalStream, testWithComments, null);
                        FileStream fs = File.OpenRead(sampleXmlFileName);
                        XmlReader webdataReader = XmlReader.Create(fs);
                        CopyXmlToWriter(webdataReader, dicWriter);
                        dicWriter.EndCanonicalization();
                        dicWriter.WriteEndElement();
                        dicWriter.Flush();
                        webdataReader.Close();
                        fs.Close();

                        outputFromIndigo = canonicalStream.ToArray();

                        Helper.DumpToFile(outputFromSecurity);
                        Helper.DumpToFile(outputFromIndigo);
                        Assert.True(Enumerable.SequenceEqual(outputFromSecurity, outputFromIndigo), $"ReaderWriter_C14N_DifferentReadersWriters test variation #{count} failed");
                    }
                }
            }

            //TestC14NInReader
            foreach (var input in params4.Inputs)
            {
                count++;
                string sampleXmlFileName = input.Arguments[3].Value;
                string rwTypeStr = input.Arguments[0].Value;
                ReaderWriterFactory.ReaderWriterType rwType = (ReaderWriterFactory.ReaderWriterType)Enum.Parse(typeof(ReaderWriterFactory.ReaderWriterType), rwTypeStr, true);
                Encoding encoding = Encoding.GetEncoding((string)input.Arguments[1].Value);

                bool mustSupportV14N = input.Arguments[2].Value == "true";
                string baselineFileName = "ReaderWriter_C14N_BaselineXML_OnlyLF.xml";

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                transform = new XmlDsigExcC14NTransform();
                xmlDoc.Load(baselineFileName);
                transform.LoadInput(xmlDoc);
                transformedOutput = transform.GetOutput(typeof(Stream)) as Stream;
                outputFromSecurity = StreamToByteArray(transformedOutput);
                byte[] sampleXmlFileBytes = File.ReadAllBytes(sampleXmlFileName);

                XmlReader r = ReaderWriterFactory.CreateXmlReader(rwType, sampleXmlFileBytes, encoding);
                XmlDictionaryReader dicReader = r as XmlDictionaryReader;
                if (dicReader == null)
                {
                    dicReader = XmlDictionaryReader.CreateDictionaryReader(r);
                }

                canonicalStream = new MemoryStream();

                if (!dicReader.CanCanonicalize)
                {
                    Assert.False(mustSupportV14N, "Error, reader should support C14N, but it doesn't!");
                    continue;
                }

                dicReader.StartCanonicalization(canonicalStream, false, null);

                canonicalStream.Position = 0;
                string str = new StreamReader(canonicalStream).ReadToEnd();
                canonicalStream.Position = 0;
                while (dicReader.Read()) ; // simply read it all into the C14N writer
                dicReader.EndCanonicalization();
                dicReader.Close();

                outputFromIndigo = canonicalStream.ToArray();
                Helper.DumpToFile(outputFromSecurity);
                Helper.DumpToFile(outputFromIndigo);
                Assert.True(Enumerable.SequenceEqual(outputFromSecurity, outputFromIndigo), $"ReaderWriter_C14N_DifferentReadersWriters test variation #{count} failed");
            }

            //TestC14NWriterWithManyAttributes
            int numberOfAttributes = 1000;
            int seed = (int)DateTime.Now.Ticks;
            Random rndGen = new Random(seed);
            StringBuilder sb = new StringBuilder();
            sb.Append("<Root><Element");
            int prefixIndex = 0;
            for (int i = 0; i < numberOfAttributes; i++)
            {
                string namespaceUri = null;
                string prefix = null;
                if ((rndGen.Next() % 5) == 0)
                {
                    prefix = "p" + (prefixIndex++);
                    namespaceUri = "http://namespace_" + i;
                }

                string localName = "attr" + i;
                string value = "attrValue" + i;
                if (prefix == null)
                {
                    sb.AppendFormat(" {0}=\"{1}\"", localName, value);
                }
                else
                {
                    sb.AppendFormat(" {0}:{1}=\"{2}\" xmlns:{0}=\"{3}\"",
                        prefix, localName, value, namespaceUri);
                }
            }
            sb.Append(">Hello world</Element></Root>");
            string xmlString = sb.ToString();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            ms = new MemoryStream();
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(ms);
            canonicalStream = new MemoryStream();
            writer.StartCanonicalization(canonicalStream, false, null);
            doc.WriteTo(writer);
            writer.Flush();
            writer.EndCanonicalization();
            outputFromIndigo = canonicalStream.ToArray();
            byte[] nonCanonicalOutput = ms.ToArray();

            XmlDsigExcC14NTransform transform2 = new XmlDsigExcC14NTransform();
            transform2.LoadInput(doc);
            transformedOutput = transform2.GetOutput(typeof(Stream)) as Stream;
            outputFromSecurity = StreamToByteArray(transformedOutput);

            Helper.DumpToFile(outputFromSecurity);
            Helper.DumpToFile(outputFromIndigo);
            Helper.DumpToFile(nonCanonicalOutput);

            Assert.True(Enumerable.SequenceEqual(outputFromSecurity, outputFromIndigo), $"ReaderWriter_C14N_DifferentReadersWriters test variation #{count} failed");
            count++;
            Assert.Equal(params1.Inputs.Count * params2.Inputs.Count * params3.Inputs.Count + params4.Inputs.Count + 1, count);
        }

        [Fact]
        public static void CryptoCanonicalizationTest()
        {
            Engine engine;
            TestMode mode;
            XmlBuffer xmlBuffer;
            bool includeComments;
            string inclusivePrefixes;
            string startAt;
            TestCase tc = TestConfigHelper.GetTest("CryptoCanonicalization");

            foreach (var input in tc.Inputs)
            {
                includeComments = input.Arguments[1].Value.ToLower() == "true";
                inclusivePrefixes = input.Arguments[2].Value;
                startAt = input.Arguments[3].Value;

                if (startAt.Equals(""))
                {
                    mode = TestMode.FullDocument;
                }
                else
                {
                    mode = TestMode.StartAtSpecifiedElement;
                }

                xmlBuffer = new XmlBuffer(input.Arguments[0].Value);
                engine = new Engine(includeComments, inclusivePrefixes, mode == TestMode.FullDocument);

                XmlReader reader = CreateReader(mode, xmlBuffer, startAt);
                //Canonicalization using Dictionary writer
                byte[] dr = engine.CanonicalizeUsingDictionaryReader(reader);

                reader = CreateReader(mode, xmlBuffer, startAt);
                //Canonicalization using writer
                byte[] w = engine.CanonicalizeUsingWriter(reader);

                reader = CreateReader(mode, xmlBuffer, startAt);
                //Canonicalization using Dictionary writer
                byte[] dw = engine.CanonicalizeUsingDictionaryWriter(reader);

                //Canonicalization using CLR
                byte[] c = CanonicalizeUsingClrLibrary(engine, mode, xmlBuffer, startAt);

                string dicReaderOutput = dr == null ? null : Encoding.UTF8.GetString(dr);
                string writerOutput = w == null ? null : Encoding.UTF8.GetString(w);
                string dicWriterOutput = dw == null ? null : Encoding.UTF8.GetString(dw);
                string clrOutput = Encoding.UTF8.GetString(c);
                
                Assert.Equal(input.Arguments[4].Value.ToLower() == "true", dicReaderOutput == writerOutput);
                Assert.Equal(input.Arguments[5].Value.ToLower() == "true", dicWriterOutput == writerOutput);
                Assert.Equal(input.Arguments[6].Value.ToLower() == "true", clrOutput == writerOutput);
            }
        }

        private static byte[] StreamToByteArray(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[1000];
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead != 0);
            return ms.ToArray();
        }

        private static void CopyXmlToWriter(XmlReader r, XmlWriter w)
        {
            while (r.Read())
            {
                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        w.WriteStartElement(r.Prefix, r.LocalName, r.NamespaceURI);
                        if (r.HasAttributes)
                        {
                            r.MoveToFirstAttribute();
                            do
                            {
                                if (r.LocalName == "xop" && r.Prefix == "xmlns" && r.Value == "http://www.w3.org/2004/08/xop/include")
                                {
                                    // special case: do not need to rewrite the xmlns:xop for MTOM
                                    // skip
                                }
                                else
                                {
                                    w.WriteAttributeString(r.Prefix, r.LocalName, r.NamespaceURI, r.Value);
                                }
                            } while (r.MoveToNextAttribute());
                            r.MoveToElement();
                        }

                        if (r.IsEmptyElement)
                        {
                            w.WriteEndElement();
                        }

                        break;
                    case XmlNodeType.CDATA:
                        w.WriteCData(r.Value);
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        w.WriteWhitespace(r.Value);
                        break;
                    case XmlNodeType.Text:
                        w.WriteString(r.Value);
                        break;
                    case XmlNodeType.XmlDeclaration:
                        string stdAlone = r.GetAttribute("standalone");
                        if (stdAlone == null)
                        {
                            w.WriteStartDocument();
                        }
                        else
                        {
                            bool isStandAlone = false;
                            if (bool.TryParse(stdAlone, out isStandAlone))
                            {
                                w.WriteStartDocument(isStandAlone);
                            }
                            else
                            {
                                w.WriteStartDocument();
                            }
                        }
                        break;
                    case XmlNodeType.Comment:
                        w.WriteComment(r.Value);
                        break;
                    case XmlNodeType.EndElement:
                        w.WriteFullEndElement();
                        break;
                }
            }
        }

        private enum TestMode
        {
            FullDocument,
            StartAtSpecifiedElement
        }

        private static XmlReader CreateReader(TestMode mode, XmlBuffer xmlBuffer, string startAt)
        {
            switch (mode)
            {
                case TestMode.FullDocument:
                    return xmlBuffer.CreateReader();
                case TestMode.StartAtSpecifiedElement:
                    return xmlBuffer.CreateReaderAt(startAt);
                default:
                    throw new InvalidOperationException("Reader cannot be created in mode " + mode);
            }
        }

        private static byte[] CanonicalizeUsingClrLibrary(Engine engine, TestMode mode, XmlBuffer xmlBuffer, string startAt)
        {
            if (mode == TestMode.FullDocument)
            {
                return engine.CanonicalizeUsingClrLibrary(xmlBuffer.CreateStream());
            }
            else
            {
                return engine.CanonicalizeUsingClrLibrary(xmlBuffer.CreateSubtreeNodeList(startAt));
            }
        }
    }
}