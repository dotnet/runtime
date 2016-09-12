// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoMiscTests
    {
        [Fact]
        [ActiveIssue(11612, Xunit.PlatformID.AnyUnix)]
        public void DigitSubstitutionTest()
        {
            // DigitSubstitution is not used in number formatting.
            Assert.Equal(DigitShapes.None, CultureInfo.InvariantCulture.NumberFormat.DigitSubstitution);
        }

        [Fact]
        [ActiveIssue(11612, Xunit.PlatformID.AnyUnix)]
        public void NativeDigitsTest()
        {
            string [] nativeDigits = CultureInfo.InvariantCulture.NumberFormat.NativeDigits;
            Assert.Equal(new string [] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }, nativeDigits);

            NumberFormatInfo nfi = (NumberFormatInfo) CultureInfo.InvariantCulture.NumberFormat.Clone();
            string [] newDigits = new string [] { "\u0660", "\u0661", "\u0662", "\u0663", "\u0664", "\u0665", "\u0666", "\u0667", "\u0668", "\u0669" };
            nfi.NativeDigits = newDigits;

            Assert.Equal(newDigits, nfi.NativeDigits);
        }
    }
}
