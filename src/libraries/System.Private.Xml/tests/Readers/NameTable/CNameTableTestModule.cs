// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CNameTableTestModule : CTestModule
    {
        private static void RunTestCaseAsync(Func<CTestBase> testCaseGenerator)
        {
            CModInfo.CommandLine = "/async";
            RunTestCase(testCaseGenerator);
        }

        private static void RunTestCase(Func<CTestBase> testCaseGenerator)
        {
            var module = new CNameTableTestModule();

            module.Init(null);
            module.AddChild(testCaseGenerator());
            module.Execute();

            Assert.Equal(0, module.FailCount);
        }

        private static void RunTest(Func<CTestBase> testCaseGenerator)
        {
            RunTestCase(testCaseGenerator);
            RunTestCaseAsync(testCaseGenerator);
        }

        [Fact]
        [OuterLoop]
        static public void TCUserNameTable()
        {
            RunTest(() => new TCUserNameTable() { Attribute = new TestCase() { Name = "XmlNameTable user scenario inheritance", Desc = "XmlNameTable inheritance" } });
        }

        [Fact]
        [OuterLoop]
        static public void NameTableVerifyWGetChar()
        {
            RunTest(() => new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWGetChar", Desc = "VerifyWGetChar" } });
        }

        [Fact]
        [OuterLoop]
        static public void NameTableVerifyWAddChar()
        {
            RunTest(() => new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWAddChar", Desc = "VerifyWAddChar" } });
            RunTest(() => new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWAddString", Desc = "VerifyWAddString" } });
            RunTest(() => new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWGetString", Desc = "VerifyWGetString" } });
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddChar", Desc = "VerifyWAddChar" } });
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetChar", Desc = "VerifyWGetChar" } });
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddString", Desc = "VerifyWAddString" } });
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetString", Desc = "VerifyWGetString" } });
        }

        [Fact]
        [OuterLoop]
        static public void NameTableVerifyWAddString()
        {
            RunTest(() => new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWAddString", Desc = "VerifyWAddString" } });
        }

        [Fact]
        [OuterLoop]
        static public void NameTableVerifyWGetString()
        {
            RunTest(() => new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWGetString", Desc = "VerifyWGetString" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRecordNameTableAddVerifyWAddChar()
        {
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddChar", Desc = "VerifyWAddChar" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRecordNameTableAddVerifyWGetChar()
        {
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetChar", Desc = "VerifyWGetChar" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRecordNameTableAddVerifyWAddString()
        {
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddString", Desc = "VerifyWAddString" } });
        }

        [Fact]
        [OuterLoop]
        static public void TCRecordNameTableAddVerifyWGetString()
        {
            RunTest(() => new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetString", Desc = "VerifyWGetString" } });
        }
    }
}
