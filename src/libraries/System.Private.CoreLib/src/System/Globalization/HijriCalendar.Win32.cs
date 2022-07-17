// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Win32;

namespace System.Globalization
{
    public partial class HijriCalendar : Calendar
    {
        private int GetHijriDateAdjustment()
        {
            if (_hijriAdvance == int.MinValue)
            {
                // Never been set before.  Use the system value from registry.
                _hijriAdvance = GetAdvanceHijriDate();
            }
            return _hijriAdvance;
        }

        private const string InternationalRegKey = "Control Panel\\International";
        private const string HijriAdvanceRegKeyEntry = "AddHijriDate";

        /*=================================GetAdvanceHijriDate==========================
        **Action: Gets the AddHijriDate value from the registry.
        **Returns:
        **Arguments:    None.
        **Exceptions:
        **Note:
        **  The HijriCalendar has a user-overridable calculation.  That is, use can set a value from the control
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
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(InternationalRegKey))
            {
                // Abort if we didn't find anything
                if (key == null)
                {
                    return 0;
                }

                object? value = key.GetValue(HijriAdvanceRegKeyEntry);
                if (value == null)
                {
                    return 0;
                }

                int hijriAdvance = 0;
                string? str = value.ToString();
                if (string.Compare(str, 0, HijriAdvanceRegKeyEntry, 0, HijriAdvanceRegKeyEntry.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (str!.Length == HijriAdvanceRegKeyEntry.Length)
                        hijriAdvance = -1;
                    else
                    {
                        try
                        {
                            int advance = int.Parse(str.AsSpan(HijriAdvanceRegKeyEntry.Length), provider: CultureInfo.InvariantCulture);
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
                return hijriAdvance;
            }
        }
    }
}
