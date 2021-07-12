// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Data
{
    internal sealed class DataTableTypeConverter : ReferenceConverter
    {
        public DataTableTypeConverter() : base(typeof(DataTable)) { }
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => false;
    }
}
