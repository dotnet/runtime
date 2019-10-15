// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Runtime.Serialization.Formatters.Tests
{
    public readonly struct TypeSerializableValue
    {
        public readonly string Base64Blob;

        // This is the minimum version, when the blob changed.
        public readonly TargetFrameworkMoniker Platform;

        public TypeSerializableValue(string base64Blob, TargetFrameworkMoniker platform)
        {
            Base64Blob = base64Blob;
            Platform = platform;
        }
    }
}
