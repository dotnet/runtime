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
        public static IEnumerable<object[]> DigitSubstitution_TestData()
        {
            yield return new object[] { "ar"        , DigitShapes.Context };
            yield return new object[] { "ar-001"    , DigitShapes.Context };
            yield return new object[] { "ar-AE"     , DigitShapes.Context };
            yield return new object[] { "ar-BH"     , DigitShapes.Context };
            yield return new object[] { "ar-DJ"     , DigitShapes.Context };
            yield return new object[] { "ar-EG"     , DigitShapes.Context };
            yield return new object[] { "ar-ER"     , DigitShapes.Context };
            yield return new object[] { "ar-IL"     , DigitShapes.Context };
            yield return new object[] { "ar-IQ"     , DigitShapes.Context };
            yield return new object[] { "ar-JO"     , DigitShapes.Context };
            yield return new object[] { "ar-KM"     , DigitShapes.Context };
            yield return new object[] { "ar-KW"     , DigitShapes.Context };
            yield return new object[] { "ar-LB"     , DigitShapes.Context };
            yield return new object[] { "ar-MR"     , DigitShapes.Context };
            yield return new object[] { "ar-OM"     , DigitShapes.Context };
            yield return new object[] { "ar-PS"     , DigitShapes.Context };
            yield return new object[] { "ar-QA"     , DigitShapes.Context };
            yield return new object[] { "ar-SA"     , DigitShapes.Context };
            yield return new object[] { "ar-SD"     , DigitShapes.Context };
            yield return new object[] { "ar-SO"     , DigitShapes.Context };
            yield return new object[] { "ar-SS"     , DigitShapes.Context };
            yield return new object[] { "ar-SY"     , DigitShapes.Context };
            yield return new object[] { "ar-TD"     , DigitShapes.Context };
            yield return new object[] { "ar-YE"     , DigitShapes.Context };
            yield return new object[] { "dz"        , DigitShapes.NativeNational };
            yield return new object[] { "dz-BT"     , DigitShapes.NativeNational };
            yield return new object[] { "fa"        , DigitShapes.Context };
            yield return new object[] { "fa-IR"     , DigitShapes.Context };
            yield return new object[] { "km"        , DigitShapes.NativeNational };
            yield return new object[] { "km-KH"     , DigitShapes.NativeNational };
            yield return new object[] { "ks"        , DigitShapes.NativeNational };
            yield return new object[] { "ks-Arab"   , DigitShapes.NativeNational };
            yield return new object[] { "ks-Arab-IN", DigitShapes.NativeNational };
            yield return new object[] { "ku"        , DigitShapes.Context };
            yield return new object[] { "ku-Arab"   , DigitShapes.Context };
            yield return new object[] { "ku-Arab-IQ", DigitShapes.Context };
            yield return new object[] { "my"        , DigitShapes.NativeNational };
            yield return new object[] { "my-MM"     , DigitShapes.NativeNational };
            yield return new object[] { "ne-IN"     , DigitShapes.NativeNational };
            yield return new object[] { "nqo"       , DigitShapes.NativeNational };
            yield return new object[] { "nqo-GN"    , DigitShapes.NativeNational };
            yield return new object[] { "pa-Arab"   , DigitShapes.NativeNational };
            yield return new object[] { "pa-Arab-PK", DigitShapes.NativeNational };
            yield return new object[] { "prs"       , DigitShapes.NativeNational };
            yield return new object[] { "prs-AF"    , DigitShapes.NativeNational };
            yield return new object[] { "ps"        , DigitShapes.NativeNational };
            yield return new object[] { "ps-AF"     , DigitShapes.NativeNational };
            yield return new object[] { "sd"        , DigitShapes.NativeNational };
            yield return new object[] { "sd-Arab"   , DigitShapes.NativeNational };
            yield return new object[] { "sd-Arab-PK", DigitShapes.NativeNational };
            yield return new object[] { "ur-IN"     , DigitShapes.NativeNational };
            yield return new object[] { "uz-Arab"   , DigitShapes.NativeNational };
            yield return new object[] { "uz-Arab-AF", DigitShapes.NativeNational };
        }
        
        [Fact]
        public void DigitSubstitutionTest()
        {
            // DigitSubstitution is not used in number formatting.
            Assert.Equal(DigitShapes.None, CultureInfo.InvariantCulture.NumberFormat.DigitSubstitution);
        }

        [Fact]
        public void NativeDigitsTest()
        {
            string [] nativeDigits = CultureInfo.InvariantCulture.NumberFormat.NativeDigits;
            Assert.Equal(new string [] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }, nativeDigits);

            NumberFormatInfo nfi = (NumberFormatInfo) CultureInfo.InvariantCulture.NumberFormat.Clone();
            string [] newDigits = new string [] { "\u0660", "\u0661", "\u0662", "\u0663", "\u0664", "\u0665", "\u0666", "\u0667", "\u0668", "\u0669" };
            nfi.NativeDigits = newDigits;

            Assert.Equal(newDigits, nfi.NativeDigits);
        }
        
        [Theory]
        [MemberData(nameof(DigitSubstitution_TestData))]
        public void DigitSubstitutionListTest(string cultureName, DigitShapes shape)
        {
            try
            {
                CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
                Assert.Equal(ci.NumberFormat.DigitSubstitution, shape);
            }
            catch (CultureNotFoundException)
            {
                // ignore the cultures that we cannot create as it is not supported on the platforms
            }
        }
        
    }
}
