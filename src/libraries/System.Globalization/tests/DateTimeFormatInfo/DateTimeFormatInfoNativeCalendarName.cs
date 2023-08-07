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
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "islamic-umalqura" }; // التقويم الإسلامي (أم القرى)
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "gregory" }; // የግሪጎሪያን የቀን አቆጣጠር
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "gregory" }; // григориански календар
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "gregory" }; // গ্রিগোরিয়ান ক্যালেন্ডার
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "gregory" }; // গ্রিগোরিয়ান ক্যালেন্ডার
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "gregory" }; // calendari gregorià
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "gregory" }; // calendari gregorià
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "gregory" }; // Gregoriánský kalendář
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "gregory" }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "gregory" }; // Gregorianischer Kalender
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "gregory" }; // Γρηγοριανό ημερολόγιο
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "gregory" }; // Gregorian Calendar
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "gregory" }; // calendario gregoriano
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "gregory" }; // Gregoriuse kalender
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "persian" }; // تقویم هجری شمسی
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "gregory" }; // gregoriaaninen kalenteri
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "gregory" }; // Gregorian na Kalendaryo
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "gregory" }; // calendrier grégorien
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "gregory" }; // ગ્રેગોરિઅન કેલેન્ડર
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "gregory" }; // לוח השנה הגרגוריאני
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "gregory" }; // ग्रेगोरियन कैलेंडर"
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "gregory" }; // gregorijanski kalendar
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "gregory" }; // Gergely-naptár
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "gregory" }; // Kalender Gregorian"
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "gregory" }; // Calendario gregoriano
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "gregory" }; // 西暦(グレゴリオ暦)
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "gregory" }; // ಗ್ರೆಗೋರಿಯನ್ ಕ್ಯಾಲೆಂಡರ್
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "gregory" }; // 양력
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "gregory" }; // Grigaliaus kalendorius
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "gregory" }; // Gregora kalendārs
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "gregory" }; // ഇംഗ്ലീഷ് കലണ്ടർ
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "gregory" }; // ग्रेगोरियन दिनदर्शिका
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "gregory" }; // Kalendar Gregory
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "gregory" }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "gregory" }; // Gregoriaanse kalender
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "gregory" }; // kalendarz gregoriański
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "gregory" }; // Calendário Gregoriano
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "gregory" }; // calendar gregory
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "gregory" }; // григорианский календарь
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "gregory" }; // gregoriánsky kalendár
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "gregory" }; // gregorijanski koledar
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "gregory" }; // грегоријански календар
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "gregory" }; // gregorijanski kalendar
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "gregory" }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "gregory" }; // Kalenda ya Kigregori
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "gregory" }; // கிரிகோரியன் நாள்காட்டி
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "gregory" };// గ్రేగోరియన్ క్యాలెండర్
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "buddhist" }; // ปฏิทินพุทธ
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "gregory" }; // Miladi Takvim
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "gregory" }; // григоріанський календар
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "gregory" }; // Lịch Gregory
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "gregory" }; // 公历
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "gregory" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "gregory" }; // 公曆
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "gregory" };
        }
        
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(NativeCalendarName_Get_TestData_HybridGlobalization))]
        public void NativeCalendarName_Get_ReturnsExpected_HybridGlobalization(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.NativeCalendarName);
        }
    }
}
