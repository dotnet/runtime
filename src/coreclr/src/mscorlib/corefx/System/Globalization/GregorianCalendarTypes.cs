// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Globalization
{
    // Note: The values of the members of this enum must match the coresponding values
    // in the CalendarId enum (since we cast between GregorianCalendarTypes and CalendarId).
    public enum GregorianCalendarTypes
    {
        Localized = CalendarId.GREGORIAN,
        USEnglish = CalendarId.GREGORIAN_US,
        MiddleEastFrench = CalendarId.GREGORIAN_ME_FRENCH,
        Arabic = CalendarId.GREGORIAN_ARABIC,
        TransliteratedEnglish = CalendarId.GREGORIAN_XLIT_ENGLISH,
        TransliteratedFrench = CalendarId.GREGORIAN_XLIT_FRENCH,
    }
}
