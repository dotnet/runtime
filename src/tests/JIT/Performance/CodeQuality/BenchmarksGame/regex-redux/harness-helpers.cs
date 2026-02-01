// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Helper functionality to locate inputs and find outputs for
// regex-redux benchmark in CoreCLR test harness

using System;
using System.IO;
using System.Reflection;

namespace BenchmarksGame
{
    class TestHarnessHelpers
    {
        public readonly int ExpectedLength;
        private readonly string resourceName;

        public TestHarnessHelpers(bool bigInput, [System.Runtime.CompilerServices.CallerFilePath] string csFileName = "")
        {
            if (bigInput)
            {
                ExpectedLength = 136381;
                resourceName = $"{Path.GetFileNameWithoutExtension(csFileName)}.regexdna-input25000.txt";
            }
            else
            {
                ExpectedLength = 152;
                resourceName = $"{Path.GetFileNameWithoutExtension(csFileName)}.regexdna-input25.txt";
            }
        }

        public Stream GetInputStream()
        {
            var assembly = typeof(TestHarnessHelpers).GetTypeInfo().Assembly;
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}
