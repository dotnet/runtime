// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Ecma335
{
    internal static class COR20Constants
    {
        internal const int SizeOfCorHeader = 72;
        internal const uint COR20MetadataSignature = 0x424A5342;
        internal const int MinimumSizeofMetadataHeader = 16;
        internal const int SizeofStorageHeader = 4;
        internal const int MinimumSizeofStreamHeader = 8;
        internal const string StringStreamName = "#Strings";
        internal const string BlobStreamName = "#Blob";
        internal const string GUIDStreamName = "#GUID";
        internal const string UserStringStreamName = "#US";
        internal const string CompressedMetadataTableStreamName = "#~";
        internal const string UncompressedMetadataTableStreamName = "#-";
        internal const string MinimalDeltaMetadataTableStreamName = "#JTD";
        internal const int LargeStreamHeapSize = 0x0001000;
    }
}