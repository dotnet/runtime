// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace System.Resources.Extensions.Tests
{
    public class MyResourceTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(byte[]))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(byte[]))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is byte[] bytes)
            {
                return new MyResourceType(bytes);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(byte[]) && value is MyResourceType myResourceType)
            {
                return myResourceType.Data;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverter(typeof(MyResourceTypeConverter))]
    public class MyResourceType
    {
        public MyResourceType(byte[] data)
        {
            Data = data;
        }

        public MyResourceType(Stream stream)
        {
            Data = new byte[stream.Length];
            stream.Read(Data, 0, Data.Length);
        }

        public byte[] Data { get; }

        public override bool Equals(object obj)
        {
            if (obj is MyResourceType myResourceType)
            {
                return Data.AsSpan().SequenceEqual(myResourceType.Data);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (Data?.Length > 0)
            {
                // we don't care about collisions, this is a test.
                return Data[0].GetHashCode();
            }

            return 0;
        }
    }
}
