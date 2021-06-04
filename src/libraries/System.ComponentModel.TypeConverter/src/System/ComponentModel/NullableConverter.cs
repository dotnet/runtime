// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// TypeConverter to convert Nullable types to and from strings or the underlying simple type.
    /// </summary>
    public class NullableConverter : TypeConverter
    {
        private static readonly ConstructorInfo s_nullableConstructor = typeof(Nullable<>).GetConstructor(typeof(Nullable<>).GetGenericArguments())!;

        /// <summary>
        /// Nullable converter is initialized with the underlying simple type.
        /// </summary>
        [RequiresUnreferencedCode("The UnderlyingType cannot be statically discovered.")]
        public NullableConverter(Type type)
        {
            NullableType = type;

            UnderlyingType = Nullable.GetUnderlyingType(type);
            if (UnderlyingType == null)
            {
                throw new ArgumentException(SR.NullableConverterBadCtorArg, nameof(type));
            }

            UnderlyingTypeConverter = TypeDescriptor.GetConverter(UnderlyingType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the
        /// given source type to the underlying simple type or a null.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == UnderlyingType)
            {
                return true;
            }
            else if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.CanConvertFrom(context, sourceType);
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given value to the converter's underlying simple type or a null.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null || value.GetType() == UnderlyingType)
            {
                return value;
            }
            else if (value is string && string.IsNullOrEmpty(value as string))
            {
                return null;
            }
            else if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.ConvertFrom(context, culture, value);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert a value object to the destination type.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == UnderlyingType)
            {
                return true;
            }
            else if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            else if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.CanConvertTo(context, destinationType);
            }

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value object to the destination type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == UnderlyingType && value != null && NullableType.IsInstanceOfType(value))
            {
                return value;
            }
            else if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = (ConstructorInfo)NullableType.GetMemberWithSameMetadataDefinitionAs(s_nullableConstructor);
                Debug.Assert(ci != null, "Couldn't find constructor");
                return new InstanceDescriptor(ci, new object[] { value }, true);
            }
            else if (value == null)
            {
                // Handle our own nulls here
                if (destinationType == typeof(string))
                {
                    return string.Empty;
                }
            }
            else if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.ConvertTo(context, culture, value, destinationType);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// </summary>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (UnderlyingTypeConverter != null)
            {
                object instance = UnderlyingTypeConverter.CreateInstance(context, propertyValues);
                return instance;
            }

            return base.CreateInstance(context, propertyValues);
        }

        /// <summary>
        /// Gets a value indicating whether changing a value on this object requires a call to
        /// <see cref='System.ComponentModel.TypeConverter.CreateInstance(IDictionary)'/> to create a new value,
        /// using the specified context.
        /// </summary>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.GetCreateInstanceSupported(context);
            }

            return base.GetCreateInstanceSupported(context);
        }

        /// <summary>
        /// Gets a collection of properties for the type of array specified by the value
        /// parameter using the specified context and attributes.
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (UnderlyingTypeConverter != null)
            {
                object unwrappedValue = value;
                return UnderlyingTypeConverter.GetProperties(context, unwrappedValue, attributes);
            }

            return base.GetProperties(context, value, attributes);
        }

        /// <summary>
        /// Gets a value indicating whether this object supports properties using the specified context.
        /// </summary>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.GetPropertiesSupported(context);
            }

            return base.GetPropertiesSupported(context);
        }

        /// <summary>
        /// Gets a collection of standard values for the data type this type converter is designed for.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (UnderlyingTypeConverter != null)
            {
                StandardValuesCollection values = UnderlyingTypeConverter.GetStandardValues(context);
                if (GetStandardValuesSupported(context) && values != null)
                {
                    // Create a set of standard values around nullable instances.
                    object[] wrappedValues = new object[values.Count + 1];
                    int idx = 0;

                    wrappedValues[idx++] = null;
                    foreach (object value in values)
                    {
                        wrappedValues[idx++] = value;
                    }

                    return new StandardValuesCollection(wrappedValues);
                }
            }

            return base.GetStandardValues(context);
        }

        /// <summary>
        /// Gets a value indicating whether the collection of standard values returned from
        /// <see cref='System.ComponentModel.TypeConverter.GetStandardValues()'/> is an exclusive
        /// list of possible values, using the specified context.
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.GetStandardValuesExclusive(context);
            }

            return base.GetStandardValuesExclusive(context);
        }

        /// <summary>
        /// Gets a value indicating whether this object supports a standard set of values that can
        /// be picked from a list using the specified context.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            if (UnderlyingTypeConverter != null)
            {
                return UnderlyingTypeConverter.GetStandardValuesSupported(context);
            }

            return base.GetStandardValuesSupported(context);
        }

        /// <summary>
        /// Gets a value indicating whether the given value object is valid for this type.
        /// </summary>
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            if (UnderlyingTypeConverter != null)
            {
                object unwrappedValue = value;
                if (unwrappedValue == null)
                {
                    return true; // null is valid for nullable.
                }
                else
                {
                    return UnderlyingTypeConverter.IsValid(context, unwrappedValue);
                }
            }

            return base.IsValid(context, value);
        }

        /// <summary>
        /// The type this converter was initialized with.
        /// </summary>
        public Type NullableType { get; }

        /// <summary>
        /// The simple type that is represented as a nullable.
        /// </summary>
        public Type UnderlyingType { get; }

        /// <summary>
        /// Converter associated with the underlying simple type.
        /// </summary>
        public TypeConverter UnderlyingTypeConverter { get; }
    }
}
