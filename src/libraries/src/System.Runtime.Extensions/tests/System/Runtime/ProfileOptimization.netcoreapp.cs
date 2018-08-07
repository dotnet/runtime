// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using Xunit;

namespace System.Runtime.Tests
{
    public class ProfileOptimizationTest : RemoteExecutorTestBase
    {
        [Fact]
        public void ProfileOptimization_CheckFileExists()
        {
            string tmpProfileFilePath = GetTestFileName();

            RemoteInvoke(profileFilePath =>
            {
                ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(profileFilePath));
                ProfileOptimization.StartProfile(Path.GetFileName(profileFilePath));
                return 42;
            }, tmpProfileFilePath).Dispose();

            FileInfo fileInfo = new FileInfo(tmpProfileFilePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);
        }
    }
}
