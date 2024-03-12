// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoNativeCalendarName
    {
        public static IEnumerable<object[]> NativeCalendarName_Get_TestData_HybridGlobalization()
        {
            // see the comments on the right to check the non-Hybrid result, in this collection it always differs
            string islamicName = "islamic-umalqura";
            string gregorianName = "gregory";
            string persianName = "persian";
            string bhuddistName = "buddhist";
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, islamicName }; // التقويم الإسلامي (أم القرى)
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, gregorianName }; // የግሪጎሪያን የቀን አቆጣጠር
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, gregorianName }; // григориански календар
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, gregorianName }; // গ্রিগোরিয়ান ক্যালেন্ডার
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, gregorianName }; // গ্রিগোরিয়ান ক্যালেন্ডার
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, gregorianName }; // calendari gregorià
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, gregorianName }; // calendari gregorià
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, gregorianName }; // Gregoriánský kalendář
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, gregorianName }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, gregorianName }; // Gregorianischer Kalender
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, gregorianName }; // Γρηγοριανό ημερολόγιο
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, gregorianName }; // Gregorian Calendar
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, gregorianName }; // calendario gregoriano
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, gregorianName }; // Gregoriuse kalender
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, persianName }; // تقویم هجری شمسی
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, gregorianName }; // gregoriaaninen kalenteri
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, gregorianName }; // Gregorian na Kalendaryo
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, gregorianName }; // calendrier grégorien
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, gregorianName }; // ગ્રેગોરિઅન કેલેન્ડર
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, gregorianName }; // לוח השנה הגרגוריאני
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, gregorianName }; // ग्रेगोरियन कैलेंडर"
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, gregorianName }; // gregorijanski kalendar
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, gregorianName }; // Gergely-naptár
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, gregorianName }; // Kalender Gregorian"
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, gregorianName }; // Calendario gregoriano
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, gregorianName }; // 西暦(グレゴリオ暦)
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, gregorianName }; // ಗ್ರೆಗೋರಿಯನ್ ಕ್ಯಾಲೆಂಡರ್
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, gregorianName }; // 양력
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, gregorianName }; // Grigaliaus kalendorius
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, gregorianName }; // Gregora kalendārs
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, gregorianName }; // ഇംഗ്ലീഷ് കലണ്ടർ
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, gregorianName }; // ग्रेगोरियन दिनदर्शिका
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, gregorianName }; // Kalendar Gregory
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, gregorianName }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("no").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, gregorianName }; // Gregoriaanse kalender
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, gregorianName }; // kalendarz gregoriański
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, gregorianName }; // Calendário Gregoriano
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, gregorianName }; // calendar gregory
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, gregorianName }; // григорианский календарь
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, gregorianName }; // gregoriánsky kalendár
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, gregorianName }; // gregorijanski koledar
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, gregorianName }; // грегоријански календар
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, gregorianName }; // gregorijanski kalendar
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, gregorianName }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, gregorianName }; // Kalenda ya Kigregori
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, gregorianName }; // கிரிகோரியன் நாள்காட்டி
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, gregorianName };// గ్రేగోరియన్ క్యాలెండర్
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, bhuddistName }; // ปฏิทินพุทธ
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, gregorianName }; // Miladi Takvim
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, gregorianName }; // григоріанський календар
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, gregorianName }; // Lịch Gregory
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, gregorianName }; // 公历
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, gregorianName };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, gregorianName }; // 公曆
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, gregorianName };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(NativeCalendarName_Get_TestData_HybridGlobalization))]
        public void NativeCalendarName_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.NativeCalendarName);
        }
    }
}
