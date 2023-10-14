// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthNames
    {
        [Fact]
        public void MonthNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" }, DateTimeFormatInfo.InvariantInfo.MonthNames);
        }

        [Fact]
        public void MonthNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.MonthNames, format.MonthNames);
            Assert.NotSame(format.MonthNames, format.MonthNames);
        }

        public static IEnumerable<object[]> MonthNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        public static IEnumerable<object[]> MonthNames_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, if it differs
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, new string[] { "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة", "" } };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, new string[] { "ጃንዩወሪ", "ፌብሩወሪ", "ማርች", "ኤፕሪል", "ሜይ", "ጁን", "ጁላይ", "ኦገስት", "ሴፕቴምበር", "ኦክቶበር", "ኖቬምበር", "ዲሴምበር", "" } };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, new string[] { "януари", "февруари", "март", "април", "май", "юни", "юли", "август", "септември", "октомври", "ноември", "декември", "" } };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, new string[] { "জানুয়ারী", "ফেব্রুয়ারী", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", "" } };
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, new string[] { "জানুয়ারী", "ফেব্রুয়ারী", "মার্চ", "এপ্রিল", "মে", "জুন", "জুলাই", "আগস্ট", "সেপ্টেম্বর", "অক্টোবর", "নভেম্বর", "ডিসেম্বর", "" } };
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, new string[] { "gener", "febrer", "març", "abril", "maig", "juny", "juliol", "agost", "setembre", "octubre", "novembre", "desembre", "" } };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, new string[] { "gener", "febrer", "març", "abril", "maig", "juny", "juliol", "agost", "setembre", "octubre", "novembre", "desembre", "" } };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, new string[] { "leden", "únor", "březen", "duben", "květen", "červen", "červenec", "srpen", "září", "říjen", "listopad", "prosinec", "" } };
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, new string[] { "januar", "februar", "marts", "april", "maj", "juni", "juli", "august", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, new string[] { "Jänner", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, new string[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, new string[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, new string[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, new string[] { "Jänner", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, new string[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, new string[] { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", "" } };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, new string[] { "Ιανουαρίου", "Φεβρουαρίου", "Μαρτίου", "Απριλίου", "Μαΐου", "Ιουνίου", "Ιουλίου", "Αυγούστου", "Σεπτεμβρίου", "Οκτωβρίου", "Νοεμβρίου", "Δεκεμβρίου", "" } };  // BUG!!! JS returns Genitive for Greek even though we expect Nominative; "Ιανουάριος", "Φεβρουάριος", "Μάρτιος", "Απρίλιος", "Μάιος", "Ιούνιος", "Ιούλιος", "Αύγουστος", "Σεπτέμβριος", "Οκτώβριος", "Νοέμβριος", "Δεκέμβριος"
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, new string[] { "Ιανουαρίου", "Φεβρουαρίου", "Μαρτίου", "Απριλίου", "Μαΐου", "Ιουνίου", "Ιουλίου", "Αυγούστου", "Σεπτεμβρίου", "Οκτωβρίου", "Νοεμβρίου", "Δεκεμβρίου", "" } }; // BUG!!! JS returns Genitive for Greek even though we expect Nominative; "Ιανουάριος", "Φεβρουάριος", "Μάρτιος", "Απρίλιος", "Μάιος", "Ιούνιος", "Ιούλιος", "Αύγουστος", "Σεπτέμβριος", "Οκτώβριος", "Νοέμβριος", "Δεκέμβριος"
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, new string[] { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre", "" } };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, new string[] { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre", "" } };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, new string[] { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre", "" } };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, new string[] { "jaanuar", "veebruar", "märts", "aprill", "mai", "juuni", "juuli", "august", "september", "oktoober", "november", "detsember", "" } };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, new string[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" } };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, new string[] { "tammikuu", "helmikuu", "maaliskuu", "huhtikuu", "toukokuu", "kesäkuu", "heinäkuu", "elokuu", "syyskuu", "lokakuu", "marraskuu", "joulukuu", "" } };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, new string[] { "Enero", "Pebrero", "Marso", "Abril", "Mayo", "Hunyo", "Hulyo", "Agosto", "Setyembre", "Oktubre", "Nobyembre", "Disyembre", "" } };
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, new string[] { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre", "" } };
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, new string[] { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre", "" } };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, new string[] { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre", "" } };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, new string[] { "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre", "" } };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, new string[] { "જાન્યુઆરી", "ફેબ્રુઆરી", "માર્ચ", "એપ્રિલ", "મે", "જૂન", "જુલાઈ", "ઑગસ્ટ", "સપ્ટેમ્બર", "ઑક્ટોબર", "નવેમ્બર", "ડિસેમ્બર", "" } };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, new string[] { "ינואר", "פברואר", "מרץ", "אפריל", "מאי", "יוני", "יולי", "אוגוסט", "ספטמבר", "אוקטובר", "נובמבר", "דצמבר", "" } };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, new string[] { "जनवरी", "फ़रवरी", "मार्च", "अप्रैल", "मई", "जून", "जुलाई", "अगस्त", "सितंबर", "अक्तूबर", "नवंबर", "दिसंबर", "" } };
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, new string[] { "siječanj", "veljača", "ožujak", "travanj", "svibanj", "lipanj", "srpanj", "kolovoz", "rujan", "listopad", "studeni", "prosinac", "" } };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, new string[] { "siječanj", "veljača", "ožujak", "travanj", "svibanj", "lipanj", "srpanj", "kolovoz", "rujan", "listopad", "studeni", "prosinac", "" } };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, new string[] { "január", "február", "március", "április", "május", "június", "július", "augusztus", "szeptember", "október", "november", "december", "" } };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, new string[] { "Januari", "Februari", "Maret", "April", "Mei", "Juni", "Juli", "Agustus", "September", "Oktober", "November", "Desember", "" } };
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, new string[] { "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno", "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre", "" } };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, new string[] { "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno", "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre", "" } };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, new string[] { "ಜನವರಿ", "ಫೆಬ್ರವರಿ", "ಮಾರ್ಚ್", "ಏಪ್ರಿಲ್", "ಮೇ", "ಜೂನ್", "ಜುಲೈ", "ಆಗಸ್ಟ್", "ಸೆಪ್ಟೆಂಬರ್", "ಅಕ್ಟೋಬರ್", "ನವೆಂಬರ್", "ಡಿಸೆಂಬರ್", "" } };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, new string[] { "1월", "2월", "3월", "4월", "5월", "6월", "7월", "8월", "9월", "10월", "11월", "12월", "" } };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, new string[] { "sausis", "vasaris", "kovas", "balandis", "gegužė", "birželis", "liepa", "rugpjūtis", "rugsėjis", "spalis", "lapkritis", "gruodis", "" } };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, new string[] { "janvāris", "februāris", "marts", "aprīlis", "maijs", "jūnijs", "jūlijs", "augusts", "septembris", "oktobris", "novembris", "decembris", "" } };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, new string[] { "ജനുവരി", "ഫെബ്രുവരി", "മാർച്ച്", "ഏപ്രിൽ", "മേയ്", "ജൂൺ", "ജൂലൈ", "ഓഗസ്റ്റ്", "സെപ്റ്റംബർ", "ഒക്‌ടോബർ", "നവംബർ", "ഡിസംബർ", "" } };
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, new string[] { "जानेवारी", "फेब्रुवारी", "मार्च", "एप्रिल", "मे", "जून", "जुलै", "ऑगस्ट", "सप्टेंबर", "ऑक्टोबर", "नोव्हेंबर", "डिसेंबर", "" } };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, new string[] { "Januari", "Februari", "Mac", "April", "Mei", "Jun", "Julai", "Ogos", "September", "Oktober", "November", "Disember", "" } };
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, new string[] { "Januari", "Februari", "Mac", "April", "Mei", "Jun", "Julai", "Ogos", "September", "Oktober", "November", "Disember", "" } };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, new string[] { "Januari", "Februari", "Mac", "April", "Mei", "Jun", "Julai", "Ogos", "September", "Oktober", "November", "Disember", "" } };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, new string[] { "januar", "februar", "mars", "april", "mai", "juni", "juli", "august", "september", "oktober", "november", "desember", "" } };
            yield return new object[] { new CultureInfo("no").DateTimeFormat, new string[] { "januar", "februar", "mars", "april", "mai", "juni", "juli", "august", "september", "oktober", "november", "desember", "" } };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, new string[] { "januar", "februar", "mars", "april", "mai", "juni", "juli", "august", "september", "oktober", "november", "desember", "" } };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, new string[] { "januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, new string[] { "januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, new string[] { "januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, new string[] { "styczeń", "luty", "marzec", "kwiecień", "maj", "czerwiec", "lipiec", "sierpień", "wrzesień", "październik", "listopad", "grudzień", "" } };
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, new string[] { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro", "" } };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, new string[] { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro", "" } };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, new string[] { "ianuarie", "februarie", "martie", "aprilie", "mai", "iunie", "iulie", "august", "septembrie", "octombrie", "noiembrie", "decembrie", "" } };
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, new string[] { "январь", "февраль", "март", "апрель", "май", "июнь", "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь", "" } };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, new string[] { "január", "február", "marec", "apríl", "máj", "jún", "júl", "august", "september", "október", "november", "december", "" } };
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, new string[] { "januar", "februar", "marec", "april", "maj", "junij", "julij", "avgust", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, new string[] { "јануар", "фебруар", "март", "април", "мај", "јун", "јул", "август", "септембар", "октобар", "новембар", "децембар", "" } };
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, new string[] { "januar", "februar", "mart", "april", "maj", "jun", "jul", "avgust", "septembar", "oktobar", "novembar", "decembar", "" } };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, new string[] { "januari", "februari", "mars", "april", "maj", "juni", "juli", "augusti", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, new string[] { "januari", "februari", "mars", "april", "maj", "juni", "juli", "augusti", "september", "oktober", "november", "december", "" } };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, new string[] { "Januari", "Februari", "Machi", "Aprili", "Mei", "Juni", "Julai", "Agosti", "Septemba", "Oktoba", "Novemba", "Desemba", "" } };
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, new string[] { "Januari", "Februari", "Machi", "Aprili", "Mei", "Juni", "Julai", "Agosti", "Septemba", "Oktoba", "Novemba", "Desemba", "" } };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, new string[] { "Januari", "Februari", "Machi", "Aprili", "Mei", "Juni", "Julai", "Agosti", "Septemba", "Oktoba", "Novemba", "Desemba", "" } };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, new string[] { "Januari", "Februari", "Machi", "Aprili", "Mei", "Juni", "Julai", "Agosti", "Septemba", "Oktoba", "Novemba", "Desemba", "" } };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, new string[] { "ஜனவரி", "பிப்ரவரி", "மார்ச்", "ஏப்ரல்", "மே", "ஜூன்", "ஜூலை", "ஆகஸ்ட்", "செப்டம்பர்", "அக்டோபர்", "நவம்பர்", "டிசம்பர்", "" } };
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, new string[] { "ஜனவரி", "பிப்ரவரி", "மார்ச்", "ஏப்ரல்", "மே", "ஜூன்", "ஜூலை", "ஆகஸ்ட்", "செப்டம்பர்", "அக்டோபர்", "நவம்பர்", "டிசம்பர்", "" } };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, new string[] { "ஜனவரி", "பிப்ரவரி", "மார்ச்", "ஏப்ரல்", "மே", "ஜூன்", "ஜூலை", "ஆகஸ்ட்", "செப்டம்பர்", "அக்டோபர்", "நவம்பர்", "டிசம்பர்", "" } };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, new string[] { "ஜனவரி", "பிப்ரவரி", "மார்ச்", "ஏப்ரல்", "மே", "ஜூன்", "ஜூலை", "ஆகஸ்ட்", "செப்டம்பர்", "அக்டோபர்", "நவம்பர்", "டிசம்பர்", "" } };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, new string[] { "జనవరి", "ఫిబ్రవరి", "మార్చి", "ఏప్రిల్", "మే", "జూన్", "జులై", "ఆగస్టు", "సెప్టెంబర్", "అక్టోబర్", "నవంబర్", "డిసెంబర్", "" } };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, new string[] { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม", "" } };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, new string[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık", "" } };
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, new string[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık", "" } };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, new string[] { "січень", "лютий", "березень", "квітень", "травень", "червень", "липень", "серпень", "вересень", "жовтень", "листопад", "грудень", "" } };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12", "" } };
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, new string[] { "一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月", "" } };
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, new string[] { "一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月", "" } };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } }; // "一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月", ""
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(MonthNames_Get_TestData_HybridGlobalization))]
        public void MonthNames_Get_ReturnsExpected(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.MonthNames);
        }

        [Theory]
        [MemberData(nameof(MonthNames_Set_TestData))]
        public void MonthNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.MonthNames = value;
            Assert.Equal(value, format.MonthNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.MonthNames);
            Assert.Equal(value, format.MonthNames);
        }

        [Fact]
        public void MonthNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthNames = null);
        }

        [Fact]
        public void MonthNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> MonthNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(MonthNames_SetInvalidLength_TestData))]
        public void MonthNames_SetNullValueInValues_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.MonthNames = value);
        }

        [Fact]
        public void MonthNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        [Fact]
        public void MonthNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.MonthNames = new string[] { "Jan.", "Feb.", "Mar.", "Apr.", "May.", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", "." };
            Assert.Equal("Jun. 76", new DateTime(1976, 6, 19).ToString("MMMM yy", format));
        }

        [Fact]
        public void MonthNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                MonthNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("MMMM", format));
        }
    }
}
