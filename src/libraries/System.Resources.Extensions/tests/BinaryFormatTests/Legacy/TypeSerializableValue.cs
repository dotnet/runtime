// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BinaryFormatTests;

public readonly struct TypeSerializableValue
{
    public string Base64Blob { get; }

    // This is the minimum version, when the blob changed.
    public readonly TargetFrameworkMoniker Platform { get; }

    public TypeSerializableValue(string base64Blob, TargetFrameworkMoniker platform)
    {
        Base64Blob = base64Blob;
        Platform = platform;
    }
}
