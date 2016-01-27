// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

internal sealed class InterceptStreamWriter : StreamWriter
{
    private readonly StreamWriter _wrappedWriter;

    public InterceptStreamWriter(Stream baseStream, StreamWriter wrappedWriter, Encoding encoding, int bufferSize, bool leaveOpen) :
        base(baseStream, encoding, bufferSize, leaveOpen)
    {
        _wrappedWriter = wrappedWriter;
    }

    public override void Write(string value)
    {
        base.Write(value);
        _wrappedWriter.Write(value);
    }

    public override void Write(char value)
    {
        base.Write(value);
        _wrappedWriter.Write(value);
    }
}
