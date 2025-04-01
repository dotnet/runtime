// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BinaryFormatTests.FormatterTests;

internal static class PlatformExtensions
{
    public static int GetPlatformIndex(this TypeSerializableValue[] blobs)
    {
        List<TypeSerializableValue> blobList = [.. blobs];
        int index;

        // Check if a specialized blob for >=netcoreapp3.0 is present and return if found.
        index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netcoreapp30);
        if (index >= 0)
        {
            return index;
        }

        // Check if a specialized blob for netcoreapp2.1 is present and return if found.
        index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netcoreapp21);
        if (index >= 0)
        {
            return index;
        }

        // If no newer blob for >=netcoreapp2.1 is present use existing one.
        // If no netcoreapp blob is present then -1 will be returned.
        return blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netcoreapp20);
    }
}
