// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Compression
{
    internal enum ZipVersionNeededValues : ushort { Default = 10, ExplicitDirectory = 20, Deflate = 20, Zip64 = 45 }
}
