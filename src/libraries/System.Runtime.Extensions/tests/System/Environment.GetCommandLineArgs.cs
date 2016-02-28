// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public class GetCommandLineArgs : RemoteExecutorTestBase
    {
        [Fact]
        public void CheckCommandLineArgs_OneArg()
        {
            // 1. Check if passing no additional args work.
            RemoteInvoke(new string[] { "singleArg" });
        }

        [Fact]
        public void CheckCommandLineArgs_MultipleArgs()
        {
            // 1. Check if passing no additional args work.
            RemoteInvoke(new string[] { "Arg1", "Arg2" });
        }

        [Fact]
        public void CheckCommandLineArgs_SingleArgWithQuotes()
        {
            // 1. Check if passing no additional args work.
            RemoteInvoke(new string[] { "\"Arg With Quotes\"" });
        }

        [Fact]
        public void CheckCommandLineArgs_MultipleArgsWithQuotes()
        {
            // 1. Check if passing no additional args work.
            RemoteInvoke(new string[] { "\"Arg1 With Quotes\"", "\"Arg2 With Quotes\"" });
        }

        [Fact]
        public void CheckCommandLineArgs_MixOfQuotedUnquotedArgs()
        {
            // 1. Check if passing no additional args work.
            RemoteInvoke(new string[] { "\"Arg1 With Quotes\"", "Arg2", "\"Arg3 With Quotes\"" });
        }

        [Fact]
        public void CheckCommandLineArgs_ArgsWithEvenBackSlash()
        {
            //1.Check if passing no additional args work.
            RemoteInvoke(new string[] { "arg1", @"\\\\\" + "\"alpha", @"\" + "\"arg3" });
        }

        public static void RemoteInvoke(string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    RemoteInvoke((arg) => { return CheckCommandLineArgs(new string[] { arg }); }, args[0]);
                    break;

                case 2:
                    RemoteInvoke((arg1, arg2) => { return CheckCommandLineArgs(new string[] { arg1, arg2 }); }, args[0], args[1]);
                    break;

                case 3:
                    RemoteInvoke((arg1, arg2, arg3) => { return CheckCommandLineArgs(new string[] { arg1, arg2, arg3 }); }, args[0], args[1], args[2]);
                    break;
            }

        }

        public static int CheckCommandLineArgs(string[] args)
        {

            string[] cmdLineArgs = Environment.GetCommandLineArgs();

            Assert.InRange(cmdLineArgs.Length, 4, int.MaxValue); /*AppName, AssemblyName, TypeName, MethodName*/
            Assert.True(cmdLineArgs[0].Contains(TestConsoleApp)); /*The host returns the fullName*/


            Type t = typeof(GetCommandLineArgs);
            MethodInfo mi = t.GetMethod("CheckCommandLineArgs");
            Assembly a = t.GetTypeInfo().Assembly;

            Assert.Equal(cmdLineArgs[1], a.FullName);
            Assert.True(cmdLineArgs[2].Contains(t.FullName));
            Assert.True(cmdLineArgs[3].Contains("RemoteInvoke"));

            // Check the arguments sent to the method.
            Assert.True(cmdLineArgs.Length - 4 == args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                Assert.Equal(args[i], cmdLineArgs[i + 4]);
            }

            return SuccessExitCode;
        }
    }
}
