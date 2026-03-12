// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the ExecutionManager EH clause enumeration.
/// The CrashInExceptionHandler method has a try/catch with filter, a typed catch,
/// a catch-all handler (catch without a type), and a finally block,
/// then calls FailFast from the finally to produce the dump.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        CrashInExceptionHandler();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CrashInExceptionHandler()
    {
        try
        {
            try
            {
                try
                {
                    throw new NotImplementedException("bad input");
                }
                catch (NotImplementedException ex) when (ex.Message.Contains("good"))
                {
                    Console.WriteLine($"caught: {ex.Message}");
                }
            }
            catch (NotImplementedException ex)
            {
                Console.WriteLine($"outer caught: {ex.Message}");
            }
        }
        catch
        {
            Console.WriteLine("catch-all handler");
        }
        finally
        {
            Environment.FailFast("cDAC dump test: ExceptionHandlingInfo debuggee intentional crash");
        }
    }
}
