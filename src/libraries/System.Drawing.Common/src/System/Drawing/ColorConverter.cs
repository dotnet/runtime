// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing {
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Win32;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;

    /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter"]/*' />
    /// <devdoc>
    ///      ColorConverter is a class that can be used to convert
    ///      colors from one data type to another.  Access this
    ///      class through the TypeDescriptor.
    /// </devdoc>
    public class ColorConverter : TypeConverter {
        private static string ColorConstantsLock = "colorConstants";
        private static Hashtable colorConstants;
        private static string SystemColorConstantsLock = "systemColorConstants";
        private static Hashtable systemColorConstants;
        private static string ValuesLock = "values";
        private static StandardValuesCollection values;

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.ColorConverter"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ColorConverter() {
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.Colors"]/*' />
        /// <devdoc>
        ///      Hashtable of color / value pairs (color name is key)
        ///      for standard colors.
        /// </devdoc>
        private static Hashtable Colors {
            get {
                if (colorConstants == null) {
                    lock(ColorConstantsLock) {
                        if (colorConstants == null) {
                            Hashtable tempHash = new Hashtable(StringComparer.OrdinalIgnoreCase);
                            FillConstants(tempHash, typeof(Color));
                            colorConstants = tempHash;
                        }
                    }
                }

                return colorConstants;
            }
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.SystemColors"]/*' />
        /// <devdoc>
        ///      Hashtable of color / value pairs (color name is key)
        ///      for system colors.
        /// </devdoc>
        private static Hashtable SystemColors {
            get {
                if (systemColorConstants == null) {
                    lock (SystemColorConstantsLock) {
                        if (systemColorConstants == null) {
                            Hashtable tempHash = new Hashtable(StringComparer.OrdinalIgnoreCase);
                            FillConstants(tempHash, typeof(System.Drawing.SystemColors));
                            systemColorConstants = tempHash;
                        }
                    }
                }

                return systemColorConstants;
            }
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.CanConvertFrom"]/*' />
        /// <devdoc>
        ///      Determines if this converter can convert an object in the given source
        ///      type to the native type of the converter.
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.CanConvertTo"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </devdoc>
        public override bool CanConvertTo(ITypeDescriptorContext context, [NotNullWhen(true)] Type? destinationType) {
            if (destinationType == typeof(InstanceDescriptor)) {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        internal static object GetNamedColor(string name) {
            object color = null;
            // First, check to see if this is a standard name.
            //
            color = Colors[name];
            if (color != null) {
                return color;
            }
            // Ok, how about a system color?
            //
            color = SystemColors[name];
            return color;
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.ConvertFrom"]/*' />
        /// <devdoc>
        ///      Converts the given object to the converter's native type.
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            string strValue = value as string;
            if (strValue != null) {
                object obj = null;
                string text = strValue.Trim();

                if (text.Length == 0) {
                    obj = Color.Empty;
                }
                else {
                    // First, check to see if this is a standard name.
                    //
                    obj = GetNamedColor(text);

                    if (obj == null) {
                        culture ??= CultureInfo.CurrentCulture;

                        char sep = culture.TextInfo.ListSeparator[0];
                        bool tryMappingToKnownColor = true;

                        TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));

                        // If the value is a 6 digit hex number only, then
                        // we want to treat the Alpha as 255, not 0
                        //
                        if (text.IndexOf(sep) == -1) {

                            // text can be '' (empty quoted string)
                            if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[0] == text[text.Length -1]) {
                                // In quotes means a named value
                                string colorName = text.Substring(1, text.Length - 2);
                                obj = Color.FromName(colorName);
                                tryMappingToKnownColor = false;
                            }
                            else if ((text.Length == 7 && text[0] == '#') ||
                                     (text.Length == 8 && (text.StartsWith("0x") || text.StartsWith("0X"))) ||
                                     (text.Length == 8 && (text.StartsWith("&h") || text.StartsWith("&H")))) {
                                // Note: ConvertFromString will raise exception if value cannot be converted.
                                obj = Color.FromArgb(unchecked((int)(0xFF000000 | (uint)(int)intConverter.ConvertFromString(context, culture, text))));
                            }
                        }

                        // Nope.  Parse the RGBA from the text.
                        //
                        if (obj == null) {
                            string[] tokens = text.Split(sep);
                            int[] values = new int[tokens.Length];
                            for (int i = 0; i < values.Length; i++) {
                                values[i] = unchecked((int)intConverter.ConvertFromString(context, culture, tokens[i]));
                            }

                            // We should now have a number of parsed integer values.
                            // We support 1, 3, or 4 arguments:
                            //
                            // 1 -- full ARGB encoded
                            // 3 -- RGB
                            // 4 -- ARGB
                            //
                            switch (values.Length) {
                                case 1:
                                    obj = Color.FromArgb(values[0]);
                                    break;

                                case 3:
                                    obj = Color.FromArgb(values[0], values[1], values[2]);
                                    break;

                                case 4:
                                    obj = Color.FromArgb(values[0], values[1], values[2], values[3]);
                                    break;
                            }
                            tryMappingToKnownColor = true;
                        }

                        if ((obj != null) && tryMappingToKnownColor) {

                            // Now check to see if this color matches one of our known colors.
                            // If it does, then substitute it.  We can only do this for "Colors"
                            // because system colors morph with user settings.
                            //
                            int targetARGB = ((Color)obj).ToArgb();

                            foreach (Color c in Colors.Values) {
                                if (c.ToArgb() == targetARGB) {
                                    obj = c;
                                    break;
                                }
                            }
                        }
                    }

                    if (obj == null) {
                        throw new ArgumentException(SR.Format(SR.InvalidColor, text));
                    }
                }
                return obj;
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.ConvertTo"]/*' />
        /// <devdoc>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the destination type, this will
        ///      throw a NotSupportedException.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == null) {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if ( value is Color ){
                if (destinationType == typeof(string)) {
                    Color c = (Color)value;

                    if (c == Color.Empty) {
                        return string.Empty;
                    }
                    else {
                        // If this is a known color, then Color can provide its own
                        // name.  Otherwise, we fabricate an ARGB value for it.
                        //
                        if (c.IsKnownColor) {
                            return c.Name;
                        }
                        else if (c.IsNamedColor) {
                            return "'" + c.Name + "'";
                        }
                        else {
                            culture ??= CultureInfo.CurrentCulture;
                            string sep = culture.TextInfo.ListSeparator + " ";
                            TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
                            string[] args;
                            int nArg = 0;

                            if (c.A < 255) {
                                args = new string[4];
                                args[nArg++] = intConverter.ConvertToString(context, culture, (object)c.A);
                            }
                            else {
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
                if (destinationType == typeof(InstanceDescriptor)) {
                    MemberInfo member = null;
                    object[] args = null;

                    Color c = (Color)value;

                    if (c.IsEmpty) {
                        member = typeof(Color).GetField("Empty");
                    }
                    else if (c.IsSystemColor) {
                        member = typeof(SystemColors).GetProperty(c.Name);
                    }
                    else if (c.IsKnownColor) {
                        member = typeof(Color).GetProperty(c.Name);
                    }
                    else if (c.A != 255) {
                        member = typeof(Color).GetMethod("FromArgb", new Type[] {typeof(int), typeof(int), typeof(int), typeof(int)});
                        args = new object[] {c.A, c.R, c.G, c.B};
                    }
                    else if (c.IsNamedColor) {
                        member = typeof(Color).GetMethod("FromName", new Type[] {typeof(string)});
                        args = new object[] {c.Name};
                    }
                    else {
                        member = typeof(Color).GetMethod("FromArgb", new Type[] {typeof(int), typeof(int), typeof(int)});
                        args = new object[] {c.R, c.G, c.B};
                    }

                    Debug.Assert(member != null, "Could not convert color to member.  Did someone change method name / signature and not update Colorconverter?");
                    if (member != null) {
                        return new InstanceDescriptor(member, args);
                    }
                    else {
                        return null;
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.FillConstants"]/*' />
        /// <devdoc>
        ///      Fills the given hashtable with field name / value pairs.  It walks all public static
        ///      properties of enumType that have a property type of Color.
        /// </devdoc>
        private static void FillConstants(Hashtable hash, Type enumType) {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.Static;
            PropertyInfo[] props = enumType.GetProperties();

            for (int i = 0; i < props.Length; i++) {
                PropertyInfo prop = props[i];
                if (prop.PropertyType == typeof(Color)) {
                    MethodInfo method = prop.GetGetMethod();
                    if (method != null && (method.Attributes & attrs) == attrs) {
                        object[] tempIndex = null;
                        hash[prop.Name] = prop.GetValue(null, tempIndex);
                    }
                }
            }
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.GetStandardValues"]/*' />
        /// <devdoc>
        ///      Retrieves a collection containing a set of standard values
        ///      for the data type this validator is designed for.  This
        ///      will return null if the data type does not support a
        ///      standard set of values.
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (values == null) {
                lock (ValuesLock) {
                    if (values == null) {

                       // We must take the value from each hashtable and combine them.
                       //
                       ArrayList arrayValues = new ArrayList();
                       arrayValues.AddRange(Colors.Values);
                       arrayValues.AddRange(SystemColors.Values);

                       // Now, we have a couple of colors that have the same names but
                       // are identical values.  Look for these and remove them.  Too
                       // bad this is n^2.
                       //
                       int count = arrayValues.Count;
                       for (int i = 0; i < count - 1; i++) {
                           for (int j = i + 1; j < count; j++) {
                               if (arrayValues[i].Equals(arrayValues[j])) {
                                   // Remove this item!
                                   //
                                   arrayValues.RemoveAt(j);
                                   count--;
                                   j--;
                               }
                           }
                       }

                       // Sort the array.
                       //
                       arrayValues.Sort(0, arrayValues.Count, new ColorComparer());
                       values = new StandardValuesCollection(arrayValues.ToArray());
                    }
                }
            }

            return values;
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.GetStandardValuesSupported"]/*' />
        /// <devdoc>
        ///      Determines if this object supports a standard set of values
        ///      that can be picked from a list.
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }

        /// <include file='doc\ColorConverter.uex' path='docs/doc[@for="ColorConverter.ColorComparer"]/*' />
        /// <devdoc>
        ///      IComparer for color values.  This takes color values but compares their
        ///      names.
        /// </devdoc>
        private sealed class ColorComparer : IComparer {

            public int Compare(object left, object right) {
                Color cLeft = (Color)left;
                Color cRight = (Color)right;
                return string.Compare(cLeft.Name, cRight.Name, false, CultureInfo.InvariantCulture);
            }
        }
    }
}
