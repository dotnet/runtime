// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.Tests
{
    public class ProfileOptimizationTest : RemoteExecutorTestBase
    {
        private readonly ITestOutputHelper _output;

        public ProfileOptimizationTest(ITestOutputHelper output) => _output = output;

        [Fact]
        public void ProfileOptimization_CheckFileExists()
        {
            string tmpProfileFilePath = GetTestFileName();
            string tmpTestFileName = Path.Combine(Path.GetDirectoryName(tmpProfileFilePath), Path.GetRandomFileName());

            _output.WriteLine($"We'll test write permission on path '{tmpTestFileName}'");

            RemoteInvoke((profileFilePath, testFileName) =>
            {
                // after test fail tracked by https://github.com/dotnet/corefx/issues/31792
                // we suspect that the reason is something related to write permission to the location
                // to prove that we added a simple write to file in same location of profile file directory path
                // ProfileOptimization/Multi-Core JIT could fail silently
                File.WriteAllText(testFileName, "42");

                ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(profileFilePath));
                ProfileOptimization.StartProfile(Path.GetFileName(profileFilePath));

            }, tmpProfileFilePath, tmpTestFileName).Dispose();

            FileInfo fileInfo = new FileInfo(tmpProfileFilePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);
        }
    }
}
