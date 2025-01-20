// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Tests
{
    public static partial class TimeZoneInfoTests
    {
        [Fact]
        public static void OnlyUtcWhenInvariant()
        {
            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_strPacific));
            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_strSydney));
            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_strGMT));
        }

        [Fact]
        public static void NoAlternateUTCIdsInvariant()
        {
            foreach (string alias in s_UtcAliases)
            {
                if (alias == s_strUtc)
                {
                    continue;
                }
                Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(alias));
            }
        }
    }
}
