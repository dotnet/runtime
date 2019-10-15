// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class FormattableStringTests
    {
        [Fact]
        public static void Invariant_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("formattable", () => FormattableString.Invariant(null));
        }

        [Fact]
        public static void Invariant_DutchCulture_FormatsDoubleBasedOnInvariantCulture()
        {
            RemoteExecutor.Invoke(
                () =>
                {
                    CultureInfo.CurrentCulture = new CultureInfo("nl"); // would be 123,456 in Dutch
                    double d = 123.456;
                    string expected = string.Format(CultureInfo.InvariantCulture, "Invariant culture is used {0}", d);
                    string actual = FormattableString.Invariant($"Invariant culture is used {d}");
                    Assert.Equal(expected, actual);

                    return RemoteExecutor.SuccessExitCode;
                }).Dispose();
        }

        [Fact]
        public static void ToString_ReturnsSameAsStringTargetTyped()
        {
            double d = 123.456;
            string text1 = $"This will be formatted using current culture {d}";
            string text2 = ((FormattableString)$"This will be formatted using current culture {d}").ToString();
            Assert.Equal(text1, text2);
        }

        [Fact]
        public static void IFormattableToString_UsesSuppliedFormatProvider()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("nl"); // would be 123,456 in Dutch
                double d = 123.456;
                string expected = string.Format(CultureInfo.InvariantCulture, "Invariant culture is used {0}", d);
                string actual = ((IFormattable)((FormattableString)$"Invariant culture is used {d}")).ToString(null, CultureInfo.InvariantCulture);
                Assert.Equal(expected, actual);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

    }
}
