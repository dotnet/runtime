// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace VoidMainWithExitCodeApp
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            int exitCode = args.Length > 0 ? int.Parse(args[0]) : 0;
            typeof(Environment).GetTypeInfo().GetDeclaredProperty("ExitCode").SetValue(null, exitCode);
            // Environment.ExitCode = exitCode; // TODO: Remove reflection when package updated with latest Environment exposing ExitCode
        }
    }
}
