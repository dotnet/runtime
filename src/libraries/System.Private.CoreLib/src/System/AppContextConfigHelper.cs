// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static class AppContextConfigHelper
    {
        internal static bool GetBooleanConfig(string configName, bool defaultValue) =>
            AppContext.TryGetSwitch(configName, out bool value) ? value : defaultValue;

        internal static bool GetBooleanConfig(string switchName, string envVariable, bool defaultValue = false)
        {
            if (!AppContext.TryGetSwitch(switchName, out bool ret))
            {
                string? switchValue = Environment.GetEnvironmentVariable(envVariable);
                ret = switchValue != null ? (bool.IsTrueStringIgnoreCase(switchValue) || switchValue.Equals("1")) : defaultValue;
            }

            return ret;
        }

        internal static int GetInt32Config(string configName, int defaultValue, bool allowNegative = true)
        {
            try
            {
                object? config = AppContext.GetData(configName);
                int result = defaultValue;
                switch (config)
                {
                    case uint value:
                        result = (int)value;
                        break;
                    case string str:
                        if (str.StartsWith('0'))
                        {
                            if (str.Length >= 2 && str[1] == 'x')
                            {
                                result = Convert.ToInt32(str, 16);
                            }
                            else
                            {
                                result = Convert.ToInt32(str, 8);
                            }
                        }
                        else
                        {
                            result = int.Parse(str, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
                        }
                        break;
                    case IConvertible convertible:
                        result = convertible.ToInt32(NumberFormatInfo.InvariantInfo);
                        break;
                }
                return !allowNegative && result < 0 ? defaultValue : result;
            }
            catch (FormatException)
            {
                return defaultValue;
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
        }


        internal static short GetInt16Config(string configName, short defaultValue, bool allowNegative = true)
        {
            try
            {
                object? config = AppContext.GetData(configName);
                short result = defaultValue;
                switch (config)
                {
                    case uint value:
                        {
                            result = (short)value;
                            if ((uint)result != value)
                            {
                                return defaultValue; // overflow
                            }
                            break;
                        }
                    case string str:
                        if (str.StartsWith("0x"))
                        {
                            result = Convert.ToInt16(str, 16);
                        }
                        else if (str.StartsWith("0"))
                        {
                            result = Convert.ToInt16(str, 8);
                        }
                        else
                        {
                            result = short.Parse(str, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
                        }
                        break;
                    case IConvertible convertible:
                        result = convertible.ToInt16(NumberFormatInfo.InvariantInfo);
                        break;
                }
                return !allowNegative && result < 0 ? defaultValue : result;
            }
            catch (FormatException)
            {
                return defaultValue;
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
        }
    }
}
