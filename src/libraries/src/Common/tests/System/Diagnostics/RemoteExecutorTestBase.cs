// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
using Xunit;

namespace System.Diagnostics
{
    /// <summary>Base class used for all tests that need to spawn a remote process.</summary>
    public abstract class RemoteExecutorTestBase : FileCleanupTestBase
    {
        /// <summary>The CoreCLR host used to host the test console app.</summary>
        protected const string HostRunner = "corerun";
        /// <summary>The name of the test console app.</summary>
        protected const string TestConsoleApp = "RemoteExecutorConsoleApp.exe";

        /// <summary>A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.</summary>
        internal const int FailWaitTimeoutMilliseconds = 30 * 1000;
        /// <summary>The exit code returned when the test process exits successfully.</summary>
        internal const int SuccessExitCode = 42;

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<int> method, 
            bool start = true)
        {
            return RemoteInvoke(method.GetMethodInfo(), Array.Empty<string>(), start);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, int> method, 
            string arg, 
            bool start = true)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { arg }, start);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, int> method, 
            string arg1, string arg2, 
            bool start = true)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { arg1, arg2 }, start);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, int> method, 
            string arg1, string arg2, string arg3, 
            bool start = true)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { arg1, arg2, arg3 }, start);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        private static RemoteInvokeHandle RemoteInvoke(MethodInfo method, string[] args, bool start)
        {
            // Verify the specified method is and that it returns an int (the exit code),
            // and that if it accepts any arguments, they're all strings.
            Assert.Equal(typeof(int), method.ReturnType);
            Assert.All(method.GetParameters(), pi => Assert.Equal(typeof(string), pi.ParameterType));

            // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
            // that the method to invoke is available because we're already running in this assembly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;
            Assert.Equal(typeof(RemoteExecutorTestBase).GetTypeInfo().Assembly, a);

            // Start the other process and return a wrapper for it to handle its lifetime and exit checking.
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = HostRunner,
                Arguments = TestConsoleApp + " \"" + a.FullName + "\" " + t.FullName + " " + method.Name + " " + string.Join(" ", args),
                UseShellExecute = false
            };

            // Profilers / code coverage tools doing coverage of the test process set environment
            // variables to tell the targeted process what profiler to load.  We don't want the child process 
            // to be profiled / have code coverage, so we remove these environment variables for that process 
            // before it's started.
            psi.Environment.Remove("Cor_Profiler");
            psi.Environment.Remove("Cor_Enable_Profiling");
            psi.Environment.Remove("CoreClr_Profiler");
            psi.Environment.Remove("CoreClr_Enable_Profiling");

            // Return the handle to the process, which may or not be started
            return new RemoteInvokeHandle(start ?
                Process.Start(psi) :
                new Process() { StartInfo = psi });
        }

        /// <summary>A cleanup handle to the Process created for the remote invocation.</summary>
        internal sealed class RemoteInvokeHandle : IDisposable
        {
            public RemoteInvokeHandle(Process process)
            {
                Process = process;
            }

            public Process Process { get; private set; }

            public void Dispose()
            {
                if (Process != null)
                {
                    // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
                    // needing to do this in every derived test and keep each test much simpler.
                    try
                    {
                        Assert.True(Process.WaitForExit(FailWaitTimeoutMilliseconds));
                        Assert.Equal(SuccessExitCode, Process.ExitCode);
                    }
                    finally
                    {
                        // Cleanup
                        try { Process.Kill(); }
                        catch { } // ignore all cleanup errors

                        Process.Dispose();
                        Process = null;
                    }
                }
            }
        }
    }
}
