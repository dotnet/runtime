// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
public class test
{
    public static String[] locStrings = {
                "ca-ES", // 0x00403;
                "cs-CZ", // 0x00405;
                "el-GR", // 0x00408;
                "fr-FR", // 0x0040c;
                "it-IT", // 0x00410;
                "pt-BR", // 0x00416;
                "sk-SK", // 0x0041b;
                "gl-ES", // 0x00456;
                "de-CH", // 0x00807;
                "es-MX", // 0x0080a;
                "fr-BE", // 0x0080c;
                "it-CH", // 0x00810;
                "pt-PT", // 0x00816;
                "de-AT", // 0x00c07;
                "fr-CA", // 0x00c0c;
                "de-LU", // 0x01007;
                "es-GT", // 0x0100a;
                "fr-CH", // 0x0100c;
                "de-LI", // 0x01407;
                "es-CR", // 0x0140a;
                "fr-LU", // 0x0140c;
                "es-PA", // 0x0180a;
                "fr-MC", // 0x0180c;
                "es-DO", // 0x01c0a;
                "es-VE", // 0x0200a;
                "es-CO", // 0x0240a;
                "es-PE", // 0x0280a;
                "es-AR", // 0x02c0a;
                "es-EC", // 0x0300a;
                "es-UY", // 0x0380a;
                "es-PY", // 0x03c0a;
                "es-BO", // 0x0400a;
                "es-SV", // 0x0440a;
                "es-HN", // 0x0480a;
                "es-NI", // 0x04c0a;
                "es-PR", // 0x0500a;
                "es-CL", // 13322;
                "ja-JP", // 0x00411;
                "ko-KR", // 0x00412;
                "ur-PK", // 0x00420;
                "fa-IR", // 0x00429;
                "vi-VN", // 0x0042a;
                "hy-AM", // 0x0042b;
                "ka-GE", // 0x00437;
                "hi-IN", // 0x00439;
                "pa-IN", // 0x00446;
                "gu-IN", // 0x00447;
                "ta-IN", // 0x00449;
                "te-IN", // 0x0044a;
                "kn-IN", // 0x0044b;
                "mr-IN", // 0x0044e;
                "sa-IN", // 0x0044f;
                "kok-IN", // 0x00457;
                "syr-SY", // 0x0045a;
                "ar-IQ", // 0x00801;
                "zh-CN", // 0x00804;
                "ar-EG", // 0x00c01;
                "zh-HK", // 0x00c04;
                "ar-LY", // 0x01001;
                "zh-SG", // 0x01004;
                "ar-DZ", // 0x01401;
                "zh-MO", // 0x01404;
                "ar-MA", // 0x01801;
                "ar-TN", // 0x01c01;
                "ar-OM", // 0x02001;
                "ar-YE", // 0x02401;
                "ar-SY", // 0x02801;
                "ar-JO", // 0x02c01;
                "ar-LB", // 0x03001;
                "ar-KW", // 0x03401;
                "ar-AE", // 0x03801;
                "ar-BH", // 0x03c01;
                "ar-QA", // 0x04001;
                "ka-GE_modern", // 0x10437;
                "zh-CN_stroke", // 0x20804;
                "zh-SG_stroke", // 0x21004;
                "zh-MO_stroke", // 0x21404;
                "zh-TW_pronun", // 0x30404;
                "he-IL", // 1037;
        };

    public static int Main()
    {
        int retVal = 100;
        if (!TestLibrary.Utilities.IsWindows)
        {
            TestLibrary.Logging.WriteLine("running tests on Mac");
            if (!RunTest())
            {
                retVal = 0;
            }

        }
        TestLibrary.Logging.WriteLine("returning " + retVal.ToString());
        return retVal;
    }

    public static bool RunTest()
    {
        bool retVal = true;
        for (int i = 0; i < locStrings.Length; i++)
        {
            retVal = retVal & CheckPattern(locStrings[i]);
        }
        return retVal;
    }
    public static bool CheckPattern(string locale)
    {
        string expectedMonthDayPattern = "MMMM dd";
        string expectedYearMonthPattern = "MMMM yyyy";
        bool retVal = false;
        try
        {
            DateTimeFormatInfo DTFI = new CultureInfo(locale).DateTimeFormat;
            if ((expectedMonthDayPattern.Equals(DTFI.MonthDayPattern)) &&
                (expectedYearMonthPattern.Equals(DTFI.YearMonthPattern)))
            {
                retVal = true;
            }
            else
            {
                TestLibrary.Logging.WriteLine("Error processing locale " + locale);
                TestLibrary.Logging.WriteLine("MonthDayPattern, expected= " + expectedMonthDayPattern +", actual="+DTFI.MonthDayPattern);
                TestLibrary.Logging.WriteLine("YearMonthPattern, expected= " + expectedYearMonthPattern + ", actual=" + DTFI.YearMonthPattern);
            }
            
        }
        catch (Exception ex)
        {
            TestLibrary.Logging.WriteLine("CheckPattern::Unexpected Exception Thrown processing locale "+locale+":" + ex);
            retVal = false;
        }
        return retVal;
    }
}