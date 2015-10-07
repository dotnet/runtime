// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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