// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace XUnitWrapperLibrary;

public sealed class TestOutputRecorder : TextWriter
{
    private TextWriter _inner;
    private ThreadLocal<StringBuilder> _testOutput = new(() => new StringBuilder());

    public TestOutputRecorder(TextWriter inner)
    {
        _inner = inner;
    }

    public override void Write(char value)
    {
        _inner.Write(value);
        _testOutput.Value!.Append(value);
    }

    public override Encoding Encoding => _inner.Encoding;

    public void ResetTestOutput() => _testOutput.Value!.Clear();

    public string GetTestOutput() => _testOutput.Value!.ToString();
}
