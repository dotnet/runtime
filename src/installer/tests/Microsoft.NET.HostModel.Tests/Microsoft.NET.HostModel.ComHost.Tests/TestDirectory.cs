// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.NET.HostModel.ComHost.Tests
{
    internal class TestDirectory : IDisposable
    {
        public string Path { get; private set; }

        private TestDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public static TestDirectory Create([CallerMemberName] string callingMethod = "")
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "dotNetSdkUnitTest_" + callingMethod + (Guid.NewGuid().ToString().Substring(0, 8)));
            return new TestDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
