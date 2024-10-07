// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    internal enum HashType : byte
    {
        None,
        SHA1,
        SHA256,
        SHA256Truncated,
        SHA384,
        SHA512,
    }
}
