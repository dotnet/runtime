// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private static unsafe bool TryConvertIanaIdToWindowsId(string ianaId, bool allocate, out string? windowsId)
        {
            if (GlobalizationMode.Invariant || GlobalizationMode.UseNls || ianaId is null)
            {
                windowsId = null;
                return false;
            }

            foreach (char c in ianaId)
            {
                // ICU uses some characters as a separator and trim the id at that character.
                // while we should fail if the Id contained one of these characters.
                if (c == '\\' || c == '\n' || c == '\r')
                {
                    windowsId = null;
                    return false;
                }
            }

            char* buffer = stackalloc char[100];
            int length = Interop.Globalization.IanaIdToWindowsId(ianaId, buffer, 100);
            if (length > 0)
            {
                windowsId = allocate ? new string(buffer, 0, length) : null;
                return true;
            }

            windowsId = null;
            return false;
        }

        private static unsafe bool TryConvertWindowsIdToIanaId(string windowsId, string? region, bool allocate,  out string? ianaId)
        {
            // This functionality is not enabled in the browser for the sake of size reduction.
            if (GlobalizationMode.Invariant || GlobalizationMode.UseNls || windowsId is null)
            {
                ianaId = null;
                return false;
            }

            if (windowsId.Equals("utc", StringComparison.OrdinalIgnoreCase))
            {
                // Special case UTC, as previously ICU would convert it to "Etc/GMT" which is incorrect name for UTC.
                ianaId = "Etc/UTC";
                return true;
            }

            foreach (char c in windowsId)
            {
                // ICU uses some characters as a separator and trim the id at that character.
                // while we should fail if the Id contained one of these characters.
                if (c == '\\' || c == '\n' || c == '\r')
                {
                    ianaId = null;
                    return false;
                }
            }

            // regionPtr will point at the region name encoded as ASCII.
            IntPtr regionPtr = IntPtr.Zero;

             // Regions usually are 2 or 3 characters length.
            const int MaxRegionNameLength = 11;

            // Ensure uppercasing the region as ICU require the region names be uppercased, otherwise ICU will assume default region and return unexpected result.
            if (region is not null && region.Length < MaxRegionNameLength)
            {
                byte* regionInAscii = stackalloc byte[region.Length + 1];
                int i = 0;
                for (; i < region.Length && region[i] <= '\u007F'; i++)
                {
                    regionInAscii[i] = (uint)(region[i] - 'a') <= ('z' - 'a') ? (byte)((region[i] - 'a') + 'A') : (byte)region[i];
                }

                if (i >= region.Length)
                {
                    regionInAscii[region.Length] = 0;
                    regionPtr = new IntPtr(regionInAscii);
                }

                // In case getting unexpected region names, we just fallback using the default region (pasing null region name to the ICU API).
            }

            char* buffer = stackalloc char[100];

            int length = Interop.Globalization.WindowsIdToIanaId(windowsId, regionPtr, buffer, 100);
            if (length > 0)
            {
                ianaId = allocate ? new string(buffer, 0, length) : null;
                return true;
            }

            ianaId = null;
            return false;
        }

    }
}
