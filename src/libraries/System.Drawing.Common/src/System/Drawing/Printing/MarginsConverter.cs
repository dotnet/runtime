// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.Drawing.Printing.Margins'/> to and from various other representations, such as a string.
    /// </summary>
    public class MarginsConverter : ExpandableObjectConverter
    {
        /// <summary>
        /// Determines if a converter can convert an object of the given source
        /// type to the native type of the converter.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can
        /// convert an object to the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to the converter's native type.
        /// </summary>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string strValue)
            {
                string text = strValue.Trim();

                if (text.Length == 0)
                {
                    return null;
                }
                else
                {
                    // Parse 4 integer values.
                    culture ??= CultureInfo.CurrentCulture;
                    char sep = culture.TextInfo.ListSeparator[0];
                    string[] tokens = text.Split(sep);
                    int[] values = new int[tokens.Length];
                    TypeConverter intConverter = GetIntConverter();
                    for (int i = 0; i < values.Length; i++)
                    {
                        // Note: ConvertFromString will raise exception if value cannot be converted.
                        values[i] = (int)intConverter.ConvertFromString(context, culture, tokens[i])!;
                    }
                    if (values.Length != 4)
                    {
                        throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text, "left, right, top, bottom"));
                    }
                    return new Margins(values[0], values[1], values[2], values[3]);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "TypeDescriptor.GetConverter is safe for primitive types.")]
        private static TypeConverter GetIntConverter() => TypeDescriptor.GetConverter(typeof(int));

        /// <summary>
        /// Converts the given object to another type. The most common types to convert
        /// are to and from a string object. The default implementation will make a call
        /// to ToString on the object if the object is valid and if the destination
        /// type is string. If this cannot convert to the destination type, this will
        /// throw a NotSupportedException.
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is Margins margins)
            {
                if (destinationType == typeof(string))
                {
                    culture ??= CultureInfo.CurrentCulture;
                    string sep = culture.TextInfo.ListSeparator + " ";
                    TypeConverter intConverter = GetIntConverter();
                    string?[] args = new string[4];
                    int nArg = 0;

                    // Note: ConvertToString will raise exception if value cannot be converted.
                    args[nArg++] = intConverter.ConvertToString(context, culture, margins.Left);
                    args[nArg++] = intConverter.ConvertToString(context, culture, margins.Right);
                    args[nArg++] = intConverter.ConvertToString(context, culture, margins.Top);
                    args[nArg++] = intConverter.ConvertToString(context, culture, margins.Bottom);

                    return string.Join(sep, args);
                }
                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(Margins).GetConstructor(new Type[] {
                        typeof(int), typeof(int), typeof(int), typeof(int)});

                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] {
                            margins.Left, margins.Right, margins.Top, margins.Bottom});
                    }
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Determines if changing a value on this object should require a call to
        /// CreateInstance to create a new value.
        /// </summary>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

        /// <summary>
        /// Creates an instance of this type given a set of property values
        /// for the object.  This is useful for objects that are immutable, but still
        /// want to provide changable properties.
        /// </summary>
        public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues)
        {
            ArgumentNullException.ThrowIfNull(propertyValues);

            object? left = propertyValues["Left"];
            object? right = propertyValues["Right"];
            object? top = propertyValues["Top"];
            object? bottom = propertyValues["Bottom"];

            if (left == null || right == null || bottom == null || top == null ||
                !(left is int) || !(right is int) || !(bottom is int) || !(top is int))
            {
                throw new ArgumentException(SR.PropertyValueInvalidEntry);
            }

            return new Margins((int)left,
                                (int)right,
                                (int)top,
                                (int)bottom);
        }
    }
}
