// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedMonthGenitiveNames
    {
        public static IEnumerable<object[]> AbbreviatedMonthGenitiveNames_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, new string[] { "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة", "" } };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, new string[] { "ጃንዩ", "ፌብሩ", "ማርች", "ኤፕሪ", "ሜይ", "ጁን", "ጁላይ", "ኦገስ", "ሴፕቴ", "ኦክቶ", "ኖቬም", "ዲሴም", "" } };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "" } }; //"яну", "фев", "март", "апр", "май", "юни", "юли", "авг", "сеп", "окт", "ное", "дек", ""
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat,
                PlatformDetection.IsNodeJS ? // NodeJs responds like dotnet
                    new string[] { "জানু", "ফেব", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", "" } :
                    new string[] { "জানু", "ফেব", "মার্চ", "এপ্রি", "মে", "জুন", "জুল", "আগ", "সেপ", "অক্টো", "নভে", "ডিসে", "" } };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat,
                PlatformDetection.IsNodeJS ? // NodeJs responds like dotnet
                    new string[] { "জানু", "ফেব", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", "" } :
                    new string[] { "জানু", "ফেব", "মার্চ", "এপ্রি", "মে", "জুন", "জুল", "আগ", "সেপ্টেঃ", "অক্টোঃ", "নভেঃ", "ডিসেঃ", "" } };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, new string[] { "gen.", "febr.", "març", "abr.", "maig", "juny", "jul.", "ag.", "set.", "oct.", "nov.", "des.", "" } }; // "de gen.", "de febr.", "de març", "d’abr.", "de maig", "de juny", "de jul.", "d’ag.", "de set.", "d’oct.", "de nov.", "de des.", ""
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, new string[] { "gen.", "febr.", "març", "abr.", "maig", "juny", "jul.", "ag.", "set.", "oct.", "nov.", "des.", "" } };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, new string[] { "led", "úno", "bře", "dub", "kvě", "čvn", "čvc", "srp", "zář", "říj", "lis", "pro", "" } };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, new string[] { "jan.", "feb.", "mar.", "apr.", "maj", "jun.", "jul.", "aug.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, new string[] { "Jän.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sep.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, new string[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, new string[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, new string[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, new string[] { "Jän.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sep.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, new string[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, new string[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" } };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, new string[] { "Ιαν", "Φεβ", "Μαρ", "Απρ", "Μαΐ", "Ιουν", "Ιουλ", "Αυγ", "Σεπ", "Οκτ", "Νοε", "Δεκ", "" } };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, new string[] { "Ιαν", "Φεβ", "Μαρ", "Απρ", "Μαΐ", "Ιουν", "Ιουλ", "Αυγ", "Σεπ", "Οκτ", "Νοε", "Δεκ", "" } };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "June", "July", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Jun", "Jul", .., "Sep"
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", PlatformDetection.IsNodeJS ? "Sept" : "Sep", "Oct", "Nov", "Dec", "" } }; // "Jan.", "Feb.", "Mar.", "Apr.", "May", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", ""
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" } };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec", "" } }; // "Sep"
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, new string[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic", "" } }; //  "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic.", ""
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, new string[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic", "" } }; //  "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic.", ""
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, new string[] { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic", "" } }; //  "ene.", "feb.", "mar.", "abr.", "may.", "jun.", "jul.", "ago.", "sep.", "oct.", "nov.", "dic.", ""
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, new string[] { "jaan", "veebr", "märts", "apr", "mai", "juuni", "juuli", "aug", "sept", "okt", "nov", "dets", "" } };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, new string[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" } };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, new string[] { "tammik.", "helmik.", "maalisk.", "huhtik.", "toukok.", "kesäk.", "heinäk.", "elok.", "syysk.", "lokak.", "marrask.", "jouluk.", "" } };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, new string[] { "Ene", "Peb", "Mar", "Abr", "May", "Hun", "Hul", "Ago", "Set", "Okt", "Nob", "Dis", "" } };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juill.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, new string[] { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc.", "" } };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, new string[] { "જાન્યુ", "ફેબ્રુ", "માર્ચ", "એપ્રિલ", "મે", "જૂન", "જુલાઈ", "ઑગસ્ટ", "સપ્ટે", "ઑક્ટો", "નવે", "ડિસે", "" } };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, new string[] { "ינו׳", "פבר׳", "מרץ", "אפר׳", "מאי", "יוני", "יולי", "אוג׳", "ספט׳", "אוק׳", "נוב׳", "דצמ׳", "" } };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, new string[] { "जन॰", "फ़र॰", "मार्च", "अप्रैल", "मई", "जून", "जुल॰", "अग॰", "सित॰", "अक्तू॰", "नव॰", "दिस॰", "" } };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, new string[] { "sij", "velj", "ožu", "tra", "svi", "lip", "srp", "kol", "ruj", "lis", "stu", "pro", "" } };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, new string[] { "sij", "velj", "ožu", "tra", "svi", "lip", "srp", "kol", "ruj", "lis", "stu", "pro", "" } };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, new string[] { "jan.", "febr.", "márc.", "ápr.", "máj.", "jún.", "júl.", "aug.", "szept.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, new string[] { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, new string[] { "gen", "feb", "mar", "apr", "mag", "giu", "lug", "ago", "set", "ott", "nov", "dic", "" } };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, new string[] { "gen", "feb", "mar", "apr", "mag", "giu", "lug", "ago", "set", "ott", "nov", "dic", "" } };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            string[] kannadianMonths = PlatformDetection.IsNodeJS ? // NodeJs responds like dotnet
                new string[] { "ಜನವರಿ", "ಫೆಬ್ರವರಿ", "ಮಾರ್ಚ್", "ಏಪ್ರಿ", "ಮೇ", "ಜೂನ್", "ಜುಲೈ", "ಆಗ", "ಸೆಪ್ಟೆಂ", "ಅಕ್ಟೋ", "ನವೆಂ", "ಡಿಸೆಂ", "" } :
                new string[] { "ಜನವರಿ", "ಫೆಬ್ರವರಿ", "ಮಾರ್ಚ್", "ಏಪ್ರಿ", "ಮೇ", "ಜೂನ್", "ಜುಲೈ", "ಆಗಸ್ಟ್", "ಸೆಪ್ಟೆಂ", "ಅಕ್ಟೋ", "ನವೆಂ", "ಡಿಸೆಂ", "" };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, kannadianMonths };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, new string[] { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월", "" } };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "" } }; // "saus.", "vas.", "kov.", "bal.", "geg.", "birž.", "liep.", "rugp.", "rugs.", "spal.", "lapkr.", "gruod."
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, new string[] { "janv.", "febr.", "marts", "apr.", "maijs", "jūn.", "jūl.", "aug.", "sept.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, new string[] { "ജനു", "ഫെബ്രു", "മാർ", "ഏപ്രി", "മേയ്", "ജൂൺ", "ജൂലൈ", "ഓഗ", "സെപ്റ്റം", "ഒക്ടോ", "നവം", "ഡിസം", "" } };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, new string[] { "जाने", "फेब्रु", "मार्च", "एप्रि", "मे", "जून", "जुलै", "ऑग", "सप्टें", "ऑक्टो", "नोव्हें", "डिसें", "" } };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ogo", "Sep", "Okt", "Nov", "Dis", "" } };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ogo", "Sep", "Okt", "Nov", "Dis", "" } };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ogo", "Sep", "Okt", "Nov", "Dis", "" } };
            string[] norwegianMonths = PlatformDetection.IsBrowserDomSupported ? // dotnet responds like non-browser
                new string [] { "jan.", "feb.", "mar.", "apr.", "mai", "jun.", "jul.", "aug.", "sep.", "okt.", "nov.", "des.", "" } :
                new string [] { "jan.", "feb.", "mars", "apr.", "mai", "juni", "juli", "aug.", "sep.", "okt.", "nov.", "des.", "" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, norwegianMonths };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, norwegianMonths };
            string[] dutchMonths = PlatformDetection.IsNodeJS ? // NodeJs responds like dotnet
                new string[] { "jan.", "feb.", "mrt.", "apr.", "mei", "jun.", "jul.", "aug.", "sep.", "okt.", "nov.", "dec.", "" } :
                new string[] { "jan", "feb", "mrt", "apr", "mei", "jun", "jul", "aug", "sep", "okt", "nov", "dec", "" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, dutchMonths };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, dutchMonths };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, dutchMonths };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, new string[] { "sty", "lut", "mar", "kwi", "maj", "cze", "lip", "sie", "wrz", "paź", "lis", "gru", "" } };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, new string[] { "jan.", "fev.", "mar.", "abr.", "mai.", "jun.", "jul.", "ago.", "set.", "out.", "nov.", "dez.", "" } };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, new string[] { "jan.", "fev.", "mar.", "abr.", "mai.", "jun.", "jul.", "ago.", "set.", "out.", "nov.", "dez.", "" } };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, new string[] { "ian.", "feb.", "mar.", "apr.", "mai", "iun.", "iul.", "aug.", "sept.", "oct.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, new string[] { "янв.", "февр.", "мар.", "апр.", "мая", "июн.", "июл.", "авг.", "сент.", "окт.", "нояб.", "дек.", "" } };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, new string[] { "jan", "feb", "mar", "apr", "máj", "jún", "júl", "aug", "sep", "okt", "nov", "dec", "" } };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, new string[] { "jan.", "feb.", "mar.", "apr.", "maj", "jun.", "jul.", "avg.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, new string[] { "јан", "феб", "мар", "апр", "мај", "јун", "јул", "авг", "сеп", "окт", "нов", "дец", "" } };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, new string[] { "jan", "feb", "mar", "apr", "maj", "jun", "jul", "avg", "sep", "okt", "nov", "dec", "" } };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, new string[] { "jan.", "feb.", "mars", "apr.", "maj", "juni", "juli", "aug.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, new string[] { "jan.", "feb.", "mars", "apr.", "maj", "juni", "juli", "aug.", "sep.", "okt.", "nov.", "dec.", "" } };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, new string[] { "Jan", "Feb", "Mac", "Apr", "Mei", "Jun", "Jul", "Ago", "Sep", "Okt", "Nov", "Des", "" } };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, new string[] { "ஜன.", "பிப்.", "மார்.", "ஏப்.", "மே", "ஜூன்", "ஜூலை", "ஆக.", "செப்.", "அக்.", "நவ.", "டிச.", "" } };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, new string[] { "జన", "ఫిబ్ర", "మార్చి", "ఏప్రి", "మే", "జూన్", "జులై", "ఆగ", "సెప్టెం", "అక్టో", "నవం", "డిసెం", "" } };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, new string[] { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค.", "" } };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, new string[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara", "" } };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, new string[] { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara", "" } };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, new string[] { "січ.", "лют.", "бер.", "квіт.", "трав.", "черв.", "лип.", "серп.", "вер.", "жовт.", "лист.", "груд.", "" } };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, new string[] { "Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12", "" } }; // thg
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(AbbreviatedMonthGenitiveNames_Get_TestData_HybridGlobalization))]
        public void AbbreviatedMonthGenitiveNames_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.AbbreviatedMonthGenitiveNames);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" }, DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthGenitiveNames);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.AbbreviatedMonthGenitiveNames, format.AbbreviatedMonthGenitiveNames);
            Assert.NotSame(format.AbbreviatedMonthGenitiveNames, format.AbbreviatedMonthGenitiveNames);
        }

        public static IEnumerable<object[]> AbbreviatedMonthGenitiveNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthGenitiveNames_Set_TestData))]
        public void AbbreviatedMonthGenitiveNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthGenitiveNames = value;
            Assert.Equal(value, format.AbbreviatedMonthGenitiveNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.AbbreviatedMonthGenitiveNames);
            Assert.Equal(value, format.AbbreviatedMonthGenitiveNames);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthGenitiveNames = null);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthGenitiveNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> AbbreviatedMonthGenitiveNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthGenitiveNames_SetInvalidLength_TestData))]
        public void AbbreviatedMonthGenitiveNames_SetNullValueInValues_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.AbbreviatedMonthGenitiveNames = value);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthGenitiveNames = new string[] { "GenJan", "GenFeb", "GenMar", "GenApr", "GenMay", "GenJun", "GenJul", "GenAug", "GenSep", "GenOct", "GenNov", "GenDec", "Gen" };

            var dateTime = new DateTime(1976, 6, 19);
            string formattedDate = dateTime.ToString("d MMM yy", format);
            Assert.Equal("19 GenJun 76", formattedDate);
            Assert.Equal(dateTime, DateTime.ParseExact(formattedDate, "d MMM yy", format));
            Assert.Equal(dateTime, DateTime.Parse(formattedDate, format));
        }

        [Fact]
        public void TestAbbreviatedGenitiveNamesWithAllCultures()
        {
            // WASM in Hybrid mode does not support NeutralCultures (only "no"), its support is limited to the list:
            // https://github.com/dotnet/icu/blob/dotnet/main/icu-filters/icudt_wasm.json
            CultureInfo[] cultures = PlatformDetection.IsHybridGlobalizationOnBrowser ?
                CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures) :
                CultureInfo.GetCultures(CultureTypes.AllCultures);
            DateTime dt = new DateTime(2000, 1, 20);

            foreach (CultureInfo ci in cultures)
            {
                string formattedDate = dt.ToString("d MMM yyyy", ci);

                for (int i = 0; i < 12; i++)
                {
                    if (!ci.DateTimeFormat.MonthNames[i].Equals(ci.DateTimeFormat.MonthGenitiveNames[i]) ||
                        !ci.DateTimeFormat.AbbreviatedMonthNames[i].Equals(ci.DateTimeFormat.AbbreviatedMonthGenitiveNames[i]))
                    {
                        // We have genitive month names, we expect parsing to work and produce the exact original result.
                        Assert.Equal(dt, DateTime.Parse(formattedDate, ci));
                        break;
                    }
                }

                // ParseExact should succeeded all the time even with non genitive cases .
                Assert.Equal(dt, DateTime.ParseExact(formattedDate, "d MMM yyyy", ci));
            }
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                AbbreviatedMonthGenitiveNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("dd MMM yy", format));
        }
    }
}
