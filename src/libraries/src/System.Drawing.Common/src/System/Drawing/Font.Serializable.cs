// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Drawing
{
    [Serializable]
    partial class Font
    {
        private Font(SerializationInfo info, StreamingContext context)
        {
            Debug.Assert(info != null, "Didn't expect a null parameter");

            string name = null;
            float size = -1f;
            FontStyle style = FontStyle.Regular;
            GraphicsUnit unit = GraphicsUnit.Point;

            SerializationInfoEnumerator sie = info.GetEnumerator();
            for (; sie.MoveNext();)
            {
                if (string.Equals(sie.Name, "Name", StringComparison.OrdinalIgnoreCase))
                    name = (string)sie.Value;
                else if (string.Equals(sie.Name, "Size", StringComparison.OrdinalIgnoreCase))
                {
                    if (sie.Value is System.String)
                    {
                        size = ConvertFromString((string)sie.Value);
                    }
                    else
                    {
                        size = (float)sie.Value;
                    }
                }
                else if (string.Compare(sie.Name, "Style", true, CultureInfo.InvariantCulture) == 0)
                    style = (FontStyle)sie.Value;
                else if (string.Compare(sie.Name, "Unit", true, CultureInfo.InvariantCulture) == 0)
                    unit = (GraphicsUnit)sie.Value;
                else
                {
                    Debug.Fail("Unknown serialization item for font: " + sie.Name);
                }
            }

            Initialize(name, size, style, unit, SafeNativeMethods.DEFAULT_CHARSET, IsVerticalName(name));
        }

        private float ConvertFromString(string text)
        {
            // Simplified version of SingleConverter.ConvertFrom(string).

            CultureInfo culture = CultureInfo.CurrentCulture;
            text = text.Trim();

            try
            {
                if (text[0] == '#')
                {
                    return Convert.ToSingle(text.Substring(1), CultureInfo.CurrentCulture);
                }
                else if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                         || text.StartsWith("&h", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToSingle(text.Substring(2), CultureInfo.CurrentCulture);
                }
                else
                {
                    if (culture == null)
                    {
                        culture = CultureInfo.CurrentCulture;
                    }
                    NumberFormatInfo formatInfo = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
                    return float.Parse(text, NumberStyles.Float, formatInfo);
                }
            }
            catch (Exception e)
            {
                throw new Exception(SR.Format(SR.ConvertInvalidPrimitive, text, typeof(float).Name), e);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            // Serialize the original Font name rather than the fallback font name if we have one
            si.AddValue("Name", String.IsNullOrEmpty(OriginalFontName) ? Name : OriginalFontName);
            si.AddValue("Size", Size);
            si.AddValue("Style", Style);
            si.AddValue("Unit", Unit);
        }
    }
}

