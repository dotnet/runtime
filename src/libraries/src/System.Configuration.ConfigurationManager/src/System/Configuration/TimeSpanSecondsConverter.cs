// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.ComponentModel;

namespace System.Configuration
{
    public class TimeSpanSecondsConverter : ConfigurationConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            ValidateType(value, typeof(TimeSpan));

            long data = (long)((TimeSpan)value).TotalSeconds;

            return data.ToString(CultureInfo.InvariantCulture);
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            long min;
            try
            {
                min = long.Parse((string)data, CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new ArgumentException(SR.Converter_timespan_not_in_second);
            }
            return TimeSpan.FromSeconds(min);
        }
    }
}