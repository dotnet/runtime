// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.TypeLoading.Ecma;

namespace System.Reflection
{
    internal abstract partial class NameFilter
    {
        public abstract bool Matches(StringHandle stringHandle, MetadataReader reader);
    }

    internal sealed partial class NameFilterCaseSensitive : NameFilter
    {
        public sealed override bool Matches(StringHandle stringHandle, MetadataReader reader) => stringHandle.Equals(_expectedNameUtf8, reader);
    }

    internal sealed partial class NameFilterCaseInsensitive : NameFilter
    {
        public sealed override bool Matches(StringHandle stringHandle, MetadataReader reader) => reader.StringComparer.Equals(stringHandle, ExpectedName, true);
    }
}
