// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace System.Drawing
{
    public class FontConverter : TypeConverter
    {
        private const string StylePrefix = "style=";

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
        {
            return (destinationType == typeof(string)) || (destinationType == typeof(InstanceDescriptor));
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is Font font)
            {
                if (destinationType == typeof(string))
                {
                    if (culture == null)
                    {
                        culture = CultureInfo.CurrentCulture;
                    }

                    ValueStringBuilder sb = default;
                    sb.Append(font.Name);
                    sb.Append(culture.TextInfo.ListSeparator[0]);
                    sb.Append(" ");
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
                        sb.Append(culture.TextInfo.ListSeparator[0]);
                        sb.Append(" style=");
                        sb.Append(font.Style.ToString());
                    }

                    return sb.ToString();
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? met = typeof(Font).GetConstructor(new Type[] { typeof(string), typeof(float), typeof(FontStyle), typeof(GraphicsUnit) });
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

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (!(value is string font))
            {
                return base.ConvertFrom(context, culture, value);
            }

            font = font.Trim();

            // Expected string format: "name[, size[, units[, style=style1[, style2[...]]]]]"
            // Example using 'vi-VN' culture: "Microsoft Sans Serif, 8,25pt, style=Italic, Bold"
            if (font.Length == 0)
            {
                return null;
            }

            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            char separator = culture.TextInfo.ListSeparator[0]; // For vi-VN: ','
            string fontName = font; // start with the assumption that only the font name was provided.
            string? style = null;
            string? sizeStr;
            float fontSize = 8.25f;
            FontStyle fontStyle = FontStyle.Regular;
            GraphicsUnit units = GraphicsUnit.Point;

            // Get the index of the first separator (would indicate the end of the name in the string).
            int nameIndex = font.IndexOf(separator);

            if (nameIndex < 0)
            {
                return new Font(fontName, fontSize, fontStyle, units);
            }

            // Some parameters are provided in addition to name.
            fontName = font.Substring(0, nameIndex);

            if (nameIndex < font.Length - 1)
            {
                // Get the style index (if any). The size is a bit problematic because it can be formatted differently
                // depending on the culture, we'll parse it last.
                int styleIndex = culture.CompareInfo.IndexOf(font, StylePrefix, CompareOptions.IgnoreCase);

                if (styleIndex != -1)
                {
                    // style found.
                    style = font.Substring(styleIndex, font.Length - styleIndex);

                    // Get the mid-substring containing the size information.
                    sizeStr = font.Substring(nameIndex + 1, styleIndex - nameIndex - 1);
                }
                else
                {
                    // no style.
                    sizeStr = font.Substring(nameIndex + 1);
                }

                // Parse size.
                (string? size, string? unit) unitTokens = ParseSizeTokens(sizeStr, separator);

                if (unitTokens.size != null)
                {
                    try
                    {
                        fontSize = (float)GetFloatConverter().ConvertFromString(context, culture, unitTokens.size)!;
                    }
                    catch
                    {
                        // Exception from converter is too generic.
                        throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, font, $"name{separator} size[units[{separator} style=style1[{separator} style2{separator} ...]]]"), nameof(value));
                    }
                }

                if (unitTokens.unit != null)
                {
                    // ParseGraphicsUnits throws an ArgumentException if format is invalid.
                    units = ParseGraphicsUnits(unitTokens.unit);
                }

                if (style != null)
                {
                    // Parse FontStyle
                    style = style.Substring(6); // style string always starts with style=
                    string[] styleTokens = style.Split(separator);

                    for (int tokenCount = 0; tokenCount < styleTokens.Length; tokenCount++)
                    {
                        string styleText = styleTokens[tokenCount];
                        styleText = styleText.Trim();

                        fontStyle |= Enum.Parse<FontStyle>(styleText, true);

                        // Enum.IsDefined doesn't do what we want on flags enums...
                        FontStyle validBits = FontStyle.Regular | FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikeout;
                        if ((fontStyle | validBits) != validBits)
                        {
                            throw new InvalidEnumArgumentException(nameof(style), (int)fontStyle, typeof(FontStyle));
                        }
                    }
                }
            }

            return new Font(fontName, fontSize, fontStyle, units);

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "TypeDescriptor.GetConverter is safe for primitive types.")]
            static TypeConverter GetFloatConverter() => TypeDescriptor.GetConverter(typeof(float));
        }

        private static (string?, string?) ParseSizeTokens(string text, char separator)
        {
            string? size = null;
            string? units = null;

            text = text.Trim();

            int length = text.Length;
            int splitPoint;

            if (length > 0)
            {
                // text is expected to have a format like " 8,25pt, ". Leading and trailing spaces (trimmed above),
                // last comma, unit and decimal value may not appear.  We need to make it ####.##CC
                for (splitPoint = 0; splitPoint < length; splitPoint++)
                {
                    if (char.IsLetter(text[splitPoint]))
                    {
                        break;
                    }
                }

                char[] trimChars = new char[] { separator, ' ' };

                if (splitPoint > 0)
                {
                    size = text.Substring(0, splitPoint);
                    // Trimming spaces between size and units.
                    size = size.Trim(trimChars);
                }

                if (splitPoint < length)
                {
                    units = text.Substring(splitPoint);
                    units = units.TrimEnd(trimChars);
                }
            }

            return (size, units);
        }

        private static GraphicsUnit ParseGraphicsUnits(string units) =>
            units switch
            {
                "display" => GraphicsUnit.Display,
                "doc" => GraphicsUnit.Document,
                "pt" => GraphicsUnit.Point,
                "in" => GraphicsUnit.Inch,
                "mm" => GraphicsUnit.Millimeter,
                "px" => GraphicsUnit.Pixel,
                "world" => GraphicsUnit.World,
                _ => throw new ArgumentException(SR.Format(SR.InvalidArgumentValueFontConverter, units), nameof(units)),
            };

        public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues!!)
        {
            object? value;
            byte charSet = 1;
            float size = 8;
            string? name = null;
            bool vertical = false;
            FontStyle style = FontStyle.Regular;
            FontFamily? fontFamily = null;
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

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override PropertyDescriptorCollection? GetProperties(
            ITypeDescriptorContext? context,
            object value,
            Attribute[]? attributes)
        {
            if (value is not Font)
                return base.GetProperties(context, value, attributes);

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(value, attributes);
            return props.Sort(new string[] { nameof(Font.Name), nameof(Font.Size), nameof(Font.Unit) });
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

        public sealed class FontNameConverter : TypeConverter, IDisposable
        {
            private readonly FontFamily[] _fonts;

            public FontNameConverter()
            {
                _fonts = FontFamily.Families;
            }

            void IDisposable.Dispose()
            {
            }

            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                return value is string strValue ? MatchFontName(strValue, context) : base.ConvertFrom(context, culture, value);
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
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
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

            // Yes, we support picking an element from the list.
            public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

            private string MatchFontName(string name, ITypeDescriptorContext? context)
            {
                // Try a partial match
                string? bestMatch = null;

                // setting fontName as nullable since IEnumerable.Current returned nullable in 3.0
                foreach (string? fontName in GetStandardValues(context))
                {
                    Debug.Assert(fontName != null);
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

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
            {
                // display graphic unit is not supported.
                if (Values == null)
                {
                    base.GetStandardValues(context); // sets "values"
                    Debug.Assert(Values != null);
                    ArrayList filteredValues = new ArrayList(Values);
                    filteredValues.Remove(GraphicsUnit.Display);
                    Values = new StandardValuesCollection(filteredValues);
                }
                return Values;
            }
        }
    }
}
