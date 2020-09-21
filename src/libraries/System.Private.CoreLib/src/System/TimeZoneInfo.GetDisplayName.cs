// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Security;

using Internal.IO;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private unsafe void GetDisplayName(Interop.Globalization.TimeZoneDisplayNameType nameType, string uiCulture, ref string? displayName)
        {
            if (GlobalizationMode.Invariant)
            {
                displayName = _standardDisplayName;
                return;
            }

            string? timeZoneDisplayName;
            bool result = Interop.CallStringMethod(
                (buffer, locale, id, type) =>
                {
                    fixed (char* bufferPtr = buffer)
                    {
                        return Interop.Globalization.GetTimeZoneDisplayName(locale, id, type, bufferPtr, buffer.Length);
                    }
                },
                uiCulture,
                _id,
                nameType,
                out timeZoneDisplayName);

            if (!result && uiCulture != FallbackCultureName)
            {
                // Try to fallback using FallbackCultureName just in case we can make it work.
                result = Interop.CallStringMethod(
                    (buffer, locale, id, type) =>
                    {
                        fixed (char* bufferPtr = buffer)
                        {
                            return Interop.Globalization.GetTimeZoneDisplayName(locale, id, type, bufferPtr, buffer.Length);
                        }
                    },
                    FallbackCultureName,
                    _id,
                    nameType,
                    out timeZoneDisplayName);
            }

            // If there is an unknown error, don't set the displayName field.
            // It will be set to the abbreviation that was read out of the tzfile.
            if (result)
            {
                displayName = timeZoneDisplayName;
            }
        }
    }
}
