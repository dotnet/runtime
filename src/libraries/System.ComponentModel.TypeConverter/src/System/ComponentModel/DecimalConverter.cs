// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides a type converter to convert <see cref='System.Decimal'/>
    /// objects to and from various
    /// other representations.</para>
    /// </summary>
    public class DecimalConverter : BaseNumberConverter
    {
        /// <summary>
        /// Determines whether this editor will attempt to convert hex (0x or #) strings
        /// </summary>
        internal override bool AllowHex => false;

        /// <summary>
        /// The Type this converter is targeting (e.g. Int16, UInt32, etc.)
        /// </summary>
        internal override Type TargetType => typeof(decimal);

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this converter can
        ///        convert an object to the given destination type using the context.
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

            if (destinationType == typeof(InstanceDescriptor) && value is Decimal)
            {

                object[] args = new object[] { Decimal.GetBits((Decimal)value) };
                MemberInfo member = typeof(Decimal).GetConstructor(new Type[] { typeof(Int32[]) });

                Debug.Assert(member != null, "Could not convert decimal to member.  Did someone change method name / signature and not update DecimalConverter?");
                if (member != null)
                {
                    return new InstanceDescriptor(member, args);
                }
                else
                {
                    return null;
                }
            }
            
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Convert the given value to a string using the given radix
        /// </summary>
        internal override object FromString(string value, int radix)
        {
            return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Convert the given value to a string using the given formatInfo
        /// </summary>
        internal override object FromString(string value, NumberFormatInfo formatInfo)
        {
            return Decimal.Parse(value, NumberStyles.Float, formatInfo);
        }

        /// <summary>
        /// Convert the given value from a string using the given formatInfo
        /// </summary>
        internal override string ToString(object value, NumberFormatInfo formatInfo)
        {
            return ((Decimal)value).ToString("G", formatInfo);
        }
    }
}

