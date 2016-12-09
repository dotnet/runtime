// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics
{
    /// <summary>Base class used for all tests that need to spawn a remote process.</summary>
    public abstract class RemoteExecutorTestBase : FileCleanupTestBase
    {
        /// <summary>The name of the test console app.</summary>
        protected const string TestConsoleApp = "RemoteExecutorConsoleApp.exe";
        /// <summary>The name of the CoreCLR host used to host the test console app.</summary>
        protected static readonly string HostRunnerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "CoreRun.exe" : "corerun";
        /// <summary>The absolute path to the host runner executable.</summary>
        protected static string HostRunner => Path.Combine(AppContext.BaseDirectory, HostRunnerName);

        /// <summary>A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.</summary>
        public const int FailWaitTimeoutMilliseconds = 30 * 1000;
        /// <summary>The exit code returned when the test process exits successfully.</summary>
        internal const int SuccessExitCode = 42;

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<int> method, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(method.GetMethodInfo(), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<Task<int>> method,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(method.GetMethodInfo(), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, int> method, 
            string arg, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, int> method, 
            string arg1, string arg2, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, int> method, 
            string arg1, string arg2, string arg3, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvokeRaw(Delegate method, string unparsedArg,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(method.GetMethodInfo(), new[] { unparsedArg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        /// <param name="psi">The ProcessStartInfo to use, or null for a default.</param>
        private static RemoteInvokeHandle RemoteInvoke(MethodInfo method, string[] args, RemoteInvokeOptions options)
        {
            options = options ?? new RemoteInvokeOptions();

            // Verify the specified method is and that it returns an int (the exit code),
            // and that if it accepts any arguments, they're all strings.
            Assert.True(method.ReturnType == typeof(int) || method.ReturnType == typeof(Task<int>));
            Assert.All(method.GetParameters(), pi => Assert.Equal(typeof(string), pi.ParameterType));

            // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
            // that the method to invoke is available because we're already running in this assembly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;
            Assert.Equal(typeof(RemoteExecutorTestBase).GetTypeInfo().Assembly, a);

            // Start the other process and return a wrapper for it to handle its lifetime and exit checking.
            var psi = options.StartInfo;
            psi.UseShellExecute = false;

            if (!options.EnableProfiling)
            {
                // Profilers / code coverage tools doing coverage of the test process set environment
                // variables to tell the targeted process what profiler to load.  We don't want the child process 
                // to be profiled / have code coverage, so we remove these environment variables for that process 
                // before it's started.
                psi.Environment.Remove("Cor_Profiler");
                psi.Environment.Remove("Cor_Enable_Profiling");
                psi.Environment.Remove("CoreClr_Profiler");
                psi.Environment.Remove("CoreClr_Enable_Profiling");
            }

            // If we need the host (if it exists), use it, otherwise target the console app directly.
            string testConsoleAppArgs = "\"" + a.FullName + "\" " + t.FullName + " " + method.Name + " " + string.Join(" ", args);
            if (File.Exists(HostRunner))
            {
                psi.FileName = HostRunner;
                psi.Arguments = TestConsoleApp + " " + testConsoleAppArgs;
            }
            else
            {
                psi.FileName = TestConsoleApp;
                psi.Arguments = testConsoleAppArgs;
            }

            // Return the handle to the process, which may or not be started
            return new RemoteInvokeHandle(options.Start ?
                Process.Start(psi) :
                new Process() { StartInfo = psi }, options);
        }

        /// <summary>A cleanup handle to the Process created for the remote invocation.</summary>
        internal sealed class RemoteInvokeHandle : IDisposable
        {
            public RemoteInvokeHandle(Process process, RemoteInvokeOptions options)
            {
                Process = process;
                Options = options;
            }

            public Process Process { get; private set; }
            public RemoteInvokeOptions Options { get; private set; }

            public void Dispose()
            {
                if (Process != null)
                {
                    // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
                    // needing to do this in every derived test and keep each test much simpler.
                    try
                    {
                        Assert.True(Process.WaitForExit(Options.TimeOut));
                        if (Options.CheckExitCode)
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

    /// <summary>Options used with RemoteInvoke.</summary>
    internal sealed class RemoteInvokeOptions
    {
        public bool Start { get; set; } = true;
        public ProcessStartInfo StartInfo { get; set; } = new ProcessStartInfo();
        public bool EnableProfiling { get; set; } = true;
        public bool CheckExitCode {get; set; } = true;

        public int TimeOut {get; set; } = RemoteExecutorTestBase.FailWaitTimeoutMilliseconds;
    }
}
