// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

internal class BufferState
{
    internal const int Idle = 0;
    internal const int InUse = 1;

    private int currentState;
    
    internal BufferState()
    {
        this.currentState = Idle;
    }

    internal bool IsIdle
    {
        get { return this.currentState == Idle; }
    }
    
    internal bool EnterInUseState()
    {
        return this.TransitionState(Idle, InUse);
    }
    
    internal bool EnterIdleState()
    {
        return this.TransitionState(InUse, Idle);
    }
    
    private bool TransitionState(int expectedCurrentState, int desiredState)
    {
        if (Interlocked.CompareExchange(ref this.currentState,
                                        desiredState,
                                        expectedCurrentState) == expectedCurrentState)
        {
            return true;
        }
        
        return false;
    }
}

public class Program
{
    bool forceUpload;
    BufferState currentState;

    Program()
    {
        this.forceUpload = false;
        this.currentState = new BufferState();
        while(!this.currentState.EnterInUseState())
        {
            Console.WriteLine("Failed to enterInUseState");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void ThrowIfDisposed()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void QueueCurrentBufferForUploadAndSetNewBuffer()
    {
    }

    internal void Test()
    {
        this.ThrowIfDisposed();

        try
        {
            if (forceUpload == true)
            {
                // Queue the buffer for upload.
                this.QueueCurrentBufferForUploadAndSetNewBuffer();
            }
        }
        finally
        {
            // Always transition back to the idle state.
            this.currentState.EnterIdleState();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Program p = new Program();
        if (p.currentState.IsIdle)
        {
            Console.WriteLine("Failed! - 102");
            return 102;
        }

        p.Test();

        if (p.currentState.IsIdle)
        {
            Console.WriteLine("Passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed! - 101");
            return 101;
        }
    }
}
