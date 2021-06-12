// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Xml.Xsl
{
    /**
    * Converts according to XPath/XSLT rules.
    */
    internal static class XPathConvert
    {
        public static string DoubleToString(double dbl)
        {
            return dbl.ToString(CultureInfo.InvariantCulture);
        }

        public static double StringToDouble(string s)
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return double.NaN;
        }
    }
}
