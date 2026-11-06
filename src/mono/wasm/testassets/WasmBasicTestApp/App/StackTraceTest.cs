// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;

public partial class StackTraceTest
{
    [JSExport]
    public static void Run()
    {
        StackFrame frame = new StackTrace(true).GetFrame(0)!;
        TestOutput.WriteLine($"Source info: {frame.GetFileLineNumber() > 0}");
        TestOutput.WriteLine($"Method: {DiagnosticMethodInfo.Create(frame)?.Name}");
    }
}
