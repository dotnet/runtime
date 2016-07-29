// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public static class NetStandard17Tests
    {
        public static IEnumerable<object[]> Equals_EncyclopaediaData()
        {
            yield return new object[] { StringComparison.CurrentCulture, false };
            yield return new object[] { StringComparison.CurrentCultureIgnoreCase, false };
            yield return new object[] { StringComparison.Ordinal, false };
            yield return new object[] { StringComparison.OrdinalIgnoreCase, false };

            // Windows and ICU disagree about how these strings compare in the default locale.
            yield return new object[] { StringComparison.InvariantCulture, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) };
            yield return new object[] { StringComparison.InvariantCultureIgnoreCase, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) };
        }

        [Theory]
        [MemberData(nameof(Equals_EncyclopaediaData))]
        public static void Equals_Encyclopaedia(StringComparison comparison, bool expected)
        {
            string source = "encyclop\u00e6dia";
            string target = "encyclopaedia";

            Helpers.PerformActionWithCulture(new CultureInfo("se-SE"), () =>
            {
                Assert.Equal(expected, string.Equals(source, target, comparison));
            });
        }
    }
}
