// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;
using Microsoft.XmlSerializer.Generator;
using System.IO;

namespace Microsoft.XmlSerializer.Generator.Tests
{
    public static class SgenTests
    {
        [Fact]
        public static void BasicTest()
        {
            int n = Sgen.Main(new string[0]);
            Assert.Equal(0, n);
        }

        [Fact]
        public static void SgenCommandTest()
        {
            string codefile = "Microsoft.XmlSerializer.Generator.Tests.XmlSerializers.cs";
            int n = Sgen.Main(new string[] { "Microsoft.XmlSerializer.Generator.Tests.dll", "/force", "/casesensitive" });
            Assert.Equal(0, n);
            Assert.True(File.Exists(codefile), string.Format("Fail to generate {0}.", codefile));
        }
    }
}
