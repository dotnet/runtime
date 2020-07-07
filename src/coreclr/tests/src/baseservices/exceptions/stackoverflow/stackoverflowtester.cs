// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TestStackOverflow
{
    class Program
    {
        static string s_corerunPath;
        static string s_currentPath;

        static bool TestStackOverflow(string testName, string testArgs, out List<string> stderrLines)
        {
            Console.WriteLine($"Running {testName} test({testArgs})");
            List<string> lines = new List<string>();

            Process testProcess = new Process();

            testProcess.StartInfo.FileName = s_corerunPath;
            testProcess.StartInfo.Arguments = $"{Path.Combine(s_currentPath, "..", testName, $"{testName}.dll")} {testArgs}";
            testProcess.StartInfo.UseShellExecute = false;
            testProcess.StartInfo.RedirectStandardError = true;
            testProcess.ErrorDataReceived += (sender, line) => 
            {
                Console.WriteLine($"\"{line.Data}\"");
                if (!string.IsNullOrEmpty(line.Data))
                {
                    lines.Add(line.Data);
                }
            };

            testProcess.Start();
            testProcess.BeginErrorReadLine();
            testProcess.WaitForExit();
            testProcess.CancelErrorRead();

            stderrLines = lines;

            int expectedExitCode;
            if ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX))
            {
                expectedExitCode = 128 + 6;
            }
            else
            {
                expectedExitCode = unchecked((int)0xC00000FD);
            }

            if (testProcess.ExitCode != expectedExitCode)
            {
                Console.WriteLine($"Exit code: 0x{testProcess.ExitCode:X8}, expected 0x{expectedExitCode:X8}");
                return false;
            }

            if (lines[0] != "Stack overflow.")
            {
                Console.WriteLine("Missing \"Stack overflow.\" at the first line");
                return false;
            }

            return true;
        }

        static bool TestStackOverflowSmallFrameMainThread()
        {
            List<string> lines;
            if (TestStackOverflow("stackoverflow", "smallframe main", out lines))
            {
                if (!lines[lines.Count - 1].EndsWith("at TestStackOverflow.Program.Main(System.String[])"))
                {
                    Console.WriteLine("Missing \"Main\" method frame at the last line");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.Test(Boolean)")))
                {
                    Console.WriteLine("Missing \"Test\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionA\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionB()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionB\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionC()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionC\" method frame");
                    return false;
                }

                return true;
            }

            return false;
        }

        static bool TestStackOverflowLargeFrameMainThread()
        {
            List<string> lines;
            if (TestStackOverflow("stackoverflow", "largeframe main", out lines))
            {
                if (!lines[lines.Count - 1].EndsWith("at TestStackOverflow.Program.Main(System.String[])"))
                {
                    Console.WriteLine("Missing \"Main\" method frame at the last line");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.Test(Boolean)")))
                {
                    Console.WriteLine("Missing \"Test\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA2()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionA2\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionB2()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionB2\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionC2()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionC2\" method frame");
                    return false;
                }

                return true;
            }

            return false;
        }

        static bool TestStackOverflowSmallFrameSecondaryThread()
        {
            List<string> lines;
            if (TestStackOverflow("stackoverflow", "smallframe secondary", out lines))
            {
                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.Test(Boolean)")))
                {
                    Console.WriteLine("Missing \"TestStackOverflow.Program.Test\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionA\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionB()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionB\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionC()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionC\" method frame");
                    return false;
                }

                return true;
            }

            return false;
        }

        static bool TestStackOverflowLargeFrameSecondaryThread()
        {
            List<string> lines;
            if (TestStackOverflow("stackoverflow", "largeframe secondary", out lines))
            {
                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.Test(Boolean)")))
                {
                    Console.WriteLine("Missing \"TestStackOverflow.Program.Test\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow.Program.InfiniteRecursionA2()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionA2\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.InfiniteRecursionB2()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionB2\" method frame");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("TestStackOverflow.Program.InfiniteRecursionC2()")))
                {
                    Console.WriteLine("Missing \"InfiniteRecursionC2\" method frame");
                    return false;
                }

                return true;
            }

            return false;
        }

        static bool TestStackOverflow3()
        {
            List<string> lines;
            if (TestStackOverflow("stackoverflow3", "", out lines))
            {
                if (!lines[lines.Count - 1].EndsWith("at TestStackOverflow3.Program.Main()"))
                {
                    Console.WriteLine("Missing \"Main\" method frame at the last line");
                    return false;
                }

                if (!lines.Exists(elem => elem.EndsWith("at TestStackOverflow3.Program.Execute(System.String)")))
                {
                    Console.WriteLine("Missing \"Execute\" method frame");
                    return false;
                }

                return true;
            }

            return false;
        }

        static int Main(string[] args)
        {
            s_currentPath = Directory.GetCurrentDirectory();
            s_corerunPath = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "corerun");

            if (!TestStackOverflowSmallFrameMainThread())
            {
                return 101;
            }

            if (!TestStackOverflowLargeFrameMainThread())
            {
                return 102;
            }

            if (!TestStackOverflowSmallFrameSecondaryThread())
            {
                return 103;
            }

            if (!TestStackOverflowLargeFrameSecondaryThread())
            {
                return 104;
            }

            if (!TestStackOverflow3())
            {
                return 105;
            }

            return 100;
        }
    }
}
