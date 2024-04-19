// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedMonthNames
    {
        [Fact]
        public void AbbreviatedMonthNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" }, DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames);
        }

        [Fact]
        public void AbbreviatedMonthNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.AbbreviatedMonthNames, format.AbbreviatedMonthNames);
            Assert.NotSame(format.AbbreviatedMonthNames, format.AbbreviatedMonthNames);
        }

        public static IEnumerable<object[]> AbbreviatedMonthNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        public static IEnumerable<object[]> AbbreviatedMonthNames_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { "ar-SA", new string[] { "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة", "" } };
            yield return new object[] { "am-ET", new string[] { "ጃንዩ", "ፌብሩ", "ማርች", "ኤፕሪ", "ሜይ", "ጁን", "ጁላይ", "ኦገስ", "ሴፕቴ", "ኦክቶ", "ኖቬም", "ዲሴም", "" } };
            yield return new object[] { "bg-BG", new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "" } }; // "яну", "фев", "март", "апр", "май", "юни", "юли", "авг", "сеп", "окт", "ное", "дек", ""
            yield return new object[] { "bn-BD", new string[] { "জানু", "ফেব", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", "" } }; // "জানুয়ারী", "ফেব্রুয়ারী", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", ""
            yield return new object[] { "bn-IN", new string[] { "জানু", "ফেব", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেঃ", "অক্টোঃ", "নভেঃ", "ডিসেঃ", "" } }; // BUG. JS returns Genitive even though we expect Nominative; "জানু", "ফেব", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", ""
            yield return new object[] { "ca-AD", new string[] { "gen.", "febr.", "març", "abr.", "maig", "juny", "jul.", "ag.", "set.", "oct.", "nov.", "des.", "" } };
            yield return new object[] { "ca-ES", new string[] { "gen.", "febr.", "març", "abr.", "maig", "juny", "jul.", "ag.", "set.", "oct.", "nov.", "des.", "" } };
            yield return new object[] { "cs-CZ", new string[] { "led", "úno", "bře", "dub", "kvě", "čvn", "čvc", "srp", "zář", "říj", "lis", "pro", "" } };
            yield return new object[] { "da-DK", new string[] { "jan.", "feb.", "mar.", "apr.", "maj", "jun.", "jul.", "aug.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { "de-AT", new string[] { "Jän", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "de-BE", new string[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "de-CH", new string[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "de-DE", new string[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "de-IT", new string[] { "Jän", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "de-LI", new string[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "de-LU", new string[] { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez", "" } };
            yield return new object[] { "el-CY", new string[] { "Ιαν", "Φεβ", "Μαρ", "Απρ", "Μαΐ", "Ιουν", "Ιουλ", "Αυγ", "Σεπ", "Οκτ", "Νοε", "Δεκ", "" } }; // BUG. JS returns Genitive for Greek even though we expect Nominative; "Ιαν", "Φεβ", "Μάρ", "Απρ", "Μάι", "Ιούν", "Ιούλ", "Αύγ", "Σεπ", "Οκτ", "Νοέ", "Δεκ", ""
            yield return new object[] { "el-GR", new string[] { "Ιαν", "Φεβ", "Μαρ", "Απρ", "Μαΐ", "Ιουν", "Ιουλ", "Αυγ", "Σεπ", "Οκτ", "Νοε", "Δεκ", "" } }; // BUG.
            yield return new object[] { "en-AE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-AG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-AI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-AS", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-AT", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-AU", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-BB", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-BE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-BI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-BM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-BS", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-BW", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-BZ", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-CA", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } }; // "Jan.", "Feb.", "Mar.", "Apr.", "May", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", ""
            yield return new object[] { "en-CC", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-CH", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-CK", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-CM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-CX", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-CY", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-DE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-DK", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-DM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-ER", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-FI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-FJ", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-FK", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-FM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GB", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GD", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GH", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-GU", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-GY", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-HK", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-IE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-IL", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-IM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-IN", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-IO", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-JE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-JM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-KE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-KI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-KN", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-KY", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-LC", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-LR", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-LS", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MH", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-MO", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MP", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-MS", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MT", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MU", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MW", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-MY", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NA", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NF", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NL", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NR", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NU", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-NZ", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-PG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-PH", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-PK", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-PN", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-PR", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-PW", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-RW", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SB", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SC", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SD", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SE", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SH", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SL", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SS", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SX", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-SZ", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-TC", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-TK", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-TO", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-TT", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-TV", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-TZ", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-UG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-UM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-US", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-VC", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-VG", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-VI", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { "en-VU", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-WS", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-ZA", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-ZM", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "en-ZW", new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { "es-419", new string[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic", "" } }; //  "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic.", ""
            yield return new object[] { "es-ES", new string[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic", "" } }; //  "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic.", ""
            yield return new object[] { "es-MX", new string[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic", "" } }; //  "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic.", ""
            yield return new object[] { "et-EE", new string[] { "jaanuar", "veebruar", "märts", "aprill", "mai", "juuni", "juuli", "august", "september", "oktoober", "november", "detsember", "" } }; // "jaan", "veebr", "märts", "apr", "mai", "juuni", "juuli", "aug", "sept", "okt", "nov", "dets", ""
            yield return new object[] { "fa-IR", new string[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" } };
            yield return new object[] { "fi-FI", new string[] { "tammi", "helmi", "maalis", "huhti", "touko", "kesä", "heinä", "elo", "syys", "loka", "marras", "joulu", "" } };
            yield return new object[] { "fil-PH", new string[] { "Ene", "Peb", "Mar", "Abr", "May", "Hun", "Hul", "Ago", "Set", "Okt", "Nob", "Dis", "" } };
            yield return new object[] { "fr-BE", new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { "fr-CA", new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juill.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { "fr-CH", new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { "fr-FR", new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { "gu-IN", new string[] { "જાન્યુ", "ફેબ્રુ", "માર્ચ", "એપ્રિલ", "મે", "જૂન", "જુલાઈ", "ઑગસ્ટ", "સપ્ટે", "ઑક્ટો", "નવે", "ડિસે", "" } };
            yield return new object[] { "he-IL", new string[] { "ינו׳", "פבר׳", "מרץ", "אפר׳", "מאי", "יוני", "יולי", "אוג׳", "ספט׳", "אוק׳", "נוב׳", "דצמ׳", "" } };
            yield return new object[] { "hi-IN", new string[] { "जन॰", "फ़र॰", "मार्च", "अप्रैल", "मई", "जून", "जुल॰", "अग॰", "सित॰", "अक्तू॰", "नव॰", "दिस॰", "" } };
            yield return new object[] { "hr-BA", new string[] { "sij", "velj", "ožu", "tra", "svi", "lip", "srp", "kol", "ruj", "lis", "stu", "pro", "" } };
            yield return new object[] { "hr-HR", new string[] { "sij", "velj", "ožu", "tra", "svi", "lip", "srp", "kol", "ruj", "lis", "stu", "pro", "" } };
            yield return new object[] { "hu-HU", new string[] { "jan.", "febr.", "márc.", "ápr.", "máj.", "jún.", "júl.", "aug.", "szept.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { "id-ID", new string[] { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { "it-CH", new string[] { "gen", "feb", "mar", "apr", "mag", "giu", "lug", "ago", "set", "ott", "nov", "dic", "" } };
            yield return new object[] { "it-IT", new string[] { "gen", "feb", "mar", "apr", "mag", "giu", "lug", "ago", "set", "ott", "nov", "dic", "" } };
            yield return new object[] { "ja-JP", new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { "kn-IN", new string[] { "ಜನ", "ಫೆಬ್ರ", "ಮಾರ್ಚ್", "ಏಪ್ರಿ", "ಮೇ", "ಜೂನ್", "ಜುಲೈ", "ಆಗ", "ಸೆಪ್ಟೆಂ", "ಅಕ್ಟೋ", "ನವೆಂ", "ಡಿಸೆಂ", "" } };
            yield return new object[] { "ko-KR", new string[] { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월", "" } };
            yield return new object[] { "lt-LT", new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "" } }; // "saus.", "vas.", "kov.", "bal.", "geg.", "birž.", "liep.", "rugp.", "rugs.", "spal.", "lapkr.", "gruod."
            yield return new object[] { "lv-LV", new string[] { "janv.", "febr.", "marts", "apr.", "maijs", "jūn.", "jūl.", "aug.", "sept.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { "ml-IN", new string[] { "ജനു", "ഫെബ്രു", "മാർ", "ഏപ്രി", "മേയ്", "ജൂൺ", "ജൂലൈ", "ഓഗ", "സെപ്റ്റം", "ഒക്ടോ", "നവം", "ഡിസം", "" } };
            yield return new object[] { "mr-IN", new string[] { "जाने", "फेब्रु", "मार्च", "एप्रि", "मे", "जून", "जुलै", "ऑग", "सप्टें", "ऑक्टो", "नोव्हें", "डिसें", "" } };
            yield return new object[] { "ms-BN", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ogo", "Sep", "Okt", "Nov", "Dis", "" } };
            yield return new object[] { "ms-MY", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ogo", "Sep", "Okt", "Nov", "Dis", "" } };
            yield return new object[] { "ms-SG", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ogo", "Sep", "Okt", "Nov", "Dis", "" } };
            yield return new object[] { "nb-NO", new string[] { "jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "des", "" } };
            yield return new object[] { "no", new string[] { "jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "des", "" } };
            yield return new object[] { "no-NO", new string[] { "jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "des", "" } };
            var dutchMonths = new string[] { "jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec", "" }; // "jan.", "feb.", "mrt.", "apr.", "mei", "jun.", "jul.", "aug.", "sep.", "okt.", "nov.", "dec.", ""
            yield return new object[] { "nl-AW", dutchMonths };
            yield return new object[] { "nl-BE", dutchMonths };
            yield return new object[] { "nl-NL", dutchMonths };
            yield return new object[] { "pl-PL", new string[] { "sty", "lut", "mar", "kwi", "maj", "cze", "lip", "sie", "wrz", "paź", "lis", "gru", "" } };
            yield return new object[] { "pt-BR", new string[] { "jan.", "fev.", "mar.", "abr.", "mai.", "jun.", "jul.", "ago.", "set.", "out.", "nov.", "dez.", "" } };
            yield return new object[] { "pt-PT", new string[] { "jan.", "fev.", "mar.", "abr.", "mai.", "jun.", "jul.", "ago.", "set.", "out.", "nov.", "dez.", "" } };
            yield return new object[] { "ro-RO", new string[] { "ian.", "feb.", "mar.", "apr.", "mai", "iun.", "iul.", "aug.", "sept.", "oct.", "nov.", "dec.", "" } };
            yield return new object[] { "ru-RU", new string[] { "янв.", "февр.", "март", "апр.", "май", "июнь", "июль", "авг.", "сент.", "окт.", "нояб.", "дек.", "" } };
            yield return new object[] { "sk-SK", new string[] { "jan", "feb", "mar", "apr", "máj", "jún", "júl", "aug", "sep", "okt", "nov", "dec", "" } };
            yield return new object[] { "sl-SI", new string[] { "jan.", "feb.", "mar.", "apr.", "maj", "jun.", "jul.", "avg.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { "sr-Cyrl-RS", new string[] { "јан", "феб", "мар", "апр", "мај", "јун", "јул", "авг", "сеп", "окт", "нов", "дец", "" } };
            yield return new object[] { "sr-Latn-RS", new string[] { "jan", "feb", "mar", "apr", "maj", "jun", "jul", "avg", "sep", "okt", "nov", "dec", "" } };
            yield return new object[] { "sv-AX", new string[] { "jan.", "feb.", "mars", "apr.", "maj", "juni", "juli", "aug.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { "sv-SE", new string[] { "jan.", "feb.", "mars", "apr.", "maj", "juni", "juli", "aug.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { "sw-CD", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { "sw-KE", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { "sw-TZ", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { "sw-UG", new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { "ta-IN", new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { "ta-LK", new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { "ta-MY", new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { "ta-SG", new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { "te-IN", new string[] { "జన", "ఫిబ్ర", "మార్చి", "ఏప్రి", "మే", "జూన్", "జులై", "ఆగ", "సెప్టెం", "అక్టో", "నవం", "డిసెం", "" } };
            yield return new object[] { "th-TH", new string[] { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค.", "" } };
            yield return new object[] { "tr-CY", new string[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara", "" } };
            yield return new object[] { "tr-TR", new string[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara", "" } };
            yield return new object[] { "uk-UA", new string[] { "січ", "лют", "бер", "кві", "тра", "чер", "лип", "сер", "вер", "жов", "лис", "гру", "" } };
            yield return new object[] { "vi-VN", new string[] { "Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12", "" } };
            yield return new object[] { "zh-CN", new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { "zh-Hans-HK", new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { "zh-SG", new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { "zh-HK", new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { "zh-TW", new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(AbbreviatedMonthNames_Get_TestData_HybridGlobalization))]
        public void AbbreviatedMonthNames_Get_ReturnsExpected_HybridGlobalization(string cultureName, string[] expected)
        {
            var format = new CultureInfo(cultureName).DateTimeFormat;
            int length = format.AbbreviatedMonthNames.Length;
            Assert.True(length == expected.Length, $"Length comparison failed for culture: {cultureName}. Expected: {expected.Length}, Actual: {length}");
            for (int i = 0; i<length; i++)
                Assert.True(expected[i] == format.AbbreviatedMonthNames[i], $"Failed for culture: {cultureName} on index: {i}. Expected: {expected[i]}, Actual: {format.AbbreviatedMonthNames[i]}");
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthNames_Set_TestData))]
        public void AbbreviatedMonthNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthNames = value;
            Assert.Equal(value, format.AbbreviatedMonthNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.AbbreviatedMonthNames);
            Assert.Equal(value, format.AbbreviatedMonthNames);
        }

        [Fact]
        public void AbbreviatedMonthNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthNames = null);
        }

        [Fact]
        public void AbbreviatedMonthNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> AbbreviatedMonthNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthNames_SetInvalidLength_TestData))]
        public void AbbreviatedMonthNames_SetNullValueInValues_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.AbbreviatedMonthNames = value);
        }

        [Fact]
        public void AbbreviatedMonthNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        [Fact]
        public void AbbreviatedMonthNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthNames = new string[] { "Jan.", "Feb.", "Mar.", "Apr.", "May.", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", "." };
            Assert.Equal("Jun. 76", new DateTime(1976, 6, 19).ToString("MMM yy", format));
        }

        [Fact]
        public void AbbreviatedMonthNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                AbbreviatedMonthNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("MMM", format));
        }
    }
}
