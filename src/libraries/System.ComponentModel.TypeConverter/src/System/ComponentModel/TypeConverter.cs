// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Converts the value of an object into a different data type.</para>
    /// </summary>
    public class TypeConverter
    {
        /// <summary>
        ///    <para>Gets a value indicating whether this converter can convert an object in the
        ///       given source type to the native type of the converter.</para>
        /// </summary>
        public bool CanConvertFrom(Type sourceType)
        {
            return CanConvertFrom(null, sourceType);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object in the given source type to the native type of the converter
        ///       using the context.</para>
        /// </summary>
        public virtual bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </summary>
        public bool CanConvertTo(Type destinationType)
        {
            return CanConvertTo(null, destinationType);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </summary>
        public virtual bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string));
        }

        /// <summary>
        ///    <para>Converts the given value
        ///       to the converter's native type.</para>
        /// </summary>
        public object ConvertFrom(object value)
        {
            return ConvertFrom(null, CultureInfo.CurrentCulture, value);
        }

        /// <summary>
        ///    <para>Converts the given object to the converter's native type.</para>
        /// </summary>
        public virtual object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            throw GetConvertFromException(value);
        }

        /// <summary>
        ///    Converts the given string to the converter's native type using the invariant culture.
        /// </summary>
        public object ConvertFromInvariantString(string text)
        {
            return ConvertFromString(null, CultureInfo.InvariantCulture, text);
        }

        /// <summary>
        ///    Converts the given string to the converter's native type using the invariant culture.
        /// </summary>
        public object ConvertFromInvariantString(ITypeDescriptorContext context, string text)
        {
            return ConvertFromString(context, CultureInfo.InvariantCulture, text);
        }

        /// <summary>
        ///    <para>Converts the specified text into an object.</para>
        /// </summary>
        public object ConvertFromString(string text)
        {
            return ConvertFrom(null, null, text);
        }

        /// <summary>
        ///    <para>Converts the specified text into an object.</para>
        /// </summary>
        public object ConvertFromString(ITypeDescriptorContext context, string text)
        {
            return ConvertFrom(context, CultureInfo.CurrentCulture, text);
        }

        /// <summary>
        ///    <para>Converts the specified text into an object.</para>
        /// </summary>
        public object ConvertFromString(ITypeDescriptorContext context, CultureInfo culture, string text)
        {
            return ConvertFrom(context, culture, text);
        }

        /// <summary>
        ///    <para>Converts the given
        ///       value object to the specified destination type using the arguments.</para>
        /// </summary>
        public object ConvertTo(object value, Type destinationType)
        {
            return ConvertTo(null, null, value, destinationType);
        }

        /// <summary>
        ///    <para>Converts the given value object to
        ///       the specified destination type using the specified context and arguments.</para>
        /// </summary>
        public virtual object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return string.Empty;
                }

                if (culture != null && culture != CultureInfo.CurrentCulture)
                {
                    IFormattable formattable = value as IFormattable;
                    if (formattable != null)
                    {
                        return formattable.ToString(/* format = */ null, /* formatProvider = */ culture);
                    }
                }
                return value.ToString();
            }
            throw GetConvertToException(value, destinationType);
        }

        /// <summary>
        ///    <para>Converts the specified value to a culture-invariant string representation.</para>
        /// </summary>
        public string ConvertToInvariantString(object value)
        {
            return ConvertToString(null, CultureInfo.InvariantCulture, value);
        }

        /// <summary>
        ///    <para>Converts the specified value to a culture-invariant string representation.</para>
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public string ConvertToInvariantString(ITypeDescriptorContext context, object value)
        {
            return ConvertToString(context, CultureInfo.InvariantCulture, value);
        }

        /// <summary>
        ///    <para>Converts the specified value to a string representation.</para>
        /// </summary>
        public string ConvertToString(object value)
        {
            return (string)ConvertTo(null, CultureInfo.CurrentCulture, value, typeof(string));
        }

        /// <summary>
        ///    <para>Converts the specified value to a string representation.</para>
        /// </summary>
        public string ConvertToString(ITypeDescriptorContext context, object value)
        {
            return (string)ConvertTo(context, CultureInfo.CurrentCulture, value, typeof(string));
        }

        /// <summary>
        ///    <para>Converts the specified value to a string representation.</para>
        /// </summary>
        public string ConvertToString(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return (string)ConvertTo(context, culture, value, typeof(string));
        }

        /// <summary>
        /// <para>Re-creates an <see cref='System.Object'/> given a set of property values for the object.</para>
        /// </summary>
        public object CreateInstance(IDictionary propertyValues)
        {
            return CreateInstance(null, propertyValues);
        }

        /// <summary>
        /// <para>Re-creates an <see cref='System.Object'/> given a set of property values for the object.</para>
        /// </summary>
        public virtual object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            return null;
        }

        /// <summary>
        ///    <para>Gets a suitable exception to throw when a conversion cannot be performed.</para>
        /// </summary>
        protected Exception GetConvertFromException(object value)
        {
            string valueTypeName;

            valueTypeName = value == null ? SR.Null : value.GetType().FullName;

            throw new NotSupportedException(SR.Format(SR.ConvertFromException, GetType().Name, valueTypeName));
        }

        /// <summary>
        ///    <para>Retrieves a suitable exception to throw when a conversion cannot
        ///       be performed.</para>
        /// </summary>
        protected Exception GetConvertToException(object value, Type destinationType)
        {
            string valueTypeName;

            valueTypeName = value == null ? SR.Null : value.GetType().FullName;

            throw new NotSupportedException(SR.Format(SR.ConvertToException, GetType().Name, valueTypeName, destinationType.FullName));
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether changing a value on this object requires a call to
        ///        <see cref='System.ComponentModel.TypeConverter.CreateInstance'/> to create a new value.
        ///    </para>
        /// </summary>
        public bool GetCreateInstanceSupported()
        {
            return GetCreateInstanceSupported(null);
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether changing a value on this object requires a call to
        ///        <see cref='System.ComponentModel.TypeConverter.CreateInstance'/> to create a new value,
        ///        using the specified context.
        ///    </para>
        /// </summary>
        public virtual bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return false;
        }

        /// <summary>
        ///    <para>Gets a collection of properties for the type of array specified by the value parameter.</para>
        /// </summary>
        public PropertyDescriptorCollection GetProperties(object value)
        {
            return GetProperties(null, value);
        }

        /// <summary>
        ///    <para>
        ///        Gets a collection of properties for the type of array specified by the value parameter using
        ///        the specified context.
        ///    </para>
        /// </summary>
        public PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value)
        {
            return GetProperties(context, value, new Attribute[] { BrowsableAttribute.Yes });
        }

        /// <summary>
        ///    <para>
        ///        Gets a collection of properties for the type of array specified by the value parameter using
        ///        the specified context and attributes.
        ///    </para>
        /// </summary>
        public virtual PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return null;
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this object supports properties.</para>
        /// </summary>
        public bool GetPropertiesSupported()
        {
            return GetPropertiesSupported(null);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this object supports properties using the specified context.</para>
        /// </summary>
        public virtual bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return false;
        }

        /// <summary>
        ///    <para>Gets a collection of standard values for the data type this type converter is designed for.</para>
        /// </summary>
        public ICollection GetStandardValues()
        {
            return GetStandardValues(null);
        }

        /// <summary>
        ///    <para>Gets a collection of standard values for the data type this type converter is designed for.</para>
        /// </summary>
        public virtual StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return null;
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether the collection of standard values returned from
        ///        <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive list.
        ///    </para>
        /// </summary>
        public bool GetStandardValuesExclusive()
        {
            return GetStandardValuesExclusive(null);
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether the collection of standard values returned from
        ///        <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> is an exclusive 
        ///        list of possible values, using the specified context.
        ///    </para>
        /// </summary>
        public virtual bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether this object supports a standard set of values
        ///       that can be picked from a list.
        ///    </para>
        /// </summary>
        public bool GetStandardValuesSupported()
        {
            return GetStandardValuesSupported(null);
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this object supports a standard set of values that can be picked
        ///       from a list using the specified context.
        ///    </para>
        /// </summary>
        public virtual bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }

        /// <summary>
        ///    <para>Gets a value indicating whether the given value object is valid for this type.</para>
        /// </summary>
        public bool IsValid(object value)
        {
            return IsValid(null, value);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether the given value object is valid for this type.</para>
        /// </summary>
        public virtual bool IsValid(ITypeDescriptorContext context, object value)
        {
            bool isValid = true;
            try
            {
                // Because null doesn't have a type, so we couldn't pass this to CanConvertFrom.
                // Meanwhile, we couldn't silence null value here, such as type converter like
                // NullableConverter would consider null value as a valid value.
                if (value == null || CanConvertFrom(context, value.GetType()))
                {
                    ConvertFrom(context, CultureInfo.InvariantCulture, value);
                }
                else
                {
                    isValid = false;
                }
            }
            catch
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        ///    <para>Sorts a collection of properties.</para>
        /// </summary>
        protected PropertyDescriptorCollection SortProperties(PropertyDescriptorCollection props, string[] names)
        {
            props.Sort(names);
            return props;
        }

        /// <summary>
        ///    <para>
        ///       An <see langword='abstract '/> class that provides properties for objects that do not have properties.
        ///    </para>
        /// </summary>
        protected abstract class SimplePropertyDescriptor : PropertyDescriptor
        {
            /// <summary>
            ///    <para>
            ///       Initializes a new instance of the <see cref='System.ComponentModel.TypeConverter.SimplePropertyDescriptor'/> class.
            ///    </para>
            /// </summary>
            protected SimplePropertyDescriptor(Type componentType, string name, Type propertyType) : this(componentType, name, propertyType, Array.Empty<Attribute>())
            {
            }

            /// <summary>
            ///    <para>
            ///       Initializes a new instance of the <see cref='System.ComponentModel.TypeConverter.SimplePropertyDescriptor'/> class.
            ///    </para>
            /// </summary>
            protected SimplePropertyDescriptor(Type componentType, string name, Type propertyType, Attribute[] attributes) : base(name, attributes)
            {
                ComponentType = componentType;
                PropertyType = propertyType;
            }

            /// <summary>
            ///    <para>Gets the type of the component this property description is bound to.</para>
            /// </summary>
            public override Type ComponentType { get; }

            /// <summary>
            ///    <para>Gets a value indicating whether this property is read-only.</para>
            /// </summary>
            public override bool IsReadOnly => Attributes.Contains(ReadOnlyAttribute.Yes);

            /// <summary>
            ///    <para>Gets the type of the property.</para>
            /// </summary>
            public override Type PropertyType { get; }

            /// <summary>
            ///    <para>
            ///        Gets a value indicating whether resetting the component will change the value of the component.
            ///    </para>
            /// </summary>
            public override bool CanResetValue(object component)
            {
                DefaultValueAttribute attr = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
                if (attr == null)
                {
                    return false;
                }
                return (attr.Value.Equals(GetValue(component)));
            }

            /// <summary>
            ///    <para>Resets the value for this property of the component.</para>
            /// </summary>
            public override void ResetValue(object component)
            {
                DefaultValueAttribute attr = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
                if (attr != null)
                {
                    SetValue(component, attr.Value);
                }
            }

            /// <summary>
            ///    <para>Gets a value indicating whether the value of this property needs to be persisted.</para>
            /// </summary>
            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        /// <summary>
        ///    <para>Represents a collection of values.</para>
        /// </summary>
        public class StandardValuesCollection : ICollection
        {
            private ICollection _values;
            private Array _valueArray;

            /// <summary>
            ///    <para>
            ///       Initializes a new instance of the <see cref='System.ComponentModel.TypeConverter.StandardValuesCollection'/> class.
            ///    </para>
            /// </summary>
            public StandardValuesCollection(ICollection values)
            {
                if (values == null)
                {
                    values = Array.Empty<object>();
                }

                Array a = values as Array;
                if (a != null)
                {
                    _valueArray = a;
                }

                _values = values;
            }

            /// <summary>
            ///    <para>
            ///       Gets the number of objects in the collection.
            ///    </para>
            /// </summary>
            public int Count
            {
                get
                {
                    if (_valueArray != null)
                    {
                        return _valueArray.Length;
                    }
                    else
                    {
                        return _values.Count;
                    }
                }
            }

            /// <summary>
            ///    <para>Gets the object at the specified index number.</para>
            /// </summary>
            public object this[int index]
            {
                get
                {
                    if (_valueArray != null)
                    {
                        return _valueArray.GetValue(index);
                    }
                    IList list = _values as IList;
                    if (list != null)
                    {
                        return list[index];
                    }
                    // No other choice but to enumerate the collection.
                    //
                    _valueArray = new object[_values.Count];
                    _values.CopyTo(_valueArray, 0);
                    return _valueArray.GetValue(index);
                }
            }

            /// <summary>
            ///    <para>
            ///        Copies the contents of this collection to an array.
            ///    </para>
            /// </summary>
            public void CopyTo(Array array, int index)
            {
                _values.CopyTo(array, index);
            }

            /// <summary>
            ///    <para>
            ///       Gets an enumerator for this collection.
            ///    </para>
            /// </summary>
            public IEnumerator GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            /// <internalonly/>
            /// <summary>
            /// Determines if this collection is synchronized. The ValidatorCollection is not synchronized for
            /// speed.  Also, since it is read-only, there is no need to synchronize it.
            /// </summary>
            bool ICollection.IsSynchronized => false;

            /// <internalonly/>
            /// <summary>
            /// Retrieves the synchronization root for this collection.  Because we are not synchronized,
            /// this returns null.
            /// </summary>
            object ICollection.SyncRoot => null;
        }
    }
}
