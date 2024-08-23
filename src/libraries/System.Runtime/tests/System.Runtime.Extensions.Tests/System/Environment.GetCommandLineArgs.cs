// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class GetCommandLineArgs
    {
        public static IEnumerable<object[]> GetCommandLineArgs_TestData()
        {
            yield return new object[] { new string[] { "singleArg" } };
            yield return new object[] { new string[] { "Arg1", "Arg2" } };
            yield return new object[] { new string[] { "\"Arg With Quotes\"" } };
            yield return new object[] { new string[] { "\"Arg1 With Quotes\"", "\"Arg2 With Quotes\"" } };
            yield return new object[] { new string[] { "\"Arg1 With Quotes\"", "Arg2", "\"Arg3 With Quotes\"" } };
            yield return new object[] { new string[] { "arg1", @"\\\\\" + "\"alpha", @"\" + "\"arg3" } };
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(GetCommandLineArgs_TestData))]
        public void GetCommandLineArgs_Invoke_ReturnsExpected(string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    RemoteExecutor.Invoke((arg) => CheckCommandLineArgs(new string[] { arg }), args[0]).Dispose();
                    break;

                case 2:
                    RemoteExecutor.Invoke((arg1, arg2) => CheckCommandLineArgs(new string[] { arg1, arg2 }), args[0], args[1]).Dispose();
                    break;

                case 3:
                    RemoteExecutor.Invoke((arg1, arg2, arg3) => CheckCommandLineArgs(new string[] { arg1, arg2, arg3 }), args[0], args[1], args[2]).Dispose();
                    break;

                default:
                    Assert.Fail("Unexpected number of args passed to test");
                    break;
            }

        }

        public static int CheckCommandLineArgs(string[] args)
        {
            string[] cmdLineArgs = Environment.GetCommandLineArgs();

            Assert.InRange(cmdLineArgs.Length, 5, int.MaxValue); /*AppName, AssemblyName, TypeName, MethodName, ExceptionFile */
            Assert.Contains(RemoteExecutor.Path, cmdLineArgs[0]); /*The host returns the fullName*/

            Type t = typeof(GetCommandLineArgs);
            MethodInfo mi = t.GetMethod("CheckCommandLineArgs");
            Assembly a = t.GetTypeInfo().Assembly;

            Assert.Equal(cmdLineArgs[1], a.FullName);
            Assert.Contains(t.FullName, cmdLineArgs[2]);
            Assert.Contains("GetCommandLineArgs_Invoke_ReturnsExpected", cmdLineArgs[3]);

            // Check the arguments sent to the method.
            Assert.Equal(args.Length, cmdLineArgs.Length - 5);
            for (int i = 0; i < args.Length; i++)
            {
                Assert.Equal(args[i], cmdLineArgs[i + 5]);
            }

            return RemoteExecutor.SuccessExitCode;
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetCommandLineArgs_Fallback_Returns()
        {
            if (PlatformDetection.IsNotMonoRuntime
                && PlatformDetection.IsNotNativeAot
                && PlatformDetection.IsWindows)
            {
                // Currently fallback command line is only implemented on Windows coreclr
                RemoteExecutor.Invoke(CheckCommandLineArgsFallback).Dispose();
            }
        }

        public static int CheckCommandLineArgsFallback()
        {
            string[] oldArgs = Environment.GetCommandLineArgs();

            // Clear the command line args set for managed entry point
            var field = typeof(Environment).GetField("s_commandLineArgs", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(null, null);

            string[] args = Environment.GetCommandLineArgs();
            Assert.NotEmpty(args);

            // The native command line should be superset of managed command line
            foreach (string arg in oldArgs)
            {
                Assert.Contains(arg, args);
            }

            return RemoteExecutor.SuccessExitCode;
        }

        public static bool IsWindowsCoreCLRJit
            => PlatformDetection.IsWindows
            && PlatformDetection.IsNotMonoRuntime
            && PlatformDetection.IsNotNativeAot;

        [ConditionalTheory(typeof(GetCommandLineArgs), nameof(IsWindowsCoreCLRJit))]
        [InlineData(@"cmd ""abc"" d e", new[] { "cmd", "abc", "d", "e" })]
        [InlineData(@"cmd a\\b d""e f""g h", new[] { "cmd", @"a\\b", "de fg", "h" })]
        [InlineData(@"cmd a\\\""b c d", new[] { "cmd", @"a\""b", "c", "d" })]
        [InlineData(@"cmd a\\\\""b c"" d e", new[] { "cmd", @"a\\b c", "d", "e" })]
        [InlineData(@"cmd a""b"""" c d", new[] { "cmd", @"ab"" c d" })]
        [InlineData(@"X:\No""t A"""" P""ath arg", new[] { @"X:\Not A Path", "arg" })]
        [InlineData(@"""\\Some Server\cmd"" ""arg", new[] { @"\\Some Server\cmd", "arg" })]
        public static unsafe void CheckCommandLineParser(string cmdLine, string[] args)
        {
            var method = typeof(Environment).GetMethod("SegmentCommandLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var span = cmdLine.AsSpan(); // Workaround
            fixed (char* p = span)
            {
                Assert.Equal(args, method.Invoke(null, new object[] { Pointer.Box(p, typeof(char*)) }));
            }
        }
    }
}
