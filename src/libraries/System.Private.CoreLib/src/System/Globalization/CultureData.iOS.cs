// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private bool InitNativeCultureDataCore()
        {
            Debug.Assert(_sRealName != null);
            Debug.Assert(!GlobalizationMode.Invariant);
            string realNameBuffer = _sRealName;

            _sWindowsName = GetLocaleNameNative(realNameBuffer);
            return true;
        }

        internal static unsafe string GetLocaleNameNative(string localeName)
        {
            return Interop.Globalization.GetLocaleNameNative(localeName);
        }

        private string GetLocaleInfoNative(LocaleStringData type)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfoNative] Expected _sWindowsName to be populated already");

            return GetLocaleInfoNative(_sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private static unsafe string GetLocaleInfoNative(string localeName, LocaleStringData type)
        {
            Debug.Assert(localeName != null, "[CultureData.GetLocaleInfoNative] Expected localeName to be not be null");

            return Interop.Globalization.GetLocaleInfoStringNative(localeName, (uint)type);
        }
    }
}
