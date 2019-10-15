// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CNameTableTestModule : CTestModule
    {
        [Theory]
        [XmlTests(nameof(Create))]
        public void RunTests(XunitTestCase testCase)
        {
            testCase.Run();
        }

        public static CTestModule Create()
        {
            var module = new CNameTableTestModule();

            module.Init(null);
            module.AddChild(new TCUserNameTable() { Attribute = new TestCase() { Name = "XmlNameTable user scenario inheritance", Desc = "XmlNameTable inheritance" } });
            module.AddChild(new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWGetChar", Desc = "VerifyWGetChar" } });
            module.AddChild(new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWAddChar", Desc = "VerifyWAddChar" } });
            module.AddChild(new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWAddString", Desc = "VerifyWAddString" } });
            module.AddChild(new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWGetString", Desc = "VerifyWGetString" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddChar", Desc = "VerifyWAddChar" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetChar", Desc = "VerifyWGetChar" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddString", Desc = "VerifyWAddString" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetString", Desc = "VerifyWGetString" } });
            module.AddChild(new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWAddString", Desc = "VerifyWAddString" } });
            module.AddChild(new TCRecordNameTableGet() { Attribute = new TestCase() { Name = "NameTable(Get) VerifyWGetString", Desc = "VerifyWGetString" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddChar", Desc = "VerifyWAddChar" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetChar", Desc = "VerifyWGetChar" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWAddString", Desc = "VerifyWAddString" } });
            module.AddChild(new TCRecordNameTableAdd() { Attribute = new TestCase() { Name = "NameTable(Add) VerifyWGetString", Desc = "VerifyWGetString" } });

            return module;
        }
    }
}
