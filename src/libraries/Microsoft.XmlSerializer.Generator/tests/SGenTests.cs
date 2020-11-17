// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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
            const string CodeFile = "SerializableAssembly.XmlSerializers.cs";
            const string LKGCodeFile = "LKG." + CodeFile;

            var type = Type.GetType("Microsoft.XmlSerializer.Generator.Sgen, dotnet-Microsoft.XmlSerializer.Generator");
            MethodInfo md = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
            string[] args = new string[] { "SerializableAssembly.dll", "--force", "--quiet" };
            int n = (int)md.Invoke(null, new object[] { args });

            Assert.Equal(0, n);
            Assert.True(File.Exists(CodeFile), string.Format("Fail to generate {0}.", CodeFile));
            // Compare the generated CodeFiles from the LKG with the live built shared framework one.
            // Not comparing byte per byte as the generated output isn't deterministic.
            Assert.Equal(new System.IO.FileInfo(LKGCodeFile).Length, new System.IO.FileInfo(CodeFile).Length);
        }
    }
}
