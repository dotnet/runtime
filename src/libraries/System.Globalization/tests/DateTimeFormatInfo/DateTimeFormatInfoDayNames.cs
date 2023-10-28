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
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, new string[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" } };
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, new string[] { "እሑድ", "ሰኞ", "ማክሰኞ", "ረቡዕ", "ሐሙስ", "ዓርብ", "ቅዳሜ" } };
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, new string[] { "неделя", "понеделник", "вторник", "сряда", "четвъртък", "петък", "събота" } };
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, new string[] { "রবিবার", "সোমবার", "মঙ্গলবার", "বুধবার", "বৃহস্পতিবার", "শুক্রবার", "শনিবার" } };
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, new string[] { "diumenge", "dilluns", "dimarts", "dimecres", "dijous", "divendres", "dissabte" } };
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, new string[] { "neděle", "pondělí", "úterý", "středa", "čtvrtek", "pátek", "sobota" } };
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, new string[] { "Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag" } };
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, new string[] { "Κυριακή", "Δευτέρα", "Τρίτη", "Τετάρτη", "Πέμπτη", "Παρασκευή", "Σάββατο" } };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" } };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, new string[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" } };
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, new string[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" } };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, new string[] { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" } };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, new string[] { "pühapäev", "esmaspäev", "teisipäev", "kolmapäev", "neljapäev", "reede", "laupäev" } };
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, new string[] { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" } };
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, new string[] { "sunnuntai", "maanantai", "tiistai", "keskiviikko", "torstai", "perjantai", "lauantai" } };
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, new string[] { "Linggo", "Lunes", "Martes", "Miyerkules", "Huwebes", "Biyernes", "Sabado" } };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, new string[] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" } };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, new string[] { "રવિવાર", "સોમવાર", "મંગળવાર", "બુધવાર", "ગુરુવાર", "શુક્રવાર", "શનિવાર" } };
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, new string[] { "יום ראשון", "יום שני", "יום שלישי", "יום רביעי", "יום חמישי", "יום שישי", "יום שבת" } };
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, new string[] { "रविवार", "सोमवार", "मंगलवार", "बुधवार", "गुरुवार", "शुक्रवार", "शनिवार" } };
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, new string[] { "nedjelja", "ponedjeljak", "utorak", "srijeda", "četvrtak", "petak", "subota" } };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, new string[] { "vasárnap", "hétfő", "kedd", "szerda", "csütörtök", "péntek", "szombat" } };
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, new string[] { "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu" } };
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, new string[] { "domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato" } };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, new string[] { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" } };
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, new string[] { "ಭಾನುವಾರ", "ಸೋಮವಾರ", "ಮಂಗಳವಾರ", "ಬುಧವಾರ", "ಗುರುವಾರ", "ಶುಕ್ರವಾರ", "ಶನಿವಾರ" } };
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, new string[] { "일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일" } };
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, new string[] { "sekmadienis", "pirmadienis", "antradienis", "trečiadienis", "ketvirtadienis", "penktadienis", "šeštadienis" } };
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, new string[] { "Svētdiena", "Pirmdiena", "Otrdiena", "Trešdiena", "Ceturtdiena", "Piektdiena", "Sestdiena" } };
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, new string[] { "ഞായറാഴ്‌ച", "തിങ്കളാഴ്‌ച", "ചൊവ്വാഴ്‌ച", "ബുധനാഴ്‌ച", "വ്യാഴാഴ്‌ച", "വെള്ളിയാഴ്‌ച", "ശനിയാഴ്‌ച" } };
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, new string[] { "Ahad", "Isnin", "Selasa", "Rabu", "Khamis", "Jumaat", "Sabtu" } };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, new string[] { "søndag", "mandag", "tirsdag", "onsdag", "torsdag", "fredag", "lørdag" } };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, new string[] { "zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag" } };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, new string[] { "niedziela", "poniedziałek", "wtorek", "środa", "czwartek", "piątek", "sobota" } };
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, new string[] { "domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado" } };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, new string[] { "duminică", "luni", "marți", "miercuri", "joi", "vineri", "sâmbătă" } };
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, new string[] { "nedeľa", "pondelok", "utorok", "streda", "štvrtok", "piatok", "sobota" } };
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, new string[] { "söndag", "måndag", "tisdag", "onsdag", "torsdag", "fredag", "lördag" } };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, new string[] { "Jumapili", "Jumatatu", "Jumanne", "Jumatano", "Alhamisi", "Ijumaa", "Jumamosi" } };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, new string[] { "ஞாயிறு", "திங்கள்", "செவ்வாய்", "புதன்", "வியாழன்", "வெள்ளி", "சனி" } };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, new string[] { "ఆదివారం", "సోమవారం", "మంగళవారం", "బుధవారం", "గురువారం", "శుక్రవారం", "శనివారం" } };
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, new string[] { "วันอาทิตย์", "วันจันทร์", "วันอังคาร", "วันพุธ", "วันพฤหัสบดี", "วันศุกร์", "วันเสาร์" } };
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, new string[] { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" } };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, new string[] { "неділя", "понеділок", "вівторок", "середа", "четвер", "пʼятниця", "субота" } };
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, new string[] { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" } };
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, new string[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(DayNames_Get_TestData_HybridGlobalization))]
        public void DayNames_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.DayNames);
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
