// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

public class LocalMemoryStream : MemoryStream
{
    // Call this to create a new stream based on the bytes of the current one
    // It creates a temporary stream because it may already be disposed
    public LocalMemoryStream Clone()
    {
        var ms = new MemoryStream(this.ToArray());
        var local = new LocalMemoryStream();
        ms.CopyTo(local);
        return local;
    }

    public static async Task<LocalMemoryStream> readAppFileAsync(String testFile)
    {
        var baseStream = await StreamHelpers.CreateTempCopyStream(testFile);
        var ms = new LocalMemoryStream();
        await baseStream.CopyToAsync(ms);

        ms.Position = 0;
        return ms;
    }

    public void SetCanRead(bool CanRead) { _canRead = CanRead; }
    private bool? _canRead = null;
    public override bool CanRead
    {
        get
        {
            return _canRead ?? base.CanRead;
        }
    }


    public void SetCanWrite(bool CanWrite) { _canWrite = CanWrite; }
    private bool? _canWrite = null;
    public override bool CanWrite
    {
        get
        {
            return _canWrite ?? base.CanWrite;
        }
    }

    public void SetCanSeek(bool CanSeek) { _canSeek = CanSeek; }
    private bool? _canSeek = null;
    public override bool CanSeek
    {
        get
        {
            return _canSeek ?? base.CanSeek;
        }
    }
}
