// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Extensions;

using System;
using System.Xml;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Collections;

namespace System.Xml.Serialization
{
    /// <summary>
    ///   The <see cref="XmlCustomFormatter"/> class provides a set of static methods for converting
    ///   primitive type values to and from their XML string representations.</summary>
    internal class XmlCustomFormatter
    {
        private XmlCustomFormatter() { }

        internal static string FromDate(DateTime value)
        {
            return XmlConvert.ToString(value, "yyyy-MM-dd");
        }

        internal static string FromTime(DateTime value)
        {
            string dateFormat = value.Kind == DateTimeKind.Utc ? "HH:mm:ss.fffffffZ" : "HH:mm:ss.fffffffzzzzzz";
            return XmlConvert.ToString(DateTime.MinValue + value.TimeOfDay, dateFormat);
        }

        internal static string FromDateTime(DateTime value)
        {
            {
                // for mode DateTimeSerializationMode.Roundtrip and DateTimeSerializationMode.Default
                return XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind);
            }
        }

        internal static string FromChar(char value)
        {
            return XmlConvert.ToString((UInt16)value);
        }

        internal static string FromXmlName(string name)
        {
            return XmlConvert.EncodeName(name);
        }

        internal static string FromXmlNCName(string ncName)
        {
            return XmlConvert.EncodeLocalName(ncName);
        }

        internal static string FromXmlNmToken(string nmToken)
        {
            return XmlConvert.EncodeNmToken(nmToken);
        }

        internal static string FromXmlNmTokens(string nmTokens)
        {
            if (nmTokens == null)
                return null;
            if (nmTokens.IndexOf(' ') < 0)
                return FromXmlNmToken(nmTokens);
            else
            {
                string[] toks = nmTokens.Split(new char[] { ' ' });
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < toks.Length; i++)
                {
                    if (i > 0) sb.Append(' ');
                    sb.Append(FromXmlNmToken(toks[i]));
                }
                return sb.ToString();
            }
        }

        internal static void WriteArrayBase64(XmlWriter writer, byte[] inData, int start, int count)
        {
            if (inData == null || count == 0)
            {
                return;
            }
            writer.WriteBase64(inData, start, count);
        }

        internal static string FromByteArrayHex(byte[] value)
        {
            if (value == null)
                return null;
            if (value.Length == 0)
                return "";
            return ExtensionMethods.ToBinHexString(value);
        }

        internal static string FromEnum(long val, string[] vals, long[] ids, string typeName)
        {
#if DEBUG
            // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
            if (ids.Length != vals.Length) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Invalid enum"));
#endif

            long originalValue = val;
            StringBuilder sb = new StringBuilder();
            int iZero = -1;

            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == 0)
                {
                    iZero = i;
                    continue;
                }
                if (val == 0)
                {
                    break;
                }
                if ((ids[i] & originalValue) == ids[i])
                {
                    if (sb.Length != 0)
                        sb.Append(" ");
                    sb.Append(vals[i]);
                    val &= ~ids[i];
                }
            }
            if (val != 0)
            {
                // failed to parse the enum value
                throw new InvalidOperationException(SR.Format(SR.XmlUnknownConstant, originalValue, typeName == null ? "enum" : typeName));
            }
            if (sb.Length == 0 && iZero >= 0)
            {
                sb.Append(vals[iZero]);
            }
            return sb.ToString();
        }



        private static string[] s_allDateFormats = new string[] {
            "yyyy-MM-ddzzzzzz",
            "yyyy-MM-dd",
            "yyyy-MM-ddZ",
            "yyyy",
            "---dd",
            "---ddZ",
            "---ddzzzzzz",
            "--MM-dd",
            "--MM-ddZ",
            "--MM-ddzzzzzz",
            "--MM--",
            "--MM--Z",
            "--MM--zzzzzz",
            "yyyy-MM",
            "yyyy-MMZ",
            "yyyy-MMzzzzzz",
            "yyyyzzzzzz",
        };

        private static string[] s_allTimeFormats = new string[] {
            "HH:mm:ss.fffffffzzzzzz",
            "HH:mm:ss",
            "HH:mm:ss.f",
            "HH:mm:ss.ff",
            "HH:mm:ss.fff",
            "HH:mm:ss.ffff",
            "HH:mm:ss.fffff",
            "HH:mm:ss.ffffff",
            "HH:mm:ss.fffffff",
            "HH:mm:ssZ",
            "HH:mm:ss.fZ",
            "HH:mm:ss.ffZ",
            "HH:mm:ss.fffZ",
            "HH:mm:ss.ffffZ",
            "HH:mm:ss.fffffZ",
            "HH:mm:ss.ffffffZ",
            "HH:mm:ss.fffffffZ",
            "HH:mm:sszzzzzz",
            "HH:mm:ss.fzzzzzz",
            "HH:mm:ss.ffzzzzzz",
            "HH:mm:ss.fffzzzzzz",
            "HH:mm:ss.ffffzzzzzz",
            "HH:mm:ss.fffffzzzzzz",
            "HH:mm:ss.ffffffzzzzzz",
        };

        internal static DateTime ToDateTime(string value)
        {
            {
                // for mode DateTimeSerializationMode.Roundtrip and DateTimeSerializationMode.Default
                return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.RoundtripKind);
            }
        }

        internal static DateTime ToDateTime(string value, string[] formats)
        {
            return XmlConvert.ToDateTimeOffset(value, formats).DateTime;
        }

        internal static DateTime ToDate(string value)
        {
            return ToDateTime(value, s_allDateFormats);
        }

        internal static DateTime ToTime(string value)
        {
            return DateTime.ParseExact(value, s_allTimeFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.RoundtripKind);
        }

        internal static char ToChar(string value)
        {
            return (char)XmlConvert.ToUInt16(value);
        }

        internal static string ToXmlName(string value)
        {
            return XmlConvert.DecodeName(CollapseWhitespace(value));
        }

        internal static string ToXmlNCName(string value)
        {
            return XmlConvert.DecodeName(CollapseWhitespace(value));
        }

        internal static string ToXmlNmToken(string value)
        {
            return XmlConvert.DecodeName(CollapseWhitespace(value));
        }

        internal static string ToXmlNmTokens(string value)
        {
            return XmlConvert.DecodeName(CollapseWhitespace(value));
        }

        internal static byte[] ToByteArrayBase64(string value)
        {
            if (value == null) return null;
            value = value.Trim();
            if (value.Length == 0)
                return Array.Empty<byte>();
            return Convert.FromBase64String(value);
        }
        internal static byte[] ToByteArrayHex(string value)
        {
            if (value == null) return null;
            value = value.Trim();
            return ExtensionMethods.FromBinHexString(value, true);
        }

        internal static long ToEnum(string val, IDictionary vals, string typeName, bool validate)
        {
            long value = 0;
            string[] parts = val.Split(null);
            for (int i = 0; i < parts.Length; i++)
            {
                object id = vals[parts[i]];
                if (id != null)
                    value |= (long)id;
                else if (validate && parts[i].Length > 0)
                    throw new InvalidOperationException(SR.Format(SR.XmlUnknownConstant, parts[i], typeName));
            }
            return value;
        }

        private static string CollapseWhitespace(string value)
        {
            if (value == null)
                return null;
            return value.Trim();
        }
    }
}
