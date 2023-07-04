// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoNativeCalendarName
    {
        public static IEnumerable<object[]> NativeCalendarName_Get_TestData()
        {
            yield return new object[] { new CultureInfo("ar-SA").DateTimeFormat, "islamic-umalqura" }; // التقويم الإسلامي (أم القرى)
            yield return new object[] { new CultureInfo("am-ET").DateTimeFormat, "gregorian" }; // የግሪጎሪያን የቀን አቆጣጠር
            yield return new object[] { new CultureInfo("bg-BG").DateTimeFormat, "gregorian" }; // григориански календар
            yield return new object[] { new CultureInfo("bn-BD").DateTimeFormat, "gregorian" }; // গ্রিগোরিয়ান ক্যালেন্ডার
            yield return new object[] { new CultureInfo("bn-IN").DateTimeFormat, "gregorian" }; // গ্রিগোরিয়ান ক্যালেন্ডার
            yield return new object[] { new CultureInfo("ca-AD").DateTimeFormat, "gregorian" }; // calendari gregorià
            yield return new object[] { new CultureInfo("ca-ES").DateTimeFormat, "gregorian" }; // calendari gregorià
            yield return new object[] { new CultureInfo("cs-CZ").DateTimeFormat, "gregorian" }; // Gregoriánský kalendář
            yield return new object[] { new CultureInfo("da-DK").DateTimeFormat, "gregorian" }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("de-AT").DateTimeFormat, "gregorian" }; // Gregorianischer Kalender
            yield return new object[] { new CultureInfo("de-BE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("de-CH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("de-DE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("de-IT").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("de-LI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("de-LU").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("el-CY").DateTimeFormat, "gregorian" }; // Γρηγοριανό ημερολόγιο
            yield return new object[] { new CultureInfo("el-GR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-AE").DateTimeFormat, "gregorian" }; // Gregorian Calendar
            yield return new object[] { new CultureInfo("en-AG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-AI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-AS").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-AT").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-AU").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BB").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BS").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BW").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-BZ").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CA").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CC").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CX").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-CY").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-DE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-DK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-DM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-ER").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-FI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-FJ").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-FK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-FM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GB").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GD").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GU").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-GY").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-HK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-IE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-IL").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-IM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-IN").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-IO").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-JE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-JM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-KE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-KI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-KN").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-KY").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-LC").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-LR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-LS").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MO").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MP").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MS").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MT").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MU").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MW").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-MY").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NA").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NF").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NL").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NU").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-NZ").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-PG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-PH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-PK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-PN").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-PR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-PW").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-RW").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SB").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SC").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SD").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SL").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SS").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SX").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-SZ").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-TC").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-TK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-TO").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-TT").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-TV").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-TZ").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-UG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-UM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-VC").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-VG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-VI").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-VU").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-WS").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-ZA").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-ZM").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-ZW").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("es-419").DateTimeFormat, "gregorian" }; // calendario gregoriano
            yield return new object[] { new CultureInfo("es-ES").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("es-MX").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("et-EE").DateTimeFormat, "gregorian" }; // Gregoriuse kalender
            yield return new object[] { new CultureInfo("fa-IR").DateTimeFormat, "persian" }; // تقویم هجری شمسی
            yield return new object[] { new CultureInfo("fi-FI").DateTimeFormat, "gregorian" }; // gregoriaaninen kalenteri
            yield return new object[] { new CultureInfo("fil-PH").DateTimeFormat, "gregorian" }; // Gregorian na Kalendaryo
            yield return new object[] { new CultureInfo("fr-BE").DateTimeFormat, "gregorian" }; // calendrier grégorien
            yield return new object[] { new CultureInfo("fr-CA").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("fr-CH").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("gu-IN").DateTimeFormat, "gregorian" }; // ગ્રેગોરિઅન કેલેન્ડર
            yield return new object[] { new CultureInfo("he-IL").DateTimeFormat, "gregorian" }; // לוח השנה הגרגוריאני
            yield return new object[] { new CultureInfo("hi-IN").DateTimeFormat, "gregorian" }; // ग्रेगोरियन कैलेंडर"
            yield return new object[] { new CultureInfo("hr-BA").DateTimeFormat, "gregorian" }; // gregorijanski kalendar
            yield return new object[] { new CultureInfo("hr-HR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("hu-HU").DateTimeFormat, "gregorian" }; // Gergely-naptár
            yield return new object[] { new CultureInfo("id-ID").DateTimeFormat, "gregorian" }; // Kalender Gregorian"
            yield return new object[] { new CultureInfo("it-CH").DateTimeFormat, "gregorian" }; // Calendario gregoriano
            yield return new object[] { new CultureInfo("it-IT").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("ja-JP").DateTimeFormat, "gregorian" }; // 西暦(グレゴリオ暦)
            yield return new object[] { new CultureInfo("kn-IN").DateTimeFormat, "gregorian" }; // ಗ್ರೆಗೋರಿಯನ್ ಕ್ಯಾಲೆಂಡರ್
            yield return new object[] { new CultureInfo("ko-KR").DateTimeFormat, "gregorian" }; // 양력
            yield return new object[] { new CultureInfo("lt-LT").DateTimeFormat, "gregorian" }; // Grigaliaus kalendorius
            yield return new object[] { new CultureInfo("lv-LV").DateTimeFormat, "gregorian" }; // Gregora kalendārs
            yield return new object[] { new CultureInfo("ml-IN").DateTimeFormat, "gregorian" }; // ഇംഗ്ലീഷ് കലണ്ടർ
            yield return new object[] { new CultureInfo("mr-IN").DateTimeFormat, "gregorian" }; // ग्रेगोरियन दिनदर्शिका
            yield return new object[] { new CultureInfo("ms-BN").DateTimeFormat, "gregorian" }; // Kalendar Gregory
            yield return new object[] { new CultureInfo("ms-MY").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("ms-SG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("nb-NO").DateTimeFormat, "gregorian" }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("no").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("no-NO").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("nl-AW").DateTimeFormat, "gregorian" }; // Gregoriaanse kalender
            yield return new object[] { new CultureInfo("nl-BE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("nl-NL").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("pl-PL").DateTimeFormat, "gregorian" }; // kalendarz gregoriański
            yield return new object[] { new CultureInfo("pt-BR").DateTimeFormat, "gregorian" }; // Calendário Gregoriano
            yield return new object[] { new CultureInfo("pt-PT").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("ro-RO").DateTimeFormat, "gregorian" }; // calendar gregorian
            yield return new object[] { new CultureInfo("ru-RU").DateTimeFormat, "gregorian" }; // григорианский календарь
            yield return new object[] { new CultureInfo("sk-SK").DateTimeFormat, "gregorian" }; // gregoriánsky kalendár
            yield return new object[] { new CultureInfo("sl-SI").DateTimeFormat, "gregorian" }; // gregorijanski koledar
            yield return new object[] { new CultureInfo("sr-Cyrl-RS").DateTimeFormat, "gregorian" }; // грегоријански календар
            yield return new object[] { new CultureInfo("sr-Latn-RS").DateTimeFormat, "gregorian" }; // gregorijanski kalendar
            yield return new object[] { new CultureInfo("sv-AX").DateTimeFormat, "gregorian" }; // gregoriansk kalender
            yield return new object[] { new CultureInfo("sv-SE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("sw-CD").DateTimeFormat, "gregorian" }; // Kalenda ya Kigregori
            yield return new object[] { new CultureInfo("sw-KE").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("sw-TZ").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("sw-UG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("ta-IN").DateTimeFormat, "gregorian" }; // கிரிகோரியன் நாள்காட்டி
            yield return new object[] { new CultureInfo("ta-LK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("ta-MY").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("ta-SG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("te-IN").DateTimeFormat, "gregorian" };// గ్రేగోరియన్ క్యాలెండర్
            yield return new object[] { new CultureInfo("th-TH").DateTimeFormat, "buddhist" }; // ปฏิทินพุทธ
            yield return new object[] { new CultureInfo("tr-CY").DateTimeFormat, "gregorian" }; // Miladi Takvim
            yield return new object[] { new CultureInfo("tr-TR").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("uk-UA").DateTimeFormat, "gregorian" }; // григоріанський календар
            yield return new object[] { new CultureInfo("vi-VN").DateTimeFormat, "gregorian" }; // Lịch Gregory
            yield return new object[] { new CultureInfo("zh-CN").DateTimeFormat, "gregorian" }; // 公历
            yield return new object[] { new CultureInfo("zh-Hans-HK").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("zh-SG").DateTimeFormat, "gregorian" };
            yield return new object[] { new CultureInfo("zh-HK").DateTimeFormat, "gregorian" }; // 公曆
            yield return new object[] { new CultureInfo("zh-TW").DateTimeFormat, "gregorian" };
        }
        
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(NativeCalendarName_Get_TestData))]
        public void NativeCalendarName_Get_GetReturnsExpected(DateTimeFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.NativeCalendarName);
        }
    }
}
