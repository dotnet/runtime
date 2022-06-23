// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static class RegexAssert
    {
        public static void Equal(string expected, Capture actual)
        {
            Assert.Equal(expected, actual.Value);
            Assert.Equal(expected, actual.ValueSpan.ToString());
        }
    }
}
