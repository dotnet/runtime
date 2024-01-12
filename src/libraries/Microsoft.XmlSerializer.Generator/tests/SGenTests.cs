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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/96796", typeof(PlatformDetection), nameof(PlatformDetection.IsReadyToRunCompiled))]
        public static void SgenCommandTest()
        {
            /*
             * The intent of this test is to verify that we have not inadvertently changed the
             * code output of Microsoft.XmlSerializer.Generator. To do this, we generate
             * code output using the current Microsoft.XmlSerializer.Generator and compare it
             * to code output from a previous version of Microsoft.XmlSerializer.Generator.
             * 
             * There are times when we intentionally update the code output however. If the
             * change in code output is intentional - and correct - then update the
             * 'Expected.SerializableAssembly.XmlSerializers.cs' file with the new code output
             * to use for comparison.
             * 
             * [dotnet.exe $(OutputPath)dotnet-Microsoft.XmlSerializer.Generator.dll $(OutputPath)SerializableAssembly.dll --force --quiet]
             */

            const string CodeFile = "SerializableAssembly.XmlSerializers.cs";
            const string LKGCodeFile = "Expected.SerializableAssembly.XmlSerializers.cs";

            var type = Type.GetType("Microsoft.XmlSerializer.Generator.Sgen, dotnet-Microsoft.XmlSerializer.Generator");
            MethodInfo md = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
            string[] args = new string[] { "SerializableAssembly.dll", "--force", "--quiet" };
            int n = (int)md.Invoke(null, new object[] { args });

            Assert.Equal(0, n);
            Assert.True(File.Exists(CodeFile), string.Format("Fail to generate {0}.", CodeFile));
            // Compare the generated CodeFiles from the LKG with the live built shared framework one.
            // Not comparing byte per byte as the generated output isn't deterministic.
            Assert.Equal(LineEndingsHelper.Normalize(File.ReadAllText(LKGCodeFile)).Length, File.ReadAllText(CodeFile).Length);
        }
    }
}
