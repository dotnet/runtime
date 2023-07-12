// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private int JsGetLocaleInfo(LocaleNumberData type)
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
    }
}
