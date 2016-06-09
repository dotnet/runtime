// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CReaderTestModule : CGenericTestModule
    {
        private static void RunTestCase(Func<CTestBase> testCaseGenerator)
        {
            var module = new CReaderTestModule();

            module.Init(null);
            module.AddChild(testCaseGenerator());
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }

        private static void RunTest(Func<CTestBase> testCaseGenerator)
        {
            CModInfo.CommandLine = "";
            RunTestCase(testCaseGenerator);
            CModInfo.CommandLine = "/async";
            RunTestCase(testCaseGenerator);
        }

        [Fact]
        [OuterLoop]
        static public void TCConformanceSettings()
        {
            RunTest(() => new TCConformanceSettings() { Attribute = new TestCase() { Name = "Conformance Settings", Desc = "Conformance Settings" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCCreateOverloads()
        {
            RunTest(() => new TCCreateOverloads() { Attribute = new TestCase() { Name = "Create Overloads", Desc = "Create Overloads" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCFilterSettings()
        {
            RunTest(() => new TCFilterSettings() { Attribute = new TestCase() { Name = "Filter Settings", Desc = "Filter Settings" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCLineInfo()
        {
            RunTest(() => new TCLineInfo() { Attribute = new TestCase() { Name = "LineInfo", Desc = "LineInfo" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCMaxSettings()
        {
            RunTest(() => new TCMaxSettings() { Attribute = new TestCase() { Name = "MaxCharacters Settings", Desc = "MaxCharacters Settings" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCReaderSettingsGenericTestsCharCheckingReader()
        {
            RunTest(() => new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CharCheckingReader", Param = "CharCheckingReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCReaderSettingsGenericTestsSubtreeReader()
        {
            RunTest(() => new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.SubtreeReader", Param = "SubtreeReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCReaderSettingsGenericTestsCoreReader()
        {
            RunTest(() => new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CoreReader", Param = "CoreReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCReaderSettingsGenericTestsWrappedReader()
        {
            RunTest(() => new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.WrappedReader", Param = "WrappedReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCCloseInputWrappedReader()
        {
            RunTest(() => new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.WrappedReader", Param = "WrappedReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCCloseInputCharCheckingReader()
        {
            RunTest(() => new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.CharCheckingReader", Param = "CharCheckingReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCCloseInputCoreReader()
        {
            RunTest(() => new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.CoreReader", Param = "CoreReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCCloseInputSubtreeReader()
        {
            RunTest(() => new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.SubtreeReader", Param = "SubtreeReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRSGenericCharCheckingReader()
        {
            RunTest(() => new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CharCheckingReader", Param = "CharCheckingReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRSGenericCoreReader()
        {
            RunTest(() => new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CoreReader", Param = "CoreReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRSGenericWrappedReader()
        {
            RunTest(() => new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.WrappedReader", Param = "WrappedReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRSGenericSubtreeReader()
        {
            RunTest(() => new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.SubtreeReader", Param = "SubtreeReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCDtdProcessingCoreReaderCharCheckingReader()
        {
            RunTest(() => new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.CharCheckingReader", Param = "CharCheckingReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCDtdProcessingCoreReaderCoreReader()
        {
            RunTest(() => new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.CoreReader", Param = "CoreReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCDtdProcessingCoreReaderWrappedReader()
        {
            RunTest(() => new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.WrappedReader", Param = "WrappedReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCDtdProcessingCoreReaderSubtreeReader()
        {
            RunTest(() => new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.SubtreeReader", Param = "SubtreeReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCOneByteStreamCharCheckingReader()
        {
            RunTest(() => new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.CharCheckingReader", Param = "CharCheckingReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCOneByteStreamSubtreeReader()
        {
            RunTest(() => new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.SubtreeReader", Param = "SubtreeReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCOneByteStreamCoreReader()
        {
            RunTest(() => new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.CoreReader", Param = "CoreReader" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCOneByteStreamWrappedReader()
        {
            RunTest(() => new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.WrappedReader", Param = "WrappedReader" } });
        }
    }
}
