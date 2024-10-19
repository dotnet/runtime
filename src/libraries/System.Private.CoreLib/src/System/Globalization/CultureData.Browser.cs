// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private const int CULTURE_INFO_BUFFER_LEN = 60;
        private const int LOCALE_INFO_BUFFER_LEN = 80;

        private void JSInitLocaleInfo()
        {
            string? localeName = _sName;
            if (string.IsNullOrEmpty(localeName))
            {
                _sEnglishLanguage = "Invariant Language";
                _sNativeLanguage = _sEnglishLanguage;
                _sEnglishCountry = "Invariant Country";
                _sNativeCountry = _sEnglishCountry;
                _sEnglishDisplayName = $"{_sEnglishLanguage} ({_sEnglishCountry})";
                _sNativeDisplayName = _sEnglishDisplayName;
            }
            else
            {
                // English locale info
                (_sEnglishLanguage, _sEnglishCountry) = JSGetLocaleInfo("en-US", localeName);
                _sEnglishDisplayName = string.IsNullOrEmpty(_sEnglishCountry) ?
                    _sEnglishLanguage :
                    $"{_sEnglishLanguage} ({_sEnglishCountry})";
                // Native locale info
                (_sNativeLanguage, _sNativeCountry) = JSGetLocaleInfo(localeName, localeName);
                _sNativeDisplayName = string.IsNullOrEmpty(_sNativeCountry) ?
                    _sNativeLanguage :
                    $"{_sNativeLanguage} ({_sNativeCountry})";
            }
        }

        private unsafe (string, string) JSGetLocaleInfo(string cultureName, string localeName)
        {
            ReadOnlySpan<char> cultureNameSpan = cultureName.AsSpan();
            ReadOnlySpan<char> localeNameSpan = localeName.AsSpan();
            fixed (char* pCultureName = &MemoryMarshal.GetReference(cultureNameSpan))
            fixed (char* pLocaleName = &MemoryMarshal.GetReference(localeNameSpan))
            {
                char* buffer = stackalloc char[LOCALE_INFO_BUFFER_LEN];
                nint exceptionPtr = Interop.JsGlobalization.GetLocaleInfo(pCultureName, cultureNameSpan.Length, pLocaleName, localeNameSpan.Length, buffer, LOCALE_INFO_BUFFER_LEN, out int resultLength);
                Helper.MarshalAndThrowIfException(exceptionPtr);
                string result = new string(buffer, 0, resultLength);
                string[] subresults = result.Split("##");
                if (subresults.Length == 0)
                    throw new Exception("LocaleInfo recieved from the Browser is in incorrect format.");
                if (subresults.Length == 1)
                    return (subresults[0], ""); // Neutral culture
                return (subresults[0], subresults[1]);
            }
        }

        private string JSGetNativeDisplayName(string localeName, string cultureName)
        {
            (string languageName, string countryName) = JSGetLocaleInfo(localeName, cultureName);
            return string.IsNullOrEmpty(countryName) ?
                    languageName :
                    $"{languageName} ({countryName})";
        }

        private static unsafe CultureData JSLoadCultureInfoFromBrowser(string localeName, CultureData culture)
        {
            ReadOnlySpan<char> localeNameSpan = localeName.AsSpan();
            fixed (char* pLocaleName = &MemoryMarshal.GetReference(localeNameSpan))
            {
                char* buffer = stackalloc char[CULTURE_INFO_BUFFER_LEN];
                nint exceptionPtr = Interop.JsGlobalization.GetCultureInfo(pLocaleName, localeNameSpan.Length, buffer, CULTURE_INFO_BUFFER_LEN, out int resultLength);
                Helper.MarshalAndThrowIfException(exceptionPtr);
                string result = new string(buffer, 0, resultLength);
                string[] subresults = result.Split("##");
                if (subresults.Length < 4)
                    throw new Exception("CultureInfo recieved from the Browser is in incorrect format.");
                culture._sAM1159 = subresults[0];
                culture._sPM2359 = subresults[1];
                culture._saLongTimes = new string[] { subresults[2] };
                culture._saShortTimes = new string[] { subresults[3] };
            }
            return culture;
        }

        private static unsafe int GetFirstDayOfWeek(string localeName)
        {
            ReadOnlySpan<char> localeNameSpan = localeName.AsSpan();
            fixed (char* pLocaleName = &MemoryMarshal.GetReference(localeNameSpan))
            {
                nint exceptionPtr = Interop.JsGlobalization.GetFirstDayOfWeek(pLocaleName, localeNameSpan.Length, out int result);
                if (exceptionPtr != IntPtr.Zero)
                {
                    int success = Helper.MarshalAndThrowIfException(
                        exceptionPtr,
                        failOnlyDebug: true,
                        failureMessage: $"[CultureData.GetFirstDayOfWeek()] failed with");
                    // Failed, just use 0
                    if (success == -1)
                        return 0;
                }
                return result;
            }
        }

        private static unsafe int GetFirstWeekOfYear(string localeName)
        {
            ReadOnlySpan<char> localeNameSpan = localeName.AsSpan();
            fixed (char* pLocaleName = &MemoryMarshal.GetReference(localeNameSpan))
            {
                nint exceptionPtr = Interop.JsGlobalization.GetFirstWeekOfYear(pLocaleName, localeNameSpan.Length, out int result);
                if (exceptionPtr != IntPtr.Zero)
                {
                    int success = Helper.MarshalAndThrowIfException(
                        exceptionPtr,
                        failOnlyDebug: true,
                        failureMessage: $"[CultureData.GetFirstWeekOfYear()] failed with");
                    // Failed, just use 0
                    if (success == -1)
                        return 0;
                }
                return result;
            }
        }
    }

    internal static class Helper
    {
        internal static int MarshalAndThrowIfException(nint exceptionPtr, bool failOnlyDebug = false, string failureMessage = "")
        {
            if (exceptionPtr != IntPtr.Zero)
            {
                string message = Marshal.PtrToStringUni(exceptionPtr)!;
                Marshal.FreeHGlobal(exceptionPtr);
                if (failOnlyDebug)
                {
                    Debug.Fail($"{failureMessage} {message}");
                    return -1;
                }
                throw new Exception(message);
            }
            return 0;
        }

    }
}
