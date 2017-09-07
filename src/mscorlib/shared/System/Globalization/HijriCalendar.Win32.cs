// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;

namespace System.Globalization
{
    public partial class HijriCalendar : Calendar
    {
        private int GetHijriDateAdjustment()
        {
            if (_hijriAdvance == Int32.MinValue)
            {
                // Never been set before.  Use the system value from registry.
                _hijriAdvance = GetAdvanceHijriDate();
            }
            return (_hijriAdvance);
        }

        private const String InternationalRegKey = "Control Panel\\International";
        private const String HijriAdvanceRegKeyEntry = "AddHijriDate";

        /*=================================GetAdvanceHijriDate==========================
        **Action: Gets the AddHijriDate value from the registry.
        **Returns:
        **Arguments:    None.
        **Exceptions:
        **Note:
        **  The HijriCalendar has a user-overidable calculation.  That is, use can set a value from the control
        **  panel, so that the calculation of the Hijri Calendar can move ahead or backwards from -2 to +2 days.
        **
        **  The valid string values in the registry are:
        **      "AddHijriDate-2"  =>  Add -2 days to the current calculated Hijri date.
        **      "AddHijriDate"    =>  Add -1 day to the current calculated Hijri date.
        **      ""              =>  Add 0 day to the current calculated Hijri date.
        **      "AddHijriDate+1"  =>  Add +1 days to the current calculated Hijri date.
        **      "AddHijriDate+2"  =>  Add +2 days to the current calculated Hijri date.
        ============================================================================*/
        private static int GetAdvanceHijriDate()
        {
            int hijriAdvance = 0;
            Microsoft.Win32.RegistryKey key = null;

            try
            {
                // Open in read-only mode.
                key = RegistryKey.GetBaseKey(RegistryKey.HKEY_CURRENT_USER).OpenSubKey(InternationalRegKey, false);
            }
            //If this fails for any reason, we'll just return 0.
            catch (ObjectDisposedException) { return 0; }
            catch (ArgumentException) { return 0; }

            if (key != null)
            {
                try
                {
                    Object value = key.InternalGetValue(HijriAdvanceRegKeyEntry, null, false, false);
                    if (value == null)
                    {
                        return (0);
                    }
                    String str = value.ToString();
                    if (String.Compare(str, 0, HijriAdvanceRegKeyEntry, 0, HijriAdvanceRegKeyEntry.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (str.Length == HijriAdvanceRegKeyEntry.Length)
                            hijriAdvance = -1;
                        else
                        {
                            try
                            {
                                int advance = Int32.Parse(str.AsReadOnlySpan().Slice(HijriAdvanceRegKeyEntry.Length), provider:CultureInfo.InvariantCulture);
                                if ((advance >= MinAdvancedHijri) && (advance <= MaxAdvancedHijri))
                                {
                                    hijriAdvance = advance;
                                }
                            }
                            // If we got garbage from registry just ignore it.
                            // hijriAdvance = 0 because of declaraction assignment up above.
                            catch (ArgumentException) { }
                            catch (FormatException) { }
                            catch (OverflowException) { }
                        }
                    }
                }
                finally
                {
                    key.Close();
                }
            }
            return (hijriAdvance);
        }
    }
}
