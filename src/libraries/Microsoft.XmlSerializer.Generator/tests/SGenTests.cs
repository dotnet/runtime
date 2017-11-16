// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;
using Microsoft.XmlSerializer.Generator;
using System.IO;
using System;
using System.Reflection;

namespace Microsoft.XmlSerializer.Generator.Tests
{
    public static class SgenTests
    {
        [Fact]
        public static void SgenCommandTest()
        {
            string codefile = "Microsoft.XmlSerializer.Generator.Tests.XmlSerializers.cs";
            var type = Type.GetType("Microsoft.XmlSerializer.Generator.Sgen, dotnet-Microsoft.XmlSerializer.Generator");
            MethodInfo md = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
            string[] args = new string[] { "Microsoft.XmlSerializer.Generator.Tests.dll", "/force", "/quiet" };
            int n = (int)md.Invoke(null, new object[] { args });
            Assert.Equal(0, n);
            Assert.True(File.Exists(codefile), string.Format("Fail to generate {0}.", codefile));
        }
    }
}
