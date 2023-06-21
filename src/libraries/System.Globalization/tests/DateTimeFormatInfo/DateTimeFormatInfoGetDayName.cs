// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetDayName
    {
        [Fact]
        public static IEnumerable<object[]> GetDayName_TestData()
        {
            string[] englishDayNames = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, englishDayNames };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, englishDayNames };
            yield return new object[] { new DateTimeFormatInfo(), englishDayNames };

            if (!PlatformDetection.IsUbuntu)
            {
                yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, DateTimeFormatInfoData.FrFRDayNames() };
            }
            if (PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { CultureInfo.GetCultureInfo("ar-SA").DateTimeFormat, new string[] { "\u0627\u0644\u0623\u062d\u062f", "\u0627\u0644\u0627\u062b\u0646\u064a\u0646", "\u0627\u0644\u062b\u0644\u0627\u062b\u0627\u0621", "\u0627\u0644\u0623\u0631\u0628\u0639\u0627\u0621", "\u0627\u0644\u062e\u0645\u064a\u0633", "\u0627\u0644\u062c\u0645\u0639\u0629", "\u0627\u0644\u0633\u0628\u062a" }};
                yield return new object[] { CultureInfo.GetCultureInfo("am-ET").DateTimeFormat, new string[] { "\u12a5\u1211\u12f5", "\u1230\u129e", "\u121b\u12ad\u1230\u129e", "\u1228\u1261\u12d5", "\u1210\u1219\u1235", "\u12d3\u122d\u1265", "\u1245\u12f3\u121c" }};
                yield return new object[] { CultureInfo.GetCultureInfo("bg-BG").DateTimeFormat, new string[] { "\u043d\u0435\u0434\u0435\u043b\u044f", "\u043f\u043e\u043d\u0435\u0434\u0435\u043b\u043d\u0438\u043a", "\u0432\u0442\u043e\u0440\u043d\u0438\u043a", "\u0441\u0440\u044f\u0434\u0430", "\u0447\u0435\u0442\u0432\u044a\u0440\u0442\u044a\u043a", "\u043f\u0435\u0442\u044a\u043a", "\u0441\u044a\u0431\u043e\u0442\u0430" }};
                yield return new object[] { CultureInfo.GetCultureInfo("bn-BD").DateTimeFormat, new string[] { "\u09b0\u09ac\u09bf\u09ac\u09be\u09b0", "\u09b8\u09cb\u09ae\u09ac\u09be\u09b0", "\u09ae\u0999\u09cd\u0997\u09b2\u09ac\u09be\u09b0", "\u09ac\u09c1\u09a7\u09ac\u09be\u09b0", "\u09ac\u09c3\u09b9\u09b8\u09cd\u09aa\u09a4\u09bf\u09ac\u09be\u09b0", "\u09b6\u09c1\u0995\u09cd\u09b0\u09ac\u09be\u09b0", "\u09b6\u09a8\u09bf\u09ac\u09be\u09b0" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ca-AD").DateTimeFormat, new string[] { "diumenge", "dilluns", "dimarts", "dimecres", "dijous", "divendres", "dissabte" }};
                yield return new object[] { CultureInfo.GetCultureInfo("cs-CZ").DateTimeFormat, new string[] { "ned\u011ble", "pond\u011bl\u00ed", "\u00fater\u00fd", "st\u0159eda", "\u010dtvrtek", "p\u00e1tek", "sobota" }};
                yield return new object[] { CultureInfo.GetCultureInfo("da-DK").DateTimeFormat, new string[] { "s\u00f8ndag", "mandag", "tirsdag", "onsdag", "torsdag", "fredag", "l\u00f8rdag" }};
                yield return new object[] { CultureInfo.GetCultureInfo("de-AT").DateTimeFormat, new string[] { "Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag" }};
                yield return new object[] { CultureInfo.GetCultureInfo("el-CY").DateTimeFormat, new string[] { "\u039a\u03c5\u03c1\u03b9\u03b1\u03ba\u03ae", "\u0394\u03b5\u03c5\u03c4\u03ad\u03c1\u03b1", "\u03a4\u03c1\u03af\u03c4\u03b7", "\u03a4\u03b5\u03c4\u03ac\u03c1\u03c4\u03b7", "\u03a0\u03ad\u03bc\u03c0\u03c4\u03b7", "\u03a0\u03b1\u03c1\u03b1\u03c3\u03ba\u03b5\u03c5\u03ae", "\u03a3\u03ac\u03b2\u03b2\u03b1\u03c4\u03bf" }};
                yield return new object[] { CultureInfo.GetCultureInfo("es-MX").DateTimeFormat, new string[] { "domingo", "lunes", "martes", "mi\u00e9rcoles", "jueves", "viernes", "s\u00e1bado" }};
                yield return new object[] { CultureInfo.GetCultureInfo("et-EE").DateTimeFormat, new string[] { "p\u00fchap\u00e4ev", "esmasp\u00e4ev", "teisip\u00e4ev", "kolmap\u00e4ev", "neljap\u00e4ev", "reede", "laup\u00e4ev" }};
                yield return new object[] { CultureInfo.GetCultureInfo("fa-IR").DateTimeFormat, new string[] { "\u06cc\u06a9\u0634\u0646\u0628\u0647", "\u062f\u0648\u0634\u0646\u0628\u0647", "\u0633\u0647\u200c\u0634\u0646\u0628\u0647", "\u0686\u0647\u0627\u0631\u0634\u0646\u0628\u0647", "\u067e\u0646\u062c\u0634\u0646\u0628\u0647", "\u062c\u0645\u0639\u0647", "\u0634\u0646\u0628\u0647" }};
                yield return new object[] { CultureInfo.GetCultureInfo("fi-FI").DateTimeFormat, new string[] { "sunnuntai", "maanantai", "tiistai", "keskiviikko", "torstai", "perjantai", "lauantai" }};
                yield return new object[] { CultureInfo.GetCultureInfo("fil-PH").DateTimeFormat, new string[] { "Linggo", "Lunes", "Martes", "Miyerkules", "Huwebes", "Biyernes", "Sabado" }};
                yield return new object[] { CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat, new string[] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" }};
                yield return new object[] { CultureInfo.GetCultureInfo("gu-IN").DateTimeFormat, new string[] { "\u0ab0\u0ab5\u0abf\u0ab5\u0abe\u0ab0", "\u0ab8\u0acb\u0aae\u0ab5\u0abe\u0ab0", "\u0aae\u0a82\u0a97\u0ab3\u0ab5\u0abe\u0ab0", "\u0aac\u0ac1\u0aa7\u0ab5\u0abe\u0ab0", "\u0a97\u0ac1\u0ab0\u0ac1\u0ab5\u0abe\u0ab0", "\u0ab6\u0ac1\u0a95\u0acd\u0ab0\u0ab5\u0abe\u0ab0", "\u0ab6\u0aa8\u0abf\u0ab5\u0abe\u0ab0" }};
                yield return new object[] { CultureInfo.GetCultureInfo("he-IL").DateTimeFormat, new string[] { "\u05d9\u05d5\u05dd \u05e8\u05d0\u05e9\u05d5\u05df", "\u05d9\u05d5\u05dd \u05e9\u05e0\u05d9", "\u05d9\u05d5\u05dd \u05e9\u05dc\u05d9\u05e9\u05d9", "\u05d9\u05d5\u05dd \u05e8\u05d1\u05d9\u05e2\u05d9", "\u05d9\u05d5\u05dd \u05d7\u05de\u05d9\u05e9\u05d9", "\u05d9\u05d5\u05dd \u05e9\u05d9\u05e9\u05d9", "\u05d9\u05d5\u05dd \u05e9\u05d1\u05ea" }};
                yield return new object[] { CultureInfo.GetCultureInfo("hi-IN").DateTimeFormat, new string[] { "\u0930\u0935\u093f\u0935\u093e\u0930", "\u0938\u094b\u092e\u0935\u093e\u0930", "\u092e\u0902\u0917\u0932\u0935\u093e\u0930", "\u092c\u0941\u0927\u0935\u093e\u0930", "\u0917\u0941\u0930\u0941\u0935\u093e\u0930", "\u0936\u0941\u0915\u094d\u0930\u0935\u093e\u0930", "\u0936\u0928\u093f\u0935\u093e\u0930" }};
                yield return new object[] { CultureInfo.GetCultureInfo("hr-BA").DateTimeFormat, new string[] { "nedjelja", "ponedjeljak", "utorak", "srijeda", "\u010detvrtak", "petak", "subota" }};
                yield return new object[] { CultureInfo.GetCultureInfo("hu-HU").DateTimeFormat, new string[] { "vas\u00e1rnap", "h\u00e9tf\u0151", "kedd", "szerda", "cs\u00fct\u00f6rt\u00f6k", "p\u00e9ntek", "szombat" }};
                yield return new object[] { CultureInfo.GetCultureInfo("id-ID").DateTimeFormat, new string[] { "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu" }};
                yield return new object[] { CultureInfo.GetCultureInfo("it-IT").DateTimeFormat, new string[] { "domenica", "luned\u00ec", "marted\u00ec", "mercoled\u00ec", "gioved\u00ec", "venerd\u00ec", "sabato" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ja-JP").DateTimeFormat, new string[] { "\u65e5\u66dc\u65e5", "\u6708\u66dc\u65e5", "\u706b\u66dc\u65e5", "\u6c34\u66dc\u65e5", "\u6728\u66dc\u65e5", "\u91d1\u66dc\u65e5", "\u571f\u66dc\u65e5" }};
                yield return new object[] { CultureInfo.GetCultureInfo("kn-IN").DateTimeFormat, new string[] { "\u0cad\u0cbe\u0ca8\u0cc1\u0cb5\u0cbe\u0cb0", "\u0cb8\u0ccb\u0cae\u0cb5\u0cbe\u0cb0", "\u0cae\u0c82\u0c97\u0cb3\u0cb5\u0cbe\u0cb0", "\u0cac\u0cc1\u0ca7\u0cb5\u0cbe\u0cb0", "\u0c97\u0cc1\u0cb0\u0cc1\u0cb5\u0cbe\u0cb0", "\u0cb6\u0cc1\u0c95\u0ccd\u0cb0\u0cb5\u0cbe\u0cb0", "\u0cb6\u0ca8\u0cbf\u0cb5\u0cbe\u0cb0" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ko-KR").DateTimeFormat, new string[] { "\uc77c\uc694\uc77c", "\uc6d4\uc694\uc77c", "\ud654\uc694\uc77c", "\uc218\uc694\uc77c", "\ubaa9\uc694\uc77c", "\uae08\uc694\uc77c", "\ud1a0\uc694\uc77c" }};
                yield return new object[] { CultureInfo.GetCultureInfo("lt-LT").DateTimeFormat, new string[] { "sekmadienis", "pirmadienis", "antradienis", "tre\u010diadienis", "ketvirtadienis", "penktadienis", "\u0161e\u0161tadienis" }};
                yield return new object[] { CultureInfo.GetCultureInfo("lv-LV").DateTimeFormat, new string[] { "Sv\u0113tdiena", "Pirmdiena", "Otrdiena", "Tre\u0161diena", "Ceturtdiena", "Piektdiena", "Sestdiena" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ml-IN").DateTimeFormat, new string[] { "\u0d1e\u0d3e\u0d2f\u0d31\u0d3e\u0d34\u0d4d\u200c\u0d1a", "\u0d24\u0d3f\u0d19\u0d4d\u0d15\u0d33\u0d3e\u0d34\u0d4d\u200c\u0d1a", "\u0d1a\u0d4a\u0d35\u0d4d\u0d35\u0d3e\u0d34\u0d4d\u200c\u0d1a", "\u0d2c\u0d41\u0d27\u0d28\u0d3e\u0d34\u0d4d\u200c\u0d1a", "\u0d35\u0d4d\u0d2f\u0d3e\u0d34\u0d3e\u0d34\u0d4d\u200c\u0d1a", "\u0d35\u0d46\u0d33\u0d4d\u0d33\u0d3f\u0d2f\u0d3e\u0d34\u0d4d\u200c\u0d1a", "\u0d36\u0d28\u0d3f\u0d2f\u0d3e\u0d34\u0d4d\u200c\u0d1a" }};
                yield return new object[] { CultureInfo.GetCultureInfo("mr-IN").DateTimeFormat, new string[] { "\u0930\u0935\u093f\u0935\u093e\u0930", "\u0938\u094b\u092e\u0935\u093e\u0930", "\u092e\u0902\u0917\u0933\u0935\u093e\u0930", "\u092c\u0941\u0927\u0935\u093e\u0930", "\u0917\u0941\u0930\u0941\u0935\u093e\u0930", "\u0936\u0941\u0915\u094d\u0930\u0935\u093e\u0930", "\u0936\u0928\u093f\u0935\u093e\u0930" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ms-SG").DateTimeFormat, new string[] { "Ahad", "Isnin", "Selasa", "Rabu", "Khamis", "Jumaat", "Sabtu" }};
                yield return new object[] { CultureInfo.GetCultureInfo("nb-NO").DateTimeFormat, new string[] { "s\u00f8ndag", "mandag", "tirsdag", "onsdag", "torsdag", "fredag", "l\u00f8rdag" }};
                yield return new object[] { CultureInfo.GetCultureInfo("nl-NL").DateTimeFormat, new string[] { "zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag" }};
                yield return new object[] { CultureInfo.GetCultureInfo("pl-PL").DateTimeFormat, new string[] { "niedziela", "poniedzia\u0142ek", "wtorek", "\u015broda", "czwartek", "pi\u0105tek", "sobota" }};
                yield return new object[] { CultureInfo.GetCultureInfo("pt-PT").DateTimeFormat, new string[] { "domingo", "segunda-feira", "ter\u00e7a-feira", "quarta-feira", "quinta-feira", "sexta-feira", "s\u00e1bado" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ro-RO").DateTimeFormat, new string[] { "duminic\u0103", "luni", "mar\u021bi", "miercuri", "joi", "vineri", "s\u00e2mb\u0103t\u0103" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat, new string[] { "\u0432\u043e\u0441\u043a\u0440\u0435\u0441\u0435\u043d\u044c\u0435", "\u043f\u043e\u043d\u0435\u0434\u0435\u043b\u044c\u043d\u0438\u043a", "\u0432\u0442\u043e\u0440\u043d\u0438\u043a", "\u0441\u0440\u0435\u0434\u0430", "\u0447\u0435\u0442\u0432\u0435\u0440\u0433", "\u043f\u044f\u0442\u043d\u0438\u0446\u0430", "\u0441\u0443\u0431\u0431\u043e\u0442\u0430"  }};
                yield return new object[] { CultureInfo.GetCultureInfo("sk-SK").DateTimeFormat, new string[] { "nede\u013ea", "pondelok", "utorok", "streda", "\u0161tvrtok", "piatok", "sobota" }};
                yield return new object[] { CultureInfo.GetCultureInfo("sr-Cyrl-RS").DateTimeFormat, new string[] { "\u043d\u0435\u0434\u0435\u0459\u0430", "\u043f\u043e\u043d\u0435\u0434\u0435\u0459\u0430\u043a", "\u0443\u0442\u043e\u0440\u0430\u043a", "\u0441\u0440\u0435\u0434\u0430", "\u0447\u0435\u0442\u0432\u0440\u0442\u0430\u043a", "\u043f\u0435\u0442\u0430\u043a", "\u0441\u0443\u0431\u043e\u0442\u0430" }};
                yield return new object[] { CultureInfo.GetCultureInfo("sr-Latn-RS").DateTimeFormat, new string[] { "nedelja", "ponedeljak", "utorak", "sreda", "\u010detvrtak", "petak", "subota" }};
                yield return new object[] { CultureInfo.GetCultureInfo("sv-AX").DateTimeFormat, new string[] { "s\u00f6ndag", "m\u00e5ndag", "tisdag", "onsdag", "torsdag", "fredag", "l\u00f6rdag" }};
                yield return new object[] { CultureInfo.GetCultureInfo("sw-UG").DateTimeFormat, new string[] { "Jumapili", "Jumatatu", "Jumanne", "Jumatano", "Alhamisi", "Ijumaa", "Jumamosi" }};
                yield return new object[] { CultureInfo.GetCultureInfo("ta-SG").DateTimeFormat, new string[] { "\u0b9e\u0bbe\u0baf\u0bbf\u0bb1\u0bc1", "\u0ba4\u0bbf\u0b99\u0bcd\u0b95\u0bb3\u0bcd", "\u0b9a\u0bc6\u0bb5\u0bcd\u0bb5\u0bbe\u0baf\u0bcd", "\u0baa\u0bc1\u0ba4\u0ba9\u0bcd", "\u0bb5\u0bbf\u0baf\u0bbe\u0bb4\u0ba9\u0bcd", "\u0bb5\u0bc6\u0bb3\u0bcd\u0bb3\u0bbf", "\u0b9a\u0ba9\u0bbf" }};
                yield return new object[] { CultureInfo.GetCultureInfo("te-IN").DateTimeFormat, new string[] { "\u0c06\u0c26\u0c3f\u0c35\u0c3e\u0c30\u0c02", "\u0c38\u0c4b\u0c2e\u0c35\u0c3e\u0c30\u0c02", "\u0c2e\u0c02\u0c17\u0c33\u0c35\u0c3e\u0c30\u0c02", "\u0c2c\u0c41\u0c27\u0c35\u0c3e\u0c30\u0c02", "\u0c17\u0c41\u0c30\u0c41\u0c35\u0c3e\u0c30\u0c02", "\u0c36\u0c41\u0c15\u0c4d\u0c30\u0c35\u0c3e\u0c30\u0c02", "\u0c36\u0c28\u0c3f\u0c35\u0c3e\u0c30\u0c02" }};
                yield return new object[] { CultureInfo.GetCultureInfo("th-TH").DateTimeFormat, new string[] { "\u0e27\u0e31\u0e19\u0e2d\u0e32\u0e17\u0e34\u0e15\u0e22\u0e4c", "\u0e27\u0e31\u0e19\u0e08\u0e31\u0e19\u0e17\u0e23\u0e4c", "\u0e27\u0e31\u0e19\u0e2d\u0e31\u0e07\u0e04\u0e32\u0e23", "\u0e27\u0e31\u0e19\u0e1e\u0e38\u0e18", "\u0e27\u0e31\u0e19\u0e1e\u0e24\u0e2b\u0e31\u0e2a\u0e1a\u0e14\u0e35", "\u0e27\u0e31\u0e19\u0e28\u0e38\u0e01\u0e23\u0e4c", "\u0e27\u0e31\u0e19\u0e40\u0e2a\u0e32\u0e23\u0e4c" }};
                yield return new object[] { CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat, new string[] { "Pazar", "Pazartesi", "Sal\u0131", "\u00c7ar\u015famba", "Per\u015fembe", "Cuma", "Cumartesi" }};
                yield return new object[] { CultureInfo.GetCultureInfo("uk-UA").DateTimeFormat, new string[] { "\u043d\u0435\u0434\u0456\u043b\u044f", "\u043f\u043e\u043d\u0435\u0434\u0456\u043b\u043e\u043a", "\u0432\u0456\u0432\u0442\u043e\u0440\u043e\u043a", "\u0441\u0435\u0440\u0435\u0434\u0430", "\u0447\u0435\u0442\u0432\u0435\u0440", "\u043f\u02bc\u044f\u0442\u043d\u0438\u0446\u044f", "\u0441\u0443\u0431\u043e\u0442\u0430" }};
                yield return new object[] { CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat, new string[] { "Ch\u1ee7 Nh\u1eadt", "Th\u1ee9 Hai", "Th\u1ee9 Ba", "Th\u1ee9 T\u01b0", "Th\u1ee9 N\u0103m", "Th\u1ee9 S\u00e1u", "Th\u1ee9 B\u1ea3y" }};
                yield return new object[] { CultureInfo.GetCultureInfo("zh-CN").DateTimeFormat, new string[] { "\u661f\u671f\u65e5", "\u661f\u671f\u4e00", "\u661f\u671f\u4e8c", "\u661f\u671f\u4e09", "\u661f\u671f\u56db", "\u661f\u671f\u4e94", "\u661f\u671f\u516d" }};
            }
        }

        [Theory]
        [MemberData(nameof(GetDayName_TestData))]
        public void GetDayName_Invoke_ReturnsExpected(DateTimeFormatInfo format, string[] expected)
        {
            DayOfWeek[] values = new DayOfWeek[]
            {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            for (int i = 0; i < values.Length; ++i)
            {
                Assert.Equal(expected[i], format.GetDayName(values[i]));
            }
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday - 1)]
        [InlineData(DayOfWeek.Saturday + 1)]
        public void GetDayName_InvalidDayOfWeek_ThrowsArgumentOutOfRangeException(DayOfWeek dayofweek)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dayofweek", () => format.GetDayName(dayofweek));
        }
    }
}
