// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
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
        const int ThreadCount = 4;
        Thread[] threads = new Thread[ThreadCount * 2];
        ExceptionDispatchInfo[] exceptions = new ExceptionDispatchInfo[threads.Length];
        using Barrier loadBarrier = new Barrier(ThreadCount + 1);
        using Barrier firstWaveAccessBarrier = new Barrier(ThreadCount);
        using Barrier secondWaveAccessBarrier = new Barrier(ThreadCount);

        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    loadBarrier.SignalAndWait();
                    loadBarrier.SignalAndWait();
                    RunTest(threadIndex, 0);
                }
                catch (Exception ex)
                {
                    exceptions[threadIndex] = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    loadBarrier.RemoveParticipant();
                    firstWaveAccessBarrier.RemoveParticipant();
                }
            });
            threads[i].Start();
        }

        loadBarrier.SignalAndWait();

        ExceptionDispatchInfo loadException = null;
        try
        {
            Assert.Equal("TlsFieldLibrary", Assembly.Load("TlsFieldLibrary").GetName().Name);
        }
        catch (Exception ex)
        {
            loadException = ExceptionDispatchInfo.Capture(ex);
        }
        finally
        {
            loadBarrier.SignalAndWait();
        }

        if (loadException is null)
        {
            for (int i = 0; i < ThreadCount; i++)
            {
                int threadIndex = i;
                int exceptionIndex = ThreadCount + i;
                threads[exceptionIndex] = new Thread(() =>
                {
                    try
                    {
                        RunTest(threadIndex, 1);
                    }
                    catch (Exception ex)
                    {
                        exceptions[exceptionIndex] = ExceptionDispatchInfo.Capture(ex);
                    }
                    finally
                    {
                        secondWaveAccessBarrier.RemoveParticipant();
                    }
                });
                threads[exceptionIndex].Start();
            }
        }

        foreach (Thread thread in threads)
        {
            thread?.Join();
        }

        loadException?.Throw();

        foreach (ExceptionDispatchInfo exception in exceptions)
        {
            exception?.Throw();
        }

        void RunTest(int threadIndex, int wave)
        {
            Barrier accessBarrier = wave == 0 ? firstWaveAccessBarrier : secondWaveAccessBarrier;
            Assert.Equal(42, TlsField.Read());

            int firstValue = 123 + (wave * ThreadCount) + threadIndex;
            int secondValue = 456 + (wave * ThreadCount) + threadIndex;
            int thirdValue = 789 + (wave * ThreadCount) + threadIndex;

            TlsField.Write(firstValue);
            accessBarrier.SignalAndWait();
            Assert.Equal(firstValue, TlsField.Read());

            int* address = TlsField.GetAddress();
            Assert.Equal(firstValue, *address);

            *address = secondValue;
            accessBarrier.SignalAndWait();
            Assert.Equal(secondValue, TlsField.Read());

            TlsField.Write(thirdValue);
            accessBarrier.SignalAndWait();
            Assert.Equal(thirdValue, *address);
        }
    }
}
