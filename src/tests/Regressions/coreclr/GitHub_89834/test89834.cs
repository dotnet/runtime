// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class test89834
{
    public static async Task<int> Main()
    {
        int status = 103;
        Console.WriteLine("boo");

        EventHandler<FirstChanceExceptionEventArgs> handler =
            (s, e) => FirstChanceExceptionCallback(e.Exception, ref status);

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            await ThrowAndCatchTaskCancellationExceptionAsync();
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        return status;
    }

    private static readonly ThreadLocal<bool> ReentrancyTracker = new();

    private static void FirstChanceExceptionCallback(Exception thrownException, ref int status)
    {
        if (ReentrancyTracker.Value)
            return;

        ReentrancyTracker.Value = true;

        Console.WriteLine("Begin Observing Exception: " + thrownException.GetType());

        status = ValidateExceptionStackFrame(thrownException);
        Console.WriteLine("End Observing Exception: " + thrownException.GetType());

        ReentrancyTracker.Value = false;
    }

    private static int ValidateExceptionStackFrame(Exception thrownException)
    {
        StackTrace exceptionStackTrace = new(thrownException, fNeedFileInfo: false);

        // The stack trace of thrown exceptions is populated as the exception unwinds the
        // stack. In the case of observing the exception from the FirstChanceException event,
        // there is only one frame on the stack (the throwing frame). In order to get the
        // full call stack of the exception, get the current call stack of the thread and
        // filter out the call frames that are "above" the exception's throwing frame.
        StackFrame throwingFrame = null;
        foreach (StackFrame stackFrame in exceptionStackTrace.GetFrames())
        {
            if (null != stackFrame.GetMethod())
            {
                throwingFrame = stackFrame;
                break;
            }
        }

        if (throwingFrame == null)
        {
            return 101;
        }

        Console.WriteLine($"Throwing Frame: [{throwingFrame.GetMethod().Name}, 0x{GetOffset(throwingFrame):X}]");

        StackTrace threadStackTrace = new(fNeedFileInfo: false);
        ReadOnlySpan<StackFrame> threadStackFrames = threadStackTrace.GetFrames();
        int index = 0;

        Console.WriteLine("Begin Checking Thread Frames:");
        while (index < threadStackFrames.Length)
        {
            StackFrame threadStackFrame = threadStackFrames[index];

            Console.WriteLine($"- [{threadStackFrame.GetMethod().Name}, 0x{GetOffset(threadStackFrame):X}]");

            if (throwingFrame.GetMethod() == threadStackFrame.GetMethod() &&
                GetOffset(throwingFrame) == GetOffset(threadStackFrame))
            {
                break;
            }

            index++;
        }
        Console.WriteLine("End Checking Thread Frames:");

        return (index != threadStackFrames.Length) ? 100 : 102;
    }

    private static int GetOffset(StackFrame stackFrame)
    {
        return stackFrame.GetILOffset();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task ThrowAndCatchTaskCancellationExceptionAsync()
    {
        using CancellationTokenSource source = new();
        CancellationToken token = source.Token;

        Task innerTask = Task.Run(
            () => Task.Delay(Timeout.InfiniteTimeSpan, token),
            token);

        try
        {
            source.Cancel();
            await innerTask;
        }
        catch (Exception)
        {
        }
    }
}
