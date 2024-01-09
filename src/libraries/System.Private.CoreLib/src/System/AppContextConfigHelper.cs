// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static class AppContextConfigHelper
    {
        internal static bool GetBooleanConfig(string switchName, bool defaultValue) =>
            AppContext.TryGetSwitch(switchName, out bool value) ? value : defaultValue;

        internal static bool GetBooleanConfig(string switchName, string envVariable, bool defaultValue = false)
        {
            string? str = Environment.GetEnvironmentVariable(envVariable);
            if (str != null)
            {
                if (str == "1" || bool.IsTrueStringIgnoreCase(str))
                {
                    return true;
                }
                if (str == "0" || bool.IsFalseStringIgnoreCase(str))
                {
                    return false;
                }
            }

            return GetBooleanConfig(switchName, defaultValue);
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

        internal static int GetInt32Config(string configName, string envVariable, int defaultValue, bool allowNegative = true)
        {
            string? str = Environment.GetEnvironmentVariable(envVariable);
            if (str != null)
            {
                try
                {
                    int result;
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

                    if (allowNegative || result >= 0)
                    {
                        return result;
                    }
                }
                catch (FormatException)
                {
                }
                catch (OverflowException)
                {
                }
            }

            return GetInt32Config(configName, defaultValue, allowNegative);
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
                        else if (str.StartsWith('0'))
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

        internal static short GetInt16Config(string configName, string envVariable, short defaultValue, bool allowNegative = true)
        {
            string? str = Environment.GetEnvironmentVariable(envVariable);
            if (str != null)
            {
                try
                {
                    short result;
                    if (str.StartsWith('0'))
                    {
                        if (str.Length >= 2 && str[1] == 'x')
                        {
                            result = Convert.ToInt16(str, 16);
                        }
                        else
                        {
                            result = Convert.ToInt16(str, 8);
                        }
                    }
                    else
                    {
                        result = short.Parse(str, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
                    }

                    if (allowNegative || result >= 0)
                    {
                        return result;
                    }
                }
                catch (FormatException)
                {
                }
                catch (OverflowException)
                {
                }
            }

            return GetInt16Config(configName, defaultValue, allowNegative);
        }
    }
}
