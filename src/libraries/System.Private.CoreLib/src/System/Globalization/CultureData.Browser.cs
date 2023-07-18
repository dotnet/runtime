// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private const int CULTURE_INFO_BUFFER_LEN = 100;
        private int JsGetLocaleInfo(LocaleNumberData type) //should we incorporate this method into JSLoadCultureInfoFromBrowser?
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.IcuGetLocaleInfo(LocaleNumberData)] Expected _sWindowsName to be populated already");

            int result = Interop.JsGlobalization.GetLocaleInfoInt(_sWindowsName, (uint)type, out int exception, out object ex_result);
            if (exception != 0)
            {
                // Failed, just use 0
                Debug.Fail($"[CultureData.IcuGetLocaleInfo(LocaleNumberData)] failed with {ex_result}");
                result = 0;
            }

            return result;
        }

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
            if (subresults.Length < 2)
                throw new Exception("CultureInfo recieved from the Browser is in icorrect format.");
            culture._sAM1159 = subresults[0];
            culture._sPM2359 = subresults[1];
            return culture;
        }
    }
}
