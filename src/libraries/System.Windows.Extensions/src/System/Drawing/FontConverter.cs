// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing.Text;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace System.Drawing
{
    public class FontConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string)) || (destinationType == typeof(InstanceDescriptor));
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is Font font)
            {
                if (destinationType == typeof(string))
                {
                    if (culture == null)
                    {
                        culture = CultureInfo.CurrentCulture;
                    }

                    ValueStringBuilder sb = new ValueStringBuilder();
                    sb.Append(font.Name);
                    sb.Append(culture.TextInfo.ListSeparator[0] + " ");
                    sb.Append(font.Size.ToString(culture.NumberFormat));

                    switch (font.Unit)
                    {
                        // MS throws ArgumentException, if unit is set
                        // to GraphicsUnit.Display
                        // Don't know what to append for GraphicsUnit.Display
                        case GraphicsUnit.Display:
                            sb.Append("display");
                            break;

                        case GraphicsUnit.Document:
                            sb.Append("doc");
                            break;

                        case GraphicsUnit.Point:
                            sb.Append("pt");
                            break;

                        case GraphicsUnit.Inch:
                            sb.Append("in");
                            break;

                        case GraphicsUnit.Millimeter:
                            sb.Append("mm");
                            break;

                        case GraphicsUnit.Pixel:
                            sb.Append("px");
                            break;

                        case GraphicsUnit.World:
                            sb.Append("world");
                            break;
                    }

                    if (font.Style != FontStyle.Regular)
                    {
                        sb.Append(culture.TextInfo.ListSeparator[0] + " style=");
                        sb.Append(font.Style.ToString());
                    }

                    return sb.ToString();
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo met = typeof(Font).GetTypeInfo().GetConstructor(new Type[] { typeof(string), typeof(float), typeof(FontStyle), typeof(GraphicsUnit) });
                    object[] args = new object[4];
                    args[0] = font.Name;
                    args[1] = font.Size;
                    args[2] = font.Style;
                    args[3] = font.Unit;

                    return new InstanceDescriptor(met, args);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            FontStyle f_style;
            float f_size;
            GraphicsUnit f_unit;
            string font;
            string units;
            string[] fields;

            if (!(value is string))
            {
                return base.ConvertFrom(context, culture, value);
            }

            font = (string)value;
            font = font.Trim();

            if (font.Length == 0)
            {
                return null;
            }

            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            // Format is FontFamily, size[<units>[, style=1,2,3]]
            // This is a bit tricky since the comma can be used for styles and fields
            fields = font.Split(new char[] { culture.TextInfo.ListSeparator[0] });
            if (fields.Length < 1)
            {
                throw new ArgumentException("Failed to parse font format");
            }

            font = fields[0];
            f_size = 8f;
            units = "px";
            f_unit = GraphicsUnit.Pixel;
            if (fields.Length > 1)
            {   // We have a size
                for (int i = 0; i < fields[1].Length; i++)
                {
                    if (char.IsLetter(fields[1][i]))
                    {
                        f_size = (float)TypeDescriptor.GetConverter(typeof(float)).ConvertFromString(context, culture, fields[1].Substring(0, i));
                        units = fields[1].Substring(i);
                        break;
                    }
                }
                switch (units)
                {
                    case "display":
                        f_unit = GraphicsUnit.Display;
                        break;

                    case "doc":
                        f_unit = GraphicsUnit.Document;
                        break;

                    case "pt":
                        f_unit = GraphicsUnit.Point;
                        break;

                    case "in":
                        f_unit = GraphicsUnit.Inch;
                        break;

                    case "mm":
                        f_unit = GraphicsUnit.Millimeter;
                        break;

                    case "px":
                        f_unit = GraphicsUnit.Pixel;
                        break;

                    case "world":
                        f_unit = GraphicsUnit.World;
                        break;
                }
            }

            f_style = FontStyle.Regular;
            if (fields.Length > 2)
            {   // We have style
                string compare;

                for (int i = 2; i < fields.Length; i++)
                {
                    compare = fields[i];

                    if (compare.IndexOf("Regular") != -1)
                    {
                        f_style |= FontStyle.Regular;
                    }
                    if (compare.IndexOf("Bold") != -1)
                    {
                        f_style |= FontStyle.Bold;
                    }
                    if (compare.IndexOf("Italic") != -1)
                    {
                        f_style |= FontStyle.Italic;
                    }
                    if (compare.IndexOf("Strikeout") != -1)
                    {
                        f_style |= FontStyle.Strikeout;
                    }
                    if (compare.IndexOf("Underline") != -1)
                    {
                        f_style |= FontStyle.Underline;
                    }
                }
            }

            return new Font(font, f_size, f_style, f_unit);
        }

        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            object value;
            byte charSet = 1;
            float size = 8;
            string name = null;
            bool vertical = false;
            FontStyle style = FontStyle.Regular;
            FontFamily fontFamily = null;
            GraphicsUnit unit = GraphicsUnit.Point;

            if ((value = propertyValues["GdiCharSet"]) != null)
                charSet = (byte)value;

            if ((value = propertyValues["Size"]) != null)
                size = (float)value;

            if ((value = propertyValues["Unit"]) != null)
                unit = (GraphicsUnit)value;

            if ((value = propertyValues["Name"]) != null)
                name = (string)value;

            if ((value = propertyValues["GdiVerticalFont"]) != null)
                vertical = (bool)value;

            if ((value = propertyValues["Bold"]) != null)
            {
                if ((bool)value == true)
                    style |= FontStyle.Bold;
            }

            if ((value = propertyValues["Italic"]) != null)
            {
                if ((bool)value == true)
                    style |= FontStyle.Italic;
            }

            if ((value = propertyValues["Strikeout"]) != null)
            {
                if ((bool)value == true)
                    style |= FontStyle.Strikeout;
            }

            if ((value = propertyValues["Underline"]) != null)
            {
                if ((bool)value == true)
                    style |= FontStyle.Underline;
            }

            if (name == null)
            {
                fontFamily = new FontFamily("Tahoma");
            }
            else
            {
                FontCollection collection = new InstalledFontCollection();
                FontFamily[] installedFontList = collection.Families;
                foreach (FontFamily font in installedFontList)
                {
                    if (name.Equals(font.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        fontFamily = font;
                        break;
                    }
                }

                // font family not found in installed fonts
                if (fontFamily == null)
                {
                    collection = new PrivateFontCollection();
                    FontFamily[] privateFontList = collection.Families;
                    foreach (FontFamily font in privateFontList)
                    {
                        if (name.Equals(font.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            fontFamily = font;
                            break;
                        }
                    }
                }

                // font family not found in private fonts also
                if (fontFamily == null)
                    fontFamily = FontFamily.GenericSansSerif;
            }

            return new Font(fontFamily, size, style, unit, charSet, vertical);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) => true;

        public override PropertyDescriptorCollection GetProperties(
            ITypeDescriptorContext context,
            object value,
            Attribute[] attributes)
        {
            return value is Font ? TypeDescriptor.GetProperties(value, attributes) : base.GetProperties(context, value, attributes);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

        public sealed class FontNameConverter : TypeConverter, IDisposable
        {
            FontFamily[] _fonts;

            public FontNameConverter()
            {
                _fonts = FontFamily.Families;
            }

            void IDisposable.Dispose()
            {
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return value is string strValue ? MatchFontName(strValue, context) : base.ConvertFrom(context, culture, value);
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                string[] values = new string[_fonts.Length];
                for (int i = 0; i < _fonts.Length; i++)
                {
                    values[i] = _fonts[i].Name;
                }
                Array.Sort(values, Comparer.Default);

                return new TypeConverter.StandardValuesCollection(values);
            }

            // We allow other values other than those in the font list.
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

            // Yes, we support picking an element from the list.
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            private string MatchFontName(string name, ITypeDescriptorContext context)
            {
                // Try a partial match
                string bestMatch = null;

                foreach (string fontName in GetStandardValues(context))
                {
                    if (fontName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // For an exact match, return immediately
                        return fontName;
                    }
                    if (fontName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (bestMatch == null || fontName.Length <= bestMatch.Length)
                        {
                            bestMatch = fontName;
                        }
                    }
                }

                // No match... fall back on whatever was provided
                return bestMatch ?? name;
            }
        }

        public class FontUnitConverter : EnumConverter
        {
            public FontUnitConverter() : base(typeof(GraphicsUnit)) { }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return base.GetStandardValues(context);
            }
        }
    }
}
