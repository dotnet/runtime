// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Internal.Runtime.Augments;

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        public static int GetJapaneseEraCount()
        {
            return WinRTInterop.Callbacks.GetJapaneseEraCount();
        }

        public static bool GetJapaneseEraInfo(int era, out DateTimeOffset dateOffset, out string eraName, out string abbreviatedEraName)
        {
            return  WinRTInterop.Callbacks.GetJapaneseEraInfo(era, out dateOffset, out eraName, out abbreviatedEraName);
        }
    }
}
