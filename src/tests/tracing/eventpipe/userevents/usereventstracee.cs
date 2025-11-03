// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Tracing.Tests.UserEvents
{
    public class UserEventsTracee
    {
        private static byte[] s_array;

        public static void Run()
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            long targetTicks = Stopwatch.Frequency * 10; // 10s

            while (Stopwatch.GetTimestamp() - startTimestamp < targetTicks)
            {
                for (int i = 0; i < 100; i++)
                {
                    s_array = new byte[1024 * 10];
                }

                GC.Collect();
                Thread.Sleep(100);
            }
        }
    }
}
