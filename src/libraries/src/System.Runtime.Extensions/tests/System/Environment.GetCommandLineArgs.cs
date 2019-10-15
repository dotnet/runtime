// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    [ActiveIssue("https://github.com/dotnet/corefx/issues/21413", TargetFrameworkMonikers.Uap)]
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

        [Theory]
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
                    Assert.True(false, "Unexpected number of args passed to test");
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
    }
}
