using System.Diagnostics;

class Program
{
    static int Main(string[] args)
    {
        // StackTrace constructor calls CaptureStackTrace method which in turn will call
        // StackFrameHelper.InitializeSourceInfo which has annotations as it will require
        // StackTraceSymbols.ctor and StackTraceSymbols.GetSourceLineInfo
        StackTrace thisTrace = new StackTrace(true);
        // Validate that the stackTrace indeed contains source line info
        // If something failed in GetSourceLineInfo, then GetFileLineNumber
        // will return 0
        int lineNumber = thisTrace.GetFrame(0).GetFileLineNumber();
        if (lineNumber == 0)
            return -1;
        return 100;
    }
}
