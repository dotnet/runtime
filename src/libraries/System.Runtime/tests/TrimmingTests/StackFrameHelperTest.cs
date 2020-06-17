using System;
using System.Diagnostics;

class Program
{
    static int Main(string[] args)
    {
        // StackTrace constructor calls CaptureStackTrace method which in turn will call
        // StackFrameHelper.InitializeSourceInfo which has annotations as it will require
        // StackTraceSymbols.ctor and StackTraceSymbols.GetSourceLineInfo
        StackTrace thisTrace = new StackTrace();
        return 100;
    }
}
