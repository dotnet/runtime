// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a
    ///       type converter to convert globally unique identifier objects to and from various
    ///       other representations.</para>
    /// </summary>
    public class GuidConverter : TypeConverter
    {
        /// <summary>
        ///    <para>Gets a value indicating whether this
        ///       converter can convert an object in the given source type to a globally unique identifier object
        ///       using the context.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this converter can convert an object to
        ///        the given destination type using the context.
        ///    </para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///    <para>Converts the given object to a globally unique identifier object.</para>
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text != null)
            {
                text = text.Trim();
                return new Guid(text);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the destination type, this will
        ///      throw a NotSupportedException.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }
            if (destinationType == typeof(InstanceDescriptor) && value is Guid)
            {
                ConstructorInfo ctor = typeof(Guid).GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    return new InstanceDescriptor(ctor, new object[] { value.ToString() });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
