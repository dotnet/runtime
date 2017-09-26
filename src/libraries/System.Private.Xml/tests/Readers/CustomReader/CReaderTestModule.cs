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
            module.AddChild(new TCReadReader() { Attribute = new TestCase() { Name = "Read", Desc = "CustomInheritedReader" } });

            return module;
        }
    }
}
