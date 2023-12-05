// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace System.Configuration
{
    public class TimeSpanMinutesConverter : ConfigurationConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            ValidateType(value, typeof(TimeSpan));

            long data = (long)((TimeSpan)value).TotalMinutes;
            return data.ToString(CultureInfo.InvariantCulture);
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            long min = long.Parse((string)data, CultureInfo.InvariantCulture);
            return TimeSpan.FromMinutes(min);
        }
    }
}
