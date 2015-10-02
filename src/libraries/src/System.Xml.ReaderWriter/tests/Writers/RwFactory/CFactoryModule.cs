// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CFactoryModule : CXmlDriverModule
    {
        [Fact]
        [OuterLoop]
        static public void RunTests()
        {
            CModInfo.CommandLine = "";
            var module = new CFactoryModule();

            module.Init(null);
            module.Execute();
        }

        [Fact]
        [OuterLoop]
        static public void RunTestsAsync()
        {
            CModInfo.CommandLine = "/async";
            var module = new CFactoryModule();

            module.Init(null);
            module.Execute();
        }
    }
}