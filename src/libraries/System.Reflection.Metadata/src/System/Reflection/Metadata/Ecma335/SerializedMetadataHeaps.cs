// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335
{
    internal sealed class SerializedMetadata
    {
        internal readonly ImmutableArray<int> StringMap;
        internal readonly BlobBuilder StringHeap;
        internal readonly MetadataSizes Sizes;

        public SerializedMetadata(
            MetadataSizes sizes,
            BlobBuilder stringHeap,
            ImmutableArray<int> stringMap)
        {
            Sizes = sizes;
            StringHeap = stringHeap;
            StringMap = stringMap;
        }
    }
}
