﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Configuration.Tests
{
    /// <summary>
    /// Tests ConfigurationManager works even when Assembly.GetEntryAssembly() returns null.
    /// </summary>
    public class CustomHostTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Does not apply to .NET Framework.")]
        public void FilePathIsPopulatedCorrectly()
        {
            RemoteExecutor.Invoke(() =>
            {
                MakeAssemblyGetEntryAssemblyReturnNull();

                string expectedFilePathEnding = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    "dotnet.exe.config" :
                    "dotnet.config";

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                Assert.EndsWith(expectedFilePathEnding, config.FilePath);
            }).Dispose();
        }

        /// <summary>
        /// Makes Assembly.GetEntryAssembly() return null using private reflection.
        /// </summary>
        private static void MakeAssemblyGetEntryAssemblyReturnNull()
        {
            typeof(Assembly)
                .GetField("s_forceNullEntryPoint", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, true);

            Assert.Null(Assembly.GetEntryAssembly());
        }
    }
}
