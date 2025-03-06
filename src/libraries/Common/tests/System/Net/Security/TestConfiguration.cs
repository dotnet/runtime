// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    internal static partial class TestConfiguration
    {
        public const int PassingTestTimeoutMilliseconds = 1 * 60 * 1000;
        public static TimeSpan PassingTestTimeout => TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds);
    }
}
