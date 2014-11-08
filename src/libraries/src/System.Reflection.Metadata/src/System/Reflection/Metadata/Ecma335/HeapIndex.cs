// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Ecma335
{
    public enum HeapIndex
    {
        UserString,
        String,
        Blob,
        Guid
    }

    internal static class HeapIndexExtensions
    {
        internal const int Count = (int)HeapIndex.Guid + 1;
    }
}
