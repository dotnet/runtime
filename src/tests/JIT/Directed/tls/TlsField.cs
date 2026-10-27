// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using TestLibrary;
using Xunit;

public static class TlsFieldTest
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static unsafe void ReadWriteAcrossMultipleThreads()
    {
        Thread[] threads = new Thread[4];
        ExceptionDispatchInfo[] exceptions = new ExceptionDispatchInfo[threads.Length];
        using Barrier barrier = new Barrier(threads.Length);

        for (int i = 0; i < threads.Length; i++)
        {
            int threadIndex = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    RunTest(123 + threadIndex, 456 + threadIndex, 789 + threadIndex);
                }
                catch (Exception ex)
                {
                    exceptions[threadIndex] = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    barrier.RemoveParticipant();
                }
            });
            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        foreach (ExceptionDispatchInfo exception in exceptions)
        {
            exception?.Throw();
        }

        void RunTest(int firstValue, int secondValue, int thirdValue)
        {
            Assert.Equal(42, TlsField.Read());

            TlsField.Write(firstValue);
            barrier.SignalAndWait();
            Assert.Equal(firstValue, TlsField.Read());

            int* address = TlsField.GetAddress();
            Assert.Equal(firstValue, *address);

            *address = secondValue;
            barrier.SignalAndWait();
            Assert.Equal(secondValue, TlsField.Read());

            TlsField.Write(thirdValue);
            barrier.SignalAndWait();
            Assert.Equal(thirdValue, *address);
        }
    }
}
