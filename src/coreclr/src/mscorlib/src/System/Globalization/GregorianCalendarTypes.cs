// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
