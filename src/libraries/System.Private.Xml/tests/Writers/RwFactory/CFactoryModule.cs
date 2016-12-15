// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public partial class CFactoryModule : CXmlDriverModule
    {
        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            CModInfo.CommandLine = "";
            var module = new CFactoryModule();

            module.Init(null);
            module.Execute();
        }

        [Fact]
        [OuterLoop]
        public static void RunTestsAsync()
        {
            CModInfo.CommandLine = "/async";
            var module = new CFactoryModule();

            module.Init(null);
            module.Execute();
        }
    }
}
