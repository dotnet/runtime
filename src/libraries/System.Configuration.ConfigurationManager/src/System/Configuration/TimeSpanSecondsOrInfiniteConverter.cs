// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace System.Configuration
{
    public sealed class TimeSpanSecondsOrInfiniteConverter : TimeSpanSecondsConverter
    {
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            ValidateType(value, typeof(TimeSpan));

            return (TimeSpan)value == TimeSpan.MaxValue ? "Infinite" : base.ConvertTo(ctx, ci, value, type);
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            return (string)data == "Infinite" ? TimeSpan.MaxValue : base.ConvertFrom(ctx, ci, data);
        }
    }
}
