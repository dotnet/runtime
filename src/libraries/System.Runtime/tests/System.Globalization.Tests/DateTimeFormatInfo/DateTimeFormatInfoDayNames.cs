// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoDayNames
    {
        [Fact]
        public void DayNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" }, DateTimeFormatInfo.InvariantInfo.DayNames);
        }

        [Fact]
        public void DayNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.DayNames, format.DayNames);
            Assert.NotSame(format.DayNames, format.DayNames);
        }

        public static IEnumerable<object[]> DayNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "" } };
        }

        public static IEnumerable<object[]> DayNames_Get_TestData_HybridGlobalization()
        {
            yield return new object[] { "ar-SA", new string[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" } };
            yield return new object[] { "am-ET", new string[] { "እሑድ", "ሰኞ", "ማክሰኞ", "ረቡዕ", "ሐሙስ", "ዓርብ", "ቅዳሜ" } };
            yield return new object[] { "bg-BG", new string[] { "неделя", "понеделник", "вторник", "сряда", "четвъртък", "петък", "събота" } };
            yield return new object[] { "bn-BD", new string[] { "রবিবার", "সোমবার", "মঙ্গলবার", "বুধবার", "বৃহস্পতিবার", "শুক্রবার", "শনিবার" } };
            yield return new object[] { "ca-ES", new string[] { "diumenge", "dilluns", "dimarts", "dimecres", "dijous", "divendres", "dissabte" } };
            yield return new object[] { "cs-CZ", new string[] { "neděle", "pondělí", "úterý", "středa", "čtvrtek", "pátek", "sobota" } };
            yield return new object[] { "de-AT", new string[] { "Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag" } };
            yield return new object[] { "el-GR", new string[] { "Κυριακή", "Δευτέρα", "Τρίτη", "Τετάρτη", "Πέμπτη", "Παρασκευή", "Σάββατο" } };
            yield return new object[] { "en-US", new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" } };
            yield return new object[] { "es-419", new string[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" } };
            yield return new object[] { "es-ES", new string[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" } };
            yield return new object[] { "es-MX", new string[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" } };
            yield return new object[] { "et-EE", new string[] { "pühapäev", "esmaspäev", "teisipäev", "kolmapäev", "neljapäev", "reede", "laupäev" } };
            yield return new object[] { "fa-IR", new string[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" } };
            yield return new object[] { "fi-FI", new string[] { "sunnuntai", "maanantai", "tiistai", "keskiviikko", "torstai", "perjantai", "lauantai" } };
            yield return new object[] { "fil-PH", new string[] { "Linggo", "Lunes", "Martes", "Miyerkules", "Huwebes", "Biyernes", "Sabado" } };
            yield return new object[] { "fr-FR", new string[] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" } };
            yield return new object[] { "gu-IN", new string[] { "રવિવાર", "સોમવાર", "મંગળવાર", "બુધવાર", "ગુરુવાર", "શુક્રવાર", "શનિવાર" } };
            yield return new object[] { "he-IL", new string[] { "יום ראשון", "יום שני", "יום שלישי", "יום רביעי", "יום חמישי", "יום שישי", "יום שבת" } };
            yield return new object[] { "hi-IN", new string[] { "रविवार", "सोमवार", "मंगलवार", "बुधवार", "गुरुवार", "शुक्रवार", "शनिवार" } };
            yield return new object[] { "hr-HR", new string[] { "nedjelja", "ponedjeljak", "utorak", "srijeda", "četvrtak", "petak", "subota" } };
            yield return new object[] { "hu-HU", new string[] { "vasárnap", "hétfő", "kedd", "szerda", "csütörtök", "péntek", "szombat" } };
            yield return new object[] { "id-ID", new string[] { "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu" } };
            yield return new object[] { "it-IT", new string[] { "domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato" } };
            yield return new object[] { "ja-JP", new string[] { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" } };
            yield return new object[] { "kn-IN", new string[] { "ಭಾನುವಾರ", "ಸೋಮವಾರ", "ಮಂಗಳವಾರ", "ಬುಧವಾರ", "ಗುರುವಾರ", "ಶುಕ್ರವಾರ", "ಶನಿವಾರ" } };
            yield return new object[] { "ko-KR", new string[] { "일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일" } };
            yield return new object[] { "lt-LT", new string[] { "sekmadienis", "pirmadienis", "antradienis", "trečiadienis", "ketvirtadienis", "penktadienis", "šeštadienis" } };
            yield return new object[] { "lv-LV", new string[] { "Svētdiena", "Pirmdiena", "Otrdiena", "Trešdiena", "Ceturtdiena", "Piektdiena", "Sestdiena" } };
            yield return new object[] { "ml-IN", new string[] { "ഞായറാഴ്‌ച", "തിങ്കളാഴ്‌ച", "ചൊവ്വാഴ്‌ച", "ബുധനാഴ്‌ച", "വ്യാഴാഴ്‌ച", "വെള്ളിയാഴ്‌ച", "ശനിയാഴ്‌ച" } };
            yield return new object[] { "ms-BN", new string[] { "Ahad", "Isnin", "Selasa", "Rabu", "Khamis", "Jumaat", "Sabtu" } };
            yield return new object[] { "no-NO", new string[] { "søndag", "mandag", "tirsdag", "onsdag", "torsdag", "fredag", "lørdag" } };
            yield return new object[] { "nl-AW", new string[] { "zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag" } };
            yield return new object[] { "pl-PL", new string[] { "niedziela", "poniedziałek", "wtorek", "środa", "czwartek", "piątek", "sobota" } };
            yield return new object[] { "pt-PT", new string[] { "domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado" } };
            yield return new object[] { "ro-RO", new string[] { "duminică", "luni", "marți", "miercuri", "joi", "vineri", "sâmbătă" } };
            yield return new object[] { "sk-SK", new string[] { "nedeľa", "pondelok", "utorok", "streda", "štvrtok", "piatok", "sobota" } };
            yield return new object[] { "sv-AX", new string[] { "söndag", "måndag", "tisdag", "onsdag", "torsdag", "fredag", "lördag" } };
            yield return new object[] { "sw-CD", new string[] { "Jumapili", "Jumatatu", "Jumanne", "Jumatano", "Alhamisi", "Ijumaa", "Jumamosi" } };
            yield return new object[] { "ta-IN", new string[] { "ஞாயிறு", "திங்கள்", "செவ்வாய்", "புதன்", "வியாழன்", "வெள்ளி", "சனி" } };
            yield return new object[] { "te-IN", new string[] { "ఆదివారం", "సోమవారం", "మంగళవారం", "బుధవారం", "గురువారం", "శుక్రవారం", "శనివారం" } };
            yield return new object[] { "th-TH", new string[] { "วันอาทิตย์", "วันจันทร์", "วันอังคาร", "วันพุธ", "วันพฤหัสบดี", "วันศุกร์", "วันเสาร์" } };
            yield return new object[] { "tr-CY", new string[] { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" } };
            yield return new object[] { "uk-UA", new string[] { "неділя", "понеділок", "вівторок", "середа", "четвер", "пʼятниця", "субота" } };
            yield return new object[] { "vi-VN", new string[] { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" } };
            yield return new object[] { "zh-TW", new string[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(DayNames_Get_TestData_HybridGlobalization))]
        public void DayNames_Get_ReturnsExpected_HybridGlobalization(string cultureName, string[] expected)
        {
            var format = new CultureInfo(cultureName).DateTimeFormat;
            int length = format.DayNames.Length;
            Assert.True(length == expected.Length, $"Length comparison failed for culture: {cultureName}. Expected: {expected.Length}, Actual: {length}");
            for (int i = 0; i<length; i++)
                Assert.True(expected[i] == format.DayNames[i], $"Failed for culture: {cultureName} on index: {i}. Expected: {expected[i]}, Actual: {format.DayNames[i]}");
        }

        [Theory]
        [MemberData(nameof(DayNames_Set_TestData))]
        public void DayNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.DayNames = value;
            Assert.Equal(value, format.DayNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.DayNames);
            Assert.Equal(value, format.DayNames);
        }

        [Fact]
        public void DayNames_SetNulValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.DayNames = null);
        }

        [Fact]
        public void DayNames_SetNulValueInValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.DayNames = new string[] { "1", "2", "3", null, "5", "6", "7" });
        }

        public static IEnumerable<object[]> DayNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Sun" } };
            yield return new object[] { new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(DayNames_SetInvalidLength_TestData))]
        public void DayNames_SetInvalidLength_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", (() => format.DayNames = value));
        }

        [Fact]
        public void DayNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.DayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }

        [Fact]
        public void DayNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7" };
            var format = new DateTimeFormatInfo
            {
                DayNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 5, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("dddd MMM yy", format));
        }
    }
}
