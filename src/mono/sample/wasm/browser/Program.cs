// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sample
{
    public partial class Test
    {
        public static Task<int> Main(string[] args)
        {
            Console.WriteLine("Hello from WASM!");
            Console.WriteLine($"Arguments: {string.Join(", ", args)}");
            if (args.Length > 0 && args[0] == "M4-root")
            {
                int depth = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 3;
                try
                {
                    int result = DisplayMeaning(depth);
                    if (result != depth + 1)
                        throw new InvalidOperationException($"Root import nesting returned {result} for {depth}.");
                }
                catch (JSException exception) when (args.Length > 2 && args[2] == "catch-capacity"
                    && exception.Message.Contains("queue is full", StringComparison.Ordinal))
                {
                    Console.WriteLine("Root import capacity error recovered.");
                }
                Console.WriteLine("Root import nesting completed.");
                return Task.FromResult(0);
            }
            for (int value = 0; value < 64; value++)
            {
                int result = DisplayMeaning(value);
                if (result != value + 1)
                    throw new InvalidOperationException($"UI import returned {result} for {value}.");
            }
            try
            {
                DisplayMeaning(-1);
                throw new InvalidOperationException("Expected a JSException from the UI import.");
            }
            catch (JSException exception) when (exception.Message.Contains("M2 import failure", StringComparison.Ordinal))
            {
                Console.WriteLine("Expected UI import exception received.");
            }
            if (DisplayMeaning(-2) != 0 || DisplayMeaning(-3) != 0)
                throw new InvalidOperationException("Null and undefined UI results must marshal to zero.");
            if (DisplayMeaning(42) != 43)
                throw new InvalidOperationException("UI import did not recover after an exception.");
            Console.WriteLine("UI import round trips completed.");
            return Task.FromResult(0);
        }

        [JSImport("Sample.Test.displayMeaning", "main.js")]
        internal static partial int DisplayMeaning(int meaning);

        [JSImport("Sample.Test.readMeaningAsync", "main.js")]
        internal static partial Task<int> ReadMeaningAsync(int meaning);

        [JSExport]
        internal static Task<int> TaskValue(int value)
        {
            return value switch
            {
                -1 => throw new InvalidOperationException("M5 synchronous Task failure"),
                -2 => null!,
                -3 => Task.FromException<int>(new InvalidOperationException("M5 asynchronous Task failure")),
                _ => Task.FromResult(checked(value + 1))
            };
        }

        [JSExport]
        internal static async Task<int> DelayedValue(int value)
        {
            await Task.Delay(10);
            if (value == -1)
                throw new InvalidOperationException("M5 delayed Task failure");

            return checked(value + 1);
        }

        [JSExport]
        internal static Task<int> TaskThroughImport(int value)
        {
            return ReadMeaningAsync(value);
        }

        [JSExport]
        internal static Task<int> TaskSyncPrefix(int value)
        {
            return Task.FromResult(DisplayMeaning(value));
        }

        [JSExport]
        internal static int Increment(int value)
        {
            if (value == -1)
                throw new InvalidOperationException("M3 export failure");
            return checked(value + 1);
        }

        [JSExport]
        internal static int ThroughImport(int value)
        {
            return DisplayMeaning(value);
        }

        [JSExport]
        internal static async Task PrintMeaning(Task<int> meaningPromise)
        {
            Console.WriteLine("Meaning of life is " + await meaningPromise);
        }
    }
}
