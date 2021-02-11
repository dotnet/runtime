// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
