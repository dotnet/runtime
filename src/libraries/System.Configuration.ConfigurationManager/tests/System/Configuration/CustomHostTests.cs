// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
                .GetMethod("SetEntryAssembly", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[]{ null });

            Assert.Null(Assembly.GetEntryAssembly());
        }
    }
}
