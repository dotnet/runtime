// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class FormattableStringTests : RemoteExecutorTestBase
    {
        [Fact]
        public static void Invariant_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("formattable", () => FormattableString.Invariant(null));
        }

        [Fact]
        public static void Invariant_DutchCulture_FormatsDoubleBasedOnInvariantCulture()
        {
            RemoteInvoke(
                () =>
                {
                    CultureInfo.CurrentCulture = new CultureInfo("nl"); // would be 123,456 in Dutch
                    double d = 123.456;
                    string expected = string.Format(CultureInfo.InvariantCulture, "Invariant culture is used {0}", d);
                    string actual = FormattableString.Invariant($"Invariant culture is used {d}");
                    Assert.Equal(expected, actual);

                    return SuccessExitCode;
                }).Dispose();
        }
    }
}
