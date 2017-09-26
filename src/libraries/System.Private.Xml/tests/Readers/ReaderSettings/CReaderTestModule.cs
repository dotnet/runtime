// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CReaderTestModule : CGenericTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new CReaderTestModule();

            module.Init(null);
            module.AddChild(new TCConformanceSettings() { Attribute = new TestCase() { Name = "Conformance Settings", Desc = "Conformance Settings" } });
            module.AddChild(new TCCreateOverloads() { Attribute = new TestCase() { Name = "Create Overloads", Desc = "Create Overloads" } });
            module.AddChild(new TCFilterSettings() { Attribute = new TestCase() { Name = "Filter Settings", Desc = "Filter Settings" } });
            module.AddChild(new TCLineInfo() { Attribute = new TestCase() { Name = "LineInfo", Desc = "LineInfo" } });
            module.AddChild(new TCMaxSettings() { Attribute = new TestCase() { Name = "MaxCharacters Settings", Desc = "MaxCharacters Settings" } });
            module.AddChild(new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CharCheckingReader", Param = "CharCheckingReader" } });
            module.AddChild(new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.SubtreeReader", Param = "SubtreeReader" } });
            module.AddChild(new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CoreReader", Param = "CoreReader" } });
            module.AddChild(new TCReaderSettings() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.WrappedReader", Param = "WrappedReader" } });
            module.AddChild(new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.WrappedReader", Param = "WrappedReader" } });
            module.AddChild(new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.CharCheckingReader", Param = "CharCheckingReader" } });
            module.AddChild(new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.CoreReader", Param = "CoreReader" } });
            module.AddChild(new TCCloseInput() { Attribute = new TestCase() { Name = "CloseInput.SubtreeReader", Param = "SubtreeReader" } });
            module.AddChild(new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CharCheckingReader", Param = "CharCheckingReader" } });
            module.AddChild(new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.CoreReader", Param = "CoreReader" } });
            module.AddChild(new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.WrappedReader", Param = "WrappedReader" } });
            module.AddChild(new TCRSGeneric() { Attribute = new TestCase() { Name = "ReaderSettings Generic Tests.SubtreeReader", Param = "SubtreeReader" } });
            module.AddChild(new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.CharCheckingReader", Param = "CharCheckingReader" } });
            module.AddChild(new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.CoreReader", Param = "CoreReader" } });
            module.AddChild(new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.WrappedReader", Param = "WrappedReader" } });
            module.AddChild(new TCDtdProcessingCoreReader() { Attribute = new TestCase() { Name = "TCDtdProcessingCoreReader.SubtreeReader", Param = "SubtreeReader" } });
            module.AddChild(new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.CharCheckingReader", Param = "CharCheckingReader" } });
            module.AddChild(new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.SubtreeReader", Param = "SubtreeReader" } });
            module.AddChild(new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.CoreReader", Param = "CoreReader" } });
            module.AddChild(new TCOneByteStream() { Attribute = new TestCase() { Name = "Read xml as one byte stream.WrappedReader", Param = "WrappedReader" } });

            return module;
        }
    }
}
