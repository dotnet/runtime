// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

public static partial class StreamHelpers
{
    public static async Task<MemoryStream> CreateTempCopyStream(string path)
    {
        var bytes = File.ReadAllBytes(path);

        var ms = new MemoryStream();
        await ms.WriteAsync(bytes, 0, bytes.Length);
        ms.Position = 0;

        return ms;
    }
}
