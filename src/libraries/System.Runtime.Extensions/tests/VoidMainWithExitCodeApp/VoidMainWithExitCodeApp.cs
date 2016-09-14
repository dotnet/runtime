// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;

namespace VoidMainWithExitCodeApp
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            int exitCode = int.Parse(args[0]);
            int mode = int.Parse(args[1]);

            PropertyInfo set_ExitCode = typeof(Environment).GetTypeInfo().GetDeclaredProperty("ExitCode");
            MethodInfo Exit = typeof(Environment).GetTypeInfo().GetDeclaredMethod("Exit");

            switch (mode)
            {
                case 1: // set ExitCode and exit
                    set_ExitCode.SetValue(null, exitCode); // TODO: Environment.ExitCode = exitCode;
                    break;

                case 2: // set ExitCode, exit, and then set ExitCode from another foreground thread
                    new Thread(() => // foreground thread
                    {
                        Thread.Sleep(1000); // time for Main to exit
                        set_ExitCode.SetValue(null, exitCode); // TODO: Environment.ExitCode = exitCode;
                    }).Start();
                    set_ExitCode.SetValue(null, exitCode - 1); // TODO: Environment.ExitCode = exitCode - 1;
                    break;

                case 3: // call Environment.Exit(exitCode)
                    Exit.Invoke(null, new object[] { exitCode }); // TODO: Environment.Exit(exitCode);
                    break;
            }
        }
    }
}
