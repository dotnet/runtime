// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter"]/*' />
    /// <devdoc>
    ///      PointConverter is a class that can be used to convert
    ///      Point from one data type to another.  Access this
    ///      class through the TypeDescriptor.
    /// </devdoc>
    public class PointConverter : TypeConverter {
    
        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.CanConvertFrom"]/*' />
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

        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.CanConvertTo"]/*' />
        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </devdoc>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            if (destinationType == typeof(InstanceDescriptor)) {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.ConvertFrom"]/*' />
        /// <devdoc>
        ///      Converts the given object to the converter's native type.
        /// </devdoc>        
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {

            string strValue = value as string;

            if (strValue != null) {

                string text = strValue.Trim();
            
                if (text.Length == 0) {
                    return null;
                }
                else {
                
                    // Parse 2 integer values.
                    //
                    if (culture == null) {
                        culture = CultureInfo.CurrentCulture;
                    }                    
                    char sep = culture.TextInfo.ListSeparator[0];
                    string[] tokens = text.Split(new char[] {sep});
                    int[] values = new int[tokens.Length];
                    TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
                    for (int i = 0; i < values.Length; i++) {
                        // Note: ConvertFromString will raise exception if value cannot be converted.
                        values[i] = (int)intConverter.ConvertFromString(context, culture, tokens[i]);
                    }
                    
                    if (values.Length == 2) {
                        return new Point(values[0], values[1]);
                    }
                    else {
                        throw new ArgumentException(SR.Format(SR.TextParseFailedFormat,
                                                                  text,
                                                                  "x, y"));
                    }
                }
            }
            
            return base.ConvertFrom(context, culture, value);
        }

        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.ConvertTo"]/*' />
        /// <devdoc>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the desitnation type, this will
        ///      throw a NotSupportedException.
        /// </devdoc>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == null) {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if(value is Point){
                if (destinationType == typeof(string)) {
                    Point pt = (Point)value;
                    
                    if (culture == null) {
                        culture = CultureInfo.CurrentCulture;
                    }
                    string sep = culture.TextInfo.ListSeparator + " ";
                    TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
                    string[] args = new string[2];
                    int nArg = 0;
                    
                    // Note: ConvertToString will raise exception if value cannot be converted.
                    args[nArg++] = intConverter.ConvertToString(context, culture, pt.X);
                    args[nArg++] = intConverter.ConvertToString(context, culture, pt.Y);
                    
                    return string.Join(sep, args);
                }
                if (destinationType == typeof(InstanceDescriptor)) {
                    Point pt = (Point)value;

                    ConstructorInfo ctor = typeof(Point).GetConstructor(new Type[] {typeof(int), typeof(int)});
                    if (ctor != null) {
                        return new InstanceDescriptor(ctor, new object[] {pt.X, pt.Y});
                    }
                }
            }
            
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.CreateInstance"]/*' />
        /// <devdoc>
        ///      Creates an instance of this type given a set of property values
        ///      for the object.  This is useful for objects that are immutable, but still
        ///      want to provide changable properties.
        /// </devdoc>
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]        
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues) {
            if( propertyValues == null ) {
                throw new ArgumentNullException( nameof(propertyValues) );
            }

            object x = propertyValues["X"];
            object y = propertyValues["Y"];

            if(x == null || y == null || 
                !(x is int) || !(y is int)) {
                throw new ArgumentException(SR.PropertyValueInvalidEntry);
            }
    
            
            return new Point((int)x,
                              (int)y);
            
        }

        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.GetCreateInstanceSupported"]/*' />
        /// <devdoc>
        ///      Determines if changing a value on this object should require a call to
        ///      CreateInstance to create a new value.
        /// </devdoc>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) {
            return true;
        }
       
        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.GetProperties"]/*' />
        /// <devdoc>
        ///      Retrieves the set of properties for this type.  By default, a type has
        ///      does not return any properties.  An easy implementation of this method
        ///      can just call TypeDescriptor.GetProperties for the correct data type.
        /// </devdoc>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(Point), attributes);
            return props.Sort(new string[] {"X", "Y"});
        }

       
        /// <include file='doc\PointConverter.uex' path='docs/doc[@for="PointConverter.GetPropertiesSupported"]/*' />
        /// <devdoc>
        ///      Determines if this object supports properties.  By default, this
        ///      is false.
        /// </devdoc>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) {
            return true;
        }
        
    }
}

