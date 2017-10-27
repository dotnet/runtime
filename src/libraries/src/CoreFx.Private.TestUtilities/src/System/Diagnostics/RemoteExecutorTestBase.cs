// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace System.Diagnostics
{
    /// <summary>Base class used for all tests that need to spawn a remote process.</summary>
    public abstract partial class RemoteExecutorTestBase : FileCleanupTestBase
    {
        /// <summary>A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.</summary>
        public const int FailWaitTimeoutMilliseconds = 60 * 1000;
        /// <summary>The exit code returned when the test process exits successfully.</summary>
        public const int SuccessExitCode = 42;

        /// <summary>The name of the test console app.</summary>
        protected static readonly string TestConsoleApp = "RemoteExecutorConsoleApp.exe";

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Action method,
            RemoteInvokeOptions options = null)
        {
            // There's no exit code to check
            options = options ?? new RemoteInvokeOptions();
            options.CheckExitCode = false;

            return RemoteInvoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<int> method, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<Task<int>> method,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<string, Task<int>> method,
            string arg,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<string, int> method, 
            string arg, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, int> method, 
            string arg1, string arg2, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, int> method, 
            string arg1, string arg2, string arg3, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, string, int> method, 
            string arg1, string arg2, string arg3, string arg4, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="arg5">The fifth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, string, string, int> method, 
            string arg1, string arg2, string arg3, string arg4, string arg5, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4, arg5 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments without performing any modifications to the arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle RemoteInvokeRaw(Delegate method, string unparsedArg,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { unparsedArg }, options, pasteArguments: false);
        }

        private static MethodInfo GetMethodInfo(Delegate d)
        {
            // RemoteInvoke doesn't support marshaling state on classes associated with
            // the delegate supplied (often a display class of a lambda).  If such fields
            // are used, odd errors result, e.g. NullReferenceExceptions during the remote
            // execution.  Try to ward off the common cases by proactively failing early
            // if it looks like such fields are needed.
            if (d.Target != null)
            {
                // The only fields on the type should be compiler-defined (any fields of the compiler's own
                // making generally include '<' and '>', as those are invalid in C# source).  Note that this logic
                // may need to be revised in the future as the compiler changes, as this relies on the specifics of
                // actually how the compiler handles lifted fields for lambdas.
                Type targetType = d.Target.GetType();
                Assert.All(
                    targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                    fi => Assert.True(fi.Name.IndexOf('<') != -1, $"Field marshaling is not supported by {nameof(RemoteInvoke)}: {fi.Name}"));
            }

            return d.GetMethodInfo();
        }

        /// <summary>A cleanup handle to the Process created for the remote invocation.</summary>
        public sealed class RemoteInvokeHandle : IDisposable
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
                        Assert.True(Process.WaitForExit(Options.TimeOut),
                            $"Timed out after {Options.TimeOut}ms waiting for remote process {Process.Id}");

                        if (File.Exists(Options.ExceptionFile))
                        {
                            throw new RemoteExecutionException(File.ReadAllText(Options.ExceptionFile));
                        }

                        if (Options.CheckExitCode)
                        {
                            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Options.ExpectedExitCode : unchecked((sbyte)Options.ExpectedExitCode);
                            int actual = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Process.ExitCode : unchecked((sbyte)Process.ExitCode);

                            Assert.True(expected == actual, $"Exit code was {Process.ExitCode} but it should have been {Options.ExpectedExitCode}");
                        }
                    }
                    finally
                    {
                        if (File.Exists(Options.ExceptionFile))
                        {
                            File.Delete(Options.ExceptionFile);
                        }

                        // Cleanup
                        try { Process.Kill(); }
                        catch { } // ignore all cleanup errors

                        Process.Dispose();
                        Process = null;
                    }
                }
            }

            private sealed class RemoteExecutionException : XunitException
            {
                internal RemoteExecutionException(string stackTrace) : base("Remote process failed with an unhandled exception.", stackTrace) { }
            }
        }
    }

    /// <summary>Options used with RemoteInvoke.</summary>
    public sealed class RemoteInvokeOptions
    {
        public bool Start { get; set; } = true;
        public ProcessStartInfo StartInfo { get; set; } = new ProcessStartInfo();
        public bool EnableProfiling { get; set; } = true;
        public bool CheckExitCode { get; set; } = true;

        public int TimeOut {get; set; } = RemoteExecutorTestBase.FailWaitTimeoutMilliseconds;
        public int ExpectedExitCode { get; set; } = RemoteExecutorTestBase.SuccessExitCode;
        public string ExceptionFile { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }
}
