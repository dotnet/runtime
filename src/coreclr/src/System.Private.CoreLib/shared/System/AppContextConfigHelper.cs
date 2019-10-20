// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System
{
    internal static class AppContextConfigHelper
    {
        internal static int GetInt32Config(string configName, int defaultValue, bool allowNegative = true)
        {
            try
            {
                object config = AppContext.GetData(configName);
                int result = defaultValue;
                switch (config)
                {
                    case string str:
                        if (str.StartsWith("0x"))
                        {
                            result = Convert.ToInt32(str, 16);
                        }
                        else if (str.StartsWith("0"))
                        {
                            result = Convert.ToInt32(str, 8);
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
                object config = AppContext.GetData(configName);
                short result = defaultValue;
                switch (config)
                {
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
