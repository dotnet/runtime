// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;

namespace System.Data
{
    internal sealed class PrimaryKeyTypeConverter : ReferenceConverter
    {
        // converter classes should have public ctor
        public PrimaryKeyTypeConverter() : base(typeof(DataColumn[]))
        {
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => false;

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
            destinationType == typeof(string) ||
            base.CanConvertTo(context, destinationType);

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            return destinationType == typeof(string) ?
                Array.Empty<DataColumn>().GetType().Name :
                base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
