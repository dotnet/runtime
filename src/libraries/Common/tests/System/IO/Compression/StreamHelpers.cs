// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

public static partial class StreamHelpers
{
    public static async Task<Stream> CreateTempCopyStream(String path)
    {
        var bytes = File.ReadAllBytes(path);

        var ms = new MemoryStream();
        await ms.WriteAsync(bytes, 0, bytes.Length);
        ms.Position = 0;

        return ms;
    }
}
