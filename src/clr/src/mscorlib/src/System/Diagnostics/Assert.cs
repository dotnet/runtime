// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    // Class which handles code asserts.  Asserts are used to explicitly protect
    // assumptions made in the code.  In general if an assert fails, it indicates 
    // a program bug so is immediately called to the attention of the user.
    // Only static data members, does not need to be marked with the serializable attribute
    internal static class Assert
    {
        internal const int COR_E_FAILFAST = unchecked((int)0x80131623);
        private static AssertFilter Filter;

        static Assert()
        {
            Filter = new DefaultFilter();
        }

        // Called when an assertion is being made.
        //
        internal static void Check(bool condition, String conditionString, String message)
        {
            if (!condition)
            {
                Fail(conditionString, message, null, COR_E_FAILFAST);
            }
        }

        internal static void Fail(String conditionString, String message)
        {
            Fail(conditionString, message, null, COR_E_FAILFAST);
        }

        internal static void Fail(String conditionString, String message, String windowTitle, int exitCode)
        {
            Fail(conditionString, message, windowTitle, exitCode, StackTrace.TraceFormat.Normal, 0);
        }

        internal static void Fail(String conditionString, String message, int exitCode, StackTrace.TraceFormat stackTraceFormat)
        {
            Fail(conditionString, message, null, exitCode, stackTraceFormat, 0);
        }

        internal static void Fail(String conditionString, String message, String windowTitle, int exitCode, StackTrace.TraceFormat stackTraceFormat, int numStackFramesToSkip)
        {
            // get the stacktrace
            StackTrace st = new StackTrace(numStackFramesToSkip, true);

            AssertFilters iResult = Filter.AssertFailure(conditionString, message, st, stackTraceFormat, windowTitle);

            if (iResult == AssertFilters.FailDebug)
            {
                if (Debugger.IsAttached == true)
                    Debugger.Break();
                else
                {
                    if (Debugger.Launch() == false)
                    {
                        throw new InvalidOperationException(
                                SR.InvalidOperation_DebuggerLaunchFailed);
                    }
                }
            }
            else if (iResult == AssertFilters.FailTerminate)
            {
                // We want to exit the Silverlight application, after displaying a message.
                // Our best known way to emulate this is to exit the process with a known 
                // error code.  Jolt may not be prepared for an appdomain to be unloaded.
                Environment._Exit(exitCode);
            }
        }

        // Called when an assert happens.
        // windowTitle can be null.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern static int ShowDefaultAssertDialog(String conditionString, String message, String stackTrace, String windowTitle);
    }
}
