// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.ComponentModel;

namespace System.Configuration
{
    public sealed class InfiniteTimeSpanConverter : ConfigurationConverterBase
    {
        private static readonly TypeConverter s_timeSpanConverter = TypeDescriptor.GetConverter(typeof(TimeSpan));

        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            ValidateType(value, typeof(TimeSpan));

            return (TimeSpan)value == TimeSpan.MaxValue
                ? "Infinite"
                : s_timeSpanConverter.ConvertToInvariantString(value);
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            return (string)data == "Infinite"
                ? TimeSpan.MaxValue
                : s_timeSpanConverter.ConvertFromInvariantString((string)data);
        }
    }
}