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
            if (double.IsPositiveInfinity(dbl))
            {
                return "Infinity";
            }
            else if (double.IsNegativeInfinity(dbl))
            {
                return "-Infinity";
            }
            // NaN is "NaN" in InvariantCulture

            return dbl.ToString(CultureInfo.InvariantCulture);
        }

        public static double StringToDouble(string s)
        {
            if (s.Contains("∞"))
            {
                // Previous unsafe implementation doesn't accept inf symbol
                return double.NaN;
            }

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return double.NaN;
        }
    }
}
