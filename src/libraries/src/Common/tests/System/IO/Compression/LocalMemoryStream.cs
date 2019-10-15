// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

public class LocalMemoryStream : MemoryStream
{
    // Call this to create a new stream based on the bytes of the current one
    // It creates a temporary stream because it may already be disposed
    public LocalMemoryStream Clone()
    {
        var ms = new MemoryStream(ToArray());
        var local = new LocalMemoryStream();
        ms.CopyTo(local);
        return local;
    }

    public static async Task<LocalMemoryStream> readAppFileAsync(string testFile)
    {
        var baseStream = await StreamHelpers.CreateTempCopyStream(testFile);
        var ms = new LocalMemoryStream();
        await baseStream.CopyToAsync(ms);

        ms.Position = 0;
        return ms;
    }

    public void SetCanRead(bool CanRead) { _canRead = CanRead; }
    private bool? _canRead = null;
    public override bool CanRead => _canRead ?? base.CanRead;

    public void SetCanWrite(bool CanWrite) { _canWrite = CanWrite; }
    private bool? _canWrite = null;
    public override bool CanWrite => _canWrite ?? base.CanWrite;

    public void SetCanSeek(bool CanSeek) { _canSeek = CanSeek; }
    private bool? _canSeek = null;
    public override bool CanSeek => _canSeek ?? base.CanSeek;
}
