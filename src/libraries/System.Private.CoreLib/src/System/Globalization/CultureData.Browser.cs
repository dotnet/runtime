// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private const int CULTURE_INFO_BUFFER_LEN = 50;

        private static unsafe CultureData JSLoadCultureInfoFromBrowser(string localeName, CultureData culture)
        {
            char* buffer = stackalloc char[CULTURE_INFO_BUFFER_LEN];
            int exception;
            object exResult;
            int resultLength = Interop.JsGlobalization.GetCultureInfo(localeName, buffer, CULTURE_INFO_BUFFER_LEN, out exception, out exResult);
            if (exception != 0)
                throw new Exception((string)exResult);
            string result = new string(buffer, 0, resultLength);
            string[] subresults = result.Split("##");
            if (subresults.Length < 4)
                throw new Exception("CultureInfo recieved from the Browser is in incorrect format.");
            culture._sAM1159 = subresults[0];
            culture._sPM2359 = subresults[1];
            culture._saLongTimes = new string[] { subresults[2] };
            culture._saShortTimes = new string[] { subresults[3] };
            return culture;
        }

        private static unsafe int GetFirstDayOfWeek(string localeName)
        {
            int result = Interop.JsGlobalization.GetFirstDayOfWeek(localeName, out int exception, out object ex_result);
            if (exception != 0)
            {
                // Failed, just use 0
                Debug.Fail($"[CultureData.GetFirstDayOfWeek()] failed with {ex_result}");
                return 0;
            }
            return result;
        }

        private static unsafe int GetFirstWeekOfYear(string localeName)
        {
            int result = Interop.JsGlobalization.GetFirstWeekOfYear(localeName, out int exception, out object ex_result);
            if (exception != 0)
            {
                // Failed, just use 0
                Debug.Fail($"[CultureData.GetFirstDayOfWeek()] failed with {ex_result}");
                return 0;
            }
            return result;
        }
    }
}
