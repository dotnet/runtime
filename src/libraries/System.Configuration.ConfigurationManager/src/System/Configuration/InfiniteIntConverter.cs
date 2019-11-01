// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Configuration
{
    public sealed class InfiniteIntConverter : ConfigurationConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            ValidateType(value, typeof(int));

            return (int)value == int.MaxValue ? "Infinite" : ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            Debug.Assert(data is string, "data is string");

            return (string)data == "Infinite" ? int.MaxValue : Convert.ToInt32((string)data, 10);
        }
    }
}