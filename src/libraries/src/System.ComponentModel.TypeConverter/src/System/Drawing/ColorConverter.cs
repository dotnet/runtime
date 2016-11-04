// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.Drawing
{
    public class ColorConverter : TypeConverter
    {
        private static object s_valuesLock = new object();
        private static StandardValuesCollection s_values;

        public ColorConverter()
        {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string strValue = value as string;
            if (strValue != null)
            {
                string text = strValue.Trim();

                if (text.Length == 0)
                {
                    return Color.Empty;
                }

                {
                    Color c;
                    // First, check to see if this is a standard name.
                    //
                    if (ColorTable.TryGetNamedColor(text, out c))
                    {
                        return c;
                    }
                }

                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                char sep = culture.TextInfo.ListSeparator[0];

                TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));

                // If the value is a 6 digit hex number only, then
                // we want to treat the Alpha as 255, not 0
                //
                if (text.IndexOf(sep) == -1)
                {
                    // text can be '' (empty quoted string)
                    if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[0] == text[text.Length - 1])
                    {
                        // In quotes means a named value
                        string colorName = text.Substring(1, text.Length - 2);
                        return Color.FromName(colorName);
                    }
                    else if ((text.Length == 7 && text[0] == '#') ||
                             (text.Length == 8 && (text.StartsWith("0x") || text.StartsWith("0X"))) ||
                             (text.Length == 8 && (text.StartsWith("&h") || text.StartsWith("&H"))))
                    {
                        // Note: ConvertFromString will raise exception if value cannot be converted.
                        return PossibleKnownColor(Color.FromArgb(unchecked((int)(0xFF000000 | (uint)(int)intConverter.ConvertFromString(context, culture, text)))));
                    }
                }

                // Nope.  Parse the RGBA from the text.
                //
                string[] tokens = text.Split(sep);
                int[] values = new int[tokens.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = unchecked((int)intConverter.ConvertFromString(context, culture, tokens[i]));
                }

                // We should now have a number of parsed integer values.
                // We support 1, 3, or 4 arguments:
                //
                // 1 -- full ARGB encoded
                // 3 -- RGB
                // 4 -- ARGB
                //
                switch (values.Length)
                {
                    case 1:
                        return PossibleKnownColor(Color.FromArgb(values[0]));

                    case 3:
                        return PossibleKnownColor(Color.FromArgb(values[0], values[1], values[2]));

                    case 4:
                        return PossibleKnownColor(Color.FromArgb(values[0], values[1], values[2], values[3]));
                }

                throw new ArgumentException(SR.Format(SR.InvalidColor, text));
            }
            return base.ConvertFrom(context, culture, value);
        }

        private Color PossibleKnownColor(Color color)
        {
            // Now check to see if this color matches one of our known colors.
            // If it does, then substitute it.  We can only do this for "Colors"
            // because system colors morph with user settings.
            //
            int targetARGB = color.ToArgb();

            foreach (Color c in ColorTable.Colors.Values)
            {
                if (c.ToArgb() == targetARGB)
                {
                    return c;
                }
            }
            return color;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (value is Color)
            {
                if (destinationType == typeof(string))
                {
                    Color c = (Color)value;

                    if (c == Color.Empty)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        // If this is a known color, then Color can provide its own
                        // name.  Otherwise, we fabricate an ARGB value for it.
                        //
                        if (c.IsKnownColor)
                        {
                            return c.Name;
                        }
                        else if (c.IsNamedColor)
                        {
                            return "'" + c.Name + "'";
                        }
                        else
                        {
                            if (culture == null)
                            {
                                culture = CultureInfo.CurrentCulture;
                            }
                            string sep = culture.TextInfo.ListSeparator + " ";
                            TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
                            string[] args;
                            int nArg = 0;

                            if (c.A < 255)
                            {
                                args = new string[4];
                                args[nArg++] = intConverter.ConvertToString(context, culture, (object)c.A);
                            }
                            else
                            {
                                args = new string[3];
                            }

                            // Note: ConvertToString will raise exception if value cannot be converted.
                            args[nArg++] = intConverter.ConvertToString(context, culture, (object)c.R);
                            args[nArg++] = intConverter.ConvertToString(context, culture, (object)c.G);
                            args[nArg++] = intConverter.ConvertToString(context, culture, (object)c.B);

                            // Now slam all of these together with the fantastic Join 
                            // method.
                            //
                            return string.Join(sep, args);
                        }
                    }
                }
                
                if (destinationType == typeof(InstanceDescriptor))
                {
                    MemberInfo member = null;
                    object[] args = null;

                    Color c = (Color)value;

                    if (c.IsEmpty)
                    {
                        member = typeof(Color).GetField("Empty");
                    }
                    else if (c.IsSystemColor)
                    {
                        member = typeof(SystemColors).GetProperty(c.Name);
                    }
                    else if (c.IsKnownColor)
                    {
                        member = typeof(Color).GetProperty(c.Name);
                    }
                    else if (c.A != 255)
                    {
                        member = typeof(Color).GetMethod("FromArgb", new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) });
                        args = new object[] { c.A, c.R, c.G, c.B };
                    }
                    else if (c.IsNamedColor)
                    {
                        member = typeof(Color).GetMethod("FromName", new Type[] { typeof(string) });
                        args = new object[] { c.Name };
                    }
                    else
                    {
                        member = typeof(Color).GetMethod("FromArgb", new Type[] { typeof(int), typeof(int), typeof(int) });
                        args = new object[] { c.R, c.G, c.B };
                    }

                    Debug.Assert(member != null, "Could not convert color to member.  Did someone change method name / signature and not update Colorconverter?");
                    if (member != null)
                    {
                        return new InstanceDescriptor(member, args);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (s_values == null)
            {
                lock (s_valuesLock)
                {
                    if (s_values == null)
                    {
                        // We must take the value from each hashtable and combine them.
                        //
                        HashSet<Color> set =
                            new HashSet<Color>(ColorTable.Colors.Values.Concat(ColorTable.SystemColors.Values));

                        s_values = new StandardValuesCollection(set.OrderBy(c => c, new ColorComparer()).ToList());
                    }
                }
            }

            return s_values;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private class ColorComparer : IComparer<Color>
        {
            public int Compare(Color left, Color right)
            {
                return string.CompareOrdinal(left.Name, right.Name);
            }
        }
    }
}
