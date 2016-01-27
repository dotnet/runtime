// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {
    using System;
    using System.Security.Permissions;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

    // Class which handles code asserts.  Asserts are used to explicitly protect
    // assumptions made in the code.  In general if an assert fails, it indicates 
    // a program bug so is immediately called to the attention of the user.
    // Only static data members, does not need to be marked with the serializable attribute
    internal static class Assert
    {
        internal const int COR_E_FAILFAST = unchecked((int) 0x80131623);
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
                Fail (conditionString, message, null, COR_E_FAILFAST);
            }
        }

        internal static void Check(bool condition, String conditionString, String message, int exitCode)
        {
            if (!condition)
            {
                Fail(conditionString, message, null, exitCode);
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static void Fail(String conditionString, String message, String windowTitle, int exitCode, StackTrace.TraceFormat stackTraceFormat, int numStackFramesToSkip)
        {
            // get the stacktrace
            StackTrace st = new StackTrace(numStackFramesToSkip, true);
       
            AssertFilters iResult = Filter.AssertFailure (conditionString, message, st, stackTraceFormat, windowTitle);
    
            if (iResult == AssertFilters.FailDebug)
            {
                if (Debugger.IsAttached == true)
                    Debugger.Break();
                else
                {
                    if (Debugger.Launch() == false)
                    {
                        throw new InvalidOperationException(
                                Environment.GetResourceString("InvalidOperation_DebuggerLaunchFailed"));
                    }                        
                }   
            }
            else if (iResult == AssertFilters.FailTerminate)
            {
#if FEATURE_CORECLR
                // We want to exit the Silverlight application, after displaying a message.
                // Our best known way to emulate this is to exit the process with a known 
                // error code.  Jolt may not be prepared for an appdomain to be unloaded.
                Environment._Exit(exitCode);
#else
                // This assert dialog will be common for code contract failures.  If a code contract failure
                // occurs on an end user machine, we believe the right experience is to do a FailFast, which 
                // will report this error via Watson, so someone could theoretically fix the bug.
                // However, in CLR v4, Environment.FailFast when a debugger is attached gives you an MDA
                // saying you've hit a bug in the runtime or unsafe managed code, and this is most likely caused
                // by heap corruption or a stack imbalance from COM Interop or P/Invoke.  That extremely
                // misleading error isn't right, and we can temporarily work around this by using Environment.Exit
                // if a debugger is attached.  The right fix is to plumb FailFast correctly through our native
                // Watson code, adding in a TypeOfReportedError for fatal managed errors.
                if (Debugger.IsAttached)
                    Environment._Exit(exitCode);
                else
                    Environment.FailFast(message, unchecked((uint) exitCode));
#endif
            }
        }
    
      // Called when an assert happens.
      // windowTitle can be null.
      [System.Security.SecurityCritical]  // auto-generated
      [MethodImplAttribute(MethodImplOptions.InternalCall)]
      internal extern static int ShowDefaultAssertDialog(String conditionString, String message, String stackTrace, String windowTitle);
    }
}
