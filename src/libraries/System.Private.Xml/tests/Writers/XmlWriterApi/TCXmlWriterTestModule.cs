// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class XmlWriterTestModule : CTestModule
    {
        private static void RunTestCase(Func<CTestBase> testCaseGenerator)
        {
            var module = new XmlWriterTestModule();

            module.Init(null);
            module.AddChild(testCaseGenerator());
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }

        private static void RunTest(Func<CTestBase> testCaseGenerator)
        {
            CModInfo.CommandLine = "/WriterType UnicodeWriter";
            RunTestCase(testCaseGenerator);
            CModInfo.CommandLine = "/WriterType UnicodeWriter /Async true";
            RunTestCase(testCaseGenerator);
            CModInfo.CommandLine = "/WriterType UTF8Writer";
            RunTestCase(testCaseGenerator);
            CModInfo.CommandLine = "/WriterType UTF8Writer /Async true";
            RunTestCase(testCaseGenerator);
        }

        [Fact]
        [OuterLoop]
        public static void TCErrorState()
        {
            RunTest(() => new TCErrorState() { Attribute = new TestCase() { Name = "Invalid State Combinations" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCAutoComplete()
        {
            RunTest(() => new TCAutoComplete() { Attribute = new TestCase() { Name = "Auto-completion of tokens" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCDocument()
        {
            RunTest(() => new TCDocument() { Attribute = new TestCase() { Name = "WriteStart/EndDocument" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCDocType()
        {
            RunTest(() => new TCDocType() { Attribute = new TestCase() { Name = "WriteDocType" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCElement()
        {
            RunTest(() => new TCElement() { Attribute = new TestCase() { Name = "WriteStart/EndElement" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCAttribute()
        {
            RunTest(() => new TCAttribute() { Attribute = new TestCase() { Name = "WriteStart/EndAttribute" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCWriteAttributes()
        {
            RunTest(() => new TCWriteAttributes() { Attribute = new TestCase() { Name = "WriteAttributes(CoreReader)", Param = "COREREADER" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCWriteNode_XmlReader()
        {
            RunTest(() => new TCWriteNode_XmlReader() { Attribute = new TestCase() { Name = "WriteNode(CoreReader)", Param = "COREREADER" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCWriteNode_With_ReadValueChunk()
        {
            RunTest(() => new TCWriteNode_With_ReadValueChunk() { Attribute = new TestCase() { Name = "WriteNode with streaming API ReadValueChunk - COREREADER", Param = "COREREADER" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCFullEndElement()
        {
            RunTest(() => new TCFullEndElement() { Attribute = new TestCase() { Name = "WriteFullEndElement" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCEOFHandling()
        {
            RunTest(() => new TCEOFHandling() { Attribute = new TestCase() { Name = "XmlWriterSettings: NewLineHandling" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCErrorConditionWriter()
        {
            RunTest(() => new TCErrorConditionWriter() { Attribute = new TestCase() { Name = "ErrorCondition" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCNamespaceHandling()
        {
            RunTest(() => new TCNamespaceHandling() { Attribute = new TestCase() { Name = "XmlWriterSettings: NamespaceHandling" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCDefaultWriterSettings()
        {
            RunTest(() => new TCDefaultWriterSettings() { Attribute = new TestCase() { Name = "XmlWriterSettings: Default Values" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCWriterSettingsMisc()
        {
            RunTest(() => new TCWriterSettingsMisc() { Attribute = new TestCase() { Name = "XmlWriterSettings: Reset/Clone" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCOmitXmlDecl()
        {
            RunTest(() => new TCOmitXmlDecl() { Attribute = new TestCase() { Name = "XmlWriterSettings: OmitXmlDeclaration" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCCheckChars()
        {
            RunTest(() => new TCCheckChars() { Attribute = new TestCase() { Name = "XmlWriterSettings: CheckCharacters" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCNewLineHandling()
        {
            RunTest(() => new TCNewLineHandling() { Attribute = new TestCase() { Name = "XmlWriterSettings: NewLineHandling" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCNewLineChars()
        {
            RunTest(() => new TCNewLineChars() { Attribute = new TestCase() { Name = "XmlWriterSettings: NewLineChars" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCIndent()
        {
            RunTest(() => new TCIndent() { Attribute = new TestCase() { Name = "XmlWriterSettings: Indent" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCIndentChars()
        {
            RunTest(() => new TCIndentChars() { Attribute = new TestCase() { Name = "XmlWriterSettings: IndentChars" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCNewLineOnAttributes()
        {
            RunTest(() => new TCNewLineOnAttributes() { Attribute = new TestCase() { Name = "XmlWriterSettings: NewLineOnAttributes" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCStandAlone()
        {
            RunTest(() => new TCStandAlone() { Attribute = new TestCase() { Name = "Standalone" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCCloseOutput()
        {
            RunTest(() => new TCCloseOutput() { Attribute = new TestCase() { Name = "XmlWriterSettings: CloseOutput" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCFragmentCL()
        {
            RunTest(() => new TCFragmentCL() { Attribute = new TestCase() { Name = "CL = Fragment Tests" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCAutoCL()
        {
            RunTest(() => new TCAutoCL() { Attribute = new TestCase() { Name = "CL = Auto Tests" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCFlushClose()
        {
            RunTest(() => new TCFlushClose() { Attribute = new TestCase() { Name = "Close()/Flush()" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCWriterWithMemoryStream()
        {
            RunTest(() => new TCWriterWithMemoryStream() { Attribute = new TestCase() { Name = "XmlWriter with MemoryStream" } });
        }

        [Fact]
        [OuterLoop]
        public static void TCWriteEndDocumentOnCloseTest()
        {
            RunTest(() => new TCWriteEndDocumentOnCloseTest() { Attribute = new TestCase() { Name = "XmlWriterSettings: WriteEndDocumentOnClose" } });
        }
    }
}
