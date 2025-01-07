// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

class Program
{
    static int Main(string[] args)
    {
        // StackTrace constructor calls CaptureStackTrace method which in turn will call
        // StackFrameHelper.InitializeSourceInfo which has annotations as it will require
        // StackTraceSymbols.ctor and StackTraceSymbols.GetSourceLineInfo
        StackTrace thisTrace = new StackTrace(fNeedFileInfo: true);
        // Validate that the stackTrace indeed contains source line info for the last frame of the stack.
        // If something failed in GetSourceLineInfo, then GetFileLineNumber
        // will return 0
        int lineNumber = thisTrace.GetFrame(thisTrace.FrameCount - 1).GetFileLineNumber();
        if (lineNumber == 0)
            return -1;
        return 100;
    }
}
