// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides a type converter to convert <see cref='System.Array'/>
    /// objects to and from various other representations.</para>
    /// </summary>
    public class ArrayConverter : CollectionConverter
    {
        /// <summary>
        ///    <para>Converts the given value object to the specified destination type.</para>
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string) && value is Array)
            {
                return SR.Format(SR.Array, value.GetType().Name);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        ///    <para>Gets a collection of properties for the type of array specified by the value parameter.</para>
        /// </summary>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            if (value == null)
            {
                return null;
            }

            PropertyDescriptor[] props = null;

            if (value.GetType().IsArray)
            {
                Array valueArray = (Array)value;
                int length = valueArray.GetLength(0);
                props = new PropertyDescriptor[length];

                Type arrayType = value.GetType();
                Type elementType = arrayType.GetElementType();

                for (int i = 0; i < length; i++)
                {
                    props[i] = new ArrayPropertyDescriptor(arrayType, elementType, i);
                }
            }

            return new PropertyDescriptorCollection(props);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this object supports properties.</para>
        /// </summary>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private class ArrayPropertyDescriptor : SimplePropertyDescriptor
        {
            private readonly int _index;

            public ArrayPropertyDescriptor(Type arrayType, Type elementType, int index)
                : base(arrayType, "[" + index + "]", elementType, null)
            {
                _index = index;
            }

            public override object GetValue(object instance)
            {
                var array = instance as Array;
                if (array != null && array.GetLength(0) > _index)
                {
                    return array.GetValue(_index);
                }

                return null;
            }

            public override void SetValue(object instance, object value)
            {
                if (instance is Array)
                {
                    Array array = (Array)instance;
                    if (array.GetLength(0) > _index)
                    {
                        array.SetValue(value, _index);
                    }

                    OnValueChanged(instance, EventArgs.Empty);
                }
            }
        }
    }
}
