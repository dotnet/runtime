// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        private static int GetJapaneseEraCount()
        {
            //UNIXTODO: Implement this fully.
            return 0;
        }

        private static bool GetJapaneseEraInfo(int era, out DateTimeOffset dateOffset, out string eraName, out string abbreviatedEraName)
        {
            //UNIXTODO: Implement this fully.
            dateOffset = default(DateTimeOffset);
            eraName = null;
            abbreviatedEraName = null;

            return false;
        }
    }
}
