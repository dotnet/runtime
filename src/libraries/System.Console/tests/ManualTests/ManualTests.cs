// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System
{
    public class ConsoleManualTests
    {
        public static bool ManualTestsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MANUAL_TESTS"));

        [ConditionalTheory(nameof(ManualTestsEnabled))]
        [InlineData(false)]
        [InlineData(true)]
        public static void ReadLine(bool consoleIn)
        {
            string expectedLine = $"This is a test of Console.{(consoleIn ? "In." : "")}ReadLine.";
            Console.WriteLine($"Please type the sentence (without the quotes): \"{expectedLine}\"");
            string result = consoleIn ? Console.In.ReadLine() : Console.ReadLine();
            Assert.Equal(expectedLine, result);
            AssertUserExpectedResults("the characters you typed properly echoed as you typed");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void ReadLine_BackSpaceCanMoveAccrossWrappedLines()
        {
            Console.WriteLine("Please press 'a' until it wraps to the next terminal line, then press 'Backspace' until the input is erased, and then type a single 'a' and press 'Enter'.");
            Console.Write("Input: ");
            Console.Out.Flush();

            string result = Console.ReadLine();
            Assert.Equal("a", result);
            AssertUserExpectedResults("the previous line is 'Input: a'");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void InPeek()
        {
            Console.WriteLine("Please type \"peek\" (without the quotes). You should see it as you type:");
            foreach (char c in new[] { 'p', 'e', 'e', 'k' })
            {
                Assert.Equal(c, Console.In.Peek());
                Assert.Equal(c, Console.In.Peek());
                Assert.Equal(c, Console.In.Read());
            }
            Console.In.ReadLine(); // enter
            AssertUserExpectedResults("the characters you typed properly echoed as you typed");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void Beep()
        {
            Console.Beep();
            AssertUserExpectedResults("hear a beep");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void ReadKey()
        {
            Console.WriteLine("Please type \"console\" (without the quotes). You shouldn't see it as you type:");
            foreach (ConsoleKey k in new[] { ConsoleKey.C, ConsoleKey.O, ConsoleKey.N, ConsoleKey.S, ConsoleKey.O, ConsoleKey.L, ConsoleKey.E })
            {
                Assert.Equal(k, Console.ReadKey(intercept: true).Key);
            }
            AssertUserExpectedResults("\"console\" correctly not echoed as you typed it");
        }

        [ConditionalTheory(nameof(ManualTestsEnabled))]
        [MemberData(nameof(GetKeyChords))]
        public static void ReadKey_KeyChords(ConsoleKeyInfo expected)
        {
            Console.Write($"Please type key chord {RenderKeyChord(expected)}: ");
            var actual = Console.ReadKey(intercept: true);
            Console.WriteLine();

            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.Modifiers, actual.Modifiers);
            Assert.Equal(expected.KeyChar, actual.KeyChar);

            static string RenderKeyChord(ConsoleKeyInfo key)
            {
                string modifiers = "";
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control)) modifiers += "Ctrl+";
                if (key.Modifiers.HasFlag(ConsoleModifiers.Alt)) modifiers += "Alt+";
                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)) modifiers += "Shift+";
                return modifiers + key.Key;
            }
        }

        public static IEnumerable<object[]> GetKeyChords()
        {
            yield return MkConsoleKeyInfo('\x01', ConsoleKey.A, ConsoleModifiers.Control);
            yield return MkConsoleKeyInfo('\x01', ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Alt);
            yield return MkConsoleKeyInfo('\r', ConsoleKey.Enter, (ConsoleModifiers)0);

            if (OperatingSystem.IsWindows())
            {
                // windows will report '\n' as 'Ctrl+Enter', which is typically not picked up by Unix terminals
                yield return MkConsoleKeyInfo('\n', ConsoleKey.Enter, ConsoleModifiers.Control);
            }
            else
            {
                yield return MkConsoleKeyInfo('\n', ConsoleKey.J, ConsoleModifiers.Control);
            }

            static object[] MkConsoleKeyInfo (char keyChar, ConsoleKey consoleKey, ConsoleModifiers modifiers)
            {
                return new object[]
                {
                    new ConsoleKeyInfo(keyChar, consoleKey, 
                        control: modifiers.HasFlag(ConsoleModifiers.Control),
                        alt: modifiers.HasFlag(ConsoleModifiers.Alt),
                        shift: modifiers.HasFlag(ConsoleModifiers.Shift))
                };
            }
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void OpenStandardInput()
        {
            Console.WriteLine("Please type \"console\" (without the quotes). You shouldn't see it as you type:");
            var stream = Console.OpenStandardInput();
            var textReader = new System.IO.StreamReader(stream);
            var result = textReader.ReadLine();

            Assert.Equal("console", result);
            AssertUserExpectedResults("\"console\" correctly not echoed as you typed it");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void ConsoleOutWriteLine()
        {
            Console.Out.WriteLine("abcdefghijklmnopqrstuvwxyz");
            AssertUserExpectedResults("the alphabet above");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void KeyAvailable()
        {
            Console.WriteLine("Wait a few seconds, then press any key...");
            while (Console.KeyAvailable)
            {
                Console.ReadKey();
            }
            while (!Console.KeyAvailable)
            {
                Task.Delay(500).Wait();
                Console.WriteLine("\t...waiting...");
            }
            Console.ReadKey();
            AssertUserExpectedResults("several wait messages get printed out");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void Clear()
        {
            Console.Clear();
            AssertUserExpectedResults("the screen get cleared");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void Colors()
        {
            const int squareSize = 20;
            var colors = new[] { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Yellow };
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < squareSize / 2; i++)
                {
                    Console.WriteLine();
                    Console.Write("  ");
                    for (int col = 0; col < 2; col++)
                    {
                        Console.BackgroundColor = colors[row * 2 + col];
                        Console.ForegroundColor = colors[row * 2 + col];
                        for (int j = 0; j < squareSize; j++) Console.Write('@');
                        Console.ResetColor();
                    }
                }
            }
            Console.WriteLine();

            AssertUserExpectedResults("a Microsoft flag in solid color");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void CursorPositionAndArrowKeys()
        {
            Console.WriteLine("Use the up, down, left, and right arrow keys to move around.  When done, press enter.");

            while (true)
            {
                ConsoleKeyInfo k = Console.ReadKey(intercept: true);
                if (k.Key == ConsoleKey.Enter)
                {
                    break;
                }

                int left = Console.CursorLeft, top = Console.CursorTop;
                switch (k.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (top > 0) Console.CursorTop = top - 1;
                        break;
                    case ConsoleKey.LeftArrow:
                        if (left > 0) Console.CursorLeft = left - 1;
                        break;
                    case ConsoleKey.RightArrow:
                        Console.CursorLeft = left + 1;
                        break;
                    case ConsoleKey.DownArrow:
                        Console.CursorTop = top + 1;
                        break;
                }
            }

            AssertUserExpectedResults("the arrow keys move around the screen as expected with no other bad artificts");
        }

        [ConditionalFact(nameof(ManualTestsEnabled))]
        public static void EncodingTest()
        {
            Console.WriteLine(Console.OutputEncoding);
            Console.WriteLine("'\u03A0\u03A3'.");
            AssertUserExpectedResults("Pi and Segma or question marks");
        }

        private static void AssertUserExpectedResults(string expected)
        {
            Console.Write($"Did you see {expected}? [y/n] ");
            ConsoleKeyInfo info = Console.ReadKey();
            Console.WriteLine();

            switch (info.Key)
            {
                case ConsoleKey.Y or ConsoleKey.N:
                    Assert.Equal(ConsoleKey.Y, info.Key);
                    break;

                default:
                    AssertUserExpectedResults(expected);
                    break;
            };
        }
    }
}
