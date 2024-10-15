// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    internal abstract partial class NameFilter
    {
        public abstract bool Matches(ConstantStringValueHandle stringHandle, MetadataReader reader);
    }

    internal sealed partial class NameFilterCaseSensitive : NameFilter
    {
        public sealed override bool Matches(ConstantStringValueHandle stringHandle, MetadataReader reader) => stringHandle.StringEquals(ExpectedName, reader);
    }

    internal sealed partial class NameFilterCaseInsensitive : NameFilter
    {
        public sealed override bool Matches(ConstantStringValueHandle stringHandle, MetadataReader reader) => stringHandle.GetConstantStringValue(reader).Value.Equals(ExpectedName, StringComparison.OrdinalIgnoreCase);
    }
}
