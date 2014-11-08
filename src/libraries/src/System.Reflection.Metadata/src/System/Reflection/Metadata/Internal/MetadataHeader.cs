// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Ecma335
{
    internal struct MetadataHeader
    {
        internal uint Signature;
        internal ushort MajorVersion;
        internal ushort MinorVersion;
        internal uint ExtraData;
        internal int VersionStringSize;
        internal string VersionString;
    }
}