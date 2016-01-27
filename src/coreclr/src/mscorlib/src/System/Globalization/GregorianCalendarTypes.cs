// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization {
    using System;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum GregorianCalendarTypes {
        Localized = Calendar.CAL_GREGORIAN,
        USEnglish = Calendar.CAL_GREGORIAN_US,
        MiddleEastFrench = Calendar.CAL_GREGORIAN_ME_FRENCH,
        Arabic = Calendar.CAL_GREGORIAN_ARABIC,
        TransliteratedEnglish = Calendar.CAL_GREGORIAN_XLIT_ENGLISH,
        TransliteratedFrench = Calendar.CAL_GREGORIAN_XLIT_FRENCH,
    }
}
