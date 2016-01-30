// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.ConsoleTests
{
    /// <summary>
    /// Perf tests for Console are chosen based on which functions have PAL code. They are:
    /// 
    /// - OpenStandardInput, OpenStandardOutput, OpenStandardError
    /// - ForegroundColor, BackgroundColor, ResetColor
    /// </summary>
    public class Perf_Console
    {
        [Benchmark]
        public void OpenStandardInput()
        {
            // We preserve copies of each opened Stream so that the perf area doesn't 
            // cover the disposal of those streams.
            const int innerIterations = 50;
            Stream[] streams = new Stream[innerIterations * 4];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        streams[0 + i * 4] = Console.OpenStandardInput(); streams[1 + i * 4] = Console.OpenStandardInput();
                        streams[2 + i * 4] = Console.OpenStandardInput(); streams[3 + i * 4] = Console.OpenStandardInput();
                    }
                }
                foreach (Stream s in streams)
                    s.Dispose();
            }
        }

        [Benchmark]
        public void OpenStandardOutput()
        {
            // We preserve copies of each opened Stream so that the perf area doesn't 
            // cover the disposal of those streams.
            const int innerIterations = 50;
            Stream[] streams = new Stream[innerIterations * 4];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        streams[0 + i * 4] = Console.OpenStandardOutput(); streams[1 + i * 4] = Console.OpenStandardOutput();
                        streams[2 + i * 4] = Console.OpenStandardOutput(); streams[3 + i * 4] = Console.OpenStandardOutput();
                    }
                }
                foreach (Stream s in streams)
                    s.Dispose();
            }
        }

        [Benchmark]
        public void OpenStandardError()
        {
            // We preserve copies of each opened Stream so that the perf area doesn't 
            // cover the disposal of those streams.
            const int innerIterations = 50;
            Stream[] streams = new Stream[innerIterations * 4];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        streams[0 + i * 4] = Console.OpenStandardError(); streams[1 + i * 4] = Console.OpenStandardError();
                        streams[2 + i * 4] = Console.OpenStandardError(); streams[3 + i * 4] = Console.OpenStandardError();
                    }
                }
                foreach (Stream s in streams)
                    s.Dispose();
            }
        }

        [Benchmark]
        public void ForegroundColor()
        {
            const int innerIterations = 1000;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Console.ForegroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.Cyan; Console.ForegroundColor = ConsoleColor.DarkBlue;
                        Console.ForegroundColor = ConsoleColor.DarkGray; Console.ForegroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.DarkGreen; Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            Console.ResetColor();
        }

        [Benchmark]
        public void BackgroundColor()
        {
            const int innerIterations = 1000;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Console.BackgroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Blue;
                        Console.BackgroundColor = ConsoleColor.Cyan; Console.BackgroundColor = ConsoleColor.DarkBlue;
                        Console.BackgroundColor = ConsoleColor.DarkGray; Console.BackgroundColor = ConsoleColor.Red;
                        Console.BackgroundColor = ConsoleColor.DarkGreen; Console.BackgroundColor = ConsoleColor.White;
                    }
                }
            }
            Console.ResetColor();
}

        [Benchmark]
        public void ResetColor()
        {
            const int innerIterations = 1000;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.DarkRed; Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.ResetColor();
                    }
                }
            }
        }
    }
}
