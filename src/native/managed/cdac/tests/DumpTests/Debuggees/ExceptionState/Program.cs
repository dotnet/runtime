// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the Exception contract.
/// Creates a nested exception chain then crashes with FailFast.
/// </summary>
internal static class Program
{
    public class DebuggeeException : Exception
    {
        public DebuggeeException(string message) : base(message) { }
        public DebuggeeException(string message, Exception inner) : base(message, inner) { }
    }

    private static void Main()
    {
        Exception? caughtException;

        // Build a nested exception chain
        try
        {
            try
            {
                try
                {
                    throw new InvalidOperationException("innermost exception");
                }
                catch (Exception ex)
                {
                    throw new DebuggeeException("middle exception", ex);
                }
            }
            catch (Exception ex)
            {
                throw new DebuggeeException("outermost exception", ex);
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Keep the exception chain alive
        GC.KeepAlive(caughtException);

        Environment.FailFast("cDAC dump test: ExceptionState debuggee intentional crash");
    }
}
