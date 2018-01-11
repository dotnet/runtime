// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tests
{
    /// <summary>
    /// This class is used as interop between uap and netfx app running in the same app container.
    /// On uap code running here will be running on netfx.
    /// This is a workaround for a limitation of uap RemoteInvoke which does not give us much control over the process.
    /// </summary>
    internal static class RemotelyInvokable
    {
        public static readonly int SuccessExitCode = 42;
        public const int WaitInMS = 30 * 1000;
        public const string TestConsoleApp = "System.Diagnostics.Process.Tests";
        public static event EventHandler ClosedEvent;


        public static string DummyUapCmd()
        {
            return $@"exit {SuccessExitCode}";
        }

        public static int Dummy()
        {
            return SuccessExitCode;
        }

        public static int LongWait()
        {
            Thread.Sleep(WaitInMS);
            return SuccessExitCode;
        }

        public static string ExitWithCodeUapCmd(string exitCodeStr)
        {
            return $@"exit {exitCodeStr}";
        }

        public static int ExitWithCode(string exitCodeStr)
        {
            return int.Parse(exitCodeStr);
        }

        public static string ErrorProcessBodyUapCmd()
        {
            return $"(echo {TestConsoleApp} started error stream) 1>&2 & " +
                $"(echo {TestConsoleApp} closed error stream) 1>&2 & " +
                $"exit {SuccessExitCode}";
        }

        public static int ErrorProcessBody()
        {
            Console.Error.WriteLine(TestConsoleApp + " started error stream");
            Console.Error.WriteLine(TestConsoleApp + " closed error stream");
            return SuccessExitCode;
        }

        public static string StreamBodyUapCmd()
        {
            return $"(echo {TestConsoleApp} started) & " +
                $"(echo {TestConsoleApp} closed) & " +
                $"exit {SuccessExitCode}";
        }

        public static int StreamBody()
        {
            Console.WriteLine(TestConsoleApp + " started");
            Console.WriteLine(TestConsoleApp + " closed");
            return SuccessExitCode;
        }

        public static string ReadLineUapCmd()
        {
            return "findstr -src:^..*$";
        }

        public static int ReadLine()
        {
            Console.ReadLine();
            return SuccessExitCode;
        }

        public static string WriteLineReadLineUapCmd()
        {
            return $"((echo Signal) && findstr -src:^Success$ && exit {SuccessExitCode}) || exit {SuccessExitCode + 1}";
        }

        public static int WriteLineReadLine()
        {
            Console.WriteLine("Signal");
            string line = Console.ReadLine();
            return line == "Success" ? SuccessExitCode : SuccessExitCode + 1;
        }

        public static string ReadLineWriteIfNullUapCmd()
        {
            return $"(((findstr -src:^..*$) && (echo NOT_NULL)) || (echo NULL)) & exit {SuccessExitCode}";
        }
        
        public static int ReadLineWriteIfNull()
        {
            string line = Console.ReadLine();
            Console.WriteLine(line == null ? "NULL" : "NOT_NULL");
            return SuccessExitCode;
        }

        public static int ReadLineWithCustomEncodingWriteLineWithUtf8(string inputEncoding)
        {
            string line;
            using (var inputReader = new StreamReader(Console.OpenStandardInput(), Encoding.GetEncoding(inputEncoding)))
            {
                line = inputReader.ReadLine();
            }

            using (var outputWriter = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8))
            {
                outputWriter.WriteLine(line);
            }

            return SuccessExitCode;
        }

        public static string WriteSlowlyByByteUapCmd()
        {
            throw new Exception("No simple way of doing this using cmd.exe");
        }

        public static int WriteSlowlyByByte()
        {
            var stdout = Console.OpenStandardOutput();
            var bytes = new byte[] { 97, 0 }; //Encoding.Unicode.GetBytes("a");

            for (int i = 0; i != bytes.Length; ++i)
            {
                stdout.WriteByte(bytes[i]);
                stdout.Flush();
                Thread.Sleep(100);
            }
            return SuccessExitCode;
        }

        public static string Write144LinesUapCmd()
        {
            return $"for /L %i in (1,1,144) do @echo %i";
        }

        public static int Write144Lines()
        {
            for (int i = 0; i < 144; i++)
            {
                Console.WriteLine("This is line #" + i + ".");
            }
            return SuccessExitCode;
        }

        public static int WriteLinesAfterClose()
        {
            ClosedEvent += (s, e) =>
            {
                Console.WriteLine("This is a line to output.");
                Console.Error.WriteLine("This is a line to error.");
            };
            return SuccessExitCode;
        }

        public static string WriteLinesAfterCloseUapCmd()
        {
            ClosedEvent += (s, e) =>
            {
                // Finish the pause
                Console.WriteLine();
            };
            return "(pause > nul) & (echo This is a line to output) & (echo This is a line to error 1>&2)";
        }

        public static string ConcatThreeArgumentsUapCmd(string one, string two, string three)
        {
            return $"echo {string.Join(",", one, two, three)} & exit {SuccessExitCode}";
        }

        public static int ConcatThreeArguments(string one, string two, string three)
        {
            Console.Write(string.Join(",", one, two, three));
            return SuccessExitCode;
        }

        public static string SelfTerminateUapCmd()
        {
            return $"exit 0";
        }

        public static int SelfTerminate()
        {
            Process.GetCurrentProcess().Kill();
            throw new ShouldNotBeInvokedException();
        }

        public static void FireClosedEvent()
        {
            ClosedEvent?.Invoke(null, EventArgs.Empty);
        }
    }
}
